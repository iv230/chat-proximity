using ChatProximity.Commands;
using ChatProximity.Config;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ChatProximity.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ChatProximity.Handlers;
using ChatProximity.Service;

namespace ChatProximity;

public sealed class ChatProximity : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    private const string CommandName = "/chatprox";

    public readonly WindowSystem WindowSystem = new("ChatProximity");

    public Configuration Configuration { get; init; }
    public ChatMessageService ChatMessageService { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private ChatHandler ChatHandler { get; init; }
    private ChatProxCommand ChatProxCommand { get; init; }

    public ChatProximity()
    {
        Log.Info($"===Starting {PluginInterface.Manifest.Name}===");
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.ValidateAndMigrate();

        // Windows
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // Handlers
        ChatHandler = new ChatHandler(this);
        
        // Commands
        ChatProxCommand = new ChatProxCommand(this, CommandName);
        
        // Services
        ChatMessageService = new ChatMessageService();

        // Events
        ChatGui.ChatMessage += HandleMessage;

        // Dalamud plugin interface buttons
        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Commands
        CommandManager.AddHandler(CommandName, new CommandInfo(OnChatProxCommand)
        {
            HelpMessage = "(Without arguments) Open settings (With arguments) help for more information."
        });
    }

    public void Dispose()
    {
        Log.Info("Disposing Chat Proximity");

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ChatGui.ChatMessage -= HandleMessage;
    }

    private void OnChatProxCommand(string command, string args)
    {
        if (args.Length == 0)
        {
            ToggleConfigUi();   
        }
        else
        {
            ChatProxCommand.OnCommand(args);
        }
    }

    private void HandleMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        ChatHandler.OnMessage(type, ref sender, ref message, ref isHandled);
    }
    
    private void DrawUi() => WindowSystem.Draw();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
