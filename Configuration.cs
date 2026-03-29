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

        if (GetActiveProfile() == null)
        {
            Profiles.Add(new ProfileData { Name = ActiveProfileName });
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
            return profile;

        profile = new ProfileData { Name = string.IsNullOrWhiteSpace(ActiveProfileName) ? "Default" : ActiveProfileName };
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
    public string StartingBankInput { get; set; } = string.Empty;
    public string FinalBankInput { get; set; } = string.Empty;
    public string TipsInput { get; set; } = string.Empty;
    public string GoalInput { get; set; } = string.Empty;

    public List<HistoryEntry> History { get; set; } = new();
}

[Serializable]
public class HistoryEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string StartingBank { get; set; } = string.Empty;
    public string FinalBank { get; set; } = string.Empty;
    public string Tips { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}