// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;

namespace Pong.Game
{
    /// <summary>
    /// The Pong court — walls, center line, visual decorations.
    /// Pure geometry, no game logic.
    /// </summary>
    public class PongCourt : MonoBehaviour
    {
        public float Width { get; set; } = 16f;
        public float Height { get; set; } = 10f;

        public float HalfWidth => Width / 2f;
        public float HalfHeight => Height / 2f;

        private GameObject _topWall;
        private GameObject _bottomWall;
        private GameObject _centerLine;

        public void Initialize()
        {
            float wallThickness = 0.2f;

            // Top wall
            _topWall = CreateWall("TopWall",
                new Vector3(0f, HalfHeight + wallThickness / 2f, 0f),
                new Vector3(Width + 2f, wallThickness, 1f));

            // Bottom wall
            _bottomWall = CreateWall("BottomWall",
                new Vector3(0f, -HalfHeight - wallThickness / 2f, 0f),
                new Vector3(Width + 2f, wallThickness, 1f));

            // Center dashed line
            CreateCenterLine();
        }

        private GameObject CreateWall(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.white;
            }

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            return go;
        }

        private void CreateCenterLine()
        {
            _centerLine = new GameObject("CenterLine");
            _centerLine.transform.SetParent(transform, false);

            int segments = (int)(Height / 0.6f);
            for (int i = 0; i < segments; i++)
            {
                float y = -HalfHeight + i * (Height / segments) + (Height / segments) / 2f;
                if (i % 2 == 0) // dashed
                {
                    var dash = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    dash.name = $"Dash_{i}";
                    dash.transform.SetParent(_centerLine.transform, false);
                    dash.transform.localPosition = new Vector3(0f, y, 0f);
                    dash.transform.localScale = new Vector3(0.1f, Height / segments * 0.6f, 1f);

                    var r = dash.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.material = new Material(Shader.Find("Sprites/Default"));
                        r.material.color = new Color(0.4f, 0.4f, 0.4f);
                    }

                    var c = dash.GetComponent<Collider>();
                    if (c != null) Destroy(c);
                }
            }
        }
    }
}
