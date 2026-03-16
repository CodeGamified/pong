// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Procedural;
using CodeGamified.Quality;
using CodeGamified.Time;

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

        // Line mode (Ultra) — one LineRenderer per color segment
        private List<LineRenderer> _lineSegments;
        private List<List<Vector3>> _segmentPoints;
        private bool _lineMode;
        private Color _currentLineColor;
        private Material _lineMaterial;
        private static readonly Color GoldHDR = new Color(3f, 2.4f, 0.3f);

        // Fade-out state (line mode)
        private Coroutine _fadeCoroutine;
        private const float FADE_DURATION = 0.4f;
        private const float FADE_SPEED_THRESHOLD = 10f;

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
            _lineSegments = new List<LineRenderer>(16);
            _segmentPoints = new List<List<Vector3>>(16);
            _currentLineColor = GoldHDR;

            // Shared material — vertex colors drive the color per-segment
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            _lineMaterial = new Material(shader);
            _lineMaterial.SetFloat("_Surface", 0); // Opaque
            _lineMaterial.SetColor("_BaseColor", Color.white);

            // Start first segment in gold
            StartNewSegment(GoldHDR);
        }

        private void BuildSphereMode(Color trailColor)
        {
            _trailParts = new Transform[_trailLength];
            _trailRenderers = new Renderer[_trailLength];

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
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
            ClearLineSegments();
            _writeIndex = 0;
        }

        private void ClearLineSegments()
        {
            if (_lineSegments != null)
            {
                for (int i = 0; i < _lineSegments.Count; i++)
                    if (_lineSegments[i] != null)
                        Destroy(_lineSegments[i]);
                _lineSegments.Clear();
            }
            if (_segmentPoints != null)
                _segmentPoints.Clear();
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

            if (_lineSegments == null || _lineSegments.Count == 0) return;
            var points = _segmentPoints[_segmentPoints.Count - 1];
            var lr = _lineSegments[_lineSegments.Count - 1];

            // Skip if too close to last point
            if (points.Count > 0 &&
                Vector3.SqrMagnitude(pos - points[points.Count - 1]) < 0.001f)
                return;

            points.Add(pos);
            lr.positionCount = points.Count;
            lr.SetPosition(points.Count - 1, pos);
        }

        /// <summary>Start a new line segment with the given HDR color.</summary>
        private void StartNewSegment(Color hdrColor)
        {
            var go = new GameObject($"TrailSeg_{_lineSegments.Count}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 0;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.03f;
            lr.useWorldSpace = true;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.material = new Material(_lineMaterial);
            lr.material.SetColor("_BaseColor", hdrColor);
            if (lr.material.HasProperty("_EmissionColor"))
            {
                lr.material.EnableKeyword("_EMISSION");
                lr.material.SetColor("_EmissionColor", hdrColor);
            }

            // Gradient drives alpha only — HDR color lives in the material
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            lr.colorGradient = grad;

            var points = new List<Vector3>(256);

            // Bridge: duplicate last point of previous segment so there's no gap
            if (_segmentPoints != null && _segmentPoints.Count > 0)
            {
                var prev = _segmentPoints[_segmentPoints.Count - 1];
                if (prev.Count > 0)
                {
                    var bridgePos = prev[prev.Count - 1];
                    points.Add(bridgePos);
                    lr.positionCount = 1;
                    lr.SetPosition(0, bridgePos);
                }
            }

            _lineSegments.Add(lr);
            _segmentPoints.Add(points);
        }

        /// <summary>Set the trail color from this point forward (e.g. on paddle hit).</summary>
        public void SetSideColor(Color hdrColor)
        {
            if (!_lineMode) return;
            _currentLineColor = hdrColor;
            StartNewSegment(hdrColor);
        }

        /// <summary>Clear the persistent line trail (call on match reset).
        /// At low time scales (&lt;10×) fades out over FADE_DURATION; otherwise instant.</summary>
        public void ClearLine()
        {
            float scale = SimulationTime.Instance != null ? SimulationTime.Instance.timeScale : 1f;
            if (scale < FADE_SPEED_THRESHOLD && _lineMode && _lineSegments != null && _lineSegments.Count > 0)
            {
                // Fade then clear
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeAndClear());
            }
            else
            {
                ClearLineImmediate();
            }
        }

        private void ClearLineImmediate()
        {
            if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
            ClearLineSegments();
            _currentLineColor = GoldHDR;
            if (_lineMode)
                StartNewSegment(GoldHDR);
        }

        private IEnumerator FadeAndClear()
        {
            // Snapshot the segments to fade — new segment starts immediately for the next rally
            var fadingLRs = new List<LineRenderer>(_lineSegments);
            var originalBaseColors = new List<Color>(fadingLRs.Count);
            for (int i = 0; i < fadingLRs.Count; i++)
                originalBaseColors.Add(fadingLRs[i] != null && fadingLRs[i].material.HasProperty("_BaseColor")
                    ? fadingLRs[i].material.GetColor("_BaseColor")
                    : Color.white);

            _lineSegments.Clear();
            _segmentPoints.Clear();
            _currentLineColor = GoldHDR;
            StartNewSegment(GoldHDR);

            float elapsed = 0f;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / FADE_DURATION;
                for (int i = 0; i < fadingLRs.Count; i++)
                {
                    if (fadingLRs[i] == null) continue;
                    var mat = fadingLRs[i].material;
                    Color faded = Color.Lerp(originalBaseColors[i], Color.black, t);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", faded);
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", faded);
                }
                yield return null;
            }

            for (int i = 0; i < fadingLRs.Count; i++)
                if (fadingLRs[i] != null)
                    Destroy(fadingLRs[i].gameObject);

            _fadeCoroutine = null;
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

            ResetColor(_writeIndex);
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

                // Fade to black — opaque URP shaders ignore alpha
                c = Color.Lerp(c, Color.black, Time.deltaTime * 5f);
                if (c.maxColorComponent <= 0.01f)
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

        private void ResetColor(int index)
        {
            var r = _trailRenderers[index];
            Color c = _trailMaterial.HasProperty("_BaseColor")
                ? _trailMaterial.GetColor("_BaseColor")
                : _trailMaterial.color;
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
