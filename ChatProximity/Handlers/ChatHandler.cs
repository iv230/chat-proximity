using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace ChatProximity.Handlers;

internal partial class ChatHandler(ChatProximityPlugin chatProximityPlugin)
{
    public const int SayRange = 20;

    public ChatProximityPlugin ChatProximityPlugin { get; init; } = chatProximityPlugin;
    
    [System.Text.RegularExpressions.GeneratedRegex("[★●▲♦♥♠♣]")]
    private static partial System.Text.RegularExpressions.Regex FriendIconsRegex();

    /// <summary>
    /// Main message handling process
    /// </summary>
    /// <param name="type">Message type</param>
    /// <param name="sender">Character who sent the message</param>
    /// <param name="message">The message content</param>
    /// <param name="isHandled">Whenever the message is handled or not</param>
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

            ChatProximityPlugin.PluginLog.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->NameString)}");
            ChatProximityPlugin.PluginLog.Verbose($"Message is {message.ToJson()}");

            var distance = GetDistance(currentPlayer->Position, senderCharacter->Position);
            var colorKey = GetColor(distance);

            if (IsMessageDirty(message))
            {
                HandleDirtyMessage(ref message, colorKey);
            }
            else
            {
                HandleNotDirtyMessage(ref message, colorKey);
            }
            ChatProximityPlugin.PluginLog.Verbose($"New message is {message.ToJson()}");
        }
        catch (Exception e)
        {
            ChatProximityPlugin.PluginLog.Error(e, "Exception while processing message");
        }

        ChatProximityPlugin.PluginLog.Debug("Finished processing message");
    }

    /// <summary>
    /// Retrieve the sender object from its name
    /// </summary>
    /// <param name="sender">The sender name</param>
    /// <returns>The sender object pointers</returns>
    private static unsafe BattleChara* GetSender(SeString sender)
    {
        var senderName = FriendIconsRegex().Replace(sender.ToString(), "");
        return CharacterManager.Instance()->LookupBattleCharaByName(senderName, true);
    }
    
    /// <summary>
    /// Indicates is a message is already touched by Dalamud or another plugin
    /// </summary>
    /// <param name="message">The message to check</param>
    /// <returns>A boolean indicating if the message is dirty or not</returns>
    private static bool IsMessageDirty(SeString message)
    {
        return message.Payloads.Count > 0 && message.Payloads[0].Dirty;
    }

    /// <summary>
    /// Anonymise if needed a player name
    /// </summary>
    /// <param name="playerName">The player name to anonymise</param>
    /// <returns>The two first characters of the name followed by ... if anonymise is enabled, the whole name otherwise</returns>
    private String GetPlayerNameForLog(String playerName)
    {
        return ChatProximityPlugin.Configuration.AnonymiseNames && playerName.Length > 2
                   ? playerName[..2] + "..."
                   : playerName;
    }

    /// <summary>
    /// Anonymise if needed a message
    /// </summary>
    /// <param name="message">The message to anonymise</param>
    /// <returns>The two first characters of the message followed by ... if anonymise is enabled, the whole message otherwise</returns>
    private string GetMessageForLog(SeString message)
    {
        var messageText = message.ToString();
        return ChatProximityPlugin.Configuration.AnonymiseNames && messageText.Length > 2
                   ? messageText[..2] + "..."
                   : messageText;
    }

    /// <summary>
    /// Compute the distance between two players
    /// Add a weight to the vertical distance if related config is enabled
    /// </summary>
    /// <param name="player1">The first player position</param>
    /// <param name="player2">The second player position</param>
    /// <returns>The distance between those two players</returns>
    private float GetDistance(Vector3 player1, Vector3 player2)
    {
        var distanceVector = player1 - player2;

        if (ChatProximityPlugin.Configuration.VerticalIncrease)
        {
            distanceVector.Y *= 2;   
        }
            
        return distanceVector.Magnitude;
    }
    
    /// <summary>
    /// Get the color according to distance
    /// </summary>
    /// <param name="distance">The distance between the two players</param>
    /// <returns>A UI payload containing the color</returns>
    private static ushort GetColor(float distance)
    {
        var colors = new List<ushort> { 1, 2, 3, 4, 5 };
        var colorIndex = (int)(distance * colors.Count / SayRange);
        colorIndex = Math.Clamp(colorIndex, 0, colors.Count - 1);  // Ensure index is within bounds

        ChatProximityPlugin.PluginLog.Debug($"Computed distance: {distance}, index {colorIndex}");
        return colors[colorIndex];
    }

    /// <summary>
    /// Handles a dirty message by modifying its payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="colorKey">The color of the message</param>
    private void HandleDirtyMessage(ref SeString message, ushort colorKey)
    {
        ChatProximityPlugin.PluginLog.Debug("Message dirty");

        // Adding a first element to color the first chunk
        if (message.Payloads[0] is UIForegroundPayload { IsEnabled: false })
        {
            message.Payloads.Insert(0, new UIForegroundPayload(colorKey));
        }

        // Then process all other chunks
        for (var i = 1; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];
            if (payload is UIForegroundPayload { IsEnabled: false } currentForegroundPayload)
            {
                currentForegroundPayload.ColorKey = colorKey;
            }
        }

        // Adding a last color to prevent propagation to other lines
        message.Payloads.Insert(message.Payloads.Count, new UIForegroundPayload(0));
    }

    /// <summary>
    /// Handles a not dirty message by building a new payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="colorKey">The color of the message</param>
    private void HandleNotDirtyMessage(ref SeString message, ushort colorKey)
    {
        ChatProximityPlugin.PluginLog.Debug("Message not dirty");
        List<Payload> finalPayload =
        [
            new UIForegroundPayload(colorKey),
            new TextPayload(message.TextValue),
            UIForegroundPayload.UIForegroundOff

        ];

        message = new SeString(finalPayload);
    }
}
