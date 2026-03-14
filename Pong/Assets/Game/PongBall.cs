// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Time;

namespace Pong.Game
{
    /// <summary>
    /// The Pong ball. Full physics: wall bounce, paddle collision, speed ramp.
    /// Time-scale aware — runs at warp speed for batch testing.
    /// </summary>
    public class PongBall : MonoBehaviour
    {
        // Config
        private float _startSpeed;
        private float _maxSpeed;
        private float _speedIncrease;
        private float _radius;
        private float _maxBounceAngle;
        private float _courtWidth;
        private float _courtHeight;

        // State
        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public float CurrentSpeed { get; private set; }
        public bool IsActive { get; private set; }

        // Events
        public System.Action<PaddleSide> OnGoalScored;
        public System.Action<PaddleSide> OnPaddleHit;
        public System.Action OnWallHit;

        // References (set after creation)
        public PongPaddle LeftPaddle { get; set; }
        public PongPaddle RightPaddle { get; set; }

        public float HalfW => _courtWidth / 2f;
        public float HalfH => _courtHeight / 2f;

        public void Initialize(float startSpeed, float maxSpeed, float speedIncrease,
                               float radius, float maxBounceAngle,
                               float courtWidth, float courtHeight)
        {
            _startSpeed = startSpeed;
            _maxSpeed = maxSpeed;
            _speedIncrease = speedIncrease;
            _radius = radius;
            _maxBounceAngle = maxBounceAngle;
            _courtWidth = courtWidth;
            _courtHeight = courtHeight;

            Position = Vector2.zero;
            Velocity = Vector2.zero;
            IsActive = false;
        }

        /// <summary>Serve the ball from center toward a random direction.</summary>
        public void Serve(PaddleSide toward)
        {
            Position = Vector2.zero;
            transform.position = new Vector3(0f, 0f, 0f);

            float angle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            float dirX = (toward == PaddleSide.Right) ? 1f : -1f;
            Velocity = new Vector2(dirX * Mathf.Cos(angle), Mathf.Sin(angle)).normalized * _startSpeed;
            CurrentSpeed = _startSpeed;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
            Velocity = Vector2.zero;
            Position = Vector2.zero;
            transform.position = Vector3.zero;
        }

        private void Update()
        {
            if (!IsActive) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);

            // Sub-step for high time scales to avoid tunneling
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.004f));
            float subDt = dt / steps;

            for (int i = 0; i < steps && IsActive; i++)
                StepPhysics(subDt);

            transform.position = new Vector3(Position.x, Position.y, 0f);
        }

        private void StepPhysics(float dt)
        {
            Position += Velocity * dt;

            // Wall bounce (top/bottom)
            if (Position.y + _radius >= HalfH)
            {
                Position = new Vector2(Position.x, HalfH - _radius);
                Velocity = new Vector2(Velocity.x, -Mathf.Abs(Velocity.y));
                OnWallHit?.Invoke();
            }
            else if (Position.y - _radius <= -HalfH)
            {
                Position = new Vector2(Position.x, -HalfH + _radius);
                Velocity = new Vector2(Velocity.x, Mathf.Abs(Velocity.y));
                OnWallHit?.Invoke();
            }

            // Left paddle collision
            if (LeftPaddle != null && Velocity.x < 0)
            {
                float px = LeftPaddle.transform.position.x;
                float py = LeftPaddle.currentY;
                float halfH = LeftPaddle.HalfPaddleH;
                float halfT = LeftPaddle.Thickness / 2f;

                if (Position.x - _radius <= px + halfT &&
                    Position.x + _radius >= px - halfT &&
                    Position.y >= py - halfH &&
                    Position.y <= py + halfH)
                {
                    BounceOffPaddle(LeftPaddle, px + halfT);
                    OnPaddleHit?.Invoke(PaddleSide.Left);
                }
            }

            // Right paddle collision
            if (RightPaddle != null && Velocity.x > 0)
            {
                float px = RightPaddle.transform.position.x;
                float py = RightPaddle.currentY;
                float halfH = RightPaddle.HalfPaddleH;
                float halfT = RightPaddle.Thickness / 2f;

                if (Position.x + _radius >= px - halfT &&
                    Position.x - _radius <= px + halfT &&
                    Position.y >= py - halfH &&
                    Position.y <= py + halfH)
                {
                    BounceOffPaddle(RightPaddle, px - halfT);
                    OnPaddleHit?.Invoke(PaddleSide.Right);
                }
            }

            // Goal detection (ball past paddles)
            if (Position.x < -HalfW - 1f)
            {
                IsActive = false;
                OnGoalScored?.Invoke(PaddleSide.Right); // Right scores
            }
            else if (Position.x > HalfW + 1f)
            {
                IsActive = false;
                OnGoalScored?.Invoke(PaddleSide.Left); // Left scores
            }
        }

        private void BounceOffPaddle(PongPaddle paddle, float bounceX)
        {
            // Position correction
            Position = new Vector2(bounceX + (_radius * Mathf.Sign(Velocity.x) * -1f), Position.y);

            // Angle depends on where on the paddle the ball hit
            float relativeHit = (Position.y - paddle.currentY) / paddle.HalfPaddleH;
            relativeHit = Mathf.Clamp(relativeHit, -1f, 1f);

            float angle = relativeHit * _maxBounceAngle * Mathf.Deg2Rad;
            float dirX = (Velocity.x > 0) ? -1f : 1f;

            // Speed up
            CurrentSpeed = Mathf.Min(CurrentSpeed + _speedIncrease, _maxSpeed);

            Velocity = new Vector2(dirX * Mathf.Cos(angle), Mathf.Sin(angle)).normalized * CurrentSpeed;
        }
    }
}
