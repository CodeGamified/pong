// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CodeGamified.TUI;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using Pong.Game;
using Pong.AI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// Pong status bar — 3-column TerminalWindow.
    ///
    /// Collapsed (2 rows): single triple-column status line.
    /// Expanded (drag up): full 3-column layout:
    ///   LEFT:   Script controls (YOU top, AI bottom)
    ///   CENTER: "PONG" ASCII art + score + match stats
    ///   RIGHT:  Settings / controls / keybinds
    /// </summary>
    public class PongStatusBar : TerminalWindow
    {
        private PongMatchManager _match;
        private PongLeaderboard _leaderboard;
        private PongAIController _ai;
        private PaddleProgram _playerProgram;

        // Track which sample the player loaded (null = custom/default)
        private AIDifficulty? _playerScriptTier;

        // ── ASCII art animation ─────────────────────────────────
        private float _asciiTimer;
        private int _asciiPhase; // 0=CODE/GAME, 1=anim→PING/PONG, 2=PING/PONG, 3=anim→CODE/GAME
        private float[] _revealThresholds;
        private const float AsciiHold = 5f;
        private const float AsciiAnim = 1f;

        private static readonly char[] GlitchGlyphs =
            "░▒▓█▀▄▌▐╬╫╪╩╦╠╣─│┌┐└┘├┤┬┴┼".ToCharArray();

        // ── Whole-word ASCII glyphs (human-editable) ────────────
        // Each array is 5 rows. All rows within a pair must be the same length.
        // Edit these directly — no letter bank, no assembly.

        private static readonly string[] CodeRows =
        {
            "░▒▓██▓▒░   █████████  ████████  █████████   █████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██      ██ ██      ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██      ██ ██      ██ ██████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██      ██ ██      ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░   █████████  ████████  █████████   █████████  ░▒▓██▓▒░",
        };

        private static readonly string[] GameRows =
        {
            "░▒▓██▓▒░   █████████  ████████   ████████   █████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██      ██ ██  ██  ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██   █████ ██████████ ██  ██  ██ ██████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██      ██ ██      ██ ██  ██  ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░   █████████ ██      ██ ██  ██  ██  █████████  ░▒▓██▓▒░",
        };

        private static readonly string[] PingRows =
        {
            "░▒▓██▓▒░  █████████  ██████████ ██      ██  █████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██      ██     ██     ████    ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░  █████████      ██     ██  ██  ██ ██   █████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██             ██     ██    ████ ██      ██  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██████████ ██      ██  █████████  ░▒▓██▓▒░",
        };

        private static readonly string[] PongRows =
        {
            "░▒▓██▓▒░  █████████   ████████  ██      ██  █████████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██      ██ ██      ██ ████    ██ ██          ░▒▓██▓▒░",
            "░▒▓██▓▒░  █████████  ██      ██ ██  ██  ██ ██   █████  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██         ██      ██ ██    ████ ██      ██  ░▒▓██▓▒░",
            "░▒▓██▓▒░  ██          ████████  ██      ██  █████████  ░▒▓██▓▒░",
        };

        // Expand detection
        private bool IsExpanded => totalRows > 3;
        private float _charPx; // cached char advance for <mspace> tag

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "PONG";
            // Don't hardcode totalRows — let CheckResize() compute from panel height.
            // Use a large initial value so BuildRows() creates enough rows for the 25% panel.
            totalRows = 40;
        }

        /// <summary>
        /// Called after layout measurement. Recalculate row count from actual panel height
        /// so the status bar fills its space on first load.
        /// </summary>
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
        }

        public void Bind(PongMatchManager match, PongLeaderboard leaderboard,
                         PongAIController ai, PaddleProgram playerProgram = null)
        {
            _match = match;
            _leaderboard = leaderboard;
            _ai = ai;
            _playerProgram = playerProgram;
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady) return;
            AdvanceAsciiTimer();
            if (!IsExpanded) return;
            HandleMenuInput();
        }

        private void AdvanceAsciiTimer()
        {
            _asciiTimer += UnityEngine.Time.deltaTime;
            switch (_asciiPhase)
            {
                case 0: // showing CODE/GAME
                case 2: // showing PING/PONG
                    if (_asciiTimer >= AsciiHold)
                    {
                        _asciiTimer = 0f;
                        _asciiPhase = (_asciiPhase + 1) % 4;
                        InitRevealThresholds();
                    }
                    break;
                case 1: // animating → PING/PONG
                case 3: // animating → CODE/GAME
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
            int totalChars = CodeRows[0].Length * 10; // 43 × 10 rows
            _revealThresholds = new float[totalChars];
            for (int i = 0; i < totalChars; i++)
                _revealThresholds[i] = Random.value;
        }

        private void HandleMenuInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Easy);
                else SetAIDifficulty(AIDifficulty.Easy);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Medium);
                else SetAIDifficulty(AIDifficulty.Medium);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Hard);
                else SetAIDifficulty(AIDifficulty.Hard);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Expert);
                else SetAIDifficulty(AIDifficulty.Expert);
            }

            if (Input.GetKeyDown(KeyCode.R) && _playerProgram != null)
            {
                _playerProgram.UploadCode(null);
                _playerScriptTier = null;
                Debug.Log("[Menu] Player script reset to starter code");
            }

            // Quality cycle
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (SettingsBridge.QualityLevel + 1) % 4;
                SettingsBridge.SetQualityLevel(next);
                Debug.Log($"[Menu] Quality → {(QualityTier)next}");
            }

            // Font size: [ / ] keys
            if (Input.GetKeyDown(KeyCode.RightBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f);
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f);

            // Audio: F5/F6/F7 cycle Master/Music/SFX by ±10%
            if (Input.GetKeyDown(KeyCode.F5))
                SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F6))
                SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F7))
                SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume + (shift ? -0.1f : 0.1f));
        }

        private void LoadPlayerSample(AIDifficulty diff)
        {
            if (_playerProgram == null) return;
            string code = PongAIController.GetSampleCode(diff);
            _playerProgram.UploadCode(code);
            _playerScriptTier = diff;
            Debug.Log($"[Menu] Player script → {diff} sample");
        }

        private void SetAIDifficulty(AIDifficulty diff)
        {
            if (_ai == null) return;
            _ai.SetDifficulty(diff);
            Debug.Log($"[Menu] AI difficulty → {diff}");
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        protected override void Render()
        {
            ClearAllRows();

            // Row 0 — collapsed status (always visible)
            if (rows.Count > 0)
            {
                rows[0].SetTripleColumnMode(true, 10f);
                rows[0].SetTripleTexts(
                    BuildCollapsedLeft(),
                    BuildCollapsedCenter(),
                    BuildCollapsedRight());
            }

            if (!IsExpanded) return;

            // Row 1 — separator
            SetRow(1, Separator());

            // Rows 2+ — three-column expanded layout
            RenderExpandedLayout();
        }

        // ═══════════════════════════════════════════════════════════════
        // COLLAPSED (single row — same as before)
        // ═══════════════════════════════════════════════════════════════

        private string BuildCollapsedLeft()
        {
            if (_match == null) return " PONG";
            string you = TUIColors.Fg(TUIColors.BrightCyan, $"YOU: {_match.LeftScore}");
            string aiLabel = _ai != null ? $"AI ({_ai.Difficulty})" : "AI";
            string them = TUIColors.Fg(TUIColors.BrightMagenta, $"{aiLabel}: {_match.RightScore}");
            return $" {you}  {TUIGlyphs.BoxV}  {them}";
        }

        private string BuildCollapsedCenter()
        {
            string stats = "";
            if (_match != null)
                stats = $"M:{_match.MatchesPlayed} W:{_match.PlayerWins} L:{_match.AIWins}";
            if (_leaderboard != null)
                stats += $" {TUIGlyphs.BoxV} {_leaderboard.CurrentRank}";
            return stats;
        }

        private string BuildCollapsedRight()
        {
            var sim = SimulationTime.Instance;
            if (sim == null) return "";
            string expand = IsExpanded ? "" : $" {TUIGlyphs.ArrowU} MENU";
            return $"{sim.GetFormattedTimeScale()} {TUIGlyphs.BoxV} +/- speed {TUIGlyphs.BoxV} SPACE pause{expand} ";
        }

        // ═══════════════════════════════════════════════════════════════
        // EXPANDED — 3-column layout
        //   LEFT:   Your Script / AI Script
        //   CENTER: PONG ASCII + score + stats
        //   RIGHT:  Controls / keybinds
        // ═══════════════════════════════════════════════════════════════

        private void RenderExpandedLayout()
        {
            // Triple-column: left-aligned | center-aligned | right-aligned.
            // ASCII art spacing is preserved by <mspace> tags in Mono().
            var left = BuildLeftColumn();
            var center = BuildCenterColumn();
            var right = BuildRightColumn();

            int maxLines = Mathf.Max(left.Length, Mathf.Max(center.Length, right.Length));

            // Cache char pixel width for <mspace> tag (forces █ and space to same advance)
            if (rows.Count > 2) _charPx = rows[2].CharWidth;

            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 2; // offset past row 0 (status) and row 1 (separator)
                if (r >= totalRows) break;

                rows[r].SetTripleColumnMode(true, 6f);

                string l = i < left.Length   ? left[i]   : "";
                string c = i < center.Length ? center[i] : "";
                string rt = i < right.Length  ? right[i]  : "";

                rows[r].SetTripleTexts(l, c, rt);
            }
        }

        // ── LEFT COLUMN: Script controls ────────────────────────

        private string[] BuildLeftColumn()
        {
            var lines = new System.Collections.Generic.List<string>();

            // Header
            lines.Add($" {TUIColors.Fg(TUIColors.BrightCyan, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("YOUR SCRIPT")}");

            if (_playerProgram != null)
            {
                string name = _playerProgram.ProgramName ?? "PaddleAI";
                int inst = _playerProgram.Program?.Instructions?.Length ?? 0;
                string status = _playerProgram.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STOP");
                string tier = _playerScriptTier.HasValue
                    ? TUIColors.Fg(TUIColors.BrightMagenta, $"({_playerScriptTier.Value})")
                    : TUIColors.Dimmed("(custom)");
                lines.Add($"  {name} {status} {tier}");
                lines.Add($"  {TUIColors.Dimmed($"{inst} instructions")}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
            }

            // Sample loader
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("Load sample:")}");
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _playerScriptTier.HasValue && _playerScriptTier.Value == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[S+{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                lines.Add($"  {key} {label}");
            }

            lines.Add("");

            // AI section
            lines.Add($" {TUIColors.Fg(TUIColors.BrightMagenta, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("AI OPPONENT")}");
            string aiDiff = _ai != null ? _ai.Difficulty.ToString() : "?";
            lines.Add($"  Difficulty: {TUIColors.Fg(TUIColors.BrightYellow, aiDiff)}");

            lines.Add($"  {TUIColors.Dimmed("Set AI:")}");
            var names = new[] { "Tracker", "Anticipator", "Predictor", "Strategist" };
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _ai != null && _ai.Difficulty == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                string desc = TUIColors.Dimmed(names[i]);
                lines.Add($"  {key} {label} {desc}");
            }

            return lines.ToArray();
        }

        // ── CENTER COLUMN: ASCII art + score + stats ────────────

        private string[] BuildCenterColumn()
        {
            var lines = new List<string>();

            // Animated ASCII art (5 rows top word + blank + 5 rows bottom word)
            lines.AddRange(BuildAsciiArt());
            lines.Add("");

            // Score
            if (_match != null)
            {
                string you = TUIColors.Fg(TUIColors.BrightCyan, $"{_match.LeftScore}");
                string them = TUIColors.Fg(TUIColors.BrightMagenta, $"{_match.RightScore}");
                lines.Add($"    {you}  {TUIGlyphs.BoxH}{TUIGlyphs.BoxH}  {them}");
                lines.Add($"   {TUIColors.Fg(TUIColors.BrightCyan, "YOU")}    {TUIColors.Fg(TUIColors.BrightMagenta, "AI")}");
                lines.Add("");
                lines.Add($"  Matches: {_match.MatchesPlayed}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"W:{_match.PlayerWins}")}  {TUIColors.Fg(TUIColors.BrightMagenta, $"L:{_match.AIWins}")}");
            }

            // Leaderboard rank
            if (_leaderboard != null)
            {
                lines.Add("");
                lines.Add($"  Rank: {TUIColors.Fg(TUIColors.BrightGreen, _leaderboard.CurrentRank)}");
                lines.Add($"  Total: {_leaderboard.TotalMatches}");

                var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
                foreach (var diff in diffs)
                {
                    if (!_leaderboard.Records.ContainsKey(diff)) continue;
                    var rec = _leaderboard.Records[diff];
                    string check = rec.Wins >= 3
                        ? TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.Check)
                        : TUIColors.Dimmed(" ");
                    string wr = (rec.Wins + rec.Losses) > 0 ? $"{rec.WinRate:P0}" : "---";
                    lines.Add($"  {check} {diff,-7} {rec.Wins}W/{rec.Losses}L {TUIColors.Dimmed(wr)}");
                }
            }

            return lines.ToArray();
        }

        // ── ASCII ART ENGINE ────────────────────────────────────

        /// <summary>Build 11 lines: 5 top-word rows + blank + 5 bottom-word rows.</summary>
        private string[] BuildAsciiArt()
        {
            switch (_asciiPhase)
            {
                case 0: return ColorizeBlock(CodeRows, GameRows);
                case 2: return ColorizeBlock(PingRows, PongRows);
                case 1: return DecipherBlock(CodeRows, GameRows, PingRows, PongRows);
                case 3: return DecipherBlock(PingRows, PongRows, CodeRows, GameRows);
                default: return new string[11];
            }
        }

        /// <summary>Wrap text in mspace tag to force uniform char advance.</summary>
        private string Mono(string text)
            => _charPx > 0 ? $"<mspace={_charPx:F1}px>{text}</mspace>" : text;

        /// <summary>Lerp a Color32 by column position for horizontal gradient.</summary>
        private static Color32 GradientAt(float t)
        {
            // Accent1 (cyan) → Accent2 (magenta)
            byte r = (byte)Mathf.Lerp(TUIColors.BrightCyan.r, TUIColors.BrightMagenta.r, t);
            byte g = (byte)Mathf.Lerp(TUIColors.BrightCyan.g, TUIColors.BrightMagenta.g, t);
            byte b = (byte)Mathf.Lerp(TUIColors.BrightCyan.b, TUIColors.BrightMagenta.b, t);
            return new Color32(r, g, b, 255);
        }

        /// <summary>Apply per-character horizontal gradient to a row string.</summary>
        private string GradientRow(string row)
        {
            int len = row.Length;
            if (len == 0) return "";
            var sb = new StringBuilder(len * 32);
            for (int i = 0; i < len; i++)
            {
                float t = len > 1 ? (float)i / (len - 1) : 0f;
                sb.Append(TUIColors.Fg(GradientAt(t), row[i].ToString()));
            }
            return Mono(sb.ToString());
        }

        /// <summary>Static display: both words use horizontal gradient.</summary>
        private string[] ColorizeBlock(string[] top, string[] bot)
        {
            var lines = new string[11];
            for (int i = 0; i < 5; i++)
                lines[i] = GradientRow(top[i]);
            lines[5] = "";
            for (int i = 0; i < 5; i++)
                lines[6 + i] = GradientRow(bot[i]);
            return lines;
        }

        /// <summary>Decipher animation: characters settle from source to target.</summary>
        private string[] DecipherBlock(string[] srcTop, string[] srcBot,
                                       string[] tgtTop, string[] tgtBot)
        {
            float progress = Mathf.Clamp01(_asciiTimer / AsciiAnim);
            int w = tgtTop[0].Length;
            var lines = new string[11];

            for (int r = 0; r < 5; r++)
                lines[r] = DecipherRow(srcTop[r], tgtTop[r], progress, r * w);
            lines[5] = "";
            for (int r = 0; r < 5; r++)
                lines[6 + r] = DecipherRow(srcBot[r], tgtBot[r], progress, (5 + r) * w);
            return lines;
        }

        /// <summary>Build one row of the decipher animation with per-char gradient reveal.</summary>
        private string DecipherRow(string src, string tgt, float progress,
                                   int threshOffset)
        {
            int len = tgt.Length;
            var sb = new StringBuilder(len * 32);

            for (int i = 0; i < len; i++)
            {
                float t = len > 1 ? (float)i / (len - 1) : 0f;
                char srcCh = i < src.Length ? src[i] : ' ';
                char tgtCh = tgt[i];

                // Unchanged characters stay static — no glitch
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

                // Unsettled chars shift the gradient toward yellow
                Color32 color = isSettled
                    ? GradientAt(t)
                    : Color32.Lerp(TUIColors.BrightYellow, GradientAt(t), progress);
                sb.Append(TUIColors.Fg(color, ch.ToString()));
            }

            return Mono(sb.ToString());
        }

        // ── RIGHT COLUMN: Settings / Controls ───────────────────

        private string[] BuildRightColumn()
        {
            var lines = new List<string>();
            var sim = SimulationTime.Instance;
            string speed = sim != null ? sim.GetFormattedTimeScale() : "1x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED")
                : "";

            // ── Controls ───────────────────
            lines.Add($" {TUIColors.Bold("CONTROLS")}{paused}");
            lines.Add($"  Speed: {TUIColors.Fg(TUIColors.BrightGreen, speed)}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[1-4]")}     AI difficulty");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[S+1-4]")}   Load sample");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[R]")}       Reset code");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[+/-]")}     Time scale");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[SPACE]")}   Pause");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[W]")}       Warp 10");
            lines.Add("");

            // ── Quality ────────────────────
            string tierName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            lines.Add($" {TUIColors.Bold("QUALITY")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, tierName)}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[Q]")}       Cycle quality");
            lines.Add("");

            // ── Audio ──────────────────────
            lines.Add($" {TUIColors.Bold("AUDIO")}");
            lines.Add($"  Master: {VolumeBar(SettingsBridge.MasterVolume)}");
            lines.Add($"  Music:  {VolumeBar(SettingsBridge.MusicVolume)}");
            lines.Add($"  SFX:    {VolumeBar(SettingsBridge.SfxVolume)}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F5]")}  Master  {TUIColors.Fg(TUIColors.BrightCyan, "[S+F5]")} -");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F6]")}  Music   {TUIColors.Fg(TUIColors.BrightCyan, "[S+F6]")} -");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F7]")}  SFX     {TUIColors.Fg(TUIColors.BrightCyan, "[S+F7]")} -");
            lines.Add("");

            // ── Display ────────────────────
            lines.Add($" {TUIColors.Bold("DISPLAY")}");
            lines.Add($"  Font: {TUIColors.Fg(TUIColors.BrightGreen, $"{SettingsBridge.FontSize:F0}pt")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[]]")}  Font+   {TUIColors.Fg(TUIColors.BrightCyan, "[[]")}  Font-");

            return lines.ToArray();
        }

        /// <summary>Render a 10-char volume bar: [██████░░░░] 60%</summary>
        private static string VolumeBar(float vol)
        {
            int filled = Mathf.RoundToInt(vol * 10);
            string bar = new string('█', filled) + new string('░', 10 - filled);
            string pct = $"{vol * 100:F0}%";
            return $"[{TUIColors.Fg(TUIColors.BrightGreen, bar)}] {TUIColors.Dimmed(pct)}";
        }
    }
}
