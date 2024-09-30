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
        DrawGeneralConfig();
        DrawChatTable();
    }

    private void DrawGeneralConfig()
    {
        var verticalIncrease = configuration.VerticalIncrease;
        if (ImGui.Checkbox("Increase vertical distance incidence", ref verticalIncrease))
        {
            configuration.VerticalIncrease = verticalIncrease;
            configuration.Save();
        }
        DrawTooltip("(?)", "Enabling this setting increases the importance of vertical distance when calculating proximity. Vertical distances will weigh more heavily when determining chat range, which can be useful in multi-floor or elevated areas.");

        var insideReducer = configuration.InsideReducer;
        if (ImGui.Checkbox("Reduce the chat range while inside a housing", ref insideReducer))
        {
            configuration.InsideReducer = insideReducer;
            configuration.Save();
        }
        DrawTooltip("(?)", "When enabled, this option reduces the effective chat range while inside a house or apartment, allowing for more immersive communication by adjusting distance calculations.");

        var anonymise = configuration.AnonymiseNames;
        if (ImGui.Checkbox("Anonymise player names in logs", ref anonymise))
        {
            configuration.AnonymiseNames = anonymise;
            configuration.Save();
        }
        DrawTooltip("(?)", "When this option is enabled, player names will be anonymized in Dalamud's debug logs. This is useful for protecting privacy in logs created for plugin debugging or troubleshooting.");
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

    private static void DrawTooltip(String tooltip, String text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled(tooltip);
        if (ImGui.IsItemHovered())
        { 
            ImGui.SetNextWindowSize(new Vector2(300, 0), ImGuiCond.Always);
            ImGui.BeginTooltip();
            ImGui.TextWrapped(text);
            ImGui.EndTooltip();
        }
    }
}
