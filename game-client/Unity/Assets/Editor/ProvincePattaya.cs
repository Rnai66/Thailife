using UnityEditor;
using UnityEngine;

// ชลบุรี (พัทยา): the seaside resort town (ProvinceTown) and its ten sights: หาดพัทยา with its
// umbrellas and loungers, the PATTAYA CITY letters on เขาพระตำหนัก, the golden Big Buddha on เขาพระบาท,
// the all-wood ปราสาทสัจธรรม, Walking Street in neon, เกาะล้าน out in the bay, the floating market,
// สวนนงนุช, วัดญาณสังวราราม and the 3D art museum.
public static class ProvincePattaya
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "Pattaya", Root = "Pattaya", Name = "ชลบุรี (พัทยา)",
        Airport = "ท่าอากาศยานอู่ตะเภา", Station = "สถานีรถไฟพัทยา",
        MainStreet = "ถนนพัทยาสาย 2", Monument = "พัทยา · เมืองพัทยา",
        GantryWest = "↓ พระใหญ่เขาพระบาท · สวนนงนุช\n← เขาพระตำหนัก · ปราสาทสัจธรรม",
        GantryEast = "↑ วอล์กกิ้งสตรีท · ตลาดน้ำ 4 ภาค\nหาดพัทยา · ท่าเรือไปเกาะล้าน ↑",
        ShopsWest = new[] { "ร้านอาหารทะเล", "ร้านนวดแผนไทย", "ร้านของฝาก", "ร้านเช่ามอเตอร์ไซค์" },
        ShopsEast = new[] { "ร้านทอง", "ร้านแลกเงิน", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = true, Accent = new Color(0.1f, 0.6f, 0.75f), Airlines = new[] { 2, 4 },
        CoachRoutes = new[] { "กรุงเทพฯ (เอกมัย)", "ระยอง", "สุวรรณภูมิ" },
        SongthaewRoutes = new[] { "รถสองแถว · เลียบหาด", "พัทยาใต้", "นาเกลือ", "จอมเทียน" },
    };

    [MenuItem("Thai Life/Provinces/Build ชลบุรี (พัทยา)")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        PattayaBeach(root);
        ViewPoint(root);
        BigBuddha(root);
        Sanctuary(root);
        WalkingStreet(root);
        KohLarn(root);
        FloatingMarket(root);
        NongNooch(root);
        Yansangwararam(root);
        ArtMuseum(root);
    });

    // 1. หาดพัทยา: beach umbrellas, loungers, palms, food stalls at the back of the beach
    private static void PattayaBeach(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Beach", "หาดพัทยา", new Vector3(-20f, 0f, 104f), 0f,
            "ชายหาดยาว 3 กม. · ร่มชายหาด กีฬาทางน้ำ", new Vector3(5f, 0f, -2f), new Vector3(0f, 0f, 4f), new Vector2(20f, 4f), 10);
        Color[] cloth = { new Color(0.95f, 0.3f, 0.25f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.82f, 0.2f), new Color(0.3f, 0.75f, 0.45f) };
        var white = PrimitiveKit.Mat("PK_Lounger", new Color(0.95f, 0.95f, 0.93f), 0.4f);
        for (int i = 0; i < 8; i++)
        {
            float x = -21f + i * 6f;
            PrimitiveKit.Cylinder("UmbrellaPole", t, new Vector3(x, 1.2f, 6f), new Vector3(0.08f, 1.2f, 0.08f), ProvinceKit.Steel, collider: false);
            PrimitiveKit.Cylinder("Umbrella", t, new Vector3(x, 2.4f, 6f), new Vector3(2.6f, 0.08f, 2.6f), PrimitiveKit.Mat("PK_BeachCloth" + i % cloth.Length, cloth[i % cloth.Length], 0.3f), new Vector3(8f, 0f, 0f), collider: false);
            foreach (float s in new[] { -0.7f, 0.7f })
                PrimitiveKit.Box("Lounger", t, new Vector3(x + s, 0.3f, 6.4f), new Vector3(0.6f, 0.12f, 1.8f), white, new Vector3(-8f, 0f, 0f), collider: false);
        }
        for (int i = 0; i < 3; i++)
            ProvinceKit.Stall(t, new Vector3(12f + i * 5f, 0f, -1f), 0f, cloth[(i + 1) % cloth.Length], new Color(0.95f, 0.75f, 0.55f));
        TambonBuilder.ShopZone(t, new Vector3(17f, 0f, -2.5f), "beach");
    }

    // 2. จุดชมวิวเขาพระตำหนัก: up the stairs to the PATTAYA CITY letters over the bay
    private static void ViewPoint(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "จุดชมวิวเขาพระตำหนัก", new Vector3(-70f, 0f, 22f), -90f,
            "ป้าย PATTAYA CITY · มองเห็นอ่าวพัทยาทั้งอ่าว", new Vector3(6f, 0f, -17f), new Vector3(0f, 0f, -17f), new Vector2(8f, 2f), 5);
        ProvinceKit.Hill(t, Vector3.zero, 0f, 15f, 7f, 7f, out var summit);
        var frame = PrimitiveKit.Group("PattayaCity_ป้าย", t, summit + new Vector3(0f, 0f, 3f)).transform;
        foreach (float x in new[] { -4.5f, 4.5f })
            PrimitiveKit.Box("Leg", frame, new Vector3(x, 1.4f, 0f), new Vector3(0.25f, 2.8f, 0.25f), ProvinceKit.Steel);
        PrimitiveKit.Box("Board", frame, new Vector3(0f, 2.6f, 0f), new Vector3(10f, 1.6f, 0.15f), PrimitiveKit.Mat("PK_LetterWhite", new Color(0.97f, 0.97f, 0.97f), 0.4f));
        PrimitiveKit.Label("PATTAYA CITY", frame, new Vector3(0f, 2.6f, -0.1f), 0.16f, new Color(0.85f, 0.1f, 0.15f));
        PrimitiveKit.Label("PATTAYA CITY", frame, new Vector3(0f, 2.6f, 0.1f), 0.16f, new Color(0.85f, 0.1f, 0.15f), 180f);
    }

    // 3. พระใหญ่ เขาพระบาท: the golden Buddha on the hilltop, the naga stairs toward the avenue
    private static void BigBuddha(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "พระใหญ่ เขาพระบาท", new Vector3(-58f, 0f, -58f), 180f,
            "พระพุทธรูปทองคำปางมารวิชัย สูง 18 ม. · วัดพระใหญ่", new Vector3(6f, 0f, -22f), new Vector3(0f, 0f, -22f), new Vector2(10f, 3f), 8);
        ProvinceKit.Hill(t, Vector3.zero, 0f, 20f, 9f, 6.5f, out var summit);
        BuddhaStatue.Build(t, summit + new Vector3(0f, 0f, 1.5f), 3.2f, detail: true, arch: false);
        for (int i = 0; i < 4; i++) // the seven-day Buddhas round the summit (small)
            ProvinceKit.StandingBuddha(t, summit + Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, 5.2f), 180f + i * 90f, 1.6f);
    }

    // 4. ปราสาทสัจธรรม: the great carved-wood sanctuary, its spire and two wings
    private static void Sanctuary(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "ปราสาทสัจธรรม", new Vector3(-50f, 0f, 78f), 0f,
            "ปราสาทไม้แกะสลักทั้งหลัง สูง 105 ม. · ริมทะเล", new Vector3(-20f, 0f, -8f), new Vector3(0f, 0f, -9f), new Vector2(16f, 2f), 8);
        var teak = PrimitiveKit.Mat("PK_Teak", new Color(0.55f, 0.36f, 0.2f), 0.35f);
        ProvinceKit.SpirePavilion(t, new Vector3(0f, 0f, 1f), 0f, 12f, teak, ProvinceKit.Wood);
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.ThaiHall(t, new Vector3(s * 15f, 0f, 2f), s * 90f, 6f, 8f, teak, ProvinceKit.Wood);
        for (int i = 0; i < 6; i++) // carved figures along the forecourt
            PrimitiveKit.Box("Carving", t, new Vector3(-10f + i * 4f, 1.2f, -8.5f), new Vector3(0.7f, 2.4f, 0.7f), teak);
    }

    // 5. วอล์กกิ้งสตรีท: the neon gateway, bar fronts glowing down both sides
    private static void WalkingStreet(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "วอล์กกิ้งสตรีท", new Vector3(29f, 0f, 31f), 0f,
            "ถนนคนเดินยามค่ำคืน · ไฟนีออน ร้านอาหารทะเล", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(30f, 3f), 12);
        ProvinceKit.Path(t, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f), 6f);
        var gate = PrimitiveKit.Group("Gate_ซุ้ม", t, new Vector3(-20f, 0f, 0f), 90f).transform;
        foreach (float x in new[] { -3.3f, 3.3f })
            PrimitiveKit.Box("Post", gate, new Vector3(x, 2.5f, 0f), new Vector3(0.5f, 5f, 0.5f), ProvinceKit.Steel);
        PrimitiveKit.Box("Neon", gate, new Vector3(0f, 5.3f, 0f), new Vector3(7.4f, 1.2f, 0.3f), PrimitiveKit.Glow("PK_NeonBlue", new Color(0.2f, 0.45f, 1f), 2.5f, 0.3f), collider: false);
        PrimitiveKit.Label("WALKING STREET", gate, new Vector3(0f, 5.3f, -0.2f), 0.07f, Color.white);
        PrimitiveKit.Label("WALKING STREET", gate, new Vector3(0f, 5.3f, 0.2f), 0.07f, Color.white, 180f);
        Color[] neon = { new Color(1f, 0.2f, 0.6f), new Color(0.2f, 0.9f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.6f, 0.3f, 1f), new Color(0.3f, 1f, 0.4f) };
        for (int i = 0; i < 6; i++)
            foreach (float s in new[] { -1f, 1f })
            {
                float x = -15f + i * 6f;
                PrimitiveKit.Box("BarFront", t, new Vector3(x, 2f, s * 6.5f), new Vector3(5.4f, 4f, 2f), PrimitiveKit.Mat("PK_BarWall", new Color(0.15f, 0.13f, 0.18f), 0.5f));
                PrimitiveKit.Box("NeonSign", t, new Vector3(x, 3.4f, s * 5.45f), new Vector3(3.6f, 0.7f, 0.1f), PrimitiveKit.Glow("PK_Neon" + (i + (s > 0 ? 2 : 0)) % neon.Length, neon[(i + (s > 0 ? 2 : 0)) % neon.Length], 2.6f, 0.3f), collider: false);
            }
        TambonBuilder.ShopZone(t, new Vector3(-6f, 0f, -3.5f), "city_food");
    }

    // 6. เกาะล้าน out in the bay (the sign by the pier on the beach)
    private static void KohLarn(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Beach", "เกาะล้าน", new Vector3(40f, 0f, 180f), 0f,
            "หาดตาแหวน หาดเทียน · เรือเฟอร์รี่จากแหลมบาลีฮาย", new Vector3(0f, 0f, -74f), new Vector3(0f, 0f, -72f), new Vector2(10f, 3f), 4);
        ProvinceKit.Island(t, Vector3.zero, new Vector3(60f, 32f, 36f), 8);
        ProvinceKit.Island(t, new Vector3(-75f, 0f, 10f), new Vector3(22f, 14f, 18f), 9); // เกาะสาก
        PrimitiveKit.Box("Pier_ท่าเรือ", t, new Vector3(0f, 0.35f, -69f), new Vector3(3f, 0.2f, 8f), ProvinceKit.Wood);
    }

    // 7. ตลาดน้ำ 4 ภาค: the lake, wooden houses on stilts round it, boats with goods on the water
    private static void FloatingMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดน้ำ 4 ภาค", new Vector3(66f, 0f, 16f), 0f,
            "ตลาดน้ำ ของกิน ของฝาก 4 ภาค · เรือพายขายของ", new Vector3(12f, 0f, -13f), new Vector3(0f, 0f, -11f), new Vector2(20f, 2f), 8);
        ProvinceKit.Pond(t, new Vector3(0f, 0f, 2f), new Vector2(34f, 16f));
        PrimitiveKit.Box("Boardwalk", t, new Vector3(0f, 0.3f, -7.6f), new Vector3(36f, 0.2f, 2.4f), ProvinceKit.Wood);
        for (int i = 0; i < 4; i++)
            ProvinceKit.StiltHouse(t, new Vector3(-15f + i * 10f, 0f, 14f), 180f, 6f, 5f);
        var hull = PrimitiveKit.Mat("PK_BoatHull", new Color(0.35f, 0.22f, 0.12f), 0.3f);
        Color[] fruit = { new Color(1f, 0.6f, 0.1f), new Color(0.9f, 0.2f, 0.2f), new Color(0.4f, 0.75f, 0.2f) };
        for (int i = 0; i < 6; i++)
        {
            var b = PrimitiveKit.Group("Boat_เรือพาย", t, new Vector3(-13f + i * 5.2f, 0f, -1f + (i % 2) * 5f), 90f + (i % 3) * 8f).transform;
            PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.25f, 3.6f), hull, collider: false);
            PrimitiveKit.Box("Goods", b, new Vector3(0f, 0.4f, 0.3f), new Vector3(0.7f, 0.2f, 1.2f), PrimitiveKit.Mat("PK_Fruit" + i % 3, fruit[i % 3], 0.3f), collider: false);
            PrimitiveKit.Cylinder("Hat_งอบ", b, new Vector3(0f, 0.9f, -0.9f), new Vector3(0.6f, 0.12f, 0.6f), PrimitiveKit.Mat("PK_Ngob", new Color(0.85f, 0.75f, 0.5f), 0.2f), collider: false);
        }
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -7.6f), "market");
    }

    // 8. สวนนงนุช: lawns, clipped topiary, flower beds, a lake and a Thai pavilion
    private static void NongNooch(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "สวนนงนุชพัทยา", new Vector3(-2f, 0f, -55f), 180f,
            "สวนพฤกษศาสตร์ระดับโลก · สวนฝรั่งเศส สวนไดโนเสาร์", new Vector3(9f, 0f, -26f), new Vector3(0f, 0f, -22f), new Vector2(16f, 4f), 9);
        PrimitiveKit.Box("Lawn", t, new Vector3(0f, 0.012f, 0f), new Vector3(44f, 0.02f, 56f), ProvinceKit.Grass, collider: false);
        ProvinceKit.Path(t, new Vector3(0f, 0f, -28f), new Vector3(0f, 0f, 20f), 3f);
        var clipped = PrimitiveKit.Mat("PK_Topiary", new Color(0.22f, 0.5f, 0.22f), 0.15f);
        for (int i = 0; i < 12; i++) // topiary cones and balls in two rows
        {
            float z = -22f + (i / 2) * 6f, x = i % 2 == 0 ? -4f : 4f;
            if (i % 4 < 2) PrimitiveKit.Sphere("Topiary", t, new Vector3(x, 1f, z), new Vector3(2f, 2f, 2f), clipped, collider: true);
            else PrimitiveKit.Cylinder("Topiary", t, new Vector3(x, 1.2f, z), new Vector3(1.4f, 1.2f, 1.4f), clipped);
        }
        for (int i = 0; i < 16; i++)
            Nature.Bush(t, new Vector3((i % 2 == 0 ? -1f : 1f) * (10f + (i % 4) * 2.5f), 0f, -20f + (i / 4) * 9f), 1.2f, i + 160, flowers: true);
        ProvinceKit.Pond(t, new Vector3(-12f, 0f, 16f), new Vector2(14f, 10f));
        ProvinceKit.SpirePavilion(t, new Vector3(10f, 0f, 18f), 180f, 5f, ProvinceKit.White, ProvinceKit.RoofRed);
    }

    // 9. วัดญาณสังวราราม: the ordination hall and the white chedi by a lotus pond
    private static void Yansangwararam(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดญาณสังวรารามวรมหาวิหาร", new Vector3(-27f, 0f, 31f), 0f,
            "วัดในพระบรมราชานุเคราะห์ · มณฑปพระพุทธบาท", new Vector3(6f, 0f, -10f), new Vector3(0f, 0f, -8.5f), new Vector2(12f, 2f), 6);
        ProvinceKit.ThaiHall(t, Vector3.zero, 0f, 8f, 9f, ProvinceKit.White, ProvinceKit.RoofRed, "พระอุโบสถ");
        ProvinceKit.Chedi(t, new Vector3(-12f, 0f, 3f), 8f);
        ProvinceKit.Pond(t, new Vector3(12f, 0f, 2f), new Vector2(8f, 8f));
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Cylinder("Lotus", t, new Vector3(10f + (i % 3) * 1.6f, 0.06f, 0.5f + (i / 3) * 2.2f), new Vector3(0.6f, 0.02f, 0.6f), PrimitiveKit.Mat("PK_LotusPink", new Color(0.95f, 0.55f, 0.7f), 0.3f), collider: false);
    }

    // 10. พิพิธภัณฑ์ภาพสามมิติ: a big bright box of a museum, painted walls
    private static void ArtMuseum(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "พิพิธภัณฑ์ภาพสามมิติ", new Vector3(45f, 0f, 78f), 0f,
            "ถ่ายรูปกับภาพวาดลวงตา 3 มิติ · ในร่มทั้งวัน", new Vector3(-17f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(18f, 2f), 7);
        PrimitiveKit.Box("Building", t, new Vector3(0f, 4.5f, 3f), new Vector3(28f, 9f, 10f), PrimitiveKit.Mat("PK_MuseumWhite", new Color(0.96f, 0.96f, 0.95f), 0.3f));
        Color[] paint = { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.6f, 0.95f), new Color(1f, 0.8f, 0.15f), new Color(0.4f, 0.8f, 0.35f), new Color(0.75f, 0.35f, 0.85f) };
        for (int i = 0; i < 5; i++) // the murals on the front
            PrimitiveKit.Box("Mural", t, new Vector3(-11.2f + i * 5.6f, 4.6f, -2.04f), new Vector3(4.8f, 5.6f, 0.05f), PrimitiveKit.Mat("PK_Mural" + i, paint[i], 0.4f), collider: false);
        PrimitiveKit.Box("Entrance", t, new Vector3(0f, 1.4f, -2.06f), new Vector3(3f, 2.8f, 0.05f), ProvinceKit.Dark, collider: false);
        PrimitiveKit.Label("ART IN 3D · ภาพสามมิติ", t, new Vector3(0f, 8f, -2.1f), 0.07f, new Color(0.15f, 0.15f, 0.2f));
        ProvinceKit.Path(t, new Vector3(0f, 0f, -2f), new Vector3(0f, 0f, -10f), 4f);
    }
}
