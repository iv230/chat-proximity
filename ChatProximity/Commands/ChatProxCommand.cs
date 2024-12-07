using System;
using System.Linq;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace ChatProximity.Commands;

public enum GeneralOptions
{
    Help,
    Conf,
    Chat
}

public enum ConfOptions
{
    Vertical,
    Inside,
    Targeting,
    Targeted,
    Anonymize,
    Threshold,
}

public enum ChatOptions
{
    Enabled,
    Near,
    Far,
    Targeting,
    Targeted,
    Threshold,
}

public class ChatProxCommand(ChatProximity plugin, string commandName)
{
    public void OnCommand(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            ShowHelp();
            return;
        }

        var splitArgs = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parameters = splitArgs.Skip(1).ToArray();
        
        if (!Enum.TryParse(splitArgs[0].ToLower(), true, out GeneralOptions command))
        {
            ChatProximity.ChatGui.Print($"Unknown command: {command}");
            return;
        }

        ChatProximity.Log.Debug($"Command: {command}, parameters: {string.Join(", ", parameters)}");

        switch (command)
        {
            case GeneralOptions.Help:
                ShowHelp();
                break;

            case GeneralOptions.Conf:
                HandleConfCommand(parameters);
                break;

            case GeneralOptions.Chat:
                HandleChatCommand(parameters);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    private void ShowHelp()
    {
        ChatProximity.ChatGui.Print($"{commandName} help - Displays this help menu");

        ChatProximity.ChatGui.Print("Configuration options:");
        foreach (var option in Enum.GetValues<ConfOptions>())
        {
            ChatProximity.ChatGui.Print($"- {commandName} conf {option.ToString().ToLower()} <on|off|toggle>");
        }

        ChatProximity.ChatGui.Print("Chat configuration options:");
        foreach (var chatOption in Enum.GetValues<ChatOptions>())
        {
            ChatProximity.ChatGui.Print(chatOption == ChatOptions.Enabled
                                            ? $"- {commandName} chat <type> enabled <on|off|toggle>"
                                            : $"- {commandName} chat <type> {chatOption.ToString().ToLower()} <r> <g> <b>");
        }
    }

    private void HandleConfCommand(string[] parameters)
    {
        if (parameters.Length < 2)
        {
            ChatProximity.ChatGui.Print($"Usage: {commandName} conf <option> <on|off|toggle>");
            return;
        }

        if (!Enum.TryParse(parameters[0], true, out ConfOptions option))
        {
            ChatProximity.ChatGui.Print($"Unknown configuration option: {parameters[0]}");
            return;
        }

        var newState = ParseToggle(parameters[1]);
        if (newState == null && !parameters[1].Equals("toggle", StringComparison.OrdinalIgnoreCase))
        {
            ChatProximity.ChatGui.Print($"Invalid action: {parameters[1]}");
            return;
        }

        switch (option)
        {
            case ConfOptions.Vertical:
                plugin.Configuration.VerticalIncrease = ToggleOption(plugin.Configuration.VerticalIncrease, newState);
                ChatProximity.ChatGui.Print($"Vertical range increase set to: {plugin.Configuration.VerticalIncrease}");
                break;

            case ConfOptions.Inside:
                plugin.Configuration.InsideReducer = ToggleOption(plugin.Configuration.InsideReducer, newState);
                ChatProximity.ChatGui.Print($"Inside reducer set to: {plugin.Configuration.InsideReducer}");
                break;

            case ConfOptions.Targeting:
                plugin.Configuration.RecolorTargeting = ToggleOption(plugin.Configuration.RecolorTargeting, newState);
                ChatProximity.ChatGui.Print($"Targeting recolor set to: {plugin.Configuration.RecolorTargeting}");
                break;

            case ConfOptions.Targeted:
                plugin.Configuration.RecolorTargeted = ToggleOption(plugin.Configuration.RecolorTargeted, newState);
                ChatProximity.ChatGui.Print($"Targeted recolor set to: {plugin.Configuration.RecolorTargeted}");
                break;

            case ConfOptions.Anonymize:
                plugin.Configuration.AnonymiseNames = ToggleOption(plugin.Configuration.AnonymiseNames, newState);
                ChatProximity.ChatGui.Print($"Anonymize names set to: {plugin.Configuration.AnonymiseNames}");
                break;

            case ConfOptions.Threshold:
                plugin.Configuration.EditThreshold = ToggleOption(plugin.Configuration.EditThreshold, newState);
                ChatProximity.ChatGui.Print($"Threshold editing set to: {plugin.Configuration.EditThreshold}");
                break;

            default:
                ChatProximity.ChatGui.Print($"Unhandled configuration option: {option}");
                break;
        }

        plugin.Configuration.Save();
    }

    private void HandleChatCommand(string[] parameters)
    {
        if (parameters.Length < 3)
        {
            ChatProximity.ChatGui.Print($"Usage: {commandName} chat <type> <option> <value>");
            return;
        }

        if (!Enum.TryParse(parameters[0], true, out XivChatType chatType))
        {
            ChatProximity.ChatGui.Print($"Invalid chat type: {parameters[0]}");
            return;
        }

        if (!plugin.Configuration.ChatTypeConfigs.TryGetValue(chatType, out var config))
        {
            ChatProximity.ChatGui.Print($"Chat type not configured or invalid: {chatType}");
            return;
        }

        if (!Enum.TryParse(parameters[1], true, out ChatOptions option))
        {
            ChatProximity.ChatGui.Print($"Unknown chat option: {parameters[1]}");
            return;
        }

        switch (option)
        {
            case ChatOptions.Enabled:
                config.Enabled = ToggleOption(config.Enabled, ParseToggle(parameters[2]));
                ChatProximity.ChatGui.Print($"Chat type {chatType} enabled set to: {config.Enabled}");
                break;

            case ChatOptions.Near:
            case ChatOptions.Far:
            case ChatOptions.Targeting:
            case ChatOptions.Targeted:
                if (parameters.Length < 5)
                {
                    ChatProximity.ChatGui.Print($"Usage: {commandName} chat {chatType} {option.ToString().ToLower()} <r> <g> <b>");
                    return;
                }

                if (!TryParseColor(parameters.Skip(2).ToArray(), out var color))
                {
                    ChatProximity.ChatGui.Print("Invalid color format. Expected: r g b");
                    return;
                }

                switch (option)
                {
                    case ChatOptions.Near:
                        config.NearColor = color;
                        break;
                    case ChatOptions.Far:
                        config.FarColor = color;
                        break;
                    case ChatOptions.Targeting:
                        config.TargetingColor = color;
                        break;
                    case ChatOptions.Targeted:
                        config.TargetedColor = color;
                        break;
                    case ChatOptions.Enabled:
                    case ChatOptions.Threshold:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(option), option, null);
                }

                ChatProximity.ChatGui.Print($"Chat type {chatType} {option.ToString().ToLower()} color set to: {parameters[2]} {parameters[3]} {parameters[4]}");
                break;

            case ChatOptions.Threshold:
                if (parameters.Length < 3 || !float.TryParse(parameters[2], out var newThreshold))
                {
                    ChatProximity.ChatGui.Print($"Usage: {commandName} chat {chatType} threshold <value>");
                    return;
                }

                config.Threshold = Math.Clamp(newThreshold, 0f, plugin.Configuration.ChatTypeConfigs[chatType].Range);
                ChatProximity.ChatGui.Print($"Chat type {chatType} threshold set to: {newThreshold}");
                break;

            default:
                ChatProximity.ChatGui.Print($"Unhandled chat option: {option}");
                break;
        }

        plugin.Configuration.Save();
    }

    private static bool ToggleOption(bool currentValue, bool? newValue)
    {
        return newValue ?? !currentValue;
    }

    private static bool? ParseToggle(string input)
    {
        return input.ToLower() switch
        {
            "on" => true,
            "true" => true,
            "off" => false,
            "false" => false,
            "toggle" => null,
            _ => null
        };
    }

    private static bool TryParseColor(string[] values, out Vector4 color)
    {
        color = default;

        if (values.Length != 3 ||
            !byte.TryParse(values[0], out var r) ||
            !byte.TryParse(values[1], out var g) ||
            !byte.TryParse(values[2], out var b))
        {
            return false;
        }

        color = new Vector4(r / 255f, g / 255f, b / 255f, 1f);
        return true;
    }
}
