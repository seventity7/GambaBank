using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    private BigInteger lastPlayerBankChangeValue;

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

    private int latencyPendingPlayerTotal;
    private DateTime latencyPendingUntilUtc = DateTime.MinValue;

    private enum TrackerIndicatorState
    {
        Off,
        On,
        Detected,
        Done,
        Error
    }

    private TrackerIndicatorState trackerWinsState = TrackerIndicatorState.Off;
    private TrackerIndicatorState trackerLossesState = TrackerIndicatorState.Off;
    private TrackerIndicatorState trackerBlackjacksState = TrackerIndicatorState.Off;
    private TrackerIndicatorState trackerDoubleDownsState = TrackerIndicatorState.Off;
    private string dealerHouseFilter = "All";
    private string dealerResultFilter = "All";
    private string dealerDateFilter = "All";
    private DateTime dealerExportStatusUntilUtc = DateTime.MinValue;
    private string dealerExportStatusText = string.Empty;
    private string dealerExportDirectoryPath = string.Empty;
    private string dealerExportFileName = string.Empty;
    private string dealerExportFilePath = string.Empty;
    private bool dealerExportFailed;
    private bool showDealerShiftSummaryWindow;
    private bool showDealerHistoryDetailWindow;
    private HistoryEntry? selectedDealerHistoryEntry;
    private string selectedDealerHistoryDetailId = string.Empty;
    private bool showHistoryExportDirectoryPopup;
    private string historyExportDirectoryInput = string.Empty;
    private readonly HashSet<string> collapsedHistoryDateGroups = new(StringComparer.OrdinalIgnoreCase);

    private DateTime doubleDownPromptUntilUtc = DateTime.MinValue;
    private DateTime doubleDownPendingUntilUtc = DateTime.MinValue;
    private DateTime blackjackPendingUntilUtc = DateTime.MinValue;
    private DateTime doubleDownDoneUntilUtc = DateTime.MinValue;
    private DateTime blackjackDoneUntilUtc = DateTime.MinValue;
    private DateTime autoTrackRequireDealerUntilUtc = DateTime.MinValue;

    // Add new chat keywords here if your dealer uses different result words.
    private static readonly string[] WinTrackLabels = { "Win", "Won", "Winners", "Winner", "Wins" };
    private static readonly string[] LossTrackLabels = { "Loss", "Lose", "Lost", "Busted", "Losses", "Loses", "Losts", "Busts", "Busteds" };
    private static readonly string[] PushTrackLabels = { "Push", "Pushed", "Draw", "Tie" };

    // Add more dealer-message triggers for the FIRST blackjack detection stage here.
    // The plugin arms blackjack pending when a tracked dealer message mentions the local player
    // and contains any of these phrases, then it waits for the next dealer outcome message.
    private static readonly string[] BlackjackStageOneKeywords =
    {
        "blackjack",
        "natural blackjack",
        "nat blackjack",
        "natty blackjack",
        "natural bj",
        "nat bj",
        "natty bj",
        "got a natty",
        "got natty",
        "got a natural",
        "got natural",
        "natty",
        "natural",
        "natty!",
        "nat!",
        "natt!",
        "blackjack <se.",
        "bj",
        "blackjack!"
    };

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
    private static readonly Vector4 DoubleDownTextColor = Hex("#ff42ef");
    private static readonly Vector4 BlackText = new(0.0f, 0.0f, 0.0f, 1.0f);

    private static readonly Vector4 CopyButtonColor = Hex("#005210");
    private static readonly Vector4 UtilityButtonColor = Hex("#005232");
    private static readonly Vector4 DangerButtonColor = Hex("#520000");
    private static readonly Vector4 WhiteText = new(1.0f, 1.0f, 1.0f, 1.0f);

    private static readonly Vector4 DealerButtonColor = Hex("#DA9E00");
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
    private static readonly Vector4 DealerQuickTip100Color = WithAlpha(Hex("#d463cc"), 0.85f);
    private static readonly Vector4 DealerQuickTip500Color = WithAlpha(Hex("#a054d6"), 0.85f);
    private static readonly Vector4 DealerQuickTip1MColor = WithAlpha(Hex("#545ad6"), 0.85f);
    private static readonly Vector4 DealerBreakColor = Hex("#292f56");
    private static readonly Vector4 DealerResumeColor = Hex("#741a53");
    private static readonly Vector4 DealerExportCsvColor = Hex("#9EB60A");

    private bool IsPlayerMode => string.Equals(Plugin.Configuration.ActiveMode, "Player", StringComparison.OrdinalIgnoreCase);
    private bool IsDealerMode => !IsPlayerMode;
    private bool IsTrackDealerButtonDisabled => DateTime.UtcNow < trackDealerButtonDisabledUntilUtc;
    private bool IsAutoTrackingActive => IsPlayerMode && playerAutoTrackEnabled && !string.IsNullOrWhiteSpace(trackedDealerInput);
    private bool IsPartyChatMonitoringActive => IsAutoTrackingActive;

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

        _ = DebugHub.Snapshot();
        AddDebugLog("INIT", "Background debug capture armed.");
        Plugin.ChatGui.ChatMessage += OnChatMessage;
        AddDebugLog("INIT", "MainWindow initialized.");
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

        if (IsDealerMode)
        {
            TryAutoDailyDealerBackup();
            DrawDealerShiftSummaryWindow();
            DrawDealerHistoryDetailWindow();
        }

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

        const float settingsButtonWidth = 72f;
        const float helpButtonWidth = 58f;
        const float modeButtonWidth = 70f;
        const float questionButtonWidth = 22f;
        const float dividerVisualWidth = 8f;

        ImGui.SameLine();
        AlignTopRightControls(
            settingsButtonWidth +
            dividerVisualWidth +
            helpButtonWidth +
            dividerVisualWidth +
            modeButtonWidth +
            modeButtonWidth +
            questionButtonWidth +
            (ImGui.GetStyle().ItemSpacing.X * 6f));

        if (DrawStyledBoldButton("Settings", "OpenSettingsWindowButton", new Vector2(settingsButtonWidth, 20f), Hex("#9784e8"), WhiteText))
            Plugin.OpenConfigUi();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("|");

        ImGui.SameLine();
        if (DrawStyledBoldButton("Help", "OpenHelpWindowButton", new Vector2(helpButtonWidth, 20f), Hex("#2aa163"), WhiteText))
            Plugin.OpenHelpUi();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("|");

        ImGui.SameLine();
        if (DrawStyledBoldButton(
                "Dealer",
                "DealerModeButton",
                new Vector2(modeButtonWidth, 20f),
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
                new Vector2(modeButtonWidth, 20f),
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
        DrawDealerSessionStatusSection();
        ImGui.Spacing();
        DrawDealerCheckpointButtons();
        ImGui.Spacing();

        if (!ImGui.BeginTable("##DealerBankFieldsTable", 5, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 150f);

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

        DrawReadOnlyNumericCell(2, 3, "Results:", "##Results", GetCurrentResultText(), 145f);

        ImGui.TableSetColumnIndex(4);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, ImGui.GetStyle().ItemSpacing.Y));
        if (DrawStyledBoldButton("Save", "SaveToHistoryButton", new Vector2(70f, 0f), CopyButtonColor))
            AddHistoryEntry();
        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear", "DealerClearButton", new Vector2(70f, 0f), LossButtonColor, WhiteText))
            ClearCurrentInputs();
        ImGui.PopStyleVar();

        ImGui.TableNextRow();

        DrawEditableNumericCell(
            0,
            1,
            "Final Bank:",
            "##FinalBank",
            ref finalBankInput,
            ref finalBankValue,
            145f);

        DrawEditableNumericCell(
            2,
            3,
            "Tips:",
            "##Tips",
            ref tipsInput,
            ref tipsValue,
            145f);

        ImGui.TableNextRow();

        DrawEditableHouseSuggestCell(0, 1, "House:", "##HouseInput", ref houseInput, 145f);

        ImGui.TableSetColumnIndex(2);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Quick tips:");

        ImGui.TableSetColumnIndex(3);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2f, ImGui.GetStyle().ItemSpacing.Y));
        const float quickTipButtonWidth = 47f;
        if (DrawStyledBoldButton("+100K", "DealerTips100KButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip100Color, WhiteText))
            IncrementDealerTips(100_000);
        ImGui.SameLine();
        if (DrawStyledBoldButton("+500K", "DealerTips500KButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip500Color, WhiteText))
            IncrementDealerTips(500_000);
        ImGui.SameLine();
        if (DrawStyledBoldButton("+1M", "DealerTips1MButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip1MColor, WhiteText))
            IncrementDealerTips(1_000_000);
        ImGui.PopStyleVar();

        ImGui.EndTable();
    }

    private void DrawPlayerBankFields()
    {
        DrawSectionTitle("☆ Bank Tracking");
        ImGui.Dummy(new Vector2(0f, 2f));

        if (ImGui.BeginTable("##PlayerTopBankFieldsTable", 3, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("PlayerLeftColumn", ImGuiTableColumnFlags.WidthFixed, 250f);
            ImGui.TableSetupColumn("PlayerCenterColumn", ImGuiTableColumnFlags.WidthFixed, 250f);
            ImGui.TableSetupColumn("PlayerActionColumn", ImGuiTableColumnFlags.WidthFixed, 96f);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            DrawPlayerLeftFields();

            ImGui.TableSetColumnIndex(1);
            DrawPlayerCenterFields();

            ImGui.TableSetColumnIndex(2);
            DrawPlayerActionButtons();

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






    private void DrawPlayerTrackingLayout()
    {
        const float labelWidth = 96f;
        const float betFieldWidth = 140f;
        const float middleSpacerWidth = 16f;
        const float clonedBlockWidth = 145f;
        const float separatorColumnWidth = 14f;
        const float dealerBlockWidth = 145f;
        const float trackerStatusColumnWidth = 470f;

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

        if (!ImGui.BeginTable("##PlayerTrackingUnifiedLayout", 8, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("TrackingLabelColumn", ImGuiTableColumnFlags.WidthFixed, labelWidth);
        ImGui.TableSetupColumn("TrackingOriginalColumn", ImGuiTableColumnFlags.WidthFixed, betFieldWidth);
        ImGui.TableSetupColumn("TrackingSpacerColumn", ImGuiTableColumnFlags.WidthFixed, middleSpacerWidth);
        ImGui.TableSetupColumn("TrackingCloneColumn", ImGuiTableColumnFlags.WidthFixed, clonedBlockWidth);
        ImGui.TableSetupColumn("TrackingSeparatorColumn", ImGuiTableColumnFlags.WidthFixed, separatorColumnWidth);
        ImGui.TableSetupColumn("TrackingDealerColumn", ImGuiTableColumnFlags.WidthFixed, dealerBlockWidth);
        ImGui.TableSetupColumn("TrackingStatusSeparatorColumn", ImGuiTableColumnFlags.WidthFixed, separatorColumnWidth);
        ImGui.TableSetupColumn("TrackingStatusColumn", ImGuiTableColumnFlags.WidthFixed, trackerStatusColumnWidth);

        RefreshTrackerIndicatorStates();
        UpdateTrackerTransientStates();

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

        ImGui.TableSetColumnIndex(3);
        DrawCenteredNumericInputField(
            "##ClonedBetValue",
            ref betInput,
            ref betValue,
            clonedBlockWidth,
            ref betInputHasUserEdited,
            false,
            null,
            currentBetDisplayText,
            currentBetDisplayColor,
            0f,
            false,
            false);

        ImGui.TableSetColumnIndex(4);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(5);
        if (IsTrackDealerButtonDisabled)
            ImGui.BeginDisabled();

        bool hasTrackedDealer = !string.IsNullOrWhiteSpace(trackedDealerInput);
        string trackButtonText = IsTrackDealerButtonDisabled
            ? "Target not found"
            : hasTrackedDealer ? "● Tracking Dealer:" : "○ Track Dealer";
        Vector4 trackButtonTextColor = WhiteText;

        if (DrawStyledBoldButton(trackButtonText, "TrackDealerButton", new Vector2(dealerBlockWidth, 0f), UtilityButtonColor, trackButtonTextColor) && !IsTrackDealerButtonDisabled)
        {
            AddDebugLog("TRACK", "Track Dealer button pressed.");
            TrackDealerFromCurrentTarget();
        }

        if (IsTrackDealerButtonDisabled)
            ImGui.EndDisabled();

        ImGui.TableSetColumnIndex(6);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(7);
        DrawTrackerStatusTitle("Tracker Status:", trackerStatusColumnWidth);

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
        {
            AddDebugLog("BET", "Manual WIN button pressed.");
            ApplyBetResult(true);
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("↓ LOSS", "LossBetButton", new Vector2(originalButtonWidth, 0f), LossButtonColor, WhiteText))
        {
            AddDebugLog("BET", "Manual LOSS button pressed.");
            ApplyBetResult(false);
        }

        ImGui.TableSetColumnIndex(2);
        ImGui.Dummy(Vector2.Zero);

        ImGui.TableSetColumnIndex(3);
        DrawMultiplierDropdownButton("Natbj", ref natbjMultiplierIndex, new Vector2(clonedButtonWidth, 0f), WinButtonColor);

        ImGui.SameLine();
        DrawMultiplierDropdownButton("Dirtytbj", ref dirtytbjMultiplierIndex, new Vector2(clonedButtonWidth, 0f), LossButtonColor);

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

        ImGui.TableSetColumnIndex(6);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(7);
        DrawTrackerStatusLineThree(
            "Wins:",
            ProfitColor,
            trackerWinsState,
            "Losses:",
            LossColor,
            trackerLossesState,
            "Dealer:",
            Hex("#075B39"),
            GetDealerTrackingState(),
            trackerStatusColumnWidth);

        // Row 3
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.Dummy(Vector2.Zero);

        string autoTrackText = DateTime.UtcNow < autoTrackRequireDealerUntilUtc
            ? "Track Dealer First"
            : playerAutoTrackEnabled ? "● Auto Track" : "○ Auto Track";
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
            if (string.IsNullOrWhiteSpace(trackedDealerInput))
            {
                autoTrackRequireDealerUntilUtc = DateTime.UtcNow.AddSeconds(5);
                AddDebugLog("WARN", "Auto Track blocked because no dealer is currently tracked.");
            }
            else
            {
                playerAutoTrackEnabled = !playerAutoTrackEnabled;
                AddDebugLog("AUTO", $"Auto Track toggled => {(playerAutoTrackEnabled ? "ON" : "OFF")}. Party-only monitoring {(playerAutoTrackEnabled && !string.IsNullOrWhiteSpace(trackedDealerInput) ? "armed" : "inactive") }.");
                ResetTrackerIndicatorStates();
                SaveCurrentProfileValues();
            }
        }

        ImGui.TableSetColumnIndex(2);
        ImGui.Dummy(Vector2.Zero);

        ImGui.TableSetColumnIndex(3);
        if (DrawStyledBoldButton("NAT BJ", "NatBjActionButton", new Vector2(clonedButtonWidth, 0f), Hex("#292f56"), WhiteText))
        {
            AddDebugLog("BET", $"Manual NAT BJ button pressed with multiplier {BlackjackMultiplierLabels[Math.Clamp(natbjMultiplierIndex, 0, BlackjackMultiplierLabels.Length - 1)]}.");
            ApplyBlackjackResult(natbjMultiplierIndex);
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("DIRTY BJ", "DirtyBjActionButton", new Vector2(clonedButtonWidth, 0f), Hex("#741a53"), WhiteText))
        {
            AddDebugLog("BET", $"Manual DIRTY BJ button pressed with multiplier {BlackjackMultiplierLabels[Math.Clamp(dirtytbjMultiplierIndex, 0, BlackjackMultiplierLabels.Length - 1)]}.");
            ApplyBlackjackResult(dirtytbjMultiplierIndex);
        }

        ImGui.TableSetColumnIndex(4);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(5);
        if (DrawStyledBoldButton("Clear", "ClearTrackedDealerButton", new Vector2(dealerBlockWidth, 0f), LossButtonColor, WhiteText))
        {
            AddDebugLog("TRACK", $"Clearing tracked dealer '{trackedDealerInput}'. Party-only monitoring disabled until Track Dealer is set again.");
            trackedDealerInput = string.Empty;
            ResetTrackerIndicatorStates();
            SaveCurrentProfileValues();
        }

        ImGui.TableSetColumnIndex(6);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(7);
        DrawTrackerStatusLineTwoAligned(
            "Blackjacks:",
            Hex("#00c0c7"),
            trackerBlackjacksState,
            "Double-Downs:",
            Hex("#ff42ef"),
            trackerDoubleDownsState,
            trackerStatusColumnWidth);

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
        try
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
            BigInteger previousCurrentBank = finalBankValue;
            finalBankValue += payout;
            lastPlayerBankChangeValue = finalBankValue - previousCurrentBank;
            finalBankInput = FormatNumber(finalBankValue);

            RemoveMostRecentHistoryEntry();
            AddBlackjackHistoryEntry(payout);

            trackerBlackjacksState = TrackerIndicatorState.Done;
            blackjackDoneUntilUtc = DateTime.UtcNow.AddSeconds(5);
            SaveCurrentProfileValues();
        }
        catch
        {
            trackerBlackjacksState = TrackerIndicatorState.Error;
            throw;
        }
    }

    private void RemoveMostRecentHistoryEntry()
    {
        var history = GetCurrentHistory();
        if (history.Count == 0)
            return;

        var mostRecentEntry = history[^1];
        if (!string.IsNullOrWhiteSpace(mostRecentEntry.Result) &&
            mostRecentEntry.Result.Contains("Blackjack", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        history.RemoveAt(history.Count - 1);
    }

    private void AddBlackjackHistoryEntry(BigInteger payout)
    {
        var history = GetCurrentHistory();

        AddDebugLog("HISTORY", $"Adding blackjack history entry: +{FormatNumber(payout)} Blackjack.");
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

    private void DrawTrackerStatusTitle(string text, float availableWidth)
    {
        Vector2 start = ImGui.GetCursorScreenPos();
        Vector2 textSize = ImGui.CalcTextSize(text);
        float titleOffsetX = -185f;
        float x = start.X + MathF.Max(0f, (availableWidth - textSize.X) * 0.5f) + titleOffsetX;
        float y = start.Y + MathF.Max(0f, (ImGui.GetFrameHeight() - textSize.Y) * 0.5f);

        var drawList = ImGui.GetWindowDrawList();
        uint color = ImGui.ColorConvertFloat4ToU32(WhiteText);
        drawList.AddText(new Vector2(x, y), color, text);
        drawList.AddText(new Vector2(x + 1f, y), color, text);

        ImGui.Dummy(new Vector2(availableWidth, ImGui.GetFrameHeight()));
    }

    private void DrawTrackerStatusLine(
        string leftLabel,
        Vector4 leftLabelColor,
        TrackerIndicatorState leftState,
        string rightLabel,
        Vector4 rightLabelColor,
        TrackerIndicatorState rightState,
        float availableWidth)
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 14f;

        string leftValue = GetTrackerIndicatorText(leftState);
        string rightValue = GetTrackerIndicatorText(rightState);
        Vector4 leftValueColor = GetTrackerIndicatorColor(leftState);
        Vector4 rightValueColor = GetTrackerIndicatorColor(rightState);

        float sectionWidth = MathF.Max(165f, (availableWidth - sectionGap) * 0.5f);
        Vector2 start = ImGui.GetCursorScreenPos();
        float centerY = start.Y + (ImGui.GetFrameHeight() * 0.5f);
        float textBaseY = centerY - (ImGui.GetTextLineHeight() * 0.5f);

        DrawTrackerStatusSection(start.X, textBaseY, sectionWidth, leftLabel, leftLabelColor, leftValue, leftValueColor, highlightPadX, highlightPadY, segmentGap);
        DrawTrackerStatusSection(start.X + sectionWidth + sectionGap, textBaseY, sectionWidth, rightLabel, rightLabelColor, rightValue, rightValueColor, highlightPadX, highlightPadY, segmentGap);

        ImGui.Dummy(new Vector2(availableWidth, ImGui.GetFrameHeight()));
    }

    private void DrawTrackerStatusLineThree(
        string leftLabel,
        Vector4 leftLabelColor,
        TrackerIndicatorState leftState,
        string middleLabel,
        Vector4 middleLabelColor,
        TrackerIndicatorState middleState,
        string rightLabel,
        Vector4 rightLabelColor,
        TrackerIndicatorState rightState,
        float availableWidth)
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 12f;

        string leftValue = GetTrackerIndicatorText(leftState);
        string middleValue = GetTrackerIndicatorText(middleState);
        string rightValue = GetTrackerIndicatorText(rightState);

        Vector4 leftValueColor = GetTrackerIndicatorColor(leftState);
        Vector4 middleValueColor = GetTrackerIndicatorColor(middleState);
        Vector4 rightValueColor = GetTrackerIndicatorColor(rightState);

        float sectionWidth = MathF.Max(135f, (availableWidth - (sectionGap * 2f)) / 3f);
        Vector2 start = ImGui.GetCursorScreenPos();
        float centerY = start.Y + (ImGui.GetFrameHeight() * 0.5f);
        float textBaseY = centerY - (ImGui.GetTextLineHeight() * 0.5f);

        DrawTrackerStatusSection(start.X, textBaseY, sectionWidth, leftLabel, leftLabelColor, leftValue, leftValueColor, highlightPadX, highlightPadY, segmentGap);
        DrawTrackerStatusSection(start.X + sectionWidth + sectionGap, textBaseY, sectionWidth, middleLabel, middleLabelColor, middleValue, middleValueColor, highlightPadX, highlightPadY, segmentGap);
        DrawTrackerStatusSection(start.X + (sectionWidth + sectionGap) * 2f, textBaseY, sectionWidth, rightLabel, rightLabelColor, rightValue, rightValueColor, highlightPadX, highlightPadY, segmentGap);

        ImGui.Dummy(new Vector2(availableWidth, ImGui.GetFrameHeight()));
    }

    private void DrawTrackerStatusLineTwoAligned(
        string leftLabel,
        Vector4 leftLabelColor,
        TrackerIndicatorState leftState,
        string middleLabel,
        Vector4 middleLabelColor,
        TrackerIndicatorState middleState,
        float availableWidth)
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 12f;

        string leftValue = GetTrackerIndicatorText(leftState);
        string middleValue = GetTrackerIndicatorText(middleState);

        Vector4 leftValueColor = GetTrackerIndicatorColor(leftState);
        Vector4 middleValueColor = GetTrackerIndicatorColor(middleState);

        float sectionWidth = MathF.Max(135f, (availableWidth - (sectionGap * 2f)) / 3f);
        Vector2 start = ImGui.GetCursorScreenPos();
        float centerY = start.Y + (ImGui.GetFrameHeight() * 0.5f);
        float textBaseY = centerY - (ImGui.GetTextLineHeight() * 0.5f);

        DrawTrackerStatusSection(start.X, textBaseY, sectionWidth, leftLabel, leftLabelColor, leftValue, leftValueColor, highlightPadX, highlightPadY, segmentGap);
        DrawTrackerStatusSection(start.X + sectionWidth + sectionGap, textBaseY, sectionWidth, middleLabel, middleLabelColor, middleValue, middleValueColor, highlightPadX, highlightPadY, segmentGap);

        ImGui.Dummy(new Vector2(availableWidth, ImGui.GetFrameHeight()));
    }

    private void DrawTrackerStatusSection(
        float sectionX,
        float textBaseY,
        float sectionWidth,
        string label,
        Vector4 labelColor,
        string value,
        Vector4 valueColor,
        float highlightPadX,
        float highlightPadY,
        float segmentGap)
    {
        float x = sectionX;
        DrawHighlightedInlineTextSegment(label, labelColor, new Vector2(x, textBaseY), highlightPadX, highlightPadY);

        float labelWidth = ImGui.CalcTextSize(label).X + (highlightPadX * 2f);
        x += labelWidth + segmentGap;

        DrawTrackerIndicatorValue(value, valueColor, new Vector2(x, textBaseY));
    }


    private void DrawTrackerIndicatorValue(string text, Vector4 accentColor, Vector2 textPos)
    {
        var drawList = ImGui.GetWindowDrawList();
        uint neutralCol = ImGui.ColorConvertFloat4ToU32(WhiteText);
        uint accentCol = ImGui.ColorConvertFloat4ToU32(accentColor);

        const string openBracket = "[";
        const string closeBracket = "]";

        if (text.StartsWith("[✓]", StringComparison.Ordinal) || text.StartsWith("[X]", StringComparison.Ordinal))
        {
            string symbol = text.StartsWith("[✓]", StringComparison.Ordinal) ? "✓" : "X";
            string suffix = text.Length > 3 ? text.Substring(3) : string.Empty;

            Vector2 pos = textPos;
            drawList.AddText(pos, neutralCol, openBracket);
            pos.X += ImGui.CalcTextSize(openBracket).X;

            drawList.AddText(pos, accentCol, symbol);
            drawList.AddText(pos + new Vector2(1f, 0f), accentCol, symbol);
            pos.X += ImGui.CalcTextSize(symbol).X;

            drawList.AddText(pos, neutralCol, closeBracket);
            pos.X += ImGui.CalcTextSize(closeBracket).X;

            if (!string.IsNullOrEmpty(suffix))
            {
                drawList.AddText(pos, accentCol, suffix);
                drawList.AddText(pos + new Vector2(1f, 0f), accentCol, suffix);
            }

            return;
        }

        drawList.AddText(textPos, accentCol, text);
        drawList.AddText(textPos + new Vector2(1f, 0f), accentCol, text);
    }

    private void DrawHighlightedInlineTextSegment(string text, Vector4 textColor, Vector2 textPos, float padX, float padY)
    {
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 bgMin = new(textPos.X - padX, textPos.Y - padY);
        Vector2 bgMax = new(textPos.X + textSize.X + padX, textPos.Y + textSize.Y + padY);

        var drawList = ImGui.GetWindowDrawList();
        uint bgColor = ImGui.ColorConvertFloat4ToU32(BlackText);
        uint color = ImGui.ColorConvertFloat4ToU32(textColor);

        drawList.AddRectFilled(bgMin, bgMax, bgColor, 2f);
        drawList.AddText(textPos, color, text);
    }

    private void DrawInlineTextSegment(string text, Vector4 color, Vector2 textPos)
    {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(color), text);
    }

    private void DrawBoldInlineTextSegment(string text, Vector4 color, Vector2 textPos)
    {
        var drawList = ImGui.GetWindowDrawList();
        uint col = ImGui.ColorConvertFloat4ToU32(color);
        drawList.AddText(textPos, col, text);
        drawList.AddText(textPos + new Vector2(1f, 0f), col, text);
    }

    private void RefreshTrackerIndicatorStates()
    {
        TrackerIndicatorState activeState = IsAutoTrackingActive ? TrackerIndicatorState.On : TrackerIndicatorState.Off;

        if (trackerWinsState != TrackerIndicatorState.Error)
            trackerWinsState = activeState;
        if (trackerLossesState != TrackerIndicatorState.Error)
            trackerLossesState = activeState;
        if (trackerBlackjacksState != TrackerIndicatorState.Error && trackerBlackjacksState != TrackerIndicatorState.Detected && trackerBlackjacksState != TrackerIndicatorState.Done)
            trackerBlackjacksState = activeState;
        if (trackerDoubleDownsState != TrackerIndicatorState.Error && trackerDoubleDownsState != TrackerIndicatorState.Detected && trackerDoubleDownsState != TrackerIndicatorState.Done)
            trackerDoubleDownsState = activeState;
    }

    private void UpdateTrackerTransientStates()
    {
        TrackerIndicatorState activeState = IsAutoTrackingActive ? TrackerIndicatorState.On : TrackerIndicatorState.Off;
        DateTime now = DateTime.UtcNow;

        if (trackerDoubleDownsState == TrackerIndicatorState.Detected && now >= doubleDownPromptUntilUtc && now >= doubleDownPendingUntilUtc)
            trackerDoubleDownsState = activeState;
        else if (trackerDoubleDownsState == TrackerIndicatorState.Done && now >= doubleDownDoneUntilUtc)
            trackerDoubleDownsState = activeState;

        if (trackerBlackjacksState == TrackerIndicatorState.Detected && now >= blackjackPendingUntilUtc)
            trackerBlackjacksState = activeState;
        else if (trackerBlackjacksState == TrackerIndicatorState.Done && now >= blackjackDoneUntilUtc)
            trackerBlackjacksState = activeState;
    }

    private void ResetTrackerIndicatorStates()
    {
        trackerWinsState = TrackerIndicatorState.Off;
        trackerLossesState = TrackerIndicatorState.Off;
        trackerBlackjacksState = TrackerIndicatorState.Off;
        trackerDoubleDownsState = TrackerIndicatorState.Off;
        blackjackPendingUntilUtc = DateTime.MinValue;
        blackjackDoneUntilUtc = DateTime.MinValue;
        doubleDownDoneUntilUtc = DateTime.MinValue;
        ClearPendingLatencyTrack();
        RefreshTrackerIndicatorStates();
    }

    private static string GetTrackerIndicatorText(TrackerIndicatorState state)
    {
        return state switch
        {
            TrackerIndicatorState.On => "[✓]",
            TrackerIndicatorState.Detected => "[✓] Detected",
            TrackerIndicatorState.Done => "[✓] Done",
            TrackerIndicatorState.Error => "Erro",
            _ => "[X]"
        };
    }

    private static Vector4 GetTrackerIndicatorColor(TrackerIndicatorState state)
    {
        return state switch
        {
            TrackerIndicatorState.On => ProfitColor,
            TrackerIndicatorState.Detected => ProfitColor,
            TrackerIndicatorState.Done => ProfitColor,
            TrackerIndicatorState.Error => LossColor,
            _ => LossColor
        };
    }

    private TrackerIndicatorState GetDealerTrackingState()
    {
        return string.IsNullOrWhiteSpace(trackedDealerInput)
            ? TrackerIndicatorState.Off
            : TrackerIndicatorState.On;
    }

    private void AddDebugLog(string category, string message)
    {
        DebugHub.Add(category, message);
    }

    public string BuildDebugSnapshot()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Mode: {(IsPlayerMode ? "Player" : "Dealer")}");
        sb.AppendLine($"Profile: {Plugin.Configuration.ActiveProfileName}");
        sb.AppendLine($"Starting Bank: {FormatNumber(startingBankValue)}");
        sb.AppendLine($"Current/Final Bank: {FormatNumber(finalBankValue)}");
        sb.AppendLine($"Tips: {FormatNumber(tipsValue)}");
        sb.AppendLine($"Bet: {FormatNumber(betValue)}");
        sb.AppendLine($"Banking: {FormatNumber(bankingValue)}");
        sb.AppendLine($"Last Bank Delta: {FormatSignedResult(lastPlayerBankChangeValue)}");
        sb.AppendLine($"Auto Track Enabled: {playerAutoTrackEnabled}");
        sb.AppendLine($"Auto Track Active: {IsAutoTrackingActive}");
        sb.AppendLine($"Party Chat Monitoring Active: {IsPartyChatMonitoringActive}");
        sb.AppendLine($"Tracked Dealer: {trackedDealerInput}");
        sb.AppendLine($"Local Player: {Plugin.PlayerState.CharacterName ?? string.Empty}");
        sb.AppendLine($"Latency Pending Total: {latencyPendingPlayerTotal}");
        sb.AppendLine($"Latency Pending Until (UTC): {(latencyPendingUntilUtc == DateTime.MinValue ? "--" : latencyPendingUntilUtc.ToString("O"))}");
        sb.AppendLine($"Double-Down Prompt Until (UTC): {(doubleDownPromptUntilUtc == DateTime.MinValue ? "--" : doubleDownPromptUntilUtc.ToString("O"))}");
        sb.AppendLine($"Double-Down Pending Until (UTC): {(doubleDownPendingUntilUtc == DateTime.MinValue ? "--" : doubleDownPendingUntilUtc.ToString("O"))}");
        sb.AppendLine($"Blackjack Pending Until (UTC): {(blackjackPendingUntilUtc == DateTime.MinValue ? "--" : blackjackPendingUntilUtc.ToString("O"))}");
        sb.AppendLine($"Tracker States: W={GetTrackerIndicatorText(trackerWinsState)} L={GetTrackerIndicatorText(trackerLossesState)} BJ={GetTrackerIndicatorText(trackerBlackjacksState)} DD={GetTrackerIndicatorText(trackerDoubleDownsState)}");
        sb.AppendLine($"Dealer Filters: House={dealerHouseFilter} Result={dealerResultFilter} Date={dealerDateFilter}");
        sb.AppendLine($"Search: {searchInput}");
        sb.AppendLine($"Sort: {sortBy}");
        sb.AppendLine($"Tracked Dealer Button Disabled: {IsTrackDealerButtonDisabled}");
        sb.AppendLine($"History Count: {GetCurrentHistory().Count}");
        return sb.ToString();
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


    private string GetCurrentHistoryTitle() => IsPlayerMode ? "★ Player History" : "★ Dealer History";

    private string GetHistoryDateGroupKey(string dateText) => $"{(IsPlayerMode ? "Player" : "Dealer")}::{dateText}";

    private bool IsHistoryDateGroupCollapsed(string dateText) => collapsedHistoryDateGroups.Contains(GetHistoryDateGroupKey(dateText));

    private void ToggleHistoryDateGroup(string dateText)
    {
        string key = GetHistoryDateGroupKey(dateText);
        if (!collapsedHistoryDateGroups.Add(key))
            collapsedHistoryDateGroups.Remove(key);
    }

    private void DrawHistoryGroupHeader(string dateText, int columnCount)
    {
        bool collapsed = IsHistoryDateGroupCollapsed(dateText);
        string arrow = collapsed ? "▶" : "▼";

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
        if (ImGui.Selectable($"{arrow}  {dateText}", false, ImGuiSelectableFlags.SpanAllColumns))
            ToggleHistoryDateGroup(dateText);
        ImGui.PopStyleColor();

        for (int i = 1; i < columnCount; i++)
        {
            ImGui.TableSetColumnIndex(i);
            ImGui.TextUnformatted(string.Empty);
        }
    }

    private string GetHistoryExportDirectory()
    {
        string configured = Plugin.Configuration.HistoryExportDirectory?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return configured;

        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private void DrawHistoryExportDirectoryPopup(Vector2 buttonMin)
    {
        const string popupId = "##HistoryExportDirectoryPopupInline";
        const float popupWidth = 520f;

        ImGui.SetNextWindowPos(new Vector2(buttonMin.X - popupWidth - 6f, buttonMin.Y), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0f), ImGuiCond.Appearing);

        if (ImGui.BeginPopup(popupId))
        {
            ImGui.TextUnformatted("Choose where TXT/CSV history exports will be saved.");
            ImGui.Spacing();

            if (string.IsNullOrWhiteSpace(historyExportDirectoryInput))
                historyExportDirectoryInput = Plugin.Configuration.HistoryExportDirectory ?? string.Empty;

            ImGui.SetNextItemWidth(popupWidth - 32f);
            if (ImGui.InputText("##HistoryExportDirectoryInput", ref historyExportDirectoryInput, 512))
            {
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            if (ImGui.Button("Use Desktop"))
            {
                historyExportDirectoryInput = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button("Use Documents"))
            {
                historyExportDirectoryInput = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button("Use Backup Folder"))
            {
                historyExportDirectoryInput = Plugin.Configuration.DealerBackupDirectory ?? string.Empty;
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button("Clear"))
            {
                historyExportDirectoryInput = string.Empty;
                Plugin.Configuration.HistoryExportDirectory = string.Empty;
                Plugin.Configuration.Save();
            }

            ImGui.EndPopup();
        }
    }

    private void ExportCurrentHistory(bool asCsv)
    {
        ExportCurrentHistory(asCsv, false, IsPlayerMode ? "PlayerHistory" : "DealerHistory");
    }

    private void ExportCurrentHistory(bool asCsv, bool isAutoBackup, string filePrefix)
    {
        try
        {
            AddDebugLog("UI", $"Export requested | Format={(asCsv ? "CSV" : "TXT")} | AutoBackup={isAutoBackup} | Prefix='{filePrefix}'.");
            var entries = GetSortedHistory();
            string outputDirectory = isAutoBackup ? GetDealerBackupDirectory() : GetHistoryExportDirectory();
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string extension = asCsv ? "xls" : "txt";
            string safePrefix = string.IsNullOrWhiteSpace(filePrefix) ? (IsPlayerMode ? "PlayerHistory" : "DealerHistory") : filePrefix;
            string fileName = $"GambaBank_{safePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
            string filePath = Path.Combine(outputDirectory, fileName);

            if (asCsv)
                File.WriteAllText(filePath, BuildDealerSpreadsheetXml(entries), Encoding.UTF8);
            else
                File.WriteAllText(filePath, BuildDealerTxt(entries), Encoding.UTF8);

            dealerExportStatusText = isAutoBackup ? "Backup exported:" : "Exported:";
            dealerExportDirectoryPath = outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            dealerExportFileName = fileName;
            dealerExportFilePath = filePath;
            dealerExportFailed = false;
            dealerExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
            AddDebugLog("UI", $"Export completed | Format={(asCsv ? "CSV" : "TXT")} | Path='{filePath}'.");
        }
        catch (Exception ex)
        {
            AddDebugLog("ERR", $"Export failed | Format={(asCsv ? "CSV" : "TXT")} | Prefix='{filePrefix}' | {ex.GetType().Name}: {ex.Message}");
            dealerExportStatusText = isAutoBackup ? "Backup export failed" : "Export failed";
            dealerExportDirectoryPath = string.Empty;
            dealerExportFileName = string.Empty;
            dealerExportFilePath = string.Empty;
            dealerExportFailed = true;
            dealerExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
        }
    }

    private void DrawHistorySection()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Hex("#ffbb00"));
        ImGui.Text(GetCurrentHistoryTitle());
        ImGui.PopStyleColor();

        ImGui.SameLine();
        AlignTopRightControls(32f + 84f + 84f + (ImGui.GetStyle().ItemSpacing.X * 3f));

        if (DrawExportDirectoryIconButton("HistoryExportDirectoryButton", new Vector2(32f, ImGui.GetTextLineHeight() + 2f), UtilityButtonColor))
        {
            historyExportDirectoryInput = Plugin.Configuration.HistoryExportDirectory ?? string.Empty;
            ImGui.OpenPopup("##HistoryExportDirectoryPopupInline");
        }
        Vector2 historyExportButtonMin = ImGui.GetItemRectMin();
        DrawHistoryExportDirectoryPopup(historyExportButtonMin);

        ImGui.SameLine();
        if (DrawStyledBoldButton("Export TXT", IsPlayerMode ? "PlayerExportTxtButton" : "DealerExportTxtButton", new Vector2(84f, ImGui.GetTextLineHeight() + 2f), Hex("#DA9E00"), WhiteText))
        {
            AddDebugLog("UI", $"Export TXT button clicked in {(IsPlayerMode ? "Player" : "Dealer")} mode.");
            ExportCurrentHistory(false);
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Export CSV", IsPlayerMode ? "PlayerExportCsvButton" : "DealerExportCsvButton", new Vector2(84f, ImGui.GetTextLineHeight() + 2f), DealerExportCsvColor, WhiteText))
        {
            AddDebugLog("UI", $"Export CSV button clicked in {(IsPlayerMode ? "Player" : "Dealer")} mode.");
            ExportCurrentHistory(true);
        }

        ImGui.Spacing();

        if (IsDealerMode)
        {
            DrawDealerPeriodSummary();
            ImGui.Spacing();
        }

        ImGui.Text("Sort by:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(220f);
        string sortPreview = IsDealerMode ? GetDealerSortComboPreviewText() : sortBy;
        if (ImGui.BeginCombo("##SortBy", sortPreview))
        {
            DrawSortOption("Most recent");
            DrawSortOption("Ascending");
            DrawSortOption("Results");

            if (IsDealerMode)
            {
                DrawSortOption("Tips");
                DrawDealerSortAndFilterOptions();
            }

            if (IsPlayerMode)
                DrawSortOption("Blackjack");

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Text("Search:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(190f);
        ImGui.InputText("##HistorySearch", ref searchInput, 128);

        ImGui.SameLine();
        if (DrawStyledBoldButton("Undo", "UndoButton", new Vector2(70f, 0f), UtilityButtonColor))
        {
            AddDebugLog("UI", $"Undo button clicked in {(IsPlayerMode ? "Player" : "Dealer")} mode.");
            UndoMostRecentHistory();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear History", "ClearHistoryButton", new Vector2(115f, 0f), UtilityButtonColor))
        {
            AddDebugLog("UI", $"Clear History button clicked in {(IsPlayerMode ? "Player" : "Dealer")} mode. Removing {GetCurrentHistory().Count} entries.");
            GetCurrentHistory().Clear();
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        DrawProfitsLossesSummary();

        if (DateTime.UtcNow < dealerExportStatusUntilUtc && !string.IsNullOrWhiteSpace(dealerExportStatusText))
        {
            ImGui.Spacing();
            if (dealerExportFailed || string.IsNullOrWhiteSpace(dealerExportDirectoryPath) || string.IsNullOrWhiteSpace(dealerExportFileName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
                ImGui.TextUnformatted(dealerExportStatusText);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
                ImGui.TextUnformatted(dealerExportStatusText);
                ImGui.PopStyleColor();

                ImGui.SameLine(0f, 4f);
                DrawClickableInlineText(dealerExportDirectoryPath, ProfitColor, OpenExportDirectory, false, "Click to open folder");

                ImGui.SameLine(0f, 0f);
                DrawClickableInlineText(dealerExportFileName, WhiteText, OpenExportDirectory, false, "Click to open folder");
            }
        }


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

        ImGui.TableSetupColumn("Results", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableHeadersRow();

        string? currentDateGroup = null;
        int rowIndex = 0;

        foreach (var entry in sortedHistory)
        {
            string entryDate = ParseTimestamp(entry.Timestamp).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (!string.Equals(currentDateGroup, entryDate, StringComparison.OrdinalIgnoreCase))
            {
                currentDateGroup = entryDate;
                DrawHistoryGroupHeader(entryDate, columnCount);
            }

            if (IsHistoryDateGroupCollapsed(entryDate))
                continue;

            ImGui.TableNextRow();
            rowIndex++;

            ImGui.TableSetColumnIndex(0);
            DrawColoredCell(entry.House, NeutralColor);

            ImGui.TableSetColumnIndex(1);
            DrawHistoryEntryLink(entry, rowIndex);

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

            if (IsCheckpointEntry(entry))
                DrawCheckpointResultCell(entry.Result);
            else if (entry.Result.Contains("Backup", StringComparison.OrdinalIgnoreCase))
                DrawBoldColoredCell("Backup", GoldColor);
            else
            {
                bool isBlackjackResult = entry.Result.Contains("Blackjack", StringComparison.OrdinalIgnoreCase);
                bool isDoubleDownResult = entry.Result.Contains("Double-Down", StringComparison.OrdinalIgnoreCase);
                var resultColor = isBlackjackResult
                    ? BlackjackColor
                    : ParseSignedFormatted(entry.Result) < BigInteger.Zero ? LossColor : ProfitColor;

                if (isDoubleDownResult)
                    DrawDoubleDownColoredCell(entry.Result);
                else if (isBlackjackResult)
                    DrawBoldColoredCell(entry.Result, resultColor);
                else
                    DrawColoredCell(entry.Result, resultColor);
            }
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

    private void DrawEditableHouseSuggestCell(
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
        bool isActive = ImGui.IsItemActive();
        bool isFocused = ImGui.IsItemFocused();
        Vector2 inputMin = ImGui.GetItemRectMin();
        Vector2 inputMax = ImGui.GetItemRectMax();

        string searchText = inputText?.Trim() ?? string.Empty;
        var allSuggestions = GetDealerHousePresets();
        var suggestions = string.IsNullOrWhiteSpace(searchText)
            ? allSuggestions.Take(8).ToList()
            : allSuggestions.Where(x => x.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Take(8).ToList();

        float popupHeight = MathF.Min(8, Math.Max(1, suggestions.Count)) * (ImGui.GetTextLineHeightWithSpacing() + 2f) + 10f;
        Vector2 popupMin = new(inputMin.X, inputMax.Y + 2f);
        Vector2 popupMax = new(inputMin.X + MathF.Max(valueWidth, inputMax.X - inputMin.X), inputMax.Y + 2f + popupHeight);
        bool hoveringPopupArea = ImGui.IsMouseHoveringRect(popupMin, popupMax, true);

        bool shouldShowSuggestions = suggestions.Count > 0 && (isActive || isFocused || hoveringPopupArea);

        if (shouldShowSuggestions)
        {
            ImGui.SetNextWindowPos(popupMin, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(popupMax.X - popupMin.X, popupHeight), ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f, 4f));
            if (ImGui.Begin(
                    $"{inputId}SuggestionsWindow",
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNavFocus))
            {
                foreach (string suggestion in suggestions)
                {
                    if (ImGui.Selectable(suggestion, false))
                    {
                        inputText = suggestion;
                        SaveCurrentProfileValues();
                    }
                }
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

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


    private void DrawDoubleDownColoredCell(string text)
    {
        const string suffix = "Double-Down";
        int suffixIndex = text.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);

        if (suffixIndex < 0)
        {
            DrawColoredCell(text, ParseSignedFormatted(text) < BigInteger.Zero ? LossColor : ProfitColor);
            return;
        }

        string prefix = text[..suffixIndex].TrimEnd();
        string suffixText = text[suffixIndex..];
        Vector4 prefixColor = prefix.StartsWith("-", StringComparison.Ordinal) ? LossColor : ProfitColor;

        ImGui.PushStyleColor(ImGuiCol.Text, prefixColor);
        ImGui.TextUnformatted(prefix);
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 4f);

        ImGui.PushStyleColor(ImGuiCol.Text, DoubleDownTextColor);
        ImGui.TextUnformatted(suffixText);
        ImGui.PopStyleColor();
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


    private bool DrawExportDirectoryIconButton(string id, Vector2 size, Vector4 baseColor)
    {
        Vector2 actualSize = size;
        if (actualSize.X <= 0f)
            actualSize.X = 32f;
        if (actualSize.Y <= 0f)
            actualSize.Y = ImGui.GetFrameHeight();

        Vector4 hoverColor = Lighten(baseColor, 0.18f);
        Vector4 activeColor = Lighten(baseColor, 0.08f);

        ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f));

        bool pressed = ImGui.Button($"##{id}", actualSize);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        Vector2 center = new((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);

        var drawList = ImGui.GetWindowDrawList();
        uint iconColor = ImGui.ColorConvertFloat4ToU32(WhiteText);

        float trayHalfWidth = MathF.Min(8f, (max.X - min.X) * 0.24f);
        float trayHeight = MathF.Min(5f, (max.Y - min.Y) * 0.18f);
        float trayY = center.Y + 5f;

        drawList.AddLine(
            new Vector2(center.X - trayHalfWidth, trayY),
            new Vector2(center.X + trayHalfWidth, trayY),
            iconColor,
            2f);

        drawList.AddLine(
            new Vector2(center.X - trayHalfWidth, trayY),
            new Vector2(center.X - trayHalfWidth, trayY - trayHeight),
            iconColor,
            2f);

        drawList.AddLine(
            new Vector2(center.X + trayHalfWidth, trayY),
            new Vector2(center.X + trayHalfWidth, trayY - trayHeight),
            iconColor,
            2f);

        float arrowTopY = center.Y - 7f;
        float arrowBottomY = center.Y + 1f;

        drawList.AddLine(
            new Vector2(center.X, arrowBottomY),
            new Vector2(center.X, arrowTopY + 3f),
            iconColor,
            2.3f);

        drawList.AddLine(
            new Vector2(center.X, arrowTopY),
            new Vector2(center.X - 4f, arrowTopY + 4f),
            iconColor,
            2.3f);

        drawList.AddLine(
            new Vector2(center.X, arrowTopY),
            new Vector2(center.X + 4f, arrowTopY + 4f),
            iconColor,
            2.3f);

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

    private static Vector4 WithAlpha(Vector4 color, float alpha)
    {
        return new Vector4(color.X, color.Y, color.Z, Math.Clamp(alpha, 0f, 1f));
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

        if (IsDealerMode)
        {
            if (!string.Equals(dealerHouseFilter, "All", StringComparison.OrdinalIgnoreCase))
                history = history.Where(entry => string.Equals(entry.House, dealerHouseFilter, StringComparison.OrdinalIgnoreCase));

            history = dealerResultFilter switch
            {
                "Profit only" => history.Where(entry => !IsCheckpointEntry(entry) && ParseSignedFormatted(entry.Result) > BigInteger.Zero),
                "Loss only" => history.Where(entry => !IsCheckpointEntry(entry) && ParseSignedFormatted(entry.Result) < BigInteger.Zero),
                "Has Tips" => history.Where(entry => ParseBankValue(entry.Tips) > BigInteger.Zero),
                _ => history
            };

            history = dealerDateFilter switch
            {
                "Today" => history.Where(entry => ParseTimestamp(entry.Timestamp).Date == DateTime.Today),
                "This Week" => history.Where(entry => ParseTimestamp(entry.Timestamp) >= DateTime.Today.AddDays(-6)),
                "This Month" => history.Where(entry => ParseTimestamp(entry.Timestamp).Year == DateTime.Today.Year && ParseTimestamp(entry.Timestamp).Month == DateTime.Today.Month),
                _ => history
            };
        }

        if (!string.IsNullOrWhiteSpace(searchInput))
        {
            string needle = searchInput.Trim();
            history = history.Where(entry => HistoryEntryMatchesSearch(entry, needle));
        }

        return sortBy switch
        {
            "Ascending" => history.OrderBy(x => ParseTimestamp(x.Timestamp)).ToList(),
            "Results" => history.OrderByDescending(x => ParseSignedFormatted(x.Result)).ToList(),
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
        {
            AddDebugLog("WARN", "Undo requested but history is empty.");
            return;
        }

        var mostRecent = history
            .OrderByDescending(x => ParseTimestamp(x.Timestamp))
            .First();

        history.Remove(mostRecent);
        Plugin.Configuration.Save();
        AddDebugLog("HISTORY", $"Undo removed entry: House: {mostRecent.House} | Time: {mostRecent.Timestamp} | Start Bank: {mostRecent.StartingBank} | Final Bank: {mostRecent.FinalBank} | Tips: {mostRecent.Tips} | Results: {mostRecent.Result}");
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
            trackedDealerInput = string.Empty;
            playerAutoTrackEnabled = false;
            profile.PlayerTrackedDealerInput = string.Empty;
            profile.PlayerAutoTrackEnabled = false;
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
            lastPlayerBankChangeValue = BigInteger.Zero;
            betInputHasUserEdited = ParseBankValue(profile.PlayerBetInput ?? string.Empty) > BigInteger.Zero;
            bankingInputHasUserEdited = false;
            ResetTrackerIndicatorStates();
            Plugin.Configuration.Save();
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
        ResetTrackerIndicatorStates();
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
        AddDebugLog("UI", $"Clearing inputs for {(IsPlayerMode ? "Player" : "Dealer")} mode.");
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
            lastPlayerBankChangeValue = BigInteger.Zero;
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
        ResetTrackerIndicatorStates();
        SaveCurrentProfileValues();
    }


    private void CreateProfile()
    {
        string trimmed = newProfileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        AddDebugLog("PROFILE", $"Creating profile '{trimmed}'.");

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

        AddDebugLog("PROFILE", $"Deleting profile '{profile.Name}'.");
        Plugin.Configuration.Profiles.Remove(profile);
        Plugin.Configuration.ActiveProfileName = Plugin.Configuration.Profiles[0].Name;
        Plugin.Configuration.Save();

        LoadFromConfiguration();
    }

    private void AddHistoryEntry()
    {
        AddDebugLog("HISTORY", $"Saving history entry in {(IsDealerMode ? "Dealer" : "Player")} mode.");
        var history = GetCurrentHistory();
        string resultText = IsPlayerMode
            ? FormatSignedResult(lastPlayerBankChangeValue)
            : GetCurrentResultText();

        var entry = new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = IsPlayerMode ? string.Empty : FormatNumber(tipsValue),
            Result = resultText
        };

        history.Add(entry);

        string bankLabel = IsPlayerMode ? "Total Bank" : "Final Bank";
        string tipsSegment = IsPlayerMode ? string.Empty : $" | Tips: {entry.Tips}";
        AddDebugLog("HISTORY", $"Entry: House: {entry.House} | Time: {entry.Timestamp} | Start Bank: {entry.StartingBank} | {bankLabel}: {entry.FinalBank}{tipsSegment} | Results: {entry.Result}");

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
        AddDebugLog("MODE", $"Switching mode to {mode}.");
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
        lastPlayerBankChangeValue = BigInteger.Zero;
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

        BigInteger previousCurrentBank = finalBankValue;

        finalBankValue = isWin
            ? finalBankValue + betValue
            : BigInteger.Max(BigInteger.Zero, finalBankValue - betValue);

        lastPlayerBankChangeValue = finalBankValue - previousCurrentBank;
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

        BigInteger previousCurrentBank = finalBankValue;
        finalBankValue += bankingValue;
        lastPlayerBankChangeValue = finalBankValue - previousCurrentBank;
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
            AddDebugLog("WARN", "Track Dealer failed: target not found.");
            trackDealerButtonDisabledUntilUtc = DateTime.UtcNow.AddSeconds(5);
            return;
        }

        trackedDealerInput = StripWorldSuffix(targetName);
        AddDebugLog("TRACK", $"Tracked dealer set to '{trackedDealerInput}'. Party-only monitoring {(playerAutoTrackEnabled ? "armed" : "waiting for Auto Track") }.");
        ResetTrackerIndicatorStates();
        SaveCurrentProfileValues();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            if (!IsPartyChatMonitoringActive)
                return;

            if (type != XivChatType.Party)
                return;

            if (DateTime.UtcNow >= latencyPendingUntilUtc)
            {
                if (latencyPendingPlayerTotal > 0)
                    AddDebugLog("AUTO-LATENCY", "Latency pending window expired. Clearing pending tracked total.");
                ClearPendingLatencyTrack();
            }

            string trackedDealer = NormalizeLooseText(trackedDealerInput);
            if (string.IsNullOrWhiteSpace(trackedDealer))
            {
                AddDebugLog("AUTO", "Party chat monitoring skipped because tracked dealer is empty.");
                return;
            }

            string senderRaw = sender.TextValue ?? string.Empty;
            string senderName = NormalizeLooseText(StripWorldSuffix(senderRaw));
            string localPlayerName = NormalizeLooseText(Plugin.PlayerState.CharacterName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(localPlayerName))
            {
                AddDebugLog("AUTO", "Party chat monitoring skipped because local player name is empty.");
                return;
            }

            string messageText = message.TextValue?.Replace('\n', ' ').Replace('\r', ' ').Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            AddDebugLog("CHAT", $"Party | Sender='{senderRaw}' | Message='{messageText}'");
            AddDebugLog("AUTO", $"Monitoring=PartyOnly | TrackedDealer='{trackedDealer}' | SenderNormalized='{senderName}' | LocalPlayer='{localPlayerName}'");

            bool senderIsTrackedDealer = NamesRoughlyMatch(senderName, trackedDealer);
            bool senderIsLocalPlayer = NamesRoughlyMatch(senderName, localPlayerName);
            AddDebugLog("AUTO", $"Sender match flags => Dealer:{senderIsTrackedDealer} | Local:{senderIsLocalPlayer}");

            if (senderIsTrackedDealer && MessageMentionsTrackedPlayer(messageText, localPlayerName) && IsDecisionPromptMessage(messageText))
            {
                doubleDownPromptUntilUtc = DateTime.UtcNow.AddSeconds(20);
                trackerDoubleDownsState = TrackerIndicatorState.Detected;
                AddDebugLog("AUTO", "Detected tracked-player decision prompt from dealer message. Double-down prompt window opened for 20s.");
            }

            if (senderIsLocalPlayer && DateTime.UtcNow < doubleDownPromptUntilUtc && IsDoubleDownCallMessage(messageText))
            {
                AddDebugLog("AUTO-DD", $"Detected local double-down call: {messageText}");
                trackerDoubleDownsState = TrackerIndicatorState.Detected;
                doubleDownPromptUntilUtc = DateTime.MinValue;
                doubleDownPendingUntilUtc = DateTime.UtcNow.AddSeconds(35);
                AddDebugLog("AUTO-DD", "Double-down pending window opened for 35s.");
                return;
            }

            if (!senderIsTrackedDealer)
            {
                AddDebugLog("AUTO", "Party message ignored because sender is not the tracked dealer.");
                return;
            }

            if (latencyPendingPlayerTotal > 0 &&
                TryResolveLatencyTrackedOutcome(messageText, localPlayerName, latencyPendingPlayerTotal, out string latencyOutcome))
            {
                AddDebugLog("AUTO-LATENCY", $"Resolved pending player total {latencyPendingPlayerTotal} as '{latencyOutcome}'.");
                ClearPendingLatencyTrack();

                if (DateTime.UtcNow < doubleDownPendingUntilUtc &&
                    !string.Equals(latencyOutcome, "Blackjack", StringComparison.OrdinalIgnoreCase))
                {
                    int latencyWinCount = string.Equals(latencyOutcome, "Win", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    int latencyLossCount = string.Equals(latencyOutcome, "Loss", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    int latencyPushCount = string.Equals(latencyOutcome, "Pushed", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                    AddDebugLog("AUTO-DD", "Applying pending double-down resolution from latency parser.");
                    ApplyTrackedDoubleDownOutcome(latencyWinCount, latencyLossCount, latencyPushCount);
                    doubleDownPendingUntilUtc = DateTime.MinValue;
                    doubleDownPromptUntilUtc = DateTime.MinValue;
                    return;
                }

                if (string.Equals(latencyOutcome, "Blackjack", StringComparison.OrdinalIgnoreCase))
                {
                    AddDebugLog("AUTO-BJ", "Applying blackjack outcome from latency parser.");
                    ApplyTrackedBlackjackOutcome();
                    blackjackPendingUntilUtc = DateTime.MinValue;
                    return;
                }

                if (DateTime.UtcNow < blackjackPendingUntilUtc)
                {
                    AddDebugLog("AUTO-BJ", $"Resolving pending blackjack from latency parser as {latencyOutcome}.");
                    int latencyWinCount = string.Equals(latencyOutcome, "Win", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    int latencyLossCount = string.Equals(latencyOutcome, "Loss", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    int latencyPushCount = string.Equals(latencyOutcome, "Pushed", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    ApplyTrackedBlackjackResolution(latencyWinCount, latencyLossCount, latencyPushCount);
                    blackjackPendingUntilUtc = DateTime.MinValue;
                    return;
                }

                if (string.Equals(latencyOutcome, "Win", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTrackedChatOutcome(1, 0, 0);
                    return;
                }

                if (string.Equals(latencyOutcome, "Loss", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTrackedChatOutcome(0, 1, 0);
                    return;
                }

                if (string.Equals(latencyOutcome, "Pushed", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTrackedChatOutcome(0, 0, 1);
                    return;
                }
            }

            if (TryCaptureLatencyPendingTotal(messageText, localPlayerName, out int latencyPlayerTotal))
            {
                latencyPendingPlayerTotal = latencyPlayerTotal;
                latencyPendingUntilUtc = DateTime.UtcNow.AddSeconds(30);
                AddDebugLog("AUTO-LATENCY", $"Captured pending player total {latencyPlayerTotal}. Waiting up to 30s for dealer resolution.");
                return;
            }

            if (TryDetectTrackedBlackjackMessage(messageText, localPlayerName))
            {
                AddDebugLog("AUTO-BJ", "Detected blackjack mention for tracked player from dealer message. Waiting for final dealer result resolution.");
                doubleDownPendingUntilUtc = DateTime.MinValue;
                doubleDownPromptUntilUtc = DateTime.MinValue;
                ClearPendingLatencyTrack();
                blackjackPendingUntilUtc = DateTime.UtcNow.AddSeconds(35);
                trackerBlackjacksState = TrackerIndicatorState.Detected;
                AddDebugLog("AUTO-BJ", "Blackjack pending window opened for 35s.");
                return;
            }

            if (!TryGetTrackedOutcomeCounts(messageText, localPlayerName, out int winCount, out int lossCount, out int pushCount))
            {
                AddDebugLog("AUTO", "Tracked dealer party message did not contain a resolvable outcome for the local player.");
                return;
            }

            AddDebugLog("AUTO", $"Matched tracked outcome counts - Win:{winCount} Loss:{lossCount} Push:{pushCount}.");

            if (DateTime.UtcNow < doubleDownPendingUntilUtc)
            {
                AddDebugLog("AUTO-DD", "Applying pending double-down resolution.");
                ApplyTrackedDoubleDownOutcome(winCount, lossCount, pushCount);
                doubleDownPendingUntilUtc = DateTime.MinValue;
                doubleDownPromptUntilUtc = DateTime.MinValue;
                return;
            }

            if (DateTime.UtcNow < blackjackPendingUntilUtc)
            {
                AddDebugLog("AUTO-BJ", "Applying pending blackjack resolution from direct tracked counts.");
                ApplyTrackedBlackjackResolution(winCount, lossCount, pushCount);
                blackjackPendingUntilUtc = DateTime.MinValue;
                return;
            }

            ApplyTrackedChatOutcome(winCount, lossCount, pushCount);
        }
        catch (Exception ex)
        {
            AddDebugLog("ERR", $"OnChatMessage failed: {ex.GetType().Name}: {ex.Message}");
            trackerWinsState = TrackerIndicatorState.Error;
            trackerLossesState = TrackerIndicatorState.Error;
            trackerBlackjacksState = TrackerIndicatorState.Error;
            trackerDoubleDownsState = TrackerIndicatorState.Error;
        }
    }

    private bool TryGetTrackedOutcomeCounts(string messageText, string localPlayerName, out int winCount, out int lossCount, out int pushCount)
    {
        winCount = 0;
        lossCount = 0;
        pushCount = 0;

        AddDebugLog("AUTO-PARSE", $"Parsing tracked results from: {messageText}");

        foreach (Match match in TrackResultRegex.Matches(messageText))
        {
            string label = match.Groups["label"].Value;
            string namesSection = match.Groups["names"].Value;
            int matchCount = CountPlayerMentions(namesSection, localPlayerName);
            AddDebugLog("AUTO-PARSE", $"Section label='{label}' names='{namesSection}' matches={matchCount}.");

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

        return winCount > 0 || lossCount > 0 || pushCount > 0;
    }

    private void ApplyTrackedDoubleDownOutcome(int winCount, int lossCount, int pushCount)
    {
        try
        {
            AddDebugLog("AUTO-DD", $"Applying double-down outcome - Win:{winCount} Loss:{lossCount} Push:{pushCount}.");
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

            BigInteger doubleDownUnit = betValue * 2;

            if (netCount > 0)
                finalBankValue += doubleDownUnit * netCount;
            else if (netCount < 0)
                finalBankValue = BigInteger.Max(BigInteger.Zero, finalBankValue - (doubleDownUnit * BigInteger.Abs(netCount)));

            finalBankInput = FormatNumber(finalBankValue);

            if (isPushed)
            {
                AddDebugLog("AUTO-DD", "Double-down outcome resolved as PUSHED.");
                lastPlayerBankChangeValue = BigInteger.Zero;
                AddTrackedHistoryEntry("+Pushed Double-Down");
                SetTemporaryCurrentBetDisplay("Bet Pushed!", ProfitColor, 5);
                trackerDoubleDownsState = TrackerIndicatorState.Done;
                doubleDownDoneUntilUtc = DateTime.UtcNow.AddSeconds(5);
                SaveCurrentProfileValues();
                return;
            }

            BigInteger trackedResultValue = finalBankValue - oldCurrentBankValue;
            AddDebugLog("AUTO-DD", $"Double-down bank delta: {FormatSignedResult(trackedResultValue)}.");
            lastPlayerBankChangeValue = trackedResultValue;
            AddTrackedHistoryEntry($"{FormatSignedResult(trackedResultValue)} Double-Down");

            if (trackedResultValue > BigInteger.Zero)
                SetTemporaryCurrentBetDisplay("You won!", ProfitColor, 5);
            else if (trackedResultValue < BigInteger.Zero)
                SetTemporaryCurrentBetDisplay("You Lost!", LossColor, 5);

            trackerDoubleDownsState = TrackerIndicatorState.Done;
            doubleDownDoneUntilUtc = DateTime.UtcNow.AddSeconds(5);
            SaveCurrentProfileValues();
        }
        catch
        {
            trackerDoubleDownsState = TrackerIndicatorState.Error;
            throw;
        }
    }

    private static bool MessageMentionsTrackedPlayer(string messageText, string normalizedPlayerName)
    {
        if (string.IsNullOrWhiteSpace(messageText) || string.IsNullOrWhiteSpace(normalizedPlayerName))
            return false;

        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        foreach (string playerNameVariant in GetLocalPlayerNameVariants(normalizedPlayerName))
        {
            if (Regex.IsMatch(
                    normalizedMessage,
                    $@"\b{Regex.Escape(playerNameVariant)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDecisionPromptMessage(string messageText)
    {
        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        return normalizedMessage.Contains("hit", StringComparison.OrdinalIgnoreCase) ||
               normalizedMessage.Contains("stand", StringComparison.OrdinalIgnoreCase) ||
               normalizedMessage.Contains("stay", StringComparison.OrdinalIgnoreCase) ||
               normalizedMessage.Contains("split", StringComparison.OrdinalIgnoreCase) ||
               normalizedMessage.Contains("double", StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(normalizedMessage, @"\bdd\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private bool TryDetectTrackedBlackjackMessage(string messageText, string localPlayerName)
    {
        if (!MessageMentionsTrackedPlayer(messageText, localPlayerName))
            return false;

        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        if (Regex.IsMatch(
                normalizedMessage,
                @"\b(?:[1-9]|[1-9][0-9]|100)\s+bjs?\b|\bgot\s+(?:[1-9]|[1-9][0-9]|100)\s+bjs?\b|\bhad\s+(?:[1-9]|[1-9][0-9]|100)\s+bjs?\b|\bstealing\s+all\s+bjs?\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            AddDebugLog("AUTO-BJ", $"Ignoring blackjack stage-1 false positive dealer message: {messageText}");
            return false;
        }

        foreach (string keyword in BlackjackStageOneKeywords)
        {
            if (normalizedMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                AddDebugLog("AUTO-BJ", $"Blackjack stage-1 keyword matched: '{keyword}' in dealer message: {messageText}");
                return true;
            }
        }

        if (Regex.IsMatch(normalizedMessage, @"\bbj\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            AddDebugLog("AUTO-BJ", $"Blackjack stage-1 keyword matched: 'bj' in dealer message: {messageText}");
            return true;
        }

        return false;
    }
    private static bool MessageStartsWithTrackedPlayer(string messageText, string normalizedPlayerName)
    {
        if (string.IsNullOrWhiteSpace(messageText) || string.IsNullOrWhiteSpace(normalizedPlayerName))
            return false;

        string normalizedMessage = NormalizeLooseText(messageText);
        if (!normalizedMessage.StartsWith(normalizedPlayerName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalizedMessage.Length == normalizedPlayerName.Length)
            return true;

        char nextChar = normalizedMessage[normalizedPlayerName.Length];
        return char.IsWhiteSpace(nextChar);
    }

    private static bool IsDoubleDownCallMessage(string messageText)
    {
        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        if (normalizedMessage.Contains("double down", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("i'll double down", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("ill double down", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("doubling down", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("going dd", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("DOUBLE DOWN!!!!!!!!!!", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("DOUBLE DOWN!", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("dd for sure", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("double down for sure", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("Lets dd", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("fck it dd", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("fuck it dd", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedMessage.Contains("dd", StringComparison.OrdinalIgnoreCase))
            return true;

        var parts = normalizedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Any(x => string.Equals(x, "dd", StringComparison.OrdinalIgnoreCase));
    }


    private void ClearPendingLatencyTrack()
    {
        if (latencyPendingPlayerTotal != 0 || latencyPendingUntilUtc != DateTime.MinValue)
            AddDebugLog("AUTO-LATENCY", $"Clearing pending latency track (total={latencyPendingPlayerTotal}).");
        latencyPendingPlayerTotal = 0;
        latencyPendingUntilUtc = DateTime.MinValue;
    }

    private bool TryCaptureLatencyPendingTotal(string messageText, string localPlayerName, out int playerTotal)
    {
        playerTotal = 0;

        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage) || string.IsNullOrWhiteSpace(localPlayerName))
            return false;

        Match directMatch = Regex.Match(
            normalizedMessage,
            $@"\b{Regex.Escape(localPlayerName)}\s+(?<value>\d{{1,2}})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!directMatch.Success || !int.TryParse(directMatch.Groups["value"].Value, out playerTotal))
            return false;

        bool valid = playerTotal >= 1 && playerTotal <= 40;
        if (valid)
            AddDebugLog("AUTO-LATENCY", $"Captured possible player total {playerTotal} from message: {messageText}");
        return valid;
    }

    private bool TryResolveLatencyTrackedOutcome(string messageText, string localPlayerName, int pendingPlayerTotal, out string outcome)
    {
        outcome = string.Empty;

        string normalizedMessage = NormalizeLooseText(messageText);
        if (string.IsNullOrWhiteSpace(normalizedMessage) || string.IsNullOrWhiteSpace(localPlayerName))
            return false;

        Match dealerMatch = Regex.Match(
            normalizedMessage,
            @"\bdealer\s+(?<dealer>\d{1,2})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!dealerMatch.Success || !int.TryParse(dealerMatch.Groups["dealer"].Value, out int dealerTotal))
            return false;

        if (dealerTotal < 1 || dealerTotal > 21)
            return false;

        bool playerBusted = Regex.IsMatch(
            normalizedMessage,
            $@"\b{Regex.Escape(localPlayerName)}\b.*\b(?:bust|busted)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        bool playerTotalMatches = Regex.IsMatch(
            normalizedMessage,
            $@"\b{Regex.Escape(localPlayerName)}\s+{pendingPlayerTotal}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!playerBusted && !playerTotalMatches)
            return false;

        if (playerBusted || pendingPlayerTotal > 21 || pendingPlayerTotal < dealerTotal)
        {
            AddDebugLog("AUTO-LATENCY", $"Latency resolution => LOSS | pending={pendingPlayerTotal} dealer={dealerTotal} busted={playerBusted}.");
            outcome = "Loss";
            return true;
        }

        if (pendingPlayerTotal == 21 && dealerTotal < 21)
        {
            AddDebugLog("AUTO-LATENCY", $"Latency resolution => BLACKJACK | pending={pendingPlayerTotal} dealer={dealerTotal}.");
            outcome = "Blackjack";
            return true;
        }

        if (pendingPlayerTotal > dealerTotal)
        {
            AddDebugLog("AUTO-LATENCY", $"Latency resolution => WIN | pending={pendingPlayerTotal} dealer={dealerTotal}.");
            outcome = "Win";
            return true;
        }

        if (pendingPlayerTotal == dealerTotal)
        {
            AddDebugLog("AUTO-LATENCY", $"Latency resolution => PUSHED | pending={pendingPlayerTotal} dealer={dealerTotal}.");
            outcome = "Pushed";
            return true;
        }

        return false;
    }

    private void ApplyTrackedBlackjackResolution(int winCount, int lossCount, int pushCount)
    {
        try
        {
            trackerBlackjacksState = TrackerIndicatorState.Detected;
            AddDebugLog("AUTO-BJ", $"Resolving blackjack follow-up - Win:{winCount} Loss:{lossCount} Push:{pushCount}.");

            int netCount = winCount - lossCount;
            bool isPurePush = pushCount > 0 && winCount == 0 && lossCount == 0;
            bool isBalancedPush = netCount == 0 && (winCount > 0 || lossCount > 0);
            bool isPushed = isPurePush || isBalancedPush;

            if (isPushed)
            {
                AddDebugLog("AUTO-BJ", "Blackjack follow-up resolved as PUSHED.");
                lastPlayerBankChangeValue = BigInteger.Zero;
                AddTrackedHistoryEntry("+Pushed Blackjack");
                SetTemporaryCurrentBetDisplay("Blackjack Pushed!", BlackjackColor, 5);
                SaveCurrentProfileValues();
                return;
            }

            if (netCount > 0)
            {
                AddDebugLog("AUTO-BJ", "Blackjack follow-up resolved as WIN.");
                ApplyTrackedBlackjackOutcome();
                return;
            }

            if (netCount < 0)
            {
                AddDebugLog("AUTO-BJ", "Blackjack follow-up resolved as LOSS.");
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
                finalBankValue = BigInteger.Max(BigInteger.Zero, finalBankValue - betValue);
                finalBankInput = FormatNumber(finalBankValue);
                lastPlayerBankChangeValue = finalBankValue - oldCurrentBankValue;

                AddTrackedHistoryEntry($"{FormatSignedResult(lastPlayerBankChangeValue)} Blackjack");
                SetTemporaryCurrentBetDisplay("Blackjack Lost!", LossColor, 5);
                SaveCurrentProfileValues();
                return;
            }

            AddDebugLog("AUTO-BJ", "Blackjack follow-up had no resolvable net outcome.");
        }
        catch
        {
            trackerBlackjacksState = TrackerIndicatorState.Error;
            throw;
        }
    }

    private void ApplyTrackedBlackjackOutcome()
    {
        try
        {
            trackerBlackjacksState = TrackerIndicatorState.Detected;
            AddDebugLog("AUTO-BJ", "Applying tracked blackjack outcome.");
            betValue = ParseBankValue(betInput);
            betInput = FormatNumber(betValue);

            if (betValue <= BigInteger.Zero)
            {
                SaveCurrentProfileValues();
                return;
            }

            if (finalBankValue == BigInteger.Zero && startingBankValue > BigInteger.Zero && string.IsNullOrWhiteSpace(finalBankInput))
                finalBankValue = startingBankValue;

            int multiplierIndex = Math.Clamp(natbjMultiplierIndex, 0, BlackjackMultiplierTenths.Length - 1);
            BigInteger payout = (betValue * BlackjackMultiplierTenths[multiplierIndex]) / 10;
            BigInteger oldCurrentBankValue = finalBankValue;

            finalBankValue += payout;
            finalBankInput = FormatNumber(finalBankValue);
            lastPlayerBankChangeValue = finalBankValue - oldCurrentBankValue;

            AddDebugLog("AUTO-BJ", $"Blackjack payout delta: +{FormatNumber(payout)}.");
            AddBlackjackHistoryEntry(payout);
            SetTemporaryCurrentBetDisplay("Blackjack!", BlackjackColor, 5);
            SaveCurrentProfileValues();
        }
        catch
        {
            trackerBlackjacksState = TrackerIndicatorState.Error;
            throw;
        }
    }

    private void ApplyTrackedChatOutcome(int winCount, int lossCount, int pushCount)
    {
        try
        {
            AddDebugLog("AUTO", $"Applying tracked outcome - Win:{winCount} Loss:{lossCount} Push:{pushCount}.");
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
                AddDebugLog("AUTO", "Tracked outcome resolved as PUSHED.");
                lastPlayerBankChangeValue = BigInteger.Zero;
                AddTrackedHistoryEntry("Pushed");
                SetTemporaryCurrentBetDisplay("Bet Pushed!", ProfitColor, 5);
                SaveCurrentProfileValues();
                return;
            }

            BigInteger trackedResultValue = finalBankValue - oldCurrentBankValue;
            AddDebugLog("AUTO", $"Tracked outcome bank delta: {FormatSignedResult(trackedResultValue)}.");
            lastPlayerBankChangeValue = trackedResultValue;
            AddTrackedHistoryEntry(FormatSignedResult(trackedResultValue));

            if (trackedResultValue > BigInteger.Zero)
                SetTemporaryCurrentBetDisplay("You won!", ProfitColor, 5);
            else if (trackedResultValue < BigInteger.Zero)
                SetTemporaryCurrentBetDisplay("You Lost!", LossColor, 5);

            SaveCurrentProfileValues();
        }
        catch
        {
            if (winCount > 0)
                trackerWinsState = TrackerIndicatorState.Error;
            if (lossCount > 0 || pushCount > 0)
                trackerLossesState = TrackerIndicatorState.Error;
            throw;
        }
    }

    private void AddTrackedHistoryEntry(string resultText)
    {
        var history = GetCurrentHistory();

        AddDebugLog("HISTORY", $"Adding tracked history entry: {resultText}.");
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
        AddDebugLog("UI", $"Setting temporary bet display to '{text}' for {seconds}s.");
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

    private static IEnumerable<string> GetLocalPlayerNameVariants(string normalizedPlayerName)
    {
        if (string.IsNullOrWhiteSpace(normalizedPlayerName))
            yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (seen.Add(normalizedPlayerName))
            yield return normalizedPlayerName;

        foreach (string part in normalizedPlayerName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (seen.Add(part))
                yield return part;
        }
    }

    private static int CountPlayerMentions(string namesSection, string normalizedPlayerName)
    {
        if (string.IsNullOrWhiteSpace(namesSection) || string.IsNullOrWhiteSpace(normalizedPlayerName))
            return 0;

        string normalizedSection = NormalizeLooseText(namesSection);
        if (string.IsNullOrWhiteSpace(normalizedSection))
            return 0;

        int count = 0;

        foreach (string playerNameVariant in GetLocalPlayerNameVariants(normalizedPlayerName))
        {
            int index = 0;

            while (true)
            {
                index = normalizedSection.IndexOf(playerNameVariant, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;

                bool validLeft = index == 0 || normalizedSection[index - 1] == ' ';
                int endIndex = index + playerNameVariant.Length;
                bool validRight = endIndex >= normalizedSection.Length || normalizedSection[endIndex] == ' ';

                if (validLeft && validRight)
                {
                    count++;
                    break;
                }

                index = endIndex;
            }

            if (count > 0)
                break;
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


    private void DrawDealerSessionStatusSection()
    {
        string currentHouse = string.IsNullOrWhiteSpace(houseInput) ? "Not set" : houseInput.Trim();
        string checkpointState = GetDealerCheckpointState();
        DateTime? shiftStart = TryGetCurrentDealerShiftStart();
        string shiftStartText = shiftStart.HasValue ? shiftStart.Value.ToString("yyyy-MM-dd HH:mm") : "--";
        string elapsedText = shiftStart.HasValue ? FormatElapsed(DateTime.Now - shiftStart.Value) : "--";

        Vector4 statusColor = GoldColor;
        if (string.Equals(checkpointState, "No Shift", StringComparison.OrdinalIgnoreCase))
            statusColor = LossColor;
        else if (string.Equals(checkpointState, "Ended", StringComparison.OrdinalIgnoreCase))
            statusColor = LossColor;
        else if (string.Equals(checkpointState, "Active", StringComparison.OrdinalIgnoreCase))
            statusColor = ProfitColor;
        else if (string.Equals(checkpointState, "On Break", StringComparison.OrdinalIgnoreCase))
            statusColor = Hex("#00B3D8");

        ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
        ImGui.TextUnformatted("♯ Current Dealer Session");
        ImGui.PopStyleColor();

        ImGui.TextUnformatted($"House: {currentHouse} | ");
        ImGui.SameLine(0f, 0f);
        ImGui.TextUnformatted("Status:");
        ImGui.SameLine(0f, 4f);
        DrawColoredInlineText(checkpointState, statusColor);
        ImGui.SameLine(0f, 0f);
        ImGui.TextUnformatted($" | Started: {shiftStartText} | Elapsed: {elapsedText}");
    }

    private void DrawDealerCheckpointButtons()
    {
        bool isOnBreak = string.Equals(GetDealerCheckpointState(), "On Break", StringComparison.OrdinalIgnoreCase);
        bool hasActiveShift = TryGetCurrentDealerShiftStart().HasValue;

        if (DrawStyledBoldButton("Start Shift", "DealerStartShiftButton", new Vector2(92f, 0f), DealerButtonColor))
        {
            AddDebugLog("UI", "Dealer checkpoint button clicked: Start Shift.");
            AddDealerCheckpoint("Start Shift");
        }

        ImGui.SameLine();
        if (!hasActiveShift || isOnBreak)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Break", "DealerBreakButton", new Vector2(70f, 0f), DealerBreakColor, WhiteText))
        {
            AddDebugLog("UI", "Dealer checkpoint button clicked: Break.");
            AddDealerCheckpoint("Break");
        }
        if (!hasActiveShift || isOnBreak)
            ImGui.EndDisabled();

        ImGui.SameLine();
        if (!hasActiveShift || !isOnBreak)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Resume", "DealerResumeButton", new Vector2(78f, 0f), DealerResumeColor, WhiteText))
        {
            AddDebugLog("UI", "Dealer checkpoint button clicked: Resume.");
            AddDealerCheckpoint("Resume");
        }
        if (!hasActiveShift || !isOnBreak)
            ImGui.EndDisabled();

        ImGui.SameLine();
        if (!hasActiveShift)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("End Shift", "DealerEndShiftButton", new Vector2(90f, 0f), LossButtonColor, WhiteText))
        {
            AddDebugLog("UI", "Dealer checkpoint button clicked: End Shift.");
            AddDealerCheckpoint("End Shift");
        }
        if (!hasActiveShift)
            ImGui.EndDisabled();
    }

    private void IncrementDealerTips(BigInteger amount)
    {
        tipsValue += amount;
        tipsInput = FormatNumber(tipsValue);
        AddDebugLog("UI", $"Quick tip button applied: +{FormatNumber(amount)} | New Tips Total: {tipsInput}");
        SaveCurrentProfileValues();
    }

    private void AddDealerCheckpoint(string checkpointName)
    {
        var history = GetCurrentHistory();

        var entry = new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = FormatNumber(tipsValue),
            Result = checkpointName
        };

        history.Add(entry);

        if (history.Count > 200)
            history.RemoveAt(0);

        AddDebugLog("HISTORY", $"Checkpoint entry saved: House: {entry.House} | Time: {entry.Timestamp} | Start Bank: {entry.StartingBank} | Final Bank: {entry.FinalBank} | Tips: {entry.Tips} | Results: {entry.Result}");
        Plugin.Configuration.Save();

        if (string.Equals(checkpointName, "End Shift", StringComparison.OrdinalIgnoreCase) && Plugin.Configuration.DealerAutoBackupOnEndShift)
        {
            AddBackupHistoryEntry();
            ExportCurrentHistory(false, true, "EndShiftBackup");
            ExportCurrentHistory(true, true, "EndShiftBackup");
        }
    }


    private void DrawDealerPeriodSummary()
    {
        DrawDealerSummaryLine("Today", DateTime.Today, DateTime.Now);
        DrawDealerSummaryLine("This Week", DateTime.Today.AddDays(-6), DateTime.Now);
        DrawDealerSummaryLine("This Month", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Now);
    }

    private void DrawDealerSummaryLine(string label, DateTime fromInclusive, DateTime toInclusive)
    {
        var entries = GetCurrentHistory()
            .Where(x => !IsCheckpointEntry(x))
            .Where(x =>
            {
                DateTime ts = ParseTimestamp(x.Timestamp);
                return ts >= fromInclusive && ts <= toInclusive;
            })
            .ToList();

        BigInteger net = BigInteger.Zero;
        BigInteger tips = BigInteger.Zero;
        foreach (var entry in entries)
        {
            net += ParseSignedFormatted(entry.Result);
            tips += ParseBankValue(entry.Tips);
        }

        Vector4 netColor = net > BigInteger.Zero ? ProfitColor : LossColor;
        Vector4 tipsColor = tips > BigInteger.Zero ? ProfitColor : LossColor;

        ImGui.TextUnformatted($"{label}:");
        ImGui.SameLine(0f, 4f);
        DrawColoredInlineText(FormatSignedResult(net), netColor);
        ImGui.SameLine(0f, 8f);
        ImGui.TextUnformatted("| Tips");
        ImGui.SameLine(0f, 4f);
        DrawColoredInlineText(FormatNumber(tips), tipsColor);
        ImGui.SameLine(0f, 8f);
        ImGui.TextUnformatted($"| Entries {entries.Count}");
    }

    private void DrawDealerSortAndFilterOptions()
    {
        ImGui.Separator();
        ImGui.TextDisabled("House Filter");
        var houseOptions = new List<string> { "All" };
        houseOptions.AddRange(GetDealerHousePresets());

        foreach (string option in houseOptions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            bool selected = string.Equals(dealerHouseFilter, option, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable($"House: {option}", selected))
                dealerHouseFilter = option;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.Separator();
        ImGui.TextDisabled("Result Filter");
        foreach (string option in new[] { "All", "Profit only", "Loss only", "Has Tips" })
        {
            bool selected = string.Equals(dealerResultFilter, option, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable($"Result: {option}", selected))
                dealerResultFilter = option;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.Separator();
        ImGui.TextDisabled("Date Filter");
        foreach (string option in new[] { "All", "Today", "This Week", "This Month" })
        {
            bool selected = string.Equals(dealerDateFilter, option, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable($"Date: {option}", selected))
                dealerDateFilter = option;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private string GetDealerSortComboPreviewText()
    {
        var parts = new List<string> { sortBy };

        if (!string.Equals(dealerHouseFilter, "All", StringComparison.OrdinalIgnoreCase))
            parts.Add($"House: {dealerHouseFilter}");

        if (!string.Equals(dealerResultFilter, "All", StringComparison.OrdinalIgnoreCase))
            parts.Add($"Result: {dealerResultFilter}");

        if (!string.Equals(dealerDateFilter, "All", StringComparison.OrdinalIgnoreCase))
            parts.Add($"Date: {dealerDateFilter}");

        return string.Join(" | ", parts);
    }

    private void DrawCheckpointResultCell(string text)
    {
        DrawColoredCell(text, GetCheckpointResultColor(text));
    }

    private Vector4 GetCheckpointResultColor(string text)
    {
        if (text.Contains("Break", StringComparison.OrdinalIgnoreCase))
            return Hex("#1628C2");

        if (text.Contains("Resume", StringComparison.OrdinalIgnoreCase))
            return Hex("#D90090");

        if (text.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
            return LossColor;

        return GoldColor;
    }

    private void DrawColoredInlineText(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private void DrawClickableInlineText(string text, Vector4 color, Action onClick, bool underlineOnlyOnHover = false, string tooltip = "Click to open")
    {
        bool hovered;
        Vector2 min;
        Vector2 max;
        Vector4 drawColor = color;

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        hovered = ImGui.IsItemHovered();
        min = ImGui.GetItemRectMin();
        max = ImGui.GetItemRectMax();
        ImGui.PopStyleColor();

        if (hovered)
        {
            drawColor = Lighten(color, 0.22f);
            uint hoverBg = ImGui.ColorConvertFloat4ToU32(new Vector4(color.X, color.Y, color.Z, 0.12f));
            ImGui.GetWindowDrawList().AddRectFilled(
                new Vector2(min.X - 2f, min.Y - 1f),
                new Vector2(max.X + 2f, max.Y + 1f),
                hoverBg,
                3f);

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(tooltip);
                ImGui.EndTooltip();
            }
        }

        if (!underlineOnlyOnHover || hovered)
        {
            uint underlineColor = ImGui.ColorConvertFloat4ToU32(drawColor);
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(min.X, max.Y),
                new Vector2(max.X, max.Y),
                underlineColor,
                hovered ? 2f : 1f);
        }

        if (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            onClick();
    }

    private void OpenExportDirectory()
    {
        if (string.IsNullOrWhiteSpace(dealerExportFilePath))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{dealerExportFilePath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(dealerExportFilePath) ?? dealerExportDirectoryPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
        }
    }


    private void AddBackupHistoryEntry()
    {
        var history = GetCurrentHistory();

        var entry = new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = FormatNumber(finalBankValue),
            Tips = IsPlayerMode ? string.Empty : FormatNumber(tipsValue),
            Result = "Backup"
        };

        history.Add(entry);

        if (history.Count > 200)
            history.RemoveAt(0);

        AddDebugLog("HISTORY", $"Backup entry saved: House: {entry.House} | Time: {entry.Timestamp} | Start Bank: {entry.StartingBank} | Final Bank: {entry.FinalBank} | Tips: {entry.Tips} | Results: {entry.Result}");
        Plugin.Configuration.Save();
    }

    private static bool IsCheckpointEntry(HistoryEntry entry)
    {
        return entry.Result.Equals("Start Shift", StringComparison.OrdinalIgnoreCase) ||
               entry.Result.Equals("Break", StringComparison.OrdinalIgnoreCase) ||
               entry.Result.Equals("Resume", StringComparison.OrdinalIgnoreCase) ||
               entry.Result.Equals("End Shift", StringComparison.OrdinalIgnoreCase);
    }

    private DateTime? TryGetCurrentDealerShiftStart()
    {
        var history = GetCurrentHistory()
            .Where(IsCheckpointEntry)
            .OrderBy(x => ParseTimestamp(x.Timestamp))
            .ToList();

        DateTime? currentShiftStart = null;

        foreach (var entry in history)
        {
            if (entry.Result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
                currentShiftStart = ParseTimestamp(entry.Timestamp);
            else if (entry.Result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
                currentShiftStart = null;
        }

        return currentShiftStart;
    }

    private string GetDealerCheckpointState()
    {
        var latestCheckpoint = GetCurrentHistory()
            .Where(IsCheckpointEntry)
            .OrderByDescending(x => ParseTimestamp(x.Timestamp))
            .FirstOrDefault();

        if (latestCheckpoint == null)
            return "No Shift";

        DateTime latestTimestamp = ParseTimestamp(latestCheckpoint.Timestamp);

        if (latestCheckpoint.Result.Contains("Break", StringComparison.OrdinalIgnoreCase))
            return "On Break";

        if (latestCheckpoint.Result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
        {
            if ((DateTime.Now - latestTimestamp).TotalSeconds >= 10)
                return "No Shift";

            return "Ended";
        }

        if (latestCheckpoint.Result.Contains("Resume", StringComparison.OrdinalIgnoreCase) ||
            latestCheckpoint.Result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
            return "Active";

        return "No Shift";
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m";

        return $"{elapsed.Minutes:D2}m";
    }

    private List<string> GetDealerHousePresets()
    {
        return Plugin.Configuration.GetOrCreateActiveProfile().DealerHistory
            .Select(x => x.House?.Trim() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private void ExportDealerHistory(bool asCsv)
    {
        ExportCurrentHistory(asCsv);
    }

    private void ExportDealerHistory(bool asCsv, bool isAutoBackup, string filePrefix)
    {
        ExportCurrentHistory(asCsv, isAutoBackup, filePrefix);
    }

    private static string BuildDealerTxt(IEnumerable<HistoryEntry> entries)
    {
        var builder = new StringBuilder();
        bool isFirst = true;

        foreach (var entry in entries)
        {
            if (!isFirst)
                builder.AppendLine();

            builder.AppendLine($"House: {entry.House} | Time: {entry.Timestamp} | Start: {entry.StartingBank} | Final: {entry.FinalBank} | Tips: {entry.Tips} | Result: {entry.Result}");
            isFirst = false;
        }

        return builder.ToString();
    }

    private static string BuildDealerSpreadsheetXml(IEnumerable<HistoryEntry> entries)
    {
        static string StyleIdForResult(string result, bool darkRow)
        {
            result ??= string.Empty;

            if (result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
                return darkRow ? "resultStartDark" : "resultStartBlack";

            if (result.Contains("Break", StringComparison.OrdinalIgnoreCase))
                return darkRow ? "resultBreakDark" : "resultBreakBlack";

            if (result.Contains("Resume", StringComparison.OrdinalIgnoreCase))
                return darkRow ? "resultResumeDark" : "resultResumeBlack";

            if (result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
                return darkRow ? "resultEndDark" : "resultEndBlack";

            return ParseSignedFormatted(result) > BigInteger.Zero
                ? darkRow ? "resultPositiveDark" : "resultPositiveBlack"
                : darkRow ? "resultNegativeDark" : "resultNegativeBlack";
        }

        static void AppendEmptyBlackCells(StringBuilder builder, int count)
        {
            for (int i = 0; i < count; i++)
                builder.AppendLine("    <Cell ss:StyleID=\"baseBlack\"><Data ss:Type=\"String\"></Data></Cell>");
        }

        var entryList = entries.ToList();
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\"?>");
        builder.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        builder.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        builder.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        builder.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        builder.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        builder.AppendLine(" <Styles>");
        builder.AppendLine(BuildSpreadsheetStyleXml("baseBlack", "#000000", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("baseDark", "#1f1f1f", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("header", "#000000", "#FFFFFF", true, true));
        builder.AppendLine(BuildSpreadsheetStyleXml("houseBlack", "#000000", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("houseDark", "#1f1f1f", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("timeBlack", "#000000", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("timeDark", "#1f1f1f", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("startBlack", "#000000", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("startDark", "#1f1f1f", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("finalBlack", "#000000", "#00FF66", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("finalDark", "#1f1f1f", "#00FF66", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("tipsBlack", "#000000", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("tipsDark", "#1f1f1f", "#FFFFFF", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultPositiveBlack", "#000000", "#00FF66", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultPositiveDark", "#1f1f1f", "#00FF66", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultNegativeBlack", "#000000", "#FF8A8A", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultNegativeDark", "#1f1f1f", "#FF8A8A", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultStartBlack", "#000000", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultStartDark", "#1f1f1f", "#FFA500", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultBreakBlack", "#000000", "#00B3D8", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultBreakDark", "#1f1f1f", "#00B3D8", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultResumeBlack", "#000000", "#D90090", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultResumeDark", "#1f1f1f", "#D90090", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultEndBlack", "#000000", "#FF8A8A", true));
        builder.AppendLine(BuildSpreadsheetStyleXml("resultEndDark", "#1f1f1f", "#FF8A8A", true));
        builder.AppendLine(" </Styles>");
        builder.AppendLine(" <Worksheet ss:Name=\"Dealer History\">");
        builder.AppendLine("  <Table ss:ExpandedColumnCount=\"26\" ss:ExpandedRowCount=\"500\" x:FullColumns=\"1\" x:FullRows=\"1\">");
        builder.AppendLine("   <Column ss:Width=\"140\"/>");
        builder.AppendLine("   <Column ss:Width=\"153\"/>");
        builder.AppendLine("   <Column ss:Width=\"123\"/>");
        builder.AppendLine("   <Column ss:Width=\"123\"/>");
        builder.AppendLine("   <Column ss:Width=\"123\"/>");
        builder.AppendLine("   <Column ss:Width=\"130\"/>");
        for (int i = 0; i < 20; i++)
            builder.AppendLine("   <Column ss:Width=\"80\"/>");

        builder.AppendLine("   <Row>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">House</Data></Cell>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">Time</Data></Cell>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">Start Bank</Data></Cell>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">Final Bank</Data></Cell>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">Tips</Data></Cell>");
        builder.AppendLine("    <Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">Results</Data></Cell>");
        AppendEmptyBlackCells(builder, 20);
        builder.AppendLine("   </Row>");

        for (int rowNumber = 2; rowNumber <= 500; rowNumber++)
        {
            bool darkRow = rowNumber % 2 == 0;
            string suffix = darkRow ? "Dark" : "Black";
            HistoryEntry? entry = rowNumber - 2 < entryList.Count ? entryList[rowNumber - 2] : null;

            builder.AppendLine("   <Row>");

            if (entry != null)
            {
                builder.AppendLine($"    <Cell ss:StyleID=\"house{suffix}\"><Data ss:Type=\"String\">{EscapeXml(entry.House)}</Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"time{suffix}\"><Data ss:Type=\"String\">{EscapeXml(entry.Timestamp)}</Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"start{suffix}\"><Data ss:Type=\"String\">{EscapeXml(entry.StartingBank)}</Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"final{suffix}\"><Data ss:Type=\"String\">{EscapeXml(entry.FinalBank)}</Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"tips{suffix}\"><Data ss:Type=\"String\">{EscapeXml(entry.Tips)}</Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"{StyleIdForResult(entry.Result, darkRow)}\"><Data ss:Type=\"String\">{EscapeXml(entry.Result)}</Data></Cell>");
            }
            else
            {
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
                builder.AppendLine($"    <Cell ss:StyleID=\"base{suffix}\"><Data ss:Type=\"String\"></Data></Cell>");
            }

            AppendEmptyBlackCells(builder, 20);
            builder.AppendLine("   </Row>");
        }

        builder.AppendLine("  </Table>");
        builder.AppendLine(" </Worksheet>");
        builder.AppendLine("</Workbook>");
        return builder.ToString();
    }

    private static string BuildSpreadsheetStyleXml(string id, string backgroundHex, string foregroundHex, bool centered, bool bold = false)
    {
        string alignment = centered ? "<Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/>" : string.Empty;
        string font = bold
            ? $"<Font ss:Bold=\"1\" ss:Color=\"{foregroundHex}\"/>"
            : $"<Font ss:Color=\"{foregroundHex}\"/>";

        return
            $"  <Style ss:ID=\"{id}\">" +
            "<Borders>" +
            "<Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#404040\"/>" +
            "<Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#404040\"/>" +
            "<Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#404040\"/>" +
            "<Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#404040\"/>" +
            "</Borders>" +
            $"<Interior ss:Color=\"{backgroundHex}\" ss:Pattern=\"Solid\"/>" +
            alignment +
            font +
            "</Style>";
    }

    private static string EscapeXml(string value)
    {
        value ??= string.Empty;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private void DrawHistoryEntryLink(HistoryEntry entry, int rowIndex)
    {
        string label = string.IsNullOrWhiteSpace(entry.Timestamp) ? "--" : entry.Timestamp;
        DrawClickableInlineText(label, GoldColor, () =>
        {
            selectedDealerHistoryEntry = entry;
            selectedDealerHistoryDetailId = $"detail_{rowIndex}_{entry.Timestamp}";
            showDealerHistoryDetailWindow = true;
        }, true, "Click to view details");
    }

    private void DrawDealerShiftSummaryWindow()
    {
        if (!showDealerShiftSummaryWindow)
            return;

        if (ImGui.Begin("Current Shift Summary###DealerShiftSummary", ref showDealerShiftSummaryWindow, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var shiftEntries = GetCurrentDealerShiftEntries();
            DateTime? shiftStart = TryGetCurrentDealerShiftStart();
            string checkpointState = GetDealerCheckpointState();

            ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
            ImGui.TextUnformatted("♯ Current Shift Summary");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            ImGui.TextUnformatted($"House: {(string.IsNullOrWhiteSpace(houseInput) ? "Not set" : houseInput.Trim())}");
            ImGui.TextUnformatted($"Start Bank: {FormatNumber(startingBankValue)}");
            ImGui.TextUnformatted($"Final Bank: {FormatNumber(finalBankValue)}");
            ImGui.TextUnformatted($"Tips: {FormatNumber(tipsValue)}");
            ImGui.TextUnformatted($"Result: {GetCurrentResultText()}");
            ImGui.TextUnformatted($"Status: {checkpointState}");
            ImGui.TextUnformatted($"Started: {(shiftStart.HasValue ? shiftStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : "--")}");
            ImGui.TextUnformatted($"Elapsed: {(shiftStart.HasValue ? FormatElapsed(DateTime.Now - shiftStart.Value) : "--")}");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
            ImGui.TextUnformatted("Shift Timeline");
            ImGui.PopStyleColor();

            if (shiftEntries.Count == 0)
            {
                ImGui.TextDisabled("No active shift entries.");
            }
            else
            {
                if (ImGui.BeginChild("##DealerShiftEntries", new Vector2(520f, 180f), true))
                {
                    foreach (var entry in shiftEntries)
                    {
                        ImGui.TextUnformatted($"{entry.Timestamp} | {entry.House} | {entry.Result} | Final {entry.FinalBank} | Tips {entry.Tips}");
                    }
                    ImGui.EndChild();
                }
            }
        }

        ImGui.End();
    }

    private void DrawDealerHistoryDetailWindow()
    {
        if (!showDealerHistoryDetailWindow || selectedDealerHistoryEntry == null)
            return;

        string windowTitle = $"History Entry Details##{selectedDealerHistoryDetailId}";
        if (ImGui.Begin(windowTitle, ref showDealerHistoryDetailWindow, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var entry = selectedDealerHistoryEntry;
            string entryType = IsCheckpointEntry(entry) ? "Checkpoint" : "Session Entry";
            string relatedShift = GetRelatedShiftLabel(entry);

            ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
            ImGui.TextUnformatted("♯ Dealer History Details");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            ImGui.TextUnformatted($"Type: {entryType}");
            ImGui.TextUnformatted($"Related Shift: {relatedShift}");
            ImGui.TextUnformatted($"House: {entry.House}");
            ImGui.TextUnformatted($"Time: {entry.Timestamp}");
            ImGui.TextUnformatted($"Start Bank: {entry.StartingBank}");
            ImGui.TextUnformatted($"Final Bank: {entry.FinalBank}");
            ImGui.TextUnformatted($"Tips: {entry.Tips}");
            ImGui.TextUnformatted($"Result: {entry.Result}");
        }

        ImGui.End();

        if (!showDealerHistoryDetailWindow)
        {
            selectedDealerHistoryEntry = null;
            selectedDealerHistoryDetailId = string.Empty;
        }
    }

    private string GetRelatedShiftLabel(HistoryEntry entry)
    {
        var timestamp = ParseTimestamp(entry.Timestamp);
        var history = GetCurrentHistory()
            .Where(IsCheckpointEntry)
            .OrderBy(x => ParseTimestamp(x.Timestamp))
            .ToList();

        DateTime? shiftStart = null;
        foreach (var checkpoint in history)
        {
            DateTime checkpointTime = ParseTimestamp(checkpoint.Timestamp);
            if (checkpointTime > timestamp)
                break;

            if (checkpoint.Result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
                shiftStart = checkpointTime;
            else if (checkpoint.Result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
                shiftStart = null;
        }

        return shiftStart.HasValue ? shiftStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : "No active shift";
    }

    private List<HistoryEntry> GetCurrentDealerShiftEntries()
    {
        var history = GetCurrentHistory()
            .OrderBy(x => ParseTimestamp(x.Timestamp))
            .ToList();

        int startIndex = -1;
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
                startIndex = i;
            else if (history[i].Result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
                startIndex = -1;
        }

        if (startIndex < 0)
            return new List<HistoryEntry>();

        return history.Skip(startIndex).ToList();
    }

    private void TryAutoDailyDealerBackup()
    {
        if (!Plugin.Configuration.DealerAutoDailyBackup)
            return;

        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (string.Equals(Plugin.Configuration.DealerLastDailyBackupDate, today, StringComparison.OrdinalIgnoreCase))
            return;

        if (GetCurrentHistory().Count == 0)
            return;

        AddDebugLog("UI", "Auto daily dealer backup triggered.");
        AddBackupHistoryEntry();
        ExportCurrentHistory(false, true, "DailyBackup");
        ExportCurrentHistory(true, true, "DailyBackup");
        Plugin.Configuration.DealerLastDailyBackupDate = today;
        Plugin.Configuration.Save();
    }

    private string GetDealerBackupDirectory()
    {
        string configured = Plugin.Configuration.DealerBackupDirectory?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return configured;

        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }
}
