// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Procedural;
using CodeGamified.Quality;

namespace Pong.Game
{
    /// <summary>
    /// The Pong court — 3D walls, floor, center line.
    /// Built via ProceduralAssembler from PongCourtBlueprint.
    /// Rebuilds center-line dash density when quality changes.
    /// </summary>
    public class PongCourt : MonoBehaviour, IQualityResponsive
    {
        public float Width { get; set; } = 16f;
        public float Height { get; set; } = 10f;

        public float HalfWidth => Width / 2f;
        public float HalfHeight => Height / 2f;

        public AssemblyResult Visual { get; private set; }

        private ColorPalette _palette;

        public void Initialize(ColorPalette palette)
        {
            _palette = palette;
            RebuildVisual();
        }

        private void OnEnable()  => QualityBridge.Register(this);
        private void OnDisable() => QualityBridge.Unregister(this);

        public void OnQualityChanged(QualityTier tier)
        {
            RebuildVisual();
        }

        private void RebuildVisual()
        {
            if (Visual.Root != null)
                Destroy(Visual.Root);

            float density = QualityHints.CourtDashDensity(QualityBridge.CurrentTier);
            var blueprint = new PongCourtBlueprint(Width, Height, density);
            Visual = ProceduralAssembler.BuildWithVisualState(blueprint, _palette);

            if (Visual.Root != null)
                Visual.Root.transform.SetParent(transform, false);

            // Ultra: mild passive glow on center line
            if (QualityBridge.CurrentTier == QualityTier.Ultra &&
                Visual.Renderers != null &&
                Visual.Renderers.TryGetValue("center_line", out var r))
            {
                var mat = r.material;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", new Color(1.2f, 1.2f, 1.2f));
            }
        }
    }
}
