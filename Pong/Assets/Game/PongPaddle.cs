// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Time;

namespace Pong.Game
{
    /// <summary>
    /// A Pong paddle. Moves along Y axis.
    /// Controlled by code (player) or AI — never by WASD.
    /// 
    /// The API exposed to player scripts:
    ///   - get_ball_y()      → ball Y position
    ///   - get_ball_x()      → ball X position
    ///   - get_ball_vy()     → ball Y velocity
    ///   - get_ball_vx()     → ball X velocity
    ///   - get_paddle_y()    → this paddle's Y position
    ///   - set_target_y(y)   → move paddle toward Y
    ///   - get_score()       → player's current score
    ///   - get_opponent_score() → opponent's score
    /// </summary>
    public class PongPaddle : MonoBehaviour
    {
        public PaddleSide Side { get; private set; }
        public float PaddleHeight { get; private set; }
        public float Thickness { get; private set; }
        public float CourtHeight { get; private set; }
        public float HalfPaddleH => PaddleHeight / 2f;
        public float HalfCourtH => CourtHeight / 2f;

        // Movement
        public float moveSpeed = 12f;
        public float targetY = 0f;
        public float currentY = 0f;

        public void Initialize(float height, float thickness, float courtH, PaddleSide side)
        {
            PaddleHeight = height;
            Thickness = thickness;
            CourtHeight = courtH;
            Side = side;
        }

        private void Update()
        {
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
            float clampedTarget = Mathf.Clamp(targetY, -HalfCourtH + HalfPaddleH, HalfCourtH - HalfPaddleH);
            currentY = Mathf.MoveTowards(currentY, clampedTarget, moveSpeed * dt);
            currentY = Mathf.Clamp(currentY, -HalfCourtH + HalfPaddleH, HalfCourtH - HalfPaddleH);

            var pos = transform.position;
            pos.y = currentY;
            transform.position = pos;
        }

        /// <summary>Set movement target (called by code/AI).</summary>
        public void SetTargetY(float y)
        {
            targetY = y;
        }
    }
}
