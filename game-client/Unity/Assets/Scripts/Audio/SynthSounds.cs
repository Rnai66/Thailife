using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Every sound in the game made in code (no audio files): punches, footsteps, crickets, waves,
// the kokai bell and a ranat (ระนาด) fair tune on the Thai pentatonic scale. Place sounds are in
// SynthVoices, background music in SynthMusic; recorded Kenney takes win where there are some.
public static class SynthSounds
{
    internal const int Rate = 22050;
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, Task<float[]>> pending = new Dictionary<string, Task<float[]>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        pending.Clear();
    }

    // Made right away (short sounds)
    public static AudioClip Get(string name)
    {
        if (cache.TryGetValue(name, out var clip) && clip != null) return clip;
        float[] data;
        if (pending.TryGetValue(name, out var task)) // already being made: use that (short sounds finish fast)
        {
            pending.Remove(name);
            data = task.Status == TaskStatus.Faulted ? Generate(name) : task.Result;
        }
        else data = Generate(name);
        return data == null ? null : Store(name, data);
    }

    // Long sounds (chanting, music…) are made on a worker thread so the game doesn't stutter:
    // returns null until ready — call again later (Prepare starts it early).
    public static AudioClip TryGet(string name)
    {
        if (cache.TryGetValue(name, out var clip) && clip != null) return clip;
        if (!pending.TryGetValue(name, out var task))
        {
            Prepare(name);
            return null;
        }
        if (!task.IsCompleted) return null;
        pending.Remove(name);
        return task.Status == TaskStatus.RanToCompletion && task.Result != null ? Store(name, task.Result) : null;
    }

    public static void Prepare(string name)
    {
        if (cache.ContainsKey(name) || pending.ContainsKey(name)) return;
        pending[name] = Task.Run(() => Generate(name));
    }

    private static AudioClip Store(string name, float[] data)
    {
        var clip = AudioClip.Create("Synth_" + name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        cache[name] = clip;
        return clip;
    }

    // Pure math, safe off the main thread
    private static float[] Generate(string name)
    {
        switch (name)
        {
            case "punch": return Punch();
            case "whoosh": return Whoosh();
            case "step": return Step();
            case "crickets": return Crickets();
            case "waves": return Waves();
            case "bell": return Bell();
            case "ranat": return Ranat();
            default: return SynthVoices.Make(name) ?? SynthMusic.Make(name);
        }
    }

    // ── Generators ──────────────────────────────────────────────────────────────
    internal static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

    private static float[] Punch()
    {
        var d = Buffer(0.22f);
        var rng = new System.Random(1);
        float low = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            low += (noise - low) * 0.25f; // dull slap
            float thump = Mathf.Sin(2f * Mathf.PI * (90f - 120f * t) * t);
            d[i] = (low * 1.4f * Mathf.Exp(-t * 45f) + thump * Mathf.Exp(-t * 18f)) * 0.8f;
        }
        return d;
    }

    private static float[] Whoosh()
    {
        var d = Buffer(0.25f);
        var rng = new System.Random(2);
        float a = 0f, b = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / d.Length;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            a += (noise - a) * 0.35f;
            b += (a - b) * 0.35f;
            d[i] = (a - b) * Mathf.Sin(Mathf.PI * t) * 1.2f;
        }
        return d;
    }

    private static float[] Step()
    {
        var d = Buffer(0.09f);
        var rng = new System.Random(3);
        float low = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / Rate;
            low += ((float)rng.NextDouble() * 2f - 1f - low) * 0.12f;
            d[i] = low * Mathf.Exp(-t * 60f) * 1.6f;
        }
        return d;
    }

    // Loop: trains of short 4.5 kHz pulses with gaps (จิ้งหรีด)
    private static float[] Crickets()
    {
        var d = Buffer(2.4f);
        float[] starts = { 0.05f, 0.62f, 1.25f, 1.8f };
        foreach (float s in starts)
            for (int p = 0; p < 5; p++)
            {
                int start = (int)((s + p * 0.035f) * Rate);
                int len = (int)(0.022f * Rate);
                for (int i = 0; i < len && start + i < d.Length; i++)
                {
                    float t = (float)i / len;
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * 4500f * i / Rate) * Mathf.Sin(Mathf.PI * t) * 0.25f;
                }
            }
        return d;
    }

    // Loop: soft surf that swells twice per loop (envelope is periodic so the loop is seamless)
    private static float[] Waves()
    {
        var d = Buffer(8f);
        var rng = new System.Random(4);
        float low = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / d.Length;
            low += ((float)rng.NextDouble() * 2f - 1f - low) * 0.05f;
            float swell = 0.35f + 0.65f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t - 1.2f), 2f);
            d[i] = low * swell * 2.2f;
        }
        int fade = Rate / 20; // smooth the noise seam
        for (int i = 0; i < fade; i++)
        {
            float k = (float)i / fade;
            d[i] = Mathf.Lerp(d[d.Length - fade + i], d[i], k);
        }
        return d;
    }

    private static float[] Bell()
    {
        var d = Buffer(1.2f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / Rate;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 3170f * t) * 0.25f) * Mathf.Exp(-t * 4f) * 0.6f;
        }
        return d;
    }

    // Loop: ranat ek melody in octaves over ching (ฉิ่ง) and a klong drum, 8 bars of 8 eighth notes
    private static float[] Ranat()
    {
        const float beat = 60f / 230f; // one eighth note at 115 BPM, the recorded tune's tempo
        const int steps = 64;
        var d = Buffer(beat * steps);
        // Thai pentatonic (do re mi sol la) across two octaves, as scale degrees
        float[] scale = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };
        int[] melody =
        {
            5, 6, 7, 6, 5, 3, 4, 5,   7, 8, 7, 6, 5, 6, 5, -1,
            3, 4, 5, 4, 3, 2, 1, 2,   3, 5, 4, 3, 2, 1, 0, -1,
            5, 6, 7, 8, 9, 8, 7, 6,   5, 6, 7, 6, 5, 4, 3, -1,
            2, 3, 4, 5, 4, 3, 2, 1,   0, 2, 1, 2, 3, 2, 0, -1,
        };
        const float root = 587.33f; // D, the recorded tune's key
        for (int s = 0; s < steps; s++)
        {
            int start = (int)(s * beat * Rate);
            if (melody[s] >= 0)
            {
                float f = root * Mathf.Pow(2f, scale[melody[s]] / 12f);
                Mallet(d, start, f, 0.3f);
                Mallet(d, start, f * 0.5f, 0.2f); // the left hand plays an octave below
            }
            if (s % 4 == 0) Ching(d, start, open: s % 8 == 0);
            if (s % 8 == 0 || s % 8 == 5) Drum(d, start);
        }
        for (int i = 0; i < d.Length; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
        return d;
    }

    // Wooden bar: fundamental plus the inharmonic partials of a xylophone, fast decay
    internal static void Mallet(float[] d, int start, float f, float amp)
    {
        int len = (int)(0.5f * Rate);
        for (int i = 0; i < len && start + i < d.Length; i++)
        {
            float t = (float)i / Rate;
            float v = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 9f)
                    + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 3.93f * t) * Mathf.Exp(-t * 28f)
                    + 0.15f * Mathf.Sin(2f * Mathf.PI * f * 9.2f * t) * Mathf.Exp(-t * 60f);
            d[start + i] += v * amp * Mathf.Min(1f, t * 400f);
        }
    }

    private static void Ching(float[] d, int start, bool open)
    {
        int len = (int)((open ? 0.6f : 0.08f) * Rate);
        float decay = open ? 6f : 50f;
        for (int i = 0; i < len && start + i < d.Length; i++)
        {
            float t = (float)i / Rate;
            float v = Mathf.Sin(2f * Mathf.PI * 2730f * t) + 0.7f * Mathf.Sin(2f * Mathf.PI * 4120f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 6290f * t);
            d[start + i] += v * 0.06f * Mathf.Exp(-t * decay);
        }
    }

    private static void Drum(float[] d, int start)
    {
        int len = (int)(0.3f * Rate);
        for (int i = 0; i < len && start + i < d.Length; i++)
        {
            float t = (float)i / Rate;
            d[start + i] += Mathf.Sin(2f * Mathf.PI * (110f - 90f * t) * t) * Mathf.Exp(-t * 14f) * 0.35f;
        }
    }
}
