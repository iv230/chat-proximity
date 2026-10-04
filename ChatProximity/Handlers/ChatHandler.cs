using System;
using ChatProximity.Service;
using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Common.Math;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;

namespace ChatProximity.Handlers;

internal class ChatHandler(ChatProximity plugin, ColorService colorService)
{
    public ChatProximity Plugin { get; init; } = plugin;
    private ColorService ColorService { get; init; } = colorService;

    /// <summary>
    /// Main message handling process
    /// </summary>
    /// <param name="message">The message object wrapper</param>
    public unsafe void OnMessage(IHandleableChatMessage message)
    {
        if (message.IsHandled)
        {
            ChatProximity.Log.Verbose("Message considered as handled, abort");
            return;
        }

        Plugin.Configuration.ChatTypeConfigs.TryGetValue(message.LogKind, out var config);

        if (config is not { Enabled: true })
        {
            ChatProximity.Log.Verbose($"Not supported message or config disabled" +
                                      $"(config is {config?.Type.ToString() ?? "null"} for type {message.LogKind})");
            return;
        }

        ChatProximity.Log.Debug($"Caught {message.LogKind} message from" +
                                $"{GetPlayerNameForLog(message.Sender.TextValue)}: {GetMessageForLog(message.Message)}");
        try
        {
            var currentCharacter = CharacterService.GetLocalPlayer();
            if (currentCharacter == null)
            {
                ChatProximity.Log.Warning("Current player is null");
                return;
            }

            var senderCharacter = GetSender(message.Sender);
            if (senderCharacter == null)
            {
                ChatProximity.Log.Warning($"Could not resolve sender character: {message.Sender}");
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
                ChatProximity.Log.Info($"Messaged dropped: [{message.LogKind.ToString()}] " +
                                       $"{GetPlayerNameForLog(senderCharacter->NameString)} : {message.Message }");
                ChatProximity.Log.Debug($"Distance {distance} exceeds threshold {config.Threshold}");
                message.PreventOriginal();
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

            var colorKey = ColorService.GetColor(recolorMode, config, distance);
            message.Message = ChatMessageService.HandleMessage(message.Message, colorKey);
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
    /// Indicates whenever or ot the local player is indoor
    /// </summary>
    /// <returns>A boolean indicating if the player is indoor</returns>
    private static unsafe bool IsIndoor()
    {
        return HousingManager.Instance()->IsInside();
    }
}
