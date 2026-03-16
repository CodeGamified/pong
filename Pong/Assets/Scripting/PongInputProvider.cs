// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pong.Scripting
{
    /// <summary>
    /// Captures vertical input via Unity Input System for use by paddle scripts.
    /// Supports keyboard (W/S, UpArrow/DownArrow) and gamepad left stick.
    /// Exposes a single float value [-1, 1] readable by the bytecode engine.
    /// </summary>
    public class PongInputProvider : MonoBehaviour
    {
        public static PongInputProvider Instance { get; private set; }

        private InputAction _moveAction;

        /// <summary>Current vertical input value in [-1, 1].</summary>
        public float VerticalInput { get; private set; }

        /// <summary>Mouse Y position in world space (on the court plane Z=0).</summary>
        public float MouseWorldY { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Composite axis: W/UpArrow = +1, S/DownArrow = -1, gamepad left stick Y
            _moveAction = new InputAction("PaddleMove", InputActionType.Value);
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/s")
                .With("Positive", "<Keyboard>/w");
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/downArrow")
                .With("Positive", "<Keyboard>/upArrow");
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Gamepad>/leftStick/down")
                .With("Positive", "<Gamepad>/leftStick/up");
            _moveAction.Enable();
        }

        private void Update()
        {
            VerticalInput = _moveAction.ReadValue<float>();
            UpdateMouseWorldY();
        }

        private void UpdateMouseWorldY()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // Raycast from mouse into the court plane (Z = 0)
            Ray ray = cam.ScreenPointToRay(Mouse.current != null
                ? (Vector3)Mouse.current.position.ReadValue()
                : Input.mousePosition);
            // Court lies on Z = 0 plane
            if (Mathf.Abs(ray.direction.z) > 0.0001f)
            {
                float t = -ray.origin.z / ray.direction.z;
                MouseWorldY = ray.origin.y + ray.direction.y * t;
            }
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            _moveAction?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
