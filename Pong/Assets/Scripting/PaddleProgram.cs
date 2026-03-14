// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using Pong.Game;

namespace Pong.Scripting
{
    /// <summary>
    /// PaddleProgram — the player's code-controlled paddle.
    /// Subclasses ProgramBehaviour from .engine.
    ///
    /// The player writes Python-like code using these builtins:
    ///   get_ball_x()         → ball X position
    ///   get_ball_y()         → ball Y position
    ///   get_ball_vx()        → ball X velocity
    ///   get_ball_vy()        → ball Y velocity
    ///   get_paddle_y()       → this paddle's Y
    ///   get_paddle_x()       → this paddle's X
    ///   get_score()          → player's score
    ///   get_opponent_score() → AI's score
    ///   get_opponent_y()     → AI paddle Y
    ///   set_target_y(y)      → move paddle to Y
    ///
    /// The code loops every frame. Write your strategy!
    /// </summary>
    public class PaddleProgram : ProgramBehaviour
    {
        private PongPaddle _paddle;
        private PongBall _ball;
        private PongCourt _court;
        private PongIOHandler _ioHandler;
        private PongCompilerExtension _compilerExt;

        // Default starter code
        private const string DEFAULT_CODE = @"# 🏓 PONG — Write your paddle AI!
# Your code runs every frame in a loop.
#
# AVAILABLE FUNCTIONS:
#   get_ball_x()         → ball X position
#   get_ball_y()         → ball Y position  
#   get_ball_vx()        → ball X velocity
#   get_ball_vy()        → ball Y velocity
#   get_paddle_y()       → your paddle Y
#   get_opponent_y()     → AI paddle Y
#   set_target_y(y)      → move your paddle to Y
#
# START SIMPLE — just track the ball:
ball_y = get_ball_y()
set_target_y(ball_y)
";

        public string CurrentSourceCode => _sourceCode;

        public void Initialize(PongPaddle paddle, PongBall ball, PongCourt court)
        {
            _paddle = paddle;
            _ball = ball;
            _court = court;
            _compilerExt = new PongCompilerExtension();

            _programName = "PaddleAI";
            _sourceCode = DEFAULT_CODE;
            _autoRun = true;
            _stepDelay = 0.016f;           // ~60fps execution
            _stepThroughThreshold = 2f;    // Step-through below 2x

            // Initial compile
            LoadAndRun(_sourceCode);
        }

        protected override IGameIOHandler CreateIOHandler()
        {
            _ioHandler = new PongIOHandler(_paddle, _ball, _court);
            return _ioHandler;
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            // Wrap in while True loop so code runs every frame
            string wrappedSource = $"while True:\n{IndentSource(source)}\n    wait(0.016)";
            return PythonCompiler.Compile(wrappedSource, name, _compilerExt);
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

            // Drain output events (Pong doesn't produce audio, but engine expects this)
            while (_executor.State.OutputEvents.Count > 0)
                _executor.State.OutputEvents.Dequeue();
        }

        /// <summary>Upload new code (called from TUI editor).</summary>
        public void UploadCode(string newSource)
        {
            _sourceCode = newSource;
            LoadAndRun(newSource);
            Debug.Log($"[PaddleAI] Uploaded new code ({_program?.Instructions?.Length ?? 0} instructions)");
        }

        /// <summary>Indent each line of source for wrapping inside while True:</summary>
        private string IndentSource(string source)
        {
            var lines = source.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            var sb = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                {
                    sb.AppendLine($"    {line}");
                    continue;
                }
                sb.AppendLine($"    {line}");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
