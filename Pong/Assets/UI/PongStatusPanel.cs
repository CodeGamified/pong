// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
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
    /// Unified status panel — 7 columns with draggable dividers:
    ///   YOU │ AI │ MATCH │ PONG │ CONTROLS │ SETTINGS │ AUDIO
    /// Merges the former StatusLeft, StatusCenter, StatusRight into one panel.
    /// </summary>
    public class PongStatusPanel : TerminalWindow
    {
        // ── Dependencies ────────────────────────────────────────
        private PongMatchManager _match;
        private PaddleProgram _playerProgram;
        private PongAIController _ai;
        private AIDifficulty? _playerScriptTier;
        private string _playerInputMode;

        // ── Column layout (7 columns, 6 draggers) ───────────────
        private const int COL_COUNT = 7;
        private float[] _colRatios = { 0f, 0.11f, 0.22f, 0.33f, 0.67f, 0.78f, 0.89f };
        private int[] _colPositions;
        private TUIColumnDragger[] _colDraggers;
        private bool _columnsReady;

        // ── Audio slider overlays ───────────────────────────────
        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;

        // ── ASCII art animation ─────────────────────────────────
        private float _asciiTimer;
        private int _asciiPhase;
        private float[] _revealThresholds;
        private const float AsciiHold = 5f;
        private const float AsciiAnim = 0.5f;
        private float _charPx;

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

        // ── Button overlays ─────────────────────────────────────
        private bool _buttonsCreated;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "PONG";
            totalRows = 40;
        }

        public void Bind(PongMatchManager match, PaddleProgram playerProgram, PongAIController ai)
        {
            _match = match;
            _playerProgram = playerProgram;
            _ai = ai;
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
            SetupColumns();
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady) return;
            AdvanceAsciiTimer();
            if (IsExpanded) HandleInput();
        }

        // ═══════════════════════════════════════════════════════════════
        // COLUMN LAYOUT
        // ═══════════════════════════════════════════════════════════════

        private void SetupColumns()
        {
            ComputeColumnPositions();
            _hoverColumnPositions = _colPositions;

            foreach (var row in rows)
                row.SetNPanelMode(true, _colPositions);

            _columnsReady = true;

            if (_colDraggers == null)
            {
                _colDraggers = new TUIColumnDragger[COL_COUNT - 1];
                for (int i = 0; i < COL_COUNT - 1; i++)
                {
                    int idx = i;
                    int minPos = (i > 0 ? _colPositions[i] : 0) + 4;
                    int maxPos = (i + 2 < COL_COUNT ? _colPositions[i + 2] : totalChars) - 4;
                    _colDraggers[i] = AddColumnDragger(
                        _colPositions[i + 1], minPos, maxPos, pos => OnColumnDragged(idx, pos));
                }
            }
            else
            {
                float cw = rows.Count > 0 ? rows[0].CharWidth : 10f;
                for (int i = 0; i < COL_COUNT - 1; i++)
                {
                    _colDraggers[i].UpdateCharWidth(cw);
                    _colDraggers[i].UpdatePosition(_colPositions[i + 1]);
                    UpdateDraggerLimits(i);
                }
            }

            CreateAudioSliders();
            CreateOrRepositionButtons();
        }

        private void ComputeColumnPositions()
        {
            _colPositions = new int[COL_COUNT];
            _colPositions[0] = 0;
            for (int i = 1; i < COL_COUNT; i++)
            {
                int minPos = _colPositions[i - 1] + 4;
                int maxPos = totalChars - (COL_COUNT - i) * 4;
                _colPositions[i] = Mathf.Clamp(
                    Mathf.RoundToInt(totalChars * _colRatios[i]), minPos, maxPos);
            }
        }

        private void OnColumnDragged(int draggerIndex, int newPos)
        {
            // draggerIndex is 0-based; it controls _colPositions[draggerIndex + 1]
            int colIdx = draggerIndex + 1;
            _colPositions[colIdx] = newPos;
            _colRatios[colIdx] = (float)newPos / totalChars;

            // Update neighbour dragger limits
            if (draggerIndex > 0) UpdateDraggerLimits(draggerIndex - 1);
            if (draggerIndex < COL_COUNT - 2) UpdateDraggerLimits(draggerIndex + 1);

            ApplyNPanelResize(_colPositions);
            _hoverColumnPositions = _colPositions;

            // Reposition audio sliders if col 6 boundary moved
            if (colIdx == 6) CreateAudioSliders();
        }

        private void UpdateDraggerLimits(int draggerIdx)
        {
            int minPos = _colPositions[draggerIdx] + 4;
            int maxPos = (draggerIdx + 2 < COL_COUNT ? _colPositions[draggerIdx + 2] : totalChars) - 4;
            _colDraggers[draggerIdx].UpdateLimits(minPos, maxPos);
        }

        private int ColWidth(int colIdx)
        {
            if (_colPositions == null) return 10;
            int end = colIdx + 1 < COL_COUNT ? _colPositions[colIdx + 1] : totalChars;
            return end - _colPositions[colIdx];
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT
        // ═══════════════════════════════════════════════════════════════

        private void HandleInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Script / AI selection
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            { if (shift) SetAIDifficulty(AIDifficulty.Easy); else LoadPlayerSample(AIDifficulty.Easy); }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            { if (shift) SetAIDifficulty(AIDifficulty.Medium); else LoadPlayerSample(AIDifficulty.Medium); }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            { if (shift) SetAIDifficulty(AIDifficulty.Hard); else LoadPlayerSample(AIDifficulty.Hard); }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            { if (shift) SetAIDifficulty(AIDifficulty.Expert); else LoadPlayerSample(AIDifficulty.Expert); }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            { if (!shift) LoadUserControlled(); }
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            { if (!shift) LoadMouseControlled(); }

            if (Input.GetKeyDown(KeyCode.R)) ResetPlayerScript();

            // Quality
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (SettingsBridge.QualityLevel + 1) % 4;
                SettingsBridge.SetQualityLevel(next);
                QualityBridge.SetTier((QualityTier)next);
            }

            // Font size
            if (Input.GetKeyDown(KeyCode.RightBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f);
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f);

            // Audio
            if (Input.GetKeyDown(KeyCode.F5))
                SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F6))
                SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F7))
                SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume + (shift ? -0.1f : 0.1f));
        }

        // ═══════════════════════════════════════════════════════════════
        // SCRIPT/AI ACTIONS
        // ═══════════════════════════════════════════════════════════════

        private void ResetPlayerScript()
        {
            if (_playerProgram == null) return;
            _playerProgram.UploadCode(null);
            _playerScriptTier = null;
            _playerInputMode = null;
        }

        private void LoadPlayerSample(AIDifficulty diff)
        {
            if (_playerProgram == null) return;
            _playerProgram.UploadCode(PongAIController.GetSampleCode(diff));
            _playerScriptTier = diff;
            _playerInputMode = null;
        }

        private void LoadUserControlled()
        {
            if (_playerProgram == null) return;
            _playerProgram.UploadCode(PaddleProgram.USER_CONTROLLED_CODE);
            _playerScriptTier = null;
            _playerInputMode = "keyboard";
        }

        private void LoadMouseControlled()
        {
            if (_playerProgram == null) return;
            _playerProgram.UploadCode(PaddleProgram.MOUSE_CONTROLLED_CODE);
            _playerScriptTier = null;
            _playerInputMode = "mouse";
        }

        private void SetAIDifficulty(AIDifficulty diff)
        {
            if (_ai == null) return;
            _ai.SetDifficulty(diff);
        }

        // ═══════════════════════════════════════════════════════════════
        // AUDIO SLIDERS
        // ═══════════════════════════════════════════════════════════════

        private void CreateAudioSliders()
        {
            if (_colPositions == null || rows.Count <= 3) return;

            const int barOffset = 6;
            const int barWidth = 12;
            int sliderStart = _colPositions[COL_COUNT - 1] + barOffset;

            if (_masterSlider != null)
            {
                rows[1].RepositionSliderOverlay(sliderStart, barWidth);
                rows[2].RepositionSliderOverlay(sliderStart, barWidth);
                rows[3].RepositionSliderOverlay(sliderStart, barWidth);
                return;
            }

            _masterSlider = rows[1].CreateSliderOverlay(sliderStart, barWidth);
            _masterSlider.SetValueWithoutNotify(SettingsBridge.MasterVolume);
            _masterSlider.onValueChanged.AddListener(v => SettingsBridge.SetMasterVolume(v));

            _musicSlider = rows[2].CreateSliderOverlay(sliderStart, barWidth);
            _musicSlider.SetValueWithoutNotify(SettingsBridge.MusicVolume);
            _musicSlider.onValueChanged.AddListener(v => SettingsBridge.SetMusicVolume(v));

            _sfxSlider = rows[3].CreateSliderOverlay(sliderStart, barWidth);
            _sfxSlider.SetValueWithoutNotify(SettingsBridge.SfxVolume);
            _sfxSlider.onValueChanged.AddListener(v => SettingsBridge.SetSfxVolume(v));
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON OVERLAYS
        // ═══════════════════════════════════════════════════════════════

        private void CreateOrRepositionButtons()
        {
            // Script load buttons in col 0 (rows 6-11), AI buttons in col 1 (rows 6-9)
            int btnWidth = Mathf.Max(6, ColWidth(0) - 2);
            int rBtnWidth = Mathf.Max(6, ColWidth(1) - 2);
            int col1Start = _colPositions[1];

            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };

            if (_buttonsCreated)
            {
                for (int i = 0; i <= diffs.Length + 1; i++)
                {
                    int r = 6 + i;
                    if (r < rows.Count)
                        rows[r].RepositionButtonOverlays(new[] { 2 }, new[] { btnWidth });
                }
                if (13 < rows.Count)
                    rows[13].RepositionButtonOverlays(new[] { 2 }, new[] { btnWidth });

                for (int i = 0; i < diffs.Length; i++)
                {
                    int r = 6 + i;
                    if (r < rows.Count)
                        rows[r].RepositionButtonOverlays(new[] { col1Start + 2 }, new[] { rBtnWidth });
                }
                return;
            }

            // Col 0: sample load buttons (rows 6-9)
            for (int i = 0; i < diffs.Length; i++)
            {
                int r = 6 + i;
                if (r >= rows.Count) break;
                var diff = diffs[i];
                rows[r].CreateButtonOverlays(
                    new[] { 2 }, new[] { btnWidth },
                    new Action<int>[] { _ => LoadPlayerSample(diff) });
            }
            // Col 0: keyboard (row 10), mouse (row 11)
            if (10 < rows.Count)
                rows[10].CreateButtonOverlays(new[] { 2 }, new[] { btnWidth },
                    new Action<int>[] { _ => LoadUserControlled() });
            if (11 < rows.Count)
                rows[11].CreateButtonOverlays(new[] { 2 }, new[] { btnWidth },
                    new Action<int>[] { _ => LoadMouseControlled() });
            // Col 0: reset (row 13)
            if (13 < rows.Count)
                rows[13].CreateButtonOverlays(new[] { 2 }, new[] { btnWidth },
                    new Action<int>[] { _ => ResetPlayerScript() });

            // Col 1: AI difficulty buttons (rows 6-9)
            for (int i = 0; i < diffs.Length; i++)
            {
                int r = 6 + i;
                if (r >= rows.Count) break;
                var diff = diffs[i];
                rows[r].CreateButtonOverlays(
                    new[] { col1Start + 2 }, new[] { rBtnWidth },
                    new Action<int>[] { _ => SetAIDifficulty(diff) });
            }

            _buttonsCreated = true;
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        private void SetN(int r, string[] texts)
        {
            Row(r)?.SetNPanelTexts(texts);
        }

        protected override void Render()
        {
            ClearAllRows();

            if (!_columnsReady)
            {
                SetRow(0, BuildCollapsedLine());
                return;
            }

            // Row 0 — collapsed header (one text per column, centered)
            Row(0)?.SetNPanelTextsCentered(BuildCollapsedRow());

            if (!IsExpanded) return;

            if (rows.Count > 1) _charPx = rows[1].CharWidth;

            // Sync audio sliders
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(SettingsBridge.MasterVolume);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(SettingsBridge.MusicVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(SettingsBridge.SfxVolume);

            // Build all 7 columns
            var cols = new string[COL_COUNT][];
            cols[0] = BuildScriptColumn();
            cols[1] = BuildAIColumn();
            cols[2] = BuildMatchColumn();
            cols[3] = BuildTitleColumn();
            cols[4] = BuildControlsColumn();
            cols[5] = BuildQualityFontColumn();
            cols[6] = BuildAudioColumn();

            int maxLines = 0;
            foreach (var col in cols)
                if (col.Length > maxLines) maxLines = col.Length;

            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 1;
                if (r >= totalRows) break;
                var texts = new string[COL_COUNT];
                for (int c = 0; c < COL_COUNT; c++)
                    texts[c] = i < cols[c].Length ? cols[c][i] : "";
                SetN(r, texts);
            }
        }

        private string BuildCollapsedLine()
        {
            if (_match == null) return $" {TUIColors.Bold("PONG")}";
            string you = TUIColors.Fg(TUIColors.BrightCyan, $"YOU:{_match.LeftScore}");
            string them = TUIColors.Fg(TUIColors.BrightMagenta, $"AI:{_match.RightScore}");
            return $" {TUIColors.Bold("PONG")}  {you} {TUIGlyphs.BoxH}{TUIGlyphs.BoxH} {them}";
        }

        private string[] BuildCollapsedRow()
        {
            var t = new string[COL_COUNT];

            // Static labels (shown by default)
            string[] labels = {
                " YOU", "OPPONENT", " MATCH",
                $" {TUIColors.Bold("PONG")}", " CONTROLS", " QUALITY", " AUDIO"
            };

            // Dynamic info (shown on hover)
            string[] dynamic = new string[COL_COUNT];
            // Col 0: score
            if (_match != null)
                dynamic[0] = $" {TUIColors.Fg(TUIColors.BrightCyan, $"YOU:{_match.LeftScore}")}";
            else
                dynamic[0] = labels[0];
            // Col 1: AI info
            if (_ai != null && _match != null)
                dynamic[1] = $" {TUIColors.Fg(TUIColors.BrightMagenta, $"AI({_ai.Difficulty}):{_match.RightScore}")}";
            else
                dynamic[1] = labels[1];
            // Col 2: match record
            if (_match != null)
                dynamic[2] = $" M:{_match.MatchesPlayed} W:{_match.PlayerWins}";
            else
                dynamic[2] = labels[2];
            // Col 3: title — PING on hover to contrast PONG
            dynamic[3] = $" {TUIColors.Bold("PING")}";
            // Col 4: speed
            var sim = SimulationTime.Instance;
            if (sim != null)
            {
                string paused = sim.isPaused ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED") : "";
                dynamic[4] = $" {sim.GetFormattedTimeScale()}{paused}";
            }
            else
                dynamic[4] = labels[4];
            // Col 5: quality
            dynamic[5] = $" {((QualityTier)SettingsBridge.QualityLevel)}";
            // Col 6: volume
            dynamic[6] = $" Vol:{SettingsBridge.MasterVolume * 100:F0}%";

            for (int i = 0; i < COL_COUNT; i++)
                t[i] = IsColumnHovered(i) ? (dynamic[i] ?? labels[i]) : labels[i];

            return t;
        }

        // ── Column 0: YOUR SCRIPT ───────────────────────────────

        private string[] BuildScriptColumn()
        {
            var lines = new List<string>();

            if (_playerProgram != null)
            {
                string name = _playerProgram.ProgramName ?? "PaddleAI";
                int inst = _playerProgram.Program?.Instructions?.Length ?? 0;
                string status = _playerProgram.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STOP");
                string tier = _playerInputMode == "keyboard"
                    ? TUIColors.Fg(TUIColors.BrightYellow, "(Keyboard)")
                    : _playerInputMode == "mouse"
                    ? TUIColors.Fg(TUIColors.BrightYellow, "(Mouse)")
                    : _playerScriptTier.HasValue
                    ? TUIColors.Fg(TUIColors.BrightMagenta, $"({_playerScriptTier.Value})")
                    : TUIColors.Dimmed("(custom)");
                lines.Add($"  {name}");
                lines.Add($"  {status} {tier}");
                lines.Add($"  {TUIColors.Dimmed($"{inst} inst")}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
                lines.Add("");
                lines.Add("");
            }

            lines.Add("");
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("Load sample:")}");
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _playerScriptTier.HasValue && _playerScriptTier.Value == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                lines.Add($"  {key} {label}");
            }
            {
                string key = TUIColors.Fg(TUIColors.BrightCyan, "[5]");
                string label = _playerInputMode == "keyboard"
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"Keyboard{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed("Keyboard");
                lines.Add($"  {key} {label}");
            }
            {
                string key = TUIColors.Fg(TUIColors.BrightCyan, "[6]");
                string label = _playerInputMode == "mouse"
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"Mouse{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed("Mouse");
                lines.Add($"  {key} {label}");
            }

            return lines.ToArray();
        }

        // ── Column 1: AI OPPONENT ───────────────────────────────

        private string[] BuildAIColumn()
        {
            var lines = new List<string>();

            if (_ai != null && _ai.Program != null)
            {
                string name = _ai.Program.ProgramName ?? "AI";
                int inst = _ai.Program.Program?.Instructions?.Length ?? 0;
                string status = _ai.Program.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STOP");
                string diff = TUIColors.Fg(TUIColors.BrightMagenta, $"({_ai.Difficulty})");
                lines.Add($"  {name}");
                lines.Add($"  {status} {diff}");
                lines.Add($"  {TUIColors.Dimmed($"{inst} inst")}");
            }
            else
            {
                string aiDiff = _ai != null ? _ai.Difficulty.ToString() : "?";
                lines.Add($"  {TUIColors.Dimmed("AI")}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightYellow, aiDiff)}");
                lines.Add("");
            }

            lines.Add("");
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("Set AI:")}");
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _ai != null && _ai.Difficulty == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[S+{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                lines.Add($"  {key} {label}");
            }

            return lines.ToArray();
        }

        // ── Column 3: TITLE (ASCII art, centered) ──────────────

        private string[] BuildTitleColumn()
        {
            var art = BuildAsciiArt();
            int artWidth = CodeRows[0].Length + 2; // inner + border chars
            int colW = ColWidth(3);
            int pad = Mathf.Max(0, (colW - artWidth) / 2);
            if (pad > 0)
            {
                // Wrap padding in mspace so it matches the monospaced art
                string spaces = Mono(new string(' ', pad));
                for (int i = 0; i < art.Length; i++)
                    if (!string.IsNullOrEmpty(art[i]))
                        art[i] = spaces + art[i];
            }
            return art;
        }

        // ── Column 2: MATCH ─────────────────────────────────────

        private string[] BuildMatchColumn()
        {
            var lines = new List<string>();

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

        // ── Column 4: CONTROLS ──────────────────────────────────

        private string[] BuildControlsColumn()
        {
            var lines = new List<string>();
            var sim = SimulationTime.Instance;
            string speed = sim != null ? sim.GetFormattedTimeScale() : "1x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED")
                : "";

            lines.Add($"  Speed: {TUIColors.Fg(TUIColors.BrightGreen, speed)}{paused}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[1-6]")}   Sample");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[S+1-4]")} AI diff");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[R]")}     Reset");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[+/-]")}   Speed");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[SPACE]")} Pause");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[W]")}     Warp");
            return lines.ToArray();
        }

        // ── Column 5: QUALITY / FONT ─────────────────────────────

        private string[] BuildQualityFontColumn()
        {
            var lines = new List<string>();

            string tierName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, tierName)}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[Q]")} Cycle");
            lines.Add("");
            lines.Add($" {TUIColors.Bold("DISPLAY")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"{SettingsBridge.FontSize:F0}pt")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[]]")} Font+");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[[]")} Font-");
            return lines.ToArray();
        }

        // ── Column 6: AUDIO ──────────────────────────────────────

        private string[] BuildAudioColumn()
        {
            var lines = new List<string>();

            lines.Add($"  {VolumeBar("Mst", SettingsBridge.MasterVolume)}");
            lines.Add($"  {VolumeBar("Mus", SettingsBridge.MusicVolume)}");
            lines.Add($"  {VolumeBar("SFX", SettingsBridge.SfxVolume)}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F5]")} Mst");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F6]")} Mus");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F7]")} SFX");
            lines.Add($"  {TUIColors.Dimmed("S+key = -")}");
            return lines.ToArray();
        }

        private static string VolumeBar(string label, float vol)
        {
            int filled = Mathf.RoundToInt(vol * 10);
            string bar = new string('█', filled) + new string('░', 10 - filled);
            string pct = $"{vol * 100:F0}%";
            return $"{label} [{TUIColors.Fg(TUIColors.BrightGreen, bar)}] {TUIColors.Dimmed(pct)}";
        }

        // ═══════════════════════════════════════════════════════════════
        // ASCII ART ENGINE (from PongStatusCenter)
        // ═══════════════════════════════════════════════════════════════

        private void AdvanceAsciiTimer()
        {
            _asciiTimer += Time.deltaTime;
            switch (_asciiPhase)
            {
                case 0: case 2:
                    if (_asciiTimer >= AsciiHold) { _asciiTimer = 0f; _asciiPhase = (_asciiPhase + 1) % 4; InitRevealThresholds(); }
                    break;
                case 1: case 3:
                    if (_asciiTimer >= AsciiAnim) { _asciiTimer = 0f; _asciiPhase = (_asciiPhase + 1) % 4; }
                    break;
            }
        }

        private void InitRevealThresholds()
        {
            int innerW = CodeRows[0].Length;
            int total = innerW * 10;
            _revealThresholds = new float[total];
            for (int i = 0; i < total; i++) _revealThresholds[i] = UnityEngine.Random.value;
        }

        private string[] BuildAsciiArt()
        {
            return _asciiPhase switch
            {
                0 => ColorizeBlock(CodeRows, GameRows),
                2 => ColorizeBlock(PingRows, PongRows),
                1 => DecipherBlock(CodeRows, GameRows, PingRows, PongRows),
                3 => DecipherBlock(PingRows, PongRows, CodeRows, GameRows),
                _ => new string[15],
            };
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
            int innerW = top[0].Length;
            int totalW = innerW + 2;
            var lines = new string[15];
            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int i = 0; i < 5; i++)
                lines[2 + i] = GradientBorderV(GradientRowRaw(top[i], totalW));
            lines[7] = GradientBorderV(new string(' ', innerW));
            for (int i = 0; i < 5; i++)
                lines[8 + i] = GradientBorderV(GradientRowRaw(bot[i], totalW));
            lines[13] = GradientBorderV(new string(' ', innerW));
            lines[14] = GradientBorderH('╚', '═', '╝', innerW);
            return lines;
        }

        private string[] DecipherBlock(string[] srcTop, string[] srcBot,
                                       string[] tgtTop, string[] tgtBot)
        {
            float progress = Mathf.Clamp01(_asciiTimer / AsciiAnim);
            int innerW = tgtTop[0].Length;
            int totalW = innerW + 2;
            var lines = new string[15];

            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int r = 0; r < 5; r++)
                lines[2 + r] = GradientBorderV(
                    DecipherRowRaw(srcTop[r], tgtTop[r], progress, r * innerW, totalW));
            lines[7] = GradientBorderV(new string(' ', innerW));
            for (int r = 0; r < 5; r++)
                lines[8 + r] = GradientBorderV(
                    DecipherRowRaw(srcBot[r], tgtBot[r], progress, (5 + r) * innerW, totalW));
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

                if (srcCh == tgtCh) { sb.Append(TUIColors.Fg(GradientAt(t), tgtCh.ToString())); continue; }

                int idx = threshOffset + i;
                bool isSettled = _revealThresholds != null && idx < _revealThresholds.Length
                    && progress >= _revealThresholds[idx];
                char ch;
                if (isSettled) ch = tgtCh;
                else
                {
                    bool hasContent = srcCh != ' ' || tgtCh != ' ';
                    ch = hasContent ? GlitchGlyphs[UnityEngine.Random.Range(0, GlitchGlyphs.Length)] : ' ';
                }
                sb.Append(TUIColors.Fg(GradientAt(t), ch.ToString()));
            }
            return sb.ToString();
        }
    }
}
