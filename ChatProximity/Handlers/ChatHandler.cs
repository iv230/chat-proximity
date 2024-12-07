using System;
using ChatProximity.Config;
using ChatProximity.Service;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Common.Math;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;

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
            ChatProximity.Log.Verbose("Message considered as handled, abort");
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
            if (currentCharacter == null)
            {
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

            float? distance = GetDistance(currentCharacter->Position, senderCharacter->Position);

            if (Math.Abs(config.Range - config.Threshold) > 0.001f && distance > config.Threshold)
            {
                ChatProximity.Log.Info($"Messaged dropped: [{type.ToString()}] {senderCharacter->NameString} : {message.TextValue}");
                ChatProximity.Log.Debug($"Distance {distance} exceeds threshold {config.Threshold}");
                ChatMessageService.DropMessage(ref message, ref isHandled);
                return;
            }
            
            ChatProximity.Log.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->NameString)}");

            RecolorMode recolorMode;

            if (Plugin.Configuration.RecolorTargeted &&
                currentCharacter->EntityId == senderCharacter->GetTargetId().ObjectId)
            {
                recolorMode = RecolorMode.Targeting;
            }
            else if (Plugin.Configuration.RecolorTargeting &&
                     currentCharacter->GetTargetId().ObjectId == senderCharacter->EntityId)
            {
                recolorMode = RecolorMode.Targeted;
            }
            else
            {
                recolorMode = RecolorMode.Distance;
            }

            var colorKey = GetColor(recolorMode, config, distance);
            ChatMessageService.HandleMessage(ref message, colorKey);
        }
        catch (Exception e)
        {
            ChatProximity.Log.Error(e, "Exception while processing message");
        }
        finally
        {
            ChatProximity.Log.Debug("Finished processing message");   
        }
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
    /// Get the color according to the mode and pre-calculated data.
    /// </summary>
    /// <param name="mode">The coloring mode</param>
    /// <param name="config">The used config for this type</param>
    /// <param name="distance">The pre-calculated distance (if applicable)</param>
    /// <returns>A UI payload containing the color</returns>
    private static Vector4 GetColor(RecolorMode mode, ChatTypeConfig config, float? distance = null)
    {
        switch (mode)
        {
            case RecolorMode.Distance:
                if (distance == null)
                    throw new ArgumentException("Distance must be provided for RecolorMode.Distance");

                var ratio = Math.Clamp(distance.Value / config.Range, 0f, 1f);

                var nearColor = config.NearColor;
                var farColor = config.FarColor;

                // Interpolate each component of the color
                return new Vector4(
                    nearColor.X + ((farColor.X - nearColor.X) * ratio),
                    nearColor.Y + ((farColor.Y - nearColor.Y) * ratio),
                    nearColor.Z + ((farColor.Z - nearColor.Z) * ratio),
                    nearColor.W + ((farColor.W - nearColor.W) * ratio)
                );

            case RecolorMode.Targeting:
                return config.TargetingColor;

            case RecolorMode.Targeted:
                return config.TargetedColor;

            case RecolorMode.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
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
