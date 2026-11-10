using UnityEditor;
using UnityEngine;

// อุบลราชธานี: the town on the แม่น้ำมูล (ProvinceTown, inland) and its ten sights: วัดหนองป่าพง in its
// forest, the square stupa of วัดพระธาตุหนองบัว, ทุ่งศรีเมือง with the great carved candle of the
// candle festival, the sandy beach of the Mun at วัดใต้, the prehistoric paintings on the cliff of
// ผาแต้ม, the rock pools of สามพันโบก, the walking street, วัดศรีอุบลรัตนาราม, น้ำตกแสงจันทร์ falling
// through its hole in the rock, and ศาลหลักเมือง.
public static class ProvinceUbon
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "Ubon", Root = "Ubon", Name = "อุบลราชธานี",
        Airport = "ท่าอากาศยานอุบลราชธานี", Station = "สถานีรถไฟอุบลราชธานี",
        MainStreet = "ถนนชยางกูร", Monument = "อุบลราชธานี · เมืองดอกบัว",
        GantryWest = "↓ วัดหนองป่าพง · พระธาตุหนองบัว · สามพันโบก\n← ผาแต้ม · ศาลหลักเมือง",
        GantryEast = "↑ ทุ่งศรีเมือง · ถนนคนเดิน\nหาดวัดใต้ แม่น้ำมูล ↑",
        ShopsWest = new[] { "ร้านหมูยอ กุนเชียง", "ร้านผ้ากาบบัว", "ร้านข้าวเปียกเส้น", "ร้านกาแฟ" },
        ShopsEast = new[] { "ร้านทอง", "ร้านเทียนพรรษา", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = false, Accent = new Color(0.9f, 0.55f, 0.65f), Airlines = new[] { 0, 3 },
        CoachRoutes = new[] { "กรุงเทพฯ (หมอชิต 2)", "ศรีสะเกษ", "ขอนแก่น" },
        SongthaewRoutes = new[] { "รถสองแถว สาย 2", "วารินชำราบ", "ทุ่งศรีเมือง", "ตลาดใหญ่" },
    };

    [MenuItem("Thai Life/Provinces/Build อุบลราชธานี")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        NongPaPhong(root);
        ThatNongBua(root);
        ThungSiMuang(root);
        WatTaiBeach(root);
        PhaTaem(root);
        SamPhanBok(root);
        WalkingStreet(root);
        SiUbon(root);
        SaengChan(root);
        CityPillar(root);
    });

    // 1. วัดหนองป่าพง: the plain white ordination hall and chedi in the forest (a forest monastery)
    private static void NongPaPhong(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดหนองป่าพง", new Vector3(-58f, 0f, -40f), 180f,
            "วัดป่าสายหลวงพ่อชา · สงบร่มรื่น", new Vector3(8f, 0f, -12f), new Vector3(0f, 0f, -10f), new Vector2(10f, 2f), 5);
        ProvinceKit.ThaiHall(t, Vector3.zero, 0f, 8f, 9f, ProvinceKit.White, ProvinceKit.RoofRed, "อุโบสถ");
        ProvinceKit.Chedi(t, new Vector3(14f, 0f, 3f), 8f, ProvinceKit.Gold);
        for (int i = 0; i < 14; i++)
            Places.ShadeTree(t, new Vector3(-24f + (i % 7) * 8f, 0f, i < 7 ? -12f + (i % 2) * 2f : 12f), 1.15f);
        for (int i = 0; i < 3; i++) // the monks' huts (กุฏิ) among the trees
            ProvinceKit.StiltHouse(t, new Vector3(-24f + i * 6f, 0f, 4f), 0f, 3.6f, 3.4f);
    }

    // 2. วัดพระธาตุหนองบัว: the square stupa modelled on Bodh Gaya, its four corner towers
    private static void ThatNongBua(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดพระธาตุหนองบัว", new Vector3(-2f, 0f, -48f), 0f,
            "เจดีย์ทรงพุทธคยาสี่เหลี่ยม · 2,500 ปีพุทธศาสนา", new Vector3(9f, 0f, 12f), new Vector3(0f, 0f, 11f), new Vector2(12f, 2f), 7);
        PrimitiveKit.Box("Court", t, new Vector3(0f, 0.012f, 0f), new Vector3(24f, 0.02f, 24f), ProvinceKit.Paving, collider: false);
        PrimitiveKit.Box("Base", t, new Vector3(0f, 1f, 0f), new Vector3(14f, 2f, 14f), ProvinceKit.White);
        Tower(t, new Vector3(0f, 2f, 0f), 7f, 20f);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Tower(t, new Vector3(sx * 5.2f, 2f, sz * 5.2f), 2.4f, 8f);
        PrimitiveKit.Box("Door", t, new Vector3(0f, 3.4f, 3.53f), new Vector3(1.6f, 2.6f, 0.06f), ProvinceKit.Gold, collider: false);
    }

    // a square tower tapering in tiers, white with gold bands, a gold finial
    private static void Tower(Transform t, Vector3 at, float w, float h)
    {
        const int tiers = 6;
        float y = 0f, th = h / tiers;
        for (int i = 0; i < tiers; i++)
        {
            float s = w * (1f - i * 0.13f);
            PrimitiveKit.Box("Tier", t, at + new Vector3(0f, y + th * 0.5f, 0f), new Vector3(s, th, s), ProvinceKit.White, collider: i == 0);
            PrimitiveKit.Box("Band", t, at + new Vector3(0f, y + th - 0.1f, 0f), new Vector3(s + 0.12f, 0.2f, s + 0.12f), ProvinceKit.Gold, collider: false);
            y += th;
        }
        var bud = MeshKit.Lathe("PK_ThatBud", new[] { new Vector2(0.5f, 0f), new Vector2(0.55f, 0.2f), new Vector2(0.35f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.01f, 1f) }, 20);
        MeshKit.Place("Finial", t, bud, ProvinceKit.Gold, at + new Vector3(0f, y, 0f), default, new Vector3(w * 0.3f, w * 0.5f, w * 0.3f));
    }

    // 3. ทุ่งศรีเมือง: the lawn and the great carved candle on its float (งานแห่เทียนพรรษา)
    private static void ThungSiMuang(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "ทุ่งศรีเมือง · ต้นเทียนพรรษา", new Vector3(-27f, 0f, 31f), 0f,
            "ต้นเทียนแกะสลักยักษ์ · งานแห่เทียนเข้าพรรษา", new Vector3(8f, 0f, -9f), new Vector3(0f, 0f, -7f), new Vector2(14f, 3f), 9);
        PrimitiveKit.Box("Lawn", t, new Vector3(0f, 0.012f, 0f), new Vector3(34f, 0.02f, 20f), ProvinceKit.Grass, collider: false);
        var wax = PrimitiveKit.Mat("PK_CandleWax", new Color(0.96f, 0.72f, 0.2f), 0.55f, 0.2f);
        var f = PrimitiveKit.Group("CandleFloat_ต้นเทียน", t, new Vector3(0f, 0f, 2f)).transform;
        PrimitiveKit.Box("Float", f, new Vector3(0f, 0.6f, 0f), new Vector3(5f, 1.2f, 9f), wax);
        PrimitiveKit.Box("Bow", f, new Vector3(0f, 1.4f, -5f), new Vector3(1.2f, 2.4f, 1.6f), wax, new Vector3(-25f, 0f, 0f), collider: false); // the naga's head at the front
        PrimitiveKit.Cylinder("Candle", f, new Vector3(0f, 6.2f, 1f), new Vector3(1.6f, 5f, 1.6f), wax);
        for (int i = 0; i < 5; i++) // carved rings up the candle
            PrimitiveKit.Cylinder("Carving", f, new Vector3(0f, 2.4f + i * 1.9f, 1f), new Vector3(1.9f, 0.25f, 1.9f), ProvinceKit.Gold, collider: false);
        PrimitiveKit.Cylinder("Flame", f, new Vector3(0f, 11.6f, 1f), new Vector3(0.3f, 0.4f, 0.3f), PrimitiveKit.Glow("PK_CandleFlame", new Color(1f, 0.6f, 0.15f), 2.4f), collider: false);
        foreach (float x in new[] { -1.9f, 1.9f }) // angels (เทวดา) round the base
            foreach (float z in new[] { -2.5f, 3.5f })
                PrimitiveKit.Box("Angel", f, new Vector3(x, 2f, z), new Vector3(0.5f, 1.6f, 0.4f), wax);
        for (int i = 0; i < 8; i++)
            Places.ShadeTree(t, new Vector3(-15f + (i % 4) * 10f, 0f, i < 4 ? -8f : 9f), 0.9f);
    }

    // 4. หาดวัดใต้ · แม่น้ำมูล: the sandy bank beyond the ring road, the river, long-tail boats
    private static void WatTaiBeach(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "หาดวัดใต้ · แม่น้ำมูล", new Vector3(0f, 0f, 104f), 0f,
            "หาดทรายกลางแม่น้ำมูลช่วงหน้าแล้ง · แพริมน้ำ", new Vector3(-14f, 0f, -4f), new Vector3(0f, 0f, 0f), new Vector2(24f, 3f), 8);
        PrimitiveKit.Box("Sand", t, new Vector3(0f, 0.016f, 0f), new Vector3(86f, 0.02f, 10f), ProvinceKit.Sand, collider: false);
        PrimitiveKit.Box("River_แม่น้ำมูล", t, new Vector3(0f, 0.03f, 13f), new Vector3(300f, 0.02f, 16f), ProvinceKit.Water, collider: false);
        ProvinceKit.Blocker(t, new Vector3(0f, 1f, 6f), new Vector3(90f, 2f, 1f));
        var hull = PrimitiveKit.Mat("PK_BoatHull", new Color(0.35f, 0.22f, 0.12f), 0.3f);
        for (int i = 0; i < 3; i++)
        {
            var b = PrimitiveKit.Group("LongTail_เรือหางยาว", t, new Vector3(-20f + i * 18f, 0f, 9f + i), 90f).transform;
            PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.4f, 7f), hull, collider: false);
            PrimitiveKit.Box("Prow", b, new Vector3(0f, 0.7f, 3.6f), new Vector3(0.3f, 0.8f, 0.6f), hull, new Vector3(-30f, 0f, 0f), collider: false);
        }
        for (int i = 0; i < 4; i++) // the bamboo rafts with roofs (แพ) along the sand
        {
            var r = PrimitiveKit.Group("Raft_แพ", t, new Vector3(-30f + i * 9f, 0f, 3f)).transform;
            PrimitiveKit.Box("Deck", r, new Vector3(0f, 0.3f, 0f), new Vector3(4f, 0.2f, 3f), ProvinceKit.Wood);
            foreach (float x in new[] { -1.8f, 1.8f })
                PrimitiveKit.Cylinder("Post", r, new Vector3(x, 1.4f, 0f), new Vector3(0.12f, 1.1f, 0.12f), ProvinceKit.Wood, collider: false);
            PrimitiveKit.Box("Roof", r, new Vector3(0f, 2.6f, 0f), new Vector3(4.4f, 0.15f, 3.4f), PrimitiveKit.Mat("PK_Thatch", new Color(0.72f, 0.6f, 0.35f), 0.1f), collider: false);
        }
        TambonBuilder.ShopZone(t, new Vector3(-21f, 0f, 1f), "city_food");
    }

    // 5. ผาแต้ม: the long cliff with the red ochre paintings (people, fish, elephants, hands), a viewing deck
    private static void PhaTaem(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "อุทยานแห่งชาติผาแต้ม", new Vector3(-72f, 0f, 22f), -90f,
            "ภาพเขียนสีก่อนประวัติศาสตร์ 3,000 ปี · ชมพระอาทิตย์ขึ้นแรกสุดในไทย", new Vector3(7f, 0f, -10f), new Vector3(0f, 0f, -7f), new Vector2(14f, 2f), 6);
        for (int i = 0; i < 9; i++)
        {
            var blob = MeshKit.Blob("PK_Boulder" + (i % 4), 100 + i % 4, 0.22f);
            var r = MeshKit.Place("Cliff", t, blob, ProvinceKit.Sandstone, new Vector3(-18f + i * 4.5f, 4f, 4f), new Vector3(0f, i * 41f, 0f), new Vector3(6f, 9f, 5f));
            r.AddComponent<MeshCollider>().sharedMesh = blob;
        }
        PrimitiveKit.Box("Face", t, new Vector3(0f, 3.4f, 1.9f), new Vector3(38f, 6.8f, 0.4f), ProvinceKit.Sandstone);
        var ochre = PrimitiveKit.Mat("PK_Ochre", new Color(0.65f, 0.22f, 0.12f), 0.1f);
        var rng = new System.Random(21);
        for (int i = 0; i < 26; i++) // the paintings
        {
            float x = -16f + (float)rng.NextDouble() * 32f, y = 1.4f + (float)rng.NextDouble() * 3.6f;
            var size = i % 3 == 0 ? new Vector3(0.9f, 0.5f, 0.02f) : i % 3 == 1 ? new Vector3(0.25f, 0.7f, 0.02f) : new Vector3(0.35f, 0.35f, 0.02f);
            PrimitiveKit.Box("Painting", t, new Vector3(x, y, 1.68f), size, ochre, new Vector3(0f, 0f, (float)rng.NextDouble() * 30f - 15f), collider: false);
        }
        PrimitiveKit.Box("Boardwalk", t, new Vector3(0f, 0.3f, -1f), new Vector3(36f, 0.2f, 2.4f), ProvinceKit.Wood);
        foreach (float x in new[] { -17f, -6f, 6f, 17f })
            PrimitiveKit.Box("Rail", t, new Vector3(x, 0.9f, -2.1f), new Vector3(10f, 0.08f, 0.08f), ProvinceKit.Wood, collider: false);
    }

    // 6. สามพันโบก: the sandstone shelf full of round pools and potholes (in the dry season)
    private static void SamPhanBok(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "สามพันโบก", new Vector3(-58f, 0f, -72f), 180f,
            "แกรนด์แคนยอนแม่น้ำโขง · แอ่งหินนับพัน", new Vector3(9f, 0f, -9f), new Vector3(0f, 0f, -6f), new Vector2(16f, 3f), 6);
        PrimitiveKit.Box("Shelf", t, new Vector3(0f, 0.15f, 0f), new Vector3(42f, 0.3f, 18f), ProvinceKit.Sandstone);
        var rng = new System.Random(33);
        for (int i = 0; i < 34; i++)
        {
            float x = -19f + (float)rng.NextDouble() * 38f, z = -7.5f + (float)rng.NextDouble() * 15f, r = 0.8f + (float)rng.NextDouble() * 1.8f;
            PrimitiveKit.Cylinder("Bok_โบก", t, new Vector3(x, 0.31f, z), new Vector3(r, 0.01f, r), ProvinceKit.Water, collider: false);
            PrimitiveKit.Cylinder("Rim", t, new Vector3(x, 0.305f, z), new Vector3(r + 0.3f, 0.01f, r + 0.3f), ProvinceKit.Rock, collider: false);
        }
        for (int i = 0; i < 8; i++)
            Nature.Rock(t, new Vector3(-20f + i * 5.7f, 0.3f, 8.5f), 1.4f + (i % 3) * 0.4f, i + 50);
    }

    // 7. ถนนคนเดินอุบล: stalls and lights
    private static void WalkingStreet(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ถนนคนเดินอุบลราชธานี", new Vector3(29f, 0f, 31f), 0f,
            "ของกินอีสาน ผ้าพื้นเมือง · ดนตรีโปงลาง", new Vector3(-18f, 0f, -8f), new Vector3(0f, 0f, 0f), new Vector2(30f, 3f), 10);
        ProvinceKit.Path(t, new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f), 5f);
        Color[] tops = { new Color(0.9f, 0.45f, 0.6f), new Color(0.25f, 0.55f, 0.9f), new Color(1f, 0.8f, 0.25f), new Color(0.35f, 0.7f, 0.4f) };
        for (int i = 0; i < 7; i++)
            foreach (float s in new[] { -1f, 1f })
                ProvinceKit.Stall(t, new Vector3(-18f + i * 6f, 0f, s * 4f), s > 0 ? 180f : 0f, tops[(i + (s > 0 ? 2 : 0)) % tops.Length], tops[(i + 1) % tops.Length]);
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.FairyLights(t, new Vector3(-20f, 0f, s * 2.6f), new Vector3(20f, 0f, s * 2.6f), 3.4f);
        TambonBuilder.ShopZone(t, new Vector3(-6f, 0f, -4f), "city_food");
    }

    // 8. วัดศรีอุบลรัตนาราม: the white marble-style ordination hall (after วัดเบญจมบพิตร), a lotus pond
    private static void SiUbon(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดศรีอุบลรัตนาราม", new Vector3(66f, 0f, 16f), 0f,
            "พระแก้วบุษราคัม · อุโบสถจำลองวัดเบญจมบพิตร", new Vector3(10f, 0f, -11f), new Vector3(0f, 0f, -10f), new Vector2(12f, 2f), 7);
        PrimitiveKit.Box("Court", t, new Vector3(0f, 0.012f, 0f), new Vector3(34f, 0.02f, 24f), ProvinceKit.Paving, collider: false);
        ProvinceKit.ThaiHall(t, new Vector3(0f, 0f, 2f), 0f, 9f, 11f, ProvinceKit.White, ProvinceKit.RoofRed, "พระอุโบสถ");
        foreach (float s in new[] { -1f, 1f })
            ProvinceKit.Pond(t, new Vector3(s * 12f, 0f, -2f), new Vector2(6f, 6f));
        var pink = PrimitiveKit.Mat("PK_LotusPink", new Color(0.95f, 0.55f, 0.7f), 0.3f);
        for (int i = 0; i < 8; i++)
            PrimitiveKit.Cylinder("Lotus", t, new Vector3((i < 4 ? -12f : 12f) + (i % 2) * 1.5f - 0.7f, 0.06f, -3f + (i % 4 / 2) * 2f), new Vector3(0.6f, 0.02f, 0.6f), pink, collider: false);
    }

    // 9. น้ำตกแสงจันทร์: the fall drops through a hole in the rock roof over it (น้ำตกทะลุหลังคา)
    private static void SaengChan(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "น้ำตกแสงจันทร์", new Vector3(45f, 0f, 76f), 0f,
            "น้ำตกทะลุหลังคา · อุทยานแห่งชาติผาแต้ม", new Vector3(-12f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(12f, 2f), 6);
        ProvinceKit.Waterfall(t, Vector3.zero, 0f, 8f, 2);
        // the rock roof with the hole the water falls through
        foreach (float x in new[] { -4.5f, 4.5f })
            PrimitiveKit.Box("RoofRock", t, new Vector3(x, 8.4f, 2.6f), new Vector3(6f, 1.2f, 6f), ProvinceKit.Rock, collider: false);
        foreach (float z in new[] { 0.4f, 4.8f })
            PrimitiveKit.Box("RoofRock", t, new Vector3(0f, 8.4f, z), new Vector3(3f, 1.2f, 1.6f), ProvinceKit.Rock, collider: false);
        PrimitiveKit.Box("Shaft", t, new Vector3(0f, 7.6f, 2.6f), new Vector3(1.4f, 2.4f, 0.12f), ProvinceKit.Fall, collider: false);
    }

    // 10. ศาลหลักเมืองอุบลราชธานี
    private static void CityPillar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "ศาลหลักเมืองอุบลราชธานี", new Vector3(-50f, 0f, 78f), 0f,
            "หลักเมืองไม้ชัยพฤกษ์ · ศาลาทรงจตุรมุข", new Vector3(-10f, 0f, -7f), new Vector3(0f, 0f, -7f), new Vector2(10f, 2f), 5);
        ProvinceKit.SpirePavilion(t, Vector3.zero, 0f, 6f, ProvinceKit.White, ProvinceKit.RoofRed);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Nature.Bush(t, new Vector3(Mathf.Sin(a) * 8.5f, 0f, Mathf.Cos(a) * 7.5f), 0.7f, i + 200, flowers: true);
        }
        ProvinceKit.Path(t, new Vector3(0f, 0f, -4.5f), new Vector3(0f, 0f, -10f), 3f);
        for (int i = 0; i < 4; i++)
            Places.ShadeTree(t, new Vector3(i < 2 ? -18f : 18f, 0f, -4f + (i % 2) * 8f), 1f);
    }
}
