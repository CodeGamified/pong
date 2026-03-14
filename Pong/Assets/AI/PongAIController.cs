// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using Pong.Core;
using Pong.Game;

namespace Pong.AI
{
    /// <summary>
    /// AI paddle controller with 4 difficulty tiers.
    /// These are also SAMPLE SCRIPTS — players can study them to learn strategies.
    ///
    /// EASY:    Lazy tracker — follows ball Y with reaction delay and low speed.
    /// MEDIUM:  Decent tracker — faster reactions, slight anticipation.
    /// HARD:    Predictive — calculates where ball will arrive, positions early.
    /// EXPERT:  Near-perfect — full trajectory prediction + strategic offset.
    /// </summary>
    public class PongAIController : MonoBehaviour
    {
        private PongPaddle _paddle;
        private PongBall _ball;
        private PongCourt _court;
        private AIDifficulty _difficulty;

        // AI tuning
        private float _reactionDelay;
        private float _speedMultiplier;
        private float _predictionAccuracy;
        private float _errorMargin;
        private float _timeSinceLastDecision;
        private float _decisionInterval;
        private float _lastTargetY;

        public AIDifficulty Difficulty => _difficulty;

        public void Initialize(PongPaddle paddle, PongBall ball, PongCourt court, AIDifficulty difficulty)
        {
            _paddle = paddle;
            _ball = ball;
            _court = court;
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(AIDifficulty difficulty)
        {
            _difficulty = difficulty;

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    _reactionDelay = 0.4f;
                    _speedMultiplier = 0.5f;
                    _predictionAccuracy = 0f;
                    _errorMargin = 1.5f;
                    _decisionInterval = 0.3f;
                    _paddle.moveSpeed = 5f;
                    break;

                case AIDifficulty.Medium:
                    _reactionDelay = 0.2f;
                    _speedMultiplier = 0.75f;
                    _predictionAccuracy = 0.3f;
                    _errorMargin = 0.8f;
                    _decisionInterval = 0.15f;
                    _paddle.moveSpeed = 8f;
                    break;

                case AIDifficulty.Hard:
                    _reactionDelay = 0.08f;
                    _speedMultiplier = 0.95f;
                    _predictionAccuracy = 0.75f;
                    _errorMargin = 0.3f;
                    _decisionInterval = 0.05f;
                    _paddle.moveSpeed = 11f;
                    break;

                case AIDifficulty.Expert:
                    _reactionDelay = 0.02f;
                    _speedMultiplier = 1f;
                    _predictionAccuracy = 0.95f;
                    _errorMargin = 0.1f;
                    _decisionInterval = 0.02f;
                    _paddle.moveSpeed = 13f;
                    break;
            }
        }

        private void Update()
        {
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;
            if (!_ball.IsActive) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
            _timeSinceLastDecision += dt;

            if (_timeSinceLastDecision < _decisionInterval) return;
            _timeSinceLastDecision = 0f;

            float targetY = ComputeTarget();
            _lastTargetY = Mathf.Lerp(_lastTargetY, targetY, _speedMultiplier);
            _paddle.SetTargetY(_lastTargetY);
        }

        private float ComputeTarget()
        {
            Vector2 ballPos = _ball.Position;
            Vector2 ballVel = _ball.Velocity;

            // If ball moving away, return to center (all AI levels)
            bool movingToward = (_paddle.Side == PaddleSide.Right && ballVel.x > 0) ||
                                (_paddle.Side == PaddleSide.Left && ballVel.x < 0);

            if (!movingToward)
                return Mathf.Lerp(_paddle.currentY, 0f, 0.3f);

            // ── EASY: just track ball Y ──
            if (_difficulty == AIDifficulty.Easy)
                return ballPos.y + Random.Range(-_errorMargin, _errorMargin);

            // ── MEDIUM+: blend between tracking and prediction ──
            float predictedY = PredictBallArrivalY(ballPos, ballVel);
            float trackedY = ballPos.y;
            float target = Mathf.Lerp(trackedY, predictedY, _predictionAccuracy);

            // Add controlled error
            target += Random.Range(-_errorMargin, _errorMargin);

            // ── EXPERT: strategic offset to control bounce angle ──
            if (_difficulty == AIDifficulty.Expert)
            {
                // Aim to return ball at an angle the opponent can't reach
                float offset = Mathf.Sign(Random.Range(-1f, 1f)) * _paddle.HalfPaddleH * 0.3f;
                target += offset;
            }

            return target;
        }

        /// <summary>
        /// Predict where the ball will be when it reaches this paddle's X.
        /// Simulates wall bounces. This is the key algorithm players need to discover.
        /// </summary>
        private float PredictBallArrivalY(Vector2 pos, Vector2 vel)
        {
            if (Mathf.Abs(vel.x) < 0.01f) return pos.y;

            float targetX = _paddle.transform.position.x;
            float timeToArrive = (targetX - pos.x) / vel.x;
            if (timeToArrive < 0) return pos.y;

            // Simulate Y position with wall bounces
            float simY = pos.y + vel.y * timeToArrive;
            float halfH = _court.HalfHeight - 0.25f; // Account for wall/ball

            // Reflect off walls
            while (simY > halfH || simY < -halfH)
            {
                if (simY > halfH)
                    simY = 2f * halfH - simY;
                else if (simY < -halfH)
                    simY = -2f * halfH - simY;
            }

            return simY;
        }

        // =================================================================
        // SAMPLE CODE (Python-like pseudocode the player can learn from)
        // These strings are displayed in the TUI as learning examples.
        // =================================================================

        public static string GetSampleCode(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return @"# EASY AI — ""The Tracker""
# Just follow the ball's Y position. Simple but slow.
#
ball_y = get_ball_y()
set_target_y(ball_y)";

                case AIDifficulty.Medium:
                    return @"# MEDIUM AI — ""The Anticipator""
# Track ball, but also look at velocity to anticipate.
#
ball_y = get_ball_y()
ball_vy = get_ball_vy()
predicted = ball_y + ball_vy * 0.3
set_target_y(predicted)";

                case AIDifficulty.Hard:
                    return @"# HARD AI — ""The Predictor""
# Calculate where ball will arrive at our paddle X.
# Account for wall bounces.
#
ball_x = get_ball_x()
ball_y = get_ball_y()
ball_vx = get_ball_vx()
ball_vy = get_ball_vy()
paddle_x = get_paddle_x()

time_to_arrive = (paddle_x - ball_x) / ball_vx
predicted_y = ball_y + ball_vy * time_to_arrive

# Reflect off walls (court half-height ~4.75)
while predicted_y > 4.75:
    predicted_y = 9.5 - predicted_y
while predicted_y < -4.75:
    predicted_y = -9.5 - predicted_y

set_target_y(predicted_y)";

                case AIDifficulty.Expert:
                    return @"# EXPERT AI — ""The Strategist""
# Perfect prediction + offensive positioning.
# Aim to return ball where opponent CAN'T reach.
#
ball_x = get_ball_x()
ball_y = get_ball_y()
ball_vx = get_ball_vx()
ball_vy = get_ball_vy()
paddle_x = get_paddle_x()
paddle_y = get_paddle_y()
opp_y = get_opponent_y()

# Predict arrival
time_to_arrive = (paddle_x - ball_x) / ball_vx
predicted_y = ball_y + ball_vy * time_to_arrive

while predicted_y > 4.75:
    predicted_y = 9.5 - predicted_y
while predicted_y < -4.75:
    predicted_y = -9.5 - predicted_y

# Strategic offset: hit ball AWAY from opponent
offset = 0.0
if opp_y > 0:
    offset = -0.5
if opp_y < 0:
    offset = 0.5

set_target_y(predicted_y + offset)";

                default:
                    return "# Unknown difficulty";
            }
        }
    }
}
