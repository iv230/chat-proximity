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

    // public List<XivChatType> AffectedChatTypes = [XivChatType.Say, XivChatType.Shout, XivChatType.CustomEmote];

    public bool RecolorSayChat { get; set; } = true;
    public bool VerticalIncrease { get; set; } = true;
    public bool AnonymiseNames { get; set; } = true;
    // public List<ChatTypeConfig> ChatTypeConfigs = [
    //     new ChatTypeConfig(XivChatType.Say, true, new Color(255, 255, 255, 255), new Color(100, 100, 100, 255)),
    //     new ChatTypeConfig(XivChatType.Shout, true, new Color(255, 255, 255, 255), new Color(100, 100, 100, 255)),
    //     new ChatTypeConfig(XivChatType.CustomEmote, true, new Color(255, 255, 255, 255), new Color(100, 100, 100, 255)),
    // ];

    public ChatTypeConfig SayConfig { get; set; } =
        new (XivChatType.Say, true, new Vector4(255, 255, 255, 255), new Vector4(100, 100, 100, 255));
    public ChatTypeConfig ShoutConfig { get; set; } =
        new (XivChatType.Shout, true, new Vector4(255, 255, 255, 255), new Vector4(100, 100, 100, 255));
    public ChatTypeConfig EmoteConfig { get; set; } =
        new (XivChatType.CustomEmote, true, new Vector4(255, 255, 255, 255), new Vector4(100, 100, 100, 255));

    public void Save()
    {
        ChatProximity.PluginInterface.SavePluginConfig(this);
    }
}
