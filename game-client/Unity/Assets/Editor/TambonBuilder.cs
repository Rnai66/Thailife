using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menus: Thai Life > Build Tambon Scene (ตำบล) / Build City Scene (เมือง)
// Each creates a location scene with its own Network (connection, match, career, fishing),
// mobile controls, a main road and a songthaew stop for travelling.
public static class TambonBuilder
{
    [MenuItem("Thai Life/Build Tambon Scene (ตำบล)")]
    private static void BuildTambonFromMenu() => BuildTambon(ask: true);

    [MenuItem("Thai Life/Build City Scene (เมือง)")]
    private static void BuildCityFromMenu() => BuildCity(ask: true);

    [MenuItem("Thai Life/Build Beach Scene (ทะเล)")]
    private static void BuildBeachFromMenu() => BuildBeach(ask: true);

    [MenuItem("Thai Life/Build Island Scene (เกาะ)")]
    private static void BuildIslandFromMenu() => BuildIsland(ask: true);

    // เกาะ: Maya Bay-style lagoon with cliffs, tyre swing, beach bars, treasure chests; ferry back to the mainland
    internal static void BuildIsland(bool ask) => BuildLocation("Assets/Scenes/Island.unity", "Island", 10f, ask, root =>
    {
        Island.Build(root);
        ShopZone(root, new Vector3(-6f, 0f, 2.6f), "beach");                                   // บาร์ริมหาด
        ShopZone(root, new Vector3(31f, 0f, 1f) + Quaternion.Euler(0f, -12f, 0f) * new Vector3(0f, 0f, 2.3f), "community"); // บาร์กระท่อม: sell fish, buy rods
    }, road: false);

    // ทะเล: beach, longtail boats, fishing pier with sea fishing, umbrellas to rest under
    internal static void BuildBeach(bool ask) => BuildLocation("Assets/Scenes/Beach.unity", "Beach", 10f, ask, root =>
    {
        Seaside.Beach(root);
        ShopZone(root, new Vector3(-40f, 0f, 2.8f), "beach");
        ShopZone(root, new Vector3(-28f, 0f, -13f), "beach");      // ร้านอาหารทะเลสด
        ShopZone(root, new Vector3(-18.5f, 0f, -13.2f), "community"); // ร้านสะดวกซื้อ: sell your sea fish, buy rods
        ShopZone(root, new Vector3(9.8f, 0f, -13f), "noodle");        // ก๋วยเตี๋ยว ส้มตำ
        ShopZone(root, new Vector3(26f, 0f, -13f), "beach");        // ร้านอาหารริมหาด
        Places.BusStop(root, new Vector3(16f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(19f, 0f, -11f), "ไปหมู่บ้าน / ตำบล / เมือง\n(ยืนใต้ป้ายรถ)", 180f);
        StreetFurniture.PoleLine(root, new[] { new Vector3(-40f, 0f, -10.6f), new Vector3(-12f, 0f, -10.6f), new Vector3(24f, 0f, -10.6f), new Vector3(46f, 0f, -10.6f) }, 180f, lamps: true);
    });

    internal static void BuildTambon(bool ask) => BuildLocation("Assets/Scenes/Tambon.unity", "Tambon", 10f, ask, root =>
    {
        // ตำบล: SAO office, health station, school, public park, canal with fishing
        Places2.SaoOffice(root, new Vector3(-24f, 0f, 10f), "อบต.");
        Places.HealthStation(root, new Vector3(24f, 0f, 9f), 0f, "รพ.สต.");
        Places.School(root, new Vector3(-10f, 0f, -28f), "โรงเรียน", 180f);
        Lane(root, -10f, -15f, -10f);
        Places2.Park(root, new Vector3(2f, 0f, 14f), "สวนสาธารณะ");
        Places2.Canal(root, new Vector3(0f, 0f, 34f), 100f, new[] { -18f, -6f, 8f, 20f });
        Lane(root, 2f, -6f, 4f);

        Npcs.Crowd(root, new Vector3(2f, 0f, 14f), new Vector2(14f, 14f), 4, "", 7, 19, Npcs.VillageTalk); // park

        // Fairground for the small festivals (งานวัดเล็ก: มาฆบูชา วิสาขบูชา เข้าพรรษา ผ้าป่า)
        FestivalGrounds.Build(root, new Vector3(-36f, 0f, -30f), 180f, big: false);
        Lane(root, -36f, -17.5f, -10f);

        // Tambon event boxing ring (เวทีมวยงานตำบล), entrance facing the road
        Places.BoxingRing(root, new Vector3(38f, 0f, 18f), 0f, "เวทีมวยงานตำบล", "tambon");
        StreetFurniture.LampPost(root, new Vector3(34.5f, 0f, 13.2f));
        StreetFurniture.LampPost(root, new Vector3(41.5f, 0f, 13.2f));

        // ตลาดนัด south of the road, entrance facing it (evening market, 16:00-22:00)
        Markets.FleaMarket(root, new Vector3(28f, 0f, -28f), "ตลาดนัด", "16:00-22:00", 180f);
        ShopZone(root, new Vector3(28f, 0f, -28f), "flea");
        Lane(root, 28f, -21f, -10f);

        Places.BusStop(root, new Vector3(16f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(19f, 0f, -11f), "ไปหมู่บ้าน / เมือง\n(ยืนใต้ป้ายรถ)", 180f);
        Places.Sign(root, new Vector3(-3f, 0f, -4.5f), "ตำบล", 0f);
        Places.Sign(root, new Vector3(8f, 0f, 27f), "คลอง\nมีปลาให้ตก", 0f);

        foreach (var pos in new[] { new Vector3(-12f, 0f, -44f), new Vector3(40f, 0f, -22f), new Vector3(-42f, 0f, 20f), new Vector3(42f, 0f, 24f), new Vector3(14f, 0f, -30f) })
            Places.ShadeTree(root, pos);

        // Power line with street lamps along the north side of the road
        StreetFurniture.PoleLine(root, new[]
        {
            new Vector3(-40f, 0f, -5.5f), new Vector3(-12f, 0f, -5.5f), new Vector3(16f, 0f, -5.5f), new Vector3(44f, 0f, -5.5f),
        }, 0f, lamps: true);
        // One lamp at the ตลาดนัด entrance, bulbs along its aisle
        StreetFurniture.PoleLine(root, new[] { new Vector3(30.2f, 0f, -20.5f) }, 90f, lamps: true);
        StreetFurniture.HangingLight(root, new Vector3(28f, 2.5f, -25f));
        StreetFurniture.HangingLight(root, new Vector3(28f, 2.5f, -31f));

        // Lamp posts at the SAO office, health station, school and around the park
        foreach (var p in new[]
        {
            new Vector3(-27.5f, 0f, 3.2f), new Vector3(-20.5f, 0f, 3.2f),
            new Vector3(19.5f, 0f, 4f), new Vector3(28.5f, 0f, 4f),
            new Vector3(-15f, 0f, -23.5f), new Vector3(-5f, 0f, -23.5f),
            new Vector3(-4.4f, 0f, 7.6f), new Vector3(8.4f, 0f, 7.6f), new Vector3(-4.4f, 0f, 20.4f), new Vector3(8.4f, 0f, 20.4f),
        })
            StreetFurniture.LampPost(root, p);
        // Solar lamps along the canal bank by the fishing spots, facing the water
        foreach (float x in new[] { -16f, -4f, 10f, 22f })
            StreetFurniture.SolarLamp(root, new Vector3(x, 0f, 29.3f), 180f);

        // Palms along the road and the canal bank, shrubs by the buildings, grass everywhere else
        var palms = new[]
        {
            new Vector3(-36f, 0f, -4.5f), new Vector3(-12f, 0f, -12.5f), new Vector3(6f, 0f, -4.5f), new Vector3(30f, 0f, -12.5f),
            new Vector3(-30f, 0f, 27f), new Vector3(-12f, 0f, 26.5f), new Vector3(14f, 0f, 26.5f), new Vector3(32f, 0f, 27f),
        };
        for (int i = 0; i < palms.Length; i++) Nature.CoconutPalm(root, palms[i], 0.9f + (i % 3) * 0.12f, i);
        var bushes = new[]
        {
            new Vector3(-30f, 0f, 2f), new Vector3(-18f, 0f, 2f), new Vector3(18f, 0f, 2f), new Vector3(30f, 0f, 2f),
            new Vector3(-16f, 0f, -16f), new Vector3(-4f, 0f, -16f), new Vector3(-6f, 0f, 8f), new Vector3(10f, 0f, 8f),
        };
        for (int i = 0; i < bushes.Length; i++) Nature.Bush(root, bushes[i], 1f + (i % 2) * 0.3f, i + 20, flowers: i % 2 == 0);
        Nature.GrassField(root, Vector3.zero, new Vector2(96f, 96f), 1800, 13,
            new Vector4(-50f, -10.6f, 50f, -5.4f),   // road
            new Vector4(-50f, 27.5f, 50f, 41f),      // canal
            new Vector4(-35f, 0f, -13f, 20f),        // SAO office
            new Vector4(13f, 0f, 35f, 18f),          // health station
            new Vector4(-26f, -42f, 6f, -15f),       // school
            new Vector4(21f, -36f, 35f, -10f),       // ตลาดนัด and its lane
            new Vector4(34.5f, 13f, 41.5f, 21.5f),   // boxing ring
            new Vector4(-49.5f, -43.5f, -22.5f, -10f), // fairground and its lane
            new Vector4(-11.3f, -15f, -8.7f, -10f), new Vector4(0.7f, -6f, 3.3f, 4f));
    });

    internal static void BuildCity(bool ask) => BuildLocation("Assets/Scenes/City.unity", "City", 10f, ask, root =>
    {
        // ตัวเมือง (อำเภอ/จังหวัด): clock tower roundabout, shophouses, boxing camp (career tier 3)
        Places2.ClockTower(root, new Vector3(0f, 0f, 14f));
        Places2.Shophouses(root, new Vector3(-20f, 0f, 2f), 0f, 5, new[] { "ร้านทอง", "ร้านขายยา", "", "ร้านข้าวมันไก่", "ร้านโทรศัพท์" });
        Places2.Shophouses(root, new Vector3(22f, 0f, 2f), 0f, 4, new[] { "ร้านกาแฟ", "", "ร้านตัดผม", "ร้านเครื่องดนตรี" });
        // Walk up to ร้านข้าวมันไก่ (left row, 4th unit) or ร้านกาแฟ (right row, 1st unit) to buy food
        FoodCounter(root, new Vector3(-15.5f, 0f, -4.4f));
        FoodCounter(root, new Vector3(15.25f, 0f, -4.4f));
        ShopZone(root, new Vector3(28.75f, 0f, -4.4f), "music");   // ร้านเครื่องดนตรี (right row, 4th unit)

        // Music venues: city event stage by the clock tower (Lv 8) and the pub (Lv 10, night)
        Places2.MusicStage(root, new Vector3(10f, 0f, 22f), 90f, "city_event", "เวทีอีเวนต์เมือง", new Color(0.15f, 0.3f, 0.6f));
        // Pub street (ย่านผับ): three pubs by style, open at night, each playable once per game day
        Places2.Pub(root, new Vector3(-22f, 0f, 22f), 0f, "pub_puea_chiwit", "ผับเพื่อชีวิต", new Color(0.35f, 0.22f, 0.12f), new Color(1f, 0.7f, 0.25f));
        Places2.Pub(root, new Vector3(-36f, 0f, 22f), 0f, "pub_blues", "ผับบลูส์", new Color(0.1f, 0.14f, 0.3f), new Color(0.35f, 0.65f, 1f));
        Places2.Pub(root, new Vector3(26f, 0f, 24f), 0f, "pub_indie", "ผับอินดี้", new Color(0.18f, 0.12f, 0.25f), new Color(1f, 0.35f, 0.8f));
        Places.Sign(root, new Vector3(-16f, 0f, 16f), "ย่านผับ · เพื่อชีวิต บลูส์ อินดี้\nนักดนตรี Lv 10 · กลางคืน ร้านละครั้ง", 0f);

        Places.BoxingCamp(root, new Vector3(0f, 0f, 34f), "ค่ายมวย");

        // ตลาดสดเทศบาล east of the clock tower (morning market, 05:00-13:00)
        Markets.FreshMarket(root, new Vector3(39f, 0f, 11f), "ตลาดสดเทศบาล", "05:00-13:00");
        ShopZone(root, new Vector3(39f, 0f, 11f), "market");
        Places.Sign(root, new Vector3(34f, 0f, 4.2f), "ตลาดสด →\nเปิด 05:00-13:00", 0f);

        // Cross street up to the camp, past the clock tower
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        PrimitiveKit.Box("CrossStreet", root, new Vector3(0f, 0.02f, 12f), new Vector3(6f, 0.04f, 36f), asphalt);

        Places.BusStop(root, new Vector3(9f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(12f, 0f, -11f), "ไปหมู่บ้าน / ตำบล\n(ยืนใต้ป้ายรถ)", 180f);
        Places.Sign(root, new Vector3(-4f, 0f, 24f), "ค่ายมวย →\nซ้อม Lv 6+", 0f);

        foreach (var pos in new[] { new Vector3(-8f, 0f, -16f), new Vector3(8f, 0f, -16f), new Vector3(-40f, 0f, 20f), new Vector3(40f, 0f, 22f) })
            Places.ShadeTree(root, pos);

        Npcs.Crowd(root, new Vector3(0f, 0f, -4.4f), new Vector2(76f, 1.2f), 6, "", 6, 22, Npcs.CityTalk);   // pavement along the shops
        Npcs.Crowd(root, new Vector3(0f, 0f, 22f), new Vector2(10f, 6f), 3, "", 7, 21, Npcs.CityTalk);       // by the clock tower

        // Noodle shop (ร้านก๋วยเตี๋ยว) by the fresh market, skytrain (รถไฟฟ้า) over the main road
        Seaside.NoodleShop(root, new Vector3(36f, 0f, 20f), 180f);
        ShopZone(root, new Vector3(36f, 0f, 18.2f), "noodle");
        Seaside.SkyTrainLine(root);

        // Hospital, hotel, police station and pawn shop (north row); district office and อบจ. (south of the road)
        CityCivic.Build(root);

        // Fairground for the big festivals (งานใหญ่: งานประจำปีวัด สงกรานต์ ออกพรรษา กฐิน ลอยกระทง)
        FestivalGrounds.Build(root, new Vector3(-34.5f, 0f, -30f), 180f, big: true); // beside the town temple
        PrimitiveKit.Box("FairPath", root, new Vector3(-34.5f, 0.01f, -13.5f), new Vector3(4f, 0.012f, 7f),
            PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f)), collider: false);

        // Power line on the south side of the main road (the shop awnings fill the north side),
        // and lamp posts along the street up to the boxing camp
        StreetFurniture.PoleLine(root, new[]
        {
            new Vector3(-38f, 0f, -10.6f), new Vector3(-10f, 0f, -10.6f), new Vector3(18f, 0f, -10.6f), new Vector3(46f, 0f, -10.6f),
        }, 180f, lamps: true);
        StreetFurniture.PoleLine(root, new[] { new Vector3(-3.6f, 0f, 0.5f) }, -90f, lamps: true);
        StreetFurniture.PoleLine(root, new[] { new Vector3(3.6f, 0f, 25.5f) }, 90f, lamps: true);

        // Lamp posts at the boxing camp, the event stage and in front of each pub; bulbs in the market hall
        foreach (var p in new[]
        {
            new Vector3(-6f, 0f, 28.5f), new Vector3(6f, 0f, 28.5f), new Vector3(13f, 0f, 18f),
            new Vector3(-22f, 0f, 18.5f), new Vector3(-36f, 0f, 18.5f), new Vector3(26f, 0f, 20.5f),
        })
            StreetFurniture.LampPost(root, p);
        StreetFurniture.HangingLight(root, new Vector3(35f, 3.6f, 11f));
        StreetFurniture.HangingLight(root, new Vector3(43f, 3.6f, 11f));

        // Paved town centre with street palms in planters along both streets
        PrimitiveKit.Box("Pavement", root, new Vector3(0f, 0.008f, 19f), new Vector3(96f, 0.012f, 49f),
            PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f)), collider: false);
        var planter = PrimitiveKit.Mat("Planter", new Color(0.82f, 0.8f, 0.76f));
        var streetPalms = new[]
        {
            new Vector3(-34f, 0f, -4.6f), new Vector3(-6f, 0f, -4.6f), new Vector3(6f, 0f, -4.6f), new Vector3(36f, 0f, -4.6f),
            new Vector3(-4.2f, 0f, 6f), new Vector3(4.2f, 0f, 6f), new Vector3(-4.2f, 0f, 20f), new Vector3(4.2f, 0f, 20f),
        };
        for (int i = 0; i < streetPalms.Length; i++)
        {
            PrimitiveKit.Box("Planter", root, streetPalms[i] + new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.5f, 1.2f), planter);
            Nature.CoconutPalm(root, streetPalms[i] + new Vector3(0f, 0.5f, 0f), 0.85f + (i % 3) * 0.1f, i + 3);
        }
        foreach (var pos in new[] { new Vector3(-10f, 0f, 30f), new Vector3(10f, 0f, 30f), new Vector3(-30f, 0f, 14f), new Vector3(-44f, 0f, 4f) })
            Nature.Bush(root, pos, 1.2f, (int)(pos.x + pos.z), flowers: true);
    });

    private static void BuildLocation(string scenePath, string rootName, float groundScale, bool ask, Action<Transform> content, bool road = true)
    {
        if (ask && File.Exists(scenePath) &&
            !EditorUtility.DisplayDialog("สร้างฉาก", $"{Path.GetFileName(scenePath)} มีอยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?\n(ไม่กระทบฉากอื่น)", "สร้างใหม่", "ยกเลิก"))
            return;

        if (ask && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = ThaiVillageSceneBuilder.CreateBaseScene();
        GameObject.Find("Ground").transform.localScale = new Vector3(groundScale, 1f, groundScale);

        NakamaSetup.Setup();
        MobileControlsSetup.EnsureMobileControls();

        var root = new GameObject(rootName).transform;
        if (road) MainRoad(root, groundScale * 10f);
        content(root);

        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        EditorSceneManager.SaveScene(scene, scenePath);
        ThaiVillageSceneBuilder.AddSceneToBuildSettings(scenePath);

        Debug.Log($"[ThaiLife] Built {scenePath}.");
    }

    private static void MainRoad(Transform root, float length)
    {
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var paint = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        PrimitiveKit.Box("Road", root, new Vector3(0f, 0.02f, -8f), new Vector3(length, 0.04f, 4f), asphalt);
        for (float x = -length * 0.5f + 2f; x <= length * 0.5f - 2f; x += 4f)
            PrimitiveKit.Box("Dash", root, new Vector3(x, 0.045f, -8f), new Vector3(1.6f, 0.01f, 0.15f), paint, collider: false);
    }

    private static void FoodCounter(Transform parent, Vector3 position) => ShopZone(parent, position, "city_food");

    private static void ShopZone(Transform parent, Vector3 position, string shopId)
    {
        var zone = new GameObject("ShopZone_" + shopId);
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = position + Vector3.up;
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.6f, 2f, 2f);
        zone.AddComponent<ShopCounter>().Configure(shopId);
    }

    private static void Lane(Transform parent, float x, float z0, float z1)
    {
        PrimitiveKit.Box("DirtLane", parent, new Vector3(x, 0.012f, (z0 + z1) * 0.5f), new Vector3(2.4f, 0.02f, Mathf.Abs(z1 - z0)),
            PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f)), collider: false);
    }
}
