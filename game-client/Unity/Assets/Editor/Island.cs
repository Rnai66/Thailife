using UnityEngine;

// The island (เกาะ · อ่าวมาหยา): a white-sand bay closed in by sheer limestone cliffs draped in jungle,
// sea stacks out in the turquoise water, like Maya Bay on Phi Phi — dressed with the Caribbean
// paintings: a tyre swing on a leaning palm, thatched cabanas, a patchwork beach shack, an open bar
// pavilion, pastel cottages, a brigantine at anchor, dinghies, heliconia, bananas and a lily pond.
// Three treasure chests (island.lua) hide around the bay. The ferry pier takes you back to the mainland.
// The sea starts at z = 22; the bay mouth opens north between the cliffs.
public static class Island
{
    private const float Shore = 22f;

    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);
    private static readonly Color White = new Color(0.97f, 0.96f, 0.92f);

    public static void Build(Transform root)
    {
        Ground(root);
        Cliffs(root);
        Pier(root, new Vector3(20f, 0f, 16f));

        // Cabanas with paid deck chairs along the sand
        for (int i = 0; i < 4; i++)
            Cabana(root, new Vector3(-28f + i * 11f, 0f, 12.5f), i);
        TireSwingPalm(root, new Vector3(-35f, 0f, 18.5f));
        BombaShack(root, new Vector3(31f, 0f, 1f), -12f);
        BarPavilion(root, new Vector3(-6f, 0f, 0f));
        Hammock(root, new Vector3(9f, 0f, -6f));

        Cottage(root, "YellowHouse_บ้านสีเหลือง", new Vector3(-27f, 0f, -7f), 8f, new Color(1f, 0.84f, 0.32f), new Color(0.78f, 0.22f, 0.18f), White);
        Cottage(root, "SunsetHouse_บ้านพระอาทิตย์ตก", new Vector3(-17f, 0f, -12f), -4f, new Color(0.98f, 0.62f, 0.66f), new Color(0.3f, 0.62f, 0.56f), White);
        Cottage(root, "CarrotBayHouse_บ้านฟ้าคราม", new Vector3(18f, 0f, -11f), 6f, new Color(0.38f, 0.8f, 0.8f), new Color(0.95f, 0.55f, 0.2f), new Color(1f, 0.9f, 0.4f));
        Cottage(root, "StThomasHouse_บ้านขาวฟ้า", new Vector3(30f, 0f, -16f), -10f, White, new Color(0.2f, 0.45f, 0.75f), new Color(0.2f, 0.45f, 0.75f));

        Brigantine(root, new Vector3(-6f, 0f, 50f), 72f);
        Dinghy(root, new Vector3(6f, 0f, 24.6f), 35f, new Color(0.2f, 0.5f, 0.85f), afloat: true);
        Dinghy(root, new Vector3(-17f, 0f, 19.5f), -60f, new Color(0.95f, 0.45f, 0.3f), afloat: false);
        Seaside.LongtailBoat(root, new Vector3(-24f, 0f, 30f), 15f, new Color(0.3f, 0.85f, 0.6f));

        LilyPond(root, new Vector3(-3f, 0f, -24f));
        Treasure(root);
        Plants(root);
        Torches(root);

        Places.Sign(root, new Vector3(4f, 0f, 17f), "อ่าวมาหยา\nเกาะสวรรค์กลางทะเล", 0f);
        Places.Sign(root, new Vector3(-30f, 0f, 16.5f), "ชิงช้าริมทะเล\nนั่งแกว่งรับลม", 30f);
        Npcs.Crowd(root, new Vector3(-4f, 0f, 15f), new Vector2(56f, 9f), 9, "", 7, 18,
            "อ่าวนี้สวยที่สุดเลย|น้ำใสเห็นปลาเลย|ได้ยินว่ามีหีบสมบัติซ่อนอยู่|ไปนั่งชิงช้ากัน!|หน้าผาสูงมาก|ถ่ายรูปกับเรือใบหน่อย");
    }

    // ── Sand, lagoon water and the walls you can't walk past ──────────────────────
    private static void Ground(Transform root)
    {
        var sand = PrimitiveKit.Mat("IslandSandWarm", new Color(0.96f, 0.89f, 0.72f));
        var wetSand = PrimitiveKit.Mat("WetSand", new Color(0.88f, 0.8f, 0.62f));
        var jungle = PrimitiveKit.Mat("JungleFloor", new Color(0.24f, 0.44f, 0.2f));
        var clear = PrimitiveKit.Mat("LagoonClear", new Color(0.55f, 0.95f, 0.88f), 0.85f);
        var lagoon = PrimitiveKit.Mat("Lagoon", new Color(0.12f, 0.78f, 0.8f), 0.85f);
        var deep = PrimitiveKit.Mat("SeaDeepBlue", new Color(0.05f, 0.42f, 0.7f), 0.85f);

        PrimitiveKit.Box("JungleFloor", root, new Vector3(0f, 0.004f, -60f), new Vector3(300f, 0.01f, 120f), jungle, collider: false);
        PrimitiveKit.Box("Sand", root, new Vector3(0f, 0.015f, 4f), new Vector3(92f, 0.03f, 36f), sand, collider: false);
        // Sand bulges round into the cliffs at both ends so the beach curves like a bay
        foreach (float x in new[] { -42f, 42f })
            MeshKit.Place("SandCurve", root, MeshKit.Lathe("IslandSandDisc", new[] { new Vector2(1f, 0f), new Vector2(0.98f, 0.5f), new Vector2(0f, 0.6f) }, 24), sand,
                new Vector3(x, 0f, 10f), default, new Vector3(16f, 0.05f, 14f));
        PrimitiveKit.Box("WetSand", root, new Vector3(0f, 0.022f, Shore - 1.2f), new Vector3(92f, 0.02f, 2.4f), wetSand, collider: false);
        PrimitiveKit.Box("ClearShallows", root, new Vector3(0f, 0.03f, Shore + 3f), new Vector3(110f, 0.02f, 6f), clear, collider: false);
        PrimitiveKit.Box("Lagoon", root, new Vector3(0f, 0.027f, Shore + 26f), new Vector3(130f, 0.02f, 40f), lagoon, collider: false);
        PrimitiveKit.Box("OpenSea", root, new Vector3(0f, 0.024f, 190f), new Vector3(600f, 0.02f, 300f), deep, collider: false);
        foreach (float x in new[] { -190f, 190f })
            PrimitiveKit.Box("OpenSeaSide", root, new Vector3(x, 0.024f, -10f), new Vector3(220f, 0.02f, 100f), deep, collider: false);

        // Can't walk into the sea, except out along the pier (x 18.4..21.6)
        Blocker(root, new Vector3(-15.8f, 1f, Shore + 1.5f), new Vector3(68.4f, 2f, 3f));
        Blocker(root, new Vector3(32.8f, 1f, Shore + 1.5f), new Vector3(22.4f, 2f, 3f));
        Blocker(root, new Vector3(-42f, 1.5f, -8f), new Vector3(1f, 3f, 66f));   // cliffs left and right
        Blocker(root, new Vector3(42f, 1.5f, -8f), new Vector3(1f, 3f, 66f));
        Blocker(root, new Vector3(0f, 1.5f, -40f), new Vector3(84f, 3f, 1f));    // jungle behind
    }

    private static void Blocker(Transform root, Vector3 pos, Vector3 size)
    {
        var b = new GameObject("SeaBlocker");
        b.transform.SetParent(root, false);
        b.transform.localPosition = pos;
        b.AddComponent<BoxCollider>().size = size;
    }

    // ── Limestone karst: sheer pale cliffs, a dark wet notch at the waterline, jungle on top ──
    private static void Cliffs(Transform root)
    {
        // West wall, running out to the bay mouth
        Karst(root, "West1", new Vector3(-62f, 0f, -30f), new Vector3(42f, 72f, 40f), 1);
        Karst(root, "West2", new Vector3(-64f, 0f, 6f), new Vector3(38f, 88f, 40f), 2);
        Karst(root, "West3", new Vector3(-58f, 0f, 38f), new Vector3(34f, 78f, 36f), 3);
        Karst(root, "WestMouth", new Vector3(-42f, 0f, 66f), new Vector3(30f, 62f, 28f), 4);
        // East wall
        Karst(root, "East1", new Vector3(62f, 0f, -28f), new Vector3(44f, 82f, 40f), 5);
        Karst(root, "East2", new Vector3(65f, 0f, 10f), new Vector3(38f, 70f, 40f), 6);
        Karst(root, "East3", new Vector3(58f, 0f, 42f), new Vector3(32f, 92f, 34f), 7);
        Karst(root, "EastMouth", new Vector3(40f, 0f, 70f), new Vector3(26f, 56f, 26f), 8);
        // Behind the jungle
        Karst(root, "Back1", new Vector3(-26f, 0f, -64f), new Vector3(52f, 60f, 36f), 9);
        Karst(root, "Back2", new Vector3(14f, 0f, -66f), new Vector3(58f, 74f, 36f), 10);
        Karst(root, "Back3", new Vector3(46f, 0f, -58f), new Vector3(30f, 52f, 30f), 11);
        // Sea stacks out beyond the bay mouth
        Karst(root, "Stack1", new Vector3(-14f, 0f, 98f), new Vector3(10f, 34f, 10f), 12);
        Karst(root, "Stack2", new Vector3(9f, 0f, 114f), new Vector3(15f, 50f, 14f), 13);
        Karst(root, "Stack3", new Vector3(27f, 0f, 92f), new Vector3(7f, 20f, 7f), 14);
        Karst(root, "Stack4", new Vector3(-38f, 0f, 124f), new Vector3(20f, 42f, 18f), 15);
        Karst(root, "Stack5", new Vector3(62f, 0f, 126f), new Vector3(26f, 58f, 22f), 16);
        Karst(root, "Stack6", new Vector3(-80f, 0f, 96f), new Vector3(30f, 50f, 26f), 17);
        Karst(root, "Stack7", new Vector3(90f, 0f, 88f), new Vector3(34f, 66f, 30f), 18);

        // Boulders at the foot of the cliffs where they meet the sand
        foreach (var p in new[]
        {
            new Vector3(-40f, 0f, 20f), new Vector3(-41f, 0f, 4f), new Vector3(-39f, 0f, -14f), new Vector3(40f, 0f, 21f),
            new Vector3(41f, 0f, 8f), new Vector3(39.5f, 0f, -6f), new Vector3(-36f, 0f, 24f), new Vector3(37f, 0f, 25f),
        })
            Nature.Rock(root, p, 2.6f + Mathf.Abs(p.z) % 3f * 0.5f, (int)(p.x + p.z));
    }

    private static void Karst(Transform root, string name, Vector3 pos, Vector3 size, int seed)
    {
        var k = PrimitiveKit.Group("Karst_" + name, root, pos, seed * 47f % 360f).transform;
        var rock = PrimitiveKit.Mat(seed % 2 == 0 ? "Limestone" : "LimestoneWarm",
            seed % 2 == 0 ? new Color(0.8f, 0.77f, 0.7f) : new Color(0.84f, 0.74f, 0.6f), 0.1f);
        var streak = PrimitiveKit.Mat("LimestoneStreak", new Color(0.55f, 0.5f, 0.44f), 0.1f);
        var wet = PrimitiveKit.Mat("LimestoneWet", new Color(0.3f, 0.28f, 0.25f), 0.4f);
        var jungle = PrimitiveKit.Mat("CliffJungle", new Color(0.17f, 0.42f, 0.16f), 0.15f);
        var jungleLight = PrimitiveKit.Mat("CliffJungleLight", new Color(0.3f, 0.55f, 0.2f), 0.15f);

        Part(k, "Rock", seed, rock, new Vector3(0f, size.y * 0.42f, 0f), size);
        // A shoulder off one side so the outline isn't a single egg, and a grey streaked face
        Part(k, "Shoulder", seed + 1, rock, new Vector3(size.x * 0.26f, size.y * 0.28f, -size.z * 0.12f), new Vector3(size.x * 0.62f, size.y * 0.66f, size.z * 0.72f));
        Part(k, "Streak", seed + 2, streak, new Vector3(-size.x * 0.12f, size.y * 0.36f, size.z * 0.1f), new Vector3(size.x * 0.82f, size.y * 0.74f, size.z * 0.84f));
        // Dark notch the sea has worn into the foot
        Part(k, "Notch", seed + 3, wet, new Vector3(0f, size.y * 0.02f, 0f), new Vector3(size.x * 1.04f, size.y * 0.09f, size.z * 1.04f));

        // Jungle crown on top, and vines draping down the faces
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.7f + seed;
            var p = new Vector3(Mathf.Cos(a) * size.x * 0.1f, size.y * (0.86f - i * 0.015f), Mathf.Sin(a) * size.z * 0.1f);
            Part(k, "Crown", seed + 10 + i, i % 2 == 0 ? jungle : jungleLight, p, new Vector3(size.x * 0.55f, size.y * 0.2f, size.z * 0.55f));
        }
        int vines = size.y > 40f ? 7 : 4;
        for (int i = 0; i < vines; i++)
        {
            float a = i * Mathf.PI * 2f / vines + seed * 0.7f;
            float h = 0.56f + (i % 3) * 0.07f;
            var p = new Vector3(Mathf.Cos(a) * size.x * 0.41f, size.y * h, Mathf.Sin(a) * size.z * 0.41f);
            Part(k, "Vines", seed + 20 + i, i % 2 == 0 ? jungleLight : jungle, p, new Vector3(size.x * 0.24f, size.y * 0.36f, size.z * 0.24f));
        }
    }

    private static void Part(Transform k, string name, int seed, Material mat, Vector3 pos, Vector3 size)
    {
        var go = MeshKit.Place(name, k, MeshKit.Blob("Karst" + seed % 6, 500 + seed % 6, 0.26f, 2), mat, pos, new Vector3(0f, seed * 53f % 360f, 0f), size);
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ── Ferry pier (ท่าเรือเกาะ): arrive here, fish off the end, take the ferry back ──────
    private static void Pier(Transform root, Vector3 start)
    {
        var p = PrimitiveKit.Group("FishingPier_ท่าเรือเกาะ", root, start).transform;
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var plank = PrimitiveKit.Mat("WoodPlank", Wood);
        PrimitiveKit.Box("Deck", p, new Vector3(0f, 0.15f, 10f), new Vector3(3.2f, 0.3f, 20f), plank);
        for (int i = 0; i <= 6; i++)
            foreach (float x in new[] { -1.5f, 1.5f })
                PrimitiveKit.Cylinder("Post", p, new Vector3(x, 0.2f, i * 3.2f + 0.5f), new Vector3(0.22f, 0.6f, 0.22f), dark, collider: false);
        foreach (float x in new[] { -1.55f, 1.55f })
        {
            PrimitiveKit.Box("Rail", p, new Vector3(x, 0.95f, 12f), new Vector3(0.08f, 0.08f, 16f), dark, collider: false);
            Blocker(p, new Vector3(x + Mathf.Sign(x) * 0.3f, 1f, 13f), new Vector3(0.4f, 2f, 18f)); // keep off the water
        }
        Blocker(p, new Vector3(0f, 1f, 20.3f), new Vector3(3.6f, 2f, 0.4f));

        // Arrival point at the landward end, facing the beach
        var spawn = PrimitiveKit.Group("SpawnPoint", p, new Vector3(0f, 0.3f, 7f), 180f);
        spawn.AddComponent<SpawnPoint>().Configure("pier");

        // Ferry stop half way along: the travel map, titled for the boat
        FerryStop(p, new Vector3(0f, 0.3f, 12.5f), 0f, "ท่าเรือ · กลับฝั่ง", "ท่าเรือข้ามฝั่ง");
        Seaside.LongtailBoat(root, start + new Vector3(3.6f, 0f, 13f), 0f, new Color(1f, 0.85f, 0.2f)); // the ferry, moored alongside

        foreach (float x in new[] { -0.8f, 0.8f })
        {
            var spot = PrimitiveKit.Group("FishingSpot_ตกปลาทะเล", p, new Vector3(x, 0.3f, 18.8f));
            var bobber = PrimitiveKit.Group("BobberPoint", spot.transform, new Vector3(x * 2f, -0.25f, 5f));
            var fz = new GameObject("FishingZone");
            fz.transform.SetParent(spot.transform, false);
            fz.transform.localPosition = new Vector3(0f, 1f, 0f);
            var fbox = fz.AddComponent<BoxCollider>();
            fbox.isTrigger = true;
            fbox.size = new Vector3(1.5f, 2f, 2f);
            fz.AddComponent<FishingSpot>().Configure(bobber.transform, "sea");
        }
        StreetFurniture.LampPost(p, new Vector3(1.2f, 0.3f, 19.5f));
        Places.Sign(root, start + new Vector3(-2.6f, 0f, 0.5f), "ท่าเรือเกาะ\nเรือกลับฝั่ง · ตกปลาทะเล", 0f);
    }

    // Ferry shelter (ท่าเรือข้ามฝั่ง): standing under it opens the travel map, titled for the boat
    internal static void FerryStop(Transform parent, Vector3 pos, float yaw, string sign, string title)
    {
        var ferry = PrimitiveKit.Group("FerryStop_เรือข้ามฝั่ง", parent, pos, yaw);
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var roof = PrimitiveKit.Mat("FerryRoof", new Color(0.15f, 0.55f, 0.75f));
        foreach (float x in new[] { -1.4f, 1.4f })
            PrimitiveKit.Cylinder("Post", ferry.transform, new Vector3(x, 1.25f, 0f), new Vector3(0.1f, 1.25f, 0.1f), dark, collider: false);
        PrimitiveKit.Box("Roof", ferry.transform, new Vector3(0f, 2.55f, 0f), new Vector3(3.4f, 0.1f, 2f), roof, collider: false);
        PrimitiveKit.Box("SignBoard", ferry.transform, new Vector3(0f, 2.95f, -0.9f), new Vector3(3f, 0.6f, 0.08f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label(sign, ferry.transform, new Vector3(0f, 2.95f, -0.96f), 0.06f, new Color(0.2f, 0.1f, 0.05f));
        StreetFurniture.HangingLight(ferry.transform, new Vector3(0f, 2.4f, 0f));
        var zone = new GameObject("TravelZone");
        zone.transform.SetParent(ferry.transform, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 0f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.8f, 2f, 1.6f);
        zone.AddComponent<TravelStop>().Configure(title);
    }

    // ── Thatched cabana (ซุ้มหลังคาจาก) with two deck chairs; lie down to rest (paid) ──
    private static void Cabana(Transform root, Vector3 pos, int index)
    {
        var c = PrimitiveKit.Group("Cabana_ซุ้มหลังคาจาก", root, pos).transform;
        var pole = PrimitiveKit.Mat("BambooPole", new Color(0.72f, 0.62f, 0.38f));
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.78f, 0.66f, 0.38f));
        var thatchDark = PrimitiveKit.Mat("ThatchDark", new Color(0.62f, 0.5f, 0.28f));
        PrimitiveKit.Cylinder("Pole", c, new Vector3(0f, 1.3f, 0f), new Vector3(0.16f, 1.3f, 0.16f), pole);
        // Two layers of shaggy thatch with a fringe hanging off the rim
        MeshKit.Place("Thatch", c, MeshKit.Lathe("CabanaThatch", new[] { new Vector2(1.9f, 0f), new Vector2(1.5f, 0.35f), new Vector2(0.2f, 1.05f), new Vector2(0f, 1.1f) }, 12),
            thatch, new Vector3(0f, 2.35f, 0f));
        MeshKit.Place("ThatchTop", c, MeshKit.Lathe("CabanaThatchTop", new[] { new Vector2(0.9f, 0f), new Vector2(0.5f, 0.35f), new Vector2(0f, 0.55f) }, 10),
            thatchDark, new Vector3(0f, 3.05f, 0f), new Vector3(0f, 15f, 0f));
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            var p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 2.22f, 1.82f);
            PrimitiveKit.Box("Fringe", c, p, new Vector3(0.38f, 0.32f, 0.04f), i % 2 == 0 ? thatch : thatchDark, new Vector3(-12f, a, 0f), collider: false);
        }

        var chair = PrimitiveKit.Mat("TeakChair", new Color(0.55f, 0.36f, 0.2f));
        Color[] cushion = { new Color(0.15f, 0.6f, 0.75f), new Color(0.95f, 0.45f, 0.35f), new Color(1f, 0.82f, 0.3f), new Color(0.4f, 0.75f, 0.45f) };
        var cushionMat = PrimitiveKit.Mat($"Cushion{index % 4}", cushion[index % 4]);
        foreach (float x in new[] { -0.8f, 0.8f })
        {
            PrimitiveKit.Box("Lounger", c, new Vector3(x, 0.26f, 0.3f), new Vector3(0.66f, 0.1f, 1.9f), chair);
            PrimitiveKit.Box("Back", c, new Vector3(x, 0.55f, -0.62f), new Vector3(0.66f, 0.1f, 0.7f), chair, new Vector3(-45f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Cushion", c, new Vector3(x, 0.34f, 0.4f), new Vector3(0.58f, 0.07f, 1.5f), cushionMat, collider: false);
        }
        // Coconut drink on a little table between the chairs
        PrimitiveKit.Cylinder("SideTable", c, new Vector3(0f, 0.25f, 0.9f), new Vector3(0.36f, 0.25f, 0.36f), chair, collider: false);
        MeshKit.Place("CoconutDrink", c, MeshKit.Blob("Coconut", 21, 0.15f, 1), PrimitiveKit.Mat("Coconut", new Color(0.38f, 0.45f, 0.15f)), new Vector3(0f, 0.62f, 0.9f), default, Vector3.one * 0.26f);

        var bed = PrimitiveKit.Group("RestPoint", c, new Vector3(0.8f, 0.55f, 0.3f), 90f);
        var zone = new GameObject("RestZone");
        zone.transform.SetParent(c, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 1.9f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 2f, 1.4f);
        var rest = zone.AddComponent<SleepSpot>();
        rest.Configure("ซุ้มหลังคาจากริมอ่าว", "เช่าเก้าอี้นอนพัก", bed.transform);
        rest.ConfigurePaid("beach_chair", 40); // same price as life.lua PAID_SPOTS
    }

    // ── Leaning palm with a tyre swing hanging out over the shallows (Cane Garden Bay) ──
    private static void TireSwingPalm(Transform root, Vector3 pos)
    {
        var palm = PrimitiveKit.Group("TireSwingPalm_ชิงช้ายาง", root, pos).transform;
        var bark = PrimitiveKit.Mat("PalmBark", new Color(0.5f, 0.4f, 0.3f), 0.15f);
        var frond = PrimitiveKit.Mat("PalmFrond", new Color(0.26f, 0.55f, 0.18f), 0.3f);
        var nut = PrimitiveKit.Mat("Coconut", new Color(0.38f, 0.45f, 0.15f), 0.4f);

        // Trunk leans far out toward the sea (+Z)
        var path = MeshKit.Curve(Vector3.zero, new Vector3(0f, 3.6f, 0.8f), new Vector3(0f, 6.4f, 5.6f), 12);
        MeshKit.Place("Trunk", palm, MeshKit.Sweep("PalmTrunkLeaning", path, MeshKit.Taper(12, 0.3f, 0.17f), 10), bark, Vector3.zero);
        var collider = palm.gameObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.2f, 0.1f);
        collider.height = 2.4f;
        collider.radius = 0.3f;
        Vector3 top = path[path.Length - 1];
        var leaf = MeshKit.Leaf("PalmFrond", 3.4f, 0.55f, 0.6f, 2.3f, 12, 0.6f);
        for (int i = 0; i < 11; i++)
            MeshKit.Place("Frond", palm, leaf, frond, top, new Vector3(-20f - (i % 3) * 10f, i * 33f + (i % 2) * 12f, 0f));
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Sphere("Coconut", palm, top + Quaternion.Euler(0f, i * 90f + 20f, 0f) * new Vector3(0.18f, -0.25f, 0f), Vector3.one * 0.26f, nut);

        // The rope hangs from the trunk part-way up; the pivot rocks, the tyre and seat ride on it
        Vector3 hang = path[8];
        var pivot = PrimitiveKit.Group("SwingPivot", palm, hang).transform;
        var rope = PrimitiveKit.Mat("Rope", new Color(0.78f, 0.68f, 0.48f));
        float ropeLength = hang.y - 1.05f;
        PrimitiveKit.Cylinder("Rope", pivot, new Vector3(0f, -ropeLength * 0.5f, 0f), new Vector3(0.04f, ropeLength * 0.5f, 0.04f), rope, collider: false);
        PrimitiveKit.Cylinder("Knot", pivot, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.06f, 0.36f), rope, collider: false);
        var tyre = MeshKit.Lathe("Tyre", Ring(0.42f, 0.14f, 12), 18);
        MeshKit.Place("Tyre", pivot, tyre, PrimitiveKit.Mat("TyreRubber", new Color(0.1f, 0.1f, 0.11f), 0.35f), new Vector3(0f, -ropeLength - 0.05f, 0f));
        // Seat point is where the feet go: a seated body's hips are ~0.85 m above it, so they rest on the tyre
        var seat = PrimitiveKit.Group("SwingSeat", pivot, new Vector3(0f, -ropeLength - 0.76f, 0f)).transform; // facing out to sea

        var zone = new GameObject("SwingZone");
        zone.transform.SetParent(palm, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 2.2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.4f, 2f, 2.4f);
        zone.AddComponent<TireSwing>().Configure(pivot, seat);
    }

    // Cross-section of a tyre for Lathe: a circle of radius r centred at distance R from the axis
    private static Vector2[] Ring(float R, float r, int steps)
    {
        var p = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.PI * 2f * i / steps;
            p[i] = new Vector2(R + Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return p;
    }

    // ── Patchwork beach shack (Bomba Shack): planks in every colour, flags, surfboards ──
    private static void BombaShack(Transform root, Vector3 pos, float yaw)
    {
        var s = PrimitiveKit.Group("BombaShack_บาร์กระท่อมหลากสี", root, pos, yaw).transform;
        Color[] paint =
        {
            new Color(0.95f, 0.3f, 0.25f), new Color(1f, 0.82f, 0.2f), new Color(0.2f, 0.65f, 0.9f), new Color(0.3f, 0.78f, 0.4f),
            new Color(0.95f, 0.5f, 0.75f), new Color(1f, 0.55f, 0.15f), new Color(0.55f, 0.4f, 0.85f), new Color(0.95f, 0.95f, 0.9f),
        };
        var mats = new Material[paint.Length];
        for (int i = 0; i < paint.Length; i++) mats[i] = PrimitiveKit.Mat($"ShackPaint{i}", paint[i]);
        var post = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var tin = PrimitiveKit.Mat("RustyTin", new Color(0.62f, 0.4f, 0.28f), 0.3f);

        // Back and side walls of mismatched vertical boards, open front with a counter
        for (int i = 0; i < 14; i++)
        {
            float x = -3.25f + i * 0.5f;
            float h = 2.6f + (i * 7 % 5) * 0.08f;
            PrimitiveKit.Box("Board", s, new Vector3(x, h * 0.5f, -1.9f), new Vector3(0.48f, h, 0.08f), mats[(i * 3) % mats.Length], new Vector3(0f, 0f, (i % 4 - 1.5f) * 1.5f), collider: i % 4 == 0);
        }
        foreach (float side in new[] { -1f, 1f })
            for (int i = 0; i < 7; i++)
            {
                float z = -1.65f + i * 0.5f;
                float h = 2.5f - i * 0.05f;
                PrimitiveKit.Box("Board", s, new Vector3(side * 3.45f, h * 0.5f, z), new Vector3(0.08f, h, 0.48f), mats[(i * 5 + (side > 0 ? 2 : 0)) % mats.Length], collider: i % 3 == 0);
            }
        foreach (float x in new[] { -3.4f, 3.4f })
            PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.4f, 1.6f), new Vector3(0.12f, 1.4f, 0.12f), post);
        // Patched tin roof, sloping to the front
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("RoofSheet", s, new Vector3(-2.9f + i * 1.45f, 2.95f + (i % 2) * 0.03f, -0.1f), new Vector3(1.55f, 0.05f, 4.2f), i == 2 ? mats[2] : tin, new Vector3(-10f, 0f, (i % 2 - 0.5f) * 2f), collider: false);

        PrimitiveKit.Box("Counter", s, new Vector3(0f, 0.55f, 1.2f), new Vector3(5.4f, 1.1f, 0.5f), mats[1]);
        PrimitiveKit.Box("CounterTop", s, new Vector3(0f, 1.13f, 1.2f), new Vector3(5.6f, 0.06f, 0.7f), post, collider: false);
        // Bottles on the back shelf
        PrimitiveKit.Box("Shelf", s, new Vector3(0f, 1.6f, -1.75f), new Vector3(4f, 0.06f, 0.3f), post, collider: false);
        for (int i = 0; i < 10; i++)
            PrimitiveKit.Cylinder("Bottle", s, new Vector3(-1.8f + i * 0.4f, 1.8f, -1.75f), new Vector3(0.1f, 0.17f, 0.1f),
                PrimitiveKit.Glow($"BottleGlass{i % 3}", i % 3 == 0 ? new Color(0.3f, 0.75f, 0.35f) : i % 3 == 1 ? new Color(0.9f, 0.55f, 0.2f) : new Color(0.4f, 0.6f, 0.95f), 0.4f), collider: false);

        // Sign board and hand-painted bunting
        PrimitiveKit.Box("SignBoard", s, new Vector3(0f, 3.35f, 1.9f), new Vector3(4.2f, 0.7f, 0.08f), mats[7], new Vector3(0f, 0f, -2f), collider: false);
        PrimitiveKit.Label("บาร์กระท่อม", s, new Vector3(0f, 3.35f, 1.96f), 0.06f, new Color(0.75f, 0.15f, 0.1f), 180f);
        for (int i = 0; i < 12; i++)
            PrimitiveKit.Box("Flag", s, new Vector3(-3.3f + i * 0.6f, 2.55f - Mathf.Sin(i / 11f * Mathf.PI) * 0.25f, 1.65f), new Vector3(0.3f, 0.32f, 0.02f), mats[i % mats.Length], new Vector3(0f, 0f, 45f), collider: false);
        // Surfboards leaning on the side and a buoy hanging up
        for (int i = 0; i < 3; i++)
            MeshKit.Place("Surfboard", s, MeshKit.Blob("Surfboard", 33, 0.04f, 1), mats[(i * 2 + 2) % mats.Length],
                new Vector3(3.75f, 1.1f, -1.2f + i * 0.75f), new Vector3(0f, 0f, -12f), new Vector3(0.1f, 2.2f, 0.55f));
        MeshKit.Place("Buoy", s, MeshKit.Lathe("Tyre", Ring(0.42f, 0.14f, 12), 18), mats[0], new Vector3(-3.55f, 2f, 0.6f), new Vector3(0f, 0f, 90f), Vector3.one * 0.7f);
        StreetFurniture.HangingLight(s, new Vector3(0f, 2.6f, 0f));
    }

    // ── Open-sided beach bar pavilion (Little Dix): hexagonal thatched roof, a round bar, stools ──
    private static void BarPavilion(Transform root, Vector3 pos)
    {
        var b = PrimitiveKit.Group("BeachBar_บาร์ริมหาด", root, pos).transform;
        var post = PrimitiveKit.Mat("TeakChair", new Color(0.55f, 0.36f, 0.2f));
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.78f, 0.66f, 0.38f));
        var thatchDark = PrimitiveKit.Mat("ThatchDark", new Color(0.62f, 0.5f, 0.28f));
        var deck = PrimitiveKit.Mat("WoodPlank", Wood);
        PrimitiveKit.Cylinder("Deck", b, new Vector3(0f, 0.08f, 0f), new Vector3(9f, 0.08f, 9f), deck, collider: false);
        for (int i = 0; i < 6; i++)
        {
            var p = Quaternion.Euler(0f, i * 60f + 30f, 0f) * new Vector3(0f, 1.6f, 4f);
            PrimitiveKit.Cylinder("Post", b, p, new Vector3(0.2f, 1.6f, 0.2f), post);
        }
        MeshKit.Place("Roof", b, MeshKit.Lathe("PavilionRoof", new[] { new Vector2(5.4f, 0f), new Vector2(4.6f, 0.4f), new Vector2(0.4f, 2.6f), new Vector2(0f, 2.7f) }, 6),
            thatch, new Vector3(0f, 3.15f, 0f), new Vector3(0f, 30f, 0f));
        MeshKit.Place("RoofCap", b, MeshKit.Lathe("PavilionCap", new[] { new Vector2(1.2f, 0f), new Vector2(0.4f, 0.6f), new Vector2(0f, 0.9f) }, 6),
            thatchDark, new Vector3(0f, 5.4f, 0f), new Vector3(0f, 30f, 0f));
        // Round bar in the middle with stools around it
        MeshKit.Place("Bar", b, MeshKit.Lathe("BarRing", new[] { new Vector2(1.9f, 0f), new Vector2(1.9f, 1.1f), new Vector2(1.3f, 1.1f), new Vector2(1.3f, 0f) }, 20, false, false),
            PrimitiveKit.Mat("BarBamboo", new Color(0.82f, 0.7f, 0.42f)), new Vector3(0f, 0.16f, 0f));
        PrimitiveKit.Cylinder("BarCollider", b, new Vector3(0f, 0.7f, 0f), new Vector3(3.8f, 0.55f, 3.8f), deck).GetComponent<MeshRenderer>().enabled = false;
        MeshKit.Place("BarTop", b, MeshKit.Lathe("BarTop", new[] { new Vector2(2.05f, 0f), new Vector2(2.05f, 0.07f), new Vector2(1.15f, 0.07f), new Vector2(1.15f, 0f) }, 20),
            post, new Vector3(0f, 1.26f, 0f));
        var stoolSeat = PrimitiveKit.Mat("Cushion0", new Color(0.15f, 0.6f, 0.75f));
        for (int i = 0; i < 8; i++)
        {
            var p = Quaternion.Euler(0f, i * 45f + 10f, 0f) * new Vector3(0f, 0f, 2.6f);
            PrimitiveKit.Cylinder("StoolLeg", b, p + Vector3.up * 0.4f, new Vector3(0.06f, 0.4f, 0.06f), post, collider: false);
            PrimitiveKit.Cylinder("Stool", b, p + Vector3.up * 0.82f, new Vector3(0.42f, 0.05f, 0.42f), stoolSeat, collider: false);
        }
        // Bottles and a lantern hanging from the roof
        for (int i = 0; i < 8; i++)
        {
            var p = Quaternion.Euler(0f, i * 45f, 0f) * new Vector3(0f, 1.45f, 1.6f);
            PrimitiveKit.Cylinder("Bottle", b, p, new Vector3(0.1f, 0.17f, 0.1f), PrimitiveKit.Glow($"BottleGlass{i % 3}", i % 3 == 0 ? new Color(0.3f, 0.75f, 0.35f) : i % 3 == 1 ? new Color(0.9f, 0.55f, 0.2f) : new Color(0.4f, 0.6f, 0.95f), 0.4f), collider: false);
        }
        StreetFurniture.HangingLight(b, new Vector3(0f, 3.1f, 0f));
        PrimitiveKit.Box("SignBoard", b, new Vector3(0f, 3.4f, 4.75f), new Vector3(3.2f, 0.55f, 0.08f), PrimitiveKit.Mat("SignTeal", new Color(0.12f, 0.6f, 0.6f)), collider: false);
        PrimitiveKit.Label("บาร์ริมหาด", b, new Vector3(0f, 3.4f, 4.81f), 0.055f, Color.white, 180f);
    }

    // Free hammock between two palms: lie down and rest (เปลญวน)
    private static void Hammock(Transform root, Vector3 pos)
    {
        var h = PrimitiveKit.Group("Hammock_เปลญวน", root, pos).transform;
        Nature.CoconutPalm(h, new Vector3(-2.2f, 0f, 0f), 0.95f, 7);
        Nature.CoconutPalm(h, new Vector3(2.2f, 0f, 0f), 1.05f, 8);
        var cloth = PrimitiveKit.Mat("HammockCloth", new Color(0.95f, 0.45f, 0.35f));
        var stripe = PrimitiveKit.Mat("HammockStripe", new Color(1f, 0.85f, 0.35f));
        var sag = MeshKit.Curve(new Vector3(-2f, 1.4f, 0f), new Vector3(0f, 0.2f, 0f), new Vector3(2f, 1.4f, 0f), 10);
        for (int i = 0; i < sag.Length - 1; i++)
        {
            var mid = (sag[i] + sag[i + 1]) * 0.5f;
            var d = sag[i + 1] - sag[i];
            PrimitiveKit.Box("Cloth", h, mid, new Vector3(d.magnitude + 0.02f, 0.03f, 0.9f), i % 2 == 0 ? cloth : stripe,
                new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg), collider: false);
        }
        var bed = PrimitiveKit.Group("RestPoint", h, new Vector3(0f, 0.85f, 0f), 90f);
        var zone = new GameObject("RestZone");
        zone.transform.SetParent(h, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 1.2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.4f, 2f, 1.4f);
        zone.AddComponent<SleepSpot>().Configure("เปลญวนใต้ต้นมะพร้าว", "นอนเปลพักผ่อน", bed.transform);
    }

    // ── Pastel Caribbean cottage: raised porch with white rails, shutters, gingerbread trim ──
    private static void Cottage(Transform root, string name, Vector3 pos, float yaw, Color wall, Color roof, Color trim)
    {
        var h = PrimitiveKit.Group("Cottage_" + name, root, pos, yaw).transform;
        string key = name.Substring(0, name.IndexOf('_'));
        var wallMat = PrimitiveKit.Mat(key + "Wall", wall);
        var roofMat = PrimitiveKit.Mat(key + "Roof", roof, 0.35f);
        var trimMat = PrimitiveKit.Mat(key + "Trim", trim);
        var white = PrimitiveKit.Mat("TrimWhite", White);
        var stone = PrimitiveKit.Mat("CoralStone", new Color(0.86f, 0.82f, 0.74f));
        var glass = PrimitiveKit.Mat("WindowDark", new Color(0.12f, 0.2f, 0.26f), 0.8f);
        var plank = PrimitiveKit.Mat("WoodPlank", Wood);

        PrimitiveKit.Box("Plinth", h, new Vector3(0f, 0.3f, -0.4f), new Vector3(6f, 0.6f, 4f), stone);
        PrimitiveKit.Box("Walls", h, new Vector3(0f, 2.05f, -0.6f), new Vector3(5.4f, 2.9f, 3.4f), wallMat);
        PrimitiveKit.Box("Porch", h, new Vector3(0f, 0.55f, 2.1f), new Vector3(6f, 0.12f, 1.6f), plank, collider: false);
        PrimitiveKit.Box("Steps", h, new Vector3(0f, 0.25f, 3.15f), new Vector3(1.4f, 0.5f, 0.5f), plank, collider: false);
        for (int i = 0; i < 5; i++)
        {
            float x = -2.9f + i * 1.45f;
            PrimitiveKit.Cylinder("PorchPost", h, new Vector3(x, 1.95f, 2.8f), new Vector3(0.1f, 1.4f, 0.1f), white, collider: false);
        }
        // Railing either side of the steps
        foreach (float side in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Rail", h, new Vector3(side * 1.8f, 1.15f, 2.8f), new Vector3(2.2f, 0.06f, 0.06f), white, collider: false);
            for (int i = 0; i < 7; i++)
                PrimitiveKit.Box("Baluster", h, new Vector3(side * (0.8f + i * 0.33f), 0.87f, 2.8f), new Vector3(0.04f, 0.5f, 0.04f), white, collider: false);
        }

        // Roof: two pitched panels over house and porch, gable ends filled in, fretwork on the eaves
        const float ridgeZ = 0.35f, ridgeY = 4.7f, slope = 26f, half = 2.85f;
        float run = Mathf.Cos(slope * Mathf.Deg2Rad) * half, rise = Mathf.Sin(slope * Mathf.Deg2Rad) * half;
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Roof", h, new Vector3(0f, ridgeY - rise * 0.5f, ridgeZ + side * run * 0.5f), new Vector3(6.6f, 0.12f, half + 0.1f), roofMat, new Vector3(side * slope, 0f, 0f), collider: false);
        PrimitiveKit.Box("Ridge", h, new Vector3(0f, ridgeY + 0.04f, ridgeZ), new Vector3(6.7f, 0.12f, 0.2f), white, collider: false);
        MeshKit.Place("GableFill", h, MeshKit.Gable("CottageGable", run * 2f - 0.6f, ridgeY - 3.5f - 0.05f, 5.4f), wallMat,
            new Vector3(-2.7f, 3.5f, ridgeZ), new Vector3(0f, 90f, 0f));
        for (int i = 0; i < 14; i++)
            PrimitiveKit.Box("Fretwork", h, new Vector3(-3.05f + i * 0.47f, ridgeY - rise - 0.18f, ridgeZ + run + 0.02f), new Vector3(0.2f, 0.28f, 0.03f), white, new Vector3(0f, 0f, 45f), collider: false);

        // Door and shuttered windows on the front, a window each side
        PrimitiveKit.Box("Door", h, new Vector3(0f, 1.6f, 1.12f), new Vector3(1f, 2f, 0.06f), trimMat, collider: false);
        foreach (float x in new[] { -1.8f, 1.8f })
        {
            PrimitiveKit.Box("Window", h, new Vector3(x, 2.1f, 1.12f), new Vector3(0.9f, 1.1f, 0.05f), glass, collider: false);
            PrimitiveKit.Box("Sill", h, new Vector3(x, 1.5f, 1.16f), new Vector3(1.1f, 0.08f, 0.12f), white, collider: false);
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Shutter", h, new Vector3(x + s * 0.68f, 2.1f, 1.14f), new Vector3(0.42f, 1.15f, 0.05f), trimMat, collider: false);
            // Window box of flowers
            PrimitiveKit.Box("FlowerBox", h, new Vector3(x, 1.38f, 1.25f), new Vector3(0.9f, 0.18f, 0.22f), trimMat, collider: false);
            for (int f = 0; f < 4; f++)
                PrimitiveKit.Sphere("Flower", h, new Vector3(x - 0.33f + f * 0.22f, 1.52f, 1.25f), Vector3.one * 0.14f,
                    PrimitiveKit.Mat(f % 2 == 0 ? "HeliconiaRed" : "FlowerYellow", f % 2 == 0 ? new Color(0.9f, 0.15f, 0.12f) : new Color(1f, 0.85f, 0.2f)));
        }
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("SideWindow", h, new Vector3(side * 2.72f, 2.1f, -0.6f), new Vector3(0.05f, 1f, 0.9f), glass, collider: false);
        StreetFurniture.HangingLight(h, new Vector3(0f, 2.95f, 2.2f));
    }

    // ── Brigantine (เรือใบสองเสา) at anchor in the bay ────────────────────────────────
    private static void Brigantine(Transform root, Vector3 pos, float yaw)
    {
        var anchor = PrimitiveKit.Group("Brigantine_เรือใบ", root, pos, yaw).transform;
        var s = PrimitiveKit.Group("Ship", anchor, Vector3.zero).transform;
        var hull = PrimitiveKit.Mat("ShipHull", new Color(0.18f, 0.12f, 0.08f));
        var band = PrimitiveKit.Mat("ShipBand", new Color(0.95f, 0.85f, 0.45f));
        var deck = PrimitiveKit.Mat("WoodPlank", Wood);
        var spar = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var sail = PrimitiveKit.Mat("Sailcloth", new Color(0.97f, 0.94f, 0.85f));
        var port = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        var red = PrimitiveKit.Mat("FlagRed", new Color(0.85f, 0.12f, 0.12f));

        // Hull: a long body, tapering bow, raised quarterdeck at the stern
        PrimitiveKit.Box("Hull", s, new Vector3(0f, 0.9f, 0f), new Vector3(4.6f, 2.4f, 15f), hull, collider: false);
        PrimitiveKit.Box("Bow", s, new Vector3(0f, 1.05f, 8.3f), new Vector3(3.2f, 2.1f, 3.2f), hull, new Vector3(0f, 45f, 0f), collider: false);
        PrimitiveKit.Box("Stern", s, new Vector3(0f, 2.6f, -6.1f), new Vector3(4.6f, 1.2f, 3f), hull, collider: false);
        PrimitiveKit.Box("Band", s, new Vector3(0f, 1.25f, 0f), new Vector3(4.66f, 0.35f, 15.02f), band, collider: false);
        for (int i = 0; i < 6; i++)
            foreach (float side in new[] { -1f, 1f })
                PrimitiveKit.Box("Gunport", s, new Vector3(side * 2.34f, 1.25f, -5f + i * 2f), new Vector3(0.04f, 0.26f, 0.32f), port, collider: false);
        PrimitiveKit.Box("Deck", s, new Vector3(0f, 2.12f, 0.5f), new Vector3(4.4f, 0.05f, 13f), deck, collider: false);
        PrimitiveKit.Box("SternWindows", s, new Vector3(0f, 2.6f, -7.62f), new Vector3(3.2f, 0.5f, 0.04f), PrimitiveKit.Glow("CabinGlow", new Color(1f, 0.8f, 0.45f), 1.2f), collider: false);
        PrimitiveKit.Cylinder("Bowsprit", s, new Vector3(0f, 2.8f, 11.2f), new Vector3(0.14f, 2.6f, 0.14f), spar, new Vector3(70f, 0f, 0f), collider: false);

        // Two masts with square sails (fore) and a gaff mainsail (main), jibs to the bowsprit
        Mast(s, new Vector3(0f, 0f, 3.6f), 14f, true, spar, sail);
        Mast(s, new Vector3(0f, 0f, -2.6f), 15f, false, spar, sail);
        for (int i = 0; i < 2; i++)
            PrimitiveKit.Box("Jib", s, new Vector3(0f, 6.5f - i * 1.5f, 8.2f + i * 0.7f), new Vector3(0.03f, 5f - i * 1.2f, 2.6f), sail, new Vector3(-28f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Pennant", s, new Vector3(0f, 17.9f, -3.3f), new Vector3(0.02f, 0.5f, 1.4f), red, collider: false);
        PrimitiveKit.Box("Flag", s, new Vector3(0f, 3.9f, -7.9f), new Vector3(0.02f, 0.8f, 1.2f), red, collider: false);
        // Lanterns
        foreach (var p in new[] { new Vector3(0f, 3.6f, -7.7f), new Vector3(0f, 3f, 9.4f) })
            PrimitiveKit.Sphere("Lantern", s, p, Vector3.one * 0.3f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        s.gameObject.AddComponent<Bob>();
        // Anchor chain into the water
        PrimitiveKit.Cylinder("AnchorChain", anchor, new Vector3(1.6f, 0.5f, 10.4f), new Vector3(0.06f, 1.4f, 0.06f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)), new Vector3(35f, 0f, 0f), collider: false);
    }

    private static void Mast(Transform s, Vector3 at, float height, bool squareRig, Material spar, Material sail)
    {
        PrimitiveKit.Cylinder("Mast", s, at + new Vector3(0f, height * 0.5f + 1f, 0f), new Vector3(0.28f, height * 0.5f, 0.28f), spar, collider: false);
        PrimitiveKit.Cylinder("Top", s, at + new Vector3(0f, height * 0.62f, 0f), new Vector3(1.1f, 0.08f, 1.1f), spar, collider: false);
        if (squareRig)
        {
            // Three square sails, each narrower than the one below, bellied forward
            for (int i = 0; i < 3; i++)
            {
                float y = 4.5f + i * 3.6f, w = 7f - i * 1.6f, h = 3f - i * 0.4f;
                PrimitiveKit.Cylinder("Yard", s, at + new Vector3(0f, y + h * 0.5f + 0.1f, 0f), new Vector3(0.12f, w * 0.5f + 0.3f, 0.12f), spar, new Vector3(0f, 0f, 90f), collider: false);
                MeshKit.Place("Sail", s, MeshKit.Blob("Sail", 44, 0.02f, 1), sail, at + new Vector3(0f, y, 0.3f), default, new Vector3(w, h, 0.5f));
            }
        }
        else
        {
            // Fore-and-aft mainsail on a boom and gaff, swung a little off the centre line
            PrimitiveKit.Cylinder("Boom", s, at + new Vector3(0.3f, 3.2f, -2.8f), new Vector3(0.12f, 2.8f, 0.12f), spar, new Vector3(90f, 8f, 0f), collider: false);
            PrimitiveKit.Cylinder("Gaff", s, at + new Vector3(0.3f, 11.4f, -2.2f), new Vector3(0.1f, 2.4f, 0.1f), spar, new Vector3(60f, 8f, 0f), collider: false);
            PrimitiveKit.Box("Mainsail", s, at + new Vector3(0.35f, 7.2f, -2.6f), new Vector3(0.04f, 7.6f, 5f), sail, new Vector3(-4f, 8f, 0f), collider: false);
        }
    }

    // Little rowing dinghy with oars, afloat or pulled up on the sand
    private static void Dinghy(Transform root, Vector3 pos, float yaw, Color paint, bool afloat)
    {
        var d = PrimitiveKit.Group("Dinghy_เรือพาย", root, pos, yaw).transform;
        var body = PrimitiveKit.Group("Boat", d, new Vector3(0f, afloat ? 0f : 0.05f, 0f)).transform;
        if (!afloat) body.localRotation = Quaternion.Euler(0f, 0f, 8f);
        var paintMat = PrimitiveKit.Mat("DinghyPaint" + (afloat ? "A" : "B"), paint);
        var inside = PrimitiveKit.Mat("WoodPlank", Wood);
        var white = PrimitiveKit.Mat("TrimWhite", White);
        MeshKit.Place("Hull", body, MeshKit.Blob("DinghyHull", 61, 0.02f, 2), paintMat, new Vector3(0f, 0.2f, 0f), default, new Vector3(1.4f, 0.75f, 3.2f));
        MeshKit.Place("Inside", body, MeshKit.Blob("DinghyHull", 61, 0.02f, 2), inside, new Vector3(0f, 0.36f, 0f), default, new Vector3(1.2f, 0.5f, 2.9f));
        PrimitiveKit.Box("Gunwale", body, new Vector3(0f, 0.57f, 0f), new Vector3(1.32f, 0.04f, 2.6f), white, collider: false);
        PrimitiveKit.Box("Thwart", body, new Vector3(0f, 0.5f, 0.1f), new Vector3(1.25f, 0.05f, 0.3f), inside, collider: false);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Oar", body, new Vector3(side * 0.5f, 0.6f, -0.2f), new Vector3(0.06f, 0.04f, 2.2f), inside, new Vector3(0f, side * 12f, 0f), collider: false);
        if (afloat) body.gameObject.AddComponent<Bob>();
    }

    // ── Lily pond in the jungle clearing (สระบัว) ────────────────────────────────────
    private static void LilyPond(Transform root, Vector3 pos)
    {
        var p = PrimitiveKit.Group("LilyPond_สระบัว", root, pos).transform;
        var water = PrimitiveKit.Mat("PondWater", new Color(0.12f, 0.32f, 0.28f), 0.9f);
        var pad = PrimitiveKit.Mat("LilyPad", new Color(0.25f, 0.55f, 0.2f), 0.4f);
        var petal = PrimitiveKit.Mat("LotusPink", new Color(1f, 0.6f, 0.75f));
        var heart = PrimitiveKit.Mat("FlowerYellow", new Color(1f, 0.85f, 0.2f));
        PrimitiveKit.Cylinder("Water", p, new Vector3(0f, 0.03f, 0f), new Vector3(9f, 0.02f, 6f), water, collider: false);
        var wall = new GameObject("PondBlocker");
        wall.transform.SetParent(p, false);
        wall.transform.localPosition = new Vector3(0f, 1f, 0f);
        wall.transform.localScale = new Vector3(8.4f, 1f, 5.4f);
        var cap = wall.AddComponent<CapsuleCollider>();
        cap.direction = 0;
        cap.height = 1f;
        cap.radius = 0.5f;
        var rng = new System.Random(17);
        for (int i = 0; i < 18; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = (float)rng.NextDouble() * 0.85f;
            var at = new Vector3(Mathf.Cos(a) * r * 4f, 0.05f, Mathf.Sin(a) * r * 2.6f);
            float size = 0.35f + (float)rng.NextDouble() * 0.25f;
            PrimitiveKit.Cylinder("Pad", p, at, new Vector3(size, 0.01f, size), pad, collider: false);
            if (i % 3 == 0)
            {
                for (int k = 0; k < 6; k++)
                    PrimitiveKit.Box("Petal", p, at + Quaternion.Euler(0f, k * 60f, 0f) * new Vector3(0f, 0.12f, 0.08f), new Vector3(0.07f, 0.03f, 0.18f), petal, new Vector3(-35f, k * 60f, 0f), collider: false);
                PrimitiveKit.Sphere("Heart", p, at + Vector3.up * 0.14f, Vector3.one * 0.08f, heart);
            }
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            Nature.Rock(p, new Vector3(Mathf.Cos(a) * 4.7f, 0f, Mathf.Sin(a) * 3.2f), 0.8f + (i % 3) * 0.3f, i + 60);
        }
        Places.Sign(root, pos + new Vector3(0f, 0f, 3.8f), "สระบัวกลางป่า", 0f);
    }

    // ── Treasure chests (หีบสมบัติ): one in a sea cave, one under the swing palm's rocks, one by the east cliff ──
    private static void Treasure(Transform root)
    {
        // A little cave of boulders at the foot of the west cliff
        var cave = PrimitiveKit.Group("SeaCave_ถ้ำหีบสมบัติ", root, new Vector3(-36f, 0f, -24f), 30f).transform;
        var rock = PrimitiveKit.Mat("LimestoneWet", new Color(0.3f, 0.28f, 0.25f), 0.4f);
        var stone = PrimitiveKit.Mat("Limestone", new Color(0.8f, 0.77f, 0.7f), 0.1f);
        MeshKit.Place("CaveWallL", cave, MeshKit.Blob("Karst1", 501, 0.26f, 2), stone, new Vector3(-2.6f, 1.6f, 0f), default, new Vector3(2.6f, 3.6f, 4.4f)).AddComponent<BoxCollider>();
        MeshKit.Place("CaveWallR", cave, MeshKit.Blob("Karst2", 502, 0.26f, 2), stone, new Vector3(2.6f, 1.6f, 0f), default, new Vector3(2.6f, 3.6f, 4.4f)).AddComponent<BoxCollider>();
        MeshKit.Place("CaveBack", cave, MeshKit.Blob("Karst3", 503, 0.26f, 2), rock, new Vector3(0f, 1.6f, -2.2f), default, new Vector3(5.6f, 3.6f, 2f)).AddComponent<BoxCollider>();
        MeshKit.Place("CaveRoof", cave, MeshKit.Blob("Karst4", 504, 0.26f, 2), stone, new Vector3(0f, 3.6f, -0.2f), default, new Vector3(7.4f, 1.8f, 5f));
        Chest(cave, new Vector3(0f, 0f, -0.6f), 0f, "cave");
        StreetFurniture.OrangeLight(cave, new Vector3(0f, 2.6f, 0.4f));

        // Behind the boulders near the swing
        var rocks = PrimitiveKit.Group("SwingRocks_หินหีบสมบัติ", root, new Vector3(-39f, 0f, 12f), 70f).transform;
        Nature.Rock(rocks, new Vector3(-1.5f, 0f, 0.8f), 2.2f, 3);
        Nature.Rock(rocks, new Vector3(1.6f, 0f, 1f), 1.8f, 5);
        Chest(rocks, new Vector3(0f, 0f, -0.4f), 0f, "swing");

        // Under a palm by the east cliff, marked with an X in the sand
        var cliff = PrimitiveKit.Group("CliffTreasure_หีบใต้หน้าผา", root, new Vector3(36f, 0f, -26f), -40f).transform;
        var mark = PrimitiveKit.Mat("TreasureMark", new Color(0.7f, 0.15f, 0.1f));
        PrimitiveKit.Box("X", cliff, new Vector3(0f, 0.04f, 1.6f), new Vector3(1.6f, 0.02f, 0.2f), mark, new Vector3(0f, 45f, 0f), collider: false);
        PrimitiveKit.Box("X", cliff, new Vector3(0f, 0.04f, 1.6f), new Vector3(1.6f, 0.02f, 0.2f), mark, new Vector3(0f, -45f, 0f), collider: false);
        Nature.CoconutPalm(cliff, new Vector3(-1.6f, 0f, -0.6f), 1.1f, 12);
        Chest(cliff, Vector3.zero, 0f, "cliff");
        Places.Sign(root, new Vector3(4f, 0f, -16f), "ตำนานเล่าว่า...\nโจรสลัดซ่อนหีบไว้ 3 ใบบนเกาะนี้", 0f);
    }

    private static void Chest(Transform parent, Vector3 pos, float yaw, string id)
    {
        var c = PrimitiveKit.Group("TreasureChest_หีบสมบัติ", parent, pos, yaw).transform;
        var wood = PrimitiveKit.Mat("ChestWood", new Color(0.45f, 0.26f, 0.12f));
        var iron = PrimitiveKit.Mat("ChestIron", new Color(0.25f, 0.22f, 0.2f), 0.5f, 0.6f);
        var brass = PrimitiveKit.Mat("ChestBrass", new Color(0.95f, 0.75f, 0.3f), 0.7f, 0.8f);
        PrimitiveKit.Box("Body", c, new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.6f, 0.7f), wood);
        foreach (float x in new[] { -0.38f, 0.38f })
            PrimitiveKit.Box("Band", c, new Vector3(x, 0.3f, 0f), new Vector3(0.08f, 0.62f, 0.72f), iron, collider: false);
        // Lid hinged along the back edge
        var lid = PrimitiveKit.Group("LidHinge", c, new Vector3(0f, 0.6f, -0.35f)).transform;
        PrimitiveKit.Box("Lid", lid, new Vector3(0f, 0.08f, 0.35f), new Vector3(1.12f, 0.16f, 0.72f), wood, collider: false);
        PrimitiveKit.Cylinder("LidRound", lid, new Vector3(0f, 0.16f, 0.35f), new Vector3(0.3f, 0.56f, 0.72f), wood, new Vector3(0f, 0f, 90f), collider: false);
        foreach (float x in new[] { -0.38f, 0.38f })
            PrimitiveKit.Cylinder("LidBand", lid, new Vector3(x, 0.165f, 0.35f), new Vector3(0.32f, 0.045f, 0.74f), iron, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Lock", c, new Vector3(0f, 0.52f, 0.37f), new Vector3(0.16f, 0.2f, 0.05f), brass, collider: false);
        var gold = PrimitiveKit.Group("Gold", c, Vector3.zero);
        MeshKit.Place("Coins", gold.transform, MeshKit.Blob("GoldPile", 71, 0.25f, 1), PrimitiveKit.Glow("TreasureGold", new Color(1f, 0.78f, 0.25f), 1.6f),
            new Vector3(0f, 0.62f, 0f), default, new Vector3(0.9f, 0.3f, 0.55f));
        // A faint golden glint so players spot it from afar
        PrimitiveKit.Sphere("Glint", c, new Vector3(0.3f, 0.9f, 0.2f), Vector3.one * 0.12f, PrimitiveKit.Glow("TreasureGlint", new Color(1f, 0.9f, 0.5f), 3f));

        var zone = new GameObject("ChestZone");
        zone.transform.SetParent(c, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 1.2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2f, 2f, 1.6f);
        zone.AddComponent<TreasureChest>().Configure(id, lid, gold);
    }

    // ── Plants: palms on the sand, a jungle of bananas, heliconia and palms behind ──
    private static void Plants(Transform root)
    {
        var palms = new[]
        {
            new Vector3(-30f, 0f, 6f), new Vector3(-21f, 0f, 19f), new Vector3(-12f, 0f, 8f), new Vector3(-1f, 0f, 20f), new Vector3(13f, 0f, 9f),
            new Vector3(26f, 0f, 19f), new Vector3(34f, 0f, 10f), new Vector3(-37f, 0f, -2f), new Vector3(24f, 0f, -4f), new Vector3(-3f, 0f, -8f),
        };
        for (int i = 0; i < palms.Length; i++)
            Nature.CoconutPalm(root, palms[i], 0.95f + (i % 3) * 0.15f, i + 70);

        // Jungle belt behind the beach (z -14 .. -38), around the pond clearing
        var rng = new System.Random(91);
        for (int i = 0; i < 46; i++)
        {
            var p = new Vector3(-38f + (float)rng.NextDouble() * 76f, 0f, -16f - (float)rng.NextDouble() * 22f);
            if (Mathf.Abs(p.x + 3f) < 8f && Mathf.Abs(p.z + 24f) < 6f) continue; // the pond
            if (p.x < -31f && p.z < -18f && p.z > -30f) continue;                // the treasure cave
            if (p.x > 31f && p.z < -20f && p.z > -32f) continue;                 // the X under the palm
            switch (i % 5)
            {
                case 0: Nature.CoconutPalm(root, p, 1f + (float)rng.NextDouble() * 0.4f, i + 100); break;
                case 1: case 3: Banana(root, p, i); break;
                case 2: Heliconia(root, p, i); break;
                default: Nature.Bush(root, p, 1.3f, i, flowers: i % 2 == 0); break;
            }
        }
        // Flowering heliconia and bougainvillea around the cottages and bars
        foreach (var p in new[]
        {
            new Vector3(-31f, 0f, -4f), new Vector3(-22f, 0f, -4.5f), new Vector3(-12f, 0f, -9f), new Vector3(14f, 0f, -8f),
            new Vector3(22f, 0f, -8f), new Vector3(35f, 0f, -12f), new Vector3(26f, 0f, -1f), new Vector3(-12f, 0f, 3f),
        })
            Heliconia(root, p, (int)(p.x * 3f + p.z));
        foreach (var p in new[] { new Vector3(-34f, 0f, -8f), new Vector3(27f, 0f, -14f), new Vector3(36f, 0f, 4f), new Vector3(-20f, 0f, -14f) })
            Nature.Bush(root, p, 1.3f, (int)(p.x + p.z), flowers: true);
        foreach (var p in new[] { new Vector3(-25f, 0f, -12f), new Vector3(12f, 0f, -14f), new Vector3(-9f, 0f, -14f) })
            Banana(root, p, (int)(p.x - p.z));
    }

    // Banana plant (ต้นกล้วย): a few broad drooping leaves on a short trunk, a bunch of green bananas
    private static void Banana(Transform root, Vector3 pos, int seed)
    {
        var b = PrimitiveKit.Group("Banana_ต้นกล้วย", root, pos, seed * 41f % 360f).transform;
        var stem = PrimitiveKit.Mat("BananaStem", new Color(0.45f, 0.55f, 0.25f));
        var leafMat = PrimitiveKit.Mat("BananaLeaf", new Color(0.32f, 0.62f, 0.2f), 0.3f);
        var fruit = PrimitiveKit.Mat("BananaGreen", new Color(0.55f, 0.7f, 0.2f));
        float h = 2.2f + seed % 3 * 0.3f;
        PrimitiveKit.Cylinder("Trunk", b, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.28f, h * 0.5f, 0.28f), stem);
        var leaf = MeshKit.Leaf("BananaLeaf", 2.4f, 0.75f, 0.5f, 1.4f, 10, 0.4f);
        for (int i = 0; i < 7; i++)
            MeshKit.Place("Leaf", b, leaf, leafMat, new Vector3(0f, h, 0f), new Vector3(-35f - (i % 2) * 15f, i * 51f, 0f));
        MeshKit.Place("Bunch", b, MeshKit.Blob("Bunch", 81, 0.3f, 1), fruit, new Vector3(0.3f, h - 0.5f, 0.2f), default, new Vector3(0.4f, 0.6f, 0.4f));
    }

    // Heliconia (ก้ามกุ้ง): paddle leaves and hanging zig-zag red bracts tipped with yellow
    private static void Heliconia(Transform root, Vector3 pos, int seed)
    {
        var h = PrimitiveKit.Group("Heliconia_ก้ามกุ้ง", root, pos, seed * 29f % 360f).transform;
        var leafMat = PrimitiveKit.Mat("HeliconiaLeaf", new Color(0.18f, 0.5f, 0.2f), 0.3f);
        var red = PrimitiveKit.Mat("HeliconiaRed", new Color(0.9f, 0.15f, 0.12f));
        var yellow = PrimitiveKit.Mat("FlowerYellow", new Color(1f, 0.85f, 0.2f));
        var stem = PrimitiveKit.Mat("BananaStem", new Color(0.45f, 0.55f, 0.25f));
        var leaf = MeshKit.Leaf("HeliconiaLeaf", 1.2f, 0.4f, 0.5f, 0.3f, 8, 0.4f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f + seed % 20;
            PrimitiveKit.Cylinder("Stem", h, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.55f, 0.15f), new Vector3(0.04f, 0.55f, 0.04f), stem, collider: false);
            MeshKit.Place("Leaf", h, leaf, leafMat, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 1.05f, 0.15f), new Vector3(-50f, a, 0f));
        }
        for (int f = 0; f < 2; f++)
        {
            var top = new Vector3(f == 0 ? -0.2f : 0.25f, 1.55f + f * 0.2f, f == 0 ? 0.1f : -0.1f);
            for (int k = 0; k < 6; k++)
            {
                float side = k % 2 == 0 ? 1f : -1f;
                var p = top + new Vector3(side * 0.08f, -k * 0.13f, 0f);
                PrimitiveKit.Box("Bract", h, p, new Vector3(0.22f, 0.07f, 0.08f), red, new Vector3(0f, 0f, side * 30f), collider: false);
                PrimitiveKit.Box("Tip", h, p + new Vector3(side * 0.12f, 0.05f, 0f), new Vector3(0.05f, 0.05f, 0.08f), yellow, collider: false);
            }
        }
    }

    // ── Tiki torches along the beach path (light up at night) ─────────────────────────
    private static void Torches(Transform root)
    {
        var bamboo = PrimitiveKit.Mat("BambooPole", new Color(0.72f, 0.62f, 0.38f));
        var flame = PrimitiveKit.Glow("TorchFlame", new Color(1f, 0.55f, 0.15f), 3f);
        for (int i = 0; i < 10; i++)
        {
            var p = new Vector3(-34f + i * 7.4f, 0f, 6.5f + (i % 2) * 1.2f);
            if (Mathf.Abs(p.x + 6f) < 5f) continue; // the bar pavilion
            var t = PrimitiveKit.Group("TikiTorch", root, p).transform;
            PrimitiveKit.Cylinder("Pole", t, new Vector3(0f, 0.9f, 0f), new Vector3(0.07f, 0.9f, 0.07f), bamboo, collider: false);
            PrimitiveKit.Cylinder("Cup", t, new Vector3(0f, 1.85f, 0f), new Vector3(0.18f, 0.12f, 0.18f), bamboo, collider: false);
            MeshKit.Place("Flame", t, MeshKit.Lathe("TorchFlame", new[] { new Vector2(0.1f, 0f), new Vector2(0.12f, 0.1f), new Vector2(0.05f, 0.3f), new Vector2(0f, 0.4f) }, 8),
                flame, new Vector3(0f, 1.95f, 0f));
            if (i % 3 == 0) StreetFurniture.OrangeLight(t, new Vector3(0f, 2.15f, 0f)); // every third torch lights the sand
        }
    }
}
