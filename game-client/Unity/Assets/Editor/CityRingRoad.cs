using System.Collections.Generic;
using UnityEngine;

// ถนนวงแหวนรอบเมือง: a two-lane road right round the edge of a town (RingSpec says where) — the four
// sides, with the town's streets meeting it at T-junctions. Where its east and west sides cross the
// railway, a จุดตัดทางรถไฟ: the road rises over the track on a timber-and-rubber deck, with red-and-white
// barrier arms, flashing red lamps and the white X sign on both approaches (LevelCrossing lowers them
// for the train and the traffic waits); where they cross a river, a bridge (RiverBridges). Its traffic
// (CityTraffic) goes round and round both ways and never turns into the town, so nothing can jam at
// the junctions.
public class RingSpec
{
    public float Edge;                              // the four sides' centre lines: x ±Edge, z ±Edge
    public float RailZ = float.NaN;                 // a railway along x here: level crossings
    public float RiverZ = float.NaN, RiverHalf;     // a river along x here: bridges (west สะพานพุทธ, east พระราม 8)
    public float BridgeTop = 6.8f, BridgeFrom, BridgeTo; // the deck's height and where its ramps start and end
    public (float, float)[] LegGaps = new (float, float)[0];  // z spans where streets join the east and west sides
    public (float, float)[] SideGaps = new (float, float)[0]; // x spans where streets join the south side

    public float Inner => Edge - CityRingRoad.HalfWidth;    // the town side: where the streets end
    public bool Bridges => !float.IsNaN(RiverZ);

    // The east and west sides' rise and fall, going north: (z, height)
    public (float z, float y)[] Profile
    {
        get
        {
            if (Bridges)
                return new[] { (BridgeFrom, 0f), (RiverZ - RiverHalf - 3f, BridgeTop), (RiverZ + RiverHalf + 3f, BridgeTop), (BridgeTo, 0f) };
            if (!float.IsNaN(RailZ))
                return new[] { (RailZ - 9.2f, 0f), (RailZ - 3.2f, 0.58f), (RailZ + 3.2f, 0.58f), (RailZ + 9.2f, 0f) };
            return new (float, float)[0];
        }
    }
}

public static class CityRingRoad
{
    public const float HalfWidth = 3.2f;    // two lanes of 3.2 m
    public const float LaneOffset = 1.6f;
    public const float RailZ = 62f;         // the provincial town's railway (ThaiRailway.Line)
    private const float DeckHalf = 3.2f, Ramp = 6f, DeckTop = 0.58f;

    // The provincial town (ground ±100): avenue and station road meet it, railway crossings
    public static readonly RingSpec Town = new RingSpec
    {
        Edge = 94f, RailZ = CityRingRoad.RailZ,
        LegGaps = new[] { (-17.7f, -5.3f), (44.6f, 49.4f) },
    };

    // กรุงเทพฯ (ground ±125): over the เจ้าพระยา on two bridges (RiverBridges); the avenue and ถนนพระราม 4
    // meet its legs, ถนนวิทยุ its south side (BangkokStreets)
    public static readonly RingSpec Capital = new RingSpec
    {
        Edge = 118f, RiverZ = 62f, RiverHalf = 13f, BridgeTop = 6.8f, BridgeFrom = 12f, BridgeTo = 110f,
        LegGaps = new[] { (-17.7f, -5.3f), (-94f, -86f) },
        SideGaps = new[] { (51.5f, 59.5f) },
    };

    // A closed driving loop round the ring keeping left. anticlockwise: east along the south side, up
    // the east side, west along the north, down the west side (all left turns, the inner lane);
    // otherwise the reverse on the outer lane.
    public static Vector3[] Loop(RingSpec ring, bool anticlockwise)
    {
        const float r = 6f;
        float e = anticlockwise ? ring.Edge - LaneOffset : ring.Edge + LaneOffset; // lane line on each side
        var profile = ring.Profile;
        var p = new List<Vector3>();
        void Arc(Vector3 c, float from, float to)
        {
            for (int i = 0; i <= 6; i++)
            {
                float a = Mathf.Lerp(from, to, i / 6f) * Mathf.Deg2Rad;
                p.Add(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r);
            }
        }
        void Rise(float x, bool north)
        {
            for (int i = 0; i < profile.Length; i++)
            {
                var (z, y) = profile[north ? i : profile.Length - 1 - i];
                p.Add(new Vector3(x, y, z));
            }
        }
        if (anticlockwise)
        {
            Arc(new Vector3(e - r, 0f, -e + r), -90f, 0f);   // south side, left up the east side
            Rise(e, north: true);
            Arc(new Vector3(e - r, 0f, e - r), 0f, 90f);     // left along the north, heading west
            Arc(new Vector3(-e + r, 0f, e - r), 90f, 180f);  // left down the west side
            Rise(-e, north: false);
            Arc(new Vector3(-e + r, 0f, -e + r), 180f, 270f); // left along the south, heading east
        }
        else
        {
            Arc(new Vector3(-e + r, 0f, -e + r), 270f, 180f); // south side heading west, right up the west side
            Rise(-e, north: true);
            Arc(new Vector3(-e + r, 0f, e - r), 180f, 90f);  // right along the north, heading east
            Arc(new Vector3(e - r, 0f, e - r), 90f, 0f);     // right down the east side
            Rise(e, north: false);
            Arc(new Vector3(e - r, 0f, -e + r), 0f, -90f);   // right along the south, heading west
        }
        return p.ToArray();
    }

    public static void Build(Transform root, RingSpec ring, Transform railway = null)
    {
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
        var yellow = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        var t = PrimitiveKit.Group("RingRoad_ถนนวงแหวน", root, Vector3.zero).transform;
        float edge = ring.Edge, inner = ring.Inner;
        float outer = edge + HalfWidth - 0.2f, innerLine = inner + 0.2f;
        bool Gap((float, float)[] gaps, float v, float margin)
        {
            foreach (var (a, b) in gaps) if (v > a - margin && v < b + margin) return true;
            return false;
        }

        // the north and south sides, full width (their ends are the corners)
        foreach (float z in new[] { -edge, edge })
        {
            float s = Mathf.Sign(z);
            var gaps = s < 0f ? ring.SideGaps : new (float, float)[0];
            PrimitiveKit.Box("Ring_Side", t, new Vector3(0f, 0.021f, z), new Vector3((edge + HalfWidth) * 2f, 0.04f, HalfWidth * 2f), asphalt);
            PrimitiveKit.Box("EdgeLine", t, new Vector3(0f, 0.042f, s * outer), new Vector3(outer * 2f, 0.005f, 0.1f), white, collider: false);
            Line(t, s * innerLine, -innerLine, innerLine, gaps, white, alongX: true);
            for (float x = -inner + 2f; x < inner - 1f; x += 4f)
                if (!Gap(gaps, x, 1f)) PrimitiveKit.Box("Dash", t, new Vector3(x, 0.043f, z), new Vector3(1.8f, 0.005f, 0.12f), yellow, collider: false);
        }
        // the east and west sides between them, on the ground either side of the crossing or bridge;
        // the town-side edge line and the centre dashes stop where streets join
        var profile = ring.Profile;
        var spans = profile.Length > 0
            ? new[] { (-inner, profile[0].z), (profile[profile.Length - 1].z, inner) }
            : new[] { (-inner, inner) };
        foreach (float x in new[] { -edge, edge })
        {
            float s = Mathf.Sign(x);
            foreach (var (z0, z1) in spans)
            {
                PrimitiveKit.Box("Ring_Side", t, new Vector3(x, 0.021f, (z0 + z1) * 0.5f), new Vector3(HalfWidth * 2f, 0.04f, z1 - z0), asphalt);
                Line(t, s * outer, z0 == -inner ? -outer : z0, z1 == inner ? outer : z1, null, white, alongX: false);
                Line(t, s * innerLine, z0 == -inner ? -innerLine : z0, z1 == inner ? innerLine : z1, ring.LegGaps, white, alongX: false);
                for (float z = z0 + 1.5f; z < z1 - 1f; z += 4f)
                    if (!Gap(ring.LegGaps, z, 1f)) PrimitiveKit.Box("Dash", t, new Vector3(x, 0.043f, z), new Vector3(0.12f, 0.005f, 1.8f), yellow, collider: false);
            }
            if (ring.Bridges) RiverBridges.Build(t, ring, x, memorial: x < 0f);
            else if (!float.IsNaN(ring.RailZ)) Crossing(t, railway, x, ring.RailZ, asphalt, white);
        }

        // name plates where the streets meet it
        foreach (float s in new[] { -1f, 1f })
            foreach (var (_, b) in ring.LegGaps)
                Sign(t, new Vector3(s * (inner - 0.8f), 0f, b + 2.7f), s * 90f, "ถนนวงแหวนรอบเมือง");
        // street lamps on the town side
        float lampFrom = profile.Length > 0 ? profile[0].z - (ring.Bridges ? 2f : 12f) : float.MaxValue;
        float lampTo = profile.Length > 0 ? profile[profile.Length - 1].z + (ring.Bridges ? 2f : 12f) : float.MinValue;
        for (float z = -inner + 9f; z < inner - 6f; z += 16f)
        {
            if (Gap(ring.LegGaps, z, 4f) || (z > lampFrom && z < lampTo)) continue;
            foreach (float s in new[] { -1f, 1f })
                StreetFurniture.LampPost(t, new Vector3(s * (inner - 0.7f), 0f, z));
        }
        for (float x = -inner + 19f; x <= inner - 18f; x += 24f)
            foreach (float s in new[] { -1f, 1f })
                if (s > 0f || !Gap(ring.SideGaps, x, 4f))
                    StreetFurniture.LampPost(t, new Vector3(x, 0f, s * (inner - 0.7f)));
    }

    // A white edge line from a to b (along x at z = at, or along z at x = at), broken where side roads join (gaps)
    private static void Line(Transform t, float at, float a0, float a1, (float, float)[] gaps, Material white, bool alongX)
    {
        var cuts = new List<float> { a0 };
        if (gaps != null)
            foreach (var (a, b) in gaps)
                if (b > a0 && a < a1) { cuts.Add(Mathf.Max(a0, a)); cuts.Add(Mathf.Min(a1, b)); }
        cuts.Add(a1);
        for (int i = 0; i + 1 < cuts.Count; i += 2)
        {
            float len = cuts[i + 1] - cuts[i], mid = (cuts[i] + cuts[i + 1]) * 0.5f;
            if (len <= 0.1f) continue;
            if (alongX) PrimitiveKit.Box("EdgeLine", t, new Vector3(mid, 0.042f, at), new Vector3(len, 0.005f, 0.1f), white, collider: false);
            else PrimitiveKit.Box("EdgeLine", t, new Vector3(at, 0.042f, mid), new Vector3(0.1f, 0.005f, len), white, collider: false);
        }
    }

    // ── The level crossing over the railway ───────────────────────────────────────
    private static void Crossing(Transform t, Transform railway, float x, float railZ, Material asphalt, Material white)
    {
        var c = PrimitiveKit.Group("LevelCrossing_จุดตัดทางรถไฟ", t, new Vector3(x, 0f, railZ)).transform;
        var rubber = PrimitiveKit.Mat("CrossingRubber", new Color(0.16f, 0.16f, 0.17f));
        float w = HalfWidth * 2f;
        // the deck over the track (just under the rail heads) and the ramps up to it
        PrimitiveKit.Box("Deck", c, new Vector3(0f, DeckTop - 0.06f, 0f), new Vector3(w, 0.12f, DeckHalf * 2f), rubber);
        float rise = Mathf.Atan2(DeckTop, Ramp) * Mathf.Rad2Deg, len = Mathf.Sqrt(Ramp * Ramp + DeckTop * DeckTop);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Ramp", c, new Vector3(0f, DeckTop * 0.5f - 0.03f, s * (DeckHalf + Ramp * 0.5f)), new Vector3(w, 0.1f, len), asphalt, new Vector3(s * rise, 0f, 0f));
            foreach (float k in new[] { -1f, 1f })
                PrimitiveKit.Box("Kerb", c, new Vector3(k * (HalfWidth + 0.1f), DeckTop * 0.5f, s * (DeckHalf + Ramp * 0.5f)), new Vector3(0.2f, 0.2f, len), white, new Vector3(s * rise, 0f, 0f), collider: false);
            // the stop line before the gate
            PrimitiveKit.Box("StopLine", c, new Vector3(s * LaneOffset, 0.045f, s * (DeckHalf + Ramp + 3.2f)), new Vector3(HalfWidth - 0.3f, 0.005f, 0.35f), white, collider: false);
        }

        var red = PrimitiveKit.Mat("SignRed", new Color(0.75f, 0.12f, 0.1f));
        var whitePaint = PrimitiveKit.Mat("SignWhite", Color.white);
        var steel = PrimitiveKit.Mat("SignalGrey", new Color(0.3f, 0.3f, 0.32f));
        var dark = PrimitiveKit.Mat("WindowDark", new Color(0.12f, 0.1f, 0.09f));
        var lamp = PrimitiveKit.Glow("CrossingRed", new Color(1f, 0.12f, 0.08f), 2.5f);
        var arms = new List<Transform>();
        var lampsA = new List<GameObject>();
        var lampsB = new List<GameObject>();
        var blocks = new List<Collider>();

        // A gate on each approach, on the driver's left (traffic keeps left): northbound traffic comes
        // up the west half of the leg, southbound down the east half
        foreach (float s in new[] { -1f, 1f }) // -1 the south approach, +1 the north
        {
            float side = s;                                      // the lane: west half going north (from the south), east half going south
            float gz = s * (DeckHalf + Ramp + 2.4f);             // the gate line, beyond the foot of the ramp
            var post = PrimitiveKit.Group("Gate", c, new Vector3(side * (HalfWidth + 0.6f), 0f, gz), side < 0f ? 0f : 180f).transform;
            PrimitiveKit.Box("Post", post, new Vector3(0f, 1.6f, 0f), new Vector3(0.18f, 3.2f, 0.18f), steel, collider: false);
            PrimitiveKit.Box("Base", post, new Vector3(0f, 0.5f, 0f), new Vector3(0.55f, 1f, 0.45f), whitePaint);
            // the barrier arm: pivot on the post, reaching across the lane (local +X), striped
            var pivot = PrimitiveKit.Group("ArmPivot", post, new Vector3(0f, 1f, 0f)).transform;
            pivot.localRotation = Quaternion.Euler(0f, 0f, 85f);
            for (int i = 0; i < 6; i++)
                PrimitiveKit.Box("Arm", pivot, new Vector3(0.45f + i * 0.55f, 0f, 0f), new Vector3(0.55f, 0.12f, 0.06f), i % 2 == 0 ? red : whitePaint, collider: false);
            arms.Add(pivot);
            // the crossbuck (white X, red edge) and two red lamps that flash in turn
            foreach (float a in new[] { 35f, -35f })
            {
                PrimitiveKit.Box("CrossbuckEdge", post, new Vector3(0f, 3.05f, -0.12f), new Vector3(1.5f, 0.26f, 0.03f), red, new Vector3(0f, 0f, a), collider: false);
                PrimitiveKit.Box("Crossbuck", post, new Vector3(0f, 3.05f, -0.14f), new Vector3(1.4f, 0.18f, 0.03f), whitePaint, new Vector3(0f, 0f, a), collider: false);
            }
            PrimitiveKit.Label("ระวังรถไฟ", post, new Vector3(0f, 2.55f, -0.16f), 0.025f, new Color(0.75f, 0.1f, 0.08f));
            PrimitiveKit.Box("LampBar", post, new Vector3(0f, 2.2f, -0.1f), new Vector3(0.9f, 0.08f, 0.06f), steel, collider: false);
            foreach (float lx in new[] { -0.35f, 0.35f })
            {
                PrimitiveKit.Cylinder("LampHood", post, new Vector3(lx, 2.2f, -0.12f), new Vector3(0.3f, 0.06f, 0.3f), dark, new Vector3(90f, 0f, 0f), collider: false);
                var on = PrimitiveKit.Sphere("Lamp", post, new Vector3(lx, 2.2f, -0.2f), Vector3.one * 0.2f, lamp);
                on.SetActive(false);
                (lx < 0f ? lampsA : lampsB).Add(on);
            }
            // what the traffic stops for while the arm is down (the lane only, so cars already over can leave)
            var block = new GameObject("GateBlock");
            block.transform.SetParent(c, false);
            block.transform.localPosition = new Vector3(side * LaneOffset, 1f, gz);
            var box = block.AddComponent<BoxCollider>();
            box.size = new Vector3(HalfWidth - 0.2f, 2f, 0.3f);
            box.enabled = false;
            block.AddComponent<TrafficBlock>();
            blocks.Add(box);
            // the yellow warning sign some way back
            var warn = PrimitiveKit.Group("CrossingAhead", c, new Vector3(side * (HalfWidth + 0.8f), 0f, s * (DeckHalf + Ramp + 16f)), side < 0f ? 0f : 180f).transform;
            PrimitiveKit.Box("Post", warn, new Vector3(0f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.08f), steel, collider: false);
            PrimitiveKit.Box("Diamond", warn, new Vector3(0f, 2.2f, -0.05f), new Vector3(0.75f, 0.75f, 0.03f), PrimitiveKit.Mat("WarnYellow", new Color(1f, 0.82f, 0.1f)), new Vector3(0f, 0f, 45f), collider: false);
            PrimitiveKit.Label("ทางรถไฟ", warn, new Vector3(0f, 2.2f, -0.08f), 0.02f, Color.black);
        }
        c.gameObject.AddComponent<LevelCrossing>().Configure(railway, x, arms.ToArray(), lampsA.ToArray(), lampsB.ToArray(), blocks.ToArray());
    }

    private static void Sign(Transform t, Vector3 at, float yaw, string text)
    {
        var g = PrimitiveKit.Group("StreetSign_" + text, t, at, yaw).transform;
        PrimitiveKit.Cylinder("Post", g, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("Plate", g, new Vector3(0f, 2.45f, 0f), new Vector3(2.6f, 0.42f, 0.04f), PrimitiveKit.Mat("StreetSignBlue", new Color(0.1f, 0.3f, 0.65f)), collider: false);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, -0.03f), 0.03f, Color.white);
        PrimitiveKit.Label(text, g, new Vector3(0f, 2.45f, 0.03f), 0.03f, Color.white, 180f);
    }
}
