using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public sealed class DebugWindow : Window, IDisposable
{
    private readonly Func<string> snapshotProvider;
    private bool autoScroll = true;
    private bool scrollToBottom;
    private string filterText = string.Empty;
    private int selectedLogIndex = -1;

    public DebugWindow(Func<string> snapshotProvider)
        : base("GambaBank Debug###GambaBankDebug")
    {
        this.snapshotProvider = snapshotProvider;
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(1100f, 720f);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        DebugHub.Add("UI", "Debug window opened.");
        scrollToBottom = true;
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Live plugin diagnostics");
        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref autoScroll);

        ImGui.SameLine();
        if (ImGui.Button("Clear Logs"))
        {
            DebugHub.Clear();
            DebugHub.Add("UI", "Debug log buffer cleared from debug window.");
            selectedLogIndex = -1;
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy Logs"))
        {
            ImGui.SetClipboardText(DebugHub.SnapshotText());
            DebugHub.Add("UI", "Debug log buffer copied to clipboard.");
        }

        ImGui.SameLine();
        if (ImGui.Button("Jump to Bottom"))
            scrollToBottom = true;

        ImGui.SetNextItemWidth(260f);
        ImGui.InputTextWithHint("##DebugFilter", "Filtro opcional (ex.: AUTO, CHAT, TRACK)", ref filterText, 128);

        ImGui.Separator();
        ImGui.TextUnformatted("Runtime snapshot");

        if (ImGui.BeginChild("##GambaDebugSnapshot", new Vector2(0f, 150f), true, ImGuiWindowFlags.HorizontalScrollbar))
            ImGui.TextUnformatted(snapshotProvider.Invoke());
        ImGui.EndChild();

        ImGui.Separator();
        ImGui.TextUnformatted("Live logs");

        if (ImGui.BeginChild("##GambaDebugLogs", new Vector2(0f, 0f), true, ImGuiWindowFlags.HorizontalScrollbar))
        {
            var entries = DebugHub.Snapshot();
            bool hasFilter = !string.IsNullOrWhiteSpace(filterText);
            int visibleIndex = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                if (hasFilter && entry.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                visibleIndex++;
                DrawSelectableLogLine(entry, visibleIndex);
            }

            if (ImGui.IsWindowFocused() && selectedLogIndex >= 0 && ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C))
            {
                int copyVisibleIndex = -1;
                for (int i = 0; i < entries.Count; i++)
                {
                    string entry = entries[i];
                    if (hasFilter && entry.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    copyVisibleIndex++;
                    if (copyVisibleIndex == selectedLogIndex)
                    {
                        ImGui.SetClipboardText(entry);
                        break;
                    }
                }
            }

            if (scrollToBottom || (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f))
                ImGui.SetScrollHereY(1f);

            scrollToBottom = false;
        }
        ImGui.EndChild();
    }

    private void DrawSelectableLogLine(string entry, int visibleIndex)
    {
        Vector2 start = ImGui.GetCursorScreenPos();
        float width = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);
        float height = ImGui.GetTextLineHeightWithSpacing();

        ImGui.InvisibleButton($"##DebugLogLine{visibleIndex}", new Vector2(width, height));
        bool hovered = ImGui.IsItemHovered();
        bool clicked = ImGui.IsItemClicked();
        if (clicked)
            selectedLogIndex = visibleIndex;

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();

        if (selectedLogIndex == visibleIndex)
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.22f, 0.32f, 0.55f, 0.45f)), 2f);
        else if (hovered)
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.06f)), 2f);

        Vector2 textPos = new(min.X, min.Y + MathF.Max(0f, (height - ImGui.GetTextLineHeight()) * 0.5f));
        DrawLogLine(entry, textPos);
    }

    private static void DrawLogLine(string entry, Vector2 textPos)
    {
        var drawList = ImGui.GetWindowDrawList();
        if (TrySplitLogLine(entry, out string prefix, out string category, out string message))
        {
            uint timeColor = ImGui.ColorConvertFloat4ToU32(Hex("#fa8034"));
            uint white = ImGui.ColorConvertFloat4ToU32(Hex("#ffffff"));
            uint categoryColor = ImGui.ColorConvertFloat4ToU32(GetCategoryColor(category));

            drawList.AddText(textPos, timeColor, prefix);
            Vector2 size1 = ImGui.CalcTextSize(prefix);
            Vector2 pos2 = new(textPos.X + size1.X, textPos.Y);
            drawList.AddText(pos2, categoryColor, category);
            Vector2 size2 = ImGui.CalcTextSize(category);
            Vector2 pos3 = new(pos2.X + size2.X, textPos.Y);
            drawList.AddText(pos3, white, message);
            return;
        }

        drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(Hex("#ffffff")), entry);
    }

    private static bool TrySplitLogLine(string entry, out string prefix, out string category, out string message)
    {
        prefix = string.Empty;
        category = string.Empty;
        message = string.Empty;

        int firstClose = entry.IndexOf(']');
        if (firstClose < 0 || firstClose + 2 >= entry.Length)
            return false;

        int secondOpen = entry.IndexOf('[', firstClose + 1);
        int secondClose = secondOpen >= 0 ? entry.IndexOf(']', secondOpen + 1) : -1;
        if (secondOpen < 0 || secondClose < 0)
            return false;

        prefix = entry.Substring(0, secondOpen);
        category = entry.Substring(secondOpen, secondClose - secondOpen + 1);
        message = secondClose + 1 < entry.Length ? entry.Substring(secondClose + 1) : string.Empty;
        return true;
    }

    private static Vector4 GetCategoryColor(string category)
    {
        return category switch
        {
            "[TRACK]" => new Vector4(0.35f, 0.85f, 0.35f, 1f),
            "[MODE]" => Hex("#0091ff"),
            "[HISTORY]" => new Vector4(0.95f, 0.80f, 0.25f, 1f),
            "[UI]" => Hex("#a600ff"),
            "[PROFILE]" => Hex("#f6ff00"),
            "[BET]" => Hex("#9f29ff"),
            "[AUTO-BJ]" => Hex("#ff59d3"),
            "[AUTO-PARSE]" => Hex("#59ffbd"),
            "[CHAT]" => Hex("#75bfff"),
            "[AUTO]" => Hex("#ff8a75"),
            "[AUTO-DD]" => Hex("#9f75ff"),
            "[COMMAND]" => Hex("#ffad7a"),
            "[PLUGIN]" => Hex("#c28aff"),
            "[INIT]" => Hex("#e2ff8a"),
            "[WARN]" => Hex("#ffa229"),
            "[ERR]" => Hex("#ff2929"),
            _ => Hex("#ffffff")
        };
    }

    private static Vector4 Hex(string hex)
    {
        hex = hex.TrimStart('#');
        float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Vector4(r, g, b, 1f);
    }
}
