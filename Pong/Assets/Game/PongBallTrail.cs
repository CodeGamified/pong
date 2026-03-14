// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Procedural;
using System.Collections.Generic;

namespace Pong.Game
{
    /// <summary>
    /// Procedural ball trail — ring buffer of small cubes that pulse/fade behind the ball.
    /// Uses ProceduralAssembler for initial geometry and ProceduralVisualState for fade.
    /// </summary>
    public class PongBallTrail : MonoBehaviour
    {
        private const int TRAIL_LENGTH = 12;
        private const float TRAIL_INTERVAL = 0.02f;

        private Transform[] _trailParts;
        private Renderer[] _trailRenderers;
        private int _writeIndex;
        private float _nextSpawnTime;
        private PongBall _ball;
        private Material _trailMaterial;

        public void Initialize(PongBall ball, ColorPalette palette)
        {
            _ball = ball;
            _trailParts = new Transform[TRAIL_LENGTH];
            _trailRenderers = new Renderer[TRAIL_LENGTH];

            // Find a shader
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Color");

            Color trailColor = palette != null ? palette.Resolve("ball_trail") : new Color(1f, 1f, 0.3f, 0.5f);

            _trailMaterial = new Material(shader);
            if (_trailMaterial.HasProperty("_BaseColor"))
                _trailMaterial.SetColor("_BaseColor", trailColor);
            else
                _trailMaterial.color = trailColor;

            for (int i = 0; i < TRAIL_LENGTH; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Trail_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.15f;
                go.SetActive(false);

                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var r = go.GetComponent<Renderer>();
                r.material = new Material(_trailMaterial);

                _trailParts[i] = go.transform;
                _trailRenderers[i] = r;
            }
        }

        private void Update()
        {
            if (_ball == null || !_ball.IsActive)
            {
                HideAll();
                return;
            }

            if (Time.time >= _nextSpawnTime)
            {
                _nextSpawnTime = Time.time + TRAIL_INTERVAL;
                SpawnTrailPoint();
            }

            UpdateFade();
        }

        private void SpawnTrailPoint()
        {
            var part = _trailParts[_writeIndex];
            part.position = new Vector3(_ball.Position.x, _ball.Position.y, 0f);
            part.gameObject.SetActive(true);

            // Scale based on speed — faster = bigger trail
            float speedRatio = _ball.CurrentSpeed / 20f;
            float scale = Mathf.Lerp(0.1f, 0.25f, Mathf.Clamp01(speedRatio));
            part.localScale = Vector3.one * scale;

            // Reset alpha
            SetAlpha(_writeIndex, 0.6f);

            _writeIndex = (_writeIndex + 1) % TRAIL_LENGTH;
        }

        private void UpdateFade()
        {
            for (int i = 0; i < TRAIL_LENGTH; i++)
            {
                if (!_trailParts[i].gameObject.activeSelf) continue;

                var r = _trailRenderers[i];
                Color c = r.material.HasProperty("_BaseColor")
                    ? r.material.GetColor("_BaseColor")
                    : r.material.color;

                c.a -= Time.deltaTime * 3f;
                if (c.a <= 0f)
                {
                    _trailParts[i].gameObject.SetActive(false);
                }
                else
                {
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", c);
                    else
                        r.material.color = c;

                    // Shrink as it fades
                    _trailParts[i].localScale *= 0.97f;
                }
            }
        }

        private void SetAlpha(int index, float alpha)
        {
            var r = _trailRenderers[index];
            Color c = r.material.HasProperty("_BaseColor")
                ? r.material.GetColor("_BaseColor")
                : r.material.color;
            c.a = alpha;
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", c);
            else
                r.material.color = c;
        }

        private void HideAll()
        {
            if (_trailParts == null) return;
            for (int i = 0; i < TRAIL_LENGTH; i++)
                if (_trailParts[i] != null)
                    _trailParts[i].gameObject.SetActive(false);
        }
    }
}
