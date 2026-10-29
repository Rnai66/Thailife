using System.Collections.Generic;
using UnityEngine;

// High-rise towers for a big-city scene (Bangkok, later abroad); not used in the provincial City.
// Each one its own design:
//   GlassFins — curtain-wall office with vertical fins and a crown (flat, spire or raked top)
//   Stepped   — stone tower set back in tiers, ribbon windows, art-deco spire
//   Round     — cylindrical glass tower ringed with floor slabs
//   Condo     — residential slab with balconies on every floor and a roof garden
//   Twist     — floors turning a few degrees each, a spiral of glass
//   Twin      — two slim towers joined by a sky bridge
// Materials vary (blue, teal, bronze, smoke and green glass; white, cream, terracotta, grey
// stone). After dark TowerLights lights about half the rooms, softly, and only some towers carry
// LED accents; tall roofs blink red. North of town a landmark of three towers carries a sky park.
public static class CitySkyline
{
    private const float FH = 3.4f; // floor height

    private class Kit
    {
        public Transform T, Night;
        public readonly List<Renderer> Faces = new List<Renderer>(), Accents = new List<Renderer>(), Beacons = new List<Renderer>();
        public readonly List<int> First = new List<int>(), Count = new List<int>();
        public readonly List<Transform> Beams = new List<Transform>();
    }

    private struct Lights
    {
        public TowerLights.Show Show;
        public Color Color;
        public Lights(TowerLights.Show show, Color color) { Show = show; Color = color; }
    }

    private static readonly Color White = new Color(0.9f, 0.95f, 1f), Gold = new Color(1f, 0.72f, 0.35f),
        Cyan = new Color(0.3f, 0.8f, 1f), Magenta = new Color(0.85f, 0.35f, 0.95f), Warm = new Color(1f, 0.88f, 0.7f);

    // A mix: most towers just have their rooms lit; some a steady crown, a few a slow show
    private static readonly Lights[] Schemes =
    {
        new Lights(TowerLights.Show.None, White), new Lights(TowerLights.Show.Steady, White), new Lights(TowerLights.Show.None, White),
        new Lights(TowerLights.Show.Breathe, Cyan), new Lights(TowerLights.Show.Steady, Gold), new Lights(TowerLights.Show.None, White),
        new Lights(TowerLights.Show.Rainbow, White), new Lights(TowerLights.Show.None, White), new Lights(TowerLights.Show.Chase, Magenta),
        new Lights(TowerLights.Show.Steady, Warm), new Lights(TowerLights.Show.None, White), new Lights(TowerLights.Show.Breathe, Gold),
    };

    private static Material Glass(int i)
    {
        Color[] c = { new Color(0.2f, 0.32f, 0.45f), new Color(0.2f, 0.4f, 0.42f), new Color(0.42f, 0.34f, 0.24f), new Color(0.16f, 0.18f, 0.22f), new Color(0.25f, 0.38f, 0.32f), new Color(0.45f, 0.52f, 0.58f) };
        int k = i % c.Length;
        return PrimitiveKit.Mat("SkyGlass" + k, c[k], 0.92f, 0.7f);
    }

    private static Material Stone(int i)
    {
        Color[] c = { new Color(0.92f, 0.9f, 0.86f), new Color(0.88f, 0.8f, 0.66f), new Color(0.72f, 0.46f, 0.34f), new Color(0.62f, 0.62f, 0.64f), new Color(0.82f, 0.84f, 0.86f) };
        int k = i % c.Length;
        return PrimitiveKit.Mat("SkyStone" + k, c[k], 0.3f, 0f);
    }

    private static Material Trim(int i)
    {
        Color[] c = { new Color(0.85f, 0.87f, 0.9f), new Color(0.18f, 0.18f, 0.2f), new Color(0.55f, 0.42f, 0.28f), new Color(0.95f, 0.95f, 0.93f) };
        int k = i % c.Length;
        return PrimitiveKit.Mat("SkyTrim" + k, c[k], 0.6f, 0.5f);
    }

    // south: the row of towers south of town (off where the airport's apron runs instead)
    public static void Build(Transform root, bool south = true)
    {
        var sky = PrimitiveKit.Group("Skyline_ตึกสูง", root, Vector3.zero).transform;
        int n = 0;
        Lights Next() => Schemes[n++ % Schemes.Length];

        // North, either side of the view to the landmark
        Round(sky, new Vector3(-78f, 0f, 70f), 7f, 22, 0, 1, new Lights(TowerLights.Show.Media, White), 11);
        Condo(sky, new Vector3(-59f, 0f, 72f), 0f, 15f, 11f, 14, 0, Next(), 12);
        Stepped(sky, new Vector3(-40f, 0f, 68f), 0f, 15f, 14f, 21, 1, 3, Next(), 13);
        Twist(sky, new Vector3(36f, 0f, 70f), 12f, 24, 1, 0, new Lights(TowerLights.Show.Chase, Cyan), 14);
        GlassFins(sky, new Vector3(55f, 0f, 74f), 0f, 13f, 13f, 27, 0, 0, crown: 1, Next(), 15);
        Condo(sky, new Vector3(74f, 0f, 68f), 0f, 14f, 10f, 12, 2, Next(), 16);
        if (south)
        {
            // South, beyond the temple and the offices
            GlassFins(sky, new Vector3(-71f, 0f, -68f), 0f, 14f, 12f, 16, 2, 2, crown: 2, Next(), 21);
            Condo(sky, new Vector3(-52f, 0f, -66f), 0f, 15f, 11f, 18, 1, Next(), 22);
            Round(sky, new Vector3(-33f, 0f, -70f), 6f, 14, 4, 2, Next(), 23);
            Stepped(sky, new Vector3(-14f, 0f, -67f), 0f, 15f, 14f, 25, 0, 1, Next(), 24);
            Condo(sky, new Vector3(5f, 0f, -72f), 0f, 14f, 10f, 10, 3, Next(), 25);
            GlassFins(sky, new Vector3(24f, 0f, -66f), 0f, 13f, 13f, 20, 3, 1, crown: 0, new Lights(TowerLights.Show.Media, White), 26);
            Twist(sky, new Vector3(43f, 0f, -70f), 11f, 18, 5, 3, Next(), 27);
            Condo(sky, new Vector3(62f, 0f, -67f), 0f, 15f, 11f, 15, 4, Next(), 28);
            GlassFins(sky, new Vector3(81f, 0f, -71f), 0f, 12f, 12f, 22, 1, 3, crown: 1, Next(), 29);
        }
        // West
        Condo(sky, new Vector3(-74f, 0f, -42f), 90f, 15f, 11f, 12, 0, Next(), 31);
        Twin(sky, new Vector3(-74f, 0f, -20f), 9f, 9f, 18, 4, 0, Next(), 32);
        Stepped(sky, new Vector3(-74f, 0f, 2f), 90f, 14f, 13f, 16, 2, 0, Next(), 33);
        Round(sky, new Vector3(-74f, 0f, 24f), 6.5f, 20, 1, 0, Next(), 34);
        Condo(sky, new Vector3(-74f, 0f, 46f), 90f, 14f, 10f, 16, 1, Next(), 35);
        // East
        GlassFins(sky, new Vector3(74f, 0f, -33f), 90f, 13f, 12f, 18, 5, 1, crown: 2, Next(), 41);
        Condo(sky, new Vector3(74f, 0f, -11f), 90f, 15f, 10f, 14, 2, Next(), 42);
        Twin(sky, new Vector3(74f, 0f, 11f), 9f, 9f, 22, 2, 1, new Lights(TowerLights.Show.Rainbow, White), 43);
        Twist(sky, new Vector3(74f, 0f, 33f), 11f, 20, 3, 2, Next(), 44);
        Stepped(sky, new Vector3(76f, 0f, 55f), 90f, 13f, 12f, 14, 4, 2, Next(), 45);

        Landmark(sky, new Vector3(0f, 0f, 84f));
        int k = 0;
        foreach (var p in new[] { new Vector3(52f, 0f, 56f), new Vector3(60f, 0f, 48f), new Vector3(45f, 0f, 47f), new Vector3(59f, 0f, 60f) })
            LightTree(sky, p, 13f + (k % 3) * 4f, 400 + k++);
    }

    // ── Parts shared by every design ─────────────────────────────────────────
    private static Kit Start(Transform parent, string name, Vector3 p, float yaw)
    {
        var k = new Kit { T = PrimitiveKit.Group(name, parent, p, yaw).transform };
        k.Night = PrimitiveKit.Group("NightLights", k.T, Vector3.zero).transform;
        return k;
    }

    // A facade piece of the rooms texture: a plain cube or cylinder (one UV square per face) showing
    // floors first..first+count
    private static void Facade(Kit k, PrimitiveType type, Vector3 at, Vector3 scale, float yaw, int first, int count)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = "RoomLights";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(k.Night, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = PrimitiveKit.Unlit("TowerRooms", Color.white);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        k.Faces.Add(r);
        k.First.Add(first);
        k.Count.Add(count);
    }

    private static void Accent(Kit k, PrimitiveType type, Vector3 at, Vector3 scale, float yaw = 0f)
    {
        var go = PrimitiveKit.Create(type, "LedAccent", k.Night, at, scale, PrimitiveKit.Unlit("TowerLed", Color.white), new Vector3(0f, yaw, 0f), collider: false);
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        k.Accents.Add(r);
    }

    private static void Beacon(Kit k, Vector3 at)
    {
        var go = PrimitiveKit.Create(PrimitiveType.Sphere, "Beacon", k.Night, at, Vector3.one * 0.55f, PrimitiveKit.Unlit("TowerBeacon", new Color(1f, 0.1f, 0.08f)), default, collider: false);
        k.Beacons.Add(go.GetComponent<Renderer>());
    }

    private static void Finish(Kit k, int rooms, int floors, bool stone, Lights lights, int seed)
    {
        k.T.gameObject.AddComponent<TowerLights>().Configure(k.Night.gameObject, k.Faces.ToArray(), k.First.ToArray(), k.Count.ToArray(),
            Mathf.Max(3, rooms), floors, stone, lights.Show == TowerLights.Show.None || lights.Show == TowerLights.Show.Media ? new Renderer[0] : k.Accents.ToArray(),
            lights.Color, k.Beacons.ToArray(), k.Beams.ToArray(), lights.Show, seed);
        if (lights.Show == TowerLights.Show.None || lights.Show == TowerLights.Show.Media) // no accent show: drop the strips
            foreach (var r in k.Accents) Object.DestroyImmediate(r.gameObject);
    }

    // ── Curtain-wall office with fins ────────────────────────────────────────
    private static void GlassFins(Transform parent, Vector3 p, float yaw, float w, float d, int floors, int glass, int trim, int crown, Lights lights, int seed,
        System.Action<Kit, float> extra = null)
    {
        var k = Start(parent, "Tower_Office", p, yaw);
        var t = k.T;
        float h = floors * FH;
        var fin = Trim(trim);
        PrimitiveKit.Box("Podium", t, new Vector3(0f, 3f, 0f), new Vector3(w + 3f, 6f, d + 3f), Stone(seed));
        PrimitiveKit.Box("Lobby", t, new Vector3(0f, 2.2f, -(d + 3f) / 2f - 0.02f), new Vector3(w * 0.6f, 3.2f, 0.06f), Glass(glass + 3), collider: false);
        PrimitiveKit.Box("Glass", t, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), Glass(glass));
        for (float x = -w / 2f + 1.2f; x < w / 2f - 0.5f; x += 2.4f)
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Fin", t, new Vector3(x, (h + 6f) / 2f, s * (d / 2f + 0.17f)), new Vector3(0.14f, h - 6f, 0.36f), fin, collider: false);
        for (float z = -d / 2f + 1.2f; z < d / 2f - 0.5f; z += 2.4f)
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Fin", t, new Vector3(s * (w / 2f + 0.17f), (h + 6f) / 2f, z), new Vector3(0.36f, h - 6f, 0.14f), fin, collider: false);
        Facade(k, PrimitiveType.Cube, new Vector3(0f, (h + 6f) / 2f, 0f), new Vector3(w + 0.08f, h - 6f, d + 0.08f), 0f, 2, floors - 2);

        float topY = h;
        switch (crown)
        {
            case 1: // stepped crown and spire
                PrimitiveKit.Box("Crown", t, new Vector3(0f, h + 1.6f, 0f), new Vector3(w * 0.72f, 3.2f, d * 0.72f), Glass(glass));
                PrimitiveKit.Box("Crown2", t, new Vector3(0f, h + 4.6f, 0f), new Vector3(w * 0.45f, 2.8f, d * 0.45f), fin, collider: false);
                PrimitiveKit.Cylinder("Spire", t, new Vector3(0f, h + 12f, 0f), new Vector3(0.35f, 6f, 0.35f), fin, collider: false);
                Accent(k, PrimitiveType.Cube, new Vector3(0f, h + 3.25f, 0f), new Vector3(w * 0.72f + 0.12f, 0.14f, d * 0.72f + 0.12f));
                Accent(k, PrimitiveType.Cube, new Vector3(0f, h + 6.05f, 0f), new Vector3(w * 0.45f + 0.12f, 0.14f, d * 0.45f + 0.12f));
                Accent(k, PrimitiveType.Cylinder, new Vector3(0f, h + 12f, 0f), new Vector3(0.38f, 6f, 0.38f));
                topY = h + 18f;
                break;
            case 2: // raked roof, glass to a sharp edge
                PrimitiveKit.Box("Parapet", t, new Vector3(0f, h + 0.6f, 0f), new Vector3(w + 0.3f, 1.2f, d + 0.3f), fin);
                PrimitiveKit.Box("Rake", t, new Vector3(0f, h + d * 0.28f + 0.6f, 0f), new Vector3(w + 0.2f, 0.4f, d * 1.16f), Glass(glass), new Vector3(-29f, 0f, 0f), collider: false);
                PrimitiveKit.Box("RakeFill", t, new Vector3(0f, h + d * 0.18f + 0.6f, d * 0.12f), new Vector3(w, d * 0.36f, d * 0.6f), fin, collider: false);
                Accent(k, PrimitiveType.Cube, new Vector3(0f, h + d * 0.28f + 0.85f, 0f), new Vector3(w + 0.3f, 0.12f, d * 1.18f), 0f);
                topY = h + d * 0.56f + 0.6f;
                break;
            default: // flat crown with a lit band and a plant room
                PrimitiveKit.Box("Parapet", t, new Vector3(0f, h + 0.8f, 0f), new Vector3(w + 0.4f, 1.6f, d + 0.4f), fin);
                PrimitiveKit.Box("PlantRoom", t, new Vector3(0f, h + 2.4f, 0f), new Vector3(w * 0.5f, 3f, d * 0.5f), Stone(seed + 1), collider: false);
                Accent(k, PrimitiveType.Cube, new Vector3(0f, h + 1.2f, 0f), new Vector3(w + 0.46f, 0.18f, d + 0.46f));
                topY = h + 3.9f;
                break;
        }
        if (lights.Show == TowerLights.Show.Chase || lights.Show == TowerLights.Show.Rainbow || lights.Show == TowerLights.Show.Breathe)
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f })
                    Accent(k, PrimitiveType.Cube, new Vector3(x * (w / 2f + 0.36f), (h + 6f) / 2f, z * (d / 2f + 0.36f)), new Vector3(0.12f, h - 6f, 0.12f));
        if (floors >= 15) Beacon(k, new Vector3(0f, topY + 0.3f, 0f));
        extra?.Invoke(k, h);
        Finish(k, Mathf.RoundToInt(w / 3.2f), floors, false, lights, seed);
    }

    // ── Stone tower in tiers ─────────────────────────────────────────────────
    private static void Stepped(Transform parent, Vector3 p, float yaw, float w, float d, int floors, int stone, int glass, Lights lights, int seed)
    {
        var k = Start(parent, "Tower_Stepped", p, yaw);
        var t = k.T;
        var wall = Stone(stone);
        var band = Glass(glass + 3);
        var trim = Trim(seed);
        int[] tierEnd = { Mathf.RoundToInt(floors * 0.55f), Mathf.RoundToInt(floors * 0.82f), floors };
        int from = 0;
        float tw = w, td = d;
        for (int i = 0; i < 3; i++)
        {
            int to = tierEnd[i];
            float y0 = from * FH, y1 = to * FH, th = y1 - y0;
            PrimitiveKit.Box("Tier", t, new Vector3(0f, (y0 + y1) / 2f, 0f), new Vector3(tw, th, td), wall);
            for (int f = Mathf.Max(from, 1); f < to; f++)
                PrimitiveKit.Box("Ribbon", t, new Vector3(0f, f * FH + 2f, 0f), new Vector3(tw + 0.05f, 1.9f, td + 0.05f), band, collider: false);
            PrimitiveKit.Box("Cornice", t, new Vector3(0f, y1 + 0.25f, 0f), new Vector3(tw + 0.7f, 0.5f, td + 0.7f), trim, collider: false);
            Facade(k, PrimitiveType.Cube, new Vector3(0f, (y0 + y1) / 2f, 0f), new Vector3(tw + 0.1f, th, td + 0.1f), 0f, from, to - from);
            Accent(k, PrimitiveType.Cube, new Vector3(0f, y1 + 0.55f, 0f), new Vector3(tw + 0.74f, 0.1f, td + 0.74f));
            from = to;
            tw -= 3f; td -= 3f;
        }
        float h = floors * FH;
        PrimitiveKit.Box("Lantern", t, new Vector3(0f, h + 2f, 0f), new Vector3(tw * 0.7f, 3.5f, td * 0.7f), wall, collider: false);
        PrimitiveKit.Cylinder("Spire", t, new Vector3(0f, h + 8.5f, 0f), new Vector3(0.45f, 4.5f, 0.45f), trim, collider: false);
        Accent(k, PrimitiveType.Cube, new Vector3(0f, h + 2.4f, 0f), new Vector3(tw * 0.7f + 0.08f, 1.6f, td * 0.7f + 0.08f));
        if (floors >= 15) Beacon(k, new Vector3(0f, h + 13.2f, 0f));
        Finish(k, Mathf.RoundToInt(w / 3.2f), floors, true, lights, seed);
    }

    // ── Round glass tower ────────────────────────────────────────────────────
    private static void Round(Transform parent, Vector3 p, float r, int floors, int glass, int trim, Lights lights, int seed)
    {
        var k = Start(parent, "Tower_Round", p, 0f);
        var t = k.T;
        float h = floors * FH, dia = r * 2f;
        var ring = Trim(trim);
        PrimitiveKit.Cylinder("Podium", t, new Vector3(0f, 3f, 0f), new Vector3(dia + 5f, 3f, dia + 5f), Stone(seed));
        PrimitiveKit.Cylinder("Glass", t, new Vector3(0f, h / 2f, 0f), new Vector3(dia, h / 2f, dia), Glass(glass));
        for (int f = 2; f <= floors; f += 2)
            PrimitiveKit.Cylinder("Ring", t, new Vector3(0f, f * FH, 0f), new Vector3(dia + 0.5f, 0.12f, dia + 0.5f), ring, collider: false);
        PrimitiveKit.Cylinder("Crown", t, new Vector3(0f, h + 1.5f, 0f), new Vector3(dia - 1.5f, 1.5f, dia - 1.5f), Glass(glass), collider: false);
        PrimitiveKit.Cylinder("Halo", t, new Vector3(0f, h + 3.2f, 0f), new Vector3(dia + 1f, 0.15f, dia + 1f), ring, collider: false);
        Facade(k, PrimitiveType.Cylinder, new Vector3(0f, (h + 6f) / 2f, 0f), new Vector3(dia + 0.08f, (h - 6f) / 2f, dia + 0.08f), 0f, 2, floors - 2);
        Accent(k, PrimitiveType.Cylinder, new Vector3(0f, h + 3.2f, 0f), new Vector3(dia + 1.06f, 0.08f, dia + 1.06f));
        Accent(k, PrimitiveType.Cylinder, new Vector3(0f, h + 0.1f, 0f), new Vector3(dia + 0.56f, 0.1f, dia + 0.56f));
        if (floors >= 15) Beacon(k, new Vector3(0f, h + 3.6f, 0f));
        Finish(k, Mathf.RoundToInt(Mathf.PI * dia / 3.2f), floors, false, lights, seed);
    }

    // ── Condo with balconies ─────────────────────────────────────────────────
    private static void Condo(Transform parent, Vector3 p, float yaw, float w, float d, int floors, int stone, Lights lights, int seed)
    {
        var k = Start(parent, "Tower_Condo", p, yaw);
        var t = k.T;
        float h = floors * FH;
        var wall = Stone(stone + 1);
        var slab = Stone(stone + 3);
        var rail = PrimitiveKit.Mat("BalconyGlass", new Color(0.55f, 0.7f, 0.78f), 0.9f, 0.3f);
        PrimitiveKit.Box("Body", t, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), wall);
        PrimitiveKit.Box("Base", t, new Vector3(0f, 2f, 0f), new Vector3(w + 2f, 4f, d + 2f), Stone(seed));
        for (int f = 2; f < floors; f++)
        {
            PrimitiveKit.Box("Balcony", t, new Vector3(0f, f * FH, 0f), new Vector3(w * 0.88f, 0.18f, d + 2.4f), slab, collider: false);
            PrimitiveKit.Box("Rail", t, new Vector3(0f, f * FH + 0.55f, 0f), new Vector3(w * 0.88f, 0.9f, d + 2.3f), rail, collider: false);
        }
        foreach (float x in new[] { -1f, 1f }) // side fins framing the balconies
            PrimitiveKit.Box("SideFin", t, new Vector3(x * w * 0.46f, (h + 6.8f) / 2f, 0f), new Vector3(0.3f, h - 6.8f, d + 2.6f), wall, collider: false);
        PrimitiveKit.Box("RoofGarden", t, new Vector3(0f, h + 0.2f, 0f), new Vector3(w * 0.85f, 0.4f, d * 0.8f), PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f)), collider: false);
        PrimitiveKit.Box("Pergola", t, new Vector3(-w * 0.2f, h + 2.6f, 0f), new Vector3(w * 0.35f, 0.15f, d * 0.6f), Trim(1), collider: false);
        foreach (float x in new[] { -w * 0.35f, -w * 0.05f })
            foreach (float z in new[] { -d * 0.28f, d * 0.28f })
                PrimitiveKit.Box("PergolaPost", t, new Vector3(x, h + 1.45f, z), new Vector3(0.15f, 2.3f, 0.15f), Trim(1), collider: false);
        PrimitiveKit.Box("Pool", t, new Vector3(w * 0.22f, h + 0.45f, 0f), new Vector3(w * 0.3f, 0.12f, d * 0.5f), PrimitiveKit.Mat("PoolWater", new Color(0.3f, 0.75f, 0.9f), 0.95f, 0f), collider: false);
        Facade(k, PrimitiveType.Cube, new Vector3(0f, (h + 4f) / 2f, 0f), new Vector3(w + 0.06f, h - 4f, d + 0.06f), 0f, 1, floors - 1);
        Accent(k, PrimitiveType.Cube, new Vector3(w * 0.22f, h + 0.52f, 0f), new Vector3(w * 0.3f + 0.1f, 0.06f, d * 0.5f + 0.1f));
        Accent(k, PrimitiveType.Cube, new Vector3(0f, h + 0.45f, 0f), new Vector3(w + 0.06f, 0.1f, d + 0.06f));
        if (floors >= 15) Beacon(k, new Vector3(0f, h + 3f, 0f));
        Finish(k, Mathf.RoundToInt(w / 3.6f), floors, true, lights, seed);
    }

    // ── Twisting tower ───────────────────────────────────────────────────────
    private static void Twist(Transform parent, Vector3 p, float w, int floors, int glass, int trim, Lights lights, int seed)
    {
        var k = Start(parent, "Tower_Twist", p, 0f);
        var t = k.T;
        float d = w * 0.78f, turn = 90f / floors;
        var g = Glass(glass);
        var slab = Trim(trim);
        PrimitiveKit.Box("Podium", t, new Vector3(0f, 3.4f, 0f), new Vector3(w + 4f, 6.8f, w + 4f), Stone(seed));
        for (int f = 2; f < floors; f++)
        {
            float yaw = f * turn;
            PrimitiveKit.Box("Floor", t, new Vector3(0f, f * FH + 1.55f, 0f), new Vector3(w, 3.1f, d), g, new Vector3(0f, yaw, 0f));
            PrimitiveKit.Box("Slab", t, new Vector3(0f, (f + 1) * FH - 0.15f, 0f), new Vector3(w + 0.5f, 0.3f, d + 0.5f), slab, new Vector3(0f, yaw, 0f), collider: false);
            Facade(k, PrimitiveType.Cube, new Vector3(0f, f * FH + 1.55f, 0f), new Vector3(w + 0.06f, 3.1f, d + 0.06f), yaw, f, 1);
            if (f % 3 == 0) Accent(k, PrimitiveType.Cube, new Vector3(0f, (f + 1) * FH - 0.15f, 0f), new Vector3(w + 0.56f, 0.1f, d + 0.56f), yaw);
        }
        float h = floors * FH;
        PrimitiveKit.Box("Crown", t, new Vector3(0f, h + 1.2f, 0f), new Vector3(w * 0.6f, 2.4f, d * 0.6f), slab, new Vector3(0f, floors * turn, 0f), collider: false);
        if (floors >= 15) Beacon(k, new Vector3(0f, h + 2.8f, 0f));
        Finish(k, Mathf.RoundToInt(w / 3.2f), floors, false, lights, seed);
    }

    // ── Twin towers with a sky bridge ────────────────────────────────────────
    private static void Twin(Transform parent, Vector3 p, float w, float d, int floors, int glass, int trim, Lights lights, int seed)
    {
        var group = PrimitiveKit.Group("Tower_Twin", parent, p).transform;
        float gap = 6f, x = (w + gap) / 2f;
        int bridge = Mathf.RoundToInt(floors * 0.62f);
        GlassFins(group, new Vector3(-x, 0f, 0f), 0f, w, d, floors, glass, trim, crown: 1, lights, seed);
        GlassFins(group, new Vector3(x, 0f, 0f), 0f, w, d, floors, glass, trim, crown: 1, lights, seed + 1, (k, h) =>
        {
            // the bridge, built with the east tower (its local space: west tower at -2x)
            float y = bridge * FH;
            PrimitiveKit.Box("SkyBridge", k.T, new Vector3(-x, y + FH, 0f), new Vector3(gap + 0.6f, FH * 2f, d * 0.45f), Glass(glass + 2), collider: false);
            PrimitiveKit.Box("BridgeDeck", k.T, new Vector3(-x, y, 0f), new Vector3(gap + 1f, 0.4f, d * 0.5f), Trim(trim), collider: false);
            Accent(k, PrimitiveType.Cube, new Vector3(-x, y - 0.25f, 0f), new Vector3(gap + 1f, 0.1f, d * 0.52f));
        });
    }

    // ── Landmark: three towers and a sky park ────────────────────────────────
    private static void Landmark(Transform sky, Vector3 at)
    {
        var mark = PrimitiveKit.Group("Landmark_สกายพาร์ค", sky, at).transform;
        var warm = new Lights(TowerLights.Show.Steady, Warm);
        foreach (float x in new[] { -22f, 0f, 22f })
            GlassFins(mark, new Vector3(x, 0f, 0f), 0f, 10f, 15f, 16, 5, 3, crown: 0, warm, 300 + (int)x);
        float h = 16 * FH + 3.9f;
        var deck = Start(mark, "SkyPark", Vector3.zero, 0f);
        var deckMat = PrimitiveKit.Mat("SkyParkDeck", new Color(0.88f, 0.88f, 0.86f), 0.4f, 0.1f);
        PrimitiveKit.Box("SkyDeck", deck.T, new Vector3(4f, h + 0.8f, 0f), new Vector3(70f, 1.6f, 13f), deckMat, collider: false);
        PrimitiveKit.Box("SkyDeckNose", deck.T, new Vector3(41f, h + 0.95f, 0f), new Vector3(4f, 1.3f, 10f), deckMat, new Vector3(0f, 0f, 8f), collider: false);
        PrimitiveKit.Box("Pool", deck.T, new Vector3(-2f, h + 1.65f, -4.6f), new Vector3(50f, 0.12f, 2.6f), PrimitiveKit.Mat("PoolWater", new Color(0.3f, 0.75f, 0.9f), 0.95f, 0f), collider: false);
        for (float x = -24f; x <= 32f; x += 8f)
            Nature.CoconutPalm(deck.T, new Vector3(x, h + 1.6f, 3.5f), 0.7f, (int)x + 50);
        PrimitiveKit.Label("สกายพาร์ค", deck.T, new Vector3(4f, h + 0.8f, -6.55f), 0.25f, new Color(0.95f, 0.8f, 0.45f));
        Accent(deck, PrimitiveType.Cube, new Vector3(-2f, h + 1.72f, -4.6f), new Vector3(50.2f, 0.05f, 2.8f));
        Accent(deck, PrimitiveType.Cube, new Vector3(4f, h - 0.05f, 0f), new Vector3(70.3f, 0.1f, 13.3f));
        for (int i = 0; i < 4; i++)
            deck.Beams.Add(Searchlight(deck.Night, new Vector3(-24f + i * 16f, h + 1.7f, 2f), i));
        Finish(deck, 1, 1, false, new Lights(TowerLights.Show.Breathe, Cyan), 777);
    }

    private static Transform Searchlight(Transform parent, Vector3 at, int index)
    {
        Color[] colors = { new Color(0.1f, 0.12f, 0.18f), new Color(0.14f, 0.09f, 0.16f), new Color(0.08f, 0.14f, 0.13f), new Color(0.15f, 0.12f, 0.08f) };
        var pivot = PrimitiveKit.Group("Searchlight", parent, at).transform;
        var beam = PrimitiveKit.Cylinder("Beam", pivot, new Vector3(0f, 40f, 0f), new Vector3(0.8f, 40f, 0.8f),
            PrimitiveKit.Beam("SkyBeam" + index % colors.Length, colors[index % colors.Length]), collider: false);
        beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return pivot;
    }

    // ── Light-trees in the garden east of town ───────────────────────────────
    private static void LightTree(Transform parent, Vector3 p, float height, int seed)
    {
        var k = Start(parent, "LightTree", p, 0f);
        var t = k.T;
        var trunk = PrimitiveKit.Mat("LightTreeTrunk", new Color(0.32f, 0.22f, 0.35f), 0.4f, 0.2f);
        var leaf = PrimitiveKit.Mat("LightTreeLeaf", new Color(0.25f, 0.45f, 0.3f));
        const int rings = 6;
        for (int i = 0; i < rings; i++)
        {
            float r = Mathf.Lerp(1.6f, 0.8f, i / (float)(rings - 1)) + (i == rings - 1 ? 0.6f : 0f);
            PrimitiveKit.Cylinder("Trunk", t, new Vector3(0f, (i + 0.5f) * height / rings, 0f), new Vector3(r, height / rings / 2f, r), trunk, collider: i == 0);
        }
        PrimitiveKit.Cylinder("Canopy", t, new Vector3(0f, height + 0.3f, 0f), new Vector3(9f, 0.35f, 9f), leaf, collider: false);
        PrimitiveKit.Cylinder("CanopyTop", t, new Vector3(0f, height + 0.9f, 0f), new Vector3(6f, 0.3f, 6f), leaf, collider: false);
        for (int i = 1; i < rings; i++)
        {
            float r = Mathf.Lerp(1.7f, 0.9f, i / (float)(rings - 1));
            Accent(k, PrimitiveType.Cylinder, new Vector3(0f, i * height / rings, 0f), new Vector3(r, 0.06f, r));
        }
        Accent(k, PrimitiveType.Cylinder, new Vector3(0f, height + 0.3f, 0f), new Vector3(9.12f, 0.1f, 9.12f));
        Accent(k, PrimitiveType.Cylinder, new Vector3(0f, height + 0.9f, 0f), new Vector3(6.12f, 0.08f, 6.12f));
        Finish(k, 1, 1, false, new Lights(TowerLights.Show.Chase, Magenta), seed);
    }
}
