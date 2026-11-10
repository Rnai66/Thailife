using UnityEditor;
using UnityEngine;

// เชียงใหม่: the old Lanna capital — the inland town (ProvinceTown) and its ten sights: วัดพระธาตุ
// ดอยสุเทพ up its naga stairs, ประตูท่าแพ and the moat, the great ruined chedi of วัดเจดีย์หลวง,
// วัดพระสิงห์, the Sunday walking street with its paper lanterns, the night bazaar, ดอยอินทนนท์ (the
// twin royal pagodas, the mountains beyond), น้ำตกแม่กลาง, the umbrella village of บ่อสร้าง and the
// tunnels of วัดอุโมงค์.
public static class ProvinceChiangMai
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "ChiangMai", Root = "ChiangMai", Name = "เชียงใหม่",
        Airport = "ท่าอากาศยานเชียงใหม่", Station = "สถานีรถไฟเชียงใหม่",
        MainStreet = "ถนนท่าแพ", Monument = "เชียงใหม่ · นครพิงค์",
        GantryWest = "↓ ดอยสุเทพ · วัดเจดีย์หลวง · วัดอุโมงค์\n← ประตูท่าแพ · คูเมือง",
        GantryEast = "↑ ไนท์บาซาร์ · น้ำตกแม่กลาง\nบ่อสร้าง · ดอยอินทนนท์ ↑",
        ShopsWest = new[] { "ร้านข้าวซอย", "ร้านไส้อั่ว แคบหมู", "ร้านผ้าฝ้าย", "ร้านกาแฟดอย" },
        ShopsEast = new[] { "ร้านทอง", "ร้านเครื่องเงิน", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = false, Accent = new Color(0.45f, 0.2f, 0.55f), Airlines = new[] { 0, 2 },
        CoachRoutes = new[] { "กรุงเทพฯ (หมอชิต 2)", "เชียงราย", "ขอนแก่น" },
        SongthaewRoutes = new[] { "รถแดง · รอบเมือง", "ดอยสุเทพ", "บ่อสร้าง", "ตลาดวโรรส" },
    };

    [MenuItem("Thai Life/Provinces/Build เชียงใหม่")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        DoiSuthep(root);
        ThaPhaeGate(root);
        ChediLuang(root);
        PhraSingh(root);
        SundayMarket(root);
        NightBazaar(root);
        Inthanon(root);
        MaeKlang(root);
        BoSang(root);
        Umong(root);
    });

    // 1. วัดพระธาตุดอยสุเทพ: up the naga stairs to the golden chedi on the summit (stairs to the avenue)
    private static void DoiSuthep(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดพระธาตุดอยสุเทพ", new Vector3(-58f, 0f, -58f), 180f,
            "บันไดนาค 306 ขั้น · พระธาตุทองคำคู่เมืองเชียงใหม่", new Vector3(6f, 0f, -24f), new Vector3(0f, 0f, -25f), new Vector2(10f, 3f), 8);
        ProvinceKit.Hill(t, Vector3.zero, 0f, 22f, 10f, 7.5f, out var summit);
        ProvinceKit.Chedi(t, summit + new Vector3(0f, 0f, 1.5f), 9f, ProvinceKit.Gold);
        foreach (float x in new[] { -5.2f, 5.2f }) // the gilded umbrellas (ฉัตร) at the corners
        {
            PrimitiveKit.Cylinder("ChatPole", t, summit + new Vector3(x, 1.6f, -2.5f), new Vector3(0.12f, 1.6f, 0.12f), ProvinceKit.Gold, collider: false);
            for (int k = 0; k < 3; k++)
                PrimitiveKit.Cylinder("Chat", t, summit + new Vector3(x, 2.2f + k * 0.5f, -2.5f), new Vector3(1.4f - k * 0.35f, 0.05f, 1.4f - k * 0.35f), ProvinceKit.Gold, collider: false);
        }
    }

    // 2. ประตูท่าแพ: the brick gate and wall facing the town, the moat behind
    private static void ThaPhaeGate(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "ประตูท่าแพ · คูเมือง", new Vector3(-70f, 0f, 22f), -90f,
            "ประตูเมืองเก่า 700 ปี · ลานท่าแพ", new Vector3(6f, 0f, -9f), new Vector3(0f, 0f, -7f), new Vector2(16f, 4f), 8);
        ProvinceKit.CityWall(t, Vector3.zero, 0f, 40f, ProvinceKit.Brick, "ประตูท่าแพ");
        ProvinceKit.Pond(t, new Vector3(-12f, 0f, 6f), new Vector2(16f, 4f));
        ProvinceKit.Pond(t, new Vector3(12f, 0f, 6f), new Vector2(16f, 4f));
        PrimitiveKit.Box("Plaza", t, new Vector3(0f, 0.014f, -6f), new Vector3(30f, 0.02f, 8f), ProvinceKit.Paving, collider: false);
        var grey = PrimitiveKit.Mat("PK_Pigeon", new Color(0.55f, 0.57f, 0.62f), 0.3f);
        var rng = new System.Random(11);
        for (int i = 0; i < 14; i++) // the pigeons on the square
            PrimitiveKit.Sphere("Pigeon", t, new Vector3(-8f + (float)rng.NextDouble() * 16f, 0.12f, -9f + (float)rng.NextDouble() * 5f), new Vector3(0.18f, 0.16f, 0.26f), grey);
    }

    // 3. วัดเจดีย์หลวง: the great ruined brick chedi on its stepped Lanna base, naga stairs up the front
    private static void ChediLuang(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดเจดีย์หลวง", new Vector3(-2f, 0f, -45f), 180f,
            "พระเจดีย์หลวงอิฐโบราณ · เคยประดิษฐานพระแก้วมรกต", new Vector3(9f, 0f, -12f), new Vector3(0f, 0f, -11f), new Vector2(12f, 3f), 8);
        PrimitiveKit.Box("Base1", t, new Vector3(0f, 1.5f, 0f), new Vector3(16f, 3f, 16f), ProvinceKit.Brick);
        PrimitiveKit.Box("Base2", t, new Vector3(0f, 4f, 0f), new Vector3(12f, 2f, 12f), ProvinceKit.Brick);
        PrimitiveKit.Box("Base3", t, new Vector3(0f, 5.8f, 0f), new Vector3(8.6f, 1.6f, 8.6f), ProvinceKit.Brick);
        ProvinceKit.Chedi(t, new Vector3(0f, 6.6f, 0f), 10f, ProvinceKit.Brick, goldTop: false);
        for (int i = 0; i < 5; i++) // the stairs up the front, a naga either side
            PrimitiveKit.Box("Stair", t, new Vector3(0f, (i + 1) * 0.3f, -8f - (4 - i) * 0.6f), new Vector3(3f, (i + 1) * 0.6f, 0.62f), ProvinceKit.Brick);
        foreach (float x in new[] { -2f, 2f })
            PrimitiveKit.Box("Naga", t, new Vector3(x, 1.8f, -9.4f), new Vector3(0.5f, 0.5f, 3.4f), ProvinceKit.Laterite, new Vector3(-35f, 0f, 0f), collider: false);
        for (int i = 0; i < 4; i++) // the elephants round the base (ช้างล้อม)
        {
            var q = Quaternion.Euler(0f, i * 90f + 45f, 0f) * new Vector3(0f, 0f, 12.2f);
            PrimitiveKit.Box("Elephant", t, q + new Vector3(0f, 1f, 0f), new Vector3(1.4f, 2f, 2.2f), ProvinceKit.Laterite, new Vector3(0f, i * 90f + 45f, 0f));
        }
        for (int i = 0; i < 6; i++)
            Places.ShadeTree(t, new Vector3(i < 3 ? -16f : 16f, 0f, -8f + (i % 3) * 8f), 1f);
    }

    // 4. วัดพระสิงห์: the Lanna-style wooden viharn, a golden chedi and a white one
    private static void PhraSingh(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดพระสิงห์วรมหาวิหาร", new Vector3(-27f, 0f, 31f), 0f,
            "พระพุทธสิหิงค์ · วิหารลายคำ ศิลปะล้านนา", new Vector3(6f, 0f, -10f), new Vector3(0f, 0f, -8.5f), new Vector2(12f, 2f), 7);
        ProvinceKit.ThaiHall(t, new Vector3(0f, 0f, 0f), 0f, 8f, 9f, ProvinceKit.Wood, ProvinceKit.RoofRed, "วิหารลายคำ");
        ProvinceKit.Chedi(t, new Vector3(-12f, 0f, 3f), 8f, ProvinceKit.Gold);
        ProvinceKit.Chedi(t, new Vector3(12f, 0f, 3f), 7f);
    }

    // 5. ถนนคนเดินวันอาทิตย์: stalls down both sides under fairy lights and paper lanterns (โคมล้านนา)
    private static void SundayMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ถนนคนเดินวันอาทิตย์ ท่าแพ", new Vector3(29f, 0f, 31f), 0f,
            "ของกินเหนือ งานฝีมือ ผ้าฝ้าย · ดนตรีพื้นเมือง", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(30f, 3f), 10);
        ProvinceKit.Path(t, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f), 5f);
        Color[] tops = { new Color(0.85f, 0.25f, 0.2f), new Color(0.95f, 0.75f, 0.2f), new Color(0.35f, 0.6f, 0.3f), new Color(0.45f, 0.25f, 0.6f) };
        for (int i = 0; i < 7; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-18f + i * 6f, 0f, s * 4f), s > 0 ? 180f : 0f, tops[(i + (s > 0 ? 1 : 0)) % tops.Length], tops[(i + 2) % tops.Length]);
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.FairyLights(t, new Vector3(-20f, 0f, s * 2.6f), new Vector3(20f, 0f, s * 2.6f), 3.4f);
        var lantern = PrimitiveKit.Glow("PK_LannaLantern", new Color(1f, 0.55f, 0.15f), 2f);
        for (int i = 0; i < 9; i++)
            PrimitiveKit.Sphere("Lantern_โคมล้านนา", t, new Vector3(-18f + i * 4.5f, 3.6f, 0f), new Vector3(0.6f, 0.75f, 0.6f), lantern);
        TambonBuilder.ShopZone(t, new Vector3(-6f, 0f, -4f), "city_food");
    }

    // 6. ไนท์บาซาร์: a long market hall on posts, rows of stalls under it
    private static void NightBazaar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "เชียงใหม่ไนท์บาซาร์", new Vector3(45f, 0f, 78f), 0f,
            "ตลาดกลางคืน ของที่ระลึก เครื่องเงิน งานไม้แกะสลัก", new Vector3(-17f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(26f, 6f), 10);
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.015f, 0f), new Vector3(34f, 0.03f, 14f), ProvinceKit.Paving, collider: false);
        for (float x = -16f; x <= 16.1f; x += 8f)
            foreach (float z in new[] { -6.5f, 6.5f })
                PrimitiveKit.Cylinder("Post", t, new Vector3(x, 2.6f, z), new Vector3(0.35f, 2.6f, 0.35f), ProvinceKit.Steel);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 5.3f, 0f), new Vector3(35f, 0.3f, 15f), ProvinceKit.RoofRed, collider: false);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 6.1f, -7.4f), new Vector3(14f, 1.2f, 0.15f), PrimitiveKit.Glow("PK_BazaarSign", new Color(0.8f, 0.1f, 0.1f), 1.4f, 0.8f), collider: false);
        PrimitiveKit.Label("CHIANG MAI NIGHT BAZAAR · ไนท์บาซาร์", t, new Vector3(0f, 6.1f, -7.5f), 0.06f, Color.white);
        Color[] goods = { new Color(0.8f, 0.8f, 0.85f), new Color(0.55f, 0.35f, 0.2f), new Color(0.9f, 0.4f, 0.5f), new Color(0.3f, 0.5f, 0.8f) };
        for (int i = 0; i < 6; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-14f + i * 5.6f, 0f, s * 4f), s > 0 ? 180f : 0f, new Color(0.95f, 0.92f, 0.85f), goods[(i + (s > 0 ? 1 : 0)) % goods.Length]);
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -3f), "market");
    }

    // 7. ดอยอินทนนท์: the highest mountain in Thailand off to the north-west; on the hill beyond the ring
    // road, the twin royal pagodas (พระมหาธาตุนภเมทนีดล · นภพลภูมิสิริ) and a viewpoint
    private static void Inthanon(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "ดอยอินทนนท์", new Vector3(0f, 0f, 106f), 0f,
            "หลังคาประเทศไทย 2,565 ม. · พระมหาธาตุเจดีย์คู่", new Vector3(-14f, 0f, -5f), new Vector3(-12f, 0f, -2f), new Vector2(8f, 3f), 6);
        ProvinceKit.Hill(t, new Vector3(0f, 0f, 2f), 0f, 9f, 4f, 7f, out var summit);
        var top = summit + new Vector3(0f, 0f, 2f);
        ProvinceKit.Chedi(t, top + new Vector3(-3f, 0f, 1f), 5f, PrimitiveKit.Mat("PK_PagodaPurple", new Color(0.6f, 0.5f, 0.68f), 0.4f));
        ProvinceKit.Chedi(t, top + new Vector3(3f, 0f, 1f), 5f, PrimitiveKit.Mat("PK_PagodaBrown", new Color(0.62f, 0.45f, 0.35f), 0.4f));
        for (int i = 0; i < 10; i++)
            Nature.Bush(t, new Vector3(-18f + i * 4f, 0f, -6f + (i % 2) * 1.5f), 1f, i + 140, flowers: true);
        PrimitiveKit.Box("Viewpoint", t, new Vector3(16f, 0.4f, 0f), new Vector3(8f, 0.2f, 6f), ProvinceKit.Wood);
        PrimitiveKit.Label("จุดสูงสุดแดนสยาม", t, new Vector3(16f, 1.6f, -3.1f), 0.04f, new Color(0.2f, 0.3f, 0.15f));
        ProvinceKit.Mountain(root, new Vector3(-200f, -10f, 160f), new Vector3(220f, 130f, 160f), 6, "ดอยอินทนนท์");
        ProvinceKit.Mountain(root, new Vector3(-215f, -10f, -40f), new Vector3(150f, 95f, 180f), 7, "ดอยสุเทพ-ปุย");
    }

    // 8. น้ำตกแม่กลาง (its pool toward the avenue)
    private static void MaeKlang(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "น้ำตกแม่กลาง", new Vector3(66f, 0f, 16f), 0f,
            "อุทยานแห่งชาติดอยอินทนนท์ · เล่นน้ำได้", new Vector3(9f, 0f, -9f), new Vector3(0f, 0f, -8f), new Vector2(12f, 3f), 6);
        ProvinceKit.Waterfall(t, Vector3.zero, 0f, 10f, 3);
    }

    // 9. หมู่บ้านทำร่มบ่อสร้าง: painted paper umbrellas open to dry and on show, the workshop
    private static void BoSang(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "หมู่บ้านทำร่มบ่อสร้าง", new Vector3(-50f, 0f, 78f), 0f,
            "ร่มกระดาษสาเขียนลาย · ศูนย์หัตถกรรม", new Vector3(-14f, 0f, -8f), new Vector3(0f, 0f, -5f), new Vector2(20f, 3f), 7);
        Color[] paper = { new Color(0.9f, 0.25f, 0.2f), new Color(0.95f, 0.75f, 0.2f), new Color(0.25f, 0.55f, 0.85f), new Color(0.35f, 0.7f, 0.4f), new Color(0.9f, 0.45f, 0.65f) };
        // the big umbrella at the entrance
        PrimitiveKit.Cylinder("BigPole", t, new Vector3(0f, 2.5f, 3f), new Vector3(0.2f, 2.5f, 0.2f), ProvinceKit.Wood);
        PrimitiveKit.Cylinder("BigUmbrella", t, new Vector3(0f, 5f, 3f), new Vector3(7f, 0.25f, 7f), PrimitiveKit.Mat("PK_Paper0", paper[0], 0.3f), collider: false);
        PrimitiveKit.Cylinder("BigTop", t, new Vector3(0f, 5.4f, 3f), new Vector3(2f, 0.2f, 2f), PrimitiveKit.Mat("PK_Paper1", paper[1], 0.3f), collider: false);
        for (int i = 0; i < 18; i++) // drying in rows on the ground, tilted
        {
            float x = -24f + (i % 9) * 6f, z = i < 9 ? -3f : 7f;
            if (Mathf.Abs(x) < 4f) continue;
            PrimitiveKit.Cylinder("Umbrella", t, new Vector3(x, 0.9f, z), new Vector3(1.8f, 0.06f, 1.8f), PrimitiveKit.Mat("PK_Paper" + i % paper.Length, paper[i % paper.Length], 0.3f), new Vector3(60f, i * 37f, 0f), collider: false);
        }
        for (int i = 0; i < 3; i++)
            ProvinceKit.Stall(t, new Vector3(-12f + i * 12f, 0f, -5.5f), 0f, paper[(i + 2) % paper.Length], paper[i % paper.Length]);
        ProvinceKit.StiltHouse(t, new Vector3(22f, 0f, 4f), 0f, 6f, 5f);
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -5.5f), "market");
    }

    // 10. วัดอุโมงค์: the brick tunnels into the mound, an old chedi on top, the forest round it
    private static void Umong(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดอุโมงค์", new Vector3(-2f, 0f, -73f), 180f,
            "อุโมงค์อิฐโบราณกลางป่า · วัดป่าในเมือง", new Vector3(9f, 0f, -10f), new Vector3(0f, 0f, -9f), new Vector2(10f, 2f), 5);
        PrimitiveKit.Box("Mound", t, new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 12f), ProvinceKit.Laterite);
        PrimitiveKit.Box("MoundGrass", t, new Vector3(0f, 4.05f, 0f), new Vector3(20.2f, 0.1f, 12.2f), ProvinceKit.Grass, collider: false);
        foreach (float x in new[] { -6f, 0f, 6f }) // the tunnel mouths
        {
            PrimitiveKit.Box("TunnelArch", t, new Vector3(x, 1.4f, -6.02f), new Vector3(2.4f, 2.8f, 0.06f), ProvinceKit.Brick, collider: false);
            PrimitiveKit.Box("TunnelDark", t, new Vector3(x, 1.2f, -6.06f), new Vector3(1.6f, 2.2f, 0.04f), ProvinceKit.Dark, collider: false);
        }
        ProvinceKit.Chedi(t, new Vector3(0f, 4.1f, 1f), 7f, ProvinceKit.Brick, goldTop: false);
        for (int i = 0; i < 10; i++)
            Places.ShadeTree(t, new Vector3(-20f + (i % 5) * 10f, 0f, i < 5 ? -10f : 10f), 1.1f);
    }
}
