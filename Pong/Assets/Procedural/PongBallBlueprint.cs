// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Procedural;
using UnityEngine;

namespace Pong.Game
{
    /// <summary>
    /// Procedural blueprint for the Pong ball.
    /// Emits a single 3D Sphere.
    /// </summary>
    public class PongBallBlueprint : IProceduralBlueprint
    {
        private readonly float _radius;

        public PongBallBlueprint(float radius)
        {
            _radius = radius;
        }

        public string DisplayName => "PongBall";
        public ProceduralLODHint LODHint => ProceduralLODHint.Lightweight;
        public string PaletteId => "pong";

        public ProceduralPartDef[] GetParts()
        {
            float diameter = _radius * 2f;
            return new[]
            {
                new ProceduralPartDef("body", PrimitiveType.Sphere,
                    Vector3.zero,
                    new Vector3(diameter, diameter, diameter),
                    "ball")
            };
        }
    }
}
