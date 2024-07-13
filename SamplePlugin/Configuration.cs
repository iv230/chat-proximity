using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace ChatProximity;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool RecolorSayChat { get; set; } = true;
    public bool AnonymiseNames { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
