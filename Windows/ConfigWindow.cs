using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow()
        : base("GambaBank Config###GambaBankConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        Flags = Plugin.Configuration.IsConfigWindowMovable ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoMove;
    }

    public override void Draw()
    {
        var exampleValue = Plugin.Configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.DragInt("Example Value", ref exampleValue))
        {
            Plugin.Configuration.SomePropertyToBeSavedAndWithADefault = exampleValue;
            Plugin.Configuration.Save();
        }

        var movable = Plugin.Configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable", ref movable))
        {
            Plugin.Configuration.IsConfigWindowMovable = movable;
            Plugin.Configuration.Save();
        }
    }
}