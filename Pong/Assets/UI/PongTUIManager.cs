// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using Pong.Core;
using Pong.Game;
using Pong.AI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// TUI Manager for Pong — powered by .engine's TerminalWindow system.
    /// Shows scoreboard, code editor, leaderboard, AI samples, time controls.
    ///
    /// Layout on screen:
    ///   ┌──────────────────────────────────┐
    ///   │  SCOREBOARD  │  LEADERBOARD      │
    ///   ├──────────────┼──────────────────-─┤
    ///   │  CODE EDITOR │  AI SAMPLE CODE   │
    ///   │              │  (learn from AI)  │
    ///   ├──────────────┴──────────────────-─┤
    ///   │  STATUS BAR: time scale, match#  │
    ///   └──────────────────────────────────┘
    /// </summary>
    public class PongTUIManager : MonoBehaviour
    {
        private PongMatchManager _match;
        private PaddleProgram _playerProgram;
        private PongLeaderboard _leaderboard;
        private PongAIController _ai;

        // TUI state
        private bool _showCodeEditor = true;
        private bool _showLeaderboard = true;
        private bool _showAISamples = false;
        private AIDifficulty _viewingSampleDifficulty = AIDifficulty.Easy;

        // Style
        private GUIStyle _terminalStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _codeStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        // Code editor
        private string _editBuffer = "";
        private Vector2 _codeScroll;
        private Vector2 _sampleScroll;

        public void Initialize(PongMatchManager match, PaddleProgram program,
                               PongLeaderboard leaderboard, PongAIController ai)
        {
            _match = match;
            _playerProgram = program;
            _leaderboard = leaderboard;
            _ai = ai;

            if (_playerProgram != null)
                _editBuffer = _playerProgram.CurrentSourceCode;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _terminalStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = new Color(0f, 1f, 0.4f), background = MakeTex(1, 1, new Color(0.05f, 0.05f, 0.1f, 0.95f)) },
                font = Font.CreateDynamicFontFromOSFont("Consolas", 14),
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 8, 8),
                wordWrap = true
            };

            _headerStyle = new GUIStyle(_terminalStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            _codeStyle = new GUIStyle(GUI.skin.textArea)
            {
                normal = { textColor = new Color(0f, 1f, 0.4f), background = MakeTex(1, 1, new Color(0.02f, 0.02f, 0.06f, 0.95f)) },
                font = Font.CreateDynamicFontFromOSFont("Consolas", 13),
                fontSize = 13,
                wordWrap = false,
                padding = new RectOffset(6, 6, 6, 6)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white, background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.4f, 0.9f)) },
                hover = { textColor = Color.cyan, background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.5f, 0.9f)) },
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            float margin = 10f;

            // ── SCOREBOARD (top center) ──
            DrawScoreboard(sw, margin);

            // ── STATUS BAR (bottom) ──
            DrawStatusBar(sw, sh, margin);

            // ── CODE EDITOR (left panel) ──
            if (_showCodeEditor)
                DrawCodeEditor(margin, 80f, sw * 0.35f, sh - 160f);

            // ── LEADERBOARD (top right) ──
            if (_showLeaderboard && _leaderboard != null)
                DrawLeaderboard(sw - sw * 0.3f - margin, 80f, sw * 0.3f, sh * 0.3f);

            // ── AI SAMPLE CODE (right panel, below leaderboard) ──
            if (_showAISamples)
                DrawAISamples(sw - sw * 0.3f - margin, 80f + sh * 0.32f, sw * 0.3f, sh * 0.45f);

            // ── TOGGLE KEYS ──
            HandleInputToggles();
        }

        private void DrawScoreboard(float sw, float margin)
        {
            float w = 400f;
            float h = 60f;
            float x = (sw - w) / 2f;

            string score = _match != null
                ? $"  YOUR CODE: {_match.LeftScore}   │   AI ({_ai?.Difficulty}): {_match.RightScore}  "
                : "  PONG — Write Code. Beat AI. Level Up.  ";

            GUI.Box(new Rect(x, margin, w, h), score, _headerStyle);
        }

        private void DrawStatusBar(float sw, float sh, float margin)
        {
            float h = 30f;
            float y = sh - h - margin;
            string timeInfo = SimulationTime.Instance != null
                ? $"  TIME: {SimulationTime.Instance.GetFormattedTime()} │ " +
                  $"SPEED: {SimulationTime.Instance.GetFormattedTimeScale()} │ " +
                  $"+/- to change │ SPACE to pause"
                : "";

            string matchInfo = _match != null
                ? $" │ MATCHES: {_match.MatchesPlayed} (W:{_match.PlayerWins} L:{_match.AIWins})"
                : "";

            string rankInfo = _leaderboard != null
                ? $" │ RANK: {_leaderboard.CurrentRank}"
                : "";

            GUI.Box(new Rect(margin, y, sw - margin * 2, h),
                $"{timeInfo}{matchInfo}{rankInfo}  │  [TAB] code  [L] leaderboard  [S] AI samples",
                _terminalStyle);
        }

        private void DrawCodeEditor(float x, float y, float w, float h)
        {
            GUI.Box(new Rect(x, y, w, 28f),
                "  CODE EDITOR — Write your paddle AI", _headerStyle);

            _codeScroll = GUI.BeginScrollView(
                new Rect(x, y + 30f, w, h - 70f),
                _codeScroll,
                new Rect(0, 0, w - 20f, Mathf.Max(h - 70f, _editBuffer.Split('\n').Length * 18f)));

            _editBuffer = GUI.TextArea(
                new Rect(0, 0, w - 20f, Mathf.Max(h - 70f, _editBuffer.Split('\n').Length * 18f)),
                _editBuffer, _codeStyle);

            GUI.EndScrollView();

            // Buttons row
            float btnY = y + h - 36f;
            float btnW = (w - 20f) / 3f;

            if (GUI.Button(new Rect(x + 5f, btnY, btnW, 30f), "▶ UPLOAD & RUN", _buttonStyle))
            {
                if (_playerProgram != null)
                    _playerProgram.UploadCode(_editBuffer);
            }

            if (GUI.Button(new Rect(x + btnW + 10f, btnY, btnW, 30f), "⟳ RESET CODE", _buttonStyle))
            {
                _editBuffer = @"# Just track the ball:
ball_y = get_ball_y()
set_target_y(ball_y)";
            }

            if (GUI.Button(new Rect(x + btnW * 2 + 15f, btnY, btnW, 30f), "📋 LOAD SAMPLE", _buttonStyle))
            {
                _showAISamples = !_showAISamples;
            }
        }

        private void DrawLeaderboard(float x, float y, float w, float h)
        {
            GUI.Box(new Rect(x, y, w, h), _leaderboard.GetSummary(), _terminalStyle);
        }

        private void DrawAISamples(float x, float y, float w, float h)
        {
            // Difficulty tabs
            float tabW = w / 4f;
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            for (int i = 0; i < 4; i++)
            {
                string label = diffs[i].ToString().ToUpper();
                if (diffs[i] == _viewingSampleDifficulty) label = $"[{label}]";
                if (GUI.Button(new Rect(x + i * tabW, y, tabW, 25f), label, _buttonStyle))
                    _viewingSampleDifficulty = diffs[i];
            }

            string code = PongAIController.GetSampleCode(_viewingSampleDifficulty);
            _sampleScroll = GUI.BeginScrollView(
                new Rect(x, y + 28f, w, h - 65f), _sampleScroll,
                new Rect(0, 0, w - 20f, code.Split('\n').Length * 18f));
            GUI.TextArea(new Rect(0, 0, w - 20f, code.Split('\n').Length * 18f), code, _codeStyle);
            GUI.EndScrollView();

            if (GUI.Button(new Rect(x + 5f, y + h - 34f, w - 10f, 30f), 
                $"📋 COPY {_viewingSampleDifficulty} CODE TO EDITOR", _buttonStyle))
            {
                _editBuffer = PongAIController.GetSampleCode(_viewingSampleDifficulty);
            }
        }

        private void HandleInputToggles()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) _showCodeEditor = !_showCodeEditor;
            if (Input.GetKeyDown(KeyCode.L)) _showLeaderboard = !_showLeaderboard;
            if (Input.GetKeyDown(KeyCode.S) && !GUI.GetNameOfFocusedControl().Contains("TextArea"))
                _showAISamples = !_showAISamples;

            // Quick AI difficulty switch with number keys (when not typing)
            if (!IsTyping())
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetAIDifficulty(AIDifficulty.Easy);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetAIDifficulty(AIDifficulty.Medium);
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetAIDifficulty(AIDifficulty.Hard);
                if (Input.GetKeyDown(KeyCode.Alpha4)) SetAIDifficulty(AIDifficulty.Expert);
            }
        }

        private void SetAIDifficulty(AIDifficulty diff)
        {
            if (_ai != null)
            {
                _ai.SetDifficulty(diff);
                Debug.Log($"[PONG] AI difficulty → {diff}");
            }
        }

        private bool IsTyping()
        {
            // Rough heuristic — if code editor is visible, user might be typing
            return false; // OnGUI textareas handle this internally
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
