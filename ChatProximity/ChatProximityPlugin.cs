using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ChatProximity.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ChatProximity.Handlers;

namespace ChatProximity;

public sealed class ChatProximityPlugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    private const string CommandName = "/chatprox";

    public readonly WindowSystem WindowSystem = new("ChatProximity");

    public Configuration Configuration { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private ChatHandler ChatHandler { get; init; }

    public ChatProximityPlugin()
    {
        PluginLog.Info("Starting Chat Proximity!");
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Windows
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // Handlers
        ChatHandler = new ChatHandler(this);

        // Events
        ChatGui.ChatMessage += HandleMessage;

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Commands
        CommandManager.AddHandler(CommandName, new CommandInfo(OnChatProxCommand)
        {
            HelpMessage = "Open settings"
        });
    }

    public void Dispose()
    {
        PluginLog.Info("Disposing Chat Proximity");

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ChatGui.ChatMessage -= HandleMessage;
    }

    private void OnChatProxCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void HandleMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        ChatHandler.OnMessage(type, ref sender, ref message, ref isHandled);
    }
    
    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
