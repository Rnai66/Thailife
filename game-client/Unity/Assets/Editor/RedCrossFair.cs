using System.Collections.Generic;
using UnityEngine;

// The city's big fair (งานกาชาดจังหวัด and every other city festival): spreads out west and south of
// the city event ground (FestivalGrounds, big) with the big rides and shows — a gateway of lights on
// the main road, รถไต่ถัง, เรือไวกิ้ง, a รถไฟเหาะ looping round the รถบั๊ม floor, บ้านผีสิง, the เมียงู tent
// and a concert stage — plus searchlights sweeping the sky and พลุ after dark. Everything shows only
// while a big festival is on. Positions are in the city root's space.
public static class RedCrossFair
{
    // West wing x -68..-48.5, z -59..-22; south strip x -48..-22.5, z -62..-48.5
    private static readonly Vector3 CoasterCentre = new Vector3(-58.5f, 0f, -46.8f);
    private const float CoasterRx = 7.6f, CoasterRz = 10f;

    public static void Build(Transform root)
    {
        var t = FairLights.Gated(root, "RedCrossFair_งานกาชาด", "big");
        var ground = PrimitiveKit.Mat("FairGround", new Color(0.42f, 0.36f, 0.33f));
        PrimitiveKit.Box("WestWing", t, new Vector3(-58.25f, 0.011f, -40.5f), new Vector3(19.5f, 0.02f, 37f), ground, collider: false);
        PrimitiveKit.Box("SouthStrip", t, new Vector3(-35.25f, 0.011f, -55.25f), new Vector3(25.5f, 0.02f, 13.5f), ground, collider: false);

        Gateway(root, new Vector3(-58.25f, 0f, -23f));
        WallOfDeath(t, new Vector3(-62f, 0f, -29f));
        Viking(t, new Vector3(-52.5f, 0f, -29.5f));
        Coaster(t);
        BumperCars(t, CoasterCentre);
        HauntedHouse(t, new Vector3(-44.6f, 0f, -55.5f));
        SnakeTent(t, new Vector3(-50.75f, 0f, -55.5f));
        Concert(root, new Vector3(-31.5f, 0f, -57f));
        Searchlights(t);
        Sparkle(t);

        Npcs.Crowd(t, new Vector3(-56f, 0f, -34f), new Vector2(14f, 2.5f), 8, "big", 0, 24,
            "รถไต่ถังเสียงดังมาก!|ขึ้นรถไฟเหาะกัน!|กลัวบ้านผีสิงอ่ะ|เมียงูของจริงไหมเนี่ย|เรือไวกิ้งเสียวสุดๆ|ไปรถบั๊มกัน");
        Npcs.Crowd(t, new Vector3(-32f, 0f, -51f), new Vector2(14f, 3.5f), 12, "big", 0, 24,
            "คอนเสิร์ตเริ่มแล้ว!|เพลงนี้มันส์มาก|กรี๊ดดด!|ร้องตามกันเลย|ปีหน้ามาอีกนะ");
    }

    // Thai vowels like ำ need the game font (Mitr); the built-in one drops them
    private static TextMesh Thai(TextMesh label)
    {
        var font = Resources.Load<Font>("Fonts/Mitr-SemiBold");
        if (font == null) return label;
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        return label;
    }

    private static FestivalBooth Zone(Transform parent, Vector3 local, Vector3 size, string game, string title, Transform seat = null, float seconds = 18f)
    {
        var go = new GameObject("PlayZone");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var booth = go.AddComponent<FestivalBooth>();
        booth.Configure(game, title, 0, seat, seconds);
        return booth;
    }

    private static void Bulbs(Transform parent, Vector3 a, Vector3 b, int count, float size = 0.14f)
    {
        Color[] c = { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.35f, 1f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.85f) };
        for (int i = 0; i < count; i++)
        {
            var s = PrimitiveKit.Sphere("Bulb", parent, Vector3.Lerp(a, b, count == 1 ? 0.5f : i / (float)(count - 1)), Vector3.one * size,
                PrimitiveKit.Glow($"GateBulb{i % c.Length}", c[i % c.Length], 6f, 0.4f));
            s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // ซุ้มประตูงาน on the main road: two LED towers and a lit banner board showing the festival's name
    private static void Gateway(Transform root, Vector3 at)
    {
        var g = PrimitiveKit.Group("Gateway_ซุ้มประตูงานกาชาด", root, at).transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        foreach (float x in new[] { -8.5f, 8.5f })
            PrimitiveKit.Box("Tower", g, new Vector3(x, 4.5f, 0f), new Vector3(1.2f, 9f, 1.2f), steel);
        PrimitiveKit.Box("Beam", g, new Vector3(0f, 8.6f, 0f), new Vector3(18f, 0.6f, 0.6f), steel, collider: false);
        PrimitiveKit.Box("Board", g, new Vector3(0f, 7.3f, 0.05f), new Vector3(13f, 2f, 0.2f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), collider: false);
        var banner = Thai(PrimitiveKit.Label("งานกาชาด", g, new Vector3(0f, 7.3f, 0.17f), 0.16f, new Color(1f, 0.9f, 0.45f), 180f));
        // Red Cross badges either side of the name
        foreach (float x in new[] { -5.6f, 5.6f })
        {
            PrimitiveKit.Box("BadgeWhite", g, new Vector3(x, 7.3f, 0.17f), new Vector3(1.4f, 1.4f, 0.02f), PrimitiveKit.Mat("TrimWhite", new Color(0.97f, 0.96f, 0.92f)), collider: false);
            PrimitiveKit.Box("CrossV", g, new Vector3(x, 7.3f, 0.19f), new Vector3(0.36f, 1.1f, 0.02f), PrimitiveKit.Glow("RedCross", new Color(1f, 0.1f, 0.12f), 2f), collider: false);
            PrimitiveKit.Box("CrossH", g, new Vector3(x, 7.3f, 0.19f), new Vector3(1.1f, 0.36f, 0.02f), PrimitiveKit.Glow("RedCross", new Color(1f, 0.1f, 0.12f), 2f), collider: false);
        }
        var show = PrimitiveKit.Group("Attractions", g, Vector3.zero).transform;
        g.gameObject.AddComponent<FestivalGate>().Configure("big", banner);
        // LED panels up the towers and chasing bulbs round the board (festival nights only)
        Color[] led = { new Color(1f, 0.25f, 0.55f), new Color(0.25f, 0.7f, 1f), new Color(1f, 0.85f, 0.25f) };
        foreach (float x in new[] { -8.5f, 8.5f })
            for (int i = 0; i < 3; i++)
                PrimitiveKit.Box("LedPanel", show, new Vector3(x, 2f + i * 2.4f, 0.62f), new Vector3(1f, 2f, 0.04f), PrimitiveKit.Glow($"GateLed{i}", led[i], 4f, 0.4f), collider: false);
        Bulbs(show, new Vector3(-6.5f, 8.4f, 0.2f), new Vector3(6.5f, 8.4f, 0.2f), 22);
        Bulbs(show, new Vector3(-6.5f, 6.2f, 0.2f), new Vector3(6.5f, 6.2f, 0.2f), 22);
        FairLights.Bunting(show, new Vector3(-8f, 6f, 0.3f), new Vector3(8f, 6f, 0.3f), 0.6f);
    }

    // รถไต่ถัง: a tall wooden drum with a viewing gallery round the rim; inside, motorbikes and an old car
    // race round the wall. Climb up to watch.
    private static void WallOfDeath(Transform parent, Vector3 at)
    {
        var w = PrimitiveKit.Group("WallOfDeath_รถไต่ถัง", parent, at).transform;
        const float R = 4.5f, H = 4.2f;
        var plank = PrimitiveKit.Mat("DrumPlank", new Color(0.62f, 0.45f, 0.28f));
        MeshKit.Place("DrumOut", w, MeshKit.Lathe("WallDrumOut", new[] { new Vector2(R, 0f), new Vector2(R, H) }, 40, false, false), plank, Vector3.zero);
        MeshKit.Place("DrumIn", w, MeshKit.Lathe("WallDrumIn", new[] { new Vector2(R - 0.12f, H), new Vector2(R - 0.12f, 0f) }, 40, false, false), plank, Vector3.zero);
        var col = w.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = MeshKit.Lathe("WallDrumOut", new[] { new Vector2(R, 0f), new Vector2(R, H) }, 40, false, false);
        // Painted stripes and the name round the outside
        for (int i = 0; i < 3; i++)
            MeshKit.Place("Stripe", w, MeshKit.Lathe("WallDrumStripe", new[] { new Vector2(R + 0.02f, 0f), new Vector2(R + 0.02f, 0.25f) }, 40, false, false),
                PrimitiveKit.Mat(i == 1 ? "TrimWhite" : "TentRed", i == 1 ? new Color(0.97f, 0.96f, 0.92f) : new Color(0.85f, 0.2f, 0.18f)), new Vector3(0f, 0.8f + i * 0.9f, 0f));
        PrimitiveKit.Box("NameBoard", w, new Vector3(0f, 5.4f, R + 0.3f), new Vector3(5f, 1f, 0.12f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("รถไต่ถัง", w, new Vector3(0f, 5.4f, R + 0.37f), 0.09f, new Color(0.7f, 0.08f, 0.08f), 180f));
        Bulbs(w, new Vector3(-2.4f, 6f, R + 0.3f), new Vector3(2.4f, 6f, R + 0.3f), 11);
        // Gallery ring and rail on top, stairs up the front
        // (each surface twice, once per facing, so it shows from above and below / inside and out)
        var deck = PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f));
        MeshKit.Place("Gallery", w, MeshKit.Lathe("WallGallery", new[] { new Vector2(R + 1f, 0f), new Vector2(R - 0.1f, 0f) }, 40, false, false), deck, new Vector3(0f, H, 0f));
        MeshKit.Place("GalleryUnder", w, MeshKit.Lathe("WallGalleryUnder", new[] { new Vector2(R - 0.1f, 0f), new Vector2(R + 1f, 0f) }, 40, false, false), deck, new Vector3(0f, H, 0f));
        var rail = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        MeshKit.Place("Rail", w, MeshKit.Lathe("WallRail", new[] { new Vector2(R + 1f, 0f), new Vector2(R + 1f, 1f) }, 40, false, false), rail, new Vector3(0f, H, 0f));
        MeshKit.Place("RailIn", w, MeshKit.Lathe("WallRailIn", new[] { new Vector2(R + 1f, 1f), new Vector2(R + 1f, 0f) }, 40, false, false), rail, new Vector3(0f, H, 0f));
        for (int i = 0; i < 8; i++)
            PrimitiveKit.Box("Stair", w, new Vector3(2.2f, 0.25f + i * 0.5f, R + 1.6f - i * 0.05f), new Vector3(1.2f, 0.12f, 0.4f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)),
                new Vector3(0f, 0f, 0f), collider: false);
        // Riders: two bikes and a car on spinning frames, leaning into the wall
        var black = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        (float speed, float height, float radius, bool car, Color color)[] riders =
        {
            (220f, 2.6f, R - 0.6f, false, new Color(0.9f, 0.15f, 0.15f)),
            (-190f, 1.7f, R - 0.6f, false, new Color(0.2f, 0.4f, 0.95f)),
            (140f, 1.0f, R - 0.9f, true, new Color(0.95f, 0.8f, 0.2f)),
        };
        int n = 0;
        foreach (var r in riders)
        {
            var spin = PrimitiveKit.Group("RiderSpin", w, Vector3.zero, n * 120f).transform;
            spin.gameObject.AddComponent<Spinner>().Configure(Vector3.up, r.speed);
            var v = PrimitiveKit.Group(r.car ? "Car" : "Bike", spin, new Vector3(0f, r.height, r.radius)).transform;
            v.localRotation = Quaternion.Euler(0f, r.speed > 0 ? 90f : -90f, r.speed > 0 ? -72f : 72f); // nose along the wall, wheels on it
            var paint = PrimitiveKit.Mat($"Rider{n}", r.color, 0.6f, 0.3f);
            if (r.car)
            {
                PrimitiveKit.Box("Body", v, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.6f, 2.6f), paint, collider: false);
                PrimitiveKit.Box("Cabin", v, new Vector3(0f, 0.95f, -0.2f), new Vector3(1.2f, 0.5f, 1.3f), paint, collider: false);
                foreach (float x in new[] { -0.7f, 0.7f })
                    foreach (float z in new[] { -0.85f, 0.85f })
                        PrimitiveKit.Cylinder("Wheel", v, new Vector3(x, 0.2f, z), new Vector3(0.4f, 0.08f, 0.4f), black, new Vector3(0f, 0f, 90f), collider: false);
            }
            else
            {
                PrimitiveKit.Box("Frame", v, new Vector3(0f, 0.45f, 0f), new Vector3(0.25f, 0.35f, 1.2f), paint, collider: false);
                foreach (float z in new[] { -0.55f, 0.55f })
                    PrimitiveKit.Cylinder("Wheel", v, new Vector3(0f, 0.3f, z), new Vector3(0.6f, 0.06f, 0.6f), black, new Vector3(0f, 0f, 90f), collider: false);
                PrimitiveKit.Box("Rider", v, new Vector3(0f, 0.95f, -0.1f), new Vector3(0.4f, 0.7f, 0.35f), PrimitiveKit.Mat("RiderLeather", new Color(0.15f, 0.12f, 0.1f)), collider: false);
                PrimitiveKit.Sphere("Helmet", v, new Vector3(0f, 1.4f, 0f), Vector3.one * 0.32f, paint);
            }
            n++;
        }
        var lamp = new GameObject("DrumLight");
        lamp.transform.SetParent(w, false);
        lamp.transform.localPosition = new Vector3(0f, H + 2f, 0f);
        var l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.6f);
        l.range = 8f;
        l.intensity = 8f;
        l.shadows = LightShadows.None;
        // Watch from the rim
        var seat = PrimitiveKit.Group("Seat", w, new Vector3(0f, H, R + 0.5f), 180f).transform;
        Zone(w, new Vector3(2.2f, 1f, R + 2.4f), new Vector3(2.4f, 2f, 1.6f), "wall", "รถไต่ถัง", seat, 20f);
    }

    // เรือไวกิ้ง: a dragon-prow boat swinging high on an A-frame (swings along Z)
    private static void Viking(Transform parent, Vector3 at)
    {
        var v = PrimitiveKit.Group("Viking_เรือไวกิ้ง", parent, at).transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        const float Hub = 6.2f;
        foreach (float x in new[] { -1.8f, 1.8f })
            foreach (float z in new[] { -1f, 1f })
                PrimitiveKit.Box("Leg", v, new Vector3(x, Hub * 0.5f, z * 1.6f), new Vector3(0.25f, Hub + 0.4f, 0.25f), steel, new Vector3(-z * 28f, 0f, 0f));
        PrimitiveKit.Box("Axle", v, new Vector3(0f, Hub, 0f), new Vector3(4f, 0.3f, 0.3f), steel, collider: false);
        PrimitiveKit.Box("Base", v, new Vector3(0f, 0.15f, 0f), new Vector3(4.6f, 0.3f, 8.6f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        var swing = PrimitiveKit.Group("Swing", v, new Vector3(0f, Hub, 0f)).transform;
        swing.gameObject.AddComponent<Swinger>().Configure(55f, 4.6f);
        foreach (float x in new[] { -1.3f, 1.3f })
            PrimitiveKit.Box("Arm", swing, new Vector3(x, -2.1f, 0f), new Vector3(0.18f, 4.2f, 0.18f), steel, collider: false);
        var hull = PrimitiveKit.Mat("VikingHull", new Color(0.55f, 0.28f, 0.12f), 0.4f);
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));
        PrimitiveKit.Box("Hull", swing, new Vector3(0f, -4.4f, 0f), new Vector3(2.6f, 0.9f, 6f), hull, collider: false);
        foreach (float z in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Bow", swing, new Vector3(0f, -3.9f, z * 3.3f), new Vector3(1.6f, 1.6f, 0.8f), hull, new Vector3(z * -25f, 0f, 0f), collider: false);
            PrimitiveKit.Sphere("DragonHead", swing, new Vector3(0f, -2.9f, z * 3.8f), new Vector3(0.6f, 0.8f, 0.9f), PrimitiveKit.Mat("DragonGreen", new Color(0.2f, 0.6f, 0.3f), 0.5f));
        }
        PrimitiveKit.Box("GoldTrim", swing, new Vector3(0f, -3.92f, 0f), new Vector3(2.65f, 0.1f, 6.05f), gold, collider: false);
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("Bench", swing, new Vector3(0f, -4f, -2.2f + i * 1.1f), new Vector3(2.3f, 0.12f, 0.35f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
        Bulbs(swing, new Vector3(-1.32f, -3.95f, -2.9f), new Vector3(-1.32f, -3.95f, 2.9f), 12, 0.12f);
        Bulbs(swing, new Vector3(1.32f, -3.95f, -2.9f), new Vector3(1.32f, -3.95f, 2.9f), 12, 0.12f);
        var seat = PrimitiveKit.Group("Seat", swing, new Vector3(0f, -4.5f, 2.2f), 90f).transform;
        PrimitiveKit.Box("SignBoard", v, new Vector3(2.6f, 1.5f, 0f), new Vector3(0.1f, 0.8f, 3f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("เรือไวกิ้ง", v, new Vector3(2.67f, 1.5f, 0f), 0.06f, new Color(0.7f, 0.08f, 0.08f), -90f));
        Zone(v, new Vector3(3.4f, 1f, 0f), new Vector3(1.6f, 2f, 3f), "viking", "เรือไวกิ้ง", seat, 22f);
    }

    // Height of the coaster track round the loop (fraction 0..1 from the station): the chain lift, the
    // big drop, then smaller hills back to the station
    private static readonly Vector2[] Profile =
    {
        new Vector2(0f, 1.3f), new Vector2(0.05f, 1.3f), new Vector2(0.3f, 12f), new Vector2(0.36f, 11.6f), new Vector2(0.45f, 2.2f),
        new Vector2(0.55f, 7.4f), new Vector2(0.63f, 2.6f), new Vector2(0.72f, 6f), new Vector2(0.8f, 2.4f), new Vector2(0.88f, 4f),
        new Vector2(0.95f, 1.5f),
    };

    private static float TrackHeight(float u)
    {
        // Catmull-Rom through the profile, wrapping round
        int n = Profile.Length;
        int i = 0;
        while (i < n - 1 && Profile[i + 1].x <= u) i++;
        Vector2 Get(int k) { int m = ((k % n) + n) % n; float shift = k >= n ? 1f : k < 0 ? -1f : 0f; return new Vector2(Profile[m].x + shift, Profile[m].y); }
        var p0 = Get(i - 1); var p1 = Get(i); var p2 = Get(i + 1); var p3 = Get(i + 2);
        float s = (u - p1.x) / Mathf.Max(0.0001f, p2.x - p1.x);
        float s2 = s * s, s3 = s2 * s;
        return Mathf.Max(0.8f, 0.5f * (2f * p1.y + (-p0.y + p2.y) * s + (2f * p0.y - 5f * p1.y + 4f * p2.y - p3.y) * s2 + (-p0.y + 3f * p1.y - 3f * p2.y + p3.y) * s3));
    }

    // รถไฟเหาะ: an oval track on steel posts round the bumper car floor, rails lit with bulbs; the train
    // runs all the time (CoasterTrain), the station platform is on the east side
    private static void Coaster(Transform parent)
    {
        var c = PrimitiveKit.Group("Coaster_รถไฟเหาะ", parent, CoasterCentre).transform;
        const int N = 180;
        var path = new Vector3[N];
        for (int i = 0; i < N; i++)
        {
            float u = i / (float)N;
            float a = u * Mathf.PI * 2f;
            path[i] = new Vector3(Mathf.Cos(a) * CoasterRx, TrackHeight(u), Mathf.Sin(a) * CoasterRz);
        }
        var closed = new Vector3[N + 1];
        for (int i = 0; i <= N; i++) closed[i] = path[i % N];
        var steel = PrimitiveKit.Mat("CoasterSteel", new Color(0.85f, 0.2f, 0.2f), 0.6f, 0.5f);
        var post = PrimitiveKit.Mat("CoasterPost", new Color(0.92f, 0.92f, 0.95f), 0.5f, 0.4f);
        // Two rails either side of the centre line, a spine under them
        Vector3[] Offset(float side, float down)
        {
            var o = new Vector3[N + 1];
            for (int i = 0; i <= N; i++)
            {
                var f = (closed[(i + 1) % N] - closed[i % N]);
                f.y = 0f;
                var right = Vector3.Cross(Vector3.up, f.normalized);
                o[i] = closed[i] + right * side + Vector3.down * down;
            }
            return o;
        }
        MeshKit.Place("RailL", c, MeshKit.Sweep("CoasterRailL", Offset(-0.5f, 0f), MeshKit.Taper(N + 1, 0.07f, 0.07f), 6), steel, Vector3.zero);
        MeshKit.Place("RailR", c, MeshKit.Sweep("CoasterRailR", Offset(0.5f, 0f), MeshKit.Taper(N + 1, 0.07f, 0.07f), 6), steel, Vector3.zero);
        MeshKit.Place("Spine", c, MeshKit.Sweep("CoasterSpine", Offset(0f, 0.3f), MeshKit.Taper(N + 1, 0.14f, 0.14f), 6), steel, Vector3.zero);
        var tie = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        for (int i = 0; i < N; i += 3)
        {
            var f = closed[i + 1] - closed[i];
            PrimitiveKit.Box("Tie", c, closed[i] + Vector3.down * 0.1f, new Vector3(1.1f, 0.06f, 0.12f), tie,
                Quaternion.LookRotation(f, Vector3.up).eulerAngles, collider: false);
        }
        for (int i = 0; i < N; i += 9) // support posts, braced to the ground
        {
            float h = path[i].y - 0.3f;
            if (h < 0.6f) continue;
            PrimitiveKit.Cylinder("Post", c, new Vector3(path[i].x, h * 0.5f, path[i].z), new Vector3(0.22f, h * 0.5f, 0.22f), post);
        }
        // Chase lights along the outer rail
        var outer = Offset(0.62f, -0.05f);
        Color[] bulb = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.6f), new Color(0.35f, 0.8f, 1f) };
        for (int i = 0; i < N; i += 5)
        {
            var s = PrimitiveKit.Sphere("RailBulb", c, outer[i], Vector3.one * 0.14f, PrimitiveKit.Glow($"CoasterBulb{(i / 5) % 3}", bulb[(i / 5) % 3], 10f, 0.4f));
            s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // The train: four cars, the first with a face on the nose and the player's seat
        var cars = new Transform[4];
        Color[] paint = { new Color(0.95f, 0.8f, 0.15f), new Color(0.2f, 0.5f, 0.95f), new Color(0.95f, 0.3f, 0.5f), new Color(0.3f, 0.8f, 0.4f) };
        Transform seat = null;
        for (int k = 0; k < cars.Length; k++)
        {
            var car = PrimitiveKit.Group("CoasterCar", c, path[0]).transform;
            var mat = PrimitiveKit.Mat($"CoasterCar{k}", paint[k], 0.7f, 0.3f);
            PrimitiveKit.Box("Body", car, new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.5f, 1.6f), mat, collider: false);
            PrimitiveKit.Box("SeatBack", car, new Vector3(0f, 0.6f, -0.55f), new Vector3(1.1f, 0.5f, 0.12f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
            PrimitiveKit.Box("Bar", car, new Vector3(0f, 0.65f, 0.35f), new Vector3(1f, 0.06f, 0.06f), tie, collider: false);
            if (k == 0)
            {
                PrimitiveKit.Box("Nose", car, new Vector3(0f, 0.3f, 0.95f), new Vector3(1.1f, 0.45f, 0.3f), mat, new Vector3(20f, 0f, 0f), collider: false);
                foreach (float x in new[] { -0.3f, 0.3f })
                    PrimitiveKit.Sphere("Lamp", car, new Vector3(x, 0.38f, 1.12f), Vector3.one * 0.16f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
                seat = PrimitiveKit.Group("Seat", car, new Vector3(0f, 0.15f, -0.1f)).transform;
            }
            cars[k] = car;
        }
        // Chain lift: from 5% to 30% of the way round by angle, as fractions of the track length
        float total = 0f, liftFrom = 0f, liftTo = 0f;
        for (int i = 0; i < N; i++)
        {
            if (i == Mathf.RoundToInt(N * 0.05f)) liftFrom = total;
            if (i == Mathf.RoundToInt(N * 0.3f)) liftTo = total;
            total += Vector3.Distance(closed[i], closed[i + 1]);
        }
        c.gameObject.AddComponent<CoasterTrain>().Configure(path, cars, liftFrom / total, liftTo / total);

        // Station platform on the east side (outside the oval) with a roof and the queue gate
        var st = PrimitiveKit.Group("Station", c, new Vector3(CoasterRx + 1.6f, 0f, 0f)).transform;
        PrimitiveKit.Box("Platform", st, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 1f, 6f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        foreach (float z in new[] { -2.8f, 2.8f })
            PrimitiveKit.Cylinder("RoofPost", st, new Vector3(0.8f, 1.9f, z), new Vector3(0.1f, 1.4f, 0.1f), post, collider: false);
        PrimitiveKit.Box("Roof", st, new Vector3(0f, 3.35f, 0f), new Vector3(2.4f, 0.15f, 6.4f), steel, collider: false);
        PrimitiveKit.Box("SignBoard", st, new Vector3(1.25f, 2.8f, 0f), new Vector3(0.1f, 0.7f, 3.4f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("รถไฟเหาะ", st, new Vector3(1.32f, 2.8f, 0f), 0.06f, new Color(0.7f, 0.08f, 0.08f), -90f));
        Zone(st, new Vector3(1.9f, 1f, -2.6f), new Vector3(1.6f, 2f, 2.4f), "coaster", "รถไฟเหาะ", seat, 26f);
    }

    // รถบั๊ม: a steel floor with a rubber rail and a sparking net roof, cars bumping about (BumperCars);
    // inside the coaster's oval, so the ticket gate is beside the coaster station
    private static void BumperCars(Transform parent, Vector3 at)
    {
        var b = PrimitiveKit.Group("BumperCars_รถบั๊ม", parent, at).transform;
        const float HX = 5f, HZ = 4f;
        PrimitiveKit.Box("Floor", b, new Vector3(0f, 0.08f, 0f), new Vector3(HX * 2f, 0.16f, HZ * 2f), PrimitiveKit.Mat("BumperFloor", new Color(0.55f, 0.58f, 0.62f), 0.8f, 0.6f), collider: false);
        var rubber = PrimitiveKit.Mat("BumperRail", new Color(0.95f, 0.75f, 0.15f));
        PrimitiveKit.Box("RailN", b, new Vector3(0f, 0.35f, HZ), new Vector3(HX * 2f, 0.4f, 0.25f), rubber);
        PrimitiveKit.Box("RailS", b, new Vector3(0f, 0.35f, -HZ), new Vector3(HX * 2f, 0.4f, 0.25f), rubber);
        PrimitiveKit.Box("RailE", b, new Vector3(HX, 0.35f, 0f), new Vector3(0.25f, 0.4f, HZ * 2f), rubber);
        PrimitiveKit.Box("RailW", b, new Vector3(-HX, 0.35f, 0f), new Vector3(0.25f, 0.4f, HZ * 2f), rubber);
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        foreach (float x in new[] { -HX, HX })
            foreach (float z in new[] { -HZ, HZ })
                PrimitiveKit.Cylinder("RoofPost", b, new Vector3(x, 1.6f, z), new Vector3(0.15f, 1.6f, 0.15f), steel, collider: false);
        PrimitiveKit.Box("RoofNet", b, new Vector3(0f, 3.2f, 0f), new Vector3(HX * 2f, 0.04f, HZ * 2f), PrimitiveKit.Glow("BumperNet", new Color(0.35f, 0.5f, 1f), 1.2f, 0.3f), collider: false);
        Bulbs(b, new Vector3(-HX, 3.3f, -HZ), new Vector3(HX, 3.3f, -HZ), 14);
        Bulbs(b, new Vector3(-HX, 3.3f, HZ), new Vector3(HX, 3.3f, HZ), 14);
        Color[] paint = { new Color(0.95f, 0.2f, 0.25f), new Color(0.2f, 0.5f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.8f, 0.4f), new Color(0.85f, 0.4f, 0.9f), new Color(1f, 0.55f, 0.2f) };
        var cars = new Transform[6];
        Transform seat = null;
        for (int i = 0; i < cars.Length; i++)
        {
            var car = PrimitiveKit.Group("BumperCar", b, new Vector3(-3.5f + (i % 3) * 3.5f, 0.16f, i < 3 ? -1.8f : 1.8f)).transform;
            var mat = PrimitiveKit.Mat($"BumperCar{i}", paint[i], 0.8f, 0.3f);
            PrimitiveKit.Cylinder("Skirt", car, new Vector3(0f, 0.15f, 0f), new Vector3(1.5f, 0.12f, 1.5f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
            PrimitiveKit.Box("Body", car, new Vector3(0f, 0.38f, 0f), new Vector3(1.1f, 0.4f, 1.3f), mat, collider: false);
            PrimitiveKit.Cylinder("Pole", car, new Vector3(0f, 1.7f, -0.5f), new Vector3(0.05f, 1.4f, 0.05f), steel, collider: false);
            PrimitiveKit.Sphere("Spark", car, new Vector3(0f, 3.1f, -0.5f), Vector3.one * 0.18f, PrimitiveKit.Glow("BumperSpark", new Color(0.7f, 0.85f, 1f), 12f, 0.5f));
            if (i == 0) seat = PrimitiveKit.Group("Seat", car, new Vector3(0f, 0.1f, 0f)).transform;
            cars[i] = car;
        }
        b.gameObject.AddComponent<global::BumperCars>().Configure(cars, new Vector2(HX, HZ));
        // Ticket gate by the coaster station (outside the oval)
        var gate = PrimitiveKit.Group("BumperGate", parent, at + new Vector3(CoasterRx + 1.8f, 0f, 5.2f), -90f).transform;
        PrimitiveKit.Box("SignBoard", gate, new Vector3(0f, 2.2f, 0.4f), new Vector3(2.4f, 0.7f, 0.1f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("รถบั๊ม →", gate, new Vector3(0f, 2.2f, 0.33f), 0.06f, new Color(0.7f, 0.08f, 0.08f)));
        foreach (float x in new[] { -1.1f, 1.1f })
            PrimitiveKit.Cylinder("Post", gate, new Vector3(x, 1.25f, 0.4f), new Vector3(0.08f, 1.25f, 0.08f), steel);
        Zone(gate, new Vector3(0f, 1f, -0.6f), new Vector3(2.4f, 2f, 1.6f), "bumper", "รถบั๊ม", seat, 22f);
    }

    // บ้านผีสิง: a crooked dark house with a giant skull doorway, glowing green windows, a ghost on
    // the roof and a flickering lamp. Front faces +Z (north); walk up to the skull's mouth.
    private static void HauntedHouse(Transform parent, Vector3 at)
    {
        var h = PrimitiveKit.Group("HauntedHouse_บ้านผีสิง", parent, at, 180f).transform;
        var wall = PrimitiveKit.Mat("HauntWall", new Color(0.2f, 0.16f, 0.2f));
        var trim = PrimitiveKit.Mat("HauntTrim", new Color(0.35f, 0.08f, 0.1f));
        PrimitiveKit.Box("House", h, new Vector3(0f, 2.5f, 0.5f), new Vector3(6f, 5f, 7f), wall);
        MeshKit.Place("Roof", h, MeshKit.Gable("HauntRoof", 6.6f, 2.2f, 7.6f), trim, new Vector3(0f, 5f, -3.3f));
        PrimitiveKit.Box("Tower", h, new Vector3(-2f, 6.5f, 1.5f), new Vector3(1.6f, 3f, 1.6f), wall, new Vector3(0f, 0f, 4f), collider: false);
        PrimitiveKit.Cylinder("Spire", h, new Vector3(-2.1f, 8.6f, 1.5f), new Vector3(1.2f, 0.8f, 1.2f), trim, collider: false);
        var green = PrimitiveKit.Glow("HauntWindow", new Color(0.35f, 1f, 0.4f), 3f, 0.4f);
        foreach (var p in new[] { new Vector3(-1.8f, 3.6f, -3.02f), new Vector3(1.8f, 3.6f, -3.02f), new Vector3(-2f, 6.6f, 0.68f) })
            PrimitiveKit.Box("Window", h, p, new Vector3(0.9f, 1.1f, 0.02f), green, new Vector3(0f, 0f, p.x * 3f), collider: false);
        // Skull doorway: a big white skull face with black eyes and the door as its mouth
        var bone = PrimitiveKit.Mat("Bone", new Color(0.92f, 0.9f, 0.82f));
        PrimitiveKit.Sphere("Skull", h, new Vector3(0f, 2.3f, -3.1f), new Vector3(3f, 3f, 0.6f), bone);
        foreach (float x in new[] { -0.65f, 0.65f })
            PrimitiveKit.Sphere("EyeHole", h, new Vector3(x, 2.8f, -3.38f), new Vector3(0.75f, 0.85f, 0.1f), PrimitiveKit.Glow("GhostEye", new Color(1f, 0.1f, 0.05f), 4f, 0.3f));
        PrimitiveKit.Box("Mouth", h, new Vector3(0f, 1f, -3.2f), new Vector3(1.4f, 2f, 0.3f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("Tooth", h, new Vector3(-0.5f + i * 0.33f, 1.9f, -3.36f), new Vector3(0.22f, 0.3f, 0.06f), bone, collider: false);
        PrimitiveKit.Box("SignBoard", h, new Vector3(0f, 4.6f, -3.1f), new Vector3(3.6f, 0.7f, 0.1f), PrimitiveKit.Mat("BloodRed", new Color(0.55f, 0.03f, 0.05f)), collider: false);
        Thai(PrimitiveKit.Label("บ้านผีสิง", h, new Vector3(0f, 4.6f, -3.17f), 0.07f, new Color(0.9f, 1f, 0.8f)));
        // A white ghost (แม่นาค) floating over the roof
        var ghost = PrimitiveKit.Group("Ghost", h, new Vector3(1.6f, 7.4f, 0f)).transform;
        ghost.gameObject.AddComponent<Bob>();
        MeshKit.Place("Sheet", ghost, MeshKit.Blob("Ghost", 91, 0.2f, 1), PrimitiveKit.Glow("GhostWhite", new Color(0.9f, 0.95f, 1f), 1.5f, 0.6f), Vector3.zero, default, new Vector3(0.9f, 1.4f, 0.7f));
        PrimitiveKit.Sphere("Hair", ghost, new Vector3(0f, 0.55f, 0.05f), new Vector3(0.75f, 0.6f, 0.7f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)));
        var lamp = new GameObject("GreenLight");
        lamp.transform.SetParent(h, false);
        lamp.transform.localPosition = new Vector3(0f, 3f, -5f);
        var l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.4f, 1f, 0.45f);
        l.range = 7f;
        l.intensity = 6f;
        l.shadows = LightShadows.None;
        Zone(h, new Vector3(0f, 1f, -4.2f), new Vector3(2.6f, 2f, 1.6f), "haunted", "บ้านผีสิง");
    }

    // เมียงู: a canvas tent with painted banners of the snake woman out front; inside, a glass box with
    // the performer and her snakes, benches for the audience. Front faces +Z (north).
    private static void SnakeTent(Transform parent, Vector3 at)
    {
        var s = PrimitiveKit.Group("SnakeShow_เมียงู", parent, at, 180f).transform;
        var canvas = PrimitiveKit.Mat("TentGreen", new Color(0.25f, 0.62f, 0.35f));
        var stripe = PrimitiveKit.Mat("TentYellow", new Color(0.95f, 0.75f, 0.2f));
        PrimitiveKit.Box("Back", s, new Vector3(0f, 1.4f, 2.2f), new Vector3(4f, 2.8f, 0.1f), canvas);
        foreach (float x in new[] { -2f, 2f })
            PrimitiveKit.Box("Side", s, new Vector3(x, 1.4f, 0f), new Vector3(0.1f, 2.8f, 4.4f), canvas);
        for (int i = 0; i < 4; i++)
            MeshKit.Place("Canopy", s, MeshKit.Gable("SnakeRoof", 1.05f, 1.2f, 4.6f), i % 2 == 0 ? canvas : stripe, new Vector3(-1.5f + i * 1f, 2.8f, -2.3f));
        // Painted banners either side of the door: the snake woman
        foreach (float x in new[] { -1.4f, 1.4f })
        {
            PrimitiveKit.Box("Banner", s, new Vector3(x, 1.7f, -2.25f), new Vector3(1.1f, 2f, 0.05f), PrimitiveKit.Mat("BannerCream", new Color(0.95f, 0.88f, 0.7f)), collider: false);
            PrimitiveKit.Sphere("PaintedFace", s, new Vector3(x, 2.25f, -2.29f), new Vector3(0.4f, 0.5f, 0.02f), PrimitiveKit.Mat("SkinTone", new Color(0.9f, 0.7f, 0.55f)));
            var body = new[] { new Vector3(x, 1.95f, -2.29f), new Vector3(x + 0.4f, 1.45f, -2.29f), new Vector3(x - 0.35f, 0.95f, -2.29f) };
            MeshKit.Place("PaintedSnake", s, MeshKit.Sweep("SnakeBody" + (x < 0 ? "L" : "R"), MeshKit.Curve(body[0], body[1], body[2], 10), MeshKit.Taper(10, 0.12f, 0.04f), 6),
                PrimitiveKit.Mat("SnakeGreen", new Color(0.2f, 0.55f, 0.25f), 0.6f), Vector3.zero);
        }
        PrimitiveKit.Box("SignBoard", s, new Vector3(0f, 3.4f, -2.3f), new Vector3(2.8f, 0.6f, 0.1f), PrimitiveKit.Mat("BloodRed", new Color(0.55f, 0.03f, 0.05f)), collider: false);
        Thai(PrimitiveKit.Label("เมียงู", s, new Vector3(0f, 3.4f, -2.37f), 0.07f, new Color(1f, 0.9f, 0.45f)));
        // Inside: the glass box with her in it, a couple of snakes, benches
        PrimitiveKit.Box("Stage", s, new Vector3(0f, 0.3f, 1.2f), new Vector3(2.4f, 0.6f, 1.4f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        PrimitiveKit.Box("GlassBox", s, new Vector3(0f, 1.35f, 1.2f), new Vector3(1.8f, 1.5f, 1f), PrimitiveKit.Mat("GlassBlue", new Color(0.55f, 0.75f, 0.85f), 0.9f), collider: false);
        var performer = PrimitiveKit.Group("Performer", s, new Vector3(0f, 0.6f, 1.2f), 180f);
        performer.AddComponent<MarketVendor>().Configure(3);
        for (int k = 0; k < 2; k++)
        {
            var coil = new[] { new Vector3(-0.7f + k * 1.1f, 0.65f, 0.9f), new Vector3(-0.3f + k * 1.1f, 0.75f, 1.5f), new Vector3(-0.6f + k * 1.1f, 0.65f, 1.6f) };
            MeshKit.Place("Snake", s, MeshKit.Sweep("SnakeCoil" + k, MeshKit.Curve(coil[0], coil[1], coil[2], 10), MeshKit.Taper(10, 0.07f, 0.03f), 6),
                PrimitiveKit.Mat("SnakeGreen", new Color(0.2f, 0.55f, 0.25f), 0.6f), Vector3.zero);
        }
        for (int r = 0; r < 2; r++)
            PrimitiveKit.Box("Bench", s, new Vector3(0f, 0.25f, -0.4f - r * 0.9f), new Vector3(3.2f, 0.5f, 0.35f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
        StreetFurniture.HangingLight(s, new Vector3(0f, 2.6f, 0.8f));
        var seat = PrimitiveKit.Group("Seat", s, new Vector3(0.6f, 0.15f, -0.4f)).transform;
        Zone(s, new Vector3(0f, 1f, -3f), new Vector3(2f, 2f, 1.4f), "snake", "เมียงู", seat, 16f);
    }

    // The big concert stage (เวทีคอนเสิร์ต) at the south end facing the fair: truss, LED wall, speaker
    // stacks, a band and the full light show (Seaside.ConcertRig). Free to watch once per festival.
    private static void Concert(Transform root, Vector3 at)
    {
        var st = PrimitiveKit.Group("ConcertStage_เวทีคอนเสิร์ตงานกาชาด", root, at, 180f).transform; // audience to the north
        var showRoot = PrimitiveKit.Group("StageShow", st, Vector3.zero);
        var show = PrimitiveKit.Group("Attractions", showRoot.transform, Vector3.zero).transform;
        showRoot.AddComponent<FestivalGate>().Configure("big", null, "concert");
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var black = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        PrimitiveKit.Box("StageDeck", show, new Vector3(0f, 0.6f, 1f), new Vector3(14f, 1.2f, 8f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussTower", show, new Vector3(x, 4.5f, 4.5f), new Vector3(0.5f, 9f, 0.5f), steel);
        PrimitiveKit.Box("TrussBeam", show, new Vector3(0f, 9f, 4.5f), new Vector3(15f, 0.5f, 0.5f), steel, collider: false);
        PrimitiveKit.Box("TrussFront", show, new Vector3(0f, 8.2f, -2.5f), new Vector3(15f, 0.4f, 0.4f), steel, collider: false);
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussSide", show, new Vector3(x, 8.6f, 1f), new Vector3(0.4f, 0.4f, 7f), steel, collider: false);
        PrimitiveKit.Box("BannerBoard", show, new Vector3(0f, 10f, 4.5f), new Vector3(11f, 1.4f, 0.2f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), collider: false);
        Thai(PrimitiveKit.Label("คอนเสิร์ตงานกาชาด", show, new Vector3(0f, 10f, 4.38f), 0.1f, new Color(1f, 0.9f, 0.45f)));
        PrimitiveKit.Box("LedScreen", show, new Vector3(0f, 4.6f, 4.2f), new Vector3(12f, 5.4f, 0.2f), PrimitiveKit.Glow("LedScreen", new Color(0.35f, 0.25f, 0.95f), 2.2f, 0.4f), collider: false);
        foreach (float x in new[] { -8.8f, 8.8f })
        {
            PrimitiveKit.Box("SpeakerStack", show, new Vector3(x, 2.2f, 0f), new Vector3(1.6f, 4.4f, 1.4f), black);
            for (int i = 0; i < 3; i++)
                PrimitiveKit.Cylinder("Cone", show, new Vector3(x, 0.9f + i * 1.4f, -0.72f), new Vector3(0.9f, 0.02f, 0.9f), steel, new Vector3(90f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Cylinder("BassDrum", show, new Vector3(0f, 1.65f, 2.6f), new Vector3(0.9f, 0.25f, 0.9f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), new Vector3(90f, 0f, 0f), collider: false);
        foreach (float x in new[] { -0.8f, 0.8f })
            PrimitiveKit.Cylinder("Cymbal", show, new Vector3(x, 2.4f, 2.4f), new Vector3(0.6f, 0.02f, 0.6f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        PrimitiveKit.Box("Keyboard", show, new Vector3(-4f, 2f, 1.4f), new Vector3(1.6f, 0.15f, 0.5f), black, collider: false);
        PrimitiveKit.Cylinder("MicStand", show, new Vector3(0f, 2f, -1.6f), new Vector3(0.04f, 0.8f, 0.04f), steel, collider: false);
        int look = 6;
        foreach (var p in new[] { new Vector3(0f, 1.2f, -1.2f), new Vector3(-4f, 1.2f, 1.9f), new Vector3(3.8f, 1.2f, 0.6f), new Vector3(0f, 1.2f, 3.2f), new Vector3(-2f, 1.2f, -0.6f), new Vector3(2f, 1.2f, -0.6f) })
            PrimitiveKit.Group("Performer", show, p, 180f).AddComponent<MarketVendor>().Configure(look++);
        Color[] colors = { new Color(1f, 0.2f, 0.5f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 1f, 0.5f), new Color(0.75f, 0.3f, 1f) };
        for (int i = 0; i < 7; i++)
            PrimitiveKit.Cylinder("StageCan", show, new Vector3(-6f + i * 2f, 7.8f, -2.5f), new Vector3(0.35f, 0.25f, 0.35f), PrimitiveKit.Glow($"StageCan{i % colors.Length}", colors[i % colors.Length], 6f, 0.4f), collider: false);
        foreach (float x in new[] { -4f, 0f, 4f })
        {
            var lg = new GameObject("StageLight");
            lg.transform.SetParent(show, false);
            lg.transform.localPosition = new Vector3(x, 7.5f, -2.5f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = colors[(int)(x + 4f) % colors.Length];
            l.range = 14f;
            l.intensity = 6f;
            l.shadows = LightShadows.None;
        }
        Seaside.ConcertRig(show);
        // Watch from the front: free, once per festival
        var zone = new GameObject("PlayZone");
        zone.transform.SetParent(show, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -5f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(10f, 2f, 3f);
        zone.AddComponent<FestivalBooth>().Configure("concert", "คอนเสิร์ตงานกาชาด", 0);
    }

    // Four searchlights round the fair sweeping the sky at night (SkyBeams)
    private static void Searchlights(Transform t)
    {
        var beam = MeshKit.Lathe("SearchBeam", new[] { new Vector2(0.35f, 0f), new Vector2(2.6f, 45f) }, 16, capTop: false, capBottom: false);
        var beamMat = PrimitiveKit.Beam("SearchBeam", new Color(0.75f, 0.8f, 1f, 1f));
        var housing = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        foreach (var p in new[] { new Vector3(-67.4f, 0f, -38f), new Vector3(-67.4f, 0f, -58f), new Vector3(-40.5f, 0f, -50.5f), new Vector3(-23.5f, 0f, -61f) })
        {
            PrimitiveKit.Box("SearchlightBase", t, p + Vector3.up * 0.4f, new Vector3(1.2f, 0.8f, 1.2f), housing);
            var head = PrimitiveKit.Group("Searchlight", t, p + Vector3.up * 1f).transform;
            PrimitiveKit.Cylinder("Lamp", head, new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.3f, 0.8f), housing, collider: false);
            PrimitiveKit.Cylinder("Lens", head, new Vector3(0f, 0.62f, 0f), new Vector3(0.7f, 0.02f, 0.7f), PrimitiveKit.Glow("StageLens", Color.white, 6f, 0.5f), collider: false);
            var b = MeshKit.Place("Beam", head, beam, beamMat, new Vector3(0f, 0.65f, 0f));
            b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        t.gameObject.AddComponent<SkyBeams>();
    }

    // Fireworks over the whole fair, and strings of bulbs criss-crossing the west wing
    private static void Sparkle(Transform t)
    {
        var fw = PrimitiveKit.Group("Fireworks_พลุ", t, new Vector3(-45f, 0f, -42f)).transform;
        Color[] c = { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(0.35f, 1f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.95f), Color.white };
        var mats = new List<Material>();
        for (int i = 0; i < c.Length; i++) mats.Add(PrimitiveKit.Glow($"Firework{i}", c[i], 25f, 0.3f));
        fw.gameObject.AddComponent<Fireworks>().Configure(mats.ToArray(), new Vector2(36f, 26f), 28f);

        var corners = new[] { new Vector3(-67.8f, 0f, -24.5f), new Vector3(-48.9f, 0f, -24.5f), new Vector3(-48.9f, 0f, -35.5f), new Vector3(-67.8f, 0f, -35.5f) };
        foreach (var p in corners) FairLights.Pole(t, p, 6f);
        FairLights.Festoon(t, corners[0] + Vector3.up * 5.8f, corners[1] + Vector3.up * 5.8f, 0.7f);
        FairLights.Festoon(t, corners[1] + Vector3.up * 5.8f, corners[2] + Vector3.up * 5.8f, 0.7f);
        FairLights.Festoon(t, corners[3] + Vector3.up * 5.8f, corners[0] + Vector3.up * 5.8f, 0.7f);
        FairLights.Festoon(t, corners[0] + Vector3.up * 5.8f, corners[2] + Vector3.up * 5.8f, 1.2f, 0.8f);
        FairLights.Festoon(t, corners[1] + Vector3.up * 5.8f, corners[3] + Vector3.up * 5.8f, 1.2f, 0.8f);
        var south = new[] { new Vector3(-47.5f, 0f, -49f), new Vector3(-23f, 0f, -49f) };
        foreach (var p in south) FairLights.Pole(t, p, 6f);
        FairLights.Festoon(t, south[0] + Vector3.up * 5.8f, south[1] + Vector3.up * 5.8f, 0.9f);
        for (int i = 0; i < 6; i++)
            FairLights.Lantern(t, new Vector3(-45f + i * 4.2f, 4.6f, -49f), i);
    }
}

