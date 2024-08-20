using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool VerticalIncrease { get; set; } = true;
    public bool AnonymiseNames { get; set; } = true;
    
    public Dictionary<XivChatType, ChatTypeConfig> ChatTypeConfigs { get; set; } = new()
    {
        { XivChatType.Say, new ChatTypeConfig(XivChatType.Say, true, 20, new Vector4(1f, 1f, 1f, 1f), new Vector4(0.3f, 0.3f, 0.3f, 1f)) },
        { XivChatType.Yell, new ChatTypeConfig(XivChatType.Yell, true, 100, new Vector4(1f, 0.85f, 0.01f, 1f), new Vector4(0.29f, 0.25f, 0.11f, 1f)) },
        { XivChatType.CustomEmote, new ChatTypeConfig(XivChatType.CustomEmote, true, 20, new Vector4(0.72f, 1f, 0.94f, 1f), new Vector4(0.21f, 0.29f, 0.28f, 1f)) }
    };

    public void Save()
    {
        ValidateAndMigrate();
        ChatProximity.PluginInterface.SavePluginConfig(this);
    }

    private void ValidateAndMigrate()
    {
        if (!ChatTypeConfigs.TryGetValue(XivChatType.Say, out _))
        {
            ChatTypeConfigs[XivChatType.Say] = new ChatTypeConfig(XivChatType.Say, true, 20,
                                                                  new Vector4(1f, 1f, 1f, 1f),
                                                                  new Vector4(0.3f, 0.3f, 0.3f, 1f));
        }
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.Yell, out _))
        {
            ChatTypeConfigs[XivChatType.Say] = new ChatTypeConfig(XivChatType.Yell, true, 100,
                                                                  new Vector4(1f, 0.85f, 0.01f, 1f),
                                                                  new Vector4(0.29f, 0.25f, 0.11f, 1f));
        }
        
        if (!ChatTypeConfigs.TryGetValue(XivChatType.CustomEmote, out _))
        {
            ChatTypeConfigs[XivChatType.Say] = new ChatTypeConfig(XivChatType.CustomEmote, true, 20,
                                                                  new Vector4(0.72f, 1f, 0.94f, 1f), 
                                                                  new Vector4(0.21f, 0.29f, 0.28f, 1f));
        }
    }
}
