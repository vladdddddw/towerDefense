using UnityEngine;

namespace Vanguard.TD.Core
{
    /// <summary>
    /// Procedural synth — every clip is generated at first use via math (sine/noise/sweep).
    /// No external WAV/OGG files. Singleton; auto-bootstrapped on first call.
    /// </summary>
    public static class SfxRack
    {
        const int   Sr     = 22050;     // sample rate
        const float Master = 1.0f;

        static AudioSource _bus;
        static AudioClip _fire, _strike, _bloom, _frost, _scrap, _coin, _hit, _win, _lose;
        static bool _ready;

        static void Initialize()
        {
            if (_ready) return;
            _ready = true;

            var go = new GameObject("[SfxRack]");
            Object.DontDestroyOnLoad(go);
            _bus = go.AddComponent<AudioSource>();
            _bus.playOnAwake = false;
            _bus.spatialBlend = 0f;

            _fire   = Tone(720f, 0.04f, 22f, 0.30f);
            _strike = Noise     (0.07f,  18f, 0.40f);
            _bloom  = Detonation(0.28f);
            _frost  = Sweep   (1500f, 320f, 0.16f, 0.32f);
            _scrap  = Sweep   ( 480f,  90f, 0.28f, 0.42f);
            _coin   = Tone   (1280f, 0.09f, 14f, 0.28f);
            _hit    = Noise     (0.18f,   7f, 0.50f);
            _win    = Arpeggio(new[]{ 523, 659, 784, 1047 }, 0.11f, 0.45f);
            _lose   = Arpeggio(new[]{ 392, 330, 262, 196 },  0.17f, 0.45f);
        }

        // ── Public triggers ──────────────────────────────────────────────────
        public static void Fire()      { Initialize(); _bus.PlayOneShot(_fire   , Master); }
        public static void Strike()    { Initialize(); _bus.PlayOneShot(_strike , Master); }
        public static void Bloom()     { Initialize(); _bus.PlayOneShot(_bloom  , Master); }
        public static void Frost()     { Initialize(); _bus.PlayOneShot(_frost  , Master); }
        public static void Scrap()     { Initialize(); _bus.PlayOneShot(_scrap  , Master); }
        public static void Coin()      { Initialize(); _bus.PlayOneShot(_coin   , Master); }
        public static void Hit()       { Initialize(); _bus.PlayOneShot(_hit    , Master); }
        public static void Victory()   { Initialize(); _bus.PlayOneShot(_win    , Master); }
        public static void Defeat()    { Initialize(); _bus.PlayOneShot(_lose   , Master); }

        // ── Synthesis primitives ─────────────────────────────────────────────
        static AudioClip Tone(float freq, float seconds, float decay, float gain)
        {
            int n = Mathf.Max(1, (int)(Sr * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-decay * t) * gain;
            }
            return BuildClip("tone", data);
        }

        static AudioClip Noise(float seconds, float decay, float gain)
        {
            int n = Mathf.Max(1, (int)(Sr * seconds));
            var data = new float[n];
            var rnd = new System.Random(91);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                data[i] = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-decay * t) * gain;
            }
            return BuildClip("noise", data);
        }

        static AudioClip Detonation(float seconds)
        {
            int n = Mathf.Max(1, (int)(Sr * seconds));
            var data = new float[n];
            var rnd = new System.Random(13);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Sr;
                float env  = Mathf.Exp(-7f * t);
                float boom = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.55f;
                float roar = ((float)rnd.NextDouble() * 2f - 1f) * 0.55f;
                data[i] = (boom + roar) * env * 0.55f;
            }
            return BuildClip("boom", data);
        }

        static AudioClip Sweep(float a, float b, float seconds, float gain)
        {
            int n = Mathf.Max(1, (int)(Sr * seconds));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float f = Mathf.Lerp(a, b, u);
                phase += 2f * Mathf.PI * f / Sr;
                float env = (1f - u) * Mathf.Exp(-1.4f * u);
                data[i] = Mathf.Sin(phase) * env * gain;
            }
            return BuildClip("sweep", data);
        }

        static AudioClip Arpeggio(int[] freqs, float each, float gain)
        {
            int per = (int)(Sr * each);
            var data = new float[per * freqs.Length];
            for (int k = 0; k < freqs.Length; k++)
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)Sr;
                float env = Mathf.Exp(-4.5f * t);
                data[k * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * env * gain;
            }
            return BuildClip("arp", data);
        }

        static AudioClip BuildClip(string name, float[] samples)
        {
            var c = AudioClip.Create(name, samples.Length, 1, Sr, false);
            c.SetData(samples, 0);
            return c;
        }
    }
}
