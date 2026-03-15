// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.TUI;
using Pong.Game;
using Pong.AI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// Left status panel — two-column: YOUR SCRIPT (left) │ AI OPPONENT (right).
    /// Right-edge dragger is linked to the player code debugger above.
    /// </summary>
    public class PongStatusLeft : TerminalWindow
    {
        private PongMatchManager _match;
        private PaddleProgram _playerProgram;
        private PongAIController _ai;
        private AIDifficulty? _playerScriptTier;

        private bool IsExpanded => totalRows > 3;
        private bool _dualReady;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "SCRIPTS";
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
            SetupDualColumns();
        }

        private void SetupDualColumns()
        {
            dividerPos = totalChars / 2;
            leftColWidth = dividerPos - 1;
            rightColWidth = totalChars - dividerPos - 1;
            foreach (var row in rows)
                row.SetDualColumnMode(true, dividerPos);
            _dualReady = true;
        }

        public void Bind(PongMatchManager match, PaddleProgram playerProgram, PongAIController ai)
        {
            _match = match;
            _playerProgram = playerProgram;
            _ai = ai;
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady || !IsExpanded) return;
            HandleInput();
        }

        private void HandleInput()
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

        protected override void Render()
        {
            ClearAllRows();

            if (!_dualReady)
            {
                SetRow(0, BuildCollapsedLeft());
                return;
            }

            // Row 0 — collapsed header
            Row(0)?.SetBothTexts(BuildCollapsedLeft(), BuildCollapsedRight());

            if (!IsExpanded) return;

            // Row 1 — separator
            string sepL = TUIColors.Dimmed(TUIWidgets.Divider(Mathf.Max(1, leftColWidth - 2)));
            string sepR = TUIColors.Dimmed(TUIWidgets.Divider(Mathf.Max(1, rightColWidth - 2)));
            Row(1)?.SetBothTexts(sepL, sepR);

            // Rows 2+ — dual-column content
            var left = BuildLeftContent();
            var right = BuildRightContent();
            int maxLines = Mathf.Max(left.Length, right.Length);

            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 2;
                if (r >= totalRows) break;
                string l = i < left.Length ? left[i] : "";
                string rt = i < right.Length ? right[i] : "";
                Row(r)?.SetBothTexts(l, rt);
            }
        }

        private string BuildCollapsedLeft()
        {
            if (_match == null) return " PONG";
            string you = TUIColors.Fg(TUIColors.BrightCyan, $"YOU: {_match.LeftScore}");
            return $" {you}";
        }

        private string BuildCollapsedRight()
        {
            if (_match == null) return "";
            string aiLabel = _ai != null ? $"AI ({_ai.Difficulty})" : "AI";
            return $" {TUIColors.Fg(TUIColors.BrightMagenta, $"{aiLabel}: {_match.RightScore}")}";
        }

        // ── Left column: YOUR SCRIPT ─────────────────────────────

        private string[] BuildLeftContent()
        {
            var lines = new List<string>();

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
                lines.Add($"  {name}");
                lines.Add($"  {status} {tier}");
                lines.Add($"  {TUIColors.Dimmed($"{inst} inst")}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
            }

            lines.Add("");
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
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[R]")} {TUIColors.Dimmed("Reset")}");

            return lines.ToArray();
        }

        // ── Right column: AI OPPONENT ────────────────────────────

        private string[] BuildRightContent()
        {
            var lines = new List<string>();

            lines.Add($" {TUIColors.Fg(TUIColors.BrightMagenta, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("AI OPPONENT")}");

            string aiDiff = _ai != null ? _ai.Difficulty.ToString() : "?";
            lines.Add($"  Difficulty:");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightYellow, aiDiff)}");
            lines.Add("");

            lines.Add("");
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            var names = new[] { "Tracker", "Anticipator", "Predictor", "Strategist" };
            lines.Add($"  {TUIColors.Dimmed("Set AI:")}");
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _ai != null && _ai.Difficulty == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                string desc = TUIColors.Dimmed(names[i]);
                lines.Add($"  {key} {label}");
                lines.Add($"   {desc}");
            }

            return lines.ToArray();
        }
    }
}
