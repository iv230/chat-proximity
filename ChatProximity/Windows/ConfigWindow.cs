using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ChatProximity.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(ChatProximity chatProximity) : base("Chat Proximity Config")
    {
        SizeCondition = ImGuiCond.Always;

        configuration = chatProximity.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        var recolorSayChat = configuration.RecolorSayChat;
        if (ImGui.Checkbox("Recolor Say chat", ref recolorSayChat))
        {
            configuration.RecolorSayChat = recolorSayChat;
            configuration.Save();
        }
        
        var verticalIncrease = configuration.VerticalIncrease;
        if (ImGui.Checkbox("Increase vertical distance incidence", ref verticalIncrease))
        {
            configuration.VerticalIncrease = verticalIncrease;
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
