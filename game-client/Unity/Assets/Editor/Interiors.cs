using UnityEngine;

// Walk-in ground floors for buildings (front faces local -Z), furnished in a clean modern style:
// polished floors, white walls with an accent wall and LED strips, digital queue screens, glass,
// plants. Rooms() makes the thin walls (with a doorway) and the floor; Furnish() fills the room by kind.
// BuildingInterior (runtime) hides the outside and the camera-side walls while you're inside.
public static class Interiors
{
    private const float Wall = 0.2f;

    // Thin walls with a doorway in the front, the floor, and the BuildingInterior component.
    // Returns the "Interior" group (origin at the room's centre, on the floor).
    public static Transform Rooms(Transform b, float width, float depth, float height, float doorWidth, float doorTop,
        Material outside, Color inside, Color accent, Transform shellRoot = null, float shellCenterX = 0f)
    {
        float hw = width * 0.5f, hd = depth * 0.5f, cy = height * 0.5f;
        var paint = PrimitiveKit.Mat("InteriorWall" + Key(inside), inside);
        var led = PrimitiveKit.Glow("InteriorLed" + Key(accent), Color.Lerp(accent, Color.white, 0.4f), 1.8f);

        var back = Group(b, "WallBack");
        PrimitiveKit.Box("Wall", back, new Vector3(0f, cy, hd - Wall * 0.5f), new Vector3(width, height, Wall), outside);
        PrimitiveKit.Box("AccentWall", back, new Vector3(0f, cy, hd - Wall - 0.01f), new Vector3(width - 0.4f, height, 0.02f),
            PrimitiveKit.Mat("AccentWall" + Key(accent), Color.Lerp(accent, new Color(0.15f, 0.17f, 0.2f), 0.55f)), collider: false);
        PrimitiveKit.Box("Led", back, new Vector3(0f, height - 0.3f, hd - Wall - 0.04f), new Vector3(width - 0.6f, 0.05f, 0.04f), led, collider: false);

        foreach (var (name, sx) in new[] { ("WallLeft", -1f), ("WallRight", 1f) })
        {
            var side = Group(b, name);
            PrimitiveKit.Box("Wall", side, new Vector3(sx * (hw - Wall * 0.5f), cy, 0f), new Vector3(Wall, height, depth), outside);
            PrimitiveKit.Box("Paint", side, new Vector3(sx * (hw - Wall - 0.01f), cy, 0f), new Vector3(0.02f, height, depth - 0.4f), paint, collider: false);
            PrimitiveKit.Box("Led", side, new Vector3(sx * (hw - Wall - 0.04f), height - 0.3f, 0f), new Vector3(0.04f, 0.05f, depth - 0.6f), led, collider: false);
        }

        var front = Group(b, "WallFront");
        float seg = hw - doorWidth * 0.5f;
        foreach (float sx in new[] { -1f, 1f })
        {
            float cx = sx * (doorWidth * 0.5f + seg * 0.5f);
            PrimitiveKit.Box("Wall", front, new Vector3(cx, cy, -hd + Wall * 0.5f), new Vector3(seg, height, Wall), outside);
            PrimitiveKit.Box("Paint", front, new Vector3(cx, cy, -hd + Wall + 0.01f), new Vector3(seg - 0.2f, height, 0.02f), paint, collider: false);
        }
        PrimitiveKit.Box("Lintel", front, new Vector3(0f, (doorTop + height) * 0.5f, -hd + Wall * 0.5f), new Vector3(doorWidth, height - doorTop, Wall), outside);

        var room = Group(b, "Interior");
        PrimitiveKit.Box("Floor", room, new Vector3(0f, 0.02f, 0f), new Vector3(width - 0.3f, 0.04f, depth - 0.3f),
            PrimitiveKit.Mat("PolishedFloor", new Color(0.88f, 0.87f, 0.85f), 0.75f), collider: false);
        for (float x = -hw + 1.2f; x < hw - 0.6f; x += 1.2f) // tile joints
            PrimitiveKit.Box("Joint", room, new Vector3(x, 0.045f, 0f), new Vector3(0.015f, 0.005f, depth - 0.3f), PrimitiveKit.Mat("TileJoint", new Color(0.75f, 0.74f, 0.72f)), collider: false);

        b.gameObject.AddComponent<BuildingInterior>().Configure(hw - Wall, hd - Wall, height, shellRoot, shellCenterX);
        return room;
    }

    // ── Furniture by kind ────────────────────────────────────────────────────────────────────────
    public static void Furnish(Transform room, string kind, float width, float depth, float height, Color accent)
    {
        float hw = width * 0.5f - Wall, hd = depth * 0.5f - Wall;
        switch (kind)
        {
            case "bank": Bank(room, hw, hd, height, accent); break;
            case "hospital": Hospital(room, hw, hd, height, accent); break;
            case "hotel": Hotel(room, hw, hd, height, accent); break;
            case "gold": Gold(room, hw, hd, height); break;
            case "store": Store(room, hw, hd, height, accent); break;
            case "spa": Spa(room, hw, hd, height); break;
            case "cafe": Cafe(room, hw, hd, height, accent); break;
            case "food": Food(room, hw, hd, height, accent); break;
            case "barber": Barber(room, hw, hd, height); break;
            case "pharmacy": Pharmacy(room, hw, hd, height); break;
            case "it": It(room, hw, hd, height, accent); break;
            case "music": Music(room, hw, hd, height); break;
            case "clothes": Clothes(room, hw, hd, height); break;
            case "church": Church(room, hw, hd, height); break;
            case "mosque": Mosque(room, hw, hd, height); break;
            case "temple": Temple(room, hw, hd, height); break;
            default: Office(room, hw, hd, height, accent, kind == "sao" ? "อบต. บริการประชาชน" : "บริการประชาชน"); break;
        }
    }

    public static string KindOfShop(string sign) =>
        sign.Contains("กาแฟ") ? "cafe" : sign.Contains("ตัดผม") ? "barber" : sign.Contains("ขายยา") ? "pharmacy"
        : sign.Contains("ไอที") || sign.Contains("ซอฟต์แวร์") ? "it" : sign.Contains("ข้าว") || sign.Contains("อาหาร") || sign.Contains("ก๋วยเตี๋ยว") ? "food"
        : sign.Contains("ดนตรี") ? "music" : sign.Contains("เสื้อผ้า") ? "clothes" : sign.Contains("ทอง") ? "gold" : "store";

    // Government office (อบต., อบจ., อำเภอ, police): service counter with screens, queue display, waiting seats
    private static void Office(Transform r, float hw, float hd, float h, Color accent, string title)
    {
        float counterW = Mathf.Min(hw * 2f - 1.6f, 6f);
        Counter(r, new Vector3(0f, 0f, hd - 1.3f), counterW, accent, Mathf.Max(2, Mathf.FloorToInt(counterW / 1.8f)));
        Screen(r, new Vector3(0f, h - 0.85f, hd - 0.03f), Mathf.Min(counterW, 3.2f), 0.7f, "คิวที่ A012 → ช่อง 2", accent);
        PrimitiveKit.Label(title, r, new Vector3(0f, h - 0.35f, hd - 0.05f), 0.035f, Color.white);
        int rows = depth(hd) > 6f ? 2 : 1;
        for (int i = 0; i < rows; i++) Seats(r, new Vector3(0f, 0f, -hd + 2.2f + i * 1.3f), Mathf.Min(hw * 2f - 2.4f, 4.2f), accent);
        Plant(r, new Vector3(-hw + 0.5f, 0f, hd - 0.5f), 1);
        Plant(r, new Vector3(hw - 0.5f, 0f, hd - 0.5f), 2);
        WallTv(r, new Vector3(-hw + 0.03f, 1.8f, 0f), 90f, "ข่าวสาร · ประกาศ");
        Entrance.Kiosk(r, new Vector3(hw - 0.6f, 0f, -hd + 1.2f), "บัตรคิว", accent);
    }

    private static void Bank(Transform r, float hw, float hd, float h, Color accent)
    {
        float counterW = Mathf.Min(hw * 2f - 1.6f, 6f);
        Counter(r, new Vector3(0f, 0f, hd - 1.3f), counterW, accent, 3);
        for (int i = 0; i < 3; i++) // glass partitions on the counter
            PrimitiveKit.Box("CounterGlass", r, new Vector3((i - 1) * counterW / 3f, 1.4f, hd - 1.55f), new Vector3(counterW / 3f - 0.1f, 0.7f, 0.03f),
                PrimitiveKit.Glow("PartitionGlass", new Color(0.7f, 0.85f, 0.95f), 0.2f, 0.6f), collider: false);
        Screen(r, new Vector3(0f, h - 0.85f, hd - 0.03f), 3f, 0.7f, "Q 105 → ช่อง 3", accent);
        for (int i = 0; i < 3; i++) // ATM wall
        {
            var at = new Vector3(-hw + 0.3f, 0f, -hd + 1.6f + i * 1.1f);
            PrimitiveKit.Box("Atm", r, at + new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 1.6f, 0.8f), PrimitiveKit.Mat("AtmBody", new Color(0.25f, 0.28f, 0.35f)));
            PrimitiveKit.Box("AtmScreen", r, at + new Vector3(0.26f, 1.2f, 0f), new Vector3(0.02f, 0.35f, 0.45f), PrimitiveKit.Glow("AtmScreen", new Color(0.3f, 0.7f, 1f), 1.2f), collider: false);
        }
        PrimitiveKit.Label("ATM", r, new Vector3(-hw + 0.04f, 2f, -hd + 2.7f), 0.04f, Color.Lerp(accent, Color.white, 0.5f), 90f);
        Sofa(r, new Vector3(hw - 1.2f, 0f, -hd + 2f), new Color(0.2f, 0.25f, 0.4f));
        Plant(r, new Vector3(hw - 0.5f, 0f, -hd + 0.8f), 3);
    }

    private static void Hospital(Transform r, float hw, float hd, float h, Color accent)
    {
        Counter(r, new Vector3(-hw * 0.35f, 0f, hd - 1.3f), Mathf.Min(hw, 4f), accent, 2);
        Screen(r, new Vector3(-hw * 0.35f, h - 0.85f, hd - 0.03f), 2.6f, 0.6f, "เรียกคิว 048 → ห้องตรวจ 3", accent);
        PrimitiveKit.Box("PharmacyWindow", r, new Vector3(hw * 0.55f, 1.4f, hd - 0.05f), new Vector3(2.2f, 1f, 0.04f), PrimitiveKit.Glow("PartitionGlass", new Color(0.7f, 0.85f, 0.95f), 0.2f, 0.6f), collider: false);
        PrimitiveKit.Label("ห้องจ่ายยา", r, new Vector3(hw * 0.55f, 2.15f, hd - 0.07f), 0.03f, new Color(0.1f, 0.5f, 0.3f));
        var cross = PrimitiveKit.Glow("MedicalGreen", new Color(0.15f, 0.8f, 0.4f), 1.6f);
        PrimitiveKit.Box("Cross", r, new Vector3(hw - 0.03f, 2f, 0f), new Vector3(0.03f, 0.6f, 0.2f), cross, collider: false);
        PrimitiveKit.Box("Cross", r, new Vector3(hw - 0.03f, 2f, 0f), new Vector3(0.03f, 0.2f, 0.6f), cross, collider: false);
        for (int i = 0; i < 2; i++) Seats(r, new Vector3(0f, 0f, -hd + 2f + i * 1.4f), Mathf.Min(hw * 2f - 2f, 5f), accent);
        Plant(r, new Vector3(-hw + 0.5f, 0f, -hd + 0.6f), 4);
    }

    private static void Hotel(Transform r, float hw, float hd, float h, Color accent)
    {
        Counter(r, new Vector3(0f, 0f, hd - 1.3f), Mathf.Min(hw * 2f - 2f, 4.5f), accent, 2);
        PrimitiveKit.Label("RECEPTION · แผนกต้อนรับ", r, new Vector3(0f, h - 0.6f, hd - 0.05f), 0.035f, Color.Lerp(accent, Color.white, 0.6f));
        Rug(r, new Vector3(0f, 0f, -hd + 2.2f), new Vector2(3.4f, 2.2f), accent);
        Sofa(r, new Vector3(-1.7f, 0f, -hd + 2.6f), new Color(0.9f, 0.88f, 0.82f)); // either side of the path in
        Sofa(r, new Vector3(1.7f, 0f, -hd + 2.6f), new Color(0.9f, 0.88f, 0.82f));
        PrimitiveKit.Box("CoffeeTable", r, new Vector3(0f, 0.22f, -hd + 2.4f), new Vector3(1.2f, 0.06f, 0.6f), PrimitiveKit.Mat("WalnutTop", new Color(0.4f, 0.26f, 0.16f), 0.6f), collider: false);
        Pendant(r, new Vector3(0f, h - 0.2f, -hd + 2.3f), 0.9f, h);
        Plant(r, new Vector3(-hw + 0.5f, 0f, hd - 0.5f), 5);
        Plant(r, new Vector3(hw - 0.5f, 0f, hd - 0.5f), 6);
    }

    private static void Gold(Transform r, float hw, float hd, float h)
    {
        var red = PrimitiveKit.Mat("GoldShopRed", new Color(0.7f, 0.08f, 0.08f));
        var glass = PrimitiveKit.Glow("DisplayGlass", new Color(0.85f, 0.92f, 1f), 0.35f, 0.7f);
        var gold = PrimitiveKit.Glow("GoldGlint", new Color(1f, 0.8f, 0.25f), 1.4f);
        foreach (float sx in new[] { -1f, 1f }) // display counters along both walls
        {
            float x = sx * (hw - 0.45f);
            PrimitiveKit.Box("Cabinet", r, new Vector3(x, 0.45f, 0.2f), new Vector3(0.7f, 0.9f, hd * 1.4f), red);
            PrimitiveKit.Box("Glass", r, new Vector3(x, 1.05f, 0.2f), new Vector3(0.7f, 0.3f, hd * 1.4f), glass, collider: false);
            for (int i = 0; i < 6; i++)
                PrimitiveKit.Box("Gold", r, new Vector3(x, 0.95f, 0.2f - hd * 0.6f + i * hd * 0.24f), new Vector3(0.3f, 0.06f, 0.15f), gold, collider: false);
        }
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.4f, 0.6f, "ราคาทองวันนี้ 41,250 ฿", new Color(1f, 0.8f, 0.25f));
        Stool(r, new Vector3(-hw + 1.3f, 0f, 0f)); Stool(r, new Vector3(hw - 1.3f, 0f, 0f));
    }

    private static void Store(Transform r, float hw, float hd, float h, Color accent)
    {
        for (int i = 0; i < 2; i++) Shelf(r, new Vector3(i == 0 ? -hw * 0.45f : hw * 0.45f, 0f, 0.4f), new Vector3(0.6f, 1.5f, hd * 1.1f), 10 + i);
        for (int i = 0; i < 3; i++) // glowing fridges on the back wall
        {
            var at = new Vector3(-hw + 0.8f + i * 1.2f, 0f, hd - 0.4f);
            PrimitiveKit.Box("Fridge", r, at + new Vector3(0f, 1f, 0f), new Vector3(1.1f, 2f, 0.7f), PrimitiveKit.Glow("FridgeGlow", new Color(0.8f, 0.92f, 1f), 0.8f, 0.85f));
        }
        Counter(r, new Vector3(hw - 1.2f, 0f, -hd + 1.4f), 1.6f, accent, 1);
        Screen(r, new Vector3(0f, h - 0.6f, hd - 0.03f), 2.6f, 0.45f, "สแกนจ่าย · เปิด 24 ชม.", accent);
    }

    private static void Spa(Transform r, float hw, float hd, float h)
    {
        var mat = PrimitiveKit.Mat("SpaBed", new Color(0.95f, 0.9f, 0.82f));
        for (int i = 0; i < 2; i++)
        {
            var at = new Vector3(i == 0 ? -hw * 0.45f : hw * 0.45f, 0f, 0.6f);
            PrimitiveKit.Box("SpaBed", r, at + new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 2f), mat);
            PrimitiveKit.Box("Towel", r, at + new Vector3(0f, 0.72f, 0.6f), new Vector3(0.7f, 0.05f, 0.4f), PrimitiveKit.Mat("TowelWhite", Color.white), collider: false);
        }
        PrimitiveKit.Box("Divider", r, new Vector3(0f, 1f, 0.6f), new Vector3(0.05f, 2f, 2.2f), PrimitiveKit.Mat("BambooScreen", new Color(0.75f, 0.6f, 0.35f)));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Sphere("Candle", r, new Vector3(-0.6f + i * 0.6f, 0.08f, hd - 0.3f), Vector3.one * 0.12f, PrimitiveKit.Glow("CandleGlow", new Color(1f, 0.7f, 0.35f), 2f));
        Plant(r, new Vector3(-hw + 0.5f, 0f, -hd + 0.6f), 7);
        Plant(r, new Vector3(hw - 0.5f, 0f, hd - 0.5f), 8);
    }

    // ── Shophouse units (about 4 m wide, 9.6 m deep) ──
    private static void Cafe(Transform r, float hw, float hd, float h, Color accent)
    {
        var top = PrimitiveKit.Mat("WalnutTop", new Color(0.4f, 0.26f, 0.16f), 0.6f);
        PrimitiveKit.Box("Bar", r, new Vector3(0f, 0.5f, hd - 1.3f), new Vector3(hw * 2f - 0.6f, 1f, 0.6f), PrimitiveKit.Mat("CafeBar", new Color(0.2f, 0.2f, 0.22f)));
        PrimitiveKit.Box("BarTop", r, new Vector3(0f, 1.03f, hd - 1.3f), new Vector3(hw * 2f - 0.5f, 0.06f, 0.7f), top, collider: false);
        PrimitiveKit.Box("Espresso", r, new Vector3(-0.6f, 1.3f, hd - 1.35f), new Vector3(0.6f, 0.45f, 0.4f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.6f, 0.6f, "เมนู · อเมริกาโน่ 55 · ลาเต้ 65", accent);
        for (int i = 0; i < 3; i++) Table(r, new Vector3(i % 2 == 0 ? -0.8f : 0.8f, 0f, -hd + 1.6f + i * 1.6f), top);
        for (int i = 0; i < 3; i++) Pendant(r, new Vector3(-1f + i, h - 0.2f, hd - 1.3f), 0.8f, h);
        Plant(r, new Vector3(hw - 0.4f, 0f, -hd + 0.5f), 9);
    }

    private static void Food(Transform r, float hw, float hd, float h, Color accent)
    {
        var top = PrimitiveKit.Mat("TableWhite", new Color(0.95f, 0.95f, 0.93f));
        PrimitiveKit.Box("Kitchen", r, new Vector3(0f, 0.5f, hd - 1f), new Vector3(hw * 2f - 0.4f, 1f, 0.8f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)));
        PrimitiveKit.Box("ChickenDisplay", r, new Vector3(0.6f, 1.3f, hd - 1f), new Vector3(1f, 0.6f, 0.6f), PrimitiveKit.Glow("PartitionGlass", new Color(0.7f, 0.85f, 0.95f), 0.2f, 0.6f), collider: false);
        for (int i = 0; i < 3; i++) PrimitiveKit.Sphere("HangingChicken", r, new Vector3(0.3f + i * 0.3f, 1.45f, hd - 1f), new Vector3(0.18f, 0.25f, 0.15f), PrimitiveKit.Mat("RoastChicken", new Color(0.85f, 0.55f, 0.25f)));
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.6f, 0.6f, "ข้าวมันไก่ 50 · พิเศษ 60", accent);
        for (int i = 0; i < 3; i++) Table(r, new Vector3(i % 2 == 0 ? -0.8f : 0.8f, 0f, -hd + 1.6f + i * 1.7f), top);
    }

    private static void Barber(Transform r, float hw, float hd, float h)
    {
        var mirror = PrimitiveKit.Glow("Mirror", new Color(0.85f, 0.92f, 0.98f), 0.5f, 0.8f);
        for (int i = 0; i < 2; i++)
        {
            float z = -0.6f + i * 2.2f;
            PrimitiveKit.Box("Mirror", r, new Vector3(-hw + 0.03f, 1.5f, z), new Vector3(0.03f, 1.1f, 0.9f), mirror, collider: false);
            PrimitiveKit.Box("MirrorLight", r, new Vector3(-hw + 0.05f, 2.1f, z), new Vector3(0.04f, 0.05f, 0.9f), PrimitiveKit.Glow("InteriorLedWhite", Color.white, 2f), collider: false);
            PrimitiveKit.Box("Shelf", r, new Vector3(-hw + 0.2f, 0.9f, z), new Vector3(0.35f, 0.05f, 0.9f), PrimitiveKit.Mat("TableWhite", new Color(0.95f, 0.95f, 0.93f)), collider: false);
            BarberChair(r, new Vector3(-hw + 1.1f, 0f, z));
        }
        PrimitiveKit.Box("Pole", r, new Vector3(hw - 0.15f, 1.6f, -hd + 0.6f), new Vector3(0.14f, 0.8f, 0.14f), PrimitiveKit.Glow("BarberPole", new Color(0.9f, 0.2f, 0.25f), 1.2f), collider: false);
        Seats(r, new Vector3(hw - 0.5f, 0f, hd - 2f), 2.4f, new Color(0.2f, 0.2f, 0.25f), 90f);
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.4f, 0.55f, "ตัดผม 150 · สระไดร์ 100", new Color(0.9f, 0.25f, 0.3f));
    }

    private static void Pharmacy(Transform r, float hw, float hd, float h)
    {
        foreach (float sx in new[] { -1f, 1f }) Shelf(r, new Vector3(sx * (hw - 0.35f), 0f, 0.4f), new Vector3(0.5f, 2f, hd * 1.4f), sx < 0 ? 20 : 21);
        Counter(r, new Vector3(0f, 0f, hd - 1.3f), 2.4f, new Color(0.15f, 0.7f, 0.45f), 1);
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.4f, 0.55f, "ปรึกษาเภสัชกร", new Color(0.15f, 0.7f, 0.45f));
    }

    private static void It(Transform r, float hw, float hd, float h, Color accent)
    {
        for (int row = 0; row < 3; row++)
            foreach (float sx in new[] { -1f, 1f })
                Desk(r, new Vector3(sx * (hw - 0.75f), 0f, -hd + 2f + row * 2.2f), sx < 0 ? 90f : -90f, accent);
        Screen(r, new Vector3(0f, h - 0.9f, hd - 0.03f), 3f, 0.8f, "Sprint 12 · Build ✓ · 99.9%", accent);
        PrimitiveKit.Box("Beanbag", r, new Vector3(0f, 0.25f, hd - 1.2f), new Vector3(0.8f, 0.5f, 0.8f), PrimitiveKit.Mat("BeanbagOrange", new Color(0.95f, 0.5f, 0.15f)), collider: false);
        Plant(r, new Vector3(0f, 0f, -hd + 0.6f), 10);
    }

    private static void Music(Transform r, float hw, float hd, float h)
    {
        var colors = new[] { new Color(0.8f, 0.2f, 0.15f), new Color(0.15f, 0.15f, 0.18f), new Color(0.85f, 0.6f, 0.3f), new Color(0.2f, 0.4f, 0.8f) };
        for (int i = 0; i < 6; i++) // guitars on the side walls
        {
            float sx = i % 2 == 0 ? -1f : 1f;
            var at = new Vector3(sx * (hw - 0.12f), 1.5f, -hd + 1.8f + (i / 2) * 1.6f);
            PrimitiveKit.Sphere("GuitarBody", r, at, new Vector3(0.12f, 0.5f, 0.38f), PrimitiveKit.Mat("Guitar" + (i % 4), colors[i % 4]));
            PrimitiveKit.Box("GuitarNeck", r, at + new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 0.7f, 0.07f), PrimitiveKit.Mat("WalnutTop", new Color(0.4f, 0.26f, 0.16f), 0.6f), collider: false);
        }
        PrimitiveKit.Box("Keyboard", r, new Vector3(0f, 0.8f, hd - 1.5f), new Vector3(1.4f, 0.12f, 0.4f), PrimitiveKit.Mat("PianoBlack", new Color(0.08f, 0.08f, 0.09f), 0.8f));
        PrimitiveKit.Box("Keys", r, new Vector3(0f, 0.87f, hd - 1.6f), new Vector3(1.3f, 0.02f, 0.15f), PrimitiveKit.Mat("TowelWhite", Color.white), collider: false);
        Screen(r, new Vector3(0f, h - 0.8f, hd - 0.03f), 2.4f, 0.55f, "ลองเล่นได้ · ผ่อน 0%", new Color(0.6f, 0.3f, 0.9f));
    }

    private static void Clothes(Transform r, float hw, float hd, float h)
    {
        var colors = new[] { new Color(0.9f, 0.4f, 0.5f), new Color(0.3f, 0.5f, 0.8f), new Color(0.95f, 0.85f, 0.4f), new Color(0.3f, 0.65f, 0.45f), Color.white };
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = sx * (hw - 0.4f);
            PrimitiveKit.Box("Rail", r, new Vector3(x, 1.6f, 0.2f), new Vector3(0.04f, 0.04f, hd * 1.4f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
            for (int i = 0; i < 10; i++)
                PrimitiveKit.Box("Shirt", r, new Vector3(x, 1.15f, 0.2f - hd * 0.65f + i * hd * 0.14f), new Vector3(0.45f, 0.8f, 0.05f), PrimitiveKit.Mat("Cloth" + (i % 5), colors[i % 5]), collider: false);
        }
        PrimitiveKit.Box("FittingMirror", r, new Vector3(0f, 1.1f, hd - 0.03f), new Vector3(0.8f, 1.8f, 0.03f), PrimitiveKit.Glow("Mirror", new Color(0.85f, 0.92f, 0.98f), 0.5f, 0.8f), collider: false);
        Mannequin(r, new Vector3(0f, 0f, -hd + 1.2f), colors[0]);
    }

    // ── Places of worship ──
    // Church: two banks of pews either side of the aisle, the altar and a cross on the back wall
    private static void Church(Transform r, float hw, float hd, float h)
    {
        var wood = PrimitiveKit.Mat("PewWood", new Color(0.45f, 0.28f, 0.15f), 0.5f);
        var red = PrimitiveKit.Mat("AisleRed", new Color(0.6f, 0.1f, 0.12f));
        PrimitiveKit.Box("Aisle", r, new Vector3(0f, 0.05f, 0f), new Vector3(1.4f, 0.02f, hd * 2f - 0.6f), red, collider: false);
        for (int row = 0; row < 5; row++)
            foreach (float sx in new[] { -1f, 1f })
            {
                var at = new Vector3(sx * (0.9f + (hw - 1.1f) * 0.5f), 0f, -hd + 1.6f + row * 1.3f);
                float w = hw - 1.1f;
                PrimitiveKit.Box("Pew", r, at + new Vector3(0f, 0.45f, 0f), new Vector3(w, 0.08f, 0.45f), wood, collider: false);
                PrimitiveKit.Box("PewBack", r, at + new Vector3(0f, 0.75f, -0.22f), new Vector3(w, 0.6f, 0.06f), wood, collider: false);
            }
        PrimitiveKit.Box("AltarStep", r, new Vector3(0f, 0.1f, hd - 1f), new Vector3(hw * 2f - 0.4f, 0.2f, 1.6f), PrimitiveKit.Mat("TableWhite", new Color(0.95f, 0.95f, 0.93f)));
        PrimitiveKit.Box("Altar", r, new Vector3(0f, 0.65f, hd - 0.8f), new Vector3(2f, 0.9f, 0.7f), PrimitiveKit.Mat("TowelWhite", Color.white));
        var gold = PrimitiveKit.Glow("AltarGold", new Color(1f, 0.8f, 0.3f), 1.2f);
        PrimitiveKit.Box("CrossV", r, new Vector3(0f, 2.8f, hd - 0.05f), new Vector3(0.18f, 1.8f, 0.06f), gold, collider: false);
        PrimitiveKit.Box("CrossH", r, new Vector3(0f, 3.2f, hd - 0.05f), new Vector3(1f, 0.18f, 0.06f), gold, collider: false);
        foreach (float x in new[] { -0.7f, 0.7f })
            PrimitiveKit.Sphere("Candle", r, new Vector3(x, 1.2f, hd - 0.8f), new Vector3(0.08f, 0.15f, 0.08f), PrimitiveKit.Glow("CandleGlow", new Color(1f, 0.7f, 0.35f), 2f));
    }

    // Mosque prayer hall: carpet in rows facing the mihrab niche, the minbar and a chandelier
    private static void Mosque(Transform r, float hw, float hd, float h)
    {
        var green = PrimitiveKit.Mat("MosqueGreen", new Color(0.1f, 0.5f, 0.35f));
        PrimitiveKit.Box("Carpet", r, new Vector3(0f, 0.05f, 0.2f), new Vector3(hw * 2f - 0.6f, 0.02f, hd * 2f - 1.6f), PrimitiveKit.Mat("PrayerCarpet", new Color(0.55f, 0.12f, 0.15f)), collider: false);
        for (int row = 0; row < 5; row++) // rows (saf) woven into the carpet
            PrimitiveKit.Box("CarpetRow", r, new Vector3(0f, 0.065f, -hd + 1.6f + row * 1.2f), new Vector3(hw * 2f - 0.7f, 0.005f, 0.08f), PrimitiveKit.Mat("CarpetGold", new Color(0.85f, 0.65f, 0.25f)), collider: false);
        PrimitiveKit.Box("Mihrab", r, new Vector3(0f, 1.4f, hd - 0.08f), new Vector3(1.4f, 2.8f, 0.1f), green, collider: false);
        PrimitiveKit.Sphere("MihrabArch", r, new Vector3(0f, 2.8f, hd - 0.08f), new Vector3(1.4f, 0.9f, 0.1f), green);
        PrimitiveKit.Box("MihrabInner", r, new Vector3(0f, 1.3f, hd - 0.14f), new Vector3(0.9f, 2.2f, 0.04f), PrimitiveKit.Glow("MihrabGold", new Color(0.95f, 0.8f, 0.4f), 0.8f), collider: false);
        for (int i = 0; i < 4; i++) // minbar steps
            PrimitiveKit.Box("Minbar", r, new Vector3(1.5f, 0.2f + i * 0.2f, hd - 0.9f + i * 0.15f), new Vector3(0.7f, 0.4f + i * 0.4f, 0.3f), PrimitiveKit.Mat("PewWood", new Color(0.45f, 0.28f, 0.15f), 0.5f));
        PrimitiveKit.Cylinder("ChandelierRing", r, new Vector3(0f, h - 1.2f, 0f), new Vector3(1.6f, 0.03f, 1.6f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            PrimitiveKit.Sphere("ChandelierBulb", r, new Vector3(Mathf.Cos(a) * 0.8f, h - 1.3f, Mathf.Sin(a) * 0.8f), Vector3.one * 0.15f, PrimitiveKit.Glow("PendantGlow", new Color(1f, 0.85f, 0.6f), 2f));
        }
    }

    // Ordination hall (โบสถ์): the gold Buddha on a tiered altar, red carpet, candles, lotus, murals
    private static void Temple(Transform r, float hw, float hd, float h)
    {
        var gold = PrimitiveKit.Mat("BuddhaGold", new Color(0.95f, 0.75f, 0.25f), 0.8f, 0.85f);
        var red = PrimitiveKit.Mat("TempleCarpet", new Color(0.65f, 0.1f, 0.1f));
        PrimitiveKit.Box("Carpet", r, new Vector3(0f, 0.05f, -0.4f), new Vector3(hw * 2f - 1f, 0.02f, hd * 2f - 3f), red, collider: false);
        for (int i = 0; i < 3; i++) // tiered altar (ฐานชุกชี)
            PrimitiveKit.Box("Altar", r, new Vector3(0f, 0.2f + i * 0.35f, hd - 1.3f + i * 0.2f), new Vector3(3.6f - i * 0.8f, 0.4f, 1.6f - i * 0.4f), i % 2 == 0 ? PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f) : gold);
        // Seated Buddha (พระประธาน)
        var at = new Vector3(0f, 1.05f, hd - 0.8f);
        PrimitiveKit.Sphere("BuddhaLap", r, at + new Vector3(0f, 0.15f, -0.1f), new Vector3(1.1f, 0.35f, 0.7f), gold);
        PrimitiveKit.Sphere("BuddhaBody", r, at + new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 1f, 0.5f), gold);
        PrimitiveKit.Sphere("BuddhaHead", r, at + new Vector3(0f, 1.4f, -0.02f), Vector3.one * 0.38f, gold);
        PrimitiveKit.Cylinder("Ushnisha", r, at + new Vector3(0f, 1.7f, -0.02f), new Vector3(0.1f, 0.12f, 0.1f), gold, collider: false);
        PrimitiveKit.Box("Halo", r, at + new Vector3(0f, 1.1f, 0.35f), new Vector3(1.6f, 2.2f, 0.05f), PrimitiveKit.Glow("HaloGold", new Color(1f, 0.82f, 0.4f), 0.9f), collider: false);
        foreach (float x in new[] { -1.4f, 1.4f })
        {
            PrimitiveKit.Cylinder("Candle", r, new Vector3(x, 1.25f, hd - 1.7f), new Vector3(0.06f, 0.2f, 0.06f), PrimitiveKit.Mat("CandleYellow", new Color(1f, 0.9f, 0.5f)), collider: false);
            PrimitiveKit.Sphere("Flame", r, new Vector3(x, 1.5f, hd - 1.7f), new Vector3(0.05f, 0.09f, 0.05f), PrimitiveKit.Glow("CandleGlow", new Color(1f, 0.7f, 0.35f), 2f));
            PrimitiveKit.Sphere("Lotus", r, new Vector3(x * 0.6f, 1.15f, hd - 1.6f), new Vector3(0.2f, 0.18f, 0.2f), PrimitiveKit.Mat("LotusPink", new Color(0.95f, 0.55f, 0.7f)));
        }
        var muralColours = new[] { new Color(0.75f, 0.35f, 0.2f), new Color(0.2f, 0.45f, 0.4f), new Color(0.85f, 0.65f, 0.3f), new Color(0.35f, 0.3f, 0.55f) };
        for (int i = 0; i < 4; i++) // murals (จิตรกรรมฝาผนัง) between the windows
            foreach (float sx in new[] { -1f, 1f })
                PrimitiveKit.Box("Mural", r, new Vector3(sx * (hw - 0.03f), 1.7f, -hd + 2f + i * (hd * 2f - 4f) / 3f), new Vector3(0.02f, 1.2f, 1.1f), PrimitiveKit.Mat("Mural" + i, muralColours[i]), collider: false);
    }

    // ── Furniture pieces ──
    private static void Counter(Transform r, Vector3 at, float width, Color accent, int seats)
    {
        PrimitiveKit.Box("Counter", r, at + new Vector3(0f, 0.55f, 0f), new Vector3(width, 1.1f, 0.7f), PrimitiveKit.Mat("CounterWhite", new Color(0.96f, 0.96f, 0.95f)));
        PrimitiveKit.Box("CounterTop", r, at + new Vector3(0f, 1.13f, 0f), new Vector3(width + 0.1f, 0.06f, 0.8f), PrimitiveKit.Mat("WalnutTop", new Color(0.4f, 0.26f, 0.16f), 0.6f), collider: false);
        PrimitiveKit.Box("CounterLed", r, at + new Vector3(0f, 0.15f, -0.36f), new Vector3(width, 0.05f, 0.02f), PrimitiveKit.Glow("InteriorLed" + Key(accent), Color.Lerp(accent, Color.white, 0.4f), 1.8f), collider: false);
        for (int i = 0; i < seats; i++)
        {
            float x = (i - (seats - 1) * 0.5f) * (width / seats);
            PrimitiveKit.Box("Monitor", r, at + new Vector3(x, 1.42f, 0.15f), new Vector3(0.5f, 0.32f, 0.03f), PrimitiveKit.Glow("MonitorScreen", new Color(0.35f, 0.65f, 0.95f), 0.9f), collider: false);
            PrimitiveKit.Box("MonitorStand", r, at + new Vector3(x, 1.2f, 0.17f), new Vector3(0.05f, 0.14f, 0.05f), PrimitiveKit.Mat("DoorAluminium", new Color(0.2f, 0.21f, 0.23f)), collider: false);
            OfficeChair(r, at + new Vector3(x, 0f, 0.8f));
        }
    }

    private static void Screen(Transform r, Vector3 at, float width, float height, string text, Color accent)
    {
        PrimitiveKit.Box("Screen", r, at, new Vector3(width, height, 0.04f), PrimitiveKit.Glow("ScreenDark", new Color(0.05f, 0.09f, 0.14f), 0.5f), collider: false);
        PrimitiveKit.Label(text, r, at + new Vector3(0f, 0f, -0.04f), Mathf.Min(0.035f, width / (text.Length * 1.1f) * 1.6f), Color.Lerp(accent, Color.white, 0.55f));
    }

    private static void WallTv(Transform r, Vector3 at, float yaw, string text)
    {
        var tv = PrimitiveKit.Group("WallTv", r, at, yaw).transform;
        PrimitiveKit.Box("Tv", tv, Vector3.zero, new Vector3(1.4f, 0.8f, 0.05f), PrimitiveKit.Glow("TvScreen", new Color(0.2f, 0.45f, 0.75f), 0.8f), collider: false);
        PrimitiveKit.Label(text, tv, new Vector3(0f, 0f, -0.04f), 0.025f, Color.white);
    }

    private static void Seats(Transform r, Vector3 at, float width, Color accent, float yaw = 0f)
    {
        var g = PrimitiveKit.Group("WaitingSeats", r, at, yaw).transform;
        var seat = PrimitiveKit.Mat("SeatCushion" + Key(accent), Color.Lerp(accent, new Color(0.2f, 0.22f, 0.26f), 0.4f));
        PrimitiveKit.Box("Beam", g, new Vector3(0f, 0.35f, 0f), new Vector3(width, 0.05f, 0.08f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        int n = Mathf.Max(2, Mathf.FloorToInt(width / 0.6f));
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) * 0.5f) * (width / n);
            PrimitiveKit.Box("Seat", g, new Vector3(x, 0.45f, 0f), new Vector3(0.48f, 0.08f, 0.45f), seat, collider: false);
            PrimitiveKit.Box("Back", g, new Vector3(x, 0.75f, -0.22f), new Vector3(0.48f, 0.5f, 0.06f), seat, collider: false);
        }
        foreach (float x in new[] { -width * 0.45f, width * 0.45f })
            PrimitiveKit.Box("Leg", g, new Vector3(x, 0.18f, 0f), new Vector3(0.06f, 0.36f, 0.4f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
    }

    private static void Sofa(Transform r, Vector3 at, Color color)
    {
        var m = PrimitiveKit.Mat("Sofa" + Key(color), color);
        PrimitiveKit.Box("SofaBase", r, at + new Vector3(0f, 0.22f, 0f), new Vector3(1.6f, 0.44f, 0.8f), m);
        PrimitiveKit.Box("SofaBack", r, at + new Vector3(0f, 0.6f, -0.32f), new Vector3(1.6f, 0.5f, 0.18f), m, collider: false);
        foreach (float x in new[] { -0.78f, 0.78f })
            PrimitiveKit.Box("SofaArm", r, at + new Vector3(x, 0.4f, 0f), new Vector3(0.14f, 0.35f, 0.8f), m, collider: false);
    }

    private static void Table(Transform r, Vector3 at, Material top)
    {
        PrimitiveKit.Cylinder("TableTop", r, at + new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.03f, 0.8f), top, collider: false);
        PrimitiveKit.Cylinder("TableLeg", r, at + new Vector3(0f, 0.37f, 0f), new Vector3(0.08f, 0.37f, 0.08f), PrimitiveKit.Mat("DoorAluminium", new Color(0.2f, 0.21f, 0.23f)));
        foreach (float x in new[] { -0.6f, 0.6f }) Stool(r, at + new Vector3(x, 0f, 0f));
    }

    private static void Stool(Transform r, Vector3 at)
    {
        PrimitiveKit.Cylinder("StoolSeat", r, at + new Vector3(0f, 0.48f, 0f), new Vector3(0.38f, 0.04f, 0.38f), PrimitiveKit.Mat("WalnutTop", new Color(0.4f, 0.26f, 0.16f), 0.6f), collider: false);
        PrimitiveKit.Cylinder("StoolLeg", r, at + new Vector3(0f, 0.23f, 0f), new Vector3(0.05f, 0.23f, 0.05f), PrimitiveKit.Mat("DoorAluminium", new Color(0.2f, 0.21f, 0.23f)), collider: false);
    }

    private static void OfficeChair(Transform r, Vector3 at)
    {
        var black = PrimitiveKit.Mat("ChairBlack", new Color(0.12f, 0.12f, 0.14f));
        PrimitiveKit.Box("ChairSeat", r, at + new Vector3(0f, 0.48f, 0f), new Vector3(0.45f, 0.08f, 0.45f), black, collider: false);
        PrimitiveKit.Box("ChairBack", r, at + new Vector3(0f, 0.8f, 0.2f), new Vector3(0.42f, 0.5f, 0.06f), black, collider: false);
        PrimitiveKit.Cylinder("ChairPost", r, at + new Vector3(0f, 0.24f, 0f), new Vector3(0.05f, 0.22f, 0.05f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
    }

    private static void Desk(Transform r, Vector3 at, float yaw, Color accent)
    {
        var d = PrimitiveKit.Group("Desk", r, at, yaw).transform;
        PrimitiveKit.Box("DeskTop", d, new Vector3(0f, 0.74f, 0f), new Vector3(1.4f, 0.05f, 0.7f), PrimitiveKit.Mat("TableWhite", new Color(0.95f, 0.95f, 0.93f)));
        foreach (float x in new[] { -0.65f, 0.65f })
            PrimitiveKit.Box("DeskLeg", d, new Vector3(x, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.6f), PrimitiveKit.Mat("DoorAluminium", new Color(0.2f, 0.21f, 0.23f)), collider: false);
        foreach (float x in new[] { -0.3f, 0.3f })
            PrimitiveKit.Box("Monitor", d, new Vector3(x, 1.05f, 0.2f), new Vector3(0.55f, 0.34f, 0.03f), PrimitiveKit.Glow("MonitorScreen" + Key(accent), Color.Lerp(accent, new Color(0.3f, 0.6f, 0.95f), 0.5f), 0.9f), collider: false);
        OfficeChair(d, new Vector3(0f, 0f, -0.6f));
    }

    private static void Shelf(Transform r, Vector3 at, Vector3 size, int seed)
    {
        PrimitiveKit.Box("Shelf", r, at + new Vector3(0f, size.y * 0.5f, 0f), size, PrimitiveKit.Mat("TableWhite", new Color(0.95f, 0.95f, 0.93f)));
        var colors = new[] { new Color(0.9f, 0.3f, 0.25f), new Color(0.25f, 0.55f, 0.9f), new Color(0.95f, 0.8f, 0.2f), new Color(0.3f, 0.75f, 0.4f), new Color(0.85f, 0.45f, 0.85f) };
        int levels = Mathf.Max(2, Mathf.FloorToInt(size.y / 0.45f));
        int per = Mathf.Max(3, Mathf.FloorToInt(size.z / 0.35f));
        for (int l = 0; l < levels; l++)
            for (int i = 0; i < per; i++)
            {
                var c = colors[(i * 7 + l * 3 + seed) % colors.Length];
                foreach (float sx in new[] { -1f, 1f })
                    PrimitiveKit.Box("Product", r, at + new Vector3(sx * (size.x * 0.5f + 0.06f), 0.25f + l * 0.42f, -size.z * 0.5f + 0.2f + i * (size.z - 0.4f) / Mathf.Max(1, per - 1)),
                        new Vector3(0.12f, 0.25f, 0.22f), PrimitiveKit.Mat("Product" + ((i * 7 + l * 3 + seed) % colors.Length), c), collider: false);
            }
    }

    private static void BarberChair(Transform r, Vector3 at)
    {
        var leather = PrimitiveKit.Mat("BarberLeather", new Color(0.55f, 0.12f, 0.12f), 0.6f);
        PrimitiveKit.Cylinder("ChairBase", r, at + new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.05f, 0.5f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Cylinder("ChairPost", r, at + new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.22f, 0.12f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("ChairSeat", r, at + new Vector3(0f, 0.6f, 0f), new Vector3(0.55f, 0.14f, 0.55f), leather, collider: false);
        PrimitiveKit.Box("ChairBack", r, at + new Vector3(0.28f, 1f, 0f), new Vector3(0.1f, 0.75f, 0.52f), leather, collider: false);
        PrimitiveKit.Box("Headrest", r, at + new Vector3(0.3f, 1.45f, 0f), new Vector3(0.1f, 0.16f, 0.28f), leather, collider: false);
    }

    private static void Mannequin(Transform r, Vector3 at, Color shirt)
    {
        var white = PrimitiveKit.Mat("TowelWhite", Color.white);
        PrimitiveKit.Cylinder("Stand", r, at + new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Create(PrimitiveType.Capsule, "Torso", r, at + new Vector3(0f, 1.3f, 0f), new Vector3(0.4f, 0.35f, 0.25f), PrimitiveKit.Mat("Cloth0", shirt), default, false);
        PrimitiveKit.Sphere("Head", r, at + new Vector3(0f, 1.78f, 0f), Vector3.one * 0.22f, white);
    }

    private static void Rug(Transform r, Vector3 at, Vector2 size, Color accent) =>
        PrimitiveKit.Box("Rug", r, at + new Vector3(0f, 0.05f, 0f), new Vector3(size.x, 0.02f, size.y), PrimitiveKit.Mat("Rug" + Key(accent), Color.Lerp(accent, Color.white, 0.55f)), collider: false);

    private static void Pendant(Transform r, Vector3 at, float drop, float ceiling)
    {
        PrimitiveKit.Cylinder("PendantCord", r, at + new Vector3(0f, -drop * 0.5f + 0.1f, 0f), new Vector3(0.02f, drop * 0.5f, 0.02f), PrimitiveKit.Mat("ChairBlack", new Color(0.12f, 0.12f, 0.14f)), collider: false);
        PrimitiveKit.Sphere("PendantBulb", r, at + new Vector3(0f, -drop, 0f), Vector3.one * 0.25f, PrimitiveKit.Glow("PendantGlow", new Color(1f, 0.85f, 0.6f), 2f));
    }

    private static void Plant(Transform r, Vector3 at, int seed) => BuildingDetail.PottedPlant(r, at, seed);

    private static float depth(float hd) => hd * 2f;

    private static Transform Group(Transform parent, string name) => PrimitiveKit.Group(name, parent, Vector3.zero).transform;

    private static string Key(Color c) => $"{Mathf.RoundToInt(c.r * 9)}{Mathf.RoundToInt(c.g * 9)}{Mathf.RoundToInt(c.b * 9)}";
}
