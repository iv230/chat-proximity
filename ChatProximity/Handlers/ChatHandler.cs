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
            ChatProximity.Log.Verbose($"Not supported message or config disabled" +
                                      $"(config is {config?.Type.ToString() ?? "null"} for type {type})");
            return;
        }

        ChatProximity.Log.Debug($"Caught {type} message from" +
                                $"{GetPlayerNameForLog(sender.TextValue)}: {GetMessageForLog(message)}");
        try
        {
            var currentCharacter = CharacterService.GetLocalPlayer();
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

            if (Plugin.Configuration.EditThreshold && distance > config.Threshold)
            {
                ChatProximity.Log.Info($"Messaged dropped: [{type.ToString()}] " +
                                       $"{GetPlayerNameForLog(senderCharacter->NameString)} : {message.TextValue}");
                ChatProximity.Log.Debug($"Distance {distance} exceeds threshold {config.Threshold}");
                ChatMessageService.DropMessage(ref message, ref isHandled);
                return;
            }

            var focusedCharacter = CharacterService.GetFocusedBattleChara();

            ChatProximity.Log.Debug($"Found character: {GetPlayerNameForLog(senderCharacter->NameString)}");
            ChatProximity.Log.Verbose($"Character ID is {senderCharacter->EntityId}");
            ChatProximity.Log.Debug($"Found focused character: {(focusedCharacter != null
                                         ? GetPlayerNameForLog(focusedCharacter->NameString)
                                         : "null")}");
            ChatProximity.Log.Verbose($"Focused character ID is {(focusedCharacter != null
                                        ? focusedCharacter->EntityId.ToString()
                                        : "null")}");
            ChatProximity.Log.Verbose($"Config state: " +
                                      $"RecolorTargeted={Plugin.Configuration.RecolorTargeted} " +
                                      $"RecolorTargeting={Plugin.Configuration.RecolorTargeting} " +
                                      $"RecolorFocusTarget={Plugin.Configuration.RecolorFocusTarget}");

            RecolorMode recolorMode;
            if (Plugin.Configuration.RecolorTargeted &&
                currentCharacter->EntityId == senderCharacter->GetTargetId().ObjectId)
            {
                recolorMode = RecolorMode.Targeted;
            }
            else if (Plugin.Configuration.RecolorTargeting &&
                     currentCharacter->GetTargetId().ObjectId == senderCharacter->EntityId)
            {
                recolorMode = RecolorMode.Targeting;
            }
            else if (Plugin.Configuration.RecolorFocusTarget && focusedCharacter != null &&
                     focusedCharacter->EntityId == senderCharacter->EntityId)
            {
                recolorMode = RecolorMode.FocusTarget;
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
    /// Anonymize if needed a player name
    /// </summary>
    /// <param name="playerName">The player name to anonymize</param>
    /// <returns>The two first characters of the name followed by ... if anonymize is enabled, the whole name otherwise</returns>
    private String GetPlayerNameForLog(String playerName)
    {
        return Plugin.Configuration.AnonymiseNames && playerName.Length > 2
                   ? playerName[..2] + "..."
                   : playerName;
    }

    /// <summary>
    /// Anonymize if needed a message
    /// </summary>
    /// <param name="message">The message to anonymize</param>
    /// <returns>The two first characters of the message followed by ... if anonymize is enabled, the whole message otherwise</returns>
    private string GetMessageForLog(SeString message)
    {
        var messageText = message.ToString();
        return Plugin.Configuration.AnonymiseNames && messageText.Length > 2
                   ? messageText[..2] + "..."
                   : messageText;
    }

    /// <summary>
    /// Compute the distance between two players
    /// Add a weight to the vertical distance if the related config is enabled
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
        ChatProximity.Log.Debug($"Recolor mode is {mode}");
        Vector4 color;

        switch (mode)
        {
            case RecolorMode.Distance:
                if (distance == null)
                    throw new ArgumentException("Distance must be provided for RecolorMode.Distance");

                var ratio = Math.Clamp(distance.Value / config.Range, 0f, 1f);

                var nearColor = config.NearColor;
                var farColor = config.FarColor;

                // Interpolate each component of the color
                color = new Vector4(
                    nearColor.X + ((farColor.X - nearColor.X) * ratio),
                    nearColor.Y + ((farColor.Y - nearColor.Y) * ratio),
                    nearColor.Z + ((farColor.Z - nearColor.Z) * ratio),
                    nearColor.W + ((farColor.W - nearColor.W) * ratio)
                );
                break;

            case RecolorMode.Targeting:
                color = config.TargetingColor;
                break;

            case RecolorMode.Targeted:
                color = config.TargetedColor;
                break;
            
            case RecolorMode.FocusTarget:
                color = config.FocusTargetColor;
                break;

            case RecolorMode.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        ChatProximity.Log.Debug($"Got color {color}");
        return color;
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
