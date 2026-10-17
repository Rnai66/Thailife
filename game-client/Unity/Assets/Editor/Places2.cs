using UnityEngine;

// More place builders (front faces local -Z), split from Places.cs to keep files readable:
// village hall, SAO office, park, fishing canal, shophouses and the clock tower.
public static class Places2
{
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);
    private static readonly Color DarkWood = new Color(0.36f, 0.22f, 0.12f);

    // ── Village hall (ศาลากลางหมู่บ้าน) with the village loudspeaker tower (หอกระจายข่าว) ──
    public static GameObject VillageHall(Transform parent, Vector3 position, string name)
    {
        var hall = PrimitiveKit.Group("VillageHall_ศาลากลางหมู่บ้าน", parent, position);
        var t = hall.transform;
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var dark = PrimitiveKit.Mat("DarkWood", DarkWood);
        var roof = PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f));
        var concrete = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var board = PrimitiveKit.Mat("NoticeGreen", new Color(0.15f, 0.4f, 0.25f));
        var metal = PrimitiveKit.Mat("Metal", new Color(0.35f, 0.35f, 0.38f));

        // Open pavilion on a low concrete floor (0.3 m — one step)
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.15f, 0f), new Vector3(12f, 0.3f, 8f), concrete);
        foreach (float x in new[] { -5.6f, -1.9f, 1.9f, 5.6f })
        foreach (float z in new[] { -3.6f, 3.6f })
            PrimitiveKit.Cylinder("Post", t, new Vector3(x, 1.9f, z), new Vector3(0.25f, 1.6f, 0.25f), dark);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 4.3f, -2.3f), new Vector3(13f, 0.15f, 5.4f), roof, new Vector3(-30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 4.3f, 2.3f), new Vector3(13f, 0.15f, 5.4f), roof, new Vector3(30f, 0f, 0f), collider: false);

        // Benches for village meetings
        for (int row = 0; row < 3; row++)
            PrimitiveKit.Box("Bench", t, new Vector3(0f, 0.55f, -1.5f + row * 1.4f), new Vector3(7f, 0.1f, 0.4f), wood, collider: false);

        // Notice board and name sign
        PrimitiveKit.Box("NoticeBoard", t, new Vector3(-3.5f, 1.3f, -4.6f), new Vector3(2.2f, 1.4f, 0.08f), board);
        PrimitiveKit.Label("ประกาศ", t, new Vector3(-3.5f, 1.8f, -4.65f), 0.045f, Color.white);
        PrimitiveKit.Box("NameBoard", t, new Vector3(0f, 3.45f, -3.75f), new Vector3(5f, 0.6f, 0.08f), wood, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 3.45f, -3.8f), 0.06f, new Color(1f, 0.95f, 0.8f));

        // หอกระจายข่าว: pole with horn speakers
        var tower = PrimitiveKit.Group("BroadcastTower_หอกระจายข่าว", t, new Vector3(7.5f, 0f, -3f));
        PrimitiveKit.Cylinder("Pole", tower.transform, new Vector3(0f, 4f, 0f), new Vector3(0.2f, 4f, 0.2f), metal);
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            var horn = PrimitiveKit.Group("Horn", tower.transform, new Vector3(0f, 7.6f, 0f), yaw);
            PrimitiveKit.Cylinder("Speaker", horn.transform, new Vector3(0f, 0f, -0.35f), new Vector3(0.35f, 0.25f, 0.35f), concrete, new Vector3(90f, 0f, 0f), collider: false);
        }
        return hall;
    }

    // ── Rice barn (ยุ้งข้าว): small raised wooden storehouse; stand in front to see the stock ──
    public static GameObject Granary(Transform parent, Vector3 position, float yaw)
    {
        var barn = PrimitiveKit.Group("Granary_ยุ้งข้าว", parent, position, yaw);
        var t = barn.transform;
        var dark = PrimitiveKit.Mat("DarkWood", DarkWood);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var thatch = PrimitiveKit.Mat("Thatch", new Color(0.75f, 0.62f, 0.35f));
        var rice = PrimitiveKit.Mat("BananaFruit", new Color(0.85f, 0.75f, 0.35f));

        // Raised on posts (keeps rice dry and away from rats)
        foreach (float x in new[] { -1.4f, 1.4f })
        foreach (float z in new[] { -1.4f, 1.4f })
            PrimitiveKit.Cylinder("Post", t, new Vector3(x, 0.6f, z), new Vector3(0.2f, 0.6f, 0.2f), dark);
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 1.25f, 0f), new Vector3(3.4f, 0.12f, 3.4f), dark);
        PrimitiveKit.Box("Walls", t, new Vector3(0f, 2.2f, 0f), new Vector3(3f, 1.8f, 3f), wood);
        PrimitiveKit.Box("Door", t, new Vector3(0f, 2.1f, -1.52f), new Vector3(0.9f, 1.4f, 0.04f), dark, collider: false);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 3.55f, -0.95f), new Vector3(3.8f, 0.12f, 2.4f), thatch, new Vector3(-38f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 3.55f, 0.95f), new Vector3(3.8f, 0.12f, 2.4f), thatch, new Vector3(38f, 0f, 0f), collider: false);
        // Ladder and rice sacks in front
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("LadderRung", t, new Vector3(0f, 0.3f + i * 0.28f, -1.9f + i * 0.08f), new Vector3(0.8f, 0.05f, 0.08f), dark, collider: false);
        PrimitiveKit.Sphere("RiceSack", t, new Vector3(1.2f, 0.3f, -2.1f), new Vector3(0.6f, 0.6f, 0.5f), rice);
        PrimitiveKit.Sphere("RiceSack", t, new Vector3(1.7f, 0.28f, -1.9f), new Vector3(0.55f, 0.55f, 0.45f), rice);

        var zone = new GameObject("GranaryZone");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -2.6f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 2f, 1.8f);
        zone.AddComponent<GranarySpot>();

        Places.Sign(t, new Vector3(-2.2f, 0f, -2.4f), "ยุ้งข้าว\nเกี่ยวแล้วเก็บที่นี่", 0f);
        return barn;
    }

    // ── SAO office (อบต. — องค์การบริหารส่วนตำบล) ───────────────────────────────
    public static GameObject SaoOffice(Transform parent, Vector3 position, string name)
    {
        var office = PrimitiveKit.Group("SAO_อบต", parent, position);
        var t = office.transform;
        var cream = PrimitiveKit.Mat("SaoCream", new Color(0.96f, 0.92f, 0.8f));
        var roof = PrimitiveKit.Mat("SaoRoof", new Color(0.55f, 0.15f, 0.12f));
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));
        var concrete = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));

        PrimitiveKit.Box("Building", t, new Vector3(0f, 3f, 0f), new Vector3(14f, 6f, 8f), cream);
        for (int floor = 0; floor < 2; floor++)
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("Window", t, new Vector3(-5.2f + i * 2.6f, 1.6f + floor * 3f, -4.03f), new Vector3(1.4f, 1.2f, 0.05f), window, collider: false);
        // Thai-style roof with a gold gable over the entrance
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 7.1f, -2.15f), new Vector3(15f, 0.15f, 5.2f), roof, new Vector3(-35f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 7.1f, 2.15f), new Vector3(15f, 0.15f, 5.2f), roof, new Vector3(35f, 0f, 0f));
        PrimitiveKit.Box("Porch", t, new Vector3(0f, 3.4f, -5.3f), new Vector3(5f, 0.2f, 2.6f), cream, collider: false);
        PrimitiveKit.Cylinder("PorchPostL", t, new Vector3(-2.2f, 1.7f, -6.4f), new Vector3(0.3f, 1.7f, 0.3f), white);
        PrimitiveKit.Cylinder("PorchPostR", t, new Vector3(2.2f, 1.7f, -6.4f), new Vector3(0.3f, 1.7f, 0.3f), white);
        PrimitiveKit.Box("Gable", t, new Vector3(0f, 4.4f, -6.5f), new Vector3(2f, 2f, 0.1f), gold, new Vector3(0f, 0f, 45f), collider: false);
        PrimitiveKit.Box("Steps", t, new Vector3(0f, 0.1f, -5f), new Vector3(4f, 0.2f, 2f), concrete);
        PrimitiveKit.Box("NameBoard", t, new Vector3(0f, 6.4f, -4.05f), new Vector3(9f, 0.7f, 0.06f), roof, collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 6.4f, -4.1f), 0.065f, new Color(1f, 0.9f, 0.5f));

        // Flagpoles: national flag in front
        ThaiFlag(t, new Vector3(-6f, 0f, -9f));
        return office;
    }

    // ── Public park (สวนสาธารณะ): lawn, ring path, fountain, benches, exercise bars ──
    public static GameObject Park(Transform parent, Vector3 position, string name)
    {
        var park = PrimitiveKit.Group("Park_สวนสาธารณะ", parent, position);
        var t = park.transform;
        var lawn = PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f));
        var path = PrimitiveKit.Mat("ParkPath", new Color(0.8f, 0.72f, 0.58f));
        var water = PrimitiveKit.Mat("Water", new Color(0.25f, 0.5f, 0.75f));
        var stone = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var metal = PrimitiveKit.Mat("Metal", new Color(0.35f, 0.35f, 0.38f));
        var flowers = new[]
        {
            PrimitiveKit.Mat("FlowerPink", new Color(1f, 0.45f, 0.7f)),
            PrimitiveKit.Mat("Marigold", new Color(1f, 0.6f, 0.05f)),
            PrimitiveKit.Mat("FlowerWhite", new Color(0.97f, 0.97f, 0.95f)),
        };

        // Ring path = a wide flat disc of path with a lawn disc on top inside it
        PrimitiveKit.Cylinder("PathRing", t, new Vector3(0f, 0.012f, 0f), new Vector3(18f, 0.01f, 18f), path, collider: false);
        PrimitiveKit.Cylinder("InnerLawn", t, new Vector3(0f, 0.022f, 0f), new Vector3(14f, 0.01f, 14f), lawn, collider: false);
        PrimitiveKit.Box("PathIn", t, new Vector3(0f, 0.02f, -7f), new Vector3(2f, 0.01f, 6f), path, collider: false);

        // Fountain pond in the middle
        PrimitiveKit.Cylinder("FountainRim", t, new Vector3(0f, 0.25f, 0f), new Vector3(4.4f, 0.25f, 4.4f), stone);
        PrimitiveKit.Cylinder("FountainWater", t, new Vector3(0f, 0.48f, 0f), new Vector3(4f, 0.02f, 4f), water, collider: false);
        PrimitiveKit.Cylinder("FountainColumn", t, new Vector3(0f, 1f, 0f), new Vector3(0.4f, 0.6f, 0.4f), stone, collider: false);
        PrimitiveKit.Sphere("FountainSpray", t, new Vector3(0f, 1.8f, 0f), new Vector3(0.7f, 0.5f, 0.7f), water);

        // Benches around the ring and flower beds
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f + 30f;
            var spot = PrimitiveKit.Group("BenchSpot", t, Vector3.zero, angle);
            PrimitiveKit.Box("Bench", spot.transform, new Vector3(0f, 0.45f, 10.2f), new Vector3(2f, 0.1f, 0.5f), wood);
            PrimitiveKit.Box("Bed", spot.transform, new Vector3(0f, 0.15f, 5.2f), new Vector3(1.6f, 0.3f, 0.8f), flowers[i % flowers.Length], collider: false);
        }

        // Outdoor exercise bars
        var gym = PrimitiveKit.Group("ExerciseBars", t, new Vector3(11f, 0f, 6f));
        foreach (float x in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Post", gym.transform, new Vector3(x, 1.1f, 0f), new Vector3(0.1f, 1.1f, 0.1f), metal);
        PrimitiveKit.Cylinder("Bar", gym.transform, new Vector3(0f, 2.1f, 0f), new Vector3(0.06f, 1f, 0.06f), metal, new Vector3(0f, 0f, 90f), collider: false);

        foreach (var pos in new[] { new Vector3(-11f, 0f, -6f), new Vector3(-12f, 0f, 6f), new Vector3(12f, 0f, -7f), new Vector3(0f, 0f, 12f) })
            Places.ShadeTree(t, pos, 0.9f);

        Places.Sign(t, new Vector3(3f, 0f, -10f), name, 0f);
        return park;
    }

    // ── Canal (คลอง) with fish and fishing spots on the near (-Z) bank ─────────────
    public static GameObject Canal(Transform parent, Vector3 position, float length, float[] fishingSpotsX)
    {
        var canal = PrimitiveKit.Group("Canal_คลอง", parent, position);
        var t = canal.transform;
        var water = PrimitiveKit.Mat("CanalWater", new Color(0.25f, 0.45f, 0.5f));
        var bank = PrimitiveKit.Mat("Concrete", new Color(0.75f, 0.75f, 0.72f));
        var fishMat = PrimitiveKit.Mat("FishShadow", new Color(0.12f, 0.2f, 0.22f));
        var wood = PrimitiveKit.Mat("DarkWood", DarkWood);
        const float width = 6f;

        PrimitiveKit.Box("Water", t, new Vector3(0f, 0.03f, 0f), new Vector3(length, 0.02f, width), water, collider: false);
        PrimitiveKit.Box("BankNear", t, new Vector3(0f, 0.12f, -width * 0.5f - 0.3f), new Vector3(length, 0.25f, 0.6f), bank);
        PrimitiveKit.Box("BankFar", t, new Vector3(0f, 0.12f, width * 0.5f + 0.3f), new Vector3(length, 0.25f, 0.6f), bank);

        // Invisible wall so players can't walk on the water
        var blocker = new GameObject("WaterBlocker");
        blocker.transform.SetParent(t, false);
        blocker.transform.localPosition = new Vector3(0f, 1f, 0f);
        blocker.AddComponent<BoxCollider>().size = new Vector3(length, 2f, width);

        // Fish shadows drifting under the surface
        for (int i = 0; i < 12; i++)
        {
            float x = Mathf.Lerp(-length * 0.4f, length * 0.4f, i / 11f);
            var fish = PrimitiveKit.Sphere("Fish", t, new Vector3(x, 0.045f, Random.Range(-1.5f, 1.5f)), new Vector3(0.18f, 0.01f, 0.55f), fishMat);
            fish.AddComponent<FishSwim>();
        }

        // Fishing spots: a small wooden landing (ท่าน้ำ) on the near bank, trigger zone, bobber over the water
        foreach (float x in fishingSpotsX)
        {
            var spot = PrimitiveKit.Group("FishingSpot_ท่าน้ำ", t, new Vector3(x, 0f, -width * 0.5f - 1.6f));
            PrimitiveKit.Box("Landing", spot.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.4f, 0.16f, 2f), wood);
            PrimitiveKit.Box("Pole", spot.transform, new Vector3(1f, 0.9f, 0.8f), new Vector3(0.08f, 1.6f, 0.08f), wood, collider: false);

            var bobber = PrimitiveKit.Group("BobberPoint", spot.transform, new Vector3(0f, 0.1f, 4f));
            var zone = new GameObject("FishingZone");
            zone.transform.SetParent(spot.transform, false);
            zone.transform.localPosition = new Vector3(0f, 1f, 0f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.6f, 2f, 2.2f);
            zone.AddComponent<FishingSpot>().Configure(bobber.transform);

            Places.Sign(spot.transform, new Vector3(-1.8f, 0f, -0.6f), "ตกปลาตรงนี้\n(กด F / ปุ่มตกปลา)", 0f);
        }
        return canal;
    }

    // ── Shophouse row (ตึกแถว) ───────────────────────────────────────────────────
    public static GameObject Shophouses(Transform parent, Vector3 position, float yaw, int units, string[] signs)
    {
        var row = PrimitiveKit.Group("Shophouses_ตึกแถว", parent, position, yaw);
        var t = row.transform;
        Color[] facades = { new Color(0.95f, 0.85f, 0.65f), new Color(0.75f, 0.85f, 0.9f), new Color(0.95f, 0.75f, 0.7f), new Color(0.85f, 0.9f, 0.75f) };
        var window = PrimitiveKit.Mat("Window", new Color(0.3f, 0.45f, 0.55f));
        var shutter = PrimitiveKit.Mat("RollShutter", new Color(0.6f, 0.62f, 0.64f));
        var awning = PrimitiveKit.Mat("BusStopBlue", new Color(0.2f, 0.4f, 0.75f));
        var signMat = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        const float unitWidth = 4.5f;

        for (int i = 0; i < units; i++)
        {
            float x = (i - (units - 1) * 0.5f) * unitWidth;
            var facade = PrimitiveKit.Mat($"Facade{i % facades.Length}", facades[i % facades.Length]);
            PrimitiveKit.Box("Unit", t, new Vector3(x, 4.5f, 0f), new Vector3(unitWidth - 0.1f, 9f, 10f), facade);
            PrimitiveKit.Box("Shutter", t, new Vector3(x, 1.4f, -5.03f), new Vector3(3.6f, 2.8f, 0.05f), shutter, collider: false);
            for (int floor = 1; floor < 3; floor++)
                PrimitiveKit.Box("Window", t, new Vector3(x, 1.6f + floor * 3f, -5.03f), new Vector3(2.8f, 1.4f, 0.05f), window, collider: false);
            PrimitiveKit.Box("Awning", t, new Vector3(x, 3.1f, -5.8f), new Vector3(unitWidth - 0.2f, 0.08f, 1.6f), awning, new Vector3(-12f, 0f, 0f), collider: false);
            if (signs != null && i < signs.Length && !string.IsNullOrEmpty(signs[i]))
            {
                PrimitiveKit.Box("Sign", t, new Vector3(x, 3.75f, -5.08f), new Vector3(3.8f, 0.7f, 0.06f), signMat, collider: false);
                PrimitiveKit.Label(signs[i], t, new Vector3(x, 3.75f, -5.13f), 0.05f, new Color(0.6f, 0.1f, 0.1f));
            }
        }
        return row;
    }

    // ── Clock tower roundabout (หอนาฬิกา) ─────────────────────────────────────────
    public static GameObject ClockTower(Transform parent, Vector3 position)
    {
        var tower = PrimitiveKit.Group("ClockTower_หอนาฬิกา", parent, position);
        var t = tower.transform;
        var white = PrimitiveKit.Mat("TempleWhite", new Color(0.95f, 0.94f, 0.9f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));
        var face = PrimitiveKit.Mat("ClockFace", new Color(0.98f, 0.98f, 0.95f));
        var hands = PrimitiveKit.Mat("ClockHands", new Color(0.1f, 0.1f, 0.1f));
        var lawn = PrimitiveKit.Mat("FieldGreen", new Color(0.3f, 0.65f, 0.28f));

        PrimitiveKit.Cylinder("Island", t, new Vector3(0f, 0.15f, 0f), new Vector3(9f, 0.15f, 9f), lawn);
        PrimitiveKit.Box("Base", t, new Vector3(0f, 0.8f, 0f), new Vector3(3f, 1f, 3f), white);
        PrimitiveKit.Box("Shaft", t, new Vector3(0f, 5f, 0f), new Vector3(2f, 7.5f, 2f), white);
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            var side = PrimitiveKit.Group("ClockSide", t, new Vector3(0f, 7.6f, 0f), yaw);
            PrimitiveKit.Cylinder("Face", side.transform, new Vector3(0f, 0f, -1.02f), new Vector3(1.5f, 0.02f, 1.5f), face, new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Hour", side.transform, new Vector3(0f, 0.2f, -1.05f), new Vector3(0.08f, 0.45f, 0.02f), hands, collider: false);
            PrimitiveKit.Box("Minute", side.transform, new Vector3(0.25f, 0f, -1.05f), new Vector3(0.6f, 0.06f, 0.02f), hands, collider: false);
        }
        PrimitiveKit.Box("Cap", t, new Vector3(0f, 9f, 0f), new Vector3(2.4f, 0.3f, 2.4f), gold, collider: false);
        PrimitiveKit.Cylinder("Spire", t, new Vector3(0f, 10f, 0f), new Vector3(0.6f, 0.8f, 0.6f), gold, collider: false);
        return tower;
    }

    public static void ThaiFlag(Transform parent, Vector3 position)
    {
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        PrimitiveKit.Cylinder("Flagpole", parent, position + new Vector3(0f, 4f, 0f), new Vector3(0.12f, 4f, 0.12f), white);
        Color[] stripes = { new Color(0.75f, 0.1f, 0.15f), Color.white, new Color(0.1f, 0.15f, 0.45f), Color.white, new Color(0.75f, 0.1f, 0.15f) };
        float[] heights = { 0.2f, 0.2f, 0.4f, 0.2f, 0.2f };
        float y = 7.6f;
        for (int i = 0; i < stripes.Length; i++)
        {
            y -= heights[i] * 0.5f;
            PrimitiveKit.Box("Flag", parent, position + new Vector3(0.75f, y, 0f), new Vector3(1.5f, heights[i], 0.03f), PrimitiveKit.Mat($"Flag{i}", stripes[i]), collider: false);
            y -= heights[i] * 0.5f;
        }
    }
}
