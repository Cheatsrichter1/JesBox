using System.Collections.Generic;
using UnityEngine;

namespace JesBox.Game
{
    /// <summary>
    /// Procedurally generated sound effects — no audio asset files needed,
    /// so this works immediately. Each cue is a short sequence of sine-wave
    /// tones synthesized once (via AudioClip.Create) and cached. If you later
    /// want real recorded sound effects instead, replace the body of a Play*
    /// method with `_source.PlayOneShot(yourClip)`.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Range(0f, 1f)] public float Volume = 0.5f;

        private const int SampleRate = 44100;
        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        public void PlayClick() => Play(Tone("click", new[] { 880f }, 0.05f));
        public void PlayJoin() => Play(Tone("join", new[] { 700f, 900f }, 0.15f));
        public void PlayRoundStart() => Play(Tone("roundstart", new[] { 440f, 660f }, 0.22f));
        public void PlayTick() => Play(Tone("tick", new[] { 1000f }, 0.07f));
        public void PlayCountdownBeep() => Play(Tone("countdown", new[] { 1200f }, 0.08f));
        public void PlayReveal() => Play(Tone("reveal", new[] { 660f, 880f, 1100f }, 0.35f));
        public void PlayVictoryFanfare() => Play(Tone("victory", new[] { 523f, 659f, 784f, 1047f }, 0.8f));

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            _source.PlayOneShot(clip, Volume);
        }

        /// <summary>Builds (and caches) a clip that plays each frequency in
        /// <paramref name="freqs"/> back-to-back for an equal share of
        /// <paramref name="totalDuration"/>, with a short fade in/out on each
        /// segment so they don't click.</summary>
        private AudioClip Tone(string key, float[] freqs, float totalDuration)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;

            int totalSamples = Mathf.CeilToInt(SampleRate * totalDuration);
            var data = new float[totalSamples];
            int segments = freqs.Length;
            int samplesPerSegment = totalSamples / segments;

            for (int s = 0; s < segments; s++)
            {
                float freq = freqs[s];
                int start = s * samplesPerSegment;
                int end = (s == segments - 1) ? totalSamples : start + samplesPerSegment;
                float segmentDuration = (float)(end - start) / SampleRate;
                float fade = Mathf.Min(0.05f, segmentDuration * 0.4f);

                for (int i = start; i < end; i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    float envelope = 1f;
                    if (t < fade) envelope = t / fade;
                    else if (segmentDuration - t < fade) envelope = (segmentDuration - t) / fade;
                    data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
                }
            }

            var clip = AudioClip.Create(key, totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            _cache[key] = clip;
            return clip;
        }
    }
}
