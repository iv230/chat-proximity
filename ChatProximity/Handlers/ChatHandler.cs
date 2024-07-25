using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ChatProximity.Handlers;

internal class ChatHandler(ChatProximityPlugin chatProximityPlugin)
{
    public const int SayRange = 20;

    public ChatProximityPlugin ChatProximityPlugin { get; init; } = chatProximityPlugin;

    public unsafe void OnMessage(XivChatType type, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isHandled)
        {
            ChatProximityPlugin.PluginLog.Verbose("Message considered has handled, abort");
            return;
        }

        if (type != XivChatType.Say || !ChatProximityPlugin.Configuration.RecolorSayChat)
        {
            ChatProximityPlugin.PluginLog.Verbose("Not a say message, or config is disabled, abort");
            return;
        }

        ChatProximityPlugin.PluginLog.Debug($"Caught {type} message from {GetPlayerNameForLog(sender.TextValue)}: {GetMessageForLog(message)}");
        try
        {
            List<Payload> finalPayload = [];

            var currentPlayer = (BattleChara*)(ChatProximityPlugin.ClientState.LocalPlayer?.Address ?? 0);

            if (currentPlayer == null) {
                ChatProximityPlugin.PluginLog.Warning("Current player is null");
                return;
            }

            var senderCharacter = GetSender(sender);

            if (senderCharacter == null)
            {
                ChatProximityPlugin.PluginLog.Warning($"Could not resolve sender character: {sender}");
                return;
            }

            if (senderCharacter == currentPlayer)
            {
                ChatProximityPlugin.PluginLog.Debug("Self message, no color change");
                return;
            }

            ChatProximityPlugin.PluginLog.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->Name.ToString())}");
            var distance = Vector3.Distance(currentPlayer->Position, senderCharacter->Position);

            finalPayload.Add(GetColor(distance));
            finalPayload.Add(new TextPayload(message.TextValue));
            finalPayload.Add(UIForegroundPayload.UIForegroundOff);

            message = new SeString(finalPayload);
        }
        catch (Exception e)
        {
            ChatProximityPlugin.PluginLog.Error(e, "Exception while processing message");
        }

        ChatProximityPlugin.PluginLog.Debug("Finished processing message");
    }

    private unsafe BattleChara* GetSender(SeString sender)
    {
        var senderName = sender.ToString().Replace("★", "").Replace("●", "").Replace("▲", "").Replace("♦", "").Replace("♥", "").Replace("♠", "").Replace("♣", "");
        return CharacterManager.Instance()->LookupBattleCharaByName(senderName, true);
    }

    private String GetPlayerNameForLog(String playerName)
    {
        if (ChatProximityPlugin.Configuration.AnonymiseNames)
        {
            return playerName[..2] + "...";
        }

        return playerName;
    }

    private String GetMessageForLog(SeString message)
    {
        if (message.ToString().Length <= 2)
        {
            return message.ToString();
        }

        if (ChatProximityPlugin.Configuration.AnonymiseNames)
        {
            return message.ToString()[..2] + "...";
        }

        return message.ToString(); 
    }

    private static UIForegroundPayload GetColor(float distance)
    {
        var colors = new List<UIForegroundPayload> { new(1), new(2), new(3), new(4), new(5) };
        var colorIndex = (int)distance * colors.Count / SayRange;
        ChatProximityPlugin.PluginLog.Debug($"Computed distance: {distance}, index {colorIndex}");

        return colors[colorIndex];
    }
}
