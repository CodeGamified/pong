// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Audio;
using UnityEngine;

namespace Pong.Audio
{
    /// <summary>
    /// Procedural synth audio provider for Pong.
    /// Generates retro blip/boop/buzz tones via AudioClip.Create() — zero asset files.
    /// Each method plays a one-shot tone through a shared AudioSource.
    /// </summary>
    public class PongAudioProvider : IAudioProvider
    {
        private AudioSource _source;

        // Pre-generated clips
        private AudioClip _blip;       // paddle hit — short high beep
        private AudioClip _boop;       // wall bounce — lower tick
        private AudioClip _score;      // goal scored — rising sweep
        private AudioClip _serve;      // serve — quick chirp
        private AudioClip _matchWon;   // match end — descending arpeggio
        private AudioClip _tick;       // instruction step — tiny click
        private AudioClip _error;      // compile error — harsh buzz
        private AudioClip _tap;        // generic tap

        public PongAudioProvider()
        {
            var go = new GameObject("PongAudio");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D

            GenerateClips();
        }

        private void GenerateClips()
        {
            _blip = SynthTone(880f, 0.06f, "blip");         // A5 — paddle hit
            _boop = SynthTone(440f, 0.04f, "boop");          // A4 — wall bounce
            _score = SynthSweep(440f, 880f, 0.2f, "score");  // A4→A5 rising sweep
            _serve = SynthTone(660f, 0.08f, "serve");        // E5 — chirp
            _matchWon = SynthSweep(880f, 220f, 0.4f, "win"); // A5→A3 descending
            _tick = SynthTone(1200f, 0.015f, "tick");         // tiny click
            _error = SynthNoise(0.15f, "error");              // harsh noise burst
            _tap = SynthTone(600f, 0.03f, "tap");             // quick tap
        }

        private void Play(AudioClip clip, float volume = 0.3f)
        {
            if (_source != null && clip != null)
                _source.PlayOneShot(clip, volume);
        }

        // ── IAudioProvider: Editor ──
        public void PlayTap()            => Play(_tap, 0.15f);
        public void PlayInsert()         => Play(_tap, 0.2f);
        public void PlayDelete()         => Play(_boop, 0.2f);
        public void PlayUndo()           => Play(_boop, 0.15f);
        public void PlayRedo()           => Play(_blip, 0.15f);
        public void PlayCompileSuccess() => Play(_blip, 0.25f);
        public void PlayCompileError()   => Play(_error, 0.3f);
        public void PlayNavigate()       => Play(_tap, 0.1f);

        // ── IAudioProvider: Engine ──
        public void PlayInstructionStep() => Play(_tick, 0.05f);
        public void PlayOutput()          => Play(_tap, 0.1f);
        public void PlayHalted()          => Play(_boop, 0.1f);
        public void PlayIOBlocked()       => Play(_error, 0.15f);
        public void PlayWaitStateChanged() => Play(_tap, 0.08f);

        // ── IAudioProvider: Time ──
        public void PlayWarpStart()      => Play(_serve, 0.2f);
        public void PlayWarpCruise()     { }
        public void PlayWarpDecelerate() => Play(_boop, 0.15f);
        public void PlayWarpArrived()    => Play(_score, 0.3f);
        public void PlayWarpCancelled()  => Play(_error, 0.2f);
        public void PlayWarpComplete()   => Play(_blip, 0.2f);

        // ── IAudioProvider: Persistence ──
        public void PlaySaveStarted()    { }
        public void PlaySaveCompleted()  => Play(_tap, 0.1f);
        public void PlaySyncCompleted()  => Play(_blip, 0.1f);

        // ── Pong-specific game sounds ──
        public void PlayPaddleHit()  => Play(_blip, 0.4f);
        public void PlayWallBounce() => Play(_boop, 0.25f);
        public void PlayGoalScored() => Play(_score, 0.5f);
        public void PlayMatchWon()   => Play(_matchWon, 0.5f);
        public void PlayServe()      => Play(_serve, 0.3f);

        // ═══════════════════════════════════════════════════════════════
        // SYNTH — procedural AudioClip generation
        // ═══════════════════════════════════════════════════════════════

        private static AudioClip SynthTone(float freq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration); // linear decay
                envelope *= envelope; // quadratic decay — punchier
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthSweep(float startFreq, float endFreq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - progress;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip SynthNoise(float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            // Seeded for determinism
            var rng = new System.Random(42);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
