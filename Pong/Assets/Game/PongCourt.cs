// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Procedural;

namespace Pong.Game
{
    /// <summary>
    /// The Pong court — 3D walls, floor, center line.
    /// Built via ProceduralAssembler from PongCourtBlueprint.
    /// </summary>
    public class PongCourt : MonoBehaviour
    {
        public float Width { get; set; } = 16f;
        public float Height { get; set; } = 10f;

        public float HalfWidth => Width / 2f;
        public float HalfHeight => Height / 2f;

        public AssemblyResult Visual { get; private set; }

        public void Initialize(ColorPalette palette)
        {
            var blueprint = new PongCourtBlueprint(Width, Height);
            Visual = ProceduralAssembler.BuildWithVisualState(blueprint, palette);

            if (Visual.Root != null)
                Visual.Root.transform.SetParent(transform, false);
        }
    }
}
