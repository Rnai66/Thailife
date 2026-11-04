using UnityEngine;

// Temple life and the haunted places (temple.lua, Scripts/Temple):
//  · monks chanting in the ubosot and the sala, the abbot at his kuti (ordain, disrobe, adopt a dog
//    or raise a กุมารทอง), the amulet pavilion, temple dogs lazing about the grounds
//  · the meditation mat before the Buddha with an incense urn and candles
//  · ป่าช้า: the cemetery behind the temple (through the back gate) — ash stupas, the crematorium,
//    dead trees and a banana clump — and ป่าชุมชน, the community forest NE of the village, with a
//    ตะเคียน tree wrapped in seven-coloured cloth; ghosts come out at both after dark
//  · at the stilt house: the shrine shelf (หิ้งพระ) over the door with what you offered for, the
//    kuman, and the dog's kennel (HomeGuardians)
// BuildTambon adds ป่าตำบล in the tambon scene.
public static class TempleLife
{
    private static readonly Color Robe = new Color(0.93f, 0.52f, 0.12f);
    private static readonly Color AbbotRobe = new Color(0.7f, 0.38f, 0.1f);
    private static readonly Color Skin = new Color(0.8f, 0.6f, 0.45f);

    public static void BuildVillage(Transform v)
    {
        var root = PrimitiveKit.Group("TempleLife_ชีวิตในวัด", v, Vector3.zero).transform;

        // Ubosot hall (world): floor at y 0.74, centre (-32, 24), the Buddha at the back (z ~30)
        foreach (float z in new[] { 21.5f, 23f, 24.5f })
            SeatedMonk(root, new Vector3(-35f, 0.74f, z), -90f);   // the west side, facing across the hall
        Meditation(root, new Vector3(-32f, 0.74f, 27.2f));

        // Sala: two monks chanting
        foreach (float x in new[] { -42.2f, -39.8f })
            SeatedMonk(root, new Vector3(x, 0.5f, 18.6f), 0f);

        Kuti(root, new Vector3(-43.5f, 0f, 24f), -90f);           // west side behind the sala, front faces the ubosot
        AmuletPavilion(root, new Vector3(-41f, 0f, 10.5f), -90f); // front faces the gate path
        TempleDog(root, new Vector3(-24f, 0f, 10f), new Vector2(2.5f, 1.8f), new Color(0.78f, 0.55f, 0.3f), 1);
        TempleDog(root, new Vector3(-45f, 0f, 13.5f), new Vector2(1.4f, 1.8f), new Color(0.2f, 0.18f, 0.16f), 2);

        Cemetery(root);
        CommunityForest(root);
        HomeShrine(v, new Vector3(7f, 0f, 7f));
    }

    public static void BuildTambon(Transform root)
    {
        var f = PrimitiveKit.Group("TambonForest_ป่าตำบล", root, Vector3.zero).transform;
        Places.Sign(f, new Vector3(37f, 0f, -33.5f), "ป่าตำบล\nกลางคืนมีผี · หมอผีปราบได้", 0f);
        Trail(f, new Vector3(37f, 0f, -33f), new Vector3(42f, 0f, -42f));
        foreach (var p in new[] { new Vector3(39f, 0f, -38f), new Vector3(46f, 0f, -36f), new Vector3(46.5f, 0f, -46f), new Vector3(37.5f, 0f, -46.5f) })
            Places.ShadeTree(f, p, 0.9f);
        DeadTree(f, new Vector3(44f, 0f, -40.5f), 1.1f, 3);
        Takhian(f, new Vector3(41f, 0f, -46f));
        BananaClump(f, new Vector3(47f, 0f, -42f));
        for (int i = 0; i < 6; i++) Nature.Bush(f, new Vector3(36.5f + i * 2.1f, 0f, -48f + (i % 2) * 1.2f), 1f, 40 + i);
        Spot(f, new Vector3(43.5f, 0f, -38f), "tambon1", "phrai");
        Spot(f, new Vector3(45.5f, 0f, -43.5f), "tambon2", "pop");
        Spot(f, new Vector3(40.5f, 0f, -44f), "tambon3", "takhian");
    }

    // ── Monks ─────────────────────────────────────────────────────────────────────────────
    // A monk sitting cross-legged in meditation, front toward local -Z (no colliders)
    private static Transform SeatedMonk(Transform parent, Vector3 at, float yaw, bool abbot = false)
    {
        var m = PrimitiveKit.Group(abbot ? "Abbot_เจ้าอาวาส" : "Monk_พระ", parent, at, yaw).transform;
        var robe = PrimitiveKit.Mat(abbot ? "AbbotRobe" : "MonkRobe", abbot ? AbbotRobe : Robe, 0.25f);
        var skin = PrimitiveKit.Mat("MonkSkin", Skin, 0.3f);
        float s = abbot ? 1.1f : 1f;
        PrimitiveKit.Sphere("Lap", m, new Vector3(0f, 0.12f, -0.05f) * s, new Vector3(0.72f, 0.24f, 0.55f) * s, robe);
        PrimitiveKit.Cylinder("Body", m, new Vector3(0f, 0.47f, 0f) * s, new Vector3(0.44f, 0.3f, 0.32f) * s, robe, collider: false);
        PrimitiveKit.Sphere("Shoulders", m, new Vector3(0f, 0.72f, 0f) * s, new Vector3(0.48f, 0.2f, 0.32f) * s, robe);
        PrimitiveKit.Sphere("BareShoulder", m, new Vector3(0.17f, 0.72f, -0.02f) * s, new Vector3(0.14f, 0.13f, 0.14f) * s, skin);
        PrimitiveKit.Box("Sash", m, new Vector3(-0.05f, 0.55f, -0.15f) * s, new Vector3(0.1f, 0.5f, 0.04f) * s,
            PrimitiveKit.Mat("MonkSash", Color.Lerp(abbot ? AbbotRobe : Robe, Color.black, 0.15f)), new Vector3(0f, 0f, -35f), collider: false);
        PrimitiveKit.Sphere("Head", m, new Vector3(0f, 0.95f, 0f) * s, new Vector3(0.25f, 0.29f, 0.26f) * s, skin);
        PrimitiveKit.Sphere("Hands", m, new Vector3(0f, 0.27f, -0.22f) * s, new Vector3(0.18f, 0.08f, 0.12f) * s, skin);
        foreach (float sx in new[] { -1f, 1f }) // closed eyes
            PrimitiveKit.Box("Eye", m, new Vector3(sx * 0.05f, 0.97f, -0.125f) * s, new Vector3(0.05f, 0.008f, 0.01f) * s, PrimitiveKit.Mat("MonkEye", new Color(0.15f, 0.1f, 0.08f)), collider: false);
        if (abbot) // the ceremonial fan (ตาลปัตร) standing beside him
        {
            var wood = PrimitiveKit.Mat("FanHandle", new Color(0.35f, 0.2f, 0.1f));
            PrimitiveKit.Cylinder("FanHandle", m, new Vector3(0.5f, 0.55f, -0.15f), new Vector3(0.03f, 0.55f, 0.03f), wood, collider: false);
            PrimitiveKit.Cylinder("Fan", m, new Vector3(0.5f, 1.2f, -0.15f), new Vector3(0.42f, 0.015f, 0.5f), PrimitiveKit.Mat("FanSilk", new Color(0.75f, 0.12f, 0.12f), 0.5f), new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Cylinder("FanGold", m, new Vector3(0.5f, 1.2f, -0.16f), new Vector3(0.22f, 0.02f, 0.26f), PrimitiveKit.Mat("FanGold", new Color(1f, 0.8f, 0.3f), 0.7f, 0.85f), new Vector3(90f, 0f, 0f), collider: false);
        }
        return m;
    }

    // ── Meditation mat before the Buddha ────────────────────────────────────────────────────
    private static void Meditation(Transform parent, Vector3 at)
    {
        var g = PrimitiveKit.Group("Meditation_ที่นั่งสมาธิ", parent, at).transform;
        PrimitiveKit.Box("Mat", g, new Vector3(0f, 0.015f, 0f), new Vector3(1.1f, 0.03f, 1.1f), PrimitiveKit.Mat("MeditationMat", new Color(0.85f, 0.7f, 0.4f)), collider: false);
        PrimitiveKit.Cylinder("Cushion", g, new Vector3(0f, 0.06f, 0.05f), new Vector3(0.5f, 0.04f, 0.5f), PrimitiveKit.Mat("MeditationCushion", new Color(0.75f, 0.35f, 0.1f)), collider: false);

        // Low offering table with the incense urn (กระถางธูป), two candles and a garland
        var table = PrimitiveKit.Group("OfferingTable", g, new Vector3(0f, 0f, 1.05f)).transform;
        var lacquer = PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.25f));
        PrimitiveKit.Box("Table", table, new Vector3(0f, 0.22f, 0f), new Vector3(1f, 0.06f, 0.42f), lacquer, collider: false);
        foreach (float sx in new[] { -0.45f, 0.45f })
            PrimitiveKit.Box("Leg", table, new Vector3(sx, 0.1f, 0f), new Vector3(0.06f, 0.2f, 0.36f), lacquer, collider: false);
        PrimitiveKit.Cylinder("Urn", table, new Vector3(0f, 0.31f, 0f), new Vector3(0.22f, 0.07f, 0.22f), gold, collider: false);
        PrimitiveKit.Cylinder("Ash", table, new Vector3(0f, 0.38f, 0f), new Vector3(0.18f, 0.005f, 0.18f), PrimitiveKit.Mat("IncenseAsh", new Color(0.7f, 0.68f, 0.64f)), collider: false);
        var candle = PrimitiveKit.Mat("CandleYellow", new Color(1f, 0.9f, 0.5f));
        foreach (float sx in new[] { -0.32f, 0.32f })
        {
            PrimitiveKit.Cylinder("Holder", table, new Vector3(sx, 0.27f, 0f), new Vector3(0.08f, 0.02f, 0.08f), gold, collider: false);
            PrimitiveKit.Cylinder("Candle", table, new Vector3(sx, 0.38f, 0f), new Vector3(0.035f, 0.1f, 0.035f), candle, collider: false);
        }
        PrimitiveKit.Sphere("Garland", table, new Vector3(0f, 0.27f, -0.15f), new Vector3(0.22f, 0.05f, 0.08f), PrimitiveKit.Mat("JasmineGarland", new Color(0.97f, 0.97f, 0.92f)));

        // Lit only while you meditate with incense: three smouldering sticks, two flames, a little light
        var flames = PrimitiveKit.Group("Flames", table, Vector3.zero);
        var stick = PrimitiveKit.Mat("IncenseStick", new Color(0.55f, 0.15f, 0.1f));
        var ember = PrimitiveKit.Glow("IncenseEmber", new Color(1f, 0.35f, 0.1f), 4f);
        for (int i = -1; i <= 1; i++)
        {
            PrimitiveKit.Box("Incense", flames.transform, new Vector3(i * 0.05f, 0.5f, 0f), new Vector3(0.012f, 0.25f, 0.012f), stick, new Vector3(0f, 0f, i * 8f), collider: false);
            PrimitiveKit.Sphere("Ember", flames.transform, new Vector3(i * 0.068f, 0.625f, 0f), Vector3.one * 0.02f, ember);
        }
        var flame = PrimitiveKit.Glow("CandleGlow", new Color(1f, 0.7f, 0.35f), 2f);
        foreach (float sx in new[] { -0.32f, 0.32f })
            PrimitiveKit.Sphere("Flame", flames.transform, new Vector3(sx, 0.52f, 0f), new Vector3(0.04f, 0.08f, 0.04f), flame);
        var lightGo = new GameObject("CandleLight");
        lightGo.transform.SetParent(flames.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.7f, -0.2f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.7f, 0.4f);
        l.range = 3.5f;
        l.intensity = 1.2f;

        // The cushion top: the player sits พับเพียบ with the hips here
        var seat = PrimitiveKit.Group("SeatPoint", g, new Vector3(0f, 0.1f, 0.05f));
        var zone = PrimitiveKit.Group("MeditateZone", g, new Vector3(0f, 1f, -0.2f));
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.6f, 2f, 1.8f);
        zone.AddComponent<MeditationSpot>().Configure(seat.transform, flames);
    }

    // ── The abbot's kuti ────────────────────────────────────────────────────────────────────
    private static void Kuti(Transform parent, Vector3 at, float yaw)
    {
        var k = PrimitiveKit.Group("Kuti_กุฏิเจ้าอาวาส", parent, at, yaw).transform;
        var teak = PrimitiveKit.Mat("WoodPlank", new Color(0.55f, 0.35f, 0.2f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.3f, 0.18f, 0.1f));
        var roof = PrimitiveKit.Mat("TempleOrange", new Color(0.85f, 0.35f, 0.1f));
        foreach (float x in new[] { -1.5f, 1.5f })
        foreach (float z in new[] { -2.2f, 1.4f })
            PrimitiveKit.Cylinder("Stilt", k, new Vector3(x, 0.35f, z), new Vector3(0.18f, 0.35f, 0.18f), dark);
        PrimitiveKit.Box("Floor", k, new Vector3(0f, 0.75f, -0.4f), new Vector3(3.4f, 0.12f, 4f), dark);   // room + front porch
        PrimitiveKit.Box("Walls", k, new Vector3(0f, 1.95f, 0.4f), new Vector3(3.2f, 2.3f, 2.4f), teak);
        PrimitiveKit.Box("Door", k, new Vector3(0f, 1.75f, -0.81f), new Vector3(0.9f, 1.8f, 0.04f), dark, collider: false);
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Box("Window", k, new Vector3(sx * 1.05f, 2.1f, -0.81f), new Vector3(0.6f, 0.6f, 0.04f), dark, collider: false);
        PrimitiveKit.Box("RoofFront", k, new Vector3(0f, 3.55f, -0.55f), new Vector3(3.8f, 0.1f, 2.6f), roof, new Vector3(-32f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", k, new Vector3(0f, 3.55f, 1.35f), new Vector3(3.8f, 0.1f, 2.6f), roof, new Vector3(32f, 0f, 0f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Step", k, new Vector3(1.2f, 0.12f + i * 0.22f, -2.75f + i * 0.28f), new Vector3(0.8f, 0.24f + i * 0.22f, 0.3f), teak);
        // The abbot sits on a mat on the porch, receiving visitors
        PrimitiveKit.Box("Mat", k, new Vector3(-0.3f, 0.82f, -1.6f), new Vector3(1.2f, 0.02f, 1f), PrimitiveKit.Mat("MeditationMat", new Color(0.85f, 0.7f, 0.4f)), collider: false);
        SeatedMonk(k, new Vector3(-0.3f, 0.83f, -1.5f), 0f, abbot: true);
        Places.Sign(k, new Vector3(-1.9f, 0f, -3.2f), "กุฏิเจ้าอาวาส\nบวช · สึก · หมาวัด · กุมารทอง", 0f);

        var zone = PrimitiveKit.Group("AbbotCounter", k, new Vector3(0f, 1f, -3.1f));
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.4f, 2f, 2.2f);
        zone.AddComponent<TempleCounter>().Configure(TempleUI.Abbot);
    }

    // ── Amulet pavilion (ศาลาบูชาพระเครื่อง) ──────────────────────────────────────────────────
    private static void AmuletPavilion(Transform parent, Vector3 at, float yaw)
    {
        var p = PrimitiveKit.Group("AmuletPavilion_ศาลาพระเครื่อง", parent, at, yaw).transform;
        var white = PrimitiveKit.Mat("TempleWhite", new Color(0.96f, 0.94f, 0.9f));
        var roof = PrimitiveKit.Mat("TempleOrange", new Color(0.85f, 0.35f, 0.1f));
        var green = PrimitiveKit.Mat("TempleGreen", new Color(0.1f, 0.45f, 0.3f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.25f));
        var velvet = PrimitiveKit.Mat("AmuletVelvet", new Color(0.55f, 0.06f, 0.08f), 0.3f);
        var glass = PrimitiveKit.Mat("DisplayGlass", new Color(0.8f, 0.92f, 0.95f), 0.9f);

        PrimitiveKit.Box("Platform", p, new Vector3(0f, 0.12f, 0f), new Vector3(4.4f, 0.24f, 3f), white);
        foreach (float x in new[] { -2f, 2f })
        foreach (float z in new[] { -1.3f, 1.3f })
            PrimitiveKit.Cylinder("Post", p, new Vector3(x, 1.5f, z), new Vector3(0.16f, 1.3f, 0.16f), white);
        PrimitiveKit.Box("RoofFront", p, new Vector3(0f, 3.15f, -0.75f), new Vector3(4.9f, 0.1f, 1.9f), roof, new Vector3(-30f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", p, new Vector3(0f, 3.15f, 0.75f), new Vector3(4.9f, 0.1f, 1.9f), green, new Vector3(30f, 0f, 0f));
        PrimitiveKit.Box("Ridge", p, new Vector3(0f, 3.62f, 0f), new Vector3(5f, 0.1f, 0.12f), gold, collider: false);
        foreach (float sx in new[] { -1f, 1f }) // ช่อฟ้า horns
            PrimitiveKit.Box("Chofa", p, new Vector3(sx * 2.5f, 3.85f, 0f), new Vector3(0.08f, 0.5f, 0.08f), gold, new Vector3(0f, 0f, sx * -25f), collider: false);

        // Back board covered in framed amulets
        PrimitiveKit.Box("Board", p, new Vector3(0f, 1.6f, 1.15f), new Vector3(3.2f, 1.8f, 0.08f), velvet);
        var frames = new[] { gold, PrimitiveKit.Mat("SilverFrame", new Color(0.8f, 0.82f, 0.85f), 0.7f, 0.8f), PrimitiveKit.Mat("ClayAmulet", new Color(0.62f, 0.5f, 0.38f)) };
        for (int r = 0; r < 4; r++)
        for (int c = 0; c < 9; c++)
            PrimitiveKit.Cylinder("Amulet", p, new Vector3(-1.35f + c * 0.34f, 0.95f + r * 0.4f, 1.1f), new Vector3(0.16f, 0.01f, 0.22f), frames[(r + c) % 3], new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Label("พระเครื่อง · ผ้ายันต์", p, new Vector3(0f, 2.65f, 1.1f), 0.05f, new Color(1f, 0.85f, 0.4f));

        // Glass counter with amulets and folded yantra cloths on red velvet
        PrimitiveKit.Box("Counter", p, new Vector3(0f, 0.65f, -0.55f), new Vector3(2.8f, 0.8f, 0.6f), PrimitiveKit.Mat("DarkWood", new Color(0.3f, 0.18f, 0.1f)));
        PrimitiveKit.Box("CounterVelvet", p, new Vector3(0f, 1.06f, -0.55f), new Vector3(2.7f, 0.02f, 0.5f), velvet, collider: false);
        PrimitiveKit.Box("CounterGlass", p, new Vector3(0f, 1.2f, -0.55f), new Vector3(2.8f, 0.26f, 0.6f), glass, collider: false);
        for (int c = 0; c < 8; c++)
            PrimitiveKit.Cylinder("Amulet", p, new Vector3(-1.2f + c * 0.34f, 1.085f, -0.55f), new Vector3(0.14f, 0.01f, 0.18f), frames[c % 3], collider: false);
        var cloth = new[] { PrimitiveKit.Mat("YantraWhite", new Color(0.96f, 0.95f, 0.9f)), PrimitiveKit.Mat("YantraRed", new Color(0.8f, 0.12f, 0.1f)), PrimitiveKit.Mat("YantraYellow", new Color(0.98f, 0.82f, 0.3f)) };
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Yantra", p, new Vector3(-1.9f + i * 0.02f, 1.3f + i * 0.35f, 0.9f), new Vector3(0.02f, 0.3f, 0.4f), cloth[i], collider: false);
        SeatedMonk(p, new Vector3(0.6f, 0.24f, 0.2f), 0f);
        Places.Sign(p, new Vector3(-2.6f, 0f, -1.9f), "บูชาพระเครื่อง\nกันผี · กันขโมย", 0f);

        var zone = PrimitiveKit.Group("AmuletCounter", p, new Vector3(0f, 1f, -2.1f));
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.6f, 2f, 2.2f);
        zone.AddComponent<TempleCounter>().Configure(TempleUI.Amulets);
    }

    // ── Dogs ───────────────────────────────────────────────────────────────────────────────
    // A Thai village dog (front toward +Z, as CowWander walks), four LegPivots for its trot
    private static Transform Dog(Transform parent, Vector3 at, Color fur, out Transform head)
    {
        var d = PrimitiveKit.Group("Dog_หมา", parent, at).transform;
        var coat = PrimitiveKit.Mat("DogFur" + ColorUtility.ToHtmlStringRGB(fur), fur, 0.15f);
        var light = PrimitiveKit.Mat("DogFurLight" + ColorUtility.ToHtmlStringRGB(fur), Color.Lerp(fur, Color.white, 0.55f), 0.15f);
        var dark = PrimitiveKit.Mat("DogNose", new Color(0.08f, 0.06f, 0.05f), 0.6f);
        PrimitiveKit.Sphere("Body", d, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.3f, 0.7f), coat);
        PrimitiveKit.Sphere("Chest", d, new Vector3(0f, 0.42f, 0.24f), new Vector3(0.22f, 0.24f, 0.24f), light);
        foreach (var (name, x, z) in new[] { ("LegPivotFL", -0.1f, 0.22f), ("LegPivotFR", 0.1f, 0.22f), ("LegPivotBL", -0.1f, -0.22f), ("LegPivotBR", 0.1f, -0.22f) })
        {
            var hip = PrimitiveKit.Group(name, d, new Vector3(x, 0.38f, z)).transform;
            PrimitiveKit.Box("Leg", hip, new Vector3(0f, -0.19f, 0f), new Vector3(0.07f, 0.38f, 0.08f), coat, collider: false);
            PrimitiveKit.Box("Paw", hip, new Vector3(0f, -0.37f, 0.03f), new Vector3(0.08f, 0.04f, 0.11f), light, collider: false);
        }
        head = PrimitiveKit.Group("Head", d, new Vector3(0f, 0.66f, 0.36f)).transform;
        PrimitiveKit.Sphere("Skull", head, Vector3.zero, new Vector3(0.22f, 0.2f, 0.22f), coat);
        PrimitiveKit.Sphere("Muzzle", head, new Vector3(0f, -0.04f, 0.13f), new Vector3(0.12f, 0.1f, 0.16f), light);
        PrimitiveKit.Sphere("Nose", head, new Vector3(0f, -0.02f, 0.21f), Vector3.one * 0.04f, dark);
        foreach (float sx in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Ear", head, new Vector3(sx * 0.07f, 0.13f, -0.02f), new Vector3(0.06f, 0.11f, 0.03f), coat, new Vector3(0f, 0f, sx * -15f), collider: false);
            PrimitiveKit.Sphere("Eye", head, new Vector3(sx * 0.055f, 0.03f, 0.1f), Vector3.one * 0.03f, dark);
        }
        PrimitiveKit.Box("Tail", d, new Vector3(0f, 0.6f, -0.38f), new Vector3(0.05f, 0.05f, 0.28f), coat, new Vector3(-50f, 0f, 0f), collider: false);
        return d;
    }

    private static void TempleDog(Transform parent, Vector3 at, Vector2 area, Color fur, int seed)
    {
        var d = Dog(parent, at, fur, out var head);
        d.localRotation = Quaternion.Euler(0f, seed * 97f, 0f);
        d.gameObject.AddComponent<CowWander>().Configure(area, head);
    }

    // ── At home: shrine shelf, kuman, yantra cloths, the dog and its kennel ──────────────────
    private static void HomeShrine(Transform v, Vector3 house)
    {
        var h = PrimitiveKit.Group("HomeShrine_หิ้งพระ", v, house).transform;
        var teak = PrimitiveKit.Mat("ShrineTeak", new Color(0.45f, 0.24f, 0.1f), 0.5f);
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.25f));
        // The shelf over the front door (the stilt house's walls face -Z at z -2.3, door top at 4.5)
        PrimitiveKit.Box("Shelf", h, new Vector3(0f, 4.6f, -2.46f), new Vector3(2f, 0.05f, 0.3f), teak, collider: false);
        foreach (float sx in new[] { -0.85f, 0.85f })
            PrimitiveKit.Box("Bracket", h, new Vector3(sx, 4.5f, -2.4f), new Vector3(0.05f, 0.18f, 0.18f), teak, collider: false);
        PrimitiveKit.Box("Valance", h, new Vector3(0f, 4.56f, -2.615f), new Vector3(2f, 0.06f, 0.01f), PrimitiveKit.Mat("ShrineCloth", new Color(0.85f, 0.65f, 0.2f)), collider: false);
        // A small Buddha (always there) and a garland
        var buddha = PrimitiveKit.Mat("BuddhaGold", new Color(0.95f, 0.75f, 0.25f));
        PrimitiveKit.Sphere("BuddhaLap", h, new Vector3(0f, 4.66f, -2.46f), new Vector3(0.16f, 0.06f, 0.11f), buddha);
        PrimitiveKit.Sphere("BuddhaBody", h, new Vector3(0f, 4.75f, -2.45f), new Vector3(0.1f, 0.14f, 0.08f), buddha);
        PrimitiveKit.Sphere("BuddhaHead", h, new Vector3(0f, 4.86f, -2.45f), Vector3.one * 0.06f, buddha);

        var owned = PrimitiveKit.Group("Guardians", h, Vector3.zero);
        var g = owned.transform;

        // Amulets on stands along the shelf, each named after its item id
        string[] amulets = { "phra_pidta", "takrut", "nangphaya", "somdej", "khunphaen", "thuat", "phra_rod", "rian_raek", "phra_kring" };
        Color[] frames = { new Color(0.62f, 0.5f, 0.38f), new Color(0.8f, 0.82f, 0.85f), new Color(0.95f, 0.75f, 0.25f) };
        for (int i = 0; i < amulets.Length; i++)
        {
            float x = i < 5 ? -0.9f + i * 0.13f : 0.4f + (i - 5) * 0.13f;
            var a = PrimitiveKit.Group(amulets[i], g, new Vector3(x, 4.63f, -2.48f)).transform;
            var frame = PrimitiveKit.Mat("ShrineAmulet" + i % 3, frames[i % 3], 0.6f, i % 3 == 0 ? 0f : 0.8f);
            PrimitiveKit.Box("Stand", a, new Vector3(0f, 0.01f, 0.02f), new Vector3(0.06f, 0.02f, 0.05f), teak, collider: false);
            if (amulets[i] == "takrut") // a rolled metal tube on a cord
                PrimitiveKit.Cylinder("Takrut", a, new Vector3(0f, 0.05f, 0f), new Vector3(0.025f, 0.04f, 0.025f), frame, collider: false);
            else
                PrimitiveKit.Cylinder("Frame", a, new Vector3(0f, 0.06f, 0f), new Vector3(0.07f, 0.008f, 0.09f), frame, new Vector3(90f, 0f, 0f), collider: false);
        }

        // Kuman thong (กุมารทอง): a gilded boy with a topknot, kneeling on the shelf
        var kuman = PrimitiveKit.Group("kuman", g, new Vector3(0.22f, 4.625f, -2.47f)).transform;
        var gild = PrimitiveKit.Mat("KumanGold", new Color(0.92f, 0.68f, 0.22f), 0.75f, 0.8f);
        PrimitiveKit.Sphere("Knees", kuman, new Vector3(0f, 0.03f, -0.01f), new Vector3(0.1f, 0.05f, 0.08f), gild);
        PrimitiveKit.Sphere("Body", kuman, new Vector3(0f, 0.1f, 0f), new Vector3(0.08f, 0.1f, 0.06f), gild);
        PrimitiveKit.Box("Sash", kuman, new Vector3(0f, 0.1f, -0.03f), new Vector3(0.08f, 0.015f, 0.01f), PrimitiveKit.Mat("KumanSash", new Color(0.8f, 0.1f, 0.1f)), new Vector3(0f, 0f, 30f), collider: false);
        PrimitiveKit.Sphere("Head", kuman, new Vector3(0f, 0.19f, 0f), Vector3.one * 0.065f, gild);
        PrimitiveKit.Sphere("Topknot", kuman, new Vector3(0f, 0.235f, 0f), Vector3.one * 0.025f, PrimitiveKit.Mat("KumanHair", new Color(0.08f, 0.06f, 0.05f)));
        PrimitiveKit.Sphere("Glow", kuman, new Vector3(0f, 0.12f, -0.02f), Vector3.one * 0.015f, PrimitiveKit.Glow("KumanAura", new Color(1f, 0.8f, 0.4f), 3f));

        // Yantra cloths hang on the porch wall either side of the windows
        var yantras = new (string id, Color cloth, Vector3 at)[]
        {
            ("yant_5", new Color(0.96f, 0.95f, 0.9f), new Vector3(-2.55f, 4.2f, -2.33f)),
            ("yant_8", new Color(0.85f, 0.12f, 0.1f), new Vector3(2.55f, 4.2f, -2.33f)),
            ("yant_kraphet", new Color(0.98f, 0.82f, 0.3f), new Vector3(-2.55f, 3.35f, -2.33f)),
            ("yant_wessuwan", new Color(0.35f, 0.3f, 0.6f), new Vector3(2.55f, 3.35f, -2.33f)),
        };
        var ink = PrimitiveKit.Mat("YantraInk", new Color(0.12f, 0.08f, 0.06f));
        foreach (var (id, color, at) in yantras)
        {
            var y = PrimitiveKit.Group(id, g, at).transform;
            PrimitiveKit.Box("Cloth", y, Vector3.zero, new Vector3(0.38f, 0.5f, 0.01f), PrimitiveKit.Mat("Yantra" + ColorUtility.ToHtmlStringRGB(color), color), collider: false);
            for (int r = 0; r < 4; r++) // rows of script
                PrimitiveKit.Box("Script", y, new Vector3(0f, 0.16f - r * 0.1f, -0.007f), new Vector3(0.28f, 0.02f, 0.003f), ink, collider: false);
            PrimitiveKit.Box("Seal", y, new Vector3(0f, -0.04f, -0.007f), new Vector3(0.1f, 0.1f, 0.003f), ink, new Vector3(0f, 0f, 45f), collider: false);
        }

        // The temple dog's kennel under the house, and the dog trotting about the yard
        var dogGroup = PrimitiveKit.Group("dog", g, Vector3.zero).transform;
        var plank = PrimitiveKit.Mat("WoodPlank", new Color(0.55f, 0.35f, 0.2f));
        var kennel = PrimitiveKit.Group("Kennel", dogGroup, new Vector3(-2.1f, 0f, 1.2f), 90f).transform;
        PrimitiveKit.Box("Floor", kennel, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.1f, 0.9f), plank);
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Box("Side", kennel, new Vector3(sx * 0.38f, 0.35f, 0f), new Vector3(0.05f, 0.6f, 0.9f), plank, collider: false);
        PrimitiveKit.Box("Back", kennel, new Vector3(0f, 0.35f, 0.43f), new Vector3(0.8f, 0.6f, 0.05f), plank, collider: false);
        PrimitiveKit.Box("RoofL", kennel, new Vector3(-0.22f, 0.78f, 0f), new Vector3(0.55f, 0.04f, 1f), PrimitiveKit.Mat("RoofRed", new Color(0.6f, 0.15f, 0.1f)), new Vector3(0f, 0f, 35f), collider: false);
        PrimitiveKit.Box("RoofR", kennel, new Vector3(0.22f, 0.78f, 0f), new Vector3(0.55f, 0.04f, 1f), PrimitiveKit.Mat("RoofRed", new Color(0.6f, 0.15f, 0.1f)), new Vector3(0f, 0f, -35f), collider: false);
        PrimitiveKit.Cylinder("Bowl", kennel, new Vector3(0.2f, 0.04f, -0.6f), new Vector3(0.2f, 0.04f, 0.2f), PrimitiveKit.Mat("DogBowl", new Color(0.2f, 0.4f, 0.8f), 0.6f), collider: false);
        var dog = Dog(dogGroup, new Vector3(-1f, 0f, -6f), new Color(0.82f, 0.6f, 0.35f), out var head);
        dog.gameObject.AddComponent<CowWander>().Configure(new Vector2(2.5f, 2f), head);

        owned.AddComponent<HomeGuardians>();
    }

    // ── The cemetery (ป่าช้า) behind the temple ──────────────────────────────────────────────
    private static void Cemetery(Transform parent)
    {
        var c = PrimitiveKit.Group("Cemetery_ป่าช้า", parent, Vector3.zero).transform;
        Trail(c, new Vector3(-43.5f, 0f, 35.2f), new Vector3(-43.5f, 0f, 46f));
        Trail(c, new Vector3(-43.5f, 0f, 46f), new Vector3(-36f, 0f, 48f));
        Places.Sign(c, new Vector3(-41.5f, 0f, 36.8f), "ป่าช้าหลังวัด\nกลางคืนมีผี · หมอผีปราบได้", 0f);

        var stupa = MeshKit.Lathe("AshStupa", new[]
        {
            new Vector2(0.55f, 0f), new Vector2(0.55f, 0.3f), new Vector2(0.45f, 0.32f), new Vector2(0.45f, 0.55f),
            new Vector2(0.35f, 0.6f), new Vector2(0.38f, 0.85f), new Vector2(0.3f, 1.05f), new Vector2(0.12f, 1.2f),
            new Vector2(0.08f, 1.5f), new Vector2(0.02f, 1.75f),
        }, 12);
        var stone = new[] { PrimitiveKit.Mat("StupaWhite", new Color(0.88f, 0.86f, 0.82f), 0.2f), PrimitiveKit.Mat("StupaGrey", new Color(0.6f, 0.6f, 0.58f), 0.15f),
                            PrimitiveKit.Mat("StupaMoss", new Color(0.5f, 0.55f, 0.45f), 0.1f) };
        var photo = PrimitiveKit.Mat("StupaPhoto", new Color(0.2f, 0.18f, 0.16f), 0.6f);
        var stupas = new[]
        {
            new Vector3(-48f, 0f, 39f), new Vector3(-48f, 0f, 41.6f), new Vector3(-48f, 0f, 44.2f), new Vector3(-46f, 0f, 40.3f),
            new Vector3(-40.5f, 0f, 39.5f), new Vector3(-38.6f, 0f, 41f), new Vector3(-40.5f, 0f, 42.6f), new Vector3(-36.5f, 0f, 39.2f),
            new Vector3(-34.5f, 0f, 41.5f), new Vector3(-29f, 0f, 38.5f), new Vector3(-27f, 0f, 41f), new Vector3(-29.5f, 0f, 49.5f),
            new Vector3(-26.5f, 0f, 47f), new Vector3(-49f, 0f, 54f), new Vector3(-40f, 0f, 55f),
        };
        for (int i = 0; i < stupas.Length; i++)
        {
            float size = 0.8f + (i * 37 % 5) * 0.12f;
            var s = MeshKit.Place("AshStupa_ธาตุ", c, stupa, stone[i % 3], stupas[i], new Vector3(0f, i * 23f, 0f), Vector3.one * size);
            var col = s.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.4f, 0f);
            col.size = new Vector3(1f, 0.8f, 1f);
            PrimitiveKit.Box("Photo", s.transform, new Vector3(0f, 0.43f, -0.455f), new Vector3(0.16f, 0.2f, 0.01f), photo, collider: false);
            if (i % 3 == 0) // old garlands and an incense stub
                PrimitiveKit.Sphere("Garland", s.transform, new Vector3(0f, 0.31f, -0.5f), new Vector3(0.2f, 0.04f, 0.08f), PrimitiveKit.Mat("WiltedGarland", new Color(0.8f, 0.75f, 0.55f)));
        }

        Meru(c, new Vector3(-36f, 0f, 51.5f));
        DharmaSala(c, new Vector3(-47.5f, 0f, 49.5f));
        DeadTree(c, new Vector3(-28.5f, 0f, 54f), 1.4f, 1);
        DeadTree(c, new Vector3(-32f, 0f, 38.5f), 0.9f, 2);
        DeadTree(c, new Vector3(-51f, 0f, 46f), 1.1f, 4);
        BananaClump(c, new Vector3(-30.5f, 0f, 45.5f));
        Places.ShadeTree(c, new Vector3(-44f, 0f, 56f), 1.1f);
        for (int i = 0; i < 8; i++) Nature.Bush(c, new Vector3(-52f + i * 3.6f, 0f, 57.5f - (i % 2) * 1.4f), 1.1f, 60 + i);

        Spot(c, new Vector3(-31.5f, 0f, 43.6f), "cem1", "krasue");
        Spot(c, new Vector3(-36f, 0f, 47.6f), "cem2", "pop");
        Spot(c, new Vector3(-27.5f, 0f, 51.5f), "cem3", "pret");
        Spot(c, new Vector3(-45.5f, 0f, 46.8f), "cem4", "nak");
    }

    // Crematorium (เมรุ): a white hall on a plinth with a tiered roof, spire and a tall chimney
    private static void Meru(Transform parent, Vector3 at)
    {
        var m = PrimitiveKit.Group("Meru_เมรุ", parent, at).transform;
        var white = PrimitiveKit.Mat("TempleWhite", new Color(0.96f, 0.94f, 0.9f));
        var grey = PrimitiveKit.Mat("MeruGrey", new Color(0.55f, 0.55f, 0.55f), 0.2f);
        var roof = PrimitiveKit.Mat("TempleOrange", new Color(0.85f, 0.35f, 0.1f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.25f));
        PrimitiveKit.Box("Plinth", m, new Vector3(0f, 0.3f, 0f), new Vector3(6f, 0.6f, 5f), white);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Stair", m, new Vector3(0f, 0.1f + i * 0.1f, -2.8f + i * 0.2f), new Vector3(2f, 0.2f + i * 0.2f, 0.3f), white);
        PrimitiveKit.Box("Hall", m, new Vector3(0f, 1.8f, 0.3f), new Vector3(4.2f, 2.4f, 3.4f), white);
        PrimitiveKit.Box("Door", m, new Vector3(0f, 1.5f, -1.42f), new Vector3(1.3f, 1.8f, 0.04f), PrimitiveKit.Mat("MeruDoor", new Color(0.12f, 0.1f, 0.09f)), collider: false);
        PrimitiveKit.Box("DoorFrame", m, new Vector3(0f, 2.48f, -1.43f), new Vector3(1.5f, 0.14f, 0.04f), gold, collider: false);
        for (int i = 0; i < 2; i++)
        {
            float w = 5f - i * 1.4f, y = 3.4f + i * 0.9f;
            PrimitiveKit.Box("RoofFront", m, new Vector3(0f, y, 0.3f - 0.9f + i * 0.25f), new Vector3(w, 0.1f, 2.4f - i * 0.5f), roof, new Vector3(-35f, 0f, 0f));
            PrimitiveKit.Box("RoofBack", m, new Vector3(0f, y, 0.3f + 0.9f - i * 0.25f), new Vector3(w, 0.1f, 2.4f - i * 0.5f), roof, new Vector3(35f, 0f, 0f));
        }
        PrimitiveKit.Cylinder("Spire", m, new Vector3(0f, 5.6f, 0.3f), new Vector3(0.12f, 0.8f, 0.12f), gold, collider: false);
        PrimitiveKit.Cylinder("Chimney", m, new Vector3(0f, 4f, 2.3f), new Vector3(0.6f, 4f, 0.6f), grey);
        PrimitiveKit.Cylinder("ChimneyCap", m, new Vector3(0f, 8.05f, 2.3f), new Vector3(0.75f, 0.1f, 0.75f), grey, collider: false);
    }

    // ศาลาธรรมสังเวช: a small open shelter for funeral rites, a little run down
    private static void DharmaSala(Transform parent, Vector3 at)
    {
        var s = PrimitiveKit.Group("DharmaSala_ศาลาธรรมสังเวช", parent, at, 90f).transform;
        var wood = PrimitiveKit.Mat("OldWood", new Color(0.4f, 0.32f, 0.25f));
        var roof = PrimitiveKit.Mat("RustyZinc", new Color(0.5f, 0.38f, 0.3f), 0.3f);
        PrimitiveKit.Box("Floor", s, new Vector3(0f, 0.2f, 0f), new Vector3(3.6f, 0.4f, 2.6f), PrimitiveKit.Mat("MeruGrey", new Color(0.55f, 0.55f, 0.55f), 0.2f));
        foreach (float x in new[] { -1.6f, 1.6f })
        foreach (float z in new[] { -1.1f, 1.1f })
            PrimitiveKit.Cylinder("Post", s, new Vector3(x, 1.4f, z), new Vector3(0.12f, 1.1f, 0.12f), wood);
        PrimitiveKit.Box("RoofFront", s, new Vector3(0f, 2.8f, -0.65f), new Vector3(4f, 0.06f, 1.7f), roof, new Vector3(-28f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", s, new Vector3(0.15f, 2.75f, 0.65f), new Vector3(4f, 0.06f, 1.7f), roof, new Vector3(30f, 0f, 3f)); // sagging
        PrimitiveKit.Box("Bench", s, new Vector3(0f, 0.6f, 0.8f), new Vector3(2.8f, 0.08f, 0.5f), wood);
    }

    // ── Community forest (ป่าชุมชน), NE of the village ───────────────────────────────────────
    private static void CommunityForest(Transform parent)
    {
        var f = PrimitiveKit.Group("CommunityForest_ป่าชุมชน", parent, Vector3.zero).transform;
        Trail(f, new Vector3(36f, 0f, 24f), new Vector3(40f, 0f, 36f));
        Trail(f, new Vector3(40f, 0f, 36f), new Vector3(47f, 0f, 46f));
        Trail(f, new Vector3(47f, 0f, 46f), new Vector3(44f, 0f, 54f));
        Places.Sign(f, new Vector3(38.5f, 0f, 27f), "ป่าชุมชน\nกลางคืนมีผี · หมอผีปราบได้", 0f);
        foreach (var p in new[]
        {
            new Vector3(36f, 0f, 40f), new Vector3(52f, 0f, 37f), new Vector3(55f, 0f, 47f), new Vector3(38f, 0f, 52f),
            new Vector3(50.5f, 0f, 55.5f), new Vector3(43f, 0f, 41f), new Vector3(56f, 0f, 30f),
        })
            Places.ShadeTree(f, p, 0.85f + (p.x * 7 % 3) * 0.1f);
        Takhian(f, new Vector3(51f, 0f, 50f));
        BananaClump(f, new Vector3(39.5f, 0f, 46.5f));
        DeadTree(f, new Vector3(46f, 0f, 35f), 1f, 5);
        for (int i = 0; i < 10; i++) Nature.Bush(f, new Vector3(35f + (i * 2.3f) % 22f, 0f, 33f + (i * 5.7f) % 24f), 0.9f + (i % 3) * 0.2f, 80 + i);
        for (int i = 0; i < 4; i++) Nature.Rock(f, new Vector3(37f + i * 5f, 0f, 57f - (i % 2) * 20f), 0.7f + (i % 2) * 0.4f, 90 + i);

        Spot(f, new Vector3(45.5f, 0f, 40f), "forest1", "krahang");
        Spot(f, new Vector3(41.5f, 0f, 47.5f), "forest2", "tani");
        Spot(f, new Vector3(49f, 0f, 48f), "forest3", "takhian");
    }

    // ── Pieces ─────────────────────────────────────────────────────────────────────────────
    private static void Spot(Transform parent, Vector3 at, string id, string ghost)
    {
        var g = PrimitiveKit.Group("GhostSpot_" + id, parent, at);
        var model = Ghosts.Build(g.transform, new Vector3(0f, 0.3f, 0f), ghost);
        var col = g.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4f;
        col.center = Vector3.up;
        g.AddComponent<GhostSpot>().Configure(id, ghost, model);
    }

    private static void Trail(Transform parent, Vector3 from, Vector3 to)
    {
        var d = to - from;
        var mid = (from + to) * 0.5f;
        float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        PrimitiveKit.Box("Trail", parent, new Vector3(mid.x, 0.011f, mid.z), new Vector3(1.6f, 0.02f, d.magnitude + 1f),
            PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f)), new Vector3(0f, yaw, 0f), collider: false);
    }

    // A bare, twisted dead tree
    private static void DeadTree(Transform parent, Vector3 at, float size, int seed)
    {
        var t = PrimitiveKit.Group("DeadTree_ต้นไม้ตาย", parent, at, seed * 71f).transform;
        t.localScale = Vector3.one * size;
        var bark = PrimitiveKit.Mat("DeadBark", new Color(0.3f, 0.27f, 0.25f), 0.1f);
        PrimitiveKit.Cylinder("Trunk", t, new Vector3(0f, 1.6f, 0f), new Vector3(0.38f, 1.6f, 0.38f), bark, new Vector3(4f, 0f, -3f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f + seed * 13f, tilt = 35f + (i * 17 + seed * 11) % 25;
            var dir = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(tilt, 0f, 0f);
            var start = new Vector3(0f, 2.2f + (i % 3) * 0.5f, 0f);
            float len = 1.1f + (i % 2) * 0.5f;
            var go = PrimitiveKit.Cylinder("Branch", t, start + dir * Vector3.up * len * 0.5f, new Vector3(0.1f, len * 0.5f, 0.1f), bark, collider: false);
            go.transform.localRotation = dir;
            var tip = start + dir * Vector3.up * len;
            var twig = Quaternion.Euler(0f, a + 40f, 0f) * Quaternion.Euler(tilt + 20f, 0f, 0f);
            var tw = PrimitiveKit.Cylinder("Twig", t, tip + twig * Vector3.up * 0.3f, new Vector3(0.05f, 0.3f, 0.05f), bark, collider: false);
            tw.transform.localRotation = twig;
        }
    }

    // The ตะเคียน: a great tree wrapped in seven-coloured cloth (ผ้าแพรเจ็ดสี), a spirit shelf and offerings at its foot
    private static void Takhian(Transform parent, Vector3 at)
    {
        var tree = Places.ShadeTree(parent, at, 1.3f);
        tree.name = "Takhian_ต้นตะเคียน";
        var t = tree.transform;
        Color[] colors =
        {
            new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.7f, 0.3f),
            new Color(0.2f, 0.5f, 0.9f), new Color(0.6f, 0.3f, 0.8f), new Color(1f, 0.5f, 0.7f),
        };
        for (int i = 0; i < colors.Length; i++)
            PrimitiveKit.Cylinder("Cloth", t, new Vector3(0f, 0.55f + i * 0.09f, 0f), new Vector3(0.62f - i * 0.012f, 0.045f, 0.62f - i * 0.012f),
                PrimitiveKit.Mat("SevenCloth" + i, colors[i], 0.5f), collider: false);
        var wood = PrimitiveKit.Mat("SpiritShelf", new Color(0.5f, 0.3f, 0.15f));
        PrimitiveKit.Box("Shelf", t, new Vector3(0f, 0.8f, -0.55f), new Vector3(0.6f, 0.04f, 0.3f), wood, collider: false);
        PrimitiveKit.Box("ShelfLeg", t, new Vector3(0f, 0.4f, -0.6f), new Vector3(0.05f, 0.8f, 0.05f), wood, collider: false);
        PrimitiveKit.Box("Dress", t, new Vector3(-0.15f, 0.92f, -0.55f), new Vector3(0.1f, 0.2f, 0.06f), PrimitiveKit.Mat("ThaiDress", new Color(0.85f, 0.15f, 0.4f)), collider: false); // offered Thai dresses
        PrimitiveKit.Box("Dress", t, new Vector3(0.1f, 0.92f, -0.55f), new Vector3(0.1f, 0.2f, 0.06f), PrimitiveKit.Mat("ThaiDressGreen", new Color(0.2f, 0.6f, 0.35f)), collider: false);
        PrimitiveKit.Sphere("Garland", t, new Vector3(0.25f, 0.85f, -0.55f), new Vector3(0.12f, 0.04f, 0.08f), PrimitiveKit.Mat("JasmineGarland", new Color(0.97f, 0.97f, 0.92f)));
    }

    private static void BananaClump(Transform parent, Vector3 at)
    {
        foreach (var o in new[] { new Vector3(0f, 0f, 0f), new Vector3(1.2f, 0f, 0.6f), new Vector3(-0.8f, 0f, 1.1f), new Vector3(0.4f, 0f, -1.1f) })
            Places.BananaTree(parent, at + o, trainable: false);
    }
}
