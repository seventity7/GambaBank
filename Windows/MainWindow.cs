using System;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class MainWindow : Window, IDisposable
{
    private string startingBankInput;
    private string finalBankInput;
    private string tipsInput;

    private BigInteger startingBankValue;
    private BigInteger finalBankValue;
    private BigInteger tipsValue;

    private string resultsLabel;
    private string startingLabel;
    private string finalLabel;

    private static readonly Vector2 FixedWindowSize = new(735f, 195f);

    public MainWindow()
        : base("Bank Calc", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        startingBankInput = Plugin.Configuration.StartingBankInput ?? string.Empty;
        finalBankInput = Plugin.Configuration.FinalBankInput ?? string.Empty;
        tipsInput = Plugin.Configuration.TipsInput ?? string.Empty;

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

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = FixedWindowSize,
            MaximumSize = FixedWindowSize
        };

        Size = FixedWindowSize;
        SizeCondition = ImGuiCond.Always;
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        Size = FixedWindowSize;
        SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        DrawBankFields();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawFinalMessageRow();
        ImGui.Dummy(new Vector2(0f, 8f));
        DrawEditMessageSection();
    }

    private void DrawBankFields()
    {
        const float labelWidth = 0f;
        const float valueWidth = 180f;

        if (ImGui.BeginTable("##BankFieldsTable", 6, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthFixed, valueWidth);
            ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthFixed, valueWidth);
            ImGui.TableSetupColumn("Label3", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("Value3", ImGuiTableColumnFlags.WidthFixed, valueWidth);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Starting Bank:");

            ImGui.TableSetColumnIndex(1);
            ImGui.SetNextItemWidth(valueWidth);
            bool startingConfirmedByEnter = ImGui.InputText("##StartingBank", ref startingBankInput, 256, ImGuiInputTextFlags.EnterReturnsTrue);
            bool startingConfirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();
            if (startingConfirmedByEnter || startingConfirmedByFocusLoss)
            {
                startingBankValue = ParseBankValue(startingBankInput);
                startingBankInput = FormatNumber(startingBankValue);
                SaveConfiguration();
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Final Bank:");

            ImGui.TableSetColumnIndex(3);
            ImGui.SetNextItemWidth(valueWidth);
            bool finalConfirmedByEnter = ImGui.InputText("##FinalBank", ref finalBankInput, 256, ImGuiInputTextFlags.EnterReturnsTrue);
            bool finalConfirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();
            if (finalConfirmedByEnter || finalConfirmedByFocusLoss)
            {
                finalBankValue = ParseBankValue(finalBankInput);
                finalBankInput = FormatNumber(finalBankValue);
                SaveConfiguration();
            }

            ImGui.TableSetColumnIndex(4);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Results:");

            ImGui.TableSetColumnIndex(5);
            ImGui.SetNextItemWidth(valueWidth);
            var resultText = GetSignedResultText();
            ImGui.BeginDisabled();
            ImGui.InputText("##Results", ref resultText, 256, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Tips:");

            ImGui.TableSetColumnIndex(1);
            ImGui.SetNextItemWidth(valueWidth);
            bool tipsConfirmedByEnter = ImGui.InputText("##Tips", ref tipsInput, 256, ImGuiInputTextFlags.EnterReturnsTrue);
            bool tipsConfirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();
            if (tipsConfirmedByEnter || tipsConfirmedByFocusLoss)
            {
                tipsValue = ParseBankValue(tipsInput);
                tipsInput = FormatNumber(tipsValue);
                SaveConfiguration();
            }

            ImGui.EndTable();
        }
    }

    private void DrawFinalMessageRow()
    {
        var finalMessage = BuildFinalMessage();

        ImGui.SetNextItemWidth(740f);
        ImGui.InputText("##FinalMessage", ref finalMessage, 2048, ImGuiInputTextFlags.ReadOnly);

        ImGui.SameLine();

        if (ImGui.Button("Copy"))
        {
            ImGui.SetClipboardText(finalMessage);
        }
    }

    private void DrawEditMessageSection()
    {
        if (ImGui.CollapsingHeader("Edit message", ImGuiTreeNodeFlags.DefaultOpen))
        {
            const float editWidth = 260f;

            ImGui.SetNextItemWidth(editWidth);
            if (ImGui.InputText("##ResultsLabel", ref resultsLabel, 256))
            {
                SaveConfiguration();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(editWidth);
            if (ImGui.InputText("##StartingLabel", ref startingLabel, 256))
            {
                SaveConfiguration();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(editWidth);
            if (ImGui.InputText("##FinalLabel", ref finalLabel, 256))
            {
                SaveConfiguration();
            }
        }
    }

    private string BuildFinalMessage()
    {
        return $"{resultsLabel} {GetSignedResultText()} Gil | {startingLabel} {FormatNumber(startingBankValue)} Gil | {finalLabel} {FormatNumber(finalBankValue)} Gil";
    }

    private string GetSignedResultText()
    {
        var result = startingBankValue - finalBankValue - tipsValue;

        if (result > BigInteger.Zero)
            return $"+ {FormatNumber(result)}";

        if (result < BigInteger.Zero)
            return $"- {FormatNumber(BigInteger.Abs(result))}";

        return "0";
    }

    private void SaveConfiguration()
    {
        Plugin.Configuration.StartingBankInput = startingBankInput;
        Plugin.Configuration.FinalBankInput = finalBankInput;
        Plugin.Configuration.TipsInput = tipsInput;
        Plugin.Configuration.ResultsLabel = resultsLabel;
        Plugin.Configuration.StartingLabel = startingLabel;
        Plugin.Configuration.FinalLabel = finalLabel;
        Plugin.Configuration.Save();
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
}