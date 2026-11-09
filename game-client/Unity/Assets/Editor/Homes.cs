using UnityEngine;

// The houses players can live in (home.lua, HomePlot), built as templates for the house lots (HouseLotsBuilder):
//   village  บ้านปูนชั้นเดียว · บ้านไม้ชั้นเดียวยกพื้น · บ้านครึ่งปูนครึ่งไม้ 2 ชั้น (a lane east of the hall)
//   City     ทาวน์เฮาส์ 2 ชั้น · บ้านเดี่ยวในเมือง (หมู่บ้านในเมือง, north-east of the avenue)
//   Bangkok  ทาวน์โฮมโมเดิร์น · บ้านเดี่ยวหมู่บ้านจัดสรร (north-east of the avenue)
// Every house has the same room as the stilt house — a "Walls" box 5.6 x 2.4 x 4.6 m on its floor, the
// "Door" and "Window"s on its front (-Z) — so HomeInterior can open any of them up and your furniture
// fits. Everything that would block the view into the room (roofs, upper floors, trims on the walls) is
// named "Roof…" so HomeInterior hides it while you're inside. A porch in front, steps, a sleeping mat.
public static class Homes
{
    private const float RoomW = 5.6f, RoomD = 4.6f, RoomH = 2.4f;

    // ── Where they stand ──────────────────────────────────────────────────────────────────
    // House lots (home.lua LOTS: village 22, city 16, bangkok 29). Lot fronts face their soi.
    private static HouseLotsBuilder.Lot L(int no, float x, float z, float yaw, float w, float d) => new HouseLotsBuilder.Lot(no, x, z, yaw, w, d);

    private static HouseLotsBuilder.Lot[] VillageLots()
    {
        var lots = new System.Collections.Generic.List<HouseLotsBuilder.Lot>
        {
            L(1, 47f, 12f, 90f, 14f, 14f), L(2, 47f, 26f, 90f, 14f, 14f), L(3, 47f, 40f, 90f, 14f, 14f),  // east of the lane
            L(4, 30f, 27f, -90f, 12f, 12f), L(5, 30f, 39f, -90f, 12f, 12f), L(6, 30f, 51f, -90f, 12f, 12f), // west of the lane
        };
        float[] north = { 3f, 15f, 27f, 39f }, south = { -17.75f, -29.25f, -40.75f, -52.25f };
        int no = 7;
        foreach (float z in north) lots.Add(L(no++, 71f, z, -90f, 12f, 14f));  // ซอยตะวันออก, west side
        foreach (float z in north) lots.Add(L(no++, 89f, z, 90f, 12f, 14f));   // east side
        foreach (float z in south) lots.Add(L(no++, 71f, z, -90f, 11.5f, 14f));
        foreach (float z in south) lots.Add(L(no++, 89f, z, 90f, 11.5f, 14f));
        return lots.ToArray();
    }

    public static void Village(Transform v)
    {
        var dirt = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        PrimitiveKit.Box("DirtLane", v, new Vector3(38f, 0.012f, 25.5f), new Vector3(2.6f, 0.02f, 63f), dirt, collider: false);
        Places.Sign(v, new Vector3(36f, 0f, -4.5f), "ซอยบ้านใหม่ →\nแปลงบ้าน 1–6", 0f);
        // ซอยตะวันออก: a concrete soi across the main road, lots 7–22 on both sides
        var concrete = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        PrimitiveKit.Box("EastSoi", v, new Vector3(80f, 0.014f, -50.25f), new Vector3(4f, 0.028f, 190.5f), concrete, collider: false); // on down to the farm roads
        Places.Sign(v, new Vector3(77f, 0f, -4.5f), "ซอยตะวันออก\nแปลงบ้าน 7–22", 0f);
        foreach (float z in new[] { -46f, -24f, 21f, 40f })
            StreetFurniture.LampPost(v, new Vector3(81.6f, 0f, z));
        HouseLotsBuilder.Build(v, "village", VillageLots(), tpl =>
        {
            VillageBuilder.StiltTemplate(HouseLotsBuilder.Template(tpl, "stilt"), new Vector3(-0.2f, 0f, 2f));
            Concrete1(HouseLotsBuilder.Template(tpl, "concrete1"), new Vector3(-0.3f, 0f, 1f), 0f);
            Wood1(HouseLotsBuilder.Template(tpl, "wood1"), new Vector3(-0.3f, 0f, 1f), 0f);
            Half2(HouseLotsBuilder.Template(tpl, "half2"), new Vector3(-0.3f, 0f, 1f), 0f);
        }, lawn: false);
    }

    public static void City(Transform root)
    {
        Estate(root, "หมู่บ้านในเมือง", 50f);
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        PrimitiveKit.Box("Soi", root, new Vector3(56f, 0.02f, -66f), new Vector3(76f, 0.04f, 6f), asphalt);
        PrimitiveKit.Box("Soi", root, new Vector3(-84f, 0.02f, 25f), new Vector3(5f, 0.04f, 52f), asphalt);
        var lots = new System.Collections.Generic.List<HouseLotsBuilder.Lot>();
        int no = 1;
        for (int i = 0; i < 6; i++) lots.Add(L(no++, 23f + 12f * i, -77f, 180f, 12f, 16f));
        for (int i = 0; i < 2; i++) lots.Add(L(no++, 70f + 12f * i, -55f, 0f, 12f, 16f));
        for (int i = 0; i < 4; i++) lots.Add(L(no++, 66.5f, 6f + 12f * i, 90f, 12f, 17f));
        for (int i = 0; i < 4; i++) lots.Add(L(no++, -73f, 8f + 12f * i, 90f, 12f, 17f));
        HouseLotsBuilder.Build(root, "city", lots.ToArray(), tpl =>
        {
            Townhouse(HouseLotsBuilder.Template(tpl, "townhouse"), new Vector3(-2.4f, 0f, 1.5f), 0f, modern: false, solo: true);
            Single(HouseLotsBuilder.Template(tpl, "city_single"), new Vector3(0f, 0f, 1.5f), 0f, "city_single", modern: false);
        }, lawn: true);
    }

    public static void Bangkok(Transform root)
    {
        Estate(root, "หมู่บ้านจัดสรร", 45f);
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        PrimitiveKit.Box("Soi", root, new Vector3(89f, 0.02f, -52f), new Vector3(62f, 0.04f, 6f), asphalt);
        var lots = new System.Collections.Generic.List<HouseLotsBuilder.Lot>();
        int no = 1;
        for (int i = 0; i < 4; i++) lots.Add(L(no++, 69f + 12f * i, -41f, 0f, 12f, 16f));
        for (int i = 0; i < 4; i++) lots.Add(L(no++, 69f + 12f * i, -63f, 180f, 12f, 16f));
        for (int i = 0; i < 13; i++) lots.Add(L(no++, -107f + 12f * i, -104f, 0f, 12f, 16f)); // along the ring road
        for (int i = 0; i < 4; i++) lots.Add(L(no++, 69f + 12f * i, -104f, 0f, 12f, 16f));
        for (int i = 0; i < 4; i++) lots.Add(L(no++, 66.5f, 5.5f + 11f * i, 90f, 11f, 17f));
        HouseLotsBuilder.Build(root, "bangkok", lots.ToArray(), tpl =>
        {
            Townhouse(HouseLotsBuilder.Template(tpl, "townhome"), new Vector3(-2.4f, 0f, 1.5f), 0f, modern: true, solo: true);
            Single(HouseLotsBuilder.Template(tpl, "bkk_single"), new Vector3(0f, 0f, 1.5f), 0f, "bkk_single", modern: true);
        }, lawn: true);
    }

    // A paved soi from the avenue's north pavement up past the houses, with an arch over its mouth
    private static void Estate(Transform root, string name, float soiEnd)
    {
        var road = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var white = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        PrimitiveKit.Box("Soi", root, new Vector3(55.5f, 0.02f, (soiEnd - 5f) * 0.5f), new Vector3(5f, 0.04f, soiEnd + 5f), road);
        PrimitiveKit.Box("SoiPavement", root, new Vector3(55.5f, 0.015f, -3f), new Vector3(5f, 0.03f, 4f), PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f)), collider: false);
        foreach (float x in new[] { 52.6f, 58.4f })
            PrimitiveKit.Box("GatePost", root, new Vector3(x, 1.6f, -1.2f), new Vector3(0.5f, 3.2f, 0.5f), white);
        PrimitiveKit.Box("GateBeam", root, new Vector3(55.5f, 3.4f, -1.2f), new Vector3(6.4f, 0.6f, 0.5f), white, collider: false);
        PrimitiveKit.Label(name, root, new Vector3(55.5f, 3.4f, -1.5f), 0.05f, new Color(0.15f, 0.25f, 0.45f));
        foreach (float z in new[] { 6f, 22f, 38f })
            StreetFurniture.LampPost(root, new Vector3(52.6f, 0f, z)); // the lots line the east side
        Nature.Bush(root, new Vector3(52.5f, 0f, 9f), 1.1f, 7, flowers: true);
        Nature.Bush(root, new Vector3(52.5f, 0f, 30f), 1.1f, 8, flowers: true);
    }

    // ── What every house has: the room, its door and windows, floor, porch, steps, mat ─────────
    private static Transform Plot(Transform parent, string name, Vector3 at, float yaw) =>
        PrimitiveKit.Group(name, parent, at, yaw).transform;

    private static void Room(Transform t, float floorY, Material wall, Material door, Material window, float porchDepth = 1.6f, Material porchMat = null, float doorX = 0f)
    {
        PrimitiveKit.Box("Walls", t, new Vector3(0f, floorY + RoomH * 0.5f, 0f), new Vector3(RoomW, RoomH, RoomD), wall);
        PrimitiveKit.Box("Door", t, new Vector3(doorX, floorY + 0.95f, -2.32f), new Vector3(1.1f, 1.9f, 0.05f), door, collider: false);
        PrimitiveKit.Box("WindowL", t, new Vector3(-1.8f, floorY + 1.3f, -2.32f), new Vector3(0.9f, 0.8f, 0.05f), window, collider: false);
        PrimitiveKit.Box("WindowR", t, new Vector3(1.8f, floorY + 1.3f, -2.32f), new Vector3(0.9f, 0.8f, 0.05f), window, collider: false);
        if (porchMat != null && porchDepth > 0f)
            PrimitiveKit.Box("Porch", t, new Vector3(0f, floorY - 0.1f, -2.3f - porchDepth * 0.5f), new Vector3(RoomW + 0.6f, 0.2f, porchDepth), porchMat);
    }

    // Steps from the ground up to the porch (each rise within the character's step)
    private static void Steps(Transform t, float floorY, float x, float frontZ, Material m)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(floorY / 0.25f));
        float rise = floorY / n;
        for (int i = 0; i < n; i++)
        {
            float h = rise * (i + 1);
            float z = frontZ - 0.35f * (n - 1 - i) - 0.175f;
            PrimitiveKit.Box("Step", t, new Vector3(x, h * 0.5f, z), new Vector3(1.3f, h, 0.35f), m);
        }
    }

    // The mat on the porch to sleep on, the spot you arrive at, and the HomePlot itself
    private static void Finish(Transform t, string style, float floorY, float porchZ, Transform carport = null)
    {
        PrimitiveKit.Box("SleepingMat", t, new Vector3(-1.6f, floorY + 0.02f, porchZ), new Vector3(1.4f, 0.03f, 1.2f),
            PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f)), collider: false);
        var bed = new GameObject("SleepZone_" + style);
        bed.transform.SetParent(t, false);
        bed.transform.localPosition = new Vector3(-1.6f, floorY + 1f, porchZ);
        var zone = bed.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2f, 2f, 1.4f);
        var rest = new GameObject("RestPoint");
        rest.transform.SetParent(t, false);
        rest.transform.localPosition = new Vector3(-1.6f, floorY + 0.16f, porchZ);
        rest.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        bed.AddComponent<SleepSpot>().Configure("เสื่อหน้าบ้าน", "นอนพัก", rest.transform);

        var porch = PrimitiveKit.Group("HomePorch", t, new Vector3(1.4f, floorY, porchZ), 180f).transform;
        t.gameObject.AddComponent<HomePlot>().Configure(style, floorY, porch, bed, carport);
    }

    private static Transform Carport(Transform t, Vector3 local, float yaw)
    {
        var c = PrimitiveKit.Group("HomeCarport", t, local, yaw).transform;
        PrimitiveKit.Box("Driveway", t, local + new Vector3(0f, 0.015f, 0f), new Vector3(3f, 0.03f, 5f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)), collider: false);
        return c;
    }

    // A pitched roof over the room: two slabs meeting at a ridge along X, with gable ends
    private static void Pitched(Transform t, float eaveY, float rise, float overhang, Material roof, Material gable, string key, Material ridge)
    {
        float halfD = RoomD * 0.5f + overhang;
        float slope = Mathf.Atan2(rise, halfD) * Mathf.Rad2Deg;
        float slab = Mathf.Sqrt(halfD * halfD + rise * rise);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box(s < 0 ? "RoofFront" : "RoofBack", t, new Vector3(0f, eaveY + rise * 0.5f, s * halfD * 0.5f), new Vector3(RoomW + overhang * 2f, 0.12f, slab),
                roof, new Vector3(s * slope, 0f, 0f), collider: false);
        var g = MeshKit.Gable(key, RoomD, rise, 0.1f);
        MeshKit.Place("RoofGableL", t, g, gable, new Vector3(-RoomW * 0.5f, eaveY, 0f), new Vector3(0f, 90f, 0f));
        MeshKit.Place("RoofGableR", t, g, gable, new Vector3(RoomW * 0.5f, eaveY, 0f), new Vector3(0f, -90f, 0f));
        PrimitiveKit.Box("RoofRidge", t, new Vector3(0f, eaveY + rise + 0.05f, 0f), new Vector3(RoomW + overhang * 2f + 0.1f, 0.14f, 0.26f), ridge, collider: false);
    }

    private static void Pot(Transform t, Vector3 at, int seed)
    {
        PrimitiveKit.Cylinder("RoofPot", t, at + new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.18f, 0.36f), PrimitiveKit.Mat("ClayPot", new Color(0.66f, 0.35f, 0.2f)), collider: false);
        Nature.Bush(t, at + new Vector3(0f, 0.34f, 0f), 0.45f, seed, flowers: seed % 2 == 0);
    }

    // ── Village ───────────────────────────────────────────────────────────────────────────
    // บ้านปูนชั้นเดียว: cream plaster on a raised slab, an orange tile roof, a veranda on two columns
    private static void Concrete1(Transform parent, Vector3 at, float yaw)
    {
        const float floorY = 0.45f;
        var t = Plot(parent, "Home_concrete1_บ้านปูนชั้นเดียว", at, yaw);
        var slab = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var plaster = PrimitiveKit.Mat("PlasterCream", new Color(0.94f, 0.88f, 0.74f));
        var trim = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var tile = PrimitiveKit.Mat("RoofTileOrange", new Color(0.82f, 0.38f, 0.18f));
        PrimitiveKit.Box("Foundation", t, new Vector3(0f, floorY * 0.5f, 0f), new Vector3(RoomW + 0.4f, floorY, RoomD + 0.4f), slab);
        Room(t, floorY, plaster, PrimitiveKit.Mat("DoorTeak", new Color(0.42f, 0.24f, 0.12f)), PrimitiveKit.Mat("WindowGlassBlue", new Color(0.45f, 0.6f, 0.7f)), 2f, slab);
        foreach (float x in new[] { -2.9f, 2.9f })
            PrimitiveKit.Box("Column", t, new Vector3(x, floorY + 1.2f, -4.1f), new Vector3(0.25f, 2.4f, 0.25f), trim);
        PrimitiveKit.Box("RoofVeranda", t, new Vector3(0f, floorY + 2.45f, -3.4f), new Vector3(RoomW + 0.8f, 0.12f, 2.3f), tile, new Vector3(-8f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofTrimTop", t, new Vector3(0f, floorY + RoomH + 0.05f, 0f), new Vector3(RoomW + 0.1f, 0.1f, RoomD + 0.1f), trim, collider: false);
        Pitched(t, floorY + RoomH + 0.1f, 1.5f, 0.6f, tile, plaster, "HomeGable_concrete1", PrimitiveKit.Mat("RoofTileDark", new Color(0.55f, 0.22f, 0.12f)));
        Steps(t, floorY, 1.6f, -4.3f, slab);
        Pot(t, new Vector3(-2.6f, 0f, -4.9f), 1);
        Pot(t, new Vector3(2.9f, 0f, -4.9f), 2);
        Finish(t, "concrete1", floorY, -3.4f, Carport(t, new Vector3(4.6f, 0f, -1f), 180f));
    }

    // บ้านไม้ชั้นเดียวยกพื้น: teak boards on low concrete posts, a grey corrugated tin roof
    private static void Wood1(Transform parent, Vector3 at, float yaw)
    {
        const float floorY = 1.1f;
        var t = Plot(parent, "Home_wood1_บ้านไม้ชั้นเดียว", at, yaw);
        var teak = PrimitiveKit.Mat("WoodTeak", new Color(0.5f, 0.3f, 0.16f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var post = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var tin = PrimitiveKit.Mat("ZincRoof", new Color(0.62f, 0.64f, 0.66f));
        foreach (float x in new[] { -2.8f, 0f, 2.8f })
        foreach (float z in new[] { -4.0f, -2.3f, 0f, 2.3f })
            PrimitiveKit.Box("Post", t, new Vector3(x, (floorY - 0.2f) * 0.5f, z), new Vector3(0.22f, floorY - 0.2f, 0.22f), post);
        PrimitiveKit.Box("Floor", t, new Vector3(0f, floorY - 0.1f, 0f), new Vector3(RoomW + 0.4f, 0.2f, RoomD + 0.4f), dark);
        Room(t, floorY, teak, dark, PrimitiveKit.Mat("Shutter", new Color(0.25f, 0.32f, 0.3f)), 1.8f, dark);
        // vertical boards on the front, a rail round the porch
        for (int i = 0; i < 12; i++)
        {
            float x = -2.6f + i * 0.47f;
            if (Mathf.Abs(x) < 0.7f || Mathf.Abs(Mathf.Abs(x) - 1.8f) < 0.5f) continue;
            PrimitiveKit.Box("RoofBoard", t, new Vector3(x, floorY + RoomH * 0.5f, -2.34f), new Vector3(0.05f, RoomH, 0.02f), dark, collider: false);
        }
        PrimitiveKit.Box("Rail", t, new Vector3(-0.9f, floorY + 0.75f, -4.1f), new Vector3(4.2f, 0.07f, 0.07f), dark);
        foreach (float x in new[] { -2.9f, -1.5f, 0f, 1.2f })
            PrimitiveKit.Box("RailPost", t, new Vector3(x, floorY + 0.4f, -4.1f), new Vector3(0.07f, 0.75f, 0.07f), dark);
        Pitched(t, floorY + RoomH, 1.1f, 0.8f, tin, teak, "HomeGable_wood1", tin);
        PrimitiveKit.Box("RoofPorch", t, new Vector3(0f, floorY + 2.3f, -3.5f), new Vector3(RoomW + 0.8f, 0.08f, 2.3f), tin, new Vector3(-12f, 0f, 0f), collider: false);
        foreach (float x in new[] { -2.95f, 2.95f })
            PrimitiveKit.Box("PorchPost", t, new Vector3(x, floorY + 1.1f, -4.3f), new Vector3(0.14f, 2.2f, 0.14f), dark);
        Steps(t, floorY, 2.2f, -4.1f, PrimitiveKit.Mat("StairWood", new Color(0.66f, 0.47f, 0.28f)));
        // a water jar and a bench under the eaves, a banana clump by the side
        PrimitiveKit.Box("Bench", t, new Vector3(-3.6f, 0.4f, -1f), new Vector3(0.5f, 0.06f, 1.8f), dark);
        Places.BananaTree(t, new Vector3(-4.6f, 0f, 2.2f), trainable: false);
        Finish(t, "wood1", floorY, -3.4f, Carport(t, new Vector3(4.6f, 0f, -0.5f), 180f));
    }

    // บ้านครึ่งปูนครึ่งไม้ 2 ชั้น: a white ground floor, a teak upper storey with a balcony, tiled roof
    private static void Half2(Transform parent, Vector3 at, float yaw)
    {
        const float floorY = 0.45f;
        var t = Plot(parent, "Home_half2_บ้านครึ่งปูนครึ่งไม้", at, yaw);
        var slab = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var white = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var teak = PrimitiveKit.Mat("WoodTeak", new Color(0.5f, 0.3f, 0.16f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var tile = PrimitiveKit.Mat("RoofTileRed", new Color(0.62f, 0.2f, 0.14f));
        PrimitiveKit.Box("Foundation", t, new Vector3(0f, floorY * 0.5f, 0f), new Vector3(RoomW + 0.4f, floorY, RoomD + 0.4f), slab);
        Room(t, floorY, white, dark, PrimitiveKit.Mat("WindowGlassBlue", new Color(0.45f, 0.6f, 0.7f)), 1.8f, slab);
        float up = floorY + RoomH;
        PrimitiveKit.Box("RoofUpperFloor", t, new Vector3(0f, up + 0.1f, -0.2f), new Vector3(RoomW + 0.6f, 0.2f, RoomD + 1.4f), dark);
        PrimitiveKit.Box("RoofUpperWalls", t, new Vector3(0f, up + 1.3f, 0.2f), new Vector3(RoomW + 0.4f, 2.2f, RoomD + 0.2f), teak);
        foreach (float x in new[] { -1.6f, 1.6f })
            PrimitiveKit.Box("RoofUpperWindow", t, new Vector3(x, up + 1.4f, -2.12f), new Vector3(1f, 0.9f, 0.05f), PrimitiveKit.Mat("Shutter", new Color(0.25f, 0.32f, 0.3f)), collider: false);
        PrimitiveKit.Box("RoofBalconyRail", t, new Vector3(0f, up + 0.75f, -2.85f), new Vector3(RoomW + 0.6f, 0.08f, 0.08f), white, collider: false);
        for (int i = 0; i < 9; i++)
            PrimitiveKit.Box("RoofBaluster", t, new Vector3(-2.9f + i * 0.725f, up + 0.45f, -2.85f), new Vector3(0.08f, 0.6f, 0.08f), white, collider: false);
        Pitched(t, up + 2.4f, 1.6f, 0.7f, tile, teak, "HomeGable_half2", dark);
        foreach (float x in new[] { -2.9f, 2.9f })
            PrimitiveKit.Box("Column", t, new Vector3(x, floorY + 1.2f, -2.85f), new Vector3(0.25f, 2.4f, 0.25f), white);
        Steps(t, floorY, 1.6f, -4.1f, slab);
        Pot(t, new Vector3(-2.7f, 0f, -4.6f), 3);
        Finish(t, "half2", floorY, -3.2f, Carport(t, new Vector3(4.6f, 0f, -1f), 180f));
    }

    // ── Town and Bangkok ──────────────────────────────────────────────────────────────────
    // A row of three two-storey units; yours is the middle one. modern = the Bangkok townhome (grey and
    // white, wood slats, a glass balcony, flat roof); otherwise a plain cream town ทาวน์เฮาส์.
    private static void Townhouse(Transform parent, Vector3 at, float yaw, bool modern, bool solo = false)
    {
        const float floorY = 0.3f;
        string style = modern ? "townhome" : "townhouse";
        var row = Plot(parent, modern ? "Townhomes_ทาวน์โฮม" : "Townhouses_ทาวน์เฮาส์", at, yaw);
        var wall = modern ? PrimitiveKit.Mat("PlasterGrey", new Color(0.62f, 0.63f, 0.64f)) : PrimitiveKit.Mat("PlasterCream", new Color(0.94f, 0.88f, 0.74f));
        var white = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var slab = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var glass = PrimitiveKit.Mat("WindowGlassBlue", new Color(0.45f, 0.6f, 0.7f));
        var shutter = PrimitiveKit.Mat("ShutterMetal", new Color(0.55f, 0.57f, 0.6f));
        var slats = PrimitiveKit.Mat("WoodTeak", new Color(0.5f, 0.3f, 0.16f));
        const float unit = RoomW + 0.6f;
        for (int u = solo ? 0 : -1; u <= (solo ? 0 : 1); u++) // solo: one unit with a carport beside it (a house lot)
        {
            bool mine = u == 0;
            var t = mine ? Plot(row, "Home_" + style, Vector3.zero, 0f) : Plot(row, "Neighbour", new Vector3(u * unit, 0f, 0f), 0f);
            PrimitiveKit.Box("Foundation", t, new Vector3(0f, floorY * 0.5f, -0.3f), new Vector3(unit, floorY, RoomD + 2.2f), slab);
            if (mine) Room(t, floorY, wall, PrimitiveKit.Mat("DoorTeak", new Color(0.42f, 0.24f, 0.12f)), glass, 0f);
            else
            {
                PrimitiveKit.Box("NeighbourWalls", t, new Vector3(0f, floorY + RoomH * 0.5f, 0f), new Vector3(unit, RoomH, RoomD), wall);
                PrimitiveKit.Box("RollerShutter", t, new Vector3(0f, floorY + 1.1f, -2.32f), new Vector3(2.6f, 2.2f, 0.05f), shutter, collider: false);
            }
            float up = floorY + RoomH;
            string p = mine ? "Roof" : "Upper";
            PrimitiveKit.Box(p + "Upper", t, new Vector3(0f, up + 1.35f, 0f), new Vector3(unit, 2.7f, RoomD), wall);
            PrimitiveKit.Box(p + "Slab", t, new Vector3(0f, up + 0.05f, -0.4f), new Vector3(unit, 0.15f, RoomD + 0.8f), white, collider: false);
            PrimitiveKit.Box(p + "UpperWindow", t, new Vector3(0f, up + 1.4f, -2.32f), new Vector3(2.4f, 1.6f, 0.05f), glass, collider: false);
            if (modern)
            {
                PrimitiveKit.Box(p + "GlassRail", t, new Vector3(0f, up + 0.6f, -2.75f), new Vector3(unit - 0.4f, 0.9f, 0.04f), PrimitiveKit.Mat("GlassClear", new Color(0.7f, 0.85f, 0.9f), 0.9f), collider: false);
                for (int s = 0; s < 6; s++)
                    PrimitiveKit.Box(p + "Slat", t, new Vector3(1.9f + s * 0.12f - 0.3f, up + 1.35f, -2.36f), new Vector3(0.06f, 2.6f, 0.04f), slats, collider: false);
                PrimitiveKit.Box(p + "Parapet", t, new Vector3(0f, up + 2.85f, 0f), new Vector3(unit + 0.05f, 0.3f, RoomD + 0.1f), white, collider: false);
            }
            else
            {
                for (int s = 0; s < 7; s++)
                    PrimitiveKit.Box(p + "Baluster", t, new Vector3(-2.7f + s * 0.9f, up + 0.45f, -2.75f), new Vector3(0.08f, 0.6f, 0.08f), white, collider: false);
                PrimitiveKit.Box(p + "BalconyRail", t, new Vector3(0f, up + 0.78f, -2.75f), new Vector3(unit - 0.2f, 0.08f, 0.1f), white, collider: false);
                PrimitiveKit.Box(p + "RoofTop", t, new Vector3(0f, up + 2.9f, 0f), new Vector3(unit + 0.3f, 0.2f, RoomD + 0.6f), PrimitiveKit.Mat("RoofTileDark", new Color(0.55f, 0.22f, 0.12f)), collider: false);
            }
            PrimitiveKit.Box(p + "AirCon", t, new Vector3(-2f, up + 0.4f, -2.5f), new Vector3(0.8f, 0.55f, 0.3f), white, collider: false);
            // the small front yard behind a low wall with a gate gap
            PrimitiveKit.Box("YardWallL", t, new Vector3(-1.7f, 0.6f, -4.5f), new Vector3(unit * 0.5f - 0.6f, 0.9f, 0.15f), white);
            PrimitiveKit.Box("YardWallR", t, new Vector3(2.15f, 0.6f, -4.5f), new Vector3(unit * 0.5f - 1.5f, 0.9f, 0.15f), white);
            if (u == -1 || solo) PrimitiveKit.Box("YardSide", t, new Vector3(-unit * 0.5f, 0.6f, -3.4f), new Vector3(0.15f, 0.9f, 2.2f), white);
            if (u == 1 || solo) PrimitiveKit.Box("YardSide", t, new Vector3(unit * 0.5f, 0.6f, -3.4f), new Vector3(0.15f, 0.9f, 2.2f), white);
            if (mine)
            {
                Steps(t, floorY, 1.4f, -3.7f, slab);
                Finish(t, style, floorY, -3.3f, solo ? Carport(t, new Vector3(5.7f, 0f, -1.5f), 180f) : null);
            }
            else Pot(t, new Vector3(-2.4f, floorY, -3.6f), u + 5);
        }
    }

    // รั้วเหล็กดัด: a white plinth and posts with black wrought-iron bars between, from a to b (plot-local);
    // one collider for the whole run, the visible parts merged
    private static void Railing(Transform t, string name, Vector3 a, Vector3 b, Material plinth, Material iron, string key)
    {
        var d = b - a;
        float len = d.magnitude;
        var g = PrimitiveKit.Group(name, t, (a + b) * 0.5f, -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg).transform;
        PrimitiveKit.Box("Plinth", g, new Vector3(0f, 0.22f, 0f), new Vector3(len, 0.44f, 0.2f), plinth, collider: false);
        PrimitiveKit.Box("PlinthCap", g, new Vector3(0f, 0.46f, 0f), new Vector3(len + 0.02f, 0.04f, 0.24f), plinth, collider: false);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
        for (int i = 0; i <= posts; i++)
            PrimitiveKit.Box("Post", g, new Vector3(-len * 0.5f + len * i / posts, 0.62f, 0f), new Vector3(0.18f, 1.24f, 0.18f), plinth, collider: false);
        PrimitiveKit.Box("TopRail", g, new Vector3(0f, 1.15f, 0f), new Vector3(len, 0.05f, 0.05f), iron, collider: false);
        PrimitiveKit.Box("MidRail", g, new Vector3(0f, 0.62f, 0f), new Vector3(len, 0.04f, 0.04f), iron, collider: false);
        for (float x = -len * 0.5f + 0.17f; x < len * 0.5f - 0.1f; x += 0.17f)
            PrimitiveKit.Box("Bar", g, new Vector3(x, 0.82f, 0f), new Vector3(0.022f, 0.66f, 0.022f), iron, collider: false);
        MeshKit.MergeByMaterial(g, $"Homes/{key}_{name}");
        var block = new GameObject("FenceCollider");
        block.transform.SetParent(g, false);
        var box = block.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.6f, 0f);
        box.size = new Vector3(len, 1.2f, 0.22f);
    }

    // A detached house behind a fence with a lawn: a gable roof (town) or a flat-roofed two-storey with
    // a cantilevered upper floor (Bangkok estate)
    private static void Single(Transform parent, Vector3 at, float yaw, string style, bool modern)
    {
        const float floorY = 0.45f;
        var t = Plot(parent, "Home_" + style, at, yaw);
        var white = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var grey = PrimitiveKit.Mat("PlasterGrey", new Color(0.62f, 0.63f, 0.64f));
        var slab = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var glass = PrimitiveKit.Mat("WindowGlassBlue", new Color(0.45f, 0.6f, 0.7f));
        var lawn = PrimitiveKit.Mat("Grass", new Color(0.36f, 0.56f, 0.24f));
        PrimitiveKit.Box("Lawn", t, new Vector3(0f, 0.035f, -1f), new Vector3(10.7f, 0.04f, 10.9f), lawn, collider: false); // a hair above the soi, so they don't flicker
        PrimitiveKit.Box("Foundation", t, new Vector3(0f, floorY * 0.5f, 0f), new Vector3(RoomW + 0.4f, floorY, RoomD + 0.4f), slab);
        Room(t, floorY, white, PrimitiveKit.Mat("DoorTeak", new Color(0.42f, 0.24f, 0.12f)), glass, 1.6f, slab);
        float up = floorY + RoomH;
        if (modern)
        {
            PrimitiveKit.Box("RoofUpper", t, new Vector3(0.6f, up + 1.4f, -0.3f), new Vector3(RoomW + 1.6f, 2.8f, RoomD + 0.8f), grey);
            PrimitiveKit.Box("RoofUpperGlass", t, new Vector3(0.6f, up + 1.4f, -2.72f), new Vector3(4f, 1.8f, 0.05f), glass, collider: false);
            PrimitiveKit.Box("RoofFlat", t, new Vector3(0.6f, up + 2.9f, -0.3f), new Vector3(RoomW + 2.2f, 0.2f, RoomD + 1.4f), white, collider: false);
            for (int s = 0; s < 8; s++)
                PrimitiveKit.Box("RoofSlat", t, new Vector3(-2.2f + s * 0.14f, up + 1.4f, -2.76f), new Vector3(0.07f, 2.6f, 0.05f), PrimitiveKit.Mat("WoodTeak", new Color(0.5f, 0.3f, 0.16f)), collider: false);
        }
        else
        {
            PrimitiveKit.Box("RoofTrimTop", t, new Vector3(0f, up + 0.05f, 0f), new Vector3(RoomW + 0.1f, 0.1f, RoomD + 0.1f), white, collider: false);
            Pitched(t, up + 0.1f, 1.4f, 0.7f, PrimitiveKit.Mat("RoofTileGrey", new Color(0.4f, 0.42f, 0.45f)), white, "HomeGable_" + style, grey);
            PrimitiveKit.Box("RoofVeranda", t, new Vector3(0f, floorY + 2.4f, -3.2f), new Vector3(RoomW + 0.6f, 0.1f, 1.9f), grey, new Vector3(-6f, 0f, 0f), collider: false);
            foreach (float x in new[] { -2.9f, 2.9f })
                PrimitiveKit.Box("Column", t, new Vector3(x, floorY + 1.2f, -3.9f), new Vector3(0.2f, 2.4f, 0.2f), white);
        }
        Steps(t, floorY, 1.6f, -3.9f, slab);
        // a fence round the lawn with a gate in front, a carport bay at the side
        // (sides stop short of the front and back runs, so no two faces share a plane and flicker)
        var fence = PrimitiveKit.Mat("FenceMetal", new Color(0.2f, 0.22f, 0.24f));
        Railing(t, "FenceFrontL", new Vector3(-5.4f, 0f, -6.4f), new Vector3(-1.2f, 0f, -6.4f), white, fence, style);
        Railing(t, "FenceFrontR", new Vector3(2.4f, 0f, -6.4f), new Vector3(5.4f, 0f, -6.4f), white, fence, style);
        Railing(t, "FenceSideL", new Vector3(-5.4f, 0f, -6.2f), new Vector3(-5.4f, 0f, 4.2f), white, fence, style);
        Railing(t, "FenceSideR", new Vector3(5.4f, 0f, -6.2f), new Vector3(5.4f, 0f, 4.2f), white, fence, style);
        Railing(t, "FenceBack", new Vector3(-5.4f, 0f, 4.4f), new Vector3(5.4f, 0f, 4.4f), white, fence, style);
        Pot(t, new Vector3(-2.7f, 0f, -4.4f), 9);
        Nature.Bush(t, new Vector3(-4.3f, 0f, 2.5f), 1.1f, 11, flowers: true);
        Finish(t, style, floorY, -3.1f);
    }
}
