// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using Pong.Game;
using Pong.Scripting;
using Pong.AI;
using Pong.UI;

namespace Pong.Core
{
    /// <summary>
    /// Bootstrap for Pong — the "Hello World" of CodeGamified.
    ///
    /// Architecture (same pattern as SeaRäuber / BitNaughts):
    ///   - Instantiate managers → wire cross-references → configure scene
    ///   - .engine submodule gives us TUI + Code Execution for free
    ///   - Players don't use WASD — they WRITE CODE to control their paddle
    ///   - "Unit test" your script against Easy/Medium/Hard/Expert AI
    ///   - Crank time scale to run thousands of matches at warp speed
    ///
    /// Attach to a GameObject. Press Play → Pong appears.
    /// </summary>
    public class PongBootstrap : MonoBehaviour
    {
        // =================================================================
        // INSPECTOR
        // =================================================================

        [Header("Court")]
        [Tooltip("Court width (X axis)")]
        public float courtWidth = 16f;

        [Tooltip("Court height (Y axis)")]
        public float courtHeight = 10f;

        [Tooltip("Paddle height (fraction of court)")]
        public float paddleHeight = 2f;

        [Tooltip("Paddle thickness")]
        public float paddleThickness = 0.3f;

        [Tooltip("Paddle offset from wall")]
        public float paddleOffset = 1f;

        [Tooltip("Ball radius")]
        public float ballRadius = 0.25f;

        [Header("Ball Physics")]
        public float ballStartSpeed = 6f;
        public float ballMaxSpeed = 20f;
        public float ballSpeedIncrease = 0.5f;
        public float maxBounceAngle = 60f;

        [Header("Match")]
        [Tooltip("Points to win a match")]
        public int pointsToWin = 11;

        [Tooltip("Auto-restart after match ends")]
        public bool autoRestart = true;

        [Tooltip("Delay before serving (sim-seconds)")]
        public float serveDelay = 1f;

        [Header("AI Opponent")]
        [Tooltip("Which AI difficulty the player's code tests against")]
        public AIDifficulty aiDifficulty = AIDifficulty.Easy;

        [Header("Time")]
        [Tooltip("Enable time scale modulation for fast testing")]
        public bool enableTimeScale = true;

        [Header("TUI Frontend")]
        [Tooltip("Enable terminal UI overlay (.engine)")]
        public bool enableTUI = true;

        [Header("Scripting")]
        [Tooltip("Enable code execution (.engine)")]
        public bool enableScripting = true;

        [Header("Leaderboard")]
        [Tooltip("Enable leaderboard tracking")]
        public bool enableLeaderboard = true;

        [Header("Camera")]
        public bool configureCamera = true;

        [Header("Debug")]
        public bool debugLogging = true;

        // =================================================================
        // RUNTIME REFERENCES
        // =================================================================

        private PongCourt _court;
        private PongBall _ball;
        private PongPaddle _leftPaddle;   // Player's code-controlled paddle
        private PongPaddle _rightPaddle;  // AI paddle
        private PongMatchManager _match;
        private PongAIController _aiController;
        private PaddleProgram _playerProgram;
        private PongLeaderboard _leaderboard;

        // TUI (from .engine)
        private PongTUIManager _tuiManager;

        // =================================================================
        // BOOTSTRAP
        // =================================================================

        private void Start()
        {
            Log("🏓 Pong Bootstrap starting...");

            SetupSimulationTime();
            SetupCamera();
            CreateCourt();
            CreatePaddles();
            CreateBall();
            CreateMatchManager();
            CreateAI();

            if (enableScripting) CreatePlayerProgram();
            if (enableLeaderboard) CreateLeaderboard();
            if (enableTUI) CreateTUI();

            WireEvents();
            StartCoroutine(RunBootSequence());
        }

        // =================================================================
        // SIMULATION TIME (reuse from Core)
        // =================================================================

        private void SetupSimulationTime()
        {
            if (SimulationTime.Instance != null)
            {
                Log("SimulationTime already exists, configuring for Pong.");
                ConfigureSimulationTime(SimulationTime.Instance);
                return;
            }

            var go = new GameObject("SimulationTime");
            var sim = go.AddComponent<SimulationTime>();
            ConfigureSimulationTime(sim);
            Log("Created SimulationTime");
        }

        private void ConfigureSimulationTime(SimulationTime sim)
        {
            // Pong doesn't need day/night — just time scale control
            sim.dayLengthSeconds = 99999f;
            sim.startingHour = 12f;
            // Pong time scale presets: 1x real-time → 1000x warp speed
            sim.timeScalePresets = new float[] { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f, 500f, 1000f };
        }

        // =================================================================
        // CAMERA
        // =================================================================

        private void SetupCamera()
        {
            if (!configureCamera) return;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            // Orthographic top-down view centered on court
            cam.orthographic = true;
            cam.orthographicSize = courtHeight / 2f + 1f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.LookAt(Vector3.zero, Vector3.up);
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            Log($"Camera: ortho, size={cam.orthographicSize}");
        }

        // =================================================================
        // COURT
        // =================================================================

        private void CreateCourt()
        {
            var go = new GameObject("Court");
            _court = go.AddComponent<PongCourt>();
            _court.Width = courtWidth;
            _court.Height = courtHeight;
            _court.Initialize();
            Log($"Created Court ({courtWidth}x{courtHeight})");
        }

        // =================================================================
        // PADDLES
        // =================================================================

        private void CreatePaddles()
        {
            float halfW = courtWidth / 2f;

            // Left paddle — player's CODE controls this
            var leftGo = CreatePaddleObject("LeftPaddle (CODE)", 
                new Vector3(-halfW + paddleOffset, 0f, 0f));
            _leftPaddle = leftGo.AddComponent<PongPaddle>();
            _leftPaddle.Initialize(paddleHeight, paddleThickness, courtHeight, PaddleSide.Left);

            // Right paddle — AI opponent
            var rightGo = CreatePaddleObject("RightPaddle (AI)", 
                new Vector3(halfW - paddleOffset, 0f, 0f));
            _rightPaddle = rightGo.AddComponent<PongPaddle>();
            _rightPaddle.Initialize(paddleHeight, paddleThickness, courtHeight, PaddleSide.Right);

            Log("Created Paddles (Left=CODE, Right=AI)");
        }

        private GameObject CreatePaddleObject(string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = new Vector3(paddleThickness, paddleHeight, 1f);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.white;
            }

            // Remove 3D collider, we do our own collision
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            return go;
        }

        // =================================================================
        // BALL
        // =================================================================

        private void CreateBall()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Ball";
            go.transform.localScale = new Vector3(ballRadius * 2f, ballRadius * 2f, 1f);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.white;
            }

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            _ball = go.AddComponent<PongBall>();
            _ball.Initialize(ballStartSpeed, ballMaxSpeed, ballSpeedIncrease, 
                             ballRadius, maxBounceAngle, courtWidth, courtHeight);

            Log("Created Ball");
        }

        // =================================================================
        // MATCH MANAGER
        // =================================================================

        private void CreateMatchManager()
        {
            var go = new GameObject("MatchManager");
            _match = go.AddComponent<PongMatchManager>();
            _match.Initialize(_ball, _leftPaddle, _rightPaddle, _court,
                              pointsToWin, autoRestart, serveDelay);
            Log($"Created MatchManager (first to {pointsToWin})");
        }

        // =================================================================
        // AI
        // =================================================================

        private void CreateAI()
        {
            var go = new GameObject("AIController");
            _aiController = go.AddComponent<PongAIController>();
            _aiController.Initialize(_rightPaddle, _ball, _court, aiDifficulty);
            Log($"Created AI ({aiDifficulty})");
        }

        // =================================================================
        // PLAYER SCRIPTING (.engine powered)
        // =================================================================

        private void CreatePlayerProgram()
        {
            var go = new GameObject("PlayerProgram");
            _playerProgram = go.AddComponent<PaddleProgram>();
            _playerProgram.Initialize(_leftPaddle, _ball, _court);
            Log("Created PlayerProgram (code-controlled paddle)");
        }

        // =================================================================
        // LEADERBOARD
        // =================================================================

        private void CreateLeaderboard()
        {
            var go = new GameObject("Leaderboard");
            _leaderboard = go.AddComponent<PongLeaderboard>();
            Log("Created Leaderboard");
        }

        // =================================================================
        // TUI (.engine powered)
        // =================================================================

        private void CreateTUI()
        {
            var go = new GameObject("PongTUI");
            _tuiManager = go.AddComponent<PongTUIManager>();
            _tuiManager.Initialize(_match, _playerProgram, _leaderboard, _aiController);
            Log("Created PongTUI");
        }

        // =================================================================
        // EVENT WIRING
        // =================================================================

        private void WireEvents()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale → {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "⏸ PAUSED" : "▶ RESUMED");
            }

            if (_match != null)
            {
                _match.OnPointScored += (side, leftScore, rightScore) =>
                    Log($"SCORE! {side} │ {leftScore}-{rightScore}");

                _match.OnMatchEnded += (winner) =>
                {
                    Log($"MATCH OVER — {winner} WINS!");
                    if (_leaderboard != null)
                    {
                        bool playerWon = (winner == PaddleSide.Left);
                        _leaderboard.RecordMatch(aiDifficulty, playerWon,
                            _match.LeftScore, _match.RightScore);
                    }
                };
            }
        }

        // =================================================================
        // BOOT SEQUENCE
        // =================================================================

        private System.Collections.IEnumerator RunBootSequence()
        {
            yield return null;
            yield return null;

            Log("────────────────────────────────────────");
            Log("🏓 PONG — Write Code. Beat AI. Level Up.");
            Log("────────────────────────────────────────");
            Log($"  COURT │ {courtWidth}×{courtHeight}");
            Log($"  BALL  │ speed={ballStartSpeed}, max={ballMaxSpeed}");
            Log($"  MATCH │ first to {pointsToWin}");
            Log($"  AI    │ {aiDifficulty}");
            Log($"  TIME  │ {SimulationTime.Instance?.GetFormattedTimeScale() ?? "1x"}");
            Log("────────────────────────────────────────");
            Log($"  Scripting │ {(enableScripting ? "✅ ACTIVE" : "── disabled")}");
            Log($"  TUI       │ {(enableTUI ? "✅ ACTIVE" : "── disabled")}");
            Log($"  Leaderboard│ {(enableLeaderboard ? "✅ ACTIVE" : "── disabled")}");
            Log("────────────────────────────────────────");
            Log("🏓 Bootstrap complete. Write your paddle code!");

            // Auto-serve first ball
            if (_match != null) _match.StartMatch();
        }

        // =================================================================
        // CLEANUP
        // =================================================================

        private void OnDestroy()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged -= s => { };
                SimulationTime.Instance.OnPausedChanged -= p => { };
            }
        }

        private void Log(string message)
        {
            if (debugLogging)
                Debug.Log($"[PONG] {message}");
        }
    }
}
