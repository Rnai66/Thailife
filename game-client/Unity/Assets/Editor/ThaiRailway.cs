using UnityEngine;

// ทางรถไฟสายเก่า for a provincial town (no skytrain out here): a metre-gauge track on a raised earth
// embankment (คันดิน) with grey ballast and wooden sleepers, wooden telegraph poles alongside, and a
// traditional Thai station (สถานีรถไฟ) — a cream-and-maroon wooden building under a two-tier red-tiled
// gable roof with gold ช่อฟ้า finials, a long low platform under a canopy on carved wooden posts,
// the white station name board on its two posts, semaphore signals at both ends.
// The train: a diesel locomotive (รถจักรดีเซล) in maroon and cream with a yellow nose, pulling four
// cream-and-red third-class carriages (รถนั่งชั้นสาม) with open windows. It runs along the line,
// stops at the platform (SkyTrain does the moving) and carries on out of town.
// The platform's ticket point is where you take the train to other provinces (TravelStop).
public static class ThaiRailway
{
    private const float Half = 89f;          // the line runs right across the map
    private const float RailTop = 0.62f;     // embankment + ballast + sleeper + rail
    private const float Gauge = 1f;          // metre gauge (ทางกว้าง 1 เมตร)

    public static void Line(Transform root, float z, string stationName)
    {
        var line = PrimitiveKit.Group("Railway_ทางรถไฟ", root, new Vector3(0f, 0f, z)).transform;
        var earth = PrimitiveKit.Mat("EmbankmentEarth", new Color(0.55f, 0.42f, 0.3f));
        var ballast = PrimitiveKit.Mat("Ballast", new Color(0.5f, 0.48f, 0.45f));
        var sleeperMat = PrimitiveKit.Mat("SleeperWood", new Color(0.33f, 0.24f, 0.16f));
        var rail = PrimitiveKit.Mat("RailSteel", new Color(0.55f, 0.52f, 0.5f), 0.7f, 0.8f);
        var grass = PrimitiveKit.Mat("EmbankmentGrass", new Color(0.42f, 0.55f, 0.25f));

        // the embankment: earth, grassy shoulders, ballast on top
        PrimitiveKit.Box("Embankment", line, new Vector3(0f, 0.12f, 0f), new Vector3(Half * 2f, 0.24f, 5.2f), earth);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Shoulder", line, new Vector3(0f, 0.08f, s * 3.2f), new Vector3(Half * 2f, 0.16f, 1.4f), grass, new Vector3(s * 6f, 0f, 0f), collider: false);
        PrimitiveKit.Box("BallastBed", line, new Vector3(0f, 0.32f, 0f), new Vector3(Half * 2f, 0.18f, 2.9f), ballast);

        var track = PrimitiveKit.Group("Track", line, Vector3.zero).transform;
        for (float x = -Half + 0.3f; x < Half; x += 0.62f)
            PrimitiveKit.Box("Sleeper", track, new Vector3(x, 0.45f, 0f), new Vector3(0.22f, 0.1f, 2f), sleeperMat, collider: false);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Rail", track, new Vector3(0f, 0.55f, s * Gauge * 0.5f), new Vector3(Half * 2f, 0.12f, 0.07f), rail, collider: false);
            PrimitiveKit.Box("RailFoot", track, new Vector3(0f, 0.505f, s * Gauge * 0.5f), new Vector3(Half * 2f, 0.03f, 0.16f), rail, collider: false);
        }
        MeshKit.MergeByMaterial(track, "Railway/Track");

        // wooden telegraph poles along the far side, the wires sagging between them
        var pole = PrimitiveKit.Mat("TelegraphPole", new Color(0.36f, 0.27f, 0.18f));
        var wire = PrimitiveKit.Mat("WireBlack", new Color(0.08f, 0.08f, 0.08f));
        var poles = PrimitiveKit.Group("TelegraphPoles", line, Vector3.zero).transform;
        for (float x = -Half + 4f; x < Half; x += 16f)
        {
            PrimitiveKit.Cylinder("Pole", poles, new Vector3(x, 3f, 4.2f), new Vector3(0.18f, 3f, 0.18f), pole, collider: false);
            PrimitiveKit.Box("CrossArm", poles, new Vector3(x, 5.6f, 4.2f), new Vector3(0.1f, 0.1f, 1.4f), pole, collider: false);
            if (x + 16f < Half)
                foreach (float s in new[] { -0.55f, 0.55f })
                    PrimitiveKit.Box("Wire", poles, new Vector3(x + 8f, 5.55f, 4.2f + s), new Vector3(16f, 0.02f, 0.02f), wire, collider: false);
        }
        MeshKit.MergeByMaterial(poles, "Railway/Poles");

        Station(line, stationName);
        Scenery(line);

        // the train, which stops with its carriages along the platform
        var train = PrimitiveKit.Group("Train_รถไฟ", line, new Vector3(-Half - 6f, RailTop, 0f)).transform;
        float x0 = 0f;
        Locomotive(train, x0);
        x0 -= 13.6f;
        for (int i = 0; i < 4; i++)
        {
            Carriage(train, x0 - 7.2f, i);
            x0 -= 15f;
        }
        MeshKit.MergeByMaterial(train, "Railway/Train");
        train.gameObject.AddComponent<SkyTrain>().Configure(-Half - 6f, Half + 76f, 9f, 36f);
    }

    // ── The station ──────────────────────────────────────────────────────────────
    private static void Station(Transform line, string stationName)
    {
        var cream = PrimitiveKit.Mat("StationCream", new Color(0.96f, 0.9f, 0.74f));
        var maroon = PrimitiveKit.Mat("StationMaroon", new Color(0.5f, 0.12f, 0.1f), 0.4f);
        var wood = PrimitiveKit.Mat("StationWood", new Color(0.45f, 0.28f, 0.16f), 0.3f);
        var tile = PrimitiveKit.Mat("RoofTileRed", new Color(0.72f, 0.24f, 0.12f), 0.35f);
        var gold = PrimitiveKit.Mat("ThaiGoldTrim", new Color(0.92f, 0.72f, 0.28f), 0.7f, 0.8f);
        var concrete = PrimitiveKit.Mat("PlatformConcrete", new Color(0.74f, 0.72f, 0.68f));
        var yellow = PrimitiveKit.Mat("SafetyYellow", new Color(0.98f, 0.82f, 0.1f));
        var dark = PrimitiveKit.Mat("WindowDark", new Color(0.12f, 0.1f, 0.09f));
        var pave = PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f));
        var st = PrimitiveKit.Group("Station_สถานีรถไฟ", line, Vector3.zero).transform;

        // the platform: long and low (Thai style), yellow edge line, a ramp down to the forecourt
        const float pz = -3.3f, pl = 64f, ph = 0.55f;
        PrimitiveKit.Box("Platform", st, new Vector3(0f, ph * 0.5f, pz), new Vector3(pl, ph, 3.2f), concrete);
        PrimitiveKit.Box("PlatformEdge", st, new Vector3(0f, ph + 0.005f, pz + 1.35f), new Vector3(pl, 0.01f, 0.3f), yellow, collider: false);
        foreach (float rx in new[] { 6f, -28f })
            PrimitiveKit.Box("Ramp", st, new Vector3(rx, ph * 0.5f - 0.05f, pz - 1.6f - 1.45f), new Vector3(3f, 0.2f, 3.05f), concrete, new Vector3(-10.5f, 0f, 0f));
        PrimitiveKit.Box("Forecourt", st, new Vector3(-6f, 0.015f, -12f), new Vector3(40f, 0.02f, 9f), pave, collider: false);

        // the canopy over the platform: turned wooden posts with carved brackets, a red tin roof, lace fascia
        for (float x = -pl * 0.5f + 2f; x <= pl * 0.5f - 2f; x += 4f)
        {
            PrimitiveKit.Box("CanopyPost", st, new Vector3(x, ph + 1.5f, pz - 0.6f), new Vector3(0.16f, 3f, 0.16f), wood, collider: false);
            PrimitiveKit.Box("Bracket", st, new Vector3(x, ph + 2.75f, pz + 0.05f), new Vector3(0.08f, 0.5f, 1.2f), maroon, new Vector3(-35f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("CanopyRoof", st, new Vector3(0f, ph + 3.1f, pz - 0.3f), new Vector3(pl - 2f, 0.08f, 3.8f), tile, new Vector3(-6f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Fascia", st, new Vector3(0f, ph + 2.95f, pz + 1.55f), new Vector3(pl - 2f, 0.28f, 0.05f), cream, collider: false);
        for (float x = -pl * 0.5f + 1.5f; x < pl * 0.5f - 1f; x += 0.5f) // ระบาย: a row of little points under the fascia
            PrimitiveKit.Box("Lace", st, new Vector3(x, ph + 2.76f, pz + 1.56f), new Vector3(0.16f, 0.16f, 0.03f), maroon, new Vector3(0f, 0f, 45f), collider: false);
        foreach (float x in new[] { -20f, -12f, 12f, 22f }) // benches and lamps under the canopy
        {
            PrimitiveKit.Box("Bench", st, new Vector3(x, ph + 0.45f, pz - 1.1f), new Vector3(2.2f, 0.08f, 0.5f), wood, collider: false);
            PrimitiveKit.Box("BenchBack", st, new Vector3(x, ph + 0.75f, pz - 1.35f), new Vector3(2.2f, 0.5f, 0.06f), wood, collider: false);
            PrimitiveKit.Box("Lamp", st, new Vector3(x + 2f, ph + 2.85f, pz + 0.2f), new Vector3(0.3f, 0.25f, 0.3f), PrimitiveKit.Glow("StationLamp", new Color(1f, 0.85f, 0.55f), 1.4f), collider: false);
        }

        // the station building behind the platform: cream boards, maroon pilasters, tall louvred windows
        var b = PrimitiveKit.Group("StationHouse", st, new Vector3(-14f, 0f, -8f)).transform;
        const float bw = 16f, bd = 5.6f, bh = 3.6f;
        PrimitiveKit.Box("Plinth", b, new Vector3(0f, 0.25f, 0f), new Vector3(bw + 0.6f, 0.5f, bd + 0.6f), maroon);
        PrimitiveKit.Box("Walls", b, new Vector3(0f, 0.5f + bh * 0.5f, 0f), new Vector3(bw, bh, bd), cream);
        for (float x = -bw * 0.5f; x <= bw * 0.5f + 0.01f; x += bw / 6f)
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Pilaster", b, new Vector3(x, 0.5f + bh * 0.5f, s * (bd * 0.5f + 0.04f)), new Vector3(0.25f, bh, 0.08f), maroon, collider: false);
        for (int i = 0; i < 6; i++)
        {
            float x = -bw * 0.5f + bw / 12f + i * bw / 6f;
            foreach (float s in new[] { -1f, 1f })
            {
                bool door = i == 2 || i == 3;
                float h = door ? 2.6f : 1.8f, y = door ? 0.5f + h * 0.5f : 1.5f + h * 0.5f;
                PrimitiveKit.Box(door ? "Door" : "Window", b, new Vector3(x, y, s * (bd * 0.5f + 0.03f)), new Vector3(1.3f, h, 0.05f), door ? wood : dark, collider: false);
                PrimitiveKit.Box("ArchTop", b, new Vector3(x, y + h * 0.5f + 0.12f, s * (bd * 0.5f + 0.05f)), new Vector3(1.5f, 0.22f, 0.05f), maroon, collider: false);
                if (!door)
                    for (float ly = y - h * 0.5f + 0.15f; ly < y + h * 0.5f; ly += 0.22f) // louvres
                        PrimitiveKit.Box("Louvre", b, new Vector3(x, ly, s * (bd * 0.5f + 0.06f)), new Vector3(1.2f, 0.05f, 0.04f), wood, collider: false);
            }
        }
        // two-tier Thai roof: the lower one broad with deep eaves, a smaller one stacked on top, gable
        // fronts in maroon edged in gold, ช่อฟ้า finials curling up at the ridge ends
        float roofY = 0.5f + bh;
        MeshKit.Place("RoofLower", b, MeshKit.Gable("Railway/StationRoofLow", bd + 2.2f, 1.7f, 1f), tile, new Vector3(-bw * 0.5f - 0.9f, roofY, 0f), new Vector3(0f, 90f, 0f), new Vector3(1f, 1f, bw + 1.8f));
        MeshKit.Place("RoofUpper", b, MeshKit.Gable("Railway/StationRoofHigh", bd * 0.75f, 1.6f, 1f), tile, new Vector3(-bw * 0.32f, roofY + 1.15f, 0f), new Vector3(0f, 90f, 0f), new Vector3(1f, 1f, bw * 0.64f));
        foreach (float s in new[] { -1f, 1f })
        {
            MeshKit.Place("GableFront", b, MeshKit.Gable("Railway/StationGable", bd * 0.72f, 1.5f, 1f), maroon, new Vector3(s * (bw * 0.32f + 0.02f), roofY + 1.17f, 0f), new Vector3(0f, s * 90f, 0f), new Vector3(1f, 1f, 0.06f));
            PrimitiveKit.Box("BargeBoard", b, new Vector3(s * (bw * 0.32f + 0.08f), roofY + 1.95f, -bd * 0.19f), new Vector3(0.08f, 0.12f, 2.5f), gold, new Vector3(-31f, 0f, 0f), collider: false);
            PrimitiveKit.Box("BargeBoard", b, new Vector3(s * (bw * 0.32f + 0.08f), roofY + 1.95f, bd * 0.19f), new Vector3(0.08f, 0.12f, 2.5f), gold, new Vector3(31f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Chofa", b, new Vector3(s * (bw * 0.32f + 0.2f), roofY + 3f, 0f), new Vector3(0.5f, 0.12f, 0.12f), gold, new Vector3(0f, 0f, s * -50f), collider: false);
            PrimitiveKit.Box("Chofa", b, new Vector3(s * (bw * 0.5f + 1f), roofY + 1.95f, 0f), new Vector3(0.45f, 0.1f, 0.1f), gold, new Vector3(0f, 0f, s * -50f), collider: false);
        }
        PrimitiveKit.Box("Ridge", b, new Vector3(0f, roofY + 2.77f, 0f), new Vector3(bw * 0.64f, 0.1f, 0.14f), gold, collider: false);
        // the name over the door (the street side) and the clock
        PrimitiveKit.Box("NameBoard", b, new Vector3(0f, roofY - 0.45f, -bd * 0.5f - 0.08f), new Vector3(6.4f, 0.6f, 0.06f), maroon, collider: false);
        PrimitiveKit.Label(stationName, b, new Vector3(0f, roofY - 0.45f, -bd * 0.5f - 0.13f), 0.045f, new Color(1f, 0.88f, 0.5f));
        PrimitiveKit.Cylinder("Clock", b, new Vector3(0f, roofY + 0.75f, -bd * 0.5f - 0.75f), new Vector3(0.7f, 0.04f, 0.7f), PrimitiveKit.Mat("ClockFace", Color.white), new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Box("ClockHand", b, new Vector3(0f, roofY + 0.85f, -bd * 0.5f - 0.8f), new Vector3(0.04f, 0.24f, 0.02f), dark, collider: false);
        PrimitiveKit.Box("ClockHand", b, new Vector3(0.08f, roofY + 0.75f, -bd * 0.5f - 0.8f), new Vector3(0.18f, 0.04f, 0.02f), dark, collider: false);

        // the white name boards at both ends of the platform (SRT style: black letters, red edges)
        foreach (float x in new[] { -pl * 0.5f + 3f, pl * 0.5f - 3f })
        {
            foreach (float s in new[] { -1.3f, 1.3f })
                PrimitiveKit.Box("BoardPost", st, new Vector3(x + s, ph + 1.1f, pz - 0.9f), new Vector3(0.1f, 2.2f, 0.1f), dark, collider: false);
            PrimitiveKit.Box("BoardEdge", st, new Vector3(x, ph + 2f, pz - 0.9f), new Vector3(3.1f, 0.9f, 0.05f), PrimitiveKit.Mat("SignRed", new Color(0.75f, 0.12f, 0.1f)), collider: false);
            PrimitiveKit.Box("Board", st, new Vector3(x, ph + 2f, pz - 0.93f), new Vector3(2.9f, 0.72f, 0.05f), PrimitiveKit.Mat("SignWhite", Color.white), collider: false);
            PrimitiveKit.Label(stationName, st, new Vector3(x, ph + 2f, pz - 0.97f), 0.035f, new Color(0.05f, 0.05f, 0.05f));
            PrimitiveKit.Label(stationName, st, new Vector3(x, ph + 2f, pz - 0.89f), 0.035f, new Color(0.05f, 0.05f, 0.05f), 180f);
        }
        // semaphore signals beyond the ends (red-and-white arms)
        foreach (float s in new[] { -1f, 1f })
        {
            var sig = PrimitiveKit.Group("Signal", st, new Vector3(s * (pl * 0.5f + 10f), 0.24f, -2.3f)).transform;
            PrimitiveKit.Box("Mast", sig, new Vector3(0f, 3f, 0f), new Vector3(0.18f, 6f, 0.18f), PrimitiveKit.Mat("SignalGrey", new Color(0.3f, 0.3f, 0.32f)), collider: false);
            PrimitiveKit.Box("Arm", sig, new Vector3(-s * 0.7f, 5.4f, -0.12f), new Vector3(1.4f, 0.26f, 0.05f), PrimitiveKit.Mat("SignRed", new Color(0.75f, 0.12f, 0.1f)), new Vector3(0f, 0f, s * 20f), collider: false);
            PrimitiveKit.Box("ArmStripe", sig, new Vector3(-s * 0.95f, 5.5f, -0.15f), new Vector3(0.2f, 0.27f, 0.05f), PrimitiveKit.Mat("SignWhite", Color.white), new Vector3(0f, 0f, s * 20f), collider: false);
            PrimitiveKit.Sphere("Lamp", sig, new Vector3(0f, 5.4f, -0.15f), Vector3.one * 0.2f, PrimitiveKit.Glow("SignalGreen", new Color(0.2f, 1f, 0.4f), 1.5f));
        }
        // the station bell (ระฆังสถานี) on its post by the door
        PrimitiveKit.Box("BellPost", st, new Vector3(-8.5f, ph + 1.1f, pz - 1.2f), new Vector3(0.1f, 2.2f, 0.1f), wood, collider: false);
        PrimitiveKit.Cylinder("Bell", st, new Vector3(-8.5f, ph + 2f, pz - 1f), new Vector3(0.3f, 0.18f, 0.3f), gold, collider: false);
        MeshKit.MergeByMaterial(st, "Railway/Station");

        // the ticket point: take the train to another province from the platform; arrivals step off here
        var gate = new GameObject("TrainGate_ไปต่างจังหวัด");
        gate.transform.SetParent(line, false);
        gate.transform.localPosition = new Vector3(0f, ph + 1f, pz);
        var zone = gate.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(3f, 2f, 3f);
        gate.AddComponent<TravelStop>().Configure("สถานีรถไฟ · ไปต่างจังหวัด", longDistance: TravelMode.Train);
        PrimitiveKit.Box("TicketWindow", line, new Vector3(0f, ph + 1f, pz - 1.45f), new Vector3(1.4f, 1.6f, 0.3f), maroon);
        PrimitiveKit.Label("ขายตั๋ว · ไปต่างจังหวัด", line, new Vector3(0f, ph + 2.05f, pz - 1.29f), 0.03f, new Color(1f, 0.9f, 0.6f), 180f);
        PrimitiveKit.Group("SpawnPoint_Train", line, new Vector3(-3.5f, ph + 0.05f, pz + 0.4f), 90f).AddComponent<SpawnPoint>().Configure(TravelMode.Train);
    }

    // ── Out beyond the line: rice fields and palms ───────────────────────────────
    private static void Scenery(Transform line)
    {
        var paddy = PrimitiveKit.Mat("PaddyGreen", new Color(0.45f, 0.65f, 0.25f));
        var dyke = PrimitiveKit.Mat("PaddyDyke", new Color(0.52f, 0.42f, 0.28f));
        for (float x = -84f; x < 84f; x += 24f)
        {
            PrimitiveKit.Box("Paddy", line, new Vector3(x + 12f, 0.02f, 13f), new Vector3(23f, 0.03f, 13f), paddy, collider: false);
            PrimitiveKit.Box("Dyke", line, new Vector3(x, 0.08f, 13f), new Vector3(0.6f, 0.16f, 13f), dyke, collider: false);
        }
        int seed = 3;
        for (float x = -80f; x < 84f; x += 13f)
            Nature.CoconutPalm(line, new Vector3(x + (seed % 3) * 2f, 0f, 7.5f + (seed % 2) * 12f), 0.9f + (seed % 3) * 0.1f, seed++);
    }

    // ── The train ────────────────────────────────────────────────────────────────
    private static Material maroonPaint, creamPaint, yellowPaint, under, glass, wheel, roof;
    private static void Paints()
    {
        maroonPaint = PrimitiveKit.Mat("SrtMaroon", new Color(0.55f, 0.1f, 0.1f), 0.6f, 0.2f);
        creamPaint = PrimitiveKit.Mat("SrtCream", new Color(0.96f, 0.9f, 0.75f), 0.6f, 0.1f);
        yellowPaint = PrimitiveKit.Mat("SrtYellow", new Color(0.98f, 0.75f, 0.1f), 0.6f, 0.1f);
        under = PrimitiveKit.Mat("SrtUnderframe", new Color(0.12f, 0.12f, 0.13f), 0.3f, 0.4f);
        glass = PrimitiveKit.Mat("SrtWindow", new Color(0.08f, 0.09f, 0.1f), 0.8f);
        wheel = PrimitiveKit.Mat("SrtWheel", new Color(0.2f, 0.2f, 0.21f), 0.5f, 0.8f);
        roof = PrimitiveKit.Mat("SrtRoof", new Color(0.55f, 0.55f, 0.56f), 0.4f, 0.3f);
    }

    // a bogie: frame, springs and two axles of wheels on the rails
    private static void Bogie(Transform car, float x)
    {
        PrimitiveKit.Box("BogieFrame", car, new Vector3(x, 0.45f, 0f), new Vector3(2.4f, 0.3f, 1.3f), under, collider: false);
        foreach (float ax in new[] { -0.8f, 0.8f })
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Cylinder("Wheel", car, new Vector3(x + ax, 0.4f, s * Gauge * 0.5f), new Vector3(0.8f, 0.06f, 0.8f), wheel, new Vector3(90f, 0f, 0f), collider: false);
    }

    // รถจักรดีเซล: a long hood with the cab at the front, maroon below and cream above with a yellow
    // band, a yellow nose with red-and-white stripes, big headlamps, horns, exhaust stacks on the hood
    private static void Locomotive(Transform train, float x)
    {
        if (maroonPaint == null) Paints();
        var c = PrimitiveKit.Group("Locomotive_รถจักร", train, new Vector3(x - 6.3f, 0f, 0f)).transform;
        const float L = 12.6f;
        PrimitiveKit.Box("Frame", c, new Vector3(0f, 0.8f, 0f), new Vector3(L, 0.35f, 2.75f), under, collider: false);
        Bogie(c, -L * 0.3f);
        Bogie(c, L * 0.3f);
        PrimitiveKit.Box("FuelTank", c, new Vector3(0f, 0.55f, 0f), new Vector3(4f, 0.6f, 2.2f), under, collider: false);
        // the hood behind the cab, narrower, with a walkway each side
        PrimitiveKit.Box("Hood", c, new Vector3(-1.4f, 1.95f, 0f), new Vector3(L - 3.4f, 1.95f, 2.1f), maroonPaint, collider: false);
        PrimitiveKit.Box("HoodBand", c, new Vector3(-1.4f, 2.3f, 0f), new Vector3(L - 3.4f, 0.3f, 2.12f), yellowPaint, collider: false);
        PrimitiveKit.Box("HoodTop", c, new Vector3(-1.4f, 3f, 0f), new Vector3(L - 3.4f, 0.14f, 2f), creamPaint, collider: false);
        for (float gx = -5f; gx < 2f; gx += 1.2f) // louvred grilles along the hood
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Grille", c, new Vector3(gx, 1.7f, s * 1.06f), new Vector3(0.8f, 0.6f, 0.02f), under, collider: false);
        foreach (float ex in new[] { -3.5f, -1.5f })
            PrimitiveKit.Cylinder("Exhaust", c, new Vector3(ex, 3.2f, 0f), new Vector3(0.3f, 0.15f, 0.3f), under, collider: false);
        // the cab: full width, windows all round, a curved roof
        float cx = L * 0.5f - 1.6f;
        PrimitiveKit.Box("Cab", c, new Vector3(cx, 2.15f, 0f), new Vector3(2.6f, 2.4f, 2.75f), maroonPaint, collider: false);
        PrimitiveKit.Box("CabUpper", c, new Vector3(cx, 2.85f, 0f), new Vector3(2.62f, 1f, 2.77f), creamPaint, collider: false);
        PrimitiveKit.Box("CabBand", c, new Vector3(cx, 2.3f, 0f), new Vector3(2.64f, 0.3f, 2.79f), yellowPaint, collider: false);
        PrimitiveKit.Cylinder("CabRoof", c, new Vector3(cx, 3.35f, 0f), new Vector3(0.4f, 1.3f, 2.75f), roof, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Windscreen", c, new Vector3(cx + 1.32f, 2.9f, 0f), new Vector3(0.04f, 0.75f, 2.3f), glass, collider: false);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("CabWindow", c, new Vector3(cx - 0.2f, 2.9f, s * 1.39f), new Vector3(1.4f, 0.7f, 0.03f), glass, collider: false);
        // the nose: yellow, with red-and-white warning stripes, headlights, the cowcatcher
        PrimitiveKit.Box("Nose", c, new Vector3(L * 0.5f + 0.05f, 1.5f, 0f), new Vector3(0.2f, 1.1f, 2.7f), yellowPaint, collider: false);
        for (int i = -3; i <= 3; i++)
            PrimitiveKit.Box("Stripe", c, new Vector3(L * 0.5f + 0.17f, 1.4f, i * 0.36f), new Vector3(0.03f, 0.9f, 0.16f),
                i % 2 == 0 ? maroonPaint : creamPaint, new Vector3(45f, 0f, 0f), collider: false);
        foreach (float s in new[] { -0.9f, 0.9f })
            PrimitiveKit.Cylinder("Headlight", c, new Vector3(L * 0.5f + 0.18f, 1.95f, s), new Vector3(0.3f, 0.04f, 0.3f), PrimitiveKit.Glow("TrainHeadlight", new Color(1f, 0.95f, 0.8f), 2f), new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Cylinder("TopLight", c, new Vector3(cx + 1.3f, 3.45f, 0f), new Vector3(0.28f, 0.04f, 0.28f), PrimitiveKit.Glow("TrainHeadlight", new Color(1f, 0.95f, 0.8f), 2f), new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Cowcatcher", c, new Vector3(L * 0.5f + 0.25f, 0.45f, 0f), new Vector3(0.5f, 0.5f, 2.4f), under, new Vector3(0f, 0f, 30f), collider: false);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Horn", c, new Vector3(cx + 0.6f, 3.55f, s * 0.35f), new Vector3(0.1f, 0.25f, 0.1f), under, new Vector3(0f, 0f, -90f), collider: false);
        PrimitiveKit.Label("รฟท.", c, new Vector3(cx, 2.3f, -1.41f), 0.03f, new Color(0.2f, 0.05f, 0f));
    }

    // รถนั่งชั้นสาม: a long steel carriage, maroon below and cream round the windows, open windows
    // with their shutters half up, a grey arched roof with ventilators, open end platforms with steps
    private static void Carriage(Transform train, float x, int index)
    {
        if (maroonPaint == null) Paints();
        var c = PrimitiveKit.Group("Carriage_ตู้โดยสาร", train, new Vector3(x, 0f, 0f)).transform;
        const float L = 14.4f;
        PrimitiveKit.Box("Frame", c, new Vector3(0f, 0.8f, 0f), new Vector3(L, 0.3f, 2.6f), under, collider: false);
        Bogie(c, -L * 0.34f);
        Bogie(c, L * 0.34f);
        PrimitiveKit.Box("Lower", c, new Vector3(0f, 1.45f, 0f), new Vector3(L - 1.4f, 1f, 2.8f), maroonPaint, collider: false);
        PrimitiveKit.Box("Upper", c, new Vector3(0f, 2.55f, 0f), new Vector3(L - 1.4f, 1.2f, 2.8f), creamPaint, collider: false);
        PrimitiveKit.Box("Band", c, new Vector3(0f, 1.98f, 0f), new Vector3(L - 1.38f, 0.08f, 2.82f), yellowPaint, collider: false);
        PrimitiveKit.Cylinder("Roof", c, new Vector3(0f, 3.15f, 0f), new Vector3(0.45f, (L - 1.2f) * 0.5f, 2.8f), roof, new Vector3(0f, 0f, 90f), collider: false);
        for (float vx = -L * 0.5f + 2f; vx < L * 0.5f - 1.5f; vx += 2.4f) // roof ventilators
            PrimitiveKit.Box("Vent", c, new Vector3(vx, 3.42f, 0f), new Vector3(0.5f, 0.18f, 0.4f), roof, collider: false);
        for (float wx = -L * 0.5f + 1.6f; wx < L * 0.5f - 1.2f; wx += 1.1f) // open windows, shutters half up
            foreach (float s in new[] { -1f, 1f })
            {
                PrimitiveKit.Box("Window", c, new Vector3(wx, 2.5f, s * 1.41f), new Vector3(0.75f, 0.7f, 0.02f), glass, collider: false);
                PrimitiveKit.Box("Shutter", c, new Vector3(wx, 2.82f, s * 1.42f), new Vector3(0.75f, 0.12f, 0.02f), maroonPaint, collider: false);
            }
        // the end platforms: steps down each side, a handrail, the gangway between the cars
        foreach (float e in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("EndWall", c, new Vector3(e * (L * 0.5f - 0.72f), 2f, 0f), new Vector3(0.06f, 2.3f, 2.8f), creamPaint, collider: false);
            PrimitiveKit.Box("EndDoor", c, new Vector3(e * (L * 0.5f - 0.68f), 1.9f, 0f), new Vector3(0.04f, 1.9f, 0.8f), maroonPaint, collider: false);
            PrimitiveKit.Box("EndFloor", c, new Vector3(e * (L * 0.5f - 0.35f), 0.98f, 0f), new Vector3(0.7f, 0.06f, 2.4f), under, collider: false);
            PrimitiveKit.Box("EndRoof", c, new Vector3(e * (L * 0.5f - 0.35f), 3.2f, 0f), new Vector3(0.75f, 0.06f, 2.6f), roof, collider: false);
            foreach (float s in new[] { -1f, 1f })
            {
                PrimitiveKit.Box("Step", c, new Vector3(e * (L * 0.5f - 0.35f), 0.62f, s * 1.25f), new Vector3(0.6f, 0.05f, 0.3f), under, collider: false);
                PrimitiveKit.Box("Handrail", c, new Vector3(e * (L * 0.5f - 0.05f), 1.7f, s * 1.2f), new Vector3(0.04f, 1.3f, 0.04f), wheel, collider: false);
            }
            PrimitiveKit.Box("Gangway", c, new Vector3(e * (L * 0.5f + 0.2f), 2f, 0f), new Vector3(0.35f, 2f, 1f), under, collider: false);
        }
        PrimitiveKit.Label($"ชั้น 3 · บชส.{index + 1}", c, new Vector3(0f, 1.45f, -1.42f), 0.025f, new Color(1f, 0.9f, 0.6f));
    }
}
