// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;

namespace Pong.Core
{
    /// <summary>
    /// Subtle camera sway for the 3D perspective view.
    /// Adds gentle sine-based orbit around the court center.
    /// </summary>
    public class PongCameraSway : MonoBehaviour
    {
        [Header("Sway")]
        public float swayAmplitudeX = 0.5f;
        public float swayAmplitudeY = 0.2f;
        public float swaySpeed = 0.3f;

        private Vector3 _basePosition;
        private bool _initialized;

        private void LateUpdate()
        {
            if (!_initialized)
            {
                _basePosition = transform.position;
                _initialized = true;
            }

            float t = Time.unscaledTime * swaySpeed;
            float offsetX = Mathf.Sin(t) * swayAmplitudeX;
            float offsetY = Mathf.Sin(t * 0.7f) * swayAmplitudeY;

            transform.position = _basePosition + new Vector3(offsetX, offsetY, 0f);
            transform.LookAt(Vector3.zero, Vector3.up);
        }
    }
}
