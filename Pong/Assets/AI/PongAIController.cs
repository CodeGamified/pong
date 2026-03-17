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
                    return @"# ""Tracker""
set_target_y(get_ball_y())";

                case AIDifficulty.Medium:
                    return @"# ""Anticipator""
# Lookahead
set_target_y(
    get_ball_y() + get_ball_vy() * 0.3)";

                case AIDifficulty.Hard:
                    return @"# ""Predictor""
# Predict arrival
set_target_y(
    get_ball_y() +
    get_ball_vy() * (
        (get_paddle_x() - get_ball_x())
        / get_ball_vx()))";

                case AIDifficulty.Expert:
                    return @"# ""Strategist""
serve:
    set_target_y(
        get_ball_y() + get_ball_vy() 
        * 
        (get_paddle_x() - get_ball_x()) 
        / 
        get_ball_vx())
hit_opp:
    m = (get_ball_y() + get_ball_vy() *
        (get_paddle_x() - get_ball_x())
        / get_ball_vx() + get_court_height() / 2) % (get_court_height() * 2)
    predict = min(m, get_court_height() * 2 - m) - get_court_height() / 2
    set_target_y(predict + get_opponent_y() * 0.3)";
                default:
                    return "# Unknown difficulty";
            }
        }
    }
}
