using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool VerticalIncrease { get; set; } = true;
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
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.Say, out _))
        {
            ChatTypeConfigs[XivChatType.Say] = new ChatTypeConfig(XivChatType.Say, true, 20,
                                                                  new Vector4(1f, 1f, 1f, 1f),
                                                                  new Vector4(0.23f, 0.23f, 0.23f, 1f));

            updated = true;
            ChatProximity.Log.Info("Created config for Say chat");
        }
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.Yell, out _))
        {
            ChatTypeConfigs[XivChatType.Yell] = new ChatTypeConfig(XivChatType.Yell, true, 100,
                                                                  new Vector4(1f, 0.85f, 0.01f, 1f),
                                                                  new Vector4(0.29f, 0.25f, 0.01f, 1f));
            
            updated = true;
            ChatProximity.Log.Info("Created config for Yell chat");
        }
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.StandardEmote, out _))
        {
            ChatTypeConfigs[XivChatType.StandardEmote] = new ChatTypeConfig(XivChatType.StandardEmote, true, 20,
                                                                          new Vector4(0.72f, 1f, 0.94f, 1f), 
                                                                          new Vector4(0.21f, 0.29f, 0.28f, 1f));
            
            updated = true;
            ChatProximity.Log.Info("Created config for Standard Emote chat");
        }
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.CustomEmote, out _))
        {
            ChatTypeConfigs[XivChatType.CustomEmote] = new ChatTypeConfig(XivChatType.CustomEmote, true, 20,
                                                                  new Vector4(0.72f, 1f, 0.94f, 1f), 
                                                                  new Vector4(0.21f, 0.29f, 0.28f, 1f));
            
            updated = true;
            ChatProximity.Log.Info("Created config for Custom Emote chat");
        }

        if (updated)
        {
            Save();
        }
    }
}
