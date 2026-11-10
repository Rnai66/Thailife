using System.Collections.Generic;
using UnityEngine;

// The town's own streets, joined up round the clock tower (หอนาฬิกา):
//   วงเวียนหอนาฬิกา  the roundabout: asphalt all round the tower's lawn, cars going round clockwise
//                     (driving on the left), white edge lines, give-way marks at each entry
//   ถนนย่านผับ        east-west through the roundabout, past the pubs, from the west soi to the market
//   ถนนสถานี          the cross street carried on north to the station road (ถนนหน้าสถานีรถไฟ),
//                     which runs along the front of the railway station's forecourt
//   ซอย 1             down the west side of town: from the avenue up past the pubs to the station road
//   ซอยหมู่บ้าน         the estate's soi in the east carried on north to meet the station road
// Town traffic (TrafficCar, closed loops, keeping left): round the west block one way and the other,
// and a market run east of the roundabout that does a lap of the tower before it comes back.
public static class CityStreets
{
    public static readonly Vector3 Roundabout = new Vector3(0f, 0f, 14f);
    private const float RingOuter = 8.6f, RingLane = 6.6f;
    private const float PubZ = 14f, StationZ = 47f, WestSoiX = -49.5f, EastSoiX = 55.5f;
    private const float Lane = 1.5f;

    private static Material asphalt, white, yellow;

    // mainStreet: the name on the plates of the street across the roundabout (the pub street in เมือง)
    public static void Build(Transform root, string mainStreet = "ถนนย่านผับ")
    {
        asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        yellow = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        var t = PrimitiveKit.Group("TownStreets_ถนนในเมือง", root, Vector3.zero).transform;

        // ── the roundabout ──
        PrimitiveKit.Cylinder("RoundaboutRing", t, Roundabout + new Vector3(0f, 0.024f, 0f), new Vector3(RingOuter * 2f, 0.012f, RingOuter * 2f), asphalt, collider: false);
        for (int i = 0; i < 40; i++) // white edge line round the outside, dashed lane line round the lawn
        {
            float a = i * Mathf.PI * 2f / 40f, deg = -a * Mathf.Rad2Deg;
            PrimitiveKit.Box("RingEdge", t, Roundabout + new Vector3(Mathf.Cos(a) * (RingOuter - 0.25f), 0.04f, Mathf.Sin(a) * (RingOuter - 0.25f)), new Vector3(0.12f, 0.01f, 1.4f), white, new Vector3(0f, deg, 0f), collider: false);
            if (i % 2 == 0)
                PrimitiveKit.Box("RingInner", t, Roundabout + new Vector3(Mathf.Cos(a) * 4.9f, 0.04f, Mathf.Sin(a) * 4.9f), new Vector3(0.12f, 0.01f, 0.5f), white, new Vector3(0f, deg, 0f), collider: false);
        }
        // give-way lines across each entry (the car on the ring goes first)
        foreach (var (at, yaw) in new[] { (new Vector3(-1.5f, 0f, 14f - RingOuter - 0.3f), 0f), (new Vector3(1.5f, 0f, 14f + RingOuter + 0.3f), 0f),
                                          (new Vector3(-RingOuter - 0.3f, 0f, 14f + 1.5f), 90f), (new Vector3(RingOuter + 0.3f, 0f, 14f - 1.5f), 90f) })
            for (int k = -2; k <= 2; k++)
                PrimitiveKit.Box("GiveWay", t, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(k * 0.55f, 0.042f, 0f), new Vector3(0.35f, 0.01f, 0.2f), white, new Vector3(0f, yaw, 0f), collider: false);

        // ── the streets (x0, x1 / z0, z1), centre lines dashed ──
        StreetX(t, "ถนนย่านผับ", PubZ, -52f, -RingOuter + 0.5f, 6f);
        StreetX(t, "ถนนย่านผับ", PubZ, RingOuter - 0.5f, 29f, 6f);
        StreetZ(t, "ถนนสถานี", 0f, 22f, StationZ - 2.4f, 6f);
        StreetX(t, "ถนนหน้าสถานีรถไฟ", StationZ, -CityRingRoad.Town.Inner, CityRingRoad.Town.Inner, 4.8f); // out to the ring road both ends
        StreetZ(t, "ซอย1", WestSoiX, -5.4f, StationZ + 2.4f, 5f);
        StreetZ(t, "ซอยหมู่บ้าน", EastSoiX, 40f, StationZ + 2.4f, 5f, centreLine: false);
        // the market end of the pub street: a turning circle
        PrimitiveKit.Cylinder("TurningCircle", t, new Vector3(28.5f, 0.022f, PubZ), new Vector3(7f, 0.012f, 7f), asphalt, collider: false);
        // zebra crossings: in front of the station, and across the pub street by the pubs
        Zebra(t, new Vector3(-6f, 0f, StationZ), 4.8f, 90f);
        Zebra(t, new Vector3(-22f, 0f, PubZ), 6f, 90f);
        Zebra(t, new Vector3(0f, 0f, 26f), 6f, 0f);

        // street name plates on the corners (blue, white letters, Thai style)
        StreetSign(t, new Vector3(-12f, 0f, PubZ + 3.6f), 0f, mainStreet);
        StreetSign(t, new Vector3(-3.6f, 0f, 28.5f), 90f, "ถนนสถานี");
        StreetSign(t, new Vector3(8f, 0f, StationZ - 3.1f), 0f, "ถนนหน้าสถานีรถไฟ");
        StreetSign(t, new Vector3(WestSoiX + 3f, 0f, PubZ - 3.6f), -90f, "ซอย 1");
        StreetSign(t, new Vector3(EastSoiX - 3f, 0f, StationZ - 3.1f), 90f, "ซอยหมู่บ้าน");
        // lamps along the station road and down soi 1
        for (float x = -44f; x <= 52f; x += 16f)
            StreetFurniture.LampPost(t, new Vector3(x, 0f, StationZ - 3f));
        for (float z = 2f; z <= 40f; z += 16f)
            StreetFurniture.LampPost(t, new Vector3(WestSoiX - 3f, 0f, z));

        Traffic(t);
    }

    private static void StreetX(Transform t, string name, float z, float x0, float x1, float width, bool centreLine = true)
    {
        PrimitiveKit.Box("Street_" + name, t, new Vector3((x0 + x1) * 0.5f, 0.021f, z), new Vector3(x1 - x0, 0.04f, width), asphalt);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("EdgeLine", t, new Vector3((x0 + x1) * 0.5f, 0.042f, z + s * (width * 0.5f - 0.2f)), new Vector3(x1 - x0, 0.005f, 0.1f), white, collider: false);
        if (centreLine)
            for (float x = x0 + 1.5f; x < x1 - 1f; x += 4f)
                PrimitiveKit.Box("Dash", t, new Vector3(x, 0.043f, z), new Vector3(1.8f, 0.005f, 0.12f), yellow, collider: false);
    }

    private static void StreetZ(Transform t, string name, float x, float z0, float z1, float width, bool centreLine = true)
    {
        PrimitiveKit.Box("Street_" + name, t, new Vector3(x, 0.021f, (z0 + z1) * 0.5f), new Vector3(width, 0.04f, z1 - z0), asphalt);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("EdgeLine", t, new Vector3(x + s * (width * 0.5f - 0.2f), 0.042f, (z0 + z1) * 0.5f), new Vector3(0.1f, 0.005f, z1 - z0), white, collider: false);
        if (centreLine)
            for (float z = z0 + 1.5f; z < z1 - 1f; z += 4f)
                PrimitiveKit.Box("Dash", t, new Vector3(x, 0.043f, z), new Vector3(0.12f, 0.005f, 1.8f), yellow, collider: false);
    }

    private static void Zebra(Transform t, Vector3 at, float width, float yaw)
    {
        var rot = Quaternion.Euler(0f, yaw, 0f);
        for (float k = -width * 0.5f + 0.5f; k < width * 0.5f - 0.2f; k += 0.9f)
            PrimitiveKit.Box("Zebra", t, at + rot * new Vector3(k, 0.044f, 0f), new Vector3(0.5f, 0.005f, 2.6f), white, new Vector3(0f, yaw, 0f), collider: false);
    }

    private static void StreetSign(Transform t, Vector3 at, float yaw, string text)
    {
        var g = PrimitiveKit.Group("StreetSign_" + text, t, at, yaw).transform;
        PrimitiveKit.Cylinder("Post", g, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("Plate", g, new Vector3(0f, 2.45f, 0f), new Vector3(1.9f, 0.42f, 0.04f), PrimitiveKit.Mat("StreetSignBlue", new Color(0.1f, 0.3f, 0.65f)), collider: false);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, -0.03f), 0.03f, Color.white);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, 0.03f), 0.03f, Color.white, 180f);
    }

    // ── Traffic ────────────────────────────────────────────────────────────────
    private static void Arc(List<Vector3> p, Vector3 centre, float radius, float from, float to, int steps)
    {
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
            p.Add(centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    private static float Angle(float dx, float dz) => Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;

    private static void Traffic(Transform t)
    {
        var c = Roundabout;
        float r = RingLane;
        // where each lane meets the ring (angles round the tower, counter-clockwise from east)
        float northOut = Angle(-1.4f, Mathf.Sqrt(r * r - 1.96f));        // leaving north, keeping left (x -1.4)
        float northIn = Angle(1.4f, Mathf.Sqrt(r * r - 1.96f));          // coming south down the station street (x +1.4)
        float westIn = Angle(-Mathf.Sqrt(r * r - Lane * Lane), Lane);     // heading east along the pub street (z +1.5)
        float westOut = Angle(-Mathf.Sqrt(r * r - Lane * Lane), -Lane);   // heading west (z -1.5)
        float eastOut = Angle(Mathf.Sqrt(r * r - Lane * Lane), Lane);     // heading east (z +1.5)
        float eastIn = Angle(Mathf.Sqrt(r * r - Lane * Lane), -Lane);     // heading west back to the ring (z -1.5)
        if (westOut > 0f) westOut -= 360f;

        // round the west block, anticlockwise: north up the station street, west along the station
        // road, south down soi 1, east along the pub street, round the ring to the north exit
        var a = new List<Vector3>();
        Arc(a, c, r, westIn, northOut, 8);
        a.Add(new Vector3(-1.4f, 0f, StationZ - 3f));
        a.Add(new Vector3(-2.1f, 0f, StationZ - 1.8f));
        a.Add(new Vector3(-3.4f, 0f, StationZ - 1.2f));
        a.Add(new Vector3(WestSoiX + 2.5f, 0f, StationZ - 1.2f));
        a.Add(new Vector3(WestSoiX + 1.5f, 0f, StationZ - 1.8f));
        a.Add(new Vector3(WestSoiX + 1.25f, 0f, StationZ - 3f));
        a.Add(new Vector3(WestSoiX + 1.25f, 0f, PubZ + 3f));
        a.Add(new Vector3(WestSoiX + 1.6f, 0f, PubZ + 1.9f));
        a.Add(new Vector3(WestSoiX + 2.8f, 0f, PubZ + Lane));
        a.Add(new Vector3(c.x - RingOuter - 2f, 0f, PubZ + Lane));

        // the other way round: south down the station street, three-quarters round the ring, west
        // along the pub street, north up soi 1, east along the station road
        var b = new List<Vector3>();
        Arc(b, c, r, northIn, westOut, 20);
        b.Add(new Vector3(WestSoiX + 2.8f, 0f, PubZ - Lane));
        b.Add(new Vector3(WestSoiX - 0.6f, 0f, PubZ - 1.1f));
        b.Add(new Vector3(WestSoiX - 1.25f, 0f, PubZ + 0.5f));
        b.Add(new Vector3(WestSoiX - 1.25f, 0f, StationZ - 0.5f));
        b.Add(new Vector3(WestSoiX - 0.6f, 0f, StationZ + 1f));
        b.Add(new Vector3(WestSoiX + 1f, 0f, StationZ + 1.2f));
        b.Add(new Vector3(-0.8f, 0f, StationZ + 1.2f));
        b.Add(new Vector3(0.9f, 0f, StationZ + 0.4f));
        b.Add(new Vector3(1.4f, 0f, StationZ - 1.2f));
        b.Add(new Vector3(1.4f, 0f, c.z + RingOuter + 1.5f));

        // the market run: east from the ring to the turning circle, back west and a whole lap of the tower
        var m = new List<Vector3>();
        Arc(m, c, r, eastIn, eastOut - 360f, 26);
        m.Add(new Vector3(26f, 0f, PubZ + Lane));
        Arc(m, new Vector3(27.5f, 0f, PubZ), Lane + 0.3f, 90f, -90f, 8);
        m.Add(new Vector3(c.x + RingOuter + 2f, 0f, PubZ - Lane));

        void Car(string kind, int variant, List<Vector3> path, float start)
        {
            var v = CityTraffic.Vehicle(t, kind, variant, out float halfLen, out _);
            var pts = path.ToArray();
            v.gameObject.AddComponent<TrafficCar>().Configure(pts, true, kind == "tuktuk" ? 6f : 7f, halfLen, start, null, null, null);
            CityTraffic.Hailable(v, kind); // wave one down for a ride round town
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.localPosition = pts[0];
        }
        Car("tuktuk", 0, a, 0f);
        Car("sedan", 0, a, 45f);
        Car("taxi", 1, a, 95f);
        Car("sedan", 1, b, 10f);
        Car("tuktuk", 2, b, 70f);
        Car("taxi", 2, b, 130f);
        Car("tuktuk", 1, m, 5f);
        Car("sedan", 0, m, 40f);
        // the whole of the station road, ring road to ring road (both ends join it, and soi 1 and the
        // station street): with it every street in town is joined up for the traffic to wander
        BackAndForth(t, "ถนนหน้าสถานี", new Vector3(-87f, 0f, StationZ), new Vector3(87f, 0f, StationZ), 1.2f,
            ("taxi", 3, 20f), ("songthaew", 2, 120f), ("tuktuk", 0, 260f));
    }

    // A two-way street with no loop of its own (a soi, the station road): out along one side, round at
    // the end, back along the other — keeping left. Its ends meet other roads, so TrafficNet joins it
    // to them and the town's traffic wanders in and out of it.
    public static void BackAndForth(Transform parent, string name, Vector3 a, Vector3 b, float halfGap, params (string kind, int variant, float start)[] cars)
    {
        var t = PrimitiveKit.Group("Traffic_" + name, parent, Vector3.zero).transform;
        var dir = (b - a).normalized;
        var left = new Vector3(-dir.z, 0f, dir.x);
        var p = new List<Vector3> { a + left * halfGap, b + left * halfGap };
        for (int i = 1; i < 8; i++)
        {
            float th = i * Mathf.PI / 8f;
            p.Add(b + (left * Mathf.Cos(th) + dir * Mathf.Sin(th)) * halfGap);
        }
        p.Add(b - left * halfGap);
        p.Add(a - left * halfGap);
        for (int i = 1; i < 8; i++)
        {
            float th = i * Mathf.PI / 8f;
            p.Add(a + (-left * Mathf.Cos(th) - dir * Mathf.Sin(th)) * halfGap);
        }
        var pts = p.ToArray();
        foreach (var (kind, variant, start) in cars)
        {
            var v = CityTraffic.Vehicle(t, kind, variant, out float halfLen, out _);
            v.gameObject.AddComponent<TrafficCar>().Configure(pts, true, kind == "tuktuk" ? 6f : 7f, halfLen, start, null, null, null);
            CityTraffic.Hailable(v, kind);
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.localPosition = pts[0];
        }
    }
}
