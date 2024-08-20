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

    public bool RecolorSayChat { get; set; } = true;
    public bool VerticalIncrease { get; set; } = true;
    public bool AnonymiseNames { get; set; } = true;

    public ChatTypeConfig SayConfig { get; set; } = new(XivChatType.Say, true, new Vector4(1f, 1f, 1f, 1f), new Vector4(0.3f, 0.3f, 0.3f, 1f));
    public ChatTypeConfig ShoutConfig { get; set; } = new(XivChatType.Shout, true, new Vector4(1f, 0.85f, 0.01f, 1f), new Vector4(0.29f, 0.25f, 0.11f, 1f));
    public ChatTypeConfig EmoteConfig { get; set; } = new (XivChatType.CustomEmote, true, new Vector4(0.72f, 1f, 0.94f, 1f), new Vector4(0.21f, 0.29f, 0.28f, 1f));

    public void Save()
    {
        ChatProximity.PluginInterface.SavePluginConfig(this);
    }
}
