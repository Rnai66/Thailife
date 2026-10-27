using UnityEngine;

// Background music made in code, one loop per place (the village plays the recorded ลายไทย tune):
//   bgm_tambon  ตำบล — a plucked khim-like melody on the Thai pentatonic over bass and pad
//   bgm_city    เมือง — lo-fi city beat: swung drums, electric-piano 7th chords, walking bass
//   bgm_sea     ทะเล — tropical: steel-pan tune, off-beat strums, shaker, bouncy bass
//   bgm_island  เกาะ — dreamy and slow: soft pads and a bell arpeggio, no drums
// plus the stage loops (concert, morlam, ramwong). Notes wrap around so every loop is seamless.
// Everything sits in D, the key of the recorded tune (Thaimusic), so nothing clashes when
// one track fades into another or a stage plays nearby.
public static class SynthMusic
{
    private const int Rate = SynthSounds.Rate;
    private const float D3 = 146.83f, D4 = 293.66f;

    private struct Track
    {
        public float Root, Bpm;
        public int Seed;
        public float Brightness; // pluck tone: 0 mellow … 1 bright
    }

    private static readonly Track Tambon = new Track { Root = D4, Bpm = 108f, Seed = 3, Brightness = 0.5f };

    public static float[] Make(string name)
    {
        switch (name)
        {
            case "concert": return Concert();
            case "morlam": return MorLam();
            case "ramwong": return Ramwong();
            case "bgm_tambon": return Render(Tambon);
            case "bgm_city": return City();
            case "bgm_sea": return Sea();
            case "bgm_island": return Island();
            default: return null;
        }
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
        const float bpm = 115f;
        const int bars = 8, steps = 16; // sixteenth notes
        float step = 60f / bpm / 4f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(42);
        float root = 146.83f; // D
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

    // หมอลำซิ่ง: a fast khaen drone (reedy fifths), a phin riff in the minor pentatonic, a driving
    // kick on every beat, ฉิ่ง (small cymbals) on the off-beats
    private static float[] MorLam()
    {
        const float bpm = 138f; // 1.2 × 115
        const int bars = 8, steps = 16;
        float step = 60f / bpm / 4f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(77);
        float root = 146.83f; // D
        int[] riff = { 12, 15, 17, 19, 22, 19, 17, 15, 12, 10, 12, 15, 17, 15, 12, 7 };
        // Khaen: reedy drone of root, fifth and octave, breathing with each bar
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / Rate;
            float breath = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t * bpm / 60f / 4f);
            float v = 0f;
            foreach (float r in new[] { 1f, 1.5f, 2f })
            {
                float ph = (t * root * r) % 1f;
                v += (ph * 2f - 1f) * 0.5f + Mathf.Sin(2f * Mathf.PI * root * r * 1.004f * t) * 0.4f;
            }
            d[i] += v * breath * 0.035f;
        }
        for (int bar = 0; bar < bars; bar++)
            for (int s = 0; s < steps; s++)
            {
                int at = (int)((bar * steps + s) * step * Rate);
                if (s % 4 == 0) Kick(d, at);
                if (s % 4 == 2) Hat(d, at, rng, 0.16f);                              // ฉิ่ง
                if (s == 6 || s == 14) Snare(d, at, rng);
                Pluck(d, at, root * Semis(riff[(s + bar * 3) % riff.Length]), s % 2 == 0 ? 0.3f : 0.16f, 0.9f, rng, 0.992f); // พิณ
            }
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] *= 0.7f / max;
        return d;
    }

    // รำวง: an easy 2/4 sway — กลองโทน on the beats, ฉิ่ง-ฉับ ticking, a sweet ระนาด/พิณ tune over a
    // soft pad; unhurried so the circle can walk and turn its hands
    private static float[] Ramwong()
    {
        const float bpm = 115f;
        const int bars = 8, steps = 8; // eighth notes
        float step = 60f / bpm / 2f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(19);
        float root = 293.66f; // D
        int[] tune = { 0, 2, 4, 7, 9, 7, 4, 2, 4, 7, 9, 12, 9, 7, 4, 7, 9, 7, 4, 2, 0, 2, 4, 2, 0, -3, 0, 2, 4, 2, 0, 0 };
        float[] chords = { 0, 0, 5, 7, 0, 9, 5, 7 };
        for (int bar = 0; bar < bars; bar++)
        {
            int barStart = (int)(bar * steps * step * Rate);
            Pad(d, barStart, (int)(steps * step * Rate), root * 0.25f * Semis(chords[bar]));
            for (int s = 0; s < steps; s++)
            {
                int at = (int)((bar * steps + s) * step * Rate);
                if (s % 4 == 0) Kick(d, at);                                   // ตุ้ม (low tone drum)
                if (s % 4 == 2) Snare(d, at, rng);                             // ป๊ะ
                Hat(d, at, rng, s % 2 == 0 ? 0.12f : 0.06f);                   // ฉิ่ง-ฉับ
                if (s % 2 == 0)
                    Pluck(d, at, root * Semis(tune[(bar * 4 + s / 2) % tune.Length]), 0.32f, 0.85f, rng, 0.993f);
            }
        }
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] *= 0.7f / max;
        return d;
    }

    // Chords as semitones from D4: Dmaj7 – Bm7 – Gmaj7 – A7 (I – vi – IV – V, with sevenths)
    private static readonly float[][] Sevenths =
    {
        new float[] { 0, 4, 7, 11 }, new float[] { -3, 0, 4, 7 }, new float[] { -7, -3, 0, 4 }, new float[] { -5, -1, 2, 5 },
    };
    private static readonly float[] SeventhRoots = { 0, -3, -7, -5 };

    // เมือง: lo-fi beat at 84 BPM — lazy swung hats, a soft kick and snare, Rhodes-like chord stabs
    private static float[] City()
    {
        const float bpm = 84f;
        const int bars = 8, steps = 16;
        float step = 60f / bpm / 4f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(31);
        float[] scale = { 0, 2, 4, 7, 9, 12, 14, 16 };
        for (int bar = 0; bar < bars; bar++)
        {
            var chord = Sevenths[bar % 4];
            float bass = D3 * 0.5f * Semis(SeventhRoots[bar % 4]);
            for (int s = 0; s < steps; s++)
            {
                float swing = s % 2 == 1 ? 0.3f * step : 0f;                     // late off-beats
                int at = (int)(((bar * steps + s) * step + swing) * Rate);
                if (s == 0 || s == 7 || s == 10) Kick(d, at);
                if (s == 4 || s == 12) Snare(d, at, rng);
                if (s % 2 == 0 || s == 15) Hat(d, at, rng, s % 4 == 0 ? 0.05f : 0.09f);
                if (s == 0 || s == 10) Keys(d, at, chord, D4, s == 0 ? 0.12f : 0.08f, (int)(step * 6 * Rate));
                if (s == 0 || s == 6 || s == 8 || s == 14)
                    Pluck(d, at, bass * (s == 14 ? Semis(7) : 1f), 0.32f, 0.2f, rng, 0.995f);
                if ((bar % 2 == 1) && s % 4 == 2 && rng.NextDouble() < 0.6)                  // a few lazy lead notes
                    Pluck(d, at, D4 * 2f * Semis(scale[rng.Next(scale.Length)] - 12f), 0.12f, 0.6f, rng, 0.996f);
            }
        }
        return Normalize(d, 0.6f);
    }

    // ทะเล: tropical at 115 BPM — steel pan tune, off-beat strums, shaker, bouncy bass
    private static float[] Sea()
    {
        const float bpm = 115f;
        const int bars = 8, steps = 16;
        float step = 60f / bpm / 4f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        var rng = new System.Random(57);
        float[] roots = { 0, 5, 7, 0, 0, 5, 7, 7 };                   // D – G – A – D
        float[] scale = { 0, 2, 4, 7, 9, 12, 14, 16, 19 };
        int[] tune = { 4, 5, 6, 5, 4, 3, 2, -1, 3, 4, 5, 7, 6, 5, 4, -1, 5, 6, 7, 8, 7, 6, 5, 4, 3, 4, 3, 2, 1, 2, 0, -1 };
        for (int bar = 0; bar < bars; bar++)
        {
            float root = roots[bar];
            float bass = D3 * 0.5f * Semis(root);
            for (int s = 0; s < steps; s++)
            {
                int at = (int)((bar * steps + s) * step * Rate);
                if (s == 0 || s == 8) Kick(d, at);
                Hat(d, at, rng, s % 4 == 2 ? 0.08f : 0.035f);                                  // shaker
                if (s % 4 == 2)                                                                 // off-beat strum
                    foreach (float n in new[] { 0f, 4f, 7f })
                        Pluck(d, at + (int)(n * 0.002f * Rate), D4 * Semis(root + n), 0.09f, 0.85f, rng, 0.985f);
                if (s == 0 || s == 3 || s == 8 || s == 11)
                    Pluck(d, at, bass * (s == 11 ? Semis(7) : 1f), 0.3f, 0.3f, rng, 0.994f);
                if (s % 2 == 0)
                {
                    int note = tune[(bar % 4) * 8 + s / 2];
                    if (bar >= 4 && bar % 2 == 1 && s >= 8) note = note < 0 ? -1 : Mathf.Min(note + 2, scale.Length - 1); // answer phrase
                    if (note >= 0) Pan(d, at, D4 * 2f * Semis(scale[note]), 0.18f);
                }
            }
        }
        return Normalize(d, 0.6f);
    }

    // เกาะ: dreamy at 66 BPM — soft chord pads and a slow bell arpeggio, like a warm evening on the sand
    private static float[] Island()
    {
        const float bpm = 66f;
        const int bars = 8, steps = 8;
        float step = 60f / bpm / 2f;
        int length = Mathf.CeilToInt(bars * steps * step * Rate);
        var d = new float[length];
        int[] order = { 0, 1, 2, 3, 2, 1, 3, 2 };
        for (int bar = 0; bar < bars; bar++)
        {
            var chord = Sevenths[(bar / 2) % 4];
            int barStart = (int)(bar * steps * step * Rate);
            if (bar % 2 == 0) Swell(d, barStart, (int)(2 * steps * step * Rate), chord, D3);
            for (int s = 0; s < steps; s++)
            {
                int at = barStart + (int)(s * step * Rate);
                float n = chord[order[s]] + (s >= 4 ? 12f : 0f);
                Bell(d, at, D4 * Semis(n), s == 0 ? 0.16f : 0.1f);
            }
        }
        return Normalize(d, 0.55f);
    }

    private static float[] Normalize(float[] d, float peak)
    {
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] *= peak / max;
        return d;
    }

    // Electric-piano chord: sines with a bell-ish second partial and a slow tremolo
    private static void Keys(float[] d, int start, float[] chord, float root, float amp, int len)
    {
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 1.6f) * Mathf.Min(1f, t * 200f) * Mathf.Min(1f, (len - i) / (0.05f * Rate));
            float trem = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 4.5f * t);
            float v = 0f;
            foreach (float n in chord)
            {
                float f = root * Semis(n);
                v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 2f * t) * Mathf.Exp(-t * 6f);
            }
            d[(start + i) % d.Length] += v * env * trem * amp * 0.5f;
        }
    }

    // Steel pan: fundamental, octave and a touch of the twelfth, quick bright attack
    private static void Pan(float[] d, int start, float f, float amp)
    {
        int len = (int)(0.9f * Rate);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float v = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 3.5f)
                    + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2f * t) * Mathf.Exp(-t * 6f)
                    + 0.2f * Mathf.Sin(2f * Mathf.PI * f * 3f * t) * Mathf.Exp(-t * 12f);
            d[(start + i) % d.Length] += v * amp * Mathf.Min(1f, t * 300f);
        }
    }

    // Soft glass bell with a long tail
    private static void Bell(float[] d, int start, float f, float amp)
    {
        int len = (int)(2.4f * Rate);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float v = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 1.6f)
                    + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 4f);
            d[(start + i) % d.Length] += v * amp * Mathf.Min(1f, t * 150f);
        }
    }

    // Slow pad on a whole chord, rising and falling over its length, slightly detuned for width
    private static void Swell(float[] d, int start, int len, float[] chord, float root)
    {
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Sin(Mathf.PI * i / len);
            float v = 0f;
            foreach (float n in chord)
            {
                float f = root * Semis(n);
                v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * f * 1.004f * t);
            }
            d[(start + i) % d.Length] += v * env * env * 0.05f;
        }
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
