using UnityEditor;
using UnityEngine;

// ขอนแก่น: the heart of the north-east (ProvinceTown, inland) and its ten sights: บึงแก่นนคร with its
// path round the lake, the nine-storey พระมหาธาตุแก่นนคร (วัดหนองแวง), the dinosaurs of ภูเวียง, the
// ต้นตาล market, ศาลหลักเมือง, the silk of ชนบท, เขื่อนอุบลรัตน์ beyond the ring road, the national
// museum with its sema stones, พระธาตุขามแก่น and the all-night market.
public static class ProvinceKhonKaen
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "KhonKaen", Root = "KhonKaen", Name = "ขอนแก่น",
        Airport = "ท่าอากาศยานขอนแก่น", Station = "สถานีรถไฟขอนแก่น",
        MainStreet = "ถนนมิตรภาพ", Monument = "ขอนแก่น · เมืองหมอแคน",
        GantryWest = "↓ บึงแก่นนคร · พระมหาธาตุแก่นนคร\n← พิพิธภัณฑ์ · ตลาดโต้รุ่ง",
        GantryEast = "↑ ไดโนเสาร์ภูเวียง · ตลาดต้นตาล\nเขื่อนอุบลรัตน์ · ผ้าไหมชนบท ↑",
        ShopsWest = new[] { "ร้านส้มตำ ไก่ย่าง", "ร้านผ้าไหม", "ร้านหมอลำ แคน", "ร้านกาแฟ" },
        ShopsEast = new[] { "ร้านทอง", "ร้านไส้กรอกอีสาน", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = false, Accent = new Color(0.85f, 0.45f, 0.1f), Airlines = new[] { 1, 5 },
        CoachRoutes = new[] { "กรุงเทพฯ (หมอชิต 2)", "อุดรธานี", "เชียงใหม่" },
        SongthaewRoutes = new[] { "รถสองแถว สาย 8", "บึงแก่นนคร", "ม.ขอนแก่น", "ตลาดบางลำภู" },
    };

    [MenuItem("Thai Life/Provinces/Build ขอนแก่น")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        BuengKaenNakhon(root);
        NongWaeng(root);
        Dinosaurs(root);
        TonTann(root);
        CityPillar(root);
        Chonnabot(root);
        UbolRatana(root);
        Museum(root);
        KhamKaen(root);
        NightMarket(root);
    });

    // 1. บึงแก่นนคร: the lake, the path all the way round it, salas on the shore, swan boats
    private static void BuengKaenNakhon(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "บึงแก่นนคร", new Vector3(-58f, 0f, -56f), 0f,
            "บึงใหญ่กลางเมือง · ทางเดิน-ปั่นจักรยานรอบบึง", new Vector3(8f, 0f, 27f), new Vector3(0f, 0f, 26f), new Vector2(16f, 3f), 8);
        ProvinceKit.Pond(t, Vector3.zero, new Vector2(40f, 40f));
        foreach (float s in new[] { -1f, 1f })
        {
            ProvinceKit.Path(t, new Vector3(-24f, 0f, s * 24f), new Vector3(24f, 0f, s * 24f), 3f);
            ProvinceKit.Path(t, new Vector3(s * 24f, 0f, -24f), new Vector3(s * 24f, 0f, 24f), 3f);
        }
        for (int i = 0; i < 4; i++)
            ProvinceKit.SpirePavilion(t, Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, 26f), 45f + i * 90f + 180f, 3f, ProvinceKit.White, ProvinceKit.RoofRed);
        var swan = PrimitiveKit.Mat("PK_SwanBoat", new Color(0.97f, 0.97f, 0.97f), 0.4f);
        for (int i = 0; i < 5; i++)
        {
            var b = PrimitiveKit.Group("SwanBoat_เป็ดปั่น", t, new Vector3(-12f + i * 6f, 0f, -6f + (i % 2) * 9f), i * 50f).transform;
            PrimitiveKit.Box("Body", b, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.6f, 2f), swan, collider: false);
            PrimitiveKit.Cylinder("Neck", b, new Vector3(0f, 1.1f, 0.8f), new Vector3(0.22f, 0.5f, 0.22f), swan, collider: false);
            PrimitiveKit.Sphere("Head", b, new Vector3(0f, 1.65f, 0.95f), Vector3.one * 0.35f, swan);
        }
        for (int i = 0; i < 12; i++)
            Places.ShadeTree(t, new Vector3(i < 6 ? -27f : 27f, 0f, -22f + (i % 6) * 8.8f), 0.9f);
    }

    // 2. พระมหาธาตุแก่นนคร (วัดหนองแวง): the nine-storey white-and-gold stupa
    private static void NongWaeng(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "พระมหาธาตุแก่นนคร (วัดหนองแวง)", new Vector3(-2f, 0f, -48f), 0f,
            "พระธาตุ 9 ชั้น สูง 80 ม. · ชมวิวเมืองจากชั้นบนสุด", new Vector3(9f, 0f, 13f), new Vector3(0f, 0f, 12.5f), new Vector2(14f, 2f), 9);
        PrimitiveKit.Box("Court", t, new Vector3(0f, 0.012f, 0f), new Vector3(26f, 0.02f, 26f), ProvinceKit.Paving, collider: false);
        float y = 0f;
        for (int i = 0; i < 9; i++)
        {
            float w = 16f - i * 1.45f, h = 3f - i * 0.12f;
            PrimitiveKit.Box("Storey", t, new Vector3(0f, y + h * 0.5f, 0f), new Vector3(w, h, w), ProvinceKit.White, collider: i == 0);
            PrimitiveKit.Box("GoldBand", t, new Vector3(0f, y + h - 0.15f, 0f), new Vector3(w + 0.2f, 0.3f, w + 0.2f), ProvinceKit.Gold, collider: false);
            y += h;
        }
        var bud = MeshKit.Lathe("PK_ThatBud", new[] { new Vector2(0.5f, 0f), new Vector2(0.55f, 0.2f), new Vector2(0.35f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.01f, 1f) }, 20);
        MeshKit.Place("Spire", t, bud, ProvinceKit.Gold, new Vector3(0f, y, 0f), default, new Vector3(4f, 9f, 4f));
        PrimitiveKit.Box("Door", t, new Vector3(0f, 1.3f, -8.03f), new Vector3(2f, 2.4f, 0.06f), ProvinceKit.Gold, collider: false);
    }

    // 3. อุทยานไดโนเสาร์ภูเวียง: a life-size sauropod (ภูเวียงโกซอรัส), a small theropod, the fossil dig
    private static void Dinosaurs(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "อุทยานไดโนเสาร์ภูเวียง", new Vector3(66f, 0f, 16f), 0f,
            "ภูเวียงโกซอรัส สิรินธรเน · หลุมขุดค้นฟอสซิล", new Vector3(10f, 0f, -14f), new Vector3(0f, 0f, -12f), new Vector2(18f, 2f), 8);
        var skin = PrimitiveKit.Mat("PK_DinoGreen", new Color(0.38f, 0.5f, 0.32f), 0.25f);
        var skin2 = PrimitiveKit.Mat("PK_DinoBrown", new Color(0.55f, 0.4f, 0.28f), 0.25f);
        var d = PrimitiveKit.Group("Sauropod", t, new Vector3(-6f, 0f, 4f), 20f).transform;
        PrimitiveKit.Sphere("Body", d, new Vector3(0f, 4.2f, 0f), new Vector3(4.6f, 4f, 9f), skin, collider: true);
        foreach (var leg in new[] { new Vector3(-1.5f, 0f, 2.6f), new Vector3(1.5f, 0f, 2.6f), new Vector3(-1.5f, 0f, -2.6f), new Vector3(1.5f, 0f, -2.6f) })
            PrimitiveKit.Cylinder("Leg", d, leg + new Vector3(0f, 1.5f, 0f), new Vector3(1.1f, 1.5f, 1.1f), skin);
        PrimitiveKit.Cylinder("Neck", d, new Vector3(0f, 7.2f, 6.2f), new Vector3(1f, 3.6f, 1f), skin, new Vector3(40f, 0f, 0f), collider: false);
        PrimitiveKit.Sphere("Head", d, new Vector3(0f, 9.8f, 8.6f), new Vector3(1.2f, 1f, 1.8f), skin);
        PrimitiveKit.Cylinder("Tail", d, new Vector3(0f, 3.4f, -7.4f), new Vector3(0.9f, 3.6f, 0.9f), skin, new Vector3(-75f, 0f, 0f), collider: false);
        var r = PrimitiveKit.Group("Theropod", t, new Vector3(12f, 0f, 6f), -30f).transform;
        PrimitiveKit.Sphere("Body", r, new Vector3(0f, 2.4f, 0f), new Vector3(1.6f, 1.8f, 3.6f), skin2, collider: true);
        foreach (float x in new[] { -0.5f, 0.5f })
            PrimitiveKit.Cylinder("Leg", r, new Vector3(x, 0.8f, -0.3f), new Vector3(0.45f, 0.8f, 0.45f), skin2);
        PrimitiveKit.Sphere("Head", r, new Vector3(0f, 3.4f, 2f), new Vector3(1f, 1f, 1.6f), skin2);
        PrimitiveKit.Cylinder("Tail", r, new Vector3(0f, 2.4f, -2.8f), new Vector3(0.5f, 1.6f, 0.5f), skin2, new Vector3(-80f, 0f, 0f), collider: false);
        // the dig: a sandy pit, bones showing
        PrimitiveKit.Box("DigPit", t, new Vector3(4f, 0.02f, -6f), new Vector3(10f, 0.03f, 5f), ProvinceKit.Sand, collider: false);
        var bone = PrimitiveKit.Mat("PK_Bone", new Color(0.92f, 0.88f, 0.78f), 0.2f);
        for (int i = 0; i < 7; i++)
            PrimitiveKit.Cylinder("Bone", t, new Vector3(0.5f + i * 1.2f, 0.08f, -6f + (i % 3 - 1) * 1f), new Vector3(0.18f, 0.6f + (i % 2) * 0.4f, 0.18f), bone, new Vector3(90f, i * 31f, 0f), collider: false);
        PrimitiveKit.Box("Rope", t, new Vector3(4f, 0.5f, -8.6f), new Vector3(10f, 0.04f, 0.04f), ProvinceKit.Wood, collider: false);
    }

    // 4. ตลาดต้นตาล: the lifestyle market, stalls under the sugar palms and the lights
    private static void TonTann(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดต้นตาล", new Vector3(29f, 0f, 31f), 0f,
            "ตลาดนัดกลางคืน ของกินอีสาน · ดนตรีสด", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(30f, 3f), 10);
        ProvinceKit.Path(t, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f), 5f);
        Color[] tops = { new Color(0.95f, 0.55f, 0.15f), new Color(0.2f, 0.6f, 0.85f), new Color(0.9f, 0.3f, 0.35f), new Color(0.4f, 0.7f, 0.3f) };
        for (int i = 0; i < 7; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-18f + i * 6f, 0f, s * 4f), s > 0 ? 180f : 0f, tops[(i + (s > 0 ? 1 : 0)) % tops.Length], tops[(i + 3) % tops.Length]);
        foreach (float s in new[] { -1f, 1f })
        {
            ProvinceKit.FairyLights(t, new Vector3(-20f, 0f, s * 2.6f), new Vector3(20f, 0f, s * 2.6f), 3.4f);
            for (int i = 0; i < 4; i++)
                Nature.CoconutPalm(t, new Vector3(-18f + i * 12f, 0f, s * 8f), 0.85f, i + (s > 0 ? 10 : 0) + 170);
        }
        TambonBuilder.ShopZone(t, new Vector3(-6f, 0f, -4f), "city_food");
    }

    // 5. ศาลหลักเมืองขอนแก่น: the pillar shrine, its spire pavilion in a ring of flowers
    private static void CityPillar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "ศาลหลักเมืองขอนแก่น", new Vector3(-27f, 0f, 31f), 0f,
            "หลักเมืองคู่บ้านคู่เมือง · ไหว้ขอพร", new Vector3(7f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(8f, 2f), 5);
        ProvinceKit.SpirePavilion(t, Vector3.zero, 0f, 6f, ProvinceKit.White, ProvinceKit.RoofRed);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Nature.Bush(t, new Vector3(Mathf.Sin(a) * 8f, 0f, Mathf.Cos(a) * 7f), 0.7f, i + 180, flowers: true);
        }
        ProvinceKit.Path(t, new Vector3(0f, 0f, -4.5f), new Vector3(0f, 0f, -10f), 3f);
    }

    // 6. หมู่บ้านผ้าไหมชนบท: silk on the racks, looms under a house on stilts
    private static void Chonnabot(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ศูนย์ผ้าไหมชนบท", new Vector3(-50f, 0f, 78f), 0f,
            "ผ้าไหมมัดหมี่ขอนแก่น · ดูการทอผ้า", new Vector3(-14f, 0f, -8f), new Vector3(0f, 0f, -5f), new Vector2(16f, 2f), 6);
        Color[] silk = { new Color(0.75f, 0.1f, 0.35f), new Color(0.15f, 0.35f, 0.75f), new Color(0.95f, 0.65f, 0.1f), new Color(0.45f, 0.15f, 0.55f), new Color(0.1f, 0.55f, 0.45f) };
        for (int i = 0; i < 5; i++)
        {
            var x = -10f + i * 5f;
            PrimitiveKit.Box("SilkRail", t, new Vector3(x, 2.1f, 2f), new Vector3(3.2f, 0.08f, 0.08f), ProvinceKit.Wood, collider: false);
            foreach (float s in new[] { -1.5f, 1.5f })
                PrimitiveKit.Cylinder("SilkPost", t, new Vector3(x + s, 1.05f, 2f), new Vector3(0.1f, 1.05f, 0.1f), ProvinceKit.Wood);
            for (int k = 0; k < 3; k++)
                PrimitiveKit.Box("Silk", t, new Vector3(x - 1f + k, 1.3f, 2f), new Vector3(0.9f, 1.6f, 0.03f), PrimitiveKit.Mat("PK_Silk" + ((i + k) % silk.Length), silk[(i + k) % silk.Length], 0.75f), collider: false);
        }
        for (int i = 0; i < 3; i++)
            ProvinceKit.Stall(t, new Vector3(-8f + i * 8f, 0f, -4f), 0f, new Color(0.9f, 0.9f, 0.85f), silk[i]);
        ProvinceKit.StiltHouse(t, new Vector3(20f, 0f, 3f), 0f, 7f, 6f);
        for (int i = 0; i < 2; i++)
        {
            var loom = PrimitiveKit.Group("Loom_กี่ทอผ้า", t, new Vector3(18.5f + i * 3f, 0f, 3f)).transform;
            PrimitiveKit.Box("Frame", loom, new Vector3(0f, 0.9f, 0f), new Vector3(1.6f, 1.8f, 2f), ProvinceKit.Wood);
            PrimitiveKit.Box("Cloth", loom, new Vector3(0f, 0.95f, -0.2f), new Vector3(1.3f, 0.03f, 1.4f), PrimitiveKit.Mat("PK_Silk" + i, silk[i], 0.75f), collider: false);
        }
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -4f), "market");
    }

    // 7. เขื่อนอุบลรัตน์: the dam wall beyond the ring road, the reservoir behind it, a viewpoint
    private static void UbolRatana(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "เขื่อนอุบลรัตน์", new Vector3(0f, 0f, 104f), 0f,
            "เขื่อนแห่งแรกของภาคอีสาน · ชมพระอาทิตย์ตก", new Vector3(-14f, 0f, -4f), new Vector3(0f, 0f, -3f), new Vector2(20f, 2f), 6);
        PrimitiveKit.Box("Crest", t, new Vector3(0f, 0.5f, 3f), new Vector3(80f, 1f, 6f), ProvinceKit.Concrete);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Parapet", t, new Vector3(0f, 1.5f, 3f + s * 2.8f), new Vector3(80f, 1f, 0.3f), ProvinceKit.Concrete);
        PrimitiveKit.Box("Reservoir", t, new Vector3(0f, 0.03f, 66f), new Vector3(300f, 0.02f, 108f), ProvinceKit.Water, collider: false);
        ProvinceKit.Blocker(t, new Vector3(0f, 1f, 7f), new Vector3(90f, 2f, 1f));
        for (int i = 0; i < 6; i++)
            StreetFurniture.LampPost(t, new Vector3(-35f + i * 14f, 1f, 0.5f));
        PrimitiveKit.Label("เขื่อนอุบลรัตน์", t, new Vector3(0f, 1.5f, 0.15f), 0.06f, new Color(0.15f, 0.25f, 0.45f));
        ProvinceKit.Mountain(root, new Vector3(-30f, -14f, 240f), new Vector3(200f, 60f, 70f), 12, "ภูพานคำ");
    }

    // 8. พิพิธภัณฑสถานแห่งชาติขอนแก่น: the Isan-roofed hall, the Dvaravati sema stones on the lawn
    private static void Museum(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "พิพิธภัณฑสถานแห่งชาติขอนแก่น", new Vector3(-70f, 0f, 22f), -90f,
            "ใบเสมาสมัยทวารวดี · ประวัติศาสตร์อีสาน", new Vector3(7f, 0f, -11f), new Vector3(0f, 0f, -10f), new Vector2(12f, 2f), 6);
        ProvinceKit.ThaiHall(t, new Vector3(0f, 0f, 2f), 0f, 14f, 8f, PrimitiveKit.Mat("PK_MuseumWall", new Color(0.93f, 0.9f, 0.82f), 0.3f), ProvinceKit.RoofGreen, "พิพิธภัณฑสถานแห่งชาติ");
        var leaf = MeshKit.Leaf("PK_Sema", 0.9f, 0.5f, 0.05f, 0f, 6, 0.5f);
        for (int i = 0; i < 8; i++)
            MeshKit.Place("Sema_ใบเสมา", t, leaf, ProvinceKit.Sandstone, new Vector3(-14f + i * 4f, 0f, -11f), new Vector3(-90f, 0f, 0f), Vector3.one * 1.8f);
    }

    // 9. พระธาตุขามแก่น: the Lao-style that, its lotus-bud spire on a square base, white and gold
    private static void KhamKaen(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "พระธาตุขามแก่น", new Vector3(45f, 0f, 78f), 0f,
            "ปูชนียสถานคู่เมืองขอนแก่น · ที่มาของชื่อเมือง", new Vector3(-12f, 0f, -8f), new Vector3(0f, 0f, -7.5f), new Vector2(12f, 2f), 6);
        PrimitiveKit.Box("Base1", t, new Vector3(0f, 0.6f, 0f), new Vector3(10f, 1.2f, 10f), ProvinceKit.White);
        PrimitiveKit.Box("Base2", t, new Vector3(0f, 1.8f, 0f), new Vector3(7f, 1.2f, 7f), ProvinceKit.White);
        PrimitiveKit.Box("Body", t, new Vector3(0f, 4f, 0f), new Vector3(4.6f, 3.2f, 4.6f), ProvinceKit.White);
        var bud = MeshKit.Lathe("PK_ThatBud", new[] { new Vector2(0.5f, 0f), new Vector2(0.55f, 0.2f), new Vector2(0.35f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.01f, 1f) }, 20);
        MeshKit.Place("Spire", t, bud, ProvinceKit.Gold, new Vector3(0f, 5.6f, 0f), default, new Vector3(4.4f, 9f, 4.4f));
        ProvinceKit.ThaiHall(t, new Vector3(16f, 0f, 1f), 0f, 6f, 7f, ProvinceKit.White, ProvinceKit.RoofRed);
        ProvinceKit.ThaiHall(t, new Vector3(-16f, 0f, 1f), 0f, 6f, 7f, ProvinceKit.White, ProvinceKit.RoofRed);
    }

    // 10. ตลาดโต้รุ่งขอนแก่น: food stalls under strings of lights, tables down the middle
    private static void NightMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดโต้รุ่งขอนแก่น", new Vector3(-2f, 0f, -76f), 180f,
            "อาหารอีสาน ไก่ย่าง ส้มตำ · เปิดถึงดึก", new Vector3(12f, 0f, -7f), new Vector3(0f, 0f, 0f), new Vector2(26f, 3f), 10);
        PrimitiveKit.Box("Ground", t, new Vector3(0f, 0.012f, 0f), new Vector3(36f, 0.02f, 14f), ProvinceKit.Paving, collider: false);
        Color[] tops = { new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 0.9f) };
        for (int i = 0; i < 6; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-15f + i * 6f, 0f, s * 5f), s > 0 ? 180f : 0f, tops[(i + (s > 0 ? 1 : 0)) % 3], new Color(0.8f, 0.5f, 0.25f));
        var table = PrimitiveKit.Mat("PK_TableSteel", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.4f);
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Box("Table", t, new Vector3(-13f + i * 5.2f, 0.75f, 0f), new Vector3(2.4f, 0.06f, 1f), table);
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.FairyLights(t, new Vector3(-18f, 0f, s * 3f), new Vector3(18f, 0f, s * 3f), 3.2f);
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -4f), "city_food");
    }
}
