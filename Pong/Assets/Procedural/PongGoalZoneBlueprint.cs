// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Procedural;
using UnityEngine;

namespace Pong.Game
{
    /// <summary>
    /// Procedural blueprint for glowing goal zones behind each paddle.
    /// Translucent planes that pulse on goals.
    /// </summary>
    public class PongGoalZoneBlueprint : IProceduralBlueprint
    {
        private readonly float _courtHeight;
        private readonly float _depth;
        private readonly PaddleSide _side;

        public PongGoalZoneBlueprint(float courtHeight, float depth, PaddleSide side)
        {
            _courtHeight = courtHeight;
            _depth = depth;
            _side = side;
        }

        public string DisplayName => _side == PaddleSide.Left ? "GoalZoneLeft" : "GoalZoneRight";
        public ProceduralLODHint LODHint => ProceduralLODHint.Lightweight;
        public string PaletteId => "pong";

        public ProceduralPartDef[] GetParts()
        {
            string colorKey = _side == PaddleSide.Left ? "goal_player" : "goal_ai";
            return new[]
            {
                new ProceduralPartDef("zone", PrimitiveType.Cube,
                    Vector3.zero,
                    new Vector3(0.15f, _courtHeight, _depth),
                    colorKey)
            };
        }
    }
}
