using UnityEngine;

// Festive decorations (ประดับไฟ) for the temple fair and the event grounds: strings of coloured
// bulbs, round lanterns (โคมไฟ) and flag bunting (ธงราว). Emissive, so the bloom makes them glow
// at night. Wrap them in a FestivalGate so they only go up during a festival.
public static class FairLights
{
    private static readonly Color[] BulbColors =
    {
        new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.35f, 1f, 0.45f),
        new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.85f),
    };

    private static readonly Color[] FlagColors =
    {
        new Color(0.85f, 0.15f, 0.15f), new Color(1f, 0.85f, 0.15f), new Color(0.15f, 0.55f, 0.3f),
        new Color(0.2f, 0.4f, 0.85f), Color.white,
    };

    // A group that only shows during festivals (scale: "any" | "small" | "big")
    public static Transform Gated(Transform parent, string name, string scale)
    {
        var root = PrimitiveKit.Group(name, parent, Vector3.zero);
        var lights = PrimitiveKit.Group("Attractions", root.transform, Vector3.zero);
        root.AddComponent<FestivalGate>().Configure(scale, null);
        return lights.transform;
    }

    // Thin pole the strings hang from
    public static void Pole(Transform parent, Vector3 foot, float height = 4.6f)
    {
        PrimitiveKit.Cylinder("LightPole", parent, foot + Vector3.up * height * 0.5f, new Vector3(0.09f, height * 0.5f, 0.09f),
            PrimitiveKit.Mat("BambooPole", new Color(0.72f, 0.62f, 0.38f)));
    }

    // String of multicoloured bulbs from a to b (parent space), sagging in the middle
    public static void Festoon(Transform parent, Vector3 a, Vector3 b, float sag = 0.6f, float spacing = 0.6f)
    {
        var path = Sag(a, b, sag, out int count, spacing);
        MeshKit.Place("FestoonWire", parent, MeshKit.Sweep("Wires/" + Key("Festoon", a, b), path, MeshKit.Taper(path.Length, 0.01f, 0.01f), 4),
            PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f)), Vector3.zero).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 1; i < path.Length - 1; i++)
        {
            var c = BulbColors[i % BulbColors.Length];
            var bulb = PrimitiveKit.Sphere("Bulb", parent, path[i] + Vector3.down * 0.08f, Vector3.one * 0.2f, PrimitiveKit.Glow($"FairBulb{i % BulbColors.Length}", c, 20f, 0.3f));
            bulb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Real coloured lights every ~8 m along the string, so the glow falls on the ground and stalls
        int lights = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / 6f));
        for (int k = 0; k < lights; k++)
        {
            var at = path[Mathf.Clamp(Mathf.RoundToInt((k + 0.5f) / lights * (path.Length - 1)), 0, path.Length - 1)] + Vector3.down * 0.3f;
            var lightGo = new GameObject("FestoonLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = at;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = BulbColors[(int)(Mathf.Abs(at.x * 3f + at.z * 7f)) % BulbColors.Length];
            light.range = 9f;
            light.intensity = 12f;
            light.shadows = LightShadows.None;
        }
    }

    // Triangular flags (ธงราว) along a sagging cord
    public static void Bunting(Transform parent, Vector3 a, Vector3 b, float sag = 0.35f)
    {
        var path = Sag(a, b, sag, out int count, 0.55f);
        MeshKit.Place("BuntingCord", parent, MeshKit.Sweep("Wires/" + Key("Bunting", a, b), path, MeshKit.Taper(path.Length, 0.008f, 0.008f), 4),
            PrimitiveKit.Mat("RopeWhite", Color.white), Vector3.zero);
        var flag = MeshKit.Gable("BuntingFlag", 0.36f, 0.42f, 0.01f);
        Vector3 along = (b - a);
        along.y = 0f;
        float yaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg + 90f;
        for (int i = 1; i < path.Length - 1; i++)
            MeshKit.Place("Flag", parent, flag, PrimitiveKit.Mat($"Bunting{i % FlagColors.Length}", FlagColors[i % FlagColors.Length]),
                path[i], new Vector3(180f, yaw, 0f)); // point down
    }

    // Round paper lantern hanging from a short cord
    public static void Lantern(Transform parent, Vector3 hangPoint, int colorIndex = 0)
    {
        Color[] colors = { new Color(1f, 0.25f, 0.2f), new Color(1f, 0.7f, 0.25f), new Color(1f, 0.45f, 0.7f) };
        var c = colors[colorIndex % colors.Length];
        PrimitiveKit.Cylinder("Cord", parent, hangPoint + Vector3.down * 0.15f, new Vector3(0.02f, 0.15f, 0.02f), PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f)), collider: false);
        MeshKit.Place("Lantern", parent, MeshKit.Lathe("Lantern", new[]
        {
            new Vector2(0.12f, 0f), new Vector2(0.3f, 0.08f), new Vector2(0.36f, 0.28f), new Vector2(0.3f, 0.48f), new Vector2(0.12f, 0.56f),
        }, 16), PrimitiveKit.Glow($"Lantern{colorIndex % colors.Length}", c, 9f, 0.45f), hangPoint + Vector3.down * 0.86f);
    }

    private static Vector3[] Sag(Vector3 a, Vector3 b, float sag, out int count, float spacing)
    {
        count = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a, b) / spacing), 4, 60);
        return MeshKit.Curve(a, (a + b) * 0.5f + Vector3.down * sag * 2f, b, count + 1);
    }

    private static string Key(string kind, Vector3 a, Vector3 b) =>
        $"{kind}_{a.x:0.0}_{a.y:0.0}_{a.z:0.0}_{b.x:0.0}_{b.y:0.0}_{b.z:0.0}".Replace('-', 'm');
}
