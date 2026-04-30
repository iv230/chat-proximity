using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ChatProximity.Config;
using ChatProximity.i18n;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace ChatProximity.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(ChatProximity chatProximity) : base(I18N.T("config.window.title"))
    {
        configuration = chatProximity.Configuration;

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(900, 400);

        TitleBarButtons =
        [
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.Heart,
                Click = _ => {
                    Process.Start(new ProcessStartInfo {FileName = "https://ko-fi.com/iriswhm", UseShellExecute = true});
                },
                IconOffset = new Vector2(2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(I18N.T("config.support.kofi"));
                    ImGui.EndTooltip();
                }
            },
        ];
    }

    public void Dispose() => GC.SuppressFinalize(this);
    public override void PreDraw() { }

    public override void Draw()
    {
        ImGui.TextUnformatted(I18N.T("config.section.general"));
        ImGui.Spacing();
        DrawGeneralConfig();

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.TextUnformatted(I18N.T("config.section.chatColors"));
        ImGui.Spacing();
        DrawChatTable();
    }

    /// <summary>
    /// Draws a list of checkboxes representing general plugin config
    /// </summary>
    private void DrawGeneralConfig()
    {
        var verticalIncrease = configuration.VerticalIncrease;
        if (ImGui.Checkbox(I18N.T("config.general.verticalIncrease"), ref verticalIncrease))
        {
            configuration.VerticalIncrease = verticalIncrease;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.verticalIncrease.tooltip"));

        var insideReducer = configuration.InsideReducer;
        if (ImGui.Checkbox(I18N.T("config.general.insideReducer"), ref insideReducer))
        {
            configuration.InsideReducer = insideReducer;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.insideReducer.tooltip"));

        var recolorTargeting = configuration.RecolorTargeting;
        if (ImGui.Checkbox(I18N.T("config.general.recolorTargeting"), ref recolorTargeting))
        {
            configuration.RecolorTargeting = recolorTargeting;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.recolorTargeting.tooltip"));

        var recolorTargeted = configuration.RecolorTargeted;
        if (ImGui.Checkbox(I18N.T("config.general.recolorTargeted"), ref recolorTargeted))
        {
            configuration.RecolorTargeted = recolorTargeted;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.recolorTargeted.tooltip"));
        
        var recolorFocusTarget = configuration.RecolorFocusTarget;
        if (ImGui.Checkbox(I18N.T("config.general.recolorFocusTarget"), ref recolorFocusTarget))
        {
            configuration.RecolorFocusTarget = recolorFocusTarget;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.recolorFocusTarget.tooltip"));

        var editThreshold = configuration.EditThreshold;
        if (ImGui.Checkbox(I18N.T("config.general.editThreshold"), ref editThreshold))
        {
            configuration.EditThreshold = editThreshold;
            if (!editThreshold)
                configuration.ResetAllThreshold();
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.editThreshold.tooltip"));

        var anonymise = configuration.AnonymiseNames;
        if (ImGui.Checkbox(I18N.T("config.general.anonymiseLogs"), ref anonymise))
        {
            configuration.AnonymiseNames = anonymise;
            configuration.Save();
        }
        DrawTooltip("(?)", I18N.T("config.general.anonymiseLogs.tooltip"));
    }

    /// <summary>
    /// Draws the table handling config for each chat type
    /// </summary>
    private void DrawChatTable()
    {
        var columnCount = 4;

        if (configuration.RecolorTargeting) columnCount++;
        if (configuration.RecolorTargeted) columnCount++;
        if (configuration.EditThreshold) columnCount++;
        if (configuration.RecolorFocusTarget) columnCount++;

        if (ImGui.BeginTable("ChatTypesTable##", columnCount))
        {
            ImGui.TableSetupColumn(I18N.T("table.channel"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(I18N.T("table.enabled"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(I18N.T("table.nearColor"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(I18N.T("table.farColor"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

        if (configuration.RecolorTargeting)
            ImGui.TableSetupColumn(I18N.T("table.targetingColor"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

        if (configuration.RecolorTargeted)
            ImGui.TableSetupColumn(I18N.T("table.targetedColor"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

        if (configuration.RecolorFocusTarget)
            ImGui.TableSetupColumn(I18N.T("table.focusTargetColor"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

        if (configuration.EditThreshold)
            ImGui.TableSetupColumn(I18N.T("table.threshold"), ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            ImGuiClip.ClippedDraw(
                configuration.ChatTypeConfigs.Values.ToList(),
                DrawChatTableLine,
                ImGui.GetTextLineHeight() + (3 * ImGui.GetStyle().FramePadding.Y)
            );

            ImGui.EndTable();
        }
    }

    /// <summary>
    /// Draws the line for a chat type config
    /// </summary>
    /// <param name="chatTypeConfig">The config to draw the line for</param>
    private void DrawChatTableLine(ChatTypeConfig chatTypeConfig)
    {
        using var id = ImRaii.PushId(chatTypeConfig.Type.ToString());

        var enabled = chatTypeConfig.Enabled;
        var closestColor = chatTypeConfig.NearColor;
        var farthestColor = chatTypeConfig.FarColor;
        var targetingColor = chatTypeConfig.TargetingColor;
        var targetedColor = chatTypeConfig.TargetedColor;
        var focusTargetColor = chatTypeConfig.FocusTargetColor;

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
        
        if (configuration.RecolorFocusTarget)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit4("##focusTargetColor", ref focusTargetColor, ImGuiColorEditFlags.NoInputs))
            {
                chatTypeConfig.FocusTargetColor = focusTargetColor;
                configuration.Save();
            }
        }

        if (configuration.EditThreshold)
        {
            DrawThresholdCell(chatTypeConfig);
        }
    }

    /// <summary>
    /// Draws the cell containing the slider for editing the threshold
    /// </summary>
    /// <param name="config">The concerned chat type</param>
    private void DrawThresholdCell(ChatTypeConfig config)
    {
        var min = config.Range * 0.1f;
        var max = config.Range;
        var threshold = config.Threshold;

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);

        if (ImGui.SliderFloat("##threshold", ref threshold, min, max, "%.1f"))
        {
            config.Threshold = Math.Clamp(threshold, min, max);
            configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(I18N.T("threshold.tooltip"));
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("EditThresholdPopup");
        }

        if (ImGui.BeginPopup("EditThresholdPopup"))
        {
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputFloat("Edit Threshold##input", ref threshold, 0.1f, 1.0f, "%.1f"))
            {
                config.Threshold = Math.Clamp(threshold, min, max);
                configuration.Save();
            }
            if (ImGui.Button(I18N.T("common.close")))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Draws a tooltip element
    /// </summary>
    /// <param name="tooltip">The text that will show the tooltip if hovered</param>
    /// <param name="text">The tooltip content</param>
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
