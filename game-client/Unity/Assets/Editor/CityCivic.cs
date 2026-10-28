using UnityEngine;

// New city buildings:
//   north row (behind the pub street, beside the boxing camp, facing the town centre):
//     hospital, hotel, police station, pawn shop
//   south of the main road, side by side and facing it: district office (ที่ว่าการอำเภอ) and อบจ.
// Each building is modelled facing local -Z, then turned to face its street.
public static class CityCivic
{
    // Buildings are built full size, then shrunk so the town isn't crowded
    private const float Scale = 0.75f;

    public static void Build(Transform root)
    {
        // North row: fronts face south (-Z) toward the town centre
        Hospital(root, new Vector3(-40f, 0f, 38.5f), 0f);
        Hotel(root, new Vector3(-29.5f, 0f, 38.5f), 0f);
        Church(root, new Vector3(-19f, 0f, 38.5f), 0f);
        Police(root, new Vector3(17f, 0f, 38.5f), 0f);
        PawnShop(root, new Vector3(24f, 0f, 38.5f), 0f);
        Mosque(root, new Vector3(35f, 0f, 38.5f), 0f);

        // South of the road, next to each other, facing the road (+Z)
        PrimitiveKit.Box("CivicPavement", root, new Vector3(15f, 0.008f, -16f), new Vector3(34f, 0.012f, 10f),
            PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f)), collider: false);
        Office(root, new Vector3(5.5f, 0f, -25f), 180f, "ที่ว่าการอำเภอ", new Color(0.96f, 0.93f, 0.84f), new Color(0.75f, 0.35f, 0.15f));
        Office(root, new Vector3(17.1f, 0f, -25f), 180f, "องค์การบริหารส่วนจังหวัด (อบจ.)", new Color(0.92f, 0.94f, 0.96f), new Color(0.2f, 0.4f, 0.7f));

        // Bank (ธนาคาร) next to the อบจ.
        Bank(root, new Vector3(28.6f, 0f, -25f), 180f);

        // The town temple (วัด) in the south-west, facing the road
        CityTemple(root, new Vector3(-10.5f, 0f, -29f), 180f); // right next to the event ground
    }

    // A building block: walls, window grid, a modern automatic glass entrance with a kiosk, canopy,
    // steps and a name board
    internal static Transform Block(Transform root, Vector3 position, float yaw, string name, float width, float depth, int floors,
        Color wall, Color trim, Color sign, Color signText, string objectName, string kiosk = "บัตรคิว", string interior = "office")
    {
        var b = PrimitiveKit.Group(objectName, root, position, yaw).transform;
        b.localScale = Vector3.one * Scale;
        float h = floors * 3.2f;
        var wallMat = PrimitiveKit.Mat(objectName + "Wall", wall);
        var trimMat = PrimitiveKit.Mat(objectName + "Trim", trim);
        var glass = PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f));
        // Walk-in ground floor (Interiors + BuildingInterior); the floors above stay a solid block
        const float ground = 3.2f;
        var accent = Color.Lerp(sign, new Color(0.2f, 0.8f, 1f), 0.5f);
        if (floors > 1) PrimitiveKit.Box("UpperFloors", b, new Vector3(0f, (ground + h) * 0.5f, 0f), new Vector3(width, h - ground, depth), wallMat);
        PrimitiveKit.Box("Ceiling", b, new Vector3(0f, ground - 0.05f, 0f), new Vector3(width, 0.1f, depth), wallMat);
        var room = Interiors.Rooms(b, width, depth, ground, 2.4f, 2.6f, wallMat, new Color(0.96f, 0.95f, 0.93f), accent);
        Interiors.Furnish(room, interior, width, depth, ground, accent);
        PrimitiveKit.Box("Cornice", b, new Vector3(0f, h + 0.15f, 0f), new Vector3(width + 0.3f, 0.3f, depth + 0.3f), trimMat, collider: false);
        int cols = Mathf.Max(2, Mathf.FloorToInt(width / 2.6f));
        float front = -depth * 0.5f;
        int pane = objectName.Length;
        for (int f = 0; f < floors; f++)
        for (int c = 0; c < cols; c++)
        {
            float x = (c - (cols - 1) * 0.5f) * (width / cols);
            if (f == 0 && Mathf.Abs(x) < 1.6f) continue; // the door goes here
            float wy = 1.7f + f * 3.2f;
            PrimitiveKit.Box("WindowFrame", b, new Vector3(x, wy, front - 0.02f), new Vector3(1.6f, 1.7f, 0.05f), trimMat, collider: false);
            PrimitiveKit.Box("Window", b, new Vector3(x, wy, front - 0.05f), new Vector3(1.4f, 1.5f, 0.05f), f == 0 ? glass : BuildingDetail.WindowGlass(pane++), collider: false);
            PrimitiveKit.Box("Mullion", b, new Vector3(x, wy, front - 0.08f), new Vector3(0.06f, 1.5f, 0.03f), trimMat, collider: false);
            PrimitiveKit.Box("Sill", b, new Vector3(x, wy - 0.8f, front - 0.1f), new Vector3(1.7f, 0.08f, 0.2f), trimMat, collider: false);
            PrimitiveKit.Box("Lintel", b, new Vector3(x, wy + 0.9f, front - 0.07f), new Vector3(1.8f, 0.12f, 0.14f), trimMat, collider: false);
            if (f > 0 && (c + f) % 3 == 1) BuildingDetail.AirCon(b, new Vector3(x + 1.05f, wy - 0.45f, front - 0.2f));
        }
        // Base plinth, a band between floors, corner pilasters, a drainpipe
        float plinth = (width + 0.1f) * 0.5f - 1.2f; // either side of the doorway
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Box("Plinth", b, new Vector3(sx * (1.2f + plinth * 0.5f), 0.25f, front - 0.04f), new Vector3(plinth, 0.5f, 0.1f), PrimitiveKit.Mat("PlinthGrey", new Color(0.55f, 0.53f, 0.5f)), collider: false);
        for (int f = 1; f < floors; f++)
            PrimitiveKit.Box("FloorBand", b, new Vector3(0f, f * 3.2f + 0.05f, front - 0.04f), new Vector3(width, 0.18f, 0.1f), trimMat, collider: false);
        foreach (float x in new[] { -width * 0.5f + 0.15f, width * 0.5f - 0.15f })
            PrimitiveKit.Box("CornerPilaster", b, new Vector3(x, h * 0.5f, front - 0.06f), new Vector3(0.35f, h, 0.14f), trimMat, collider: false);
        BuildingDetail.Drainpipe(b, new Vector3(width * 0.5f - 0.45f, h, front - 0.12f), h);

        // Roof: parapet clutter seen from the street — water tank, condensers, antenna, stair house
        BuildingDetail.WaterTank(b, new Vector3(-width * 0.25f, h + 0.3f, depth * 0.2f));
        BuildingDetail.Condenser(b, new Vector3(width * 0.2f, h + 0.3f, -depth * 0.15f));
        BuildingDetail.Condenser(b, new Vector3(width * 0.2f + 1.1f, h + 0.3f, -depth * 0.15f));
        BuildingDetail.Antenna(b, new Vector3(width * 0.35f, h + 0.3f, depth * 0.3f));
        PrimitiveKit.Box("StairHouse", b, new Vector3(width * 0.05f, h + 1.4f, depth * 0.25f), new Vector3(2.4f, 2.2f, 2.2f), wallMat, collider: false);
        PrimitiveKit.Box("StairHouseRoof", b, new Vector3(width * 0.05f, h + 2.55f, depth * 0.25f), new Vector3(2.6f, 0.12f, 2.4f), trimMat, collider: false);
        foreach (float x in new[] { -1.9f, 1.9f })
            BuildingDetail.PottedPlant(b, new Vector3(x + (x < 0 ? -0.6f : 0.6f), 0f, front - 1.9f), (int)(x * 10f) + floors);
        Entrance.Modern(b, 0f, -depth * 0.5f, 2.2f, 2.5f, accent, "ยินดีต้อนรับ", kiosk, lobby: false);
        PrimitiveKit.Box("Canopy", b, new Vector3(0f, 2.9f, -depth * 0.5f - 1.2f), new Vector3(4.2f, 0.18f, 2.4f), trimMat, collider: false);
        foreach (float x in new[] { -1.9f, 1.9f })
            PrimitiveKit.Cylinder("CanopyPost", b, new Vector3(x, 1.4f, -depth * 0.5f - 2.2f), new Vector3(0.14f, 1.4f, 0.14f), trimMat);
        PrimitiveKit.Box("Steps", b, new Vector3(0f, 0.08f, -depth * 0.5f - 0.8f), new Vector3(4f, 0.16f, 1.6f), trimMat, collider: false);

        float signW = Mathf.Min(width - 1f, 1.6f + name.Length * 0.32f);
        PrimitiveKit.Box("NameBoard", b, new Vector3(0f, h - 0.6f, -depth * 0.5f - 0.08f), new Vector3(signW, 0.9f, 0.1f),
            PrimitiveKit.Mat(objectName + "Sign", sign), collider: false);
        PrimitiveKit.Label(name, b, new Vector3(0f, h - 0.6f, -depth * 0.5f - 0.15f), 0.07f, signText);
        return b;
    }

    private static void Hospital(Transform root, Vector3 p, float yaw)
    {
        var b = Block(root, p, yaw, "โรงพยาบาล", 16f, 11f, 3, new Color(0.97f, 0.97f, 0.96f), new Color(0.2f, 0.5f, 0.75f),
            new Color(0.2f, 0.5f, 0.75f), Color.white, "Hospital_โรงพยาบาล", interior: "hospital");
        var red = PrimitiveKit.Mat("RedCross", new Color(0.85f, 0.12f, 0.12f));
        PrimitiveKit.Box("CrossV", b, new Vector3(-6.2f, 7.6f, -5.6f), new Vector3(0.5f, 1.6f, 0.08f), red, collider: false);
        PrimitiveKit.Box("CrossH", b, new Vector3(-6.2f, 7.6f, -5.6f), new Vector3(1.6f, 0.5f, 0.08f), red, collider: false);

        // Ambulance parked by the entrance
        var van = PrimitiveKit.Group("Ambulance_รถพยาบาล", b, new Vector3(5.5f, 0f, -9f), 90f).transform;
        PrimitiveKit.Box("Body", van, new Vector3(0f, 1.1f, 0f), new Vector3(1.9f, 1.6f, 4.6f), PrimitiveKit.Mat("VanWhite", Color.white));
        PrimitiveKit.Box("Stripe", van, new Vector3(0f, 1.1f, 0f), new Vector3(1.92f, 0.25f, 4.62f), red, collider: false);
        PrimitiveKit.Box("Windscreen", van, new Vector3(0f, 1.5f, 2.31f), new Vector3(1.7f, 0.6f, 0.04f), PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f)), collider: false);
        PrimitiveKit.Box("Siren", van, new Vector3(0f, 1.98f, 1.6f), new Vector3(1f, 0.16f, 0.3f), PrimitiveKit.Glow("SirenBlue", new Color(0.3f, 0.5f, 1f), 2f), collider: false);
        Wheels(van, 0.95f, 1.5f, 0.35f);

        // Treatment (รักษาตัว) at the reception desk inside: pricier than the รพ.สต. but restores full energy
        var zone = new GameObject("HealZone");
        zone.transform.SetParent(b, false);
        zone.transform.localPosition = new Vector3(-2.7f, 1f, 2.4f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4.2f, 2f, 2.4f);
        zone.AddComponent<HealSpot>().Configure("hospital", "โรงพยาบาล", 200);
        StreetFurniture.LampPost(b, new Vector3(-3.2f, 0f, -8.5f));
        // Doctor's shifts (หมออนามัย, education.lua): inside by the pharmacy window, clear of the treatment desk
        Schooling.JobSpot(b, new Vector3(4.6f, 0f, 2.4f), 0f, "doctor", null, showSign: false);
    }

    private static void Hotel(Transform root, Vector3 p, float yaw)
    {
        var trim = new Color(0.55f, 0.35f, 0.2f);
        var b = Block(root, p, yaw, "โรงแรม", 10f, 10f, 5, new Color(0.93f, 0.85f, 0.7f), trim,
            new Color(0.4f, 0.12f, 0.15f), new Color(1f, 0.85f, 0.4f), "Hotel_โรงแรม", interior: "hotel");
        var rail = PrimitiveKit.Mat("RailMetal", new Color(0.2f, 0.22f, 0.24f));
        for (int f = 1; f < 5; f++)
        {
            PrimitiveKit.Box("Balcony", b, new Vector3(0f, f * 3.2f + 0.05f, -5.5f), new Vector3(8.6f, 0.15f, 1f), PrimitiveKit.Mat("Hotel_โรงแรมTrim", trim), collider: false);
            PrimitiveKit.Box("BalconyRail", b, new Vector3(0f, f * 3.2f + 0.6f, -5.95f), new Vector3(8.6f, 0.07f, 0.07f), rail, collider: false);
        }
        PrimitiveKit.Box("Marquee", b, new Vector3(0f, 3.5f, -5.3f), new Vector3(5.5f, 0.5f, 0.15f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);
    }

    private static void Police(Transform root, Vector3 p, float yaw)
    {
        var b = Block(root, p, yaw, "สถานีตำรวจ", 11f, 10f, 2, new Color(0.95f, 0.95f, 0.95f), new Color(0.12f, 0.2f, 0.45f),
            new Color(0.12f, 0.2f, 0.45f), Color.white, "Police_สถานีตำรวจ");
        // Patrol car: white with a blue stripe and a light bar
        var car = PrimitiveKit.Group("PatrolCar_รถสายตรวจ", b, new Vector3(3.6f, 0f, -9f), 90f).transform;
        PrimitiveKit.Box("Body", car, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.7f, 4.2f), PrimitiveKit.Mat("VanWhite", Color.white));
        PrimitiveKit.Box("Cabin", car, new Vector3(0f, 1.15f, -0.2f), new Vector3(1.6f, 0.55f, 2.2f), PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f)));
        PrimitiveKit.Box("Stripe", car, new Vector3(0f, 0.65f, 0f), new Vector3(1.82f, 0.18f, 4.22f), PrimitiveKit.Mat("PoliceBlue", new Color(0.12f, 0.2f, 0.45f)), collider: false);
        PrimitiveKit.Box("LightRed", car, new Vector3(-0.3f, 1.5f, -0.2f), new Vector3(0.55f, 0.14f, 0.3f), PrimitiveKit.Glow("SirenRed", new Color(1f, 0.2f, 0.2f), 2f), collider: false);
        PrimitiveKit.Box("LightBlue", car, new Vector3(0.3f, 1.5f, -0.2f), new Vector3(0.55f, 0.14f, 0.3f), PrimitiveKit.Glow("SirenBlue", new Color(0.3f, 0.5f, 1f), 2f), collider: false);
        Wheels(car, 0.9f, 1.3f, 0.32f);
        Places2.ThaiFlag(b, new Vector3(-4.5f, 0f, -8.5f));
    }

    private static void PawnShop(Transform root, Vector3 p, float yaw)
    {
        var b = Block(root, p, yaw, "โรงรับจำนำ", 6f, 9f, 2, new Color(0.88f, 0.9f, 0.84f), new Color(0.2f, 0.45f, 0.28f),
            new Color(0.2f, 0.45f, 0.28f), new Color(1f, 0.92f, 0.6f), "PawnShop_โรงรับจำนำ", interior: "gold");
        var bars = PrimitiveKit.Mat("RailMetal", new Color(0.2f, 0.22f, 0.24f));
        for (int i = 0; i < 7; i++) // security grille across the ground floor, open at the doorway
            if (Mathf.Abs(-1.5f + i * 0.5f) > 1.2f) PrimitiveKit.Box("Grille", b, new Vector3(-1.5f + i * 0.5f, 1.25f, -4.62f), new Vector3(0.05f, 2.5f, 0.05f), bars, collider: false);
    }

    // Government office: Thai-style gable roof with a gold pediment, the national flag out front
    private static void Office(Transform root, Vector3 p, float yaw, string name, Color wall, Color roof)
    {
        string id = name.Contains("อบจ") ? "PAO_อบจ" : "District_อำเภอ";
        var b = Block(root, p, yaw, name, 15f, 11f, 2, wall, new Color(0.85f, 0.7f, 0.35f), roof, Color.white, id);
        var roofMat = PrimitiveKit.Mat(id + "Roof", roof);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Roof", b, new Vector3(0f, 7.55f, side * 2.85f), new Vector3(16f, 0.18f, 6.9f), roofMat, new Vector3(side * 35f, 0f, 0f), collider: false);
        MeshKit.Place("Gable", b, MeshKit.Gable("OfficeGable", 11f, 3.1f, 0.12f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)),
            new Vector3(7.6f, 6.4f, 0f), new Vector3(0f, -90f, 0f));
        MeshKit.Place("Gable", b, MeshKit.Gable("OfficeGable", 11f, 3.1f, 0.12f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)),
            new Vector3(-7.6f, 6.4f, 0f), new Vector3(0f, 90f, 0f));
        Places2.ThaiFlag(b, new Vector3(5.5f, 0f, -9.5f));
    }

    // Christian church (โบสถ์คริสต์): white nave, red gable roof, bell tower with a spire and cross
    private static void Church(Transform root, Vector3 p, float yaw)
    {
        var b = PrimitiveKit.Group("Church_โบสถ์คริสต์", root, p, yaw).transform;
        b.localScale = Vector3.one * Scale;
        var white = PrimitiveKit.Mat("ChurchWhite", new Color(0.96f, 0.95f, 0.92f));
        var roof = PrimitiveKit.Mat("ChurchRoof", new Color(0.6f, 0.16f, 0.14f));
        var wood = PrimitiveKit.Mat("ChurchDoor", new Color(0.4f, 0.24f, 0.12f));
        PrimitiveKit.Box("Nave", b, new Vector3(0f, 2.75f, 1.5f), new Vector3(8f, 5.5f, 11f), white);
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Roof", b, new Vector3(side * 2.2f, 7.05f, 1.5f), new Vector3(5.8f, 0.2f, 11.6f), roof, new Vector3(0f, 0f, side * -40f), collider: false);
        MeshKit.Place("NaveGable", b, MeshKit.Gable("ChurchGable", 8f, 3.4f, 0.1f), white, new Vector3(0f, 5.5f, -4.05f));
        // Bell tower at the front
        PrimitiveKit.Box("Tower", b, new Vector3(0f, 4.5f, -4.8f), new Vector3(3f, 9f, 3f), white);
        MeshKit.Place("Spire", b, MeshKit.Lathe("ChurchSpire", new[] { new Vector2(2.1f, 0f), new Vector2(1.6f, 0.4f), new Vector2(0.05f, 3.6f), new Vector2(0f, 3.7f) }, 4),
            roof, new Vector3(0f, 9f, -4.8f), new Vector3(0f, 45f, 0f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));
        PrimitiveKit.Box("CrossV", b, new Vector3(0f, 13.4f, -4.8f), new Vector3(0.14f, 1.2f, 0.14f), gold, collider: false);
        PrimitiveKit.Box("CrossH", b, new Vector3(0f, 13.6f, -4.8f), new Vector3(0.7f, 0.14f, 0.14f), gold, collider: false);
        PrimitiveKit.Box("Door", b, new Vector3(0f, 1.4f, -6.32f), new Vector3(1.6f, 2.8f, 0.06f), wood, collider: false);
        PrimitiveKit.Cylinder("RoseWindow", b, new Vector3(0f, 6.6f, -6.32f), new Vector3(1.3f, 0.03f, 1.3f),
            PrimitiveKit.Glow("StainedGlass", new Color(0.35f, 0.45f, 0.9f), 0.8f), new Vector3(90f, 0f, 0f), collider: false);
        for (int i = 0; i < 3; i++)
            foreach (float side in new[] { -1f, 1f })
                PrimitiveKit.Box("Window", b, new Vector3(side * 4.02f, 3f, -1.5f + i * 3f), new Vector3(0.06f, 2.4f, 1f),
                    PrimitiveKit.Glow("StainedGlass", new Color(0.35f, 0.45f, 0.9f), 0.8f), collider: false);
        PrimitiveKit.Label("โบสถ์คริสต์", b, new Vector3(0f, 3.4f, -6.36f), 0.06f, new Color(0.4f, 0.24f, 0.12f));
    }

    // Mosque (มัสยิด): white prayer hall with green trim, a central dome and a minaret with a crescent
    private static void Mosque(Transform root, Vector3 p, float yaw)
    {
        var b = PrimitiveKit.Group("Mosque_มัสยิด", root, p, yaw).transform;
        b.localScale = Vector3.one * Scale;
        var white = PrimitiveKit.Mat("MosqueWhite", new Color(0.97f, 0.97f, 0.95f));
        var green = PrimitiveKit.Mat("MosqueGreen", new Color(0.1f, 0.5f, 0.35f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));
        PrimitiveKit.Box("Hall", b, new Vector3(0f, 2.5f, 0.5f), new Vector3(10f, 5f, 9f), white);
        PrimitiveKit.Box("Trim", b, new Vector3(0f, 5.1f, 0.5f), new Vector3(10.3f, 0.3f, 9.3f), green, collider: false);
        MeshKit.Place("Dome", b, MeshKit.Lathe("MosqueDome", new[]
        {
            new Vector2(2.8f, 0f), new Vector2(3f, 0.6f), new Vector2(2.9f, 1.6f), new Vector2(2.4f, 2.6f), new Vector2(1.5f, 3.4f),
            new Vector2(0.5f, 3.9f), new Vector2(0.15f, 4.2f), new Vector2(0f, 4.6f),
        }, 28), green, new Vector3(0f, 5.2f, 0.5f));
        PrimitiveKit.Cylinder("DomeDrum", b, new Vector3(0f, 5.5f, 0.5f), new Vector3(5.8f, 0.3f, 5.8f), white, collider: false);
        // Arched entrance and windows
        PrimitiveKit.Box("Door", b, new Vector3(0f, 1.6f, -4.03f), new Vector3(2f, 3.2f, 0.06f), green, collider: false);
        MeshKit.Place("DoorArch", b, MeshKit.Lathe("ArchTop", new[] { new Vector2(1f, 0f), new Vector2(0.7f, 0.5f), new Vector2(0f, 0.8f) }, 12, false, false),
            green, new Vector3(0f, 3.2f, -4.05f), new Vector3(90f, 0f, 0f), new Vector3(1f, 0.05f, 1f));
        for (int i = 0; i < 2; i++)
            foreach (float x in new[] { -3.4f, 3.4f })
                PrimitiveKit.Box("Window", b, new Vector3(x, 1.8f + i * 2f, -4.03f), new Vector3(1f, 1.4f, 0.06f), PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f)), collider: false);
        // Minaret on the front-right corner
        MeshKit.Place("Minaret", b, MeshKit.Lathe("Minaret", new[]
        {
            new Vector2(0.9f, 0f), new Vector2(0.8f, 0.3f), new Vector2(0.65f, 9f), new Vector2(1f, 9.2f), new Vector2(1f, 9.6f),
            new Vector2(0.6f, 9.7f), new Vector2(0.55f, 11.5f),
        }, 12), white, new Vector3(5.6f, 0f, -3.6f));
        MeshKit.Place("MinaretCap", b, MeshKit.Lathe("MinaretCap", new[] { new Vector2(0.6f, 0f), new Vector2(0.5f, 0.6f), new Vector2(0.15f, 1.3f), new Vector2(0f, 1.6f) }, 12),
            green, new Vector3(5.6f, 11.5f, -3.6f));
        var crescent = MeshKit.Curve(new Vector3(0.25f, 0f, 0f), new Vector3(-0.45f, 0.35f, 0f), new Vector3(0.25f, 0.7f, 0f), 9);
        MeshKit.Place("Crescent", b, MeshKit.Sweep("Crescent", crescent, new[] { 0.02f, 0.05f, 0.07f, 0.08f, 0.08f, 0.08f, 0.07f, 0.05f, 0.02f }, 6),
            gold, new Vector3(5.6f, 13.2f, -3.6f));
        PrimitiveKit.Label("มัสยิด", b, new Vector3(0f, 4.3f, -4.08f), 0.08f, new Color(0.1f, 0.5f, 0.35f));
    }

    // The town temple (วัด): a smaller ordination hall and chedi inside a low white wall
    private static void CityTemple(Transform root, Vector3 p, float yaw)
    {
        var w = PrimitiveKit.Group("CityTemple_วัด", root, p, yaw).transform;
        var white = PrimitiveKit.Mat("TempleWhite", new Color(0.95f, 0.94f, 0.9f), 0.35f);
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f), 0.7f, 0.85f);
        var orange = PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f), 0.6f);
        var green = PrimitiveKit.Mat("TempleGreen", new Color(0.15f, 0.45f, 0.3f), 0.6f);
        var hall = PrimitiveKit.Group("Hall", w, new Vector3(-2f, 0f, 1.5f)).transform;
        hall.localScale = Vector3.one * 0.6f;
        Places.Ubosot(hall, Vector3.zero, white, gold, orange, green);
        var chedi = PrimitiveKit.Group("Chedi", w, new Vector3(6f, 0f, 5f)).transform;
        chedi.localScale = Vector3.one * 0.7f;
        Places.Chedi(chedi, Vector3.zero, white, gold);
        // Low wall with a gate at the front (-Z)
        PrimitiveKit.Box("WallBack", w, new Vector3(0f, 0.5f, 9f), new Vector3(20f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallL", w, new Vector3(-10f, 0.5f, 0f), new Vector3(0.3f, 1f, 18f), white);
        PrimitiveKit.Box("WallR", w, new Vector3(10f, 0.5f, 0f), new Vector3(0.3f, 1f, 18f), white);
        PrimitiveKit.Box("WallFrontL", w, new Vector3(-6.25f, 0.5f, -9f), new Vector3(7.5f, 1f, 0.3f), white);
        PrimitiveKit.Box("WallFrontR", w, new Vector3(6.25f, 0.5f, -9f), new Vector3(7.5f, 1f, 0.3f), white);
        PrimitiveKit.Box("GateL", w, new Vector3(-2.4f, 1.5f, -9f), new Vector3(0.45f, 3f, 0.45f), white);
        PrimitiveKit.Box("GateR", w, new Vector3(2.4f, 1.5f, -9f), new Vector3(0.45f, 3f, 0.45f), white);
        PrimitiveKit.Box("GateBeam", w, new Vector3(0f, 3.3f, -9f), new Vector3(5.4f, 0.7f, 0.3f), gold, collider: false);
        PrimitiveKit.Label("วัด", w, new Vector3(0f, 3.3f, -9.17f), 0.09f, new Color(0.6f, 0.1f, 0.1f));
        StreetFurniture.LampPost(w, new Vector3(-3.6f, 0f, -10.2f));
        StreetFurniture.LampPost(w, new Vector3(3.6f, 0f, -10.2f));
    }

    // Bank: stately facade with columns, a gold name band; the tellers are inside (Interiors "bank")
    private static void Bank(Transform root, Vector3 p, float yaw)
    {
        var b = Block(root, p, yaw, "ธนาคาร", 10f, 10f, 2, new Color(0.93f, 0.92f, 0.88f), new Color(0.35f, 0.3f, 0.55f),
            new Color(0.35f, 0.3f, 0.55f), new Color(1f, 0.85f, 0.4f), "Bank_ธนาคาร", "ATM 24 ชม.", "bank");
        var stone = PrimitiveKit.Mat("BankStone", new Color(0.97f, 0.96f, 0.92f));
        var column = MeshKit.Lathe("BankColumn", new[]
        {
            new Vector2(0.32f, 0f), new Vector2(0.32f, 0.25f), new Vector2(0.24f, 0.35f), new Vector2(0.22f, 5.6f),
            new Vector2(0.3f, 5.75f), new Vector2(0.34f, 5.9f),
        }, 14);
        foreach (float x in new[] { -4.3f, -2.7f, 2.7f, 4.3f }) // clear of the entrance canopy
            MeshKit.Place("Column", b, column, stone, new Vector3(x, 0f, -6.1f));
        PrimitiveKit.Box("Pediment", b, new Vector3(0f, 6.05f, -6.1f), new Vector3(9f, 0.35f, 1.2f), stone, collider: false);
        MeshKit.Place("Gable", b, MeshKit.Gable("BankGable", 9f, 1.4f, 0.9f), stone, new Vector3(0f, 6.2f, -6.55f));
        PrimitiveKit.Box("Band", b, new Vector3(0f, 6.4f, -7f), new Vector3(3.2f, 0.5f, 0.05f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);

        var zone = new GameObject("BankZone"); // at the tellers' counter inside
        zone.transform.SetParent(b, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 2f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4.2f, 2f, 2.4f);
        zone.AddComponent<BankCounter>();
        StreetFurniture.LampPost(b, new Vector3(-4.6f, 0f, -8.6f));
        StreetFurniture.LampPost(b, new Vector3(4.6f, 0f, -8.6f));
    }

    private static void Wheels(Transform vehicle, float x, float z, float radius)
    {
        var tyre = PrimitiveKit.Mat("Tyre", new Color(0.08f, 0.08f, 0.08f));
        foreach (float sx in new[] { -x, x })
        foreach (float sz in new[] { -z, z })
            PrimitiveKit.Cylinder("Wheel", vehicle, new Vector3(sx, radius, sz), new Vector3(radius * 2f, 0.12f, radius * 2f), tyre, new Vector3(0f, 0f, 90f), collider: false);
    }
}
