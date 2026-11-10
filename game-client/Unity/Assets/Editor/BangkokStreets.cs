using System.Collections.Generic;
using UnityEngine;

// กรุงเทพฯ south of the avenue: ถนนวิทยุ runs south from the avenue (carrying on the line of the
// housing estate's soi) down to the ring road, and ถนนพระราม 4 crosses the whole city east-west just
// short of the ring's south side, meeting both of its legs. Each has its own traffic (TrafficCar)
// going up and down, turning round short of the junctions so it never gets in the way of the avenue's
// or the ring's; the tuk-tuks and taxis can be waved down for a ride.
public static class BangkokStreets
{
    public const float WitthayuX = 55.5f, Rama4Z = -90f;
    private const float Width = 8f, Lane = 2f;
    private const float AvenueSouth = -17.7f; // CityTraffic's carriageway

    public static void Build(Transform root, RingSpec ring)
    {
        var t = PrimitiveKit.Group("BangkokStreets_ถนนกรุงเทพ", root, Vector3.zero).transform;
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        var yellow = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        float half = Width * 0.5f, inner = ring.Inner;

        // ถนนพระราม 4: ring to ring
        PrimitiveKit.Box("Rama4", t, new Vector3(0f, 0.022f, Rama4Z), new Vector3(inner * 2f, 0.04f, Width), asphalt);
        foreach (float s in new[] { -1f, 1f })
            foreach (var (a, b) in new[] { (-inner, WitthayuX - half), (WitthayuX + half, inner) })
                PrimitiveKit.Box("EdgeLine", t, new Vector3((a + b) * 0.5f, 0.044f, Rama4Z + s * (half - 0.25f)), new Vector3(b - a, 0.005f, 0.1f), white, collider: false);
        for (float x = -inner + 2f; x < inner - 1f; x += 4f)
            if (Mathf.Abs(x - WitthayuX) > half + 1f)
                PrimitiveKit.Box("Dash", t, new Vector3(x, 0.045f, Rama4Z), new Vector3(1.8f, 0.005f, 0.12f), yellow, collider: false);

        // ถนนวิทยุ: the avenue to the ring, across ถนนพระราม 4
        float zTop = AvenueSouth, zEnd = -inner;
        foreach (var (a, b) in new[] { (zEnd, Rama4Z - half), (Rama4Z + half, zTop) })
        {
            PrimitiveKit.Box("Witthayu", t, new Vector3(WitthayuX, 0.022f, (a + b) * 0.5f), new Vector3(Width, 0.04f, b - a), asphalt);
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("EdgeLine", t, new Vector3(WitthayuX + s * (half - 0.25f), 0.044f, (a + b) * 0.5f), new Vector3(0.1f, 0.005f, b - a), white, collider: false);
            for (float z = a + 2f; z < b - 1f; z += 4f)
                PrimitiveKit.Box("Dash", t, new Vector3(WitthayuX, 0.045f, z), new Vector3(0.12f, 0.005f, 1.8f), yellow, collider: false);
        }
        // a zebra at the crossroads
        for (float x = -half + 0.6f; x < half - 0.3f; x += 1f)
            PrimitiveKit.Box("Zebra", t, new Vector3(WitthayuX + x, 0.046f, Rama4Z + half + 1.8f), new Vector3(0.5f, 0.005f, 2.6f), white, collider: false);

        Sign(t, new Vector3(WitthayuX - half - 0.8f, 0f, AvenueSouth - 3f), "ถนนวิทยุ");
        Sign(t, new Vector3(WitthayuX + half + 0.8f, 0f, Rama4Z + half + 1f), "ถนนพระราม 4");
        Sign(t, new Vector3(-inner + 4f, 0f, Rama4Z + half + 0.8f), "ถนนพระราม 4");
        Sign(t, new Vector3(inner - 4f, 0f, Rama4Z + half + 0.8f), "ถนนพระราม 4");
        for (float x = -inner + 12f; x < inner - 8f; x += 20f)
            if (Mathf.Abs(x - WitthayuX) > 8f) StreetFurniture.LampPost(t, new Vector3(x, 0f, Rama4Z + half + 0.7f));
        for (float z = AvenueSouth - 10f; z > -inner + 6f; z -= 18f)
            if (Mathf.Abs(z - Rama4Z) > 8f) StreetFurniture.LampPost(t, new Vector3(WitthayuX + half + 0.7f, 0f, z));

        // Traffic: up and down each street keeping left, turning round short of the junctions
        var rama4 = Shuttle(new Vector3(-inner + 7f, 0f, Rama4Z), new Vector3(inner - 7f, 0f, Rama4Z));
        var witthayu = Shuttle(new Vector3(WitthayuX, 0f, AvenueSouth - 5f), new Vector3(WitthayuX, 0f, Rama4Z + half + 4.5f));
        void Car(string kind, int variant, Vector3[] path, float start)
        {
            var v = CityTraffic.Vehicle(t, kind, variant, out float halfLen, out float speed);
            v.gameObject.AddComponent<TrafficCar>().Configure(path, true, Mathf.Min(speed, 9f), halfLen, start, null, null, null);
            CityTraffic.Hailable(v, kind);
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.localPosition = path[0];
        }
        // slip roads between ถนนพระราม 4 and the ring road (CityTraffic.SlipRoads), a car on each
        int k = 0;
        foreach (var slip in CityTraffic.SlipRoads(ring, Rama4Z + Lane, Rama4Z - Lane, inner - 7f))
        {
            string kind = k % 2 == 0 ? "tuktuk" : "taxi";
            var v = CityTraffic.Vehicle(t, kind, k, out float halfLen, out float speed);
            v.gameObject.AddComponent<TrafficCar>().Configure(slip, false, Mathf.Min(speed, 9f), halfLen, 0f, null, null, null);
            CityTraffic.Hailable(v, kind);
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.localPosition = slip[0];
            k++;
        }
        Car("taxi", 0, rama4, 10f);
        Car("tuktuk", 1, rama4, 95f);
        Car("sedan", 1, rama4, 180f);
        Car("taxi", 3, rama4, 265f);
        Car("tuktuk", 2, rama4, 350f);
        Car("taxi", 2, rama4, 430f);
        Car("tuktuk", 0, witthayu, 10f);
        Car("taxi", 1, witthayu, 75f);
    }

    // A closed loop from a to b in the left lane and back in the other, with a U-turn at each end
    private static Vector3[] Shuttle(Vector3 a, Vector3 b)
    {
        var dir = (b - a).normalized;
        var left = new Vector3(-dir.z, 0f, dir.x); // keep left: the outbound lane is on the left
        var p = new List<Vector3> { a + left * Lane };
        void Turn(Vector3 c, Vector3 from)
        {
            for (int i = 0; i <= 8; i++)
            {
                float ang = -180f * i / 8f; // round to the right (a U-turn, driving on the left)
                p.Add(c + Quaternion.Euler(0f, -ang, 0f) * (from * Lane));
            }
        }
        Turn(b, left);   // heading out, swing round at the far end...
        p.RemoveAt(p.Count - 1);
        Turn(a, -left);  // ...and back
        p.RemoveAt(p.Count - 1);
        return p.ToArray();
    }

    private static void Sign(Transform t, Vector3 at, string text)
    {
        var g = PrimitiveKit.Group("StreetSign_" + text, t, at).transform;
        PrimitiveKit.Cylinder("Post", g, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("Plate", g, new Vector3(0f, 2.45f, 0f), new Vector3(2.2f, 0.42f, 0.04f), PrimitiveKit.Mat("StreetSignBlue", new Color(0.1f, 0.3f, 0.65f)), collider: false);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, -0.03f), 0.03f, Color.white);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, 0.03f), 0.03f, Color.white, 180f);
    }
}
