using UnityEngine;

// ตำรวจจราจร in the scenes (the officers themselves are made at runtime by the components):
//  TrafficPoint — an officer on a podium at a roundabout or crossroads, waving each way through in
//                 turn (TrafficPolice: the traffic waits at the line while the other way goes)
//  SpeedTrap    — an officer with a speed gun on a tripod by the road (SpeedTrap: a ticket over the limit)
//  Checkpoint   — ด่านตรวจ: cones along the road, signs, a tent and officers (PoliceCheckpoint:
//                 everyone through at 20 km/h)
// Positions are in `root` space (the scene's builder root).
public static class Police
{
    private static Material Cone => PrimitiveKit.Mat("PK_ConeOrange", new Color(0.98f, 0.45f, 0.08f), 0.4f);
    private static Material White => PrimitiveKit.Mat("PK_FasciaWhite", new Color(0.95f, 0.95f, 0.94f), 0.5f);
    private static Material Brown => PrimitiveKit.Mat("PK_PoliceBrown", new Color(0.36f, 0.25f, 0.16f), 0.4f);

    public static Transform TrafficPoint(Transform root, Vector3 junction, float radius, Vector3 officerAt)
    {
        var t = PrimitiveKit.Group("TrafficPolice_ตำรวจจราจร", root, officerAt).transform;
        // a low round podium under a striped umbrella
        PrimitiveKit.Cylinder("Podium", t, new Vector3(0f, 0.12f, 0f), new Vector3(1.4f, 0.12f, 1.4f), White, collider: false);
        PrimitiveKit.Cylinder("PodiumBand", t, new Vector3(0f, 0.2f, 0f), new Vector3(1.42f, 0.04f, 1.42f), Brown, collider: false);
        PrimitiveKit.Cylinder("UmbrellaPole", t, new Vector3(0.6f, 1.4f, 0.6f), new Vector3(0.06f, 1.4f, 0.06f), White, collider: false);
        PrimitiveKit.Cylinder("Umbrella", t, new Vector3(0.6f, 2.8f, 0.6f), new Vector3(2.2f, 0.06f, 2.2f), PrimitiveKit.Mat("PK_UmbrellaRed", new Color(0.8f, 0.12f, 0.1f), 0.3f), collider: false);
        PrimitiveKit.Cylinder("UmbrellaRing", t, new Vector3(0.6f, 2.74f, 0.6f), new Vector3(2.24f, 0.03f, 2.24f), White, collider: false);
        t.gameObject.AddComponent<TrafficPolice>().Configure(root.TransformPoint(junction), radius, root.TransformPoint(officerAt) + Vector3.up * 0.24f);
        return t;
    }

    // `yaw`: the way the officer faces (towards the road)
    public static Transform SpeedTrap(Transform root, Vector3 at, float yaw, int limit)
    {
        var t = PrimitiveKit.Group("SpeedTrap_ตรวจจับความเร็ว", root, at, yaw).transform;
        var black = PrimitiveKit.Mat("PK_PumpBody", new Color(0.18f, 0.2f, 0.22f), 0.4f);
        // the speed gun on its tripod, beside the officer
        var tripod = PrimitiveKit.Group("Tripod", t, new Vector3(0.9f, 0f, 0.2f)).transform;
        foreach (float a in new[] { 0f, 120f, 240f })
        {
            var leg = PrimitiveKit.Group("Leg", tripod, Vector3.zero, a).transform;
            PrimitiveKit.Box("Bar", leg, new Vector3(0f, 0.6f, 0.18f), new Vector3(0.04f, 1.25f, 0.04f), black, new Vector3(-16f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("Gun", tripod, new Vector3(0f, 1.3f, 0f), new Vector3(0.18f, 0.2f, 0.4f), black, collider: false);
        PrimitiveKit.Cylinder("Lens", tripod, new Vector3(0f, 1.3f, 0.22f), new Vector3(0.14f, 0.03f, 0.14f), PrimitiveKit.Glow("PK_GunLens", new Color(0.9f, 0.1f, 0.1f), 1.5f, 0.8f), new Vector3(90f, 0f, 0f), collider: false);
        // the sign facing the traffic
        var sign = PrimitiveKit.Group("Sign", t, new Vector3(-1.4f, 0f, 0.3f)).transform;
        PrimitiveKit.Box("Post", sign, new Vector3(0f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 0.08f), White, collider: false);
        PrimitiveKit.Box("Board", sign, new Vector3(0f, 1.8f, 0f), new Vector3(1.6f, 0.8f, 0.06f), PrimitiveKit.Mat("PK_SignBlue", new Color(0.1f, 0.25f, 0.6f), 0.4f), collider: false);
        PrimitiveKit.Label($"ตรวจจับความเร็ว\nจำกัด {limit} กม./ชม.", sign, new Vector3(0f, 1.8f, 0.04f), 0.03f, Color.white, 180f);
        t.gameObject.AddComponent<global::SpeedTrap>().Configure(24f, limit);
        return t;
    }

    // `yaw`: the road runs along local Z; `side` (+1/-1): the side of the road (local X) the tent and
    // officers are on; `roadWidth`: kerb to kerb
    public static Transform Checkpoint(Transform root, Vector3 centre, float yaw, float roadWidth, float side)
    {
        var t = PrimitiveKit.Group("Checkpoint_ด่านตรวจ", root, centre, yaw).transform;
        float half = roadWidth * 0.5f;
        // a few cones down the middle line only (both lanes stay open), spaced well apart
        for (float z = -8f; z <= 8.01f; z += 4f)
            ConeAt(t, new Vector3(0f, 0f, z));
        // signs well before it, on both approaches, each with a flashing lamp on top
        foreach (float s in new[] { -1f, 1f })
        {
            var sign = PrimitiveKit.Group("Sign", t, new Vector3(side * (half + 1.2f), 0f, s * 15f), s < 0f ? 0f : 180f).transform;
            PrimitiveKit.Box("Frame", sign, new Vector3(0f, 1f, 0f), new Vector3(1.8f, 1.1f, 0.06f), PrimitiveKit.Mat("PK_SignYellow", new Color(0.98f, 0.82f, 0.1f), 0.4f), collider: false);
            foreach (float x in new[] { -0.7f, 0.7f })
                PrimitiveKit.Box("Leg", sign, new Vector3(x, 0.45f, 0.1f), new Vector3(0.06f, 0.9f, 0.06f), White, collider: false);
            PrimitiveKit.Label("ด่านตรวจ\nชะลอความเร็ว 20", sign, new Vector3(0f, 1f, -0.04f), 0.03f, new Color(0.1f, 0.1f, 0.1f));
            var a = PrimitiveKit.Sphere("LampRed", sign, new Vector3(-0.45f, 1.68f, 0f), Vector3.one * 0.22f, RedLamp);
            var b = PrimitiveKit.Sphere("LampBlue", sign, new Vector3(0.45f, 1.68f, 0f), Vector3.one * 0.22f, BlueLamp);
            sign.gameObject.AddComponent<WarningBeacon>().Configure(new[] { a.GetComponent<Renderer>() }, new[] { b.GetComponent<Renderer>() },
                new Color(1f, 0.1f, 0.08f), new Color(0.1f, 0.3f, 1f), null, null);
            // a striped barrier on the shoulder at each end of the checkpoint, lamps flashing on it
            Barrier(t, new Vector3(side * (half + 1.3f), 0f, s * 10f));
        }
        PatrolCar(t, new Vector3(side * (half + 3.2f), 0f, -6.2f));
        // the tent beside the road
        var tent = PrimitiveKit.Group("Tent_เต็นท์ด่าน", t, new Vector3(side * (half + 3.2f), 0f, 0f)).transform;
        foreach (float x in new[] { -1.4f, 1.4f })
            foreach (float z in new[] { -1.8f, 1.8f })
                PrimitiveKit.Box("Leg", tent, new Vector3(x, 1.2f, z), new Vector3(0.08f, 2.4f, 0.08f), White, collider: false);
        PrimitiveKit.Box("Roof", tent, new Vector3(0f, 2.5f, 0f), new Vector3(3.2f, 0.15f, 4f), Brown, collider: false);
        PrimitiveKit.Box("Banner", tent, new Vector3(-side * 1.62f, 2.2f, 0f), new Vector3(0.04f, 0.5f, 4f), White, collider: false);
        PrimitiveKit.Label("ตำรวจ  ด่านตรวจ", tent, new Vector3(-side * 1.66f, 2.2f, 0f), 0.035f, new Color(0.36f, 0.25f, 0.16f), side > 0f ? -90f : 90f);
        PrimitiveKit.Box("Table", tent, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.05f, 1.8f), White);

        var spots = new[] { new Vector3(side * (half + 0.9f), 0f, -3.5f), new Vector3(side * (half + 0.9f), 0f, 3.5f) };
        t.gameObject.AddComponent<PoliceCheckpoint>().Configure(new Vector3(roadWidth, 4f, 24f), spots);
        return t;
    }

    private static Material RedLamp => PrimitiveKit.Glow("PK_BeaconRed", new Color(1f, 0.1f, 0.08f), 1f, 0.6f);
    private static Material BlueLamp => PrimitiveKit.Glow("PK_BeaconBlue", new Color(0.1f, 0.3f, 1f), 1f, 0.6f);

    // แผงกั้น: a red-and-white striped barrier on two feet, a red and a blue lamp on top taking turns
    private static void Barrier(Transform t, Vector3 p)
    {
        var b = PrimitiveKit.Group("Barrier_แผงกั้น", t, p).transform;
        var red = PrimitiveKit.Mat("PK_UmbrellaRed", new Color(0.8f, 0.12f, 0.1f), 0.3f);
        foreach (float z in new[] { -0.8f, 0.8f })
        {
            PrimitiveKit.Box("Foot", b, new Vector3(0f, 0.04f, z), new Vector3(0.6f, 0.08f, 0.12f), Brown, collider: false);
            PrimitiveKit.Box("Leg", b, new Vector3(0f, 0.5f, z), new Vector3(0.06f, 1f, 0.06f), White, collider: false);
        }
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Box("Stripe", b, new Vector3(0f, 0.85f, -0.75f + i * 0.3f), new Vector3(0.05f, 0.3f, 0.3f), i % 2 == 0 ? red : White, collider: false);
        var a = PrimitiveKit.Sphere("LampRed", b, new Vector3(0f, 1.12f, -0.6f), Vector3.one * 0.2f, RedLamp);
        var c = PrimitiveKit.Sphere("LampBlue", b, new Vector3(0f, 1.12f, 0.6f), Vector3.one * 0.2f, BlueLamp);
        b.gameObject.AddComponent<WarningBeacon>().Configure(new[] { a.GetComponent<Renderer>() }, new[] { c.GetComponent<Renderer>() },
            new Color(1f, 0.1f, 0.08f), new Color(0.1f, 0.3f, 1f), null, null, 0.7f);
    }

    // รถสายตรวจ parked by the tent: a white pickup with a brown stripe and a flashing light bar that
    // lights up the road round it at night
    private static void PatrolCar(Transform t, Vector3 p)
    {
        var car = PrimitiveKit.Group("PatrolCar_รถสายตรวจ", t, p).transform;
        var tyre = PrimitiveKit.Mat("PK_PumpBody", new Color(0.18f, 0.2f, 0.22f), 0.4f);
        var glass = PrimitiveKit.Mat("PK_StationGlass", new Color(0.35f, 0.5f, 0.6f), 0.9f);
        PrimitiveKit.Box("Body", car, new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 0.7f, 4.6f), White);
        PrimitiveKit.Box("Stripe", car, new Vector3(0f, 0.8f, 0f), new Vector3(1.82f, 0.16f, 4.4f), Brown, collider: false);
        PrimitiveKit.Box("Cab", car, new Vector3(0f, 1.4f, 0.55f), new Vector3(1.7f, 0.65f, 2f), White, collider: false);
        PrimitiveKit.Box("Windscreen", car, new Vector3(0f, 1.42f, 1.56f), new Vector3(1.55f, 0.5f, 0.04f), glass, new Vector3(-12f, 0f, 0f), collider: false);
        foreach (float x in new[] { -0.86f, 0.86f })
            PrimitiveKit.Box("Window", car, new Vector3(x, 1.45f, 0.55f), new Vector3(0.03f, 0.42f, 1.7f), glass, collider: false);
        PrimitiveKit.Box("BedRim", car, new Vector3(0f, 1.15f, -1.45f), new Vector3(1.8f, 0.12f, 1.7f), White, collider: false);
        foreach (float x in new[] { -0.78f, 0.78f })
            foreach (float z in new[] { -1.5f, 1.5f })
                PrimitiveKit.Cylinder("Wheel", car, new Vector3(x, 0.36f, z), new Vector3(0.68f, 0.13f, 0.68f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Label("ตำรวจ  POLICE", car, new Vector3(-0.92f, 0.82f, -0.3f), 0.03f, new Color(0.36f, 0.25f, 0.16f), 90f);
        PrimitiveKit.Label("ตำรวจ  POLICE", car, new Vector3(0.92f, 0.82f, -0.3f), 0.03f, new Color(0.36f, 0.25f, 0.16f), -90f);
        // the light bar: two red on one side, two blue on the other
        PrimitiveKit.Box("BarBase", car, new Vector3(0f, 1.76f, 0.55f), new Vector3(1.3f, 0.06f, 0.3f), tyre, collider: false);
        var reds = new Renderer[2];
        var blues = new Renderer[2];
        for (int i = 0; i < 2; i++)
        {
            reds[i] = PrimitiveKit.Box("BarRed", car, new Vector3(-0.5f + i * 0.26f, 1.86f, 0.55f), new Vector3(0.24f, 0.14f, 0.26f), RedLamp, collider: false).GetComponent<Renderer>();
            blues[i] = PrimitiveKit.Box("BarBlue", car, new Vector3(0.24f + i * 0.26f, 1.86f, 0.55f), new Vector3(0.24f, 0.14f, 0.26f), BlueLamp, collider: false).GetComponent<Renderer>();
        }
        car.gameObject.AddComponent<WarningBeacon>().Configure(reds, blues, new Color(1f, 0.1f, 0.08f), new Color(0.1f, 0.3f, 1f),
            BarLight(car, new Vector3(-0.6f, 2.2f, 0.55f), new Color(1f, 0.15f, 0.1f)), BarLight(car, new Vector3(0.6f, 2.2f, 0.55f), new Color(0.15f, 0.35f, 1f)));
    }

    private static Light BarLight(Transform car, Vector3 at, Color c)
    {
        var go = new GameObject("FlashLight");
        go.transform.SetParent(car, false);
        go.transform.localPosition = at;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = c;
        light.range = 14f;
        light.intensity = 6f;
        light.shadows = LightShadows.None;
        light.enabled = false;
        return light;
    }

    private static void ConeAt(Transform t, Vector3 p)
    {
        var c = PrimitiveKit.Group("Cone", t, p).transform;
        PrimitiveKit.Box("Base", c, new Vector3(0f, 0.02f, 0f), new Vector3(0.45f, 0.04f, 0.45f), Cone, collider: false);
        PrimitiveKit.Cylinder("Body", c, new Vector3(0f, 0.32f, 0f), new Vector3(0.26f, 0.3f, 0.26f), Cone, collider: false);
        PrimitiveKit.Cylinder("Band", c, new Vector3(0f, 0.4f, 0f), new Vector3(0.24f, 0.05f, 0.24f), White, collider: false);
    }
}
