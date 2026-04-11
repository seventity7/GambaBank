using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
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
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

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
            HelpMessage = "Open the main window.\n/bank help - Help window.\n/bank debug - Debug window.\n/bank history - Dealer Log History.\n/bank logs on|off - Toggle dealer logs.\n/bank startshift <startingbank> [house] - Start shift.\n/bank endshift <finalbank> [house] - End shift."
        });

        CommandManager.AddHandler(AliasCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /bank."
        });

        CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand)
        {
            HelpMessage = "Open the settings window."
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

        if (string.Equals(normalizedArgs, "history", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow.OpenDealerTurnLogHistoryWindow();
            return;
        }

        if (string.Equals(normalizedArgs, "logs on", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow.SetDealerTurnLogsEnabledFromCommand(true);
            ChatGui.Print("Dealer log history [ON]");
            return;
        }

        if (string.Equals(normalizedArgs, "logs off", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow.SetDealerTurnLogsEnabledFromCommand(false);
            ChatGui.Print("Dealer log history [OFF]");
            return;
        }

        if (TryHandleShiftCommand(normalizedArgs, isStartShift: true))
            return;

        if (TryHandleShiftCommand(normalizedArgs, isStartShift: false))
            return;

        MainWindow.IsOpen = true;
    }

    private bool TryHandleShiftCommand(string args, bool isStartShift)
    {
        string prefix = isStartShift ? "startshift" : "endshift";
        if (!args.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string remainder = args.Length > prefix.Length ? args[prefix.Length..].Trim() : string.Empty;
        string usageMessage = isStartShift
            ? "Invalid usage, please type \"/bank startshift <startingbank> <house>\", House is optional."
            : "Invalid usage, please type \"/bank endshift <finalbank> <house>\", House is optional.";

        if (string.IsNullOrWhiteSpace(remainder))
        {
            ChatGui.PrintError(usageMessage);
            return true;
        }

        string[] parts = remainder.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            ChatGui.PrintError(usageMessage);
            return true;
        }

        string amountToken = parts[0];
        string house = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        string fieldName = isStartShift ? "<startingbank>" : "<finalbank>";

        if (!TryParseFlexibleBankValue(amountToken, out BigInteger amount))
        {
            ChatGui.PrintError($"Invalid format for {fieldName}, examples: 5000000, 5M, 5.000.000");
            return true;
        }

        if (isStartShift)
        {
            MainWindow.StartDealerShiftFromCommand(amount, house);
            string message = string.IsNullOrWhiteSpace(house)
                ? $"Started Dealer Shift with {FormatBankValue(amount)} Starting Bank."
                : $"Started Dealer Shift with {FormatBankValue(amount)} Starting Bank in {house}.";
            ChatGui.Print(message);
        }
        else
        {
            MainWindow.EndDealerShiftFromCommand(amount, house);
            string message = string.IsNullOrWhiteSpace(house)
                ? $"Finished Dealer Shift with {FormatBankValue(amount)}."
                : $"Finished Dealer Shift with {FormatBankValue(amount)} on {house}.";
            ChatGui.Print(message);
        }

        return true;
    }

    private static bool TryParseFlexibleBankValue(string input, out BigInteger value)
    {
        value = BigInteger.Zero;
        string raw = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string compact = raw.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (compact.EndsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            string millionsPart = compact[..^1].Trim();
            if (string.IsNullOrWhiteSpace(millionsPart))
                return false;

            int separatorCount = millionsPart.Count(c => c == '.' || c == ',');
            if (separatorCount > 1)
                return false;

            string normalizedMillions = millionsPart.Replace(',', '.');
            if (!decimal.TryParse(normalizedMillions, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal millions) || millions <= 0m)
                return false;

            decimal expanded = millions * 1_000_000m;
            if (expanded <= 0m)
                return false;

            value = new BigInteger(expanded);
            return value > BigInteger.Zero;
        }

        string digitsOnly = string.Concat(compact.Where(char.IsDigit));
        if (string.IsNullOrWhiteSpace(digitsOnly))
            return false;

        return BigInteger.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > BigInteger.Zero;
    }

    private static string FormatBankValue(BigInteger value)
    {
        string digits = BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture);
        if (digits.Length <= 3)
            return digits;

        var chunks = new System.Collections.Generic.List<string>();
        for (int i = digits.Length; i > 0; i -= 3)
        {
            int start = Math.Max(0, i - 3);
            chunks.Insert(0, digits[start..i]);
        }

        return string.Join('.', chunks);
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
