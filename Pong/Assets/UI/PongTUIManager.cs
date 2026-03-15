// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeGamified.TUI;
using CodeGamified.Settings;
using Pong.Game;
using Pong.AI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// TUI Manager for Pong — two draggable debugger windows + 3-column status bar.
    /// Powered by .engine's CodeDebuggerWindow + TUIEdgeDragger.
    ///
    /// Layout (all edges draggable):
    ///   ┌─────────────────────┬─────────────────────┐
    ///   │ YOUR CODE           │ AI CODE             │
    ///   │ PY │ MACHINE │ REGS │ PY │ MACHINE │ INFO │
    ///   │    │         │      │    │         │      │
    ///   ├──────────┬──────────┬──────────────────────┤
    ///   │ SCRIPTS  │ ██ PONG  │ CONTROLS            │
    ///   │ YOU / AI │  5 — 3   │ [1-4] AI difficulty  │
    ///   │          │ W:3 L:1  │ [SPACE] pause ...    │
    ///   └──────────┴──────────┴──────────────────────┘
    /// </summary>
    public class PongTUIManager : MonoBehaviour, ISettingsListener
    {
        // Dependencies
        private PongMatchManager _match;
        private PaddleProgram _playerProgram;
        private PongLeaderboard _leaderboard;
        private PongAIController _ai;

        // Canvas
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Panels
        private PongCodeDebugger _playerDebugger;
        private PongCodeDebugger _aiDebugger;
        private PongStatusBar _statusBar;

        // Panel rects
        private RectTransform _leftPanelRect;
        private RectTransform _rightPanelRect;
        private RectTransform _statusBarRect;

        // Font
        private TMP_FontAsset _font;
        private float _fontSize;

        public void Initialize(PongMatchManager match, PaddleProgram program,
                               PongLeaderboard leaderboard, PongAIController ai)
        {
            _match = match;
            _playerProgram = program;
            _leaderboard = leaderboard;
            _ai = ai;
            _fontSize = SettingsBridge.FontSize;

            BuildCanvas();
            BuildPanels();
        }

        private void OnEnable()  => SettingsBridge.Register(this);
        private void OnDisable() => SettingsBridge.Unregister(this);

        public void OnSettingsChanged(SettingsSnapshot settings, SettingsCategory changed)
        {
            if (changed != SettingsCategory.Display) return;
            if (Mathf.Approximately(settings.FontSize, _fontSize)) return;

            _fontSize = settings.FontSize;
            RebuildPanels();
        }

        private void RebuildPanels()
        {
            // Destroy existing panels and rebuild with new font size
            if (_leftPanelRect != null) Destroy(_leftPanelRect.gameObject);
            if (_rightPanelRect != null) Destroy(_rightPanelRect.gameObject);
            if (_statusBarRect != null) Destroy(_statusBarRect.gameObject);
            _playerDebugger = null;
            _aiDebugger = null;
            _statusBar = null;

            BuildPanels();
        }

        // ═══════════════════════════════════════════════════════════════
        // CANVAS
        // ═══════════════════════════════════════════════════════════════

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("PongTUI_Canvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            _canvasRect = canvasGO.GetComponent<RectTransform>();

            // Ensure EventSystem exists
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PANELS
        // ═══════════════════════════════════════════════════════════════

        private void BuildPanels()
        {
            // ── Left panel: Player's code debugger ──
            _leftPanelRect = CreatePanel("PlayerPanel",
                new Vector2(0f, 0.25f),
                new Vector2(0.25f, 1f));

            _playerDebugger = _leftPanelRect.gameObject.AddComponent<PongCodeDebugger>();
            AddPanelBackground(_leftPanelRect);
            _playerDebugger.InitializeProgrammatic(GetFont(), _fontSize,
                _leftPanelRect.GetComponent<Image>());
            _playerDebugger.SetTitle("YOUR CODE");
            _playerDebugger.Bind(_playerProgram);

            // Draggers: right edge + bottom edge
            TUIEdgeDragger.Create(_leftPanelRect, _canvasRect, TUIEdgeDragger.Edge.Right);
            var leftBottom = TUIEdgeDragger.Create(_leftPanelRect, _canvasRect, TUIEdgeDragger.Edge.Bottom);

            // ── Right panel: AI code debugger ──
            _rightPanelRect = CreatePanel("AIPanel",
                new Vector2(0.75f, 0.25f),
                new Vector2(1f, 1f));

            _aiDebugger = _rightPanelRect.gameObject.AddComponent<PongCodeDebugger>();
            AddPanelBackground(_rightPanelRect);
            _aiDebugger.InitializeProgrammatic(GetFont(), _fontSize,
                _rightPanelRect.GetComponent<Image>());
            _aiDebugger.SetTitle("AI CODE");
            _aiDebugger.Bind(_ai.Program);

            // Draggers: left edge + bottom edge
            TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Left);
            var rightBottom = TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Bottom);

            // ── Status bar (bottom 25%) ──
            _statusBarRect = CreatePanel("StatusBar",
                new Vector2(0f, 0f),
                new Vector2(1f, 0.25f));

            _statusBar = _statusBarRect.gameObject.AddComponent<PongStatusBar>();
            AddPanelBackground(_statusBarRect);
            _statusBar.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusBarRect.GetComponent<Image>());
            _statusBar.Bind(_match, _leaderboard, _ai, _playerProgram);

            // Dragger: top edge of status bar
            var statusTop = TUIEdgeDragger.Create(_statusBarRect, _canvasRect, TUIEdgeDragger.Edge.Top);

            // Link edges: status bar top ↔ code panel bottoms
            statusTop.LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom)
                     .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            leftBottom.LinkEdge(_statusBarRect, TUIEdgeDragger.Edge.Top)
                      .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            rightBottom.LinkEdge(_statusBarRect, TUIEdgeDragger.Edge.Top)
                       .LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvasRect, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return rt;
        }

        private void AddPanelBackground(RectTransform panel)
        {
            var img = panel.gameObject.GetComponent<Image>();
            if (img == null)
                img = panel.gameObject.AddComponent<Image>();
            img.color = new Color(0.01f, 0.03f, 0.06f, 0.92f);
            img.raycastTarget = true;
        }

        private TMP_FontAsset GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.Load<TMP_FontAsset>("Fonts/Unifont SDF");
            if (_font == null)
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _font;
        }
    }
}
