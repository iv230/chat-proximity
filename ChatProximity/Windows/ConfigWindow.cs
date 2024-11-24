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

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(900, 400);
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        ImGui.TextUnformatted("General Settings");
        ImGui.Spacing();
        DrawGeneralConfig();

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.TextUnformatted("Chat Color Configuration");
        ImGui.Spacing();
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

        var recolorTargeting = configuration.RecolorTargeting;
        if (ImGui.Checkbox("Recolor message when targeting sender", ref recolorTargeting))
        {
            configuration.RecolorTargeting = recolorTargeting;
            configuration.Save();
        }
        DrawTooltip("(?)", "When enabled, messages sent by a player you are targeting will have a specific color, allowing better identification of their messages.");

        var recolorTargeted = configuration.RecolorTargeted;
        if (ImGui.Checkbox("Recolor message when sender targets you", ref recolorTargeted))
        {
            configuration.RecolorTargeted = recolorTargeted;
            configuration.Save();
        }
        DrawTooltip("(?)", "When enabled, messages from a player targeting you will have a specific color, making them easier to identify.");

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
        var columnCount = 4;

        if (configuration.RecolorTargeting) columnCount++;
        if (configuration.RecolorTargeted) columnCount++;

        if (ImGui.BeginTable("ChatTypesTable##", columnCount))
        {
            ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Near Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Far Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

            if (configuration.RecolorTargeting)
                ImGui.TableSetupColumn("Targeting Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

            if (configuration.RecolorTargeted)
                ImGui.TableSetupColumn("Targeted Color", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

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
        var targetingColor = chatTypeConfig.TargetingColor;
        var targetedColor = chatTypeConfig.TargetedColor;

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
        if (ImGui.ColorEdit4("##closestColor", ref closestColor, ImGuiColorEditFlags.NoInputs))
        {
            chatTypeConfig.NearColor = closestColor;
            configuration.Save();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.ColorEdit4("##farthestColor", ref farthestColor, ImGuiColorEditFlags.NoInputs))
        {
            chatTypeConfig.FarColor = farthestColor;
            configuration.Save();
        }

        if (configuration.RecolorTargeting)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit4("##targetingColor", ref targetingColor, ImGuiColorEditFlags.NoInputs))
            {
                chatTypeConfig.TargetingColor = targetingColor;
                configuration.Save();
            }
        }

        if (configuration.RecolorTargeted)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit4("##targetedColor", ref targetedColor, ImGuiColorEditFlags.NoInputs))
            {
                chatTypeConfig.TargetedColor = targetedColor;
                configuration.Save();
            }
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
