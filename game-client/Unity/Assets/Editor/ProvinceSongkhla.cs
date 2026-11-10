using UnityEditor;
using UnityEngine;

// สงขลา · หาดใหญ่: the seaside town (ProvinceTown) and its ten sights: แหลมสมิหลา and the golden
// mermaid, เกาะหนู เกาะแมว out in the bay, the old town's Sino-Portuguese shophouses (ถนนนครใน), the
// cable-stayed สะพานติณสูลานนท์ over ทะเลสาบสงขลา, เขาตังกวน with its chedi, พระพุทธมงคลมหาราช on the
// hill in the Hat Yai park, ตลาดกิมหยง, the old city fort, น้ำตกโตนงาช้าง and the Chinese-style
// ศาลหลักเมือง.
public static class ProvinceSongkhla
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "Songkhla", Root = "Songkhla", Name = "สงขลา",
        Airport = "ท่าอากาศยานหาดใหญ่", Station = "สถานีรถไฟหาดใหญ่",
        MainStreet = "ถนนนิพัทธ์อุทิศ", Monument = "สงขลา · หาดใหญ่",
        GantryWest = "↓ สะพานติณสูลานนท์ · พระพุทธมงคลฯ\n← น้ำตกโตนงาช้าง · ย่านเมืองเก่า",
        GantryEast = "↑ ตลาดกิมหยง · เขาตังกวน\nแหลมสมิหลา ↑",
        ShopsWest = new[] { "ไก่ทอดหาดใหญ่", "ร้านติ่มซำ", "ร้านของฝาก", "ร้านโกปี๊" },
        ShopsEast = new[] { "ร้านทอง", "ร้านแลกเงิน", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = true, Accent = new Color(0.1f, 0.5f, 0.7f), Airlines = new[] { 1, 3 },
    };

    [MenuItem("Thai Life/Provinces/Build สงขลา")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        Samila(root);
        RatAndCat(root);
        OldTown(root);
        TinsulanondaBridge(root);
        KhaoTangKuan(root);
        HatYaiPark(root);
        KimYong(root);
        OldFort(root);
        TonNgaChang(root);
        CityPillar(root);
    });

    // 1. แหลมสมิหลา: the beach, casuarinas and the golden mermaid on her rock at the water's edge
    private static void Samila(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Beach", "แหลมสมิหลา · นางเงือกทอง", new Vector3(-20f, 0f, 104f), 0f,
            "สัญลักษณ์เมืองสงขลา · ชมพระอาทิตย์ตก", new Vector3(5f, 0f, 3f), new Vector3(0f, 0f, 4f), new Vector2(16f, 4f), 9);
        ProvinceKit.Mermaid(t, new Vector3(0f, 0f, 9.5f), 160f, 1.7f);
        for (int i = 0; i < 6; i++) // casuarinas (สนทะเล) along the back of the beach
            Places.ShadeTree(t, new Vector3(-26f + i * 9f, 0f, -3f), 0.8f);
        Color[] canopy = { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.2f) };
        for (int i = 0; i < 3; i++)
            ProvinceKit.Stall(t, new Vector3(12f + i * 5f, 0f, 1f), 0f, canopy[i], new Color(0.95f, 0.85f, 0.6f));
    }

    // 2. เกาะหนู เกาะแมว out in the bay (the sign is on the beach)
    private static void RatAndCat(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "เกาะหนู เกาะแมว", new Vector3(30f, 0f, 175f), 0f,
            "ตำนานเมืองสงขลา · มองเห็นจากหาดสมิหลา", new Vector3(0f, 0f, -68f), new Vector3(0f, 0f, -66f), new Vector2(10f, 3f), 3);
        ProvinceKit.Island(t, Vector3.zero, new Vector3(48f, 34f, 30f), 4);
        ProvinceKit.Island(t, new Vector3(-58f, 0f, -18f), new Vector3(26f, 18f, 20f), 5);
    }

    // 3. ย่านเมืองเก่าสงขลา: a long row of Sino-Portuguese shophouses past the railway
    private static void OldTown(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "ย่านเมืองเก่าสงขลา", new Vector3(-50f, 0f, 76f), 0f,
            "ถนนนครใน นครนอก · ตึกชิโนโปรตุกีส · สตรีทอาร์ต", new Vector3(-30f, 0f, -5f), new Vector3(0f, 0f, -4f), new Vector2(40f, 3f), 9);
        ProvinceKit.SinoRow(t, new Vector3(0f, 0f, 2f), 0f, 12, new[] { "ร้านโกปี๊", "ขนมบ้าน", "หอศิลป์", "บ้านนครใน", "ข้าวมันไก่", "ร้านเต้าส้อ", "", "โรงแรมเก่า", "ร้านหนังสือ", "ร้านชา", "ขนมจีบ", "ร้านผ้า" });
        ProvinceKit.Path(t, new Vector3(-28f, 0f, -3.5f), new Vector3(28f, 0f, -3.5f), 4f);
        Color[] art = { new Color(0.95f, 0.45f, 0.2f), new Color(0.2f, 0.6f, 0.85f), new Color(0.85f, 0.25f, 0.55f) };
        for (int i = 0; i < 3; i++) // street art on the end walls
            PrimitiveKit.Box("StreetArt", t, new Vector3(-26.5f - 0.05f, 3f + i * 0.2f, 3.5f + i * 2f), new Vector3(0.05f, 2f, 1.6f), PrimitiveKit.Mat("PK_Art" + i, art[i], 0.3f), collider: false);
        for (float x = -24f; x <= 24f; x += 12f)
            StreetFurniture.LampPost(t, new Vector3(x, 0f, -6f));
    }

    // 4. สะพานติณสูลานนท์ over ทะเลสาบสงขลา: walk up the ramp and across
    private static void TinsulanondaBridge(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "สะพานติณสูลานนท์", new Vector3(-58f, 0f, -58f), 0f,
            "สะพานคอนกรีตข้ามทะเลสาบสงขลา ยาวที่สุดในไทย", new Vector3(6f, 0f, 27f), new Vector3(0f, 0f, 26f), new Vector2(10f, 3f), 5);
        ProvinceKit.Pond(t, new Vector3(0f, 0f, -2f), new Vector2(56f, 28f));
        PrimitiveKit.Label("ทะเลสาบสงขลา", t, new Vector3(-16f, 0.1f, -2f), 0.2f, new Color(1f, 1f, 1f, 0.8f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ProvinceKit.CableBridge(t, Vector3.zero, 0f, 50f, 4f);
        var hull = PrimitiveKit.Mat("PK_BoatHull", new Color(0.2f, 0.45f, 0.7f), 0.4f);
        foreach (var (x, z, yaw) in new[] { (-16f, -6f, 30f), (14f, 4f, -60f), (20f, -9f, 10f) }) // fishing boats on the lake
        {
            var b = PrimitiveKit.Group("FishingBoat_เรือประมง", t, new Vector3(x, 0.05f, z), yaw).transform;
            PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.5f, 5f), hull, collider: false);
            PrimitiveKit.Box("Cabin", b, new Vector3(0f, 0.8f, -1f), new Vector3(1f, 0.7f, 1.2f), ProvinceKit.Wood, collider: false);
            b.gameObject.AddComponent<Bob>();
        }
    }

    // 5. เขาตังกวน: the stairs up to the white chedi on top, the hill lift's rails up the side
    private static void KhaoTangKuan(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "เขาตังกวน", new Vector3(62f, 0f, 19f), 0f,
            "เจดีย์บนยอดเขา · ลิฟต์ขึ้นเขา · ชมวิวเมืองสงขลา", new Vector3(6f, 0f, -18.5f), new Vector3(0f, 0f, -20f), new Vector2(10f, 2.5f), 6);
        ProvinceKit.Hill(t, Vector3.zero, 0f, 17f, 10f, 5.5f, out var summit);
        ProvinceKit.Chedi(t, summit + new Vector3(0f, 0f, 1f), 9f);
        // the inclined lift on the east face
        float len = Mathf.Sqrt(100f + 11.5f * 11.5f), pitch = Mathf.Atan2(10f, 11.5f) * Mathf.Rad2Deg;
        var lift = PrimitiveKit.Group("HillLift_ลิฟต์ขึ้นเขา", t, new Vector3(11.25f, 5.2f, 0f), -90f).transform;
        foreach (float x in new[] { -0.7f, 0.7f })
            PrimitiveKit.Box("LiftRail", lift, new Vector3(x, 0f, 0f), new Vector3(0.15f, 0.2f, len), ProvinceKit.Steel, new Vector3(-pitch, 0f, 0f), collider: false);
        PrimitiveKit.Box("LiftCar", lift, new Vector3(0f, 0.9f, -1.5f), new Vector3(1.8f, 1.6f, 2.4f), PrimitiveKit.Mat("PK_LiftRed", new Color(0.85f, 0.2f, 0.15f), 0.6f), new Vector3(-pitch, 0f, 0f), collider: false);
    }

    // 6. พระพุทธมงคลมหาราช on the hill in สวนสาธารณะเทศบาลนครหาดใหญ่, facing the town
    private static void HatYaiPark(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "พระพุทธมงคลมหาราช", new Vector3(-2f, 0f, -58f), 0f,
            "สวนสาธารณะหาดใหญ่ · เขาคอหงส์ · พระพุทธรูปสูง 19.9 ม.", new Vector3(10f, 0f, 26f), new Vector3(0f, 0f, 26f), new Vector2(14f, 3f), 8);
        PrimitiveKit.Box("ParkLawn", t, new Vector3(0f, 0.012f, 0f), new Vector3(46f, 0.02f, 60f), ProvinceKit.Grass, collider: false);
        ProvinceKit.Hill(t, new Vector3(0f, 0f, -4f), 180f, 19f, 6f, 8f, out _);
        ProvinceKit.StandingBuddha(t, new Vector3(0f, 6f, -6f), 180f, 15f);
        ProvinceKit.Pond(t, new Vector3(-16f, 0f, 20f), new Vector2(10f, 6f));
        ProvinceKit.Pond(t, new Vector3(16f, 0f, 20f), new Vector2(10f, 6f));
        for (int i = 0; i < 8; i++)
            Places.ShadeTree(t, new Vector3(i % 2 == 0 ? -20f : 20f, 0f, -24f + i * 5.5f), 0.85f);
    }

    // 7. ตลาดกิมหยง: dried goods, cashews, sweets from over the border
    private static void KimYong(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดกิมหยง", new Vector3(29f, 0f, 31f), 0f,
            "ของฝากหาดใหญ่ ผลไม้แห้ง เม็ดมะม่วงหิมพานต์", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(18f, 3f), 8);
        Markets.FreshMarket(t, new Vector3(0f, 0f, 3f), "ตลาดกิมหยง", "08:00-18:00");
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, 3f), "market");
        Color[] goods = { new Color(0.75f, 0.5f, 0.25f), new Color(0.9f, 0.75f, 0.35f), new Color(0.55f, 0.3f, 0.15f), new Color(0.95f, 0.6f, 0.65f) };
        for (int i = 0; i < 6; i++)
            ProvinceKit.Stall(t, new Vector3(-15f + i * 6f, 0f, -5f), 0f, i % 2 == 0 ? new Color(0.85f, 0.15f, 0.1f) : new Color(0.95f, 0.8f, 0.2f), goods[i % goods.Length]);
    }

    // 8. ป้อมเมืองสงขลา: the old fort with its cannons
    private static void OldFort(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "ป้อมเมืองสงขลา", new Vector3(-30f, 0f, 31f), 0f,
            "ป้อมปืนและกำแพงเมืองเก่า สมัยรัชกาลที่ 3", new Vector3(12f, 0f, -9f), new Vector3(0f, 0f, -11f), new Vector2(8f, 2f), 4);
        ProvinceKit.Fort(t, Vector3.zero, 0f, 14f);
    }

    // 9. น้ำตกโตนงาช้าง (facing the avenue)
    private static void TonNgaChang(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "น้ำตกโตนงาช้าง", new Vector3(-70f, 0f, 24f), 0f,
            "น้ำตกเจ็ดชั้น · อุทยานแห่งชาติน้ำตกโตนงาช้าง", new Vector3(9f, 0f, -14f), new Vector3(0f, 0f, -12f), new Vector2(10f, 4f), 5);
        ProvinceKit.Waterfall(t, Vector3.zero, 0f, 14f, 5);
        ProvinceKit.Mountain(root, new Vector3(-215f, -10f, 20f), new Vector3(170f, 90f, 200f), 3, "เทือกเขาบรรทัด");
    }

    // 10. ศาลหลักเมืองสงขลา: Chinese style, past the railway
    private static void CityPillar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "ศาลหลักเมืองสงขลา", new Vector3(45f, 0f, 78f), 0f,
            "ศาลเจ้าพ่อหลักเมือง ศิลปะจีน", new Vector3(-12f, 0f, -6f), new Vector3(0f, 0f, -8f), new Vector2(12f, 2f), 5);
        ProvinceKit.ChineseShrine(t, Vector3.zero, 0f, 10f, 7f, "ศาลหลักเมือง");
        var stone = ProvinceKit.Granite;
        foreach (float x in new[] { -4f, 4f }) // guardian lions at the steps
        {
            PrimitiveKit.Box("LionPlinth", t, new Vector3(x, 0.4f, -6.5f), new Vector3(1f, 0.8f, 1f), stone);
            PrimitiveKit.Sphere("Lion", t, new Vector3(x, 1.2f, -6.5f), new Vector3(0.7f, 0.9f, 0.8f), stone);
            PrimitiveKit.Sphere("LionHead", t, new Vector3(x, 1.85f, -6.7f), Vector3.one * 0.55f, stone);
        }
    }
}
