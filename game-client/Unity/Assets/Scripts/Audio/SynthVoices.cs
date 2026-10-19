using UnityEngine;

// Place sounds made in code: monks chanting (พระสวด), temple and church bells, a shop door chime,
// buffalo and cows, seagulls, frogs and the school / hospital chimes. Voices are built from harmonics
// shaped by vowel formants, so they sound sung or mooed instead of beeped.
public static class SynthVoices
{
    private const int Rate = SynthSounds.Rate;

    public static float[] Make(string name)
    {
        switch (name)
        {
            case "chant": return Chant();
            case "temple_bell": return Bell(150f, new[] { 0f }, 5.5f, 0.9f);
            case "church_bell": return Bell(392f, new[] { 0f, 1.3f, 2.6f }, 5f, 0.6f);
            case "door_chime": return DoorChime();
            case "buffalo": return Moo(68f, 1.9f, 0.9f);
            case "cow": return Moo(125f, 1.4f, 0.75f);
            case "seagull": return Seagull();
            case "frog": return Frog();
            case "school_bell": return Notes(new[] { 659f, 523f, 587f, 392f, 392f, 587f, 659f, 523f }, 0.6f, 0.55f);
            case "pa_chime": return Notes(new[] { 523f, 659f, 784f }, 0.32f, 0.4f);
            default: return null;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    // Vowel formants (Hz): อา โอ อู อี เอ
    private static readonly Vector2[] Vowels =
    {
        new Vector2(730f, 1100f), new Vector2(500f, 850f), new Vector2(330f, 750f), new Vector2(300f, 2200f), new Vector2(480f, 1800f),
    };

    private static float FormantGain(float freq, Vector2 vowel) =>
        Mathf.Exp(-Sq((freq - vowel.x) / 140f)) + 0.6f * Mathf.Exp(-Sq((freq - vowel.y) / 200f)) + 0.08f;

    private static float Sq(float x) => x * x;

    private static void Normalize(float[] d, float peak)
    {
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        float k = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= k;
    }

    // A few early reflections so voices sound like they're in a hall
    private static void Room(float[] d, float wet)
    {
        int[] taps = { (int)(0.061f * Rate), (int)(0.113f * Rate), (int)(0.197f * Rate), (int)(0.29f * Rate) };
        float[] gains = { 0.45f, 0.32f, 0.22f, 0.14f };
        for (int i = d.Length - 1; i >= 0; i--)
            for (int t = 0; t < taps.Length; t++)
                if (i - taps[t] >= 0) d[i] += d[i - taps[t]] * gains[t] * wet;
    }

    // ── Monks chanting: a reciting tone with short syllables, falling at the end of each phrase ──
    private static float[] Chant()
    {
        var d = SynthSounds.Buffer(9f);
        var rng = new System.Random(7);
        const float syllable = 0.24f;
        int count = 30;
        var vowelOf = new int[count];
        for (int s = 0; s < count; s++) vowelOf[s] = rng.Next(Vowels.Length);

        float[] voices = { 1f, 1.007f, 0.497f }; // unison, a slightly-off monk and one an octave down
        foreach (float detune in voices)
        {
            float phase = 0f;
            float amp = detune < 0.6f ? 0.5f : 1f;
            var gains = new float[64];
            int harmonics = 0;
            int total = (int)((count * syllable + 0.9f) * Rate);
            for (int i = 0; i < total && i < d.Length; i++)
            {
                float t = (float)i / Rate;
                int s = Mathf.Min(count - 1, (int)(t / syllable));
                float within = (t - s * syllable) / syllable;
                bool last = s >= count - 3;
                float f0 = (last ? 104f : 112f) * detune * (1f + 0.004f * Mathf.Sin(t * 31f));
                phase += 2f * Mathf.PI * f0 / Rate;

                // Slide between vowels at the start of each syllable
                Vector2 from = Vowels[vowelOf[Mathf.Max(0, s - 1)]], to = Vowels[vowelOf[s]];
                Vector2 vowel = Vector2.Lerp(from, to, Mathf.Clamp01(within / 0.3f));

                if ((i & 63) == 0) // the vowel changes slowly: refresh harmonic gains every 64 samples
                {
                    harmonics = Mathf.Min(28, (int)(2600f / f0));
                    for (int k = 1; k <= harmonics; k++) gains[k] = FormantGain(f0 * k, vowel) / Mathf.Sqrt(k);
                }
                float sample = 0f;
                for (int k = 1; k <= harmonics; k++)
                    sample += Mathf.Sin(phase * k) * gains[k];

                float env = (0.75f + 0.25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(within))) * Mathf.Clamp01(t * 6f);
                float tail = t - count * syllable;
                if (tail > 0f) env *= Mathf.Clamp01(1f - tail / 0.9f); // last syllable is held, then fades
                d[i] += sample * env * amp;
            }
        }
        Room(d, 0.8f);
        Normalize(d, 0.7f);
        return d;
    }

    // Struck bell: inharmonic partials, each a pair slightly apart so they shimmer (ระฆัง)
    private static float[] Bell(float f, float[] strikes, float seconds, float peak)
    {
        var d = SynthSounds.Buffer(seconds);
        float[] ratio = { 0.5f, 1f, 1.19f, 1.5f, 2f, 2.5f, 3f };
        float[] gain = { 0.6f, 1f, 0.4f, 0.35f, 0.3f, 0.15f, 0.1f };
        float[] decay = { 0.5f, 0.8f, 1.2f, 1.4f, 1.8f, 2.6f, 3.4f };
        foreach (float strike in strikes)
        {
            int start = (int)(strike * Rate);
            for (int i = start; i < d.Length; i++)
            {
                float t = (float)(i - start) / Rate;
                float v = 0f;
                for (int p = 0; p < ratio.Length; p++)
                {
                    float fp = f * ratio[p];
                    v += gain[p] * Mathf.Exp(-t * decay[p]) * (Mathf.Sin(2f * Mathf.PI * fp * t) + Mathf.Sin(2f * Mathf.PI * (fp + 0.8f) * t)) * 0.5f;
                }
                d[i] += v * Mathf.Min(1f, t * 300f);
            }
        }
        Normalize(d, peak);
        return d;
    }

    // Two-tone "ding-dong" when a shop door opens
    private static float[] DoorChime()
    {
        var d = SynthSounds.Buffer(2.6f);
        float[] notes = { 988f, 784f };
        for (int n = 0; n < notes.Length; n++)
        {
            int start = (int)(n * 0.5f * Rate);
            for (int i = start; i < d.Length; i++)
            {
                float t = (float)(i - start) / Rate;
                float f = notes[n];
                d[i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f))
                        * Mathf.Exp(-t * 2.2f) * Mathf.Min(1f, t * 400f);
            }
        }
        Normalize(d, 0.5f);
        return d;
    }

    // Moo: a low voice that opens from "uu" to "aa" and closes again, pitch rising then sagging
    private static float[] Moo(float baseF, float seconds, float peak)
    {
        var d = SynthSounds.Buffer(seconds + 0.3f);
        var rng = new System.Random(11);
        float phase = 0f;
        int n = (int)(seconds * Rate);
        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            float f0 = baseF * (0.85f + 0.3f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.3f))) * (1f + 0.01f * Mathf.Sin(u * 60f));
            phase += 2f * Mathf.PI * f0 / Rate;
            Vector2 vowel = Vector2.Lerp(new Vector2(330f, 700f), new Vector2(650f, 1050f), Mathf.Sin(Mathf.PI * u));
            float v = 0f;
            int harmonics = Mathf.Min(30, (int)(2400f / f0));
            for (int k = 1; k <= harmonics; k++)
                v += Mathf.Sin(phase * k) * FormantGain(f0 * k, vowel) / k;
            float breath = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
            float env = Mathf.Clamp01(u / 0.12f) * Mathf.Clamp01((1f - u) / 0.3f);
            d[i] = (v + breath) * env;
        }
        Normalize(d, peak);
        return d;
    }

    // Three "kee-ow" calls, gliding down with a raspy edge
    private static float[] Seagull()
    {
        var d = SynthSounds.Buffer(1.6f);
        for (int c = 0; c < 3; c++)
        {
            int start = (int)(c * 0.45f * Rate);
            int len = (int)(0.34f * Rate);
            float phase = 0f;
            for (int i = 0; i < len && start + i < d.Length; i++)
            {
                float u = (float)i / len;
                float f = Mathf.Lerp(1900f, 1050f, u * u) * (c == 2 ? 0.92f : 1f);
                phase += 2f * Mathf.PI * f / Rate;
                float rasp = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 45f * i / Rate);
                float v = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) + 0.25f * Mathf.Sin(phase * 3f);
                d[start + i] += v * rasp * Mathf.Sin(Mathf.PI * u);
            }
        }
        Normalize(d, 0.35f);
        return d;
    }

    // Four buzzy croaks (อึ่งอ่าง / กบ)
    private static float[] Frog()
    {
        var d = SynthSounds.Buffer(1.6f);
        for (int c = 0; c < 4; c++)
        {
            int start = (int)(c * 0.36f * Rate);
            int len = (int)(0.2f * Rate);
            for (int i = 0; i < len && start + i < d.Length; i++)
            {
                float t = (float)i / Rate, u = (float)i / len;
                float gate = Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 32f * t));
                float v = 0f;
                for (int k = 1; k <= 8; k++)
                    v += Mathf.Sin(2f * Mathf.PI * 165f * k * t) * FormantGain(165f * k, new Vector2(600f, 1200f));
                d[start + i] += v * gate * Mathf.Sin(Mathf.PI * u);
            }
        }
        Normalize(d, 0.4f);
        return d;
    }

    // Soft marimba notes (school chime, hospital announcement)
    private static float[] Notes(float[] freqs, float gap, float peak)
    {
        var d = SynthSounds.Buffer(freqs.Length * gap + 1f);
        for (int n = 0; n < freqs.Length; n++)
            SynthSounds.Mallet(d, (int)(n * gap * Rate), freqs[n], 0.4f);
        Normalize(d, peak);
        return d;
    }
}
