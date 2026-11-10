using UnityEngine;

// เครื่องบินและสนามบิน: the airliners (a twin-engine narrow-body, nose along local +Z, wheels on the
// ground at the origin) in the colours of the made-up Thai airlines below, the ground vehicles that
// work round them, and the airfield beside a town's airport: the runway with its markings and lights,
// the taxiway, a hangar, a windsock — and AirTraffic, which flies the planes in and out of it.
public static class Aviation
{
    public struct Livery
    {
        public string name;
        public Color body, belly, stripe, accent, fin;
    }

    public static readonly Livery[] Airlines =
    {
        new Livery { name = "ไทยไลฟ์แอร์", body = new Color(0.96f, 0.96f, 0.97f), belly = new Color(0.86f, 0.86f, 0.88f), stripe = new Color(0.38f, 0.14f, 0.5f), accent = new Color(0.95f, 0.75f, 0.2f), fin = new Color(0.38f, 0.14f, 0.5f) },
        new Livery { name = "สยามสกาย", body = new Color(0.97f, 0.97f, 0.98f), belly = new Color(0.12f, 0.3f, 0.62f), stripe = new Color(0.12f, 0.3f, 0.62f), accent = new Color(0.35f, 0.7f, 0.95f), fin = new Color(0.12f, 0.3f, 0.62f) },
        new Livery { name = "บางกอกเจ็ท", body = new Color(0.97f, 0.97f, 0.97f), belly = new Color(0.9f, 0.9f, 0.9f), stripe = new Color(0.85f, 0.12f, 0.15f), accent = new Color(0.2f, 0.2f, 0.22f), fin = new Color(0.85f, 0.12f, 0.15f) },
        new Livery { name = "อันดามันแอร์", body = new Color(0.96f, 0.97f, 0.97f), belly = new Color(0.05f, 0.55f, 0.6f), stripe = new Color(0.05f, 0.55f, 0.6f), accent = new Color(1f, 0.55f, 0.15f), fin = new Color(1f, 0.55f, 0.15f) },
        new Livery { name = "ล้านนาแอร์ไลน์", body = new Color(0.96f, 0.96f, 0.95f), belly = new Color(0.16f, 0.45f, 0.25f), stripe = new Color(0.16f, 0.45f, 0.25f), accent = new Color(0.92f, 0.76f, 0.3f), fin = new Color(0.16f, 0.45f, 0.25f) },
        new Livery { name = "อีสานบิน", body = new Color(1f, 0.85f, 0.12f), belly = new Color(0.95f, 0.95f, 0.95f), stripe = new Color(0.25f, 0.2f, 0.18f), accent = new Color(0.9f, 0.3f, 0.1f), fin = new Color(0.25f, 0.2f, 0.18f) },
    };

    private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    private static Material M(Color c, float smooth = 0.75f, float metal = 0.15f) => PrimitiveKit.Mat("Jet" + Hex(c), c, smooth, metal);

    // ── The airliner ─────────────────────────────────────────────────────────────────────────────
    // ~36 m long, 31 m span. Body parts are merged by material; the wheels stay a separate "Gear" group
    // (AirTraffic folds it away after take-off).
    public static Transform Jet(Transform parent, Vector3 at, float yaw, Livery l, string key)
    {
        var root = PrimitiveKit.Group("Jet_" + l.name, parent, at, yaw).transform;
        var b = PrimitiveKit.Group("Body", root, Vector3.zero).transform;
        Material body = M(l.body), belly = M(l.belly), stripe = M(l.stripe), accent = M(l.accent), fin = M(l.fin);
        var glass = PrimitiveKit.Mat("JetWindow", new Color(0.08f, 0.1f, 0.14f), 0.95f, 0.3f);
        var metal = PrimitiveKit.Mat("JetMetal", new Color(0.72f, 0.74f, 0.78f), 0.8f, 0.8f);
        var dark = PrimitiveKit.Mat("JetIntake", new Color(0.06f, 0.06f, 0.07f), 0.4f, 0.5f);
        const float y = 4.4f, r = 2f, len = 27f;
        var along = new Vector3(90f, 0f, 0f); // cylinders lie along Z

        // fuselage: the tube, a belly half in the second colour, the nose and the up-swept tail cone
        PrimitiveKit.Cylinder("Fuselage", b, new Vector3(0f, y, 0f), new Vector3(r * 2f, len / 2f, r * 2f), body, along, collider: false);
        PrimitiveKit.Cylinder("Belly", b, new Vector3(0f, y - 0.55f, 0f), new Vector3(r * 2f - 0.06f, len / 2f - 0.2f, r * 1.75f), belly, along, collider: false);
        PrimitiveKit.Sphere("Nose", b, new Vector3(0f, y - 0.1f, len / 2f), new Vector3(r * 2f, r * 1.95f, 7.5f), body);
        PrimitiveKit.Sphere("NoseBelly", b, new Vector3(0f, y - 0.6f, len / 2f - 0.2f), new Vector3(r * 1.96f, r * 1.4f, 7f), belly);
        PrimitiveKit.Sphere("TailCone", b, new Vector3(0f, y + 0.55f, -len / 2f - 0.5f), new Vector3(r * 1.9f, r * 1.6f, 10f), body);
        // cockpit windows and the row of cabin windows along both sides, the doors
        PrimitiveKit.Box("Cockpit", b, new Vector3(0f, y + 0.85f, len / 2f + 2.4f), new Vector3(2.6f, 0.5f, 1.4f), glass, new Vector3(-24f, 0f, 0f), collider: false);
        foreach (float s in new[] { -1f, 1f })
        {
            for (float z = -len / 2f + 2f; z < len / 2f - 1f; z += 0.9f)
                PrimitiveKit.Box("Window", b, new Vector3(s * (r - 0.02f), y + 0.55f, z), new Vector3(0.08f, 0.32f, 0.26f), glass, collider: false);
            foreach (float z in new[] { len / 2f - 1.6f, -len / 2f + 1f })
                PrimitiveKit.Box("Door", b, new Vector3(s * (r - 0.01f), y + 0.15f, z), new Vector3(0.06f, 1.9f, 0.9f), M(l.belly * 0.9f + Color.white * 0.1f), collider: false);
            // the cheatline and its thin accent under it
            PrimitiveKit.Box("Cheatline", b, new Vector3(s * (r - 0.03f), y + 0.05f, 0.5f), new Vector3(0.08f, 0.32f, len - 1f), stripe, collider: false);
            PrimitiveKit.Box("Accent", b, new Vector3(s * (r - 0.03f), y - 0.22f, 0.5f), new Vector3(0.08f, 0.1f, len - 1f), accent, collider: false);
            PrimitiveKit.Label(l.name, b, new Vector3(s * (r + 0.02f), y + 1.25f, 3.5f), 0.11f, l.stripe, s > 0 ? -90f : 90f);
        }

        // wings: swept back 25°, a touch of dihedral, winglets in the fin colour; engines hung under them
        foreach (float s in new[] { -1f, 1f })
        {
            const float span = 14f, sweep = 25f;
            float cx = s * (r + span * 0.5f * Mathf.Cos(sweep * Mathf.Deg2Rad)), cz = 0.5f - span * 0.5f * Mathf.Sin(sweep * Mathf.Deg2Rad);
            PrimitiveKit.Box("Wing", b, new Vector3(cx, y - 1.1f, cz), new Vector3(span, 0.42f, 4.6f), body, new Vector3(0f, s * sweep, s * 5f), collider: false);
            PrimitiveKit.Box("WingRoot", b, new Vector3(s * (r - 0.2f), y - 1.2f, 0.8f), new Vector3(1.4f, 0.6f, 6.5f), belly, collider: false);
            float tx = s * (r + span * Mathf.Cos(sweep * Mathf.Deg2Rad)), tz = 0.5f - span * Mathf.Sin(sweep * Mathf.Deg2Rad);
            PrimitiveKit.Box("Winglet", b, new Vector3(tx, y + 0.2f, tz - 0.4f), new Vector3(0.22f, 2.3f, 1.6f), fin, new Vector3(-20f, 0f, s * 8f), collider: false);
            PrimitiveKit.Sphere(s < 0 ? "NavRed" : "NavGreen", b, new Vector3(tx, y - 0.5f, tz), Vector3.one * 0.35f,
                PrimitiveKit.Glow(s < 0 ? "JetNavRed" : "JetNavGreen", s < 0 ? new Color(1f, 0.1f, 0.1f) : new Color(0.1f, 1f, 0.3f), 3f));
            var ex = s * 5.6f;
            PrimitiveKit.Box("Pylon", b, new Vector3(ex, y - 1.7f, 2.2f), new Vector3(0.35f, 0.9f, 3.2f), body, collider: false);
            PrimitiveKit.Cylinder("Engine", b, new Vector3(ex, y - 2.5f, 2.6f), new Vector3(2.1f, 2.1f, 2.1f), accent == stripe ? body : M(l.body), along, collider: false);
            PrimitiveKit.Cylinder("EngineLip", b, new Vector3(ex, y - 2.5f, 4.7f), new Vector3(2.15f, 0.08f, 2.15f), metal, along, collider: false);
            PrimitiveKit.Cylinder("Intake", b, new Vector3(ex, y - 2.5f, 4.74f), new Vector3(1.8f, 0.06f, 1.8f), dark, along, collider: false);
            PrimitiveKit.Cylinder("Exhaust", b, new Vector3(ex, y - 2.5f, 0.3f), new Vector3(1.3f, 0.4f, 1.3f), metal, along, collider: false);
            PrimitiveKit.Box("EngineStripe", b, new Vector3(ex, y - 2.5f, 2.9f), new Vector3(2.16f, 0.25f, 1.2f), stripe, collider: false);
            // tailplane
            PrimitiveKit.Box("Tailplane", b, new Vector3(s * 3.4f, y + 0.9f, -len / 2f - 2.8f), new Vector3(5.6f, 0.3f, 2.6f), body, new Vector3(0f, s * 30f, s * 4f), collider: false);
        }

        // the fin, swept, in the airline's colour with its round emblem on both sides
        PrimitiveKit.Box("Fin", b, new Vector3(0f, y + 4.6f, -len / 2f - 2.6f), new Vector3(0.38f, 7f, 4.6f), fin, new Vector3(-28f, 0f, 0f), collider: false);
        PrimitiveKit.Box("FinRoot", b, new Vector3(0f, y + 1.8f, -len / 2f - 1f), new Vector3(0.5f, 1.4f, 4f), fin, collider: false);
        PrimitiveKit.Cylinder("Emblem", b, new Vector3(0f, y + 5.1f, -len / 2f - 3.2f), new Vector3(2.2f, 0.22f, 2.2f), accent, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Cylinder("EmblemCore", b, new Vector3(0f, y + 5.1f, -len / 2f - 3.2f), new Vector3(1.2f, 0.24f, 1.2f), body, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Sphere("TailLight", b, new Vector3(0f, y + 0.6f, -len / 2f - 5.4f), Vector3.one * 0.3f, PrimitiveKit.Glow("JetNavWhite", Color.white, 3f));
        PrimitiveKit.Sphere("Beacon", b, new Vector3(0f, y + r + 0.05f, 0f), Vector3.one * 0.35f, PrimitiveKit.Glow("BeaconRed", new Color(1f, 0.15f, 0.1f), 2f));

        // the wheels: nose gear and two main legs, two wheels each
        var gear = PrimitiveKit.Group("Gear", root, Vector3.zero).transform;
        var tyre = PrimitiveKit.Mat("JetTyre", new Color(0.08f, 0.08f, 0.09f), 0.2f);
        var axle = new Vector3(0f, 0f, 90f);
        PrimitiveKit.Cylinder("NoseLeg", gear, new Vector3(0f, 1.25f, len / 2f - 1f), new Vector3(0.22f, 1.05f, 0.22f), metal, collider: false);
        foreach (float s in new[] { -0.28f, 0.28f })
            PrimitiveKit.Cylinder("NoseWheel", gear, new Vector3(s, 0.42f, len / 2f - 1f), new Vector3(0.84f, 0.14f, 0.84f), tyre, axle, collider: false);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Cylinder("MainLeg", gear, new Vector3(s * 2.6f, 1.6f, -0.6f), new Vector3(0.32f, 1.25f, 0.32f), metal, collider: false);
            foreach (float w in new[] { -0.4f, 0.4f })
                PrimitiveKit.Cylinder("MainWheel", gear, new Vector3(s * 2.6f + w, 0.55f, -0.6f), new Vector3(1.1f, 0.2f, 1.1f), tyre, axle, collider: false);
        }

        MeshKit.MergeByMaterial(b, "Jet/" + key);
        MeshKit.MergeByMaterial(gear, "JetGear");
        foreach (var mr in b.GetComponentsInChildren<MeshRenderer>())
            if (mr.GetComponent<TextMesh>() == null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 4.4f, -1f);
        col.size = new Vector3(4.4f, 5f, 36f);
        return root;
    }

    // ── The apron's helpers ──────────────────────────────────────────────────────────────────────
    public static void GroundCrew(Transform parent, Vector3 at, float yaw)
    {
        var g = PrimitiveKit.Group("GroundCrew_รถบริการภาคพื้น", parent, at, yaw).transform;
        var yellow = PrimitiveKit.Mat("TugYellow", new Color(0.98f, 0.78f, 0.1f), 0.5f);
        var white = PrimitiveKit.Mat("TankerWhite", new Color(0.93f, 0.93f, 0.92f), 0.6f, 0.2f);
        var grey = PrimitiveKit.Mat("JetMetal", new Color(0.72f, 0.74f, 0.78f), 0.8f, 0.8f);
        var tyre = PrimitiveKit.Mat("JetTyre", new Color(0.08f, 0.08f, 0.09f), 0.2f);
        void Wheels(Vector3 c, float half, float w)
        {
            foreach (float x in new[] { -w, w })
                foreach (float z in new[] { -half, half })
                    PrimitiveKit.Cylinder("Wheel", g, c + new Vector3(x, 0.32f, z), new Vector3(0.64f, 0.12f, 0.64f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        }
        // the push-back tug
        PrimitiveKit.Box("Tug", g, new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 0.9f, 4.2f), yellow);
        PrimitiveKit.Box("TugCab", g, new Vector3(0f, 1.5f, -1.2f), new Vector3(1.6f, 0.8f, 1.2f), PrimitiveKit.Mat("JetWindow", new Color(0.08f, 0.1f, 0.14f), 0.95f, 0.3f), collider: false);
        Wheels(Vector3.zero, 1.4f, 1.05f);
        // a train of baggage carts
        for (int i = 0; i < 3; i++)
        {
            var c = new Vector3(5f, 0f, -3f + i * 3.4f);
            PrimitiveKit.Box("Cart", g, c + new Vector3(0f, 0.7f, 0f), new Vector3(1.6f, 0.2f, 2.6f), grey);
            PrimitiveKit.Box("Bags", g, c + new Vector3(0f, 1.15f, 0f), new Vector3(1.4f, 0.7f, 2.2f), PrimitiveKit.Mat("Bags" + i, new[] { new Color(0.2f, 0.3f, 0.6f), new Color(0.6f, 0.15f, 0.15f), new Color(0.25f, 0.25f, 0.25f) }[i]), collider: false);
            PrimitiveKit.Box("CartRoof", g, c + new Vector3(0f, 1.8f, 0f), new Vector3(1.6f, 0.06f, 2.6f), yellow, collider: false);
            Wheels(c, 0.9f, 0.7f);
        }
        // the fuel truck
        var f = new Vector3(-6f, 0f, 2f);
        PrimitiveKit.Box("FuelCab", g, f + new Vector3(0f, 1.3f, 3.2f), new Vector3(2.3f, 1.8f, 1.8f), white);
        PrimitiveKit.Cylinder("FuelTank", g, f + new Vector3(0f, 1.6f, -0.8f), new Vector3(2.2f, 2.8f, 2.2f), white, new Vector3(90f, 0f, 0f));
        PrimitiveKit.Box("FuelStripe", g, f + new Vector3(0f, 1.6f, -0.8f), new Vector3(2.24f, 0.3f, 5.4f), PrimitiveKit.Mat("FuelRed", new Color(0.85f, 0.15f, 0.12f)), collider: false);
        Wheels(f + new Vector3(0f, 0f, 0.6f), 2.4f, 1.05f);
        // the passenger stairs truck
        var st = new Vector3(-12f, 0f, -2f);
        PrimitiveKit.Box("StairsBase", g, st + new Vector3(0f, 0.7f, 0f), new Vector3(2.2f, 0.6f, 5f), white);
        PrimitiveKit.Box("Stairs", g, st + new Vector3(0f, 2.3f, 0.2f), new Vector3(1.6f, 0.25f, 5.2f), grey, new Vector3(-35f, 0f, 0f), collider: false);
        Wheels(st, 1.8f, 0.95f);
    }

    // ── The airfield ─────────────────────────────────────────────────────────────────────────────
    // Runway along local +Z (length × 16 m) through `centre`, turned by `yaw`; the parallel taxiway
    // 40 m off on local +X, the hangar beyond it; AirTraffic with one template plane per airline.
    public static void Field(Transform root, Vector3 centre, float yaw, float length, string name)
    {
        var t = PrimitiveKit.Group("Airfield_" + name, root, centre, yaw).transform;
        var grass = PrimitiveKit.Mat("AirfieldGrass", new Color(0.44f, 0.6f, 0.32f));
        var asphalt = PrimitiveKit.Mat("RunwayAsphalt", new Color(0.2f, 0.2f, 0.22f));
        var paint = PrimitiveKit.Mat("RunwayPaint", new Color(0.95f, 0.95f, 0.93f));
        var yellow = PrimitiveKit.Mat("TaxiYellow", new Color(0.98f, 0.8f, 0.15f));
        float half = length * 0.5f;
        const float taxiX = 40f, hangarX = 78f;

        PrimitiveKit.Box("Grass", t, new Vector3(45f, -0.02f, 0f), new Vector3(190f, 0.02f, length + 300f), grass, collider: false);
        PrimitiveKit.Box("Runway", t, new Vector3(0f, 0.01f, 0f), new Vector3(16f, 0.02f, length), asphalt, collider: false);
        for (float z = -half + 40f; z < half - 40f; z += 14f) // centreline
            PrimitiveKit.Box("Centreline", t, new Vector3(0f, 0.025f, z), new Vector3(0.45f, 0.01f, 7f), paint, collider: false);
        foreach (float e in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("EdgeLine", t, new Vector3(e * 7.6f, 0.025f, 0f), new Vector3(0.3f, 0.01f, length), paint, collider: false);
            for (int i = 0; i < 6; i++) // threshold piano keys at both ends
                foreach (float x in new[] { -1f, 1f })
                    PrimitiveKit.Box("Threshold", t, new Vector3(x * (1.2f + i * 1.05f), 0.025f, e * (half - 8f)), new Vector3(0.6f, 0.01f, 10f), paint, collider: false);
            PrimitiveKit.Box("TouchdownZone", t, new Vector3(-3.5f, 0.025f, e * (half - 45f)), new Vector3(1.6f, 0.01f, 12f), paint, collider: false);
            PrimitiveKit.Box("TouchdownZone", t, new Vector3(3.5f, 0.025f, e * (half - 45f)), new Vector3(1.6f, 0.01f, 12f), paint, collider: false);
        }
        var edgeLight = PrimitiveKit.Glow("RunwayLight", new Color(1f, 0.95f, 0.8f), 2.5f);
        for (float z = -half; z <= half; z += 30f)
            foreach (float e in new[] { -1f, 1f })
                PrimitiveKit.Box("EdgeLight", t, new Vector3(e * 8.6f, 0.2f, z), new Vector3(0.25f, 0.4f, 0.25f), edgeLight, collider: false);
        var approach = PrimitiveKit.Glow("ApproachLight", new Color(1f, 0.9f, 0.6f), 3f);
        for (int i = 1; i <= 6; i++) // approach lights out before the landing end
        {
            PrimitiveKit.Box("ApproachBar", t, new Vector3(0f, 0.6f, -half - i * 18f), new Vector3(i == 3 ? 14f : 4f, 0.25f, 0.4f), approach, collider: false);
            PrimitiveKit.Box("ApproachPost", t, new Vector3(0f, 0.3f, -half - i * 18f), new Vector3(0.2f, 0.6f, 0.2f), paint, collider: false);
        }

        // taxiways: the parallel one and the links at both ends and the hangar
        PrimitiveKit.Box("Taxiway", t, new Vector3(taxiX + 7f, 0.008f, 0f), new Vector3(30f, 0.016f, length - 20f), asphalt, collider: false);
        foreach (float z in new[] { -half + 25f, half - 40f })
            PrimitiveKit.Box("TaxiLink", t, new Vector3(taxiX * 0.5f + 4f, 0.009f, z + (z > 0 ? 25f : 0f)), new Vector3(taxiX + 8f, 0.016f, 20f), asphalt, collider: false);
        for (float z = -half + 20f; z < half - 20f; z += 6f)
            PrimitiveKit.Box("TaxiLine", t, new Vector3(taxiX, 0.02f, z), new Vector3(0.25f, 0.01f, 3.5f), yellow, collider: false);

        // the hangar: a big shed with an open front facing the taxiway, the apron before it
        float hz = half - 188f;
        var shed = PrimitiveKit.Mat("HangarSteel", new Color(0.78f, 0.8f, 0.83f), 0.5f, 0.5f);
        var h = PrimitiveKit.Group("Hangar_โรงเก็บเครื่องบิน", t, new Vector3(hangarX + 26f, 0f, hz)).transform;
        PrimitiveKit.Box("Apron", t, new Vector3(hangarX - 4f, 0.008f, hz), new Vector3(40f, 0.016f, 46f), asphalt, collider: false);
        PrimitiveKit.Box("Back", h, new Vector3(20f, 8f, 0f), new Vector3(0.5f, 16f, 50f), shed);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Side", h, new Vector3(0f, 8f, s * 25f), new Vector3(40f, 16f, 0.5f), shed);
        PrimitiveKit.Box("Roof", h, new Vector3(0f, 16.2f, 0f), new Vector3(41f, 0.5f, 51f), shed, collider: false);
        PrimitiveKit.Box("Header", h, new Vector3(-20f, 14.5f, 0f), new Vector3(0.6f, 3.5f, 50f), PrimitiveKit.Mat("HangarBlue", new Color(0.15f, 0.3f, 0.6f)), collider: false);
        PrimitiveKit.Label("ศูนย์ซ่อมบำรุงอากาศยาน", h, new Vector3(-20.4f, 14.5f, 0f), 0.3f, Color.white, 90f);
        PrimitiveKit.Box("Floor", h, new Vector3(0f, 0.01f, 0f), new Vector3(40f, 0.02f, 50f), PrimitiveKit.Mat("ApronConcrete", new Color(0.62f, 0.62f, 0.6f)), collider: false);

        // windsock by the runway
        var sock = PrimitiveKit.Group("Windsock", t, new Vector3(-22f, 0f, -half + 60f)).transform;
        PrimitiveKit.Cylinder("Pole", sock, new Vector3(0f, 3f, 0f), new Vector3(0.15f, 3f, 0.15f), paint, collider: false);
        PrimitiveKit.Cylinder("Sock", sock, new Vector3(0f, 5.8f, 1.2f), new Vector3(0.7f, 1.2f, 0.7f), PrimitiveKit.Mat("SockOrange", new Color(1f, 0.45f, 0.1f)), new Vector3(80f, 0f, 0f), collider: false);

        // the planes that will fly: one of each airline, kept switched off
        var templates = PrimitiveKit.Group("PlaneTemplates", t, Vector3.zero).transform;
        for (int i = 0; i < Airlines.Length; i++)
            Jet(templates, Vector3.zero, 0f, Airlines[i], "Fly" + i);
        templates.gameObject.SetActive(false);
        t.gameObject.AddComponent<AirTraffic>().Configure(templates, length, taxiX, hangarX);
    }
}
