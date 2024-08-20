using System;
using System.Linq;
using ChatProximity.Config;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
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

        DrawChatTable();
    }

    private void DrawChatTable()
    {
        if (ImGui.BeginTable("ChatTypesTable##", 4))
        {
            ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.NoHide);
            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.NoHide);
            ImGui.TableSetupColumn("Closest Color", ImGuiTableColumnFlags.NoHide);
            ImGui.TableSetupColumn("Farthest Color", ImGuiTableColumnFlags.NoHide);
            ImGui.TableHeadersRow();

            ImGuiClip.ClippedDraw(configuration.ChatTypeConfigs.Values.ToList(), DrawChatTableLine, ImGui.GetTextLineHeight() + (3 * ImGui.GetStyle().FramePadding.Y));

            ImGui.EndTable();
        }
    }

    private void DrawChatTableLine(ChatTypeConfig chatTypeConfig)
    {
        using var id = ImRaii.PushId(chatTypeConfig.Type.ToString());

        var enabled = chatTypeConfig.Enabled;
        var closestColor = chatTypeConfig.ClosestColor;
        var farthestColor = chatTypeConfig.FarthestColor;

        ImGui.TableNextColumn();
        ImGui.Text(chatTypeConfig.Type.ToString());

        ImGui.TableNextColumn();
        if (ImGui.Checkbox("Enabled##enabled", ref enabled))
        {
            chatTypeConfig.Enabled = enabled;
            configuration.Save();
        }

        ImGui.TableNextColumn();
        if (ImGui.ColorEdit4("##closestColor", ref closestColor))
        {
            chatTypeConfig.ClosestColor = closestColor;
            configuration.Save();
        }

        ImGui.TableNextColumn();
        if (ImGui.ColorEdit4("##farthestColor", ref farthestColor))
        {
            chatTypeConfig.FarthestColor = farthestColor;
            configuration.Save();
        }
    }
}
