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
    // the projector's beam (additive): bright enough to read as a cone of light through the night air
    private static readonly Color BeamOuter = new Color(0.16f, 0.145f, 0.11f, 1f);
    private static readonly Color BeamCore = new Color(0.22f, 0.2f, 0.15f, 1f);

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
        Searchlights(m, big ? new[] { new Vector3(-w * 0.5f - 2.5f, 0f, -12f), new Vector3(w * 0.5f + 2.5f, 0f, -12f) }
                            : new[] { new Vector3(w * 0.5f + 1.6f, 0f, -7.4f) }, s);

        // the audience sitting cross-legged on the mats, facing the screen, from the late afternoon on
        var seats = new System.Collections.Generic.List<Vector3>();
        var faces = new System.Collections.Generic.List<float>();
        var poses = new System.Collections.Generic.List<SpectatorPose.Kind>();
        for (int r = 0; r < rows; r++)
            for (int i = 0; i < perRow; i++)
            {
                float cx = (i - (perRow - 1) * 0.5f) * (big ? 3f : 2f), cz = (big ? -6f : -4.6f) - r * 2.6f;
                int each = big ? 3 : 2;
                for (int k = 0; k < each; k++)
                {
                    float x = cx + (k - (each - 1) * 0.5f) * (big ? 0.75f : 0.7f), z = cz + ((k + i + r) % 2 == 0 ? 0.35f : -0.4f);
                    seats.Add(new Vector3(x, 0f, z));
                    faces.Add(((k * 7 + i * 3 + r) % 5 - 2) * 4f); // a little this way and that
                    poses.Add(SpectatorPose.Kind.SitGround);
                }
            }
        Npcs.Watchers(m, "Audience_คนดูหนัง", Vector3.zero, 0f, seats.ToArray(), faces.ToArray(), poses.ToArray(), "", 16, 2);

        var zone = new GameObject("PlayZone");
        zone.transform.SetParent(m, false);
        zone.transform.localPosition = new Vector3(0f, 1f, big ? -7f : -4.8f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = big ? new Vector3(12f, 2f, 5f) : new Vector3(6.5f, 2f, 2.6f);
        zone.AddComponent<FestivalBooth>().Configure("movie", "หนังกลางแปลง", 0);
        return root.transform;
    }

    // the troupe's searchlights (ไฟส่องฟ้า) beside the booth: after dark their beams sweep the sky so the
    // whole district can see there's a film on tonight (SkyBeams; they go up only while the show is on)
    private static void Searchlights(Transform parent, Vector3[] at, float s)
    {
        var group = PrimitiveKit.Group("SkyLights_ไฟส่องฟ้า", parent, Vector3.zero).transform;
        var beam = MeshKit.Lathe("CinemaSkyBeam", new[] { new Vector2(0.3f, 0f), new Vector2(2.2f, 40f) }, 16, capTop: false, capBottom: false);
        var beamMat = PrimitiveKit.Beam("CinemaSkyBeam", new Color(0.8f, 0.78f, 0.6f, 1f));
        var lens = PrimitiveKit.Glow("StageLens", Color.white, 6f, 0.5f);
        foreach (var p in at)
        {
            var stand = PrimitiveKit.Group("SearchlightStand", group, p).transform;
            PrimitiveKit.Box("Trailer", stand, new Vector3(0f, 0.35f, 0f), new Vector3(1f, 0.5f, 1.3f), Black);
            foreach (float x in new[] { -0.55f, 0.55f })
                PrimitiveKit.Cylinder("Wheel", stand, new Vector3(x, 0.25f, 0f), new Vector3(0.5f, 0.06f, 0.5f), Black, new Vector3(0f, 0f, 90f), collider: false);
            var head = PrimitiveKit.Group("Searchlight", group, p + Vector3.up * 0.9f).transform;
            PrimitiveKit.Cylinder("Lamp", head, new Vector3(0f, 0.3f, 0f), new Vector3(0.75f, 0.3f, 0.75f), Black, collider: false);
            PrimitiveKit.Cylinder("Lens", head, new Vector3(0f, 0.61f, 0f), new Vector3(0.66f, 0.02f, 0.66f), lens, collider: false);
            var b = MeshKit.Place("Beam", head, beam, beamMat, new Vector3(0f, 0.63f, 0f));
            var r = b.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        group.gameObject.AddComponent<SkyBeams>();
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

    // the projector booth: a steel truss deck (no roof, so the machine stands up against the night sky)
    // with a 35 mm film projector of the old travelling kind on it — a gold pedestal on bolted feet, the
    // long black arc-lamp house with chrome bands, a fin along the top, little lit windows and its
    // chimney breathing smoke; the grey-green film head in front with the brass lens, and two big wire
    // reels, the full one up top and the take-up one below. The lamp's beam reaches all the way to the
    // screen (FilmProjector turns the reels, flickers the beam and puffs the smoke). Sound gear, flight
    // cases of film and the projectionist share the deck; the troupe's neon name box stands at the back.
    private static void Booth(Transform parent, Vector3 at, float w, float bottom, float top, bool big)
    {
        var t = PrimitiveKit.Group("ProjectorBooth", parent, at).transform;
        var gold = PrimitiveKit.Mat("PK_ProjectorGold", new Color(0.78f, 0.6f, 0.3f), 0.6f, 0.7f);
        var steel = PrimitiveKit.Mat("PK_ProjectorSteel", new Color(0.72f, 0.73f, 0.75f), 0.7f, 0.8f);
        var chrome = PrimitiveKit.Mat("PK_ProjectorChrome", new Color(0.88f, 0.89f, 0.92f), 0.93f, 0.95f);
        var enamel = PrimitiveKit.Mat("PK_ProjectorEnamel", new Color(0.06f, 0.06f, 0.07f), 0.85f, 0.3f);
        var paint = PrimitiveKit.Mat("PK_ProjectorHead", new Color(0.5f, 0.56f, 0.5f), 0.6f, 0.45f);
        var brass = PrimitiveKit.Mat("PK_ProjectorBrass", new Color(0.82f, 0.64f, 0.3f), 0.8f, 0.9f);
        var red = PrimitiveKit.Mat("PK_ProjectorKnob", new Color(0.8f, 0.08f, 0.06f), 0.7f);
        var film = PrimitiveKit.Mat("PK_FilmCoil", new Color(0.18f, 0.12f, 0.08f), 0.6f);
        var warm = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);

        // ── the truss deck ──
        const float deckW = 2.8f, deckD = 2.3f, P = 0.45f;
        var truss = PrimitiveKit.Mat("PK_TrussSteel", new Color(0.78f, 0.79f, 0.82f), 0.7f, 0.85f);
        PrimitiveKit.Box("Deck", t, new Vector3(0f, P - 0.03f, 0f), new Vector3(deckW, 0.06f, deckD), PrimitiveKit.Mat("PK_DeckPlate", new Color(0.3f, 0.31f, 0.33f), 0.45f, 0.6f));
        foreach (var (a, b) in new[]
        {
            (new Vector3(-deckW, 0f, -deckD) * 0.5f, new Vector3(deckW, 0f, -deckD) * 0.5f),
            (new Vector3(-deckW, 0f, deckD) * 0.5f, new Vector3(deckW, 0f, deckD) * 0.5f),
            (new Vector3(-deckW, 0f, -deckD) * 0.5f, new Vector3(-deckW, 0f, deckD) * 0.5f),
            (new Vector3(deckW, 0f, -deckD) * 0.5f, new Vector3(deckW, 0f, deckD) * 0.5f),
        })
        {
            float lo = 0.05f, hi = P - 0.09f;
            Rod("Chord", t, a + Vector3.up * hi, b + Vector3.up * hi, 0.045f, truss);
            Rod("Chord", t, a + Vector3.up * lo, b + Vector3.up * lo, 0.045f, truss);
            int bays = Mathf.Max(2, Mathf.RoundToInt((b - a).magnitude / 0.6f));
            for (int i = 0; i <= bays; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)bays);
                Rod("Post", t, p, p + Vector3.up * hi, 0.04f, truss);
                if (i == bays) continue;
                var q = Vector3.Lerp(a, b, (i + 1) / (float)bays);
                Rod("Brace", t, p + Vector3.up * (i % 2 == 0 ? lo : hi), q + Vector3.up * (i % 2 == 0 ? hi : lo), 0.025f, truss);
            }
        }
        PrimitiveKit.Box("Step", t, new Vector3(0.6f, P * 0.25f, -deckD * 0.5f - 0.22f), new Vector3(0.7f, P * 0.5f, 0.4f), truss);

        // ── the pedestal ──
        var foot = MeshKit.Lathe("Cinema/ProjectorFoot", new[]
        {
            new Vector2(0.46f, 0f), new Vector2(0.46f, 0.05f), new Vector2(0.34f, 0.1f), new Vector2(0.17f, 0.58f), new Vector2(0.14f, 0.62f),
        }, 4);
        MeshKit.Place("Pedestal", t, foot, gold, new Vector3(0f, P, 0.05f), new Vector3(0f, 45f, 0f));
        foreach (float x in new[] { -0.3f, 0.3f })
            foreach (float z in new[] { -0.3f, 0.3f })
            {
                PrimitiveKit.Cylinder("FootBolt", t, new Vector3(x, P + 0.065f, 0.05f + z), new Vector3(0.08f, 0.02f, 0.08f), chrome, collider: false);
                PrimitiveKit.Sphere("BoltCap", t, new Vector3(x, P + 0.09f, 0.05f + z), Vector3.one * 0.045f, chrome);
            }
        PrimitiveKit.Cylinder("Column", t, new Vector3(0f, P + 0.79f, 0.05f), new Vector3(0.17f, 0.17f, 0.17f), chrome, collider: false);
        PrimitiveKit.Cylinder("Collar", t, new Vector3(0f, P + 0.97f, 0.05f), new Vector3(0.24f, 0.03f, 0.24f), gold, collider: false);
        PrimitiveKit.Cylinder("TiltWheel", t, new Vector3(0.13f, P + 0.82f, 0.05f), new Vector3(0.16f, 0.012f, 0.16f), chrome, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("Saddle", t, new Vector3(0f, P + 1.03f, 0.05f), new Vector3(0.34f, 0.07f, 0.6f), gold, collider: false);
        foreach (float x in new[] { -0.15f, 0.15f })
            PrimitiveKit.Box("Yoke", t, new Vector3(x, P + 1.1f, 0.05f), new Vector3(0.03f, 0.14f, 0.22f), gold, collider: false);
        // a box of a rectifier beside it, as every travelling show has, with its dial lit
        PrimitiveKit.Box("Rectifier", t, new Vector3(-0.55f, P + 0.3f, 0.65f), new Vector3(0.45f, 0.6f, 0.4f), paint);
        PrimitiveKit.Box("RectifierDial", t, new Vector3(-0.55f, P + 0.45f, 0.44f), new Vector3(0.14f, 0.08f, 0.01f), warm, collider: false);
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Box("RectifierVent", t, new Vector3(-0.55f, P + 0.12f + i * 0.06f, 0.442f), new Vector3(0.3f, 0.015f, 0.01f), enamel, collider: false);

        // ── the head, tipped up at the screen ──
        var headAt = new Vector3(0f, P + 1.12f, 0.05f);
        var screenMid = new Vector3(0f, (bottom + top) * 0.5f, 0f) - at;
        var head = PrimitiveKit.Group("Projector", t, headAt).transform;
        head.localRotation = Quaternion.LookRotation((screenMid - headAt).normalized);

        // the arc-lamp house: a long rounded drum lying along the beam
        var drumRot = new Vector3(90f, 0f, 0f);
        var drumScale = new Vector3(0.82f, 1f, 1.05f);
        var drum = MeshKit.Lathe("Cinema/LampDrum", new[]
        {
            new Vector2(0f, 0f), new Vector2(0.15f, 0.012f), new Vector2(0.22f, 0.04f), new Vector2(0.26f, 0.09f), new Vector2(0.27f, 0.14f),
            new Vector2(0.27f, 0.72f), new Vector2(0.26f, 0.77f), new Vector2(0.23f, 0.81f), new Vector2(0.18f, 0.85f),
        }, 28, capTop: true, capBottom: false);
        MeshKit.Place("LampHouse", head, drum, enamel, new Vector3(0f, 0.24f, -0.92f), drumRot, drumScale);
        var band = MeshKit.Lathe("Cinema/LampBand", new[]
        {
            new Vector2(0.273f, 0f), new Vector2(0.279f, 0.01f), new Vector2(0.279f, 0.032f), new Vector2(0.273f, 0.042f),
        }, 28, capTop: false, capBottom: false);
        foreach (float z in new[] { -0.77f, -0.5f, -0.24f })
            MeshKit.Place("ChromeBand", head, band, chrome, new Vector3(0f, 0.24f, z), drumRot, drumScale);
        PrimitiveKit.Box("Fin", head, new Vector3(0f, 0.535f, -0.52f), new Vector3(0.03f, 0.04f, 0.6f), chrome, collider: false);
        PrimitiveKit.Box("Crest", head, new Vector3(0f, 0.5f, -0.17f), new Vector3(0.03f, 0.13f, 0.1f), chrome, new Vector3(-35f, 0f, 0f), collider: false);
        foreach (float side in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("WindowFrame", head, new Vector3(side * 0.214f, 0.3f, -0.4f), new Vector3(0.02f, 0.09f, 0.17f), chrome, collider: false);
            PrimitiveKit.Box("Window", head, new Vector3(side * 0.222f, 0.3f, -0.4f), new Vector3(0.012f, 0.06f, 0.13f), warm, collider: false);
            for (int i = 0; i < 4; i++)
                PrimitiveKit.Box("Vent", head, new Vector3(side * 0.205f, 0.11f + i * 0.026f, -0.66f), new Vector3(0.014f, 0.01f, 0.24f), chrome, collider: false);
            PrimitiveKit.Sphere("Knob", head, new Vector3(side * 0.205f, 0.38f, -0.8f), Vector3.one * 0.045f, red);
        }
        PrimitiveKit.Sphere("Knob", head, new Vector3(-0.2f, 0.12f, -0.22f), Vector3.one * 0.045f, red);
        PrimitiveKit.Sphere("Reflector", head, new Vector3(0f, 0.24f, -0.9f), new Vector3(0.22f, 0.22f, 0.08f), chrome);
        PrimitiveKit.Cylinder("ArcKnob", head, new Vector3(0f, 0.24f, -0.95f), new Vector3(0.05f, 0.03f, 0.05f), red, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("Chimney", head, new Vector3(0f, 0.6f, -0.62f), new Vector3(0.11f, 0.08f, 0.11f), enamel, collider: false);
        PrimitiveKit.Cylinder("ChimneyCap", head, new Vector3(0f, 0.69f, -0.62f), new Vector3(0.15f, 0.012f, 0.15f), chrome, collider: false);
        PrimitiveKit.Cylinder("Collar", head, new Vector3(0f, 0.24f, -0.06f), new Vector3(0.3f, 0.02f, 0.3f), chrome, new Vector3(90f, 0f, 0f), collider: false);

        // the film head: a grey-green box, its door with the lit gate window, the sound head under it
        PrimitiveKit.Box("FilmHead", head, new Vector3(0f, 0.26f, 0.1f), new Vector3(0.3f, 0.42f, 0.3f), paint, collider: false);
        PrimitiveKit.Box("GateFrame", head, new Vector3(0.151f, 0.3f, 0.1f), new Vector3(0.008f, 0.13f, 0.11f), chrome, collider: false);
        PrimitiveKit.Box("Gate", head, new Vector3(0.154f, 0.3f, 0.1f), new Vector3(0.008f, 0.1f, 0.08f), warm, collider: false);
        PrimitiveKit.Box("DoorHandle", head, new Vector3(0.158f, 0.2f, 0.21f), new Vector3(0.015f, 0.08f, 0.015f), chrome, collider: false);
        PrimitiveKit.Box("Hinge", head, new Vector3(0.152f, 0.26f, -0.04f), new Vector3(0.01f, 0.36f, 0.015f), chrome, collider: false);
        PrimitiveKit.Box("NamePlate", head, new Vector3(-0.152f, 0.4f, 0.1f), new Vector3(0.008f, 0.05f, 0.14f), chrome, collider: false);
        PrimitiveKit.Cylinder("Sprocket", head, new Vector3(0f, 0.49f, 0.12f), new Vector3(0.12f, 0.08f, 0.12f), chrome, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Box("SoundHead", head, new Vector3(0f, -0.03f, 0.12f), new Vector3(0.26f, 0.16f, 0.26f), paint, collider: false);
        PrimitiveKit.Box("ExciterLamp", head, new Vector3(0.132f, -0.03f, 0.16f), new Vector3(0.008f, 0.04f, 0.05f), warm, collider: false);
        // the lens: a brass barrel with a chrome ring and the lit glass
        PrimitiveKit.Cylinder("Lens", head, new Vector3(0f, 0.3f, 0.34f), new Vector3(0.1f, 0.1f, 0.1f), brass, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("LensRing", head, new Vector3(0f, 0.3f, 0.44f), new Vector3(0.13f, 0.015f, 0.13f), chrome, new Vector3(90f, 0f, 0f), collider: false);
        var lensLocal = new Vector3(0f, 0.3f, 0.46f);
        PrimitiveKit.Sphere("LensGlass", head, new Vector3(0f, 0.3f, 0.455f), new Vector3(0.085f, 0.085f, 0.03f), warm);

        // the reels: the full one high above the head, the take-up one low and to the side of the column
        var reels = new Transform[2];
        reels[0] = Reel(head, new Vector3(0f, 0.98f, 0.1f), 0.4f, 0.3f, chrome, film);
        reels[1] = Reel(head, new Vector3(0.24f, -0.48f, 0.22f), 0.38f, 0.16f, chrome, film);
        Rod("UpperArm", head, new Vector3(-0.07f, 0.47f, 0.1f), new Vector3(-0.07f, 0.98f, 0.1f), 0.03f, chrome);
        Rod("UpperSpindle", head, new Vector3(-0.07f, 0.98f, 0.1f), new Vector3(0f, 0.98f, 0.1f), 0.025f, chrome);
        Rod("LowerArm", head, new Vector3(0.13f, -0.08f, 0.16f), new Vector3(0.17f, -0.48f, 0.22f), 0.03f, chrome);
        Rod("LowerSpindle", head, new Vector3(0.17f, -0.48f, 0.22f), new Vector3(0.24f, -0.48f, 0.22f), 0.025f, chrome);
        Strip(head, new Vector3(0f, 0.77f, 0.31f), new Vector3(0f, 0.48f, 0.2f), film);
        Strip(head, new Vector3(0.1f, -0.11f, 0.22f), new Vector3(0.24f, -0.37f, 0.33f), film);

        var lampGo = new GameObject("LampGlow");
        lampGo.transform.SetParent(head, false);
        lampGo.transform.localPosition = new Vector3(0f, 0.3f, 0.65f);
        var lamp = lampGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.82f, 0.55f);
        lamp.range = 3.5f;
        lamp.intensity = 2f;
        lamp.shadows = LightShadows.None;

        // smoke from the chimney (the arc's carbons burning), drifting up into the beam of the lamps
        var smokeAt = headAt + head.localRotation * new Vector3(0f, 0.7f, -0.62f);
        var smoke = PrimitiveKit.Group("Smoke", t, smokeAt).transform;
        var smokeMat = PrimitiveKit.Beam("ProjectorSmoke", new Color(0.07f, 0.07f, 0.08f, 1f));
        var puffs = new Renderer[5];
        for (int i = 0; i < puffs.Length; i++)
        {
            puffs[i] = PrimitiveKit.Sphere("Puff", smoke, Vector3.zero, Vector3.one * 0.1f, smokeMat).GetComponent<Renderer>();
            puffs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            puffs[i].receiveShadows = false;
        }

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
            PrimitiveKit.Beam("ProjectorBeam", BeamOuter), lensInT);
        var core = MeshKit.Place("BeamCore", t, MeshKit.SaveCombined(key + "_core", BeamMesh(lensInT, far, 0.03f, 0.3f, 0.55f)),
            PrimitiveKit.Beam("ProjectorBeamCore", BeamCore), lensInT);
        foreach (var g in new[] { outer, core })
        {
            var r = g.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        t.gameObject.AddComponent<FilmProjector>().Configure(reels, new[] { 55f, 95f },
            new Renderer[] { outer.GetComponent<Renderer>(), core.GetComponent<Renderer>() },
            new[] { BeamOuter, BeamCore }, lamp, puffs, new Color(0.07f, 0.07f, 0.08f));

        // ── the sound gear on the left: an amplifier rack and the mixer on its stand ──
        var rack = PrimitiveKit.Group("AmpRack_เครื่องขยายเสียง", t, new Vector3(-1.0f, P, 0.0f), -90f).transform;
        var vu = new System.Collections.Generic.List<Renderer>();
        PrimitiveKit.Box("Rack", rack, new Vector3(0f, 0.5f, 0f), new Vector3(0.62f, 1f, 0.42f), Black);
        for (int i = 0; i < 3; i++)
        {
            float y = 0.2f + i * 0.3f;
            PrimitiveKit.Box("Amp", rack, new Vector3(0f, y, -0.215f), new Vector3(0.56f, 0.24f, 0.02f), steel, collider: false);
            foreach (float x in new[] { -0.18f, -0.06f, 0.06f })
                PrimitiveKit.Cylinder("AmpKnob", rack, new Vector3(x, y - 0.04f, -0.23f), new Vector3(0.04f, 0.01f, 0.04f), Black, new Vector3(90f, 0f, 0f), collider: false);
            vu.Add(PrimitiveKit.Box("Meter", rack, new Vector3(0.17f, y + 0.04f, -0.23f), new Vector3(0.14f, 0.07f, 0.01f), warm, collider: false).GetComponent<Renderer>());
            vu.Add(PrimitiveKit.Box("PowerLed", rack, new Vector3(-0.24f, y + 0.07f, -0.23f), new Vector3(0.025f, 0.025f, 0.01f), Bulb, collider: false).GetComponent<Renderer>());
        }
        rack.gameObject.AddComponent<LedChase>().Configure(vu.ToArray(), LedChase.Mode.Pulse, new Color(1f, 0.55f, 0.15f), new Color(0.2f, 1f, 0.3f), 1.05f);
        var mixer = PrimitiveKit.Group("Mixer_มิกเซอร์", t, new Vector3(-0.95f, P, -0.75f), 0f).transform;
        PrimitiveKit.Box("Stand", mixer, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.76f, 0.38f), PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f)), collider: false);
        PrimitiveKit.Box("Desk", mixer, new Vector3(0f, 0.8f, 0f), new Vector3(0.48f, 0.07f, 0.34f), Black, new Vector3(-12f, 0f, 0f), collider: false);
        var faders = PrimitiveKit.Mat("PK_MixerFader", new Color(0.85f, 0.85f, 0.88f), 0.5f);
        for (int i = 0; i < 6; i++)
        {
            float x = -0.18f + i * 0.072f;
            PrimitiveKit.Box("Fader", mixer, new Vector3(x, 0.845f, 0.05f - (i % 3) * 0.02f), new Vector3(0.022f, 0.02f, 0.035f), faders, new Vector3(-12f, 0f, 0f), collider: false);
            for (int k = 0; k < 3; k++)
                PrimitiveKit.Cylinder("Knob", mixer, new Vector3(x, 0.84f + k * 0.012f, -0.13f + k * 0.05f), new Vector3(0.018f, 0.01f, 0.018f), i % 2 == 0 ? red : faders, collider: false);
        }
        // cables from the rack down off the deck to the speakers
        PrimitiveKit.Box("Cable", t, new Vector3(-1.2f, P + 0.01f, 0.7f), new Vector3(0.03f, 0.02f, 0.9f), Black, collider: false);

        // ── the film in silver flight cases, round tins on top ──
        var tins = PrimitiveKit.Mat("PK_FilmTin", new Color(0.62f, 0.6f, 0.55f), 0.6f, 0.6f);
        var caseMat = PrimitiveKit.Mat("PK_FlightCase", new Color(0.74f, 0.75f, 0.78f), 0.6f, 0.65f);
        var cases = PrimitiveKit.Group("FilmCans_กล่องฟิล์ม", t, new Vector3(0.75f, P, -0.85f)).transform;
        foreach (var (pos, size) in new[]
        {
            (new Vector3(-0.3f, 0.19f, 0f), new Vector3(0.5f, 0.38f, 0.4f)),
            (new Vector3(0.28f, 0.17f, 0f), new Vector3(0.48f, 0.34f, 0.4f)),
            (new Vector3(-0.3f, 0.52f, 0f), new Vector3(0.46f, 0.28f, 0.38f)),
        })
        {
            PrimitiveKit.Box("FlightCase", cases, pos, size, caseMat);
            foreach (float y in new[] { -0.5f, 0.5f })
                PrimitiveKit.Box("Edge", cases, pos + new Vector3(0f, y * size.y, -size.z * 0.5f), new Vector3(size.x + 0.01f, 0.025f, 0.02f), Black, collider: false);
            PrimitiveKit.Box("Latch", cases, pos + new Vector3(0f, size.y * 0.3f, -size.z * 0.5f - 0.01f), new Vector3(0.06f, 0.04f, 0.01f), chrome, collider: false);
        }
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Cylinder("Tin", cases, new Vector3(0.28f, 0.355f + i * 0.045f, 0f), new Vector3(0.34f, 0.02f, 0.34f), i == 1 ? red : tins, collider: false);
        PrimitiveKit.Label("ฟิล์ม", cases, new Vector3(-0.3f, 0.2f, -0.21f), 0.02f, new Color(1f, 0.9f, 0.5f));

        // the projectionist (คนฉาย) on the reel side, minding his machine
        Npcs.Watchers(t, "Projectionist_คนฉาย", Vector3.zero, 0f, new[] { new Vector3(0.78f, P, -0.15f) }, new[] { -90f },
            new[] { SpectatorPose.Kind.Projectionist }, "", 16, 2);

        // the neon name box on two posts at the back
        foreach (float x in new[] { -0.8f, 0.8f })
            Rod("SignPost", t, new Vector3(x, P, -deckD * 0.5f + 0.06f), new Vector3(x, P + 2.35f, -deckD * 0.5f + 0.06f), 0.05f, truss);
        var box = PrimitiveKit.Group("NameBox", t, new Vector3(0f, P + 2.1f, -deckD * 0.5f + 0.02f)).transform;
        PrimitiveKit.Box("Panel", box, new Vector3(0f, 0f, 0.02f), new Vector3(1.6f, 0.45f, 0.04f), Black, collider: false);
        var tube = PrimitiveKit.Box("Tube", box, new Vector3(0f, -0.2f, 0f), new Vector3(1.55f, 0.04f, 0.04f), Bulb, collider: false).GetComponent<Renderer>();
        var tube2 = PrimitiveKit.Box("Tube", box, new Vector3(0f, 0.2f, 0f), new Vector3(1.55f, 0.04f, 0.04f), Bulb, collider: false).GetComponent<Renderer>();
        box.gameObject.AddComponent<LedChase>().Configure(new[] { tube, tube2 }, LedChase.Mode.Swap, new Color(1f, 0.2f, 0.6f), new Color(0.3f, 1f, 0.4f), 0.5f);
        PrimitiveKit.Label(Troupe, box, new Vector3(0f, 0f, -0.03f), 0.026f, new Color(1f, 0.4f, 0.75f));
    }

    // a wire film reel standing on edge: two spoked flanges with a thin round rim each, a hub, and the
    // coil of film wound between them. The returned pivot's own y is the spin axis (FilmProjector turns it).
    private static Transform Reel(Transform head, Vector3 at, float radius, float coil, Material chrome, Material film)
    {
        var pivot = PrimitiveKit.Group("Reel", head, at).transform;
        pivot.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PrimitiveKit.Cylinder("Hub", pivot, Vector3.zero, new Vector3(0.07f, 0.05f, 0.07f), chrome, collider: false);
        PrimitiveKit.Cylinder("Core", pivot, Vector3.zero, new Vector3(0.11f, 0.03f, 0.11f), chrome, collider: false);
        PrimitiveKit.Cylinder("Coil", pivot, Vector3.zero, new Vector3(coil * 2f, 0.022f, coil * 2f), film, collider: false);
        var rim = Ring($"Cinema/ReelRim{Mathf.RoundToInt(radius * 100f)}", radius, 0.009f);
        var inner = Ring($"Cinema/ReelRim{Mathf.RoundToInt(radius * 45f)}", radius * 0.45f, 0.006f);
        foreach (float y in new[] { -0.036f, 0.036f })
        {
            MeshKit.Place("Rim", pivot, rim, chrome, new Vector3(0f, y, 0f));
            MeshKit.Place("InnerRim", pivot, inner, chrome, new Vector3(0f, y, 0f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + (y > 0f ? 18f : 0f);
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                PrimitiveKit.Box("Spoke", pivot, dir * radius * 0.5f + Vector3.up * y, new Vector3(0.012f, 0.008f, radius), chrome, new Vector3(0f, a, 0f), collider: false);
            }
        }
        return pivot;
    }

    // a thin round ring lying flat (in the xz plane): a reel's wire rim
    private static Mesh Ring(string key, float radius, float thickness)
    {
        const int n = 40;
        var path = new Vector3[n + 1];
        var r = new float[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            path[i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            r[i] = thickness;
        }
        return MeshKit.Sweep(key, path, r, 6);
    }

    // a straight bar from a to b (a truss tube, an arm)
    private static void Rod(string name, Transform parent, Vector3 a, Vector3 b, float thick, Material m)
    {
        var d = b - a;
        var dir = d.normalized;
        var up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        PrimitiveKit.Box(name, parent, (a + b) * 0.5f, new Vector3(thick, thick, d.magnitude), m, Quaternion.LookRotation(dir, up).eulerAngles, collider: false);
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
