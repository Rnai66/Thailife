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

    internal static void BuildTambon(bool ask) => BuildLocation("Assets/Scenes/Tambon.unity", "Tambon", 10f, ask, root =>
    {
        // ตำบลคอโค: SAO office, health station, school, public park, canal with fishing
        Places2.SaoOffice(root, new Vector3(-24f, 0f, 10f), "อบต.คอโค");
        Places.HealthStation(root, new Vector3(24f, 0f, 9f), 0f, "รพ.สต.คอโค");
        Places.School(root, new Vector3(-10f, 0f, -28f), "โรงเรียนบ้านคอโค", 180f);
        Lane(root, -10f, -15f, -10f);
        Places2.Park(root, new Vector3(2f, 0f, 14f), "สวนสาธารณะตำบลคอโค");
        Places2.Canal(root, new Vector3(0f, 0f, 34f), 100f, new[] { -18f, -6f, 8f, 20f });
        Lane(root, 2f, -6f, 4f);

        Places.BusStop(root, new Vector3(16f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(19f, 0f, -11f), "ไปหมู่บ้าน / เมือง\n(ยืนใต้ป้ายรถ)", 180f);
        Places.Sign(root, new Vector3(-3f, 0f, -4.5f), "ตำบลคอโค", 0f);
        Places.Sign(root, new Vector3(8f, 0f, 27f), "คลองคอโค\nมีปลาให้ตก", 0f);

        foreach (var pos in new[] { new Vector3(-40f, 0f, -20f), new Vector3(40f, 0f, -22f), new Vector3(-42f, 0f, 20f), new Vector3(42f, 0f, 24f), new Vector3(14f, 0f, -30f) })
            Places.ShadeTree(root, pos);
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

        Places.BoxingCamp(root, new Vector3(0f, 0f, 34f), "ค่ายมวยเมืองคอโค");

        // Cross street up to the camp, past the clock tower
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        PrimitiveKit.Box("CrossStreet", root, new Vector3(0f, 0.02f, 12f), new Vector3(6f, 0.04f, 36f), asphalt);

        Places.BusStop(root, new Vector3(9f, 0f, -11.5f), 180f);
        Places.Sign(root, new Vector3(12f, 0f, -11f), "ไปหมู่บ้าน / ตำบล\n(ยืนใต้ป้ายรถ)", 180f);
        Places.Sign(root, new Vector3(-4f, 0f, 24f), "ค่ายมวย →\nซ้อม Lv 6+", 0f);

        foreach (var pos in new[] { new Vector3(-8f, 0f, -16f), new Vector3(8f, 0f, -16f), new Vector3(-40f, 0f, 20f), new Vector3(40f, 0f, 22f) })
            Places.ShadeTree(root, pos);
    });

    private static void BuildLocation(string scenePath, string rootName, float groundScale, bool ask, Action<Transform> content)
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
        MainRoad(root, groundScale * 10f);
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
