using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class ConfigWindow : Window, IDisposable
{
    private string resultsLabelBuffer = string.Empty;
    private string startingLabelBuffer = string.Empty;
    private string finalLabelBuffer = string.Empty;

    public ConfigWindow()
        : base("GambaBank Config###GambaBankConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;

        SyncBuffers();
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        Flags = Plugin.Configuration.IsConfigWindowMovable
            ? ImGuiWindowFlags.NoCollapse
            : ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove;

        SyncBuffers();
    }

    public override void Draw()
    {
        var movable = Plugin.Configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable", ref movable))
        {
            Plugin.Configuration.IsConfigWindowMovable = movable;
            Plugin.Configuration.Save();
        }

        var autoClear = Plugin.Configuration.AutoClearAfterCopy;
        if (ImGui.Checkbox("Auto Clear After Copy", ref autoClear))
        {
            Plugin.Configuration.AutoClearAfterCopy = autoClear;
            Plugin.Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Edit message", ImGuiTreeNodeFlags.DefaultOpen))
        {
            const float labelWidth = 88f;
            const float fieldWidth = 220f;

            if (ImGui.BeginTable("##ConfigEditMessageTable", 4, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("L1", ImGuiTableColumnFlags.WidthFixed, labelWidth);
                ImGui.TableSetupColumn("F1", ImGuiTableColumnFlags.WidthFixed, fieldWidth);
                ImGui.TableSetupColumn("L2", ImGuiTableColumnFlags.WidthFixed, labelWidth);
                ImGui.TableSetupColumn("F2", ImGuiTableColumnFlags.WidthFixed, fieldWidth);

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Result:");

                ImGui.TableSetColumnIndex(1);
                if (ImGui.InputText("##CfgResultsLabel", ref resultsLabelBuffer, 256))
                {
                    Plugin.Configuration.ResultsLabel = resultsLabelBuffer;
                    Plugin.Configuration.Save();
                }

                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Starting:");

                ImGui.TableSetColumnIndex(3);
                if (ImGui.InputText("##CfgStartingLabel", ref startingLabelBuffer, 256))
                {
                    Plugin.Configuration.StartingLabel = startingLabelBuffer;
                    Plugin.Configuration.Save();
                }

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Final:");

                ImGui.TableSetColumnIndex(1);
                if (ImGui.InputText("##CfgFinalLabel", ref finalLabelBuffer, 256))
                {
                    Plugin.Configuration.FinalLabel = finalLabelBuffer;
                    Plugin.Configuration.Save();
                }

                ImGui.EndTable();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted("Saved Labels Preview:");
        ImGui.BulletText($"Result Label: {Plugin.Configuration.ResultsLabel}");
        ImGui.BulletText($"Starting Label: {Plugin.Configuration.StartingLabel}");
        ImGui.BulletText($"Final Label: {Plugin.Configuration.FinalLabel}");
    }

    private void SyncBuffers()
    {
        if (string.IsNullOrEmpty(resultsLabelBuffer))
            resultsLabelBuffer = Plugin.Configuration.ResultsLabel ?? string.Empty;

        if (string.IsNullOrEmpty(startingLabelBuffer))
            startingLabelBuffer = Plugin.Configuration.StartingLabel ?? string.Empty;

        if (string.IsNullOrEmpty(finalLabelBuffer))
            finalLabelBuffer = Plugin.Configuration.FinalLabel ?? string.Empty;
    }
}