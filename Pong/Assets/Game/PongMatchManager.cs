// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Time;
using Pong.Scripting;

namespace Pong.Game
{
    /// <summary>
    /// Match manager — scoring, serving, win detection, match flow.
    /// Wired to ball events. Time-scale aware for warp-speed testing.
    /// </summary>
    public class PongMatchManager : MonoBehaviour
    {
        private PongBall _ball;
        private PongPaddle _leftPaddle;
        private PongPaddle _rightPaddle;
        private PongCourt _court;

        private int _pointsToWin;
        private bool _autoRestart;
        private float _serveDelay;

        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public bool MatchInProgress { get; private set; }
        public int MatchesPlayed { get; private set; }
        public int PlayerWins { get; private set; }
        public int AIWins { get; private set; }

        private int _rallyCount;
        private const int STALEMATE_THRESHOLD = 100;

        // Events
        public System.Action<PaddleSide, int, int> OnPointScored;   // (who scored, leftScore, rightScore)
        public System.Action<PaddleSide> OnMatchEnded;              // winner
        public System.Action OnMatchStarted;
        public System.Action OnServe;
        public System.Action<int> OnStalemate;                      // rally count

        public void Initialize(PongBall ball, PongPaddle left, PongPaddle right,
                               PongCourt court, int pointsToWin, bool autoRestart, float serveDelay)
        {
            _ball = ball;
            _leftPaddle = left;
            _rightPaddle = right;
            _court = court;
            _pointsToWin = pointsToWin;
            _autoRestart = autoRestart;
            _serveDelay = serveDelay;

            // Wire ball references
            _ball.LeftPaddle = _leftPaddle;
            _ball.RightPaddle = _rightPaddle;

            // Wire scoring
            _ball.OnGoalScored += OnGoal;
            _ball.OnPaddleHit += OnPaddleHit;
        }

        public void StartMatch()
        {
            LeftScore = 0;
            RightScore = 0;
            MatchInProgress = true;
            ResetPaddles();
            OnMatchStarted?.Invoke();
            ServeAfterDelay(PaddleSide.Right); // First serve toward AI
        }

        private void OnPaddleHit(PaddleSide side)
        {
            if (!MatchInProgress) return;

            _rallyCount++;
            if (_rallyCount >= STALEMATE_THRESHOLD)
            {
                Debug.Log($"[Match] STALEMATE — {_rallyCount} volleys, no point awarded");
                OnStalemate?.Invoke(_rallyCount);
                _rallyCount = 0;

                // Re-serve toward a random side
                PaddleSide toward = (Random.value > 0.5f) ? PaddleSide.Right : PaddleSide.Left;
                ServeAfterDelay(toward);
            }
        }

        private void OnGoal(PaddleSide scorer)
        {
            if (!MatchInProgress) return;
            _rallyCount = 0;

            if (scorer == PaddleSide.Left) LeftScore++;
            else RightScore++;

            OnPointScored?.Invoke(scorer, LeftScore, RightScore);

            // Reset paddles to center and restart their scripts
            ResetPaddles();

            // Check for win
            if (LeftScore >= _pointsToWin)
            {
                EndMatch(PaddleSide.Left);
                return;
            }
            if (RightScore >= _pointsToWin)
            {
                EndMatch(PaddleSide.Right);
                return;
            }

            // Serve to the scorer
            ServeAfterDelay(scorer);
        }

        private void EndMatch(PaddleSide winner)
        {
            MatchInProgress = false;
            MatchesPlayed++;
            if (winner == PaddleSide.Left) PlayerWins++;
            else AIWins++;

            _ball.Stop();
            OnMatchEnded?.Invoke(winner);

            if (_autoRestart)
                StartCoroutine(RestartAfterDelay());
        }

        private void ResetPaddles()
        {
            _leftPaddle.ResetPosition();
            _rightPaddle.ResetPosition();

            // Reset code execution state for all PaddlePrograms
            foreach (var prog in FindObjectsByType<PaddleProgram>(FindObjectsSortMode.None))
                prog.ResetExecution();
        }

        /// <summary>Force a re-serve (e.g. ball went out of bounds due to live setting change).</summary>
        public void ForceReserve()
        {
            if (!MatchInProgress) return;
            _ball.Stop();
            ResetPaddles();
            PaddleSide toward = (Random.value > 0.5f) ? PaddleSide.Right : PaddleSide.Left;
            ServeAfterDelay(toward);
        }

        private void ServeAfterDelay(PaddleSide toward)
        {
            StartCoroutine(ServeCoroutine(toward));
        }

        private System.Collections.IEnumerator ServeCoroutine(PaddleSide toward)
        {
            _ball.Stop();

            // Wait for serve delay (scaled by time)
            float waited = 0f;
            while (waited < _serveDelay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }

            _rallyCount = 0;
            OnServe?.Invoke();
            _ball.Serve(toward);
        }

        private System.Collections.IEnumerator RestartAfterDelay()
        {
            float waited = 0f;
            float delay = _serveDelay * 2f;
            while (waited < delay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }

            StartMatch();
        }

        private void OnDestroy()
        {
            if (_ball != null)
            {
                _ball.OnGoalScored -= OnGoal;
                _ball.OnPaddleHit -= OnPaddleHit;
            }
        }
    }
}
