// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Audio;
using CodeGamified.Camera;
using CodeGamified.Editor;
using CodeGamified.Persistence;
using CodeGamified.Persistence.Providers;
using CodeGamified.Procedural;
using CodeGamified.Time;
using Pong.Audio;
using Pong.Game;
using Pong.Persistence;
using Pong.Scripting;
using Pong.AI;
using Pong.UI;
using CodeGamified.Settings;
using CodeGamified.Quality;

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

        [Header("Persistence")]
        [Tooltip("Enable script persistence (saves to .data/)")]
        public bool enablePersistence = true;

        [Tooltip("Use local git repo instead of in-memory (survives restarts)")]
        public bool useLocalGitProvider = false;

        [Tooltip("Path to .data/ directory (relative to project root)")]
        public string dataPath = "Assets/.data";

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

        // Procedural (from .engine)
        private ColorPalette _palette;
        private AssemblyResult _leftPaddleVisual;
        private AssemblyResult _rightPaddleVisual;
        private AssemblyResult _ballVisual;

        // Audio + Haptic (from .engine)
        private PongAudioProvider _audioProvider;
        private AudioBridge.EngineHandlers _engineAudio;
        private PongHapticProvider _hapticProvider;
        private HapticBridge.EngineHandlers _engineHaptic;

        // Persistence (from .engine)
        private PongPersistenceManager _persistence;

        // Goal zones (from Procedural)
        private AssemblyResult _leftGoalVisual;
        private AssemblyResult _rightGoalVisual;

        // Warp (from .engine Time)
        private PongWarpController _warpController;

        // Ball trail
        private PongBallTrail _ballTrail;

        // Camera (from .engine Camera)
        private CameraAmbientMotion _cameraSway;
        private Transform _cameraFollowTarget;
        private bool _cameraFollowIsBall;
        private static readonly Vector3 DefaultCameraPos = new Vector3(0f, 8f, -12f);
        private float _followDistance = 10f;
        private float _followElevation = 5f;
        private const float FollowSideOffset = 3f;
        private const float FollowLerpSpeed = 6f;
        private const float ZoomSpeed = 8f;
        private const float MinZoom = 4f;
        private const float MaxZoom = 30f;

        // Code editor (from .engine Editor)
        private CodeEditorWindow _codeEditor;
        private PongEditorExtension _editorExt;
        private PongCompilerExtension _compilerExt;

        // =================================================================
        // UPDATE — camera click-to-follow
        // =================================================================

        private void Update()
        {
            HandleScrollZoom();
            UpdateCameraFollow();
            HandleCameraClick();
            HandleCameraEscape();
        }

        private void HandleScrollZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            if (_cameraFollowTarget != null)
            {
                // Follow mode — zoom adjusts distance and elevation proportionally
                _followDistance -= scroll * ZoomSpeed;
                _followDistance = Mathf.Clamp(_followDistance, MinZoom, MaxZoom);
                _followElevation = _followDistance * 0.5f;
            }
            else if (_cameraSway != null && _cameraSway.enabled)
            {
                // Sway mode — zoom adjusts the base position Z
                Vector3 basePos = _cameraSway.lookAtTarget - Vector3.forward;
                float currentZ = _cameraSway.lookAtTarget.z - _cameraSway.transform.position.z;
                if (currentZ < 0.1f) currentZ = Mathf.Abs(DefaultCameraPos.z);

                float newZ = currentZ - scroll * ZoomSpeed;
                newZ = Mathf.Clamp(newZ, MinZoom, MaxZoom);

                float ratio = newZ / Mathf.Max(Mathf.Abs(DefaultCameraPos.z), 0.01f);
                Vector3 newBase = new Vector3(
                    DefaultCameraPos.x,
                    DefaultCameraPos.y * ratio,
                    -newZ);
                _cameraSway.SetBasePosition(newBase);
            }
        }

        private void UpdateCameraFollow()
        {
            if (_cameraFollowTarget == null) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 targetPos = _cameraFollowTarget.position;

            // Camera sits behind the target in -Z, elevated, with side offset
            float sideOffset = 0f;
            if (!_cameraFollowIsBall)
            {
                // Offset toward the paddle's side so view isn't blocked
                sideOffset = targetPos.x < 0f ? -FollowSideOffset : FollowSideOffset;
            }

            Vector3 desiredCamPos = new Vector3(
                targetPos.x + sideOffset,
                targetPos.y + _followElevation,
                targetPos.z - _followDistance);

            // Look at the ball (or court center if ball is inactive)
            Vector3 lookPoint = (_ball != null && _ball.IsActive)
                ? new Vector3(_ball.Position.x, _ball.Position.y, 0f)
                : Vector3.zero;

            // Smooth lerp — never snap
            cam.transform.position = Vector3.Lerp(
                cam.transform.position, desiredCamPos,
                Time.unscaledDeltaTime * FollowLerpSpeed);

            Quaternion desiredRot = Quaternion.LookRotation(
                lookPoint - cam.transform.position, Vector3.up);
            cam.transform.rotation = Quaternion.Slerp(
                cam.transform.rotation, desiredRot,
                Time.unscaledDeltaTime * FollowLerpSpeed);
        }

        private void HandleCameraClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            // Check paddles
            var paddle = hit.transform.GetComponentInParent<PongPaddle>();
            if (paddle != null)
            {
                StopAllCoroutines();
                _cameraSway.enabled = false;
                _cameraFollowTarget = paddle.transform;
                _cameraFollowIsBall = false;
                _followDistance = 10f;
                _followElevation = 5f;
                Log($"Camera → following {paddle.name}");
                return;
            }

            // Check ball
            var ball = hit.transform.GetComponentInParent<PongBall>();
            if (ball != null)
            {
                StopAllCoroutines();
                _cameraSway.enabled = false;
                _cameraFollowTarget = ball.transform;
                _cameraFollowIsBall = true;
                _followDistance = 10f;
                _followElevation = 5f;
                Log("Camera → following Ball");
                return;
            }
        }

        private void HandleCameraEscape()
        {
            if (_cameraFollowTarget == null) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            _cameraFollowTarget = null;
            StartCoroutine(LerpToDefaultView());
        }

        private System.Collections.IEnumerator LerpToDefaultView()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) yield break;

            // Disable sway until lerp completes
            _cameraSway.enabled = false;

            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(Vector3.zero - DefaultCameraPos, Vector3.up);

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Cubic ease-out
                t = 1f - Mathf.Pow(1f - t, 3f);

                cam.transform.position = Vector3.Lerp(startPos, DefaultCameraPos, t);
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            cam.transform.position = DefaultCameraPos;
            cam.transform.LookAt(Vector3.zero, Vector3.up);

            _cameraSway.SetBasePosition(DefaultCameraPos);
            _cameraSway.enabled = true;
            Log("Camera → default sway");
        }

        // =================================================================
        // BOOTSTRAP
        // =================================================================

        private void Start()
        {
            Log("🏓 Pong Bootstrap starting...");

            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);
            Log($"Settings loaded (Quality={SettingsBridge.QualityLevel}, Font={SettingsBridge.FontSize}pt)");

            SetupSimulationTime();
            SetupCamera();
            CreatePalette();
            CreateCourt();
            CreateGoalZones();
            CreatePaddles();
            CreateBall();
            CreateBallTrail();
            CreateMatchManager();
            CreateWarpController();
            CreateAI();

            if (enableScripting) CreatePlayerProgram();
            if (enableLeaderboard) CreateLeaderboard();
            CreateAudio();
            if (enableScripting && enablePersistence) CreatePersistence();
            if (enableScripting) CreateCodeEditor();
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
                Log("SimulationTime already exists.");
                return;
            }

            var go = new GameObject("SimulationTime");
            go.AddComponent<PongSimulationTime>();
            Log("Created PongSimulationTime (engine subclass)");
        }

        // =================================================================
        // CAMERA — perspective 3D view of the court
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

            // Perspective 3D view — looking down at the court
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.transform.position = DefaultCameraPos;
            cam.transform.LookAt(Vector3.zero, Vector3.up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            // CameraAmbientMotion — default gentle sway
            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = Vector3.zero;

            Log($"Camera: perspective, FOV=60, 3D view + click-to-follow + sway");
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
            _court.Initialize(_palette);
            Log($"Created Court ({courtWidth}×{courtHeight}) via ProceduralAssembler");
        }

        // =================================================================
        // PADDLES
        // =================================================================

        private void CreatePaddles()
        {
            float halfW = courtWidth / 2f;

            // Left paddle — player's CODE controls this
            var leftGo = new GameObject("LeftPaddle (CODE)");
            leftGo.transform.position = new Vector3(-halfW + paddleOffset, 0f, 0f);
            _leftPaddle = leftGo.AddComponent<PongPaddle>();
            _leftPaddle.Initialize(paddleHeight, paddleThickness, courtHeight, PaddleSide.Left);

            var leftBlueprint = new PongPaddleBlueprint(paddleHeight, paddleThickness, PaddleSide.Left);
            _leftPaddleVisual = ProceduralAssembler.BuildWithVisualState(leftBlueprint, _palette);
            if (_leftPaddleVisual.Root != null)
                _leftPaddleVisual.Root.transform.SetParent(leftGo.transform, false);

            // Right paddle — AI opponent
            var rightGo = new GameObject("RightPaddle (AI)");
            rightGo.transform.position = new Vector3(halfW - paddleOffset, 0f, 0f);
            _rightPaddle = rightGo.AddComponent<PongPaddle>();
            _rightPaddle.Initialize(paddleHeight, paddleThickness, courtHeight, PaddleSide.Right);

            var rightBlueprint = new PongPaddleBlueprint(paddleHeight, paddleThickness, PaddleSide.Right);
            _rightPaddleVisual = ProceduralAssembler.BuildWithVisualState(rightBlueprint, _palette);
            if (_rightPaddleVisual.Root != null)
                _rightPaddleVisual.Root.transform.SetParent(rightGo.transform, false);

            Log("Created 3D Paddles (Left=CODE, Right=AI) via ProceduralAssembler");
        }

        // =================================================================
        // BALL
        // =================================================================

        private void CreateBall()
        {
            var go = new GameObject("Ball");
            _ball = go.AddComponent<PongBall>();
            _ball.Initialize(ballStartSpeed, ballMaxSpeed, ballSpeedIncrease,
                             ballRadius, maxBounceAngle, courtWidth, courtHeight);

            var ballBlueprint = new PongBallBlueprint(ballRadius);
            _ballVisual = ProceduralAssembler.BuildWithVisualState(ballBlueprint, _palette);
            if (_ballVisual.Root != null)
                _ballVisual.Root.transform.SetParent(go.transform, false);

            Log("Created 3D Ball (Sphere) via ProceduralAssembler");
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
        // COLOR PALETTE (Procedural — .engine)
        // =================================================================

        private void CreatePalette()
        {
            var colors = new Dictionary<string, Color>
            {
                { "wall",           Color.white },
                { "paddle_player",  new Color(0f, 1f, 1f) },         // cyan
                { "paddle_ai",     new Color(1f, 0.2f, 0.8f) },     // magenta
                { "ball",          new Color(1f, 1f, 0.3f) },        // bright yellow
                { "court_line",    new Color(0.3f, 0.3f, 0.4f) },    // dim blue-gray
                { "court_floor",   new Color(0.02f, 0.02f, 0.05f) }, // near-black
                { "goal_player",   new Color(0f, 1f, 1f, 0.15f) },    // translucent cyan
                { "goal_ai",       new Color(1f, 0.2f, 0.8f, 0.15f) }, // translucent magenta
                { "ball_trail",    new Color(1f, 1f, 0.3f, 0.5f) }     // fading yellow
            };
            _palette = ColorPalette.CreateRuntime(colors);
            Log("Created neon arcade ColorPalette");
        }

        // =================================================================
        // AUDIO (.engine powered)
        // =================================================================

        private void CreateAudio()
        {
            _audioProvider = new PongAudioProvider();
            System.Func<float> getTimeScale = () => SimulationTime.Instance?.timeScale ?? 1f;

            _engineAudio = AudioBridge.ForEngine(_audioProvider, getTimeScale);

            // Haptic bridge
            _hapticProvider = new PongHapticProvider();
            _engineHaptic = HapticBridge.ForEngine(_hapticProvider, getTimeScale);

            Log("Created AudioBridge + HapticBridge (synth tones + haptic)");
        }

        // =================================================================
        // PERSISTENCE (.engine powered)
        // =================================================================

        private void CreatePersistence()
        {
            var go = new GameObject("Persistence");
            _persistence = go.AddComponent<PongPersistenceManager>();

            IGitRepository repo;
            string providerName;
            if (useLocalGitProvider)
            {
                string fullPath = System.IO.Path.GetFullPath(dataPath);
                var localRepo = new LocalGitProvider(fullPath);
                localRepo.EnsureInitialized();
                repo = localRepo;
                providerName = "LocalGitProvider";
            }
            else
            {
                repo = new MemoryGitProvider();
                providerName = "MemoryGitProvider";
            }

            _persistence.Initialize(repo, _playerProgram);
            _persistence.autosaveInterval = 30f;
            _persistence.syncInterval = useLocalGitProvider ? 300f : 0f;

            // Wire direct code-change callback
            if (_playerProgram != null)
                _playerProgram.OnCodeChanged += () => _persistence?.OnCodeUploaded();

            Log($"Created PongPersistenceManager ({providerName})");
        }

        // =================================================================
        // GOAL ZONES (Procedural — .engine)
        // =================================================================

        private void CreateGoalZones()
        {
            float halfW = courtWidth / 2f;
            float depth = 0.5f;

            // Left goal zone — behind player's paddle
            var leftBlueprint = new PongGoalZoneBlueprint(courtHeight, depth, PaddleSide.Left);
            _leftGoalVisual = ProceduralAssembler.BuildWithVisualState(leftBlueprint, _palette);
            if (_leftGoalVisual.Root != null)
                _leftGoalVisual.Root.transform.position = new Vector3(-halfW - 0.5f, 0f, 0f);

            // Right goal zone — behind AI's paddle
            var rightBlueprint = new PongGoalZoneBlueprint(courtHeight, depth, PaddleSide.Right);
            _rightGoalVisual = ProceduralAssembler.BuildWithVisualState(rightBlueprint, _palette);
            if (_rightGoalVisual.Root != null)
                _rightGoalVisual.Root.transform.position = new Vector3(halfW + 0.5f, 0f, 0f);

            Log("Created 3D Goal Zones (translucent glow planes)");
        }

        // =================================================================
        // WARP CONTROLLER (.engine Time)
        // =================================================================

        private void CreateWarpController()
        {
            var go = new GameObject("WarpController");
            _warpController = go.AddComponent<PongWarpController>();
            _warpController.Initialize(_match);
            Log("Created PongWarpController ([W] to warp 10 matches)");
        }

        // =================================================================
        // BALL TRAIL
        // =================================================================

        private void CreateBallTrail()
        {
            var go = new GameObject("BallTrail");
            _ballTrail = go.AddComponent<PongBallTrail>();
            _ballTrail.Initialize(_ball, _palette);
            Log("Created BallTrail (fading spheres)");
        }

        // =================================================================
        // CODE EDITOR (.engine Editor)
        // =================================================================

        private void CreateCodeEditor()
        {
            _compilerExt = new PongCompilerExtension();
            _editorExt = new PongEditorExtension();
            Log("Created PongEditorExtension (ready for CodeEditorWindow)");
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
                    _audioProvider?.PlayMatchWon();
                    _hapticProvider?.TapHeavy();

                    if (_leaderboard != null)
                    {
                        bool playerWon = (winner == PaddleSide.Left);
                        _leaderboard.RecordMatch(aiDifficulty, playerWon,
                            _match.LeftScore, _match.RightScore);
                    }
                };

                _match.OnServe += () =>
                {
                    _audioProvider?.PlayServe();

                    // Ball respawn pulse
                    _ballVisual.VisualState?.Pulse("body", new Color(1f, 1f, 0.5f), 0.3f);
                    _ballVisual.VisualState?.Throb("body", 1.4f, 0.25f);
                };
            }

            // ── Procedural visual effects + audio + haptic ──
            if (_ball != null)
            {
                _ball.OnPaddleHit += (side) =>
                {
                    if (side == PaddleSide.Left && _leftPaddleVisual.VisualState != null)
                        _leftPaddleVisual.VisualState.Pulse("body", Color.cyan, 0.15f);
                    else if (side == PaddleSide.Right && _rightPaddleVisual.VisualState != null)
                        _rightPaddleVisual.VisualState.Pulse("body", new Color(1f, 0.2f, 0.8f), 0.15f);

                    // Ball reacts to paddle hit
                    _ballVisual.VisualState?.Pulse("body", Color.white, 0.12f);
                    _ballVisual.VisualState?.Throb("body", 1.3f, 0.15f);

                    _audioProvider?.PlayPaddleHit();
                    _hapticProvider?.TapMedium();
                };

                _ball.OnWallHit += () =>
                {
                    var courtVS = _court?.Visual.VisualState;
                    if (courtVS != null)
                    {
                        courtVS.Pulse("wall_top", Color.white, 0.1f);
                        courtVS.Pulse("wall_bottom", Color.white, 0.1f);
                    }

                    // Ball reacts to wall hit
                    _ballVisual.VisualState?.Pulse("body", Color.white, 0.1f);
                    _ballVisual.VisualState?.Throb("body", 1.2f, 0.12f);

                    _audioProvider?.PlayWallBounce();
                    _hapticProvider?.TapLight();
                };

                _ball.OnGoalScored += (scorer) =>
                {
                    _audioProvider?.PlayGoalScored();
                    _hapticProvider?.TapHeavy();

                    // Ball flash on score
                    _ballVisual.VisualState?.Pulse("body", Color.yellow, 0.3f);
                    _ballVisual.VisualState?.Throb("body", 1.5f, 0.25f);

                    // Flash the scoring paddle + goal zone
                    if (scorer == PaddleSide.Left)
                    {
                        _leftPaddleVisual.VisualState?.Pulse("body", Color.white, 0.3f);
                        _leftGoalVisual.VisualState?.Pulse("zone", Color.cyan, 0.5f);
                        _leftGoalVisual.VisualState?.Throb("zone", 1.15f, 0.4f);
                    }
                    else
                    {
                        _rightPaddleVisual.VisualState?.Pulse("body", Color.white, 0.3f);
                        _rightGoalVisual.VisualState?.Pulse("zone", new Color(1f, 0.2f, 0.8f), 0.5f);
                        _rightGoalVisual.VisualState?.Throb("zone", 1.15f, 0.4f);
                    }
                };
            }

            // ── Ball speed → emission glow binding ──
            if (_ballVisual.VisualState != null && _ball != null)
            {
                _ballVisual.VisualState.Bind("body",
                    () => _ball.CurrentSpeed / ballMaxSpeed,
                    VisualChannel.Emission, 0f, 2f);
            }

            // ── Audio: engine instruction step sounds ──
            if (_playerProgram?.Executor != null)
            {
                _playerProgram.Executor.OnHalted += _engineAudio.Halted;
                _playerProgram.Executor.OnHalted += _engineHaptic.Halted;
            }

            // ── Warp audio hooks ──
            if (_warpController != null)
            {
                var timeAudio = AudioBridge.ForTime(
                    _audioProvider, () => SimulationTime.Instance?.timeScale ?? 1f);
                _warpController.OnWarpArrived += timeAudio.WarpArrived;
                _warpController.OnWarpCancelled += timeAudio.WarpCancelled;
                _warpController.OnWarpComplete += timeAudio.WarpComplete;
            }

            // ── Persistence: mark dirty on every code upload ──
            if (_persistence != null && _match != null)
            {
                _match.OnMatchEnded += (_) => _persistence?.OnCodeUploaded();
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
            Log("🏓 PONG 3D — Write Code. Beat AI. Level Up.");
            Log("────────────────────────────────────────");
            Log($"  COURT │ {courtWidth}×{courtHeight} (3D ProceduralAssembler)");
            Log($"  BALL  │ speed={ballStartSpeed}, max={ballMaxSpeed} (3D Sphere + trail)");
            Log($"  MATCH │ first to {pointsToWin}");
            Log($"  AI    │ {aiDifficulty}");
            Log($"  TIME  │ {SimulationTime.Instance?.GetFormattedTimeScale() ?? "1x"} (engine SimulationTime)");
            Log("────────────────────────────────────────");
            Log($"  Scripting    │ {(enableScripting ? "✅ ACTIVE" : "── disabled")}");
            Log($"  TUI          │ {(enableTUI ? "✅ ACTIVE" : "── disabled")}");
            Log($"  Leaderboard  │ {(enableLeaderboard ? "✅ ACTIVE" : "── disabled")}");
            Log($"  Audio        │ ✅ Synth tones + HapticBridge");
            Log($"  Persistence  │ {(enablePersistence ? (useLocalGitProvider ? "✅ LocalGitProvider" : "✅ MemoryGitProvider") : "── disabled")}");
            Log($"  Procedural   │ ✅ 3D court/paddles/ball/goals/trail");
            Log($"  Warp         │ ✅ [W] warp 10 matches");
            Log($"  Editor       │ ✅ PongEditorExtension ready");
            Log($"  Camera       │ ✅ Click paddle/ball to follow, [ESC] sway");
            Log($"  Settings     │ ✅ SettingsBridge (Quality={SettingsBridge.QualityLevel}, Font={SettingsBridge.FontSize}pt)");
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
