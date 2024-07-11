using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Diagnostics;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("ChatProximity");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        PluginLog.Info("Starting Chat Proximity!");
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        ChatGui.ChatMessage += HandleMessage;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        PluginLog.Info("Disposing Chat Proximity");

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ChatGui.ChatMessage -= HandleMessage;
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private unsafe void HandleMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
       if (type == XivChatType.Say)
        {
            PluginLog.Debug($"Caught {type} message from {sender.TextValue}: {message}");
            try
            {
                if (isHandled)
                {
                    return;
                }

                List<Payload> finalPayload = [];

                var currentPlayer = (BattleChara*)(ClientState.LocalPlayer?.Address ?? 0);

                if (sender != null && currentPlayer != null)
                {
                    var senderCharacter = CharacterManager.Instance()->LookupBattleCharaByName(sender.ToString(), true);

                    if (senderCharacter != null) {
                        if (senderCharacter != currentPlayer)
                        {
                            PluginLog.Debug($"Found character: {senderCharacter->Name.ToString()}");
                            var distance = Vector3.Distance(currentPlayer->Position, senderCharacter->Position);

                            var colors = new List<UIForegroundPayload> { new(1), new(2), new(3), new(4), new(5), new(6) };
                            var colorIndex = (int)distance * colors.Count / 20; // 20 is the max range of Say chat
                            finalPayload.Add(colors[colorIndex]);

                            PluginLog.Debug($"Computed distance: {distance}, index {colorIndex}");
                        }
                        else
                        {
                            PluginLog.Debug("Self message, no color change");
                        }
                    } else {
                        PluginLog.Warning($"No sender character found: {sender}");
                    }
                }

                finalPayload.Add(new TextPayload(message.TextValue));
                message = new SeString(finalPayload);
                PluginLog.Debug($"New message is: {message}");
            }
            catch (Exception e)
            {
                PluginLog.Error("Exception while processing message", e);
            }
            PluginLog.Debug("Exiting... =============");
        }
    }
    
    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
