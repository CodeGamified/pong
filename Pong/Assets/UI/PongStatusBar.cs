// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.TUI;
using Pong.Core;
using Pong.Game;
using Pong.AI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// Pong status bar — TerminalWindow subclass.
    /// Collapsed: single status row (score, stats, time).
    /// Expanded (drag up): full menu — AI difficulty, script reset, leaderboard, controls.
    /// </summary>
    public class PongStatusBar : TerminalWindow
    {
        private PongMatchManager _match;
        private PongLeaderboard _leaderboard;
        private PongAIController _ai;
        private PaddleProgram _playerProgram;

        // Track which sample the player loaded (null = custom/default)
        private AIDifficulty? _playerScriptTier;

        // Expand detection
        private bool IsExpanded => totalRows > 3;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "PONG";
            totalRows = 2;
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
            if (!IsExpanded) return;
            HandleMenuInput();
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

            // Reset player script
            if (Input.GetKeyDown(KeyCode.R) && _playerProgram != null)
            {
                _playerProgram.UploadCode(null);
                _playerScriptTier = null;
                Debug.Log("[Menu] Player script reset to starter code");
            }
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

            // Row 0 — always the status bar (triple-column)
            if (rows.Count > 0)
            {
                rows[0].SetTripleColumnMode(true, 10f);
                rows[0].SetTripleTexts(
                    BuildLeftStatus(),
                    BuildCenterStatus(),
                    BuildRightStatus());
            }

            if (!IsExpanded) return;

            // Separator under status row
            int r = 1;
            SetRow(r++, Separator());

            // ── AI DIFFICULTY ──
            r = RenderSection(r, "AI DIFFICULTY", RenderAIDifficulty);

            // ── YOUR SCRIPT ──
            r = RenderSection(r, "YOUR SCRIPT", RenderScriptInfo);

            // ── LEADERBOARD ──
            r = RenderSection(r, "LEADERBOARD", RenderLeaderboard);

            // ── CONTROLS ──
            r = RenderSection(r, "CONTROLS", RenderControls);
        }

        private delegate int SectionRenderer(int startRow);

        private int RenderSection(int r, string title, SectionRenderer renderer)
        {
            if (r >= totalRows) return r;
            SetRow(r++, $" {TUIColors.Fg(TUIGradient.Sample(0.3f), TUIGlyphs.DiamondFilled)} {TUIColors.Bold(title)}");
            if (r >= totalRows) return r;
            SetRow(r++, $" {Separator(totalChars - 4)}");
            r = renderer(r);
            if (r < totalRows) SetRow(r++, ""); // blank spacer
            return r;
        }

        // ═══════════════════════════════════════════════════════════════
        // STATUS ROW
        // ═══════════════════════════════════════════════════════════════

        private string BuildLeftStatus()
        {
            if (_match == null) return " PONG";
            string you = TUIColors.Fg(TUIColors.BrightCyan, $"YOU: {_match.LeftScore}");
            string aiLabel = _ai != null ? $"AI ({_ai.Difficulty})" : "AI";
            string them = TUIColors.Fg(TUIColors.BrightMagenta, $"{aiLabel}: {_match.RightScore}");
            return $" {you}  {TUIGlyphs.BoxV}  {them}";
        }

        private string BuildCenterStatus()
        {
            string stats = "";
            if (_match != null)
                stats = $"M:{_match.MatchesPlayed} W:{_match.PlayerWins} L:{_match.AIWins}";
            if (_leaderboard != null)
                stats += $" {TUIGlyphs.BoxV} {_leaderboard.CurrentRank}";
            return stats;
        }

        private string BuildRightStatus()
        {
            var sim = SimulationTime.Instance;
            if (sim == null) return "";
            string expand = IsExpanded ? "" : $" {TUIGlyphs.ArrowU} MENU";
            return $"{sim.GetFormattedTimeScale()} {TUIGlyphs.BoxV} +/- speed {TUIGlyphs.BoxV} SPACE pause{expand} ";
        }

        // ═══════════════════════════════════════════════════════════════
        // MENU SECTIONS
        // ═══════════════════════════════════════════════════════════════

        private int RenderAIDifficulty(int r)
        {
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            var names = new[] { "The Tracker", "The Anticipator", "The Predictor", "The Strategist" };

            for (int i = 0; i < diffs.Length && r < totalRows; i++)
            {
                bool active = _ai != null && _ai.Difficulty == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i],-8} {TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i],-8}  ");
                string desc = TUIColors.Dimmed(names[i]);
                SetRow(r++, $"   {key} {label} {desc}");
            }
            return r;
        }

        private int RenderScriptInfo(int r)
        {
            if (_playerProgram == null)
            {
                if (r < totalRows) SetRow(r++, TUIColors.Dimmed("   No player program"));
                return r;
            }

            string name = _playerProgram.ProgramName ?? "PaddleAI";
            int instCount = _playerProgram.Program?.Instructions?.Length ?? 0;
            string status = _playerProgram.IsRunning
                ? TUIColors.Fg(TUIColors.BrightGreen, "RUNNING")
                : TUIColors.Dimmed("STOPPED");
            string tier = _playerScriptTier.HasValue
                ? TUIColors.Fg(TUIColors.BrightMagenta, $"({_playerScriptTier.Value} sample)")
                : TUIColors.Dimmed("(custom)");

            if (r < totalRows)
                SetRow(r++, $"   {TUIColors.Dimmed("Program:")} {name}  {status}  {tier}");
            if (r < totalRows)
                SetRow(r++, $"   {TUIColors.Dimmed("Instructions:")} {instCount}");

            // Load sample scripts
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            var names = new[] { "The Tracker", "The Anticipator", "The Predictor", "The Strategist" };
            for (int i = 0; i < diffs.Length && r < totalRows; i++)
            {
                bool active = _playerScriptTier.HasValue && _playerScriptTier.Value == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[Shift+{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i],-8} {TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i],-8}  ");
                string desc = TUIColors.Dimmed(names[i]);
                SetRow(r++, $"   {key} {label} {desc}");
            }

            if (r < totalRows)
            {
                string key = TUIColors.Fg(TUIColors.BrightCyan, "[R]");
                SetRow(r++, $"   {key}         Reset to starter code");
            }
            return r;
        }

        private int RenderLeaderboard(int r)
        {
            if (_leaderboard == null)
            {
                if (r < totalRows) SetRow(r++, TUIColors.Dimmed("   No leaderboard"));
                return r;
            }

            if (r < totalRows)
                SetRow(r++, $"   {TUIColors.Dimmed("Rank:")} {TUIColors.Fg(TUIColors.BrightGreen, _leaderboard.CurrentRank)}  {TUIColors.Dimmed("Matches:")} {_leaderboard.TotalMatches}");

            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            foreach (var diff in diffs)
            {
                if (r >= totalRows) break;
                if (!_leaderboard.Records.ContainsKey(diff)) continue;
                var rec = _leaderboard.Records[diff];
                string check = rec.Wins >= 3
                    ? TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.Check)
                    : TUIColors.Dimmed(" ");
                string winRate = (rec.Wins + rec.Losses) > 0
                    ? $"{rec.WinRate:P0}"
                    : "---";
                SetRow(r++, $"   {check} {diff,-8} {rec.Wins}W {rec.Losses}L  {TUIColors.Dimmed(winRate)}");
            }
            return r;
        }

        private int RenderControls(int r)
        {
            string[] controls = new[]
            {
                $"   {TUIColors.Fg(TUIColors.BrightCyan, "[1-4]")}       Change AI difficulty",
                $"   {TUIColors.Fg(TUIColors.BrightCyan, "[Shift+1-4]")} Load sample into YOUR code",
                $"   {TUIColors.Fg(TUIColors.BrightCyan, "[R]")}         Reset your code",
                $"   {TUIColors.Fg(TUIColors.BrightCyan, "[+/-]")}       Time scale",
                $"   {TUIColors.Fg(TUIColors.BrightCyan, "[SPACE]")}     Pause / Resume",
                $"   {TUIColors.Fg(TUIColors.BrightCyan, $"[{TUIGlyphs.ArrowU}/{TUIGlyphs.ArrowD}]")}       Scroll code windows",
            };

            for (int i = 0; i < controls.Length && r < totalRows; i++)
                SetRow(r++, controls[i]);

            return r;
        }
    }
}
