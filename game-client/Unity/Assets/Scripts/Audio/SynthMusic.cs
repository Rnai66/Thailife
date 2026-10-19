using UnityEngine;

// Soft background music (เพลงบรรเลง) made in code: a plucked khim-like melody on the Thai pentatonic
// scale over a gentle bass and pad. Notes are written with wrap-around so every loop is seamless.
public static class SynthMusic
{
    private const int Rate = SynthSounds.Rate;

    public struct Track
    {
        public string Name;
        public float Root, Bpm;
        public int Seed;
        public float Brightness; // pluck tone: 0 mellow … 1 bright
    }

    public static readonly Track[] Tracks =
    {
        new Track { Name = "ทุ่งนายามเช้า", Root = 293.66f, Bpm = 84f, Seed = 3, Brightness = 0.5f },
        new Track { Name = "ลมเย็นริมคลอง", Root = 392f, Bpm = 72f, Seed = 8, Brightness = 0.8f },
        new Track { Name = "ราตรีหมู่บ้าน", Root = 220f, Bpm = 60f, Seed = 21, Brightness = 0.25f },
    };

    // "bgm0", "bgm1", … and "concert" (the beach stage)
    public static float[] Make(string name)
    {
        if (name == "concert") return Concert();
        if (!name.StartsWith("bgm") || !int.TryParse(name.Substring(3), out int i) || i < 0 || i >= Tracks.Length) return null;
        return Render(Tracks[i]);
    }

    private static float[] Render(Track track)
    {
        const int stepsPerBar = 8, bars = 16;
        float step = 60f / track.Bpm / 2f; // eighth note
        int length = Mathf.CeilToInt(stepsPerBar * bars * step * Rate);
        var d = new float[length];
        var rng = new System.Random(track.Seed);

        float[] scale = { -5, -3, 0, 2, 4, 7, 9, 12, 14, 16 }; // pentatonic, a little below and above the root
        float[] chordRoots = { 0, 9, 5, 7 };                  // I – vi – IV – V, one bar each

        // Melody for bars 1-8; bars 9-16 repeat it with a new ending
        var melody = new int[stepsPerBar * 8];
        int idx = 4;
        for (int s = 0; s < melody.Length; s++)
        {
            bool onBeat = s % 2 == 0;
            bool phraseEnd = s % 16 >= 14;
            if (!onBeat || phraseEnd || rng.NextDouble() < 0.18) { melody[s] = -1; continue; }
            idx = Mathf.Clamp(idx + rng.Next(-2, 3), 1, scale.Length - 1);
            melody[s] = (s % 16 == 12) ? (rng.NextDouble() < 0.5 ? 2 : 5) : idx; // phrases settle on the root or fifth
        }

        for (int bar = 0; bar < bars; bar++)
        {
            int barStart = (int)(bar * stepsPerBar * step * Rate);
            float chord = chordRoots[bar % 4];
            float bass = track.Root * 0.25f * Semis(chord);
            Pad(d, barStart, (int)(stepsPerBar * step * Rate), bass);
            Pluck(d, barStart, bass, 0.22f, 0.3f, rng, 0.993f);
            Pluck(d, barStart + (int)(4 * step * Rate), bass * 1.5f, 0.14f, 0.3f, rng, 0.993f);

            for (int s = 0; s < stepsPerBar; s++)
            {
                int m = (bar % 8) * stepsPerBar + s;
                int note = melody[m];
                if (bar >= 14 && s == 0) note = bar == 15 ? 2 : 4; // the second half ends home
                if (note < 0) continue;
                float f = track.Root * Semis(scale[note]);
                Pluck(d, barStart + (int)(s * step * Rate), f, 0.3f, track.Brightness, rng, 0.9965f);
            }
        }

        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] *= 0.6f / max;
        return d;
    }

    private static float Semis(float n) => Mathf.Pow(2f, n / 12f);

    // Upbeat band loop for the beach concerts: kick, snare, hi-hat, bass and a bright plucked riff
    private static float[] Concert()
    {
        const float bpm = 124f;
        const int bars = 8, steps = 16; // sixteenth notes
        float step = 60f / bpm / 4f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(42);
        float root = 110f;
        float[] chords = { 0, 9, 5, 7 };               // I – vi – IV – V
        int[] riff = { 12, 16, 19, 16, 21, 19, 16, 14 }; // pentatonic, over the chord root
        for (int bar = 0; bar < bars; bar++)
        {
            float chord = chords[bar % 4];
            for (int s = 0; s < steps; s++)
            {
                int at = (int)((bar * steps + s) * step * Rate);
                if (s % 4 == 0) Kick(d, at);
                if (s == 4 || s == 12) Snare(d, at, rng);
                if (s % 2 == 0) Hat(d, at, rng, s % 4 == 2 ? 0.12f : 0.07f);
                if (s % 4 == 0 || s == 7 || s == 15) Pluck(d, at, root * Semis(chord), 0.35f, 0.4f, rng, 0.99f);
                if (s % 2 == 0) Pluck(d, at, root * 2f * Semis(chord + riff[(s / 2 + bar) % riff.Length] - 12f), 0.18f, 0.95f, rng, 0.994f);
            }
        }
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] *= 0.7f / max;
        return d;
    }

    private static void Kick(float[] d, int start)
    {
        int len = (int)(0.25f * Rate);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            phase += 2f * Mathf.PI * (50f + 90f * Mathf.Exp(-t * 30f)) / Rate;
            d[(start + i) % d.Length] += Mathf.Sin(phase) * Mathf.Exp(-t * 12f) * 0.9f;
        }
    }

    private static void Snare(float[] d, int start, System.Random rng)
    {
        int len = (int)(0.18f * Rate);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            d[(start + i) % d.Length] += (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.3f) * Mathf.Exp(-t * 22f);
        }
    }

    private static void Hat(float[] d, int start, System.Random rng, float amp)
    {
        int len = (int)(0.05f * Rate);
        float prev = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float high = noise - prev; // crude high-pass: only the hiss
            prev = noise;
            d[(start + i) % d.Length] += high * amp * Mathf.Exp(-t * 80f);
        }
    }

    // Karplus-Strong plucked string (sounds like a khim / pin); wraps around the loop end
    private static void Pluck(float[] d, int start, float f, float amp, float brightness, System.Random rng, float sustain)
    {
        int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
        var buf = new float[period];
        float lp = 0f, smooth = Mathf.Lerp(0.25f, 0.9f, brightness);
        for (int i = 0; i < period; i++)
        {
            lp += ((float)rng.NextDouble() * 2f - 1f - lp) * smooth;
            buf[i] = lp;
        }
        int len = (int)(2.6f * Rate);
        for (int i = 0; i < len; i++)
        {
            int a = i % period, b = (i + 1) % period;
            float v = buf[a];
            buf[a] = sustain * 0.5f * (buf[a] + buf[b]);
            float fadeIn = Mathf.Min(1f, i / 40f);
            d[(start + i) % d.Length] += v * amp * fadeIn;
        }
    }

    // Quiet sustained chord swelling over the bar
    private static void Pad(float[] d, int start, int len, float bass)
    {
        float[] ratio = { 2f, 3f, 4f * Semis(4) };
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Sin(Mathf.PI * i / len);
            float v = 0f;
            foreach (float r in ratio)
                v += Mathf.Sin(2f * Mathf.PI * bass * r * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * bass * r * 1.003f * t);
            d[(start + i) % d.Length] += v * env * 0.025f;
        }
    }
}
