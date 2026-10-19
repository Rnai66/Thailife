using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Which place gets which sound. Finds the places in a freshly loaded scene by the names the scene
// builders give them and adds an AmbientZone to each (so no scene rebuild is needed).
public static class AmbientPlaces
{
    private struct Rule
    {
        public string Prefix;
        public float Range, MinGap, MaxGap, Volume;
        public string[] Sounds;
    }

    private static readonly Rule[] Rules =
    {
        new Rule { Prefix = "Temple_", Range = 12f, MinGap = 60f, MaxGap = 110f, Volume = 0.55f, Sounds = new[] { "chant", "temple_bell" } },
        new Rule { Prefix = "CityTemple_", Range = 10f, MinGap = 60f, MaxGap = 110f, Volume = 0.55f, Sounds = new[] { "chant", "temple_bell" } },
        new Rule { Prefix = "Church_", Range = 8f, MinGap = 90f, MaxGap = 150f, Volume = 0.5f, Sounds = new[] { "church_bell" } },
        new Rule { Prefix = "KokNongNa_", Range = 6f, MinGap = 45f, MaxGap = 90f, Volume = 0.8f, Sounds = new[] { "buffalo", "frog" } },
        new Rule { Prefix = "CowFarm_", Range = 6f, MinGap = 35f, MaxGap = 70f, Volume = 0.8f, Sounds = new[] { "cow" } },
        new Rule { Prefix = "Canal_", Range = 5f, MinGap = 50f, MaxGap = 100f, Volume = 0.6f, Sounds = new[] { "frog" } },
        new Rule { Prefix = "FishingPier_", Range = 20f, MinGap = 30f, MaxGap = 60f, Volume = 0.6f, Sounds = new[] { "seagull" } },
        new Rule { Prefix = "School_", Range = 6f, MinGap = 120f, MaxGap = 200f, Volume = 0.45f, Sounds = new[] { "school_bell" } },
        new Rule { Prefix = "Hospital_", Range = 5f, MinGap = 70f, MaxGap = 120f, Volume = 0.5f, Sounds = new[] { "pa_chime" } },
        new Rule { Prefix = "HealthStation_", Range = 5f, MinGap = 70f, MaxGap = 120f, Volume = 0.45f, Sounds = new[] { "pa_chime" } },
        new Rule { Prefix = "BoxingCamp_", Range = 6f, MinGap = 25f, MaxGap = 50f, Volume = 0.45f, Sounds = new[] { "punch", "punch_heavy" } },
        new Rule { Prefix = "FreshMarket_", Range = 4f, MinGap = 40f, MaxGap = 80f, Volume = 0.6f, Sounds = new[] { "coins" } },
        new Rule { Prefix = "FleaMarket_", Range = 4f, MinGap = 40f, MaxGap = 80f, Volume = 0.6f, Sounds = new[] { "coins" } },
        new Rule { Prefix = "BankZone", Range = 3f, MinGap = 60f, MaxGap = 120f, Volume = 0.5f, Sounds = new[] { "coins" } },
    };

    // Shops whose counter rings the door chime as you walk up (like a minimart door)
    private static readonly HashSet<string> NoChime = new HashSet<string> { "market", "flea", "flea_furn", "noodle", "beach", "camp" };

    public static void Attach(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        var prepared = new HashSet<string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<AmbientZone>() != null) continue;
                foreach (var rule in Rules)
                {
                    if (!t.name.StartsWith(rule.Prefix)) continue;
                    Add(t, BoundsOf(t), rule.Range, rule.MinGap, rule.MaxGap, rule.Volume, rule.Sounds, prepared);
                    break;
                }
                var shop = t.GetComponent<ShopCounter>();
                if (shop != null && !NoChime.Contains(shop.ShopId))
                    Add(t, new Bounds(t.position, Vector3.one), 3f, 90f, 180f, 0.6f, new[] { "door_chime" }, prepared);
            }
    }

    private static void Add(Transform t, Bounds bounds, float range, float minGap, float maxGap, float volume, string[] sounds, HashSet<string> prepared)
    {
        t.gameObject.AddComponent<AmbientZone>().Configure(bounds, range, minGap, maxGap, volume, sounds);
        foreach (var s in sounds)
            if (prepared.Add(s)) SynthSounds.Prepare(s); // make it now, so the first "arrived" sound is ready
    }

    // The place's footprint (all its renderers), or just its position for empty markers
    private static Bounds BoundsOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.one);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
