// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World

namespace Pong.AI
{
    /// <summary>
    /// AI difficulty tiers.
    /// Players "unit test" their code against each level to climb the leaderboard.
    /// 
    /// EASY:    Tracks ball Y with delay and low speed. Beatable by any code.
    /// MEDIUM:  Faster tracking, slight prediction. Needs decent logic.
    /// HARD:    Predicts ball trajectory. Requires smart positioning.
    /// EXPERT:  Near-perfect prediction + variable strategy. The final boss.
    /// </summary>
    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
}
