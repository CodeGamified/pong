// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Audio;
using CodeGamified.Settings;
using UnityEngine;

namespace Pong.Audio
{
    /// <summary>
    /// Procedural synth audio provider for Pong.
    /// Generates retro blip/boop/buzz tones via AudioClip.Create() — zero asset files.
    /// Each method plays a one-shot tone through a shared AudioSource.
    /// </summary>
    public class PongAudioProvider : IAudioProvider, IEqualizerProvider
    {
        private static GameObject _persistentGO;

        private AudioSource _source;
        private AudioSource _musicSource;

        // ── IEqualizerProvider ──
        private const int EqBandCount = 8;
        private readonly float[] _spectrum = new float[256];

        public int BandCount => EqBandCount;

        public bool GetBands(float[] bands)
        {
            if (_musicSource == null || !_musicSource.isPlaying)
                return false;

            AudioListener.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

            for (int b = 0; b < bands.Length && b < EqBandCount; b++)
            {
                int lo = (int)Mathf.Pow(2, b);
                int hi = Mathf.Min((int)Mathf.Pow(2, b + 1), _spectrum.Length);
                float sum = 0f;
                for (int s = lo; s < hi; s++)
                    sum += _spectrum[s];
                bands[b] = Mathf.Clamp01(sum * (b + 1) * 10f);
            }
            return true;
        }

        // Pre-generated clips
        private AudioClip _blip;       // paddle hit — 808 kick
        private AudioClip _boop;       // wall bounce — low thud
        private AudioClip _score;      // goal scored — deep rising bass
        private AudioClip _serve;      // serve — punchy bass hit
        private AudioClip _matchWon;   // match end — sub-bass drop
        private AudioClip _tick;       // instruction step — soft sub tap
        private AudioClip _error;      // compile error — distorted bass
        private AudioClip _tap;        // generic bass tap

        public PongAudioProvider()
        {
            // Destroy previous instance's DontDestroyOnLoad object to prevent overlapping audio
            if (_persistentGO != null)
            {
                Object.Destroy(_persistentGO);
                _persistentGO = null;
            }

            var go = new GameObject("PongAudio");
            _persistentGO = go;
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D

            // Music source — separate so volume can be controlled independently
            _musicSource = go.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.loop = true;

            GenerateClips();
            StartMusic();

            // Auto-update music volume when settings change
            SettingsBridge.OnChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(SettingsSnapshot snapshot, SettingsCategory category)
        {
            if (category == SettingsCategory.Audio)
                UpdateMusicVolume();
        }

        private void GenerateClips()
        {
            _blip = SynthKick(160f, 50f, 0.15f, "blip");      // 808 kick — paddle hit
            _boop = SynthKick(120f, 45f, 0.10f, "boop");      // low thud — wall bounce
            _score = SynthSweep(55f, 180f, 0.3f, "score");    // deep rising bass
            _serve = SynthKick(200f, 60f, 0.12f, "serve");    // punchy bass hit
            _matchWon = SynthSweep(180f, 35f, 0.5f, "win");   // sub-bass drop
            _tick = SynthTone(80f, 0.04f, "tick");             // soft sub tap
            _error = SynthDistBass(55f, 0.2f, "error");        // distorted low buzz
            _tap = SynthKick(100f, 50f, 0.06f, "tap");         // quick bass tap
        }

        private void Play(AudioClip clip, float volume = 0.3f)
        {
            if (_source != null && clip != null)
                _source.PlayOneShot(clip, volume * SettingsBridge.SfxVolume);
        }

        private void StartMusic()
        {
            var clip = Resources.Load<AudioClip>("retro");
            if (clip == null)
            {
                Debug.LogWarning("[PongAudio] Music clip 'retro' not found in Resources.");
                return;
            }
            _musicSource.clip = clip;
            _musicSource.volume = SettingsBridge.MusicVolume * SettingsBridge.MasterVolume;
            _musicSource.time = 47f;
            _musicSource.Play();
        }

        /// <summary>Call each frame or on settings change to keep music volume in sync.</summary>
        public void UpdateMusicVolume()
        {
            if (_musicSource != null && _musicSource.clip != null)
                _musicSource.volume = SettingsBridge.MusicVolume * SettingsBridge.MasterVolume;
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
                float envelope = Mathf.Exp(-6f * t / duration); // exponential decay — 808 style
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>808-style kick: rapid pitch drop from attack to sustain with exponential decay.</summary>
        private static AudioClip SynthKick(float attackFreq, float sustainFreq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Rapid exponential pitch drop — characteristic 808 boom
                float freq = sustainFreq + (attackFreq - sustainFreq) * Mathf.Exp(-30f * t);
                float envelope = Mathf.Exp(-5f * t / duration);
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Sin(phase) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Distorted sub-bass — sine wave with soft clipping for gritty low-end.</summary>
        private static AudioClip SynthDistBass(float freq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-4f * t / duration);
                float raw = Mathf.Sin(2f * Mathf.PI * freq * t) * 3f; // overdrive
                float clipped = raw / (1f + Mathf.Abs(raw));           // soft clip
                data[i] = clipped * envelope;
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
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = Mathf.Exp(-3f * progress); // exponential decay
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Sin(phase) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
