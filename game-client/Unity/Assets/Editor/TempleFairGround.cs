using UnityEngine;

// ลานอเนกประสงค์ across the road from the village temple: an open packed-earth ground with lamps,
// free for trading or anything else on ordinary days. During a festival it becomes the temple fair's
// big night: a giant outdoor movie screen (หนังกลางแปลงจอใหญ่) at the far end, a ferris wheel, and the
// village รำวง stage (LED wall, moving-head beams and lasers, a band) where anyone can buy a ticket
// and dance round the flower table with the dancers, the ticket booth, the prize counter and the
// skill game booths (ยิงปืนจุก โยนห่วง ปาโป่ง).
// Faces the road (north, +Z).
public static class TempleFairGround
{
    public static void Build(Transform parent, Vector3 center, Vector2 size)
    {
        var g = PrimitiveKit.Group("FairGround_ลานอเนกประสงค์", parent, center).transform;
        float hw = size.x * 0.5f, hd = size.y * 0.5f;

        // Always there: the ground, a low kerb, lamps and the name sign
        PrimitiveKit.Box("Ground", g, new Vector3(0f, 0.012f, 0f), new Vector3(size.x, 0.02f, size.y), PrimitiveKit.Mat("Laterite", new Color(0.72f, 0.5f, 0.34f)), collider: false);
        var kerb = PrimitiveKit.Mat("Concrete", new Color(0.7f, 0.7f, 0.68f));
        foreach (float x in new[] { -hw, hw })
            PrimitiveKit.Box("Kerb", g, new Vector3(x, 0.08f, 0f), new Vector3(0.25f, 0.16f, size.y), kerb, collider: false);
        PrimitiveKit.Box("Kerb", g, new Vector3(0f, 0.08f, -hd), new Vector3(size.x, 0.16f, 0.25f), kerb, collider: false);
        foreach (var p in new[] { new Vector3(-hw + 0.6f, 0f, hd - 0.6f), new Vector3(hw - 0.6f, 0f, hd - 0.6f), new Vector3(-hw + 0.6f, 0f, -hd + 0.6f), new Vector3(hw - 0.6f, 0f, -hd + 0.6f), new Vector3(-hw + 0.6f, 0f, 0f), new Vector3(hw - 0.6f, 0f, 0f) })
            StreetFurniture.LampPost(g, p);
        Places.Sign(g, new Vector3(-2.5f, 0f, hd - 0.5f), "ลานอเนกประสงค์\nวันงาน: หนังกลางแปลง ชิงช้าสวรรค์ รำวง\nเกมฝีมือ ซุ้มแลกรางวัล", 180f);

        // Festival nights only
        var show = FairLights.Gated(g, "FairGroundShow", "any");
        BigMovie(show, new Vector3(1f, 0f, -hd + 2.2f));
        FestivalGrounds.FerrisWheel(show, new Vector3(hw - 4.2f, 0f, hd - 9.5f), "ferris_village");
        RamwongStage(g, new Vector3(-hw + 4.6f, 0f, 1.5f));
        // Ticket booth and prize counter by the road, the three skill booths in a row in the middle
        var games = FairLights.Gated(g, "FairGroundGames", "any");
        FestivalGrounds.Booth(games, new Vector3(-5.5f, 0f, hd - 4f), "tickets", "ซุ้มขายบัตร", 0, PrimitiveKit.Mat("TentYellow", new Color(0.95f, 0.75f, 0.2f)), 180f);
        FestivalGrounds.PrizeCounter(games, new Vector3(2.5f, 0f, hd - 4f), 180f);
        FestivalGrounds.Booth(games, new Vector3(1.5f, 0f, 1f), "shooting", "ยิงปืนจุก", 0, PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), 0f);
        FestivalGrounds.Booth(games, new Vector3(5f, 0f, 1f), "rings", "โยนห่วง", 0, PrimitiveKit.Mat("TentGreen", new Color(0.25f, 0.62f, 0.35f)), 0f);
        FestivalGrounds.Booth(games, new Vector3(8.5f, 0f, 1f), "darts", "ปาโป่ง", 0, PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)), 0f);
        // Strings of bulbs criss-crossing the ground
        var corners = new[] { new Vector3(-hw + 0.3f, 0f, hd - 0.3f), new Vector3(hw - 0.3f, 0f, hd - 0.3f), new Vector3(hw - 0.3f, 0f, -hd + 0.3f), new Vector3(-hw + 0.3f, 0f, -hd + 0.3f) };
        foreach (var c in corners) FairLights.Pole(show, c, 5.2f);
        FairLights.Festoon(show, corners[0] + Vector3.up * 5f, corners[2] + Vector3.up * 5f, 1.2f, 0.7f);
        FairLights.Festoon(show, corners[1] + Vector3.up * 5f, corners[3] + Vector3.up * 5f, 1.2f, 0.7f);
        FairLights.Festoon(show, corners[0] + Vector3.up * 5f, corners[1] + Vector3.up * 5f, 0.5f);
        for (int i = 0; i < 4; i++)
            FairLights.Lantern(show, new Vector3(-hw * 0.5f + i * hw / 3f, 4.4f, hd - 0.4f), i);
        Npcs.Crowd(g, new Vector3(2f, 0f, -2f), new Vector2(size.x * 0.45f, size.y * 0.55f), 14, "any", 0, 24,
            "ไปรำวงกัน!|ไปขึ้นชิงช้าสวรรค์กัน|หนังเรื่องนี้สนุก|ซื้อบัตรรำวงมาแล้ว|ปีหน้ามาอีกนะ|ซื้อลูกชิ้นก่อน");
    }

    // Thai vowels like ำ need the game font (Mitr); the built-in one drops them
    private static void Thai(TextMesh label)
    {
        var font = Resources.Load<Font>("Fonts/Mitr-SemiBold");
        if (font == null) return;
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    // หนังกลางแปลงจอใหญ่: a huge white screen on scaffold poles, the film flickering on it, mats on the
    // ground in front (+Z) and the projector stand at the back of the crowd
    private static void BigMovie(Transform parent, Vector3 position)
    {
        var m = PrimitiveKit.Group("BigMovie_หนังกลางแปลง", parent, position, 180f).transform; // audience to the north
        var steel = PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f));
        foreach (float x in new[] { -7f, 7f })
        {
            PrimitiveKit.Box("ScreenPole", m, new Vector3(x, 4.5f, 0.1f), new Vector3(0.25f, 9f, 0.25f), steel);
            PrimitiveKit.Box("Stay", m, new Vector3(x, 3f, 1.6f), new Vector3(0.08f, 6.4f, 0.08f), steel, new Vector3(-28f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("ScreenFrame", m, new Vector3(0f, 5f, 0.06f), new Vector3(13.6f, 7.4f, 0.08f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        var screen = PrimitiveKit.Box("Screen", m, new Vector3(0f, 5f, 0f), new Vector3(13f, 6.8f, 0.06f), PrimitiveKit.Glow("MovieScreen", new Color(0.9f, 0.9f, 0.85f), 1.2f), collider: false);
        screen.AddComponent<MovieScreen>();
        var mat = PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f));
        for (int r = 0; r < 2; r++)
            for (int i = 0; i < 4; i++)
                PrimitiveKit.Box("Mat", m, new Vector3(-4.5f + i * 3f, 0.03f, -6f - r * 2.6f), new Vector3(2.4f, 0.02f, 2.2f), mat, collider: false);
        // Projector on a little table behind the mats
        PrimitiveKit.Box("ProjectorTable", m, new Vector3(0f, 0.45f, -11.5f), new Vector3(1.2f, 0.9f, 0.8f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        PrimitiveKit.Box("Projector", m, new Vector3(0f, 1.1f, -11.5f), new Vector3(0.5f, 0.4f, 0.7f), steel, collider: false);
        PrimitiveKit.Sphere("Lens", m, new Vector3(0f, 1.12f, -11.15f), Vector3.one * 0.18f, PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f));
        var zone = new GameObject("PlayZone");
        zone.transform.SetParent(m, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -7f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(12f, 2f, 5f);
        zone.AddComponent<FestivalBooth>().Configure("movie", "หนังกลางแปลง", 0);
    }

    // เวทีรำวงชาวบ้าน facing east (+X): deck, LED wall, speaker towers, the light rig (moving heads,
    // uplights, lasers — ConcertLights), a flower table in the middle with dancers circling it and the
    // band at the back. Has its own FestivalGate so the รำวง loop plays nearby while it's on; buy a
    // ticket at the front (RamwongStage) to join the circle.
    private static void RamwongStage(Transform parent, Vector3 position)
    {
        var st = PrimitiveKit.Group("RamwongStage_เวทีรำวง", parent, position, -90f).transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var black = PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
        // Frame and deck stay all year (an empty stage on ordinary days)
        PrimitiveKit.Box("StageDeck", st, new Vector3(0f, 0.6f, 1f), new Vector3(14f, 1.2f, 8f), PrimitiveKit.Mat("StageDeck", new Color(0.18f, 0.18f, 0.2f)));
        PrimitiveKit.Box("StageSkirt", st, new Vector3(0f, 0.6f, -3.02f), new Vector3(14f, 1.2f, 0.04f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), collider: false);
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussTower", st, new Vector3(x, 4.5f, 4.5f), new Vector3(0.5f, 9f, 0.5f), steel);
        PrimitiveKit.Box("TrussBeam", st, new Vector3(0f, 9f, 4.5f), new Vector3(15f, 0.5f, 0.5f), steel, collider: false);
        PrimitiveKit.Box("TrussFront", st, new Vector3(0f, 8.2f, -2.5f), new Vector3(15f, 0.4f, 0.4f), steel, collider: false);
        foreach (float x in new[] { -7.2f, 7.2f })
            PrimitiveKit.Box("TrussSide", st, new Vector3(x, 8.6f, 1f), new Vector3(0.4f, 0.4f, 7f), steel, collider: false);
        PrimitiveKit.Box("BannerBoard", st, new Vector3(0f, 10f, 4.5f), new Vector3(10f, 1.4f, 0.2f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("รำวงชาวบ้าน", st, new Vector3(0f, 10f, 4.38f), 0.09f, new Color(0.6f, 0.1f, 0.1f)));

        var showRoot = PrimitiveKit.Group("StageShow", st, Vector3.zero);
        var show = PrimitiveKit.Group("Attractions", showRoot.transform, Vector3.zero).transform;
        showRoot.AddComponent<FestivalGate>().Configure("any", null, "ramwong");
        // LED wall with glowing bands, speaker towers either side
        PrimitiveKit.Box("LedScreen", show, new Vector3(0f, 4.6f, 4.2f), new Vector3(12f, 5.4f, 0.2f), PrimitiveKit.Glow("LedMorLam", new Color(0.95f, 0.25f, 0.55f), 2.4f, 0.4f), collider: false);
        Color[] bands = { new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.85f, 1f), new Color(0.4f, 1f, 0.4f) };
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("LedBand", show, new Vector3(0f, 2.8f + i * 1.6f, 4.08f), new Vector3(11.6f, 0.25f, 0.02f), PrimitiveKit.Glow($"LedBand{i}", bands[i], 4f, 0.4f), collider: false);
        Thai(PrimitiveKit.Label("รำวง", show, new Vector3(0f, 5.2f, 4.05f), 0.13f, Color.white));
        foreach (float x in new[] { -8.8f, 8.8f })
        {
            PrimitiveKit.Box("SpeakerStack", show, new Vector3(x, 2.2f, 0f), new Vector3(1.6f, 4.4f, 1.4f), black);
            for (int i = 0; i < 3; i++)
                PrimitiveKit.Cylinder("Cone", show, new Vector3(x, 0.9f + i * 1.4f, -0.72f), new Vector3(0.9f, 0.02f, 0.9f), steel, new Vector3(90f, 0f, 0f), collider: false);
        }
        // Band at the back (โทน drum, ฉิ่ง, khaen, singer), clear of the dancing circle
        PrimitiveKit.Cylinder("ToneDrum", show, new Vector3(4.6f, 1.5f, 3.6f), new Vector3(0.5f, 0.3f, 0.5f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), collider: false);
        PrimitiveKit.Cylinder("MicStand", show, new Vector3(-5.6f, 2f, -1.4f), new Vector3(0.04f, 0.8f, 0.04f), steel, collider: false);
        int look = 2;
        foreach (var p in new[] { new Vector3(-5.6f, 1.2f, -1f), new Vector3(-4.6f, 1.2f, 3.7f), new Vector3(4.6f, 1.2f, 3f), new Vector3(5.8f, 1.2f, 3.7f) })
            PrimitiveKit.Group("Musician", show, p, 180f).AddComponent<MarketVendor>().Configure(look++);
        // The flower table (โต๊ะกลางวง) and the circle of dancers round it
        var table = PrimitiveKit.Group("FlowerTable_โต๊ะกลางวง", show, new Vector3(0f, 1.2f, 1f)).transform;
        PrimitiveKit.Cylinder("Table", table, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.4f, 0.9f), PrimitiveKit.Mat("TableCloth", new Color(0.95f, 0.85f, 0.4f)), collider: false);
        PrimitiveKit.Cylinder("Vase", table, new Vector3(0f, 0.95f, 0f), new Vector3(0.22f, 0.18f, 0.22f), PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f)), collider: false);
        Color[] blooms = { new Color(1f, 0.4f, 0.6f), new Color(1f, 0.85f, 0.2f), new Color(1f, 1f, 1f), new Color(0.95f, 0.3f, 0.3f) };
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            PrimitiveKit.Sphere("Flower", table, new Vector3(Mathf.Cos(a) * 0.16f, 1.25f + (i % 3) * 0.07f, Mathf.Sin(a) * 0.16f), Vector3.one * 0.13f,
                PrimitiveKit.Mat($"Bloom{i % blooms.Length}", blooms[i % blooms.Length]));
        }
        const float radius = 2.3f;
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Group("Dancer", show, new Vector3(0f, 2.2f, 1f), 0f).AddComponent<RamwongDancer>().Configure(2 + i % 2, table, radius, i * 60f);
        // Colour cans on the front truss with real lights, two follow-spot rings, then the full rig
        Color[] colors = { new Color(1f, 0.2f, 0.5f), new Color(0.2f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 1f, 0.5f), new Color(0.75f, 0.3f, 1f) };
        for (int i = 0; i < 7; i++)
        {
            var c = colors[i % colors.Length];
            float x = -6f + i * 2f;
            PrimitiveKit.Cylinder("StageCan", show, new Vector3(x, 7.8f, -2.5f), new Vector3(0.35f, 0.25f, 0.35f), PrimitiveKit.Glow($"StageCan{i % colors.Length}", c, 6f, 0.4f), collider: false);
            if (i % 2 == 0)
            {
                var lg = new GameObject("StageLight");
                lg.transform.SetParent(show, false);
                lg.transform.localPosition = new Vector3(x, 7.5f, -2.5f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = 14f;
                l.intensity = 6f;
                l.shadows = LightShadows.None;
            }
        }
        foreach (float x in new[] { -4f, 4f })
            StreetFurniture.RingLight(show, new Vector3(x, 8.6f, -1f));
        Seaside.ConcertRig(show);

        // Ticket booth at the front of the stage: buy a รำวง ticket, go up and join the circle
        PrimitiveKit.Box("TicketDesk", show, new Vector3(3.5f, 0.45f, -4.6f), new Vector3(1.6f, 0.9f, 0.7f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)));
        PrimitiveKit.Box("TicketSign", show, new Vector3(3.5f, 1.3f, -4.95f), new Vector3(1.6f, 0.5f, 0.05f), PrimitiveKit.Mat("SignYellow", new Color(1f, 0.85f, 0.15f)), collider: false);
        Thai(PrimitiveKit.Label("บัตรรำวง 20.-", show, new Vector3(3.5f, 1.3f, -4.99f), 0.032f, new Color(0.6f, 0.1f, 0.1f)));
        var zone = new GameObject("RamwongZone");
        zone.transform.SetParent(show, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -5f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(8f, 2f, 3f);
        zone.AddComponent<global::RamwongStage>().Configure(table, radius, 20);
    }
}
