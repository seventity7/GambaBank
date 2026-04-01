using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class MainWindow : Window, IDisposable
{
    private string startingBankInput = string.Empty;
    private string finalBankInput = string.Empty;
    private string tipsInput = string.Empty;
    private string houseInput = string.Empty;
    private string betInput = string.Empty;
    private string trackedDealerInput = string.Empty;
    private string searchInput = string.Empty;
    private string newProfileName = string.Empty;
    private string bankingInput = string.Empty;

    private bool betInputHasUserEdited;
    private bool bankingInputHasUserEdited;

    private BigInteger startingBankValue;
    private BigInteger finalBankValue;
    private BigInteger tipsValue;
    private BigInteger betValue;
    private BigInteger bankingValue;

    private string resultsLabel = string.Empty;
    private string startingLabel = string.Empty;
    private string finalLabel = string.Empty;

    private string sortBy = "Most recent";
    private DateTime copiedUntilUtc = DateTime.MinValue;
    private bool formulaHelpPinned;
    private Vector2 pinnedTooltipPosition = Vector2.Zero;
    private bool hasPinnedTooltipPosition;

    private bool playerAutoTrackEnabled;
    private int natbjMultiplierIndex;
    private int dirtytbjMultiplierIndex;
    private DateTime trackDealerButtonDisabledUntilUtc = DateTime.MinValue;
    private DateTime currentBetDisplayOverrideUntilUtc = DateTime.MinValue;
    private string currentBetDisplayOverrideText = string.Empty;
    private Vector4 currentBetDisplayOverrideColor = new(1f, 1f, 1f, 1f);

    // Add new chat keywords here if your dealer uses different result words.
    private static readonly string[] WinTrackLabels = { "Win", "Won", "Winners", "Winner", "Wins" };
    private static readonly string[] LossTrackLabels = { "Loss", "Lost", "Busted", "Losses", "Losts", "Busts", "Busteds"};
    private static readonly string[] PushTrackLabels = { "Push", "Pushed" };

    private static readonly Regex TrackResultRegex = BuildTrackResultRegex();
    private static readonly string[] BlackjackMultiplierLabels = { "1.0x", "1.3x", "1.7x", "1.5x", "2.0x", "2.5x", "3.0x" };
    private static readonly int[] BlackjackMultiplierTenths = { 10, 13, 17, 15, 20, 25, 30 };

    private static readonly Vector2 DefaultWindowSize = new(1080f, 520f);
    private static readonly Vector2 MinimumWindowSize = new(980f, 470f);

    private static readonly Vector4 ProfitColor = new(0.35f, 0.85f, 0.35f, 1.0f);
    private static readonly Vector4 LossColor = new(1.0f, 0.45f, 0.45f, 1.0f);
    private static readonly Vector4 NeutralColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Vector4 GoldColor = new(0.95f, 0.80f, 0.25f, 1.0f);
    private static readonly Vector4 BlackjackColor = Hex("#00c0c7");
    private static readonly Vector4 BlackText = new(0.0f, 0.0f, 0.0f, 1.0f);

    private static readonly Vector4 CopyButtonColor = Hex("#005210");
    private static readonly Vector4 UtilityButtonColor = Hex("#005232");
    private static readonly Vector4 DangerButtonColor = Hex("#520000");
    private static readonly Vector4 WhiteText = new(1.0f, 1.0f, 1.0f, 1.0f);

    private static readonly Vector4 DealerButtonColor = Hex("#ffbb00");
    private static readonly Vector4 PlayerButtonColor = Hex("#006496");
    private static readonly Vector4 WinButtonColor = Hex("#098500");
    private static readonly Vector4 LossButtonColor = Hex("#852100");
    private static readonly Vector4 AutoTrackButtonColor = Hex("#ff7700");
    private static readonly Vector4 AutoTrackGradientStartColor = LossButtonColor;
    private static readonly Vector4 AutoTrackGradientEndColor = WinButtonColor;

    private static readonly Vector4 HelpButtonColor = Hex("#2C2C2C");
    private static readonly Vector4 HelpButtonHoverColor = Hex("#3A3A3A");
    private static readonly Vector4 HelpButtonActiveColor = Hex("#4A4A4A");
    private static readonly Vector4 HelpButtonTextColor = Hex("#FF8A8A");

    private bool IsPlayerMode => string.Equals(Plugin.Configuration.ActiveMode, "Player", StringComparison.OrdinalIgnoreCase);
    private bool IsDealerMode => !IsPlayerMode;
    private bool IsTrackDealerButtonDisabled => DateTime.UtcNow < trackDealerButtonDisabledUntilUtc;
    private bool IsAutoTrackingActive => IsPlayerMode && playerAutoTrackEnabled && !string.IsNullOrWhiteSpace(trackedDealerInput);

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

        Plugin.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
    }

    public override void Draw()
    {
        DrawProfilesSection();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawBankFields();

        if (IsDealerMode)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawMessageSection();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawHistorySection();
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

        const float topHelpButtonWidth = 58f;
        const float topModeButtonWidth = 70f;
        const float topQuestionButtonWidth = 22f;
        const float dividerVisualWidth = 8f;

        ImGui.SameLine();
        AlignTopRightControls(
            topHelpButtonWidth +
            dividerVisualWidth +
            topModeButtonWidth +
            topModeButtonWidth +
            topQuestionButtonWidth +
            (ImGui.GetStyle().ItemSpacing.X * 4f));

        if (DrawStyledBoldButton("Help", "OpenHelpWindowButton", new Vector2(topHelpButtonWidth, 20f), Hex("#2aa163"), WhiteText))
            Plugin.OpenHelpUi();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("|");

        ImGui.SameLine();
        if (DrawStyledBoldButton(
                "Dealer",
                "DealerModeButton",
                new Vector2(topModeButtonWidth, 20f),
                IsDealerMode ? DealerButtonColor : Darken(DealerButtonColor, 0.20f),
                WhiteText,
                pulsatingGlow: IsDealerMode))
        {
            SetMode("Dealer");
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton(
                "Player",
                "PlayerModeButton",
                new Vector2(topModeButtonWidth, 20f),
                IsPlayerMode ? PlayerButtonColor : Darken(PlayerButtonColor, 0.20f),
                WhiteText,
                pulsatingGlow: IsPlayerMode))
        {
            SetMode("Player");
        }

        ImGui.SameLine();
        DrawHelpButton();
    }

    private void DrawBankFields()
    {
        if (IsPlayerMode)
        {
            DrawPlayerBankFields();
            return;
        }

        DrawDealerBankFields();
    }

    private void DrawDealerBankFields()
    {
        if (!ImGui.BeginTable("##DealerBankFieldsTable", 7, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Label3", ImGuiTableColumnFlags.WidthFixed, 55f);
        ImGui.TableSetupColumn("Value3", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 170f);

        ImGui.TableNextRow();

        DrawEditableNumericCell(
            0,
            1,
            "Starting Bank:",
            "##StartingBank",
            ref startingBankInput,
            ref startingBankValue,
            145f,
            OnStartingBankCommitted);

        DrawEditableNumericCell(
            2,
            3,
            "Final Bank:",
            "##FinalBank",
            ref finalBankInput,
            ref finalBankValue,
            145f);

        DrawEditableTextCell(4, 5, "House:", "##HouseInput", ref houseInput, 130f);

        ImGui.TableSetColumnIndex(6);
        if (DrawStyledBoldButton("Save", "SaveToHistoryButton", new Vector2(70f, 0f), CopyButtonColor))
            AddHistoryEntry();

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear", "DealerClearButton", new Vector2(70f, 0f), LossButtonColor, WhiteText))
            ClearCurrentInputs();

        ImGui.TableNextRow();

        DrawReadOnlyNumericCell(0, 1, "Results:", "##Results", GetCurrentResultText(), 145f);

        DrawEditableNumericCell(
            2,
            3,
            "Tips:",
            "##Tips",
            ref tipsInput,
            ref tipsValue,
            145f);

        ImGui.EndTable();
    }

    private void DrawPlayerBankFields()
    {
        DrawSectionTitle("☆ Bank Tracking");
        ImGui.Dummy(new Vector2(0f, 2f));

        if (ImGui.BeginTable("##PlayerTopBankFieldsTable", 5, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("PlayerLeftColumn", ImGuiTableColumnFlags.WidthFixed, 250f);
            ImGui.TableSetupColumn("PlayerCenterColumn", ImGuiTableColumnFlags.WidthFixed, 250f);
            ImGui.TableSetupColumn("PlayerActionColumn", ImGuiTableColumnFlags.WidthFixed, 96f);
            ImGui.TableSetupColumn("PlayerTopSeparatorColumn", ImGuiTableColumnFlags.WidthFixed, 14f);
            ImGui.TableSetupColumn("PlayerAddBankColumn", ImGuiTableColumnFlags.WidthFixed, 218f);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            DrawPlayerLeftFields();

            ImGui.TableSetColumnIndex(1);
            DrawPlayerCenterFields();

            ImGui.TableSetColumnIndex(2);
            DrawPlayerActionButtons();

            ImGui.TableSetColumnIndex(3);
            DrawTallVerticalSeparator((ImGui.GetFrameHeight() * 2f) + ImGui.GetStyle().ItemSpacing.Y);

            ImGui.TableSetColumnIndex(4);
            DrawPlayerAddBankControls();

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawSectionTitle("♯ Bet Tracking");
        ImGui.Dummy(new Vector2(0f, 2f));
        DrawPlayerTrackingLayout();
    }

    private void DrawPlayerLeftFields()
    {
        if (!ImGui.BeginTable("##PlayerLeftFieldsTable", 2, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("PlayerLeftLabel", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("PlayerLeftValue", ImGuiTableColumnFlags.WidthFixed, 145f);

        ImGui.TableNextRow();
        DrawEditableNumericCell(
            0,
            1,
            "Starting Bank:",
            "##StartingBank",
            ref startingBankInput,
            ref startingBankValue,
            145f,
            OnStartingBankCommitted);

        ImGui.TableNextRow();
        DrawEditableTextCell(0, 1, "House:", "##HouseInput", ref houseInput, 145f);

        ImGui.EndTable();
    }


    private void DrawPlayerCenterFields()
    {
        if (!ImGui.BeginTable("##PlayerCenterFieldsTable", 2, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("PlayerCenterLabel", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("PlayerCenterValue", ImGuiTableColumnFlags.WidthFixed, 145f);

        ImGui.TableNextRow();
        var currentBankColor = finalBankValue < startingBankValue ? LossColor : NeutralColor;
        DrawReadOnlyNumericCell(0, 1, "Current Bank:", "##CurrentBank", finalBankInput, 145f, currentBankColor);

        ImGui.TableNextRow();
        DrawReadOnlyNumericCell(0, 1, "Results:", "##Results", GetCurrentResultText(), 145f);

        ImGui.EndTable();
    }


    private void DrawPlayerActionButtons()
    {
        const float actionButtonWidth = 82f;

        if (!ImGui.BeginTable("##PlayerActionButtonsTable", 1, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("PlayerActionButtonsColumn", ImGuiTableColumnFlags.WidthFixed, actionButtonWidth);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        if (DrawStyledBoldButton("Save", "SaveToHistoryButton", new Vector2(actionButtonWidth, 0f), CopyButtonColor))
            AddHistoryEntry();

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        if (DrawStyledBoldButton("Clear", "PlayerClearButton", new Vector2(actionButtonWidth, 0f), LossButtonColor, WhiteText))
            ClearCurrentInputs();

        ImGui.EndTable();
    }








    private void DrawPlayerAddBankControls()
    {
        const float bankingFieldWidth = 168f;
        const float quickAddButtonWidth = 42f;

        string? bankingDisplayText = null;
        Vector4? bankingDisplayColor = null;
        bool bankingIsZero = bankingValue == BigInteger.Zero && ParseBankValue(bankingInput) == BigInteger.Zero;
        if (bankingIsZero)
        {
            bankingDisplayText = "0";
            bankingDisplayColor = LossColor;
        }

        if (!ImGui.BeginTable("##PlayerAddBankControlsTable", 2, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("PlayerAddBankValueColumn", ImGuiTableColumnFlags.WidthFixed, bankingFieldWidth);
        ImGui.TableSetupColumn("PlayerAddBankQuickColumn", ImGuiTableColumnFlags.WidthFixed, quickAddButtonWidth);

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        DrawCenteredNumericInputField(
            "##BankingValue",
            ref bankingInput,
            ref bankingValue,
            bankingFieldWidth,
            ref bankingInputHasUserEdited,
            true,
            null,
            bankingDisplayText,
            bankingDisplayColor,
            0f,
            true,
            true);

        ImGui.TableSetColumnIndex(1);
        if (DrawStyledBoldButton("+1M", "AddBanking1MTopButton", new Vector2(quickAddButtonWidth, 0f), Hex("#741a53"), WhiteText))
            IncrementBankingValue(1_000_000);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        if (DrawStyledBoldButton("Add Bank", "ApplyBankingButton", new Vector2(bankingFieldWidth, 0f), CopyButtonColor, WhiteText))
            ApplyBankingAdjustment();

        ImGui.TableSetColumnIndex(1);
        ImGui.Dummy(Vector2.Zero);

        ImGui.EndTable();
    }

    private void DrawTallVerticalSeparator(float height)
    {
        Vector2 start = ImGui.GetCursorScreenPos();
        float x = start.X + 6f;
        uint separatorColor = ImGui.GetColorU32(ImGuiCol.Separator);
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(x, start.Y),
            new Vector2(x, start.Y + height),
            separatorColor,
            1f);
        ImGui.Dummy(new Vector2(12f, height));
    }

private void DrawPlayerTrackingLayout()
{
    const float labelWidth = 96f;
    const float betFieldWidth = 140f;
    const float middleSpacerWidth = 16f;
    const float clonedBlockWidth = 145f;
    const float separatorColumnWidth = 14f;
    const float dealerBlockWidth = 145f;

    float spacing = ImGui.GetStyle().ItemSpacing.X;
    float originalButtonWidth = (betFieldWidth - spacing) * 0.5f;
    float clonedButtonWidth = (clonedBlockWidth - spacing) * 0.5f;

    GetTrackingStatusDisplay(out var statusText, out var statusColor);
    GetCurrentBetDisplay(out var currentBetOverrideText, out var currentBetOverrideColor);

    bool startingBankMissing = startingBankValue <= BigInteger.Zero;
    BigInteger currentBetAmount = ParseBankValue(betInput);
    bool notEnoughBank = !startingBankMissing && currentBetAmount > BigInteger.Zero && finalBankValue < currentBetAmount;

    string? currentBetDisplayText;
    Vector4? currentBetDisplayColor;

    if (startingBankMissing)
    {
        currentBetDisplayText = "Need Start Bank";
        currentBetDisplayColor = LossColor;
    }
    else if (notEnoughBank)
    {
        currentBetDisplayText = "Not enough bank";
        currentBetDisplayColor = LossColor;
    }
    else
    {
        currentBetDisplayText = currentBetOverrideText;
        currentBetDisplayColor = currentBetOverrideColor;
    }

    if (!ImGui.BeginTable("##PlayerTrackingUnifiedLayout", 6, ImGuiTableFlags.SizingFixedFit))
        return;

    ImGui.TableSetupColumn("TrackingLabelColumn", ImGuiTableColumnFlags.WidthFixed, labelWidth);
    ImGui.TableSetupColumn("TrackingOriginalColumn", ImGuiTableColumnFlags.WidthFixed, betFieldWidth);
    ImGui.TableSetupColumn("TrackingSpacerColumn", ImGuiTableColumnFlags.WidthFixed, middleSpacerWidth);
    ImGui.TableSetupColumn("TrackingCloneColumn", ImGuiTableColumnFlags.WidthFixed, clonedBlockWidth);
    ImGui.TableSetupColumn("TrackingSeparatorColumn", ImGuiTableColumnFlags.WidthFixed, separatorColumnWidth);
    ImGui.TableSetupColumn("TrackingDealerColumn", ImGuiTableColumnFlags.WidthFixed, dealerBlockWidth);

    // Row 1
    ImGui.TableNextRow();

    DrawCenteredEditableNumericCell(
        0,
        1,
        "$ Current Bet:",
        "##BetValue",
        ref betInput,
        ref betValue,
        betFieldWidth,
        ref betInputHasUserEdited,
        !startingBankMissing && !notEnoughBank,
        null,
        currentBetDisplayText,
        currentBetDisplayColor);

    ImGui.TableSetColumnIndex(2);
    ImGui.Dummy(Vector2.Zero);

    string? clonedBetDisplayText;
    Vector4? clonedBetDisplayColor;

    BigInteger clonedBetValue = ParseBankValue(betInput);
    if (clonedBetValue > BigInteger.Zero)
    {
        clonedBetDisplayText = FormatNumber(clonedBetValue);
        clonedBetDisplayColor = NeutralColor;
    }
    else
    {
        clonedBetDisplayText = "Set Current Bet";
        clonedBetDisplayColor = LossColor;
    }

    ImGui.TableSetColumnIndex(3);
    DrawCenteredNumericInputField(
        "##ClonedBetValue",
        ref betInput,
        ref betValue,
        clonedBlockWidth,
        ref betInputHasUserEdited,
        false,
        null,
        clonedBetDisplayText,
        clonedBetDisplayColor,
        0f,
        false,
        false);

    ImGui.TableSetColumnIndex(4);
    DrawMiniVerticalSeparator();

    ImGui.TableSetColumnIndex(5);
    ImGui.SetNextItemWidth(dealerBlockWidth);
    bool confirmedByEnter = ImGui.InputText("##TrackedDealerInput", ref trackedDealerInput, 128, ImGuiInputTextFlags.EnterReturnsTrue);
    bool confirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();

    if (confirmedByEnter || confirmedByFocusLoss)
    {
        trackedDealerInput = StripWorldSuffix(trackedDealerInput);
        SaveCurrentProfileValues();
    }

    // Row 2
    ImGui.TableNextRow();

    ImGui.TableSetColumnIndex(0);
    ImGui.AlignTextToFramePadding();
    ImGui.TextUnformatted("Tracking:");
    ImGui.SameLine(0f, 2f);
    ImGui.AlignTextToFramePadding();
    DrawBoldColoredText(statusText, statusColor);

    ImGui.TableSetColumnIndex(1);
    if (DrawStyledBoldButton("WIN ↑", "WinBetButton", new Vector2(originalButtonWidth, 0f), WinButtonColor, WhiteText))
        ApplyBetResult(true);

    ImGui.SameLine();
    if (DrawStyledBoldButton("↓ LOSS", "LossBetButton", new Vector2(originalButtonWidth, 0f), LossButtonColor, WhiteText))
        ApplyBetResult(false);

    ImGui.TableSetColumnIndex(2);
    ImGui.Dummy(Vector2.Zero);

    ImGui.TableSetColumnIndex(3);
    DrawMultiplierDropdownButton("Natbj", ref natbjMultiplierIndex, new Vector2(clonedButtonWidth, 0f), WinButtonColor);

    ImGui.SameLine();
    DrawMultiplierDropdownButton("Dirtytbj", ref dirtytbjMultiplierIndex, new Vector2(clonedButtonWidth, 0f), LossButtonColor);

    ImGui.TableSetColumnIndex(4);
    DrawMiniVerticalSeparator();

    ImGui.TableSetColumnIndex(5);
    if (IsTrackDealerButtonDisabled)
        ImGui.BeginDisabled();

    bool hasTrackedDealer = !string.IsNullOrWhiteSpace(trackedDealerInput);
    string trackButtonText = IsTrackDealerButtonDisabled
        ? "Target not found"
        : hasTrackedDealer ? "● Tracking Dealer:" : "○ Track Dealer";
    Vector4 trackButtonTextColor = IsTrackDealerButtonDisabled ? BlackText : WhiteText;

    if (DrawStyledBoldButton(trackButtonText, "TrackDealerButton", new Vector2(dealerBlockWidth, 0f), UtilityButtonColor, trackButtonTextColor) && !IsTrackDealerButtonDisabled)
        TrackDealerFromCurrentTarget();

    if (IsTrackDealerButtonDisabled)
        ImGui.EndDisabled();

    // Row 3
    ImGui.TableNextRow();

    ImGui.TableSetColumnIndex(0);
    ImGui.Dummy(Vector2.Zero);

    string autoTrackText = playerAutoTrackEnabled ? "● Auto Track" : "○ Auto Track";
    ImGui.TableSetColumnIndex(1);
    if (DrawGradientStyledBoldButton(
            autoTrackText,
            "AutoTrackButton",
            new Vector2(betFieldWidth, 0f),
            AutoTrackGradientStartColor,
            AutoTrackGradientEndColor,
            65f,
            0.74f,
            0.02f,
            WhiteText))
    {
        playerAutoTrackEnabled = !playerAutoTrackEnabled;
        SaveCurrentProfileValues();
    }

    ImGui.TableSetColumnIndex(2);
    ImGui.Dummy(Vector2.Zero);

    ImGui.TableSetColumnIndex(3);
    if (DrawStyledBoldButton("NAT BJ", "NatBjActionButton", new Vector2(clonedButtonWidth, 0f), Hex("#292f56"), WhiteText))
        ApplyBlackjackResult(natbjMultiplierIndex);

    ImGui.SameLine();
    if (DrawStyledBoldButton("DIRTY BJ", "DirtyBjActionButton", new Vector2(clonedButtonWidth, 0f), Hex("#741a53"), WhiteText))
        ApplyBlackjackResult(dirtytbjMultiplierIndex);

    ImGui.TableSetColumnIndex(4);
    DrawMiniVerticalSeparator();

    ImGui.TableSetColumnIndex(5);
    if (DrawStyledBoldButton("Clear", "ClearTrackedDealerButton", new Vector2(dealerBlockWidth, 0f), LossButtonColor, WhiteText))
    {
        trackedDealerInput = string.Empty;
        SaveCurrentProfileValues();
    }

    ImGui.EndTable();
}


private void DrawMultiplierDropdownButton(string internalName, ref int selectedIndex, Vector2 size, Vector4 buttonColor)
{
    selectedIndex = Math.Clamp(selectedIndex, 0, BlackjackMultiplierLabels.Length - 1);
    string visibleText = BlackjackMultiplierLabels[selectedIndex];

    Vector2 actualSize = size;
    if (actualSize.X <= 0f)
        actualSize.X = 120f;
    if (actualSize.Y <= 0f)
        actualSize.Y = ImGui.GetFrameHeight();

    bool pressed = ImGui.InvisibleButton($"##{internalName}", actualSize);
    bool hovered = ImGui.IsItemHovered();
    bool held = ImGui.IsItemActive();

    Vector2 min = ImGui.GetItemRectMin();
    Vector2 max = ImGui.GetItemRectMax();
    var drawList = ImGui.GetWindowDrawList();
    var style = ImGui.GetStyle();

    uint frameColor = ImGui.GetColorU32(held ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
    uint borderColor = ImGui.GetColorU32(ImGuiCol.Border);
    uint textColor = ImGui.GetColorU32(ImGuiCol.Text);

    drawList.AddRectFilled(min, max, frameColor, style.FrameRounding);
    drawList.AddRect(min, max, borderColor, style.FrameRounding, ImDrawFlags.None, 1f);

    float arrowRegionWidth = ImGui.GetFrameHeight();
    float arrowSeparatorX = max.X - arrowRegionWidth;
    drawList.AddLine(new Vector2(arrowSeparatorX, min.Y), new Vector2(arrowSeparatorX, max.Y), borderColor, 1f);

    Vector2 textSize = ImGui.CalcTextSize(visibleText);
    float textRegionWidth = arrowSeparatorX - min.X;
    Vector2 textPos = new(
        min.X + ((textRegionWidth - textSize.X) * 0.5f),
        min.Y + ((max.Y - min.Y) - textSize.Y) * 0.5f);
    drawList.AddText(textPos, textColor, visibleText);

    Vector2 arrowCenter = new((arrowSeparatorX + max.X) * 0.5f, (min.Y + max.Y) * 0.5f + 1f);
    drawList.AddTriangleFilled(
        new Vector2(arrowCenter.X - 4f, arrowCenter.Y - 2f),
        new Vector2(arrowCenter.X + 4f, arrowCenter.Y - 2f),
        new Vector2(arrowCenter.X, arrowCenter.Y + 3f),
        textColor);

    if (pressed)
        ImGui.OpenPopup($"##{internalName}Popup");

    float popupHeight = (BlackjackMultiplierLabels.Length * (ImGui.GetFrameHeight() + style.ItemSpacing.Y)) + (style.WindowPadding.Y * 2f);
    ImGui.SetNextWindowPos(new Vector2(min.X, min.Y - popupHeight - 2f), ImGuiCond.Appearing);

    if (ImGui.BeginPopup($"##{internalName}Popup"))
    {
        for (int i = BlackjackMultiplierLabels.Length - 1; i >= 0; i--)
        {
            bool isSelected = selectedIndex == i;
            if (ImGui.Selectable(BlackjackMultiplierLabels[i], isSelected))
            {
                selectedIndex = i;
                ImGui.CloseCurrentPopup();
                SaveCurrentProfileValues();
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndPopup();
    }
}

private void ApplyBlackjackResult(int multiplierIndex)
{
    multiplierIndex = Math.Clamp(multiplierIndex, 0, BlackjackMultiplierTenths.Length - 1);

    betValue = ParseBankValue(betInput);
    betInput = FormatNumber(betValue);
    betInputHasUserEdited = !string.IsNullOrWhiteSpace(betInput);

    if (betValue <= BigInteger.Zero)
    {
        SaveCurrentProfileValues();
        return;
    }

    if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(finalBankInput))
        finalBankValue = startingBankValue;

    BigInteger payout = (betValue * BlackjackMultiplierTenths[multiplierIndex]) / 10;
    finalBankValue += payout;
    finalBankInput = FormatNumber(finalBankValue);

    RemoveMostRecentHistoryEntry();
    AddBlackjackHistoryEntry(payout);

    SaveCurrentProfileValues();
}

private void RemoveMostRecentHistoryEntry()
{
    var history = GetCurrentHistory();
    if (history.Count == 0)
        return;

    history.RemoveAt(history.Count - 1);
}

private void AddBlackjackHistoryEntry(BigInteger payout)
{
    var history = GetCurrentHistory();

    history.Add(new HistoryEntry
    {
        House = houseInput.Trim(),
        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        StartingBank = FormatNumber(startingBankValue),
        FinalBank = FormatNumber(finalBankValue),
        Tips = string.Empty,
        Result = $"+{FormatNumber(payout)} Blackjack"
    });

    if (history.Count > 200)
        history.RemoveAt(0);

    Plugin.Configuration.Save();
}

private void DrawMiniVerticalSeparator()
{
    Vector2 start = ImGui.GetCursorScreenPos();
    float height = ImGui.GetFrameHeight();
    float x = start.X + 6f;
    uint separatorColor = ImGui.GetColorU32(ImGuiCol.Separator);
    ImGui.GetWindowDrawList().AddLine(
        new Vector2(x, start.Y),
        new Vector2(x, start.Y + height),
        separatorColor,
        1f);
    ImGui.Dummy(new Vector2(12f, height));
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
        ImGui.PushStyleColor(ImGuiCol.Text, Hex("#ffbb00"));
        ImGui.Text(IsPlayerMode ? "★ Player History" : "★ Dealer History");
        ImGui.PopStyleColor();
        ImGui.Spacing();

        ImGui.Text("Sort by:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("##SortBy", sortBy))
        {
            DrawSortOption("Most recent");
            DrawSortOption("Ascending");
            DrawSortOption("Results");

            if (IsPlayerMode)
                DrawSortOption("Blackjack");

            if (IsDealerMode)
                DrawSortOption("Tips");

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Text("Search:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(190f);
        ImGui.InputText("##HistorySearch", ref searchInput, 128);

        ImGui.SameLine();
        if (DrawStyledBoldButton("Undo", "UndoButton", new Vector2(70f, 0f), UtilityButtonColor))
            UndoMostRecentHistory();

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear History", "ClearHistoryButton", new Vector2(115f, 0f), UtilityButtonColor))
        {
            GetCurrentHistory().Clear();
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        DrawProfitsLossesSummary();

        var sortedHistory = GetSortedHistory();
        int columnCount = IsPlayerMode ? 5 : 6;

        ImGui.Dummy(new Vector2(0f, 1.5f));

        if (!ImGui.BeginTable(
                "##HistoryTable",
                columnCount,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                new Vector2(0f, 260f)))
            return;

        ImGui.TableSetupColumn("House", ImGuiTableColumnFlags.WidthFixed, 135f);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Start Bank", ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableSetupColumn(IsPlayerMode ? "Total Bank" : "Final Bank", ImGuiTableColumnFlags.WidthFixed, 140f);

        if (IsDealerMode)
            ImGui.TableSetupColumn("Tips", ImGuiTableColumnFlags.WidthFixed, 120f);

        ImGui.TableSetupColumn("Results", ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableHeadersRow();

        foreach (var entry in sortedHistory)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            DrawColoredCell(entry.House, NeutralColor);

            ImGui.TableSetColumnIndex(1);
            DrawColoredCell(entry.Timestamp, GoldColor);

            ImGui.TableSetColumnIndex(2);
            DrawColoredCell(entry.StartingBank, GoldColor);

            ImGui.TableSetColumnIndex(3);
            var totalBankParsed = ParseSignedFormatted(entry.FinalBank);
            var totalBankColor = totalBankParsed > BigInteger.Zero
                ? ProfitColor
                : totalBankParsed < BigInteger.Zero
                    ? LossColor
                    : GoldColor;
            DrawColoredCell(entry.FinalBank, totalBankColor);

            if (IsDealerMode)
            {
                ImGui.TableSetColumnIndex(4);
                DrawColoredCell(entry.Tips, NeutralColor);

                ImGui.TableSetColumnIndex(5);
            }
            else
            {
                ImGui.TableSetColumnIndex(4);
            }

            bool isBlackjackResult = entry.Result.Contains("Blackjack", StringComparison.OrdinalIgnoreCase);
            var resultColor = isBlackjackResult
                ? BlackjackColor
                : ParseSignedFormatted(entry.Result) < BigInteger.Zero ? LossColor : ProfitColor;

            if (isBlackjackResult)
                DrawBoldColoredCell(entry.Result, resultColor);
            else
                DrawColoredCell(entry.Result, resultColor);
        }

        ImGui.EndTable();
    }

    private void DrawHelpButton()
    {
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
            DrawFormulaTooltipContent();
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

            DrawFormulaTooltipContent();

            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }

    private void DrawFormulaTooltipContent()
    {
        if (IsPlayerMode)
        {
            DrawBoldTooltipLine("Player mode:");
            ImGui.TextUnformatted("{CurrentBank} - {StartingBank} = {Results}");
            ImGui.Spacing();
            ImGui.TextUnformatted("WIN adds {Bet} to {CurrentBank}");
            ImGui.TextUnformatted("LOSS subtracts {Bet} from {CurrentBank}");
            ImGui.Spacing();
            DrawBoldTooltipLine("Current Bank: 10.000.000 - Starting Bank: 5.000.000 = Results: + 5.000.000");
            return;
        }

        DrawBoldTooltipLine("Dealer mode:");
        ImGui.TextUnformatted("{FinalBank} - {StartingBank} - {Tips} = {Results}");
        ImGui.Spacing();
        ImGui.TextUnformatted("{FinalBank} minus {StartingBank} minus {Tips} equals {Results}");
        ImGui.Spacing();
        DrawBoldTooltipLine("Final Bank: 5.000.000 - Starting Bank: 2.000.000 - Tips: 1.000.000 = Results: + 2.000.000");
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

    private void DrawEditableNumericCell(
        int labelColumn,
        int valueColumn,
        string label,
        string inputId,
        ref string inputText,
        ref BigInteger numericValue,
        float valueWidth,
        Action? afterCommit = null)
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
            afterCommit?.Invoke();
            SaveCurrentProfileValues();
        }
    }

    private void DrawEditableTextCell(
        int labelColumn,
        int valueColumn,
        string label,
        string inputId,
        ref string inputText,
        float valueWidth)
    {
        ImGui.TableSetColumnIndex(labelColumn);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);

        ImGui.TableSetColumnIndex(valueColumn);
        ImGui.SetNextItemWidth(valueWidth);

        bool confirmedByEnter = ImGui.InputText(inputId, ref inputText, 128, ImGuiInputTextFlags.EnterReturnsTrue);
        bool confirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();

        if (confirmedByEnter || confirmedByFocusLoss)
            SaveCurrentProfileValues();
    }


    private void DrawCenteredEditableNumericCell(
        int labelColumn,
        int valueColumn,
        string label,
        string inputId,
        ref string inputText,
        ref BigInteger numericValue,
        float valueWidth,
        ref bool hasUserEdited,
        bool isInputEnabled,
        Action? afterCommit = null,
        string? displayOverrideText = null,
        Vector4? displayOverrideColor = null,
        float valueOffsetX = 0f,
        bool drawBoldDisplay = false,
        bool clearDisplayOverrideOnActivate = false)
    {
        ImGui.TableSetColumnIndex(labelColumn);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);

        ImGui.TableSetColumnIndex(valueColumn);
        DrawCenteredNumericInputField(
            inputId,
            ref inputText,
            ref numericValue,
            valueWidth,
            ref hasUserEdited,
            isInputEnabled,
            afterCommit,
            displayOverrideText,
            displayOverrideColor,
            valueOffsetX,
            drawBoldDisplay,
            clearDisplayOverrideOnActivate);
    }

    private void DrawCenteredNumericInputField(
        string inputId,
        ref string inputText,
        ref BigInteger numericValue,
        float valueWidth,
        ref bool hasUserEdited,
        bool isInputEnabled = true,
        Action? afterCommit = null,
        string? displayOverrideText = null,
        Vector4? displayOverrideColor = null,
        float valueOffsetX = 0f,
        bool drawBoldDisplay = false,
        bool clearDisplayOverrideOnActivate = false)
    {
        if (Math.Abs(valueOffsetX) > 0.001f)
            NudgeCursorX(valueOffsetX);

        ImGui.SetNextItemWidth(valueWidth);

        if (!isInputEnabled)
            ImGui.BeginDisabled();

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f));
        bool confirmedByEnter = ImGui.InputText(inputId, ref inputText, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        bool confirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();
        bool isActive = ImGui.IsItemActive();
        bool isActivated = ImGui.IsItemActivated();
        ImGui.PopStyleColor();

        if (!isInputEnabled)
            ImGui.EndDisabled();

        if (isInputEnabled &&
            isActivated &&
            !hasUserEdited &&
            numericValue == BigInteger.Zero &&
            ParseBankValue(inputText) == BigInteger.Zero)
        {
            inputText = string.Empty;
        }

        string? effectiveDisplayOverrideText = displayOverrideText;
        if (clearDisplayOverrideOnActivate && isActive && numericValue == BigInteger.Zero && ParseBankValue(inputText) == BigInteger.Zero)
            effectiveDisplayOverrideText = null;

        string displayText;
        if (!string.IsNullOrWhiteSpace(effectiveDisplayOverrideText))
        {
            displayText = effectiveDisplayOverrideText;
        }
        else if (isActive && !hasUserEdited && numericValue == BigInteger.Zero && string.IsNullOrWhiteSpace(inputText))
        {
            displayText = string.Empty;
        }
        else
        {
            displayText = string.IsNullOrWhiteSpace(inputText) ? "0" : inputText;
        }

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        Vector2 textSize = ImGui.CalcTextSize(displayText);
        Vector2 textPos = new(
            min.X + ((max.X - min.X) - textSize.X) * 0.5f,
            min.Y + ((max.Y - min.Y) - textSize.Y) * 0.5f);

        uint drawColor = ImGui.ColorConvertFloat4ToU32(displayOverrideColor ?? NeutralColor);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(textPos, drawColor, displayText);
        if (drawBoldDisplay && !string.IsNullOrWhiteSpace(displayText))
            drawList.AddText(textPos + new Vector2(1f, 0f), drawColor, displayText);

        bool showCaret = isInputEnabled && isActive && string.IsNullOrWhiteSpace(effectiveDisplayOverrideText);
        if (showCaret && ((int)(ImGui.GetTime() * 1.8f) % 2 == 0))
        {
            float caretX = textPos.X + textSize.X + 1f;
            float caretTop = min.Y + 4f;
            float caretBottom = max.Y - 4f;
            uint caretColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.95f));
            drawList.AddLine(new Vector2(caretX, caretTop), new Vector2(caretX, caretBottom), caretColor, 1.5f);
        }

        if (confirmedByEnter || confirmedByFocusLoss)
        {
            bool hadAnyText = !string.IsNullOrWhiteSpace(inputText);
            numericValue = ParseBankValue(inputText);
            inputText = FormatNumber(numericValue);
            hasUserEdited = hadAnyText && numericValue != BigInteger.Zero;
            afterCommit?.Invoke();
            SaveCurrentProfileValues();
        }
    }



    private void DrawSectionTitle(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Hex("#ffbb00"));
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private void DrawBoldColoredText(string text, Vector4 color)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        uint col = ImGui.ColorConvertFloat4ToU32(color);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddText(pos, col, text);
        drawList.AddText(pos + new Vector2(1f, 0f), col, text);

        Vector2 size = ImGui.CalcTextSize(text);
        ImGui.Dummy(new Vector2(size.X + 1f, size.Y));
    }

    private void NudgeCursorX(float amount)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + amount);
    }

    private void DrawReadOnlyNumericCell(
        int labelColumn,
        int valueColumn,
        string label,
        string inputId,
        string displayValue,
        float valueWidth,
        Vector4? valueColor = null)
    {
        ImGui.TableSetColumnIndex(labelColumn);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);

        ImGui.TableSetColumnIndex(valueColumn);
        ImGui.SetNextItemWidth(valueWidth);

        string valueCopy = displayValue;

        if (valueColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, valueColor.Value);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, valueColor.Value);
        }

        ImGui.BeginDisabled();
        ImGui.InputText(inputId, ref valueCopy, 128, ImGuiInputTextFlags.ReadOnly);
        ImGui.EndDisabled();

        if (valueColor.HasValue)
            ImGui.PopStyleColor(2);
    }


    private void DrawColoredCell(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private void DrawBoldColoredCell(string text, Vector4 color)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        uint col = ImGui.ColorConvertFloat4ToU32(color);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddText(pos, col, text);
        drawList.AddText(pos + new Vector2(1f, 0f), col, text);

        Vector2 size = ImGui.CalcTextSize(text);
        ImGui.Dummy(new Vector2(size.X + 1f, size.Y));
    }

    private void DrawProfitsLossesSummary()
    {
        var history = GetCurrentHistory();

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

        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(" ♯ All time ");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, ProfitColor);
        ImGui.TextUnformatted("Profits");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted("/");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, LossColor);
        ImGui.TextUnformatted("Losses");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(": ");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, ProfitColor);
        ImGui.TextUnformatted($"↑{FormatNumber(totalProfits)}");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
        ImGui.TextUnformatted(" /");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, LossColor);
        ImGui.TextUnformatted($"↓{FormatNumber(totalLosses)}");
        ImGui.PopStyleColor();
    }

    private bool DrawStyledBoldButton(
        string text,
        string id,
        Vector2 size,
        Vector4 baseColor,
        Vector4? textColorOverride = null,
        Vector4? hoverColorOverride = null,
        Vector4? activeColorOverride = null,
        bool pulsatingGlow = false)
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

        if (pulsatingGlow)
        {
            float pulse = 0.55f + (0.45f * (float)((Math.Sin(ImGui.GetTime() * 4.5f) + 1.0) * 0.5));
            float glowAlpha = 0.10f + (0.14f * pulse);
            uint glowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, glowAlpha));

            Vector2[] glowOffsets =
            {
                new(-2f, 0f), new(2f, 0f), new(0f, -2f), new(0f, 2f),
                new(-1.5f, -1.5f), new(1.5f, -1.5f), new(-1.5f, 1.5f), new(1.5f, 1.5f)
            };

            foreach (var offset in glowOffsets)
                drawList.AddText(textPos + offset, glowColor, text);
        }

        uint drawColor = ImGui.ColorConvertFloat4ToU32(textColor);
        drawList.AddText(textPos, drawColor, text);
        drawList.AddText(textPos + new Vector2(1f, 0f), drawColor, text);

        ImGui.PopStyleColor(4);
        return pressed;
    }


    private bool DrawGradientStyledBoldButton(
        string text,
        string id,
        Vector2 size,
        Vector4 startColor,
        Vector4 endColor,
        float angleDegrees,
        float startPercent,
        float endPercent,
        Vector4? textColorOverride = null)
    {
        Vector2 actualSize = size;
        if (actualSize.X <= 0f)
            actualSize.X = ImGui.CalcTextSize(text).X + (ImGui.GetStyle().FramePadding.X * 2f);
        if (actualSize.Y <= 0f)
            actualSize.Y = ImGui.GetFrameHeight();

        bool pressed = ImGui.InvisibleButton($"##{id}", actualSize);
        bool hovered = ImGui.IsItemHovered();
        bool held = ImGui.IsItemActive();

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();

        const float rounding = 4f;

        Vector4 neutralBaseColor = held
            ? new Vector4(0.06f, 0.06f, 0.06f, 1f)
            : hovered
                ? new Vector4(0.10f, 0.10f, 0.10f, 1f)
                : new Vector4(0.08f, 0.08f, 0.08f, 1f);

        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(neutralBaseColor), rounding);

        Vector2 insetMin = min + new Vector2(1f, 1f);
        Vector2 insetMax = max - new Vector2(1f, 1f);
        DrawAngledGradientRect(drawList, insetMin, insetMax, startColor, endColor, angleDegrees, startPercent, endPercent);

        DrawVerticalOverlayGradient(
            drawList,
            insetMin,
            insetMax,
            new Vector4(1f, 1f, 1f, hovered ? 0.035f : 0.02f),
            new Vector4(1f, 1f, 1f, 0.00f),
            true);

        DrawVerticalOverlayGradient(
            drawList,
            insetMin,
            insetMax,
            new Vector4(0f, 0f, 0f, 0.00f),
            new Vector4(0f, 0f, 0f, held ? 0.10f : 0.06f),
            false);

        Vector4 textColor = textColorOverride ?? WhiteText;
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(
            min.X + ((max.X - min.X) - textSize.X) * 0.5f,
            min.Y + ((max.Y - min.Y) - textSize.Y) * 0.5f);

        uint shadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.32f));
        uint drawColor = ImGui.ColorConvertFloat4ToU32(textColor);
        drawList.AddText(textPos + new Vector2(0f, 1f), shadowColor, text);
        drawList.AddText(textPos, drawColor, text);
        drawList.AddText(textPos + new Vector2(1f, 0f), drawColor, text);

        return pressed;
    }

    private void DrawAngledGradientRect(
        ImDrawListPtr drawList,
        Vector2 min,
        Vector2 max,
        Vector4 startColor,
        Vector4 endColor,
        float angleDegrees,
        float startPercent,
        float endPercent)
    {
        float width = MathF.Max(1f, max.X - min.X);
        float height = MathF.Max(1f, max.Y - min.Y);

        float angleRadians = angleDegrees * (MathF.PI / 180f);
        Vector2 direction = Vector2.Normalize(new Vector2(MathF.Cos(angleRadians), MathF.Sin(angleRadians)));

        Vector2[] corners =
        {
            min,
            new Vector2(max.X, min.Y),
            max,
            new Vector2(min.X, max.Y)
        };

        float minProjection = float.MaxValue;
        float maxProjection = float.MinValue;
        foreach (var corner in corners)
        {
            float projection = Vector2.Dot(corner, direction);
            minProjection = MathF.Min(minProjection, projection);
            maxProjection = MathF.Max(maxProjection, projection);
        }

        float clampedStart = Math.Clamp(startPercent, 0f, 1f);
        float clampedEnd = Math.Clamp(endPercent, 0f, 1f);

        float lowPercent = MathF.Min(clampedStart, clampedEnd);
        float highPercent = MathF.Max(clampedStart, clampedEnd);

        float gradientStart = minProjection + ((maxProjection - minProjection) * lowPercent);
        float gradientEnd = minProjection + ((maxProjection - minProjection) * highPercent);

        int columns = 40;
        int rows = 12;
        float cellWidth = width / columns;
        float cellHeight = height / rows;

        Vector4 boostedStartColor = BoostRedSide(startColor);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector2 cellMin = new(min.X + (column * cellWidth), min.Y + (row * cellHeight));
                Vector2 cellMax = new(
                    column == columns - 1 ? max.X : cellMin.X + cellWidth + 0.5f,
                    row == rows - 1 ? max.Y : cellMin.Y + cellHeight + 0.5f);

                Vector2 center = new((cellMin.X + cellMax.X) * 0.5f, (cellMin.Y + cellMax.Y) * 0.5f);
                float projection = Vector2.Dot(center, direction);
                float t = (projection - gradientStart) / MathF.Max(gradientEnd - gradientStart, 0.001f);
                t = Math.Clamp(t, 0f, 1f);

                float biasedT = MathF.Pow(t, 1.65f);
                Vector4 color = LerpColor(boostedStartColor, endColor, biasedT);
                drawList.AddRectFilled(cellMin, cellMax, ImGui.ColorConvertFloat4ToU32(color));
            }
        }
    }

    private void DrawVerticalOverlayGradient(
        ImDrawListPtr drawList,
        Vector2 min,
        Vector2 max,
        Vector4 topColor,
        Vector4 bottomColor,
        bool strongerAtTop)
    {
        float height = MathF.Max(1f, max.Y - min.Y);
        int rows = 10;
        float rowHeight = height / rows;

        for (int row = 0; row < rows; row++)
        {
            float t0 = row / (float)rows;
            float t1 = (row + 1f) / rows;

            Vector4 color0 = LerpColor(topColor, bottomColor, t0);
            Vector4 color1 = LerpColor(topColor, bottomColor, t1);

            Vector2 rowMin = new(min.X, min.Y + (row * rowHeight));
            Vector2 rowMax = new(max.X, row == rows - 1 ? max.Y : rowMin.Y + rowHeight + 0.5f);

            uint fillColor = ImGui.ColorConvertFloat4ToU32(LerpColor(color0, color1, 0.5f));
            drawList.AddRectFilled(rowMin, rowMax, fillColor);
        }
    }

    private static Vector4 LerpColor(Vector4 a, Vector4 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector4(
            a.X + ((b.X - a.X) * t),
            a.Y + ((b.Y - a.Y) * t),
            a.Z + ((b.Z - a.Z) * t),
            a.W + ((b.W - a.W) * t));
    }

    private static Vector4 BoostRedSide(Vector4 color)
    {
        return new Vector4(
            Math.Clamp(color.X + 0.26f, 0f, 1f),
            Math.Clamp(color.Y - 0.07f, 0f, 1f),
            Math.Clamp(color.Z - 0.03f, 0f, 1f),
            color.W);
    }

    private static Vector4 Lighten(Vector4 color, float amount)
    {
        return new Vector4(
            Math.Clamp(color.X + amount, 0f, 1f),
            Math.Clamp(color.Y + amount, 0f, 1f),
            Math.Clamp(color.Z + amount, 0f, 1f),
            color.W);
    }

    private static Vector4 Darken(Vector4 color, float amount)
    {
        return new Vector4(
            Math.Clamp(color.X - amount, 0f, 1f),
            Math.Clamp(color.Y - amount, 0f, 1f),
            Math.Clamp(color.Z - amount, 0f, 1f),
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
        IEnumerable<HistoryEntry> history = GetCurrentHistory();

        if (!string.IsNullOrWhiteSpace(searchInput))
        {
            string needle = searchInput.Trim();
            history = history.Where(entry => HistoryEntryMatchesSearch(entry, needle));
        }

        return sortBy switch
        {
            "Ascending" => history.OrderBy(x => ParseTimestamp(x.Timestamp)).ToList(),
            "Results" => history.OrderByDescending(x => ParseSignedFormatted(x.Result)).ToList(),
            "Blackjack" when IsPlayerMode => history
                .OrderByDescending(x => x.Result.Contains("Blackjack", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => ParseTimestamp(x.Timestamp))
                .ToList(),
            "Tips" when IsDealerMode => history.OrderByDescending(x => ParseBankValue(x.Tips)).ToList(),
            _ => history.OrderByDescending(x => ParseTimestamp(x.Timestamp)).ToList(),
        };
    }

    private bool HistoryEntryMatchesSearch(HistoryEntry entry, string searchText)
    {
        return ContainsInvariant(entry.House, searchText) ||
               ContainsInvariant(entry.Timestamp, searchText) ||
               ContainsInvariant(entry.StartingBank, searchText) ||
               ContainsInvariant(entry.FinalBank, searchText) ||
               ContainsInvariant(entry.Tips, searchText) ||
               ContainsInvariant(entry.Result, searchText);
    }

    private static bool ContainsInvariant(string? source, string searchText)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        return source.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UndoMostRecentHistory()
    {
        var history = GetCurrentHistory();
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

        resultsLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.ResultsLabel)
            ? "Today Profit/Loss:"
            : Plugin.Configuration.ResultsLabel;

        startingLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.StartingLabel)
            ? "Starting Bank:"
            : Plugin.Configuration.StartingLabel;

        finalLabel = string.IsNullOrWhiteSpace(Plugin.Configuration.FinalLabel)
            ? "Final Bank:"
            : Plugin.Configuration.FinalLabel;

        if (IsPlayerMode)
        {
            startingBankInput = profile.PlayerStartingBankInput ?? string.Empty;
            finalBankInput = profile.PlayerCurrentBankInput ?? string.Empty;
            betInput = profile.PlayerBetInput ?? string.Empty;
            houseInput = profile.PlayerHouseInput ?? string.Empty;
            trackedDealerInput = profile.PlayerTrackedDealerInput ?? string.Empty;
            playerAutoTrackEnabled = profile.PlayerAutoTrackEnabled;
            tipsInput = string.Empty;

            startingBankValue = ParseBankValue(startingBankInput);
            finalBankValue = ParseBankValue(finalBankInput);
            betValue = ParseBankValue(betInput);
            tipsValue = BigInteger.Zero;

            if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(profile.PlayerCurrentBankInput))
            {
                finalBankValue = startingBankValue;
            }

            startingBankInput = FormatNumber(startingBankValue);
            finalBankInput = FormatNumber(finalBankValue);
            betInput = FormatNumber(betValue);
            bankingInput = string.Empty;
            bankingValue = BigInteger.Zero;
            betInputHasUserEdited = ParseBankValue(profile.PlayerBetInput ?? string.Empty) > BigInteger.Zero;
            bankingInputHasUserEdited = false;
            return;
        }

        startingBankInput = profile.StartingBankInput ?? string.Empty;
        finalBankInput = profile.FinalBankInput ?? string.Empty;
        tipsInput = profile.TipsInput ?? string.Empty;
        houseInput = profile.HouseInput ?? string.Empty;
        betInput = string.Empty;
        trackedDealerInput = profile.PlayerTrackedDealerInput ?? string.Empty;
        playerAutoTrackEnabled = profile.PlayerAutoTrackEnabled;

        startingBankValue = ParseBankValue(startingBankInput);
        finalBankValue = ParseBankValue(finalBankInput);
        tipsValue = ParseBankValue(tipsInput);
        betValue = BigInteger.Zero;
        bankingValue = BigInteger.Zero;

        startingBankInput = FormatNumber(startingBankValue);
        finalBankInput = FormatNumber(finalBankValue);
        tipsInput = FormatNumber(tipsValue);
        bankingInput = string.Empty;
        betInputHasUserEdited = false;
        bankingInputHasUserEdited = false;
    }


    private void SaveCurrentProfileValues()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();

        if (IsPlayerMode)
        {
            profile.PlayerStartingBankInput = startingBankInput;
            profile.PlayerCurrentBankInput = finalBankInput;
            profile.PlayerBetInput = betInput;
            profile.PlayerHouseInput = houseInput;
            profile.PlayerTrackedDealerInput = trackedDealerInput;
            profile.PlayerAutoTrackEnabled = playerAutoTrackEnabled;
        }
        else
        {
            profile.StartingBankInput = startingBankInput;
            profile.FinalBankInput = finalBankInput;
            profile.TipsInput = tipsInput;
            profile.HouseInput = houseInput;
        }

        Plugin.Configuration.ResultsLabel = resultsLabel;
        Plugin.Configuration.StartingLabel = startingLabel;
        Plugin.Configuration.FinalLabel = finalLabel;

        Plugin.Configuration.Save();
    }


    private void ClearCurrentInputs()
    {
        startingBankInput = string.Empty;
        finalBankInput = string.Empty;
        houseInput = string.Empty;

        if (IsPlayerMode)
        {
            betInput = string.Empty;
            tipsInput = string.Empty;
            bankingInput = string.Empty;

            startingBankValue = BigInteger.Zero;
            finalBankValue = BigInteger.Zero;
            betValue = BigInteger.Zero;
            tipsValue = BigInteger.Zero;
            bankingValue = BigInteger.Zero;
            betInputHasUserEdited = false;
            bankingInputHasUserEdited = false;
        }
        else
        {
            tipsInput = string.Empty;
            betInput = string.Empty;
            bankingInput = string.Empty;

            startingBankValue = BigInteger.Zero;
            finalBankValue = BigInteger.Zero;
            tipsValue = BigInteger.Zero;
            betValue = BigInteger.Zero;
            bankingValue = BigInteger.Zero;
            betInputHasUserEdited = false;
            bankingInputHasUserEdited = false;
        }

        currentBetDisplayOverrideUntilUtc = DateTime.MinValue;
        currentBetDisplayOverrideText = string.Empty;
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

        var newProfile = new ProfileData { Name = trimmed };
        newProfile.EnsureInitialized();

        Plugin.Configuration.Profiles.Add(newProfile);
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
        var history = GetCurrentHistory();

        history.Add(new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = IsPlayerMode ? string.Empty : FormatNumber(tipsValue),
            Result = GetCurrentResultText()
        });

        if (history.Count > 200)
            history.RemoveAt(0);

        Plugin.Configuration.Save();
    }

    private List<HistoryEntry> GetCurrentHistory()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        return IsPlayerMode ? profile.PlayerHistory : profile.DealerHistory;
    }

    private void SetMode(string mode)
    {
        bool switchingToPlayer = string.Equals(mode, "Player", StringComparison.OrdinalIgnoreCase);
        bool alreadyInRequestedMode = switchingToPlayer ? IsPlayerMode : IsDealerMode;

        if (alreadyInRequestedMode)
            return;

        SaveCurrentProfileValues();

        Plugin.Configuration.ActiveMode = switchingToPlayer ? "Player" : "Dealer";
        Plugin.Configuration.Save();

        if (switchingToPlayer)
            sortBy = sortBy == "Tips" ? "Most recent" : sortBy;

        formulaHelpPinned = false;
        hasPinnedTooltipPosition = false;

        LoadFromConfiguration();
    }

    private void OnStartingBankCommitted()
    {
        if (!IsPlayerMode)
            return;

        finalBankValue = startingBankValue;
        finalBankInput = FormatNumber(finalBankValue);
    }


    private void ApplyBetResult(bool isWin)
    {
        betValue = ParseBankValue(betInput);
        betInput = FormatNumber(betValue);
        betInputHasUserEdited = !string.IsNullOrWhiteSpace(betInput);

        if (betValue <= BigInteger.Zero)
        {
            SaveCurrentProfileValues();
            return;
        }

        if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(finalBankInput))
            finalBankValue = startingBankValue;

        finalBankValue = isWin
            ? finalBankValue + betValue
            : BigInteger.Max(BigInteger.Zero, finalBankValue - betValue);

        finalBankInput = FormatNumber(finalBankValue);
        SaveCurrentProfileValues();
    }

    private void IncrementBankingValue(BigInteger amount)
    {
        bankingValue = ParseBankValue(bankingInput) + amount;
        bankingInput = FormatNumber(bankingValue);
        bankingInputHasUserEdited = true;
    }

    private void ApplyBankingAdjustment()
    {
        bankingValue = ParseBankValue(bankingInput);
        bankingInput = FormatNumber(bankingValue);
        bankingInputHasUserEdited = !string.IsNullOrWhiteSpace(bankingInput);

        if (bankingValue <= BigInteger.Zero)
        {
            SaveCurrentProfileValues();
            return;
        }

        if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(finalBankInput))
            finalBankValue = startingBankValue;

        finalBankValue += bankingValue;
        finalBankInput = FormatNumber(finalBankValue);

        AddBankingHistoryEntry(bankingValue);

        bankingInput = string.Empty;
        bankingValue = BigInteger.Zero;
        bankingInputHasUserEdited = false;
        SaveCurrentProfileValues();
    }

    private void AddBankingHistoryEntry(BigInteger amount)
    {
        var history = GetCurrentHistory();

        history.Add(new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatSignedResult(amount),
            Tips = string.Empty,
            Result = "+Banking"
        });

        if (history.Count > 200)
            history.RemoveAt(0);

        Plugin.Configuration.Save();
    }



    private void TrackDealerFromCurrentTarget()
    {
        var target = Plugin.TargetManager.Target;
        string targetName = target?.Name.TextValue?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(targetName))
        {
            trackDealerButtonDisabledUntilUtc = DateTime.UtcNow.AddSeconds(3);
            return;
        }

        trackedDealerInput = StripWorldSuffix(targetName);
        SaveCurrentProfileValues();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!IsAutoTrackingActive)
            return;

        if (type != XivChatType.Party && type != XivChatType.CrossParty)
            return;

        string trackedDealer = NormalizeLooseText(trackedDealerInput);
        if (string.IsNullOrWhiteSpace(trackedDealer))
            return;

        string senderName = NormalizeLooseText(StripWorldSuffix(sender.TextValue ?? string.Empty));
        if (!NamesRoughlyMatch(senderName, trackedDealer))
            return;

        string localPlayerName = NormalizeLooseText(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(localPlayerName))
            return;

        string messageText = message.TextValue?.Replace('\n', ' ').Replace('\r', ' ').Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(messageText))
            return;

        int winCount = 0;
        int lossCount = 0;
        int pushCount = 0;

        foreach (Match match in TrackResultRegex.Matches(messageText))
        {
            string label = match.Groups["label"].Value;
            string namesSection = match.Groups["names"].Value;
            int matchCount = CountPlayerMentions(namesSection, localPlayerName);

            if (matchCount <= 0)
                continue;

            if (LabelMatches(label, WinTrackLabels))
            {
                winCount += matchCount;
                continue;
            }

            if (LabelMatches(label, LossTrackLabels))
            {
                lossCount += matchCount;
                continue;
            }

            if (LabelMatches(label, PushTrackLabels))
                pushCount += matchCount;
        }

        if (winCount == 0 && lossCount == 0 && pushCount == 0)
            return;

        ApplyTrackedChatOutcome(winCount, lossCount, pushCount);
    }

    private void ApplyTrackedChatOutcome(int winCount, int lossCount, int pushCount)
    {
        betValue = ParseBankValue(betInput);
        betInput = FormatNumber(betValue);

        if (betValue <= BigInteger.Zero)
        {
            SaveCurrentProfileValues();
            return;
        }

        if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(finalBankInput))
            finalBankValue = startingBankValue;

        BigInteger oldCurrentBankValue = finalBankValue;
        int netCount = winCount - lossCount;
        bool isPurePush = pushCount > 0 && winCount == 0 && lossCount == 0;
        bool isBalancedPush = netCount == 0 && (winCount > 0 || lossCount > 0);
        bool isPushed = isPurePush || isBalancedPush;

        if (netCount > 0)
            finalBankValue += betValue * netCount;
        else if (netCount < 0)
            finalBankValue = BigInteger.Max(BigInteger.Zero, finalBankValue - (betValue * BigInteger.Abs(netCount)));

        finalBankInput = FormatNumber(finalBankValue);

        if (isPushed)
        {
            AddTrackedHistoryEntry("Pushed");
            SetTemporaryCurrentBetDisplay("Bet Pushed!", ProfitColor, 5);
            SaveCurrentProfileValues();
            return;
        }

        BigInteger trackedResultValue = finalBankValue - oldCurrentBankValue;
        AddTrackedHistoryEntry(FormatSignedResult(trackedResultValue));

        if (trackedResultValue > BigInteger.Zero)
            SetTemporaryCurrentBetDisplay("You won!", ProfitColor, 5);
        else if (trackedResultValue < BigInteger.Zero)
            SetTemporaryCurrentBetDisplay("You Lost!", LossColor, 5);

        SaveCurrentProfileValues();
    }

    private void AddTrackedHistoryEntry(string resultText)
    {
        var history = GetCurrentHistory();

        history.Add(new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = string.Empty,
            Result = resultText
        });

        if (history.Count > 200)
            history.RemoveAt(0);

        Plugin.Configuration.Save();
    }

    private void SetTemporaryCurrentBetDisplay(string text, Vector4 color, int seconds)
    {
        currentBetDisplayOverrideText = text;
        currentBetDisplayOverrideColor = color;
        currentBetDisplayOverrideUntilUtc = DateTime.UtcNow.AddSeconds(seconds);
    }

    private void GetCurrentBetDisplay(out string? displayText, out Vector4? displayColor)
    {
        if (DateTime.UtcNow < currentBetDisplayOverrideUntilUtc && !string.IsNullOrWhiteSpace(currentBetDisplayOverrideText))
        {
            displayText = currentBetDisplayOverrideText;
            displayColor = currentBetDisplayOverrideColor;
            return;
        }

        displayText = null;
        displayColor = null;
    }

    private static Regex BuildTrackResultRegex()
    {
        var labels = WinTrackLabels
            .Concat(LossTrackLabels)
            .Concat(PushTrackLabels)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Length)
            .Select(Regex.Escape);

        string labelPattern = string.Join("|", labels);
        string pattern = $@"(?<label>{labelPattern})\s*:\s*(?<names>.*?)(?=(?:{labelPattern})\s*:|$)";

        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static bool LabelMatches(string label, IEnumerable<string> labels)
    {
        return labels.Any(x => string.Equals(x, label, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountPlayerMentions(string namesSection, string normalizedPlayerName)
    {
        if (string.IsNullOrWhiteSpace(namesSection) || string.IsNullOrWhiteSpace(normalizedPlayerName))
            return 0;

        string normalizedSection = NormalizeLooseText(namesSection);
        if (string.IsNullOrWhiteSpace(normalizedSection))
            return 0;

        int count = 0;
        int index = 0;

        while (true)
        {
            index = normalizedSection.IndexOf(normalizedPlayerName, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            bool validLeft = index == 0 || normalizedSection[index - 1] == ' ';
            int endIndex = index + normalizedPlayerName.Length;
            bool validRight = endIndex >= normalizedSection.Length || normalizedSection[endIndex] == ' ';

            if (validLeft && validRight)
                count++;

            index = endIndex;
        }

        return count;
    }

    private void GetTrackingStatusDisplay(out string statusText, out Vector4 statusColor)
    {
        if (IsAutoTrackingActive)
        {
            statusText = "ON";
            statusColor = ProfitColor;
            return;
        }

        statusText = "OFF";
        statusColor = LossColor;
    }


    private static bool NamesRoughlyMatch(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase) ||
               left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
               right.Contains(left, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLooseText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var builder = new StringBuilder(input.Length);
        bool previousWasSpace = false;

        foreach (char c in StripWorldSuffix(input))
        {
            if (char.IsLetterOrDigit(c) || c == '\'' || c == '-')
            {
                builder.Append(c);
                previousWasSpace = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || c == ',' || c == '.' || c == ':' || c == ';' || c == '|' || c == '/')
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }
            }
        }

        return builder.ToString().Trim();
    }

    private static string FormatSignedResult(BigInteger value)
    {
        if (value > BigInteger.Zero)
            return $"+{FormatNumber(value)}";

        if (value < BigInteger.Zero)
            return $"-{FormatNumber(BigInteger.Abs(value))}";

        return "0";
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
        var value = IsPlayerMode
            ? finalBankValue - startingBankValue
            : finalBankValue - startingBankValue - tipsValue;

        if (value > BigInteger.Zero)
            return $"+ {FormatNumber(value)}";

        if (value < BigInteger.Zero)
            return $"- {FormatNumber(BigInteger.Abs(value))}";

        return "0";
    }

    private void AlignTopRightControls(float totalWidth)
    {
        float available = ImGui.GetContentRegionAvail().X;
        if (available <= totalWidth)
            return;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (available - totalWidth));
    }

    private static string StripWorldSuffix(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string trimmed = input.Trim();
        int atIndex = trimmed.IndexOf('@');
        if (atIndex >= 0)
            trimmed = trimmed[..atIndex];

        return trimmed.Trim();
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

    private static Vector4 HexWithAlpha(string hex, float alpha)
    {
        var color = Hex(hex);
        return new Vector4(color.X, color.Y, color.Z, Math.Clamp(alpha, 0f, 1f));
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
