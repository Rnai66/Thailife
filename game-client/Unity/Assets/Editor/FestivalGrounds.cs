using UnityEngine;

// Event ground for the traditional festivals (ลานจัดงาน; the งานวัด itself is at the village temple). The arch stays all year and shows the
// next festival; everything under "Attractions" appears only while a festival of this size is
// on (FestivalGate): small = tambon (สอยดาว, ปาเป้า, หนังกลางแปลง, ลิเก), big = city
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
        Booth(attractions, new Vector3(-11f, 0f, -9f), "soidao", "สอยดาว", 20, PrimitiveKit.Mat("TentYellow", new Color(0.98f, 0.78f, 0.2f)));
        Booth(attractions, new Vector3(-11f, 0f, -4f), "darts", "ปาเป้าลูกโป่ง", 10, PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)));
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
        // Prizes on a back shelf: little coloured dolls and a gold star
        PrimitiveKit.Box("Shelf", b, new Vector3(0f, 1.2f, 0.9f), new Vector3(2.4f, 0.06f, 0.4f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
        Color[] prizes = { new Color(0.95f, 0.5f, 0.6f), new Color(0.5f, 0.75f, 0.95f), new Color(1f, 0.85f, 0.3f), new Color(0.6f, 0.9f, 0.55f) };
        for (int i = 0; i < 4; i++)
            MeshKit.Place("Prize", b, MeshKit.Blob("Prize", 61 + i, 0.15f, 1), PrimitiveKit.Mat($"Prize{i}", prizes[i]),
                new Vector3(-0.9f + i * 0.6f, 1.45f, 0.9f), default, new Vector3(0.35f, 0.45f, 0.3f));
        PrimitiveKit.Label(title, b, new Vector3(0f, 2.05f, -1.32f), 0.045f, Color.white);
        Zone(b, new Vector3(0f, 1f, -1.9f), new Vector3(2.6f, 2f, 1.6f)).gameObject.AddComponent<FestivalBooth>().Configure(game, title, price);
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
