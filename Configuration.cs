using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace GambaBank;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public int SomePropertyToBeSavedAndWithADefault { get; set; } = 100;
    public bool IsConfigWindowMovable { get; set; } = true;

    public string StartingBankInput { get; set; } = string.Empty;
    public string FinalBankInput { get; set; } = string.Empty;
    public string TipsInput { get; set; } = string.Empty;

    public string ResultsLabel { get; set; } = "Today Profit/Loss:";
    public string StartingLabel { get; set; } = "Starting Bank:";
    public string FinalLabel { get; set; } = "Final Bank:";

    public string SelectedTheme { get; set; } = "Default";

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }
}