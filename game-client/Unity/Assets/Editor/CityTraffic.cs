using System.Collections.Generic;
using UnityEngine;

// The city's main avenue (ถนนสี่เลน): two lanes each way either side of a planted median under the
// skytrain, black-and-white kerbs, zebra crossings and a signalised T-junction with the street up to
// the clock tower. Traffic drives on the left: รถเมล์, แท็กซี่ (Bangkok two-tone and pink), ตุ๊กตุ๊ก,
// รถหรู and ordinary cars (TrafficCar), stopping at the lights (TrafficLight).
public static class CityTraffic
{
    public const float RoadZ = -11.5f;               // centre line of the avenue
    private const float Lane = 2.8f, MedianHalf = 0.6f;
    private const float Length = 180f;                // the ends run out past the edge of town
    private const float NorthEdge = RoadZ + MedianHalf + 2f * Lane;  // -5.3
    private const float SouthEdge = RoadZ - MedianHalf - 2f * Lane;  // -17.7
    private const float ZebraX = 10f, StopX = 12.5f;  // zebra crossings either side of the junction
    private const float SideStopZ = -0.6f;            // the side street's stop line

    // Lane centres (driving on the left: eastbound on the north side)
    private static float EastFast => RoadZ + MedianHalf + Lane * 0.5f;      // -9.5
    private static float EastKerb => RoadZ + MedianHalf + Lane * 1.5f;      // -6.7
    private static float WestFast => RoadZ - MedianHalf - Lane * 0.5f;      // -13.5
    private static float WestKerb => RoadZ - MedianHalf - Lane * 1.5f;      // -16.3

    // gantryWest/East: the overhead direction signs; shelter: the bus shelter's name board (it is
    // also the stop for long-distance coaches, บขส.)
    public static void Build(Transform root, string gantryWest = "↑ ตลาดสด · ชายหาด\n← หอนาฬิกา · ย่านผับ",
        string gantryEast = "↑ วัด · ค่ายมวย · ลานวัด\nสถานีรถไฟฟ้า ↑", string shelter = "ป้ายรถเมล์ · รถทัวร์ บขส.")
    {
        var avenue = PrimitiveKit.Group("Avenue_ถนนสี่เลน", root, Vector3.zero).transform;
        Road(avenue);
        var lights = Junction(avenue);
        Traffic(avenue, lights);
        Gantries(avenue, gantryWest, gantryEast);
        BusShelter(avenue, new Vector3(40f, 0f, SouthEdge - 1.5f), 180f, shelter);
        BusPole(avenue, new Vector3(-40f, 0f, NorthEdge + 0.5f));
    }

    // ── Road surface and markings ────────────────────────────────────────────
    private static void Road(Transform t)
    {
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        var yellow = PrimitiveKit.Mat("RoadYellow", new Color(0.98f, 0.8f, 0.15f));
        var black = PrimitiveKit.Mat("KerbBlack", new Color(0.1f, 0.1f, 0.1f));
        var concrete = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        var grass = PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f));
        var pavement = PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f));
        float width = SouthEdge < NorthEdge ? NorthEdge - SouthEdge : 0f;

        PrimitiveKit.Box("Road", t, new Vector3(0f, 0.02f, RoadZ), new Vector3(Length, 0.04f, width), asphalt);
        PrimitiveKit.Box("SouthPavement", t, new Vector3(0f, 0.01f, SouthEdge - 1.9f), new Vector3(Length, 0.02f, 3.8f), pavement, collider: false);

        // Median: raised kerb with a grass strip and clipped shrubs, open at the zebra crossings
        foreach (var (a, b) in new[] { (-Length / 2f, -ZebraX - 1.6f), (-ZebraX + 1.6f, ZebraX - 1.6f), (ZebraX + 1.6f, Length / 2f) })
        {
            float mid = (a + b) / 2f, len = b - a;
            PrimitiveKit.Box("Median", t, new Vector3(mid, 0.1f, RoadZ), new Vector3(len, 0.16f, MedianHalf * 2f), concrete, collider: false);
            PrimitiveKit.Box("MedianGrass", t, new Vector3(mid, 0.185f, RoadZ), new Vector3(len - 0.2f, 0.02f, MedianHalf * 2f - 0.25f), grass, collider: false);
            for (float x = a + 2f; x < b - 1f; x += 4f)
            {
                if (Mathf.Abs(Mathf.Repeat(x + 8f, 16f) - 8f) < 1.5f) continue; // clear of the skytrain pillars
                Nature.Bush(t, new Vector3(x, 0.18f, RoadZ), 0.45f, (int)(x * 7f), flowers: ((int)x & 4) == 0);
            }
            PrimitiveKit.Box("MedianLine", t, new Vector3(mid, 0.042f, RoadZ + MedianHalf + 0.15f), new Vector3(len, 0.005f, 0.12f), yellow, collider: false);
            PrimitiveKit.Box("MedianLine", t, new Vector3(mid, 0.042f, RoadZ - MedianHalf - 0.15f), new Vector3(len, 0.005f, 0.12f), yellow, collider: false);
        }

        // Lane lines (dashed) and edge lines (solid)
        foreach (float z in new[] { RoadZ + MedianHalf + Lane, RoadZ - MedianHalf - Lane })
            for (float x = -Length / 2f + 2f; x < Length / 2f; x += 9f)
                if (Mathf.Abs(Mathf.Abs(x) - ZebraX) > 3f) PrimitiveKit.Box("LaneDash", t, new Vector3(x, 0.042f, z), new Vector3(3f, 0.005f, 0.13f), white, collider: false);
        foreach (float z in new[] { NorthEdge - 0.25f, SouthEdge + 0.25f })
            PrimitiveKit.Box("EdgeLine", t, new Vector3(0f, 0.042f, z), new Vector3(Length, 0.005f, 0.12f), white, collider: false);

        // Black-and-white kerbs through town (the north side opens onto the clock tower street)
        foreach (float z in new[] { NorthEdge + 0.12f, SouthEdge - 0.12f })
        {
            PrimitiveKit.Box("Kerb", t, new Vector3(0f, 0.07f, z), new Vector3(100f, 0.12f, 0.24f), white, collider: false);
            for (float x = -49.5f; x < 50f; x += 2f)
            {
                if (z > RoadZ && Mathf.Abs(x) < 3.5f) continue;
                PrimitiveKit.Box("KerbBlack", t, new Vector3(x, 0.072f, z), new Vector3(1f, 0.125f, 0.245f), black, collider: false);
            }
        }

        // Zebra crossings either side of the junction, with stop lines before them
        foreach (float zx in new[] { -ZebraX, ZebraX })
            for (float z = SouthEdge + 0.6f; z < NorthEdge - 0.3f; z += 1f)
            {
                if (Mathf.Abs(z - RoadZ) < MedianHalf + 0.2f) continue;
                PrimitiveKit.Box("Zebra", t, new Vector3(zx, 0.043f, z), new Vector3(3f, 0.005f, 0.5f), white, collider: false);
            }
        PrimitiveKit.Box("StopLine", t, new Vector3(-StopX, 0.043f, (NorthEdge + RoadZ + MedianHalf) / 2f), new Vector3(0.35f, 0.005f, 2f * Lane), white, collider: false);
        PrimitiveKit.Box("StopLine", t, new Vector3(StopX, 0.043f, (SouthEdge + RoadZ - MedianHalf) / 2f), new Vector3(0.35f, 0.005f, 2f * Lane), white, collider: false);

        // The clock tower street: zebra across its mouth, stop line, a yellow centre line
        for (float x = -2.5f; x <= 2.6f; x += 1f)
            PrimitiveKit.Box("Zebra", t, new Vector3(x, 0.043f, -2.4f), new Vector3(0.5f, 0.005f, 2.4f), white, collider: false);
        PrimitiveKit.Box("StopLine", t, new Vector3(1.5f, 0.043f, SideStopZ), new Vector3(2.9f, 0.005f, 0.3f), white, collider: false);
        for (float z = 0.5f; z < 8f; z += 2.5f)
            PrimitiveKit.Box("CentreDash", t, new Vector3(0f, 0.043f, z), new Vector3(0.12f, 0.005f, 1.4f), yellow, collider: false);
    }

    // ── Traffic lights ───────────────────────────────────────────────────────
    private static TrafficLight Junction(Transform t)
    {
        var j = PrimitiveKit.Group("Junction_ไฟแดง", t, Vector3.zero).transform;
        var pole = PrimitiveKit.Mat("SignalPole", new Color(0.82f, 0.83f, 0.85f), 0.6f, 0.5f);
        var heads = new List<TrafficLight.Head>();

        // Eastbound: mast arm from the north pavement over both lanes, a head per lane and one on the pole
        MastArm(j, new Vector3(-StopX - 1f, 0f, NorthEdge + 0.5f), -1f, RoadZ + MedianHalf + 0.3f, pole);
        heads.Add(Signal(j, new Vector3(-StopX - 1f, 5.2f, EastKerb), 90f, 0, pole));
        heads.Add(Signal(j, new Vector3(-StopX - 1f, 5.2f, EastFast), 90f, 0, pole));
        heads.Add(Signal(j, new Vector3(-StopX - 1f, 2.6f, NorthEdge + 0.5f), 90f, 0, pole));
        // Westbound: from the south pavement
        MastArm(j, new Vector3(StopX + 1f, 0f, SouthEdge - 0.5f), 1f, RoadZ - MedianHalf - 0.3f, pole);
        heads.Add(Signal(j, new Vector3(StopX + 1f, 5.2f, WestKerb), -90f, 0, pole));
        heads.Add(Signal(j, new Vector3(StopX + 1f, 5.2f, WestFast), -90f, 0, pole));
        heads.Add(Signal(j, new Vector3(StopX + 1f, 2.6f, SouthEdge - 0.5f), -90f, 0, pole));
        // Clock tower street, coming down to the avenue
        PrimitiveKit.Cylinder("SignalPole", j, new Vector3(3.6f, 1.6f, SideStopZ), new Vector3(0.16f, 1.6f, 0.16f), pole, collider: false);
        heads.Add(Signal(j, new Vector3(3.6f, 2.6f, SideStopZ), 180f, 1, pole));

        // People on foot: walk / don't walk at both ends of both zebras
        foreach (float x in new[] { -ZebraX + 1.9f, ZebraX - 1.9f })
            foreach (float side in new[] { 1f, -1f })
            {
                float z = side > 0f ? NorthEdge + 0.4f : SouthEdge - 0.4f;
                PrimitiveKit.Cylinder("WalkPole", j, new Vector3(x, 1.15f, z), new Vector3(0.1f, 1.15f, 0.1f), pole, collider: false);
                heads.Add(WalkSignal(j, new Vector3(x, 2.5f, z), side > 0f ? 0f : 180f));
            }

        var light = j.gameObject.AddComponent<TrafficLight>();
        light.Configure(heads.ToArray());
        return light;
    }

    // A pole with an arm reaching over the lanes at 5.6 m; dir -1 reaches south, +1 north
    private static void MastArm(Transform t, Vector3 foot, float dir, float reachZ, Material mat)
    {
        PrimitiveKit.Cylinder("SignalPole", t, foot + new Vector3(0f, 2.9f, 0f), new Vector3(0.2f, 2.9f, 0.2f), mat, collider: false);
        float len = Mathf.Abs(reachZ - foot.z);
        PrimitiveKit.Cylinder("SignalArm", t, new Vector3(foot.x, 5.7f, (foot.z + reachZ) / 2f), new Vector3(0.12f, len / 2f, 0.12f), mat, new Vector3(90f, 0f, 0f), collider: false);
    }

    // A three-lamp head (red over yellow over green) with a countdown box under it; front faces local -Z
    private static TrafficLight.Head Signal(Transform t, Vector3 at, float yaw, int approach, Material pole)
    {
        var g = PrimitiveKit.Group("Signal", t, at, yaw).transform;
        var housing = PrimitiveKit.Mat("SignalBlack", new Color(0.08f, 0.08f, 0.09f));
        var lens = PrimitiveKit.Mat("SignalLens", new Color(0.18f, 0.18f, 0.2f));
        PrimitiveKit.Box("Housing", g, Vector3.zero, new Vector3(0.38f, 1.05f, 0.25f), housing, collider: false);
        PrimitiveKit.Box("Backplate", g, new Vector3(0f, 0f, 0.14f), new Vector3(0.6f, 1.25f, 0.03f), PrimitiveKit.Mat("SignalYellowPlate", new Color(0.95f, 0.8f, 0.15f)), collider: false);
        var head = new TrafficLight.Head { approach = approach };
        Color[] colors = { new Color(1f, 0.12f, 0.08f), new Color(1f, 0.75f, 0.1f), new Color(0.15f, 1f, 0.35f) };
        string[] names = { "SignalRed", "SignalAmber", "SignalGreen" };
        for (int i = 0; i < 3; i++)
        {
            float y = 0.32f - i * 0.32f;
            PrimitiveKit.Cylinder("Lens", g, new Vector3(0f, y, -0.13f), new Vector3(0.24f, 0.01f, 0.24f), lens, new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Visor", g, new Vector3(0f, y + 0.13f, -0.2f), new Vector3(0.3f, 0.02f, 0.14f), housing, collider: false);
            var lit = PrimitiveKit.Cylinder("Lit", g, new Vector3(0f, y, -0.14f), new Vector3(0.24f, 0.012f, 0.24f), PrimitiveKit.Glow(names[i], colors[i], 3f), new Vector3(90f, 0f, 0f), collider: false);
            lit.SetActive(false);
            if (i == 0) head.red = lit; else if (i == 1) head.yellow = lit; else head.green = lit;
        }
        PrimitiveKit.Box("CountdownBox", g, new Vector3(0f, -0.72f, 0f), new Vector3(0.38f, 0.32f, 0.22f), housing, collider: false);
        head.countdown = PrimitiveKit.Label("0", g, new Vector3(0f, -0.72f, -0.115f), 0.05f, colors[0]);
        return head;
    }

    // Pedestrian signal: red standing man / green walking man
    private static TrafficLight.Head WalkSignal(Transform t, Vector3 at, float yaw)
    {
        var g = PrimitiveKit.Group("WalkSignal", t, at, yaw).transform;
        var housing = PrimitiveKit.Mat("SignalBlack", new Color(0.08f, 0.08f, 0.09f));
        PrimitiveKit.Box("Housing", g, Vector3.zero, new Vector3(0.36f, 0.7f, 0.2f), housing, collider: false);
        var head = new TrafficLight.Head { approach = 1, pedestrian = true };
        GameObject Man(string name, Color c, float y, bool walking)
        {
            var man = PrimitiveKit.Group(name, g, new Vector3(0f, y, -0.105f)).transform;
            var glow = PrimitiveKit.Glow(name, c, 3f);
            PrimitiveKit.Cylinder("Head", man, new Vector3(0f, 0.09f, 0f), new Vector3(0.05f, 0.005f, 0.05f), glow, new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Body", man, new Vector3(0f, 0.01f, 0f), new Vector3(0.05f, 0.1f, 0.008f), glow, collider: false);
            PrimitiveKit.Box("LegL", man, new Vector3(-0.02f, -0.08f, 0f), new Vector3(0.018f, 0.09f, 0.008f), glow, new Vector3(0f, 0f, walking ? -25f : 0f), collider: false);
            PrimitiveKit.Box("LegR", man, new Vector3(0.02f, -0.08f, 0f), new Vector3(0.018f, 0.09f, 0.008f), glow, new Vector3(0f, 0f, walking ? 25f : 0f), collider: false);
            man.gameObject.SetActive(false);
            return man.gameObject;
        }
        head.red = Man("WalkRed", new Color(1f, 0.15f, 0.1f), 0.16f, false);
        head.green = Man("WalkGreen", new Color(0.2f, 1f, 0.4f), -0.16f, true);
        return head;
    }

    // ── Traffic ──────────────────────────────────────────────────────────────
    private static void Traffic(Transform t, TrafficLight lights)
    {
        var traffic = PrimitiveKit.Group("Traffic_รถสัญจร", t, Vector3.zero).transform;
        float half = Length / 2f;
        Vector3[] Straight(float z, bool east) => east
            ? new[] { new Vector3(-half, 0f, z), new Vector3(half, 0f, z) }
            : new[] { new Vector3(half, 0f, z), new Vector3(-half, 0f, z) };

        var eastFast = Straight(EastFast, true);
        var eastKerb = Straight(EastKerb, true);
        var westFast = Straight(WestFast, false);
        var westKerb = Straight(WestKerb, false);
        var side = SideLoop();

        float StopE(Vector3[] p, float z) => TrafficCar.DistanceTo(p, new Vector3(-StopX, 0f, z));
        float StopW(Vector3[] p, float z) => TrafficCar.DistanceTo(p, new Vector3(StopX, 0f, z));

        void Add(string kind, int variant, Vector3[] path, float start, float[] stops, int[] approaches, float[] busStops = null)
        {
            var v = Vehicle(traffic, kind, variant, out float halfLen, out float speed);
            v.gameObject.AddComponent<TrafficCar>().Configure(path, false, speed, halfLen, start, lights, stops, approaches, busStops);
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.position = traffic.TransformPoint(path[0]);
        }

        float[] e0 = { StopE(eastFast, EastFast) }, e1 = { StopE(eastKerb, EastKerb) };
        float[] w0 = { StopW(westFast, WestFast) }, w1 = { StopW(westKerb, WestKerb) };
        int[] main = { 0 };
        Add("super", 0, eastFast, 10f, e0, main);
        Add("taxi", 0, eastFast, 70f, e0, main);
        Add("sedan", 0, eastFast, 130f, e0, main);
        Add("bus", 0, eastKerb, 30f, e1, main, new[] { TrafficCar.DistanceTo(eastKerb, new Vector3(-40f, 0f, EastKerb)) });
        Add("taxi", 1, eastKerb, 100f, e1, main);
        Add("limo", 0, eastKerb, 150f, e1, main);
        Add("super", 1, westFast, 20f, w0, main);
        Add("taxi", 2, westFast, 80f, w0, main);
        Add("sedan", 1, westFast, 140f, w0, main);
        Add("bus", 1, westKerb, 40f, w1, main, new[] { TrafficCar.DistanceTo(westKerb, new Vector3(40f, 0f, WestKerb)) });
        Add("tuktuk", 0, westKerb, 100f, w1, main);
        Add("taxi", 3, westKerb, 160f, w1, main);

        // Round the clock tower street: in from the west, left up the street, U-turn before the
        // tower, back down to the lights, left onto the avenue and away east
        float[] sideStops = { StopE(side, EastKerb), TrafficCar.DistanceTo(side, new Vector3(1.5f, 0f, SideStopZ)) };
        int[] sideFor = { 0, 1 };
        Add("tuktuk", 1, side, 15f, sideStops, sideFor);
        Add("tuktuk", 2, side, 62f, sideStops, sideFor);
        Add("taxi", 0, side, 190f, sideStops, sideFor);
    }

    private static Vector3[] SideLoop()
    {
        var p = new List<Vector3> { new Vector3(-Length / 2f, 0f, EastKerb), new Vector3(-4.5f, 0f, EastKerb) };
        const float r = 3f;
        void Arc(Vector3 c, float from, float to, float radius)
        {
            for (int i = 1; i <= 8; i++)
            {
                float a = Mathf.Lerp(from, to, i / 8f) * Mathf.Deg2Rad;
                p.Add(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
        }
        Arc(new Vector3(-4.5f, 0f, EastKerb + r), -90f, 0f, r);      // left into the street, heading north on x -1.5
        p.Add(new Vector3(-1.5f, 0f, 6f));
        Arc(new Vector3(0f, 0f, 6f), 180f, 0f, 1.5f);                // U-turn before the tower island
        p.Add(new Vector3(1.5f, 0f, EastKerb + r));                    // down past the stop line
        Arc(new Vector3(4.5f, 0f, EastKerb + r), 180f, 270f, r);      // left onto the avenue, heading east
        p.Add(new Vector3(Length / 2f, 0f, EastKerb));
        return p.ToArray();
    }

    // ── Vehicles (front along +Z, wheels on the ground) ──────────────────────
    private static readonly Color Tinted = new Color(0.2f, 0.28f, 0.36f);

    private static Transform Vehicle(Transform parent, string kind, int variant, out float halfLength, out float speed)
    {
        var v = PrimitiveKit.Group("Vehicle_" + kind, parent, Vector3.zero).transform;
        Vector3 size;
        switch (kind)
        {
            case "bus": size = Bus(v, variant); speed = 7.5f; break;
            case "taxi": size = Taxi(v, variant); speed = 10f; break;
            case "tuktuk": size = TukTuk(v, variant); speed = 8f; break;
            case "super": size = Supercar(v, variant); speed = 12f; break;
            case "limo": size = Sedan(v, new Color(0.05f, 0.05f, 0.07f), 5.2f, 1.9f, chrome: true); speed = 10.5f; break;
            default: size = Sedan(v, variant == 0 ? new Color(0.95f, 0.95f, 0.96f) : new Color(0.7f, 0.72f, 0.75f), 4.4f, 1.78f, chrome: false); speed = 10f; break;
        }
        var box = v.gameObject.AddComponent<BoxCollider>();
        box.size = size;
        box.center = new Vector3(0f, size.y / 2f, 0f);
        halfLength = size.z / 2f;
        return v;
    }

    private static Material M(string name, Color c, float smooth = 0.6f, float metal = 0.2f) => PrimitiveKit.Mat(name, c, smooth, metal);

    private static void Wheel(Transform v, Vector3 at, float diameter, float width, Color rim)
    {
        var w = new GameObject("Wheel").transform;
        w.SetParent(v, false);
        w.localPosition = at;
        w.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PrimitiveKit.Cylinder("Tyre", w, Vector3.zero, new Vector3(diameter, width / 2f, diameter), M("Tyre", new Color(0.08f, 0.08f, 0.09f), 0.2f, 0f), collider: false);
        PrimitiveKit.Box("Hub", w, Vector3.zero, new Vector3(diameter * 0.62f, width + 0.02f, diameter * 0.14f), M("Rim" + ColorUtility.ToHtmlStringRGB(rim), rim, 0.8f, 0.9f), collider: false);
        PrimitiveKit.Box("Hub", w, Vector3.zero, new Vector3(diameter * 0.14f, width + 0.02f, diameter * 0.62f), M("Rim" + ColorUtility.ToHtmlStringRGB(rim), rim, 0.8f, 0.9f), collider: false);
    }

    private static void Lamps(Transform v, float halfLen, float halfWidth, float y)
    {
        var head = PrimitiveKit.Glow("CarHeadlight", new Color(1f, 0.97f, 0.88f), 1.8f);
        var tail = PrimitiveKit.Glow("CarTaillight", new Color(1f, 0.12f, 0.1f), 1.5f);
        foreach (float x in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Headlight", v, new Vector3(x * (halfWidth - 0.3f), y, halfLen), new Vector3(0.38f, 0.12f, 0.04f), head, collider: false);
            PrimitiveKit.Box("Taillight", v, new Vector3(x * (halfWidth - 0.25f), y, -halfLen), new Vector3(0.3f, 0.12f, 0.04f), tail, collider: false);
        }
    }

    // Saloon car: lower body, glass cabin, roof; returns its size
    private static Vector3 Sedan(Transform v, Color paint, float length, float width, bool chrome, Color? cabinPaint = null)
    {
        var body = M("CarPaint" + ColorUtility.ToHtmlStringRGB(paint), paint, 0.85f, 0.4f);
        var upper = cabinPaint.HasValue ? M("CarPaint" + ColorUtility.ToHtmlStringRGB(cabinPaint.Value), cabinPaint.Value, 0.85f, 0.4f) : body;
        var glass = M("CarGlassTint", Tinted, 0.95f, 0.3f);
        float hl = length / 2f, hw = width / 2f;
        PrimitiveKit.Box("Body", v, new Vector3(0f, 0.62f, 0f), new Vector3(width, 0.58f, length), body, collider: false);
        PrimitiveKit.Box("Hood", v, new Vector3(0f, 0.93f, hl * 0.62f), new Vector3(width - 0.08f, 0.06f, length * 0.3f), body, new Vector3(4f, 0f, 0f), collider: false);
        float cabin = length * 0.46f;
        PrimitiveKit.Box("Cabin", v, new Vector3(0f, 1.2f, -0.12f), new Vector3(width - 0.14f, 0.52f, cabin), glass, collider: false);
        PrimitiveKit.Box("Roof", v, new Vector3(0f, 1.49f, -0.15f), new Vector3(width - 0.2f, 0.08f, cabin - 0.25f), upper, collider: false);
        foreach (float x in new[] { -1f, 1f })
            PrimitiveKit.Box("Pillar", v, new Vector3(x * (hw - 0.08f), 1.2f, -0.12f), new Vector3(0.06f, 0.52f, 0.1f), upper, collider: false);
        PrimitiveKit.Box("Bumper", v, new Vector3(0f, 0.42f, hl), new Vector3(width, 0.18f, 0.12f), M("Bumper", new Color(0.12f, 0.12f, 0.13f)), collider: false);
        PrimitiveKit.Box("Bumper", v, new Vector3(0f, 0.42f, -hl), new Vector3(width, 0.18f, 0.12f), M("Bumper", new Color(0.12f, 0.12f, 0.13f)), collider: false);
        if (chrome)
        {
            var c = M("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.95f, 1f);
            PrimitiveKit.Box("Grille", v, new Vector3(0f, 0.7f, hl + 0.02f), new Vector3(0.8f, 0.3f, 0.04f), c, collider: false);
            foreach (float x in new[] { -1f, 1f })
                PrimitiveKit.Box("ChromeLine", v, new Vector3(x * (hw + 0.005f), 0.88f, 0f), new Vector3(0.02f, 0.04f, length - 0.6f), c, collider: false);
        }
        Lamps(v, hl + 0.01f, hw, 0.75f);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                Wheel(v, new Vector3(x * (hw - 0.08f), 0.34f, z * (hl - 0.85f)), 0.68f, 0.24f, chrome ? new Color(0.9f, 0.75f, 0.35f) : new Color(0.75f, 0.77f, 0.8f));
        return new Vector3(width, 1.55f, length);
    }

    // Bangkok taxis: two-tone green and yellow, all pink, orange, blue — with the TAXI-METER roof sign
    private static Vector3 Taxi(Transform v, int variant)
    {
        Color lower, upper;
        switch (variant % 4)
        {
            case 0: lower = new Color(1f, 0.42f, 0.65f); upper = lower; break;                                  // pink
            case 1: lower = new Color(0.15f, 0.55f, 0.25f); upper = new Color(0.98f, 0.85f, 0.15f); break;      // green-yellow
            case 2: lower = new Color(0.95f, 0.5f, 0.1f); upper = lower; break;                                 // orange
            default: lower = new Color(0.15f, 0.4f, 0.85f); upper = new Color(0.95f, 0.95f, 0.95f); break;      // blue-white
        }
        var size = Sedan(v, lower, 4.4f, 1.75f, chrome: false, cabinPaint: upper);
        var sign = PrimitiveKit.Glow("TaxiSign", new Color(1f, 0.92f, 0.6f), 1.2f);
        PrimitiveKit.Box("TaxiSign", v, new Vector3(0f, 1.66f, -0.1f), new Vector3(0.95f, 0.26f, 0.32f), sign, collider: false);
        PrimitiveKit.Label("TAXI-METER", v, new Vector3(0f, 1.66f, -0.27f), 0.012f, new Color(0.7f, 0.1f, 0.1f));
        PrimitiveKit.Label("TAXI-METER", v, new Vector3(0f, 1.66f, 0.07f), 0.012f, new Color(0.7f, 0.1f, 0.1f), 180f);
        return size + new Vector3(0f, 0.25f, 0f);
    }

    private static Vector3 Supercar(Transform v, int variant)
    {
        Color paint = variant == 0 ? new Color(0.8f, 0.05f, 0.08f) : new Color(0.98f, 0.8f, 0.1f);
        var body = M("CarPaint" + ColorUtility.ToHtmlStringRGB(paint), paint, 0.95f, 0.5f);
        var glass = M("CarGlassTint", Tinted, 0.95f, 0.3f);
        var black = M("CarbonBlack", new Color(0.06f, 0.06f, 0.07f), 0.7f, 0.3f);
        const float length = 4.5f, width = 1.98f, hl = 2.25f, hw = 0.99f;
        PrimitiveKit.Box("Body", v, new Vector3(0f, 0.5f, 0f), new Vector3(width, 0.45f, length), body, collider: false);
        PrimitiveKit.Box("Nose", v, new Vector3(0f, 0.68f, 1.5f), new Vector3(width - 0.1f, 0.1f, 1.4f), body, new Vector3(8f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Cabin", v, new Vector3(0f, 0.9f, -0.25f), new Vector3(1.55f, 0.38f, 1.7f), glass, collider: false);
        PrimitiveKit.Box("Roof", v, new Vector3(0f, 1.1f, -0.35f), new Vector3(1.4f, 0.06f, 1.1f), body, collider: false);
        PrimitiveKit.Box("Spoiler", v, new Vector3(0f, 1.0f, -2.05f), new Vector3(width - 0.1f, 0.05f, 0.35f), black, collider: false);
        foreach (float x in new[] { -0.75f, 0.75f })
            PrimitiveKit.Box("SpoilerFin", v, new Vector3(x, 0.85f, -2.05f), new Vector3(0.05f, 0.28f, 0.25f), black, collider: false);
        foreach (float x in new[] { -1f, 1f })
            PrimitiveKit.Box("SideIntake", v, new Vector3(x * (hw + 0.005f), 0.55f, -0.7f), new Vector3(0.02f, 0.22f, 0.7f), black, collider: false);
        PrimitiveKit.Box("Diffuser", v, new Vector3(0f, 0.33f, -hl), new Vector3(width - 0.2f, 0.12f, 0.1f), black, collider: false);
        Lamps(v, hl + 0.01f, hw, 0.6f);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                Wheel(v, new Vector3(x * (hw - 0.06f), 0.34f, z * (hl - 0.8f)), 0.68f, 0.3f, new Color(0.15f, 0.15f, 0.16f));
        return new Vector3(width, 1.15f, length);
    }

    // City bus: classic cream-and-red, or the new blue electric one
    private static Vector3 Bus(Transform v, int variant)
    {
        const float length = 11f, width = 2.5f, hl = 5.5f, hw = 1.25f;
        Color main = variant == 0 ? new Color(0.95f, 0.9f, 0.78f) : new Color(0.15f, 0.4f, 0.8f);
        Color band = variant == 0 ? new Color(0.8f, 0.12f, 0.12f) : new Color(0.95f, 0.95f, 0.97f);
        var body = M("BusPaint" + variant, main, 0.7f, 0.2f);
        var stripe = M("BusBand" + variant, band, 0.7f, 0.2f);
        var glass = M("CarGlassTint", Tinted, 0.95f, 0.3f);
        var dark = M("Bumper", new Color(0.12f, 0.12f, 0.13f));
        PrimitiveKit.Box("Body", v, new Vector3(0f, 1.7f, 0f), new Vector3(width, 2.6f, length), body, collider: false);
        PrimitiveKit.Box("Windows", v, new Vector3(0f, 2.2f, -0.3f), new Vector3(width + 0.02f, 0.95f, length - 2f), glass, collider: false);
        PrimitiveKit.Box("Windscreen", v, new Vector3(0f, 2.0f, hl + 0.01f), new Vector3(width - 0.2f, 1.5f, 0.04f), glass, collider: false);
        PrimitiveKit.Box("Band", v, new Vector3(0f, 1.25f, 0f), new Vector3(width + 0.03f, 0.35f, length + 0.02f), stripe, collider: false);
        PrimitiveKit.Box("Skirt", v, new Vector3(0f, 0.5f, 0f), new Vector3(width - 0.05f, 0.25f, length - 0.1f), dark, collider: false);
        PrimitiveKit.Box("RoofUnit", v, new Vector3(0f, 3.12f, 1.5f), new Vector3(1.8f, 0.25f, 3f), M("BusRoof", new Color(0.85f, 0.86f, 0.88f)), collider: false);
        // Doors on the kerb side (left, -X): front and middle
        foreach (float z in new[] { hl - 1.3f, -0.8f })
            PrimitiveKit.Box("Door", v, new Vector3(-hw - 0.01f, 1.45f, z), new Vector3(0.03f, 2.1f, 1.1f), glass, collider: false);
        var led = PrimitiveKit.Glow("BusLed", new Color(1f, 0.6f, 0.1f), 1.8f, 0.2f);
        PrimitiveKit.Box("RouteBoard", v, new Vector3(0f, 2.86f, hl + 0.02f), new Vector3(width - 0.4f, 0.32f, 0.04f), led, collider: false);
        PrimitiveKit.Label(variant == 0 ? "8  เมือง - หาดทราย" : "EV 8  สถานีรถไฟฟ้า", v, new Vector3(0f, 2.86f, hl + 0.05f), 0.02f, new Color(0.15f, 0.05f, 0f), 180f);
        PrimitiveKit.Box("RouteBoardBack", v, new Vector3(0f, 2.86f, -hl - 0.02f), new Vector3(0.8f, 0.32f, 0.04f), led, collider: false);
        PrimitiveKit.Label("8", v, new Vector3(0f, 2.86f, -hl - 0.05f), 0.03f, new Color(0.15f, 0.05f, 0f));
        Lamps(v, hl + 0.01f, hw, 0.85f);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { hl - 2.1f, -hl + 2.4f })
                Wheel(v, new Vector3(x * (hw - 0.12f), 0.5f, z), 1f, 0.32f, new Color(0.8f, 0.82f, 0.85f));
        return new Vector3(width, 3.25f, length);
    }

    // ตุ๊กตุ๊ก: one wheel in front, an open cab under a bright canopy with a gold fringe, bench seat behind
    private static Vector3 TukTuk(Transform v, int variant)
    {
        Color[] canopies = { new Color(0.1f, 0.45f, 0.85f), new Color(0.15f, 0.6f, 0.3f), new Color(0.85f, 0.15f, 0.2f) };
        var paint = M("TukTukPaint" + variant, canopies[variant % 3], 0.85f, 0.3f);
        var trim = M("ThaiGoldTrim", new Color(0.92f, 0.72f, 0.28f), 0.7f, 0.8f);
        var chrome = M("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.95f, 1f);
        var seat = M("TukTukSeat", new Color(0.55f, 0.1f, 0.12f), 0.4f, 0f);
        var dark = M("Bumper", new Color(0.12f, 0.12f, 0.13f));
        PrimitiveKit.Box("Floor", v, new Vector3(0f, 0.42f, -0.2f), new Vector3(1.35f, 0.14f, 2.3f), dark, collider: false);
        PrimitiveKit.Box("Cowl", v, new Vector3(0f, 0.75f, 1.0f), new Vector3(0.75f, 0.75f, 0.6f), paint, collider: false);
        PrimitiveKit.Box("Windscreen", v, new Vector3(0f, 1.35f, 0.95f), new Vector3(1.2f, 0.5f, 0.03f), M("CarGlassTint", Tinted, 0.95f, 0.3f), collider: false);
        PrimitiveKit.Box("DriverSeat", v, new Vector3(0f, 0.75f, 0.35f), new Vector3(0.5f, 0.15f, 0.4f), seat, collider: false);
        PrimitiveKit.Box("Bench", v, new Vector3(0f, 0.72f, -0.75f), new Vector3(1.25f, 0.2f, 0.55f), seat, collider: false);
        PrimitiveKit.Box("BenchBack", v, new Vector3(0f, 1.05f, -1.05f), new Vector3(1.25f, 0.55f, 0.12f), seat, collider: false);
        PrimitiveKit.Box("RearPanel", v, new Vector3(0f, 0.85f, -1.32f), new Vector3(1.35f, 0.75f, 0.06f), paint, collider: false);
        PrimitiveKit.Label("TUK TUK", v, new Vector3(0f, 0.95f, -1.36f), 0.018f, new Color(1f, 0.9f, 0.4f));
        foreach (float y in new[] { 0.62f, 0.7f })
            PrimitiveKit.Box("Stripe", v, new Vector3(0f, y, -1.355f), new Vector3(1.35f, 0.04f, 0.01f), y < 0.65f ? trim : M("ThaiBlue", new Color(0.15f, 0.2f, 0.6f)), collider: false);
        PrimitiveKit.Box("Canopy", v, new Vector3(0f, 1.82f, -0.25f), new Vector3(1.5f, 0.1f, 2.4f), paint, collider: false);
        PrimitiveKit.Box("Fringe", v, new Vector3(0f, 1.73f, -0.25f), new Vector3(1.52f, 0.08f, 2.42f), trim, collider: false);
        foreach (float x in new[] { -0.68f, 0.68f })
            foreach (float z in new[] { 0.85f, -1.3f })
                PrimitiveKit.Cylinder("Post", v, new Vector3(x, 1.2f, z), new Vector3(0.04f, 0.55f, 0.04f), chrome, collider: false);
        var head = PrimitiveKit.Glow("CarHeadlight", new Color(1f, 0.97f, 0.88f), 1.8f);
        PrimitiveKit.Box("Headlight", v, new Vector3(0f, 0.95f, 1.31f), new Vector3(0.3f, 0.16f, 0.04f), head, collider: false);
        PrimitiveKit.Box("Taillight", v, new Vector3(0f, 0.5f, -1.36f), new Vector3(0.9f, 0.08f, 0.03f), PrimitiveKit.Glow("CarTaillight", new Color(1f, 0.12f, 0.1f), 1.5f), collider: false);
        Wheel(v, new Vector3(0f, 0.25f, 1.15f), 0.5f, 0.16f, new Color(0.8f, 0.82f, 0.85f));
        foreach (float x in new[] { -0.6f, 0.6f })
            Wheel(v, new Vector3(x, 0.25f, -0.95f), 0.5f, 0.18f, new Color(0.8f, 0.82f, 0.85f));
        return new Vector3(1.5f, 1.9f, 2.8f);
    }

    // ── Street furniture ─────────────────────────────────────────────────────
    // Green overhead direction signs over each carriageway
    private static void Gantries(Transform t, string west, string east)
    {
        var steel = PrimitiveKit.Mat("SignalPole", new Color(0.82f, 0.83f, 0.85f), 0.6f, 0.5f);
        var green = PrimitiveKit.Mat("RoadSignGreen", new Color(0.05f, 0.42f, 0.25f));
        void Gantry(float x, float zFrom, float zTo, float yaw, string text)
        {
            foreach (float z in new[] { zFrom, zTo })
                PrimitiveKit.Cylinder("GantryPost", t, new Vector3(x, 3.1f, z), new Vector3(0.22f, 3.1f, 0.22f), steel);
            float mid = (zFrom + zTo) / 2f, len = Mathf.Abs(zTo - zFrom);
            PrimitiveKit.Box("GantryBeam", t, new Vector3(x, 6.1f, mid), new Vector3(0.18f, 0.18f, len + 0.3f), steel, collider: false);
            var g = PrimitiveKit.Group("RoadSign", t, new Vector3(x, 5.3f, mid), yaw).transform;
            PrimitiveKit.Box("Board", g, Vector3.zero, new Vector3(len - 0.6f, 1.5f, 0.08f), green, collider: false);
            PrimitiveKit.Box("Border", g, new Vector3(0f, 0f, 0.01f), new Vector3(len - 0.4f, 1.62f, 0.06f), PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f)), collider: false);
            PrimitiveKit.Label(text, g, new Vector3(0f, 0f, -0.05f), 0.035f, Color.white);
        }
        Gantry(-30f, NorthEdge + 0.4f, RoadZ + 0.3f, 90f, west);
        Gantry(30f, SouthEdge - 0.4f, RoadZ - 0.3f, -90f, east);
    }

    private static void BusShelter(Transform parent, Vector3 at, float yaw, string name)
    {
        var t = PrimitiveKit.Group("BusShelter_ป้ายรถเมล์", parent, at, yaw).transform;
        var steel = PrimitiveKit.Mat("SignalPole", new Color(0.82f, 0.83f, 0.85f), 0.6f, 0.5f);
        var glass = PrimitiveKit.Glow("LiftGlass", new Color(0.6f, 0.8f, 0.95f), 0.3f, 0.6f);
        foreach (float x in new[] { -1.8f, 1.8f })
            PrimitiveKit.Box("Post", t, new Vector3(x, 1.25f, 0.5f), new Vector3(0.1f, 2.5f, 0.1f), steel);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 2.55f, 0.1f), new Vector3(4.2f, 0.1f, 1.5f), PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f)), collider: false);
        PrimitiveKit.Box("BackGlass", t, new Vector3(0f, 1.4f, 0.55f), new Vector3(3.5f, 1.8f, 0.04f), glass, collider: false);
        PrimitiveKit.Box("Bench", t, new Vector3(0f, 0.45f, 0.3f), new Vector3(3f, 0.08f, 0.4f), steel, collider: false);
        PrimitiveKit.Box("AdPanel", t, new Vector3(1.75f, 1.3f, 0.1f), new Vector3(0.05f, 1.6f, 0.9f), PrimitiveKit.Glow("AdLight", new Color(1f, 0.85f, 0.6f), 1.2f), collider: false);
        PrimitiveKit.Box("NameBoard", t, new Vector3(0f, 2.85f, -0.4f), new Vector3(2.6f, 0.45f, 0.06f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 2.85f, -0.44f), 0.045f, new Color(0.2f, 0.1f, 0.05f));

        // Coaches to other provinces (บขส.) stop here too: stand under the roof for tickets
        var zone = new GameObject("CoachZone_รถทัวร์");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0.1f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.4f, 2f, 1.2f);
        zone.AddComponent<TravelStop>().Configure("ป้ายรถทัวร์ บขส. · ไปต่างจังหวัด", longDistance: TravelMode.Bus);
        // Off the coach: on the pavement beside the shelter
        PrimitiveKit.Group("SpawnPoint_Bus", t, new Vector3(3.2f, 0f, 0.2f), 180f).AddComponent<SpawnPoint>().Configure(TravelMode.Bus);
    }

    private static void BusPole(Transform parent, Vector3 at)
    {
        PrimitiveKit.Cylinder("BusPole", parent, at + new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), PrimitiveKit.Mat("SignalPole", new Color(0.82f, 0.83f, 0.85f), 0.6f, 0.5f), collider: false);
        PrimitiveKit.Box("BusSign", parent, at + new Vector3(0f, 2.55f, 0f), new Vector3(0.7f, 0.45f, 0.04f), PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f)), collider: false);
        PrimitiveKit.Label("ป้ายรถเมล์\nสาย 8", parent, at + new Vector3(0f, 2.55f, -0.03f), 0.022f, Color.white);
    }
}
