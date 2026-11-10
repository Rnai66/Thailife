using UnityEditor;
using UnityEngine;

// บุรีรัมย์: เมืองปราสาทหิน · เมืองกีฬา — the inland town (ProvinceTown) and its ten sights: สนามช้าง
// อินเตอร์เนชั่นแนลเซอร์กิต with race cars lapping it, ช้าง อารีน่า (the football stadium), ปราสาทหิน
// พนมรุ้ง with its processional way and naga bridge, ปราสาทเมืองต่ำ's five brick prangs and ponds,
// เขากระโดง (the stairs up the old volcano to the golden Buddha), ศาลหลักเมือง (a Khmer prang),
// วัดเขาอังคาร, the sarus cranes at อ่างเก็บน้ำห้วยจระเข้มาก, the silk market of นาโพธิ์, and the
// ถนนคนเดินเซราะกราว night market.
public static class ProvinceBuriram
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "Buriram", Root = "Buriram", Name = "บุรีรัมย์",
        Airport = "ท่าอากาศยานบุรีรัมย์", Station = "สถานีรถไฟบุรีรัมย์",
        MainStreet = "ถนนจิระ", Monument = "บุรีรัมย์ · เมืองปราสาทหิน",
        GantryWest = "↓ สนามช้าง เซอร์กิต · ช้าง อารีน่า\n← วัดเขาอังคาร · ปราสาทเมืองต่ำ",
        GantryEast = "↑ เขากระโดง · ตลาดผ้าไหม\nปราสาทหินพนมรุ้ง ↑",
        ShopsWest = new[] { "ร้านผ้าไหม", "ส้มตำไก่ย่าง", "ร้านเสื้อบอล", "ร้านกาแฟ" },
        ShopsEast = new[] { "ร้านทอง", "ข้าวเปียก", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = false, Accent = new Color(0.12f, 0.3f, 0.75f), Airlines = new[] { 5, 0 },
    };

    [MenuItem("Thai Life/Provinces/Build บุรีรัมย์")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        ChangCircuit(root);
        ChangArena(root);
        PhanomRung(root);
        MuangTam(root);
        KhaoKradong(root);
        CityPillar(root);
        KhaoAngkhan(root);
        Cranes(root);
        SilkMarket(root);
        WalkingStreet(root);
    });

    // 1. สนามช้าง อินเตอร์เนชั่นแนลเซอร์กิต
    private static void ChangCircuit(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Sport", "สนามช้าง อินเตอร์เนชั่นแนลเซอร์กิต", new Vector3(-58f, 0f, -58f), 0f,
            "สนามแข่งรถมาตรฐานโลก FIA · MotoGP", new Vector3(-24f, 0f, 29f), new Vector3(-12f, 0f, 30f), new Vector2(10f, 3f), 6);
        var corners = new[]
        {
            new Vector3(-21f, 0f, 17f), new Vector3(21f, 0f, 17f), new Vector3(24f, 0f, -4f), new Vector3(10f, 0f, -9f),
            new Vector3(20f, 0f, -24f), new Vector3(-20f, 0f, -26f), new Vector3(-24f, 0f, -6f), new Vector3(-12f, 0f, 0f),
        };
        ProvinceKit.Circuit(t, Vector3.zero, 0f, ProvinceKit.Smooth(corners, 7f, 6), 7f, "CHANG INTERNATIONAL CIRCUIT");
    }

    // 2. ช้าง อารีน่า: the stadium of the province's football club, its pitch running north–south
    private static void ChangArena(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Sport", "ช้าง อารีน่า", new Vector3(-2f, 0f, -56f), 0f,
            "สนามฟุตบอลบุรีรัมย์ ยูไนเต็ด · ปราสาทสายฟ้า", new Vector3(16f, 0f, 29f), new Vector3(0f, 0f, 31f), new Vector2(14f, 3f), 8);
        ProvinceKit.Stadium(t, Vector3.zero, 90f, 38f, 24f, "CHANG ARENA", new Color(0.1f, 0.3f, 0.8f));
    }

    // 3. ปราสาทหินพนมรุ้ง: the processional way between lotus-bud posts, the naga bridge, the stairs up to
    // the terrace and the great sandstone prang
    private static void PhanomRung(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "ปราสาทหินพนมรุ้ง", new Vector3(45f, 0f, 78f), 0f,
            "ปราสาทหินบนภูเขาไฟ · สะพานนาคราช", new Vector3(-36f, 0f, -6f), new Vector3(-20f, 0f, 0f), new Vector2(14f, 3f), 8);
        var stone = ProvinceKit.Sandstone;
        ProvinceKit.Path(t, new Vector3(-38f, 0f, 0f), new Vector3(8f, 0f, 0f), 4f, ProvinceKit.Laterite);
        var post = MeshKit.Lathe("PK_LotusPost", new[] { new Vector2(0.2f, 0f), new Vector2(0.18f, 1f), new Vector2(0.28f, 1.15f), new Vector2(0.2f, 1.45f), new Vector2(0.02f, 1.6f) }, 10);
        for (float x = -36f; x <= 6f; x += 3f)
            foreach (float z in new[] { -2.6f, 2.6f })
                MeshKit.Place("LotusPost_เสานางเรียง", t, post, stone, new Vector3(x, 0f, z));
        // the cross-shaped naga bridge (สะพานนาคราช)
        PrimitiveKit.Box("NagaBridge", t, new Vector3(13f, 0.4f, 0f), new Vector3(10f, 0.8f, 5f), stone);
        PrimitiveKit.Box("NagaBridgeArms", t, new Vector3(13f, 0.4f, 0f), new Vector3(5f, 0.8f, 10f), stone);
        PrimitiveKit.Box("BridgeStep", t, new Vector3(7.6f, 0.2f, 0f), new Vector3(0.8f, 0.4f, 3f), stone);
        foreach (float z in new[] { -2.3f, 2.3f })
            PrimitiveKit.Box("NagaRail", t, new Vector3(13f, 1.1f, z), new Vector3(9.6f, 0.4f, 0.4f), stone, collider: false);
        foreach (float x in new[] { -2.3f, 2.3f })
            PrimitiveKit.Box("NagaRail", t, new Vector3(13f + x, 1.1f, 0f), new Vector3(0.4f, 0.4f, 9.6f), stone, collider: false);
        // stairs up to the terrace
        const float rise = 0.24f, top = 2.4f;
        int n = Mathf.RoundToInt(top / rise);
        for (int i = 0; i < n; i++)
            PrimitiveKit.Box("Stair", t, new Vector3(18.4f + i * 0.4f, (i + 1) * rise * 0.5f, 0f), new Vector3(0.42f, (i + 1) * rise, 5f), stone);
        PrimitiveKit.Box("Terrace", t, new Vector3(30f, top * 0.5f, 0f), new Vector3(15f, top, 16f), ProvinceKit.Laterite);
        ProvinceKit.Prang(t, new Vector3(31f, top, 0f), 15f, stone, 90f);
        PrimitiveKit.Box("Mandapa", t, new Vector3(25.6f, top + 2.3f, 0f), new Vector3(4f, 4.2f, 3.6f), stone);
        MeshKit.Place("MandapaRoof", t, MeshKit.Gable("PK_Gable", 1f, 1f, 1f), stone, new Vector3(23.4f, top + 4.4f, 0f), new Vector3(0f, 90f, 0f), new Vector3(3.8f, 1.6f, 4.4f));
    }

    // 4. ปราสาทเมืองต่ำ: a laterite wall with gateways, four ponds, five brick prangs on a platform
    private static void MuangTam(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "ปราสาทเมืองต่ำ", new Vector3(-50f, 0f, 78f), 0f,
            "ปราสาทอิฐห้าองค์ · สระน้ำรูปตัวแอล", new Vector3(-26f, 0f, -8f), new Vector3(0f, 0f, -8f), new Vector2(14f, 2f), 6);
        const float hw = 22f, hd = 9f;
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Wall", t, new Vector3(-hw * 0.5f - 1.5f, 1f, s * hd), new Vector3(hw - 3f, 2f, 0.8f), ProvinceKit.Laterite);
            PrimitiveKit.Box("Wall", t, new Vector3(hw * 0.5f + 1.5f, 1f, s * hd), new Vector3(hw - 3f, 2f, 0.8f), ProvinceKit.Laterite);
            PrimitiveKit.Box("Wall", t, new Vector3(s * hw, 1f, 0f), new Vector3(0.8f, 2f, hd * 2f), ProvinceKit.Laterite);
            PrimitiveKit.Box("Gopura", t, new Vector3(0f, 1.6f, s * hd), new Vector3(3f, 0.6f, 1.6f), ProvinceKit.Sandstone, collider: false);
            foreach (float x in new[] { -1.8f, 1.8f })
                PrimitiveKit.Box("GopuraPost", t, new Vector3(x, 1.3f, s * hd), new Vector3(0.6f, 2.6f, 1.6f), ProvinceKit.Sandstone);
        }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                ProvinceKit.Pond(t, new Vector3(sx * 15f, 0f, sz * 4.6f), new Vector2(9f, 4.6f));
        PrimitiveKit.Box("Platform", t, new Vector3(0f, 0.35f, 0f), new Vector3(16f, 0.7f, 11f), ProvinceKit.Laterite);
        foreach (var at in new[] { new Vector3(-5f, 0.7f, -2.4f), new Vector3(0f, 0.7f, -2.4f), new Vector3(5f, 0.7f, -2.4f), new Vector3(-2.6f, 0.7f, 2.8f), new Vector3(2.6f, 0.7f, 2.8f) })
            ProvinceKit.Prang(t, at, at.x == 0f ? 8f : 6.6f, ProvinceKit.Brick);
    }

    // 5. วนอุทยานเขากระโดง: the naga stairs up the old volcano to the golden Buddha
    private static void KhaoKradong(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "วนอุทยานเขากระโดง", new Vector3(62f, 0f, 19f), 0f,
            "ภูเขาไฟเก่า · บันไดนาค 297 ขั้น · พระสุภัทรบพิตร", new Vector3(6f, 0f, -18.5f), new Vector3(0f, 0f, -20f), new Vector2(10f, 2.5f), 6);
        ProvinceKit.Hill(t, Vector3.zero, 0f, 17f, 9f, 5.5f, out var summit);
        BuddhaStatue.Build(t, summit + new Vector3(0f, 0f, 1.5f), 2.6f, detail: true, arch: false);
    }

    // 6. ศาลหลักเมืองบุรีรัมย์: a sandstone prang
    private static void CityPillar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "ศาลหลักเมืองบุรีรัมย์", new Vector3(-36f, 0f, 31f), 0f,
            "ศาลหลักเมืองทรงปรางค์ขอม", new Vector3(6f, 0f, -7f), new Vector3(0f, 0f, -7f), new Vector2(8f, 3f), 4);
        ProvinceKit.Prang(t, Vector3.zero, 10f, ProvinceKit.Sandstone);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nature.Bush(t, new Vector3(Mathf.Sin(a) * 6f, 0f, Mathf.Cos(a) * 6f), 0.7f, i + 70, flowers: true);
        }
    }

    // 7. วัดเขาอังคาร: the ordination hall on top of a low old volcano, boundary stones round it
    private static void KhaoAngkhan(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดเขาอังคาร", new Vector3(-70f, 0f, 22f), 0f,
            "อุโบสถศิลปะผสมไทย-ขอม บนยอดภูเขาไฟ", new Vector3(9f, 0f, -14f), new Vector3(0f, 0f, -14f), new Vector2(10f, 3f), 4);
        ProvinceKit.Hill(t, new Vector3(0f, 0f, 4f), 0f, 14f, 3f, 9.5f, out var summit);
        var top = summit + new Vector3(0f, 0f, 4f);
        ProvinceKit.ThaiHall(t, top + new Vector3(0f, 0f, 1f), 0f, 6f, 7f, ProvinceKit.White, ProvinceKit.RoofRed, "อุโบสถวัดเขาอังคาร");
        var leaf = MeshKit.Leaf("PK_Sema", 0.9f, 0.5f, 0.05f, 0f, 6, 0.5f);
        for (int i = 0; i < 8; i++) // ใบเสมา round the hall
        {
            float a = i * 45f * Mathf.Deg2Rad;
            MeshKit.Place("Sema_ใบเสมา", t, leaf, ProvinceKit.Sandstone, top + new Vector3(Mathf.Sin(a) * 7.6f, 0f, 1f + Mathf.Cos(a) * 7f), new Vector3(-90f, i * 45f, 0f));
        }
        ProvinceKit.Prang(t, top + new Vector3(6f, 0f, 6f), 4.5f, ProvinceKit.Laterite);
    }

    // 8. นกกระเรียนพันธุ์ไทย at the reservoir past the ring road
    private static void Cranes(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "ทุ่งนกกระเรียน ห้วยจระเข้มาก", new Vector3(0f, 0f, 106f), 0f,
            "นกกระเรียนพันธุ์ไทย คืนถิ่นบุรีรัมย์", new Vector3(-30f, 0f, -6f), new Vector3(-10f, 0f, -5f), new Vector2(16f, 3f), 5);
        ProvinceKit.Pond(t, new Vector3(0f, 0f, 5f), new Vector2(76f, 9f));
        var reed = PrimitiveKit.Mat("PK_Reed", new Color(0.45f, 0.6f, 0.25f), 0.1f);
        var rng = new System.Random(5);
        for (int i = 0; i < 60; i++)
        {
            float x = -38f + (float)rng.NextDouble() * 76f, z = (float)rng.NextDouble() < 0.5 ? 0.3f : 9.6f;
            PrimitiveKit.Box("Reed", t, new Vector3(x, 0.6f, z + (float)rng.NextDouble() * 0.6f), new Vector3(0.06f, 1.2f, 0.06f), reed, new Vector3(0f, 0f, ((float)rng.NextDouble() - 0.5f) * 20f), collider: false);
        }
        for (int i = 0; i < 9; i++)
            ProvinceKit.Crane(t, new Vector3(-30f + i * 7.3f, 0f, i % 3 == 0 ? -1.4f : 3f + (i % 2) * 3f), (i * 73f) % 360f, i % 2 == 1);
        PrimitiveKit.Box("Hide", t, new Vector3(30f, 1.2f, -3f), new Vector3(4f, 2.4f, 2.4f), ProvinceKit.Wood);
        PrimitiveKit.Label("จุดชมนก", t, new Vector3(30f, 1.8f, -4.25f), 0.04f, Color.white);
    }

    // 9. ตลาดผ้าไหมนาโพธิ์: silk on the racks, a loom at work
    private static void SilkMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดผ้าไหมนาโพธิ์", new Vector3(-19f, 0f, 31f), 0f,
            "ผ้าไหมมัดหมี่ ผ้าไหมหางกระรอก", new Vector3(-7f, 0f, -9f), new Vector3(0f, 0f, -7f), new Vector2(10f, 3f), 5);
        Color[] silk = { new Color(0.75f, 0.1f, 0.35f), new Color(0.15f, 0.35f, 0.75f), new Color(0.95f, 0.65f, 0.1f), new Color(0.45f, 0.15f, 0.55f), new Color(0.1f, 0.55f, 0.45f) };
        for (int i = 0; i < 4; i++)
            ProvinceKit.Stall(t, new Vector3(-6f + i * 4f, 0f, -3f), 0f, new Color(0.9f, 0.9f, 0.85f), silk[i % silk.Length]);
        for (int i = 0; i < 5; i++) // silk hung on racks
        {
            var x = -8f + i * 4f;
            PrimitiveKit.Box("SilkRail", t, new Vector3(x, 2.1f, 4f), new Vector3(3.2f, 0.08f, 0.08f), ProvinceKit.Wood, collider: false);
            foreach (float s in new[] { -1.5f, 1.5f })
                PrimitiveKit.Cylinder("SilkPost", t, new Vector3(x + s, 1.05f, 4f), new Vector3(0.1f, 1.05f, 0.1f), ProvinceKit.Wood);
            for (int k = 0; k < 3; k++)
                PrimitiveKit.Box("Silk", t, new Vector3(x - 1f + k, 1.3f, 4f), new Vector3(0.9f, 1.6f, 0.03f), PrimitiveKit.Mat("PK_Silk" + ((i + k) % silk.Length), silk[(i + k) % silk.Length], 0.75f), collider: false);
        }
        var loom = PrimitiveKit.Group("Loom_กี่ทอผ้า", t, new Vector3(10f, 0f, 2f)).transform;
        PrimitiveKit.Box("Frame", loom, new Vector3(0f, 0.9f, 0f), new Vector3(1.6f, 1.8f, 2f), ProvinceKit.Wood);
        PrimitiveKit.Box("Cloth", loom, new Vector3(0f, 0.95f, -0.2f), new Vector3(1.3f, 0.03f, 1.4f), PrimitiveKit.Mat("PK_Silk1", silk[1], 0.75f), collider: false);
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, -3f), "market");
    }

    // 10. ถนนคนเดินเซราะกราว: stalls down both sides under strings of lights (evenings)
    private static void WalkingStreet(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ถนนคนเดินเซราะกราว", new Vector3(29f, 0f, 31f), 0f,
            "ตลาดคนเดิน ของกิน ของฝาก ดนตรีเปิดหมวก", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(30f, 3f), 10);
        ProvinceKit.Path(t, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f), 5f);
        Color[] tops = { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.75f, 0.45f), new Color(1f, 0.5f, 0.75f) };
        for (int i = 0; i < 7; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-18f + i * 6f, 0f, s * 4f), s > 0 ? 180f : 0f, tops[(i + (s > 0 ? 2 : 0)) % tops.Length], tops[(i + 1) % tops.Length]);
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.FairyLights(t, new Vector3(-20f, 0f, s * 2.6f), new Vector3(20f, 0f, s * 2.6f), 3.4f);
        TambonBuilder.ShopZone(t, new Vector3(-6f, 0f, -4f), "city_food");
    }
}
