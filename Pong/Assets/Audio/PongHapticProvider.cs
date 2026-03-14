// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Audio;

namespace Pong.Audio
{
    /// <summary>
    /// Stub haptic provider for Pong.
    /// All methods are no-ops — platform-specific implementations
    /// can override for mobile vibration or gamepad rumble.
    /// </summary>
    public class PongHapticProvider : IHapticProvider
    {
        public void TapLight() { }
        public void TapMedium() { }
        public void TapHeavy() { }
        public void Buzz(float duration) { }
    }
}
