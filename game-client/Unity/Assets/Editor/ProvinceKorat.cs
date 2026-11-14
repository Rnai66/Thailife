using UnityEditor;
using UnityEngine;

// นครราชสีมา (โคราช): ประตูสู่อีสาน — the inland town (ProvinceTown) and its ten sights: อนุสาวรีย์
// ท้าวสุรนารี (ย่าโม) in front of ประตูชุมพล and the old moat, ปราสาทหินพิมาย (the great white-sandstone
// prang inside its galleries, the naga bridge, the red laterite prangs), วัดศาลาลอย's ubosot built like a
// junk on the water, เขาใหญ่ (the forest mountain and น้ำตกเหวสุวัต), ไทรงามพิมาย (one banyan grown into
// a grove), the potters of ด่านเกวียน and their kiln, the sunflower fields (ทุ่งทานตะวัน) with the
// wind turbines over ลำตะคอง beyond, the ถนนคนเดิน night market, and ตลาดของฝากโคราช (ผัดหมี่โคราช).
public static class ProvinceKorat
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "Korat", Root = "Korat", Name = "นครราชสีมา",
        Airport = "ท่าอากาศยานนครราชสีมา", Station = "สถานีรถไฟนครราชสีมา",
        MainStreet = "ถนนราชดำเนิน", Monument = "นครราชสีมา · ประตูสู่อีสาน",
        GantryWest = "← อนุสาวรีย์ท้าวสุรนารี · ประตูชุมพล\n↓ ด่านเกวียน · ไทรงาม",
        GantryEast = "↑ ปราสาทหินพิมาย · ทุ่งทานตะวัน\nเขาใหญ่ →",
        ShopsWest = new[] { "ผัดหมี่โคราช", "ร้านผ้าไหมปักธงชัย", "ร้านกาแฟ", "ร้านเครื่องปั้นดินเผา" },
        ShopsEast = new[] { "ร้านทอง", "ส้มตำไก่ย่าง", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = false, Accent = new Color(0.55f, 0.18f, 0.12f), Airlines = new[] { 0, 5 },
        CoachRoutes = new[] { "กรุงเทพฯ (หมอชิต 2)", "ขอนแก่น", "อุบลราชธานี" },
    };

    private static Material Bronze => PrimitiveKit.Mat("PK_Bronze", new Color(0.24f, 0.2f, 0.15f), 0.65f, 0.75f);
    private static Material WhiteStone => PrimitiveKit.Mat("PK_PhimaiStone", new Color(0.84f, 0.82f, 0.76f), 0.15f);
    private static Material Clay => PrimitiveKit.Mat("PK_DanKwianClay", new Color(0.52f, 0.25f, 0.14f), 0.25f);
    private static Material Marigold => PrimitiveKit.Mat("PK_Marigold", new Color(1f, 0.62f, 0.08f), 0.2f);

    [MenuItem("Thai Life/Provinces/Build นครราชสีมา")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        YaMo(root);
        Phimai(root);
        SalaLoi(root);
        KhaoYai(root);
        SaiNgam(root);
        DanKwian(root);
        Sunflowers(root);
        WindFarm(root);
        NightMarket(root);
        Souvenirs(root);
    });

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    // 1. อนุสาวรีย์ท้าวสุรนารี: the bronze heroine on her tall plinth, sword point down in the right hand,
    // the left on her hip; marigold garlands heaped at the foot; ประตูชุมพล and the moat behind her
    private static void YaMo(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "อนุสาวรีย์ท้าวสุรนารี", new Vector3(-27f, 0f, 31f), 0f,
            "ย่าโม วีรสตรีเมืองโคราช · ประตูชุมพล", new Vector3(-12f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(14f, 3f), 8);
        ProvinceKit.Path(t, new Vector3(0f, 0f, -10f), new Vector3(0f, 0f, 4f), 9f, ProvinceKit.Paving);
        // the plinth: three steps, a tall granite block, a bronze plaque
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.15f, 0f), new Vector3(7f, 0.3f, 7f), ProvinceKit.Granite);
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.45f, 0f), new Vector3(5.6f, 0.3f, 5.6f), ProvinceKit.Granite);
        PrimitiveKit.Box("Plinth", t, new Vector3(0f, 2.1f, 0f), new Vector3(2.8f, 3f, 2.8f), ProvinceKit.Granite);
        PrimitiveKit.Box("Cornice", t, new Vector3(0f, 3.7f, 0f), new Vector3(3.2f, 0.25f, 3.2f), ProvinceKit.Granite);
        PrimitiveKit.Box("Plaque", t, new Vector3(0f, 2.3f, -1.42f), new Vector3(1.6f, 1f, 0.05f), Bronze, collider: false);
        PrimitiveKit.Label("ท้าวสุรนารี", t, new Vector3(0f, 2.35f, -1.46f), 0.03f, new Color(0.95f, 0.8f, 0.45f));
        Heroine(t, new Vector3(0f, 3.82f, 0f), 2.9f);
        // garlands and offerings round the foot
        for (int i = 0; i < 22; i++)
        {
            float a = i * Mathf.PI * 2f / 22f;
            PrimitiveKit.Cylinder("Garland_พวงมาลัย", t, new Vector3(Mathf.Cos(a) * 2.2f, 0.72f, Mathf.Sin(a) * 2.2f), new Vector3(0.45f, 0.12f, 0.45f), i % 4 == 0 ? ProvinceKit.ShrineRed : Marigold, collider: false);
        }
        for (int i = 0; i < 10; i++)
            Nature.Bush(t, new Vector3(-4.5f + i, 0f, -4.6f), 0.55f, i + 300, flowers: true);
        // ประตูชุมพล in the old city wall, the moat in front of it
        ProvinceKit.CityWall(t, new Vector3(0f, 0f, 8.5f), 0f, 30f, ProvinceKit.Brick, "ประตูชุมพล");
        ProvinceKit.Pond(t, new Vector3(-10.5f, 0f, 5f), new Vector2(11f, 2.6f));
        ProvinceKit.Pond(t, new Vector3(10.5f, 0f, 5f), new Vector2(11f, 2.6f));
    }

    // The figure: a woman in a sabai and chong kraben, hair in a bun; scale = height in metres
    private static void Heroine(Transform p, Vector3 at, float h)
    {
        var g = PrimitiveKit.Group("Statue_ย่าโม", p, at, 180f).transform;
        var body = PrimitiveKit.Group("Figure", g, Vector3.zero).transform;
        body.localScale = Vector3.one * h;
        var m = Bronze;
        MeshKit.Place("Base", body, MeshKit.Lathe("PK_YaMoBase", new[] { V(0.17f, 0f), V(0.17f, 0.03f), V(0.15f, 0.04f) }, 20), m, Vector3.zero);
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Cylinder("Shin", body, new Vector3(s * 0.05f, 0.14f, 0f), new Vector3(0.05f, 0.1f, 0.05f), m, collider: false);
            PrimitiveKit.Box("Foot", body, new Vector3(s * 0.05f, 0.05f, 0.03f), new Vector3(0.05f, 0.03f, 0.1f), m, collider: false);
        }
        // the chong kraben: full at the hips, gathered between the knees
        MeshKit.Place("ChongKraben", body, MeshKit.Lathe("PK_YaMoKraben", new[]
        {
            V(0.07f, 0.22f), V(0.1f, 0.3f), V(0.115f, 0.42f), V(0.11f, 0.52f), V(0.095f, 0.56f),
        }, 20), m, Vector3.zero, default, new Vector3(1f, 1f, 0.8f));
        // torso with the sabai across it
        MeshKit.Place("Torso", body, MeshKit.Lathe("PK_YaMoTorso", new[]
        {
            V(0.095f, 0.55f), V(0.085f, 0.63f), V(0.1f, 0.71f), V(0.105f, 0.77f), V(0.06f, 0.81f), V(0.03f, 0.83f),
        }, 20), m, Vector3.zero, default, new Vector3(1f, 1f, 0.7f));
        PrimitiveKit.Box("Sabai_สไบ", body, new Vector3(0.01f, 0.72f, 0f), new Vector3(0.05f, 0.24f, 0.15f), m, new Vector3(0f, 0f, 35f), collider: false);
        PrimitiveKit.Cylinder("Neck", body, new Vector3(0f, 0.85f, 0f), new Vector3(0.04f, 0.025f, 0.04f), m, collider: false);
        PrimitiveKit.Sphere("Head", body, new Vector3(0f, 0.92f, 0f), new Vector3(0.09f, 0.11f, 0.1f), m);
        PrimitiveKit.Sphere("Bun_มวยผม", body, new Vector3(0f, 0.98f, 0.035f), Vector3.one * 0.05f, m);
        // left hand on the hip
        PrimitiveKit.Cylinder("UpperArmL", body, new Vector3(-0.13f, 0.72f, 0f), new Vector3(0.035f, 0.07f, 0.035f), m, new Vector3(0f, 0f, -25f), collider: false);
        PrimitiveKit.Cylinder("ForearmL", body, new Vector3(-0.13f, 0.6f, 0f), new Vector3(0.03f, 0.065f, 0.03f), m, new Vector3(0f, 0f, 40f), collider: false);
        // right arm down at the side, the sword held point to the ground in front
        PrimitiveKit.Cylinder("UpperArmR", body, new Vector3(0.13f, 0.71f, 0f), new Vector3(0.035f, 0.08f, 0.035f), m, new Vector3(0f, 0f, 10f), collider: false);
        PrimitiveKit.Cylinder("ForearmR", body, new Vector3(0.15f, 0.56f, -0.03f), new Vector3(0.03f, 0.07f, 0.03f), m, new Vector3(-20f, 0f, 5f), collider: false);
        PrimitiveKit.Sphere("HandR", body, new Vector3(0.155f, 0.49f, -0.06f), Vector3.one * 0.035f, m);
        PrimitiveKit.Box("Hilt", body, new Vector3(0.155f, 0.5f, -0.06f), new Vector3(0.012f, 0.06f, 0.012f), m, collider: false);
        PrimitiveKit.Box("Guard", body, new Vector3(0.155f, 0.47f, -0.06f), new Vector3(0.05f, 0.01f, 0.015f), m, collider: false);
        PrimitiveKit.Box("Blade", body, new Vector3(0.155f, 0.25f, -0.06f), new Vector3(0.025f, 0.43f, 0.006f), m, collider: false);
        var col = g.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, h * 0.48f, 0f);
        col.radius = h * 0.12f;
        col.height = h * 0.96f;
    }

    // 2. ปราสาทหินพิมาย: the white-sandstone prang facing south, galleries round it with a gopura on
    // each side, the cross-shaped naga bridge before the south gate, ปรางค์หินแดง and ปรางค์พรหมทัต
    private static void Phimai(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "ปราสาทหินพิมาย", new Vector3(-50f, 0f, 78f), 0f,
            "ปราสาทหินทรายขาวที่ใหญ่ที่สุดในไทย · สะพานนาคราช", new Vector3(-30f, 0f, -8.5f), new Vector3(-14f, 0f, -7f), new Vector2(14f, 2f), 8);
        var stone = WhiteStone;
        // outer galleries (ระเบียงคด): 30 x 16, a gopura in the middle of each long side
        const float hw = 15f, hd = 8f;
        foreach (float s in new[] { -1f, 1f })
        {
            foreach (float side in new[] { -1f, 1f })
                PrimitiveKit.Box("Gallery", t, new Vector3(side * (hw * 0.5f + 1.2f), 1.3f, s * hd), new Vector3(hw - 2.4f, 2.6f, 1.6f), stone);
            PrimitiveKit.Box("Gallery", t, new Vector3(s * hw, 1.3f, 0f), new Vector3(1.6f, 2.6f, hd * 2f + 1.6f), stone);
            var gate = PrimitiveKit.Group("Gopura_โคปุระ", t, new Vector3(0f, 0f, s * hd)).transform;
            foreach (float x in new[] { -1.5f, 1.5f })
                PrimitiveKit.Box("Jamb", gate, new Vector3(x, 1.6f, 0f), new Vector3(0.9f, 3.2f, 2.2f), stone);
            PrimitiveKit.Box("Lintel_ทับหลัง", gate, new Vector3(0f, 3.5f, 0f), new Vector3(4.2f, 0.7f, 2.4f), stone, collider: false);
            MeshKit.Place("GateRoof", gate, MeshKit.Gable("PK_Gable", 1f, 1f, 1f), stone, new Vector3(-2.2f, 3.85f, 0f), new Vector3(0f, 90f, 0f), new Vector3(2.6f, 1.6f, 4.4f));
            for (float x = -hw + 1.5f; x < hw - 1f; x += 2.2f) // the curved stone roof of the galleries
                if (Mathf.Abs(x) > 2.6f) PrimitiveKit.Cylinder("GalleryRoof", t, new Vector3(x, 2.65f, s * hd), new Vector3(1.4f, 1f, 1.4f), stone, new Vector3(0f, 0f, 90f), collider: false);
        }
        PrimitiveKit.Box("Court", t, new Vector3(0f, 0.04f, 0f), new Vector3(hw * 2f - 1.6f, 0.08f, hd * 2f - 1.6f), ProvinceKit.Laterite, collider: false);
        // the main prang on its platform, a mandapa in front (south)
        PrimitiveKit.Box("Platform", t, new Vector3(0f, 0.5f, 0.5f), new Vector3(8f, 1f, 8f), stone);
        ProvinceKit.Prang(t, new Vector3(0f, 1f, 1.2f), 17f, stone);
        PrimitiveKit.Box("Mandapa", t, new Vector3(0f, 2.6f, -3.8f), new Vector3(3.4f, 3.2f, 4f), stone);
        MeshKit.Place("MandapaRoof", t, MeshKit.Gable("PK_Gable", 1f, 1f, 1f), stone, new Vector3(0f, 4.2f, -6f), default, new Vector3(3.8f, 1.8f, 4.4f));
        // the red prangs to the sides
        ProvinceKit.Prang(t, new Vector3(-8.5f, 0f, 2.5f), 8f, ProvinceKit.Laterite);
        ProvinceKit.Prang(t, new Vector3(8.5f, 0f, 2.5f), 7f, ProvinceKit.Laterite);
        // the naga bridge before the south gopura (outside the galleries, toward the town)
        var bridge = PrimitiveKit.Group("NagaBridge_สะพานนาคราช", t, new Vector3(-24f, 0f, -2f)).transform;
        PrimitiveKit.Box("Deck", bridge, new Vector3(0f, 0.4f, 0f), new Vector3(9f, 0.8f, 4.6f), stone);
        PrimitiveKit.Box("Arms", bridge, new Vector3(0f, 0.4f, 0f), new Vector3(4.6f, 0.8f, 9f), stone);
        foreach (float z in new[] { -2.1f, 2.1f })
        {
            PrimitiveKit.Box("NagaRail", bridge, new Vector3(0f, 1.05f, z), new Vector3(8.6f, 0.35f, 0.35f), stone, collider: false);
            for (int k = -2; k <= 2; k++) // a seven-headed naga hood rearing at each end
                PrimitiveKit.Sphere("NagaHood", bridge, new Vector3(-4.6f, 1.6f - Mathf.Abs(k) * 0.15f, z + k * 0.22f), new Vector3(0.3f, 0.5f, 0.3f), stone);
        }
        ProvinceKit.Path(t, new Vector3(-19.5f, 0f, -2f), new Vector3(-hw - 1f, 0f, -2f), 3f, ProvinceKit.Laterite);
    }

    // 3. วัดศาลาลอย: the ubosot as a junk (เรือสำเภา) on the water, its walls in brown Dan Kwian ceramic
    private static void SalaLoi(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดศาลาลอย", new Vector3(-70f, 0f, 22f), 0f,
            "อุโบสถรูปเรือสำเภา · ผนังกระเบื้องดินเผาด่านเกวียน", new Vector3(10f, 0f, -18f), new Vector3(0f, 0f, -17f), new Vector2(10f, 3f), 5);
        ProvinceKit.Pond(t, new Vector3(0f, 0f, 2f), new Vector2(26f, 30f), block: false);
        var boat = PrimitiveKit.Group("Junk_อุโบสถเรือสำเภา", t, new Vector3(0f, 0f, 2f), 90f).transform;
        var hull = Clay;
        var trim = ProvinceKit.Gold;
        // the hull: stacked strakes, each a little longer and wider, the bow and stern sweeping up
        for (int i = 0; i < 5; i++)
        {
            float y = 0.3f + i * 0.5f, len = 13f + i * 1.4f, w = 4.6f + i * 0.5f;
            PrimitiveKit.Box("Strake", boat, new Vector3(0f, y, 0f), new Vector3(len, 0.5f, w), hull);
            PrimitiveKit.Box("Rub", boat, new Vector3(0f, y + 0.25f, 0f), new Vector3(len + 0.1f, 0.06f, w + 0.1f), trim, collider: false);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Bow", boat, new Vector3(s * 10.4f, 2.6f, 0f), new Vector3(3.4f, 1.4f, 5.6f), hull, new Vector3(0f, 0f, -s * 18f));
            PrimitiveKit.Box("BowTip", boat, new Vector3(s * 12f, 3.6f, 0f), new Vector3(1.4f, 1.2f, 3.6f), hull, new Vector3(0f, 0f, -s * 30f), collider: false);
        }
        PrimitiveKit.Box("Deck", boat, new Vector3(0f, 2.6f, 0f), new Vector3(16f, 0.1f, 6.6f), ProvinceKit.Wood);
        // the hall on deck: brown ceramic walls, a tall three-tier roof of the same tiles
        PrimitiveKit.Box("HallWalls", boat, new Vector3(0f, 4.2f, 0f), new Vector3(10f, 3.2f, 5f), hull);
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Box("Window", boat, new Vector3(-3.8f + i * 1.5f, 4.2f, -2.52f), new Vector3(0.6f, 1.4f, 0.05f), ProvinceKit.Dark, collider: false);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        for (int i = 0; i < 3; i++)
            MeshKit.Place("Roof", boat, gable, i == 2 ? trim : PrimitiveKit.Mat("PK_SalaLoiRoof", new Color(0.4f, 0.18f, 0.1f), 0.35f),
                new Vector3(-(12f - i * 2.6f) * 0.5f, 5.8f + i * 1.4f, 0f), new Vector3(0f, 90f, 0f), new Vector3(6.4f - i * 1.6f, 2.2f, 12f - i * 2.6f));
        PrimitiveKit.Cylinder("Mast", boat, new Vector3(0f, 11.5f, 0f), new Vector3(0.18f, 1.6f, 0.18f), trim, collider: false);
        // a walkway out to it
        ProvinceKit.Path(t, new Vector3(0f, 0f, -14f), new Vector3(0f, 0f, -4.5f), 2.4f, ProvinceKit.Wood);
    }

    // 4. อุทยานแห่งชาติเขาใหญ่: the forested mountain, น้ำตกเหวสุวัต falling from it, the park sign
    private static void KhaoYai(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "อุทยานแห่งชาติเขาใหญ่", new Vector3(62f, 0f, 17f), 0f,
            "มรดกโลก · น้ำตกเหวสุวัต", new Vector3(-14f, 0f, -15f), new Vector3(0f, 0f, -12f), new Vector2(12f, 3f), 6);
        ProvinceKit.Mountain(t, new Vector3(0f, 0f, 14f), new Vector3(46f, 26f, 18f), 7, "เขาใหญ่");
        ProvinceKit.Waterfall(t, new Vector3(0f, 0f, 2f), 0f, 11f, 3);
        var sign = PrimitiveKit.Group("ParkSign", t, new Vector3(12f, 0f, -14f)).transform;
        PrimitiveKit.Box("Board", sign, new Vector3(0f, 1.6f, 0f), new Vector3(5f, 1.6f, 0.4f), ProvinceKit.Wood);
        PrimitiveKit.Label("อุทยานแห่งชาติเขาใหญ่", sign, new Vector3(0f, 1.65f, -0.22f), 0.035f, new Color(1f, 0.95f, 0.8f));
        foreach (float x in new[] { -2.3f, 2.3f })
            PrimitiveKit.Cylinder("Leg", sign, new Vector3(x, 0.8f, 0f), new Vector3(0.25f, 0.8f, 0.25f), ProvinceKit.Wood);
    }

    // 5. ไทรงามพิมาย: one banyan that grew into a grove — many trunks from its hanging roots, one canopy
    private static void SaiNgam(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "ไทรงาม พิมาย", new Vector3(-72f, 0f, -38f), 0f,
            "ต้นไทรอายุกว่า 350 ปี แผ่กิ่งก้านทั้งไร่", new Vector3(12f, 0f, -14f), new Vector3(0f, 0f, -14f), new Vector2(10f, 3f), 5);
        var bark = PrimitiveKit.Mat("PK_BanyanBark", new Color(0.45f, 0.4f, 0.33f), 0.1f);
        var leaves = PrimitiveKit.Mat("PK_BanyanLeaf", new Color(0.18f, 0.42f, 0.16f), 0.15f);
        var rng = new System.Random(350);
        PrimitiveKit.Cylinder("MainTrunk", t, new Vector3(0f, 3f, 0f), new Vector3(2.6f, 3f, 2.6f), bark);
        for (int i = 0; i < 26; i++) // trunks grown from the hanging roots
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = 3f + (float)rng.NextDouble() * 10f;
            float th = 0.3f + (float)rng.NextDouble() * 0.6f;
            PrimitiveKit.Cylinder("RootTrunk", t, new Vector3(Mathf.Cos(a) * r, 3.2f, Mathf.Sin(a) * r), new Vector3(th, 3.2f, th), bark, new Vector3(((float)rng.NextDouble() - 0.5f) * 8f, 0f, ((float)rng.NextDouble() - 0.5f) * 8f));
        }
        for (int i = 0; i < 40; i++) // hanging aerial roots
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = 2f + (float)rng.NextDouble() * 12f;
            float len = 1.5f + (float)rng.NextDouble() * 3f;
            PrimitiveKit.Cylinder("AerialRoot", t, new Vector3(Mathf.Cos(a) * r, 6.4f - len * 0.5f, Mathf.Sin(a) * r), new Vector3(0.06f, len * 0.5f, 0.06f), bark, collider: false);
        }
        for (int i = 0; i < 22; i++) // one wide canopy
        {
            float a = i * 2.4f, r = i < 4 ? 2f : 4f + (i % 5) * 2.4f;
            var blob = MeshKit.Blob("PK_Canopy" + (i % 3), 40 + i % 3, 0.25f);
            MeshKit.Place("Canopy", t, blob, leaves, new Vector3(Mathf.Cos(a) * r, 7.2f + (i % 3) * 0.6f, Mathf.Sin(a) * r), new Vector3(0f, i * 31f, 0f), new Vector3(7f, 3.2f, 7f));
        }
        // the shrine under it with coloured cloths tied round the trunk
        PrimitiveKit.Cylinder("Cloth", t, new Vector3(0f, 1.2f, 0f), new Vector3(2.7f, 0.2f, 2.7f), ProvinceKit.ShrineRed, collider: false);
        PrimitiveKit.Cylinder("Cloth", t, new Vector3(0f, 1.5f, 0f), new Vector3(2.68f, 0.15f, 2.68f), Marigold, collider: false);
        PrimitiveKit.Box("SpiritHouse_ศาลพระภูมิ", t, new Vector3(0f, 0.6f, -2.2f), new Vector3(0.9f, 1.2f, 0.7f), ProvinceKit.Gold);
    }

    // 6. หมู่บ้านด่านเกวียน: pots and vases of rust-brown clay along the road, a brick kiln, a wheel
    private static void DanKwian(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "เครื่องปั้นดินเผาด่านเกวียน", new Vector3(-45f, 0f, -62f), 0f,
            "ดินเผาสีสนิมเหล็ก · โอ่ง แจกัน กระถาง", new Vector3(-14f, 0f, -16f), new Vector3(0f, 0f, -12f), new Vector2(18f, 3f), 6);
        var vase = MeshKit.Lathe("PK_DKVase", new[] { V(0.16f, 0f), V(0.3f, 0.25f), V(0.34f, 0.5f), V(0.2f, 0.85f), V(0.13f, 0.95f), V(0.17f, 1f) }, 16);
        var jar = MeshKit.Lathe("PK_DKJar", new[] { V(0.3f, 0f), V(0.5f, 0.3f), V(0.52f, 0.6f), V(0.35f, 0.95f), V(0.3f, 1f) }, 18);
        var pot = MeshKit.Lathe("PK_DKPot", new[] { V(0.22f, 0f), V(0.32f, 0.35f), V(0.36f, 0.45f) }, 16, capTop: false);
        var darker = PrimitiveKit.Mat("PK_DanKwianDark", new Color(0.32f, 0.16f, 0.1f), 0.35f);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 12; i++)
            {
                var at = new Vector3(-16f + i * 2.8f, 0f, -6f + row * 2.2f);
                var mesh = (i + row) % 3 == 0 ? jar : (i + row) % 3 == 1 ? vase : pot;
                float sc = mesh == jar ? 1.3f : 1f + (i % 2) * 0.3f;
                MeshKit.Place("Pottery", t, mesh, (i + row) % 4 == 0 ? darker : Clay, at, new Vector3(0f, i * 40f, 0f), Vector3.one * sc);
            }
        // shop sheds
        for (int i = 0; i < 3; i++)
            ProvinceKit.Stall(t, new Vector3(-12f + i * 12f, 0f, 4f), 180f, new Color(0.75f, 0.45f, 0.25f), new Color(0.5f, 0.25f, 0.14f));
        // the kiln (เตาเผา): a brick dome with a chimney
        var kiln = PrimitiveKit.Group("Kiln_เตาเผา", t, new Vector3(15f, 0f, 9f)).transform;
        MeshKit.Place("Dome", kiln, MeshKit.Lathe("PK_Kiln", new[] { V(3f, 0f), V(3f, 1f), V(2.4f, 2.6f), V(1.2f, 3.4f), V(0.01f, 3.6f) }, 24), ProvinceKit.Brick, Vector3.zero)
            .AddComponent<MeshCollider>().sharedMesh = MeshKit.Lathe("PK_Kiln", new[] { V(3f, 0f), V(3f, 1f), V(2.4f, 2.6f), V(1.2f, 3.4f), V(0.01f, 3.6f) }, 24);
        PrimitiveKit.Cylinder("Chimney", kiln, new Vector3(2.4f, 3.5f, 0f), new Vector3(0.8f, 3.5f, 0.8f), ProvinceKit.Brick);
        PrimitiveKit.Box("Mouth", kiln, new Vector3(0f, 0.7f, -2.95f), new Vector3(1.2f, 1.2f, 0.1f), ProvinceKit.Dark, collider: false);
        PrimitiveKit.Box("Fire", kiln, new Vector3(0f, 0.45f, -2.98f), new Vector3(0.8f, 0.4f, 0.05f), PrimitiveKit.Glow("PK_KilnFire", new Color(1f, 0.45f, 0.1f), 2.5f), collider: false);
        // the potter's wheel
        PrimitiveKit.Cylinder("Wheel", t, new Vector3(8f, 0.5f, 9f), new Vector3(0.8f, 0.05f, 0.8f), ProvinceKit.Wood);
        MeshKit.Place("OnTheWheel", t, pot, Clay, new Vector3(8f, 0.55f, 9f), default, Vector3.one * 0.8f).AddComponent<Spinner>().Configure(Vector3.up, 120f);
    }

    // 7. ทุ่งทานตะวัน: row after row of sunflowers turned to the morning sun
    private static void Sunflowers(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "ทุ่งทานตะวัน", new Vector3(45f, 0f, 78f), 0f,
            "ทุ่งดอกทานตะวันบานเหลืองอร่าม ปลายฝนต้นหนาว", new Vector3(-36f, 0f, -8f), new Vector3(-26f, 0f, -6f), new Vector2(10f, 3f), 6);
        var stem = PrimitiveKit.Mat("PK_SunStem", new Color(0.3f, 0.5f, 0.18f), 0.1f);
        var petal = PrimitiveKit.Mat("PK_SunPetal", new Color(1f, 0.78f, 0.05f), 0.25f);
        var seed = PrimitiveKit.Mat("PK_SunSeed", new Color(0.25f, 0.15f, 0.06f), 0.1f);
        var soil = PrimitiveKit.Mat("PK_FieldSoil", new Color(0.45f, 0.33f, 0.22f), 0.05f);
        PrimitiveKit.Box("Field", t, new Vector3(6f, 0.02f, 0f), new Vector3(60f, 0.04f, 16f), soil, collider: false);
        var field = PrimitiveKit.Group("Flowers", t, Vector3.zero).transform;
        var rng = new System.Random(77);
        for (int row = 0; row < 7; row++)
            for (int i = 0; i < 24; i++)
            {
                float x = -22f + i * 2.4f + (float)rng.NextDouble() * 0.6f, z = -6.5f + row * 2.1f;
                float h = 1.2f + (float)rng.NextDouble() * 0.5f;
                PrimitiveKit.Cylinder("Stem", field, new Vector3(x, h * 0.5f, z), new Vector3(0.06f, h * 0.5f, 0.06f), stem, collider: false);
                PrimitiveKit.Cylinder("Petals", field, new Vector3(x, h, z - 0.08f), new Vector3(0.6f, 0.02f, 0.6f), petal, new Vector3(-70f, 0f, 0f), collider: false);
                PrimitiveKit.Cylinder("Seeds", field, new Vector3(x, h, z - 0.11f), new Vector3(0.32f, 0.02f, 0.32f), seed, new Vector3(-70f, 0f, 0f), collider: false);
                PrimitiveKit.Box("Leaf", field, new Vector3(x + 0.15f, h * 0.55f, z), new Vector3(0.3f, 0.02f, 0.2f), stem, new Vector3(0f, i * 50f, 25f), collider: false);
            }
        MeshKit.MergeByMaterial(field, "Korat_Sunflowers");
        ProvinceKit.Blocker(t, new Vector3(6f, 1f, 0f), new Vector3(58f, 2f, 15f));
        // a viewing deck at the near corner
        PrimitiveKit.Box("Deck", t, new Vector3(-31f, 0.6f, -4f), new Vector3(5f, 0.2f, 4f), ProvinceKit.Wood);
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Cylinder("Post", t, new Vector3(-33f + (i % 2) * 4f, 0.3f, -5.5f + (i / 2) * 3f), new Vector3(0.15f, 0.3f, 0.15f), ProvinceKit.Wood);
    }

    // 8. กังหันลมลำตะคอง: white wind turbines on the ridge past the ring road, slowly turning
    private static void WindFarm(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "กังหันลมลำตะคอง", new Vector3(-10f, 0f, 106f), 0f,
            "จุดชมวิวกังหันลมเหนืออ่างเก็บน้ำลำตะคอง", new Vector3(-26f, 0f, -6f), new Vector3(-14f, 0f, -5f), new Vector2(10f, 2f), 4);
        var white = ProvinceKit.White;
        var tower = MeshKit.Lathe("PK_TurbineTower", new[] { V(0.9f, 0f), V(0.45f, 24f), V(0.4f, 24.2f) }, 16);
        ProvinceKit.Pond(t, new Vector3(18f, 0f, 2f), new Vector2(26f, 8f), block: true);
        for (int i = 0; i < 4; i++)
        {
            var at = new Vector3(-16f + i * 11f, 0f, 6f + (i % 2) * 2f);
            var tw = MeshKit.Place("Tower", t, tower, white, at);
            tw.AddComponent<CapsuleCollider>().radius = 0.9f;
            PrimitiveKit.Box("Nacelle", t, at + new Vector3(0f, 24.4f, 0.3f), new Vector3(1.1f, 1.1f, 3f), white, collider: false);
            var hub = PrimitiveKit.Group("Rotor", t, at + new Vector3(0f, 24.4f, -1.4f)).transform;
            PrimitiveKit.Sphere("Hub", hub, Vector3.zero, Vector3.one * 0.9f, white);
            for (int b = 0; b < 3; b++)
            {
                var blade = PrimitiveKit.Group("Blade", hub, Vector3.zero).transform;
                blade.localRotation = Quaternion.Euler(0f, 0f, b * 120f);
                PrimitiveKit.Box("Vane", blade, new Vector3(0f, 6f, 0f), new Vector3(0.7f, 11f, 0.12f), white, new Vector3(0f, 8f, 0f), collider: false);
            }
            hub.gameObject.AddComponent<Spinner>().Configure(Vector3.forward, 30f + i * 4f);
        }
    }

    // 9. ถนนคนเดินโคราช: stalls down both sides under strings of lights
    private static void NightMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ถนนคนเดินโคราช", new Vector3(29f, 0f, 31f), 0f,
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

    // 10. ตลาดของฝากโคราช: ผัดหมี่โคราช in big woks, แหนม and กุนเชียง hanging, silk from ปักธงชัย
    private static void Souvenirs(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดของฝากโคราช", new Vector3(-2f, 0f, -55f), 0f,
            "ผัดหมี่โคราช แหนม กุนเชียง ผ้าไหมปักธงชัย", new Vector3(14f, 0f, -18f), new Vector3(0f, 0f, -12f), new Vector2(16f, 3f), 7);
        PrimitiveKit.Box("Hall", t, new Vector3(0f, 0.05f, 0f), new Vector3(28f, 0.1f, 18f), ProvinceKit.Paving, collider: false);
        var roof = PrimitiveKit.Mat("PK_MarketRoof", new Color(0.75f, 0.2f, 0.12f), 0.4f);
        for (int i = 0; i < 6; i++)
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Cylinder("Column", t, new Vector3(-12.5f + i * 5f, 2.5f, s * 8f), new Vector3(0.35f, 2.5f, 0.35f), ProvinceKit.Concrete);
        MeshKit.Place("Roof", t, MeshKit.Gable("PK_Gable", 1f, 1f, 1f), roof, new Vector3(-14.5f, 5f, 0f), new Vector3(0f, 90f, 0f), new Vector3(19f, 3f, 29f));
        PrimitiveKit.Label("ตลาดของฝากโคราช", t, new Vector3(0f, 5.6f, -9.4f), 0.06f, new Color(1f, 0.9f, 0.4f));
        var wok = MeshKit.Lathe("PK_Wok", new[] { V(0.05f, 0f), V(0.45f, 0.12f), V(0.6f, 0.25f) }, 18, capTop: false);
        var noodles = PrimitiveKit.Mat("PK_PadMee", new Color(0.82f, 0.45f, 0.3f), 0.3f);
        for (int i = 0; i < 4; i++) // ผัดหมี่ counters
        {
            var at = new Vector3(-9f + i * 6f, 0f, -3.5f);
            PrimitiveKit.Box("Counter", t, at + new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 1.4f), ProvinceKit.Steel);
            MeshKit.Place("Wok", t, wok, ProvinceKit.Dark, at + new Vector3(-0.8f, 0.92f, 0f));
            PrimitiveKit.Cylinder("PadMee_ผัดหมี่โคราช", t, at + new Vector3(-0.8f, 1.08f, 0f), new Vector3(0.8f, 0.06f, 0.8f), noodles, collider: false);
            PrimitiveKit.Cylinder("Tray", t, at + new Vector3(0.9f, 0.95f, 0f), new Vector3(0.9f, 0.05f, 0.9f), noodles, collider: false);
        }
        var naem = PrimitiveKit.Mat("PK_Naem", new Color(0.95f, 0.45f, 0.5f), 0.4f);
        var sausage = PrimitiveKit.Mat("PK_Kunchiang", new Color(0.6f, 0.12f, 0.1f), 0.55f);
        for (int i = 0; i < 4; i++) // แหนม and กุนเชียง hanging from rails
        {
            var at = new Vector3(-9f + i * 6f, 0f, 4f);
            PrimitiveKit.Box("Counter", t, at + new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 1.4f), ProvinceKit.Wood);
            PrimitiveKit.Box("Rail", t, at + new Vector3(0f, 2.3f, 0f), new Vector3(3.4f, 0.06f, 0.06f), ProvinceKit.Steel, collider: false);
            for (int k = 0; k < 8; k++)
                PrimitiveKit.Cylinder(k % 2 == 0 ? "Naem_แหนม" : "Kunchiang_กุนเชียง", t, at + new Vector3(-1.5f + k * 0.42f, 1.95f, 0f), new Vector3(0.12f, 0.3f, 0.12f), k % 2 == 0 ? naem : sausage, collider: false);
        }
        Color[] silk = { new Color(0.75f, 0.1f, 0.35f), new Color(0.15f, 0.35f, 0.75f), new Color(0.95f, 0.65f, 0.1f), new Color(0.45f, 0.15f, 0.55f) };
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("Silk_ผ้าไหมปักธงชัย", t, new Vector3(-9f + i * 6f, 1.3f, 7.4f), new Vector3(2.4f, 1.8f, 0.04f), PrimitiveKit.Mat("PK_Silk" + i, silk[i], 0.75f), collider: false);
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, 0f), "market");
    }
}
