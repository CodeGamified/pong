// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using UnityEngine;
using Pong.AI;

namespace Pong.Game
{
    /// <summary>
    /// Leaderboard — tracks win/loss records per AI difficulty.
    /// "Level up" by consistently beating harder AI.
    ///
    /// Ranking:
    ///   BRONZE  — Beat Easy AI 3+ times
    ///   SILVER  — Beat Medium AI 3+ times
    ///   GOLD    — Beat Hard AI 3+ times
    ///   DIAMOND — Beat Expert AI 3+ times
    /// </summary>
    public class PongLeaderboard : MonoBehaviour
    {
        public struct DifficultyRecord
        {
            public int Wins;
            public int Losses;
            public int TotalPointsScored;
            public int TotalPointsConceded;
            public float BestWinMargin; // largest point gap in a win

            public float WinRate => (Wins + Losses > 0) ? (float)Wins / (Wins + Losses) : 0f;
        }

        public Dictionary<AIDifficulty, DifficultyRecord> Records { get; private set; }
            = new Dictionary<AIDifficulty, DifficultyRecord>();

        public string CurrentRank { get; private set; } = "UNRANKED";
        public int TotalMatches => GetTotalMatches();

        // Events
        public System.Action<string> OnRankChanged;
        public System.Action<AIDifficulty, bool> OnMatchRecorded;

        private void Awake()
        {
            Records[AIDifficulty.Easy] = new DifficultyRecord();
            Records[AIDifficulty.Medium] = new DifficultyRecord();
            Records[AIDifficulty.Hard] = new DifficultyRecord();
            Records[AIDifficulty.Expert] = new DifficultyRecord();
        }

        public void RecordMatch(AIDifficulty difficulty, bool playerWon,
                                int playerScore, int aiScore)
        {
            var rec = Records[difficulty];

            if (playerWon) rec.Wins++;
            else rec.Losses++;

            rec.TotalPointsScored += playerScore;
            rec.TotalPointsConceded += aiScore;

            if (playerWon)
            {
                float margin = playerScore - aiScore;
                if (margin > rec.BestWinMargin)
                    rec.BestWinMargin = margin;
            }

            Records[difficulty] = rec;
            OnMatchRecorded?.Invoke(difficulty, playerWon);

            UpdateRank();
        }

        private void UpdateRank()
        {
            string oldRank = CurrentRank;

            if (Records[AIDifficulty.Expert].Wins >= 3)
                CurrentRank = "DIAMOND";
            else if (Records[AIDifficulty.Hard].Wins >= 3)
                CurrentRank = "GOLD";
            else if (Records[AIDifficulty.Medium].Wins >= 3)
                CurrentRank = "SILVER";
            else if (Records[AIDifficulty.Easy].Wins >= 3)
                CurrentRank = "BRONZE";
            else
                CurrentRank = "UNRANKED";

            if (CurrentRank != oldRank)
            {
                Debug.Log($"[LEADERBOARD] RANK UP! {oldRank} → {CurrentRank}");
                OnRankChanged?.Invoke(CurrentRank);
            }
        }

        private int GetTotalMatches()
        {
            int total = 0;
            foreach (var kvp in Records)
                total += kvp.Value.Wins + kvp.Value.Losses;
            return total;
        }

        public string GetSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"RANK: {CurrentRank}  │  MATCHES: {TotalMatches}");
            sb.AppendLine("────────────────────────────────────");

            foreach (AIDifficulty diff in new[] { AIDifficulty.Easy, AIDifficulty.Medium,
                                                   AIDifficulty.Hard, AIDifficulty.Expert })
            {
                var r = Records[diff];
                string status = r.Wins >= 3 ? "✅" : "  ";
                sb.AppendLine($"  {status} {diff,-8} │ {r.Wins}W {r.Losses}L  " +
                              $"({r.WinRate:P0})  │ PF:{r.TotalPointsScored} PA:{r.TotalPointsConceded}");
            }

            return sb.ToString();
        }
    }
}
