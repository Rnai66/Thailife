using UnityEngine;

// หนังกลางแปลง, put on the way the travelling cinema troupes do it at a temple fair: a wide screen on
// a black truss with LED pixels chasing round it, the troupe's neon name over the top (พิกุลภาพยนตร์,
// the game's own troupe), speaker stacks either side with blue lights thumping to the soundtrack,
// LED pixel tubes standing round the crowd, a row of white lamps under the screen, mats on the
// ground and the projector booth with its neon sign behind the audience. The film itself is drawn
// on the screen (MovieScreen), the lights run by LedChase, and a FestivalGate plays the film's
// soundtrack ("movie") to anyone nearby while the fair is on.
// The audience is on local -Z. `big`: the temple fair's giant screen; otherwise the event grounds' one.
public static class OutdoorCinema
{
    public const string Troupe = "พิกุลภาพยนตร์";

    private static Material Black => PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));
    private static Material Grille => PrimitiveKit.Mat("PK_CinemaGrille", new Color(0.16f, 0.16f, 0.18f), 0.3f);
    private static Material Bulb => PrimitiveKit.Glow("PK_LedPixel", Color.white, 1.5f, 0.8f);

    public static Transform Build(Transform parent, Vector3 position, float yaw, bool big, string gateScale)
    {
        float s = big ? 1f : 0.6f;
        float w = 12f * s, h = 6f * s, bottom = 1.6f * s, top = bottom + h;
        var root = PrimitiveKit.Group(big ? "BigMovie_หนังกลางแปลง" : "OutdoorMovie_หนังกลางแปลง", parent, position, yaw);
        var m = PrimitiveKit.Group("Attractions", root.transform, Vector3.zero).transform;
        root.AddComponent<FestivalGate>().Configure(gateScale, null, "movie");

        // ── the screen on its truss ──
        foreach (float x in new[] { -w * 0.5f - 0.5f * s, w * 0.5f + 0.5f * s })
        {
            PrimitiveKit.Box("TrussPost", m.transform, new Vector3(x, (top + 0.9f * s) * 0.5f, 0.15f), new Vector3(0.3f * s, top + 0.9f * s, 0.3f * s), Black);
            PrimitiveKit.Box("Stay", m, new Vector3(x, top * 0.4f, 1.4f * s), new Vector3(0.08f, top * 0.85f, 0.08f), Black, new Vector3(-28f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("TrussTop", m, new Vector3(0f, top + 0.75f * s, 0.15f), new Vector3(w + 1.3f * s, 0.3f * s, 0.3f * s), Black, collider: false);
        PrimitiveKit.Box("ScreenBack", m, new Vector3(0f, bottom + h * 0.5f, 0.08f), new Vector3(w + 0.5f * s, h + 0.5f * s, 0.08f), Black, collider: false);
        var film = GameObject.CreatePrimitive(PrimitiveType.Quad);
        film.name = "Film";
        Object.DestroyImmediate(film.GetComponent<Collider>());
        film.transform.SetParent(m, false);
        film.transform.localPosition = new Vector3(0f, bottom + h * 0.5f, 0.02f);
        film.transform.localScale = new Vector3(w, h, 1f);
        var filmRenderer = film.GetComponent<MeshRenderer>();
        filmRenderer.sharedMaterial = PrimitiveKit.Glow("PK_CinemaFilm", Color.white, 1.1f, 0.25f);
        filmRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        film.AddComponent<MovieScreen>();

        // LED pixels all round the screen, a rainbow running round them
        var border = new System.Collections.Generic.List<Renderer>();
        float gap = 0.42f * s, hw = w * 0.5f + 0.18f * s, y0 = bottom - 0.18f * s, y1 = top + 0.18f * s;
        for (float x = -hw; x <= hw + 0.01f; x += gap) border.Add(Pixel(m, new Vector3(x, y1, -0.05f), s));
        for (float y = y1 - gap; y >= y0 - 0.01f; y -= gap) border.Add(Pixel(m, new Vector3(hw, y, -0.05f), s));
        for (float x = hw - gap; x >= -hw - 0.01f; x -= gap) border.Add(Pixel(m, new Vector3(x, y0, -0.05f), s));
        for (float y = y0 + gap; y < y1 - 0.01f; y += gap) border.Add(Pixel(m, new Vector3(-hw, y, -0.05f), s));
        m.gameObject.AddComponent<LedChase>().Configure(border.ToArray(), LedChase.Mode.Rainbow, Color.white, Color.white, 1.2f);

        // the skirt under the screen with a row of white lamps pulsing to the beat
        PrimitiveKit.Box("Skirt", m, new Vector3(0f, bottom * 0.5f - 0.1f * s, -0.1f), new Vector3(w + 0.5f * s, bottom - 0.2f * s, 0.1f), Black, collider: false);
        var lamps = new System.Collections.Generic.List<Renderer>();
        for (float x = -w * 0.5f + 0.4f; x <= w * 0.5f - 0.3f; x += 0.9f * s)
            lamps.Add(PrimitiveKit.Sphere("Lamp", m, new Vector3(x, 0.3f * s, -0.18f), Vector3.one * 0.16f * s, Bulb).GetComponent<Renderer>());
        var lampRig = PrimitiveKit.Group("SkirtLamps", m, Vector3.zero);
        lampRig.AddComponent<LedChase>().Configure(lamps.ToArray(), LedChase.Mode.Pulse, new Color(0.85f, 0.9f, 1f), Color.white, 1.05f);

        // ── the troupe's neon name over the screen ──
        var sign = PrimitiveKit.Group("NeonSign", m, new Vector3(0f, top + 1.6f * s, 0f)).transform;
        PrimitiveKit.Box("Board", sign, new Vector3(0f, 0f, 0.1f), new Vector3(6.4f * s, 1.3f * s, 0.12f), Black, collider: false);
        var neon = new System.Collections.Generic.List<Renderer>();
        foreach (float y in new[] { -0.6f * s, 0.6f * s })
            neon.Add(PrimitiveKit.Box("Tube", sign, new Vector3(0f, y, 0f), new Vector3(6.2f * s, 0.07f, 0.07f), Bulb, collider: false).GetComponent<Renderer>());
        foreach (float x in new[] { -3.1f * s, 3.1f * s })
            neon.Add(PrimitiveKit.Box("Tube", sign, new Vector3(x, 0f, 0f), new Vector3(0.07f, 1.2f * s, 0.07f), Bulb, collider: false).GetComponent<Renderer>());
        sign.gameObject.AddComponent<LedChase>().Configure(neon.ToArray(), LedChase.Mode.Swap, new Color(1f, 0.25f, 0.7f), new Color(0.2f, 0.9f, 1f), 0.6f);
        PrimitiveKit.Label(Troupe, sign, new Vector3(0f, 0.05f, -0.02f), 0.075f * s, new Color(1f, 0.85f, 0.25f));
        PrimitiveKit.Label("หนังกลางแปลง · ฉายทุกคืนวันงาน", m, new Vector3(0f, top + 0.45f * s, -0.12f), 0.03f * s, new Color(1f, 1f, 1f));

        // ── speaker stacks either side, blue lights thumping ──
        foreach (float side in new[] { -1f, 1f })
            SpeakerStack(m, new Vector3(side * (w * 0.5f + (big ? 2.2f : 1.1f)), 0f, -0.4f), s, big ? 4 : 3);

        // ── LED pixel tubes round the crowd ──
        var tubes = big
            ? new[] { new Vector3(-w * 0.5f - 1f, 0f, -4f), new Vector3(w * 0.5f + 1f, 0f, -4f), new Vector3(-w * 0.5f - 1f, 0f, -9f), new Vector3(w * 0.5f + 1f, 0f, -9f), new Vector3(-w * 0.5f + 1f, 0f, -13f), new Vector3(w * 0.5f - 1f, 0f, -13f) }
            : new[] { new Vector3(-w * 0.5f - 0.4f, 0f, -3.4f), new Vector3(w * 0.5f + 0.4f, 0f, -3.4f) };
        for (int i = 0; i < tubes.Length; i++)
            PixelTube(m, tubes[i], big ? 4.2f : 3f, i);

        // ── mats on the ground and the projector booth behind them ──
        var mat = PrimitiveKit.Mat("Mat", new Color(0.85f, 0.7f, 0.4f));
        int rows = big ? 2 : 1, perRow = big ? 4 : 3;
        for (int r = 0; r < rows; r++)
            for (int i = 0; i < perRow; i++)
                PrimitiveKit.Box("Mat", m, new Vector3((i - (perRow - 1) * 0.5f) * (big ? 3f : 2f), 0.03f, (big ? -6f : -4.6f) - r * 2.6f), new Vector3(big ? 2.4f : 1.6f, 0.02f, 2.2f), mat, collider: false);
        Booth(m, new Vector3(0f, 0f, big ? -12f : -7.4f), w, bottom, top, big);

        var zone = new GameObject("PlayZone");
        zone.transform.SetParent(m, false);
        zone.transform.localPosition = new Vector3(0f, 1f, big ? -7f : -4.8f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = big ? new Vector3(12f, 2f, 5f) : new Vector3(6.5f, 2f, 2.6f);
        zone.AddComponent<FestivalBooth>().Configure("movie", "หนังกลางแปลง", 0);
        return root.transform;
    }

    private static Renderer Pixel(Transform parent, Vector3 at, float s) =>
        PrimitiveKit.Box("Pixel", parent, at, Vector3.one * 0.14f * Mathf.Max(0.7f, s), Bulb, collider: false).GetComponent<Renderer>();

    // a stack of cabinets: two woofers each, a blue LED bar, a horn box on top
    private static void SpeakerStack(Transform parent, Vector3 at, float s, int cabinets)
    {
        var t = PrimitiveKit.Group("SpeakerStack", parent, at).transform;
        float cw = 1.25f * s + 0.2f, ch = 0.95f * s + 0.1f, cd = 0.9f * s;
        var leds = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < cabinets; i++)
        {
            float y = ch * 0.5f + i * ch;
            PrimitiveKit.Box("Cabinet", t, new Vector3(0f, y, 0f), new Vector3(cw, ch - 0.04f, cd), Black);
            foreach (float x in new[] { -cw * 0.24f, cw * 0.24f })
            {
                PrimitiveKit.Cylinder("Woofer", t, new Vector3(x, y + 0.05f, -cd * 0.5f - 0.01f), new Vector3(cw * 0.4f, 0.02f, cw * 0.4f), Grille, new Vector3(90f, 0f, 0f), collider: false);
                PrimitiveKit.Cylinder("Cap", t, new Vector3(x, y + 0.05f, -cd * 0.5f - 0.03f), new Vector3(cw * 0.12f, 0.02f, cw * 0.12f), Black, new Vector3(90f, 0f, 0f), collider: false);
            }
            leds.Add(PrimitiveKit.Box("LedBar", t, new Vector3(0f, y - ch * 0.38f, -cd * 0.5f - 0.02f), new Vector3(cw * 0.8f, 0.05f, 0.03f), Bulb, collider: false).GetComponent<Renderer>());
        }
        float topY = cabinets * ch;
        PrimitiveKit.Box("Horn", t, new Vector3(0f, topY + 0.3f * s, 0f), new Vector3(cw * 0.9f, 0.6f * s, cd * 0.8f), Black, collider: false);
        PrimitiveKit.Box("HornMouth", t, new Vector3(0f, topY + 0.3f * s, -cd * 0.4f - 0.01f), new Vector3(cw * 0.7f, 0.35f * s, 0.02f), Grille, collider: false);
        t.gameObject.AddComponent<LedChase>().Configure(leds.ToArray(), LedChase.Mode.Pulse, new Color(0.15f, 0.4f, 1f), Color.white, 1.05f);
    }

    // a pole of LED segments: the light fills it from the bottom, flashes and drops, in turn
    private static void PixelTube(Transform parent, Vector3 at, float height, int index)
    {
        var t = PrimitiveKit.Group("PixelTube", parent, at).transform;
        PrimitiveKit.Box("Base", t, new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.2f, 0.5f), Black);
        PrimitiveKit.Box("Pole", t, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.08f, height, 0.08f), Black, collider: false);
        int n = Mathf.RoundToInt(height / 0.3f);
        var segs = new Renderer[n];
        for (int i = 0; i < n; i++)
            segs[i] = PrimitiveKit.Box("Seg", t, new Vector3(0f, 0.35f + i * 0.3f, 0f), new Vector3(0.14f, 0.24f, 0.14f), Bulb, collider: false).GetComponent<Renderer>();
        Color[] a = { new Color(0.1f, 1f, 0.3f), new Color(1f, 0.2f, 0.7f), new Color(0.2f, 0.6f, 1f) };
        Color[] b = { new Color(1f, 0.2f, 0.7f), new Color(1f, 0.9f, 0.2f), new Color(0.1f, 1f, 0.6f) };
        t.gameObject.AddComponent<LedChase>().Configure(segs, index % 2 == 0 ? LedChase.Mode.Climb : LedChase.Mode.Rainbow, a[index % 3], b[index % 3], 0.9f + index * 0.07f);
    }

    // the projector booth: a tin roof on four posts over a film projector of the old travelling kind —
    // a gold pedestal, a black lamp house with its chimney, the steel film head with the lens, two big
    // reels on arms (top one full, bottom one taking up) — and the lamp's beam reaching all the way to
    // the screen (FilmProjector turns the reels and flickers the beam). A neon name box on the back.
    private static void Booth(Transform parent, Vector3 at, float w, float bottom, float top, bool big)
    {
        var t = PrimitiveKit.Group("ProjectorBooth", parent, at).transform;
        foreach (float x in new[] { -0.95f, 0.95f })
            foreach (float z in new[] { -0.75f, 0.75f })
                PrimitiveKit.Box("Post", t, new Vector3(x, 1.25f, z), new Vector3(0.05f, 2.5f, 0.05f), Black, collider: false);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 2.55f, 0f), new Vector3(2.2f, 0.06f, 1.8f), PrimitiveKit.Mat("PK_RoofTin", new Color(0.55f, 0.5f, 0.45f), 0.5f), new Vector3(0f, 0f, 4f), collider: false);
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.03f, 0f), new Vector3(2f, 0.06f, 1.6f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);

        var gold = PrimitiveKit.Mat("PK_ProjectorGold", new Color(0.78f, 0.6f, 0.3f), 0.6f, 0.7f);
        var steel = PrimitiveKit.Mat("PK_ProjectorSteel", new Color(0.72f, 0.73f, 0.75f), 0.7f, 0.8f);
        var enamel = PrimitiveKit.Mat("PK_ProjectorEnamel", new Color(0.06f, 0.06f, 0.07f), 0.85f, 0.3f);
        var red = PrimitiveKit.Mat("PK_ProjectorKnob", new Color(0.8f, 0.08f, 0.06f), 0.7f);
        var film = PrimitiveKit.Mat("PK_FilmCoil", new Color(0.18f, 0.12f, 0.08f), 0.6f);
        var warm = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);

        // ── the pedestal ──
        PrimitiveKit.Box("Base", t, new Vector3(0f, 0.1f, 0.1f), new Vector3(0.9f, 0.08f, 0.8f), gold);
        foreach (float x in new[] { -0.38f, 0.38f })
            foreach (float z in new[] { -0.25f, 0.45f })
                PrimitiveKit.Cylinder("Foot", t, new Vector3(x, 0.15f, z), new Vector3(0.07f, 0.03f, 0.07f), steel, collider: false);
        PrimitiveKit.Box("Plinth", t, new Vector3(0f, 0.32f, 0.1f), new Vector3(0.6f, 0.36f, 0.55f), gold, collider: false);
        PrimitiveKit.Cylinder("Column", t, new Vector3(0f, 0.8f, 0.1f), new Vector3(0.24f, 0.32f, 0.24f), steel, collider: false);
        PrimitiveKit.Box("Table", t, new Vector3(0f, 1.15f, 0.1f), new Vector3(0.5f, 0.06f, 0.7f), gold, collider: false);

        // ── the head, tipped up at the screen ──
        var headAt = new Vector3(0f, 1.18f, 0.1f);
        var screenMid = new Vector3(0f, (bottom + top) * 0.5f, 0f) - at;
        var head = PrimitiveKit.Group("Projector", t, headAt).transform;
        head.localRotation = Quaternion.LookRotation((screenMid - headAt).normalized);
        // lamp house: a black box with a rounded top, its chimney, red knobs and a glowing peep window
        PrimitiveKit.Box("LampHouse", head, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.42f, 0.4f, 0.6f), enamel, collider: false);
        PrimitiveKit.Cylinder("LampTop", head, new Vector3(0f, 0.4f, -0.2f), new Vector3(0.42f, 0.3f, 0.42f), enamel, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Vents", head, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.44f, 0.12f, 0.4f), steel, collider: false);
        PrimitiveKit.Cylinder("Chimney", head, new Vector3(0f, 0.68f, -0.3f), new Vector3(0.13f, 0.1f, 0.13f), enamel, collider: false);
        PrimitiveKit.Cylinder("ChimneyCap", head, new Vector3(0f, 0.79f, -0.3f), new Vector3(0.17f, 0.015f, 0.17f), steel, collider: false);
        PrimitiveKit.Box("PeepWindow", head, new Vector3(-0.215f, 0.3f, -0.15f), new Vector3(0.01f, 0.07f, 0.12f), warm, collider: false);
        foreach (var k in new[] { new Vector3(-0.23f, 0.08f, -0.42f), new Vector3(-0.23f, 0.34f, -0.45f), new Vector3(0.23f, 0.08f, -0.42f) })
            PrimitiveKit.Sphere("Knob", head, k, Vector3.one * 0.05f, red);
        // film head: steel box, the lens barrel and its glass
        PrimitiveKit.Box("FilmHead", head, new Vector3(0f, 0.2f, 0.25f), new Vector3(0.3f, 0.38f, 0.3f), steel, collider: false);
        PrimitiveKit.Box("Gate", head, new Vector3(0.155f, 0.22f, 0.25f), new Vector3(0.01f, 0.14f, 0.1f), warm, collider: false);
        PrimitiveKit.Cylinder("Lens", head, new Vector3(0f, 0.22f, 0.5f), new Vector3(0.12f, 0.1f, 0.12f), enamel, new Vector3(90f, 0f, 0f), collider: false);
        var lensLocal = new Vector3(0f, 0.22f, 0.61f);
        PrimitiveKit.Sphere("LensGlass", head, lensLocal, new Vector3(0.1f, 0.1f, 0.04f), warm);

        // reels on arms, beside the film head (they spin on the head's x axis)
        var reels = new Transform[2];
        reels[0] = Reel(head, new Vector3(0.32f, 0.85f, 0.2f), 0.34f, 0.3f, steel, film);
        reels[1] = Reel(head, new Vector3(0.32f, -0.42f, 0.3f), 0.34f, 0.17f, steel, film);
        PrimitiveKit.Box("UpperArm", head, new Vector3(0.28f, 0.6f, 0.22f), new Vector3(0.03f, 0.55f, 0.04f), steel, collider: false);
        PrimitiveKit.Box("LowerArm", head, new Vector3(0.28f, -0.12f, 0.28f), new Vector3(0.03f, 0.6f, 0.04f), steel, collider: false);
        Strip(head, new Vector3(0.32f, 0.85f, 0.2f + 0.3f), new Vector3(0.16f, 0.39f, 0.36f), film);
        Strip(head, new Vector3(0.16f, 0.02f, 0.36f), new Vector3(0.32f, -0.42f, 0.3f + 0.17f), film);

        var lampGo = new GameObject("LampGlow");
        lampGo.transform.SetParent(head, false);
        lampGo.transform.localPosition = new Vector3(0f, 0.3f, 0.75f);
        var lamp = lampGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.82f, 0.55f);
        lamp.range = 3.5f;
        lamp.intensity = 2f;
        lamp.shadows = LightShadows.None;

        // ── the beam: from the lens to the edges of the picture ──
        var lensInT = headAt + head.localRotation * lensLocal;
        float hw = w * 0.5f;
        var far = new[]
        {
            new Vector3(-hw, bottom, 0f) - at, new Vector3(hw, bottom, 0f) - at,
            new Vector3(hw, top, 0f) - at, new Vector3(-hw, top, 0f) - at,
        };
        string key = big ? "Cinema/ProjectorBeam_big" : "Cinema/ProjectorBeam_small";
        var outer = MeshKit.Place("Beam", t, MeshKit.SaveCombined(key, BeamMesh(lensInT, far, 0.05f, 0.3f, 1f)),
            PrimitiveKit.Beam("ProjectorBeam", new Color(0.05f, 0.045f, 0.035f, 1f)), lensInT);
        var core = MeshKit.Place("BeamCore", t, MeshKit.SaveCombined(key + "_core", BeamMesh(lensInT, far, 0.03f, 0.3f, 0.55f)),
            PrimitiveKit.Beam("ProjectorBeamCore", new Color(0.06f, 0.055f, 0.045f, 1f)), lensInT);
        foreach (var g in new[] { outer, core })
        {
            var r = g.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        t.gameObject.AddComponent<FilmProjector>().Configure(reels, new[] { 55f, 95f },
            new Renderer[] { outer.GetComponent<Renderer>(), core.GetComponent<Renderer>() },
            new[] { new Color(0.05f, 0.045f, 0.035f), new Color(0.06f, 0.055f, 0.045f) }, lamp);

        // the neon name box on the back
        var box = PrimitiveKit.Group("NameBox", t, new Vector3(0f, 2.15f, -0.88f)).transform;
        PrimitiveKit.Box("Panel", box, new Vector3(0f, 0f, 0.02f), new Vector3(1.5f, 0.45f, 0.04f), Black, collider: false);
        var tube = PrimitiveKit.Box("Tube", box, new Vector3(0f, -0.2f, 0f), new Vector3(1.45f, 0.04f, 0.04f), Bulb, collider: false).GetComponent<Renderer>();
        var tube2 = PrimitiveKit.Box("Tube", box, new Vector3(0f, 0.2f, 0f), new Vector3(1.45f, 0.04f, 0.04f), Bulb, collider: false).GetComponent<Renderer>();
        box.gameObject.AddComponent<LedChase>().Configure(new[] { tube, tube2 }, LedChase.Mode.Swap, new Color(1f, 0.2f, 0.6f), new Color(0.3f, 1f, 0.4f), 0.5f);
        PrimitiveKit.Label(Troupe, box, new Vector3(0f, 0f, -0.03f), 0.026f, new Color(1f, 0.4f, 0.75f));
    }

    // a film reel standing on edge: hub, spokes, a rim of segments and the coil of film wound on it.
    // The returned pivot's own y is the spin axis (FilmProjector turns it).
    private static Transform Reel(Transform head, Vector3 at, float radius, float coil, Material steel, Material film)
    {
        var pivot = PrimitiveKit.Group("Reel", head, at).transform;
        pivot.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PrimitiveKit.Cylinder("Hub", pivot, Vector3.zero, new Vector3(0.07f, 0.03f, 0.07f), steel, collider: false);
        PrimitiveKit.Cylinder("Coil", pivot, Vector3.zero, new Vector3(coil * 2f, 0.018f, coil * 2f), film, collider: false);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            PrimitiveKit.Box("Spoke", pivot, dir * radius * 0.5f, new Vector3(0.018f, 0.025f, radius), steel, new Vector3(0f, a, 0f), collider: false);
        }
        const int segments = 20;
        float seg = 2f * Mathf.PI * radius / segments * 1.05f;
        for (int i = 0; i < segments; i++)
        {
            float a = i * 360f / segments;
            var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            PrimitiveKit.Box("Rim", pivot, dir * radius, new Vector3(seg, 0.03f, 0.018f), steel, new Vector3(0f, a, 0f), collider: false);
        }
        return pivot;
    }

    // a length of film between two points (in the head's space)
    private static void Strip(Transform head, Vector3 a, Vector3 b, Material film)
    {
        var mid = (a + b) * 0.5f;
        var d = b - a;
        PrimitiveKit.Box("FilmStrip", head, mid, new Vector3(0.006f, 0.035f, d.magnitude), film,
            Quaternion.LookRotation(d.normalized, Vector3.right).eulerAngles, collider: false);
    }

    // The beam's four sides, from a small square round the lens out to the picture's corners (stopped a
    // little short of the screen). Vertices are relative to the lens. `spread` < 1 makes a narrower core.
    private static Mesh BeamMesh(Vector3 lens, Vector3[] far, float lensHalf, float shortBy, float spread)
    {
        var centre = (far[0] + far[1] + far[2] + far[3]) * 0.25f;
        var axis = (centre - lens).normalized;
        var up = Vector3.Cross(axis, Vector3.right).normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;
        var right = Vector3.Cross(up, axis).normalized;
        var near = new[]
        {
            (-right - up) * lensHalf, (right - up) * lensHalf,
            (right + up) * lensHalf, (-right + up) * lensHalf,
        };
        var verts = new Vector3[16];
        var tris = new int[24];
        for (int side = 0; side < 4; side++)
        {
            int n = (side + 1) % 4;
            Vector3 F(int i)
            {
                var p = centre + (far[i] - centre) * spread;
                return p - axis * shortBy - lens;
            }
            int v = side * 4;
            verts[v] = near[side];
            verts[v + 1] = near[n];
            verts[v + 2] = F(n);
            verts[v + 3] = F(side);
            int k = side * 6;
            tris[k] = v; tris[k + 1] = v + 1; tris[k + 2] = v + 2;
            tris[k + 3] = v; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
        }
        var mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
