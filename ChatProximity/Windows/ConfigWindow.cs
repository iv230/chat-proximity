using System;
using System.Linq;
using System.Numerics;
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
        configuration = chatProximity.Configuration;

        SizeCondition = ImGuiCond.Always;
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        Size = new Vector2(850, 250);
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
            ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Near Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed, 320);
            ImGui.TableSetupColumn("Far Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed, 320);
            ImGui.TableHeadersRow();

            ImGuiClip.ClippedDraw(configuration.ChatTypeConfigs.Values.ToList(), DrawChatTableLine, ImGui.GetTextLineHeight() + (3 * ImGui.GetStyle().FramePadding.Y));

            ImGui.EndTable();
        }
    }

    private void DrawChatTableLine(ChatTypeConfig chatTypeConfig)
    {
        using var id = ImRaii.PushId(chatTypeConfig.Type.ToString());

        var enabled = chatTypeConfig.Enabled;
        var closestColor = chatTypeConfig.NearColor;
        var farthestColor = chatTypeConfig.FarColor;

        ImGui.TableNextColumn();
        ImGui.Text(chatTypeConfig.Type.ToString());

        ImGui.TableNextColumn();
        if (ImGui.Checkbox("##enabled", ref enabled))
        {
            chatTypeConfig.Enabled = enabled;
            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.ColorEdit4("##closestColor", ref closestColor))
        {
            chatTypeConfig.NearColor = closestColor;
            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.ColorEdit4("##farthestColor", ref farthestColor))
        {
            chatTypeConfig.FarColor = farthestColor;
            configuration.Save();
        }
    }
}
