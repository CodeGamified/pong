// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using Pong.Core;

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

        // Events
        public System.Action<PaddleSide, int, int> OnPointScored;   // (who scored, leftScore, rightScore)
        public System.Action<PaddleSide> OnMatchEnded;              // winner
        public System.Action OnMatchStarted;
        public System.Action OnServe;

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
        }

        public void StartMatch()
        {
            LeftScore = 0;
            RightScore = 0;
            MatchInProgress = true;
            OnMatchStarted?.Invoke();
            ServeAfterDelay(PaddleSide.Right); // First serve toward AI
        }

        private void OnGoal(PaddleSide scorer)
        {
            if (!MatchInProgress) return;

            if (scorer == PaddleSide.Left) LeftScore++;
            else RightScore++;

            OnPointScored?.Invoke(scorer, LeftScore, RightScore);

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
                _ball.OnGoalScored -= OnGoal;
        }
    }
}
