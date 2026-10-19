using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Build Thai Village
// Lays out the home village (หมู่บ้าน, GDD section 3.1) from primitives under one "Village" object.
// Everything here is walkable: home (stilt house, spirit house, banana trees for training tier 1),
// community shop (ร้านค้าชุมชน), temple (วัด), village hall (ศาลากลางหมู่บ้าน), Kok Nong Na farm and a
// songthaew stop to the sub-district (ตำบล) and the city (เมือง, where the boxing camp is).
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

    private static void Build() => Build(ask: true);

    internal static void Build(bool ask)
    {
        // The village belongs only in ThaiVillage.unity (not in the tambon/city scenes)
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (active.name != "ThaiVillage")
        {
            EditorUtility.DisplayDialog("Thai Village",
                $"ตอนนี้เปิดฉาก \"{active.name}\" อยู่ — ผังหมู่บ้านต้องสร้างในฉาก ThaiVillage\n\nเปิด Assets/Scenes/ThaiVillage ก่อน แล้วค่อยกดเมนูนี้", "OK");
            return;
        }

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (ask && !EditorUtility.DisplayDialog("Thai Village", "มี Village อยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?", "สร้างใหม่", "ยกเลิก"))
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

        // 120 x 120 m of ground for the whole village
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            Undo.RecordObject(ground.transform, "Resize Ground");
            ground.transform.localScale = new Vector3(12f, 1f, 12f);
            var groundMat = ground.GetComponent<Renderer>().sharedMaterial;
            if (groundMat != null)
            {
                Undo.RecordObject(groundMat, "Grass colour");
                groundMat.color = new Color(0.36f, 0.56f, 0.24f); // lush tropical grass
                if (groundMat.HasProperty("_Smoothness")) groundMat.SetFloat("_Smoothness", 0.05f);
            }
        }

        var v = root.transform;
        BuildRoad(v);
        BuildStiltHouse(v, new Vector3(7f, 0f, 7f));
        BuildSpiritHouse(v, new Vector3(2.8f, 0f, 3.2f));
        BuildShop(v, new Vector3(-7f, 0f, -12f), yaw: 180f);

        // Rice barn next to the house (harvests are stored here) and the chicken coop
        Places2.Granary(v, new Vector3(1.5f, 0f, 11f), 0f);
        Places2.ChickenCoop(v, new Vector3(-6.5f, 0f, 11f), 0f);

        // Home yard training (tier 1: banana trees)
        Places.BananaTree(v, new Vector3(-3f, 0f, -2.5f), trainable: true);
        Places.BananaTree(v, new Vector3(-4.8f, 0f, -0.8f), trainable: true);
        Places.Sign(v, new Vector3(-3.8f, 0f, -4.4f), "ซ้อมเตะต้นกล้วย\nLv 1+", 0f);
        foreach (var pos in new[] { new Vector3(11f, 0f, 2.5f), new Vector3(12.5f, 0f, 5.5f), new Vector3(11.5f, 0f, 9f), new Vector3(10f, 0f, 12f) })
            Places.BananaTree(v, pos, trainable: false);

        // Places around the village, each reached by a dirt lane from the main road
        Places.Temple(v, new Vector3(-32f, 0f, 21f), "วัด");
        Lane(v, -32f, -6f, 7f);
        Places2.VillageHall(v, new Vector3(28f, 0f, 16f), "ศาลากลางบ้าน");
        Lane(v, 28f, -6f, 11.5f);
        Places.KokNongNa(v, new Vector3(-12f, 0f, -36f));
        Lane(v, -12f, -21f, -10f);

        // Songthaew stop on the south side of the road, facing it
        Places.BusStop(v, new Vector3(14f, 0f, -11.5f), 180f);
        Places.Sign(v, new Vector3(17f, 0f, -11f), "ไปตำบล / เมือง\n(ยืนใต้ป้ายรถ)", 180f);

        foreach (var pos in new[] { new Vector3(-18f, 0f, -2f), new Vector3(16f, 0f, -3f), new Vector3(-6f, 0f, 16f), new Vector3(3f, 0f, 15f),
                     new Vector3(-48f, 0f, -20f), new Vector3(48f, 0f, 2f), new Vector3(-20f, 0f, 42f), new Vector3(10f, 0f, 45f), new Vector3(45f, 0f, -45f) })
            Places.ShadeTree(v, pos);

        DressNature(v);

        // Power line along the north side of the road, lamps over the road, wires to the house and shop
        var poles = StreetFurniture.PoleLine(v, new[]
        {
            new Vector3(-38f, 0f, -5.5f), new Vector3(-6f, 0f, -5.5f), new Vector3(10f, 0f, -5.5f), new Vector3(42f, 0f, -5.5f),
        }, 0f, lamps: true);
        StreetFurniture.ServiceDrop(v, poles[2], new Vector3(9.6f, 4.9f, 4.6f));   // stilt house eave
        StreetFurniture.ServiceDrop(v, poles[1], new Vector3(-7f, 3.3f, -10f));    // community shop roof

        // Lamp posts at every place, solar lamps along the Kok Nong Na paddies
        foreach (var p in new[]
        {
            new Vector3(4.4f, 0f, 1.2f),                                      // home yard
            new Vector3(-2.5f, 0f, 9f),                                       // rice barn & coop
            new Vector3(-10.5f, 0f, -11f),                                    // community shop
            new Vector3(-36f, 0f, 5.5f), new Vector3(-28f, 0f, 5.5f),         // temple gate
            new Vector3(-35f, 0f, 13f), new Vector3(-29f, 0f, 13f),           // ubosot stairs
            new Vector3(-26.5f, 0f, 27.5f),                                   // chedi
            new Vector3(21.5f, 0f, 12.5f), new Vector3(34.5f, 0f, 12.5f),     // village hall
        })
            StreetFurniture.LampPost(v, p);
        foreach (var p in new[]
        {
            new Vector3(-33.5f, 0f, -27.5f), new Vector3(-20f, 0f, -27.5f), new Vector3(-7f, 0f, -27.5f),
            new Vector3(-33.5f, 0f, -45f), new Vector3(-7f, 0f, -45f),
        })
            StreetFurniture.SolarLamp(v, p, p.z < -36f ? 180f : 0f);

        // Every location scene needs its own Network and controls
        NakamaSetup.Setup();
        MobileControlsSetup.EnsureMobileControls();
        EnsureCareerWiring();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[ThaiLife] Thai village built. Save (Cmd+S) and press Play.");
    }

    // Coconut palms along the road, shrubs and bougainvillea around the homes and lanes,
    // a few rocks, and grass tufts everywhere except roads, buildings and the paddies
    private static void DressNature(Transform v)
    {
        var palms = new[]
        {
            new Vector3(-52f, 0f, -1f), new Vector3(-26f, 0f, -12.5f), new Vector3(-16f, 0f, -4.5f), new Vector3(-2f, 0f, -12.8f),
            new Vector3(4f, 0f, -4.2f), new Vector3(22f, 0f, -12.6f), new Vector3(34f, 0f, -4.5f), new Vector3(44f, 0f, -12.5f),
            new Vector3(14f, 0f, 6f), new Vector3(-10f, 0f, 6f), new Vector3(20f, 0f, 28f), new Vector3(-44f, 0f, 40f),
        };
        for (int i = 0; i < palms.Length; i++) Nature.CoconutPalm(v, palms[i], 0.9f + (i % 3) * 0.12f, i);

        var bushes = new[]
        {
            new Vector3(0.2f, 0f, 2.2f), new Vector3(11.5f, 0f, 1f), new Vector3(5.2f, 0f, -3.8f), new Vector3(9f, 0f, -3.6f),
            new Vector3(-8f, 0f, 3f), new Vector3(-1f, 0f, 7f), new Vector3(-34.5f, 0f, 5.5f), new Vector3(-29.5f, 0f, 5.5f),
            new Vector3(25.5f, 0f, 6f), new Vector3(30.5f, 0f, 6f), new Vector3(-14f, 0f, -19f), new Vector3(-10f, 0f, -19f),
            new Vector3(18f, 0f, 12f), new Vector3(-22f, 0f, 10f),
        };
        for (int i = 0; i < bushes.Length; i++) Nature.Bush(v, bushes[i], 0.9f + (i % 3) * 0.25f, i, flowers: i % 3 == 0);

        var rocks = new[] { new Vector3(-6f, 0f, -20f), new Vector3(-17f, 0f, -22f), new Vector3(40f, 0f, 20f), new Vector3(-40f, 0f, 10f), new Vector3(15f, 0f, 30f) };
        for (int i = 0; i < rocks.Length; i++) Nature.Rock(v, rocks[i], 0.8f + i % 2 * 0.5f, i);

        Nature.GrassField(v, Vector3.zero, new Vector2(116f, 116f), 2600, 7,
            new Vector4(-60f, -10.6f, 60f, -5.4f),     // road
            new Vector4(-38.5f, 14.5f, -25.5f, 33.5f), // ubosot
            new Vector4(-26.5f, 26.5f, -19.5f, 33.5f), // chedi
            new Vector4(-42f, 13f, -36f, 21f),         // sala
            new Vector4(20f, 8f, 36f, 24f),            // village hall
            new Vector4(-32f, -56f, 8f, -20f),         // Kok Nong Na paddies and pond
            new Vector4(-11f, -16.5f, -3f, -7f),       // shop
            new Vector4(6f, -6f, 8f, 3f),              // lane to the house
            new Vector4(-33.3f, -6f, -30.7f, 8f), new Vector4(26.7f, -6f, 29.3f, 12f), new Vector4(-13.3f, -21f, -10.7f, -10f));
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
        Places.Sign(village, new Vector3(2.5f, 0f, -4.6f), "กระสอบทราย\nLv 3+", 0f);
    }

    private static void BuildRoad(Transform parent)
    {
        var road = PrimitiveKit.Group("Road", parent, new Vector3(0f, 0f, -8f));
        var asphalt = PrimitiveKit.Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
        var paint = PrimitiveKit.Mat("RoadPaint", new Color(0.95f, 0.9f, 0.6f));
        var dirt = PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f));

        PrimitiveKit.Box("Asphalt", road.transform, new Vector3(0f, 0.02f, 0f), new Vector3(120f, 0.04f, 4f), asphalt);
        for (float x = -58f; x <= 58f; x += 4f)
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

        // Sleeping mat (เสื่อ) on the porch: lie down to restore energy
        PrimitiveKit.Box("SleepingMat", t, new Vector3(-1.6f, 2.62f, -3.4f), new Vector3(1.4f, 0.03f, 2f),
            PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f)), collider: false);
        var bed = new GameObject("SleepZone");
        bed.transform.SetParent(t, false);
        bed.transform.localPosition = new Vector3(-1.6f, 3.6f, -3.4f);
        var bedZone = bed.AddComponent<BoxCollider>();
        bedZone.isTrigger = true;
        bedZone.size = new Vector3(2f, 2f, 1.6f);
        var matPoint = new GameObject("RestPoint");
        matPoint.transform.SetParent(t, false);
        matPoint.transform.localPosition = new Vector3(-1.6f, 2.76f, -3.3f); // on the mat, lying along it
        matPoint.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        bed.AddComponent<SleepSpot>().Configure("เสื่อบนชานบ้าน", "นอนพัก", matPoint.transform);

        // Porch railing (gap left for the stairs)
        PrimitiveKit.Box("Rail", t, new Vector3(-0.7f, 3.35f, -4.15f), new Vector3(4.6f, 0.08f, 0.08f), darkWood);
        foreach (float x in new[] { -3f, -1.5f, 0f, 1.6f })
            PrimitiveKit.Box("RailPost", t, new Vector3(x, 3f, -4.15f), new Vector3(0.08f, 0.8f, 0.08f), darkWood);

        // Water jars (โอ่งน้ำ) under the house
        var jar = MeshKit.Lathe("WaterJar", new[]
        {
            new Vector2(0.2f, 0f), new Vector2(0.34f, 0.1f), new Vector2(0.44f, 0.35f), new Vector2(0.42f, 0.62f),
            new Vector2(0.3f, 0.84f), new Vector2(0.22f, 0.9f), new Vector2(0.25f, 0.95f), new Vector2(0.21f, 0.97f),
        }, 20, capTop: false);
        var glazed = PrimitiveKit.Mat("ClayJar", new Color(0.42f, 0.24f, 0.12f), 0.7f);
        foreach (var (pos, size) in new[] { (new Vector3(-2f, 0f, -1.4f), 1f), (new Vector3(-1.1f, 0f, -1.75f), 0.85f) })
        {
            var j = MeshKit.Place("WaterJar_โอ่งมังกร", t, jar, glazed, pos, default, Vector3.one * size);
            var col = j.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.height = 0.9f;
            col.radius = 0.42f;
        }

        // Gable ends and ridge cap (names start with "Roof" so HomeInterior hides them when you're inside)
        var gableEnd = MeshKit.Gable("StiltGable", 4.6f, 2.3f, 0.1f);
        MeshKit.Place("RoofGableL", t, gableEnd, wood, new Vector3(-2.8f, 5f, 0f), new Vector3(0f, 90f, 0f));
        MeshKit.Place("RoofGableR", t, gableEnd, wood, new Vector3(2.8f, 5f, 0f), new Vector3(0f, -90f, 0f));
        PrimitiveKit.Box("RoofRidge", t, new Vector3(0f, 7.36f, 0f), new Vector3(6.9f, 0.16f, 0.28f), darkWood, collider: false);
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
        // Walk up to the counter to open the shop (sell fish, buy rods)
        var counterZone = new GameObject("ShopCounterZone");
        counterZone.transform.SetParent(t, false);
        counterZone.transform.localPosition = new Vector3(0f, 1f, -3f);
        var zone = counterZone.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(4f, 2f, 1.8f);
        counterZone.AddComponent<ShopCounter>();
        Places.Sign(t, new Vector3(2.9f, 0f, -2.8f), "รับซื้อปลา · ขายเบ็ด\nเดินมาที่หน้าร้าน", 0f);

        PrimitiveKit.Label("ร้านค้าชุมชน", t, new Vector3(0f, 3.65f, -2.12f), 0.09f, new Color(0.7f, 0.1f, 0.1f));
    }

    // Dirt lane along Z at x, from z0 to z1 (decoration only)
    private static void Lane(Transform parent, float x, float z0, float z1)
    {
        float length = Mathf.Abs(z1 - z0);
        PrimitiveKit.Box("DirtLane", parent, new Vector3(x, 0.012f, (z0 + z1) * 0.5f), new Vector3(2.4f, 0.02f, length),
            PrimitiveKit.Mat("Dirt", new Color(0.55f, 0.42f, 0.28f)), collider: false);
    }
}
