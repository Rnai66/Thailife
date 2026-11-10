using UnityEditor;
using UnityEngine;

// นครศรีธรรมราช: เมืองพระ — the town (ProvinceTown) by the sea, with its ten best-known sights:
// วัดพระมหาธาตุวรมหาวิหาร (the great white bell chedi, its spire gilded), ศาลหลักเมือง, หอพระพุทธสิหิงค์,
// the old city wall and ประตูชัยเหนือ, หอพระอิศวร and its giant swing, the shadow-puppet house
// (หนังตะลุง), the market for เครื่องถม, หมู่บ้านคีรีวง under เขาหลวง, น้ำตกกรุงชิง, and อ่าวขนอม with its
// pink dolphins.
public static class ProvinceNakhonSi
{
    public static readonly ProvinceSpec Spec = new ProvinceSpec
    {
        Scene = "NakhonSi", Root = "NakhonSi", Name = "นครศรีธรรมราช",
        Airport = "ท่าอากาศยานนครศรีธรรมราช", Station = "สถานีรถไฟนครศรีธรรมราช",
        MainStreet = "ถนนราชดำเนิน", Monument = "นครศรีธรรมราช · เมืองพระ",
        GantryWest = "↓ วัดพระมหาธาตุฯ · น้ำตกกรุงชิง\n← คีรีวง · กำแพงเมืองเก่า",
        GantryEast = "↑ หอพระพุทธสิหิงค์ · หอพระอิศวร\nตลาดหัวอิฐ · อ่าวขนอม ↑",
        ShopsWest = new[] { "ร้านเครื่องถม", "ขนมจีนเมืองคอน", "ร้านของฝาก", "ร้านกาแฟโบราณ" },
        ShopsEast = new[] { "ร้านทอง", "ร้านหนังตะลุง", "มินิมาร์ท", "ร้านตัดผม" },
        Coast = true, Accent = new Color(0.75f, 0.15f, 0.12f), Airlines = new[] { 3, 0 },
    };

    [MenuItem("Thai Life/Provinces/Build นครศรีธรรมราช")]
    private static void FromMenu() => Build(ask: true);

    internal static void Build(bool ask) => ProvinceTown.Build(Spec, ask, root =>
    {
        WatPhraMahathat(root);
        CityPillar(root);
        PhraBuddhaSihing(root);
        OldWall(root);
        HoPhraIsuan(root);
        ShadowPuppets(root);
        HuaItMarket(root);
        Kiriwong(root);
        KrungChing(root);
        Khanom(root);
    });

    // 1. วัดพระมหาธาตุวรมหาวิหาร: the cloister round the great chedi, small chedis about it, the viharn
    private static void WatPhraMahathat(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "วัดพระมหาธาตุวรมหาวิหาร", new Vector3(-2f, 0f, -60f), 0f,
            "พระบรมธาตุเจดีย์ ยอดทองคำ · มรดกโลก", new Vector3(8f, 0f, 30f), new Vector3(0f, 0f, 25f), new Vector2(14f, 5f), 9);
        PrimitiveKit.Box("Court", t, new Vector3(0f, 0.015f, 0f), new Vector3(40f, 0.02f, 40f), ProvinceKit.Paving, collider: false);
        Cloister(t, 40f, 6f);
        ProvinceKit.Chedi(t, Vector3.zero, 30f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f * Mathf.Deg2Rad;
            ProvinceKit.Chedi(t, new Vector3(Mathf.Sin(a) * 16f, 0f, Mathf.Cos(a) * 16f), 4.2f, ProvinceKit.White, goldTop: true);
        }
        ProvinceKit.ThaiHall(t, new Vector3(-12f, 0f, 27f), 180f, 8f, 10f, ProvinceKit.White, ProvinceKit.RoofRed, "วิหารหลวง");
        ProvinceKit.Path(t, new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, 35f), 5f);
        foreach (var x in new[] { -6f, 6f }) Places.ShadeTree(t, new Vector3(x, 0f, 30f), 0.9f);
    }

    // A walled gallery round a square court, a gate gap in the north side (local +Z)
    private static void Cloister(Transform t, float size, float gate)
    {
        var wall = ProvinceKit.White;
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        float half = size * 0.5f;
        for (int k = 0; k < 4; k++)
        {
            var g = PrimitiveKit.Group("Gallery_พระระเบียง", t, Vector3.zero, k * 90f).transform; // k = 0: the south side, local -Z
            if (k == 2)
                foreach (float s in new[] { -1f, 1f })
                {
                    float len = half - gate * 0.5f;
                    PrimitiveKit.Box("Wall", g, new Vector3(s * (gate * 0.5f + len * 0.5f), 1.6f, -half), new Vector3(len, 3.2f, 0.6f), wall);
                    MeshKit.Place("GalleryRoof", g, gable, ProvinceKit.RoofRed, new Vector3(s * (gate * 0.5f) + (s < 0 ? -len : 0f), 3.2f, -half), new Vector3(0f, 90f, 0f), new Vector3(3.2f, 1.4f, len));
                }
            else
            {
                PrimitiveKit.Box("Wall", g, new Vector3(0f, 1.6f, -half), new Vector3(size, 3.2f, 0.6f), wall);
                MeshKit.Place("GalleryRoof", g, gable, ProvinceKit.RoofRed, new Vector3(-half, 3.2f, -half), new Vector3(0f, 90f, 0f), new Vector3(3.2f, 1.4f, size));
            }
        }
    }

    // 2. ศาลหลักเมืองนครศรีธรรมราช
    private static void CityPillar(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Shrine", "ศาลหลักเมืองนครศรีธรรมราช", new Vector3(-38f, 0f, 31f), 0f,
            "ศิลปะศรีวิชัย · ขอพรให้แคล้วคลาด", new Vector3(6f, 0f, -8f), new Vector3(0f, 0f, -7f), new Vector2(8f, 3f), 4);
        ProvinceKit.SpirePavilion(t, Vector3.zero, 0f, 6f, ProvinceKit.White, ProvinceKit.RoofRed);
        foreach (float x in new[] { -6f, 6f }) Nature.Bush(t, new Vector3(x, 0f, -4f), 0.8f, (int)x + 20, flowers: true);
    }

    // 3. หอพระพุทธสิหิงค์: the hall, the revered Buddha seen through the veranda
    private static void PhraBuddhaSihing(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "หอพระพุทธสิหิงค์", new Vector3(-19f, 0f, 31f), 0f,
            "พระพุทธสิหิงค์ องค์ศักดิ์สิทธิ์คู่เมือง", new Vector3(-7f, 0f, -8f), new Vector3(0f, 0f, -8f), new Vector2(8f, 3f), 4);
        ProvinceKit.ThaiHall(t, new Vector3(0f, 0f, 1f), 0f, 8f, 9f, ProvinceKit.White, ProvinceKit.RoofRed, "หอพระพุทธสิหิงค์");
        BuddhaStatue.Build(t, new Vector3(0f, 0.6f, -4.9f), 0.7f, detail: true, arch: false);
    }

    // 4. กำแพงเมืองเก่า and ประตูชัยเหนือ past the railway: walk through the gate to the sea
    private static void OldWall(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "กำแพงเมืองเก่า ประตูชัยเหนือ", new Vector3(-50f, 0f, 78f), 0f,
            "กำแพงอิฐโบราณ เมืองนครศรีธรรมราช", new Vector3(7f, 0f, -5f), new Vector3(0f, 0f, -6f), new Vector2(12f, 3f), 4);
        ProvinceKit.CityWall(t, Vector3.zero, 0f, 60f, ProvinceKit.Brick, "ประตูชัยเหนือ");
        ProvinceKit.Path(t, new Vector3(0f, 0f, -9f), new Vector3(0f, 0f, 9f), 4f);
    }

    // 5. หอพระอิศวร and the giant swing (เสาชิงช้า)
    private static void HoPhraIsuan(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Temple", "หอพระอิศวร เสาชิงช้า", new Vector3(16f, 0f, 31f), 0f,
            "เทวสถานพราหมณ์ · พิธีโล้ชิงช้า", new Vector3(1f, 0f, -8f), new Vector3(2f, 0f, -6f), new Vector2(10f, 3f), 4);
        ProvinceKit.ThaiHall(t, new Vector3(-5f, 0f, 2f), 0f, 6f, 7f, ProvinceKit.White, ProvinceKit.RoofBlue, "หอพระอิศวร");
        GiantSwing(t, new Vector3(8f, 0f, 1f));
    }

    private static void GiantSwing(Transform t, Vector3 at)
    {
        var red = ProvinceKit.ShrineRed;
        var g = PrimitiveKit.Group("GiantSwing_เสาชิงช้า", t, at).transform;
        PrimitiveKit.Box("Base", g, new Vector3(0f, 0.2f, 0f), new Vector3(7f, 0.4f, 3f), ProvinceKit.Granite);
        foreach (float x in new[] { -2.6f, 2.6f })
        {
            PrimitiveKit.Cylinder("Pillar", g, new Vector3(x, 5.4f, 0f), new Vector3(0.5f, 5f, 0.5f), red);
            PrimitiveKit.Cylinder("Brace", g, new Vector3(x, 1.8f, -0.9f), new Vector3(0.25f, 1.8f, 0.25f), red, new Vector3(-25f, 0f, 0f), collider: false);
            PrimitiveKit.Cylinder("Brace", g, new Vector3(x, 1.8f, 0.9f), new Vector3(0.25f, 1.8f, 0.25f), red, new Vector3(25f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("CrossBeam", g, new Vector3(0f, 10.4f, 0f), new Vector3(7.2f, 0.6f, 0.7f), red, collider: false);
        PrimitiveKit.Box("Cap", g, new Vector3(0f, 10.8f, 0f), new Vector3(7.6f, 0.2f, 0.9f), ProvinceKit.Gold, collider: false);
        foreach (float x in new[] { -0.8f, 0.8f })
            PrimitiveKit.Cylinder("Rope", g, new Vector3(x, 6.4f, 0f), new Vector3(0.05f, 4f, 0.05f), ProvinceKit.Wood, collider: false);
        PrimitiveKit.Box("Seat", g, new Vector3(0f, 2.4f, 0f), new Vector3(2f, 0.12f, 0.5f), ProvinceKit.Wood, collider: false);
    }

    // 6. บ้านหนังตะลุง: a wooden house and the lit screen with the puppets on it
    private static void ShadowPuppets(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "City", "บ้านหนังตะลุง", new Vector3(40f, 0f, 31f), 0f,
            "ศิลปะการแสดงพื้นบ้านปักษ์ใต้", new Vector3(-9f, 0f, -8f), new Vector3(6f, 0f, -6f), new Vector2(8f, 3f), 5);
        ProvinceKit.StiltHouse(t, new Vector3(-6f, 0f, 3f), 0f, 8f, 6f);
        var stage = PrimitiveKit.Group("PuppetStage_โรงหนังตะลุง", t, new Vector3(8f, 0f, 1f)).transform;
        PrimitiveKit.Box("Stage", stage, new Vector3(0f, 0.6f, 1f), new Vector3(6f, 1.2f, 4f), ProvinceKit.Wood);
        foreach (float x in new[] { -2.8f, 2.8f })
            PrimitiveKit.Cylinder("Post", stage, new Vector3(x, 2.6f, -0.8f), new Vector3(0.18f, 1.4f, 0.18f), ProvinceKit.Wood);
        PrimitiveKit.Box("Roof", stage, new Vector3(0f, 4.15f, 1f), new Vector3(6.6f, 0.2f, 4.6f), PrimitiveKit.Mat("PK_Thatch", new Color(0.65f, 0.55f, 0.35f), 0.1f), collider: false);
        PrimitiveKit.Box("Screen", stage, new Vector3(0f, 2.6f, -0.8f), new Vector3(5.2f, 2.6f, 0.05f), PrimitiveKit.Glow("PK_PuppetScreen", new Color(1f, 0.93f, 0.75f), 1.2f), collider: false);
        var black = PrimitiveKit.Mat("PK_Black", new Color(0.05f, 0.05f, 0.05f), 0.3f);
        // two puppets in profile: heads, bodies, the long rods of their arms
        foreach (var (x, lean) in new[] { (-1.2f, 10f), (1.3f, -12f) })
        {
            PrimitiveKit.Sphere("PuppetHead", stage, new Vector3(x, 3.35f, -0.86f), new Vector3(0.35f, 0.4f, 0.02f), black);
            PrimitiveKit.Box("PuppetBody", stage, new Vector3(x, 2.65f, -0.86f), new Vector3(0.45f, 0.95f, 0.02f), black, new Vector3(0f, 0f, lean), collider: false);
            PrimitiveKit.Box("PuppetArm", stage, new Vector3(x + Mathf.Sign(-lean) * 0.4f, 2.8f, -0.86f), new Vector3(0.06f, 0.9f, 0.02f), black, new Vector3(0f, 0f, lean * 4f), collider: false);
            PrimitiveKit.Box("PuppetHat", stage, new Vector3(x, 3.7f, -0.86f), new Vector3(0.12f, 0.35f, 0.02f), black, collider: false);
        }
        PrimitiveKit.Label("หนังตะลุง", stage, new Vector3(0f, 4.5f, -1.4f), 0.05f, new Color(0.55f, 0.1f, 0.05f));
    }

    // 7. ตลาดหัวอิฐ and the nielloware (เครื่องถม) stalls
    private static void HuaItMarket(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Market", "ตลาดหัวอิฐ · เครื่องถมนคร", new Vector3(62f, 0f, 17f), 0f,
            "ของฝาก เครื่องถม ผ้ายก ขนมพื้นเมือง", new Vector3(-12f, 0f, -14f), new Vector3(0f, 0f, -10f), new Vector2(16f, 5f), 7);
        Markets.FreshMarket(t, new Vector3(0f, 0f, 6f), "ตลาดหัวอิฐ", "05:00-18:00");
        TambonBuilder.ShopZone(t, new Vector3(0f, 0f, 6f), "market");
        var niello = new Color(0.12f, 0.1f, 0.08f);
        for (int i = 0; i < 5; i++)
            ProvinceKit.Stall(t, new Vector3(-12f + i * 6f, 0f, -6f), 0f, i % 2 == 0 ? new Color(0.8f, 0.15f, 0.1f) : new Color(0.95f, 0.75f, 0.2f), i % 2 == 0 ? niello : new Color(0.85f, 0.65f, 0.25f));
    }

    // 8. หมู่บ้านคีรีวง: stilt houses along a mountain stream, fruit orchards, เขาหลวง behind
    private static void Kiriwong(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "หมู่บ้านคีรีวง", new Vector3(-70f, 0f, 22f), 0f,
            "หมู่บ้านอากาศดีที่สุดในประเทศไทย · ใต้เขาหลวง", new Vector3(12f, 0f, -18f), new Vector3(4f, 0f, -12f), new Vector2(8f, 6f), 5);
        PrimitiveKit.Box("Stream", t, new Vector3(4f, 0.03f, 0f), new Vector3(3.2f, 0.02f, 44f), ProvinceKit.Water, collider: false);
        for (int i = 0; i < 8; i++)
            Nature.Rock(t, new Vector3(4f + (i % 2 == 0 ? -1.9f : 1.9f), 0f, -19f + i * 5.3f), 0.8f, i + 30);
        ProvinceKit.StiltHouse(t, new Vector3(-6f, 0f, -10f), 90f, 7f, 6f);
        ProvinceKit.StiltHouse(t, new Vector3(-6f, 0f, 6f), 90f, 7f, 6f);
        ProvinceKit.StiltHouse(t, new Vector3(11f, 0f, 14f), -90f, 6f, 5f);
        PrimitiveKit.Box("Footbridge", t, new Vector3(4f, 0.25f, -2f), new Vector3(4.4f, 0.2f, 1.6f), ProvinceKit.Wood);
        for (int i = 0; i < 9; i++) // the orchards: durian, mangosteen, rambutan
            Places.ShadeTree(t, new Vector3(i % 2 == 0 ? -12f : 12f, 0f, -16f + i * 4.2f), 0.75f + (i % 3) * 0.1f);
        ProvinceKit.Mountain(root, new Vector3(-215f, -10f, 40f), new Vector3(170f, 110f, 200f), 1, "เขาหลวง");
        ProvinceKit.Mountain(root, new Vector3(-200f, -15f, -120f), new Vector3(140f, 80f, 150f), 2, "เทือกเขานครศรีธรรมราช");
    }

    // 9. น้ำตกกรุงชิง (facing north, toward the avenue)
    private static void KrungChing(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Nature", "น้ำตกกรุงชิง", new Vector3(-58f, 0f, -58f), 180f,
            "อุทยานแห่งชาติเขาหลวง · น้ำตกหนานฝนแสนห่า", new Vector3(9f, 0f, -12f), new Vector3(0f, 0f, -10f), new Vector2(12f, 4f), 6);
        ProvinceKit.Waterfall(t, Vector3.zero, 0f, 13f, 4);
        ProvinceKit.Path(t, new Vector3(0f, 0f, -9f), new Vector3(0f, 0f, -30f), 3f);
    }

    // 10. อ่าวขนอม: the beach and the pink dolphins out in the bay
    private static void Khanom(Transform root)
    {
        var t = ProvinceKit.Attraction(root, "Beach", "อ่าวขนอม · โลมาสีชมพู", new Vector3(40f, 0f, 104f), 0f,
            "หาดทรายขาว ชมโลมาสีชมพูหายาก", new Vector3(0f, 0f, -2f), new Vector3(0f, 0f, 4f), new Vector2(20f, 5f), 8);
        Color[] canopy = { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.75f, 0.45f) };
        for (int i = 0; i < 4; i++)
            ProvinceKit.Stall(t, new Vector3(-15f + i * 10f, 0f, 3f), 0f, canopy[i], new Color(0.95f, 0.9f, 0.8f));
        for (int i = 0; i < 7; i++)
            ProvinceKit.Dolphin(t, new Vector3(-30f + i * 9f, 0f, 22f + (i % 3) * 7f), i * 50f);
        ProvinceKit.Island(t, new Vector3(-60f, 0f, 130f), new Vector3(60f, 40f, 45f), 1);
    }
}
