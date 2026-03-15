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
    ///   ├──────────┼──────────┼──────────────────────┤
    ///   │ SCRIPTS  │ ██ PONG  │ CONTROLS            │
    ///   │ YOU / AI │  5 — 3   │ [1-4] AI difficulty  │
    ///   │ (panel)  │ (panel)  │ (panel)             │
    ///   └──────────┴──────────┴──────────────────────┘
    ///   Left/right status column dividers linked to code panels above.
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
        private PongStatusLeft _statusLeft;
        private PongStatusCenter _statusCenter;
        private PongStatusRight _statusRight;

        // Panel rects
        private RectTransform _leftPanelRect;
        private RectTransform _rightPanelRect;
        private RectTransform _statusLeftRect;
        private RectTransform _statusCenterRect;
        private RectTransform _statusRightRect;

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
            if (_statusLeftRect != null) Destroy(_statusLeftRect.gameObject);
            if (_statusCenterRect != null) Destroy(_statusCenterRect.gameObject);
            if (_statusRightRect != null) Destroy(_statusRightRect.gameObject);
            _playerDebugger = null;
            _aiDebugger = null;
            _statusLeft = null;
            _statusCenter = null;
            _statusRight = null;

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
            var leftRight = TUIEdgeDragger.Create(_leftPanelRect, _canvasRect, TUIEdgeDragger.Edge.Right);
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
            var rightLeft = TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Left);
            var rightBottom = TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Bottom);

            // ── Left status panel (bottom-left, aligned with player code) ──
            _statusLeftRect = CreatePanel("StatusLeft",
                new Vector2(0f, 0f),
                new Vector2(0.25f, 0.25f));

            _statusLeft = _statusLeftRect.gameObject.AddComponent<PongStatusLeft>();
            AddPanelBackground(_statusLeftRect);
            _statusLeft.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusLeftRect.GetComponent<Image>());
            _statusLeft.Bind(_match, _playerProgram, _ai);

            var statusLeftRight = TUIEdgeDragger.Create(_statusLeftRect, _canvasRect, TUIEdgeDragger.Edge.Right);
            var statusLeftTop = TUIEdgeDragger.Create(_statusLeftRect, _canvasRect, TUIEdgeDragger.Edge.Top);

            // ── Center status panel (bottom-center, PONG ASCII + scores) ──
            _statusCenterRect = CreatePanel("StatusCenter",
                new Vector2(0.25f, 0f),
                new Vector2(0.75f, 0.25f));

            _statusCenter = _statusCenterRect.gameObject.AddComponent<PongStatusCenter>();
            AddPanelBackground(_statusCenterRect);
            _statusCenter.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusCenterRect.GetComponent<Image>());
            _statusCenter.Bind(_match, _leaderboard);

            var statusCenterTop = TUIEdgeDragger.Create(_statusCenterRect, _canvasRect, TUIEdgeDragger.Edge.Top);

            // ── Right status panel (bottom-right, aligned with AI code) ──
            _statusRightRect = CreatePanel("StatusRight",
                new Vector2(0.75f, 0f),
                new Vector2(1f, 0.25f));

            _statusRight = _statusRightRect.gameObject.AddComponent<PongStatusRight>();
            AddPanelBackground(_statusRightRect);
            _statusRight.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusRightRect.GetComponent<Image>());

            var statusRightLeft = TUIEdgeDragger.Create(_statusRightRect, _canvasRect, TUIEdgeDragger.Edge.Left);
            var statusRightTop = TUIEdgeDragger.Create(_statusRightRect, _canvasRect, TUIEdgeDragger.Edge.Top);

            // ── Link vertical edges: code panels ↔ status panels ──
            // Left code right edge → left status right + center status left
            leftRight.LinkEdge(_statusLeftRect, TUIEdgeDragger.Edge.Right)
                     .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Left);
            // Left status right edge → left code right + center status left
            statusLeftRight.LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Right)
                           .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Left);
            // Right code left edge → right status left + center status right
            rightLeft.LinkEdge(_statusRightRect, TUIEdgeDragger.Edge.Left)
                     .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Right);
            // Right status left edge → right code left + center status right
            statusRightLeft.LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Left)
                           .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Right);

            // ── Link horizontal edges: all status tops ↔ code bottoms ──
            statusLeftTop.LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Top)
                         .LinkEdge(_statusRightRect, TUIEdgeDragger.Edge.Top)
                         .LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom)
                         .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            statusCenterTop.LinkEdge(_statusLeftRect, TUIEdgeDragger.Edge.Top)
                           .LinkEdge(_statusRightRect, TUIEdgeDragger.Edge.Top)
                           .LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom)
                           .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            statusRightTop.LinkEdge(_statusLeftRect, TUIEdgeDragger.Edge.Top)
                          .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Top)
                          .LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom)
                          .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            leftBottom.LinkEdge(_statusLeftRect, TUIEdgeDragger.Edge.Top)
                      .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Top)
                      .LinkEdge(_statusRightRect, TUIEdgeDragger.Edge.Top)
                      .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            rightBottom.LinkEdge(_statusLeftRect, TUIEdgeDragger.Edge.Top)
                       .LinkEdge(_statusCenterRect, TUIEdgeDragger.Edge.Top)
                       .LinkEdge(_statusRightRect, TUIEdgeDragger.Edge.Top)
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
