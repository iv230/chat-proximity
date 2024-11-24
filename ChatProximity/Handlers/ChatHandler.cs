using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using ChatProximity.Config;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Text.ReadOnly;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

namespace ChatProximity.Handlers;

internal class ChatHandler(ChatProximity plugin)
{
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
        
        Plugin.Configuration.ChatTypeConfigs.TryGetValue(type, out var config);

        if (config is not { Enabled: true })
        {
            ChatProximity.Log.Verbose($"Not supported message or config disabled (config is {config?.Type.ToString() ?? "null"} for type {type})");
            return;
        }

        ChatProximity.Log.Debug($"Caught {type} message from {GetPlayerNameForLog(sender.TextValue)}: {GetMessageForLog(message)}");
        try
        {
            var currentCharacter = (BattleChara*)(ChatProximity.ClientState.LocalPlayer?.Address ?? 0);
            if (currentCharacter == null) {
                ChatProximity.Log.Warning("Current player is null");
                return;
            }

            var senderCharacter = GetSender(sender);
            if (senderCharacter == null)
            {
                ChatProximity.Log.Warning($"Could not resolve sender character: {sender}");
                return;
            }

            if (senderCharacter == currentCharacter)
            {
                ChatProximity.Log.Debug("Self message, no color change");
                return;
            }

            ChatProximity.Log.Debug($"IDS:\ncurrentCharacter is {currentCharacter->EntityId} and targets {currentCharacter->GetTargetId().ObjectId}\nsenderCharacter is {senderCharacter->EntityId} and targets {senderCharacter->GetTargetId().ObjectId}");

            Vector4 colorKey;

            if (Plugin.Configuration.RecolorTargeted && currentCharacter->EntityId == senderCharacter->GetTargetId().ObjectId)
            {
                ChatProximity.Log.Debug("Sender is targeting, recolor");
                colorKey = Plugin.Configuration.TargetedColor;
            }
            else if (Plugin.Configuration.RecolorTargeting && currentCharacter->GetTargetId().ObjectId == senderCharacter->EntityId)
            {
                ChatProximity.Log.Debug("Targeting sender, recolor");
                colorKey = Plugin.Configuration.TargetingColor;
            }
            else
            {
                ChatProximity.Log.Debug("Recoloring over distance");
                var distance = GetDistance(currentCharacter->Position, senderCharacter->Position);
                colorKey = GetColor(distance, config);
            }

            ChatProximity.Log.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->NameString)}");
            ChatProximity.Log.Verbose($"Message is {message.ToJson()}");

            HandleMessage(ref message, colorKey);
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

        foreach (var payload in sender.Payloads)
        {
            var foundSender = payload switch
            {
                PlayerPayload playerPayload => CharacterManager.Instance()->LookupBattleCharaByName(playerPayload.PlayerName, true),
                TextPayload textPayload => CharacterManager.Instance()->LookupBattleCharaByName(textPayload.Text, true),
                _ => null
            };

            if (foundSender is not null)
                return foundSender;
        }

        return null;
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
            ChatProximity.Log.Verbose("Increasing vertical incidence");
            distanceVector.Y *= 2;   
        }

        if (Plugin.Configuration.InsideReducer && IsIndoor())
        {
            ChatProximity.Log.Verbose("Indoor, increasing distance");
            distanceVector *= 2;
        }
            
        return distanceVector.Magnitude;
    }

    /// <summary>
    /// Get the color according to distance
    /// </summary>
    /// <param name="distance">The distance between the two players</param>
    /// <param name="config">The used config for this type</param>
    /// <returns>A UI payload containing the color</returns>
    private static Vector4 GetColor(float distance, ChatTypeConfig config)
    {
       var ratio = Math.Clamp(distance / config.Range, 0f, 1f);

        var nearColor = config.NearColor;
        var farColor = config.FarColor;
        
        // Interpolate each component of the color
        var r = nearColor.X + ((farColor.X - nearColor.X) * ratio);
        var g = nearColor.Y + ((farColor.Y - nearColor.Y) * ratio);
        var b = nearColor.Z + ((farColor.Z - nearColor.Z) * ratio);
        var a = nearColor.W + ((farColor.W - nearColor.W) * ratio);

        var vector = new Vector4(r, g, b, a);
        ChatProximity.Log.Debug($"Computed distance: {distance}, color {vector}");

        // Return the interpolated color
        return vector;
    }

    /// <summary>
    /// Handles a dirty message by modifying its payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="color">The computed color</param>
    private static void HandleMessage(ref SeString message, Vector4 color)
    {
        var sb = new SeStringBuilder();

        // Extracting and processing payloads
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];

            if (payload is TextPayload textPayload)
            {
                ChatProximity.Log.Verbose($"i = {i}");
                var previousPayLoad = i > 0 ? message.Payloads[i - 1] : null;

                if (previousPayLoad is UIForegroundPayload { IsEnabled: true } previousPayload)
                    sb.PushColorType(previousPayload.ColorKey);
                else
                    sb.PushColorRgba((byte)(color.X*255), (byte)(color.Y*255), (byte)(color.Z*255), (byte)(color.W*255));

                sb.Append(textPayload.Text);
                sb.PopColor();
            }
            else if (payload is not UIForegroundPayload)
            {
                sb.Append(new ReadOnlySeStringSpan(payload.Encode()));
            }
        }

        // Update the message with the new payloads
        message = sb.ToSeString().ToDalamudString();
    }

    /// <summary>
    /// Indicates whenever or ot the local player is indoor
    /// </summary>
    /// <returns>A boolean indicating if the player is indoor</returns>
    private static unsafe bool IsIndoor()
    {
        return HousingManager.Instance()->IsInside();
    }
}
