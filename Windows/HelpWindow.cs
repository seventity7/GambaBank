using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GambaBank.Windows;

public class HelpWindow : Window, IDisposable
{
    private static readonly Vector4 ProfitColor = new(0.35f, 0.85f, 0.35f, 1.0f);
    private static readonly Vector4 LossColor = new(1.0f, 0.45f, 0.45f, 1.0f);
    private static readonly Vector4 GoldColor = new(0.95f, 0.80f, 0.25f, 1.0f);
    private static readonly Vector4 NatBjColor = Hex("#3f52d1");
    private static readonly Vector4 DirtyBjColor = Hex("#ee099a");
    private static readonly Vector4 AltNatBjColor = Hex("#00a2b8");
    private static readonly Vector4 AltDirtyBjColor = Hex("#f100f1");
    private static readonly Vector4 TipsColor = Hex("#78cf00");
    private static readonly Vector4 BlackText = new(0.06f, 0.06f, 0.06f, 1.0f);
    private static readonly Vector4 WhiteText = Hex("#FFFFFF");

    public HelpWindow()
        : base("GambaBank Help###GambaBankHelp")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(920f, 760f);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = true;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.BeginChild("##GambaHelpScroll", new Vector2(0f, 0f), false);

        DrawHeaderRow();
        ImGui.Spacing();

        DrawSectionTitle("♯ Dealer mode");
        DrawRichLine(
            Txt("Start by typing your initial Banking on the field "),
            HiCol("Starting Bank:", ProfitColor));
        DrawRichLine(
            Txt("At the end of your shift as a Dealer, type your Final Bank in its field."));
        DrawRichBullet(
            Txt("If you got any tips from people, type they total value on the "),
            HiCol("Tips", TipsColor),
            Txt(" field"));
        DrawRichBullet(
            Txt("If you want, type the name of the place you'r dealing into the field "),
            HiCol("House", GoldColor));
        ImGui.Spacing();
        DrawRichLine(
            Txt("Once done, hit the "),
            HiCol("Save", ProfitColor),
            Txt(" button. Gamba Bank will automaticaly calculate the final result on the field "),
            HiCol("Results", ProfitColor),
            Txt("."));
        DrawRichLine(
            Txt("It will also generate your custom message with the results in case you want to copy it and share."));
        DrawRichLine(
            Txt("The results will also be saved automaticaly into your "),
            GoldTxt("Dealer History"));
        ImGui.Spacing();
        DrawRichLine(
            Txt("On the "),
            GoldTxt("Dealer History"),
            Txt(" panel, use the button "),
            HiCol("Sort By", GoldColor),
            Txt(" to sort all results or the field "),
            HiCol("Search", GoldColor),
            Txt(" to manualy filter it"));

        ImGui.Spacing();
        DrawSectionTitle("★ Player mode");
        DrawRichLine(
            Txt("Start by typing your initial Banking on the field "),
            HiCol("Starting Bank", ProfitColor));
        DrawRichBullet(
            Txt("Gamba Bank will automaticaly set your Current Bank and "),
            HiCol("Results", ProfitColor));
        DrawRichBullet(
            Txt("If you want, type the name of the place you'r playing into the field "),
            HiCol("House", GoldColor));
        DrawRichLine(
            Txt("Once done, you can save the current state to your "),
            GoldTxt("Player History"),
            Txt(" clicking "),
            HiCol("Save", ProfitColor));
        ImGui.Spacing();
        DrawRichLine(
            Txt("On the "),
            GoldTxt("Bet Tracking"),
            Txt(" section, you can track the match in real-time manualy/automaticaly"));

        ImGui.Spacing();
        DrawSectionTitle("Manual mode");
        DrawRichBullet(
            Txt("Type your current bet in the field "),
            HiCol("$ Current Bet", ProfitColor));
        DrawRichBullet(
            Txt("Setup the blackjack prize multiplier in the multiplier fields "),
            HiCol("1.0x", AltNatBjColor),
            Txt(" > "),
            HiCol("3.0x", AltDirtyBjColor));
        DrawRichBullet(
            Txt("Every time you "),
            Col("Win", ProfitColor),
            Txt("/"),
            Col("Lose", LossColor),
            Txt(", hit the buttons "),
            HiCol("WIN ↑", ProfitColor),
            Txt(" / "),
            HiCol("↓ LOSS", LossColor),
            Txt(" accordingly"));
        DrawRichBullet(
            Txt("If you "),
            Col("Win", ProfitColor),
            Txt(" a "),
            HiCol("Natural Blackjack", AltNatBjColor),
            Txt(" or "),
            HiCol("Dirty Blackjack", AltDirtyBjColor),
            Txt(", hit the buttons accordingly"));
        DrawRichLine(
            Txt("Gamba Bank will still track all results in real-time into the "),
            GoldTxt("Player History"));

        ImGui.Spacing();
        DrawSectionTitle("Auto-Track mode");
        DrawRichBullet(
            Txt("Type your current bet in the field "),
            HiCol("$ Current Bet", ProfitColor));
        DrawRichBullet(
            Txt("Setup the blackjack prize multiplier in the multiplier fields "),
            HiCol("1.0x", NatBjColor),
            Txt(" > "),
            HiCol("3.0x", NatBjColor));
        DrawRichBullet(
            Txt("Right click ono the person that is the Dealer and hit the button "),
            HiCol("Track Dealer", GoldColor));
        DrawRichBullet(
            Txt("Hit the button "),
            HiCol("Auto Track", GoldColor));
        DrawRichLine(
            Txt("Gamba Bank will track the match in real-time, updating Current Bank and "),
            HiCol("Results", ProfitColor),
            Txt(" automaticaly based on your initial bank + match results."));
        DrawRichLine(
            Txt("It will also register everything on your "),
            GoldTxt("Player History"));
        DrawRichLine(
            Txt("Auto-Track doesnt track Blackjack winnings/losses for now, so you still need to manualy hit the "),
            HiCol("NAT BJ", NatBjColor),
            Txt(" / "),
            HiCol("DIRTY BJ", DirtyBjColor),
            Txt(" buttons everytime you get one"));

        ImGui.Spacing();
        DrawRichLine(
            Txt("If you need to add more banking, "),
            Col("DO NOT", LossColor),
            Txt(" change the "),
            HiCol("Starting Bank", ProfitColor),
            Txt(" field, use the "),
            HiCol("Add Bank", ProfitColor),
            Txt(" button instead"));

        ImGui.EndChild();
    }


    private void DrawHeaderRow()
    {
        const float githubButtonWidth = 72f;
        const float githubButtonHeight = 22f;
        const string titleText = "How To";

        float availableWidth = ImGui.GetContentRegionAvail().X;
        Vector2 baseCursorPos = ImGui.GetCursorPos();
        Vector2 baseScreenPos = ImGui.GetCursorScreenPos();
        Vector2 titleSize = ImGui.CalcTextSize(titleText);
        float rowHeight = MathF.Max(titleSize.Y, githubButtonHeight);

        float buttonX = baseCursorPos.X + MathF.Max(0f, availableWidth - githubButtonWidth);
        ImGui.SetCursorPos(new Vector2(buttonX, baseCursorPos.Y));

        bool githubPressed = DrawStyledBoldButton(
            "Github",
            "HelpGithubButton",
            new Vector2(githubButtonWidth, githubButtonHeight),
            Hex("#8702b8"),
            WhiteText);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Visit plugin Github page");

        if (githubPressed)
            OpenGithubPage();

        ImGui.SetCursorPos(baseCursorPos);

        float titleX = baseScreenPos.X + MathF.Max(0f, (availableWidth - titleSize.X - 1f) * 0.5f);
        float titleY = baseScreenPos.Y + ((rowHeight - titleSize.Y) * 0.5f);

        uint col = ImGui.ColorConvertFloat4ToU32(GoldColor);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(new Vector2(titleX, titleY), col, titleText);
        drawList.AddText(new Vector2(titleX + 1f, titleY), col, titleText);

        ImGui.Dummy(new Vector2(availableWidth, rowHeight));
    }

    private void OpenGithubPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/seventity7/GambaBank",
                UseShellExecute = true
            });
        }
        catch
        {
        }
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

    private void DrawCenteredBoldTitle(string text)
    {
        Vector2 textSize = ImGui.CalcTextSize(text);
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float cursorX = ImGui.GetCursorPosX() + MathF.Max(0f, (availableWidth - textSize.X - 1f) * 0.5f);
        ImGui.SetCursorPosX(cursorX);

        Vector2 pos = ImGui.GetCursorScreenPos();
        uint col = ImGui.ColorConvertFloat4ToU32(GoldColor);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddText(pos, col, text);
        drawList.AddText(pos + new Vector2(1f, 0f), col, text);

        ImGui.Dummy(new Vector2(textSize.X + 1f, textSize.Y));
    }

    private void DrawSectionTitle(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, GoldColor);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static (string Text, Vector4? Color, bool Highlight, bool Bold) Txt(string text) => (text, null, false, false);
    private static (string Text, Vector4? Color, bool Highlight, bool Bold) Col(string text, Vector4 color) => (text, color, false, true);
    private static (string Text, Vector4? Color, bool Highlight, bool Bold) GoldTxt(string text) => (text, GoldColor, false, true);
    private static (string Text, Vector4? Color, bool Highlight, bool Bold) Hi(string text) => (text, null, true, false);
    private static (string Text, Vector4? Color, bool Highlight, bool Bold) HiCol(string text, Vector4 color) => (text, color, true, true);

    private void DrawRichLine(params (string Text, Vector4? Color, bool Highlight, bool Bold)[] segments)
    {
        bool first = true;
        foreach (var segment in segments)
        {
            if (!first)
                ImGui.SameLine(0f, 0f);

            DrawRichSegment(segment.Text, segment.Color, segment.Highlight, segment.Bold);
            first = false;
        }
    }

    private void DrawRichBullet(params (string Text, Vector4? Color, bool Highlight, bool Bold)[] segments)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        DrawRichLine(segments);
    }

    private void DrawRichSegment(string text, Vector4? color, bool highlight, bool bold)
    {
        if (highlight)
        {
            Vector4 resolvedColor = color ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            DrawHighlightedText(text, Hex("#000000"), resolvedColor, bold);
            return;
        }

        if (color.HasValue || bold)
        {
            DrawColoredText(text, color ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text], bold);
            return;
        }

        ImGui.TextUnformatted(text);
    }

    private void DrawColoredText(string text, Vector4 color, bool bold)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        uint col = ImGui.ColorConvertFloat4ToU32(color);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddText(pos, col, text);
        if (bold)
            drawList.AddText(pos + new Vector2(1f, 0f), col, text);

        Vector2 size = ImGui.CalcTextSize(text);
        ImGui.Dummy(new Vector2(size.X + (bold ? 1f : 0f), size.Y));
    }

    private void DrawHighlightedText(string text, Vector4 backgroundColor, Vector4 textColor, bool bold)
    {
        Vector2 size = ImGui.CalcTextSize(text);
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 padding = new(2f, 0f);
        Vector2 rectMin = new(pos.X, pos.Y);
        Vector2 rectMax = new(pos.X + size.X + (padding.X * 2f), pos.Y + size.Y);

        var drawList = ImGui.GetWindowDrawList();
        uint bg = ImGui.ColorConvertFloat4ToU32(backgroundColor);
        uint fg = ImGui.ColorConvertFloat4ToU32(textColor);

        drawList.AddRectFilled(rectMin, rectMax, bg, 2f);

        Vector2 textPos = new(pos.X + padding.X, pos.Y);
        drawList.AddText(textPos, fg, text);
        if (bold)
            drawList.AddText(textPos + new Vector2(1f, 0f), fg, text);

        ImGui.Dummy(new Vector2(size.X + (padding.X * 2f), size.Y));
    }

    private static Vector4 Hex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            return Vector4.One;

        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
    }

}
