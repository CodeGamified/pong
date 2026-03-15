// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using Pong.Game;
using Pong.Scripting;

namespace Pong.AI
{
    /// <summary>
    /// AI paddle controller — runs the SAME bytecode engine as the player.
    /// Each difficulty tier is a Python script compiled + executed by PaddleProgram.
    /// Changing difficulty reloads a different script. No special C# logic.
    /// </summary>
    public class PongAIController : MonoBehaviour
    {
        private PongPaddle _paddle;
        private PongBall _ball;
        private PongCourt _court;
        private AIDifficulty _difficulty;
        private PaddleProgram _program;

        public AIDifficulty Difficulty => _difficulty;

        /// <summary>The live PaddleProgram — same type as the player's. Used by debugger.</summary>
        public PaddleProgram Program => _program;

        public void Initialize(PongPaddle paddle, PongBall ball, PongCourt court, AIDifficulty difficulty)
        {
            _paddle = paddle;
            _ball = ball;
            _court = court;

            // Create a PaddleProgram on the same GameObject — identical to player's
            _program = gameObject.AddComponent<PaddleProgram>();
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(AIDifficulty difficulty)
        {
            _difficulty = difficulty;

            // Configure paddle speed per tier
            switch (difficulty)
            {
                case AIDifficulty.Easy:   _paddle.moveSpeed = 5f;  break;
                case AIDifficulty.Medium: _paddle.moveSpeed = 8f;  break;
                case AIDifficulty.Hard:   _paddle.moveSpeed = 11f; break;
                case AIDifficulty.Expert: _paddle.moveSpeed = 13f; break;
            }

            // Load the tier's Python script into the bytecode engine
            string code = GetSampleCode(difficulty);
            _program.Initialize(_paddle, _ball, _court, code, $"AI_{difficulty}");

            Debug.Log($"[AI] Difficulty → {difficulty} (running bytecode)");
        }

        // =================================================================
        // SAMPLE CODE — the actual AI logic, written in the same Python
        // subset the player uses. What you see IS what runs.
        // =================================================================

        public static string GetSampleCode(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return @"# EASY — ""The Tracker"" (~8 ops)
# ~2.5 passes/sec at 20 ops/s
ball_y = get_ball_y()
set_target_y(ball_y)";

                case AIDifficulty.Medium:
                    return @"# MEDIUM — ""The Anticipator"" (~12 ops)
# Lookahead with velocity
ball_y = get_ball_y()
ball_vy = get_ball_vy()
set_target_y(ball_y + ball_vy * 0.3)";

                case AIDifficulty.Hard:
                    return @"# HARD — ""The Predictor"" (~18 ops)
# Predict arrival Y. Tight code = fast updates.
ball_x = get_ball_x()
ball_vx = get_ball_vx()
t = (get_paddle_x() - ball_x) / ball_vx
py = get_ball_y() + get_ball_vy() * t
if py > 4.75:
    py = 9.5 - py
if py < -4.75:
    py = -9.5 - py
set_target_y(py)";

                case AIDifficulty.Expert:
                    return @"# EXPERT — ""The Strategist"" (~24 ops)
# Predict + aim away from opponent
ball_x = get_ball_x()
ball_vx = get_ball_vx()
t = (get_paddle_x() - ball_x) / ball_vx
py = get_ball_y() + get_ball_vy() * t
if py > 4.75:
    py = 9.5 - py
if py < -4.75:
    py = -9.5 - py
opp_y = get_opponent_y()
if opp_y > 0:
    py = py - 0.5
if opp_y < 0:
    py = py + 0.5
set_target_y(py)";

                default:
                    return "# Unknown difficulty";
            }
        }
    }
}
