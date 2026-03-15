// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CodeGamified.TUI;
using Pong.Game;
using Pong.AI;

namespace Pong.UI
{
    /// <summary>
    /// Center status panel — three-column: MATCH (left) │ TITLE (center) │ LEADERBOARD (right).
    /// </summary>
    public class PongStatusCenter : TerminalWindow
    {
        private PongMatchManager _match;
        private PongLeaderboard _leaderboard;

        private bool _panelsReady;
        private int _col2Start;
        private int _col3Start;

        // ── ASCII art animation ─────────────────────────────────
        private float _asciiTimer;
        private int _asciiPhase;
        private float[] _revealThresholds;
        private const float AsciiHold = 5f;
        private const float AsciiAnim = 0.5f;

        private static readonly char[] GlitchGlyphs =
            "░▒▓█▀▄▌▐╬╫╪╩╦╠╣─│┌┐└┘├┤┬┴┼".ToCharArray();

        private static readonly string[] CodeRows =
        {
            "   █████████  ████████  █████████   █████████  ",
            "  ██         ██      ██ ██      ██ ██          ",
            "  ██         ██      ██ ██      ██ ██████████  ",
            "  ██         ██      ██ ██      ██ ██          ",
            "   █████████  ████████  █████████   █████████  ",
        };

        private static readonly string[] GameRows =
        {
            "   █████████  ████████   ████████   █████████  ",
            "  ██         ██      ██ ██  ██  ██ ██          ",
            "  ██   █████ ██████████ ██  ██  ██ ██████████  ",
            "  ██      ██ ██      ██ ██  ██  ██ ██          ",
            "   █████████ ██      ██ ██  ██  ██  █████████  ",
        };

        private static readonly string[] PingRows =
        {
            "  █████████  ██████████ ██      ██  █████████  ",
            "  ██      ██     ██     ████    ██ ██          ",
            "  █████████      ██     ██  ██  ██ ██   █████  ",
            "  ██             ██     ██    ████ ██      ██  ",
            "  ██         ██████████ ██      ██  █████████  ",
        };

        private static readonly string[] PongRows =
        {
            "  █████████   ████████  ██      ██  █████████  ",
            "  ██      ██ ██      ██ ████    ██ ██          ",
            "  █████████  ██      ██ ██  ██  ██ ██   █████  ",
            "  ██         ██      ██ ██    ████ ██      ██  ",
            "  ██          ████████  ██      ██  █████████  ",
        };

        private bool IsExpanded => totalRows > 3;
        private float _charPx;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "PONG";
            totalRows = 40;
        }

        protected override void OnLayoutReady()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null || rows.Count == 0) return;
            float h = rt.rect.height;
            float rowH = rows[0].RowHeight;
            if (rowH <= 0) return;
            int fitRows = Mathf.Max(2, Mathf.FloorToInt(h / rowH));
            if (fitRows != totalRows)
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(i < fitRows);
                totalRows = fitRows;
            }
            SetupThreePanels();
        }

        private void SetupThreePanels()
        {
            _col2Start = totalChars / 3;
            _col3Start = (totalChars * 2) / 3;
            foreach (var row in rows)
                row.SetThreePanelMode(true, _col2Start, _col3Start);
            _panelsReady = true;
        }

        public void Bind(PongMatchManager match, PongLeaderboard leaderboard)
        {
            _match = match;
            _leaderboard = leaderboard;
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady) return;
            AdvanceAsciiTimer();
        }

        private void AdvanceAsciiTimer()
        {
            _asciiTimer += Time.deltaTime;
            switch (_asciiPhase)
            {
                case 0:
                case 2:
                    if (_asciiTimer >= AsciiHold)
                    {
                        _asciiTimer = 0f;
                        _asciiPhase = (_asciiPhase + 1) % 4;
                        InitRevealThresholds();
                    }
                    break;
                case 1:
                case 3:
                    if (_asciiTimer >= AsciiAnim)
                    {
                        _asciiTimer = 0f;
                        _asciiPhase = (_asciiPhase + 1) % 4;
                    }
                    break;
            }
        }

        private void InitRevealThresholds()
        {
            int innerW = CodeRows[0].Length - ArtBorderLen * 2;
            int totalChars = innerW * 10;
            _revealThresholds = new float[totalChars];
            for (int i = 0; i < totalChars; i++)
                _revealThresholds[i] = Random.value;
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        private void Set3(int r, string p1, string p2, string p3)
        {
            Row(r)?.SetThreePanelTexts(p1 ?? "", p2 ?? "", p3 ?? "");
        }

        protected override void Render()
        {
            ClearAllRows();

            if (!_panelsReady)
            {
                SetRow(0, BuildCollapsedFlat());
                return;
            }

            // Row 0 — collapsed header
            Set3(0, BuildCollapsedLeft(), BuildCollapsedCenter(), BuildCollapsedRight());

            if (!IsExpanded) return;

            // Row 1 — separator per column
            string sep1 = Separator(_col2Start - 5);
            string sep2 = Separator(_col3Start - _col2Start - 5);
            string sep3 = Separator(totalChars - _col3Start - 5);
            Set3(1, sep1, sep2, sep3);

            if (rows.Count > 2) _charPx = rows[2].CharWidth;

            // Rows 2+ — three-column content
            var col1 = BuildMatchColumn();
            var col2 = BuildTitleColumn();
            var col3 = BuildLeaderboardColumn();

            int maxLines = Mathf.Max(col1.Length, Mathf.Max(col2.Length, col3.Length));
            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 2;
                if (r >= totalRows) break;
                Set3(r,
                    i < col1.Length ? col1[i] : "",
                    i < col2.Length ? col2[i] : "",
                    i < col3.Length ? col3[i] : "");
            }
        }

        private string BuildCollapsedFlat()
        {
            string stats = "";
            if (_match != null)
                stats = $"M:{_match.MatchesPlayed} W:{_match.PlayerWins} L:{_match.AIWins}";
            if (_leaderboard != null)
                stats += $" {TUIGlyphs.BoxV} {_leaderboard.CurrentRank}";
            return $" {stats}";
        }

        private string BuildCollapsedLeft()
        {
            if (_match == null) return "";
            string you = TUIColors.Fg(TUIColors.BrightCyan, $"{_match.LeftScore}");
            string them = TUIColors.Fg(TUIColors.BrightMagenta, $"{_match.RightScore}");
            return $" {you} {TUIGlyphs.BoxH}{TUIGlyphs.BoxH} {them}";
        }

        private string BuildCollapsedCenter()
        {
            return $" {TUIColors.Bold("PONG")}";
        }

        private string BuildCollapsedRight()
        {
            if (_leaderboard == null) return "";
            return $" {_leaderboard.CurrentRank}";
        }

        // ── Column 1: MATCH ─────────────────────────────────────

        private string[] BuildMatchColumn()
        {
            var lines = new List<string>();

            lines.Add($" {TUIColors.Bold("MATCH")}");

            if (_match != null)
            {
                lines.Add("");
                string you = TUIColors.Fg(TUIColors.BrightCyan, $"{_match.LeftScore}");
                string them = TUIColors.Fg(TUIColors.BrightMagenta, $"{_match.RightScore}");
                lines.Add($"  {you}  {TUIGlyphs.BoxH}{TUIGlyphs.BoxH}  {them}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "YOU")}    {TUIColors.Fg(TUIColors.BrightMagenta, "AI")}");
                lines.Add("");
                lines.Add($"  M: {_match.MatchesPlayed}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"W:{_match.PlayerWins}")}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightMagenta, $"L:{_match.AIWins}")}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No match"));
            }

            return lines.ToArray();
        }

        // ── Column 2: TITLE (ASCII art) ─────────────────────────

        private string[] BuildTitleColumn()
        {
            var lines = new List<string>();
            lines.AddRange(BuildAsciiArt());
            return lines.ToArray();
        }

        // ── Column 3: LEADERBOARD ───────────────────────────────

        private string[] BuildLeaderboardColumn()
        {
            var lines = new List<string>();

            lines.Add($" {TUIColors.Bold("LEADERBOARD")}");

            if (_leaderboard != null)
            {
                lines.Add("");
                lines.Add($"  Rank:");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, _leaderboard.CurrentRank)}");
                lines.Add($"  Total: {_leaderboard.TotalMatches}");
                lines.Add("");

                var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
                foreach (var diff in diffs)
                {
                    if (!_leaderboard.Records.ContainsKey(diff)) continue;
                    var rec = _leaderboard.Records[diff];
                    string check = rec.Wins >= 3
                        ? TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.Check)
                        : TUIColors.Dimmed(" ");
                    string wr = (rec.Wins + rec.Losses) > 0 ? $"{rec.WinRate:P0}" : "---";
                    lines.Add($"  {check} {diff}");
                    lines.Add($"    {rec.Wins}W/{rec.Losses}L");
                    lines.Add($"    {TUIColors.Dimmed(wr)}");
                }
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No data"));
            }

            return lines.ToArray();
        }

        // ── ASCII ART ENGINE ────────────────────────────────────

        private const int ArtBorderLen = 0;

        private static string StripArtBorder(string row)
            => row.Substring(ArtBorderLen, row.Length - ArtBorderLen * 2);

        private string[] BuildAsciiArt()
        {
            switch (_asciiPhase)
            {
                case 0: return ColorizeBlock(CodeRows, GameRows);
                case 2: return ColorizeBlock(PingRows, PongRows);
                case 1: return DecipherBlock(CodeRows, GameRows, PingRows, PongRows);
                case 3: return DecipherBlock(PingRows, PongRows, CodeRows, GameRows);
                default: return new string[15];
            }
        }

        private string Mono(string text)
            => _charPx > 0 ? $"<mspace={_charPx:F1}px>{text}</mspace>" : text;

        private static Color32 GradientAt(float t)
        {
            byte r = (byte)Mathf.Lerp(TUIColors.BrightCyan.r, TUIColors.BrightMagenta.r, t);
            byte g = (byte)Mathf.Lerp(TUIColors.BrightCyan.g, TUIColors.BrightMagenta.g, t);
            byte b = (byte)Mathf.Lerp(TUIColors.BrightCyan.b, TUIColors.BrightMagenta.b, t);
            return new Color32(r, g, b, 255);
        }

        private string GradientBorderH(char left, char fill, char right, int innerWidth)
        {
            int total = innerWidth + 2;
            var sb = new StringBuilder(total * 32);
            sb.Append(TUIColors.Fg(GradientAt(0f), left.ToString()));
            for (int i = 0; i < innerWidth; i++)
            {
                float t = total > 1 ? (float)(i + 1) / (total - 1) : 0f;
                sb.Append(TUIColors.Fg(GradientAt(t), fill.ToString()));
            }
            sb.Append(TUIColors.Fg(GradientAt(1f), right.ToString()));
            return Mono(sb.ToString());
        }

        private string GradientBorderV(string rawContent)
        {
            var sb = new StringBuilder(rawContent.Length + 128);
            sb.Append(TUIColors.Fg(GradientAt(0f), "║"));
            sb.Append(rawContent);
            sb.Append(TUIColors.Fg(GradientAt(1f), "║"));
            return Mono(sb.ToString());
        }

        private string GradientRowRaw(string row, int totalBorderedWidth)
        {
            int len = row.Length;
            if (len == 0) return "";
            var sb = new StringBuilder(len * 32);
            for (int i = 0; i < len; i++)
            {
                float t = totalBorderedWidth > 1 ? (float)(i + 1) / (totalBorderedWidth - 1) : 0f;
                sb.Append(TUIColors.Fg(GradientAt(t), row[i].ToString()));
            }
            return sb.ToString();
        }

        private string[] ColorizeBlock(string[] top, string[] bot)
        {
            int innerW = StripArtBorder(top[0]).Length;
            int totalW = innerW + 2;
            var lines = new string[15];
            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int i = 0; i < 5; i++)
                lines[2 + i] = GradientBorderV(GradientRowRaw(StripArtBorder(top[i]), totalW));
            lines[7] = GradientBorderV(new string(' ', innerW));
            for (int i = 0; i < 5; i++)
                lines[8 + i] = GradientBorderV(GradientRowRaw(StripArtBorder(bot[i]), totalW));
            lines[13] = GradientBorderV(new string(' ', innerW));
            lines[14] = GradientBorderH('╚', '═', '╝', innerW);
            return lines;
        }

        private string[] DecipherBlock(string[] srcTop, string[] srcBot,
                                       string[] tgtTop, string[] tgtBot)
        {
            float progress = Mathf.Clamp01(_asciiTimer / AsciiAnim);
            int innerW = StripArtBorder(tgtTop[0]).Length;
            int totalW = innerW + 2;
            var lines = new string[15];

            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int r = 0; r < 5; r++)
                lines[2 + r] = GradientBorderV(
                    DecipherRowRaw(StripArtBorder(srcTop[r]), StripArtBorder(tgtTop[r]),
                                   progress, r * innerW, totalW));
            lines[7] = GradientBorderV(new string(' ', innerW));
            for (int r = 0; r < 5; r++)
                lines[8 + r] = GradientBorderV(
                    DecipherRowRaw(StripArtBorder(srcBot[r]), StripArtBorder(tgtBot[r]),
                                   progress, (5 + r) * innerW, totalW));
            lines[13] = GradientBorderV(new string(' ', innerW));
            lines[14] = GradientBorderH('╚', '═', '╝', innerW);
            return lines;
        }

        private string DecipherRowRaw(string src, string tgt, float progress,
                                      int threshOffset, int totalBorderedWidth)
        {
            int len = tgt.Length;
            var sb = new StringBuilder(len * 32);

            for (int i = 0; i < len; i++)
            {
                float t = totalBorderedWidth > 1 ? (float)(i + 1) / (totalBorderedWidth - 1) : 0f;
                char srcCh = i < src.Length ? src[i] : ' ';
                char tgtCh = tgt[i];

                if (srcCh == tgtCh)
                {
                    sb.Append(TUIColors.Fg(GradientAt(t), tgtCh.ToString()));
                    continue;
                }

                int idx = threshOffset + i;
                bool isSettled = _revealThresholds != null
                    && idx < _revealThresholds.Length
                    && progress >= _revealThresholds[idx];

                char ch;

                if (isSettled)
                {
                    ch = tgtCh;
                }
                else
                {
                    bool hasContent = srcCh != ' ' || tgtCh != ' ';
                    ch = hasContent ? GlitchGlyphs[Random.Range(0, GlitchGlyphs.Length)] : ' ';
                }

                Color32 color = isSettled
                    ? GradientAt(t)
                    : Color32.Lerp(TUIColors.BrightYellow, GradientAt(t), progress);
                sb.Append(TUIColors.Fg(color, ch.ToString()));
            }

            return sb.ToString();
        }
    }
}
