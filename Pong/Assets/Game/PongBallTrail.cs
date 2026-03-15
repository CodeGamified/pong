// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using CodeGamified.Procedural;
using CodeGamified.Quality;
using System.Collections.Generic;

namespace Pong.Game
{
    /// <summary>
    /// Procedural ball trail.
    ///  • Low/Med/High: ring buffer of spheres that fade out.
    ///  • Ultra: persistent LineRenderer that draws the entire match path.
    /// Trail length adjusts dynamically via <see cref="IQualityResponsive"/>.
    /// </summary>
    public class PongBallTrail : MonoBehaviour, IQualityResponsive
    {
        private int _trailLength;
        private const float TRAIL_INTERVAL = 0.02f;
        private const int ULTRA_THRESHOLD = 1000; // above this = persistent line mode

        // Ring buffer mode (Low/Med/High)
        private Transform[] _trailParts;
        private Renderer[] _trailRenderers;
        private int _writeIndex;

        // Line mode (Ultra)
        private LineRenderer _lineRenderer;
        private List<Vector3> _linePoints;
        private bool _lineMode;

        // Shared
        private float _nextSpawnTime;
        private PongBall _ball;
        private Material _trailMaterial;
        private ColorPalette _palette;

        public void Initialize(PongBall ball, ColorPalette palette)
        {
            _ball = ball;
            _palette = palette;
            _trailLength = QualityHints.TrailSegments(QualityBridge.CurrentTier);
            Build();
        }

        private void OnEnable()  => QualityBridge.Register(this);
        private void OnDisable() => QualityBridge.Unregister(this);

        public void OnQualityChanged(QualityTier tier)
        {
            int newLength = QualityHints.TrailSegments(tier);
            if (newLength == _trailLength) return;
            _trailLength = newLength;
            Cleanup();
            Build();
        }

        // ═════════════════════════════════════════════════════════════
        // BUILD
        // ═════════════════════════════════════════════════════════════

        private void Build()
        {
            _lineMode = _trailLength >= ULTRA_THRESHOLD;
            Color trailColor = _palette != null
                ? _palette.Resolve("ball_trail")
                : new Color(1f, 1f, 0.3f, 0.5f);

            if (_lineMode)
                BuildLineMode(trailColor);
            else
                BuildSphereMode(trailColor);
        }

        private void BuildLineMode(Color trailColor)
        {
            _linePoints = new List<Vector3>(512);
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
            _lineRenderer.startWidth = 0.08f;
            _lineRenderer.endWidth = 0.03f;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.numCapVertices = 2;

            // Gradient: cyan → magenta along the trail length
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0f, 1f, 1f), 0f),
                    new GradientColorKey(trailColor, 0.5f),
                    new GradientColorKey(new Color(1f, 0f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.1f, 0f),
                    new GradientAlphaKey(0.4f, 0.7f),
                    new GradientAlphaKey(0.7f, 1f)
                }
            );
            _lineRenderer.colorGradient = grad;

            // Use an unlit material for the line
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _lineRenderer.material = new Material(shader);
        }

        private void BuildSphereMode(Color trailColor)
        {
            _trailParts = new Transform[_trailLength];
            _trailRenderers = new Renderer[_trailLength];

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Color");

            _trailMaterial = new Material(shader);
            if (_trailMaterial.HasProperty("_BaseColor"))
                _trailMaterial.SetColor("_BaseColor", trailColor);
            else
                _trailMaterial.color = trailColor;

            for (int i = 0; i < _trailLength; i++)
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

        private void Cleanup()
        {
            if (_trailParts != null)
            {
                for (int i = 0; i < _trailParts.Length; i++)
                    if (_trailParts[i] != null)
                        Destroy(_trailParts[i].gameObject);
                _trailParts = null;
                _trailRenderers = null;
            }
            if (_lineRenderer != null)
            {
                Destroy(_lineRenderer);
                _lineRenderer = null;
            }
            _linePoints = null;
            _writeIndex = 0;
        }

        // ═════════════════════════════════════════════════════════════
        // UPDATE
        // ═════════════════════════════════════════════════════════════

        private void Update()
        {
            if (_ball == null || !_ball.IsActive)
            {
                if (!_lineMode) HideAllSpheres();
                return;
            }

            if (Time.time >= _nextSpawnTime)
            {
                _nextSpawnTime = Time.time + TRAIL_INTERVAL;
                if (_lineMode)
                    AppendLinePoint();
                else
                    SpawnSpherePoint();
            }

            if (!_lineMode)
                UpdateSphereFade();
        }

        // ── Line mode (Ultra) ────────────────────────────────────

        private void AppendLinePoint()
        {
            var pos = new Vector3(_ball.Position.x, _ball.Position.y, 0.01f);

            // Skip if too close to last point (avoids redundant vertices)
            if (_linePoints.Count > 0 &&
                Vector3.SqrMagnitude(pos - _linePoints[_linePoints.Count - 1]) < 0.001f)
                return;

            _linePoints.Add(pos);
            _lineRenderer.positionCount = _linePoints.Count;
            _lineRenderer.SetPosition(_linePoints.Count - 1, pos);
        }

        /// <summary>Clear the persistent line trail (call on match reset).</summary>
        public void ClearLine()
        {
            if (_linePoints != null) _linePoints.Clear();
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
        }

        // ── Sphere mode (Low/Med/High) ───────────────────────────

        private void SpawnSpherePoint()
        {
            var part = _trailParts[_writeIndex];
            part.position = new Vector3(_ball.Position.x, _ball.Position.y, 0f);
            part.gameObject.SetActive(true);

            float speedRatio = _ball.CurrentSpeed / 20f;
            float scale = Mathf.Lerp(0.1f, 0.25f, Mathf.Clamp01(speedRatio));
            part.localScale = Vector3.one * scale;

            SetAlpha(_writeIndex, 0.6f);
            _writeIndex = (_writeIndex + 1) % _trailLength;
        }

        private void UpdateSphereFade()
        {
            for (int i = 0; i < _trailLength; i++)
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

        private void HideAllSpheres()
        {
            if (_trailParts == null) return;
            for (int i = 0; i < _trailParts.Length; i++)
                if (_trailParts[i] != null)
                    _trailParts[i].gameObject.SetActive(false);
        }
    }
}
