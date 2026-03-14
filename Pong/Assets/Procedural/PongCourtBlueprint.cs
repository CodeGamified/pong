// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Procedural;
using UnityEngine;
using System.Collections.Generic;

namespace Pong.Game
{
    /// <summary>
    /// Procedural blueprint for the Pong court.
    /// Emits 3D walls (Cubes), a floor plane, and center line dashes.
    /// </summary>
    public class PongCourtBlueprint : IProceduralBlueprint
    {
        private readonly float _width;
        private readonly float _height;

        public PongCourtBlueprint(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public string DisplayName => "PongCourt";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "pong";

        public ProceduralPartDef[] GetParts()
        {
            var parts = new List<ProceduralPartDef>();
            float halfW = _width / 2f;
            float halfH = _height / 2f;
            float wallThickness = 0.3f;
            float depth = 0.5f;

            // Floor — sits behind the play plane
            parts.Add(new ProceduralPartDef("floor", PrimitiveType.Cube,
                new Vector3(0f, 0f, depth),
                new Vector3(_width + 2f, _height + 2f, 0.1f),
                "court_floor"));

            // Top wall
            parts.Add(new ProceduralPartDef("wall_top", PrimitiveType.Cube,
                new Vector3(0f, halfH + wallThickness / 2f, 0f),
                new Vector3(_width + 2f, wallThickness, depth),
                "wall") { Collider = ColliderMode.Box });

            // Bottom wall
            parts.Add(new ProceduralPartDef("wall_bottom", PrimitiveType.Cube,
                new Vector3(0f, -halfH - wallThickness / 2f, 0f),
                new Vector3(_width + 2f, wallThickness, depth),
                "wall") { Collider = ColliderMode.Box });

            // Center dashed line
            int segments = (int)(_height / 0.6f);
            for (int i = 0; i < segments; i++)
            {
                float y = -halfH + i * (_height / segments) + (_height / segments) / 2f;
                if (i % 2 == 0)
                {
                    parts.Add(new ProceduralPartDef($"dash_{i}", PrimitiveType.Cube,
                        new Vector3(0f, y, 0f),
                        new Vector3(0.12f, _height / segments * 0.6f, 0.15f),
                        "court_line"));
                }
            }

            return parts.ToArray();
        }
    }
}
