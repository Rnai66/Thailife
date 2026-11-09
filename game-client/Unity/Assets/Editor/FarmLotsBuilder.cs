using UnityEngine;

// The farm lots south of the village (farmlot.lua, FarmLots/FarmLot at runtime): two laterite farm roads
// off the east soi, 24 lots of 26 x 25 m (6 on each side of each road), every one with its sign, a "ว่าง"
// board, corner pegs, a cross path, four corners for the zones and four sides for fences. The zones
// (แปลงนา, บ่อเลี้ยงปลา, คอกวัว, เล้าไก่ + เถียงนา: each 12 x 11 m, front -Z) and the fences (ไม้ไผ่,
// ลวดหนาม, ไม้ระแนง, กำแพงปูน) are built once under "FarmTemplates" (inactive) and copied onto the lots
// people own when the game runs. Lot fronts face their road.
public static class FarmLotsBuilder
{
    public const float LotW = 26f, LotD = 25f;
    public const float RoadA = -87f, RoadB = -143f, RoadHalf = 2.5f;
    private static readonly float[] Columns = { -45f, -18f, 9f, 36f, 63f, 97f }; // the east soi runs at x 78..82
    private static readonly string[] FenceKinds = { "bamboo", "wire", "wood", "wall" };

    public static void Build(Transform v)
    {
        var laterite = PrimitiveKit.Mat("Laterite", new Color(0.66f, 0.42f, 0.27f));
        foreach (float z in new[] { RoadA, RoadB })
            PrimitiveKit.Box("FarmRoad", v, new Vector3(26f, 0.016f, z), new Vector3(168f, 0.03f, RoadHalf * 2f), laterite, collider: false);
        Places.Sign(v, new Vector3(76.6f, 0f, -12.5f), "แปลงเกษตร โคกหนองนา ↓\nนา · บ่อปลา · คอกวัว · เล้าไก่", 0f);
        foreach (float z in new[] { -30f, -60f, -100f, -120f })
            StreetFurniture.LampPost(v, new Vector3(81.6f, 0f, z));
        // solar lamps along both farm roads, in the 1 m gaps between the lots, leaning over the road
        foreach (float road in new[] { RoadA, RoadB })
            foreach (float x in new[] { -31.5f, -4.5f, 22.5f, 49.5f })
                StreetFurniture.SolarLamp(v, new Vector3(x, 0f, road + RoadHalf + 0.5f), 0f);
        // the cow seller (พ่อค้าวัว) on the strip beside the east soi, just past farm road A, facing the soi
        Seaside.CowSeller(v, new Vector3(83f, 0f, RoadA - 8f), 90f);

        var root = PrimitiveKit.Group("FarmLots_village", v, Vector3.zero).transform;
        var tpl = PrimitiveKit.Group("FarmTemplates", root, new Vector3(0f, -60f, 0f)).transform;
        Rice(PrimitiveKit.Group("Tpl_rice", tpl, Vector3.zero).transform);
        Pond(PrimitiveKit.Group("Tpl_pond", tpl, Vector3.zero).transform);
        Cows(PrimitiveKit.Group("Tpl_cows", tpl, Vector3.zero).transform);
        Coop(PrimitiveKit.Group("Tpl_coop", tpl, Vector3.zero).transform);
        foreach (var kind in FenceKinds)
        {
            var front = PrimitiveKit.Group($"Fence_{kind}_front", tpl, Vector3.zero).transform;
            Fence(front, kind, -LotW * 0.5f, -1.6f, "L");
            Fence(front, kind, 1.6f, LotW * 0.5f, "R");
            Fence(PrimitiveKit.Group($"Fence_{kind}_back", tpl, Vector3.zero).transform, kind, -LotW * 0.5f, LotW * 0.5f, "B");
            Fence(PrimitiveKit.Group($"Fence_{kind}_side", tpl, Vector3.zero).transform, kind, -LotD * 0.5f, LotD * 0.5f, "S");
        }
        tpl.gameObject.SetActive(false);
        root.gameObject.AddComponent<FarmLots>().Configure(tpl);

        int no = 1;
        foreach (float road in new[] { RoadA, RoadB })
        {
            float north = road + RoadHalf + LotD * 0.5f, south = road - RoadHalf - LotD * 0.5f;
            foreach (float x in Columns) Lot(root, no++, new Vector3(x, 0f, north), 0f);
            foreach (float x in Columns) Lot(root, no++, new Vector3(x, 0f, south), 180f);
        }
    }

    private static void Lot(Transform root, int no, Vector3 at, float yaw)
    {
        var t = PrimitiveKit.Group("FarmLot_" + no, root, at, yaw).transform;
        float hw = LotW * 0.5f, hd = LotD * 0.5f;
        var peg = PrimitiveKit.Mat("LotPeg", new Color(0.92f, 0.92f, 0.9f));
        var dirt = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        var board = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var post = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var red = PrimitiveKit.Mat("LotVacantRed", new Color(0.75f, 0.18f, 0.14f));

        foreach (var c in new[] { new Vector3(-hw + 0.2f, 0f, -hd + 0.2f), new Vector3(hw - 0.2f, 0f, -hd + 0.2f), new Vector3(-hw + 0.2f, 0f, hd - 0.2f), new Vector3(hw - 0.2f, 0f, hd - 0.2f) })
            PrimitiveKit.Box("LotPeg", t, c + new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), peg, collider: false);
        // paths: from the gate up the middle, and across between the front and back corners
        PrimitiveKit.Box("Path", t, new Vector3(0f, 0.012f, 0f), new Vector3(1f, 0.02f, LotD), dirt, collider: false);
        PrimitiveKit.Box("PathCross", t, new Vector3(0f, 0.013f, 0.5f), new Vector3(LotW - 1f, 0.02f, 1f), dirt, collider: false);

        var slots = new Transform[4];
        var at4 = new[] { new Vector3(-6.5f, 0f, -5.5f), new Vector3(6.5f, 0f, -5.5f), new Vector3(-6.5f, 0f, 6.5f), new Vector3(6.5f, 0f, 6.5f) };
        for (int i = 0; i < 4; i++) slots[i] = PrimitiveKit.Group("Slot_" + (i + 1), t, at4[i]).transform;
        var sides = new[]
        {
            PrimitiveKit.Group("Side_front", t, new Vector3(0f, 0f, -hd)).transform,
            PrimitiveKit.Group("Side_back", t, new Vector3(0f, 0f, hd)).transform,
            PrimitiveKit.Group("Side_left", t, new Vector3(-hw, 0f, 0f), 90f).transform,
            PrimitiveKit.Group("Side_right", t, new Vector3(hw, 0f, 0f), 90f).transform,
        };

        // the sign right of the gate, facing the road, and the spot in front of it where you arrange your farm
        var sign = PrimitiveKit.Group("LotSign", t, new Vector3(3.2f, 0f, -hd + 0.7f)).transform;
        PrimitiveKit.Box("Post", sign, new Vector3(0f, 0.55f, 0.05f), new Vector3(0.1f, 1.1f, 0.1f), post);
        PrimitiveKit.Box("Board", sign, new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.6f, 0.05f), board, collider: false);
        var plate = PrimitiveKit.Label("แปลงเกษตร " + no, sign, new Vector3(0f, 1.25f, -0.04f), 0.022f, new Color(0.12f, 0.25f, 0.12f));
        var manage = new GameObject("FarmLotManage");
        manage.transform.SetParent(t, false);
        manage.transform.localPosition = new Vector3(3.2f, 1f, -hd + 0.2f);
        var box = manage.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.4f, 2f, 1.8f);
        manage.AddComponent<FarmLotCounter>();
        manage.SetActive(false);

        var vacant = PrimitiveKit.Group("LotVacant", t, new Vector3(0f, 0f, -6f)).transform;
        PrimitiveKit.Box("Stake", vacant, new Vector3(0f, 0.5f, 0.05f), new Vector3(0.08f, 1f, 0.08f), post, collider: false);
        PrimitiveKit.Box("Board", vacant, new Vector3(0f, 1.1f, 0f), new Vector3(1.3f, 0.5f, 0.04f), red, collider: false);
        PrimitiveKit.Label("ว่าง\nแปลงเกษตร " + no, vacant, new Vector3(0f, 1.1f, -0.03f), 0.018f, Color.white);
        vacant.gameObject.SetActive(false);

        t.gameObject.AddComponent<FarmLot>().Configure(no, plate, vacant.gameObject, manage, slots, sides);
    }

    // ── Zones (12 x 11 m, front -Z) ───────────────────────────────────────────────────────
    // แปลงนา: 3 x 4 small paddies behind low walkable dikes; the front six are นา 1-6, the back six (7-12)
    // are yours once you own the canal-side land (LandSite "rice"), dry ground with a board until then
    private static void Rice(Transform t)
    {
        var g = PrimitiveKit.Group("KokNongNa_นา", t, Vector3.zero).transform;
        var paddy = PrimitiveKit.Mat("PaddyWater", new Color(0.58f, 0.64f, 0.36f), 0.82f);
        var dike = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        var earth = PrimitiveKit.Mat("Earth", new Color(0.5f, 0.38f, 0.24f));
        float[] xs = { -4f, 0f, 4f }, zs = { -4.125f, -1.375f, 1.375f, 4.125f };
        var land = PrimitiveKit.Group("LandRice", g, Vector3.zero);
        var owned = PrimitiveKit.Group("Owned", land.transform, Vector3.zero).transform;
        var sale = PrimitiveKit.Group("ForSale", land.transform, Vector3.zero).transform;
        int id = 0;
        for (int row = 0; row < 4; row++)
        foreach (float x in xs)
        {
            id++;
            var parent = row < 2 ? g : owned;
            float z = zs[row];
            PrimitiveKit.Box("Paddy", parent, new Vector3(x, 0.015f, z), new Vector3(3.7f, 0.03f, 2.45f), paddy, collider: false);
            var plot = new GameObject($"FarmPlot_{id}");
            plot.transform.SetParent(parent, false);
            plot.transform.localPosition = new Vector3(x, 1f, z);
            var trigger = plot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.4f, 2f, 2.2f);
            plot.AddComponent<FarmPlot>().Configure(id, new Vector2(3.6f, 2.35f));
            if (row >= 2) PrimitiveKit.Box("DryPlot", sale, new Vector3(x, 0.014f, z), new Vector3(3.7f, 0.02f, 2.45f), earth, collider: false);
        }
        foreach (float z in new[] { -5.5f, -2.75f, 0f, 2.75f, 5.5f })
            PrimitiveKit.Box("Dike", g, new Vector3(0f, 0.12f, z), new Vector3(12.2f, 0.25f, 0.3f), dike);
        foreach (float x in new[] { -6f, -2f, 2f, 6f })
            PrimitiveKit.Box("Dike", g, new Vector3(x, 0.12f, 0f), new Vector3(0.3f, 0.25f, 11f), dike);
        var b = PrimitiveKit.Group("Board", sale, new Vector3(5.2f, 0f, 3f), -90f).transform;
        PrimitiveKit.Box("Stake", b, new Vector3(0f, 0.6f, 0.05f), new Vector3(0.08f, 1.2f, 0.08f), PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f)), collider: false);
        PrimitiveKit.Box("Face", b, new Vector3(0f, 1.25f, 0f), new Vector3(1.8f, 0.6f, 0.04f), PrimitiveKit.Mat("SignCream", new Color(0.98f, 0.93f, 0.8f)), collider: false);
        PrimitiveKit.Label("นา 7-12\nซื้อที่นาติดคลองที่ธนาคาร", b, new Vector3(0f, 1.25f, -0.03f), 0.016f, new Color(0.3f, 0.18f, 0.08f));
        land.AddComponent<LandSite>().Configure("rice");
    }

    // บ่อเลี้ยงปลา: a pond with three floating net cages and a little jetty at the front
    private static void Pond(Transform t)
    {
        var g = PrimitiveKit.Group("KokNongNa_สระ", t, Vector3.zero).transform;
        var water = PrimitiveKit.Mat("Water", new Color(0.25f, 0.5f, 0.75f));
        var earth = PrimitiveKit.Mat("Earth", new Color(0.5f, 0.38f, 0.24f));
        var wood = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var bamboo = PrimitiveKit.Mat("Bamboo", new Color(0.72f, 0.66f, 0.38f));
        var net = PrimitiveKit.Mat("CageNet", new Color(0.2f, 0.3f, 0.28f));
        PrimitiveKit.Box("PondRim", g, new Vector3(0f, 0.01f, 0.6f), new Vector3(11.4f, 0.02f, 9.4f), earth, collider: false);
        PrimitiveKit.Box("Pond", g, new Vector3(0f, 0.025f, 0.9f), new Vector3(10.4f, 0.02f, 7.8f), water, collider: false);
        PrimitiveKit.Box("Jetty", g, new Vector3(0f, 0.12f, -3.6f), new Vector3(2.4f, 0.08f, 1.4f), wood, collider: false);
        var fish = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var cage = PrimitiveKit.Group($"FishCage_{i + 1}", g, new Vector3(-3.4f + i * 3.4f, 0f, 1.4f)).transform;
            PrimitiveKit.Box("Net", cage, new Vector3(0f, 0.035f, 0f), new Vector3(2.2f, 0.01f, 2.2f), net, collider: false);
            foreach (float s in new[] { -1.1f, 1.1f })
            {
                PrimitiveKit.Cylinder("FloatX", cage, new Vector3(0f, 0.09f, s), new Vector3(0.14f, 1.15f, 0.14f), bamboo, new Vector3(0f, 0f, 90f), collider: false);
                PrimitiveKit.Cylinder("FloatZ", cage, new Vector3(s, 0.09f, 0f), new Vector3(0.14f, 1.15f, 0.14f), bamboo, new Vector3(90f, 0f, 0f), collider: false);
                foreach (float u in new[] { -1.1f, 1.1f })
                    PrimitiveKit.Cylinder("Pole", cage, new Vector3(s, 0.35f, u), new Vector3(0.06f, 0.35f, 0.06f), bamboo, collider: false);
            }
            PrimitiveKit.Label($"กระชัง {i + 1}", cage, new Vector3(0f, 0.9f, 0f), 0.05f, Color.white);
            var school = PrimitiveKit.Group("Fish", cage, Vector3.zero);
            school.AddComponent<CageFish>().Configure(0.8f, 5);
            school.SetActive(false);
            fish[i] = school;
        }
        var bank = new GameObject("FishPondZone");
        bank.transform.SetParent(g, false);
        bank.transform.localPosition = new Vector3(0f, 1f, -4.3f);
        var zone = bank.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(3.2f, 2f, 1.6f);
        bank.AddComponent<FishPond>().Configure(fish);
    }

    // คอกวัว: a rail fence round the corner with a gate at the front, a shed, hay and a trough at the back;
    // room for twenty cows (CowPen shows as many as you have)
    private static void Cows(Transform t)
    {
        var f = PrimitiveKit.Group("CowFarm_คอกวัว", t, Vector3.zero).transform;
        var fence = PrimitiveKit.Mat("FenceWood", new Color(0.62f, 0.45f, 0.28f));
        const float hx = 5.8f, hz = 5.3f;
        for (float x = -hx; x <= hx + 0.01f; x += 2.9f)
            foreach (float z in new[] { -hz, hz })
                if (z > 0 || Mathf.Abs(x) > 1f) PrimitiveKit.Box("FencePost", f, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), fence);
        for (float z = -hz; z <= hz + 0.01f; z += 2.65f)
            foreach (float x in new[] { -hx, hx })
                PrimitiveKit.Box("FencePost", f, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), fence);
        foreach (float y in new[] { 0.5f, 0.95f })
        {
            PrimitiveKit.Box("Rail", f, new Vector3(0f, y, hz), new Vector3(hx * 2f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailL", f, new Vector3(-hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailR", f, new Vector3(hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailFrontL", f, new Vector3(-3.4f, y, -hz), new Vector3(4.8f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailFrontR", f, new Vector3(3.4f, y, -hz), new Vector3(4.8f, 0.1f, 0.08f), fence);
        }
        var roof = PrimitiveKit.Mat("RoofRed", new Color(0.7f, 0.22f, 0.15f));
        foreach (float x in new[] { -2.6f, 2.6f })
            foreach (float z in new[] { 3.2f, 5f })
                PrimitiveKit.Cylinder("ShedPost", f, new Vector3(x, 1.3f, z), new Vector3(0.16f, 1.3f, 0.16f), fence);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("ShedRoof", f, new Vector3(0f, 2.85f, 4.1f + side * 0.6f), new Vector3(6f, 0.1f, 1.4f), roof, new Vector3(side * 25f, 0f, 0f), collider: false);
        var hay = PrimitiveKit.Mat("Hay", new Color(0.9f, 0.78f, 0.38f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("HayBale", f, new Vector3(-2f + i * 1.02f, 0.3f, 4.5f), new Vector3(1f, 0.6f, 0.7f), hay);
        PrimitiveKit.Box("HayBale", f, new Vector3(-1.5f, 0.9f, 4.5f), new Vector3(1f, 0.6f, 0.7f), hay);
        PrimitiveKit.Box("Trough", f, new Vector3(1.8f, 0.3f, 3.6f), new Vector3(2f, 0.5f, 0.7f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)));
        PrimitiveKit.Box("TroughWater", f, new Vector3(1.8f, 0.52f, 3.6f), new Vector3(1.8f, 0.04f, 0.5f), PrimitiveKit.Mat("CanalWater", new Color(0.25f, 0.45f, 0.5f)), collider: false);
        StreetFurniture.OrangeLight(f, new Vector3(0f, 2.5f, 4.1f));

        int n = 0;
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 5; col++)
        {
            var c = new Vector2(-4.4f + col * 2.2f + (row % 2) * 0.5f, -3.6f + row * 1.6f);
            Seaside.Cow(f, new Vector3(c.x, 0f, c.y), n++, new Vector2(Mathf.Max(0.8f, hx - 1.2f - Mathf.Abs(c.x)), Mathf.Max(0.6f, 2.4f - Mathf.Abs(c.y + 1.2f))));
        }
        f.gameObject.AddComponent<CowPen>();

        var penZone = new GameObject("CowPenZone");
        penZone.transform.SetParent(f, false);
        penZone.transform.localPosition = new Vector3(0f, 1f, -hz + 0.4f);
        var penBox = penZone.AddComponent<BoxCollider>();
        penBox.isTrigger = true;
        penBox.size = new Vector3(2.4f, 2f, 1.8f);
        penZone.AddComponent<RanchCounter>().Configure("pen");
    }

    // เล้าไก่ + เถียงนา: the chicken coop, a field hut to rest in and two banana trees
    private static void Coop(Transform t)
    {
        Places2.ChickenCoop(t, new Vector3(-2.6f, 0f, 1.6f), 0f);
        Places.FieldHut(t, new Vector3(3.6f, 0f, 2.6f));
        Places.BananaTree(t, new Vector3(4.6f, 0f, -3.4f), trainable: false);
        Places.BananaTree(t, new Vector3(2.6f, 0f, -4.2f), trainable: false);
    }

    // ── Fences: one run along local X from a to b, merged, with one collider ────────────────────
    private static void Fence(Transform parent, string kind, float a, float b, string tag)
    {
        float len = b - a, mid = (a + b) * 0.5f;
        var g = PrimitiveKit.Group("Run_" + tag, parent, new Vector3(mid, 0f, 0f)).transform;
        float h;
        switch (kind)
        {
            case "bamboo":
            {
                var m = PrimitiveKit.Mat("Bamboo", new Color(0.72f, 0.66f, 0.38f));
                h = 1.1f;
                for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 1.6f)
                    PrimitiveKit.Cylinder("Post", g, new Vector3(x, h * 0.5f, 0f), new Vector3(0.09f, h * 0.5f, 0.09f), m, collider: false);
                foreach (float y in new[] { 0.3f, 0.65f, 1f })
                    PrimitiveKit.Cylinder("Pole", g, new Vector3(0f, y, 0f), new Vector3(0.06f, len * 0.5f, 0.06f), m, new Vector3(0f, 0f, 90f), collider: false);
                break;
            }
            case "wire":
            {
                var wood = PrimitiveKit.Mat("FenceWood", new Color(0.62f, 0.45f, 0.28f));
                var wire = PrimitiveKit.Mat("BarbedWire", new Color(0.3f, 0.3f, 0.32f), 0.6f);
                h = 1.2f;
                for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 2.5f)
                    PrimitiveKit.Box("Post", g, new Vector3(x, h * 0.5f, 0f), new Vector3(0.12f, h, 0.12f), wood, collider: false);
                foreach (float y in new[] { 0.35f, 0.7f, 1.05f })
                    PrimitiveKit.Box("Wire", g, new Vector3(0f, y, 0f), new Vector3(len, 0.025f, 0.025f), wire, collider: false);
                break;
            }
            case "wood":
            {
                var wood = PrimitiveKit.Mat("FencePicket", new Color(0.7f, 0.52f, 0.32f));
                h = 1.15f;
                foreach (float y in new[] { 0.3f, 0.85f })
                    PrimitiveKit.Box("Rail", g, new Vector3(0f, y, 0.04f), new Vector3(len, 0.08f, 0.05f), wood, collider: false);
                for (float x = -len * 0.5f + 0.06f; x < len * 0.5f; x += 0.24f)
                    PrimitiveKit.Box("Picket", g, new Vector3(x, h * 0.5f, 0f), new Vector3(0.12f, h, 0.03f), wood, collider: false);
                break;
            }
            default: // wall
            {
                var concrete = PrimitiveKit.Mat("WallConcrete", new Color(0.82f, 0.81f, 0.77f));
                var cap = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
                h = 1.6f;
                PrimitiveKit.Box("Wall", g, new Vector3(0f, h * 0.5f, 0f), new Vector3(len, h, 0.16f), concrete, collider: false);
                PrimitiveKit.Box("Cap", g, new Vector3(0f, h + 0.04f, 0f), new Vector3(len + 0.04f, 0.08f, 0.24f), cap, collider: false);
                for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 3f)
                    PrimitiveKit.Box("Pillar", g, new Vector3(x, (h + 0.1f) * 0.5f, 0f), new Vector3(0.3f, h + 0.1f, 0.3f), cap, collider: false);
                break;
            }
        }
        MeshKit.MergeByMaterial(g, $"FarmFences/{kind}_{tag}");
        var block = new GameObject("FenceCollider");
        block.transform.SetParent(g, false);
        var box = block.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, h * 0.5f, 0f);
        box.size = new Vector3(len, h, 0.25f);
    }
}
