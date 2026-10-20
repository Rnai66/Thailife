using UnityEngine;

// Event ground for the traditional festivals (ลานจัดงาน; the งานวัด itself is at the village temple). The arch stays all year and shows the
// next festival; everything under "Attractions" appears only while a festival of this size is
// on (FestivalGate): small = tambon (สอยดาว, ปาโป่ง, หนังกลางแปลง, ลิเก), big = city
// (+ บิงโก, ชิงช้าสวรรค์, ม้าหมุน). Built facing local -Z (the entrance), then turned to the road.
public static class FestivalGrounds
{
    public static void Build(Transform parent, Vector3 position, float yaw, bool big)
    {
        var root = PrimitiveKit.Group(big ? "FestivalGrounds_งานใหญ่" : "FestivalGrounds_งานเล็ก", parent, position, yaw);
        var t = root.transform;
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f));

        // Entrance arch with the festival banner (always there)
        PrimitiveKit.Box("ArchL", t, new Vector3(-3f, 2.2f, -12.5f), new Vector3(0.35f, 4.4f, 0.35f), red);
        PrimitiveKit.Box("ArchR", t, new Vector3(3f, 2.2f, -12.5f), new Vector3(0.35f, 4.4f, 0.35f), red);
        PrimitiveKit.Box("ArchBoard", t, new Vector3(0f, 4.4f, -12.5f), new Vector3(6.8f, 1.2f, 0.18f), gold, collider: false);
        var banner = PrimitiveKit.Label("ลานจัดงาน", t, new Vector3(0f, 4.4f, -12.62f), 0.07f, new Color(0.6f, 0.08f, 0.08f));
        foreach (var p in new[] { new Vector3(-12f, 0f, -12f), new Vector3(12f, 0f, -12f), new Vector3(-12f, 0f, 12f), new Vector3(12f, 0f, 12f) })
            StreetFurniture.LampPost(t, p);

        var attractions = PrimitiveKit.Group("Attractions", t, Vector3.zero).transform;
        root.AddComponent<FestivalGate>().Configure(big ? "big" : "small", banner);

        // Game booths down the left side, facing the middle
        SoiDaoStall(attractions, new Vector3(-11f, 0f, -9.2f));
        Booth(attractions, new Vector3(-11f, 0f, -4f), "darts", "ปาโป่ง", 10, PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)));
        Booth(attractions, new Vector3(-11f, 0f, 1f), "kokai", "มหาลัย ก.ไก่", 30, PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.95f, 0.92f)));
        if (big) Booth(attractions, new Vector3(-11f, 0f, 6f), "bingo", "บิงโก", 30, PrimitiveKit.Mat("TentGreen", new Color(0.25f, 0.62f, 0.35f)));

        Movie(attractions, new Vector3(0f, 0f, 11f));
        Likay(attractions, new Vector3(-8f, 0f, 10f));
        if (big)
        {
            FerrisWheel(attractions, new Vector3(10f, 0f, 3f));
            Carousel(attractions, new Vector3(8.5f, 0f, -6.5f));
        }

        // Fair-goers wandering the middle of the ground (only during the festival)
        Npcs.Crowd(t, new Vector3(-0.5f, 0f, -1f), new Vector2(7f, 20f), big ? 14 : 8, big ? "big" : "small", 0, 24, Npcs.FairTalk);

        // ประดับไฟ: bulbs around the edge, bunting and lanterns at the arch
        var corners = new[] { new Vector3(-13f, 0f, -13f), new Vector3(13f, 0f, -13f), new Vector3(13f, 0f, 13f), new Vector3(-13f, 0f, 13f) };
        foreach (var c in corners) FairLights.Pole(attractions, c);
        FairLights.Festoon(attractions, corners[1] + Vector3.up * 4.4f, corners[2] + Vector3.up * 4.4f, 0.5f);
        FairLights.Festoon(attractions, corners[2] + Vector3.up * 4.4f, corners[3] + Vector3.up * 4.4f, 0.5f);
        FairLights.Festoon(attractions, corners[3] + Vector3.up * 4.4f, corners[0] + Vector3.up * 4.4f, 0.5f);
        FairLights.Festoon(attractions, corners[0] + Vector3.up * 4.4f, new Vector3(-3f, 4.6f, -12.5f), 0.4f);
        FairLights.Festoon(attractions, new Vector3(3f, 4.6f, -12.5f), corners[1] + Vector3.up * 4.4f, 0.4f);
        FairLights.Bunting(attractions, new Vector3(-2.8f, 3.6f, -12.7f), new Vector3(2.8f, 3.6f, -12.7f), 0.15f);
        for (int i = 0; i < 3; i++)
            FairLights.Lantern(attractions, new Vector3(-2f + i * 2f, 3.75f, -12.35f), i);
        FairLights.Festoon(attractions, new Vector3(-1.2f, 4f, -12.3f), new Vector3(-1.2f, 4f, 12.5f), 0.25f);
        for (int i = 0; i < 5; i++) // lanterns down the middle aisle, under that string
            FairLights.Lantern(attractions, new Vector3(-1.2f, 3.4f, -10f + i * 4.5f), i);

        // String lights over the middle
        var wire = PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f));
        var bulb = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);
        for (int i = 0; i < 3; i++)
        {
            var a = new Vector3(-6f, 3.6f, -9f + i * 6f);
            var b = new Vector3(5f, 3.6f, -6f + i * 6f);
            PrimitiveKit.Cylinder("LightPole", attractions, new Vector3(a.x, 1.8f, a.z), new Vector3(0.08f, 1.8f, 0.08f), wire);
            PrimitiveKit.Cylinder("LightPole", attractions, new Vector3(b.x, 1.8f, b.z), new Vector3(0.08f, 1.8f, 0.08f), wire);
            var curve = MeshKit.Curve(a, (a + b) * 0.5f + Vector3.down * 0.5f, b, 10);
            MeshKit.Place("LightString", attractions, MeshKit.Sweep($"FairString{i}", curve, MeshKit.Taper(10, 0.012f, 0.012f), 4), wire, Vector3.zero);
            for (int k = 1; k < 9; k++)
                PrimitiveKit.Sphere("Bulb", attractions, curve[k] + Vector3.down * 0.07f, Vector3.one * 0.1f,
                    k % 3 == 0 ? PrimitiveKit.Glow("BulbRed", new Color(1f, 0.35f, 0.3f), 2.5f) : bulb);
        }
    }

    private static BoxCollider Zone(Transform parent, Vector3 local, Vector3 size, string name = "PlayZone")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        return box;
    }

    // Tent booth with a prize shelf; customers stand in front (local -Z of the booth)
    private static void Booth(Transform parent, Vector3 position, string game, string title, int price, Material canopy)
    {
        var b = PrimitiveKit.Group("Booth_" + title, parent, position, -90f).transform; // front faces +X (the middle)
        var pole = PrimitiveKit.Mat("TentPole", new Color(0.85f, 0.85f, 0.85f));
        foreach (float x in new[] { -1.3f, 1.3f })
        foreach (float z in new[] { -1.2f, 1.2f })
            PrimitiveKit.Cylinder("Pole", b, new Vector3(x, 1.15f, z), new Vector3(0.06f, 1.15f, 0.06f), pole);
        MeshKit.Place("Canopy", b, MeshKit.Gable("BoothRoof", 2.9f, 0.8f, 2.6f), canopy, new Vector3(0f, 2.3f, -1.3f));
        PrimitiveKit.Box("Counter", b, new Vector3(0f, 0.5f, -0.9f), new Vector3(2.4f, 1f, 0.5f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        Color[] prizes = { new Color(0.95f, 0.5f, 0.6f), new Color(0.5f, 0.75f, 0.95f), new Color(1f, 0.85f, 0.3f), new Color(0.6f, 0.9f, 0.55f) };
        if (game == "darts")
        {
            // Balloon board: a wooden wall of coloured balloons, a couple of darts stuck in it
            PrimitiveKit.Box("BalloonBoard", b, new Vector3(0f, 1.45f, 1.05f), new Vector3(2.4f, 1.5f, 0.08f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
            Color[] balloon = { new Color(0.95f, 0.25f, 0.3f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.6f, 1f), new Color(0.4f, 0.85f, 0.4f), new Color(0.9f, 0.4f, 0.9f) };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 6; c++)
                    PrimitiveKit.Sphere("Balloon", b, new Vector3(-0.95f + c * 0.38f, 0.95f + r * 0.45f, 0.95f), new Vector3(0.28f, 0.34f, 0.2f),
                        PrimitiveKit.Mat($"Balloon{(r + c) % 5}", balloon[(r + c) % 5], 0.7f));
            // Teddy bears for the three-pop prize on the counter
            for (int i = 0; i < 2; i++)
                MeshKit.Place("Teddy", b, MeshKit.Blob("Prize", 63, 0.15f, 1), PrimitiveKit.Mat("TeddyBrown", new Color(0.7f, 0.47f, 0.27f)),
                    new Vector3(-0.8f + i * 1.6f, 1.25f, -0.9f), default, new Vector3(0.3f, 0.4f, 0.28f));
        }
        else if (game == "bingo")
        {
            // Number board (1-75) and the ball cage on the counter
            PrimitiveKit.Box("NumberBoard", b, new Vector3(0f, 1.5f, 1.05f), new Vector3(2.4f, 1.3f, 0.06f), PrimitiveKit.Mat("Blackboard", new Color(0.12f, 0.28f, 0.2f)), collider: false);
            var lit = PrimitiveKit.Glow("BingoLit", new Color(1f, 0.85f, 0.3f), 1.5f);
            var dim = PrimitiveKit.Mat("BingoDim", new Color(0.85f, 0.85f, 0.8f));
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 15; c++)
                    PrimitiveKit.Box("Num", b, new Vector3(-1.05f + c * 0.15f, 1.9f - r * 0.2f, 1.01f), new Vector3(0.11f, 0.14f, 0.01f), (r * 7 + c * 3) % 5 == 0 ? lit : dim, collider: false);
            PrimitiveKit.Sphere("BallCage", b, new Vector3(0.7f, 1.3f, -0.9f), Vector3.one * 0.5f, PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)));
            for (int i = 0; i < 5; i++)
                PrimitiveKit.Sphere("Ball", b, new Vector3(0.6f + (i % 3) * 0.1f, 1.15f + (i / 3) * 0.1f, -0.9f), Vector3.one * 0.09f, PrimitiveKit.Mat($"Prize{i % 4}", prizes[i % 4]));
        }
        else
        {
            // Prizes on a back shelf: little coloured dolls
            PrimitiveKit.Box("Shelf", b, new Vector3(0f, 1.2f, 0.9f), new Vector3(2.4f, 0.06f, 0.4f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
            for (int i = 0; i < 4; i++)
                MeshKit.Place("Prize", b, MeshKit.Blob("Prize", 61 + i, 0.15f, 1), PrimitiveKit.Mat($"Prize{i}", prizes[i]),
                    new Vector3(-0.9f + i * 0.6f, 1.45f, 0.9f), default, new Vector3(0.35f, 0.45f, 0.3f));
        }
        PrimitiveKit.Label(title, b, new Vector3(0f, 2.05f, -1.32f), 0.045f, Color.white);
        Zone(b, new Vector3(0f, 1f, -1.9f), new Vector3(2.6f, 2f, 1.6f)).gameObject.AddComponent<FestivalBooth>().Configure(game, title, price);
    }

    // สอยดาว stall (its own booth): a dry, leafless tree hung with rolled-up paper tickets tied with
    // coloured string, and a stepped prize stand behind — temple merit goods at the bottom (มาม่า
    // ผงซักฟอก กะละมัง…), the big ones on top (หม้อหุงข้าว จักรยาน ทีวี ตู้เย็น). Front faces +X.
    private static void SoiDaoStall(Transform parent, Vector3 position)
    {
        var s = PrimitiveKit.Group("SoiDao_สอยดาว", parent, position, -90f).transform;
        var pole = PrimitiveKit.Mat("TentPole", new Color(0.85f, 0.85f, 0.85f));
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        var white = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.95f, 0.92f));
        var wood = PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f));
        var bark = PrimitiveKit.Mat("DeadWood", new Color(0.45f, 0.38f, 0.32f), 0.1f);
        var paper = PrimitiveKit.Mat("TicketPaper", new Color(0.98f, 0.95f, 0.86f));
        var gold = PrimitiveKit.Mat("SignGold", new Color(1f, 0.78f, 0.3f), 0.8f, 0.85f);

        // Tent: striped canopy on four poles
        foreach (float x in new[] { -2.1f, 2.1f })
            foreach (float z in new[] { -1.6f, 1.6f })
                PrimitiveKit.Cylinder("Pole", s, new Vector3(x, 1.35f, z), new Vector3(0.07f, 1.35f, 0.07f), pole);
        for (int i = 0; i < 6; i++)
            MeshKit.Place("Canopy", s, MeshKit.Gable("SoiDaoRoof", 0.75f, 0.7f, 3.6f), i % 2 == 0 ? red : white, new Vector3(-1.875f + i * 0.75f, 2.7f, -1.8f));
        PrimitiveKit.Box("SignBoard", s, new Vector3(0f, 3.25f, -1.85f), new Vector3(2.6f, 0.55f, 0.08f), gold, collider: false);
        PrimitiveKit.Label("สอยดาว", s, new Vector3(0f, 3.25f, -1.91f), 0.06f, new Color(0.6f, 0.08f, 0.08f));
        for (int i = 0; i < 9; i++)
            PrimitiveKit.Sphere("Bulb", s, new Vector3(-2f + i * 0.5f, 2.62f, -1.75f), Vector3.one * 0.09f,
                i % 2 == 0 ? PrimitiveKit.Glow("BulbRed", new Color(1f, 0.35f, 0.3f), 2.5f) : PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        PrimitiveKit.Box("Counter", s, new Vector3(0f, 0.45f, -1.25f), new Vector3(4f, 0.9f, 0.45f), wood);

        // The dry tree in a pot on the counter's middle, tickets dangling from its twigs
        var tree = PrimitiveKit.Group("DryTree_ต้นไม้แห้ง", s, new Vector3(-0.9f, 0f, -0.4f)).transform;
        PrimitiveKit.Cylinder("Pot", tree, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.25f, 0.6f), red);
        MeshKit.Place("Trunk", tree, MeshKit.Sweep("DryTrunk", MeshKit.Curve(new Vector3(0f, 0.4f, 0f), new Vector3(0.1f, 1.2f, 0f), new Vector3(0f, 1.9f, 0f), 8), MeshKit.Taper(8, 0.09f, 0.05f), 6), bark, Vector3.zero);
        var twigs = new[]
        {
            (new Vector3(0f, 1.2f, 0f), new Vector3(-0.6f, 1.7f, 0.1f)), (new Vector3(0f, 1.3f, 0f), new Vector3(0.65f, 1.85f, -0.1f)),
            (new Vector3(0f, 1.6f, 0f), new Vector3(-0.35f, 2.25f, -0.2f)), (new Vector3(0f, 1.7f, 0f), new Vector3(0.4f, 2.3f, 0.2f)),
            (new Vector3(0f, 1.0f, 0f), new Vector3(0.5f, 1.35f, 0.3f)), (new Vector3(0f, 1.05f, 0f), new Vector3(-0.5f, 1.3f, -0.3f)),
            (new Vector3(0f, 1.85f, 0f), new Vector3(0.05f, 2.45f, 0f)),
        };
        Color[] ties = { new Color(0.95f, 0.35f, 0.55f), new Color(0.3f, 0.7f, 0.4f), new Color(0.3f, 0.55f, 0.95f), new Color(1f, 0.75f, 0.2f) };
        var stringMat = PrimitiveKit.Mat("RopeWhite", Color.white);
        int n = 0;
        for (int i = 0; i < twigs.Length; i++)
        {
            var (a, b) = twigs[i];
            var mid = Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.08f;
            MeshKit.Place("Twig", tree, MeshKit.Sweep("DryTwig" + i, MeshKit.Curve(a, mid, b, 5), MeshKit.Taper(5, 0.035f, 0.012f), 5), bark, Vector3.zero);
            for (int k = 1; k <= 3; k++)
            {
                var at = Vector3.Lerp(a, b, 0.3f + k * 0.22f);
                float drop = 0.1f + ((i + k) % 3) * 0.05f;
                PrimitiveKit.Box("String", tree, at + Vector3.down * drop * 0.5f, new Vector3(0.008f, drop, 0.008f), stringMat, collider: false);
                PrimitiveKit.Cylinder("Ticket", tree, at + Vector3.down * (drop + 0.07f), new Vector3(0.035f, 0.07f, 0.035f), paper, collider: false);
                PrimitiveKit.Cylinder("Tie", tree, at + Vector3.down * (drop + 0.07f), new Vector3(0.04f, 0.012f, 0.04f), PrimitiveKit.Mat($"TicketTie{n % 4}", ties[n % 4]), collider: false);
                n++;
            }
        }

        // Stepped prize stand at the back
        for (int step = 0; step < 3; step++)
            PrimitiveKit.Box("Step", s, new Vector3(0.8f, 0.3f + step * 0.45f, 0.4f + step * 0.4f), new Vector3(2.6f, 0.08f, 0.4f), wood, collider: false);
        PrimitiveKit.Box("StandBack", s, new Vector3(0.8f, 0.8f, 1.3f), new Vector3(2.6f, 1.6f, 0.06f), PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)));
        // Bottom: merit goods — instant-noodle packs, detergent boxes, toothpaste, a basin
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("Mama", s, new Vector3(-0.3f + i * 0.17f, 0.4f + (i % 2) * 0.1f, 0.4f), new Vector3(0.15f, 0.1f, 0.12f), PrimitiveKit.Mat("MamaYellow", new Color(1f, 0.8f, 0.15f)), collider: false);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Detergent", s, new Vector3(0.5f + i * 0.2f, 0.48f, 0.4f), new Vector3(0.16f, 0.26f, 0.1f), PrimitiveKit.Mat("DetergentBlue", new Color(0.2f, 0.45f, 0.9f)), collider: false);
        MeshKit.Place("Basin", s, MeshKit.Lathe("Basin", new[] { new Vector2(0.12f, 0f), new Vector2(0.2f, 0.12f), new Vector2(0.18f, 0.12f), new Vector2(0.1f, 0.01f) }, 14, false, false),
            PrimitiveKit.Mat("BasinRed", new Color(0.9f, 0.25f, 0.25f)), new Vector3(1.4f, 0.34f, 0.4f));
        PrimitiveKit.Box("Toothpaste", s, new Vector3(1.85f, 0.38f, 0.4f), new Vector3(0.22f, 0.06f, 0.06f), PrimitiveKit.Mat("TrimWhite", new Color(0.97f, 0.96f, 0.92f)), collider: false);
        // Middle: flasks, backpack, rice cooker
        for (int i = 0; i < 2; i++)
            PrimitiveKit.Cylinder("Flask", s, new Vector3(-0.3f + i * 0.25f, 0.92f, 0.8f), new Vector3(0.16f, 0.15f, 0.16f), PrimitiveKit.Mat("FlaskBlue", new Color(0.25f, 0.6f, 0.9f)), collider: false);
        PrimitiveKit.Box("Backpack", s, new Vector3(0.45f, 0.95f, 0.8f), new Vector3(0.32f, 0.38f, 0.2f), red, collider: false);
        PrimitiveKit.Cylinder("RiceCooker", s, new Vector3(1.1f, 0.9f, 0.8f), new Vector3(0.32f, 0.14f, 0.32f), PrimitiveKit.Mat("TrimWhite", new Color(0.97f, 0.96f, 0.92f)), collider: false);
        PrimitiveKit.Cylinder("CookerLid", s, new Vector3(1.1f, 1.06f, 0.8f), new Vector3(0.3f, 0.03f, 0.3f), PrimitiveKit.Mat("CookerPink", new Color(0.9f, 0.35f, 0.5f)), collider: false);
        PrimitiveKit.Box("Futon", s, new Vector3(1.75f, 0.86f, 0.8f), new Vector3(0.5f, 0.12f, 0.3f), PrimitiveKit.Mat("FutonPink", new Color(0.95f, 0.55f, 0.65f)), collider: false);
        // Top: the big prizes with gold ribbons — TV, fridge, bicycle
        PrimitiveKit.Box("TV", s, new Vector3(-0.2f, 1.6f, 1.2f), new Vector3(0.6f, 0.4f, 0.08f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        PrimitiveKit.Box("Screen", s, new Vector3(-0.2f, 1.6f, 1.155f), new Vector3(0.54f, 0.34f, 0.01f), PrimitiveKit.Glow("ScreenBlue", new Color(0.3f, 0.6f, 0.95f), 1f), collider: false);
        PrimitiveKit.Box("Fridge", s, new Vector3(0.55f, 1.9f, 1.2f), new Vector3(0.45f, 1f, 0.35f), PrimitiveKit.Mat("TrimWhite", new Color(0.97f, 0.96f, 0.92f)), collider: false);
        var bike = PrimitiveKit.Group("Bicycle", s, new Vector3(1.55f, 1.4f, 1.15f), 90f).transform;
        var tyre = PrimitiveKit.Mat("TyreRubber", new Color(0.1f, 0.1f, 0.11f), 0.35f);
        foreach (float z in new[] { -0.3f, 0.3f })
            PrimitiveKit.Cylinder("Wheel", bike, new Vector3(0f, 0.2f, z), new Vector3(0.4f, 0.02f, 0.4f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Frame", bike, new Vector3(0f, 0.35f, 0f), new Vector3(0.04f, 0.04f, 0.6f), PrimitiveKit.Mat("BikeRed", new Color(0.85f, 0.15f, 0.15f), 0.6f), collider: false);
        PrimitiveKit.Box("Seat", bike, new Vector3(0f, 0.48f, -0.12f), new Vector3(0.06f, 0.04f, 0.14f), tyre, collider: false);
        foreach (var x in new[] { -0.2f, 0.55f, 1.55f })
            PrimitiveKit.Box("Ribbon", s, new Vector3(x, 2.45f, 1.0f), new Vector3(0.08f, 0.3f, 0.02f), gold, collider: false);

        Zone(s, new Vector3(0f, 1f, -2.1f), new Vector3(3.6f, 2f, 1.4f)).gameObject.AddComponent<FestivalBooth>().Configure("soidao", "สอยดาว", 40);
    }

    // หนังกลางแปลง: big white screen on poles, mats on the grass in front
    private static void Movie(Transform parent, Vector3 position)
    {
        var m = PrimitiveKit.Group("OutdoorMovie_หนังกลางแปลง", parent, position).transform;
        var pole = PrimitiveKit.Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));
        foreach (float x in new[] { -3.8f, 3.8f })
            PrimitiveKit.Box("ScreenPole", m, new Vector3(x, 2.9f, 0f), new Vector3(0.18f, 5.8f, 0.18f), PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f)));
        var screen = PrimitiveKit.Box("Screen", m, new Vector3(0f, 3.6f, 0f), new Vector3(7.2f, 4f, 0.06f), PrimitiveKit.Glow("MovieScreen", new Color(0.9f, 0.9f, 0.85f), 1.2f), collider: false);
        screen.AddComponent<MovieScreen>();
        var mat = PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Mat", m, new Vector3(-2f + i * 2f, 0.02f, -5f), new Vector3(1.6f, 0.02f, 2.2f), mat, collider: false);
        Zone(m, new Vector3(0f, 1f, -5f), new Vector3(6.5f, 2f, 2.6f)).gameObject.AddComponent<FestivalBooth>().Configure("movie", "หนังกลางแปลง", 0);
    }

    // ลิเก: raised stage with a red-and-gold backdrop and two performers
    private static void Likay(Transform parent, Vector3 position)
    {
        var s = PrimitiveKit.Group("Likay_ลิเก", parent, position).transform;
        PrimitiveKit.Box("Stage", s, new Vector3(0f, 0.4f, 0f), new Vector3(6f, 0.8f, 4f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        PrimitiveKit.Box("Backdrop", s, new Vector3(0f, 2.6f, 1.9f), new Vector3(6f, 3.6f, 0.12f), PrimitiveKit.Mat("LikayRed", new Color(0.65f, 0.08f, 0.12f)));
        PrimitiveKit.Box("BackdropTrim", s, new Vector3(0f, 4.3f, 1.84f), new Vector3(6.2f, 0.35f, 0.14f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        MeshKit.Place("Arch", s, MeshKit.Gable("LikayArch", 6.4f, 1.2f, 0.14f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), new Vector3(0f, 4.45f, 1.78f));
        PrimitiveKit.Label("ลิเก", s, new Vector3(0f, 3.5f, 1.82f), 0.12f, new Color(1f, 0.85f, 0.35f));
        foreach (var (x, look) in new[] { (-1.2f, 2), (1.2f, 3) })
        {
            var dancer = new GameObject("Performer");
            dancer.transform.SetParent(s, false);
            dancer.transform.localPosition = new Vector3(x, 1.8f, 0.3f);
            dancer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            dancer.AddComponent<LikayDancer>().Configure(look);
        }
        StreetFurniture.RingLight(s, new Vector3(0f, 5f, -1.5f));
        Zone(s, new Vector3(0f, 1f, -3.2f), new Vector3(6f, 2f, 2.2f)).gameObject.AddComponent<FestivalBooth>().Configure("likay", "ลิเก", 0);
    }

    // ชิงช้าสวรรค์: wheel turning about X on an A-frame, eight cabins that stay level
    private static void FerrisWheel(Transform parent, Vector3 position)
    {
        var f = PrimitiveKit.Group("FerrisWheel_ชิงช้าสวรรค์", parent, position).transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        const float hub = 7f, radius = 5.6f;
        foreach (float x in new[] { -1.1f, 1.1f })
        foreach (float z in new[] { -2.4f, 2.4f })
            PrimitiveKit.Box("Leg", f, new Vector3(x, hub * 0.5f, z * 0.5f), new Vector3(0.18f, hub + 0.3f, 0.18f), steel, new Vector3(z > 0 ? -17f : 17f, 0f, 0f));
        var wheel = PrimitiveKit.Group("Wheel", f, new Vector3(0f, hub, 0f)).transform;
        wheel.gameObject.AddComponent<Spinner>().Configure(Vector3.right, 14f);
        var rim = MeshKit.Lathe("FerrisRim", new[] { new Vector2(radius - 0.12f, -0.08f), new Vector2(radius + 0.12f, -0.08f), new Vector2(radius + 0.12f, 0.08f), new Vector2(radius - 0.12f, 0.08f) }, 40, false, false);
        MeshKit.Place("Rim", wheel, rim, PrimitiveKit.Glow("FerrisNeon", new Color(1f, 0.45f, 0.75f), 5f, 0.4f), Vector3.zero, new Vector3(0f, 0f, 90f));
        // ประดับไฟ: a ring of bulbs on the rim, glowing spokes and a gold hub, all turning with the wheel
        Color[] rimColors = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.6f), new Color(0.35f, 0.8f, 1f), new Color(0.5f, 1f, 0.45f) };
        for (int k = 0; k < 40; k++)
        {
            float b = k * Mathf.PI * 2f / 40f;
            var bulb = PrimitiveKit.Sphere("RimBulb", wheel, new Vector3(0.2f, Mathf.Sin(b), Mathf.Cos(b)) * 1f + new Vector3(0f, Mathf.Sin(b), Mathf.Cos(b)) * (radius - 1f),
                Vector3.one * 0.24f, PrimitiveKit.Glow($"FerrisBulb{k % 4}", rimColors[k % 4], 18f, 0.3f));
            bulb.transform.localPosition = new Vector3(0.18f, Mathf.Sin(b) * radius, Mathf.Cos(b) * radius);
            bulb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        PrimitiveKit.Sphere("Hub", wheel, Vector3.zero, Vector3.one * 0.9f, PrimitiveKit.Glow("FerrisHub", new Color(1f, 0.8f, 0.35f), 6f, 0.5f));
        var hubLight = new GameObject("WheelLight");
        hubLight.transform.SetParent(f, false);
        hubLight.transform.localPosition = new Vector3(-1.5f, hub, 0f);
        var hl = hubLight.AddComponent<Light>();
        hl.type = LightType.Point;
        hl.color = new Color(1f, 0.5f, 0.8f);
        hl.range = 16f;
        hl.intensity = 18f;
        hl.shadows = LightShadows.None;
        foreach (float z in new[] { -3f, 3f })
        {
            var up = new GameObject("WheelUplight");
            up.transform.SetParent(f, false);
            up.transform.localPosition = new Vector3(-1.2f, 1.2f, z);
            var ul = up.AddComponent<Light>();
            ul.type = LightType.Point;
            ul.color = new Color(0.45f, 0.75f, 1f);
            ul.range = 9f;
            ul.intensity = 12f;
            ul.shadows = LightShadows.None;
        }
        Color[] cabinColors = { new Color(0.9f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 0.9f), new Color(0.95f, 0.8f, 0.25f), new Color(0.4f, 0.8f, 0.45f) };
        Transform seat = null;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            var spoke = new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a));
            PrimitiveKit.Box("Spoke", wheel, spoke * radius * 0.5f, new Vector3(0.08f, radius, 0.08f), steel, new Vector3(-a * Mathf.Rad2Deg + 90f, 0f, 0f), collider: false);
            PrimitiveKit.Box("SpokeGlow", wheel, spoke * radius * 0.5f + new Vector3(0.09f, 0f, 0f), new Vector3(0.05f, radius - 0.6f, 0.05f),
                PrimitiveKit.Glow("FerrisSpokeGlow", new Color(1f, 0.9f, 0.6f), 6f, 0.5f), new Vector3(-a * Mathf.Rad2Deg + 90f, 0f, 0f), collider: false);
            var cabin = PrimitiveKit.Group("Cabin", wheel, spoke * radius).transform;
            PrimitiveKit.Box("Car", cabin, new Vector3(0f, -0.75f, 0f), new Vector3(1.3f, 1f, 1.1f), PrimitiveKit.Mat($"Cabin{i % 4}", cabinColors[i % 4]), collider: false);
            PrimitiveKit.Box("Roof", cabin, new Vector3(0f, -0.15f, 0f), new Vector3(1.4f, 0.12f, 1.2f), steel, collider: false);
            if (i == 6) seat = PrimitiveKit.Group("Seat", cabin, new Vector3(0f, -1.1f, 0f)).transform; // bottom cabin at the start
        }
        Zone(f, new Vector3(-2.6f, 1f, 0f), new Vector3(2f, 2f, 3f)).gameObject.AddComponent<FestivalBooth>().Configure("ferris", "ชิงช้าสวรรค์", 20, seat, 26f);
    }

    // ม้าหมุน: turning platform with horses on poles under a striped roof
    private static void Carousel(Transform parent, Vector3 position)
    {
        var c = PrimitiveKit.Group("Carousel_ม้าหมุน", parent, position).transform;
        PrimitiveKit.Cylinder("Base", c, new Vector3(0f, 0.15f, 0f), new Vector3(6.6f, 0.15f, 6.6f), PrimitiveKit.Mat("CarouselBase", new Color(0.8f, 0.75f, 0.65f)));
        var turn = PrimitiveKit.Group("Turntable", c, new Vector3(0f, 0.3f, 0f)).transform;
        turn.gameObject.AddComponent<Spinner>().Configure(Vector3.up, 28f);
        PrimitiveKit.Cylinder("CentrePole", turn, new Vector3(0f, 1.6f, 0f), new Vector3(0.5f, 1.6f, 0.5f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        MeshKit.Place("Roof", turn, MeshKit.Lathe("CarouselRoof", new[] { new Vector2(3.4f, 0f), new Vector2(2.5f, 0.6f), new Vector2(0.4f, 1.3f), new Vector2(0f, 1.5f) }, 16),
            PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), new Vector3(0f, 3.2f, 0f));
        // ประดับไฟ: bulbs all round the roof edge, a glowing trim ring and pole, lights under the roof
        Color[] edge = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.4f), new Color(0.4f, 0.85f, 1f) };
        for (int k = 0; k < 28; k++)
        {
            float b = k * Mathf.PI * 2f / 28f;
            var bulb = PrimitiveKit.Sphere("RoofBulb", turn, new Vector3(Mathf.Cos(b) * 3.35f, 3.1f, Mathf.Sin(b) * 3.35f), Vector3.one * 0.22f,
                PrimitiveKit.Glow($"CarouselBulb{k % 3}", edge[k % 3], 18f, 0.3f));
            bulb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        MeshKit.Place("RoofTrim", turn, MeshKit.Lathe("CarouselTrim", new[] { new Vector2(3.3f, -0.12f), new Vector2(3.45f, -0.12f), new Vector2(3.45f, 0.12f), new Vector2(3.3f, 0.12f) }, 40, false, false),
            PrimitiveKit.Glow("CarouselTrimGlow", new Color(1f, 0.75f, 0.35f), 5f, 0.5f), new Vector3(0f, 3.25f, 0f));
        PrimitiveKit.Cylinder("PoleGlow", turn, new Vector3(0f, 1.6f, 0f), new Vector3(0.56f, 1.2f, 0.56f), PrimitiveKit.Glow("CarouselPoleGlow", new Color(1f, 0.8f, 0.4f), 3f, 0.6f), collider: false);
        foreach (var (pos, col) in new[] { (new Vector3(0f, 3f, 0f), new Color(1f, 0.82f, 0.55f)), (new Vector3(0f, 4.6f, 0f), new Color(1f, 0.45f, 0.7f)) })
        {
            var lg = new GameObject("CarouselLight");
            lg.transform.SetParent(c, false);
            lg.transform.localPosition = pos;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 10f;
            l.intensity = 16f;
            l.shadows = LightShadows.None;
        }
        Color[] horses = { Color.white, new Color(0.95f, 0.75f, 0.85f), new Color(0.75f, 0.85f, 1f) };
        Transform seat = null;
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            var h = PrimitiveKit.Group("Horse", turn, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 2.2f), a + 90f).transform;
            PrimitiveKit.Cylinder("Pole", h, new Vector3(0f, 1.6f, 0f), new Vector3(0.05f, 1.6f, 0.05f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
            var mat = PrimitiveKit.Mat($"Horse{i % 3}", horses[i % 3]);
            PrimitiveKit.Box("Body", h, new Vector3(0f, 1.1f, 0f), new Vector3(0.35f, 0.4f, 1f), mat, collider: false);
            PrimitiveKit.Box("Neck", h, new Vector3(0f, 1.45f, 0.42f), new Vector3(0.25f, 0.55f, 0.3f), mat, new Vector3(-25f, 0f, 0f), collider: false);
            if (i == 0) seat = PrimitiveKit.Group("Seat", h, new Vector3(0f, 1f, 0f)).transform;
        }
        Zone(c, new Vector3(0f, 1f, -4.2f), new Vector3(3f, 2f, 1.6f)).gameObject.AddComponent<FestivalBooth>().Configure("carousel", "ม้าหมุน", 10, seat, 16f);
    }
}
