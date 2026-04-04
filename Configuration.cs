using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace GambaBank;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public int SomePropertyToBeSavedAndWithADefault { get; set; } = 100;
    public bool IsConfigWindowMovable { get; set; } = true;

    public string ResultsLabel { get; set; } = "Today Profit/Loss:";
    public string StartingLabel { get; set; } = "Starting Bank:";
    public string FinalLabel { get; set; } = "Final Bank:";

    public bool AutoClearAfterCopy { get; set; } = false;
    public bool IncludeTimestampInMessage { get; set; } = true;
    public string SelectedTheme { get; set; } = "Default";
    public string ActiveProfileName { get; set; } = "Default";
    public string ActiveMode { get; set; } = "Dealer";
    public bool DealerRequireSaveConfirmation { get; set; } = true;
    public bool DealerAutoBackupOnEndShift { get; set; } = false;
    public bool DealerAutoDailyBackup { get; set; } = false;
    public string DealerBackupDirectory { get; set; } = string.Empty;
    public string DealerLastDailyBackupDate { get; set; } = string.Empty;
    public string HistoryExportDirectory { get; set; } = string.Empty;

    public List<ProfileData> Profiles { get; set; } = new();

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;

        if (Profiles.Count == 0)
        {
            Profiles.Add(new ProfileData { Name = "Default" });
        }

        if (string.IsNullOrWhiteSpace(ActiveProfileName))
        {
            ActiveProfileName = "Default";
        }

        if (!string.Equals(ActiveMode, "Dealer", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ActiveMode, "Player", StringComparison.OrdinalIgnoreCase))
        {
            ActiveMode = "Dealer";
        }

        foreach (var profile in Profiles)
        {
            profile.EnsureInitialized();
        }

        if (GetActiveProfile() == null)
        {
            var profile = new ProfileData { Name = ActiveProfileName };
            profile.EnsureInitialized();
            Profiles.Add(profile);
        }
    }

    public ProfileData? GetActiveProfile()
    {
        foreach (var profile in Profiles)
        {
            if (string.Equals(profile.Name, ActiveProfileName, StringComparison.OrdinalIgnoreCase))
                return profile;
        }

        return null;
    }

    public ProfileData GetOrCreateActiveProfile()
    {
        var profile = GetActiveProfile();
        if (profile != null)
        {
            profile.EnsureInitialized();
            return profile;
        }

        profile = new ProfileData { Name = string.IsNullOrWhiteSpace(ActiveProfileName) ? "Default" : ActiveProfileName };
        profile.EnsureInitialized();
        Profiles.Add(profile);
        return profile;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}

[Serializable]
public class ProfileData
{
    public string Name { get; set; } = "Default";

    // Dealer mode inputs
    public string StartingBankInput { get; set; } = string.Empty;
    public string FinalBankInput { get; set; } = string.Empty;
    public string TipsInput { get; set; } = string.Empty;
    public string HouseInput { get; set; } = string.Empty;

    // Legacy / compatibility
    public string GoalInput { get; set; } = string.Empty;
    public List<HistoryEntry> History { get; set; } = new();

    // Player mode inputs
    public string PlayerStartingBankInput { get; set; } = string.Empty;
    public string PlayerCurrentBankInput { get; set; } = string.Empty;
    public string PlayerBetInput { get; set; } = string.Empty;
    public string PlayerHouseInput { get; set; } = string.Empty;
    public string PlayerTrackedDealerInput { get; set; } = string.Empty;
    public bool PlayerAutoTrackEnabled { get; set; } = false;

    // Mode-specific history
    public List<HistoryEntry> DealerHistory { get; set; } = new();
    public List<HistoryEntry> PlayerHistory { get; set; } = new();

    // Dealer turn logs
    public bool DealerTurnLogsEnabled { get; set; } = false;
    public List<DealerTurnLogEntry> DealerTurnLogEntries { get; set; } = new();

    public void EnsureInitialized()
    {
        if (DealerHistory.Count == 0 && History.Count > 0)
        {
            DealerHistory.AddRange(History);
        }

        if (string.IsNullOrWhiteSpace(PlayerCurrentBankInput) && !string.IsNullOrWhiteSpace(PlayerStartingBankInput))
        {
            PlayerCurrentBankInput = PlayerStartingBankInput;
        }

        DealerTurnLogEntries ??= new();
        foreach (var entry in DealerTurnLogEntries)
            entry.EnsureInitialized();
    }
}

[Serializable]
public class HistoryEntry
{
    public string House { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string StartingBank { get; set; } = string.Empty;
    public string FinalBank { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}


[Serializable]
public class DealerTurnLogEntry
{
    public string House { get; set; } = string.Empty;
    public bool HouseEditedManually { get; set; } = false;
    public string PlayerName { get; set; } = string.Empty;
    public string JoinedTimestamp { get; set; } = string.Empty;
    public string LeftTimestamp { get; set; } = string.Empty;
    public string TotalReceivedGil { get; set; } = "0";
    public string TotalSentGil { get; set; } = "0";
    public string ChatExportFilePath { get; set; } = string.Empty;
    public string TradeExportFilePath { get; set; } = string.Empty;
    public List<string> ChatLines { get; set; } = new();
    public List<string> TradeLines { get; set; } = new();

    public void EnsureInitialized()
    {
        House ??= string.Empty;
        ChatExportFilePath ??= string.Empty;
        TradeExportFilePath ??= string.Empty;
        ChatLines ??= new();
        TradeLines ??= new();
        TotalReceivedGil ??= "0";
        TotalSentGil ??= "0";
    }
}
