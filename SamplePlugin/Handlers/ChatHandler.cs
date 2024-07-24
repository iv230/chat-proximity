using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ChatProximity.Handlers
{
    internal class ChatHandler(Plugin Plugin)
    {
        public static int SAY_RANGE = 20;

        public Plugin Plugin { get; init; } = Plugin;

        public unsafe void OnMessage(XivChatType type, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isHandled)
            {
                Plugin.PluginLog.Verbose("Message considered has handled, abort");
                return;
            }

            if (type != XivChatType.Say || !Plugin.Configuration.RecolorSayChat)
            {
                Plugin.PluginLog.Verbose("Not a say message, or config is disabled, abort");
                return;
            }

            if (sender == null)
            {
                Plugin.PluginLog.Warning($"No sender character found");
                return;
            }

            Plugin.PluginLog.Debug($"Caught {type} message from {GetPlayerNameForLog(sender.TextValue.ToString())}: {GetMessageForLog(message)}");
            try
            {
                List<Payload> finalPayload = [];

                var currentPlayer = (BattleChara*)(Plugin.ClientState.LocalPlayer?.Address ?? 0);

                if (currentPlayer == null) {
                    Plugin.PluginLog.Warning("Current player is null");
                    return;
                }

                var senderCharacter = GetSender(sender);

                if (senderCharacter == null)
                {
                    Plugin.PluginLog.Warning($"Could not resolve sender character: {sender}");
                    return;
                }

                if (senderCharacter == currentPlayer)
                {
                    Plugin.PluginLog.Debug("Self message, no color change");
                    return;
                }

                Plugin.PluginLog.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->Name.ToString())}");
                var distance = Vector3.Distance(currentPlayer->Position, senderCharacter->Position);

                finalPayload.Add(GetColor(distance));
                finalPayload.Add(new TextPayload(message.TextValue));
                finalPayload.Add(UIForegroundPayload.UIForegroundOff);

                message = new SeString(finalPayload);
            }
            catch (Exception e)
            {
                Plugin.PluginLog.Error("Exception while processing message", e);
            }

            Plugin.PluginLog.Debug("Finished processing message");
        }

        private unsafe BattleChara* GetSender(SeString sender)
        {
            var senderName = sender.ToString().Replace("★", "").Replace("●", "").Replace("▲", "").Replace("♦", "").Replace("♥", "").Replace("♠", "").Replace("♣", "");
            return CharacterManager.Instance()->LookupBattleCharaByName(senderName, true);
        }

        private String GetPlayerNameForLog(String playerName)
        {
            if (playerName == null)
            {
                return "";
            }

            if (Plugin.Configuration.AnonymiseNames)
            {
                return playerName.ToString()[..2] + "...";
            }

            return playerName;
        }

        private String GetMessageForLog(SeString message)
        {
            if (message == null)
            {
                return "";
            }

            if (message.ToString().Length <= 2)
            {
                return message.ToString();
            }

            if (Plugin.Configuration.AnonymiseNames)
            {
                return message.ToString()[..2] + "...";
            }

            return message.ToString(); 
        }

        private static UIForegroundPayload GetColor(float distance)
        {
            var colors = new List<UIForegroundPayload> { new(1), new(2), new(3), new(4), new(5) };
            var colorIndex = (int)distance * colors.Count / SAY_RANGE;
            Plugin.PluginLog.Debug($"Computed distance: {distance}, index {colorIndex}");

            return colors[colorIndex];
        }
    }
}
