using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

// ReSharper disable RedundantDefaultMemberInitializer
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool VerticalIncrease { get; set; } = true;
    public bool InsideReducer { get; set; } = true;
    public bool RecolorTargeting { get; set; } = false;
    public bool RecolorTargeted { get; set; } = false;
    public bool RecolorFocusTarget { get; set; } = false;
    public bool EditThreshold { get; set; } = false;
    public bool AnonymiseNames { get; set; } = true;
    
    public Dictionary<XivChatType, ChatTypeConfig> ChatTypeConfigs { get; set; } = new();

    public void Save()
    {
        ChatProximity.Log.Debug("Saving config...");
        ChatProximity.PluginInterface.SavePluginConfig(this);
    }

    public void ValidateAndMigrate()
    {
        Version = 1;
        var updated = false;

        // Default configurations for each chat type
        EnsureChatTypeConfig(
            XivChatType.Say, 
            true, 
            20, 
            new Vector4(1f, 1f, 1f, 1f),
            new Vector4(0.23f, 0.23f, 0.23f, 1f),
            new Vector4(0.5f, 0.8f, 1f, 1f),
            new Vector4(0.6f, 1f, 0.7f, 1f),
            new Vector4(0.5f, 0.8f, 1f, 1f),
            ref updated
        );

        EnsureChatTypeConfig(
            XivChatType.Yell, 
            true, 
            100, 
            new Vector4(1f, 0.85f, 0.01f, 1f),
            new Vector4(0.29f, 0.25f, 0.01f, 1f),
            new Vector4(1f, 0.6f, 0.3f, 1f),
            new Vector4(1f, 0.4f, 0.6f, 1f),
            new Vector4(0.5f, 0.8f, 1f, 1f),
            ref updated
        );

        EnsureChatTypeConfig(
            XivChatType.StandardEmote, 
            true, 
            20, 
            new Vector4(0.72f, 1f, 0.94f, 1f), 
            new Vector4(0.21f, 0.29f, 0.28f, 1f), 
            new Vector4(0.8f, 0.9f, 0.5f, 1f),
            new Vector4(0.6f, 0.7f, 1f, 1f),
            new Vector4(0.5f, 0.8f, 1f, 1f),
            ref updated
        );

        EnsureChatTypeConfig(
            XivChatType.CustomEmote, 
            true, 
            20, 
            new Vector4(0.72f, 1f, 0.94f, 1f), 
            new Vector4(0.21f, 0.29f, 0.28f, 1f), 
            new Vector4(0.8f, 0.9f, 0.5f, 1f),
            new Vector4(0.6f, 0.7f, 1f, 1f),
            new Vector4(0.5f, 0.8f, 1f, 1f),
            ref updated
        );

        if (updated)
        {
            Save();
        }
    }

    public void ResetAllThreshold()
    {
        foreach (var (_, config) in ChatTypeConfigs)
        {
            config.Threshold = config.Range;
        }
    }

    private void EnsureChatTypeConfig(XivChatType chatType, bool enabled, float range, Vector4 nearColor, Vector4 farColor, Vector4 targetingColor, Vector4 targetedColor, Vector4 focusTargetColor, ref bool updated)
    {
        if (!ChatTypeConfigs.TryGetValue(chatType, out var config))
        {
            ChatTypeConfigs[chatType] = new ChatTypeConfig(chatType, enabled, range, nearColor, farColor, targetingColor, targetedColor, focusTargetColor);
            ChatProximity.Log.Info($"Created config for {chatType} chat");
            updated = true;
        }
        else
        {
            // Migrate existing config to ensure it has the new properties
            var migrated = false;

            if (config.Enabled == false)
            {
                config.Enabled = enabled;
                migrated = true;
            }
            if (Math.Abs(config.Range - 0) < 0.0001f)
            {
                config.Range = range;
                migrated = true;
            }
            if (config.NearColor == default)
            {
                config.NearColor = nearColor;
                migrated = true;
            }
            if (config.FarColor == default)
            {
                config.FarColor = farColor;
                migrated = true;
            }
            if (config.TargetingColor == default)
            {
                config.TargetingColor = targetingColor;
                migrated = true;
            }
            if (config.TargetedColor == default)
            {
                config.TargetedColor = targetedColor;
                migrated = true;
            }
            if (config.FocusTargetColor == default)
            {
                config.FocusTargetColor = focusTargetColor;
                migrated = true;
            }
            if (Math.Abs(config.Threshold - 0) < 0.0001f)
            {
                config.Threshold = range;
                migrated = true;
            }

            if (migrated)
            {
                ChatProximity.Log.Info($"Migrated config for {chatType} chat to include new options.");
                updated = true;
            }
        }
    }
}
