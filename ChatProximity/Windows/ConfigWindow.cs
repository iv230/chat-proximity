using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ChatProximity.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(ChatProximityPlugin chatProximityPlugin) : base("Chat Proximity Config")
    {
        //Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        configuration = chatProximityPlugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        var configValue = configuration.RecolorSayChat;
        if (ImGui.Checkbox("Recolor Say chat", ref configValue))
        {
            configuration.RecolorSayChat = configValue;
            configuration.Save();
        }

        var anonymise = configuration.AnonymiseNames;
        if (ImGui.Checkbox("Anonymise player names in logs", ref anonymise))
        {
            configuration.AnonymiseNames = anonymise;
            configuration.Save();
        }
    }
}
