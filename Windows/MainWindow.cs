using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
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

    private DateTime addBankButtonInvalidUntilUtc = DateTime.MinValue;

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

    private sealed class StatusTextTransitionState
    {
        public string CurrentText = string.Empty;
        public string PendingTargetText = string.Empty;
        public DateTime TransitionStartedUtc = DateTime.MinValue;
    }

    private readonly Dictionary<string, StatusTextTransitionState> animatedStatusTexts = new(StringComparer.OrdinalIgnoreCase);

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
    private bool showDealerTurnLogHistoryWindow;
    private bool showDealerTableHistoryView;
    private bool showDealerTurnLogPatternTrainerWindow;
    private DateTime nextDealerTurnLogPartySyncUtc = DateTime.MinValue;
    private DateTime dealerTurnLogExportStatusUntilUtc = DateTime.MinValue;
    private string dealerTurnLogExportStatusText = string.Empty;
    private string dealerTurnLogExportStatusPath = string.Empty;
    private bool dealerTurnLogExportStatusFailed;
    private string dealerTurnLogPendingTradePlayer = string.Empty;
    private bool dealerTurnLogPendingTradeInitiatedByLocal;
    private DateTime dealerTurnLogPendingTradeUntilUtc = DateTime.MinValue;
    private DateTime dealerTurnLogsRequirePartyMessageUntilUtc = DateTime.MinValue;
    private bool dealerTurnLogPartyPreviouslyActive;
    private readonly Dictionary<string, DateTime> dealerTurnLogLastSeenUtc = new(StringComparer.OrdinalIgnoreCase);
    private HistoryEntry? selectedDealerHistoryEntry;
    private string selectedDealerHistoryDetailId = string.Empty;
    private bool showHistoryExportDirectoryPopup;
    private string historyExportDirectoryInput = string.Empty;
    private bool openDealerTurnLogDirectoryBrowserNextFrame;
    private bool openDealerTurnLogHouseEditPopupNextFrame;
    private DealerTurnLogEntry? dealerTurnLogEditingHouseEntry;
    private string dealerTurnLogHouseEditInput = string.Empty;
    private string dealerTurnLogBulkHouseInput = string.Empty;
    private string dealerTurnLogSortBy = "Most Recent";
    private string dealerTurnLogSearchInput = string.Empty;
    private string dealerTurnLogPatternSampleInput = string.Empty;
    private readonly HashSet<string> selectedDealerTurnLogRowKeys = new(StringComparer.OrdinalIgnoreCase);
    private string lastDealerTurnLogSelectedRowKey = string.Empty;
    private string dealerTurnLogDirectoryBrowserCurrentPath = string.Empty;
    private string dealerTurnLogDirectoryBrowserSelectedPath = string.Empty;
    private string dealerTurnLogDirectoryBrowserSearchInput = string.Empty;
    private string dealerTurnLogDirectoryBrowserErrorText = string.Empty;
    private readonly Dictionary<string, string> dealerTurnLogDeletedState = new(StringComparer.OrdinalIgnoreCase);
    private bool dealerTurnLogDeletedStateLoaded;
    private readonly HashSet<string> pendingDealerTurnLogChatFileWrites = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> pendingDealerTurnLogTradeFileWrites = new(StringComparer.OrdinalIgnoreCase);
    private DateTime nextDealerTurnLogFileFlushUtc = DateTime.MinValue;
    private bool dealerTurnLogConfigurationDirty;
    private DateTime nextDealerTurnLogConfigurationSaveUtc = DateTime.MinValue;
    private readonly HashSet<string> collapsedHistoryDateGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> collapsedDealerTableHistoryDateGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> openDealerTurnLogMatchHistoryWindowKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool showDealerTurnLogPatternValidationPopup;
    private string dealerTurnLogPatternValidationMessage = string.Empty;
    private Vector2 dealerTurnLogPatternValidationPopupScreenPos = Vector2.Zero;

    private DateTime doubleDownPromptUntilUtc = DateTime.MinValue;
    private DateTime doubleDownPendingUntilUtc = DateTime.MinValue;
    private DateTime blackjackPendingUntilUtc = DateTime.MinValue;
    private DateTime doubleDownDoneUntilUtc = DateTime.MinValue;
    private DateTime blackjackDoneUntilUtc = DateTime.MinValue;
    private DateTime autoTrackRequireDealerUntilUtc = DateTime.MinValue;
    private DateTime dealerRegisterTipsFeedbackUntilUtc = DateTime.MinValue;

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
    private static readonly Regex TradeRequestFromPlayerRegex = new(@"^(?<name>.+?) (?:would like|wishes) to trade with you\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeRequestFromLocalRegex = new(@"^(?:You (?:request|requested) a trade with|Trade request sent to) (?<name>.+?)\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeAwaitingConfirmationRegex = new(@"^Awaiting trade confirmation from (?<name>.+?)\.{3}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeSentNamedRegex = new(@"^You (?:trade|traded|give|gave|send|sent|hand over|handed over) (?<amount>[\d\.,]+) gil to (?<name>.+?)\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeReceivedNamedRegex = new(@"^(?<name>.+?) (?:trade|traded|give|gave|send|sent|hand|handed)s? you (?<amount>[\d\.,]+) gil\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeSentPendingRegex = new(@"^You (?:trade|traded|give|gave|send|sent|hand over|handed over) (?<amount>[\d\.,]+) gil\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeReceivedPendingRegex = new(@"^You (?:receive|received|got|get) (?<amount>[\d\.,]+) gil(?: from (?<name>.+?))?\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeReceivedPassiveRegex = new(@"^(?<name>.+?) (?:receive|received|got|get)s? (?<amount>[\d\.,]+) gil from you\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeOfferLocalNamedRegex = new(@"^You (?:offer|offered|offers|put up|puts up|add|added|adds|set|sets) (?<amount>[\d\.,]+) gil(?: for trade)?(?: to (?<name>.+?))?\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeOfferPlayerNamedRegex = new(@"^(?<name>.+?) (?:offer|offered|offers|put up|puts up|add|added|adds|set|sets) (?<amount>[\d\.,]+) gil(?: for trade)?(?: to you)?\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeGiveLocalNamedRegex = new(@"^You (?:give|gave|hand|handed|hands|pass|passed|passes) (?<amount>[\d\.,]+) gil to (?<name>.+?)\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeGivePlayerNamedRegex = new(@"^(?<name>.+?) (?:give|gave|gives|hand|handed|hands|pass|passed|passes) you (?<amount>[\d\.,]+) gil\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TradeCancelRegex = new(@"^(?:Trade cancelled\.?|Trade canceled\.?|The trade was cancelled\.?|The trade was canceled\.?|Trade request declined\.?|Trade request canceled\.?|Trade request cancelled\.?|(?<name>.+?) has declined the trade\.?|(?<name>.+?) has withdrawn the trade request\.?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string[] BlackjackMultiplierLabels = { "1.0x", "1.3x", "1.7x", "1.5x", "2.0x", "2.5x", "3.0x" };
    private static readonly int[] BlackjackMultiplierTenths = { 10, 13, 17, 15, 20, 25, 30 };
    private static readonly TimeZoneInfo EstTimeZone = ResolveEstTimeZone();

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

    public void OpenDealerTurnLogHistoryWindow()
    {
        IsOpen = true;
        showDealerTurnLogHistoryWindow = true;
    }

    public void SetDealerTurnLogsEnabledFromCommand(bool enabled)
    {
        IsOpen = true;
        if (IsPlayerMode)
            SetMode("Dealer");

        SetDealerTurnLogsEnabledInternal(enabled);
    }

    public void StartDealerShiftFromCommand(BigInteger startingBank, string house)
    {
        IsOpen = true;
        if (IsPlayerMode)
            SetMode("Dealer");

        startingBankValue = BigInteger.Max(BigInteger.Zero, startingBank);
        startingBankInput = FormatNumber(startingBankValue);
        finalBankValue = startingBankValue;
        finalBankInput = FormatNumber(finalBankValue);

        if (!string.IsNullOrWhiteSpace(house))
            houseInput = house.Trim();

        SaveCurrentProfileValues();
        AddDealerCheckpoint("Start Shift");
        AddHistoryEntry();
    }

    public void EndDealerShiftFromCommand(BigInteger finalBank, string house)
    {
        IsOpen = true;
        if (IsPlayerMode)
            SetMode("Dealer");

        finalBankValue = BigInteger.Max(BigInteger.Zero, finalBank);
        finalBankInput = FormatNumber(finalBankValue);

        if (!string.IsNullOrWhiteSpace(house))
            houseInput = house.Trim();

        SaveCurrentProfileValues();
        AddDealerCheckpoint("End Shift");
        AddHistoryEntry();
    }

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
        Plugin.Framework.Update += OnFrameworkUpdate;
        AddDebugLog("INIT", "MainWindow initialized.");
    }

    public void Dispose()
    {
        FlushPendingDealerTurnLogWrites(force: true);
        FlushPendingDealerTurnLogConfigurationSave(force: true);
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    public override void Draw()
    {
        DrawProfilesSection();

        if (IsPlayerMode)
            ImGui.Dummy(new Vector2(0f, 6f));
        else
            ImGui.Spacing();
        ImGui.Separator();
        if (IsPlayerMode)
            ImGui.Dummy(new Vector2(0f, 6f));
        else
            ImGui.Spacing();

        DrawBankFields();

        if (IsDealerMode)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawMessageSection();
        }

        if (IsPlayerMode)
            ImGui.Dummy(new Vector2(0f, 8f));
        else
            ImGui.Spacing();
        ImGui.Separator();
        if (IsPlayerMode)
            ImGui.Dummy(new Vector2(0f, 8f));
        else
            ImGui.Spacing();

        DrawHistorySection();

        if (openDealerTurnLogDirectoryBrowserNextFrame)
        {
            openDealerTurnLogDirectoryBrowserNextFrame = false;
            OpenDealerTurnLogDirectoryBrowser();
        }

        DrawDealerTurnLogDirectoryBrowserPopup();

        if (openDealerTurnLogHouseEditPopupNextFrame)
        {
            openDealerTurnLogHouseEditPopupNextFrame = false;
            ImGui.OpenPopup("##DealerTurnLogHouseEditPopup");
        }

        DrawDealerTurnLogHouseEditPopup();

        if (IsDealerMode)
        {
            TryAutoDailyDealerBackup();
            DrawDealerShiftSummaryWindow();
            DrawDealerHistoryDetailWindow();
            DrawDealerTurnLogHistoryWindow();
            DrawDealerTurnLogPatternTrainerWindow();
            DrawDealerTurnLogPatternInvalidPopup();
            DrawDealerTurnLogMatchHistoryWindows();
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

        const float dealerStatusSeparatorWidth = 14f;
        const float dealerStatusColumnWidth = 430f;

        if (!ImGui.BeginTable("##DealerBankFieldsTable", 7, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 95f);
        ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("StatusSeparator", ImGuiTableColumnFlags.WidthFixed, dealerStatusSeparatorWidth);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, dealerStatusColumnWidth);

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

        ImGui.TableSetColumnIndex(5);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(6);
        DrawDealerTrackerStatusHeader(dealerStatusColumnWidth);

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

        ImGui.TableSetColumnIndex(4);
        string registerTipsButtonText = DateTime.UtcNow < dealerRegisterTipsFeedbackUntilUtc
            ? "Registered!"
            : "Register Tips";
        if (DrawStyledBoldButton(registerTipsButtonText, "DealerRegisterTipsButton", new Vector2(144f, 0f), Hex("#b3446b"), WhiteText))
        {
            if (RegisterDealerTipsFromCurrentField())
                dealerRegisterTipsFeedbackUntilUtc = DateTime.UtcNow.AddSeconds(3);
        }

        ImGui.TableSetColumnIndex(5);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(6);
        DrawDealerTrackerStatusTopLine(dealerStatusColumnWidth);

        ImGui.TableNextRow();

        DrawEditableHouseSuggestCell(0, 1, "House:", "##HouseInput", ref houseInput, 145f);

        ImGui.TableSetColumnIndex(2);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Quick tips:");

        ImGui.TableSetColumnIndex(3);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2f, ImGui.GetStyle().ItemSpacing.Y));
        const float quickTipButtonWidth = 47f;
        if (DrawStyledBoldButton("+100K", "DealerTips100KButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip100Color, WhiteText))
            IncrementDealerTips(100_000, registerHistoryEntry: true);
        ImGui.SameLine();
        if (DrawStyledBoldButton("+500K", "DealerTips500KButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip500Color, WhiteText))
            IncrementDealerTips(500_000, registerHistoryEntry: true);
        ImGui.SameLine();
        if (DrawStyledBoldButton("+1M", "DealerTips1MButton", new Vector2(quickTipButtonWidth, 0f), DealerQuickTip1MColor, WhiteText))
            IncrementDealerTips(1_000_000, registerHistoryEntry: true);
        ImGui.PopStyleVar();

        ImGui.TableSetColumnIndex(4);
        ImGui.Dummy(Vector2.Zero);

        ImGui.TableSetColumnIndex(5);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(6);
        DrawDealerTrackerStatusBottomLine(dealerStatusColumnWidth);

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
            ImGui.TableSetupColumn("PlayerActionColumn", ImGuiTableColumnFlags.WidthFixed, 252f);

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            DrawPlayerLeftFields();

            ImGui.TableSetColumnIndex(1);
            DrawPlayerCenterFields();

            ImGui.TableSetColumnIndex(2);
            DrawPlayerActionButtons();

            ImGui.EndTable();
        }

        ImGui.Dummy(new Vector2(0f, 8f));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0f, 8f));

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
        const float addBankFieldWidth = 138f;
        const float dividerWidth = 14f;

        if (!ImGui.BeginTable("##PlayerActionButtonsTable", 3, ImGuiTableFlags.SizingFixedFit))
            return;

        ImGui.TableSetupColumn("PlayerActionButtonsColumn", ImGuiTableColumnFlags.WidthFixed, actionButtonWidth);
        ImGui.TableSetupColumn("PlayerActionDividerColumn", ImGuiTableColumnFlags.WidthFixed, dividerWidth);
        ImGui.TableSetupColumn("PlayerAddBankColumn", ImGuiTableColumnFlags.WidthFixed, addBankFieldWidth);

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (DrawStyledBoldButton("Save", "SaveToHistoryButton", new Vector2(actionButtonWidth, 0f), CopyButtonColor))
            AddHistoryEntry();

        ImGui.TableSetColumnIndex(1);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(addBankFieldWidth);
        bool addBankConfirmedByEnter = ImGui.InputText("##PlayerAddBankInput", ref bankingInput, 64, ImGuiInputTextFlags.EnterReturnsTrue);
        bool addBankInputActive = ImGui.IsItemActive();
        bool addBankConfirmedByFocusLoss = ImGui.IsItemDeactivatedAfterEdit();
        if (addBankConfirmedByEnter || addBankConfirmedByFocusLoss)
        {
            bankingValue = ParseBankValue(bankingInput);
            bankingInput = bankingValue > BigInteger.Zero ? FormatNumber(bankingValue) : string.Empty;
            bankingInputHasUserEdited = bankingValue > BigInteger.Zero;
        }

        if (!addBankInputActive && string.IsNullOrWhiteSpace(bankingInput))
        {
            Vector2 placeholderPos = ImGui.GetItemRectMin() + new Vector2(8f, 3f);
            ImGui.GetWindowDrawList().AddText(placeholderPos, ImGui.GetColorU32(LossColor), "Type bank here");
        }

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (DrawStyledBoldButton("Clear", "PlayerClearButton", new Vector2(actionButtonWidth, 0f), LossButtonColor, WhiteText))
            ClearCurrentInputs();

        ImGui.TableSetColumnIndex(1);
        DrawMiniVerticalSeparator();

        ImGui.TableSetColumnIndex(2);
        string addBankButtonText = DateTime.UtcNow < addBankButtonInvalidUntilUtc ? "Type in first" : "Add Bank";
        if (DrawStyledBoldButton(addBankButtonText, "PlayerAddBankButton", new Vector2(addBankFieldWidth, 0f), CopyButtonColor, WhiteText))
        {
            bankingValue = ParseBankValue(bankingInput);
            bankingInput = bankingValue > BigInteger.Zero ? FormatNumber(bankingValue) : string.Empty;
            bankingInputHasUserEdited = bankingValue > BigInteger.Zero;

            if (bankingValue <= BigInteger.Zero)
            {
                addBankButtonInvalidUntilUtc = DateTime.UtcNow.AddSeconds(3);
            }
            else
            {
                ApplyBankingAdjustment();
            }
        }

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
        const float trackerStatusColumnWidth = 520f;

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
            trackerStatusColumnWidth,
            "Player");

        // Row 3
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.Dummy(Vector2.Zero);

        if (playerAutoTrackEnabled && string.IsNullOrWhiteSpace(trackedDealerInput))
        {
            playerAutoTrackEnabled = false;
            AddDebugLog("AUTO", "Auto Track turned OFF automatically because no tracked dealer name is set.");
            SaveCurrentProfileValues();
        }

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
            trackerStatusColumnWidth,
            "Player");

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

    private static BigInteger CalculateBlackjackNetPayout(BigInteger betAmount, int multiplierIndex)
    {
        multiplierIndex = Math.Clamp(multiplierIndex, 0, BlackjackMultiplierTenths.Length - 1);
        BigInteger grossPayout = (betAmount * BlackjackMultiplierTenths[multiplierIndex]) / 10;
        BigInteger netPayout = grossPayout - betAmount;
        return netPayout < BigInteger.Zero ? BigInteger.Zero : netPayout;
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

            BigInteger netPayout = CalculateBlackjackNetPayout(betValue, multiplierIndex);
            BigInteger previousCurrentBank = finalBankValue;
            finalBankValue += netPayout;
            lastPlayerBankChangeValue = finalBankValue - previousCurrentBank;
            finalBankInput = FormatNumber(finalBankValue);

            RemoveMostRecentHistoryEntry();
            AddBlackjackHistoryEntry(netPayout);

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
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
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
        float availableWidth,
        string idPrefix = "Player")
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 14f;

        string leftValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(leftLabel)}", GetTrackerIndicatorText(leftState));
        string rightValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(rightLabel)}", GetTrackerIndicatorText(rightState));
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
        float availableWidth,
        string idPrefix = "Player")
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 18f;

        string leftValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(leftLabel)}", GetTrackerIndicatorText(leftState));
        string middleValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(middleLabel)}", GetTrackerIndicatorText(middleState));
        string rightValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(rightLabel)}", GetTrackerIndicatorText(rightState));

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
        float availableWidth,
        string idPrefix = "Player")
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 18f;

        string leftValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(leftLabel)}", GetTrackerIndicatorText(leftState));
        string middleValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(middleLabel)}", GetTrackerIndicatorText(middleState));

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

    private void DrawDealerTrackerStatusHeader(float availableWidth)
    {
        const string text = "Tracker Status:";
        const float contentPad = 0f;

        Vector2 start = ImGui.GetCursorScreenPos();
        Vector2 textSize = ImGui.CalcTextSize(text);
        float rowHeight = ImGui.GetFrameHeight();
        float textY = start.Y + MathF.Max(0f, (rowHeight - textSize.Y) * 0.5f);

        var drawList = ImGui.GetWindowDrawList();
        uint color = ImGui.ColorConvertFloat4ToU32(WhiteText);
        Vector2 textPos = new(start.X + contentPad, textY);
        drawList.AddText(textPos, color, text);
        drawList.AddText(new Vector2(textPos.X + 1f, textPos.Y), color, text);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availableWidth, rowHeight));
    }

    private void DrawDealerTrackerStatusTopLine(float availableWidth)
    {
        bool turnLogsEnabled = GetDealerTurnLogsEnabled();
        int partyPlayerCount = GetDealerPartyPlayerCount();
        bool inParty = partyPlayerCount > 0;

        DrawDealerStatusLineTwo(
            "Players:",
            GoldColor,
            inParty ? $"{partyPlayerCount.ToString(CultureInfo.InvariantCulture)}/8" : "Not in party",
            inParty ? Hex("#42b6f5") : LossColor,
            "Log History:",
            Hex("#00c0c7"),
            turnLogsEnabled ? "[✓]" : "[X]",
            turnLogsEnabled ? ProfitColor : LossColor,
            availableWidth,
            "Dealer");
    }

    private void DrawDealerTrackerStatusBottomLine(float availableWidth)
    {
        bool turnLogsEnabled = GetDealerTurnLogsEnabled();

        DrawDealerStatusLineTwo(
            "Chat Log:",
            ProfitColor,
            turnLogsEnabled ? "[✓]" : "[X]",
            turnLogsEnabled ? ProfitColor : LossColor,
            "Trades Log:",
            GoldColor,
            turnLogsEnabled ? "[✓]" : "[X]",
            turnLogsEnabled ? ProfitColor : LossColor,
            availableWidth,
            "Dealer");
    }

    private void DrawDealerStatusLineTwo(
        string leftLabel,
        Vector4 leftLabelColor,
        string leftValue,
        Vector4 leftValueColor,
        string rightLabel,
        Vector4 rightLabelColor,
        string rightValue,
        Vector4 rightValueColor,
        float availableWidth,
        string idPrefix = "Dealer")
    {
        const float highlightPadX = 3f;
        const float highlightPadY = 1f;
        const float segmentGap = 4f;
        const float sectionGap = 18f;

        string animatedLeftValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(leftLabel)}", leftValue);
        string animatedRightValue = GetAnimatedStatusText($"{idPrefix}_{BuildStatusAnimationKey(rightLabel)}", rightValue);

        float sectionWidth = MathF.Max(175f, (availableWidth - sectionGap) * 0.5f);
        Vector2 start = ImGui.GetCursorScreenPos();
        float centerY = start.Y + (ImGui.GetFrameHeight() * 0.5f);
        float textBaseY = centerY - (ImGui.GetTextLineHeight() * 0.5f);

        DrawDealerStatusSection(start.X, textBaseY, sectionWidth, leftLabel, leftLabelColor, animatedLeftValue, leftValueColor, highlightPadX, highlightPadY, segmentGap);
        DrawDealerStatusSection(start.X + sectionWidth + sectionGap, textBaseY, sectionWidth, rightLabel, rightLabelColor, animatedRightValue, rightValueColor, highlightPadX, highlightPadY, segmentGap);

        ImGui.Dummy(new Vector2(availableWidth, ImGui.GetFrameHeight()));
    }

    private static string BuildStatusAnimationKey(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "status";

        var sb = new StringBuilder(label.Length);
        foreach (char c in label)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.Length == 0 ? "status" : sb.ToString();
    }

    private static bool IsAnimatedStatusPlaceholder(string text)
    {
        return string.Equals(text, ".", StringComparison.Ordinal) ||
               string.Equals(text, "..", StringComparison.Ordinal) ||
               string.Equals(text, "...", StringComparison.Ordinal);
    }

    private string GetAnimatedStatusText(string key, string targetText)
    {
        targetText ??= string.Empty;

        if (!animatedStatusTexts.TryGetValue(key, out var state))
        {
            state = new StatusTextTransitionState
            {
                CurrentText = targetText
            };
            animatedStatusTexts[key] = state;
            return state.CurrentText;
        }

        if (string.IsNullOrEmpty(state.CurrentText))
            state.CurrentText = targetText;

        if (string.IsNullOrEmpty(state.PendingTargetText))
        {
            if (!string.Equals(state.CurrentText, targetText, StringComparison.Ordinal))
            {
                state.PendingTargetText = targetText;
                state.TransitionStartedUtc = DateTime.UtcNow;
            }

            return state.CurrentText;
        }

        if (!string.Equals(state.PendingTargetText, targetText, StringComparison.Ordinal))
        {
            state.PendingTargetText = targetText;
            state.TransitionStartedUtc = DateTime.UtcNow;
        }

        double elapsedSeconds = (DateTime.UtcNow - state.TransitionStartedUtc).TotalSeconds;
        if (elapsedSeconds < 1d)
            return ".";
        if (elapsedSeconds < 2d)
            return "..";
        if (elapsedSeconds < 3d)
            return "...";

        state.CurrentText = state.PendingTargetText;
        state.PendingTargetText = string.Empty;
        state.TransitionStartedUtc = DateTime.MinValue;
        return state.CurrentText;
    }
    private void DrawDealerStatusSection(
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

    private int GetDealerPartyPlayerCount()
    {
        int count = 0;

        for (int i = 0; i < Plugin.PartyList.Length; i++)
        {
            var member = Plugin.PartyList[i];
            if (member == null)
                continue;

            string memberName = StripWorldSuffix(member.Name.TextValue ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(memberName))
                count++;
        }

        return count;
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
        uint playersBlueCol = ImGui.ColorConvertFloat4ToU32(Hex("#42b6f5"));

        const string openBracket = "[";
        const string closeBracket = "]";

        if (IsAnimatedStatusPlaceholder(text))
        {
            drawList.AddText(textPos, neutralCol, text);
            drawList.AddText(textPos + new Vector2(1f, 0f), neutralCol, text);
            return;
        }

        if (Regex.IsMatch(text, @"^\d+/\d+$"))
        {
            string[] parts = text.Split('/', 2);
            Vector2 pos = textPos;

            drawList.AddText(pos, playersBlueCol, parts[0]);
            drawList.AddText(pos + new Vector2(1f, 0f), playersBlueCol, parts[0]);
            pos.X += ImGui.CalcTextSize(parts[0]).X;

            drawList.AddText(pos, neutralCol, "/");
            pos.X += ImGui.CalcTextSize("/").X;

            drawList.AddText(pos, playersBlueCol, parts[1]);
            drawList.AddText(pos + new Vector2(1f, 0f), playersBlueCol, parts[1]);
            return;
        }

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
        sb.AppendLine($"Dealer Turn Logs Enabled: {GetDealerTurnLogsEnabled()}");
        sb.AppendLine($"Dealer Turn Log Rows: {GetDealerTurnLogEntries().Count}");
        sb.AppendLine($"Pending Trade Player: {dealerTurnLogPendingTradePlayer}");
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
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private void DrawHistoryExportDirectoryPopup(Vector2 buttonMin)
    {
        const string popupId = "##HistoryExportDirectoryPopupInline";
        const float popupWidth = 560f;

        ImGui.SetNextWindowPos(new Vector2(buttonMin.X - popupWidth - 6f, buttonMin.Y), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0f), ImGuiCond.Appearing);

        if (ImGui.BeginPopup(popupId))
        {
            ImGui.TextUnformatted("Choose where TXT/CSV history exports will be saved.");
            ImGui.Spacing();

            if (string.IsNullOrWhiteSpace(historyExportDirectoryInput))
                historyExportDirectoryInput = Plugin.Configuration.HistoryExportDirectory ?? string.Empty;

            const float browseButtonWidth = 30f;
            float buttonHeight = ImGui.GetTextLineHeight() + 4f;
            float fieldWidth = MathF.Max(180f, popupWidth - browseButtonWidth - ImGui.GetStyle().ItemSpacing.X - 36f);

            if (DrawFolderBrowseIconButton("HistoryExportDirectoryPopupBrowseButton", new Vector2(browseButtonWidth, buttonHeight), Hex("#9784e8")))
                RequestOpenDealerTurnLogDirectoryBrowser();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Browse folders");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(fieldWidth);
            if (ImGui.InputText("##HistoryExportDirectoryInput", ref historyExportDirectoryInput, 512))
            {
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            if (DrawStyledBoldButton("Desktop", "HistoryExportPopupDesktopButton", new Vector2(84f, 0f), UtilityButtonColor, WhiteText))
            {
                historyExportDirectoryInput = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (DrawStyledBoldButton("Documents", "HistoryExportPopupDocumentsButton", new Vector2(94f, 0f), UtilityButtonColor, WhiteText))
            {
                historyExportDirectoryInput = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (DrawStyledBoldButton("Backup Folder", "HistoryExportPopupBackupButton", new Vector2(108f, 0f), UtilityButtonColor, WhiteText))
            {
                historyExportDirectoryInput = Plugin.Configuration.DealerBackupDirectory ?? string.Empty;
                Plugin.Configuration.HistoryExportDirectory = historyExportDirectoryInput;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (DrawStyledBoldButton("Clear", "HistoryExportPopupClearButton", new Vector2(70f, 0f), DangerButtonColor, WhiteText))
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
            string fileName = $"GambaBank_{safePrefix}_{GetEstNow():yyyyMMdd_HHmmss}.{extension}";
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

        if (DrawFolderBrowseIconButton("HistoryExportDirectoryButton", new Vector2(30f, ImGui.GetTextLineHeight() + 4f), Hex("#9784e8")))
        {
            historyExportDirectoryInput = Plugin.Configuration.HistoryExportDirectory ?? string.Empty;
            ImGui.OpenPopup("##HistoryExportDirectoryPopupInline");
        }

        Vector2 historyExportButtonMin = ImGui.GetItemRectMin();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Choose export directory");

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

        DrawHistoryExportDirectoryPopup(historyExportButtonMin);

        ImGui.Spacing();

        if (IsDealerMode)
        {
            DrawDealerPeriodSummary();
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
            DrawProfitsLossesSummary();
            ImGui.Spacing();
        }

        if (IsDealerMode && showDealerTableHistoryView)
        {
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
            DrawDealerTableHistoryControlsRow();
        }
        else
            DrawStandardHistoryControlsRow();

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

        if (IsDealerMode && showDealerTableHistoryView)
        {
            DrawDealerTableHistoryTable();
            return;
        }

        DrawStandardHistoryTable();
    }

    private void DrawStandardHistoryControlsRow()
    {
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

        if (IsDealerMode)
        {
            ImGui.SameLine();
            DrawDealerHistoryViewToggleButtons();
        }
        else
        {
            ImGui.Spacing();
            DrawProfitsLossesSummary();
        }
    }

    private void DrawDealerTableHistoryControlsRow()
    {
        ImGui.Text("Sort By:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(220f);
        if (ImGui.BeginCombo("##DealerTableHistorySortBy", dealerTurnLogSortBy))
        {
            DrawDealerTurnLogSortOption("Most Recent");
            DrawDealerTurnLogSortOption("Today");
            DrawDealerTurnLogSortOption("This Week");
            DrawDealerTurnLogSortOption("This Month");

            foreach (string houseOption in GetDealerTableHistoryHouseSortOptions())
                DrawDealerTurnLogSortOption(houseOption);

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Text("Search:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(210f);
        ImGui.InputText("##DealerTableHistorySearch", ref dealerTurnLogSearchInput, 128);

        ImGui.SameLine();
        DrawDealerHistoryViewToggleButtons();
    }

    private void DrawDealerHistoryViewToggleButtons()
    {
        const float toggleButtonWidth = 108f;
        const float trainerButtonWidth = 28f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float controlsWidth = toggleButtonWidth + (showDealerTableHistoryView ? trainerButtonWidth + spacing : 0f);

        float currentX = ImGui.GetCursorPosX();
        float rightAlignedX = MathF.Max(currentX, ImGui.GetWindowContentRegionMax().X - controlsWidth);
        ImGui.SetCursorPosX(rightAlignedX);

        if (showDealerTableHistoryView)
        {
            if (DrawDealerTurnLogPatternTrainerButton("DealerTurnLogPatternTrainerButton", new Vector2(trainerButtonWidth, 0f), Hex("#6b2da8"), WhiteText))
                showDealerTurnLogPatternTrainerWindow = true;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Open pattern trainer");

            ImGui.SameLine();
        }

        string toggleLabel = showDealerTableHistoryView ? "Dealer History" : "Table History";
        Vector4 toggleColor = showDealerTableHistoryView ? Hex("#272d9c") : Hex("#27579c");
        if (DrawStyledBoldButton(toggleLabel, "DealerHistoryTableToggleButton", new Vector2(toggleButtonWidth, 0f), toggleColor, WhiteText))
            showDealerTableHistoryView = !showDealerTableHistoryView;
    }

    private void DrawStandardHistoryTable()
    {
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
                bool isBankingResult = entry.Result.Contains("Banking", StringComparison.OrdinalIgnoreCase);
                bool isTipsResult = entry.Result.Contains("Tips", StringComparison.OrdinalIgnoreCase);
                Vector4 resultColor = isTipsResult
                    ? Hex("#b3446b")
                    : isBlackjackResult
                        ? BlackjackColor
                        : ParseSignedFormatted(entry.Result) < BigInteger.Zero ? LossColor : ProfitColor;

                if (isTipsResult)
                    DrawBoldColoredCell(entry.Result, resultColor);
                else if (isDoubleDownResult)
                    DrawDoubleDownColoredCell(entry.Result);
                else if (isBlackjackResult)
                    DrawBoldColoredCell(entry.Result, resultColor);
                else if (isBankingResult)
                    DrawBankingResultCell(entry.Result);
                else
                    DrawColoredCell(entry.Result, resultColor);
            }
        }

        ImGui.EndTable();
    }

    private string GetDealerTableHistoryGroupKey(string dateText) => $"DealerTable::{dateText}";

    private bool IsDealerTableHistoryDateGroupCollapsed(string dateText)
    {
        return collapsedDealerTableHistoryDateGroups.Contains(GetDealerTableHistoryGroupKey(dateText));
    }

    private void ToggleDealerTableHistoryDateGroup(string dateText)
    {
        string key = GetDealerTableHistoryGroupKey(dateText);
        if (!collapsedDealerTableHistoryDateGroups.Add(key))
            collapsedDealerTableHistoryDateGroups.Remove(key);
    }

    private void DrawDealerTableHistoryGroupHeader(string dateText, int columnCount)
    {
        bool collapsed = IsDealerTableHistoryDateGroupCollapsed(dateText);
        string arrow = collapsed ? "▶" : "▼";

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
        if (ImGui.Selectable($"{arrow}  {dateText}", false, ImGuiSelectableFlags.SpanAllColumns))
            ToggleDealerTableHistoryDateGroup(dateText);
        ImGui.PopStyleColor();

        for (int i = 1; i < columnCount; i++)
        {
            ImGui.TableSetColumnIndex(i);
            ImGui.TextUnformatted(string.Empty);
        }
    }

    private void DrawDealerTableHistoryTable()
    {
        var filteredEntries = GetFilteredDealerTableHistoryEntries();

        ImGui.Dummy(new Vector2(0f, 1.5f));

        string colorSeed = $"{dealerTurnLogSortBy}|{dealerTurnLogSearchInput}";
        var playerColorMap = BuildDealerTurnLogPlayerColorMap(filteredEntries, colorSeed);

        if (!ImGui.BeginTable(
                "##DealerTableHistoryTable",
                6,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                new Vector2(0f, 260f)))
            return;

        ImGui.TableSetupColumn("House", ImGuiTableColumnFlags.WidthFixed, 135f);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Joined", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Received", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Sent", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Match History", ImGuiTableColumnFlags.WidthFixed, 108f);
        ImGui.TableHeadersRow();

        if (filteredEntries.Count == 0)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextDisabled("No table history entries found.");
            for (int i = 1; i < 6; i++)
            {
                ImGui.TableSetColumnIndex(i);
                ImGui.TextUnformatted(string.Empty);
            }

            ImGui.EndTable();
            return;
        }

        string? currentDateGroup = null;
        foreach (var entry in filteredEntries)
        {
            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);

            string entryDate = GetDealerTurnLogBusinessDateLabel(entry);
            if (!string.Equals(currentDateGroup, entryDate, StringComparison.OrdinalIgnoreCase))
            {
                currentDateGroup = entryDate;
                DrawDealerTableHistoryGroupHeader(entryDate, 6);
            }

            if (IsDealerTableHistoryDateGroupCollapsed(entryDate))
                continue;

            Vector4 playerColor = playerColorMap.TryGetValue(entry.PlayerName, out var mappedPlayerColor)
                ? mappedPlayerColor
                : GetFallbackDealerTurnLogPlayerColor(entry.PlayerName);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            DrawDealerTurnLogHouseCell(entry);

            ImGui.TableSetColumnIndex(1);
            DrawLeftAlignedVerticallyCenteredColoredCell(entry.PlayerName, playerColor);

            ImGui.TableSetColumnIndex(2);
            DrawCenteredColoredCell(FormatTimestampForDisplay(entry.JoinedTimestamp), GoldColor);

            ImGui.TableSetColumnIndex(3);
            DrawCenteredColoredCell(string.IsNullOrWhiteSpace(entry.TotalReceivedGil) ? "0" : entry.TotalReceivedGil, ProfitColor);

            ImGui.TableSetColumnIndex(4);
            DrawCenteredColoredCell(string.IsNullOrWhiteSpace(entry.TotalSentGil) ? "0" : entry.TotalSentGil, LossColor);

            ImGui.TableSetColumnIndex(5);
            DrawDealerTurnLogMatchHistoryCell(entry, playerColor);
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
        ImGui.TextUnformatted("{Adjusted FinalBank} - {StartingBank} = {Results}");
        ImGui.Spacing();
        ImGui.TextUnformatted("Adjusted Final Bank = Final Bank - Tips");
        ImGui.TextUnformatted("Then: Adjusted Final Bank minus Starting Bank equals Results");
        ImGui.Spacing();
        DrawBoldTooltipLine("Final Bank: 5.000.000 | Tips: 1.000.000 => Adjusted Final Bank: 4.000.000 | Results depend on Starting Bank");
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


    private void DrawColoredCell(string? text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text ?? string.Empty);
        ImGui.PopStyleColor();
    }

    private float GetDealerTurnLogRowVerticalOffset()
    {
        return MathF.Max(0f, ((ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) * 0.5f) + 2.5f);
    }

    private void DrawCenteredColoredCell(string text, Vector4 color)
    {
        float verticalOffset = GetDealerTurnLogRowVerticalOffset();
        if (verticalOffset > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

        Vector2 textSize = ImGui.CalcTextSize(text ?? string.Empty);
        float centeredOffset = MathF.Max(0f, ((ImGui.GetColumnWidth() - textSize.X) * 0.5f) - ImGui.GetStyle().CellPadding.X);
        if (centeredOffset > 0f)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + centeredOffset);

        DrawColoredCell(text, color);
    }

    private void DrawLeftAlignedVerticallyCenteredColoredCell(string text, Vector4 color)
    {
        float verticalOffset = GetDealerTurnLogRowVerticalOffset();
        if (verticalOffset > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

        DrawColoredCell(text, color);
    }

    private void DrawCenteredHeaderCell(string text)
    {
        float verticalOffset = MathF.Max(0f, ((ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) * 0.5f) + 1f);
        if (verticalOffset > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

        Vector2 textSize = ImGui.CalcTextSize(text ?? string.Empty);
        float centeredOffset = MathF.Max(0f, ((ImGui.GetColumnWidth() - textSize.X) * 0.5f) - ImGui.GetStyle().CellPadding.X);
        if (centeredOffset > 0f)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + centeredOffset);

        ImGui.TextUnformatted(text);
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


    private void DrawBankingResultCell(string text)
    {
        const string suffix = "Banking";
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

        DrawBoldColoredText(suffixText, ProfitColor);
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


    private bool DrawDealerTurnLogPatternTrainerButton(string id, Vector2 size, Vector4 baseColor, Vector4 iconColor)
    {
        Vector2 actualSize = size;
        if (actualSize.X <= 0f)
            actualSize.X = 28f;
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
        uint drawColor = ImGui.ColorConvertFloat4ToU32(iconColor);

        float headRadius = MathF.Max(3.8f, MathF.Min(5.2f, (max.Y - min.Y) * 0.18f));
        Vector2 headCenter = new(center.X, min.Y + 7.5f);
        drawList.AddCircle(headCenter, headRadius, drawColor, 28, 1.9f);

        float bodyWidth = MathF.Min(16f, (max.X - min.X) * 0.62f);
        float bodyTop = headCenter.Y + headRadius + 3.5f;
        float bodyBottom = max.Y - 5.2f;
        float bodyLeft = center.X - (bodyWidth * 0.5f);
        float bodyRight = center.X + (bodyWidth * 0.5f);
        drawList.AddBezierCubic(
            new Vector2(bodyLeft, bodyBottom),
            new Vector2(bodyLeft, bodyTop + 1.5f),
            new Vector2(bodyRight, bodyTop + 1.5f),
            new Vector2(bodyRight, bodyBottom),
            drawColor,
            1.9f);
        drawList.AddLine(new Vector2(bodyLeft, bodyBottom), new Vector2(bodyRight, bodyBottom), drawColor, 1.9f);

        ImGui.PopStyleColor(4);
        return pressed;
    }

    private void DrawDealerTurnLogMatchHistoryCell(DealerTurnLogEntry entry, Vector4 playerColor)
    {
        _ = playerColor;
        string safeName = SanitizeFileName(entry.PlayerName);
        const float actionButtonWidth = 24f;

        float verticalOffset = GetDealerTurnLogRowVerticalOffset() - 1.0f;
        if (verticalOffset > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

        float columnWidth = ImGui.GetColumnWidth();
        float startX = ImGui.GetCursorPosX() + MathF.Max(0f, (columnWidth - actionButtonWidth) * 0.5f);
        ImGui.SetCursorPosX(startX);

        if (DrawExportDirectoryIconButton($"DealerTurnLogMatchHistory{safeName}{GetDealerTurnLogSelectionKey(entry)}", new Vector2(actionButtonWidth, 0f), CopyButtonColor))
            openDealerTurnLogMatchHistoryWindowKeys.Add(GetDealerTurnLogSelectionKey(entry));

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Open match history");
    }

    private void DrawDealerTurnLogMatchHistoryWindows()
    {
        if (openDealerTurnLogMatchHistoryWindowKeys.Count == 0)
            return;

        var openKeys = openDealerTurnLogMatchHistoryWindowKeys.ToList();
        foreach (string key in openKeys)
        {
            var entry = GetDealerTableHistoryEntries().FirstOrDefault(candidate => string.Equals(GetDealerTurnLogSelectionKey(candidate), key, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                openDealerTurnLogMatchHistoryWindowKeys.Remove(key);
                continue;
            }

            entry.EnsureInitialized();
            bool isOpen = true;
            string windowTitle = $"{entry.PlayerName} Match History###DealerTurnLogMatchHistoryWindow{key}";
            ImGui.SetNextWindowSize(new Vector2(560f, 380f), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(windowTitle, ref isOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextUnformatted($"House: {GetDealerTurnLogDisplayHouse(entry)}");
                ImGui.TextUnformatted($"Joined: {FormatTimestampForDisplay(entry.JoinedTimestamp)}");
                ImGui.TextUnformatted($"Received: {(string.IsNullOrWhiteSpace(entry.TotalReceivedGil) ? "0" : entry.TotalReceivedGil)} | Sent: {(string.IsNullOrWhiteSpace(entry.TotalSentGil) ? "0" : entry.TotalSentGil)}");
                ImGui.Spacing();

                string text = entry.MatchHistoryLines.Count > 0
                    ? string.Join(Environment.NewLine, entry.MatchHistoryLines)
                    : "No match history recorded yet.";

                byte[] readOnlyBuffer = Encoding.UTF8.GetBytes(text + "\0");
                ImGui.InputTextMultiline($"##DealerTurnLogMatchHistoryText{key}", readOnlyBuffer, new Vector2(-1f, -1f), ImGuiInputTextFlags.ReadOnly);
            }
            ImGui.End();

            if (!isOpen)
                openDealerTurnLogMatchHistoryWindowKeys.Remove(key);
        }
    }

    private void DrawDealerTurnLogPatternTrainerWindow()
    {
        if (!showDealerTurnLogPatternTrainerWindow)
        {
            showDealerTurnLogPatternValidationPopup = false;
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(640f, 360f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Pattern Trainer###DealerTurnLogPatternTrainerWindow", ref showDealerTurnLogPatternTrainerWindow, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.End();
            return;
        }

        ImGui.TextWrapped("Here you can train our system to recognize more messages patterns to be recognized by the system");
        ImGui.Spacing();

        const float registerButtonWidth = 90f;
        const float helpButtonWidth = 24f;
        float itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        float inputWidth = MathF.Max(220f, ImGui.GetContentRegionAvail().X - registerButtonWidth - helpButtonWidth - (itemSpacing * 2f));

        ImGui.SetNextItemWidth(inputWidth);
        ImGui.InputText("##DealerTurnLogPatternSampleInput", ref dealerTurnLogPatternSampleInput, 256);
        bool inputActive = ImGui.IsItemActive();
        bool inputFocused = ImGui.IsItemFocused();
        Vector2 inputMin = ImGui.GetItemRectMin();
        Vector2 inputMax = ImGui.GetItemRectMax();
        dealerTurnLogPatternValidationPopupScreenPos = new Vector2(inputMin.X, inputMax.Y + 6f);

        if (string.IsNullOrWhiteSpace(dealerTurnLogPatternSampleInput) && !inputActive && !inputFocused)
        {
            Vector2 placeholderPos = new(inputMin.X + 8f, inputMin.Y + ((inputMax.Y - inputMin.Y) - ImGui.GetTextLineHeight()) * 0.5f);
            ImGui.GetWindowDrawList().AddText(placeholderPos, ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.35f, 0.35f, 0.62f)), "Example: <username> Lost");
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Register", "DealerTurnLogPatternRegisterButton", new Vector2(registerButtonWidth, 0f), Hex("#174d22"), WhiteText))
        {
            string sample = (dealerTurnLogPatternSampleInput ?? string.Empty).Trim();
            if (string.Equals(sample, "<username>", StringComparison.OrdinalIgnoreCase))
            {
                ShowDealerTurnLogPatternValidationPopup("Invalid sample. Please use something like \"<username> Sample message\".");
            }
            else if (sample.IndexOf("<username>", StringComparison.OrdinalIgnoreCase) < 0)
            {
                ShowDealerTurnLogPatternValidationPopup("Invalid sample. Your message need to contain at least 1 \"<username>\" to indicate users");
            }
            else
            {
                var samples = GetDealerTurnLogPatternSamples();
                string existing = samples.FirstOrDefault(x => string.Equals(x, sample, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(existing))
                    samples.Remove(existing);

                samples.Insert(0, sample);
                dealerTurnLogPatternSampleInput = string.Empty;
                showDealerTurnLogPatternValidationPopup = false;
                Plugin.Configuration.Save();
                AddDebugLog("TURNLOG", $"Pattern sample registered: {sample}");
            }
        }

        ImGui.SameLine();
        DrawStyledBoldButton("?", "DealerTurnLogPatternHelpButton", new Vector2(helpButtonWidth, 0f), Hex("#3a3a3a"), Hex("#ff9aa7"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(360f, 0f), new Vector2(420f, float.MaxValue));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(380f);
            ImGui.TextWrapped("Sample examples must contain the text \"<username>\" to indicate to the plugin the Username detection. Examples: \"<username> Lost\", \"Lost: <username>\", \"Winners: <username>, <username>\"");
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionTitle("Registered samples");

        if (ImGui.BeginChild("##DealerTurnLogRegisteredSamplesChild", new Vector2(0f, 0f), false))
        {
            var samples = GetDealerTurnLogPatternSamples();
            if (samples.Count == 0)
            {
                ImGui.TextDisabled("No custom samples registered yet.");
            }
            else if (ImGui.BeginTable("##DealerTurnLogRegisteredSamplesTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Sample", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed, 34f);

                string? sampleToDelete = null;
                foreach (string sample in samples.ToList())
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(sample);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.PushID(sample);
                    if (DrawStyledBoldButton("X", "DealerTurnLogPatternDeleteButton", new Vector2(24f, 0f), Hex("#8f1f1f"), WhiteText))
                        sampleToDelete = sample;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Delete Sample");
                    ImGui.PopID();
                }

                if (!string.IsNullOrWhiteSpace(sampleToDelete))
                {
                    samples.RemoveAll(x => string.Equals(x, sampleToDelete, StringComparison.OrdinalIgnoreCase));
                    Plugin.Configuration.Save();
                    AddDebugLog("TURNLOG", $"Pattern sample removed: {sampleToDelete}");
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

        ImGui.End();
    }

    private void DrawDealerTurnLogPatternInvalidPopup()
    {
        if (!showDealerTurnLogPatternValidationPopup || string.IsNullOrWhiteSpace(dealerTurnLogPatternValidationMessage))
            return;

        ImGui.SetNextWindowPos(dealerTurnLogPatternValidationPopupScreenPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(430f, 0f), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.98f);

        bool keepOpen = true;
        bool popupHovered = false;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);

        if (ImGui.Begin(
                "##DealerTurnLogPatternInvalidPopupWindow",
                ref keepOpen,
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoSavedSettings))
        {
            popupHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
            ImGui.PushTextWrapPos(390f);
            ImGui.TextUnformatted(dealerTurnLogPatternValidationMessage);
            ImGui.PopTextWrapPos();
            ImGui.Spacing();
            if (DrawStyledBoldButton("OK", "DealerTurnLogPatternInvalidPopupOk", new Vector2(82f, 0f), UtilityButtonColor, WhiteText))
                showDealerTurnLogPatternValidationPopup = false;
        }
        ImGui.End();

        ImGui.PopStyleVar(2);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !popupHovered)
            showDealerTurnLogPatternValidationPopup = false;

        if (!keepOpen)
            showDealerTurnLogPatternValidationPopup = false;
    }

    private bool DrawFolderBrowseIconButton(string id, Vector2 size, Vector4 baseColor)
    {
        Vector2 actualSize = size;
        if (actualSize.X <= 0f)
            actualSize.X = 30f;
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
        var drawList = ImGui.GetWindowDrawList();
        uint iconColor = ImGui.ColorConvertFloat4ToU32(WhiteText);
        uint accentColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.22f));

        float width = max.X - min.X;
        float height = max.Y - min.Y;
        float iconWidth = width * 0.58f;
        float iconHeight = height * 0.50f;
        float left = min.X + ((width - iconWidth) * 0.5f);
        float top = min.Y + ((height - iconHeight) * 0.5f) + 1f;
        float right = left + iconWidth;
        float bottom = top + iconHeight;

        float tabWidth = iconWidth * 0.38f;
        float tabHeight = iconHeight * 0.26f;
        float tabLeft = left + (iconWidth * 0.06f);
        float tabTop = top - (tabHeight * 0.58f);
        float tabRight = tabLeft + tabWidth;
        float tabBottom = top + (iconHeight * 0.14f);

        drawList.AddRectFilled(new Vector2(tabLeft, tabTop), new Vector2(tabRight, tabBottom), iconColor, 2.5f);
        drawList.AddRectFilled(new Vector2(left, top), new Vector2(right, bottom), iconColor, 3f);
        drawList.AddRectFilled(new Vector2(left + 2f, top + 2f), new Vector2(right - 2f, top + (iconHeight * 0.46f)), accentColor, 2f);

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
                "Today" => history.Where(entry => ParseTimestamp(entry.Timestamp).Date == GetEstNow().Date),
                "This Week" => history.Where(entry => ParseTimestamp(entry.Timestamp) >= GetEstNow().Date.AddDays(-6)),
                "This Month" => history.Where(entry => ParseTimestamp(entry.Timestamp).Year == GetEstNow().Date.Year && ParseTimestamp(entry.Timestamp).Month == GetEstNow().Date.Month),
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
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
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

        AddTrackedHistoryEntry(FormatSignedResult(lastPlayerBankChangeValue));

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
        string resultText = $"{FormatSignedResult(amount)} Banking";

        AddDebugLog("HISTORY", $"Adding banking history entry: {resultText}.");
        history.Add(new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
            Tips = string.Empty,
            Result = resultText
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
            string senderRaw = sender.TextValue ?? string.Empty;
            string messageText = message.TextValue?.Replace('\n', ' ').Replace('\r', ' ').Trim() ?? string.Empty;

            TryCaptureDealerTurnLogChatMessage(type, senderRaw, messageText);

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

            string senderName = NormalizeLooseText(StripWorldSuffix(senderRaw));
            string localPlayerName = NormalizeLooseText(Plugin.PlayerState.CharacterName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(localPlayerName))
            {
                AddDebugLog("AUTO", "Party chat monitoring skipped because local player name is empty.");
                return;
            }

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

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            {
                FlushPendingDealerTurnLogWrites(force: false);
                FlushPendingDealerTurnLogConfigurationSave(force: false);
                return;
            }

            FlushPendingDealerTurnLogWrites(force: false);
            FlushPendingDealerTurnLogConfigurationSave(force: false);

            if (DateTime.UtcNow < nextDealerTurnLogPartySyncUtc)
                return;

            nextDealerTurnLogPartySyncUtc = DateTime.UtcNow.AddMilliseconds(900);
            SyncDealerTurnLogPartyRoster();
            SyncDealerTableHistoryPartyRoster();
        }
        catch (Exception ex)
        {
            AddDebugLog("ERR", $"Dealer turn log framework update failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private bool GetDealerTurnLogsEnabled()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        profile.EnsureInitialized();
        return profile.DealerTurnLogsEnabled;
    }

    private List<DealerTurnLogEntry> GetDealerTurnLogEntries()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        profile.EnsureInitialized();
        profile.DealerTurnLogEntries ??= new();
        foreach (var entry in profile.DealerTurnLogEntries)
            entry.EnsureInitialized();
        return profile.DealerTurnLogEntries;
    }

    private List<DealerTurnLogEntry> GetDealerTableHistoryEntries()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        profile.EnsureInitialized();
        profile.DealerTableHistoryEntries ??= new();
        foreach (var entry in profile.DealerTableHistoryEntries)
            entry.EnsureInitialized();
        return profile.DealerTableHistoryEntries;
    }

    private List<string> GetDealerTurnLogPatternSamples()
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        profile.EnsureInitialized();
        profile.DealerTurnLogPatternSamples ??= new();
        return profile.DealerTurnLogPatternSamples;
    }

    private string GetCurrentDealerTurnLogHouse()
    {
        return (houseInput ?? string.Empty).Trim();
    }

    private void ApplyDealerTurnLogHouseIfMissing(DealerTurnLogEntry entry)
    {
        if (entry == null)
            return;

        entry.EnsureInitialized();
        if (entry.HouseEditedManually)
            return;

        string currentHouse = GetCurrentDealerTurnLogHouse();
        if (!string.IsNullOrWhiteSpace(currentHouse) && string.IsNullOrWhiteSpace(entry.House))
            entry.House = currentHouse;
    }

    private void SyncDealerTurnLogHouseForActiveRows()
    {
        string currentHouse = GetCurrentDealerTurnLogHouse();
        bool changed = false;

        foreach (var entry in GetDealerTurnLogEntries())
        {
            entry.EnsureInitialized();
            if (!string.IsNullOrWhiteSpace(entry.LeftTimestamp))
                continue;
            if (entry.HouseEditedManually)
                continue;

            string existingHouse = (entry.House ?? string.Empty).Trim();
            if (!string.Equals(existingHouse, currentHouse, StringComparison.Ordinal))
            {
                entry.House = currentHouse;
                changed = true;
            }
        }

        if (changed)
            MarkDealerTurnLogConfigurationDirty();
    }

    private string GetDealerTurnLogDisplayHouse(DealerTurnLogEntry entry)
    {
        if (entry == null)
            return string.Empty;

        entry.EnsureInitialized();
        return (entry.House ?? string.Empty).Trim();
    }


    private void ToggleDealerTurnLogs()
    {
        SetDealerTurnLogsEnabledInternal(!GetDealerTurnLogsEnabled());
    }

    private void SetDealerTurnLogsEnabledInternal(bool enabled)
    {
        var profile = Plugin.Configuration.GetOrCreateActiveProfile();
        profile.EnsureInitialized();

        if (profile.DealerTurnLogsEnabled == enabled)
        {
            if (enabled)
            {
                dealerTurnLogsRequirePartyMessageUntilUtc = DateTime.MinValue;
                SyncDealerTurnLogPartyRoster(forceJoinStamp: true);
                SyncDealerTableHistoryPartyRoster(forceJoinStamp: true);
            }
            return;
        }

        profile.DealerTurnLogsEnabled = enabled;
        Plugin.Configuration.Save();

        if (enabled)
        {
            dealerTurnLogsRequirePartyMessageUntilUtc = DateTime.MinValue;
            AddDebugLog("TURNLOG", "Dealer turn logs enabled.");
            SyncDealerTurnLogPartyRoster(forceJoinStamp: true);
            SyncDealerTableHistoryPartyRoster(forceJoinStamp: true);
        }
        else
        {
            AddDebugLog("TURNLOG", "Dealer turn logs disabled.");
            ClearDealerTurnLogPendingTrade();
        }
    }

    private static string GetEstTimestampText()
    {
        return FormatEstTimestamp(GetEstNow());
    }

    private static string GetEstTimeTag()
    {
        return FormatEstTimeTag(GetEstNow());
    }

    private static DateTime GetDealerTurnLogBusinessDate(DateTime timestamp)
    {
        if (timestamp == DateTime.MinValue)
            return DateTime.MinValue;

        return timestamp.Hour < 6 ? timestamp.Date.AddDays(-1) : timestamp.Date;
    }

    private static string GetDealerTurnLogBusinessDateKey(DateTime timestamp)
    {
        DateTime businessDate = GetDealerTurnLogBusinessDate(timestamp);
        return businessDate == DateTime.MinValue
            ? string.Empty
            : businessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private string GetDealerTurnLogBusinessDateLabel(DealerTurnLogEntry entry)
    {
        DateTime businessDate = GetDealerTurnLogBusinessDateValue(entry);
        return businessDate == DateTime.MinValue
            ? "Unknown Date"
            : businessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private DateTime GetDealerTurnLogBusinessDateValue(DealerTurnLogEntry entry)
    {
        entry.EnsureInitialized();
        DateTime joinedTimestamp = ParseTimestamp(entry.JoinedTimestamp);
        if (joinedTimestamp != DateTime.MinValue)
            return GetDealerTurnLogBusinessDate(joinedTimestamp);

        DateTime leftTimestamp = ParseTimestamp(entry.LeftTimestamp);
        return leftTimestamp == DateTime.MinValue ? DateTime.MinValue : GetDealerTurnLogBusinessDate(leftTimestamp);
    }

    private bool CanReuseDealerTurnLogEntry(DealerTurnLogEntry entry, DateTime eventTimestamp)
    {
        entry.EnsureInitialized();
        DateTime joinedTimestamp = ParseTimestamp(entry.JoinedTimestamp);
        if (joinedTimestamp == DateTime.MinValue)
            return false;

        if (eventTimestamp < joinedTimestamp)
            return false;

        string targetBucketKey = GetDealerTurnLogBusinessDateKey(eventTimestamp);
        string entryBucketKey = string.IsNullOrWhiteSpace(entry.SessionBucketKey)
            ? GetDealerTurnLogBusinessDateKey(joinedTimestamp)
            : entry.SessionBucketKey;

        if (!string.Equals(entryBucketKey, targetBucketKey, StringComparison.OrdinalIgnoreCase))
            return false;

        return (eventTimestamp - joinedTimestamp) <= TimeSpan.FromHours(24);
    }

    private IEnumerable<DealerTurnLogEntry> FindDealerTurnLogEntriesForPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return Enumerable.Empty<DealerTurnLogEntry>();

        string normalized = NormalizeLooseText(playerName);
        return GetDealerTurnLogEntries()
            .Where(entry => entry != null)
            .Select(entry => { entry.EnsureInitialized(); return entry; })
            .Where(entry => NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName ?? string.Empty), normalized))
            .OrderByDescending(entry => ParseTimestamp(entry.JoinedTimestamp));
    }

    private void CloseDealerTurnLogEntry(DealerTurnLogEntry entry, string timestampText, string reason, bool addMatchHistoryLine)
    {
        entry.EnsureInitialized();
        if (string.IsNullOrWhiteSpace(entry.LeftTimestamp))
            entry.LeftTimestamp = timestampText;
        else
            entry.LeftTimestamp = timestampText;

        entry.LeftReason = reason ?? string.Empty;

        if (addMatchHistoryLine && string.Equals(reason, "LeftParty", StringComparison.OrdinalIgnoreCase))
            AppendDealerTurnLogMatchHistoryLine(entry, $"{entry.PlayerName} Left the party");

        MarkDealerTurnLogFilesDirty(entry, writeChatFile: true, writeTradeFile: false);
    }

    private void CloseNonReusableActiveDealerTurnLogEntries(string playerName, string timestampText, DateTime eventTimestamp)
    {
        foreach (var entry in FindDealerTurnLogEntriesForPlayer(playerName)
                     .Where(entry => string.IsNullOrWhiteSpace(entry.LeftTimestamp))
                     .Where(entry => !CanReuseDealerTurnLogEntry(entry, eventTimestamp)))
        {
            CloseDealerTurnLogEntry(entry, timestampText, "SessionRollover", addMatchHistoryLine: false);
        }
    }

    private void SyncDealerTurnLogPartyRoster(bool forceJoinStamp = false)
    {
        if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            return;

        var entries = GetDealerTurnLogEntries();
        var presentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DateTime nowEst = GetEstNow();
        string nowText = FormatEstTimestamp(nowEst);
        DateTime nowUtc = DateTime.UtcNow;
        bool changed = false;

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        int currentPartyMemberCount = 0;

        for (int i = 0; i < Plugin.PartyList.Length; i++)
        {
            var member = Plugin.PartyList[i];
            if (member == null)
                continue;

            string memberName = StripWorldSuffix(member.Name.TextValue ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(memberName))
            {
                presentNames.Add(memberName);
                currentPartyMemberCount++;
            }
        }

        bool partyIsCurrentlyActive = currentPartyMemberCount > 0;
        dealerTurnLogPartyPreviouslyActive = partyIsCurrentlyActive;

        foreach (string presentName in presentNames)
        {
            dealerTurnLogLastSeenUtc[presentName] = nowUtc;

            bool isLocalPlayer = !string.IsNullOrWhiteSpace(localPlayerName) &&
                string.Equals(presentName, localPlayerName, StringComparison.OrdinalIgnoreCase);

            if (isLocalPlayer)
                continue;

            CloseNonReusableActiveDealerTurnLogEntries(presentName, nowText, nowEst);

            var entry = FindDealerTurnLogEntry(presentName, nowEst);
            if (entry == null)
            {
                entry = CreateDealerTurnLogEntry(presentName, nowText, nowEst);
                entries.Add(entry);
                changed = true;
                AddDebugLog("TURNLOG", $"Player registered in logs: {presentName} | Joined={nowText}");
                continue;
            }

            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);
            bool rowChanged = false;
            if (string.IsNullOrWhiteSpace(entry.JoinedTimestamp) || (forceJoinStamp && !string.IsNullOrWhiteSpace(entry.LeftTimestamp)))
            {
                entry.JoinedTimestamp = nowText;
                entry.SessionBucketKey = GetDealerTurnLogBusinessDateKey(nowEst);
                changed = true;
                rowChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.LeftTimestamp))
            {
                entry.LeftTimestamp = string.Empty;
                entry.LeftReason = string.Empty;
                changed = true;
                rowChanged = true;
                AddDebugLog("TURNLOG", $"Player rejoined tracked party: {presentName}");
            }

            if (rowChanged)
                MarkDealerTurnLogFilesDirty(entry, writeChatFile: true, writeTradeFile: false);
        }

        foreach (var entry in entries)
        {
            entry.EnsureInitialized();
            string playerName = StripWorldSuffix(entry.PlayerName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(playerName))
                continue;
            if (presentNames.Contains(playerName))
                continue;
            if (!string.IsNullOrWhiteSpace(entry.LeftTimestamp))
                continue;

            bool isLocalPlayer = !string.IsNullOrWhiteSpace(localPlayerName) &&
                string.Equals(playerName, localPlayerName, StringComparison.OrdinalIgnoreCase);

            if (isLocalPlayer)
                continue;

            if (!dealerTurnLogLastSeenUtc.TryGetValue(playerName, out var lastSeenUtc))
                lastSeenUtc = nowUtc;

            if ((nowUtc - lastSeenUtc) < TimeSpan.FromSeconds(3))
                continue;

            CloseDealerTurnLogEntry(entry, nowText, "LeftParty", addMatchHistoryLine: true);
            changed = true;
            AddDebugLog("TURNLOG", $"Player marked as left: {playerName} | Left={nowText}");
        }

        if (changed)
            MarkDealerTurnLogConfigurationDirty();
    }

    private DealerTurnLogEntry? FindDealerTurnLogEntry(string playerName, DateTime? eventTimestamp = null, bool requireReusable = true)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        DateTime effectiveTimestamp = eventTimestamp ?? GetEstNow();
        foreach (var entry in FindDealerTurnLogEntriesForPlayer(playerName))
        {
            if (!requireReusable || CanReuseDealerTurnLogEntry(entry, effectiveTimestamp))
                return entry;
        }

        return null;
    }

    private DealerTurnLogEntry CreateDealerTurnLogEntry(string playerName, string timestampText, DateTime eventTimestamp)
    {
        var entry = new DealerTurnLogEntry
        {
            PlayerName = playerName,
            JoinedTimestamp = timestampText,
            House = GetCurrentDealerTurnLogHouse(),
            SessionBucketKey = GetDealerTurnLogBusinessDateKey(eventTimestamp),
        };
        entry.EnsureInitialized();
        return entry;
    }

    private DealerTurnLogEntry GetOrCreateDealerTurnLogEntry(string playerName, DateTime? eventTimestamp = null)
    {
        DateTime effectiveTimestamp = eventTimestamp ?? GetEstNow();
        string timestampText = FormatEstTimestamp(effectiveTimestamp);
        string cleanName = ResolveDealerTurnLogPlayerName(playerName);

        CloseNonReusableActiveDealerTurnLogEntries(cleanName, timestampText, effectiveTimestamp);

        var existing = FindDealerTurnLogEntry(cleanName, effectiveTimestamp);
        if (existing != null)
        {
            existing.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(existing);
            if (string.IsNullOrWhiteSpace(existing.PlayerName))
                existing.PlayerName = cleanName;
            return existing;
        }

        var entry = CreateDealerTurnLogEntry(cleanName, timestampText, effectiveTimestamp);
        GetDealerTurnLogEntries().Add(entry);
        dealerTurnLogLastSeenUtc[cleanName] = DateTime.UtcNow;
        MarkDealerTurnLogConfigurationDirty();
        AddDebugLog("TURNLOG", $"Created missing turn log row for player '{cleanName}'.");
        MarkDealerTurnLogFilesDirty(entry);
        return entry;
    }

    private IEnumerable<DealerTurnLogEntry> FindDealerTableHistoryEntriesForPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return Enumerable.Empty<DealerTurnLogEntry>();

        string normalized = NormalizeLooseText(playerName);
        return GetDealerTableHistoryEntries()
            .Where(entry => entry != null)
            .Select(entry => { entry.EnsureInitialized(); return entry; })
            .Where(entry => NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName ?? string.Empty), normalized))
            .OrderByDescending(entry => ParseTimestamp(entry.JoinedTimestamp));
    }

    private DealerTurnLogEntry? FindDealerTableHistoryEntry(string playerName, DateTime? eventTimestamp = null, bool requireReusable = true)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        DateTime effectiveTimestamp = eventTimestamp ?? GetEstNow();
        foreach (var entry in FindDealerTableHistoryEntriesForPlayer(playerName))
        {
            if (!requireReusable || CanReuseDealerTurnLogEntry(entry, effectiveTimestamp))
                return entry;
        }

        return null;
    }

    private DealerTurnLogEntry CreateDealerTableHistoryEntry(string playerName, string timestampText, DateTime eventTimestamp)
    {
        var entry = new DealerTurnLogEntry
        {
            PlayerName = playerName,
            JoinedTimestamp = timestampText,
            House = GetCurrentDealerTurnLogHouse(),
            SessionBucketKey = GetDealerTurnLogBusinessDateKey(eventTimestamp),
        };
        entry.EnsureInitialized();
        return entry;
    }

    private void CloseDealerTableHistoryEntry(DealerTurnLogEntry entry, string timestampText, string reason, bool addMatchHistoryLine)
    {
        entry.EnsureInitialized();
        entry.LeftTimestamp = timestampText;
        entry.LeftReason = reason ?? string.Empty;

        if (addMatchHistoryLine && string.Equals(reason, "LeftParty", StringComparison.OrdinalIgnoreCase))
            AppendDealerTableHistoryLine(entry, $"{entry.PlayerName} Left the party", ParseTimestamp(timestampText));
    }

    private void CloseNonReusableActiveDealerTableHistoryEntries(string playerName, string timestampText, DateTime eventTimestamp)
    {
        foreach (var entry in FindDealerTableHistoryEntriesForPlayer(playerName)
                     .Where(entry => string.IsNullOrWhiteSpace(entry.LeftTimestamp))
                     .Where(entry => !CanReuseDealerTurnLogEntry(entry, eventTimestamp)))
        {
            CloseDealerTableHistoryEntry(entry, timestampText, "SessionRollover", addMatchHistoryLine: false);
        }
    }

    private DealerTurnLogEntry GetOrCreateDealerTableHistoryEntry(string playerName, DateTime? eventTimestamp = null)
    {
        DateTime effectiveTimestamp = eventTimestamp ?? GetEstNow();
        string timestampText = FormatEstTimestamp(effectiveTimestamp);
        string cleanName = ResolveDealerTurnLogPlayerName(playerName);

        CloseNonReusableActiveDealerTableHistoryEntries(cleanName, timestampText, effectiveTimestamp);

        var existing = FindDealerTableHistoryEntry(cleanName, effectiveTimestamp);
        if (existing != null)
        {
            existing.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(existing);
            if (string.IsNullOrWhiteSpace(existing.PlayerName))
                existing.PlayerName = cleanName;
            return existing;
        }

        var entry = CreateDealerTableHistoryEntry(cleanName, timestampText, effectiveTimestamp);
        GetDealerTableHistoryEntries().Add(entry);
        dealerTurnLogLastSeenUtc[cleanName] = DateTime.UtcNow;
        MarkDealerTurnLogConfigurationDirty();
        return entry;
    }

    private DealerTurnLogEntry? FindActiveLocalDealerTableHistoryEntry()
    {
        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(localPlayerName))
            return null;

        return GetDealerTableHistoryEntries()
            .Where(entry => entry != null)
            .Select(entry => { entry.EnsureInitialized(); return entry; })
            .Where(entry => string.IsNullOrWhiteSpace(entry.LeftTimestamp))
            .Where(entry => NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName ?? string.Empty), NormalizeLooseText(localPlayerName)))
            .OrderByDescending(entry => ParseTimestamp(entry.JoinedTimestamp))
            .FirstOrDefault();
    }

    private void SyncDealerTableHistoryPartyRoster(bool forceJoinStamp = false)
    {
        if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            return;

        var entries = GetDealerTableHistoryEntries();
        var presentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DateTime nowEst = GetEstNow();
        string nowText = FormatEstTimestamp(nowEst);
        DateTime nowUtc = DateTime.UtcNow;
        bool changed = false;

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);

        for (int i = 0; i < Plugin.PartyList.Length; i++)
        {
            var member = Plugin.PartyList[i];
            if (member == null)
                continue;

            string memberName = StripWorldSuffix(member.Name.TextValue ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(memberName))
                presentNames.Add(memberName);
        }

        foreach (string presentName in presentNames)
        {
            dealerTurnLogLastSeenUtc[presentName] = nowUtc;

            bool isLocalPlayer = !string.IsNullOrWhiteSpace(localPlayerName) &&
                string.Equals(presentName, localPlayerName, StringComparison.OrdinalIgnoreCase);

            if (isLocalPlayer)
                continue;

            CloseNonReusableActiveDealerTableHistoryEntries(presentName, nowText, nowEst);

            var entry = FindDealerTableHistoryEntry(presentName, nowEst);
            if (entry == null)
            {
                entries.Add(CreateDealerTableHistoryEntry(presentName, nowText, nowEst));
                changed = true;
                continue;
            }

            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);
            if (string.IsNullOrWhiteSpace(entry.JoinedTimestamp) || (forceJoinStamp && !string.IsNullOrWhiteSpace(entry.LeftTimestamp)))
            {
                entry.JoinedTimestamp = nowText;
                entry.SessionBucketKey = GetDealerTurnLogBusinessDateKey(nowEst);
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(entry.LeftTimestamp))
            {
                entry.LeftTimestamp = string.Empty;
                entry.LeftReason = string.Empty;
                changed = true;
            }
        }

        foreach (var entry in entries)
        {
            entry.EnsureInitialized();
            string playerName = StripWorldSuffix(entry.PlayerName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(playerName) || presentNames.Contains(playerName) || !string.IsNullOrWhiteSpace(entry.LeftTimestamp))
                continue;

            bool isLocalPlayer = !string.IsNullOrWhiteSpace(localPlayerName) &&
                string.Equals(playerName, localPlayerName, StringComparison.OrdinalIgnoreCase);

            if (isLocalPlayer)
                continue;

            if (!dealerTurnLogLastSeenUtc.TryGetValue(playerName, out var lastSeenUtc))
                lastSeenUtc = nowUtc;

            if ((nowUtc - lastSeenUtc) < TimeSpan.FromSeconds(3))
                continue;

            CloseDealerTableHistoryEntry(entry, nowText, "LeftParty", addMatchHistoryLine: true);
            changed = true;
        }

        if (changed)
            MarkDealerTurnLogConfigurationDirty();
    }

    private void AppendDealerTableHistoryLine(DealerTurnLogEntry entry, string message, DateTime? timestampEst = null)
    {
        if (entry == null || string.IsNullOrWhiteSpace(message))
            return;

        entry.EnsureInitialized();
        DateTime effectiveTimestamp = timestampEst ?? GetEstNow();
        string line = $"{FormatEstTimeTag(effectiveTimestamp)} {message.Trim()}";
        if (entry.MatchHistoryLines.Count == 0 || !string.Equals(entry.MatchHistoryLines[^1], line, StringComparison.Ordinal))
            entry.MatchHistoryLines.Add(line);

        MarkDealerTurnLogConfigurationDirty();
    }

    private void AppendDealerTableHistoryOutcomeForPlayer(string playerName, string normalizedResult, DateTime eventTimestamp)
    {
        if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(normalizedResult))
            return;

        var entry = GetOrCreateDealerTableHistoryEntry(playerName, eventTimestamp);
        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;
        AppendDealerTableHistoryLine(entry, $"{entry.PlayerName} {normalizedResult}", eventTimestamp);
    }

    private void ApplyTradeToDealerTableHistoryEntry(DealerTurnLogEntry entry, bool sentByPlayer, BigInteger amount, string formattedAmount, DateTime eventTimestamp)
    {
        entry.EnsureInitialized();
        string directionText = sentByPlayer ? "Sent" : "Received";
        string timelineMessage = $"{entry.PlayerName} {directionText} {formattedAmount} Gil";
        AppendDealerTableHistoryLine(entry, timelineMessage, eventTimestamp);
        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;

        if (sentByPlayer)
        {
            BigInteger currentReceived = ParseBankValue(entry.TotalReceivedGil);
            entry.TotalReceivedGil = FormatNumber(currentReceived + amount);
        }
        else
        {
            BigInteger currentSent = ParseBankValue(entry.TotalSentGil);
            entry.TotalSentGil = FormatNumber(currentSent + amount);
        }
    }

    private void AppendDealerTableHistoryTrade(string playerName, bool sentByPlayer, BigInteger amount)
    {
        DateTime nowEst = GetEstNow();
        var entry = GetOrCreateDealerTableHistoryEntry(playerName, nowEst);
        string formattedAmount = FormatNumber(amount);
        ApplyTradeToDealerTableHistoryEntry(entry, sentByPlayer, amount, formattedAmount, nowEst);

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        DealerTurnLogEntry? localEntry = FindActiveLocalDealerTableHistoryEntry();
        bool shouldMirrorToLocal =
            localEntry != null &&
            !string.IsNullOrWhiteSpace(localPlayerName) &&
            !NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName), NormalizeLooseText(localPlayerName));

        if (shouldMirrorToLocal)
            ApplyTradeToDealerTableHistoryEntry(localEntry!, sentByPlayer, amount, formattedAmount, nowEst);

        MarkDealerTurnLogConfigurationDirty();
    }

    private void AppendDealerTableHistoryShiftAction(string checkpointName, DateTime timestampEst)
    {
        if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            return;

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(localPlayerName))
            return;

        string timestampText = FormatEstTimestamp(timestampEst);
        string actionText = checkpointName switch
        {
            "Start Shift" => "Started shift.",
            "Break" => "Taking a break.",
            "Resume" => "End of the break.",
            "End Shift" => "Shift Ended.",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(actionText))
            return;

        DealerTurnLogEntry? entry = FindActiveLocalDealerTableHistoryEntry();
        if (string.Equals(checkpointName, "Start Shift", StringComparison.OrdinalIgnoreCase))
        {
            if (entry == null)
            {
                entry = CreateDealerTableHistoryEntry(localPlayerName, timestampText, timestampEst);
                GetDealerTableHistoryEntries().Add(entry);
            }

            entry.JoinedTimestamp = timestampText;
            entry.SessionBucketKey = GetDealerTurnLogBusinessDateKey(timestampEst);
            entry.LeftTimestamp = string.Empty;
            entry.LeftReason = string.Empty;
        }

        if (entry == null)
            return;

        AppendDealerTableHistoryLine(entry, $"{entry.PlayerName}: {actionText}", timestampEst);
        if (string.Equals(checkpointName, "End Shift", StringComparison.OrdinalIgnoreCase))
        {
            entry.LeftTimestamp = timestampText;
            entry.LeftReason = "EndShift";
        }

        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;
        MarkDealerTurnLogConfigurationDirty();
    }

    private string GetDealerTurnLogShiftActionPrefix()
    {
        return "[SHIFT_ACTION] ";
    }

    private bool IsDealerTurnLogShiftActionLine(string line)
    {
        return !string.IsNullOrWhiteSpace(line) && line.StartsWith(GetDealerTurnLogShiftActionPrefix(), StringComparison.Ordinal);
    }

    private DealerTurnLogEntry? FindActiveLocalDealerTurnLogEntry()
    {
        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(localPlayerName))
            return null;

        return GetDealerTurnLogEntries()
            .Where(entry => entry != null)
            .Select(entry => { entry.EnsureInitialized(); return entry; })
            .Where(entry => string.IsNullOrWhiteSpace(entry.LeftTimestamp))
            .Where(entry => NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName ?? string.Empty), NormalizeLooseText(localPlayerName)))
            .OrderByDescending(entry => ParseTimestamp(entry.JoinedTimestamp))
            .FirstOrDefault();
    }

    private void AppendDealerTurnLogShiftAction(string checkpointName)
    {
        if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            return;

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(localPlayerName))
            return;

        DateTime timestampEst = GetEstNow();
        string timestampText = FormatEstTimestamp(timestampEst);
        string actionText = checkpointName switch
        {
            "Start Shift" => "Started shift.",
            "Break" => "Taking a break.",
            "Resume" => "End of the break.",
            "End Shift" => "Shift Ended.",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(actionText))
            return;

        DealerTurnLogEntry? entry = FindActiveLocalDealerTurnLogEntry();
        if (string.Equals(checkpointName, "Start Shift", StringComparison.OrdinalIgnoreCase))
        {
            if (entry == null)
            {
                entry = CreateDealerTurnLogEntry(localPlayerName, timestampText, timestampEst);
                GetDealerTurnLogEntries().Add(entry);
                AddDebugLog("TURNLOG", $"Local player registered in logs from Start Shift: {localPlayerName} | Joined={timestampText}");
            }
            else
            {
                ApplyDealerTurnLogHouseIfMissing(entry);
            }

            entry.JoinedTimestamp = timestampText;
            entry.SessionBucketKey = GetDealerTurnLogBusinessDateKey(timestampEst);
            entry.LeftTimestamp = string.Empty;
            entry.LeftReason = string.Empty;
        }

        if (entry == null)
            return;

        entry.EnsureInitialized();
        ApplyDealerTurnLogHouseIfMissing(entry);

        if (string.Equals(checkpointName, "End Shift", StringComparison.OrdinalIgnoreCase))
        {
            DateTime shiftStartTimestamp = ParseTimestamp(entry.JoinedTimestamp);
            DateTime shiftEndTimestamp = ParseTimestamp(timestampText);

            if (shiftStartTimestamp == DateTime.MinValue)
                shiftStartTimestamp = TryGetMostRecentDealerShiftStartBefore(shiftEndTimestamp) ?? DateTime.MinValue;

            if (shiftStartTimestamp != DateTime.MinValue && shiftEndTimestamp != DateTime.MinValue && shiftEndTimestamp >= shiftStartTimestamp)
                actionText += $" Total time: {FormatShiftDurationDetailed(shiftEndTimestamp - shiftStartTimestamp)}";
        }

        string shiftLine = $"{GetDealerTurnLogShiftActionPrefix()}[{timestampText}] {entry.PlayerName}: {actionText}";
        entry.ChatLines.Add(shiftLine);

        if (string.Equals(checkpointName, "End Shift", StringComparison.OrdinalIgnoreCase))
        {
            entry.LeftTimestamp = timestampText;
            entry.LeftReason = "EndShift";
        }

        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;
        MarkDealerTurnLogFilesDirty(entry, writeChatFile: true, writeTradeFile: false);
        MarkDealerTurnLogConfigurationDirty();
        AppendDealerTableHistoryShiftAction(checkpointName, timestampEst);
    }

    private void TryCaptureDealerTurnLogChatMessage(XivChatType type, string senderRaw, string messageText)
    {
        if (!GetDealerTurnLogsEnabled() || !IsDealerMode)
            return;

        if (!string.IsNullOrWhiteSpace(messageText))
            TryCaptureDealerTurnTradeMessage(type, senderRaw, messageText);

        if (type != XivChatType.Party)
            return;

        string senderName = ResolveDealerTurnLogPlayerName(senderRaw);
        if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(messageText))
            return;

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        DealerTurnLogEntry? entry = !string.IsNullOrWhiteSpace(localPlayerName) &&
            NamesRoughlyMatch(NormalizeLooseText(senderName), NormalizeLooseText(localPlayerName))
            ? FindActiveLocalDealerTurnLogEntry()
            : null;

        if (entry == null)
        {
            if (!string.IsNullOrWhiteSpace(localPlayerName) &&
                NamesRoughlyMatch(NormalizeLooseText(senderName), NormalizeLooseText(localPlayerName)))
                return;

            entry = GetOrCreateDealerTurnLogEntry(senderName);
        }
        string line = $"[{FormatEstTimestamp(GetEstNow())}] {entry.PlayerName}: {messageText}";
        entry.ChatLines.Add(line);
        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;
        MarkDealerTurnLogFilesDirty(entry, writeChatFile: true, writeTradeFile: false);
        MarkDealerTurnLogConfigurationDirty();

        if (!string.IsNullOrWhiteSpace(localPlayerName) &&
            NamesRoughlyMatch(NormalizeLooseText(senderName), NormalizeLooseText(localPlayerName)))
        {
            TryCaptureDealerTurnOutcomeMessage(messageText);
        }
    }

    private void AppendDealerTurnLogMatchHistoryLine(DealerTurnLogEntry entry, string message)
    {
        if (entry == null || string.IsNullOrWhiteSpace(message))
            return;

        entry.EnsureInitialized();
        string line = $"{GetEstTimeTag()} {message.Trim()}";
        if (entry.MatchHistoryLines.Count == 0 || !string.Equals(entry.MatchHistoryLines[^1], line, StringComparison.Ordinal))
            entry.MatchHistoryLines.Add(line);

        MarkDealerTurnLogConfigurationDirty();
    }

    private static string NormalizeDealerOutcomeResultText(string resultText)
    {
        string trimmed = (resultText ?? string.Empty).Trim().Trim(':', '-', '|', ',', '.', '!', '?');
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        if (LabelMatches(trimmed, WinTrackLabels))
            return "Wins";

        if (LabelMatches(trimmed, PushTrackLabels))
            return "Pushed";

        if (LabelMatches(trimmed, LossTrackLabels))
            return trimmed.IndexOf("bust", StringComparison.OrdinalIgnoreCase) >= 0 ? "Busted" : "Lost";

        return trimmed;
    }

    private void AppendDealerTurnOutcomeForPlayer(string playerName, string resultText)
    {
        string normalizedResult = NormalizeDealerOutcomeResultText(resultText);
        if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(normalizedResult))
            return;

        DateTime nowEst = GetEstNow();
        var entry = GetOrCreateDealerTurnLogEntry(playerName, nowEst);
        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;
        AppendDealerTurnLogMatchHistoryLine(entry, $"{entry.PlayerName} {normalizedResult}");
        AppendDealerTableHistoryOutcomeForPlayer(playerName, normalizedResult, nowEst);
        AddDebugLog("TURNLOG", $"Outcome matched for '{entry.PlayerName}': {normalizedResult}");
    }

    private IEnumerable<string> ExtractDealerOutcomeNames(string namesSection)
    {
        if (string.IsNullOrWhiteSpace(namesSection))
            yield break;

        string expanded = namesSection
            .Replace("|", ",", StringComparison.Ordinal)
            .Replace("/", ",", StringComparison.Ordinal)
            .Replace("&", ",", StringComparison.Ordinal);

        expanded = Regex.Replace(expanded, @"\band\b", ",", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string[] tokens = expanded.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string rawToken in tokens)
        {
            string token = rawToken.Trim().Trim('.', '!', '?', ';', ':');
            if (string.IsNullOrWhiteSpace(token))
                continue;
            if (string.Equals(token, "none", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return token;
        }
    }

    private void TryCaptureDealerTurnOutcomeMessage(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return;

        var appliedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in TrackResultRegex.Matches(messageText))
        {
            string outcomeText = NormalizeDealerOutcomeResultText(match.Groups["label"].Value);
            if (string.IsNullOrWhiteSpace(outcomeText))
                continue;

            foreach (string token in ExtractDealerOutcomeNames(match.Groups["names"].Value))
            {
                string resolvedName = ResolveDealerTurnLogPlayerName(token);
                if (string.IsNullOrWhiteSpace(resolvedName))
                    continue;

                string key = $"{NormalizeLooseText(resolvedName)}|{outcomeText}";
                if (!appliedKeys.Add(key))
                    continue;

                AppendDealerTurnOutcomeForPlayer(resolvedName, outcomeText);
            }
        }

        foreach (string sample in GetDealerTurnLogPatternSamples())
        {
            if (string.IsNullOrWhiteSpace(sample) || sample.IndexOf("<username>", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            Regex pattern = BuildDealerTurnLogPatternRegex(sample);
            if (pattern == null)
                continue;

            string sampleOutcome = NormalizeDealerOutcomeResultText(sample.Replace("<username>", " "));
            if (string.IsNullOrWhiteSpace(sampleOutcome))
                continue;

            foreach (Match match in pattern.Matches(messageText))
            {
                if (!match.Success || !match.Groups["name"].Success)
                    continue;

                string resolvedName = ResolveDealerTurnLogPlayerName(match.Groups["name"].Value);
                if (string.IsNullOrWhiteSpace(resolvedName))
                    continue;

                string key = $"{NormalizeLooseText(resolvedName)}|{sampleOutcome}";
                if (!appliedKeys.Add(key))
                    continue;

                AppendDealerTurnOutcomeForPlayer(resolvedName, sampleOutcome);
            }
        }
    }

    private static Regex BuildDealerTurnLogPatternRegex(string sample)
    {
        if (string.IsNullOrWhiteSpace(sample) || sample.IndexOf("<username>", StringComparison.OrdinalIgnoreCase) < 0)
            return null!;

        const string namePattern = @"(?<name>[\p{L}'\-]+(?:\s+[\p{L}'\-]+)?)";
        string[] parts = sample.Split(new[] { "<username>" }, StringSplitOptions.None);
        var pattern = new StringBuilder();
        pattern.Append(@"^\s*");

        for (int i = 0; i < parts.Length; i++)
        {
            string literal = parts[i];
            if (!string.IsNullOrWhiteSpace(literal))
                pattern.Append(Regex.Escape(literal).Replace("\\ ", @"\s+"));

            if (i < parts.Length - 1)
                pattern.Append(namePattern);
        }

        pattern.Append(@"\s*$");
        return new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private void TryCaptureDealerTurnTradeMessage(XivChatType type, string senderRaw, string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return;

        string tradeText = SanitizeTradeSystemMessage(messageText);
        if (string.IsNullOrWhiteSpace(tradeText))
            return;

        if (DateTime.UtcNow >= dealerTurnLogPendingTradeUntilUtc)
            ClearDealerTurnLogPendingTrade();

        var inboundRequestMatch = TradeRequestFromPlayerRegex.Match(tradeText);
        if (inboundRequestMatch.Success)
        {
            dealerTurnLogPendingTradePlayer = ResolveDealerTurnLogPlayerName(inboundRequestMatch.Groups["name"].Value);
            dealerTurnLogPendingTradeInitiatedByLocal = false;
            dealerTurnLogPendingTradeUntilUtc = DateTime.UtcNow.AddMinutes(2);
            GetOrCreateDealerTurnLogEntry(dealerTurnLogPendingTradePlayer);
            AddDebugLog("TURNLOG", $"Pending inbound trade detected from '{dealerTurnLogPendingTradePlayer}'.");
            return;
        }

        var outboundRequestMatch = TradeRequestFromLocalRegex.Match(tradeText);
        if (outboundRequestMatch.Success)
        {
            dealerTurnLogPendingTradePlayer = ResolveDealerTurnLogPlayerName(outboundRequestMatch.Groups["name"].Value);
            dealerTurnLogPendingTradeInitiatedByLocal = true;
            dealerTurnLogPendingTradeUntilUtc = DateTime.UtcNow.AddMinutes(2);
            GetOrCreateDealerTurnLogEntry(dealerTurnLogPendingTradePlayer);
            AddDebugLog("TURNLOG", $"Pending outbound trade detected to '{dealerTurnLogPendingTradePlayer}'.");
            return;
        }

        var awaitingConfirmationMatch = TradeAwaitingConfirmationRegex.Match(tradeText);
        if (awaitingConfirmationMatch.Success)
        {
            dealerTurnLogPendingTradePlayer = ResolveDealerTurnLogPlayerName(awaitingConfirmationMatch.Groups["name"].Value);
            dealerTurnLogPendingTradeUntilUtc = DateTime.UtcNow.AddMinutes(2);
            GetOrCreateDealerTurnLogEntry(dealerTurnLogPendingTradePlayer);
            AddDebugLog("TURNLOG", $"Trade confirmation pending with '{dealerTurnLogPendingTradePlayer}'.");
            return;
        }

        if (TradeCancelRegex.IsMatch(tradeText))
        {
            AddDebugLog("TURNLOG", "Pending trade state cleared after cancel/decline message.");
            ClearDealerTurnLogPendingTrade();
            return;
        }

        if (tradeText.IndexOf("trade", StringComparison.OrdinalIgnoreCase) >= 0 ||
            tradeText.IndexOf(" gil", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            AddDebugLog("TURNLOG", $"Inspecting possible trade message | Type='{type}' | Sender='{senderRaw}' | Text='{tradeText}'");
        }

        if (TryAppendDealerTurnTradeFromMatch(TradeSentNamedRegex.Match(tradeText), sentByPlayer: false, usePendingNameIfMissing: false))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeReceivedNamedRegex.Match(tradeText), sentByPlayer: true, usePendingNameIfMissing: false))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeReceivedPassiveRegex.Match(tradeText), sentByPlayer: false, usePendingNameIfMissing: false))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeOfferLocalNamedRegex.Match(tradeText), sentByPlayer: false, usePendingNameIfMissing: true))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeOfferPlayerNamedRegex.Match(tradeText), sentByPlayer: true, usePendingNameIfMissing: true))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeGiveLocalNamedRegex.Match(tradeText), sentByPlayer: false, usePendingNameIfMissing: false))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeGivePlayerNamedRegex.Match(tradeText), sentByPlayer: true, usePendingNameIfMissing: false))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeSentPendingRegex.Match(tradeText), sentByPlayer: false, usePendingNameIfMissing: true))
            return;
        if (TryAppendDealerTurnTradeFromMatch(TradeReceivedPendingRegex.Match(tradeText), sentByPlayer: true, usePendingNameIfMissing: true))
            return;
        if (TryAppendDealerTurnTradeByPhrase(tradeText))
            return;
    }

    private bool TryAppendDealerTurnTradeFromMatch(Match match, bool sentByPlayer, bool usePendingNameIfMissing)
    {
        if (!match.Success)
            return false;

        string playerName = ResolveDealerTurnLogPlayerName(match.Groups["name"].Success ? match.Groups["name"].Value : string.Empty);
        if (string.IsNullOrWhiteSpace(playerName) && usePendingNameIfMissing)
            playerName = ResolveDealerTurnLogPlayerName(dealerTurnLogPendingTradePlayer);

        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        BigInteger amount = ParseBankValue(match.Groups["amount"].Success ? match.Groups["amount"].Value : string.Empty);
        if (amount <= BigInteger.Zero)
            return false;

        AppendDealerTurnTrade(playerName, sentByPlayer, amount);
        return true;
    }

    private void AppendDealerTurnTrade(string playerName, bool sentByPlayer, BigInteger amount)
    {
        var entry = GetOrCreateDealerTurnLogEntry(playerName);
        string directionText = sentByPlayer ? "Sent" : "Received";
        string formattedAmount = FormatNumber(amount);

        ApplyTradeToDealerTurnLogEntry(entry, sentByPlayer, amount, formattedAmount);

        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        DealerTurnLogEntry? localEntry = FindActiveLocalDealerTurnLogEntry();
        bool shouldMirrorToLocal =
            localEntry != null &&
            !string.IsNullOrWhiteSpace(localPlayerName) &&
            !NamesRoughlyMatch(NormalizeLooseText(entry.PlayerName), NormalizeLooseText(localPlayerName));

        if (shouldMirrorToLocal)
            ApplyTradeToDealerTurnLogEntry(localEntry!, sentByPlayer, amount, formattedAmount);

        dealerTurnLogPendingTradePlayer = entry.PlayerName;
        dealerTurnLogPendingTradeInitiatedByLocal = !sentByPlayer;
        dealerTurnLogPendingTradeUntilUtc = DateTime.UtcNow.AddMinutes(2);
        MarkDealerTurnLogConfigurationDirty();
        AddDebugLog("TURNLOG", $"Trade logged | Player='{entry.PlayerName}' | Direction='{directionText}' | Amount={formattedAmount} | ReceivedTotal='{entry.TotalReceivedGil}' | SentTotal='{entry.TotalSentGil}'");

        AppendDealerTableHistoryTrade(playerName, sentByPlayer, amount);

        if (shouldMirrorToLocal && localEntry != null)
        {
            AddDebugLog("TURNLOG", $"Trade mirrored to local player | Player='{localEntry.PlayerName}' | Direction='{directionText}' | Amount={formattedAmount} | ReceivedTotal='{localEntry.TotalReceivedGil}' | SentTotal='{localEntry.TotalSentGil}'");
        }
    }

    private void ApplyTradeToDealerTurnLogEntry(DealerTurnLogEntry entry, bool sentByPlayer, BigInteger amount, string formattedAmount)
    {
        entry.EnsureInitialized();
        string directionText = sentByPlayer ? "Sent" : "Received";
        string timelineMessage = $"{entry.PlayerName} {directionText} {formattedAmount} Gil";
        string line = $"[{FormatEstTimestamp(GetEstNow())}] {timelineMessage}";
        entry.TradeLines.Add(line);
        AppendDealerTurnLogMatchHistoryLine(entry, timelineMessage);
        dealerTurnLogLastSeenUtc[entry.PlayerName] = DateTime.UtcNow;

        if (sentByPlayer)
        {
            BigInteger currentReceived = ParseBankValue(entry.TotalReceivedGil);
            entry.TotalReceivedGil = FormatNumber(currentReceived + amount);
        }
        else
        {
            BigInteger currentSent = ParseBankValue(entry.TotalSentGil);
            entry.TotalSentGil = FormatNumber(currentSent + amount);
        }

        MarkDealerTurnLogFilesDirty(entry, writeChatFile: false, writeTradeFile: true);
    }

    private void ClearDealerTurnLogPendingTrade()
    {
        dealerTurnLogPendingTradePlayer = string.Empty;
        dealerTurnLogPendingTradeInitiatedByLocal = false;
        dealerTurnLogPendingTradeUntilUtc = DateTime.MinValue;
    }

    private bool TryAppendDealerTurnTradeByPhrase(string tradeText)
    {
        BigInteger amount = ParseBankValue(tradeText);
        if (amount <= BigInteger.Zero)
            return false;

        string playerName = ResolveDealerTurnLogPlayerName(ExtractTradeCounterpartyName(tradeText));
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = ResolveDealerTurnLogPlayerName(dealerTurnLogPendingTradePlayer);

        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        if (tradeText.StartsWith("You hand over ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You offer ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You offered ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You give ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You gave ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You send ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You sent ", StringComparison.OrdinalIgnoreCase))
        {
            AppendDealerTurnTrade(playerName, sentByPlayer: false, amount);
            return true;
        }

        if (tradeText.StartsWith("You receive ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You received ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You get ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.StartsWith("You got ", StringComparison.OrdinalIgnoreCase))
        {
            AppendDealerTurnTrade(playerName, sentByPlayer: true, amount);
            return true;
        }

        if (tradeText.Contains(" gives you ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.Contains(" gave you ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.Contains(" sends you ", StringComparison.OrdinalIgnoreCase) ||
            tradeText.Contains(" sent you ", StringComparison.OrdinalIgnoreCase))
        {
            AppendDealerTurnTrade(playerName, sentByPlayer: true, amount);
            return true;
        }

        if (tradeText.Contains(" receives ", StringComparison.OrdinalIgnoreCase) &&
            tradeText.Contains(" from you", StringComparison.OrdinalIgnoreCase))
        {
            AppendDealerTurnTrade(playerName, sentByPlayer: false, amount);
            return true;
        }

        return false;
    }

    private string ExtractTradeCounterpartyName(string tradeText)
    {
        if (string.IsNullOrWhiteSpace(tradeText))
            return string.Empty;

        string[] suffixMarkers =
        {
            " wishes to trade with you",
            " would like to trade with you",
            " gives you ",
            " gave you ",
            " sends you ",
            " sent you ",
            " receives ",
            " received ",
            " from you"
        };

        foreach (string marker in suffixMarkers)
        {
            int index = tradeText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
                return tradeText[..index].Trim();
        }

        string[] prefixMarkers =
        {
            "Trade request sent to ",
            "Awaiting trade confirmation from ",
            "You requested a trade with ",
            "You request a trade with ",
            "You receive ",
            "You received "
        };

        foreach (string marker in prefixMarkers)
        {
            int index = tradeText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            string tail = tradeText[(index + marker.Length)..].Trim();
            int gilIndex = tail.IndexOf(" gil", StringComparison.OrdinalIgnoreCase);
            if (gilIndex > 0)
                tail = tail[(gilIndex + 4)..].Trim();
            return tail.Trim('.').Trim();
        }

        return string.Empty;
    }

    private string ResolveDealerTurnLogPlayerName(string rawName)
    {
        string cleanName = StripWorldSuffix(rawName);
        if (string.IsNullOrWhiteSpace(cleanName))
            return string.Empty;

        string normalized = NormalizeLooseText(cleanName);
        if (string.IsNullOrWhiteSpace(normalized))
            return cleanName;

        var candidates = new List<string>();
        string localPlayerName = StripWorldSuffix(Plugin.PlayerState.CharacterName ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(localPlayerName))
            candidates.Add(localPlayerName);

        for (int i = 0; i < Plugin.PartyList.Length; i++)
        {
            var member = Plugin.PartyList[i];
            if (member == null)
                continue;

            string memberName = StripWorldSuffix(member.Name.TextValue ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(memberName))
                candidates.Add(memberName);
        }

        foreach (var entry in GetDealerTurnLogEntries())
        {
            if (!string.IsNullOrWhiteSpace(entry.PlayerName))
                candidates.Add(entry.PlayerName);
        }

        foreach (string candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string candidateNormalized = NormalizeLooseText(candidate);
            if (NamesRoughlyMatch(candidateNormalized, normalized))
                return candidate;

            if (normalized.StartsWith(candidateNormalized, StringComparison.OrdinalIgnoreCase) ||
                candidateNormalized.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return cleanName;
    }

    private static string SanitizeTradeSystemMessage(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var builder = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '.' || c == ',' || c == ':' || c == ';' || c == '-' || c == '\'' )
                builder.Append(c);
        }

        return builder.ToString().Trim();
    }

    private string GetDealerTurnLogExportDirectory()
    {
        string configured = Plugin.Configuration.HistoryExportDirectory?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private string BuildDealerTurnLogFilePath(DealerTurnLogEntry entry, bool exportChat)
    {
        string outputDirectory = GetDealerTurnLogExportDirectory();
        string dateLabel = GetDealerTurnLogFileDateLabel(entry);
        string safeName = SanitizeFileName(entry.PlayerName);
        string kind = exportChat ? "Chat logs" : "Trade logs";
        return Path.Combine(outputDirectory, $"[{dateLabel}] {safeName} {kind}.txt");
    }

    private string GetDealerTurnLogDeletedStateFilePath()
    {
        return Path.Combine(GetDealerTurnLogExportDirectory(), ".gambabank_log_deletions.json");
    }

    private string GetDealerTurnLogDeletedStateKey(DealerTurnLogEntry entry, bool exportChat)
    {
        return BuildDealerTurnLogFilePath(entry, exportChat);
    }

    private void EnsureDealerTurnLogDeletedStateLoaded()
    {
        if (dealerTurnLogDeletedStateLoaded)
            return;

        dealerTurnLogDeletedStateLoaded = true;
        dealerTurnLogDeletedState.Clear();

        try
        {
            string statePath = GetDealerTurnLogDeletedStateFilePath();
            if (!File.Exists(statePath))
                return;

            string json = File.ReadAllText(statePath, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (loaded == null)
                return;

            foreach (var pair in loaded)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    dealerTurnLogDeletedState[pair.Key] = pair.Value;
            }
        }
        catch
        {
            dealerTurnLogDeletedState.Clear();
        }
    }

    private void SaveDealerTurnLogDeletedState()
    {
        try
        {
            string outputDirectory = GetDealerTurnLogExportDirectory();
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string statePath = GetDealerTurnLogDeletedStateFilePath();
            if (dealerTurnLogDeletedState.Count == 0)
            {
                if (File.Exists(statePath))
                    File.Delete(statePath);
                return;
            }

            string json = JsonSerializer.Serialize(dealerTurnLogDeletedState, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(statePath, json, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private string GetDealerTurnLogDeletedTimestamp(DealerTurnLogEntry entry, bool exportChat)
    {
        EnsureDealerTurnLogDeletedStateLoaded();
        string key = GetDealerTurnLogDeletedStateKey(entry, exportChat);
        return dealerTurnLogDeletedState.TryGetValue(key, out var timestamp) ? timestamp : string.Empty;
    }

    private string GetDealerTurnLogFileDateLabel(DealerTurnLogEntry entry)
    {
        DateTime timestamp = ParseTimestamp(entry.JoinedTimestamp);
        if (timestamp == DateTime.MinValue)
            timestamp = GetEstNow();

        return timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private string BuildDealerTurnLogFileContent(DealerTurnLogEntry entry, bool exportChat)
    {
        entry.EnsureInitialized();

        string displayHouse = GetDealerTurnLogDisplayHouse(entry);
        string housePrefix = string.IsNullOrWhiteSpace(displayHouse) ? string.Empty : $"[{displayHouse}] ";
        var sourceLines = exportChat ? entry.ChatLines : entry.TradeLines;
        var lines = new List<string>(sourceLines.Count + 6);
        var shiftActionLines = new List<string>();

        foreach (string sourceLine in sourceLines)
        {
            if (string.IsNullOrWhiteSpace(sourceLine))
                continue;

            if (exportChat && IsDealerTurnLogShiftActionLine(sourceLine))
            {
                shiftActionLines.Add(sourceLine.Substring(GetDealerTurnLogShiftActionPrefix().Length));
                continue;
            }

            lines.Add(string.IsNullOrWhiteSpace(housePrefix) ? sourceLine : housePrefix + sourceLine);
        }

        if (exportChat &&
            !string.IsNullOrWhiteSpace(entry.LeftTimestamp) &&
            string.Equals(entry.LeftReason, "LeftParty", StringComparison.OrdinalIgnoreCase))
        {
            string leftLine = $"[{entry.LeftTimestamp}] {entry.PlayerName} Left the party.";
            lines.Add(string.IsNullOrWhiteSpace(housePrefix) ? leftLine : housePrefix + leftLine);
        }

        if (exportChat && shiftActionLines.Count > 0)
        {
            if (lines.Count > 0)
                lines.Add("----------");

            foreach (string shiftLine in shiftActionLines)
                lines.Add(string.IsNullOrWhiteSpace(housePrefix) ? shiftLine : housePrefix + shiftLine);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void EnsureDealerTurnLogFilesExist(DealerTurnLogEntry entry, bool writeChatFile = true, bool writeTradeFile = true)
    {
        entry.EnsureInitialized();

        if (writeChatFile)
            WriteDealerTurnLogFile(entry, exportChat: true, updateStatus: false);

        if (writeTradeFile)
            WriteDealerTurnLogFile(entry, exportChat: false, updateStatus: false);
    }

    private void EnsureAllDealerTurnLogFilesExist()
    {
        foreach (var entry in GetDealerTurnLogEntries())
            EnsureDealerTurnLogFilesExist(entry);
        FlushPendingDealerTurnLogWrites(force: true);
        FlushPendingDealerTurnLogConfigurationSave(force: true);
    }

    private void WriteDealerTurnLogFile(DealerTurnLogEntry entry, bool exportChat, bool updateStatus)
    {
        entry.EnsureInitialized();
        string outputDirectory = GetDealerTurnLogExportDirectory();
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        string filePath = BuildDealerTurnLogFilePath(entry, exportChat);
        string content = BuildDealerTurnLogFileContent(entry, exportChat);

        File.WriteAllText(filePath, content, Encoding.UTF8);
        EnsureDealerTurnLogDeletedStateLoaded();
        string deleteKey = GetDealerTurnLogDeletedStateKey(entry, exportChat);
        if (dealerTurnLogDeletedState.Remove(deleteKey))
            SaveDealerTurnLogDeletedState();

        if (exportChat)
            entry.ChatExportFilePath = filePath;
        else
            entry.TradeExportFilePath = filePath;

        if (updateStatus)
        {
            dealerTurnLogExportStatusText = "Saved";
            dealerTurnLogExportStatusPath = outputDirectory;
            dealerTurnLogExportStatusFailed = false;
            dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
        }
    }

    private void MarkDealerTurnLogConfigurationDirty(int delayMilliseconds = 800)
    {
        dealerTurnLogConfigurationDirty = true;
        DateTime targetUtc = DateTime.UtcNow.AddMilliseconds(delayMilliseconds);
        if (nextDealerTurnLogConfigurationSaveUtc == DateTime.MinValue || targetUtc < nextDealerTurnLogConfigurationSaveUtc)
            nextDealerTurnLogConfigurationSaveUtc = targetUtc;
    }

    private void FlushPendingDealerTurnLogConfigurationSave(bool force)
    {
        if (!dealerTurnLogConfigurationDirty)
            return;
        if (!force && DateTime.UtcNow < nextDealerTurnLogConfigurationSaveUtc)
            return;

        Plugin.Configuration.Save();
        dealerTurnLogConfigurationDirty = false;
        nextDealerTurnLogConfigurationSaveUtc = DateTime.MinValue;
    }

    private void MarkDealerTurnLogFilesDirty(DealerTurnLogEntry entry, bool writeChatFile = true, bool writeTradeFile = true)
    {
        entry.EnsureInitialized();
        string key = GetDealerTurnLogSelectionKey(entry);
        if (writeChatFile)
            pendingDealerTurnLogChatFileWrites.Add(key);
        if (writeTradeFile)
            pendingDealerTurnLogTradeFileWrites.Add(key);

        DateTime targetUtc = DateTime.UtcNow.AddMilliseconds(500);
        if (nextDealerTurnLogFileFlushUtc == DateTime.MinValue || targetUtc < nextDealerTurnLogFileFlushUtc)
            nextDealerTurnLogFileFlushUtc = targetUtc;
    }

    private void FlushPendingDealerTurnLogWrites(bool force)
    {
        if (pendingDealerTurnLogChatFileWrites.Count == 0 && pendingDealerTurnLogTradeFileWrites.Count == 0)
            return;
        if (!force && DateTime.UtcNow < nextDealerTurnLogFileFlushUtc)
            return;

        var entries = GetDealerTurnLogEntries();
        var entryMap = new Dictionary<string, DealerTurnLogEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var existingEntry in entries)
        {
            existingEntry.EnsureInitialized();
            entryMap[GetDealerTurnLogSelectionKey(existingEntry)] = existingEntry;
        }

        int remainingBudget = force ? int.MaxValue : 2;

        foreach (string key in pendingDealerTurnLogChatFileWrites.ToList())
        {
            if (remainingBudget <= 0)
                break;
            if (!entryMap.TryGetValue(key, out var entry))
            {
                pendingDealerTurnLogChatFileWrites.Remove(key);
                continue;
            }

            WriteDealerTurnLogFile(entry, exportChat: true, updateStatus: false);
            pendingDealerTurnLogChatFileWrites.Remove(key);
            remainingBudget--;
        }

        foreach (string key in pendingDealerTurnLogTradeFileWrites.ToList())
        {
            if (remainingBudget <= 0)
                break;
            if (!entryMap.TryGetValue(key, out var entry))
            {
                pendingDealerTurnLogTradeFileWrites.Remove(key);
                continue;
            }

            WriteDealerTurnLogFile(entry, exportChat: false, updateStatus: false);
            pendingDealerTurnLogTradeFileWrites.Remove(key);
            remainingBudget--;
        }

        nextDealerTurnLogFileFlushUtc = (pendingDealerTurnLogChatFileWrites.Count == 0 && pendingDealerTurnLogTradeFileWrites.Count == 0)
            ? DateTime.MinValue
            : DateTime.UtcNow.AddMilliseconds(350);
    }

    private void ExportDealerTurnLogFile(DealerTurnLogEntry entry, bool exportChat)
    {
        string configuredDirectory = Plugin.Configuration.HistoryExportDirectory?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            dealerTurnLogExportStatusText = "Insert file directory path first";
            dealerTurnLogExportStatusPath = string.Empty;
            dealerTurnLogExportStatusFailed = true;
            dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
            ImGui.OpenPopup("##DealerTurnLogDirectoryRequiredPopup");
            AddDebugLog("TURNLOG", $"Blocked {(exportChat ? "chat" : "trade")} log export because file directory path is empty.");
            return;
        }

        try
        {
            WriteDealerTurnLogFile(entry, exportChat, updateStatus: true);
            Plugin.Configuration.Save();
            string filePath = exportChat ? entry.ChatExportFilePath : entry.TradeExportFilePath;
            AddDebugLog("TURNLOG", $"Saved dealer turn {(exportChat ? "chat" : "trade")} log: {filePath}");
        }
        catch (Exception ex)
        {
            dealerTurnLogExportStatusText = $"Failed to save {(exportChat ? "chat" : "trade")} log";
            dealerTurnLogExportStatusPath = string.Empty;
            dealerTurnLogExportStatusFailed = true;
            dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
            AddDebugLog("ERR", $"Dealer turn log export failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private bool DealerTurnLogFileExists(DealerTurnLogEntry entry, bool exportChat)
    {
        string filePath = BuildDealerTurnLogFilePath(entry, exportChat);
        return File.Exists(filePath);
    }

    private void DeleteDealerTurnLogFile(DealerTurnLogEntry entry, bool exportChat)
    {
        try
        {
            entry.EnsureInitialized();

            string canonicalPath = BuildDealerTurnLogFilePath(entry, exportChat);
            string storedPath = exportChat ? entry.ChatExportFilePath : entry.TradeExportFilePath;

            var pathsToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                canonicalPath,
                canonicalPath + ".deleted"
            };

            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                pathsToDelete.Add(storedPath);
                if (!storedPath.EndsWith(".deleted", StringComparison.OrdinalIgnoreCase))
                    pathsToDelete.Add(storedPath + ".deleted");
            }

            foreach (string path in pathsToDelete)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (File.Exists(path))
                    File.Delete(path);
            }

            EnsureDealerTurnLogDeletedStateLoaded();
            string deletedAt = FormatEstTimestamp(GetEstNow());
            dealerTurnLogDeletedState[GetDealerTurnLogDeletedStateKey(entry, exportChat)] = deletedAt;
            SaveDealerTurnLogDeletedState();

            if (exportChat)
                entry.ChatExportFilePath = string.Empty;
            else
                entry.TradeExportFilePath = string.Empty;

            dealerTurnLogExportStatusText = $"Deleted {(exportChat ? "chat" : "trade")} log";
            dealerTurnLogExportStatusPath = GetDealerTurnLogExportDirectory();
            dealerTurnLogExportStatusFailed = false;
            dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
            Plugin.Configuration.Save();
            AddDebugLog("TURNLOG", $"Deleted dealer turn {(exportChat ? "chat" : "trade")} log(s): {string.Join(" | ", pathsToDelete)}");
        }
        catch (Exception ex)
        {
            dealerTurnLogExportStatusText = $"Failed to delete {(exportChat ? "chat" : "trade")} log";
            dealerTurnLogExportStatusPath = string.Empty;
            dealerTurnLogExportStatusFailed = true;
            dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
            AddDebugLog("ERR", $"Dealer turn log delete failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void DrawDealerTurnLogFileCell(DealerTurnLogEntry entry, bool exportChat, Vector4 playerColor)
    {
        string deletedTimestamp = GetDealerTurnLogDeletedTimestamp(entry, exportChat);
        bool wasDeleted = !string.IsNullOrWhiteSpace(deletedTimestamp);
        string safeName = SanitizeFileName(entry.PlayerName);
        Vector4 buttonColor = exportChat ? CopyButtonColor : Hex("#DA9E00");
        string buttonId = $"DealerTurnLogExport{(exportChat ? "Chat" : "Trade")}{safeName}";

        const float actionButtonWidth = 24f;
        const float buttonSpacing = 10f;
        float contentWidth = (actionButtonWidth * 2f) + buttonSpacing;

        float verticalOffset = GetDealerTurnLogRowVerticalOffset() - 1.0f;
        if (verticalOffset > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

        float columnWidth = ImGui.GetColumnWidth();
        float startX = ImGui.GetCursorPosX() + MathF.Max(0f, (columnWidth - contentWidth) * 0.5f);
        ImGui.SetCursorPosX(startX);

        if (DrawExportDirectoryIconButton(buttonId, new Vector2(actionButtonWidth, 0f), buttonColor))
            ExportDealerTurnLogFile(entry, exportChat);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Save logs");

        ImGui.SameLine(0f, buttonSpacing);

        Vector4 deleteColor = wasDeleted ? WithAlpha(Darken(Hex("#b30000"), 0.15f), 0.45f) : Hex("#b30000");
        Vector4 deleteTextColor = wasDeleted ? WithAlpha(WhiteText, 0.68f) : WhiteText;
        string deleteButtonId = $"DealerTurnLogDelete{(exportChat ? "Chat" : "Trade")}{safeName}";
        bool deletePressed = DrawStyledBoldButton("X", deleteButtonId, new Vector2(actionButtonWidth, 0f), deleteColor, deleteTextColor);

        if (deletePressed && !wasDeleted)
            DeleteDealerTurnLogFile(entry, exportChat);

        if (ImGui.IsItemHovered())
        {
            if (wasDeleted)
                ImGui.SetTooltip($"Deleted on {deletedTimestamp}");
            else
                ImGui.SetTooltip("Delete logs");
        }
    }


    private void DrawDealerTurnLogHouseCell(DealerTurnLogEntry entry)
    {
        entry.EnsureInitialized();

        const float editButtonWidth = 20f;
        const float buttonSpacing = 6f;
        const float buttonLeftPadding = 2f;
        const float textRightPadding = 2f;

        string houseText = (entry.House ?? string.Empty).Trim();
        float cellStartX = ImGui.GetCursorPosX();
        float cellStartY = ImGui.GetCursorPosY();
        float columnWidth = ImGui.GetColumnWidth();
        float textYOffset = GetDealerTurnLogRowVerticalOffset();
        float buttonHeight = MathF.Max(18f, ImGui.GetFrameHeight() - 4f);
        float buttonYOffset = MathF.Max(0f, ((ImGui.GetFrameHeight() - buttonHeight) * 0.5f) + 1f);
        float buttonX = cellStartX + buttonLeftPadding;
        float textX = buttonX + editButtonWidth + buttonSpacing;
        float textAvailableWidth = MathF.Max(0f, (cellStartX + columnWidth) - textX - textRightPadding);
        string displayText = FitTextToWidth(houseText, textAvailableWidth);

        ImGui.SetCursorPos(new Vector2(buttonX, cellStartY + buttonYOffset));
        string safeName = SanitizeFileName(entry.PlayerName);
        if (DrawPencilIconButton($"DealerTurnLogHouseEdit{safeName}", new Vector2(editButtonWidth, buttonHeight), Hex("#c075eb")))
        {
            dealerTurnLogEditingHouseEntry = entry;
            dealerTurnLogHouseEditInput = entry.House ?? string.Empty;
            openDealerTurnLogHouseEditPopupNextFrame = true;
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Edit house/club");

        if (!string.IsNullOrWhiteSpace(displayText))
        {
            Vector2 screenStart = ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + new Vector2(textX, cellStartY + textYOffset);
            ImGui.GetWindowDrawList().AddText(screenStart, ImGui.ColorConvertFloat4ToU32(NeutralColor), displayText);
        }
    }

    private void DrawDealerTurnLogHouseEditPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(360f, 0f), ImGuiCond.Appearing);
        if (!ImGui.BeginPopupModal("##DealerTurnLogHouseEditPopup", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted("Edit House/Club");
        ImGui.Spacing();
        ImGui.SetNextItemWidth(300f);
        ImGui.InputText("##DealerTurnLogHouseEditInput", ref dealerTurnLogHouseEditInput, 256, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue);

        if (ImGui.IsItemDeactivatedAfterEdit() && ImGui.IsKeyPressed(ImGuiKey.Enter) && dealerTurnLogEditingHouseEntry != null)
        {
            dealerTurnLogEditingHouseEntry.House = (dealerTurnLogHouseEditInput ?? string.Empty).Trim();
            dealerTurnLogEditingHouseEntry.HouseEditedManually = true;
            Plugin.Configuration.Save();
            ImGui.CloseCurrentPopup();
        }

        ImGui.Spacing();
        if (DrawStyledBoldButton("Save", "DealerTurnLogHouseEditSave", new Vector2(72f, 0f), CopyButtonColor, WhiteText))
        {
            if (dealerTurnLogEditingHouseEntry != null)
            {
                dealerTurnLogEditingHouseEntry.House = (dealerTurnLogHouseEditInput ?? string.Empty).Trim();
                dealerTurnLogEditingHouseEntry.HouseEditedManually = true;
                Plugin.Configuration.Save();
            }
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear", "DealerTurnLogHouseEditClear", new Vector2(72f, 0f), LossButtonColor, WhiteText))
        {
            if (dealerTurnLogEditingHouseEntry != null)
            {
                dealerTurnLogEditingHouseEntry.House = string.Empty;
                dealerTurnLogEditingHouseEntry.HouseEditedManually = true;
                Plugin.Configuration.Save();
            }
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Cancel", "DealerTurnLogHouseEditCancel", new Vector2(72f, 0f), UtilityButtonColor, WhiteText))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private string FitTextToWidth(string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 4f)
            return string.Empty;

        if (ImGui.CalcTextSize(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
        float ellipsisWidth = ImGui.CalcTextSize(ellipsis).X;
        if (ellipsisWidth >= maxWidth)
            return string.Empty;

        int low = 0;
        int high = text.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            string candidate = text[..mid] + ellipsis;
            if (ImGui.CalcTextSize(candidate).X <= maxWidth)
                low = mid;
            else
                high = mid - 1;
        }

        return text[..Math.Max(0, low)] + ellipsis;
    }

    private bool DrawPencilIconButton(string id, Vector2 size, Vector4 baseColor)
    {
        Vector2 actualSize = size;
        if (actualSize.X <= 0f)
            actualSize.X = 20f;
        if (actualSize.Y <= 0f)
            actualSize.Y = MathF.Max(18f, ImGui.GetFrameHeight() - 4f);

        Vector4 hoverColor = Lighten(baseColor, 0.18f);
        Vector4 activeColor = Lighten(baseColor, 0.08f);

        ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f));

        bool pressed = ImGui.Button($"##{id}", actualSize);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        uint iconColor = ImGui.ColorConvertFloat4ToU32(WhiteText);

        float width = max.X - min.X;
        float height = max.Y - min.Y;
        float stroke = MathF.Max(1.8f, MathF.Min(width, height) * 0.10f);
        float rounding = MathF.Max(1.8f, MathF.Min(width, height) * 0.16f);

        Vector2 frameTL = new(min.X + width * 0.16f, min.Y + height * 0.22f);
        Vector2 frameBL = new(min.X + width * 0.16f, min.Y + height * 0.82f);
        Vector2 frameBR = new(min.X + width * 0.70f, min.Y + height * 0.82f);
        Vector2 frameTR = new(min.X + width * 0.70f, min.Y + height * 0.52f);
        Vector2 frameTopEnd = new(min.X + width * 0.47f, min.Y + height * 0.22f);

        drawList.AddLine(frameTL, frameBL, iconColor, stroke);
        drawList.AddLine(frameBL, frameBR, iconColor, stroke);
        drawList.AddLine(frameBR, frameTR, iconColor, stroke);
        drawList.AddLine(frameTL, frameTopEnd, iconColor, stroke);

        Vector2 axisStart = new(min.X + width * 0.45f, min.Y + height * 0.68f);
        Vector2 axisEnd = new(min.X + width * 0.78f, min.Y + height * 0.34f);
        Vector2 axis = axisEnd - axisStart;
        float axisLength = MathF.Max(0.001f, axis.Length());
        Vector2 dir = axis / axisLength;
        Vector2 perp = new Vector2(-dir.Y, dir.X) * (MathF.Min(width, height) * 0.105f);

        Vector2 bodyA = axisStart - perp;
        Vector2 bodyB = axisEnd - perp;
        Vector2 bodyC = axisEnd + perp;
        Vector2 bodyD = axisStart + perp;
        drawList.AddQuad(bodyA, bodyB, bodyC, bodyD, iconColor, stroke);

        Vector2 tipPoint = axisStart - (dir * (MathF.Min(width, height) * 0.11f));
        drawList.AddTriangle(bodyA, tipPoint, bodyD, iconColor, stroke);

        Vector2 eraserLeft = axisEnd - perp;
        Vector2 eraserRight = axisEnd + perp;
        Vector2 eraserOffset = dir * (MathF.Min(width, height) * 0.11f);
        drawList.AddLine(eraserLeft, eraserLeft + eraserOffset, iconColor, stroke);
        drawList.AddLine(eraserRight, eraserRight + eraserOffset, iconColor, stroke);
        drawList.AddLine(eraserLeft + eraserOffset, eraserRight + eraserOffset, iconColor, stroke);

        ImGui.PopStyleColor(4);
        return pressed;
    }

    private void DrawDealerTurnLogDirectoryRequiredPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(320f, 0f), ImGuiCond.Appearing);
        if (!ImGui.BeginPopupModal("##DealerTurnLogDirectoryRequiredPopup", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextWrapped("Insert file directory path first");
        ImGui.Spacing();

        float buttonWidth = 88f;
        float startX = MathF.Max(0f, (ImGui.GetContentRegionAvail().X - buttonWidth) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + startX);
        if (DrawStyledBoldButton("OK", "DealerTurnLogDirectoryRequiredPopupOk", new Vector2(buttonWidth, 0f), UtilityButtonColor, WhiteText))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private void RequestOpenDealerTurnLogDirectoryBrowser()
    {
        openDealerTurnLogDirectoryBrowserNextFrame = true;
    }

    private void OpenDealerTurnLogDirectoryBrowser()
    {
        string configured = Plugin.Configuration.HistoryExportDirectory?.Trim() ?? string.Empty;
        string startPath = configured;

        if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            startPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            startPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            startPath = Path.GetPathRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;

        try
        {
            startPath = Path.GetFullPath(startPath);
        }
        catch
        {
        }

        dealerTurnLogDirectoryBrowserCurrentPath = startPath;
        dealerTurnLogDirectoryBrowserSelectedPath = startPath;
        dealerTurnLogDirectoryBrowserSearchInput = string.Empty;
        dealerTurnLogDirectoryBrowserErrorText = string.Empty;
        ImGui.OpenPopup("##DealerTurnLogDirectoryBrowserPopup");
    }

    private void DrawDealerTurnLogDirectoryBrowserPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(820f, 520f), ImGuiCond.Appearing);
        if (!ImGui.BeginPopupModal("##DealerTurnLogDirectoryBrowserPopup", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
            return;

        ImGui.TextUnformatted("Choose Files Directory");
        ImGui.Separator();

        float footerHeight = ImGui.GetFrameHeightWithSpacing() * 3.5f;
        float sidebarWidth = 180f;

        if (ImGui.BeginChild("##DealerTurnLogDirectorySidebar", new Vector2(sidebarWidth, -footerHeight), true))
        {
            ImGui.TextUnformatted("Quick Access");
            ImGui.Separator();

            DrawDealerTurnLogDirectoryShortcutButton("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            DrawDealerTurnLogDirectoryShortcutButton("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            DrawDealerTurnLogDirectoryShortcutButton("Downloads", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            DrawDealerTurnLogDirectoryShortcutButton("User Profile", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            ImGui.Spacing();
            ImGui.TextUnformatted("Drives");
            ImGui.Separator();

            foreach (string drivePath in GetDealerTurnLogBrowserDrivePaths())
            {
                string label = drivePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(label))
                    label = drivePath;

                DrawDealerTurnLogDirectoryShortcutButton(label, drivePath);
            }
        }

        ImGui.EndChild();

        ImGui.SameLine();

        if (ImGui.BeginChild("##DealerTurnLogDirectoryContent", new Vector2(0f, -footerHeight), true))
        {
            DrawDealerTurnLogDirectoryBreadcrumbs(dealerTurnLogDirectoryBrowserCurrentPath);

            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint("##DealerTurnLogDirectorySearch", "Search folders", ref dealerTurnLogDirectoryBrowserSearchInput, 256);
            ImGui.Separator();

            string currentPath = dealerTurnLogDirectoryBrowserCurrentPath;
            bool currentExists = !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath);

            if (currentExists)
            {
                string? parentPath = null;
                try
                {
                    parentPath = Directory.GetParent(currentPath)?.FullName;
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(parentPath) && DrawDealerTurnLogDirectoryEntry("..", parentPath, false))
                {
                    dealerTurnLogDirectoryBrowserCurrentPath = parentPath;
                    dealerTurnLogDirectoryBrowserSelectedPath = parentPath;
                    dealerTurnLogDirectoryBrowserErrorText = string.Empty;
                }

                IEnumerable<string> subDirectories = Array.Empty<string>();
                try
                {
                    subDirectories = Directory.GetDirectories(currentPath).OrderBy(static dir => Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    dealerTurnLogDirectoryBrowserErrorText = ex.Message;
                }

                string search = dealerTurnLogDirectoryBrowserSearchInput?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    subDirectories = subDirectories.Where(dir =>
                    {
                        string name = Path.GetFileName(dir);
                        return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                    });
                }

                foreach (string directory in subDirectories)
                {
                    string name = Path.GetFileName(directory);
                    if (string.IsNullOrWhiteSpace(name))
                        name = directory;

                    if (DrawDealerTurnLogDirectoryEntry(name, directory, false))
                    {
                        dealerTurnLogDirectoryBrowserCurrentPath = directory;
                        dealerTurnLogDirectoryBrowserSelectedPath = directory;
                        dealerTurnLogDirectoryBrowserErrorText = string.Empty;
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("Directory not available.");
            }
        }

        ImGui.EndChild();

        ImGui.Spacing();
        ImGui.TextUnformatted("Directory Path:");
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText("##DealerTurnLogDirectorySelectedPath", ref dealerTurnLogDirectoryBrowserSelectedPath, 512))
        {
            dealerTurnLogDirectoryBrowserErrorText = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(dealerTurnLogDirectoryBrowserErrorText))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, LossColor);
            ImGui.TextWrapped(dealerTurnLogDirectoryBrowserErrorText);
            ImGui.PopStyleColor();
        }

        float actionButtonWidth = 82f;
        float buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
        float totalButtonsWidth = (actionButtonWidth * 2f) + buttonSpacing;
        float actionStartX = MathF.Max(0f, ImGui.GetContentRegionAvail().X - totalButtonsWidth);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + actionStartX);

        if (DrawStyledBoldButton("OK", "DealerTurnLogDirectoryBrowserOk", new Vector2(actionButtonWidth, 0f), Hex("#DA9E00"), WhiteText))
        {
            string selectedPath = dealerTurnLogDirectoryBrowserSelectedPath?.Trim() ?? string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(selectedPath))
                    selectedPath = Path.GetFullPath(selectedPath);
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                dealerTurnLogDirectoryBrowserErrorText = "Choose a directory first.";
            }
            else if (!Directory.Exists(selectedPath))
            {
                dealerTurnLogDirectoryBrowserErrorText = "That directory does not exist.";
            }
            else
            {
                Plugin.Configuration.HistoryExportDirectory = selectedPath;
                historyExportDirectoryInput = selectedPath;
                Plugin.Configuration.Save();
                dealerTurnLogDirectoryBrowserCurrentPath = selectedPath;
                dealerTurnLogDirectoryBrowserSelectedPath = selectedPath;
                dealerTurnLogDirectoryBrowserErrorText = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Cancel", "DealerTurnLogDirectoryBrowserCancel", new Vector2(actionButtonWidth, 0f), DangerButtonColor, WhiteText))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private void DrawDealerTurnLogDirectoryShortcutButton(string label, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        bool selected = string.Equals(dealerTurnLogDirectoryBrowserCurrentPath, path, StringComparison.OrdinalIgnoreCase);
        if (ImGui.Selectable(label, selected))
        {
            dealerTurnLogDirectoryBrowserCurrentPath = path;
            dealerTurnLogDirectoryBrowserSelectedPath = path;
            dealerTurnLogDirectoryBrowserErrorText = string.Empty;
        }
    }

    private bool DrawDealerTurnLogDirectoryEntry(string label, string uniquePath, bool selected)
    {
        bool clicked = ImGui.Selectable($"##DealerTurnLogDirectoryEntry{uniquePath}", selected, ImGuiSelectableFlags.SpanAllColumns);

        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();
        float itemHeight = max.Y - min.Y;
        float iconHeight = MathF.Min(12f, MathF.Max(9f, itemHeight * 0.52f));
        float iconWidth = iconHeight * 1.20f;
        float left = min.X + 7f;
        float top = min.Y + ((itemHeight - iconHeight) * 0.5f) + 1f;
        DrawSmallFilledFolderIcon(new Vector2(left, top), iconWidth, iconHeight, new Vector4(0.47f, 0.85f, 1.0f, 1.0f));

        Vector2 textSize = ImGui.CalcTextSize(label);
        float textX = left + iconWidth + 8f;
        float textY = min.Y + ((itemHeight - textSize.Y) * 0.5f);
        var drawList = ImGui.GetWindowDrawList();
        uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
        drawList.AddText(new Vector2(textX, textY), textColor, label);

        return clicked;
    }

    private void DrawSmallFilledFolderIcon(Vector2 topLeft, float width, float height, Vector4 color)
    {
        var drawList = ImGui.GetWindowDrawList();
        uint folderColor = ImGui.ColorConvertFloat4ToU32(color);
        uint highlightColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.18f));

        float bodyTop = topLeft.Y + (height * 0.22f);
        float bodyBottom = topLeft.Y + height;
        float bodyLeft = topLeft.X;
        float bodyRight = topLeft.X + width;
        float tabLeft = bodyLeft + (width * 0.06f);
        float tabTop = topLeft.Y;
        float tabRight = bodyLeft + (width * 0.46f);
        float tabBottom = bodyTop + (height * 0.10f);

        drawList.AddRectFilled(new Vector2(tabLeft, tabTop), new Vector2(tabRight, tabBottom), folderColor, 2f);
        drawList.AddRectFilled(new Vector2(bodyLeft, bodyTop), new Vector2(bodyRight, bodyBottom), folderColor, 2.2f);
        drawList.AddRectFilled(new Vector2(bodyLeft + 1.5f, bodyTop + 1.5f), new Vector2(bodyRight - 1.5f, bodyTop + (height * 0.42f)), highlightColor, 1.6f);
    }

    private IEnumerable<string> GetDealerTurnLogBrowserDrivePaths()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(drive =>
                {
                    try
                    {
                        return drive.IsReady || drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Select(drive => drive.RootDirectory.FullName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private void DrawDealerTurnLogDirectoryBreadcrumbs(string currentPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
            return;

        IReadOnlyList<(string Label, string Path)> segments = BuildDealerTurnLogDirectoryBreadcrumbs(currentPath);
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (i > 0)
            {
                ImGui.SameLine(0f, 4f);
                ImGui.TextUnformatted(">");
                ImGui.SameLine(0f, 4f);
            }

            if (ImGui.SmallButton($"{segment.Label}##DealerTurnLogDirectoryBreadcrumb{i}"))
            {
                dealerTurnLogDirectoryBrowserCurrentPath = segment.Path;
                dealerTurnLogDirectoryBrowserSelectedPath = segment.Path;
                dealerTurnLogDirectoryBrowserErrorText = string.Empty;
            }

            if (i < segments.Count - 1)
                ImGui.SameLine(0f, 4f);
        }
    }

    private IReadOnlyList<(string Label, string Path)> BuildDealerTurnLogDirectoryBreadcrumbs(string currentPath)
    {
        var breadcrumbs = new List<(string Label, string Path)>();

        try
        {
            string fullPath = Path.GetFullPath(currentPath);
            string root = Path.GetPathRoot(fullPath) ?? string.Empty;
            string normalizedRoot = root;

            if (!string.IsNullOrWhiteSpace(normalizedRoot))
            {
                string rootLabel = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(rootLabel))
                    rootLabel = normalizedRoot;

                breadcrumbs.Add((rootLabel, normalizedRoot));
            }

            string relative = fullPath.Substring(Math.Min(root.Length, fullPath.Length))
                .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!string.IsNullOrWhiteSpace(relative))
            {
                string running = string.IsNullOrWhiteSpace(root) ? string.Empty : root;
                string[] parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    running = string.IsNullOrWhiteSpace(running) ? part : Path.Combine(running, part);
                    breadcrumbs.Add((part, running));
                }
            }
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(currentPath))
                breadcrumbs.Add((currentPath, currentPath));
        }

        return breadcrumbs;
    }


    private void DrawDealerTurnLogHistoryWindow()
    {
        if (!showDealerTurnLogHistoryWindow)
            return;

        ImGui.SetNextWindowSize(new Vector2(980f, 470f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Dealer Log History###DealerTurnLogHistory", ref showDealerTurnLogHistoryWindow, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.End();
            return;
        }

        SyncDealerTurnLogHouseForActiveRows();

        DrawDealerTurnLogDirectoryRequiredPopup();

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Files directory path:");
        ImGui.SameLine();

        const float browseButtonWidth = 30f;
        const float browseButtonHeight = 24f;
        const float openFolderButtonWidth = 88f;
        const float desktopButtonWidth = 64f;
        const float docsButtonWidth = 48f;
        const float clearButtonWidth = 50f;
        float rowSpacing = ImGui.GetStyle().ItemSpacing.X;
        float buttonsRowWidth = browseButtonWidth + openFolderButtonWidth + desktopButtonWidth + docsButtonWidth + clearButtonWidth + (rowSpacing * 5f);

        if (DrawFolderBrowseIconButton("DealerTurnLogBrowseDirectoryButton", new Vector2(browseButtonWidth, browseButtonHeight), Hex("#9784e8")))
            RequestOpenDealerTurnLogDirectoryBrowser();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Browse folders");

        ImGui.SameLine();

        string dealerTurnDirectory = Plugin.Configuration.HistoryExportDirectory ?? string.Empty;
        float directoryFieldWidth = MathF.Max(180f, ImGui.GetContentRegionAvail().X - buttonsRowWidth);
        ImGui.SetNextItemWidth(directoryFieldWidth);
        if (ImGui.InputText("##DealerTurnLogDirectoryPath", ref dealerTurnDirectory, 512))
        {
            Plugin.Configuration.HistoryExportDirectory = dealerTurnDirectory;
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Open folder", "DealerTurnLogOpenFolderButton", new Vector2(openFolderButtonWidth, 0f), Hex("#DA9E00"), WhiteText))
            OpenDirectoryPath(GetDealerTurnLogExportDirectory());

        ImGui.SameLine();
        if (DrawStyledBoldButton("Desktop", "DealerTurnLogDesktopButton", new Vector2(desktopButtonWidth, 0f), UtilityButtonColor, WhiteText))
        {
            Plugin.Configuration.HistoryExportDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Docs", "DealerTurnLogDocumentsButton", new Vector2(docsButtonWidth, 0f), Hex("#ACC60F"), WhiteText))
        {
            Plugin.Configuration.HistoryExportDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        if (DrawStyledBoldButton("Clear", "DealerTurnLogDirectoryClearButton", new Vector2(clearButtonWidth, 0f), Hex("#b30000"), WhiteText))
        {
            Plugin.Configuration.HistoryExportDirectory = string.Empty;
            Plugin.Configuration.Save();
        }

        ImGui.Separator();
        DrawDealerTurnLogBulkActionsRow();
        ImGui.Spacing();
        ImGui.Separator();

        if (DateTime.UtcNow < dealerTurnLogExportStatusUntilUtc && !string.IsNullOrWhiteSpace(dealerTurnLogExportStatusText))
        {
            if (dealerTurnLogExportStatusFailed || string.IsNullOrWhiteSpace(dealerTurnLogExportStatusPath))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, dealerTurnLogExportStatusFailed ? LossColor : GoldColor);
                ImGui.TextUnformatted(dealerTurnLogExportStatusText);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
                ImGui.TextUnformatted($"{dealerTurnLogExportStatusText} ");
                ImGui.PopStyleColor();
                ImGui.SameLine(0f, 0f);
                DrawClickableInlineText(dealerTurnLogExportStatusPath, GoldColor, () => OpenDirectoryPath(dealerTurnLogExportStatusPath), false, "Click to open folder");
            }

            ImGui.Spacing();
        }

        ImGui.Text("Sort By:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(220f);
        if (ImGui.BeginCombo("##DealerTurnLogSortBy", dealerTurnLogSortBy))
        {
            DrawDealerTurnLogSortOption("Most Recent");
            DrawDealerTurnLogSortOption("Today");
            DrawDealerTurnLogSortOption("This Week");
            DrawDealerTurnLogSortOption("This Month");

            foreach (string houseOption in GetDealerTurnLogHouseSortOptions())
                DrawDealerTurnLogSortOption(houseOption);

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Text("Search:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(210f);
        ImGui.InputText("##DealerTurnLogSearch", ref dealerTurnLogSearchInput, 128);

        ImGui.Spacing();

        var filteredEntries = GetFilteredDealerTurnLogEntries();

        if (filteredEntries.Count == 0)
        {
            ImGui.TextDisabled("No dealer turn log entries found.");
            ImGui.End();
            return;
        }

        string colorSeed = $"{dealerTurnLogSortBy}|{dealerTurnLogSearchInput}";
        var playerColorMap = BuildDealerTurnLogPlayerColorMap(filteredEntries, colorSeed);

        if (!ImGui.BeginTable(
                "##DealerTurnLogHistoryTable",
                8,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit,
                new Vector2(0f, 0f)))
        {
            ImGui.End();
            return;
        }

        ImGui.TableSetupColumn("House/Club", ImGuiTableColumnFlags.WidthFixed, 135f);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Joined", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Left", ImGuiTableColumnFlags.WidthFixed, 145f);
        ImGui.TableSetupColumn("Received", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Sent", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Chat Log", ImGuiTableColumnFlags.WidthFixed, 92f);
        ImGui.TableSetupColumn("Trade Log", ImGuiTableColumnFlags.WidthFixed, 92f);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        ImGui.TableSetColumnIndex(0);
        DrawCenteredHeaderCell("House/Club");
        ImGui.TableSetColumnIndex(1);
        DrawCenteredHeaderCell("Player");
        ImGui.TableSetColumnIndex(2);
        DrawCenteredHeaderCell("Joined");
        ImGui.TableSetColumnIndex(3);
        DrawCenteredHeaderCell("Left");
        ImGui.TableSetColumnIndex(4);
        DrawCenteredHeaderCell("Received");
        ImGui.TableSetColumnIndex(5);
        DrawCenteredHeaderCell("Sent");
        ImGui.TableSetColumnIndex(6);
        DrawCenteredHeaderCell("Chat Log");
        ImGui.TableSetColumnIndex(7);
        DrawCenteredHeaderCell("Trade Log");

        foreach (var entry in filteredEntries)
        {
            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);
            Vector4 playerColor = playerColorMap.TryGetValue(entry.PlayerName, out var mappedPlayerColor)
                ? mappedPlayerColor
                : GetFallbackDealerTurnLogPlayerColor(entry.PlayerName);
            bool rowSelected = IsDealerTurnLogEntrySelected(entry);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            float rowStartX = ImGui.GetCursorPosX();
            float rowStartY = ImGui.GetCursorPosY();
            float rowHeight = MathF.Max(ImGui.GetFrameHeight(), 26f);

            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.28f, 0.44f, 0.72f, 0.24f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1f, 1f, 1f, 0.08f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.28f, 0.44f, 0.72f, 0.34f));
            bool rowPressed = ImGui.Selectable($"##DealerTurnLogRow{GetDealerTurnLogSelectionKey(entry)}", rowSelected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0f, rowHeight));
            bool rowHovered = ImGui.IsItemHovered();
            ImGui.SetItemAllowOverlap();
            ImGui.PopStyleColor(3);

            if (rowPressed)
            {
                ToggleDealerTurnLogEntrySelection(entry, ImGui.GetIO().KeyCtrl);
                rowSelected = IsDealerTurnLogEntrySelected(entry);
            }

            if (rowSelected)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.28f, 0.44f, 0.72f, 0.22f)));
            else if (rowHovered)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.05f)));

            ImGui.SetCursorPos(new Vector2(rowStartX, rowStartY));
            DrawDealerTurnLogHouseCell(entry);

            ImGui.TableSetColumnIndex(1);
            DrawLeftAlignedVerticallyCenteredColoredCell(entry.PlayerName, playerColor);

            ImGui.TableSetColumnIndex(2);
            DrawCenteredColoredCell(FormatTimestampForDisplay(entry.JoinedTimestamp), GoldColor);

            ImGui.TableSetColumnIndex(3);
            DrawCenteredColoredCell(string.IsNullOrWhiteSpace(entry.LeftTimestamp) ? "--" : entry.LeftTimestamp, string.IsNullOrWhiteSpace(entry.LeftTimestamp) ? ProfitColor : LossColor);

            ImGui.TableSetColumnIndex(4);
            DrawCenteredColoredCell(string.IsNullOrWhiteSpace(entry.TotalReceivedGil) ? "0" : entry.TotalReceivedGil, ProfitColor);

            ImGui.TableSetColumnIndex(5);
            DrawCenteredColoredCell(string.IsNullOrWhiteSpace(entry.TotalSentGil) ? "0" : entry.TotalSentGil, LossColor);

            ImGui.TableSetColumnIndex(6);
            DrawDealerTurnLogFileCell(entry, exportChat: true, playerColor);

            ImGui.TableSetColumnIndex(7);
            DrawDealerTurnLogFileCell(entry, exportChat: false, playerColor);
        }

        ImGui.EndTable();
        ImGui.End();
    }

    private string GetDealerTurnLogSelectionKey(DealerTurnLogEntry entry)
    {
        entry.EnsureInitialized();
        string player = (entry.PlayerName ?? string.Empty).Trim();
        string joined = (entry.JoinedTimestamp ?? string.Empty).Trim();
        return $"{player}|{joined}";
    }

    private bool IsDealerTurnLogEntrySelected(DealerTurnLogEntry entry)
    {
        string key = GetDealerTurnLogSelectionKey(entry);
        return selectedDealerTurnLogRowKeys.Contains(key) || string.Equals(lastDealerTurnLogSelectedRowKey, key, StringComparison.OrdinalIgnoreCase);
    }

    private void ToggleDealerTurnLogEntrySelection(DealerTurnLogEntry entry, bool additive)
    {
        string key = GetDealerTurnLogSelectionKey(entry);
        lastDealerTurnLogSelectedRowKey = key;

        if (!additive)
        {
            selectedDealerTurnLogRowKeys.Clear();
            selectedDealerTurnLogRowKeys.Add(key);
            return;
        }

        if (!selectedDealerTurnLogRowKeys.Add(key))
        {
            selectedDealerTurnLogRowKeys.Remove(key);
            if (selectedDealerTurnLogRowKeys.Count == 0)
                lastDealerTurnLogSelectedRowKey = key;
        }
    }

    private void ClearMissingDealerTurnLogSelections()
    {
        var validKeys = GetDealerTurnLogEntries()
            .Select(GetDealerTurnLogSelectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        selectedDealerTurnLogRowKeys.RemoveWhere(key => !validKeys.Contains(key));
        if (!string.IsNullOrWhiteSpace(lastDealerTurnLogSelectedRowKey) && !validKeys.Contains(lastDealerTurnLogSelectedRowKey))
            lastDealerTurnLogSelectedRowKey = string.Empty;
    }

    private List<DealerTurnLogEntry> GetSelectedDealerTurnLogEntries()
    {
        ClearMissingDealerTurnLogSelections();

        var entries = GetDealerTurnLogEntries();
        if (selectedDealerTurnLogRowKeys.Count > 0)
        {
            var selected = entries
                .Where(entry => selectedDealerTurnLogRowKeys.Contains(GetDealerTurnLogSelectionKey(entry)))
                .ToList();

            if (selected.Count > 0)
                return selected;
        }

        if (!string.IsNullOrWhiteSpace(lastDealerTurnLogSelectedRowKey))
        {
            var fallback = entries
                .Where(entry => string.Equals(GetDealerTurnLogSelectionKey(entry), lastDealerTurnLogSelectedRowKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (fallback.Count > 0)
                return fallback;
        }

        return new List<DealerTurnLogEntry>();
    }

    private void SetDealerTurnLogActionStatus(string text, bool failed = false, string? path = null)
    {
        dealerTurnLogExportStatusText = text;
        dealerTurnLogExportStatusFailed = failed;
        dealerTurnLogExportStatusPath = failed ? string.Empty : (path ?? string.Empty);
        dealerTurnLogExportStatusUntilUtc = DateTime.UtcNow.AddSeconds(8);
    }

    private void DrawDealerTurnLogBulkActionsRow()
    {
        int selectedCount = GetSelectedDealerTurnLogEntries().Count;
        bool hasSelectedRows = selectedCount > 0;
        bool canRenameHouse = hasSelectedRows && !string.IsNullOrWhiteSpace(dealerTurnLogBulkHouseInput);
        Vector4 bulkDeleteColor = Hex("#b30000");
        Vector4 clearButtonColor = WithAlpha(bulkDeleteColor, 0.60f);
        Vector4 disabledGreen = WithAlpha(CopyButtonColor, 0.45f);
        Vector4 disabledGold = WithAlpha(Hex("#DA9E00"), 0.45f);
        Vector4 disabledRed = WithAlpha(bulkDeleteColor, 0.45f);
        Vector4 disabledUtility = WithAlpha(UtilityButtonColor, 0.45f);

        DrawSectionTitle("Bulk Actions");
        ImGui.Dummy(new Vector2(0f, 2f));

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Rename House/Club:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(170f);
        ImGui.InputTextWithHint("##DealerTurnLogBulkHouseInput", "House/Club for selected", ref dealerTurnLogBulkHouseInput, 256);

        ImGui.SameLine();
        if (!canRenameHouse)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("✓", "DealerTurnLogBulkSetHouseButton", new Vector2(26f, 0f), canRenameHouse ? UtilityButtonColor : disabledUtility, WhiteText) && canRenameHouse)
            ApplyBulkHouseToSelectedDealerTurnLogEntries();
        if (!canRenameHouse)
            ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("|");

        ImGui.SameLine();
        if (!hasSelectedRows)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Save Chats", "DealerTurnLogBulkSaveChatsButton", new Vector2(84f, 0f), hasSelectedRows ? CopyButtonColor : disabledGreen, WhiteText) && hasSelectedRows)
            ExportSelectedDealerTurnLogFiles(exportChat: true);
        if (!hasSelectedRows)
            ImGui.EndDisabled();

        ImGui.SameLine();
        if (!hasSelectedRows)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Save Trades", "DealerTurnLogBulkSaveTradesButton", new Vector2(88f, 0f), hasSelectedRows ? Hex("#DA9E00") : disabledGold, WhiteText) && hasSelectedRows)
            ExportSelectedDealerTurnLogFiles(exportChat: false);
        if (!hasSelectedRows)
            ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("|");

        ImGui.SameLine();
        if (!hasSelectedRows)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Delete Rows", "DealerTurnLogBulkDeleteRowsButton", new Vector2(88f, 0f), hasSelectedRows ? bulkDeleteColor : disabledRed, WhiteText) && hasSelectedRows)
            DeleteSelectedDealerTurnLogEntries();
        if (!hasSelectedRows)
            ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.AlignTextToFramePadding();
        string selectedText = $"Selected: {selectedCount.ToString(CultureInfo.InvariantCulture)}";
        if (selectedCount > 0)
            ImGui.TextColored(ProfitColor, selectedText);
        else
            ImGui.TextUnformatted(selectedText);

        ImGui.SameLine();
        if (!hasSelectedRows)
            ImGui.BeginDisabled();
        if (DrawStyledBoldButton("Clear", "DealerTurnLogBulkClearSelectionButton", new Vector2(54f, 0f), hasSelectedRows ? clearButtonColor : disabledRed, WhiteText) && hasSelectedRows)
        {
            selectedDealerTurnLogRowKeys.Clear();
            lastDealerTurnLogSelectedRowKey = string.Empty;
        }
        if (!hasSelectedRows)
            ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled("Ctrl+Click to multi-select");

        ImGui.Spacing();
        DrawSectionTitle("Log History");
    }

    private void ApplyBulkHouseToSelectedDealerTurnLogEntries()
    {
        var selectedEntries = GetSelectedDealerTurnLogEntries();
        if (selectedEntries.Count == 0)
        {
            SetDealerTurnLogActionStatus("No rows selected", failed: true);
            return;
        }

        string newHouse = (dealerTurnLogBulkHouseInput ?? string.Empty).Trim();
        foreach (var entry in selectedEntries)
        {
            entry.EnsureInitialized();
            entry.House = newHouse;
            entry.HouseEditedManually = true;
        }

        Plugin.Configuration.Save();
        SetDealerTurnLogActionStatus(string.IsNullOrWhiteSpace(newHouse) ? "Cleared House/Club on selected rows" : "Updated House/Club on selected rows");
    }

    private void ExportSelectedDealerTurnLogFiles(bool exportChat)
    {
        var selectedEntries = GetSelectedDealerTurnLogEntries();
        if (selectedEntries.Count == 0)
        {
            SetDealerTurnLogActionStatus("No rows selected", failed: true);
            return;
        }

        string configuredDirectory = Plugin.Configuration.HistoryExportDirectory?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            SetDealerTurnLogActionStatus("Insert file directory path first", failed: true);
            ImGui.OpenPopup("##DealerTurnLogDirectoryRequiredPopup");
            return;
        }

        try
        {
            foreach (var entry in selectedEntries)
                WriteDealerTurnLogFile(entry, exportChat, updateStatus: false);

            Plugin.Configuration.Save();
            SetDealerTurnLogActionStatus($"Saved {(exportChat ? "chat" : "trade")} logs", failed: false, path: GetDealerTurnLogExportDirectory());
        }
        catch (Exception ex)
        {
            SetDealerTurnLogActionStatus($"Failed to save {(exportChat ? "chat" : "trade")} logs", failed: true);
            AddDebugLog("ERR", $"Dealer turn bulk export failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void DeleteDealerTurnLogEntryFiles(DealerTurnLogEntry entry)
    {
        try
        {
            EnsureDealerTurnLogDeletedStateLoaded();
            foreach (bool exportChat in new[] { true, false })
            {
                string filePath = BuildDealerTurnLogFilePath(entry, exportChat);
                if (File.Exists(filePath))
                    File.Delete(filePath);
                dealerTurnLogDeletedState.Remove(GetDealerTurnLogDeletedStateKey(entry, exportChat));
            }
            SaveDealerTurnLogDeletedState();
        }
        catch (Exception ex)
        {
            AddDebugLog("ERR", $"Dealer turn row artifact cleanup failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void DeleteSelectedDealerTurnLogEntries()
    {
        var selectedEntries = GetSelectedDealerTurnLogEntries();
        if (selectedEntries.Count == 0)
        {
            SetDealerTurnLogActionStatus("No rows selected", failed: true);
            return;
        }

        var selectedKeys = selectedEntries
            .Select(GetDealerTurnLogSelectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in selectedEntries)
            DeleteDealerTurnLogEntryFiles(entry);

        var entries = GetDealerTurnLogEntries();
        entries.RemoveAll(entry => selectedKeys.Contains(GetDealerTurnLogSelectionKey(entry)));
        selectedDealerTurnLogRowKeys.Clear();
        lastDealerTurnLogSelectedRowKey = string.Empty;
        Plugin.Configuration.Save();
        SetDealerTurnLogActionStatus("Deleted selected rows");
    }

    private void DrawDealerTurnLogSortOption(string option)
    {
        bool selected = string.Equals(dealerTurnLogSortBy, option, StringComparison.OrdinalIgnoreCase);
        if (ImGui.Selectable(option, selected))
            dealerTurnLogSortBy = option;

        if (selected)
            ImGui.SetItemDefaultFocus();
    }

    private List<string> GetDealerTurnLogHouseSortOptions()
    {
        var options = GetDealerTurnLogEntries()
            .Select(entry =>
            {
                entry.EnsureInitialized();
                ApplyDealerTurnLogHouseIfMissing(entry);
                return GetDealerTurnLogDisplayHouse(entry);
            })
            .Where(house => !string.IsNullOrWhiteSpace(house))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(house => house, StringComparer.OrdinalIgnoreCase)
            .Select(house => $"House: {house}")
            .ToList();

        if (dealerTurnLogSortBy.StartsWith("House: ", StringComparison.OrdinalIgnoreCase) &&
            !options.Any(option => string.Equals(option, dealerTurnLogSortBy, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, dealerTurnLogSortBy);
        }

        return options;
    }

    private List<string> GetDealerTableHistoryHouseSortOptions()
    {
        var options = GetDealerTableHistoryEntries()
            .Select(entry =>
            {
                entry.EnsureInitialized();
                ApplyDealerTurnLogHouseIfMissing(entry);
                return GetDealerTurnLogDisplayHouse(entry);
            })
            .Where(house => !string.IsNullOrWhiteSpace(house))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(house => house, StringComparer.OrdinalIgnoreCase)
            .Select(house => $"House: {house}")
            .ToList();

        if (dealerTurnLogSortBy.StartsWith("House: ", StringComparison.OrdinalIgnoreCase) &&
            !options.Any(option => string.Equals(option, dealerTurnLogSortBy, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, dealerTurnLogSortBy);
        }

        return options;
    }

    private List<DealerTurnLogEntry> GetFilteredDealerTableHistoryEntries()
    {
        IEnumerable<DealerTurnLogEntry> query = GetDealerTableHistoryEntries();

        query = query.Where(entry => entry != null).Select(entry =>
        {
            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);
            return entry;
        });

        DateTime now = GetEstNow();
        DateTime today = now.Date;
        DateTime filterStart;
        DateTime filterEnd;

        if (string.Equals(dealerTurnLogSortBy, "Today", StringComparison.OrdinalIgnoreCase))
        {
            filterStart = today;
            filterEnd = today.AddDays(1);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (string.Equals(dealerTurnLogSortBy, "This Week", StringComparison.OrdinalIgnoreCase))
        {
            int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            filterStart = today.AddDays(-diff);
            filterEnd = filterStart.AddDays(7);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (string.Equals(dealerTurnLogSortBy, "This Month", StringComparison.OrdinalIgnoreCase))
        {
            filterStart = new DateTime(today.Year, today.Month, 1);
            filterEnd = filterStart.AddMonths(1);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (dealerTurnLogSortBy.StartsWith("House: ", StringComparison.OrdinalIgnoreCase))
        {
            string houseFilter = dealerTurnLogSortBy[7..].Trim();
            query = query.Where(entry => string.Equals(GetDealerTurnLogDisplayHouse(entry), houseFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(dealerTurnLogSearchInput))
        {
            string needle = dealerTurnLogSearchInput.Trim();
            query = query.Where(entry => DealerTurnLogMatchesSearch(entry, needle));
        }

        return query
            .OrderByDescending(GetDealerTurnLogRowSortTimestamp)
            .ThenByDescending(GetDealerTurnLogGroupDateValue)
            .ThenBy(entry => entry.PlayerName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<DealerTurnLogEntry> GetFilteredDealerTurnLogEntries()
    {
        IEnumerable<DealerTurnLogEntry> query = GetDealerTurnLogEntries();

        query = query.Where(entry => entry != null).Select(entry =>
        {
            entry.EnsureInitialized();
            ApplyDealerTurnLogHouseIfMissing(entry);
            return entry;
        });

        DateTime now = GetEstNow();
        DateTime today = now.Date;
        DateTime filterStart;
        DateTime filterEnd;

        if (string.Equals(dealerTurnLogSortBy, "Today", StringComparison.OrdinalIgnoreCase))
        {
            filterStart = today;
            filterEnd = today.AddDays(1);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (string.Equals(dealerTurnLogSortBy, "This Week", StringComparison.OrdinalIgnoreCase))
        {
            int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            filterStart = today.AddDays(-diff);
            filterEnd = filterStart.AddDays(7);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (string.Equals(dealerTurnLogSortBy, "This Month", StringComparison.OrdinalIgnoreCase))
        {
            filterStart = new DateTime(today.Year, today.Month, 1);
            filterEnd = filterStart.AddMonths(1);
            query = query.Where(entry =>
            {
                DateTime timestamp = GetDealerTurnLogFilterTimestamp(entry);
                return timestamp >= filterStart && timestamp < filterEnd;
            });
        }
        else if (dealerTurnLogSortBy.StartsWith("House: ", StringComparison.OrdinalIgnoreCase))
        {
            string houseFilter = dealerTurnLogSortBy[7..].Trim();
            query = query.Where(entry => string.Equals(GetDealerTurnLogDisplayHouse(entry), houseFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(dealerTurnLogSearchInput))
        {
            string needle = dealerTurnLogSearchInput.Trim();
            query = query.Where(entry => DealerTurnLogMatchesSearch(entry, needle));
        }

        return query
            .OrderByDescending(GetDealerTurnLogRowSortTimestamp)
            .ThenByDescending(GetDealerTurnLogGroupDateValue)
            .ThenBy(entry => entry.PlayerName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private DateTime GetDealerTurnLogFilterTimestamp(DealerTurnLogEntry entry)
    {
        DateTime timestamp = GetDealerTurnLogRowSortTimestamp(entry);
        if (timestamp != DateTime.MinValue)
            return timestamp;

        timestamp = GetDealerTurnLogGroupDateValue(entry);
        return timestamp != DateTime.MinValue ? timestamp : DateTime.MinValue;
    }

    private bool DealerTurnLogMatchesSearch(DealerTurnLogEntry entry, string needle)
    {
        var parts = new List<string>
        {
            GetDealerTurnLogDisplayHouse(entry),
            entry.PlayerName ?? string.Empty,
            entry.JoinedTimestamp ?? string.Empty,
            entry.LeftTimestamp ?? string.Empty,
            entry.TotalReceivedGil ?? string.Empty,
            entry.TotalSentGil ?? string.Empty
        };

        if (entry.ChatLines != null && entry.ChatLines.Count > 0)
            parts.AddRange(entry.ChatLines);
        if (entry.TradeLines != null && entry.TradeLines.Count > 0)
            parts.AddRange(entry.TradeLines);
        if (entry.MatchHistoryLines != null && entry.MatchHistoryLines.Count > 0)
            parts.AddRange(entry.MatchHistoryLines);

        string haystack = string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string GetDealerTurnLogGroupDateLabel(DealerTurnLogEntry entry)
    {
        DateTime timestamp = GetDealerTurnLogGroupDateValue(entry);
        return timestamp == DateTime.MinValue
            ? "Unknown Date"
            : timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private DateTime GetDealerTurnLogGroupDateValue(DealerTurnLogEntry entry)
    {
        return GetDealerTurnLogBusinessDateValue(entry);
    }

    private DateTime GetDealerTurnLogRowSortTimestamp(DealerTurnLogEntry entry)
    {
        DateTime timestamp = ParseTimestamp(entry.LeftTimestamp);
        if (timestamp != DateTime.MinValue)
            return timestamp;

        timestamp = ParseTimestamp(entry.JoinedTimestamp);
        return timestamp;
    }

    private Dictionary<string, Vector4> BuildDealerTurnLogPlayerColorMap(IEnumerable<DealerTurnLogEntry> entries, string dateLabel)
    {
        var players = entries
            .Select(entry => entry.PlayerName ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var colorMap = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase);
        if (players.Count == 0)
            return colorMap;

        var random = new Random(unchecked((int)ComputeStableTextHash(dateLabel)));
        var usedColors = new List<Vector3>();

        foreach (string player in players)
        {
            Vector4 color = GenerateDistinctBrightRandomColor(random, usedColors);
            colorMap[player] = color;
            usedColors.Add(new Vector3(color.X, color.Y, color.Z));
        }

        return colorMap;
    }

    private Vector4 GenerateDistinctBrightRandomColor(Random random, List<Vector3> usedColors)
    {
        for (int attempt = 0; attempt < 256; attempt++)
        {
            float hue = (float)random.NextDouble();
            float saturation = 0.55f + ((float)random.NextDouble() * 0.35f);
            float value = 0.88f + ((float)random.NextDouble() * 0.12f);
            Vector4 color = HsvToRgba(hue, saturation, value);
            var rgb = new Vector3(color.X, color.Y, color.Z);

            bool tooDark = ((rgb.X + rgb.Y + rgb.Z) / 3f) < 0.62f;
            bool tooClose = usedColors.Any(existing => Vector3.Distance(existing, rgb) < 0.22f);
            if (!tooDark && !tooClose)
                return color;
        }

        float fallbackHue = (usedColors.Count * 0.61803398875f) % 1f;
        return HsvToRgba(fallbackHue, 0.70f, 0.96f);
    }

    private Vector4 GetFallbackDealerTurnLogPlayerColor(string playerName)
    {
        float hue = (ComputeStableTextHash(playerName) % 360u) / 360f;
        return HsvToRgba(hue, 0.72f, 0.96f);
    }

    private static Vector4 HsvToRgba(float hue, float saturation, float value)
    {
        hue = hue - MathF.Floor(hue);
        saturation = Math.Clamp(saturation, 0f, 1f);
        value = Math.Clamp(value, 0f, 1f);

        if (saturation <= 0f)
            return new Vector4(value, value, value, 1f);

        float scaledHue = hue * 6f;
        int sector = (int)MathF.Floor(scaledHue);
        float fraction = scaledHue - sector;

        float p = value * (1f - saturation);
        float q = value * (1f - (saturation * fraction));
        float t = value * (1f - (saturation * (1f - fraction)));

        return (sector % 6) switch
        {
            0 => new Vector4(value, t, p, 1f),
            1 => new Vector4(q, value, p, 1f),
            2 => new Vector4(p, value, t, 1f),
            3 => new Vector4(p, q, value, 1f),
            4 => new Vector4(t, p, value, 1f),
            _ => new Vector4(value, p, q, 1f),
        };
    }

    private static uint ComputeStableTextHash(string? text)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;

        uint hash = offset;
        string value = text ?? string.Empty;
        foreach (char c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            builder.Append(invalid.Contains(c) ? '_' : c);
        }

        return builder.ToString().Trim();
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

        if (normalizedMessage.Contains("blackjack", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(normalizedMessage, @"\bbj\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return false;
        }

        if (Regex.IsMatch(normalizedMessage, @"\bdouble\s+down\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\bdoubling\s+down\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\b(?:i\s*('|a)?ll|ill)\s+double\s+down\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\bgoing\s+dd\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\b(?:double\s+down|dd)\s+for\s+sure\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\b(?:lets|let's)\s+dd\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        if (Regex.IsMatch(normalizedMessage, @"\b(?:fck|fuck)\s+it\s+dd\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        return Regex.IsMatch(normalizedMessage, @"^dd$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
            BigInteger netPayout = CalculateBlackjackNetPayout(betValue, multiplierIndex);
            BigInteger oldCurrentBankValue = finalBankValue;

            finalBankValue += netPayout;
            finalBankInput = FormatNumber(finalBankValue);
            lastPlayerBankChangeValue = finalBankValue - oldCurrentBankValue;

            AddDebugLog("AUTO-BJ", $"Blackjack payout delta: +{FormatNumber(netPayout)}.");
            AddBlackjackHistoryEntry(netPayout);
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
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
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

    private BigInteger GetDealerRecordedFinalBankValue()
    {
        if (IsPlayerMode)
            return finalBankValue;

        BigInteger adjusted = finalBankValue - tipsValue;
        return adjusted < BigInteger.Zero ? BigInteger.Zero : adjusted;
    }

    private string GetRecordedFinalBankText()
    {
        return FormatNumber(IsPlayerMode ? finalBankValue : GetDealerRecordedFinalBankValue());
    }

    private string GetConfiguredResultsLabel()
    {
        return string.IsNullOrWhiteSpace(Plugin.Configuration.ResultsLabel)
            ? "Today Profit/Loss:"
            : Plugin.Configuration.ResultsLabel;
    }

    private string GetConfiguredStartingLabel()
    {
        return string.IsNullOrWhiteSpace(Plugin.Configuration.StartingLabel)
            ? "Starting Bank:"
            : Plugin.Configuration.StartingLabel;
    }

    private string GetConfiguredFinalLabel()
    {
        return string.IsNullOrWhiteSpace(Plugin.Configuration.FinalLabel)
            ? "Final Bank:"
            : Plugin.Configuration.FinalLabel;
    }

    private string BuildFinalMessage()
    {
        string prefix = Plugin.Configuration.IncludeTimestampInMessage
            ? $"[{FormatEstTimestampShort(GetEstNow())}] "
            : string.Empty;

        return $"{prefix}{GetConfiguredResultsLabel()} {GetCurrentResultText()} Gil | {GetConfiguredStartingLabel()} {FormatNumber(startingBankValue)} Gil | {GetConfiguredFinalLabel()} {GetRecordedFinalBankText()} Gil";
    }

    private string GetCurrentResultText()
    {
        var value = IsPlayerMode
            ? finalBankValue - startingBankValue
            : GetDealerRecordedFinalBankValue() - startingBankValue;

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

    private static TimeZoneInfo ResolveEstTimeZone()
    {
        string[] candidateIds =
        {
            "Eastern Standard Time",
            "America/New_York",
            "US/Eastern"
        };

        foreach (string id in candidateIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private static DateTime GetEstNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EstTimeZone);
    }

    private static string FormatEstTimestamp(DateTime timestamp)
    {
        return timestamp.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static string FormatEstTimestampShort(DateTime timestamp)
    {
        return timestamp.ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);
    }

    private static string FormatEstTimeTag(DateTime timestamp)
    {
        return $"[{timestamp:hh:mm:ss tt}]";
    }

    private static string FormatTimestampForDisplay(string timestamp, bool includeSeconds = true)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "--";

        DateTime parsed = ParseTimestamp(timestamp);
        if (parsed == DateTime.MinValue)
            return timestamp;

        return includeSeconds
            ? FormatEstTimestamp(parsed)
            : FormatEstTimestampShort(parsed);
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
        string[] formats =
        {
            "yyyy-MM-dd hh:mm:ss tt",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd hh:mm tt",
            "yyyy-MM-dd HH:mm"
        };

        if (DateTime.TryParseExact(
                timestamp,
                formats,
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
        string shiftStartText = shiftStart.HasValue ? FormatEstTimestampShort(shiftStart.Value) : "--";
        string elapsedText = shiftStart.HasValue ? FormatElapsed(GetEstNow() - shiftStart.Value) : "--";

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

        const float turnLogsButtonWidth = 144f;
        const float logHistoryButtonWidth = 96f;
        float rightButtonsWidth = turnLogsButtonWidth + ImGui.GetStyle().ItemSpacing.X + logHistoryButtonWidth;

        ImGui.SameLine();
        float rightButtonsX = MathF.Max(
            ImGui.GetCursorPosX() + 28f,
            ImGui.GetWindowContentRegionMax().X - rightButtonsWidth);
        ImGui.SetCursorPosX(rightButtonsX);

        bool dealerTurnLogsEnabled = GetDealerTurnLogsEnabled();
        string turnLogsButtonText = dealerTurnLogsEnabled
            ? "● Turn logs OFF"
            : "○ Turn logs ON";
        if (DrawStyledBoldButton(turnLogsButtonText, "DealerTurnLogsToggleButton", new Vector2(turnLogsButtonWidth, 0f), WithAlpha(Hex("#607da1"), 0.85f), WhiteText))
            ToggleDealerTurnLogs();

        ImGui.SameLine();
        if (DrawStyledBoldButton("Log History", "DealerTurnLogHistoryButton", new Vector2(logHistoryButtonWidth, 0f), WithAlpha(Hex("#e552ff"), 0.90f), WhiteText))
            showDealerTurnLogHistoryWindow = true;

    }

    private void IncrementDealerTips(BigInteger amount, bool registerHistoryEntry = false)
    {
        if (amount <= BigInteger.Zero)
            return;

        tipsValue += amount;
        tipsInput = FormatNumber(tipsValue);
        AddDebugLog("UI", $"Quick tip button applied: +{FormatNumber(amount)} | New Tips Total: {tipsInput}");

        if (registerHistoryEntry)
            AddDealerTipHistoryEntry(amount);

        SaveCurrentProfileValues();
    }

    private bool RegisterDealerTipsFromCurrentField()
    {
        if (!IsDealerMode || tipsValue <= BigInteger.Zero)
            return false;

        AddDealerTipHistoryEntry(tipsValue);
        SaveCurrentProfileValues();
        return true;
    }

    private void AddDealerTipHistoryEntry(BigInteger amount)
    {
        if (!IsDealerMode || amount <= BigInteger.Zero)
            return;

        var history = GetCurrentHistory();
        var entry = new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
            Tips = FormatNumber(tipsValue),
            Result = $"+{FormatNumber(amount)} Tips"
        };

        history.Add(entry);
        if (history.Count > 200)
            history.RemoveAt(0);

        AddDebugLog("HISTORY", $"Tips entry saved: House: {entry.House} | Time: {entry.Timestamp} | Start Bank: {entry.StartingBank} | Final Bank: {entry.FinalBank} | Tips: {entry.Tips} | Results: {entry.Result}");
        Plugin.Configuration.Save();
    }

    private void ShowDealerTurnLogPatternValidationPopup(string message)
    {
        dealerTurnLogPatternValidationMessage = message ?? string.Empty;
        showDealerTurnLogPatternValidationPopup = !string.IsNullOrWhiteSpace(dealerTurnLogPatternValidationMessage);
    }

    private void AddDealerCheckpoint(string checkpointName)
    {
        var history = GetCurrentHistory();

        var entry = new HistoryEntry
        {
            House = houseInput.Trim(),
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
            Tips = FormatNumber(tipsValue),
            Result = checkpointName
        };

        history.Add(entry);

        if (history.Count > 200)
            history.RemoveAt(0);

        AddDebugLog("HISTORY", $"Checkpoint entry saved: House: {entry.House} | Time: {entry.Timestamp} | Start Bank: {entry.StartingBank} | Final Bank: {entry.FinalBank} | Tips: {entry.Tips} | Results: {entry.Result}");
        Plugin.Configuration.Save();
        AppendDealerTurnLogShiftAction(checkpointName);

        if (string.Equals(checkpointName, "End Shift", StringComparison.OrdinalIgnoreCase) && Plugin.Configuration.DealerAutoBackupOnEndShift)
        {
            AddBackupHistoryEntry();
            ExportCurrentHistory(false, true, "EndShiftBackup");
            ExportCurrentHistory(true, true, "EndShiftBackup");
        }
    }


    private void DrawDealerPeriodSummary()
    {
        DrawDealerSummaryLine("Today", GetEstNow().Date, GetEstNow());
        DrawDealerSummaryLine("This Week", GetEstNow().Date.AddDays(-6), GetEstNow());
        DrawDealerSummaryLine("This Month", new DateTime(GetEstNow().Date.Year, GetEstNow().Date.Month, 1), GetEstNow());
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

    private void DrawCompositeInlineText(string leftText, Vector4 leftColor, string rightText, Vector4 rightColor)
    {
        ImGui.BeginGroup();
        ImGui.PushStyleColor(ImGuiCol.Text, leftColor);
        ImGui.TextUnformatted(leftText);
        ImGui.PopStyleColor();
        ImGui.SameLine(0f, 0f);
        ImGui.PushStyleColor(ImGuiCol.Text, rightColor);
        ImGui.TextUnformatted(rightText);
        ImGui.PopStyleColor();
        ImGui.EndGroup();
    }

    private void DrawCompositeClickableInlineText(
        string leftText,
        Vector4 leftColor,
        string rightText,
        Vector4 rightColor,
        string id,
        Action onClick,
        bool underlineOnlyOnHover = false,
        string tooltip = "Click to open")
    {
        Vector2 leftSize = ImGui.CalcTextSize(leftText);
        Vector2 rightSize = ImGui.CalcTextSize(rightText);
        float width = leftSize.X + rightSize.X;
        float height = MathF.Max(leftSize.Y, rightSize.Y);
        Vector2 start = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton($"##{id}", new Vector2(MathF.Max(width, 1f), MathF.Max(height, ImGui.GetTextLineHeight())));
        bool hovered = ImGui.IsItemHovered();
        Vector2 min = ImGui.GetItemRectMin();
        Vector2 max = ImGui.GetItemRectMax();

        if (hovered)
        {
            Vector4 hoverBlend = new(
                (leftColor.X + rightColor.X) * 0.5f,
                (leftColor.Y + rightColor.Y) * 0.5f,
                (leftColor.Z + rightColor.Z) * 0.5f,
                0.12f);
            uint hoverBg = ImGui.ColorConvertFloat4ToU32(hoverBlend);
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

        Vector4 drawLeftColor = hovered ? Lighten(leftColor, 0.22f) : leftColor;
        Vector4 drawRightColor = hovered ? Lighten(rightColor, 0.22f) : rightColor;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(start, ImGui.ColorConvertFloat4ToU32(drawLeftColor), leftText);
        drawList.AddText(new Vector2(start.X + leftSize.X, start.Y), ImGui.ColorConvertFloat4ToU32(drawRightColor), rightText);

        if (!underlineOnlyOnHover || hovered)
        {
            float thickness = hovered ? 2f : 1f;
            drawList.AddLine(
                new Vector2(min.X, max.Y),
                new Vector2(min.X + leftSize.X, max.Y),
                ImGui.ColorConvertFloat4ToU32(drawLeftColor),
                thickness);
            drawList.AddLine(
                new Vector2(min.X + leftSize.X, max.Y),
                new Vector2(min.X + width, max.Y),
                ImGui.ColorConvertFloat4ToU32(drawRightColor),
                thickness);
        }

        if (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            onClick();
    }

    private void OpenDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
        }
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
            Timestamp = FormatEstTimestamp(GetEstNow()),
            StartingBank = FormatNumber(startingBankValue),
            FinalBank = GetRecordedFinalBankText(),
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


    private DateTime? TryGetMostRecentDealerShiftStartBefore(DateTime endTimestamp)
    {
        if (endTimestamp == DateTime.MinValue)
            return null;

        var checkpoints = GetCurrentHistory()
            .Where(IsCheckpointEntry)
            .Select(entry => new { Entry = entry, Timestamp = ParseTimestamp(entry.Timestamp) })
            .Where(x => x.Timestamp != DateTime.MinValue && x.Timestamp <= endTimestamp)
            .OrderBy(x => x.Timestamp)
            .ToList();

        bool skippedCurrentEnd = false;

        for (int i = checkpoints.Count - 1; i >= 0; i--)
        {
            string result = checkpoints[i].Entry.Result ?? string.Empty;

            if (result.Contains("End Shift", StringComparison.OrdinalIgnoreCase))
            {
                if (!skippedCurrentEnd && checkpoints[i].Timestamp == endTimestamp)
                {
                    skippedCurrentEnd = true;
                    continue;
                }

                return null;
            }

            if (result.Contains("Start Shift", StringComparison.OrdinalIgnoreCase))
                return checkpoints[i].Timestamp;
        }

        return null;
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
            if ((GetEstNow() - latestTimestamp).TotalSeconds >= 10)
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

    private static string FormatShiftDurationDetailed(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";

        if (elapsed.TotalMinutes >= 1)
            return $"{elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";

        return $"{elapsed.Seconds:D2}s";
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
        string label = FormatTimestampForDisplay(entry.Timestamp);
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
            ImGui.TextUnformatted($"Started: {(shiftStart.HasValue ? FormatEstTimestamp(shiftStart.Value) : "--")}");
            ImGui.TextUnformatted($"Elapsed: {(shiftStart.HasValue ? FormatElapsed(GetEstNow() - shiftStart.Value) : "--")}");

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

        return shiftStart.HasValue ? FormatEstTimestamp(shiftStart.Value) : "No active shift";
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

        string today = GetEstNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
