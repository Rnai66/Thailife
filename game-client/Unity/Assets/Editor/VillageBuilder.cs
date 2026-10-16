using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Build Thai Village
// Lays out a small Thai village from primitives (GDD section 3.1) under one "Village" object:
// road, stilt house (บ้านไม้ใต้ถุนสูง), spirit house (ศาลพระภูมิ), grocery shop (ร้านโชห่วย),
// banana trees (ต้นกล้วย — career tier 1) and a boxing camp (ค่ายมวย — career tier 3).
// Re-running replaces the Village object; other scene objects are kept.
public static class VillageBuilder
{
    private const string RootName = "Village";

    // Palette
    private static readonly Color DarkWood = new Color(0.36f, 0.22f, 0.12f);
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);
    private static readonly Color RoofRed = new Color(0.62f, 0.18f, 0.12f);
    private static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);
    private static readonly Color White = new Color(0.92f, 0.9f, 0.85f);

    [MenuItem("Thai Life/Build Thai Village")]
    private static void BuildFromMenu() => Build();

    // Builds the village only if it is missing (used by Repair Scene)
    internal static void EnsureVillage()
    {
        if (GameObject.Find(RootName) == null) Build();
    }

    private static void Build()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Thai Village", "มี Village อยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?", "สร้างใหม่", "ยกเลิก"))
                return;
            Undo.DestroyObjectImmediate(existing);
        }

        // The prototype house cube is replaced by the stilt house
        foreach (var oldName in new[] { "House", "Roof" })
        {
            var old = GameObject.Find(oldName);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Thai Village");

        BuildRoad(root.transform);
        BuildStiltHouse(root.transform, new Vector3(7f, 0f, 7f));
        BuildSpiritHouse(root.transform, new Vector3(2.8f, 0f, 3.2f));
        BuildShop(root.transform, new Vector3(-7f, 0f, -12f), yaw: 180f);
        BuildTrainingYard(root.transform);
        BuildBoxingCamp(root.transform, new Vector3(-12f, 0f, 8f));

        foreach (var pos in new[] { new Vector3(11f, 0f, 2.5f), new Vector3(12.5f, 0f, 5.5f), new Vector3(11.5f, 0f, 9f), new Vector3(10f, 0f, 12f) })
            BuildBananaTree(root.transform, pos, trainable: false);

        foreach (var pos in new[] { new Vector3(-18f, 0f, -2f), new Vector3(16f, 0f, -3f), new Vector3(-6f, 0f, 16f), new Vector3(3f, 0f, 15f) })
            BuildShadeTree(root.transform, pos);

        EnsureCareerWiring();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[ThaiLife] Thai village built. Save (Cmd+S) and press Play.");
    }

    // Career needs CareerClient on the Network object and a TrainingTarget on the punching bag
    private static void EnsureCareerWiring()
    {
        var connection = Object.FindAnyObjectByType<NakamaConnection>();
        if (connection != null && connection.GetComponent<CareerClient>() == null)
            Undo.AddComponent<CareerClient>(connection.gameObject);

        var bag = GameObject.Find("Training_PunchingBag");
        if (bag == null)
        {
            bag = TrainingSetup.BuildBag("Training_PunchingBag", null, new Vector3(2.5f, 0f, -2.5f), "bag", 3, 4, new Color(0.7f, 0.12f, 0.1f));
            Undo.RegisterCreatedObjectUndo(bag, "Add Punching Bag");
        }
        else if (bag.GetComponent<TrainingTarget>() == null)
        {
            Undo.AddComponent<TrainingTarget>(bag).Configure("bag", 3, 4);
        }

        var village = GameObject.Find(RootName).transform;
        BuildSign(village, new Vector3(2.5f, 0f, -4.6f), "กระสอบทราย\nLv 3+", 0f);
    }

    private static void BuildRoad(Transform parent)
    {
        var road = PrimitiveKit.Group("Road", parent, new Vector3(0f, 0f, -8f));
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var paint = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        var dirt = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));

        PrimitiveKit.Box("Asphalt", road.transform, new Vector3(0f, 0.02f, 0f), new Vector3(50f, 0.04f, 4f), asphalt);
        for (float x = -22f; x <= 22f; x += 4f)
            PrimitiveKit.Box("Dash", road.transform, new Vector3(x, 0.045f, 0f), new Vector3(1.6f, 0.01f, 0.15f), paint, collider: false);

        // Dirt lane from the road up to the stilt house
        PrimitiveKit.Box("DirtLane", parent, new Vector3(7f, 0.012f, -1.2f), new Vector3(1.8f, 0.02f, 9.6f), dirt, collider: false);
    }

    private static void BuildStiltHouse(Transform parent, Vector3 position)
    {
        var house = PrimitiveKit.Group("StiltHouse_บ้านไม้ใต้ถุนสูง", parent, position);
        var t = house.transform;
        var darkWood = PrimitiveKit.Mat("DarkWood", DarkWood);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var roof = PrimitiveKit.Mat("RoofRed", RoofRed);
        var shutter = PrimitiveKit.Mat("Shutter", new Color(0.25f, 0.32f, 0.3f));
        var clay = PrimitiveKit.Mat("ClayJar", new Color(0.45f, 0.25f, 0.12f));

        // Stilts (เสา) — open space underneath (ใต้ถุน)
        foreach (float x in new[] { -2.8f, 0f, 2.8f })
        foreach (float z in new[] { -2.3f, 0f, 2.3f })
            PrimitiveKit.Cylinder("Stilt", t, new Vector3(x, 1.2f, z), new Vector3(0.25f, 1.2f, 0.25f), darkWood);

        // Floor and front porch (ชาน)
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 2.5f, 0f), new Vector3(6.2f, 0.2f, 5.2f), darkWood);
        PrimitiveKit.Box("Porch", t, new Vector3(0f, 2.5f, -3.4f), new Vector3(6.2f, 0.2f, 1.6f), darkWood);

        // Room, door and windows (front faces -Z)
        PrimitiveKit.Box("Walls", t, new Vector3(0f, 3.8f, 0f), new Vector3(5.6f, 2.4f, 4.6f), wood);
        PrimitiveKit.Box("Door", t, new Vector3(0f, 3.55f, -2.32f), new Vector3(1.1f, 1.9f, 0.05f), darkWood, collider: false);
        PrimitiveKit.Box("WindowL", t, new Vector3(-1.8f, 3.9f, -2.32f), new Vector3(0.9f, 0.8f, 0.05f), shutter, collider: false);
        PrimitiveKit.Box("WindowR", t, new Vector3(1.8f, 3.9f, -2.32f), new Vector3(0.9f, 0.8f, 0.05f), shutter, collider: false);

        // Steep Thai gable roof: two slabs meeting at a ridge along X
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 6.15f, -1.38f), new Vector3(6.8f, 0.15f, 3.6f), roof, new Vector3(-40f, 0f, 0f));
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 6.15f, 1.38f), new Vector3(6.8f, 0.15f, 3.6f), roof, new Vector3(40f, 0f, 0f));
        // Gable-end finials (ป้านลม / ช่อฟ้า style horns)
        PrimitiveKit.Box("FinialL", t, new Vector3(-3.35f, 7.55f, 0f), new Vector3(0.1f, 0.7f, 0.1f), roof, new Vector3(0f, 0f, 20f), collider: false);
        PrimitiveKit.Box("FinialR", t, new Vector3(3.35f, 7.55f, 0f), new Vector3(0.1f, 0.7f, 0.1f), roof, new Vector3(0f, 0f, -20f), collider: false);

        // Stairs up to the porch (rise 0.26 m: within the CharacterController step offset)
        const int steps = 10;
        const float rise = 0.26f, depth = 0.35f;
        for (int i = 0; i < steps; i++)
        {
            float h = rise * (i + 1);
            float z = -4.2f - depth * (steps - 1 - i) - depth * 0.5f;
            PrimitiveKit.Box("Step", t, new Vector3(2.4f, h * 0.5f, z), new Vector3(1.2f, h, depth), darkWood);
        }

        // Porch railing (gap left for the stairs)
        PrimitiveKit.Box("Rail", t, new Vector3(-0.7f, 3.35f, -4.15f), new Vector3(4.6f, 0.08f, 0.08f), darkWood);
        foreach (float x in new[] { -3f, -1.5f, 0f, 1.6f })
            PrimitiveKit.Box("RailPost", t, new Vector3(x, 3f, -4.15f), new Vector3(0.08f, 0.8f, 0.08f), darkWood);

        // Water jars (โอ่งน้ำ) under the house
        PrimitiveKit.Sphere("WaterJar", t, new Vector3(-2f, 0.45f, -1.4f), new Vector3(0.8f, 0.9f, 0.8f), clay, collider: true);
        PrimitiveKit.Sphere("WaterJar", t, new Vector3(-1.1f, 0.4f, -1.7f), new Vector3(0.7f, 0.8f, 0.7f), clay, collider: true);
    }

    private static void BuildSpiritHouse(Transform parent, Vector3 position)
    {
        var shrine = PrimitiveKit.Group("SpiritHouse_ศาลพระภูมิ", parent, position);
        var t = shrine.transform;
        var stone = PrimitiveKit.Mat("ShrineWhite", White);
        var gold = PrimitiveKit.Mat("Gold", Gold);
        var red = PrimitiveKit.Mat("ShrineRed", new Color(0.75f, 0.12f, 0.1f));
        var marigold = PrimitiveKit.Mat("Marigold", new Color(1f, 0.6f, 0.05f));

        PrimitiveKit.Box("Base", t, new Vector3(0f, 0.075f, 0f), new Vector3(1.4f, 0.15f, 1.4f), stone);
        PrimitiveKit.Cylinder("Pillar", t, new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 0.65f, 0.25f), stone);
        PrimitiveKit.Box("Platform", t, new Vector3(0f, 1.49f, 0f), new Vector3(1f, 0.08f, 1f), gold);
        PrimitiveKit.Box("Shrine", t, new Vector3(0f, 1.78f, 0f), new Vector3(0.6f, 0.5f, 0.5f), red);
        PrimitiveKit.Box("Doorway", t, new Vector3(0f, 1.74f, -0.255f), new Vector3(0.22f, 0.32f, 0.02f), gold, collider: false);
        PrimitiveKit.Box("RoofFront", t, new Vector3(0f, 2.19f, -0.135f), new Vector3(0.75f, 0.04f, 0.42f), gold, new Vector3(-50f, 0f, 0f), collider: false);
        PrimitiveKit.Box("RoofBack", t, new Vector3(0f, 2.19f, 0.135f), new Vector3(0.75f, 0.04f, 0.42f), gold, new Vector3(50f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("Spire", t, new Vector3(0f, 2.5f, 0f), new Vector3(0.04f, 0.15f, 0.04f), gold, collider: false);

        // Marigold garlands (พวงมาลัย) on the platform corners
        foreach (var corner in new[] { new Vector3(-0.42f, 1.56f, -0.42f), new Vector3(0.42f, 1.56f, -0.42f), new Vector3(-0.42f, 1.56f, 0.42f), new Vector3(0.42f, 1.56f, 0.42f) })
            PrimitiveKit.Sphere("Garland", t, corner, Vector3.one * 0.12f, marigold);
    }

    private static void BuildShop(Transform parent, Vector3 position, float yaw)
    {
        var shop = PrimitiveKit.Group("Shop_ร้านโชห่วย", parent, position, yaw);
        var t = shop.transform;
        var wall = PrimitiveKit.Mat("ShopWall", new Color(0.85f, 0.82f, 0.7f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var red = PrimitiveKit.Mat("AwningRed", new Color(0.85f, 0.15f, 0.15f));
        var white = PrimitiveKit.Mat("AwningWhite", Color.white);
        var sign = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        Material[] goods =
        {
            PrimitiveKit.Mat("GoodsBlue", new Color(0.2f, 0.4f, 0.85f)),
            PrimitiveKit.Mat("GoodsGreen", new Color(0.2f, 0.7f, 0.3f)),
            PrimitiveKit.Mat("GoodsOrange", new Color(1f, 0.55f, 0.1f)),
        };

        // Shell: back, sides and flat roof; the front (-Z) is open like a Thai shophouse
        PrimitiveKit.Box("BackWall", t, new Vector3(0f, 1.5f, 1.9f), new Vector3(5f, 3f, 0.2f), wall);
        PrimitiveKit.Box("SideL", t, new Vector3(-2.4f, 1.5f, 0f), new Vector3(0.2f, 3f, 4f), wall);
        PrimitiveKit.Box("SideR", t, new Vector3(2.4f, 1.5f, 0f), new Vector3(0.2f, 3f, 4f), wall);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 3.1f, 0f), new Vector3(5.6f, 0.2f, 4.6f), wall);

        PrimitiveKit.Box("Counter", t, new Vector3(-0.6f, 0.5f, -1.4f), new Vector3(3.2f, 1f, 0.6f), wood);

        // Shelves with goods
        for (int row = 0; row < 3; row++)
        {
            float y = 0.8f + row * 0.6f;
            PrimitiveKit.Box("Shelf", t, new Vector3(0f, y, 1.5f), new Vector3(4.2f, 0.06f, 0.5f), wood, collider: false);
            for (int i = 0; i < 7; i++)
                PrimitiveKit.Box("Goods", t, new Vector3(-1.8f + i * 0.6f, y + 0.18f, 1.5f), new Vector3(0.3f, 0.3f, 0.3f), goods[(i + row) % goods.Length], collider: false);
        }

        // Striped awning sloping down toward the street
        for (int i = 0; i < 7; i++)
            PrimitiveKit.Box("Awning", t, new Vector3(-2.4f + i * 0.8f, 2.7f, -2.7f), new Vector3(0.8f, 0.06f, 1.6f), i % 2 == 0 ? red : white, new Vector3(-14f, 0f, 0f), collider: false);

        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 3.65f, -2.05f), new Vector3(4.2f, 0.8f, 0.1f), sign, collider: false);
        PrimitiveKit.Label("ร้านค้า", t, new Vector3(0f, 3.65f, -2.12f), 0.09f, new Color(0.7f, 0.1f, 0.1f));
    }

    private static void BuildTrainingYard(Transform parent)
    {
        // Career tier 1: kick the banana trees (เตะต้นกล้วย), close to spawn
        BuildBananaTree(parent, new Vector3(-3f, 0f, -2.5f), trainable: true);
        BuildBananaTree(parent, new Vector3(-4.8f, 0f, -0.8f), trainable: true);
        BuildSign(parent, new Vector3(-3.8f, 0f, -4.4f), "ซ้อมเตะต้นกล้วย\nLv 1+", 0f);
    }

    private static void BuildBananaTree(Transform parent, Vector3 position, bool trainable)
    {
        var tree = PrimitiveKit.Group(trainable ? "Training_BananaTree" : "BananaTree", parent, position);
        if (trainable)
            tree.AddComponent<TrainingTarget>().Configure("banana", 1, 2);

        var trunkMat = PrimitiveKit.Mat("BananaTrunk", new Color(0.5f, 0.58f, 0.25f));
        var leafMat = PrimitiveKit.Mat("BananaLeaf", new Color(0.3f, 0.7f, 0.2f));
        var fruitMat = PrimitiveKit.Mat("BananaFruit", new Color(0.75f, 0.8f, 0.2f));

        // Pivot at the base so the trunk bends when kicked; trunk must be the first child (PunchingBag.IsRooted)
        var pivot = PrimitiveKit.Group("Pivot", tree.transform, Vector3.zero);
        if (trainable) pivot.AddComponent<PunchingBag>();

        PrimitiveKit.Cylinder("Trunk", pivot.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.35f, 1.2f, 0.35f), trunkMat);
        for (int i = 0; i < 6; i++)
        {
            var holder = PrimitiveKit.Group("LeafHolder", pivot.transform, new Vector3(0f, 2.35f, 0f), i * 60f + 15f);
            PrimitiveKit.Box("Leaf", holder.transform, new Vector3(0f, 0.15f, 0.8f), new Vector3(0.4f, 0.04f, 1.8f), leafMat, new Vector3(20f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Sphere("Bunch", pivot.transform, new Vector3(0.25f, 1.95f, 0.1f), new Vector3(0.3f, 0.5f, 0.3f), fruitMat);
    }

    private static void BuildBoxingCamp(Transform parent, Vector3 position)
    {
        var camp = PrimitiveKit.Group("BoxingCamp_ค่ายมวย", parent, position);
        var t = camp.transform;
        var baseMat = PrimitiveKit.Mat("RingBase", new Color(0.2f, 0.2f, 0.22f));
        var canvas = PrimitiveKit.Mat("RingCanvas", new Color(0.35f, 0.6f, 0.9f));
        var red = PrimitiveKit.Mat("CornerRed", new Color(0.85f, 0.15f, 0.15f));
        var blue = PrimitiveKit.Mat("CornerBlue", new Color(0.15f, 0.3f, 0.85f));
        var white = PrimitiveKit.Mat("RopeWhite", Color.white);
        var banner = PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f));
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);

        // Ring (เวทีมวย): 6x6 m, top at 0.6 m
        PrimitiveKit.Box("RingBase", t, new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 6f), baseMat);
        PrimitiveKit.Box("RingCanvas", t, new Vector3(0f, 0.55f, 0f), new Vector3(5.8f, 0.1f, 5.8f), canvas);

        PrimitiveKit.Cylinder("CornerRed", t, new Vector3(-2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), red);
        PrimitiveKit.Cylinder("CornerBlue", t, new Vector3(2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), blue);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(2.8f, 1.5f, -2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);
        PrimitiveKit.Cylinder("CornerWhite", t, new Vector3(-2.8f, 1.5f, 2.8f), new Vector3(0.15f, 0.9f, 0.15f), white);

        // Ropes on three sides; the front (-Z) is left open as the entrance
        Material[] ropeColors = { red, white, blue };
        float[] ropeHeights = { 1.0f, 1.4f, 1.8f };
        for (int i = 0; i < 3; i++)
        {
            PrimitiveKit.Box("Rope", t, new Vector3(0f, ropeHeights[i], 2.8f), new Vector3(5.6f, 0.05f, 0.05f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(-2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
            PrimitiveKit.Box("Rope", t, new Vector3(2.8f, ropeHeights[i], 0f), new Vector3(0.05f, 0.05f, 5.6f), ropeColors[i]);
        }

        // Steps up to the ring (0.2 m rises)
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.1f, -3.6f), new Vector3(2f, 0.2f, 0.4f), baseMat);
        PrimitiveKit.Box("Step", t, new Vector3(0f, 0.2f, -3.2f), new Vector3(2f, 0.4f, 0.4f), baseMat);

        // Entrance arch with banner
        PrimitiveKit.Box("ArchPostL", t, new Vector3(-3.6f, 1.9f, -5.2f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("ArchPostR", t, new Vector3(3.6f, 1.9f, -5.2f), new Vector3(0.3f, 3.8f, 0.3f), red);
        PrimitiveKit.Box("Banner", t, new Vector3(0f, 3.6f, -5.2f), new Vector3(7.5f, 0.9f, 0.15f), banner, collider: false);
        PrimitiveKit.Label("ค่ายมวย", t, new Vector3(0f, 3.62f, -5.3f), 0.1f, new Color(0.75f, 0.1f, 0.1f));
        PrimitiveKit.Label("Lv 6+", t, new Vector3(0f, 2.9f, -5.3f), 0.06f, Color.white);

        // Career tier 3: heavy bags beside the ring
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.6f, 0f, -1.2f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));
        TrainingSetup.BuildBag("Training_CampBag", t, new Vector3(4.6f, 0f, 1.8f), "camp", 6, 7, new Color(0.1f, 0.2f, 0.55f));

        PrimitiveKit.Box("Bench", t, new Vector3(-4.6f, 0.225f, 0f), new Vector3(0.6f, 0.45f, 2.4f), wood);
    }

    private static void BuildShadeTree(Transform parent, Vector3 position)
    {
        var tree = PrimitiveKit.Group("ShadeTree", parent, position);
        PrimitiveKit.Cylinder("Trunk", tree.transform, new Vector3(0f, 1.4f, 0f), new Vector3(0.45f, 1.4f, 0.45f), PrimitiveKit.Mat("Bark", new Color(0.4f, 0.26f, 0.13f)));
        PrimitiveKit.Sphere("Canopy", tree.transform, new Vector3(0f, 3.6f, 0f), new Vector3(3.2f, 2.6f, 3.2f), PrimitiveKit.Mat("Leaves", new Color(0.18f, 0.5f, 0.2f)));
    }

    private static void BuildSign(Transform parent, Vector3 position, string text, float yaw)
    {
        var sign = PrimitiveKit.Group("Sign", parent, position, yaw);
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        var board = PrimitiveKit.Mat("SignBoard", new Color(0.95f, 0.92f, 0.8f));
        PrimitiveKit.Box("Post", sign.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), wood);
        PrimitiveKit.Box("Board", sign.transform, new Vector3(0f, 1.35f, 0f), new Vector3(1.6f, 0.7f, 0.06f), board, collider: false);
        PrimitiveKit.Label(text, sign.transform, new Vector3(0f, 1.35f, -0.04f), 0.035f, new Color(0.25f, 0.15f, 0.05f));
    }
}
