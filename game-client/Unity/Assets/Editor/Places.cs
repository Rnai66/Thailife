using UnityEngine;

// Builders for Thai places made from primitives. Each builds under a group whose front faces local -Z.
// Used by VillageBuilder (หมู่บ้าน) and TambonBuilder (ตำบล).
public static class Places
{
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);
    private static readonly Color DarkWood = new Color(0.36f, 0.22f, 0.12f);
    private static readonly Color TempleWhite = new Color(0.95f, 0.94f, 0.9f);
    private static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);
    private static readonly Color TempleOrange = new Color(0.9f, 0.45f, 0.12f);
    private static readonly Color TempleGreen = new Color(0.15f, 0.45f, 0.3f);

    // ── Temple (วัด) ────────────────────────────────────────────────────────────
    public static GameObject Temple(Transform parent, Vector3 position, string name)
    {
        var temple = PrimitiveKit.Group("Temple_วัด", parent, position);
        var t = temple.transform;
        var white = PrimitiveKit.Mat("TempleWhite", TempleWhite);
        var gold = PrimitiveKit.Mat("Gold", Gold);
        var orange = PrimitiveKit.Mat("TempleOrange", TempleOrange);
        var green = PrimitiveKit.Mat("TempleGreen", TempleGreen);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);

        // Ordination hall (โบสถ์): raised base, white walls, two-tier roof with ridge along Z
        var hall = PrimitiveKit.Group("Ubosot_โบสถ์", t, new Vector3(0f, 0f, 3f));
        var h = hall.transform;
        PrimitiveKit.Box("Base", h, new Vector3(0f, 0.3f, 0f), new Vector3(10f, 0.6f, 16f), white);
        PrimitiveKit.Box("Step", h, new Vector3(0f, 0.15f, -8.5f), new Vector3(4f, 0.3f, 1f), white);
        PrimitiveKit.Box("Walls", h, new Vector3(0f, 2.6f, 0f), new Vector3(8f, 4f, 14f), white);
        PrimitiveKit.Box("Door", h, new Vector3(0f, 2f, -7.03f), new Vector3(1.6f, 2.8f, 0.05f), gold, collider: false);
        PrimitiveKit.Box("Pediment", h, new Vector3(0f, 6.4f, -7.08f), new Vector3(3f, 3f, 0.08f), gold, new Vector3(0f, 0f, 45f), collider: false);
        PrimitiveKit.Box("RoofL", h, new Vector3(-2.15f, 6.4f, 0f), new Vector3(5.6f, 0.15f, 15.6f), orange, new Vector3(0f, 0f, 40f));
        PrimitiveKit.Box("RoofR", h, new Vector3(2.15f, 6.4f, 0f), new Vector3(5.6f, 0.15f, 15.6f), orange, new Vector3(0f, 0f, -40f));
        PrimitiveKit.Box("UpperRoofL", h, new Vector3(-1.3f, 7.6f, 0f), new Vector3(3.4f, 0.15f, 12f), green, new Vector3(0f, 0f, 40f), collider: false);
        PrimitiveKit.Box("UpperRoofR", h, new Vector3(1.3f, 7.6f, 0f), new Vector3(3.4f, 0.15f, 12f), green, new Vector3(0f, 0f, -40f), collider: false);
        PrimitiveKit.Box("ChofaFront", h, new Vector3(0f, 9.1f, -6.1f), new Vector3(0.12f, 1.1f, 0.12f), gold, new Vector3(-25f, 0f, 0f), collider: false);
        PrimitiveKit.Box("ChofaBack", h, new Vector3(0f, 9.1f, 6.1f), new Vector3(0.12f, 1.1f, 0.12f), gold, new Vector3(25f, 0f, 0f), collider: false);

        // Stupa (เจดีย์)
        var chedi = PrimitiveKit.Group("Chedi_เจดีย์", t, new Vector3(9f, 0f, 9f));
        var c = chedi.transform;
        PrimitiveKit.Box("Base1", c, new Vector3(0f, 0.3f, 0f), new Vector3(4f, 0.6f, 4f), white);
        PrimitiveKit.Box("Base2", c, new Vector3(0f, 0.9f, 0f), new Vector3(3f, 0.6f, 3f), white);
        PrimitiveKit.Cylinder("Drum", c, new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, 0.4f, 2.2f), white);
        PrimitiveKit.Sphere("Bell", c, new Vector3(0f, 3f, 0f), new Vector3(2.6f, 2.6f, 2.6f), white, collider: false);
        PrimitiveKit.Cylinder("Harmika", c, new Vector3(0f, 4.4f, 0f), new Vector3(0.7f, 0.3f, 0.7f), gold, collider: false);
        PrimitiveKit.Cylinder("Spire", c, new Vector3(0f, 5.6f, 0f), new Vector3(0.35f, 1f, 0.35f), gold, collider: false);
        PrimitiveKit.Cylinder("Tip", c, new Vector3(0f, 7f, 0f), new Vector3(0.1f, 0.5f, 0.1f), gold, collider: false);

        // Open pavilion (ศาลา)
        var sala = PrimitiveKit.Group("Sala_ศาลา", t, new Vector3(-9f, 0f, -3f));
        var s = sala.transform;
        PrimitiveKit.Box("Floor", s, new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 4f), wood);
        foreach (float x in new[] { -2.7f, 0f, 2.7f })
        foreach (float z in new[] { -1.7f, 1.7f })
            PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.75f, z), new Vector3(0.18f, 1.25f, 0.18f), wood);
        PrimitiveKit.Box("RoofFront", s, new Vector3(0f, 3.5f, -1.05f), new Vector3(6.8f, 0.12f, 2.8f), orange, new Vector3(-35f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", s, new Vector3(0f, 3.5f, 1.05f), new Vector3(6.8f, 0.12f, 2.8f), orange, new Vector3(35f, 0f, 0f));

        BigTree(t, new Vector3(-9f, 0f, 9f), "BodhiTree_ต้นโพธิ์", 1.4f);

        TempleFair(t);

        // Low white boundary wall with a gate in the front (-Z)
        PrimitiveKit.Box("WallBack", t, new Vector3(0f, 0.5f, 14f), new Vector3(30f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallL", t, new Vector3(-15f, 0.5f, 0f), new Vector3(0.3f, 1f, 28f), white);
        PrimitiveKit.Box("WallR", t, new Vector3(15f, 0.5f, 0f), new Vector3(0.3f, 1f, 28f), white);
        PrimitiveKit.Box("WallFrontL", t, new Vector3(-8.5f, 0.5f, -14f), new Vector3(13f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallFrontR", t, new Vector3(8.5f, 0.5f, -14f), new Vector3(13f, 1f, 0.3f), white);
        Gate(t, new Vector3(0f, 0f, -14f), name, white, gold, new Color(0.6f, 0.1f, 0.1f));

        return temple;
    }

    // Temple fair (งานวัด) corner: chicken beauty contest stage + a couple of fair stalls.
    // The contest only runs during the monthly fair (server-side schedule).
    private static void TempleFair(Transform temple)
    {
        var fair = PrimitiveKit.Group("TempleFair_งานวัด", temple, new Vector3(8f, 0f, -7f));
        var t = fair.transform;
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var banner = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var pink = PrimitiveKit.Mat("FlowerPink", new Color(1f, 0.45f, 0.7f));
        var blue = PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f));

        // Contest stage (เวทีประกวดไก่สวย)
        PrimitiveKit.Box("ContestStage", t, new Vector3(0f, 0.2f, 0f), new Vector3(3.6f, 0.4f, 3f), red);
        foreach (float x in new[] { -1.7f, 1.7f })
            PrimitiveKit.Cylinder("BannerPole", t, new Vector3(x, 1.6f, 1.4f), new Vector3(0.08f, 1.6f, 0.08f), wood, collider: false);
        PrimitiveKit.Box("Banner", t, new Vector3(0f, 2.9f, 1.4f), new Vector3(3.6f, 0.6f, 0.06f), banner, collider: false);
        PrimitiveKit.Label("ประกวดไก่สวย", t, new Vector3(0f, 2.9f, 1.36f), 0.06f, new Color(0.7f, 0.1f, 0.1f));

        var contest = new GameObject("ContestZone");
        contest.transform.SetParent(t, false);
        contest.transform.localPosition = new Vector3(0f, 1f, -2.3f);
        var box = contest.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.6f, 2f, 1.6f);
        contest.AddComponent<WindowSpot>().Configure(WindowSpot.Window.Contest);
        Sign(t, new Vector3(-2.6f, 0f, -2.2f), "ประกวดไก่สวย\nงานวัด เดือนละครั้ง", 0f);

        // Fair stalls (ซุ้มงานวัด) with striped roofs
        for (int i = 0; i < 2; i++)
        {
            // left of the gate path so the way to the ordination hall stays clear
            var stall = PrimitiveKit.Group("FairStall", t, new Vector3(-13.5f - i * 3.5f, 0f, -2f));
            PrimitiveKit.Box("Counter", stall.transform, new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.9f, 0.8f), wood);
            foreach (float x in new[] { -1.2f, 1.2f })
                PrimitiveKit.Cylinder("Pole", stall.transform, new Vector3(x, 1.2f, 0.3f), new Vector3(0.06f, 1.2f, 0.06f), wood, collider: false);
            PrimitiveKit.Box("Roof", stall.transform, new Vector3(0f, 2.4f, 0f), new Vector3(3f, 0.08f, 1.6f), i == 0 ? pink : blue, collider: false);
        }
    }

    // ── School (โรงเรียน) ───────────────────────────────────────────────────────
    public static GameObject School(Transform parent, Vector3 position, string name, float yaw = 0f)
    {
        var school = PrimitiveKit.Group("School_โรงเรียน", parent, position, yaw);
        var t = school.transform;
        var cream = PrimitiveKit.Mat("SchoolCream", new Color(0.95f, 0.88f, 0.7f));
        var roof = PrimitiveKit.Mat("RoofRed", new Color(0.62f, 0.18f, 0.12f));
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var field = PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f));
        var gate = PrimitiveKit.Mat("SchoolGate", new Color(0.2f, 0.35f, 0.7f));

        // Two-storey classroom building
        PrimitiveKit.Box("Building", t, new Vector3(0f, 3f, 0f), new Vector3(16f, 6f, 6f), cream);
        for (int floor = 0; floor < 2; floor++)
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Box("Window", t, new Vector3(-6.25f + i * 2.5f, 1.6f + floor * 3f, -3.03f), new Vector3(1.4f, 1.2f, 0.05f), window, collider: false);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 6.85f, -1.6f), new Vector3(17f, 0.15f, 3.8f), roof, new Vector3(-30f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 6.85f, 1.6f), new Vector3(17f, 0.15f, 3.8f), roof, new Vector3(30f, 0f, 0f));

        // Flagpole with the Thai flag (red-white-blue-white-red)
        PrimitiveKit.Cylinder("Flagpole", t, new Vector3(0f, 4f, -8f), new Vector3(0.12f, 4f, 0.12f), white);
        Color[] stripes = { new Color(0.75f, 0.1f, 0.15f), Color.white, new Color(0.1f, 0.15f, 0.45f), Color.white, new Color(0.75f, 0.1f, 0.15f) };
        float[] heights = { 0.2f, 0.2f, 0.4f, 0.2f, 0.2f };
        float y = 7.6f;
        for (int i = 0; i < stripes.Length; i++)
        {
            y -= heights[i] * 0.5f;
            PrimitiveKit.Box("Flag", t, new Vector3(0.75f, y, -8f), new Vector3(1.5f, heights[i], 0.03f),
                PrimitiveKit.Mat($"Flag{i}", stripes[i]), collider: false);
            y -= heights[i] * 0.5f;
        }

        // Football field beside the building, with goals
        var pitch = PrimitiveKit.Group("FootballField", t, new Vector3(18f, 0f, -4f));
        PrimitiveKit.Box("Pitch", pitch.transform, new Vector3(0f, 0.012f, 0f), new Vector3(12f, 0.02f, 20f), field, collider: false);
        PrimitiveKit.Box("Halfway", pitch.transform, new Vector3(0f, 0.025f, 0f), new Vector3(12f, 0.01f, 0.12f), white, collider: false);
        foreach (float z in new[] { -10f, 10f })
        {
            PrimitiveKit.Box("Crossbar", pitch.transform, new Vector3(0f, 2f, z), new Vector3(4f, 0.1f, 0.1f), white);
            PrimitiveKit.Box("PostL", pitch.transform, new Vector3(-2f, 1f, z), new Vector3(0.1f, 2f, 0.1f), white);
            PrimitiveKit.Box("PostR", pitch.transform, new Vector3(2f, 1f, z), new Vector3(0.1f, 2f, 0.1f), white);
        }

        Gate(t, new Vector3(0f, 0f, -13f), name, gate, PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), new Color(0.15f, 0.2f, 0.5f));
        return school;
    }

    // ── Sub-district health promoting hospital (รพ.สต. / สถานีอนามัย) ─────────────
    public static GameObject HealthStation(Transform parent, Vector3 position, float yaw, string name)
    {
        var station = PrimitiveKit.Group("HealthStation_รพสต", parent, position, yaw);
        var t = station.transform;
        var white = PrimitiveKit.Mat("ClinicWhite", new Color(0.96f, 0.97f, 0.96f));
        var green = PrimitiveKit.Mat("ClinicGreen", new Color(0.1f, 0.55f, 0.35f));
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));

        PrimitiveKit.Box("Building", t, new Vector3(0f, 1.75f, 0f), new Vector3(12f, 3.5f, 7f), white);
        PrimitiveKit.Box("Stripe", t, new Vector3(0f, 3.3f, -3.52f), new Vector3(12f, 0.3f, 0.05f), green, collider: false);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 4.1f, -1.85f), new Vector3(12.8f, 0.15f, 4.3f), green, new Vector3(-22f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 4.1f, 1.85f), new Vector3(12.8f, 0.15f, 4.3f), green, new Vector3(22f, 0f, 0f));
        PrimitiveKit.Box("Door", t, new Vector3(0f, 1.2f, -3.53f), new Vector3(2f, 2.4f, 0.05f), window, collider: false);
        foreach (float x in new[] { -4f, -2.2f, 2.2f, 4f })
            PrimitiveKit.Box("Window", t, new Vector3(x, 1.8f, -3.53f), new Vector3(1.2f, 1f, 0.05f), window, collider: false);

        // Entrance canopy and sign with a green cross
        PrimitiveKit.Box("Canopy", t, new Vector3(0f, 2.9f, -5f), new Vector3(4.5f, 0.15f, 3f), green, collider: false);
        PrimitiveKit.Cylinder("CanopyPostL", t, new Vector3(-2f, 1.4f, -6.3f), new Vector3(0.15f, 1.4f, 0.15f), white);
        PrimitiveKit.Cylinder("CanopyPostR", t, new Vector3(2f, 1.4f, -6.3f), new Vector3(0.15f, 1.4f, 0.15f), white);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 3.4f, -6.55f), new Vector3(4.5f, 0.8f, 0.1f), green, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0.4f, 3.4f, -6.62f), 0.075f, Color.white);
        PrimitiveKit.Box("CrossV", t, new Vector3(-1.75f, 3.4f, -6.62f), new Vector3(0.14f, 0.5f, 0.02f), white, collider: false);
        PrimitiveKit.Box("CrossH", t, new Vector3(-1.75f, 3.4f, -6.62f), new Vector3(0.5f, 0.14f, 0.02f), white, collider: false);
        return station;
    }

    // ── Kok Nong Na model (โคก หนอง นา): paddies, a pond and a raised mound ─────────
    public static GameObject KokNongNa(Transform parent, Vector3 position)
    {
        var farm = PrimitiveKit.Group("KokNongNa_โคกหนองนา", parent, position);
        var t = farm.transform;
        var rice = PrimitiveKit.Mat("RicePaddy", new Color(0.55f, 0.75f, 0.25f));
        var dike = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));
        var water = PrimitiveKit.Mat("Water", new Color(0.25f, 0.5f, 0.75f));
        var earth = PrimitiveKit.Mat("Earth", new Color(0.5f, 0.38f, 0.24f));
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.75f, 0.62f, 0.35f));
        var wood = PrimitiveKit.Mat("DarkWood", DarkWood);

        // นา: 3x2 rice paddies separated by low dikes (0.25 m — walkable); each is a farm plot (ids 1..6)
        int plotId = 0;
        foreach (float x in new[] { -16f, -8f, 0f })
        foreach (float z in new[] { -4f, 3.5f })
        {
            PrimitiveKit.Box("Paddy", t, new Vector3(x, 0.015f, z), new Vector3(7.6f, 0.03f, 7.1f), rice, collider: false);

            var plotZone = new GameObject($"FarmPlot_{++plotId}");
            plotZone.transform.SetParent(t, false);
            plotZone.transform.localPosition = new Vector3(x, 1f, z);
            var trigger = plotZone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(7.2f, 2f, 6.7f);
            plotZone.AddComponent<FarmPlot>().Configure(plotId, new Vector2(7.2f, 6.7f));
            PrimitiveKit.Box("DikeN", t, new Vector3(x, 0.12f, z + 3.65f), new Vector3(8f, 0.25f, 0.3f), dike);
            PrimitiveKit.Box("DikeS", t, new Vector3(x, 0.12f, z - 3.65f), new Vector3(8f, 0.25f, 0.3f), dike);
            PrimitiveKit.Box("DikeW", t, new Vector3(x - 3.85f, 0.12f, z), new Vector3(0.3f, 0.25f, 7.6f), dike);
        }
        PrimitiveKit.Box("DikeE", t, new Vector3(3.85f, 0.12f, 0f), new Vector3(0.3f, 0.25f, 15f), dike);

        // หนอง: farm pond
        PrimitiveKit.Box("PondRim", t, new Vector3(11f, 0.01f, -3f), new Vector3(9f, 0.02f, 11f), earth, collider: false);
        PrimitiveKit.Box("Pond", t, new Vector3(11f, 0.025f, -3f), new Vector3(8f, 0.02f, 10f), water, collider: false);

        // โคก: terraced mound with fruit trees
        PrimitiveKit.Box("Mound1", t, new Vector3(11f, 0.15f, 8f), new Vector3(10f, 0.3f, 7f), earth);
        PrimitiveKit.Box("Mound2", t, new Vector3(11f, 0.45f, 8.5f), new Vector3(7f, 0.3f, 5f), earth);
        BananaTree(t, new Vector3(9f, 0.6f, 9f), trainable: false);
        BananaTree(t, new Vector3(13f, 0.6f, 8f), trainable: false);
        ShadeTree(t, new Vector3(11f, 0.6f, 10.5f), 0.7f);

        // เถียงนา: small rest hut on stilts
        var hut = PrimitiveKit.Group("Hut_เถียงนา", t, new Vector3(-8f, 0f, 11f));
        foreach (float x in new[] { -1.2f, 1.2f })
        foreach (float z in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Stilt", hut.transform, new Vector3(x, 0.5f, z), new Vector3(0.15f, 0.5f, 0.15f), wood);
        PrimitiveKit.Box("Floor", hut.transform, new Vector3(0f, 1.05f, 0f), new Vector3(2.8f, 0.1f, 2.4f), wood);
        foreach (float x in new[] { -1.2f, 1.2f })
        foreach (float z in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Post", hut.transform, new Vector3(x, 1.8f, z), new Vector3(0.1f, 0.75f, 0.1f), wood, collider: false);
        PrimitiveKit.Box("RoofFront", hut.transform, new Vector3(0f, 2.75f, -0.75f), new Vector3(3.4f, 0.12f, 1.9f), thatch, new Vector3(-35f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", hut.transform, new Vector3(0f, 2.75f, 0.75f), new Vector3(3.4f, 0.12f, 1.9f), thatch, new Vector3(35f, 0f, 0f), collider: false);

        Sign(t, new Vector3(2f, 0f, 14f), "โคก หนอง นา\nเดินเข้าแปลงเพื่อทำนา", 180f);
        return farm;
    }

    // ── Songthaew stop (ป้ายรถสองแถว): opens the travel map; arrival spawn in front ──
    public static GameObject BusStop(Transform parent, Vector3 position, float yaw)
    {
        var stop = PrimitiveKit.Group("BusStop_ป้ายรถสองแถว", parent, position, yaw);
        var t = stop.transform;
        var metal = PrimitiveKit.Mat("Metal", new Color(0.35f, 0.35f, 0.38f));
        var roof = PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var sign = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));

        foreach (float x in new[] { -1.6f, 1.6f })
            PrimitiveKit.Cylinder("Post", t, new Vector3(x, 1.25f, 0.6f), new Vector3(0.1f, 1.25f, 0.1f), metal);
        PrimitiveKit.Box("BackPanel", t, new Vector3(0f, 1.3f, 0.65f), new Vector3(3.3f, 1.6f, 0.05f), roof);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 2.55f, 0.1f), new Vector3(3.8f, 0.1f, 1.6f), roof, collider: false);
        PrimitiveKit.Box("Bench", t, new Vector3(0f, 0.45f, 0.35f), new Vector3(2.8f, 0.1f, 0.45f), wood, collider: false);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 2.95f, -0.3f), new Vector3(3f, 0.6f, 0.08f), sign, collider: false);
        PrimitiveKit.Label("ป้ายรถสองแถว", t, new Vector3(0f, 2.95f, -0.36f), 0.06f, new Color(0.2f, 0.1f, 0.05f));

        // Trigger under the roof opens the travel map
        var zone = new GameObject("TravelZone");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0.1f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 2f, 1.4f);
        zone.AddComponent<TravelStop>();

        // Arrival point just outside the trigger, facing away from the shelter
        var spawn = PrimitiveKit.Group("SpawnPoint", t, new Vector3(0f, 0f, -2.6f), 180f);
        spawn.AddComponent<SpawnPoint>().Configure("busstop");
        return stop;
    }

    // ── Boxing camp (ค่ายมวย) — career training tier 3 ─────────────────────────────
    public static GameObject BoxingCamp(Transform parent, Vector3 position, string name)
    {
        var camp = PrimitiveKit.Group("BoxingCamp_ค่ายมวย", parent, position);
        var t = camp.transform;
        var baseMat = PrimitiveKit.Mat("RingBase", new Color(0.2f, 0.2f, 0.22f));
        var canvas = PrimitiveKit.Mat("RingCanvas", new Color(0.35f, 0.6f, 0.9f));
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var blue = PrimitiveKit.Mat("CornerBlue", new Color(0.15f, 0.3f, 0.85f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var banner = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var roofMat = PrimitiveKit.Mat("CampRoof", new Color(0.55f, 0.58f, 0.6f));

        // Ring (เวทีมวย): 6x6 m, top at 0.6 m
        PrimitiveKit.Box("RingBase", t, new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 6f), baseMat);
        PrimitiveKit.Box("RingCanvas", t, new Vector3(0f, 0.55f, 0f), new Vector3(5.8f, 0.1f, 5.8f), canvas);
        PrimitiveKit.Cylinder("CornerRed", t, new Vector3(-2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), red);
        PrimitiveKit.Cylinder("CornerBlue", t, new Vector3(2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), blue);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(-2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);

        // Ropes on three sides; the front (-Z) is left open as the entrance
        Material[] ropeColors = { red, white, blue };
        float[] ropeHeights = { 1.0f, 1.4f, 1.8f };
        for (int i = 0; i < 3; i++)
        {
            PrimitiveKit.Box("Rope", t, new Vector3(0f, ropeHeights[i], 2.8f), new Vector3(5.6f, 0.05f, 0.05f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(-2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
        }
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.1f, -3.6f), new Vector3(2f, 0.2f, 0.4f), baseMat);
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.2f, -3.2f), new Vector3(2f, 0.4f, 0.4f), baseMat);

        // Open-sided gym roof over the ring and bags
        foreach (float x in new[] { -4.5f, 7.5f })
        foreach (float z in new[] { -4f, 4f })
            PrimitiveKit.Cylinder("RoofPost", t, new Vector3(x, 2.5f, z), new Vector3(0.2f, 2.5f, 0.2f), roofMat);
        PrimitiveKit.Box("GymRoof", t, new Vector3(1.5f, 5.05f, 0f), new Vector3(13f, 0.12f, 9f), roofMat, collider: false);

        // Entrance arch with banner
        PrimitiveKit.Box("ArchPostL", t, new Vector3(-3.6f, 1.9f, -6.5f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("ArchPostR", t, new Vector3(3.6f, 1.9f, -6.5f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("Banner", t, new Vector3(0f, 3.6f, -6.5f), new Vector3(7.5f, 0.9f, 0.15f), banner, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 3.62f, -6.6f), 0.09f, new Color(0.75f, 0.1f, 0.1f));
        PrimitiveKit.Label("ซ้อมกระสอบหนัก Lv 6+", t, new Vector3(0f, 2.9f, -6.6f), 0.05f, Color.white);

        // Career tier 3: heavy bags beside the ring
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.8f, 0f, -1.8f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.8f, 0f, 1.5f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));

        PrimitiveKit.Box("Bench", t, new Vector3(-4.1f, 0.225f, 0f), new Vector3(0.6f, 0.45f, 2.4f), wood);

        // Front desk (โต๊ะรับสมัคร): walk up to it to buy the camp membership
        PrimitiveKit.Box("FrontDesk", t, new Vector3(-6.2f, 0.5f, -5f), new Vector3(2.2f, 1f, 0.7f), wood);
        var desk = new GameObject("FrontDeskZone");
        desk.transform.SetParent(t, false);
        desk.transform.localPosition = new Vector3(-6.2f, 1f, -6.4f);
        var zone = desk.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2.6f, 2f, 1.8f);
        desk.AddComponent<ShopCounter>().Configure("camp");
        Sign(t, new Vector3(-8.2f, 0f, -5.6f), "สมัครสมาชิกค่าย\n300 บาท", 0f);
        return camp;
    }

    // ── Small pieces ────────────────────────────────────────────────────────────
    public static GameObject BananaTree(Transform parent, Vector3 position, bool trainable)
    {
        var tree = PrimitiveKit.Group(trainable ? "Training_BananaTree" : "BananaTree", parent, position);
        if (trainable)
            tree.AddComponent<TrainingTarget>().Configure("banana", 1, 2);

        var trunkMat = PrimitiveKit.Mat("BananaTrunk", new Color(0.5f, 0.58f, 0.25f));
        var leafMat = PrimitiveKit.Mat("BananaLeaf", new Color(0.3f, 0.7f, 0.2f));
        var fruitMat = PrimitiveKit.Mat("BananaFruit", new Color(0.75f, 0.8f, 0.2f));

        // Pivot at the base so the trunk bends when kicked; trunk must be the first child (PunchingBag.IsRooted)
        var pivot = PrimitiveKit.Group("Pivot", tree.transform, Vector3.zero);
        if (trainable) pivot.AddComponent<PunchingBag>();

        PrimitiveKit.Cylinder("Trunk", pivot.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.35f, 1.2f, 0.35f), trunkMat);
        for (int i = 0; i < 6; i++)
        {
            var holder = PrimitiveKit.Group("LeafHolder", pivot.transform, new Vector3(0f, 2.35f, 0f), i * 60f + 15f);
            PrimitiveKit.Box("Leaf", holder.transform, new Vector3(0f, 0.15f, 0.8f), new Vector3(0.4f, 0.04f, 1.8f), leafMat, new Vector3(20f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Sphere("Bunch", pivot.transform, new Vector3(0.25f, 1.95f, 0.1f), new Vector3(0.3f, 0.5f, 0.3f), fruitMat);
        return tree;
    }

    public static GameObject ShadeTree(Transform parent, Vector3 position, float size = 1f) =>
        BigTree(parent, position, "ShadeTree", size);

    private static GameObject BigTree(Transform parent, Vector3 position, string name, float size)
    {
        var tree = PrimitiveKit.Group(name, parent, position);
        PrimitiveKit.Cylinder("Trunk", tree.transform, new Vector3(0f, 1.4f * size, 0f), new Vector3(0.45f, 1.4f, 0.45f) * size, PrimitiveKit.Mat("Bark", new Color(0.4f, 0.26f, 0.13f)));
        PrimitiveKit.Sphere("Canopy", tree.transform, new Vector3(0f, 3.6f * size, 0f), new Vector3(3.2f, 2.6f, 3.2f) * size, PrimitiveKit.Mat("Leaves", new Color(0.18f, 0.5f, 0.2f)));
        return tree;
    }

    public static GameObject Sign(Transform parent, Vector3 position, string text, float yaw)
    {
        var sign = PrimitiveKit.Group("Sign", parent, position, yaw);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var board = PrimitiveKit.Mat("SignBoard", new Color(0.95f, 0.92f, 0.8f));
        PrimitiveKit.Box("Post", sign.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), wood);
        PrimitiveKit.Box("Board", sign.transform, new Vector3(0f, 1.35f, 0f), new Vector3(1.6f, 0.7f, 0.06f), board, collider: false);
        PrimitiveKit.Label(text, sign.transform, new Vector3(0f, 1.35f, -0.04f), 0.035f, new Color(0.25f, 0.15f, 0.05f));
        return sign;
    }

    // Two pillars and a beam with the place name, open in the middle
    private static void Gate(Transform parent, Vector3 position, string name, Material pillar, Material beam, Color textColor)
    {
        var gate = PrimitiveKit.Group("Gate", parent, position);
        PrimitiveKit.Box("PillarL", gate.transform, new Vector3(-2.2f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), pillar);
        PrimitiveKit.Box("PillarR", gate.transform, new Vector3(2.2f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), pillar);
        PrimitiveKit.Box("Beam", gate.transform, new Vector3(0f, 3.5f, 0f), new Vector3(5.4f, 0.8f, 0.3f), beam, collider: false);
        PrimitiveKit.Label(name, gate.transform, new Vector3(0f, 3.5f, -0.17f), 0.065f, textColor);
    }
}
