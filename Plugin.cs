using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GambaBank.Windows;

namespace GambaBank;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;

    private const string CommandName = "/bank";
    private const string AliasCommandName = "/gamba";
    private const string SettingsCommandName = "/banksettings";

    public static Configuration Configuration { get; private set; } = null!;
    private static Plugin? Instance { get; set; }

    public readonly WindowSystem WindowSystem = new("GambaBank");
    private MainWindow MainWindow { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private HelpWindow HelpWindow { get; init; }

    public Plugin()
    {
        Instance = this;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        MainWindow = new MainWindow();
        ConfigWindow = new ConfigWindow();
        HelpWindow = new HelpWindow();

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(HelpWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the GambaBank window. Use /bank help to open Help."
        });

        CommandManager.AddHandler(AliasCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the GambaBank window. Use /gamba help to open Help."
        });

        CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand)
        {
            HelpMessage = "Open the GambaBank config window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        Log.Information($"===Loaded {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        HelpWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(AliasCommandName);
        CommandManager.RemoveHandler(SettingsCommandName);

        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    private void OnCommand(string command, string args)
    {
        if (string.Equals(args.Trim(), "help", StringComparison.OrdinalIgnoreCase))
        {
            ShowHelpWindow();
            return;
        }

        MainWindow.IsOpen = true;
    }

    private void OnSettingsCommand(string command, string args)
    {
        ConfigWindow.IsOpen = true;
    }

    private void ShowHelpWindow()
    {
        HelpWindow.IsOpen = true;
    }

    public static void OpenHelpUi()
    {
        Instance?.ShowHelpWindow();
    }

    public static void OpenConfigUi()
    {
        if (Instance != null)
            Instance.ConfigWindow.IsOpen = true;
    }

    public void ToggleMainUi() => MainWindow.Toggle();
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
