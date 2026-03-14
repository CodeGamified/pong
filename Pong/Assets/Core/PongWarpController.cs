// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Time;
using Pong.Game;
using UnityEngine;

namespace Pong.Core
{
    /// <summary>
    /// Warp controller for Pong — "warp to match N" for batch testing.
    /// Estimates sim-time per match and warps forward to skip N matches.
    /// Press [W] in-game to warp 10 matches ahead.
    /// </summary>
    public class PongWarpController : TimeWarpController
    {
        private PongMatchManager _match;
        private int _targetMatchCount;

        // Estimated average match duration in sim-seconds (self-adjusting)
        private float _avgMatchDuration = 30f;
        private int _completedMatches;
        private double _lastMatchStartTime;

        public void Initialize(PongMatchManager match)
        {
            _match = match;
            // Pong-appropriate warp settings
            maxWarpSpeed = 1000f;
            accelerationDuration = 0.5f;
            decelerationDuration = 0.5f;
            arrivalTimeScale = 1f;
            minWarpSpeed = 50f;
            arrivalHoldDuration = 0.5f;

            if (_match != null)
            {
                _match.OnMatchStarted += OnMatchStart;
                _match.OnMatchEnded += OnMatchEnd;
            }
        }

        /// <summary>Warp forward by N matches from current match count.</summary>
        public bool WarpMatches(int matchCount)
        {
            if (SimulationTime.Instance == null || _match == null) return false;
            if (matchCount <= 0) return false;

            _targetMatchCount = _match.MatchesPlayed + matchCount;
            double estimatedTime = matchCount * _avgMatchDuration;
            double targetTime = SimulationTime.Instance.simulationTime + estimatedTime;

            Debug.Log($"[Warp] Warping {matchCount} matches ahead (est. {estimatedTime:F0}s sim-time)");
            return WarpToTime(targetTime);
        }

        protected override void OnWarpStarting(double targetTime)
        {
            Debug.Log($"[Warp] Accelerating → target match #{_targetMatchCount}");
        }

        protected override void OnWarpArriving()
        {
            Debug.Log($"[Warp] Arrived — completed {_match.MatchesPlayed} matches");
        }

        protected override void OnWarpCompleting()
        {
            _targetMatchCount = 0;
        }

        protected override void Update()
        {
            base.Update();

            // [W] key to warp 10 matches ahead
            if (!IsWarping && Input.GetKeyDown(KeyCode.W))
                WarpMatches(10);

            // [Escape] to cancel warp
            if (IsWarping && Input.GetKeyDown(KeyCode.Escape))
                CancelWarp();
        }

        private void OnMatchStart()
        {
            _lastMatchStartTime = SimulationTime.Instance?.simulationTime ?? 0;
        }

        private void OnMatchEnd(PaddleSide winner)
        {
            _completedMatches++;
            if (SimulationTime.Instance != null && _lastMatchStartTime > 0)
            {
                double duration = SimulationTime.Instance.simulationTime - _lastMatchStartTime;
                if (duration > 0)
                {
                    // Exponential moving average
                    _avgMatchDuration = Mathf.Lerp(_avgMatchDuration, (float)duration, 0.3f);
                }
            }

            // Stop warp if we've reached target match count
            if (IsWarping && _match.MatchesPlayed >= _targetMatchCount)
            {
                CancelWarp();
                Debug.Log($"[Warp] Target match count reached ({_match.MatchesPlayed})");
            }
        }

        private void OnDestroy()
        {
            if (_match != null)
            {
                _match.OnMatchStarted -= OnMatchStart;
                _match.OnMatchEnded -= OnMatchEnd;
            }
        }
    }
}
