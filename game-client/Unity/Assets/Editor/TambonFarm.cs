using UnityEngine;

// Farming round the tambon (TambonCountry): the paddies south-east of the crossroads, each plot at a
// different stage — ploughed mud, flooded for planting, rice cut, rice ripe — with the machines that go
// with them, some at work (FarmWork, with a driver: FarmDriver) and some parked; a machinery shed by
// the side road; and the traffic on the tambon roads (TrafficCar): farm trucks (อีแต๋น), tractors,
// pickups and a songthaew.
//   รถไถ (four-wheel tractor + rotary tiller) · ควายเหล็ก (walking tractor) · อีแต๋น (farm truck)
//   รถนวดข้าว (threshing machine) · รถเกี่ยวข้าว (combine harvester)
public static class TambonFarm
{
    private const float RoadX = TambonCountry.RoadX;
    private const float X0 = RoadX + 5f, X1 = 98f, Z0 = -92f, Z1 = -18f;
    private const int Rows = 4;

    private static Material M(string name, Color c, float smooth = 0.35f, float metal = 0f) => PrimitiveKit.Mat("Farm" + name, c, smooth, metal);
    private static Material Tyre => M("Tyre", new Color(0.08f, 0.08f, 0.09f), 0.2f);
    private static Material Steel => M("Steel", new Color(0.35f, 0.36f, 0.38f), 0.5f, 0.6f);
    private static Material Black => M("Black", new Color(0.1f, 0.1f, 0.11f), 0.3f);
    private static Material Seat => M("Seat", new Color(0.12f, 0.12f, 0.12f), 0.4f);
    private static Material Glass => M("Glass", new Color(0.55f, 0.7f, 0.78f), 0.95f, 0.2f);

    public static void Build(Transform t)
    {
        var farm = PrimitiveKit.Group("Farm_ทุ่งนา", t, Vector3.zero).transform;
        Plots(farm, out var plot);
        Machines(farm, plot);
        Shed(farm);
        RoadTraffic(t);
    }

    // ── the plots ──────────────────────────────────────────────────────────────
    // plot[r] = (centre, inner size); r 0 is the south one
    private static void Plots(Transform p, out (Vector3 c, Vector2 s)[] plot)
    {
        var dike = M("Dike", new Color(0.5f, 0.42f, 0.3f), 0.1f);
        var look = new[]
        {
            M("PaddyMud", new Color(0.36f, 0.27f, 0.18f), 0.25f),         // just ploughed
            M("PaddyWater", new Color(0.38f, 0.42f, 0.34f), 0.85f),       // flooded for planting
            M("PaddyStubble", new Color(0.66f, 0.58f, 0.34f), 0.1f),      // cut: stubble
            M("PaddyRipe", new Color(0.86f, 0.72f, 0.26f), 0.1f),         // ripe, golden
        };
        float w = X1 - X0, rh = (Z1 - Z0) / Rows;
        plot = new (Vector3, Vector2)[Rows];
        for (int r = 0; r < Rows; r++)
        {
            var c = new Vector3((X0 + X1) * 0.5f, 0f, Z0 + rh * (r + 0.5f));
            var s = new Vector2(w - 0.8f, rh - 0.8f);
            plot[r] = (c, s);
            if (r == 3) // the ripe rice stands tall, except the strip already cut on the west side
                PrimitiveKit.Box("Plot", p, new Vector3(c.x + 3f, 0.16f, c.z), new Vector3(s.x - 6f, 0.32f, s.y), look[r], collider: false);
            else
                PrimitiveKit.Box("Plot", p, new Vector3(c.x, 0.03f, c.z), new Vector3(s.x, 0.04f, s.y), look[r], collider: false);
        }
        // ripe plot: a strip along the west side already cut where the harvester has been
        var (rc, rs) = plot[3];
        PrimitiveKit.Box("CutStrip", p, new Vector3(X0 + 0.4f + 3f, 0.035f, rc.z), new Vector3(6f, 0.05f, rs.y), look[2], collider: false);
        // seedling bundles waiting on the dike of the flooded plot, a few rows already planted
        var seedling = M("Seedling", new Color(0.45f, 0.72f, 0.3f), 0.1f);
        var (wc, ws) = plot[1];
        for (int row = 0; row < 6; row++)
            for (float z = wc.z - ws.y * 0.5f + 0.6f; z < wc.z + ws.y * 0.5f - 0.5f; z += 0.8f)
                PrimitiveKit.Box("Seedling", p, new Vector3(X1 - 1.2f - row * 0.6f, 0.12f, z), new Vector3(0.12f, 0.2f, 0.12f), seedling, collider: false);
        for (int r = 0; r <= Rows; r++)
            PrimitiveKit.Box("Dike", p, new Vector3((X0 + X1) * 0.5f, 0.12f, Z0 + rh * r), new Vector3(w, 0.24f, 0.8f), dike, collider: false);
        foreach (float x in new[] { X0, X1 })
            PrimitiveKit.Box("Dike", p, new Vector3(x, 0.12f, (Z0 + Z1) * 0.5f), new Vector3(0.8f, 0.24f, Z1 - Z0), dike, collider: false);
        Places.Sign(p, new Vector3(RoadX + 3.4f, 0f, -24f), "ทุ่งนา", 270f);
        Places.ShadeTree(p, new Vector3(RoadX + 5f, 0f, -96f), 1.2f);
        // a field hut (เถียงนา) on stilts at the corner of the cut plot, for the farmers' lunch
        var hut = PrimitiveKit.Group("FieldHut_เถียงนา", p, new Vector3(X1 - 2.6f, 0f, plot[2].c.z + 4f), 270f).transform;
        var wood = M("HutWood", new Color(0.45f, 0.32f, 0.2f), 0.2f);
        foreach (float hx in new[] { -1.2f, 1.2f })
            foreach (float hz in new[] { -1f, 1f })
                PrimitiveKit.Box("Post", hut, new Vector3(hx, 1.2f, hz), new Vector3(0.14f, 2.4f, 0.14f), wood);
        PrimitiveKit.Box("Floor", hut, new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 0.1f, 2.4f), wood);
        PrimitiveKit.Box("Roof", hut, new Vector3(0f, 2.6f, 0f), new Vector3(3.2f, 0.12f, 2.8f), M("Thatch", new Color(0.62f, 0.5f, 0.3f), 0.1f), new Vector3(8f, 0f, 0f), collider: false);
    }

    // ── machines at work and parked in the fields ───────────────────────────────────────
    private static void Machines(Transform p, (Vector3 c, Vector2 s)[] plot)
    {
        // รถไถ ploughing the mud plot
        var (mc, ms) = plot[0];
        var tractor = Tractor(p, new Vector3(mc.x - ms.x * 0.5f + 2f, 0f, mc.z), 0f, 0, tiller: true);
        Work(tractor, mc, ms - new Vector2(3f, 5f), 3.2f, 1.6f);
        tractor.gameObject.AddComponent<FarmDriver>().Configure(TractorSeat, 3);

        // ควายเหล็ก in the flooded plot (leaving the planted rows alone)
        var (wc, ws) = plot[1];
        var area = new Vector2(ws.x - 7f, ws.y - 4f);
        var walker = WalkingTractor(p, new Vector3(wc.x - ws.x * 0.5f + 2f, 0f, wc.z), 0f);
        Work(walker, new Vector3(wc.x - 2.8f, 0f, wc.z), area, 2.2f, 1.1f);
        walker.gameObject.AddComponent<FarmDriver>().Configure(WalkerSeat, 8);

        // รถเกี่ยวข้าว cutting the ripe plot (from the cut strip eastwards)
        var (rc, rs) = plot[3];
        var combine = Harvester(p, new Vector3(X0 + 7.5f, 0f, rc.z), 0f, 0);
        Work(combine, new Vector3(rc.x + 2.5f, 0f, rc.z), new Vector2(rs.x - 7f, rs.y - 8f), 3f, 1.3f);
        combine.gameObject.AddComponent<FarmDriver>().Configure(HarvesterSeat, 5);

        // รถนวดข้าว at work on the cut plot, an อีแต๋น parked by it loaded with sacks, farmers round them
        var (sc, ss) = plot[2];
        var thresher = Thresher(p, new Vector3(X0 + 5f, 0f, sc.z - 2f), 270f); // its straw pipe blows east over the cut plot
        Work(thresher, Vector3.zero, Vector2.zero, 0f, 0f, straw: true);
        Etan(p, new Vector3(X0 + 4f, 0f, sc.z + 5f), 90f, 1, sacks: true);
        Npcs.Crowd(p, new Vector3(X0 + 6f, 0f, sc.z - 7.8f), new Vector2(5f, 2f), 3, "", 7, 17, "เกี่ยวข้าวเสร็จแล้ว|ปีนี้ข้าวงามนะ|นวดข้าวกันเถอะ|ขายได้กี่บาทเกวียนนี้?");

        // an old ลอมฟาง from last year: straw stacked round a pole into a cone
        var lom = PrimitiveKit.Group("StrawCone_ลอมฟาง", p, new Vector3(X0 + 16f, 0f, sc.z + 5.5f)).transform;
        var old = M("StrawStackOld", new Color(0.74f, 0.6f, 0.34f), 0.05f);
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Cylinder("Ring", lom, new Vector3(0f, 0.3f + i * 0.5f, 0f), new Vector3(3.2f - i * 0.5f, 0.3f, 3.2f - i * 0.5f), old, collider: false);
        PrimitiveKit.Cylinder("Pole", lom, new Vector3(0f, 3f, 0f), new Vector3(0.08f, 1.4f, 0.08f), M("HutWood", new Color(0.45f, 0.32f, 0.2f), 0.2f), collider: false);
    }

    private static void Work(Transform v, Vector3 c, Vector2 s, float lane, float speed, bool straw = false)
    {
        var rb = v.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        v.gameObject.AddComponent<FarmWork>().Configure(c, s, lane, speed, straw);
    }

    // ── machinery shed by the side road, with what isn't out working today ─────────────────────
    private static void Shed(Transform p)
    {
        var s = PrimitiveKit.Group("MachineShed_โรงเก็บรถ", p, new Vector3(RoadX - 9f, 0f, -52f), 90f).transform; // open side faces the road
        var post = M("ShedPost", new Color(0.5f, 0.52f, 0.55f), 0.4f, 0.5f);
        var roof = M("ShedRoof", new Color(0.62f, 0.66f, 0.7f), 0.6f, 0.6f);
        PrimitiveKit.Box("Floor", s, new Vector3(0f, 0.02f, 0f), new Vector3(16f, 0.04f, 8f), M("ShedFloor", new Color(0.6f, 0.58f, 0.54f), 0.2f), collider: false);
        foreach (float x in new[] { -7.8f, -2.6f, 2.6f, 7.8f })
            foreach (float z in new[] { -3.8f, 3.8f })
                PrimitiveKit.Box("Post", s, new Vector3(x, 1.9f, z), new Vector3(0.18f, 3.8f, 0.18f), post);
        PrimitiveKit.Box("Roof", s, new Vector3(0f, 3.9f, 0f), new Vector3(16.6f, 0.1f, 8.8f), roof, new Vector3(-5f, 0f, 0f), collider: false);
        PrimitiveKit.Box("BackWall", s, new Vector3(0f, 1.2f, -3.9f), new Vector3(16f, 2.4f, 0.1f), roof);
        Tractor(s, new Vector3(-5f, 0f, 0.4f), 0f, 1, tiller: false);
        Harvester(s, new Vector3(1f, 0f, 0f), 0f, 1);
        WalkingTractor(s, new Vector3(6f, 0f, 0.6f), 0f);
        for (int i = 0; i < 4; i++) // fuel drums and sacks of fertiliser at the back
            PrimitiveKit.Cylinder("Drum", s, new Vector3(-7f + i * 0.7f, 0.45f, -3.2f), new Vector3(0.58f, 0.45f, 0.58f), M("Drum", i % 2 == 0 ? new Color(0.2f, 0.35f, 0.6f) : new Color(0.7f, 0.2f, 0.15f), 0.5f, 0.4f));
        Places.Sign(p, new Vector3(RoadX - 2.6f, 0f, -44f), "โรงเก็บรถไถ · รถเกี่ยวข้าว", 90f);
        Etan(p, new Vector3(RoadX - 4.2f, 0f, 46f), 0f, 2, sacks: false); // parked by the road up near the river
    }

    // ── traffic on the main road and the side road ───────────────────────────────────────
    private static void RoadTraffic(Transform t)
    {
        var traffic = PrimitiveKit.Group("Traffic_รถสัญจรตำบล", t, Vector3.zero).transform;
        // keep left: eastbound on the north half of the main road, northbound on the west half of the side road
        var main = new[] { new Vector3(-96f, 0f, -7f), new Vector3(96f, 0f, -7f), new Vector3(96f, 0f, -9f), new Vector3(-96f, 0f, -9f) };
        var side = new[] { new Vector3(RoadX - 0.9f, 0f, -94f), new Vector3(RoadX - 0.9f, 0f, 62f), new Vector3(RoadX + 0.9f, 0f, 62f), new Vector3(RoadX + 0.9f, 0f, -94f) };

        void Drive(Transform v, Vector3[] path, float start, float speed, float halfLen)
        {
            v.gameObject.AddComponent<TrafficCar>().Configure(path, true, speed, halfLen, start, null, null, null);
            var body = v.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            v.position = traffic.TransformPoint(path[0]);
        }
        Transform City(string kind, int variant, out float halfLen, out float speed) => CityTraffic.Vehicle(traffic, kind, variant, out halfLen, out speed);

        int look = 11;
        Transform Farm(string kind, int variant)
        {
            Transform v = kind == "etan" ? Etan(traffic, Vector3.zero, 0f, variant, sacks: variant % 2 == 0) : Tractor(traffic, Vector3.zero, 0f, variant, tiller: false);
            v.gameObject.AddComponent<FarmDriver>().Configure(kind == "etan" ? EtanSeat : TractorSeat, look++);
            return v;
        }

        Drive(Farm("etan", 0), main, 10f, 5.5f, 2.3f);
        { var v = City("sedan", 0, out float h, out float sp); Drive(v, main, 75f, 9f, h); }
        Drive(Farm("tractor", 0), main, 140f, 4f, 2.2f);
        { var v = City("songthaew", 0, out float h, out float sp); Drive(v, main, 205f, 8f, h); }
        Drive(Farm("etan", 1), main, 270f, 5.5f, 2.3f);
        { var v = City("sedan", 1, out float h, out float sp); Drive(v, main, 335f, 9f, h); }

        Drive(Farm("etan", 2), side, 20f, 5.5f, 2.3f);
        Drive(Farm("tractor", 1), side, 120f, 4f, 2.2f);
        { var v = City("sedan", 0, out float h, out float sp); Drive(v, side, 220f, 9f, h); }
    }

    // ── the machines (all face +Z, wheels named "Wheel" so FarmWork/TrafficCar turn them) ─────────────
    private static readonly Vector3 TractorSeat = new Vector3(0f, 1.28f, -0.55f);
    private static readonly Vector3 EtanSeat = new Vector3(0f, 0.98f, 0.55f);
    private static readonly Vector3 WalkerSeat = new Vector3(0f, 0.6f, -2.05f);
    private static readonly Vector3 HarvesterSeat = new Vector3(-0.45f, 1.45f, 1.05f);

    private static void Wheel(Transform v, Vector3 at, float diameter, float width, Material rim)
    {
        var w = new GameObject("Wheel").transform;
        w.SetParent(v, false);
        w.localPosition = at;
        w.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PrimitiveKit.Cylinder("Tyre", w, Vector3.zero, new Vector3(diameter, width / 2f, diameter), Tyre, collider: false);
        PrimitiveKit.Cylinder("Rim", w, Vector3.zero, new Vector3(diameter * 0.55f, width / 2f + 0.01f, diameter * 0.55f), rim, collider: false);
        PrimitiveKit.Box("Spoke", w, Vector3.zero, new Vector3(diameter * 0.5f, width + 0.03f, diameter * 0.1f), Black, collider: false);
    }

    private static Transform Body(Transform parent, string name, Vector3 at, float yaw, Vector3 size)
    {
        var v = PrimitiveKit.Group(name, parent, at, yaw).transform;
        var box = v.gameObject.AddComponent<BoxCollider>();
        box.size = size;
        box.center = new Vector3(0f, size.y * 0.5f, 0f);
        return v;
    }

    // อีแต๋น: the home-built farm truck — an open single-cylinder engine with its flywheel up front over
    // a small wheel, tiller-style handlebars, a bench seat, and a wooden cargo bed on two big wheels
    private static Transform Etan(Transform parent, Vector3 at, float yaw, int variant, bool sacks)
    {
        Color[] beds = { new Color(0.3f, 0.5f, 0.65f), new Color(0.55f, 0.38f, 0.22f), new Color(0.35f, 0.55f, 0.35f) };
        var v = Body(parent, "Vehicle_etan_อีแต๋น", at, yaw, new Vector3(1.9f, 1.6f, 4.6f));
        var bed = M("EtanBed" + variant % 3, beds[variant % 3], 0.25f);
        var engine = M("EtanEngine", new Color(0.2f, 0.42f, 0.25f), 0.5f, 0.3f);
        var red = M("EtanRed", new Color(0.75f, 0.15f, 0.12f), 0.5f, 0.3f);
        PrimitiveKit.Box("Chassis", v, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.14f, 4.3f), Steel, collider: false);
        // the engine up front: block, radiator, flywheel, exhaust
        PrimitiveKit.Box("Engine", v, new Vector3(0f, 0.85f, 1.75f), new Vector3(0.55f, 0.5f, 0.8f), engine, collider: false);
        PrimitiveKit.Box("Radiator", v, new Vector3(0f, 1.05f, 2.15f), new Vector3(0.5f, 0.45f, 0.14f), red, collider: false);
        PrimitiveKit.Cylinder("Flywheel", v, new Vector3(0.38f, 0.9f, 1.6f), new Vector3(0.55f, 0.04f, 0.55f), red, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Cylinder("Exhaust", v, new Vector3(-0.2f, 1.35f, 1.6f), new Vector3(0.06f, 0.3f, 0.06f), Black, collider: false);
        PrimitiveKit.Box("Lamp", v, new Vector3(0f, 1.35f, 2.2f), new Vector3(0.2f, 0.16f, 0.1f), PrimitiveKit.Glow("CarHeadlight", new Color(1f, 0.97f, 0.88f), 1.8f), collider: false);
        Wheel(v, new Vector3(0f, 0.32f, 1.95f), 0.64f, 0.2f, red);
        // handlebars and the seat
        PrimitiveKit.Box("Column", v, new Vector3(0f, 1.1f, 1.15f), new Vector3(0.06f, 0.7f, 0.06f), Steel, new Vector3(-25f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Handlebar", v, new Vector3(0f, 1.42f, 1.0f), new Vector3(0.9f, 0.05f, 0.05f), Steel, collider: false);
        PrimitiveKit.Box("Seat", v, new Vector3(0f, 0.88f, 0.55f), new Vector3(1.2f, 0.12f, 0.45f), Seat, collider: false);
        // the bed
        PrimitiveKit.Box("BedFloor", v, new Vector3(0f, 0.85f, -1.05f), new Vector3(1.8f, 0.08f, 2.5f), bed, collider: false);
        foreach (float x in new[] { -0.88f, 0.88f })
            PrimitiveKit.Box("BedSide", v, new Vector3(x, 1.15f, -1.05f), new Vector3(0.06f, 0.55f, 2.5f), bed, collider: false);
        PrimitiveKit.Box("BedFront", v, new Vector3(0f, 1.15f, 0.2f), new Vector3(1.8f, 0.55f, 0.06f), bed, collider: false);
        PrimitiveKit.Box("BedBack", v, new Vector3(0f, 1.15f, -2.3f), new Vector3(1.8f, 0.55f, 0.06f), bed, collider: false);
        foreach (float x in new[] { -0.95f, 0.95f })
            Wheel(v, new Vector3(x, 0.42f, -1.2f), 0.84f, 0.24f, Steel);
        PrimitiveKit.Box("Taillight", v, new Vector3(-0.7f, 0.95f, -2.34f), new Vector3(0.2f, 0.1f, 0.04f), PrimitiveKit.Glow("CarTaillight", new Color(1f, 0.12f, 0.1f), 1.5f), collider: false);
        if (sacks)
        {
            var sack = M("RiceSack", new Color(0.9f, 0.88f, 0.8f), 0.2f);
            for (int i = 0; i < 6; i++)
                PrimitiveKit.Box("Sack", v, new Vector3(-0.42f + (i % 2) * 0.84f, 1.1f + (i / 4) * 0.35f, -0.4f - (i / 2 % 2) * 0.9f - (i / 4) * 0.4f), new Vector3(0.7f, 0.34f, 0.8f), sack, new Vector3(0f, (i * 13) % 20 - 10f, 0f), collider: false);
        }
        return v;
    }

    // รถไถ: a four-wheel tractor — big rear wheels with fenders, a long bonnet, an upright exhaust, a sun
    // canopy over the seat, and (in the field) a rotary tiller behind with its blades turning
    private static Transform Tractor(Transform parent, Vector3 at, float yaw, int variant, bool tiller)
    {
        Color[] paints = { new Color(0.88f, 0.42f, 0.1f), new Color(0.2f, 0.4f, 0.7f) };
        var v = Body(parent, "Vehicle_tractor_รถไถ", at, yaw, new Vector3(2.1f, 2.4f, tiller ? 4.6f : 3.6f));
        var paint = M("TractorPaint" + variant % 2, paints[variant % 2], 0.6f, 0.2f);
        var cream = M("TractorCream", new Color(0.92f, 0.9f, 0.82f), 0.5f);
        PrimitiveKit.Box("Bonnet", v, new Vector3(0f, 1.1f, 0.95f), new Vector3(0.8f, 0.65f, 1.6f), paint, collider: false);
        PrimitiveKit.Box("Grille", v, new Vector3(0f, 1.05f, 1.76f), new Vector3(0.7f, 0.55f, 0.04f), Black, collider: false);
        foreach (float x in new[] { -0.25f, 0.25f })
            PrimitiveKit.Box("Headlight", v, new Vector3(x, 1.3f, 1.78f), new Vector3(0.16f, 0.12f, 0.03f), PrimitiveKit.Glow("CarHeadlight", new Color(1f, 0.97f, 0.88f), 1.8f), collider: false);
        PrimitiveKit.Box("Body", v, new Vector3(0f, 0.95f, -0.45f), new Vector3(0.9f, 0.6f, 1.2f), paint, collider: false);
        PrimitiveKit.Cylinder("Exhaust", v, new Vector3(0.3f, 1.85f, 1.3f), new Vector3(0.08f, 0.45f, 0.08f), Black, collider: false);
        foreach (float x in new[] { -1f, 1f })
        {
            Wheel(v, new Vector3(x * 0.85f, 0.65f, -0.55f), 1.3f, 0.4f, cream);
            PrimitiveKit.Box("Fender", v, new Vector3(x * 0.85f, 1.38f, -0.55f), new Vector3(0.48f, 0.06f, 1.1f), paint, collider: false);
            Wheel(v, new Vector3(x * 0.68f, 0.36f, 1.25f), 0.72f, 0.22f, cream);
            PrimitiveKit.Box("CanopyPost", v, new Vector3(x * 0.6f, 1.95f, -1.1f), new Vector3(0.06f, 1.3f, 0.06f), Black, collider: false);
        }
        PrimitiveKit.Box("Seat", v, new Vector3(0f, 1.2f, -0.55f), new Vector3(0.5f, 0.12f, 0.45f), Seat, collider: false);
        PrimitiveKit.Box("SeatBack", v, new Vector3(0f, 1.45f, -0.8f), new Vector3(0.5f, 0.4f, 0.08f), Seat, collider: false);
        PrimitiveKit.Cylinder("SteeringWheel", v, new Vector3(0f, 1.6f, -0.05f), new Vector3(0.42f, 0.02f, 0.42f), Black, new Vector3(-50f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Canopy", v, new Vector3(0f, 2.62f, -0.6f), new Vector3(1.5f, 0.06f, 1.5f), cream, collider: false);
        if (tiller)
        {
            PrimitiveKit.Box("Hitch", v, new Vector3(0f, 0.75f, -1.45f), new Vector3(0.2f, 0.12f, 0.6f), Steel, collider: false);
            PrimitiveKit.Box("TillerHood", v, new Vector3(0f, 0.62f, -1.95f), new Vector3(1.9f, 0.08f, 0.8f), paint, collider: false);
            PrimitiveKit.Box("TillerFlap", v, new Vector3(0f, 0.4f, -2.35f), new Vector3(1.9f, 0.4f, 0.05f), paint, collider: false);
            var spin = new GameObject("Spin").transform;
            spin.SetParent(v, false);
            spin.localPosition = new Vector3(0f, 0.32f, -1.95f);
            PrimitiveKit.Cylinder("Shaft", spin, Vector3.zero, new Vector3(0.12f, 0.92f, 0.12f), Steel, new Vector3(0f, 0f, 90f), collider: false);
            for (int i = 0; i < 6; i++)
                PrimitiveKit.Box("Blade", spin, new Vector3(-0.8f + i * 0.32f, 0f, 0f), new Vector3(0.04f, 0.5f, 0.1f), Steel, new Vector3(i * 60f, 0f, 0f), collider: false);
        }
        return v;
    }

    // ควายเหล็ก: the walking tractor — engine over two cage wheels for the mud, long handles back to a
    // little sled seat, with a plough under it
    private static Transform WalkingTractor(Transform parent, Vector3 at, float yaw)
    {
        var v = Body(parent, "Vehicle_walker_ควายเหล็ก", at, yaw, new Vector3(1.3f, 1.3f, 3.4f));
        var paint = M("WalkerPaint", new Color(0.82f, 0.16f, 0.12f), 0.5f, 0.2f);
        PrimitiveKit.Box("Engine", v, new Vector3(0f, 0.95f, 0.35f), new Vector3(0.45f, 0.45f, 0.7f), paint, collider: false);
        PrimitiveKit.Box("Radiator", v, new Vector3(0f, 1.0f, 0.75f), new Vector3(0.4f, 0.4f, 0.1f), Black, collider: false);
        PrimitiveKit.Cylinder("Flywheel", v, new Vector3(0.3f, 0.95f, 0.3f), new Vector3(0.45f, 0.03f, 0.45f), paint, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Gearbox", v, new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.6f, 0.4f), paint, collider: false);
        foreach (float x in new[] { -0.5f, 0.5f })
        {
            Wheel(v, new Vector3(x, 0.45f, 0f), 0.9f, 0.22f, Steel);
            PrimitiveKit.Box("Handle", v, new Vector3(x * 0.6f, 1.0f, -0.85f), new Vector3(0.05f, 0.05f, 1.5f), Steel, new Vector3(-8f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("Grip", v, new Vector3(0f, 1.1f, -1.55f), new Vector3(0.7f, 0.05f, 0.05f), Black, collider: false);
        PrimitiveKit.Box("Drawbar", v, new Vector3(0f, 0.35f, -1.2f), new Vector3(0.08f, 0.08f, 1.8f), Steel, collider: false);
        PrimitiveKit.Box("Sled", v, new Vector3(0f, 0.2f, -2.05f), new Vector3(0.6f, 0.08f, 0.9f), M("SledWood", new Color(0.45f, 0.32f, 0.2f), 0.2f), collider: false);
        PrimitiveKit.Box("SledSeat", v, new Vector3(0f, 0.52f, -2.05f), new Vector3(0.4f, 0.1f, 0.35f), Seat, collider: false);
        PrimitiveKit.Box("SeatPost", v, new Vector3(0f, 0.36f, -2.05f), new Vector3(0.08f, 0.3f, 0.08f), Steel, collider: false);
        PrimitiveKit.Box("Plough", v, new Vector3(0f, 0.12f, -1.5f), new Vector3(0.4f, 0.2f, 0.3f), Steel, new Vector3(0f, 30f, 0f), collider: false);
        return v;
    }

    // รถเกี่ยวข้าว: a combine harvester on rubber tracks — the cutting header with its turning reel up
    // front, a glass cab on the left, the threshing body and the grain tank behind, the unloading auger
    private static Transform Harvester(Transform parent, Vector3 at, float yaw, int variant)
    {
        var v = Body(parent, "Vehicle_harvester_รถเกี่ยวข้าว", at, yaw, new Vector3(2.8f, 3f, 5.8f));
        var paint = M("HarvesterPaint" + variant % 2, variant % 2 == 0 ? new Color(0.85f, 0.35f, 0.12f) : new Color(0.25f, 0.5f, 0.3f), 0.55f, 0.2f);
        var white = M("HarvesterWhite", new Color(0.93f, 0.93f, 0.9f), 0.55f);
        foreach (float x in new[] { -0.9f, 0.9f }) // the tracks
        {
            PrimitiveKit.Box("Track", v, new Vector3(x, 0.32f, -0.2f), new Vector3(0.5f, 0.6f, 3.2f), Tyre, collider: false);
            foreach (float z in new[] { -1.8f, 1.4f })
                PrimitiveKit.Cylinder("TrackEnd", v, new Vector3(x, 0.32f, z), new Vector3(0.6f, 0.25f, 0.6f), Tyre, new Vector3(0f, 0f, 90f), collider: false);
        }
        PrimitiveKit.Box("Body", v, new Vector3(0.3f, 1.35f, -0.6f), new Vector3(1.9f, 1.4f, 2.8f), paint, collider: false);
        PrimitiveKit.Box("GrainTank", v, new Vector3(0.45f, 2.4f, -0.9f), new Vector3(1.5f, 0.7f, 1.8f), white, collider: false);
        PrimitiveKit.Cylinder("Auger", v, new Vector3(-0.5f, 2.55f, -0.6f), new Vector3(0.22f, 1.4f, 0.22f), paint, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("Exhaust", v, new Vector3(1.0f, 2.6f, -1.8f), new Vector3(0.1f, 0.4f, 0.1f), Black, collider: false);
        // the cab
        PrimitiveKit.Box("CabFloor", v, new Vector3(-0.45f, 0.95f, 1.0f), new Vector3(1.1f, 0.6f, 1.2f), paint, collider: false);
        PrimitiveKit.Box("Cab", v, new Vector3(-0.45f, 1.85f, 1.0f), new Vector3(1.1f, 1.2f, 1.2f), Glass, collider: false);
        PrimitiveKit.Box("CabRoof", v, new Vector3(-0.45f, 2.5f, 1.0f), new Vector3(1.25f, 0.1f, 1.35f), white, collider: false);
        PrimitiveKit.Box("Headlight", v, new Vector3(-0.45f, 2.42f, 1.7f), new Vector3(0.6f, 0.1f, 0.04f), PrimitiveKit.Glow("CarHeadlight", new Color(1f, 0.97f, 0.88f), 1.8f), collider: false);
        // the header: platform, side walls, cutter bar and the reel
        PrimitiveKit.Box("Feeder", v, new Vector3(0.3f, 0.8f, 1.6f), new Vector3(0.9f, 0.5f, 1.0f), paint, new Vector3(20f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Header", v, new Vector3(0f, 0.35f, 2.45f), new Vector3(2.7f, 0.12f, 1.1f), paint, collider: false);
        PrimitiveKit.Box("HeaderBack", v, new Vector3(0f, 0.65f, 1.95f), new Vector3(2.7f, 0.6f, 0.08f), paint, collider: false);
        foreach (float x in new[] { -1.33f, 1.33f })
        {
            PrimitiveKit.Box("Divider", v, new Vector3(x, 0.55f, 2.5f), new Vector3(0.06f, 0.5f, 1.2f), paint, collider: false);
            PrimitiveKit.Box("ReelArm", v, new Vector3(x, 1.05f, 2.35f), new Vector3(0.06f, 0.06f, 1.0f), Steel, collider: false);
        }
        PrimitiveKit.Box("CutterBar", v, new Vector3(0f, 0.28f, 3.0f), new Vector3(2.7f, 0.06f, 0.08f), Steel, collider: false);
        var reel = new GameObject("Spin").transform;
        reel.SetParent(v, false);
        reel.localPosition = new Vector3(0f, 1.05f, 2.75f);
        PrimitiveKit.Cylinder("ReelShaft", reel, Vector3.zero, new Vector3(0.08f, 1.3f, 0.08f), Steel, new Vector3(0f, 0f, 90f), collider: false);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            PrimitiveKit.Box("Bat", reel, new Vector3(0f, Mathf.Sin(a) * 0.42f, Mathf.Cos(a) * 0.42f), new Vector3(2.5f, 0.05f, 0.05f), white, collider: false);
        }
        return v;
    }

    // รถนวดข้าว: the threshing machine as it works in a Thai field — a long steel body on two wheels with a
    // drawbar, the round drum hood on top, its engine up front driving the drum by a belt, a feed conveyor
    // up the left side that the sheaves go in by, the grain spout filling sacks on the right, and the
    // blower at the back sending the straw up a tall pipe and out in an arc onto the straw stack (Pile)
    // it is building. FarmWork shakes the "Shake" part, turns the drum, blows the straw and the dust.
    private static Transform Thresher(Transform parent, Vector3 at, float yaw)
    {
        var v = Body(parent, "Machine_thresher_รถนวดข้าว", at, yaw, new Vector3(2.4f, 3f, 5.4f));
        var paint = M("ThresherPaint", new Color(0.16f, 0.45f, 0.3f), 0.45f, 0.3f);
        var yellow = M("ThresherYellow", new Color(0.95f, 0.75f, 0.15f), 0.5f, 0.2f);
        var rust = M("ThresherRust", new Color(0.45f, 0.3f, 0.2f), 0.2f, 0.2f);
        var sh = PrimitiveKit.Group("Shake", v, Vector3.zero).transform;

        // chassis, wheels, drawbar with its stand
        PrimitiveKit.Box("Frame", sh, new Vector3(0f, 0.75f, -0.2f), new Vector3(1.9f, 0.14f, 4.4f), Steel, collider: false);
        foreach (float x in new[] { -1.1f, 1.1f }) Wheel(sh, new Vector3(x, 0.5f, -0.4f), 1.0f, 0.28f, Steel);
        PrimitiveKit.Box("Drawbar", sh, new Vector3(0f, 0.6f, 2.6f), new Vector3(0.12f, 0.12f, 1.4f), Steel, new Vector3(-6f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Stand", sh, new Vector3(0f, 0.25f, 3.1f), new Vector3(0.08f, 0.5f, 0.08f), Steel, collider: false);

        // the body and the round drum hood along it
        PrimitiveKit.Box("Body", sh, new Vector3(0f, 1.55f, -0.4f), new Vector3(1.9f, 1.5f, 3.4f), paint, collider: false);
        PrimitiveKit.Cylinder("DrumHood", sh, new Vector3(0f, 2.3f, -0.4f), new Vector3(1.9f, 1.7f, 1.2f), paint, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Stripe", sh, new Vector3(0f, 1.25f, -0.4f), new Vector3(1.92f, 0.16f, 3.42f), yellow, collider: false);
        foreach (float z in new[] { -1.9f, 1.1f })
            PrimitiveKit.Box("Rib", sh, new Vector3(0f, 1.9f, z), new Vector3(1.96f, 1.6f, 0.06f), rust, collider: false);
        PrimitiveKit.Box("Sieve", sh, new Vector3(0f, 0.95f, -0.4f), new Vector3(1.7f, 0.25f, 3f), rust, collider: false);

        // engine up front, the belt to the drum pulley on the right side
        PrimitiveKit.Box("Engine", sh, new Vector3(0f, 1.15f, 1.85f), new Vector3(0.9f, 0.7f, 0.9f), yellow, collider: false);
        PrimitiveKit.Box("Radiator", sh, new Vector3(0f, 1.2f, 2.32f), new Vector3(0.8f, 0.6f, 0.08f), Black, collider: false);
        PrimitiveKit.Cylinder("Exhaust", sh, new Vector3(-0.3f, 1.9f, 2.0f), new Vector3(0.08f, 0.4f, 0.08f), Black, collider: false);
        PrimitiveKit.Cylinder("EnginePulley", sh, new Vector3(0.55f, 1.15f, 1.85f), new Vector3(0.35f, 0.05f, 0.35f), Steel, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Cylinder("DrumPulley", sh, new Vector3(1.0f, 2.2f, 0.6f), new Vector3(0.7f, 0.05f, 0.7f), Steel, new Vector3(0f, 0f, 90f), collider: false);
        var beltDir = new Vector3(0f, 2.2f, 0.6f) - new Vector3(0f, 1.15f, 1.85f);
        var beltRot = Quaternion.FromToRotation(Vector3.up, beltDir).eulerAngles;
        foreach (float off in new[] { -0.2f, 0.2f })
            PrimitiveKit.Box("Belt", sh, new Vector3(1.02f, 1.68f + off * 0.6f, 1.22f + off * 0.5f), new Vector3(0.04f, beltDir.magnitude, 0.05f), Black, beltRot, collider: false);

        // feed conveyor up the left side into the drum
        var feed = PrimitiveKit.Group("FeedConveyor", sh, new Vector3(-1.85f, 1.35f, -0.4f)).transform;
        PrimitiveKit.Box("Tray", feed, Vector3.zero, new Vector3(1.9f, 0.06f, 1.6f), Steel, new Vector3(0f, 0f, -28f), collider: false);
        foreach (float z in new[] { -0.82f, 0.82f })
            PrimitiveKit.Box("TraySide", feed, new Vector3(0f, 0.15f, z), new Vector3(1.9f, 0.3f, 0.05f), paint, new Vector3(0f, 0f, -28f), collider: false);
        PrimitiveKit.Box("Mouth", sh, new Vector3(-0.96f, 1.85f, -0.4f), new Vector3(0.04f, 0.55f, 1.5f), Black, collider: false);
        foreach (float z in new[] { -0.75f, 0f, 0.75f }) // a few sheaves going up the tray
            PrimitiveKit.Cylinder("Sheaf", feed, new Vector3(-0.2f + z * 0.3f, 0.12f + z * 0.15f, z * 0.6f), new Vector3(0.28f, 0.45f, 0.28f), M("Sheaf", new Color(0.82f, 0.68f, 0.32f), 0.1f), new Vector3(0f, 0f, 62f), collider: false);
        var drum = new GameObject("Spin").transform; // the beater drum, seen through the mouth
        drum.SetParent(sh, false);
        drum.localPosition = new Vector3(-0.85f, 1.85f, -0.4f);
        drum.localRotation = Quaternion.Euler(0f, 90f, 0f);
        PrimitiveKit.Cylinder("Drum", drum, Vector3.zero, new Vector3(0.3f, 0.7f, 0.3f), Steel, new Vector3(0f, 0f, 90f), collider: false);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f;
            var bar = PrimitiveKit.Box("Beater", drum, Vector3.zero, new Vector3(1.4f, 0.05f, 0.05f), yellow, new Vector3(a, 0f, 0f), collider: false).transform;
            bar.localPosition = Quaternion.Euler(a, 0f, 0f) * new Vector3(0f, 0.2f, 0f);
        }
        var dust = new GameObject("Dust").transform;
        dust.SetParent(sh, false);
        dust.localPosition = new Vector3(-1.1f, 2.0f, -0.4f);

        // grain spout on the right, a sack filling under it
        PrimitiveKit.Box("Spout", sh, new Vector3(1.15f, 1.1f, -1.2f), new Vector3(0.4f, 0.18f, 0.3f), Steel, new Vector3(0f, 0f, -30f), collider: false);
        Sack(sh, new Vector3(1.45f, 0f, -1.2f), 0f);

        // the blower at the back and its tall pipe, arcing back and up: the Chute is its mouth
        PrimitiveKit.Cylinder("Fan", sh, new Vector3(0.45f, 1.5f, -2.35f), new Vector3(1.1f, 0.25f, 1.1f), yellow, new Vector3(90f, 0f, 0f), collider: false);
        Vector3 a0 = new Vector3(0.45f, 1.9f, -2.5f), a1 = new Vector3(0.45f, 4.1f, -2.7f);
        Vector3 b1 = a1 + new Vector3(0f, 0.55f, -1.9f);
        Pipe(sh, a0, a1, 0.42f, paint);
        PrimitiveKit.Sphere("Elbow", sh, a1, Vector3.one * 0.46f, paint);
        Pipe(sh, a1, b1, 0.42f, paint);
        PrimitiveKit.Cylinder("Hood", sh, b1, new Vector3(0.6f, 0.12f, 0.6f), yellow, Quaternion.FromToRotation(Vector3.up, b1 - a1).eulerAngles, collider: false);
        PrimitiveKit.Box("Brace", sh, new Vector3(0.45f, 3.2f, -2.2f), new Vector3(0.05f, 1.6f, 0.05f), Steel, new Vector3(-20f, 0f, 0f), collider: false);
        var chute = new GameObject("Chute").transform;
        chute.SetParent(sh, false);
        chute.localPosition = b1;
        chute.localRotation = Quaternion.LookRotation(b1 - a1);

        // the straw stack it is blowing onto (not shaking with the machine), loose straw round it
        var pile = PrimitiveKit.Group("Pile", v, new Vector3(0.6f, 0f, -9f)).transform;
        var straw = M("StrawStack", new Color(0.86f, 0.74f, 0.42f), 0.05f);
        var strawDark = M("StrawStackOld", new Color(0.74f, 0.6f, 0.34f), 0.05f);
        var blob = MeshKit.Blob("StrawStack", 31, 0.16f, 2);
        MeshKit.Place("StackBase", pile, blob, strawDark, new Vector3(0f, 0.7f, 0f), Vector3.zero, new Vector3(5.6f, 1.8f, 5f));
        MeshKit.Place("StackMid", pile, blob, straw, new Vector3(0.2f, 1.7f, 0.1f), new Vector3(0f, 70f, 0f), new Vector3(4.2f, 1.8f, 3.8f));
        MeshKit.Place("StackTop", pile, blob, straw, new Vector3(0.1f, 2.6f, 0f), new Vector3(0f, 150f, 0f), new Vector3(2.6f, 1.3f, 2.4f));
        var rng = new System.Random(77);
        for (int i = 0; i < 46; i++) // strands lying on the ground round the stack
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f, d = 2.4f + (float)rng.NextDouble() * 2.2f;
            PrimitiveKit.Box("Strand", pile, new Vector3(Mathf.Cos(ang) * d, 0.04f, Mathf.Sin(ang) * d), new Vector3(0.04f, 0.03f, 0.5f + (float)rng.NextDouble() * 0.4f),
                i % 3 == 0 ? strawDark : straw, new Vector3(0f, (float)rng.NextDouble() * 180f, 0f), collider: false);
        }
        var trail = MeshKit.Blob("StrawTrail", 12, 0.2f, 1); // straw that fell short, along under the arc
        for (int i = 0; i < 3; i++)
            MeshKit.Place("Fallen", pile, trail, straw, new Vector3(0f, 0.1f, 3.2f + i * 1.1f), new Vector3(0f, i * 40f, 0f), new Vector3(1.6f - i * 0.3f, 0.3f, 1.1f));

        // the sheaves waiting to go in, stacked by the feed conveyor
        var sheaf = M("Sheaf", new Color(0.82f, 0.68f, 0.32f), 0.1f);
        var ear = M("SheafEars", new Color(0.7f, 0.56f, 0.22f), 0.1f);
        var heap = PrimitiveKit.Group("SheafHeap", v, new Vector3(-3.6f, 0f, -0.2f)).transform;
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < 6 - layer * 2; i++)
            {
                var c = new Vector3(0f, 0.2f + layer * 0.34f, (i - (5 - layer * 2) * 0.5f) * 0.42f);
                PrimitiveKit.Cylinder("Sheaf", heap, c, new Vector3(0.36f, 0.5f, 0.36f), sheaf, new Vector3(0f, 0f, 90f), collider: false);
                PrimitiveKit.Sphere("Ears", heap, c + new Vector3(-0.55f, 0f, 0f), new Vector3(0.3f, 0.4f, 0.42f), ear);
            }

        // the filled sacks in a row on the right
        for (int i = 0; i < 6; i++) Sack(v, new Vector3(2.3f + (i / 3) * 0.7f, 0f, 0.6f - (i % 3) * 0.75f), i * 17f);
        return v;
    }

    private static void Pipe(Transform parent, Vector3 a, Vector3 b, float dia, Material m)
    {
        Vector3 d = b - a;
        PrimitiveKit.Cylinder("Pipe", parent, (a + b) * 0.5f, new Vector3(dia, d.magnitude * 0.5f, dia), m, Quaternion.FromToRotation(Vector3.up, d).eulerAngles, collider: false);
    }

    // A rice sack, full and tied at the top
    private static void Sack(Transform parent, Vector3 at, float yaw)
    {
        var sack = M("RiceSack", new Color(0.9f, 0.88f, 0.8f), 0.2f);
        var g = PrimitiveKit.Group("Sack_กระสอบข้าว", parent, at, yaw).transform;
        PrimitiveKit.Cylinder("Bag", g, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.38f, 0.42f), sack, collider: false);
        PrimitiveKit.Sphere("Top", g, new Vector3(0f, 0.78f, 0f), new Vector3(0.36f, 0.16f, 0.3f), sack);
        PrimitiveKit.Cylinder("Tie", g, new Vector3(0f, 0.88f, 0f), new Vector3(0.1f, 0.05f, 0.1f), M("SackTie", new Color(0.3f, 0.4f, 0.75f), 0.3f), collider: false);
    }
}
