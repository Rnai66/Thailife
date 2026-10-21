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
        // The two big pens (คอก) down the left side, entrances facing the middle: สอยดาว with its tree,
        // and มหาลัย ก.ไก่ set out like a classroom; the smaller booths elsewhere
        string scale = big ? "big" : "small";
        SoiDaoPen(attractions, new Vector3(-9.6f, 0f, -8.2f), scale);
        KokaiPen(attractions, new Vector3(-9.6f, 0f, 0.2f), scale);
        Booth(attractions, new Vector3(11.4f, 0f, 10.6f), "darts", "ปาโป่ง", 10, PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)), 90f);
        if (big) Booth(attractions, new Vector3(-11f, 0f, 6.4f), "bingo", "บิงโก", 30, PrimitiveKit.Mat("TentGreen", new Color(0.25f, 0.62f, 0.35f)));

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
    private static void Booth(Transform parent, Vector3 position, string game, string title, int price, Material canopy, float yaw = -90f)
    {
        var b = PrimitiveKit.Group("Booth_" + title, parent, position, yaw).transform; // -90: front faces +X (the middle)
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

    // Low wooden fence round a pen (คอก), open in the middle of the front (-Z) for the entrance
    private static void Fence(Transform t, float width, float depth, float gap)
    {
        var wood = PrimitiveKit.Mat("FenceWood", new Color(0.55f, 0.38f, 0.22f));
        float hw = width * 0.5f, hd = depth * 0.5f;
        void Side(Vector3 a, Vector3 b)
        {
            float len = Vector3.Distance(a, b);
            int posts = Mathf.Max(1, Mathf.RoundToInt(len / 1.1f));
            for (int i = 0; i <= posts; i++)
                PrimitiveKit.Box("FencePost", t, Vector3.Lerp(a, b, i / (float)posts) + Vector3.up * 0.55f, new Vector3(0.1f, 1.1f, 0.1f), wood, collider: false);
            var mid = (a + b) * 0.5f;
            bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            foreach (float y in new[] { 0.45f, 0.9f })
                PrimitiveKit.Box("FenceRail", t, mid + Vector3.up * y, alongX ? new Vector3(len, 0.07f, 0.05f) : new Vector3(0.05f, 0.07f, len), wood, collider: y > 0.5f);
        }
        Side(new Vector3(-hw, 0f, hd), new Vector3(hw, 0f, hd));
        Side(new Vector3(-hw, 0f, -hd), new Vector3(-hw, 0f, hd));
        Side(new Vector3(hw, 0f, -hd), new Vector3(hw, 0f, hd));
        Side(new Vector3(-hw, 0f, -hd), new Vector3(-gap * 0.5f, 0f, -hd));
        Side(new Vector3(gap * 0.5f, 0f, -hd), new Vector3(hw, 0f, -hd));
    }

    // Entrance arch over the pen's gate: two red posts and a gold name board with bulbs
    private static void PenArch(Transform t, float z, float gap, string title)
    {
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        var gold = PrimitiveKit.Mat("SignGold", new Color(1f, 0.78f, 0.3f), 0.8f, 0.85f);
        foreach (float x in new[] { -gap * 0.5f - 0.1f, gap * 0.5f + 0.1f })
            PrimitiveKit.Cylinder("ArchPost", t, new Vector3(x, 1.4f, z), new Vector3(0.12f, 1.4f, 0.12f), red);
        PrimitiveKit.Box("ArchBoard", t, new Vector3(0f, 2.95f, z), new Vector3(gap + 1.2f, 0.6f, 0.1f), gold, collider: false);
        PrimitiveKit.Label(title, t, new Vector3(0f, 2.95f, z - 0.07f), 0.055f, new Color(0.6f, 0.08f, 0.08f));
        for (int i = 0; i < 7; i++)
            PrimitiveKit.Sphere("Bulb", t, new Vector3(-gap * 0.5f - 0.5f + i * (gap + 1f) / 6f, 3.3f, z), Vector3.one * 0.1f,
                i % 2 == 0 ? PrimitiveKit.Glow("BulbRed", new Color(1f, 0.35f, 0.3f), 2.5f) : PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
    }

    // สอยดาว pen (คอกสอยดาว): a fenced pen with the big dry ticket tree (ต้นสอยดาว) in the middle, hung
    // with rolled paper tickets tied with coloured string; prize stands round the fence — temple merit
    // goods (มาม่า ผงซักฟอก กะละมัง…) and a raised podium of big prizes (ทีวี ตู้เย็น จักรยาน).
    // Villagers mill about inside trying their luck. Walk up to the tree to pick a ticket. Front faces +X.
    internal static void SoiDaoPen(Transform parent, Vector3 position, string scale)
    {
        var s = PrimitiveKit.Group("SoiDao_สอยดาว", parent, position, -90f).transform;
        const float W = 6.6f, D = 6f, Gap = 2f;
        var red = PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f));
        var wood = PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f));
        var bark = PrimitiveKit.Mat("DeadWood", new Color(0.45f, 0.38f, 0.32f), 0.1f);
        var paper = PrimitiveKit.Mat("TicketPaper", new Color(0.98f, 0.95f, 0.86f));
        var gold = PrimitiveKit.Mat("SignGold", new Color(1f, 0.78f, 0.3f), 0.8f, 0.85f);
        var white = PrimitiveKit.Mat("TrimWhite", new Color(0.97f, 0.96f, 0.92f));

        PrimitiveKit.Box("Floor", s, new Vector3(0f, 0.015f, 0f), new Vector3(W, 0.03f, D), PrimitiveKit.Mat("FairMat", new Color(0.75f, 0.2f, 0.2f)), collider: false);
        Fence(s, W, D, Gap);
        PenArch(s, -D * 0.5f, Gap, "สอยดาว");
        // Fairy lights criss-crossing over the pen
        foreach (float x in new[] { -W * 0.5f, W * 0.5f })
            FairLights.Pole(s, new Vector3(x, 0f, D * 0.5f), 3.4f);
        FairLights.Festoon(s, new Vector3(-W * 0.5f, 3.3f, D * 0.5f), new Vector3(Gap * 0.5f + 0.1f, 3.2f, -D * 0.5f), 0.5f);
        FairLights.Festoon(s, new Vector3(W * 0.5f, 3.3f, D * 0.5f), new Vector3(-Gap * 0.5f - 0.1f, 3.2f, -D * 0.5f), 0.5f);

        // ต้นสอยดาว: dry tree on a round dais in the middle
        var tree = PrimitiveKit.Group("DryTree_ต้นสอยดาว", s, new Vector3(0f, 0f, 0.2f)).transform;
        PrimitiveKit.Cylinder("Dais", tree, new Vector3(0f, 0.15f, 0f), new Vector3(1.6f, 0.15f, 1.6f), wood);
        PrimitiveKit.Cylinder("DaisCloth", tree, new Vector3(0f, 0.31f, 0f), new Vector3(1.62f, 0.02f, 1.62f), red, collider: false);
        var trunkPath = MeshKit.Curve(new Vector3(0f, 0.3f, 0f), new Vector3(0.15f, 1.6f, -0.05f), new Vector3(0f, 2.9f, 0f), 10);
        MeshKit.Place("Trunk", tree, MeshKit.Sweep("SoiDaoTrunk", trunkPath, MeshKit.Taper(10, 0.16f, 0.06f), 7), bark, Vector3.zero);
        Color[] ties = { new Color(0.95f, 0.35f, 0.55f), new Color(0.3f, 0.7f, 0.4f), new Color(0.3f, 0.55f, 0.95f), new Color(1f, 0.75f, 0.2f) };
        var stringMat = PrimitiveKit.Mat("RopeWhite", Color.white);
        int n = 0;
        for (int i = 0; i < 11; i++)
        {
            float h = 1.1f + i * 0.16f, yaw = i * 137.5f * Mathf.Deg2Rad, reach = 1.25f - i * 0.06f;
            var a = new Vector3(0f, h, 0f);
            var b = new Vector3(Mathf.Cos(yaw) * reach, h + 0.45f, Mathf.Sin(yaw) * reach);
            var mid = Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.15f;
            MeshKit.Place("Branch", tree, MeshKit.Sweep("SoiDaoBranch" + i, MeshKit.Curve(a, mid, b, 6), MeshKit.Taper(6, 0.06f, 0.015f), 5), bark, Vector3.zero);
            // a twig forking off near the tip
            var fork = Vector3.Lerp(a, b, 0.6f);
            var twig = fork + new Vector3(Mathf.Cos(yaw + 0.8f) * 0.4f, 0.35f, Mathf.Sin(yaw + 0.8f) * 0.4f);
            MeshKit.Place("Twig", tree, MeshKit.Sweep("SoiDaoTwig" + i, MeshKit.Curve(fork, (fork + twig) * 0.5f + Vector3.up * 0.05f, twig, 4), MeshKit.Taper(4, 0.025f, 0.01f), 4), bark, Vector3.zero);
            foreach (var at in new[] { Vector3.Lerp(a, b, 0.45f), Vector3.Lerp(a, b, 0.75f), b, twig })
            {
                float drop = 0.12f + (n % 3) * 0.07f;
                PrimitiveKit.Box("String", tree, at + Vector3.down * drop * 0.5f, new Vector3(0.008f, drop, 0.008f), stringMat, collider: false);
                PrimitiveKit.Cylinder("Ticket", tree, at + Vector3.down * (drop + 0.08f), new Vector3(0.045f, 0.08f, 0.045f), paper, collider: false);
                PrimitiveKit.Cylinder("Tie", tree, at + Vector3.down * (drop + 0.08f), new Vector3(0.05f, 0.014f, 0.05f), PrimitiveKit.Mat($"TicketTie{n % 4}", ties[n % 4]), collider: false);
                n++;
            }
        }
        Zone(s, new Vector3(0f, 1f, 0.2f), new Vector3(3.8f, 2f, 3.8f)).gameObject.AddComponent<FestivalBooth>().Configure("soidao", "สอยดาว", 40);

        // Back: stepped stand of merit goods
        for (int step = 0; step < 3; step++)
            PrimitiveKit.Box("Step", s, new Vector3(0f, 0.3f + step * 0.42f, D * 0.5f - 0.95f + step * 0.3f), new Vector3(4f, 0.08f, 0.32f), wood, collider: false);
        PrimitiveKit.Box("StandBack", s, new Vector3(0f, 0.9f, D * 0.5f - 0.25f), new Vector3(4.2f, 1.8f, 0.06f), PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)), collider: false);
        float z0 = D * 0.5f - 0.95f;
        for (int i = 0; i < 8; i++)
            PrimitiveKit.Box("Mama", s, new Vector3(-1.8f + i * 0.2f, 0.4f, z0), new Vector3(0.17f, 0.11f, 0.13f), PrimitiveKit.Mat("MamaYellow", new Color(1f, 0.8f, 0.15f)), collider: false);
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("Detergent", s, new Vector3(0.1f + i * 0.2f, 0.47f, z0), new Vector3(0.16f, 0.26f, 0.1f), PrimitiveKit.Mat("DetergentBlue", new Color(0.2f, 0.45f, 0.9f)), collider: false);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Toothpaste", s, new Vector3(1.3f + i * 0.22f, 0.37f, z0), new Vector3(0.2f, 0.05f, 0.05f), white, collider: false);
        for (int i = 0; i < 3; i++)
            MeshKit.Place("Basin", s, MeshKit.Lathe("Basin", new[] { new Vector2(0.12f, 0f), new Vector2(0.2f, 0.12f), new Vector2(0.18f, 0.12f), new Vector2(0.1f, 0.01f) }, 14, false, false),
                PrimitiveKit.Mat("BasinRed", new Color(0.9f, 0.25f, 0.25f)), new Vector3(-1.5f + i * 0.45f, 0.76f, z0 + 0.3f));
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Cylinder("Flask", s, new Vector3(0.0f + i * 0.28f, 0.92f, z0 + 0.3f), new Vector3(0.16f, 0.15f, 0.16f), PrimitiveKit.Mat("FlaskBlue", new Color(0.25f, 0.6f, 0.9f)), collider: false);
        PrimitiveKit.Box("Backpack", s, new Vector3(1.1f, 0.96f, z0 + 0.3f), new Vector3(0.32f, 0.38f, 0.2f), red, collider: false);
        PrimitiveKit.Box("Futon", s, new Vector3(1.6f, 0.86f, z0 + 0.3f), new Vector3(0.45f, 0.12f, 0.28f), PrimitiveKit.Mat("FutonPink", new Color(0.95f, 0.55f, 0.65f)), collider: false);
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("Mat", s, new Vector3(-1.4f + i * 0.5f, 1.22f, z0 + 0.6f), new Vector3(0.12f, 0.45f, 0.12f), PrimitiveKit.Mat("MatReed", new Color(0.85f, 0.72f, 0.45f)), collider: false);
        PrimitiveKit.Cylinder("RiceCooker", s, new Vector3(0.7f, 1.32f, z0 + 0.6f), new Vector3(0.32f, 0.14f, 0.32f), white, collider: false);
        PrimitiveKit.Cylinder("CookerLid", s, new Vector3(0.7f, 1.48f, z0 + 0.6f), new Vector3(0.3f, 0.03f, 0.3f), PrimitiveKit.Mat("CookerPink", new Color(0.9f, 0.35f, 0.5f)), collider: false);
        PrimitiveKit.Box("SpoonSet", s, new Vector3(1.3f, 1.3f, z0 + 0.6f), new Vector3(0.3f, 0.06f, 0.18f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)), collider: false);

        // Side: podium of the big prizes with gold ribbons
        var podium = PrimitiveKit.Group("BigPrizes_รางวัลใหญ่", s, new Vector3(W * 0.5f - 0.7f, 0f, 0.2f), 90f).transform; // faces the tree
        PrimitiveKit.Box("Podium", podium, new Vector3(0f, 0.3f, 0f), new Vector3(3.2f, 0.6f, 0.9f), red, collider: true);
        PrimitiveKit.Box("PodiumTrim", podium, new Vector3(0f, 0.61f, 0f), new Vector3(3.25f, 0.04f, 0.95f), gold, collider: false);
        PrimitiveKit.Box("Fridge", podium, new Vector3(-1.05f, 1.15f, 0f), new Vector3(0.6f, 1.1f, 0.55f), white, collider: false);
        PrimitiveKit.Box("FridgeLine", podium, new Vector3(-1.05f, 1.35f, -0.28f), new Vector3(0.56f, 0.02f, 0.01f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)), collider: false);
        PrimitiveKit.Box("TV", podium, new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 0.6f, 0.08f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        PrimitiveKit.Box("Screen", podium, new Vector3(0f, 1.0f, -0.045f), new Vector3(0.82f, 0.52f, 0.01f), PrimitiveKit.Glow("ScreenBlue", new Color(0.3f, 0.6f, 0.95f), 1f), collider: false);
        PrimitiveKit.Box("TVStand", podium, new Vector3(0f, 0.66f, 0f), new Vector3(0.3f, 0.08f, 0.2f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        var bike = PrimitiveKit.Group("Bicycle", podium, new Vector3(1.05f, 0.62f, 0f), 90f).transform;
        var tyre = PrimitiveKit.Mat("TyreRubber", new Color(0.1f, 0.1f, 0.11f), 0.35f);
        foreach (float z in new[] { -0.35f, 0.35f })
            PrimitiveKit.Cylinder("Wheel", bike, new Vector3(0f, 0.28f, z), new Vector3(0.55f, 0.02f, 0.55f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Frame", bike, new Vector3(0f, 0.45f, 0f), new Vector3(0.05f, 0.05f, 0.7f), PrimitiveKit.Mat("BikeRed", new Color(0.85f, 0.15f, 0.15f), 0.6f), collider: false);
        PrimitiveKit.Box("Seat", bike, new Vector3(0f, 0.62f, -0.15f), new Vector3(0.08f, 0.05f, 0.18f), tyre, collider: false);
        PrimitiveKit.Box("Bars", bike, new Vector3(0f, 0.68f, 0.3f), new Vector3(0.4f, 0.04f, 0.04f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)), collider: false);
        foreach (float x in new[] { -1.05f, 0f, 1.05f })
            PrimitiveKit.Box("Ribbon", podium, new Vector3(x, 1.85f, 0f), new Vector3(0.5f, 0.08f, 0.02f), gold, new Vector3(0f, 0f, 20f), collider: false);
        PrimitiveKit.Label("รางวัลใหญ่", podium, new Vector3(0f, 0.3f, -0.47f), 0.045f, new Color(1f, 0.9f, 0.4f));

        // Side: the host's ticket table by the gate
        PrimitiveKit.Box("TicketTable", s, new Vector3(-W * 0.5f + 0.6f, 0.4f, -D * 0.5f + 1f), new Vector3(0.8f, 0.8f, 1.2f), wood);
        PrimitiveKit.Box("TicketBox", s, new Vector3(-W * 0.5f + 0.6f, 0.95f, -D * 0.5f + 1f), new Vector3(0.4f, 0.3f, 0.4f), red, collider: false);
        var host = PrimitiveKit.Group("Host", s, new Vector3(-W * 0.5f + 0.35f, 0f, -D * 0.5f + 1.9f), 135f);
        host.AddComponent<MarketVendor>().Configure(5);

        Npcs.Crowd(s, new Vector3(-0.6f, 0f, -1.7f), new Vector2(3.5f, 1.2f), 3, scale, 0, 24,
            "ขอให้ได้ตู้เย็นที!|ได้มาม่าอีกแล้ว 555|สอยอีกใบ!|ใบนี้แหละ ใบนี้!|ได้กะละมังไปล้างจาน|เห็นจักรยานไหม สวยมาก");
    }

    // มหาลัย ก.ไก่ pen: set out like an open-air classroom under a tent — a blackboard at the back with
    // the called letters chalked on it, the host's desk with the letter drum (กลองหมุน), and rows of
    // benches with players hunched over their letter cards. Take the seat in the aisle to play.
    internal static void KokaiPen(Transform parent, Vector3 position, string scale)
    {
        var k = PrimitiveKit.Group("Kokai_มหาลัยก.ไก่", parent, position, -90f).transform;
        const float W = 6.6f, D = 6.6f, Gap = 1.8f;
        var wood = PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var white = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.95f, 0.92f));
        var blue = PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f));
        var paper = PrimitiveKit.Mat("TicketPaper", new Color(0.98f, 0.95f, 0.86f));

        PrimitiveKit.Box("Floor", k, new Vector3(0f, 0.015f, 0f), new Vector3(W, 0.03f, D), PrimitiveKit.Mat("FairMatGreen", new Color(0.3f, 0.5f, 0.3f)), collider: false);
        Fence(k, W, D, Gap);
        PenArch(k, -D * 0.5f, Gap, "มหาลัย ก.ไก่");
        // Striped tent roof
        foreach (float x in new[] { -W * 0.5f, W * 0.5f })
            foreach (float z in new[] { -D * 0.5f + 0.3f, D * 0.5f })
                PrimitiveKit.Cylinder("TentPole", k, new Vector3(x, 1.5f, z), new Vector3(0.07f, 1.5f, 0.07f), PrimitiveKit.Mat("TentPole", new Color(0.85f, 0.85f, 0.85f)));
        for (int i = 0; i < 8; i++)
            MeshKit.Place("Canopy", k, MeshKit.Gable("KokaiRoof", W / 8f, 0.8f, D - 0.2f), i % 2 == 0 ? blue : white, new Vector3(-W * 0.5f + W / 16f + i * W / 8f, 3f, -D * 0.5f + 0.3f));
        StreetFurniture.HangingLight(k, new Vector3(0f, 2.8f, 0.5f));
        StreetFurniture.HangingLight(k, new Vector3(0f, 2.8f, -1.6f));

        // Blackboard on legs at the back, letters chalked on it
        float bz = D * 0.5f - 0.4f;
        foreach (float x in new[] { -1.6f, 1.6f })
            PrimitiveKit.Box("BoardLeg", k, new Vector3(x, 1f, bz), new Vector3(0.08f, 2f, 0.08f), dark, collider: false);
        PrimitiveKit.Box("BoardFrame", k, new Vector3(0f, 1.55f, bz), new Vector3(3.4f, 1.5f, 0.08f), wood);
        PrimitiveKit.Box("Blackboard", k, new Vector3(0f, 1.55f, bz - 0.045f), new Vector3(3.2f, 1.3f, 0.01f), PrimitiveKit.Mat("Blackboard", new Color(0.12f, 0.28f, 0.2f)), collider: false);
        PrimitiveKit.Label("ก  ข  ค  ง  จ  ฉ\nช  ซ  ญ  ด  ต  ถ\nท  น  บ  ป  ผ", k, new Vector3(0f, 1.5f, bz - 0.06f), 0.045f, new Color(0.95f, 0.97f, 0.92f));
        PrimitiveKit.Box("TitleBoard", k, new Vector3(0f, 2.5f, bz), new Vector3(2.6f, 0.4f, 0.06f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        PrimitiveKit.Label("มหาลัย ก.ไก่", k, new Vector3(0f, 2.5f, bz - 0.04f), 0.045f, new Color(0.6f, 0.08f, 0.08f));

        // Host's desk with the letter drum on a stand, the host behind it
        PrimitiveKit.Box("HostDesk", k, new Vector3(-1.6f, 0.4f, bz - 1f), new Vector3(1.4f, 0.8f, 0.6f), wood);
        var drum = PrimitiveKit.Group("LetterDrum_กลองหมุน", k, new Vector3(-1.6f, 1.15f, bz - 1f)).transform;
        foreach (float x in new[] { -0.35f, 0.35f })
            PrimitiveKit.Box("DrumStand", drum, new Vector3(x, -0.1f, 0f), new Vector3(0.05f, 0.5f, 0.05f), dark, collider: false);
        PrimitiveKit.Cylinder("Drum", drum, new Vector3(0f, 0.1f, 0f), new Vector3(0.45f, 0.3f, 0.45f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Crank", drum, new Vector3(0.42f, 0.1f, -0.12f), new Vector3(0.04f, 0.04f, 0.24f), PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f)), collider: false);
        var host = PrimitiveKit.Group("Host", k, new Vector3(-1.6f, 0f, bz - 0.45f), 180f);
        host.AddComponent<MarketVendor>().Configure(4);

        // Rows of benches facing the board, a desk plank in front of each with the players' cards
        float[] rows = { 0.7f, -0.5f, -1.7f };
        int look = 0;
        for (int r = 0; r < rows.Length; r++)
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 1.75f, z = rows[r];
                PrimitiveKit.Box("Bench", k, new Vector3(x, 0.42f, z), new Vector3(2.2f, 0.06f, 0.32f), wood, collider: false);
                PrimitiveKit.Box("BenchLegs", k, new Vector3(x, 0.2f, z), new Vector3(2f, 0.4f, 0.05f), dark, collider: false);
                PrimitiveKit.Box("Desk", k, new Vector3(x, 0.72f, z + 0.45f), new Vector3(2.2f, 0.05f, 0.35f), wood, collider: false);
                PrimitiveKit.Box("DeskLegs", k, new Vector3(x, 0.36f, z + 0.45f), new Vector3(2f, 0.7f, 0.05f), dark, collider: false);
                foreach (float dx in new[] { -0.55f, 0.55f })
                {
                    // Leave the aisle-side front seat on the right free for the player
                    if (r == 1 && side > 0f && dx < 0f) continue;
                    PrimitiveKit.Box("Card", k, new Vector3(x + dx, 0.755f, z + 0.42f), new Vector3(0.3f, 0.01f, 0.22f), paper, collider: false);
                    var player = PrimitiveKit.Group("Player", k, new Vector3(x + dx, -0.4f, z - 0.05f));
                    player.AddComponent<MarketVendor>().Configure(look++ % 6);
                    player.AddComponent<SeatedPose>();
                }
            }
        // The free seat in the middle row: stand there to play
        Zone(k, new Vector3(0.9f, 1f, -0.5f), new Vector3(1.6f, 2f, 1.6f)).gameObject.AddComponent<FestivalBooth>().Configure("kokai", "มหาลัย ก.ไก่", 30);
        PrimitiveKit.Box("SeatMarker", k, new Vector3(1.2f, 0.46f, -0.5f), new Vector3(0.5f, 0.02f, 0.3f), PrimitiveKit.Glow("ClassMarker", new Color(1f, 0.78f, 0.3f), 0.6f), collider: false);
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
    internal static void FerrisWheel(Transform parent, Vector3 position, string game = "ferris")
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
        Zone(f, new Vector3(-2.6f, 1f, 0f), new Vector3(2f, 2f, 3f)).gameObject.AddComponent<FestivalBooth>().Configure(game, "ชิงช้าสวรรค์", 20, seat, 26f);
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
