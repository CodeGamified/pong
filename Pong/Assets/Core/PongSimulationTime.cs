// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using UnityEngine;

namespace Pong.Core
{
    /// <summary>
    /// Central time management for Pong simulation.
    /// Controls time scale from 0x (paused) to 1000x (warp-speed testing).
    /// 
    /// At 1000x, your paddle AI runs thousands of matches per minute.
    /// Real-time unit testing of your code against all AI tiers.
    /// </summary>
    public class SimulationTime : MonoBehaviour
    {
        public static SimulationTime Instance { get; private set; }

        [Header("Time Control")]
        public double simulationTime = 0;

        [Tooltip("Time scale multiplier (1 = real-time, 1000 = warp)")]
        [Range(0f, 1000f)]
        public float timeScale = 1f;

        public bool isPaused = false;

        [Header("Presets")]
        public float[] timeScalePresets = { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f, 500f, 1000f };
        private int currentPresetIndex = 3; // Start at 1x

        // Events
        public System.Action<double> OnSimulationTimeChanged;
        public System.Action<float> OnTimeScaleChanged;
        public System.Action<bool> OnPausedChanged;

        // Day/night stubs (unused in Pong but kept for engine compat)
        [HideInInspector] public float dayLengthSeconds = 99999f;
        [HideInInspector] public float startingHour = 12f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!isPaused)
            {
                double dt = Time.deltaTime * timeScale;
                simulationTime += dt;
                OnSimulationTimeChanged?.Invoke(simulationTime);
            }
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                IncreaseTimeScale();
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                DecreaseTimeScale();
        }

        // ── Time Scale API ──

        public void TogglePause()
        {
            isPaused = !isPaused;
            OnPausedChanged?.Invoke(isPaused);
        }

        public void SetPaused(bool paused)
        {
            if (isPaused != paused)
            {
                isPaused = paused;
                OnPausedChanged?.Invoke(isPaused);
            }
        }

        public void SetTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0f, 1000f);
            if (!Mathf.Approximately(timeScale, scale))
            {
                timeScale = scale;
                OnTimeScaleChanged?.Invoke(timeScale);
            }
        }

        public void IncreaseTimeScale()
        {
            if (currentPresetIndex < timeScalePresets.Length - 1)
                SetTimeScalePreset(currentPresetIndex + 1);
        }

        public void DecreaseTimeScale()
        {
            if (currentPresetIndex > 0)
                SetTimeScalePreset(currentPresetIndex - 1);
        }

        public void SetTimeScalePreset(int index)
        {
            if (index >= 0 && index < timeScalePresets.Length)
            {
                currentPresetIndex = index;
                SetTimeScale(timeScalePresets[index]);
            }
        }

        public string GetFormattedTimeScale()
        {
            if (isPaused) return "PAUSED";
            return $"{timeScale:F0}x";
        }

        public string GetFormattedTime()
        {
            int minutes = (int)(simulationTime / 60.0);
            int seconds = (int)(simulationTime % 60.0);
            return $"{minutes:D2}:{seconds:D2}";
        }

        // Engine compat stubs
        public float GetTimeOfDay() => startingHour;
        public Vector3 GetSunDirection() => Vector3.up;
        public float GetSunAltitude() => 1f;
        public bool IsDaytime() => true;
    }
}
