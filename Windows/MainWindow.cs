using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class MainWindow : Window, IDisposable
{
    private string startingBankInput = string.Empty;
    private string finalBankInput = string.Empty;
    private string tipsInput = string.Empty;
    private string newProfileName = string.Empty;

    private BigInteger startingBankValue;
    private BigInteger finalBankValue;
    private BigInteger tipsValue;

    private string resultsLabel = string.Empty;
    private string startingLabel = string.Empty;
    private string finalLabel = string.Empty;

    private string sortBy = "Most recent";
    private DateTime copiedUntilUtc = DateTime.MinValue;
    private bool formulaHelpPinned;

    private static readonly Vector2 DefaultWindowSize = new(940f, 520f);
    private static readonly Vector2 MinimumWindowSize = new(900f, 500f);

    private static readonly Vector4 ProfitColor = new(0.35f, 0.85f, 0.35f, 1.0f);
    private static readonly Vector4 LossColor = new(1.0f, 0.45f, 0.45f, 1.0f);
    private static readonly Vector4 NeutralColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Vector4 GoldColor = new(0.95f, 0.80f, 0.25f, 1.0f);

    private static readonly Vector4 CopyButtonColor = Hex("#005210");
    private static readonly Vector4 UtilityButtonColor = Hex("#005232");
    private static readonly Vector4 DangerButtonColor = Hex("#520000");
    private static readonly Vector4 WhiteText = new(1.0f, 1.0f, 1.0f, 1.0f);

    private static readonly Vector4 HelpButtonColor = Hex("#2C2C2C");
    private static readonly Vector4 HelpButtonHoverColor = Hex("#3A3A3A");
    private static readonly Vector4 HelpButtonActiveColor = Hex("#4A4A4A");
    private static readonly Vector4 HelpButtonTextColor = Hex("#FF8A8A");

    public MainWindow()
        : base("Gamba Bank", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        LoadFromConfiguration();

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinimumWindowSize,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Size = DefaultWindowSize;
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {

        DrawProfilesSection();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawBankFields();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawMessageSection();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawHistorySection();
    }

    private Vector2 pinnedTooltipPosition = Vector2.Zero;
    private bool hasPinnedTooltipPosition = false;
    private void DrawHelpButton()
    {
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 28f);

        bool pressed = DrawStyledBoldButton(
            "?",
            "FormulaHelpButton",
            new Vector2(22f, 20f),
            HelpButtonColor,
            HelpButtonTextColor,
            HelpButtonHoverColor,
            HelpButtonActiveColor);

        bool hovered = ImGui.IsItemHovered();

        if (pressed)
        {
            formulaHelpPinned = !formulaHelpPinned;

            if (formulaHelpPinned)
            {
                pinnedTooltipPosition = ImGui.GetMousePos() + new Vector2(12f, 12f);
                hasPinnedTooltipPosition = true;
            }
            else
            {
                hasPinnedTooltipPosition = false;
            }
        }

        if (hovered && !formulaHelpPinned)
        {
            ImGui.BeginTooltip();

            DrawBoldTooltipLine("Math formula:");
            ImGui.TextUnformatted("{FinalBank} - {StartingBank} - {Tips} = {Results}");
            ImGui.Spacing();
            ImGui.TextUnformatted("{FinalBank} minus {StartingBank} minus {Tips} equals a {Results}");
            ImGui.Spacing();
            DrawBoldTooltipLine("Final Bank: 5.000.000 - Starting Bank: 2.000.000 - Tips: 1.000.000 = Results: +3.000.000");

            ImGui.EndTooltip();
        }

        if (formulaHelpPinned && hasPinnedTooltipPosition)
        {
            ImGui.SetNextWindowPos(pinnedTooltipPosition, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.98f);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);

            ImGui.Begin(
                "##FormulaHelpPinnedTooltipWindow",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoSavedSettings);

            DrawBoldTooltipLine("Math formula:");
            ImGui.TextUnformatted("{FinalBank} - {StartingBank} - {Tips} = {Results}");
            ImGui.Spacing();
            ImGui.TextUnformatted("{FinalBank} minus {StartingBank} minus {Tips} equals a {Results}");
            ImGui.Spacing();
            DrawBoldTooltipLine("Final Bank: 5.000.000 - Starting Bank: 2.000.000 - Tips: 1.000.000 = Results: +3.000.000");

            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }

    private void DrawBoldTooltipLine(string text)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        uint col = ImGui.GetColorU32(ImGuiCol.Text);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddText(pos, col, text);
        drawList.AddText(pos + new Vector2(1f, 0f), col, text);

        Vector2 size = ImGui.CalcTextSize(text);
        ImGui.Dummy(new Vector2(size.X + 1f, size.Y));
    }

    private void DrawProfilesSection()
    {
        var activeProfile = Plugin.Configuration.GetOrCreateActiveProfile();

        ImGui.Text("Profile:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(180f);
        if (ImGui.BeginCombo("##ProfileCombo", activeProfile.Name))
        {
            foreach (var profile in Plugin.Configuration.Profiles)
            {
                bool selected = string.Equals(profile.Name, activeProfile.Name, StringComparison.OrdinalIgnoreCase);
                if (ImGui.Selectable(profile.Name, selected))
                {
                    SaveCurrentProfileValues();
                    Plugin.Configuration.ActiveProfileName = profile.Name;
                    Plugin.Configuration.Save();
                    LoadFromConfiguration();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(145f);
        ImGui.InputText("##NewProfileName", ref newProfileName, 128);

        ImGui.SameLine();
        if (DrawStyledBoldButton("Add Profile", "AddProfileButton", new Vector2(110f, 0f), UtilityButtonColor))
            CreateProfile();

        ImGui.SameLine();
        if (DrawStyledBoldButton("Delete Current", "DeleteCurrentButton", new Vector2(120f, 0f), DangerButtonColor) && Plugin.Configuration.Profiles.Count > 1)
            DeleteCurrentProfile();

        DrawHelpButton();

    }

    private void DrawBankFields()
    {
        const float labelWidth = 86f;
        const float valueWidth = 148f;
        const float buttonWidth = 115f;

        if (!ImGui.BeginTable("##BankFieldsTable", 5, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed, 140f);

        ImGui.TableNextRow();

        DrawEditableCell(0, 1, "Starting Bank:", "##StartingBank", ref startingBankInput, ref startingBankValue, valueWidth);
        DrawEditableCell(2, 3, "Final Bank:", "##FinalBank", ref finalBankInput, ref finalBankValue, valueWidth);

        ImGui.TableSetColumnIndex(4);
        if (DrawStyledBoldButton("Save to history", "SaveToHistoryButton", new Vector2(buttonWidth, 0f), CopyButtonColor))
            AddHistoryEntry();

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Results:");

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(valueWidth);
        var resultText = GetCurrentResultText();
        ImGui.BeginDisabled();
        ImGui.InputText("##Results", ref resultText, 128, ImGuiInputTextFlags.ReadOnly);
        ImGui.EndDisabled();

        DrawEditableCell(2, 3, "Tips:", "##Tips", ref tipsInput, ref tipsValue, valueWidth);

        ImGui.EndTable();
    }

    private void DrawEditableCell(
        int labelColumn,
        int valueColumn,
        string label,
        string inputId,
        ref string inputText,
        ref BigInteger numericValue,
        float valueWidth)
    {
        ImGui.TableSetColumnIndex(labelColumn);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);

        ImGui.TableSetColumnIndex(valueColumn);
        ImGui.SetNextItemWidth(valueWidth);

        bool confirmedByEnter = ImGui.InputText(inputId, ref inputText, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        bool confirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();

        if (confirmedByEnter || confirmedByFocusLoss)
        {
            numericValue = ParseBankValue(inputText);
            inputText = FormatNumber(numericValue);
            SaveCurrentProfileValues();
        }
    }

    private void DrawMessageSection()
    {
        var includeTimestamp = Plugin.Configuration.IncludeTimestampInMessage;
        if (ImGui.Checkbox("Include timestamp in message", ref includeTimestamp))
        {
            Plugin.Configuration.IncludeTimestampInMessage = includeTimestamp;
            Plugin.Configuration.Save();
        }

        var finalMessage = BuildFinalMessage();
        string copyText = DateTime.UtcNow < copiedUntilUtc ? "✓ Copied" : "Copy";

        if (DrawStyledBoldButton(copyText, "CopyButton", new Vector2(80f, 0f), CopyButtonColor))
        {
            ImGui.SetClipboardText(finalMessage);
            copiedUntilUtc = DateTime.UtcNow.AddSeconds(5);

            if (Plugin.Configuration.AutoClearAfterCopy)
                ClearCurrentInputs();
        }

        ImGui.SameLine();

        float fieldWidth = MathF.Max(200f, ImGui.GetContentRegionAvail().X - 6f);
        ImGui.SetNextItemWidth(fieldWidth);
        ImGui.BeginDisabled();
        ImGui.InputText("##FinalMessage", ref finalMessage, 4096, ImGuiInputTextFlags.ReadOnly);
        ImGui.EndDisabled();
    }

    private void DrawHistorySection()
    {
        ImGui.Text("Sort by:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("##SortBy", sortBy))
        {
            DrawSortOption("Most recent");
            DrawSortOption("Ascending");
            DrawSortOption("Results");
            DrawSortOption("Tips");
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Undo", "UndoButton", new Vector2(70f, 0f), UtilityButtonColor))
            UndoMostRecentHistory();

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear History", "ClearHistoryButton", new Vector2(115f, 0f), UtilityButtonColor))
        {
            Plugin.Configuration.GetOrCreateActiveProfile().History.Clear();
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        DrawProfitsLossesSummary();

        var sortedHistory = GetSortedHistory();

        if (!ImGui.BeginTable("##HistoryTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0f, 250f)))
            return;

        ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 110f);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn("Start Bank", ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableSetupColumn("Final Bank", ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableSetupColumn("Tips", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Results", ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableHeadersRow();

        foreach (var entry in sortedHistory)
        {
            SplitTimestamp(entry.Timestamp, out var datePart, out var timePart);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            DrawColoredCell(datePart, GoldColor);

            ImGui.TableSetColumnIndex(1);
            DrawColoredCell(timePart, NeutralColor);

            ImGui.TableSetColumnIndex(2);
            DrawColoredCell(entry.StartingBank, GoldColor);

            ImGui.TableSetColumnIndex(3);
            DrawColoredCell(entry.FinalBank, GoldColor);

            ImGui.TableSetColumnIndex(4);
            DrawColoredCell(entry.Tips, NeutralColor);

            ImGui.TableSetColumnIndex(5);
            var resultColor = ParseSignedFormatted(entry.Result) < BigInteger.Zero ? LossColor : ProfitColor;
            DrawColoredCell(entry.Result, resultColor);
        }

        ImGui.EndTable();
    }

    private void DrawColoredCell(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private void DrawProfitsLossesSummary()
    {
        var history = Plugin.Configuration.GetOrCreateActiveProfile().History;

        BigInteger totalProfits = BigInteger.Zero;
        BigInteger totalLosses = BigInteger.Zero;

        foreach (var entry in history)
        {
            var result = ParseSignedFormatted(entry.Result);
            var tips = ParseBankValue(entry.Tips);

            if (result > BigInteger.Zero)
                totalProfits += result;

            if (tips > BigInteger.Zero)
                totalProfits += tips;

            if (result < BigInteger.Zero)
                totalLosses += BigInteger.Abs(result);
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ProfitColor);
        ImGui.TextUnformatted("Profits");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(" / ");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, LossColor);
        ImGui.TextUnformatted("Losses");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 6f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(": ");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, ProfitColor);
        ImGui.TextUnformatted($"+{FormatNumber(totalProfits)}");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(" / ");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, LossColor);
        ImGui.TextUnformatted($"-{FormatNumber(totalLosses)}");
        ImGui.PopStyleColor();
    }

    private bool DrawStyledBoldButton(
        string text,
        string id,
        Vector2 size,
        Vector4 baseColor,
        Vector4? textColorOverride = null,
        Vector4? hoverColorOverride = null,
        Vector4? activeColorOverride = null)
    {
        Vector4 hoverColor = hoverColorOverride ?? Lighten(baseColor, 0.18f);
        Vector4 activeColor = activeColorOverride ?? Lighten(baseColor, 0.08f);
        Vector4 textColor = textColorOverride ?? WhiteText;

        ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f));

        bool pressed = ImGui.Button($"##{id}", size);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(
            min.X + ((max.X - min.X) - textSize.X) * 0.5f,
            min.Y + ((max.Y - min.Y) - textSize.Y) * 0.5f);

        var drawList = ImGui.GetWindowDrawList();
        uint drawColor = ImGui.ColorConvertFloat4ToU32(textColor);

        drawList.AddText(textPos, drawColor, text);
        drawList.AddText(textPos + new Vector2(1f, 0f), drawColor, text);

        ImGui.PopStyleColor(4);
        return pressed;
    }

    private static Vector4 Lighten(Vector4 color, float amount)
    {
        return new Vector4(
            Math.Clamp(color.X + amount, 0f, 1f),
            Math.Clamp(color.Y + amount, 0f, 1f),
            Math.Clamp(color.Z + amount, 0f, 1f),
            color.W);
    }

    private void DrawSortOption(string option)
    {
        bool selected = string.Equals(sortBy, option, StringComparison.OrdinalIgnoreCase);
        if (ImGui.Selectable(option, selected))
            sortBy = option;

        if (selected)
            ImGui.SetItemDefaultFocus();
    }

    private List<HistoryEntry> GetSortedHistory()
    {
        var history = Plugin.Configuration.GetOrCreateActiveProfile().History;

        return sortBy switch
        {
            "Ascending" => history.OrderBy(x => ParseTimestamp(x.Timestamp)).ToList(),
            "Results" => history.OrderByDescending(x => ParseSignedFormatted(x.Result)).ToList(),
            "Tips" => history.OrderByDescending(x => ParseBankValue(x.Tips)).ToList(),
            _ => history.OrderByDescending(x => ParseTimestamp(x.Timestamp)).ToList(),
        };
    }

    private void UndoMostRecentHistory()
    {
        var history = Plugin.Configuration.GetOrCreateActiveProfile().History;
        if (history.Count == 0)
            return;

        var mostRecent = history
            .OrderByDescending(x => ParseTimestamp(x.Timestamp))
            .First();

        history.Remove(mostRecent);
        Plugin.Configuration.Save();
    }

    private void LoadFromConfiguration()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();

        startingBankInput = profile.StartingBankInput ?? string.Empty;
        finalBankInput = profile.FinalBankInput ?? string.Empty;
        tipsInput = profile.TipsInput ?? string.Empty;

        resultsLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.ResultsLabel)
            ? "Today Profit/Loss:"
            : Plugin.Configuration.ResultsLabel;

        startingLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.StartingLabel)
            ? "Starting Bank:"
            : Plugin.Configuration.StartingLabel;

        finalLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.FinalLabel)
            ? "Final Bank:"
            : Plugin.Configuration.FinalLabel;

        startingBankValue = ParseBankValue(startingBankInput);
        finalBankValue = ParseBankValue(finalBankInput);
        tipsValue = ParseBankValue(tipsInput);

        startingBankInput = FormatNumber(startingBankValue);
        finalBankInput = FormatNumber(finalBankValue);
        tipsInput = FormatNumber(tipsValue);
    }

    private void SaveCurrentProfileValues()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();

        profile.StartingBankInput = startingBankInput;
        profile.FinalBankInput = finalBankInput;
        profile.TipsInput = tipsInput;

        Plugin.Configuration.ResultsLabel = resultsLabel;
        Plugin.Configuration.StartingLabel = startingLabel;
        Plugin.Configuration.FinalLabel = finalLabel;

        Plugin.Configuration.Save();
    }

    private void ClearCurrentInputs()
    {
        startingBankInput = string.Empty;
        finalBankInput = string.Empty;
        tipsInput = string.Empty;

        startingBankValue = BigInteger.Zero;
        finalBankValue = BigInteger.Zero;
        tipsValue = BigInteger.Zero;

        SaveCurrentProfileValues();
    }

    private void CreateProfile()
    {
        string trimmed = newProfileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        foreach (var profile in Plugin.Configuration.Profiles)
        {
            if (string.Equals(profile.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                return;
        }

        SaveCurrentProfileValues();

        Plugin.Configuration.Profiles.Add(new ProfileData { Name = trimmed });
        Plugin.Configuration.ActiveProfileName = trimmed;
        Plugin.Configuration.Save();

        newProfileName = string.Empty;
        LoadFromConfiguration();
    }

    private void DeleteCurrentProfile()
    {
        if (Plugin.Configuration.Profiles.Count <= 1)
            return;

        var profile = Plugin.Configuration.GetActiveProfile();
        if (profile == null)
            return;

        Plugin.Configuration.Profiles.Remove(profile);
        Plugin.Configuration.ActiveProfileName = Plugin.Configuration.Profiles[0].Name;
        Plugin.Configuration.Save();

        LoadFromConfiguration();
    }

    private void AddHistoryEntry()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();

        profile.History.Add(new HistoryEntry
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = FormatNumber(tipsValue),
            Result = GetCurrentResultText()
        });

        if (profile.History.Count > 200)
            profile.History.RemoveAt(0);

        Plugin.Configuration.Save();
    }

    private string BuildFinalMessage()
    {
        string prefix = Plugin.Configuration.IncludeTimestampInMessage
            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm}] "
            : string.Empty;

        return $"{prefix}{resultsLabel} {GetCurrentResultText()} Gil | {startingLabel} {FormatNumber(startingBankValue)} Gil | {finalLabel} {FormatNumber(finalBankValue)} Gil";
    }

    private string GetCurrentResultText()
    {
        var value = finalBankValue - startingBankValue - tipsValue;

        if (value > BigInteger.Zero)
            return $"+ {FormatNumber(value)}";

        if (value < BigInteger.Zero)
            return $"- {FormatNumber(BigInteger.Abs(value))}";

        return "0";
    }

    private static void SplitTimestamp(string timestamp, out string datePart, out string timePart)
    {
        if (DateTime.TryParseExact(
                timestamp,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            datePart = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            timePart = parsed.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return;
        }

        var split = timestamp.Split(' ');
        datePart = split.Length > 0 ? split[0] : string.Empty;
        timePart = split.Length > 1 ? split[1] : string.Empty;
    }

    private static DateTime ParseTimestamp(string timestamp)
    {
        if (DateTime.TryParseExact(
                timestamp,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        return DateTime.MinValue;
    }

    private static BigInteger ParseSignedFormatted(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return BigInteger.Zero;

        bool negative = input.Contains('-');
        var digitsOnly = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (char.IsDigit(c))
                digitsOnly.Append(c);
        }

        if (digitsOnly.Length == 0)
            return BigInteger.Zero;

        if (!BigInteger.TryParse(digitsOnly.ToString(), out var value))
            return BigInteger.Zero;

        return negative ? -value : value;
    }

    private static BigInteger ParseBankValue(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return BigInteger.Zero;

        var digitsOnly = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (char.IsDigit(c))
                digitsOnly.Append(c);
        }

        if (digitsOnly.Length == 0)
            return BigInteger.Zero;

        return BigInteger.TryParse(digitsOnly.ToString(), out var value) ? value : BigInteger.Zero;
    }

    private static string FormatNumber(BigInteger value)
    {
        var digits = BigInteger.Abs(value).ToString();

        if (digits.Length <= 3)
            return digits;

        var builder = new StringBuilder();
        var firstGroupLength = digits.Length % 3;
        if (firstGroupLength == 0)
            firstGroupLength = 3;

        builder.Append(digits.Substring(0, firstGroupLength));

        for (var i = firstGroupLength; i < digits.Length; i += 3)
        {
            builder.Append('.');
            builder.Append(digits.Substring(i, 3));
        }

        return builder.ToString();
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