using System.Collections.Generic;
using UnityEngine;

// Recorded sounds from Kenney's CC0 packs (Resources/Audio/Kenney): punches, footsteps per surface
// and coins. Each sound has a few takes; a random one plays so repeats don't sound robotic.
public static class KenneySounds
{
    private static readonly Dictionary<string, string[]> Takes = new Dictionary<string, string[]>
    {
        { "punch", Range("impactPunch_medium_00", 5) },
        { "punch_heavy", Range("impactPunch_heavy_00", 5) },
        { "step_grass", Range("footstep_grass_00", 5) },
        { "step_concrete", Range("footstep_concrete_00", 5) },
        { "step_wood", Range("footstep_wood_00", 5) },
        { "step_sand", Range("footstep_snow_00", 5) },
        { "coins", new[] { "handleCoins", "handleCoins2" } },
    };

    private static readonly Dictionary<string, AudioClip[]> loaded = new Dictionary<string, AudioClip[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => loaded.Clear();

    private static string[] Range(string prefix, int count)
    {
        var names = new string[count];
        for (int i = 0; i < count; i++) names[i] = prefix + i;
        return names;
    }

    // A random take of the sound, or null if there's no recording for it (the synth one is used then)
    public static AudioClip Get(string name)
    {
        if (!loaded.TryGetValue(name, out var clips))
        {
            var list = new List<AudioClip>();
            if (Takes.TryGetValue(name, out var files))
                foreach (var f in files)
                {
                    var clip = Resources.Load<AudioClip>("Audio/Kenney/" + f);
                    if (clip != null) list.Add(clip);
                }
            clips = list.ToArray();
            loaded[name] = clips;
        }
        return clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];
    }
}
