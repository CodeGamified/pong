// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CodeGamified.Audio;
using CodeGamified.TUI;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using UnityEngine.SceneManagement;
using Pong.Game;
using Pong.AI;
using Pong.Core;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// Unified status panel — 7 columns with draggable dividers:
    ///   YOU │ SETTINGS │ MATCH │ PONG │ CONTROLS │ AUDIO │ OPPONENT
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
        private Equalizer _equalizer;

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

        // ── Controls / Quality slider overlays ──────────────────
        private Slider _speedSlider;
        private Slider _qualitySlider;
        private Slider _fontSlider;

        // ── Bootstrap parameter sliders ─────────────────────────
        private Slider _courtWSlider;
        private Slider _courtHSlider;
        private Slider _paddleHSlider;
        private Slider _ballRadSlider;
        private Slider _ballSpdSlider;
        private Slider _spdIncSlider;
        private Slider _bounceSlider;
        private PongBootstrap _bootstrap;

        // ── ASCII art animation ─────────────────────────────────
        private float _asciiTimer;
        private int _asciiPhase;
        private float[] _revealThresholds;
        private const float AsciiHold = 5f;
        private const float AsciiAnim = 1f;
        private const int AsciiWordCount = 4;
        private const int MaxStatusRows = 10;
        private static readonly char[] GlitchGlyphs =
            "░▒▓█▀▄▌▐╬╫╪╩╦╠╣─│┌┐└┘├┤┬┴┼".ToCharArray();

        private static readonly string[][] AsciiWords =
        {
            new[] // CODE
            {
                "   █████████  ████████  █████████   █████████  ",
                "  ██         ██      ██ ██      ██ ██          ",
                "  ██         ██      ██ ██      ██ ██████████  ",
                "  ██         ██      ██ ██      ██ ██          ",
                "   █████████  ████████  █████████   █████████  ",
            },
            new[] // GAME
            {
                "   █████████  ████████   ████████   █████████  ",
                "  ██         ██      ██ ██  ██  ██ ██          ",
                "  ██   █████ ██████████ ██  ██  ██ ██████████  ",
                "  ██      ██ ██      ██ ██  ██  ██ ██          ",
                "   █████████ ██      ██ ██  ██  ██  █████████  ",
            },
            new[] // PING
            {
                "  █████████  ██████████ ██      ██  █████████  ",
                "  ██      ██     ██     ████    ██ ██          ",
                "  █████████      ██     ██  ██  ██ ██   █████  ",
                "  ██             ██     ██    ████ ██      ██  ",
                "  ██         ██████████ ██      ██  █████████  ",
            },
            new[] // PONG
            {
                "  █████████   ████████  ██      ██  █████████  ",
                "  ██      ██ ██      ██ ████    ██ ██          ",
                "  █████████  ██      ██ ██  ██  ██ ██   █████  ",
                "  ██         ██      ██ ██    ████ ██      ██  ",
                "  ██          ████████  ██      ██  █████████  ",
            },
        };

        private bool IsExpanded => totalRows > 1;

        // ── Button overlays ─────────────────────────────────────
        private bool _buttonsCreated;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "PONG";
            totalRows = MaxStatusRows;
        }

        public void Bind(PongMatchManager match, PaddleProgram playerProgram, PongAIController ai)
        {
            _match = match;
            _playerProgram = playerProgram;
            _ai = ai;
        }

        /// <summary>Bind an equalizer for the audio column EQ visualization.</summary>
        public void BindEqualizer(Equalizer equalizer) => _equalizer = equalizer;

        protected override void OnLayoutReady()
        {
            ClampPanelHeight();
            var rt = GetComponent<RectTransform>();
            if (rt == null || rows.Count == 0) return;
            float h = rt.rect.height;
            float rowH = rows[0].RowHeight;
            if (rowH <= 0) return;
            int fitRows = Mathf.Clamp(Mathf.FloorToInt(h / rowH), 2, MaxStatusRows);
            if (fitRows != totalRows)
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(i < fitRows);
                totalRows = fitRows;
            }
            SetupColumns();
        }

        /// <summary>Prevent the panel from being dragged taller than MaxStatusRows.</summary>
        private void ClampPanelHeight()
        {
            if (rows.Count == 0) return;
            float rowH = rows[0].RowHeight;
            if (rowH <= 0) return;
            var rt = GetComponent<RectTransform>();
            if (rt == null || rt.parent == null) return;

            float maxH = MaxStatusRows * rowH;
            float canvasH = ((RectTransform)rt.parent).rect.height;
            if (canvasH <= 0) return;

            float maxAnchorSpan = maxH / canvasH;
            float currentSpan = rt.anchorMax.y - rt.anchorMin.y;
            if (currentSpan > maxAnchorSpan)
            {
                // Panel anchored to bottom (anchorMin.y = 0), so push top down
                float clampedTop = rt.anchorMin.y + maxAnchorSpan;
                var aMax = rt.anchorMax;
                aMax.y = clampedTop;
                rt.anchorMax = aMax;

                // Push linked sibling panels (debuggers) so their bottom matches our top
                foreach (RectTransform sibling in rt.parent)
                {
                    if (sibling == rt) continue;
                    if (Mathf.Abs(sibling.anchorMin.y - currentSpan) < 0.01f ||
                        sibling.anchorMin.y < clampedTop)
                    {
                        // Only adjust panels that sit directly above us
                        if (sibling.anchorMin.y < clampedTop && sibling.anchorMax.y > clampedTop)
                        {
                            var sMin = sibling.anchorMin;
                            sMin.y = clampedTop;
                            sibling.anchorMin = sMin;
                        }
                    }
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            // Enforce cap after base resize logic
            if (totalRows > MaxStatusRows)
            {
                for (int i = MaxStatusRows; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(false);
                totalRows = MaxStatusRows;
            }
            ClampPanelHeight();
            if (!rowsReady) return;
            _equalizer?.Update(UnityEngine.Time.deltaTime);
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
            CreateControlsSliders();
            CreateQualityFontSliders();
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

            // Reposition all sliders — any boundary change can shift columns
            CreateQualityFontSliders();
            CreateControlsSliders();
            CreateAudioSliders();

            CreateOrRepositionButtons();
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

            if (Input.GetKeyDown(KeyCode.R)) ReloadScene();
            if (Input.GetKeyDown(KeyCode.D))
            {
                PongBootstrap.ClearOverrides();
                SimulationTime.Instance?.SetTimeScale(1f);
                SettingsBridge.SetQualityLevel(3); QualityBridge.SetTier(QualityTier.Ultra);
                SettingsBridge.SetFontSize(20f);
                SettingsBridge.SetMasterVolume(0.5f);
                SettingsBridge.SetMusicVolume(0.25f);
                SettingsBridge.SetSfxVolume(0.75f);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

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

            const int barOffset = 8;
            const int barRightPad = 10;
            int barWidth = Mathf.Max(0, ColWidth(5) - barOffset - barRightPad);
            int sliderStart = _colPositions[5] + barOffset;
            bool slidersVisible = barWidth >= 4;

            if (_masterSlider != null)
            {
                _masterSlider.gameObject.SetActive(slidersVisible);
                _musicSlider.gameObject.SetActive(slidersVisible);
                _sfxSlider.gameObject.SetActive(slidersVisible);
                if (slidersVisible)
                {
                    rows[1].RepositionSliderOverlay(sliderStart, barWidth);
                    rows[2].RepositionSliderOverlay(sliderStart, barWidth);
                    rows[3].RepositionSliderOverlay(sliderStart, barWidth);
                }
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
        // CONTROLS / QUALITY SLIDERS
        // ═══════════════════════════════════════════════════════════════

        private void CreateControlsSliders()
        {
            if (_colPositions == null || rows.Count <= 1) return;
            if (_bootstrap == null)
                _bootstrap = FindFirstObjectByType<PongBootstrap>();

            const int barOffset = 8;
            const int barRightPad = 10;
            int barWidth = Mathf.Max(0, ColWidth(4) - barOffset - barRightPad);
            int sliderStart = _colPositions[4] + barOffset;
            bool slidersVisible = barWidth >= 10;

            if (_speedSlider != null)
            {
                _speedSlider.gameObject.SetActive(slidersVisible);
                SetRawSliderVisible(_ballSpdSlider, slidersVisible);
                SetRawSliderVisible(_spdIncSlider, slidersVisible);
                SetRawSliderVisible(_bounceSlider, slidersVisible);
                if (slidersVisible)
                {
                    RepositionRawSlider(_speedSlider, rows[1].CharWidth, sliderStart, barWidth);
                    if (_ballSpdSlider != null && 2 < rows.Count)
                        RepositionRawSlider(_ballSpdSlider, rows[2].CharWidth, sliderStart, barWidth);
                    if (_spdIncSlider != null && 3 < rows.Count)
                        RepositionRawSlider(_spdIncSlider, rows[3].CharWidth, sliderStart, barWidth);
                    if (_bounceSlider != null && 4 < rows.Count)
                        RepositionRawSlider(_bounceSlider, rows[4].CharWidth, sliderStart, barWidth);
                }
                return;
            }

            _speedSlider = CreateRawSlider(1, sliderStart, barWidth);
            var sim = SimulationTime.Instance;
            float initialSpeed = sim != null ? sim.timeScale : 1f;
            _speedSlider.SetValueWithoutNotify(SpeedToSlider(initialSpeed));
            _speedSlider.onValueChanged.AddListener(v =>
            {
                SimulationTime.Instance?.SetTimeScale(SliderToSpeed(v));
            });

            if (_bootstrap == null) return;

            _ballSpdSlider = CreateRawSlider(2, sliderStart, barWidth);
            _ballSpdSlider.SetValueWithoutNotify((_bootstrap.ballStartSpeed - 0.5f) / 9.5f);
            _ballSpdSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.ballStartSpeed = 0.5f + v * 9.5f; });

            _spdIncSlider = CreateRawSlider(3, sliderStart, barWidth);
            _spdIncSlider.SetValueWithoutNotify(_bootstrap.ballSpeedIncrease / 2f);
            _spdIncSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.ballSpeedIncrease = v * 2f; });

            _bounceSlider = CreateRawSlider(4, sliderStart, barWidth);
            _bounceSlider.SetValueWithoutNotify((_bootstrap.maxBounceAngle - 15f) / 70f);
            _bounceSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.maxBounceAngle = 15f + v * 70f; });
        }

        private void CreateQualityFontSliders()
        {
            if (_colPositions == null || rows.Count <= 2) return;
            if (_bootstrap == null)
                _bootstrap = FindFirstObjectByType<PongBootstrap>();

            const int barOffset = 8;
            const int barRightPad = 10;
            int barWidth = Mathf.Max(0, ColWidth(1) - barOffset - barRightPad);
            int sliderStart = _colPositions[1] + barOffset;
            bool slidersVisible = barWidth >= 10;

            if (_qualitySlider != null)
            {
                SetRawSliderVisible(_qualitySlider, slidersVisible);
                SetRawSliderVisible(_fontSlider, slidersVisible);
                if (slidersVisible)
                {
                    RepositionRawSlider(_qualitySlider, rows[1].CharWidth, sliderStart, barWidth);
                    RepositionRawSlider(_fontSlider, rows[2].CharWidth, sliderStart, barWidth);
                }
                for (int r = 3; r <= 6; r++)
                {
                    Slider s = r switch { 3 => _courtWSlider, 4 => _courtHSlider, 5 => _paddleHSlider,
                        _ => _ballRadSlider };
                    if (s != null && r < rows.Count)
                    {
                        SetRawSliderVisible(s, slidersVisible);
                        if (slidersVisible)
                            RepositionRawSlider(s, rows[r].CharWidth, sliderStart, barWidth);
                    }
                }
                return;
            }

            _qualitySlider = CreateRawSlider(1, sliderStart, barWidth);
            _qualitySlider.SetValueWithoutNotify(SettingsBridge.QualityLevel / 3f);
            _qualitySlider.onValueChanged.AddListener(v =>
            {
                int level = Mathf.RoundToInt(v * 3f);
                SettingsBridge.SetQualityLevel(level);
                QualityBridge.SetTier((QualityTier)level);
            });

            _fontSlider = CreateRawSlider(2, sliderStart, barWidth);
            _fontSlider.SetValueWithoutNotify(FontToSlider(SettingsBridge.FontSize));
            _fontSlider.onValueChanged.AddListener(v =>
            {
                SettingsBridge.SetFontSize(SliderToFont(v));
            });

            // Bootstrap parameter sliders (rows 3-6 in col 1)
            if (_bootstrap == null) return;

            _courtWSlider = CreateRawSlider(3, sliderStart, barWidth);
            _courtWSlider.SetValueWithoutNotify((_bootstrap.courtWidth - 8f) / 24f);
            _courtWSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.courtWidth = 8f + v * 24f; });

            _courtHSlider = CreateRawSlider(4, sliderStart, barWidth);
            _courtHSlider.SetValueWithoutNotify((_bootstrap.courtHeight - 5f) / 15f);
            _courtHSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.courtHeight = 5f + v * 15f; });

            _paddleHSlider = CreateRawSlider(5, sliderStart, barWidth);
            _paddleHSlider.SetValueWithoutNotify((_bootstrap.paddleHeight - 0.5f) / 4.5f);
            _paddleHSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.paddleHeight = 0.5f + v * 4.5f; });

            _ballRadSlider = CreateRawSlider(6, sliderStart, barWidth);
            _ballRadSlider.SetValueWithoutNotify((_bootstrap.ballRadius - 0.1f) / 0.9f);
            _ballRadSlider.onValueChanged.AddListener(v => { if (_bootstrap != null) _bootstrap.ballRadius = 0.1f + v * 0.9f; });
        }

        // ── Raw slider helpers (multiple sliders per row) ───────

        private Slider CreateRawSlider(int rowIndex, int startChar, int widthChars)
        {
            if (rowIndex >= rows.Count) return null;
            var row = rows[rowIndex];
            float cw = row.CharWidth;

            var sliderGO = new GameObject("ExtraSlider");
            sliderGO.transform.SetParent(row.transform, false);

            var rect = sliderGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(startChar * cw, 0);
            rect.sizeDelta = new Vector2(widthChars * cw, 0);

            var bgImg = sliderGO.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.01f);

            var s = sliderGO.AddComponent<Slider>();
            s.minValue = 0f;
            s.maxValue = 1f;
            s.wholeNumbers = false;
            s.direction = Slider.Direction.LeftToRight;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1, 0.8f);
            fillAreaRect.offsetMin = new Vector2(2, 0);
            fillAreaRect.offsetMax = new Vector2(-2, 0);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.7f, 0.4f, 0.01f);
            fillImg.raycastTarget = false;
            s.fillRect = fillRect;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(5, 0);
            handleAreaRect.offsetMax = new Vector2(-5, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(10, 0);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(1, 1, 1, 0.01f);

            s.targetGraphic = handleImg;
            s.handleRect = handleRect;

            return s;
        }

        private static void RepositionRawSlider(Slider s, float charWidth, int startChar, int widthChars)
        {
            if (s == null) return;
            var rect = s.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchoredPosition = new Vector2(startChar * charWidth, 0);
            rect.sizeDelta = new Vector2(widthChars * charWidth, 0);
        }

        private static void SetRawSliderVisible(Slider s, bool visible)
        {
            if (s != null) s.gameObject.SetActive(visible);
        }

        // ── Speed conversion (logarithmic 0.1x–100x) ───────────

        private static float SpeedToSlider(float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 100f);
            return Mathf.Log10(speed * 10f) / 3f;
        }

        private static float SliderToSpeed(float slider)
        {
            return 0.1f * Mathf.Pow(1000f, Mathf.Clamp01(slider));
        }

        // ── Font conversion (8pt–48pt) ──────────────────────────

        private static float FontToSlider(float fontSize)
        {
            return Mathf.Clamp01((fontSize - 8f) / 40f);
        }

        private static float SliderToFont(float slider)
        {
            return 8f + Mathf.Clamp01(slider) * 40f;
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON OVERLAYS
        // ═══════════════════════════════════════════════════════════════

        private void CreateOrRepositionButtons()
        {
            if (_colPositions == null || !IsExpanded) return;

            const int pad = 2;
            const int btnW = 3;
            int c1 = _colPositions[1];
            int c4 = _colPositions[4];
            int c5 = _colPositions[5];
            int c6 = _colPositions[6];
            int w0 = Mathf.Max(4, ColWidth(0) - 2);
            int w1 = Mathf.Max(4, ColWidth(1) - 2);
            int w4 = Mathf.Max(4, ColWidth(4) - 2);
            int w6 = Mathf.Max(4, ColWidth(6) - 2);

            // [-] at colStart + 1; [+] at 3 chars from column end (matches AdaptiveSliderRow)
            int cw1 = ColWidth(1), cw4 = ColWidth(4), cw5 = ColWidth(5);

            int spdMinus   = c4 + 1;
            int spdPlusOff = Mathf.Max(5, cw4 - 3);
            int spdPlus    = c4 + spdPlusOff;
            int spdPlusW   = Mathf.Max(btnW, cw4 - spdPlusOff);

            int qualMinus  = c1 + 1;
            int qualPlusOff = Mathf.Max(5, cw1 - 3);
            int qualPlus   = c1 + qualPlusOff;
            int qualPlusW  = Mathf.Max(btnW, cw1 - qualPlusOff);

            int fontMinus  = c1 + 1;
            int fontPlus   = qualPlus;
            int fontPlusW  = qualPlusW;

            int audioMinus = c5 + 1;
            int audPlusOff = Mathf.Max(5, cw5 - 3);
            int audioPlus  = c5 + audPlusOff;
            int audioPlusW = Mathf.Max(btnW, cw5 - audPlusOff);

            int setMinus = c1 + 1;        int setPlus = qualPlus;
            int setPlusW = qualPlusW;

            // Step lambdas for bootstrap params
            Action<int> cwDec = _ => { if (_bootstrap != null) _bootstrap.courtWidth = Mathf.Clamp(_bootstrap.courtWidth - 1f, 8f, 32f); };
            Action<int> cwInc = _ => { if (_bootstrap != null) _bootstrap.courtWidth = Mathf.Clamp(_bootstrap.courtWidth + 1f, 8f, 32f); };
            Action<int> chDec = _ => { if (_bootstrap != null) _bootstrap.courtHeight = Mathf.Clamp(_bootstrap.courtHeight - 1f, 5f, 20f); };
            Action<int> chInc = _ => { if (_bootstrap != null) _bootstrap.courtHeight = Mathf.Clamp(_bootstrap.courtHeight + 1f, 5f, 20f); };
            Action<int> pdDec = _ => { if (_bootstrap != null) _bootstrap.paddleHeight = Mathf.Clamp(_bootstrap.paddleHeight - 0.25f, 0.5f, 5f); };
            Action<int> pdInc = _ => { if (_bootstrap != null) _bootstrap.paddleHeight = Mathf.Clamp(_bootstrap.paddleHeight + 0.25f, 0.5f, 5f); };
            Action<int> rdDec = _ => { if (_bootstrap != null) _bootstrap.ballRadius = Mathf.Clamp(_bootstrap.ballRadius - 0.05f, 0.1f, 1f); };
            Action<int> rdInc = _ => { if (_bootstrap != null) _bootstrap.ballRadius = Mathf.Clamp(_bootstrap.ballRadius + 0.05f, 0.1f, 1f); };
            Action<int> bsDec = _ => { if (_bootstrap != null) _bootstrap.ballStartSpeed = Mathf.Clamp(_bootstrap.ballStartSpeed - 0.5f, 0.5f, 10f); };
            Action<int> bsInc = _ => { if (_bootstrap != null) _bootstrap.ballStartSpeed = Mathf.Clamp(_bootstrap.ballStartSpeed + 0.5f, 0.5f, 10f); };
            Action<int> siDec = _ => { if (_bootstrap != null) _bootstrap.ballSpeedIncrease = Mathf.Clamp(_bootstrap.ballSpeedIncrease - 0.05f, 0f, 2f); };
            Action<int> siInc = _ => { if (_bootstrap != null) _bootstrap.ballSpeedIncrease = Mathf.Clamp(_bootstrap.ballSpeedIncrease + 0.05f, 0f, 2f); };
            Action<int> anDec = _ => { if (_bootstrap != null) _bootstrap.maxBounceAngle = Mathf.Clamp(_bootstrap.maxBounceAngle - 5f, 15f, 85f); };
            Action<int> anInc = _ => { if (_bootstrap != null) _bootstrap.maxBounceAngle = Mathf.Clamp(_bootstrap.maxBounceAngle + 5f, 15f, 85f); };

            if (_buttonsCreated)
            {
                // ── Reposition all button overlays ──
                if (1 < rows.Count)
                    rows[1].RepositionButtonOverlays(
                        new[] { spdMinus, spdPlus, qualMinus, qualPlus, audioMinus, audioPlus },
                        new[] { btnW, spdPlusW, btnW, qualPlusW, btnW, audioPlusW });
                if (2 < rows.Count)
                    rows[2].RepositionButtonOverlays(
                        new[] { c4 + pad, fontMinus, fontPlus, audioMinus, audioPlus },
                        new[] { w4, btnW, fontPlusW, btnW, audioPlusW });
                if (3 < rows.Count)
                    rows[3].RepositionButtonOverlays(
                        new[] { pad, setMinus, setPlus, audioMinus, audioPlus, c6 + pad },
                        new[] { w0, btnW, setPlusW, btnW, audioPlusW, w6 });
                if (4 < rows.Count)
                    rows[4].RepositionButtonOverlays(
                        new[] { pad, setMinus, setPlus, c6 + pad },
                        new[] { w0, btnW, setPlusW, w6 });
                if (5 < rows.Count)
                    rows[5].RepositionButtonOverlays(
                        new[] { pad, setMinus, setPlus, spdMinus, spdPlus, c6 + pad },
                        new[] { w0, btnW, setPlusW, btnW, spdPlusW, w6 });
                if (6 < rows.Count)
                    rows[6].RepositionButtonOverlays(
                        new[] { pad, setMinus, setPlus, spdMinus, spdPlus, c6 + pad },
                        new[] { w0, btnW, setPlusW, btnW, spdPlusW, w6 });
                if (7 < rows.Count)
                    rows[7].RepositionButtonOverlays(
                        new[] { pad, spdMinus, spdPlus },
                        new[] { w0, btnW, spdPlusW });
                if (8 < rows.Count)
                    rows[8].RepositionButtonOverlays(
                        new[] { pad, c1 + pad },
                        new[] { w0, w1 });
                return;
            }

            // ── Row 1: SPD [-][+] + QTY [-][+] + VOL [-][+] ──
            if (1 < rows.Count)
                rows[1].CreateButtonOverlays(
                    new[] { spdMinus, spdPlus, qualMinus, qualPlus, audioMinus, audioPlus },
                    new[] { btnW, spdPlusW, btnW, qualPlusW, btnW, audioPlusW },
                    new Action<int>[] {
                        _ => { var s = SimulationTime.Instance; if (s != null) s.SetTimeScale(SliderToSpeed(Mathf.Clamp01(SpeedToSlider(s.timeScale) - 0.1f))); },
                        _ => { var s = SimulationTime.Instance; if (s != null) s.SetTimeScale(SliderToSpeed(Mathf.Clamp01(SpeedToSlider(s.timeScale) + 0.1f))); },
                        _ => { int lv = Mathf.Max(0, SettingsBridge.QualityLevel - 1); SettingsBridge.SetQualityLevel(lv); QualityBridge.SetTier((QualityTier)lv); },
                        _ => { int lv = Mathf.Min(3, SettingsBridge.QualityLevel + 1); SettingsBridge.SetQualityLevel(lv); QualityBridge.SetTier((QualityTier)lv); },
                        _ => SettingsBridge.SetMasterVolume(Mathf.Clamp01(SettingsBridge.MasterVolume - 0.1f)),
                        _ => SettingsBridge.SetMasterVolume(Mathf.Clamp01(SettingsBridge.MasterVolume + 0.1f))
                    });

            // ── Row 2: [P] PAUSE + FNT [-][+] + MSC [-][+] ──
            if (2 < rows.Count)
                rows[2].CreateButtonOverlays(
                    new[] { c4 + pad, fontMinus, fontPlus, audioMinus, audioPlus },
                    new[] { w4, btnW, fontPlusW, btnW, audioPlusW },
                    new Action<int>[] {
                        _ => SimulationTime.Instance?.TogglePause(),
                        _ => SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f),
                        _ => SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f),
                        _ => SettingsBridge.SetMusicVolume(Mathf.Clamp01(SettingsBridge.MusicVolume - 0.1f)),
                        _ => SettingsBridge.SetMusicVolume(Mathf.Clamp01(SettingsBridge.MusicVolume + 0.1f))
                    });

            // ── Row 3: [1] Easy + C.W [-][+] + SFX [-][+] + [S+1] Easy ──
            if (3 < rows.Count)
                rows[3].CreateButtonOverlays(
                    new[] { pad, setMinus, setPlus, audioMinus, audioPlus, c6 + pad },
                    new[] { w0, btnW, setPlusW, btnW, audioPlusW, w6 },
                    new Action<int>[] {
                        _ => LoadPlayerSample(AIDifficulty.Easy),
                        cwDec, cwInc,
                        _ => SettingsBridge.SetSfxVolume(Mathf.Clamp01(SettingsBridge.SfxVolume - 0.1f)),
                        _ => SettingsBridge.SetSfxVolume(Mathf.Clamp01(SettingsBridge.SfxVolume + 0.1f)),
                        _ => SetAIDifficulty(AIDifficulty.Easy)
                    });

            // ── Row 4: [2] Medium + C.H [-][+] + [S+2] Medium ──
            if (4 < rows.Count)
                rows[4].CreateButtonOverlays(
                    new[] { pad, setMinus, setPlus, c6 + pad },
                    new[] { w0, btnW, setPlusW, w6 },
                    new Action<int>[] {
                        _ => LoadPlayerSample(AIDifficulty.Medium),
                        chDec, chInc,
                        _ => SetAIDifficulty(AIDifficulty.Medium)
                    });

            // ── Row 5: [3] Hard + PDL [-][+] + BSP [-][+] + [S+3] Hard ──
            if (5 < rows.Count)
                rows[5].CreateButtonOverlays(
                    new[] { pad, setMinus, setPlus, spdMinus, spdPlus, c6 + pad },
                    new[] { w0, btnW, setPlusW, btnW, spdPlusW, w6 },
                    new Action<int>[] {
                        _ => LoadPlayerSample(AIDifficulty.Hard),
                        pdDec, pdInc,
                        bsDec, bsInc,
                        _ => SetAIDifficulty(AIDifficulty.Hard)
                    });

            // ── Row 6: [4] Expert + RAD [-][+] + INC [-][+] + [S+4] Expert ──
            if (6 < rows.Count)
                rows[6].CreateButtonOverlays(
                    new[] { pad, setMinus, setPlus, spdMinus, spdPlus, c6 + pad },
                    new[] { w0, btnW, setPlusW, btnW, spdPlusW, w6 },
                    new Action<int>[] {
                        _ => LoadPlayerSample(AIDifficulty.Expert),
                        rdDec, rdInc,
                        siDec, siInc,
                        _ => SetAIDifficulty(AIDifficulty.Expert)
                    });

            // ── Row 7: [5] Keyboard + ANG [-][+] ──
            if (7 < rows.Count)
                rows[7].CreateButtonOverlays(
                    new[] { pad, spdMinus, spdPlus },
                    new[] { w0, btnW, spdPlusW },
                    new Action<int>[] {
                        _ => LoadUserControlled(),
                        anDec, anInc
                    });

            // ── Row 8: [6] Mouse + [D] DEFAULTS ──
            if (8 < rows.Count)
                rows[8].CreateButtonOverlays(
                    new[] { pad, c1 + pad },
                    new[] { w0, w1 },
                    new Action<int>[] {
                        _ => LoadMouseControlled(),
                        _ => { PongBootstrap.ClearOverrides(); SimulationTime.Instance?.SetTimeScale(1f); SettingsBridge.SetQualityLevel(3); QualityBridge.SetTier(QualityTier.Ultra); SettingsBridge.SetFontSize(20f); SettingsBridge.SetMasterVolume(0.5f); SettingsBridge.SetMusicVolume(0.25f); SettingsBridge.SetSfxVolume(0.75f); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                    });

            _buttonsCreated = true;
        }

        private void CycleQuality()
        {
            int next = (SettingsBridge.QualityLevel + 1) % 4;
            SettingsBridge.SetQualityLevel(next);
            QualityBridge.SetTier((QualityTier)next);
        }

        private void ReloadScene()
        {
            if (_bootstrap != null)
                PongBootstrap.SaveOverrides(_bootstrap);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

            // Sync audio sliders
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(SettingsBridge.MasterVolume);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(SettingsBridge.MusicVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(SettingsBridge.SfxVolume);

            // Sync controls / quality sliders
            if (_speedSlider != null)
            {
                var sim = SimulationTime.Instance;
                if (sim != null) _speedSlider.SetValueWithoutNotify(SpeedToSlider(sim.timeScale));
            }
            if (_qualitySlider != null) _qualitySlider.SetValueWithoutNotify(SettingsBridge.QualityLevel / 3f);
            // Don't sync font slider here — it applies immediately and syncing would fight with drag

            // Sync bootstrap parameter sliders
            if (_bootstrap != null)
            {
                if (_courtWSlider != null) _courtWSlider.SetValueWithoutNotify((_bootstrap.courtWidth - 8f) / 24f);
                if (_courtHSlider != null) _courtHSlider.SetValueWithoutNotify((_bootstrap.courtHeight - 5f) / 15f);
                if (_paddleHSlider != null) _paddleHSlider.SetValueWithoutNotify((_bootstrap.paddleHeight - 0.5f) / 4.5f);
                if (_ballRadSlider != null) _ballRadSlider.SetValueWithoutNotify((_bootstrap.ballRadius - 0.1f) / 0.9f);
                if (_ballSpdSlider != null) _ballSpdSlider.SetValueWithoutNotify((_bootstrap.ballStartSpeed - 0.5f) / 9.5f);
                if (_spdIncSlider != null) _spdIncSlider.SetValueWithoutNotify(_bootstrap.ballSpeedIncrease / 2f);
                if (_bounceSlider != null) _bounceSlider.SetValueWithoutNotify((_bootstrap.maxBounceAngle - 15f) / 70f);
            }

            // Build all 7 columns
            var cols = new string[COL_COUNT][];
            cols[0] = BuildScriptColumn();
            cols[1] = BuildQualityFontColumn();
            cols[2] = BuildMatchColumn();
            cols[3] = BuildTitleColumn();
            cols[4] = BuildControlsColumn();
            cols[5] = BuildAudioColumn();
            cols[6] = BuildAIColumn();

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
                " YOU", " SETTINGS", " MATCH",
                $" {TUIColors.Bold("PONG")}", " CONTROLS", " AUDIO", "OPPONENT"
            };

            // Dynamic info (shown on hover)
            string[] dynamic = new string[COL_COUNT];
            // Col 0: score
            if (_match != null)
                dynamic[0] = $" {TUIColors.Fg(TUIColors.BrightCyan, $"YOU:{_match.LeftScore}")}";
            else
                dynamic[0] = labels[0];
            // Col 1: quality
            dynamic[1] = $" {((QualityTier)SettingsBridge.QualityLevel)}";
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
            // Col 5: volume
            dynamic[5] = $" VOL:{SettingsBridge.MasterVolume * 100:F0}%";
            // Col 6: AI info
            if (_ai != null && _match != null)
                dynamic[6] = $" {TUIColors.Fg(TUIColors.BrightMagenta, $"AI({_ai.Difficulty}):{_match.RightScore}")}";
            else
                dynamic[6] = labels[6];

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
                int inst = _playerProgram.Program?.Instructions?.Length ?? 0;
                string status = _playerProgram.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STP");
                string tier = _playerInputMode == "keyboard"
                    ? TUIColors.Fg(TUIColors.BrightYellow, "(Keyboard)")
                    : _playerInputMode == "mouse"
                    ? TUIColors.Fg(TUIColors.BrightYellow, "(Mouse)")
                    : _playerScriptTier.HasValue
                    ? TUIColors.Fg(TUIColors.BrightMagenta, $"({_playerScriptTier.Value})")
                    : TUIColors.Dimmed("(custom)");
                lines.Add($"  {status} {TUIColors.Dimmed($"{inst}i")} {tier}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
            }

            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("LOAD:")}");
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
                int inst = _ai.Program.Program?.Instructions?.Length ?? 0;
                string status = _ai.Program.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STP");
                string diff = TUIColors.Fg(TUIColors.BrightMagenta, $"({_ai.Difficulty})");
                lines.Add($"  {status} {TUIColors.Dimmed($"{inst}i")} {diff}");
            }
            else
            {
                string aiDiff = _ai != null ? _ai.Difficulty.ToString() : "?";
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightYellow, aiDiff)}");
            }

            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("LOAD:")}");
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
            int colW = ColWidth(3);
            var art = BuildAsciiArt(colW);
            int artWidth = art.Length > 0 ? VisibleLen(art[0]) : 0;
            int pad = Mathf.Max(0, (colW - artWidth) / 2);
            if (pad > 0)
            {
                string spaces = new string(' ', pad);
                for (int i = 0; i < art.Length; i++)
                    if (!string.IsNullOrEmpty(art[i]))
                        art[i] = spaces + art[i];
            }
            return art;
        }

        /// <summary>Count visible (non-tag) characters in a rich-text string.</summary>
        private static int VisibleLen(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 0;
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '<') { inTag = true; continue; }
                if (text[i] == '>') { inTag = false; continue; }
                if (!inTag) count++;
            }
            return count;
        }

        // ── Column 2: MATCH ─────────────────────────────────────

        private string[] BuildMatchColumn()
        {
            var lines = new List<string>();

            if (_match != null)
            {
                string emdash = "\u2014";
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, $"{_match.LeftScore}")} {emdash} {TUIColors.Fg(TUIColors.BrightCyan, "YOU")}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightMagenta, $"{_match.RightScore}")} {emdash} {TUIColors.Fg(TUIColors.BrightMagenta, "AI")}");
                lines.Add("");
                int w = ColWidth(2);
                string scoreLabel = "SCORE";
                int pad = Mathf.Max(0, (w - scoreLabel.Length) / 2);
                lines.Add(new string(' ', pad) + TUIColors.Dimmed(scoreLabel));
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"{_match.PlayerWins}")} {emdash} WIN");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightMagenta, $"{_match.AIWins}")} {emdash} LOSS");
                lines.Add($"  {_match.MatchesPlayed} {emdash} TOTAL");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No match"));
            }

            return lines.ToArray();
        }

        // ── Adaptive slider row builder ───────────────────────

        /// <summary>
        /// Builds a slider row that adapts to available column width.
        /// Tiers:  w&lt;6: "- +"  |  w&lt;10: "[-] [+]"  |  w&lt;16: "[-] LBL [+]"
        ///         w&lt;22: "[-] LBL VAL [+]"  |  w>=22: "[-] LBL BAR VAL [+]"
        /// </summary>
        private static string AdaptiveSliderRow(int colWidth, string label, float norm, string valueStr, bool showPct = false)
        {
            // Reserve 1 char for column divider
            int w = colWidth - 1;
            string minus = TUIColors.Fg(TUIColors.BrightCyan, "[-]");
            string plus = TUIColors.Fg(TUIColors.BrightCyan, "[+]");

            if (w < 6)
                return $" {TUIColors.Fg(TUIColors.BrightCyan, "-")} {TUIColors.Fg(TUIColors.BrightCyan, "+")}";
            if (w < 10)
                return $" {minus} {plus}";
            if (w < 16)
                return $" {minus} {label} {plus}";

            // overhead: " [-] LBL VVVV [+]" = 1+3+1+lbl+1+val+1+3 = 10+lbl+val
            int overhead = 10 + label.Length + valueStr.Length;
            if (w < overhead + 4)
                return $" {minus} {label} {valueStr} {plus}";

            // Full: label + bar + value
            int barLen = w - overhead;
            return $" {minus} {label}{TUIWidgets.ProgressBar(Mathf.Clamp01(norm), barLen, showPct)}{valueStr} {plus}";
        }

        // ── Column 4: CONTROLS ──────────────────────────────────

        private string[] BuildControlsColumn()
        {
            var lines = new List<string>();
            int w = ColWidth(4);
            var sim = SimulationTime.Instance;
            float speed = sim != null ? sim.timeScale : 1f;
            float speedNorm = SpeedToSlider(speed);
            string speedFmt = speed < 10f ? $"{speed:F1}" : $"{speed:F0}";
            string speedStr = $"{speedFmt,3}x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED") : "";

            lines.Add(AdaptiveSliderRow(w, "SPD", speedNorm, speedStr) + paused);
            string pauseLabel = (sim != null && sim.isPaused) ? "PLAY" : "PAUSE";
            lines.Add($" {TUIColors.Fg(TUIColors.BrightCyan, "[P]")} {pauseLabel}");
            lines.Add("");

            int cw = ColWidth(4);
            string physLabel = "PHYSICS";
            int physPad = Mathf.Max(0, (cw - physLabel.Length) / 2);
            lines.Add(new string(' ', physPad) + TUIColors.Dimmed(physLabel));

            if (_bootstrap == null) _bootstrap = FindFirstObjectByType<PongBootstrap>();
            if (_bootstrap != null)
            {
                lines.Add(AdaptiveSliderRow(w, "BSP", (_bootstrap.ballStartSpeed - 0.5f) / 9.5f, $"{_bootstrap.ballStartSpeed,4:F1}"));
                lines.Add(AdaptiveSliderRow(w, "INC", _bootstrap.ballSpeedIncrease / 2f, $"{_bootstrap.ballSpeedIncrease,4:F2}"));
                lines.Add(AdaptiveSliderRow(w, "ANG", (_bootstrap.maxBounceAngle - 15f) / 70f, $"{_bootstrap.maxBounceAngle,4:F0}"));
            }
            return lines.ToArray();
        }

        // ── Column 5: QUALITY / FONT ─────────────────────────────

        private string[] BuildQualityFontColumn()
        {
            var lines = new List<string>();
            int w = ColWidth(1);

            float qualNorm = SettingsBridge.QualityLevel / 3f;
            string qualName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            if (qualName.Length > 4) qualName = qualName.Substring(0, 4);
            else qualName = qualName.PadRight(4);

            lines.Add(AdaptiveSliderRow(w, "QTY", qualNorm, qualName));

            float fontNorm = FontToSlider(SettingsBridge.FontSize);
            string fontStr = $"{SettingsBridge.FontSize,2:F0}pt";

            lines.Add(AdaptiveSliderRow(w, "FNT", fontNorm, fontStr));

            // Bootstrap game parameters
            if (_bootstrap == null)
                _bootstrap = FindFirstObjectByType<PongBootstrap>();
            if (_bootstrap != null)
            {
                lines.Add(AdaptiveSliderRow(w, "C.W", (_bootstrap.courtWidth - 8f) / 24f, $"{_bootstrap.courtWidth,4:F0}"));
                lines.Add(AdaptiveSliderRow(w, "C.H", (_bootstrap.courtHeight - 5f) / 15f, $"{_bootstrap.courtHeight,4:F0}"));
                lines.Add(AdaptiveSliderRow(w, "PDL", (_bootstrap.paddleHeight - 0.5f) / 4.5f, $"{_bootstrap.paddleHeight,4:F1}"));
                lines.Add(AdaptiveSliderRow(w, "RAD", (_bootstrap.ballRadius - 0.1f) / 0.9f, $"{_bootstrap.ballRadius,4:F2}"));
            }

            lines.Add("");
            lines.Add($" {TUIColors.Fg(TUIColors.BrightCyan, "[D]")} DEFAULTS");

            return lines.ToArray();
        }

        private string BuildParamRow(int colWidth, string label, float norm, string valueStr)
        {
            return AdaptiveSliderRow(colWidth, label, norm, valueStr);
        }

        // ── Column 6: AUDIO ──────────────────────────────────────

        private string[] BuildAudioColumn()
        {
            var lines = new List<string>();
            int w = ColWidth(5);

            lines.Add(AdaptiveSliderRow(w, "VOL", SettingsBridge.MasterVolume, $"{SettingsBridge.MasterVolume * 100:F0}%"));
            lines.Add(AdaptiveSliderRow(w, "MSC", SettingsBridge.MusicVolume, $"{SettingsBridge.MusicVolume * 100:F0}%"));
            lines.Add(AdaptiveSliderRow(w, "SFX", SettingsBridge.SfxVolume, $"{SettingsBridge.SfxVolume * 100:F0}%"));

            // EQ visualization — fill remaining space below sliders
            if (_equalizer != null)
            {
                int availH = Mathf.Max(0, totalRows - 1 - lines.Count); // rows left after header + sliders
                int eqH = Mathf.Min(6, availH);
                if (eqH >= 1)
                {
                    var eqLines = TUIEqualizer.Render(
                        _equalizer.SmoothedBands,
                        _equalizer.PeakBands,
                        new TUIEqualizer.Config
                        {
                            Width      = w,
                            Height     = eqH,
                            Style      = TUIEqualizer.Style.Bars,
                            ShowBorder = false,
                            ShowPeaks  = true,
                            ShowLabels = false,
                        });
                    foreach (var line in eqLines)
                        lines.Add(line);
                }
            }

            return lines.ToArray();
        }



        // ═══════════════════════════════════════════════════════════════
        // ASCII ART ENGINE (from PongStatusCenter)
        // ═══════════════════════════════════════════════════════════════

        // Phase layout: even phases (0,2,4,6) = hold word, odd phases (1,3,5,7) = decipher transition
        // Word index for phase p: hold phases show word p/2, decipher phases transition from p/2 to (p/2+1)%N
        private int AsciiPhaseCount => AsciiWordCount * 2; // 8 total phases

        private void AdvanceAsciiTimer()
        {
            _asciiTimer += Time.deltaTime;
            bool isHold = (_asciiPhase % 2) == 0;
            float threshold = isHold ? AsciiHold : AsciiAnim;
            if (_asciiTimer >= threshold)
            {
                _asciiTimer = 0f;
                _asciiPhase = (_asciiPhase + 1) % AsciiPhaseCount;
                if ((_asciiPhase % 2) == 1) InitRevealThresholds();
            }
        }

        private void InitRevealThresholds()
        {
            int innerW = AsciiWords[0][0].Length;
            int total = innerW * 5;
            _revealThresholds = new float[total];
            for (int i = 0; i < total; i++) _revealThresholds[i] = UnityEngine.Random.value;
        }

        private string[] BuildAsciiArt(int maxWidth)
        {
            int wordIdx = (_asciiPhase / 2) % AsciiWordCount;
            // Clamp inner width so art + 2 border chars fit within maxWidth
            int innerW = AsciiWords[0][0].Length;
            int clampedInner = Mathf.Min(innerW, Mathf.Max(0, maxWidth - 2));
            if ((_asciiPhase % 2) == 0)
            {
                return ColorizeWord(AsciiWords[wordIdx], clampedInner);
            }
            else
            {
                int nextIdx = (wordIdx + 1) % AsciiWordCount;
                return DecipherWord(AsciiWords[wordIdx], AsciiWords[nextIdx], clampedInner);
            }
        }


        private string GradientBorderH(char left, char fill, char right, int innerWidth)
        {
            int total = innerWidth + 2;
            var sb = new StringBuilder(total * 32);
            sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(0f), left.ToString()));
            for (int i = 0; i < innerWidth; i++)
            {
                float t = total > 1 ? (float)(i + 1) / (total - 1) : 0f;
                sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(t), fill.ToString()));
            }
            sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(1f), right.ToString()));
            return sb.ToString();
        }

        private string GradientBorderV(string rawContent)
        {
            var sb = new StringBuilder(rawContent.Length + 128);
            sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(0f), "║"));
            sb.Append(rawContent);
            sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(1f), "║"));
            return sb.ToString();
        }

        private string GradientRowRaw(string row, int totalBorderedWidth)
        {
            int len = row.Length;
            if (len == 0) return "";
            var sb = new StringBuilder(len * 32);
            for (int i = 0; i < len; i++)
            {
                float t = totalBorderedWidth > 1 ? (float)(i + 1) / (totalBorderedWidth - 1) : 0f;
                sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(t), row[i].ToString()));
            }
            return sb.ToString();
        }

        private string[] ColorizeWord(string[] word, int innerW)
        {
            int totalW = innerW + 2;
            var lines = new string[9];
            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int i = 0; i < 5; i++)
            {
                string row = word[i].Length > innerW ? word[i].Substring(0, innerW) : word[i].PadRight(innerW);
                lines[2 + i] = GradientBorderV(GradientRowRaw(row, totalW));
            }
            lines[7] = GradientBorderV(new string(' ', innerW));
            lines[8] = GradientBorderH('╚', '═', '╝', innerW);
            return lines;
        }

        private string[] DecipherWord(string[] src, string[] tgt, int innerW)
        {
            float progress = Mathf.Clamp01(_asciiTimer / AsciiAnim);
            int totalW = innerW + 2;
            var lines = new string[9];

            lines[0] = GradientBorderH('╔', '═', '╗', innerW);
            lines[1] = GradientBorderV(new string(' ', innerW));
            for (int r = 0; r < 5; r++)
            {
                string s = src[r].Length > innerW ? src[r].Substring(0, innerW) : src[r].PadRight(innerW);
                string t = tgt[r].Length > innerW ? tgt[r].Substring(0, innerW) : tgt[r].PadRight(innerW);
                lines[2 + r] = GradientBorderV(
                    DecipherRowRaw(s, t, progress, r * innerW, totalW));
            }
            lines[7] = GradientBorderV(new string(' ', innerW));
            lines[8] = GradientBorderH('╚', '═', '╝', innerW);
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

                if (srcCh == tgtCh) { sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(t), tgtCh.ToString())); continue; }

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
                sb.Append(TUIColors.Fg(TUIGradient.CyanMagenta(t), ch.ToString()));
            }
            return sb.ToString();
        }
    }
}
