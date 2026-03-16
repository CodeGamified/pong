// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using CodeGamified.Time;
using Pong.Game;

namespace Pong.Scripting
{
    /// <summary>
    /// PaddleProgram — code-controlled paddle (player or AI).
    /// Subclasses ProgramBehaviour from .engine.
    ///
    /// EXECUTION MODEL (tick-based, deterministic):
    ///   - Each simulation tick (~60/sec sim-time), the script runs from the top
    ///   - Fixed instruction budget per tick (CYCLES_PER_TICK)
    ///   - Memory (variables) persists across ticks
    ///   - PC resets to 0 each tick — no while loop needed
    ///   - If script doesn't finish within budget, remaining code is skipped
    ///   - Smarter scripts win by fitting strategy in fewer instructions
    ///   - Results are IDENTICAL at 0.5x, 1x, 100x, 1000x speed
    ///
    /// BUILTINS:
    ///   get_ball_x/y()       → ball position
    ///   get_ball_vx/vy()     → ball velocity
    ///   get_paddle_x/y()     → this paddle
    ///   get_opponent_y()     → opponent paddle Y
    ///   set_target_y(y)      → move paddle toward Y
    /// </summary>
    public class PaddleProgram : ProgramBehaviour
    {
        private PongPaddle _paddle;
        private PongBall _ball;
        private PongCourt _court;
        private PongIOHandler _ioHandler;
        private PongCompilerExtension _compilerExt;

        // Execution rate — THE core gameplay constraint
        public const float OPS_PER_SECOND = 20f;
        private float _opAccumulator;

        // Default starter code
        private const string DEFAULT_CODE = @"# 🏓 PONG — Write your paddle AI!
# Your script runs at 20 ops/sec (sim-time).
# When it finishes, it restarts from the top.
# Variables persist — use them to track state.
# The game IS the code. Efficiency wins.
#
# BUILTINS:
#   get_ball_x/y()      → ball position
#   get_ball_vx/vy()    → ball velocity
#   get_paddle_x/y()    → your paddle
#   get_opponent_y()    → opponent Y
#   get_input_y()       → keyboard input (-1/0/1)
#   get_mouse_y()       → mouse Y (world space)
#   set_target_y(y)     → move to Y
#   move_target_y(dy)   → nudge target by dy
#
# This 2-line script uses ~8 ops per pass:
ball_y = get_ball_y()
set_target_y(ball_y)
";

        public const string USER_CONTROLLED_CODE = @"# USER CONTROLLED — Keyboard input (~2 ops)
# W/S or UpArrow/DownArrow to move.
move_target_y(get_input_y())
";

        public const string MOUSE_CONTROLLED_CODE = @"# MOUSE CONTROLLED — Follow the mouse (~2 ops)
set_target_y(get_mouse_y())
";

        public string CurrentSourceCode => _sourceCode;

        // Persistence callback — set by bootstrap to trigger autosave on code change
        public System.Action OnCodeChanged;

        public void Initialize(PongPaddle paddle, PongBall ball, PongCourt court,
                               string initialCode = null, string programName = "PaddleAI")
        {
            _paddle = paddle;
            _ball = ball;
            _court = court;
            _compilerExt = new PongCompilerExtension();

            _programName = programName;
            _sourceCode = initialCode ?? DEFAULT_CODE;
            _autoRun = true;

            // Initial compile
            LoadAndRun(_sourceCode);
        }

        /// <summary>
        /// Override Update — drip-feed instructions at OPS_PER_SECOND.
        /// When script reaches end (HALT), PC resets to 0. Memory persists.
        /// Deterministic: same sim-time = same ops executed regardless of time scale.
        /// </summary>
        protected override void Update()
        {
            if (_executor == null || _program == null || _isPaused) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = UnityEngine.Time.deltaTime * timeScale;
            _opAccumulator += simDelta * OPS_PER_SECOND;

            // Execute accrued ops one at a time
            int opsToRun = (int)_opAccumulator;
            _opAccumulator -= opsToRun;

            for (int i = 0; i < opsToRun; i++)
            {
                // If halted (end of script), restart from top
                if (_executor.State.IsHalted)
                {
                    _executor.State.PC = 0;
                    _executor.State.IsHalted = false;
                }

                _executor.ExecuteOne();
            }

            if (opsToRun > 0)
                ProcessEvents();
        }

        protected override IGameIOHandler CreateIOHandler()
        {
            _ioHandler = new PongIOHandler(_paddle, _ball, _court);
            return _ioHandler;
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            // Compile straight — no while wrapper, no wait.
            // Script runs top-to-bottom each tick with a fixed instruction budget.
            return PythonCompiler.Compile(source, name, _compilerExt);
        }

        protected override void ProcessEvents()
        {
            if (_executor?.State == null) return;

            // Update score references
            if (_ioHandler != null)
            {
                var match = FindAnyObjectByType<PongMatchManager>();
                if (match != null)
                {
                    _ioHandler.PlayerScore = match.LeftScore;
                    _ioHandler.OpponentScore = match.RightScore;
                }
            }

            // Drain output events
            while (_executor.State.OutputEvents.Count > 0)
                _executor.State.OutputEvents.Dequeue();
        }

        /// <summary>Upload new code (called from TUI editor). Pass null to reset to default.</summary>
        public void UploadCode(string newSource)
        {
            _sourceCode = newSource ?? DEFAULT_CODE;
            LoadAndRun(_sourceCode);
            Debug.Log($"[PaddleAI] Uploaded new code ({_program?.Instructions?.Length ?? 0} instructions)");
            OnCodeChanged?.Invoke();
        }

        /// <summary>Reset execution state (PC, registers, memory) without recompiling.</summary>
        public void ResetExecution()
        {
            if (_executor?.State == null) return;
            _executor.State.Reset();
            _opAccumulator = 0f;
        }
    }
}

