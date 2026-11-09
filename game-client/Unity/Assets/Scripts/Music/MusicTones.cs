using System.Collections.Generic;
using UnityEngine;

// The sound of each instrument in the card game (MusicUI), made in code like the rest of the game's
// audio: a plucked string (พิณ, กีต้าร์, เบส — Karplus-Strong), a struck bar (ระนาด), an electric
// piano (คีย์บอร์ด), a free reed (แคน: several pipes at once, breathy), a bowed string (ซอ, with
// vibrato), and a drum kit. One clip per instrument and note, made the first time it is played.
public static class MusicTones
{
    private const int Rate = 22050;
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    // The octave each instrument plays a pitch class in (MIDI: middle C = 60)
    public static int MidiFor(string instrument, int pc)
    {
        int c = instrument == "bass" ? 36 : instrument == "pin" || instrument == "ranat" || instrument == "so" ? 72 : 60;
        return c + pc;
    }

    public static AudioClip Note(string instrument, int midi)
    {
        string key = instrument + midi;
        if (cache.TryGetValue(key, out var clip) && clip != null) return clip;
        float f = 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        float[] data;
        switch (instrument)
        {
            case "ranat": data = Mallet(f); break;
            case "keyboard": data = EPiano(f); break;
            case "khaen": data = Reed(f); break;
            case "so": data = Bowed(f); break;
            case "bass": data = Pluck(f, 1.4f, 0.996f, 0.5f); break;
            case "guitar": data = Pluck(f, 1.3f, 0.995f, 0.5f); break;
            default: data = Pluck(f, 1.0f, 0.992f, 0.25f); break; // พิณ: bright and short
        }
        return cache[key] = Make(key, data);
    }

    public static AudioClip Drum(int index)
    {
        string key = "drum" + index;
        if (cache.TryGetValue(key, out var clip) && clip != null) return clip;
        float[] data = index == 0 ? Kick() : index == 1 ? Snare() : index == 2 ? Hat(0.12f) : index == 3 ? Tom() : Hat(1.1f);
        return cache[key] = Make(key, data);
    }

    // A wrong note: a dull muted thud
    public static AudioClip Dud()
    {
        if (cache.TryGetValue("dud", out var clip) && clip != null) return clip;
        int n = (int)(Rate * 0.18f);
        var d = new float[n];
        var rng = new System.Random(3);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.6f + (float)(rng.NextDouble() * 2 - 1) * 0.25f) * Mathf.Exp(-t * 28f) * 0.6f;
        }
        return cache["dud"] = Make("dud", d);
    }

    private static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create("Tone_" + name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Karplus-Strong: a burst of noise round a delay line the length of one period, softened each pass
    private static float[] Pluck(float f, float seconds, float decay, float brightness)
    {
        int n = (int)(Rate * seconds);
        int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
        var line = new float[period];
        var rng = new System.Random(period);
        float prev = 0f;
        for (int i = 0; i < period; i++) // a pick: noise, smoothed for a warmer tone
        {
            float noise = (float)(rng.NextDouble() * 2 - 1);
            prev = prev + (noise - prev) * (0.35f + brightness);
            line[i] = prev;
        }
        var d = new float[n];
        int at = 0;
        for (int i = 0; i < n; i++)
        {
            int next = (at + 1) % period;
            float v = line[at];
            line[at] = decay * 0.5f * (line[at] + line[next]);
            d[i] = v * 0.7f;
            at = next;
        }
        Fade(d, 0.03f);
        return d;
    }

    // Struck hardwood bar: a few inharmonic partials that ring out quickly
    private static float[] Mallet(float f)
    {
        int n = (int)(Rate * 0.9f);
        var d = new float[n];
        float[] ratio = { 1f, 3.93f, 9.5f }, amp = { 1f, 0.35f, 0.12f }, damp = { 5f, 14f, 30f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, v = 0f;
            for (int k = 0; k < 3; k++) v += Mathf.Sin(2f * Mathf.PI * f * ratio[k] * t) * amp[k] * Mathf.Exp(-t * damp[k]);
            d[i] = v * 0.45f * Mathf.Min(1f, t * 400f);
        }
        return d;
    }

    // Electric piano: a bell-ish FM tone
    private static float[] EPiano(float f)
    {
        int n = (int)(Rate * 1.3f);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float mod = Mathf.Sin(2f * Mathf.PI * f * t) * 1.6f * Mathf.Exp(-t * 6f);
            d[i] = (Mathf.Sin(2f * Mathf.PI * f * t + mod) * 0.8f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.1f)
                   * Mathf.Exp(-t * 2.4f) * Mathf.Min(1f, t * 300f) * 0.5f;
        }
        return d;
    }

    // แคน: a buzzing free reed with its drone a fifth below, breathed in and out
    private static float[] Reed(float f)
    {
        int n = (int)(Rate * 0.9f);
        var d = new float[n];
        var rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, v = 0f;
            for (int k = 1; k <= 7; k++) v += Mathf.Sin(2f * Mathf.PI * f * k * t) / (k * (k % 2 == 0 ? 2.2f : 1f));
            v += Mathf.Sin(2f * Mathf.PI * f * 0.667f * t) * 0.25f;
            v += (float)(rng.NextDouble() * 2 - 1) * 0.04f;
            float env = Mathf.Min(1f, t * 14f) * Mathf.Min(1f, (0.9f - t) * 6f);
            d[i] = v * env * 0.28f;
        }
        return d;
    }

    // ซอ: a bowed string, sawtooth softened, slow bow and vibrato
    private static float[] Bowed(float f)
    {
        int n = (int)(Rate * 0.9f);
        var d = new float[n];
        float phase = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float vib = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Min(1f, t * 3f);
            phase += f * vib / Rate;
            float saw = (phase % 1f) * 2f - 1f;
            lp += (saw - lp) * 0.25f;
            float env = Mathf.Min(1f, t * 8f) * Mathf.Min(1f, (0.9f - t) * 5f);
            d[i] = lp * env * 0.4f;
        }
        return d;
    }

    private static float[] Kick()
    {
        int n = (int)(Rate * 0.45f);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            phase += (50f + 110f * Mathf.Exp(-t * 30f)) / Rate;
            d[i] = Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 7f) * 0.9f;
        }
        return d;
    }

    private static float[] Snare()
    {
        int n = (int)(Rate * 0.3f);
        var d = new float[n];
        var rng = new System.Random(11);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            d[i] = ((float)(rng.NextDouble() * 2 - 1) * 0.6f * Mathf.Exp(-t * 14f) + Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.5f * Mathf.Exp(-t * 25f)) * 0.7f;
        }
        return d;
    }

    private static float[] Tom()
    {
        int n = (int)(Rate * 0.5f);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            phase += (130f + 60f * Mathf.Exp(-t * 12f)) / Rate;
            d[i] = Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 6f) * 0.75f;
        }
        return d;
    }

    // Hi-hat (short) and crash (long): bright noise
    private static float[] Hat(float seconds)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        var rng = new System.Random(5);
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float w = (float)(rng.NextDouble() * 2 - 1);
            float hp = w - prev; // high-passed
            prev = w;
            d[i] = hp * 0.35f * Mathf.Exp(-t * (seconds < 0.5f ? 40f : 3.5f));
        }
        return d;
    }

    private static void Fade(float[] d, float seconds)
    {
        int m = Mathf.Min(d.Length, (int)(Rate * seconds));
        for (int i = 0; i < m; i++) d[d.Length - 1 - i] *= i / (float)m;
    }
}
