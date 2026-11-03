using UnityEngine;

// The land you can buy at the bank (land.lua), laid out where it lies. Each parcel carries a LandSite
// that shows it the way it is for this player: a for-sale board until it's yours, then what you built
// ("Build_<id>" groups). A LandCounter at its board opens LandUI.
//   Village, beside Kok Nong Na and the cow farm: ที่นาติดคลอง 5 ไร่ (paddies 7-12 by a canal) and
//                                             ที่ดินเปล่า 1 ไร่ (crop field / second cow pen / house to let)
//   City, next to the bank:                    ที่ดินติดถนน (a shophouse to let)
public static class LandParcels
{
    public static readonly Vector3 RiceLand = new Vector3(21f, 0f, -49.5f);  // south of the cow farm
    public static readonly Vector3 PlainLand = new Vector3(46.5f, 0f, -31f); // east of the cow farm, front to the road
    public static readonly Vector3 CityLot = new Vector3(48f, 0f, -31f);     // east of the bank, front to the road

    // Thai vowels like ำ need the game font (Mitr); the built-in one drops them
    private static TextMesh Thai(TextMesh label)
    {
        var font = Resources.Load<Font>("Fonts/Mitr-SemiBold");
        if (font == null) return label;
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        return label;
    }

    // A painted board on two posts, read from its local -Z side
    private static void Board(Transform parent, Vector3 at, float yaw, string title, string sub, Color face, Color ink)
    {
        var g = PrimitiveKit.Group("Board", parent, at, yaw).transform;
        var post = PrimitiveKit.Mat("BoardPost", new Color(0.35f, 0.24f, 0.15f));
        foreach (float x in new[] { -1.3f, 1.3f })
            PrimitiveKit.Box("Post", g, new Vector3(x, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.12f), post);
        PrimitiveKit.Box("Face", g, new Vector3(0f, 1.75f, 0f), new Vector3(3f, 1.3f, 0.08f), PrimitiveKit.Mat("Board" + face.GetHashCode(), face), collider: false);
        Thai(PrimitiveKit.Label(title, g, new Vector3(0f, 2.05f, -0.06f), 0.075f, ink));
        Thai(PrimitiveKit.Label(sub, g, new Vector3(0f, 1.5f, -0.06f), 0.04f, ink));
    }

    private static void Counter(Transform parent, Vector3 at, Vector3 size, string parcel)
    {
        var zone = new GameObject("LandBoardZone");
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = at;
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        zone.AddComponent<LandCounter>().Configure(parcel);
    }

    private static readonly Color SaleRed = new Color(0.85f, 0.15f, 0.12f), Cream = new Color(0.98f, 0.95f, 0.85f);
    private static readonly Color OwnGreen = new Color(0.2f, 0.5f, 0.25f);

    private static (Transform forSale, Transform owned) SaleAndOwned(Transform site, Vector3 boardAt, float boardYaw, string forSale, string forSaleSub, string owned, string ownedSub)
    {
        var sale = PrimitiveKit.Group("ForSale", site, Vector3.zero).transform;
        Board(sale, boardAt, boardYaw, forSale, forSaleSub, Cream, SaleRed);
        var mine = PrimitiveKit.Group("Owned", site, Vector3.zero).transform;
        Board(mine, boardAt, boardYaw, owned, ownedSub, OwnGreen, Cream);
        return (sale, mine);
    }

    // ── Village ─────────────────────────────────────────────────────────────────────────────
    public static void BuildVillage(Transform v)
    {
        RiceParcel(v);
        PlainParcel(v);
    }

    // ที่นาติดคลอง: 3x2 paddies like Kok Nong Na's (FarmPlot 7..12) with a canal along the south side
    private static void RiceParcel(Transform v)
    {
        var site = PrimitiveKit.Group("Land_ที่นาติดคลอง", v, RiceLand).transform;
        var paddy = PrimitiveKit.Mat("PaddyWater", new Color(0.58f, 0.64f, 0.36f), 0.82f);
        var dike = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        var water = PrimitiveKit.Mat("CanalWater", new Color(0.25f, 0.45f, 0.5f));
        int id = 6;
        foreach (float x in new[] { -8f, 0f, 8f })
        foreach (float z in new[] { 3.75f, -3.75f })
        {
            PrimitiveKit.Box("Paddy", site, new Vector3(x, 0.015f, z), new Vector3(7.6f, 0.03f, 7.1f), paddy, collider: false);
            var plot = new GameObject($"FarmPlot_{++id}");
            plot.transform.SetParent(site, false);
            plot.transform.localPosition = new Vector3(x, 1f, z);
            var trigger = plot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(7.2f, 2f, 6.7f);
            plot.AddComponent<FarmPlot>().Configure(id, new Vector2(7.5f, 7.0f));
            PrimitiveKit.Box("DikeW", site, new Vector3(x - 3.85f, 0.12f, z), new Vector3(0.3f, 0.25f, 7.6f), dike);
        }
        foreach (float z in new[] { 7.4f, 0f, -7.4f })
            PrimitiveKit.Box("DikeEW", site, new Vector3(0f, 0.12f, z), new Vector3(24.3f, 0.25f, 0.3f), dike);
        PrimitiveKit.Box("DikeE", site, new Vector3(12f, 0.12f, 0f), new Vector3(0.3f, 0.25f, 15f), dike);

        // คลอง along the south: water between grassy banks, a little footbridge-free plank landing
        PrimitiveKit.Box("CanalBank", site, new Vector3(0f, 0.01f, -9f), new Vector3(26f, 0.02f, 3.4f), dike, collider: false);
        PrimitiveKit.Box("Canal", site, new Vector3(0f, 0.02f, -9.1f), new Vector3(26f, 0.02f, 2.2f), water, collider: false);
        PrimitiveKit.Box("Landing", site, new Vector3(-6f, 0.08f, -7.9f), new Vector3(1.6f, 0.06f, 0.9f), PrimitiveKit.Mat("WoodPlank", new Color(0.55f, 0.38f, 0.22f)), collider: false);

        var land = site.gameObject.AddComponent<LandSite>();
        land.Configure("rice");
        // The board at the north-west corner, facing west toward Kok Nong Na's pond
        SaleAndOwned(site, new Vector3(-13.2f, 0f, 6.5f), 90f, "ที่นาขาย ติดคลอง 5 ไร่", "ซื้อได้ที่ธนาคารในเมือง · แปลงนา 7-12",
            "ที่นาของคุณ", "แปลงนา 7-12 · เดินเข้าแปลงเพื่อทำนา");
        Counter(site, new Vector3(-14.4f, 1f, 6.5f), new Vector3(2.4f, 2f, 3f), "rice");
    }

    // ที่ดินเปล่า 1 ไร่: front (local -Z) to the road; a field, a pen or a house goes up on it
    private static void PlainParcel(Transform v)
    {
        var site = PrimitiveKit.Group("Land_ที่ดินเปล่า", v, PlainLand, 180f).transform;
        const float hx = 9.5f, hz = 7f;
        var stake = PrimitiveKit.Mat("LandStake", new Color(0.85f, 0.85f, 0.82f));
        var red = PrimitiveKit.Mat("LandStakeRed", new Color(0.8f, 0.15f, 0.1f));
        foreach (float x in new[] { -hx, hx })
        foreach (float z in new[] { -hz, hz })
        {
            PrimitiveKit.Box("หมุดที่ดิน", site, new Vector3(x, 0.3f, z), new Vector3(0.15f, 0.6f, 0.15f), stake);
            PrimitiveKit.Box("StakeTop", site, new Vector3(x, 0.62f, z), new Vector3(0.16f, 0.06f, 0.16f), red, collider: false);
        }
        site.gameObject.AddComponent<LandSite>().Configure("plain");
        var (_, owned) = SaleAndOwned(site, new Vector3(0f, 0f, -hz - 0.6f), 0f, "ที่ดินขาย 1 ไร่", "ซื้อได้ที่ธนาคารในเมือง · เลเวล 50",
            "ที่ดินของคุณ 1 ไร่", "แตะป้าย: สร้าง · ทำไร่ · เก็บค่าเช่า");
        PrimitiveKit.Box("Cleared", owned, new Vector3(0f, 0.012f, 0f), new Vector3(hx * 2f, 0.02f, hz * 2f), PrimitiveKit.Mat("FieldDirt", new Color(0.52f, 0.4f, 0.26f)), collider: false);
        Counter(site, new Vector3(0f, 1f, -hz - 2f), new Vector3(3f, 2f, 2.4f), "plain");

        // ไร่: four plots with ridges between them
        var field = PrimitiveKit.Group("Build_field", site, Vector3.zero).transform;
        var ridge = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        int id = 0;
        foreach (float z in new[] { -3.3f, 3.3f })
        foreach (float x in new[] { -4.65f, 4.65f })
        {
            var plot = new GameObject($"FieldPlot_{++id}");
            plot.transform.SetParent(field, false);
            plot.transform.localPosition = new Vector3(x, 1f, z);
            var trigger = plot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(8f, 2f, 5.4f);
            plot.AddComponent<FieldPlot>().Configure(id, new Vector2(8.4f, 5.8f));
        }
        PrimitiveKit.Box("RidgeX", field, new Vector3(0f, 0.1f, 0f), new Vector3(hx * 2f, 0.2f, 0.4f), ridge, collider: false);
        PrimitiveKit.Box("RidgeZ", field, new Vector3(0f, 0.1f, 0f), new Vector3(0.4f, 0.2f, hz * 2f), ridge, collider: false);
        var sack = PrimitiveKit.Mat("Sack", new Color(0.85f, 0.78f, 0.6f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Cylinder("Sack", field, new Vector3(2.2f + i * 0.55f, 0.35f, -hz - 0.9f), new Vector3(0.45f, 0.35f, 0.45f), sack, collider: false);

        // คอกวัวแปลงใหม่: a fenced pen with room for eight more cows (CowPen on land: the cows past the village pen)
        var pen = PrimitiveKit.Group("Build_pen", site, Vector3.zero).transform;
        var fence = PrimitiveKit.Mat("FenceWood", new Color(0.62f, 0.45f, 0.28f));
        for (float x = -hx; x <= hx + 0.01f; x += 3.8f)
            foreach (float z in new[] { -hz, hz })
                PrimitiveKit.Box("FencePost", pen, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), fence);
        foreach (float y in new[] { 0.5f, 0.95f })
        {
            PrimitiveKit.Box("Rail", pen, new Vector3(0f, y, hz), new Vector3(hx * 2f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailL", pen, new Vector3(-hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailR", pen, new Vector3(hx, y, 0f), new Vector3(0.08f, 0.1f, hz * 2f), fence);
            PrimitiveKit.Box("RailFrontL", pen, new Vector3(-5.75f, y, -hz), new Vector3(7.5f, 0.1f, 0.08f), fence);
            PrimitiveKit.Box("RailFrontR", pen, new Vector3(5.75f, y, -hz), new Vector3(7.5f, 0.1f, 0.08f), fence);
        }
        PrimitiveKit.Box("Trough", pen, new Vector3(0f, 0.3f, 5.6f), new Vector3(3f, 0.5f, 0.7f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)));
        var hay = PrimitiveKit.Mat("Hay", new Color(0.9f, 0.78f, 0.38f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("HayBale", pen, new Vector3(-6f + i * 1.05f, 0.3f, 5.8f), new Vector3(1f, 0.6f, 0.7f), hay);
        var spots = new[] { new Vector2(-6f, -3f), new Vector2(-2f, -3.5f), new Vector2(2f, -3f), new Vector2(6f, -3.5f),
                            new Vector2(-6f, 2f), new Vector2(-2f, 1.5f), new Vector2(2.5f, 2f), new Vector2(6f, 1.5f) };
        for (int i = 0; i < spots.Length; i++)
            Seaside.Cow(pen, new Vector3(spots[i].x, 0f, spots[i].y), i + 20, new Vector2(Mathf.Max(1f, hx - 1.5f - Mathf.Abs(spots[i].x)), Mathf.Max(0.8f, hz - 1.5f - Mathf.Abs(spots[i].y))));
        pen.gameObject.AddComponent<CowPen>().Configure(true);
        var penZone = new GameObject("CowPenZone");
        penZone.transform.SetParent(pen, false);
        penZone.transform.localPosition = new Vector3(0f, 1f, 4.4f);
        var penBox = penZone.AddComponent<BoxCollider>();
        penBox.isTrigger = true;
        penBox.size = new Vector3(3.4f, 2f, 1.6f);
        penZone.AddComponent<RanchCounter>().Configure("pen");

        // บ้านเช่า: a small rendered house with a gable roof, let to a family
        var house = PrimitiveKit.Group("Build_rental", site, new Vector3(0f, 0f, 1.5f)).transform;
        var wall = PrimitiveKit.Mat("RentalWall", new Color(0.95f, 0.9f, 0.78f));
        var roof = PrimitiveKit.Mat("RentalRoof", new Color(0.62f, 0.2f, 0.15f));
        var trim = PrimitiveKit.Mat("RentalTrim", new Color(0.4f, 0.26f, 0.16f));
        var glass = PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f));
        PrimitiveKit.Box("Floor", house, new Vector3(0f, 0.15f, 0f), new Vector3(10f, 0.3f, 7.5f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)));
        PrimitiveKit.Box("Walls", house, new Vector3(0f, 1.8f, 0f), new Vector3(9f, 3f, 6.5f), wall);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Roof", house, new Vector3(0f, 4.05f, side * 1.85f), new Vector3(10f, 0.15f, 4.3f), roof, new Vector3(side * 30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Door", house, new Vector3(0f, 1.3f, -3.27f), new Vector3(1.1f, 2.2f, 0.06f), trim, collider: false);
        foreach (float x in new[] { -2.8f, 2.8f })
        {
            PrimitiveKit.Box("Window", house, new Vector3(x, 1.9f, -3.27f), new Vector3(1.4f, 1.1f, 0.05f), glass, collider: false);
            PrimitiveKit.Box("Sill", house, new Vector3(x, 1.3f, -3.3f), new Vector3(1.6f, 0.08f, 0.12f), trim, collider: false);
        }
        PrimitiveKit.Box("Step", house, new Vector3(0f, 0.08f, -4.1f), new Vector3(2f, 0.16f, 0.8f), PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f)));
        StreetFurniture.OrangeLight(house, new Vector3(0f, 2.9f, -3.5f));
        Thai(PrimitiveKit.Label("บ้านเช่า", house, new Vector3(0f, 3.15f, -3.32f), 0.06f, new Color(0.45f, 0.18f, 0.1f)));
        var plants = PrimitiveKit.Mat("PotPlant", new Color(0.25f, 0.55f, 0.2f));
        foreach (float x in new[] { -1.6f, 1.6f })
            PrimitiveKit.Sphere("PotPlant", house, new Vector3(x, 0.55f, -3.9f), new Vector3(0.6f, 0.5f, 0.6f), plants);
    }

    // ── City: a lot on the main road next to the bank; a shophouse to let goes up on it ──────
    public static void BuildCity(Transform root)
    {
        var site = PrimitiveKit.Group("Land_ที่ดินในเมือง", root, CityLot, 180f).transform;
        const float hx = 8f, hz = 6.5f;
        PrimitiveKit.Box("LotGround", site, new Vector3(0f, 0.01f, 0f), new Vector3(hx * 2f, 0.012f, hz * 2f), PrimitiveKit.Mat("LotGravel", new Color(0.6f, 0.57f, 0.5f)), collider: false);
        site.gameObject.AddComponent<LandSite>().Configure("city");
        var (sale, _) = SaleAndOwned(site, new Vector3(-4.5f, 0f, -hz + 0.4f), 0f, "ที่ดินติดถนนขาย", "ซื้อได้ที่ธนาคารข้างๆ · เลเวล 50",
            "ที่ดินของคุณ", "แตะป้าย: สร้างตึกแถว · เก็บค่าเช่า");
        var post = PrimitiveKit.Mat("LotPost", new Color(0.8f, 0.8f, 0.78f));
        for (float x = -hx; x <= hx + 0.01f; x += 4f)
            PrimitiveKit.Box("LotPost", sale, new Vector3(x, 0.45f, -hz), new Vector3(0.12f, 0.9f, 0.12f), post);
        PrimitiveKit.Box("LotRope", sale, new Vector3(0f, 0.8f, -hz), new Vector3(hx * 2f, 0.03f, 0.03f), PrimitiveKit.Mat("LotRope", new Color(0.85f, 0.2f, 0.15f)), collider: false);
        Counter(site, new Vector3(-4.5f, 1f, -hz - 0.9f), new Vector3(3f, 2f, 2f), "city");

        var build = PrimitiveKit.Group("Build_shophouse", site, Vector3.zero).transform;
        CityCivic.Block(build, new Vector3(1.5f, 0f, 1.2f), 0f, "ตึกแถวของคุณ", 16f, 13f, 3, new Color(0.96f, 0.9f, 0.8f), new Color(0.55f, 0.3f, 0.2f),
            new Color(0.55f, 0.3f, 0.2f), new Color(1f, 0.9f, 0.6f), "Shophouse_ตึกแถว", "ให้เช่า", "office");
    }
}
