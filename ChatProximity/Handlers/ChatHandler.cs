using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using ChatProximity.Strategies;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace ChatProximity.Handlers;

internal class ChatHandler(ChatProximity plugin)
{
    public const int SayRange = 20;

    public ChatProximity Plugin { get; init; } = plugin;

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
            ChatProximity.Log.Verbose("Message considered has handled, abort");
            return;
        }

        if (type != XivChatType.Say || !Plugin.Configuration.RecolorSayChat)
        {
            ChatProximity.Log.Verbose("Not a say message, or config is disabled, abort");
            return;
        }

        ChatProximity.Log.Debug($"Caught {type} message from {GetPlayerNameForLog(sender.TextValue)}: {GetMessageForLog(message)}");
        try
        {
            var currentPlayer = (BattleChara*)(ChatProximity.ClientState.LocalPlayer?.Address ?? 0);
            if (currentPlayer == null) {
                ChatProximity.Log.Warning("Current player is null");
                return;
            }

            var senderCharacter = GetSender(sender);
            if (senderCharacter == null)
            {
                ChatProximity.Log.Warning($"Could not resolve sender character: {sender}");
                return;
            }

            if (senderCharacter == currentPlayer)
            {
                ChatProximity.Log.Debug("Self message, no color change");
                return;
            }

            ChatProximity.Log.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->NameString)}");
            ChatProximity.Log.Verbose($"Message is {message.ToJson()}");

            var distance = GetDistance(currentPlayer->Position, senderCharacter->Position);
            var colorKey = GetColor(distance);

            GetStrategy(message).HandleMessage(ref message, colorKey);
            ChatProximity.Log.Verbose($"New message is {message.ToJson()}");
        }
        catch (Exception e)
        {
            ChatProximity.Log.Error(e, "Exception while processing message");
        }

        ChatProximity.Log.Debug("Finished processing message");
    }

    /// <summary>
    /// Retrieve the sender object from its name
    /// </summary>
    /// <param name="sender">The sender name</param>
    /// <returns>The sender object pointers</returns>
    private static unsafe BattleChara* GetSender(SeString sender)
    {
        ChatProximity.Log.Verbose($"Sender is {sender.ToJson()}");
        string? senderName = null;

        foreach (var payload in sender.Payloads)
        {
            if (payload is PlayerPayload playerPayload)
            {
                senderName = playerPayload.PlayerName;
                break;
            }

            if (payload is TextPayload textPayload && textPayload.Text == ChatProximity.ClientState.LocalPlayer?.Name.TextValue)
            {
                senderName = textPayload.Text;
                break;
            }
        }

        return senderName != null ? CharacterManager.Instance()->LookupBattleCharaByName(senderName, true) : null;
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
        return Plugin.Configuration.AnonymiseNames && playerName.Length > 2
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
        return Plugin.Configuration.AnonymiseNames && messageText.Length > 2
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

        if (Plugin.Configuration.VerticalIncrease)
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

        ChatProximity.Log.Debug($"Computed distance: {distance}, index {colorIndex}");
        return colors[colorIndex];
    }
    
    /// <summary>
    /// Get the strategy for message processing
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <returns>The related strategy</returns>
    private IMessageHandlerStrategy GetStrategy(SeString message)
    {
        return IsMessageDirty(message) ? new DirtyMessageHandlerStrategy() : new NotDirtyMessageHandlerStrategy();
    }
}
