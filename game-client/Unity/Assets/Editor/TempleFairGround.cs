using UnityEngine;

// ลานอเนกประสงค์ across the road from the village temple: an open packed-earth ground with lamps,
// free for trading or anything else on ordinary days. During a festival it becomes the temple fair's
// big night: a giant outdoor movie screen (หนังกลางแปลงจอใหญ่) at the far end, a ferris wheel, and a
// หมอลำ / กันตรึม stage with an LED backdrop, moving-head beams and lasers, dancers and a band.
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
        Places.Sign(g, new Vector3(-2.5f, 0f, hd - 0.5f), "ลานอเนกประสงค์\nวันงาน: หนังกลางแปลง ชิงช้าสวรรค์ หมอลำ", 180f);

        // Festival nights only
        var show = FairLights.Gated(g, "FairGroundShow", "any");
        BigMovie(show, new Vector3(1f, 0f, -hd + 2.2f));
        FestivalGrounds.FerrisWheel(show, new Vector3(hw - 4.2f, 0f, hd - 9.5f), "ferris_village");
        MorLamStage(g, new Vector3(-hw + 4.6f, 0f, 1.5f));
        // Strings of bulbs criss-crossing the ground
        var corners = new[] { new Vector3(-hw + 0.3f, 0f, hd - 0.3f), new Vector3(hw - 0.3f, 0f, hd - 0.3f), new Vector3(hw - 0.3f, 0f, -hd + 0.3f), new Vector3(-hw + 0.3f, 0f, -hd + 0.3f) };
        foreach (var c in corners) FairLights.Pole(show, c, 5.2f);
        FairLights.Festoon(show, corners[0] + Vector3.up * 5f, corners[2] + Vector3.up * 5f, 1.2f, 0.7f);
        FairLights.Festoon(show, corners[1] + Vector3.up * 5f, corners[3] + Vector3.up * 5f, 1.2f, 0.7f);
        FairLights.Festoon(show, corners[0] + Vector3.up * 5f, corners[1] + Vector3.up * 5f, 0.5f);
        for (int i = 0; i < 4; i++)
            FairLights.Lantern(show, new Vector3(-hw * 0.5f + i * hw / 3f, 4.4f, hd - 0.4f), i);
        Npcs.Crowd(g, new Vector3(2f, 0f, -2f), new Vector2(size.x * 0.45f, size.y * 0.55f), 14, "any", 0, 24,
            "หมอลำมันส์มาก!|ไปขึ้นชิงช้าสวรรค์กัน|หนังเรื่องนี้สนุก|เต้นด้วยกันเลย!|ปีหน้ามาอีกนะ|ซื้อลูกชิ้นก่อน");
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

    // เวทีหมอลำ / กันตรึม facing east (+X): deck, LED wall, speaker towers, the light rig (moving
    // heads, uplights, lasers — ConcertLights), singer, dancers (หางเครื่อง) and band. Has its own
    // FestivalGate so the หมอลำ loop plays nearby while it's on.
    private static void MorLamStage(Transform parent, Vector3 position)
    {
        var st = PrimitiveKit.Group("MorLamStage_เวทีหมอลำ", parent, position, -90f).transform;
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
        PrimitiveKit.Label("หมอลำซิ่ง · กันตรึม", st, new Vector3(0f, 10f, 4.38f), 0.11f, new Color(0.6f, 0.1f, 0.1f));

        var showRoot = PrimitiveKit.Group("StageShow", st, Vector3.zero);
        var show = PrimitiveKit.Group("Attractions", showRoot.transform, Vector3.zero).transform;
        showRoot.AddComponent<FestivalGate>().Configure("any", null, "morlam");
        // LED wall with glowing bands, speaker towers either side
        PrimitiveKit.Box("LedScreen", show, new Vector3(0f, 4.6f, 4.2f), new Vector3(12f, 5.4f, 0.2f), PrimitiveKit.Glow("LedMorLam", new Color(0.95f, 0.25f, 0.55f), 2.4f, 0.4f), collider: false);
        Color[] bands = { new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.85f, 1f), new Color(0.4f, 1f, 0.4f) };
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("LedBand", show, new Vector3(0f, 2.8f + i * 1.6f, 4.08f), new Vector3(11.6f, 0.25f, 0.02f), PrimitiveKit.Glow($"LedBand{i}", bands[i], 4f, 0.4f), collider: false);
        PrimitiveKit.Label("หมอลำซิ่ง", show, new Vector3(0f, 5.2f, 4.05f), 0.14f, Color.white);
        foreach (float x in new[] { -8.8f, 8.8f })
        {
            PrimitiveKit.Box("SpeakerStack", show, new Vector3(x, 2.2f, 0f), new Vector3(1.6f, 4.4f, 1.4f), black);
            for (int i = 0; i < 3; i++)
                PrimitiveKit.Cylinder("Cone", show, new Vector3(x, 0.9f + i * 1.4f, -0.72f), new Vector3(0.9f, 0.02f, 0.9f), steel, new Vector3(90f, 0f, 0f), collider: false);
        }
        // Band at the back: khaen, phin, drums; singer at the front with the dancers either side
        PrimitiveKit.Cylinder("BassDrum", show, new Vector3(3f, 1.65f, 2.8f), new Vector3(0.9f, 0.25f, 0.9f), PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("MicStand", show, new Vector3(0f, 2f, -1.6f), new Vector3(0.04f, 0.8f, 0.04f), steel, collider: false);
        int look = 2;
        foreach (var p in new[] { new Vector3(0f, 1.2f, -1.2f), new Vector3(-3f, 1.2f, 2.6f), new Vector3(3f, 1.2f, 2.2f), new Vector3(-0.6f, 1.2f, 3f) })
            PrimitiveKit.Group("Performer", show, p, 180f).AddComponent<MarketVendor>().Configure(look++);
        for (int i = 0; i < 6; i++) // หางเครื่อง in a line across the front
        {
            float x = (i < 3 ? -5.5f : 2.5f) + (i % 3) * 1.4f;
            PrimitiveKit.Group("Dancer", show, new Vector3(x, 1.2f, -0.4f), 180f).AddComponent<LikayDancer>().Configure(2 + i % 2);
        }
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

        // Stand in front of the stage to watch (free, once per festival, restores energy)
        var zone = new GameObject("PlayZone");
        zone.transform.SetParent(show, false);
        zone.transform.localPosition = new Vector3(0f, 1f, -5f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(8f, 2f, 3f);
        zone.AddComponent<FestivalBooth>().Configure("morlam", "หมอลำ · กันตรึม", 0);
    }
}
