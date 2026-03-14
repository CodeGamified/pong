// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Procedural;
using UnityEngine;

namespace Pong.Game
{
    /// <summary>
    /// Procedural blueprint for a Pong paddle.
    /// Emits a single 3D Cube with side-dependent color.
    /// </summary>
    public class PongPaddleBlueprint : IProceduralBlueprint
    {
        private readonly float _height;
        private readonly float _thickness;
        private readonly PaddleSide _side;

        public PongPaddleBlueprint(float height, float thickness, PaddleSide side)
        {
            _height = height;
            _thickness = thickness;
            _side = side;
        }

        public string DisplayName => _side == PaddleSide.Left ? "PlayerPaddle" : "AIPaddle";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "pong";

        public ProceduralPartDef[] GetParts()
        {
            string colorKey = _side == PaddleSide.Left ? "paddle_player" : "paddle_ai";
            return new[]
            {
                new ProceduralPartDef("body", PrimitiveType.Cube,
                    Vector3.zero,
                    new Vector3(_thickness, _height, 0.4f),
                    colorKey) { Collider = ColliderMode.Box }
            };
        }
    }
}
