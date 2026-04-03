using System;
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
    private DebugWindow DebugWindow { get; init; }

    public Plugin()
    {
        Instance = this;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        MainWindow = new MainWindow();
        ConfigWindow = new ConfigWindow();
        HelpWindow = new HelpWindow();
        DebugWindow = new DebugWindow(() => MainWindow.BuildDebugSnapshot());

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(HelpWindow);
        WindowSystem.AddWindow(DebugWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the GambaBank window. Use /bank help for Help or /bank debug for Debug."
        });

        CommandManager.AddHandler(AliasCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the GambaBank window. Use /gamba help for Help or /gamba debug for Debug."
        });

        CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand)
        {
            HelpMessage = "Open the GambaBank config window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        DebugHub.Add("PLUGIN", $"=== Loaded {PluginInterface.Manifest.Name} ===");
    }

    public void Dispose()
    {
        DebugHub.Add("PLUGIN", "Disposing plugin and unregistering handlers.");

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        HelpWindow.Dispose();
        DebugWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(AliasCommandName);
        CommandManager.RemoveHandler(SettingsCommandName);

        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    private void OnCommand(string command, string args)
    {
        string normalizedArgs = (args ?? string.Empty).Trim();
        DebugHub.Add("COMMAND", $"Command received: {command} {normalizedArgs}".Trim());

        if (string.Equals(normalizedArgs, "help", StringComparison.OrdinalIgnoreCase))
        {
            ShowHelpWindow();
            return;
        }

        if (string.Equals(normalizedArgs, "debug", StringComparison.OrdinalIgnoreCase))
        {
            ShowDebugWindow();
            return;
        }

        MainWindow.IsOpen = true;
    }

    private void OnSettingsCommand(string command, string args)
    {
        DebugHub.Add("COMMAND", $"Settings command received: {command} {(args ?? string.Empty).Trim()}".Trim());
        ConfigWindow.IsOpen = true;
    }

    private void ShowHelpWindow()
    {
        DebugHub.Add("UI", "Opening help window.");
        HelpWindow.IsOpen = true;
    }

    private void ShowDebugWindow()
    {
        DebugHub.Add("UI", "Opening debug window.");
        DebugWindow.IsOpen = true;
    }

    public static void OpenHelpUi()
    {
        Instance?.ShowHelpWindow();
    }

    public static void OpenConfigUi()
    {
        if (Instance != null)
        {
            DebugHub.Add("UI", "Opening config window.");
            Instance.ConfigWindow.IsOpen = true;
        }
    }

    public static void OpenDebugUi()
    {
        Instance?.ShowDebugWindow();
    }

    public void ToggleMainUi() => MainWindow.Toggle();
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
