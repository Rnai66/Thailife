using UnityEngine;

// กรุงเทพฯ landmarks (TambonBuilder.BuildBangkok puts the avenue, skytrain and shops around them):
//   north-west  วัดพระแก้ว: a walled compound with its cloister, the Emerald Buddha's hall, the
//               upper terrace and a row of prangs (see WatPhraKaew); สนามหลวง lawn in front
//   north       ศาลพระพรหม: the gold shrine on its island at the head of the side street
//   north       แม่น้ำเจ้าพระยา, wide, right behind the city; วัดอรุณ (the great porcelain prang with
//               its corner prangs, มณฑป and วิหาร) on the far bank, reached by the cross-river ferry
//   south       ท่าอากาศยานสุวรรณภูมิ: the glass terminal under its long roof, the check-in counter
//               (flights to other provinces, SpawnPoint "air"), a jet at the gate and the control tower
public static class Bangkok
{
    public static readonly Vector3 Wat = new Vector3(-50f, 0f, 26f);
    public static readonly Vector3 Shrine = new Vector3(0f, 0f, 14f);
    public static readonly Vector3 Terminal = new Vector3(-20f, 0f, -38f);
    public static readonly Vector3 Arun = new Vector3(0f, 0f, 92f); // across the river (ฝั่งธนฯ)
    public const float RiverZ = 62f, RiverHalf = 13f; // the river: 26 m wide, between the city and วัดอรุณ

    private static Material White => PrimitiveKit.Mat("TempleWhite", new Color(0.95f, 0.94f, 0.9f));
    private static Material Gold => PrimitiveKit.Mat("TempleGold", new Color(0.95f, 0.75f, 0.2f), 0.75f, 0.8f);

    public static void Build(Transform root)
    {
        Lawn(root);
        WatPhraKaew(root, Wat);
        BrahmaShrine(root, Shrine);
        Airport(root, Terminal);
        River(root);
        WatArun(root, Arun);
    }

    // ── สนามหลวง: the lawn in front of the temple ──────────────────────────────
    private static void Lawn(Transform root)
    {
        PrimitiveKit.Box("SanamLuang_สนามหลวง", root, new Vector3(-50f, 0.016f, 2.5f), new Vector3(60f, 0.012f, 9f),
            PrimitiveKit.Mat("LawnGreen", new Color(0.35f, 0.62f, 0.27f)), collider: false);
        foreach (var p in new[] { new Vector3(-77f, 0f, 1f), new Vector3(-62f, 0f, 1f), new Vector3(-30f, 0f, 1f), new Vector3(-23f, 0f, 4f) })
            Places.ShadeTree(root, p, 1.1f); // มะขาม round the lawn
    }

    // ── วัดพระแก้ว ─────────────────────────────────────────────────────────────
    // A walled compound 60 x 34 m. Crenellated white walls with corner forts (ป้อม) and a spired
    // gate in the south wall (yaksha guarding it inside), a cloister (ระเบียงคด) with murals all round
    // the inside. In the courtyard: the walk-in ordination hall of the Emerald Buddha (west), the
    // upper terrace with the golden chedi, the มณฑป and the ปราสาทพระเทพบิดร (north), two small
    // golden chedis, a bell tower and a row of eight porcelain prangs along the east cloister.
    private const float WatW = 60f, WatD = 34f, GateX = 12f;
    private static readonly float[] ArunTerrace = { 20f, 16f, 12.5f, 9.5f }, ArunRise = { 3f, 3.4f, 3.4f, 3f }; // พระปรางค์'s terraces

    private static void WatPhraKaew(Transform root, Vector3 at)
    {
        var t = PrimitiveKit.Group("WatPhraKaew_วัดพระแก้ว", root, at).transform;
        var white = White;
        var gold = Gold;
        var orange = PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f));
        var green = PrimitiveKit.Mat("TempleGreen", new Color(0.15f, 0.45f, 0.3f));
        var lacquer = PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f);
        const float hw = WatW / 2f, hd = WatD / 2f, wallH = 4.5f, gateHalf = 4f;

        PrimitiveKit.Box("Courtyard", t, new Vector3(0f, 0.02f, 0f), new Vector3(WatW - 1f, 0.04f, WatD - 1f), PrimitiveKit.Mat("MarbleFloor", new Color(0.88f, 0.87f, 0.84f), 0.5f), collider: false);

        // Walls: north, east and west whole; the south wall either side of the gate
        PrimitiveKit.Box("WallN", t, new Vector3(0f, wallH / 2f, hd), new Vector3(WatW, wallH, 1f), white);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box(s < 0f ? "WallW" : "WallE", t, new Vector3(s * hw, wallH / 2f, 0f), new Vector3(1f, wallH, WatD), white);
        float leftLen = GateX - gateHalf + hw, rightLen = hw - GateX - gateHalf;
        PrimitiveKit.Box("WallS", t, new Vector3(-hw + leftLen / 2f, wallH / 2f, -hd), new Vector3(leftLen, wallH, 1f), white);
        PrimitiveKit.Box("WallS", t, new Vector3(hw - rightLen / 2f, wallH / 2f, -hd), new Vector3(rightLen, wallH, 1f), white);
        PrimitiveKit.Box("WallBase", t, new Vector3(0f, 0.3f, -hd - 0.6f), new Vector3(WatW + 1.2f, 0.6f, 0.3f), PrimitiveKit.Mat("PlinthGrey", new Color(0.55f, 0.53f, 0.5f)), collider: false);
        for (float x = -hw + 0.75f; x <= hw - 0.7f; x += 1.5f)
        {
            PrimitiveKit.Box("Merlon", t, new Vector3(x, wallH + 0.4f, hd), new Vector3(0.75f, 0.8f, 1.1f), white, collider: false);
            if (Mathf.Abs(x - GateX) > gateHalf + 0.4f)
                PrimitiveKit.Box("Merlon", t, new Vector3(x, wallH + 0.4f, -hd), new Vector3(0.75f, 0.8f, 1.1f), white, collider: false);
        }
        for (float z = -hd + 0.75f; z <= hd - 0.7f; z += 1.5f)
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Merlon", t, new Vector3(s * hw, wallH + 0.4f, z), new Vector3(1.1f, 0.8f, 0.75f), white, collider: false);
        // Corner forts: round white towers with a crenellated rim
        foreach (float x in new[] { -hw, hw })
            foreach (float z in new[] { -hd, hd })
            {
                PrimitiveKit.Cylinder("Fort", t, new Vector3(x, 3f, z), new Vector3(3.2f, 3f, 3.2f), white);
                PrimitiveKit.Cylinder("FortRim", t, new Vector3(x, 6.3f, z), new Vector3(3.6f, 0.3f, 3.6f), white, collider: false);
                PrimitiveKit.Cylinder("FortRoof", t, new Vector3(x, 7.2f, z), new Vector3(1.6f, 0.6f, 1.6f), orange, collider: false);
                PrimitiveKit.Cylinder("FortTip", t, new Vector3(x, 8.4f, z), new Vector3(0.3f, 0.6f, 0.3f), gold, collider: false);
            }

        // Gate: two piers and a lintel under a tiered gold spire, the name board outside
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("GatePier", t, new Vector3(GateX + s * (gateHalf - 0.4f), 3.75f, -hd), new Vector3(1.4f, 7.5f, 1.6f), white);
        PrimitiveKit.Box("GateLintel", t, new Vector3(GateX, 7.9f, -hd), new Vector3(gateHalf * 2f + 0.6f, 1f, 1.8f), white, collider: false);
        PrimitiveKit.Box("GateLintelGold", t, new Vector3(GateX, 7.3f, -hd - 0.92f), new Vector3(gateHalf * 2f, 0.2f, 0.06f), gold, collider: false);
        Spire(t, new Vector3(GateX, 8.4f, -hd), 7f, gold, orange);
        PrimitiveKit.Box("GateBoard", t, new Vector3(GateX, 6.6f, -hd - 0.95f), new Vector3(7f, 0.9f, 0.08f), lacquer, collider: false);
        PrimitiveKit.Label("วัดพระศรีรัตนศาสดาราม · วัดพระแก้ว", t, new Vector3(GateX, 6.6f, -hd - 1f), 0.05f, new Color(1f, 0.85f, 0.4f));
        Yaksha(t, new Vector3(GateX - 5.6f, 0f, -hd + 4f), new Color(0.15f, 0.5f, 0.35f));
        Yaksha(t, new Vector3(GateX + 5.6f, 0f, -hd + 4f), new Color(0.7f, 0.15f, 0.12f));

        Cloister(t, hw, hd, wallH, white, lacquer, orange, gold);

        // The ordination hall of the Emerald Buddha, long side east-west, its door facing west
        var hall = PrimitiveKit.Group("UbosotPhraKaew_พระอุโบสถ", t, new Vector3(-13f, 0f, -6.5f), 90f).transform;
        hall.localScale = Vector3.one * 1.05f;
        Places.Ubosot(hall, Vector3.zero, white, gold, orange, green);

        // The upper terrace (ฐานไพที): golden chedi, มณฑป, ปราสาทพระเทพบิดร
        const float terraceY = 1.2f;
        var terrace = new Vector3(7f, 0f, 9f);
        PrimitiveKit.Box("UpperTerrace", t, terrace + new Vector3(0f, terraceY / 2f, 0f), new Vector3(32f, terraceY, 12f), white);
        PrimitiveKit.Box("TerraceRim", t, terrace + new Vector3(0f, terraceY + 0.3f, -6f), new Vector3(32f, 0.6f, 0.3f), gold, collider: false);
        for (float x = -12f; x <= 12f; x += 12f) // steps up from the courtyard
            for (int i = 0; i < 4; i++)
                PrimitiveKit.Box("TerraceStep", t, terrace + new Vector3(x, 0.15f + i * 0.3f, -6.3f - (3 - i) * 0.35f), new Vector3(3f, 0.3f, 0.35f), white);
        var chedi = PrimitiveKit.Group("GoldenChedi_พระศรีรัตนเจดีย์", t, terrace + new Vector3(-9f, terraceY, 0f)).transform;
        chedi.localScale = Vector3.one * 2.4f;
        Places.Chedi(chedi, Vector3.zero, gold, gold);
        Mondop(t, terrace + new Vector3(2f, terraceY, 0f), green, gold, orange);
        Prasat(t, terrace + new Vector3(11.5f, terraceY, 0f), white, gold, orange, green);

        // Two small golden chedis by the path in from the gate, a bell tower in the north-west
        foreach (float x in new[] { GateX - 5f, GateX + 5f })
        {
            var c = PrimitiveKit.Group("GoldChedi", t, new Vector3(x, 0f, -3.5f)).transform;
            c.localScale = Vector3.one * 0.9f;
            Places.Chedi(c, Vector3.zero, gold, gold);
        }
        BellTower(t, new Vector3(-24f, 0f, 9.5f), white, gold, orange, lacquer);

        // Eight porcelain prangs in a row along the east cloister
        for (int i = 0; i < 8; i++)
            Prang(t, new Vector3(hw - 6f, 0f, -12.25f + i * 3.5f), 9f, 2.1f, 8);

        foreach (var p in new[] { new Vector3(-24f, 0f, -12f), new Vector3(0f, 0f, -12f), new Vector3(GateX, 0f, 4.5f), new Vector3(-24f, 0f, 2f) })
            StreetFurniture.LampPost(t, p);

        // After dark: floodlights on the golden chedi, the มณฑป, the ปราสาท, the ordination hall, the
        // row of prangs and the gate spire; warm lines of light along the top of the walls
        Flood(t, "FloodLight_พระศรีรัตนเจดีย์", terrace + new Vector3(-9f, 0.3f, -9.5f), terrace + new Vector3(-9f, 9f, 0f), 220f, 45f, 50f);
        Flood(t, "FloodLight_พระมณฑป", terrace + new Vector3(2f, 0.3f, -9.5f), terrace + new Vector3(2f, 9f, 0f), 200f, 45f, 45f);
        Flood(t, "FloodLight_ปราสาทพระเทพบิดร", terrace + new Vector3(11.5f, 0.3f, -9.5f), terrace + new Vector3(11.5f, 7f, 0f), 200f, 45f, 50f);
        Flood(t, "FloodLight_พระอุโบสถ", new Vector3(-13f, 0.3f, -15f), new Vector3(-13f, 5f, -6.5f), 160f, 40f, 70f);
        Flood(t, "FloodLight_ปรางค์", new Vector3(hw - 14f, 0.3f, 0f), new Vector3(hw - 6f, 6f, 0f), 160f, 40f, 85f);
        Flood(t, "FloodLight_ซุ้มประตู", new Vector3(GateX, 0.3f, -hd - 6f), new Vector3(GateX, 11f, -hd), 160f, 40f, 40f);
        var glow = NightGlow(t);
        GlowLine(glow, new Vector3(-hw, wallH + 0.05f, hd + 0.56f), new Vector3(hw, wallH + 0.05f, hd + 0.56f));
        foreach (float x in new[] { -1f, 1f })
            GlowLine(glow, new Vector3(x * (hw + 0.56f), wallH + 0.05f, -hd), new Vector3(x * (hw + 0.56f), wallH + 0.05f, hd));
        GlowLine(glow, new Vector3(-hw, wallH + 0.05f, -hd - 0.56f), new Vector3(GateX - gateHalf - 0.8f, wallH + 0.05f, -hd - 0.56f));
        GlowLine(glow, new Vector3(GateX + gateHalf + 0.8f, wallH + 0.05f, -hd - 0.56f), new Vector3(hw, wallH + 0.05f, -hd - 0.56f));
        GlowLine(glow, new Vector3(GateX - gateHalf - 0.3f, 8.45f, -hd - 0.95f), new Vector3(GateX + gateHalf + 0.3f, 8.45f, -hd - 0.95f)); // the gate's lintel
    }

    // The cloister (ระเบียงคด) round the inside of the walls: red pillars, an orange roof on gold
    // eaves, and the Ramakien murals painted on the wall behind
    private static void Cloister(Transform t, float hw, float hd, float wallH, Material white, Material lacquer, Material orange, Material gold)
    {
        const float depth = 3f;
        var mural = new[]
        {
            PrimitiveKit.Mat("MuralGreen", new Color(0.2f, 0.4f, 0.3f), 0.3f),
            PrimitiveKit.Mat("MuralRed", new Color(0.55f, 0.15f, 0.1f), 0.3f),
            PrimitiveKit.Mat("MuralBlue", new Color(0.18f, 0.25f, 0.45f), 0.3f),
        };
        for (int side = 0; side < 4; side++)
        {
            // Along one wall: a = run from -half to +half, inward normal n
            bool ns = side % 2 == 0;
            float half = ns ? hw - 2.2f : hd - 2.2f, wall = ns ? hd : hw;
            float sign = side < 2 ? 1f : -1f; // N, E, S, W
            Vector3 P(float along, float inward, float y) =>
                ns ? new Vector3(along, y, sign * (wall - 0.5f - inward)) : new Vector3(sign * (wall - 0.5f - inward), y, along);
            Vector3 S(float along, float inward, float y) => ns ? new Vector3(along, y, inward) : new Vector3(inward, y, along);
            bool south = side == 2;
            for (int seg = 0; seg < (south ? 2 : 1); seg++)
            {
                float a0 = -half, a1 = half;
                if (south) { if (seg == 0) a1 = GateX - 4.5f; else a0 = GateX + 4.5f; }
                float len = a1 - a0, mid = (a0 + a1) / 2f;
                PrimitiveKit.Box("CloisterRoof", t, P(mid, depth / 2f, wallH - 0.4f), S(len, depth + 0.6f, 0.25f), orange, new Vector3(ns ? sign * 14f : 0f, 0f, ns ? 0f : -sign * 14f), collider: false);
                PrimitiveKit.Box("CloisterEave", t, P(mid, depth + 0.2f, wallH - 0.85f), S(len, 0.15f, 0.25f), gold, collider: false);
                PrimitiveKit.Box("CloisterFloor", t, P(mid, depth / 2f, 0.1f), S(len, depth, 0.2f), white, collider: false);
                for (float a = a0; a < a1 - 0.5f; a += 4f)
                    PrimitiveKit.Box("Mural", t, P(a + 2f, 0.02f, 1.9f), S(Mathf.Min(3.6f, a1 - a - 0.4f), 0.05f, 2.4f), mural[Mathf.Abs((int)(a / 4f)) % mural.Length], collider: false);
                for (float a = a0; a <= a1 + 0.01f; a += 3f)
                {
                    PrimitiveKit.Box("CloisterPillar", t, P(a, depth, 1.8f), new Vector3(0.35f, 3.6f, 0.35f), white);
                    PrimitiveKit.Box("PillarBand", t, P(a, depth, 3.4f), new Vector3(0.42f, 0.4f, 0.42f), lacquer, collider: false);
                }
            }
        }
    }

    // พระมณฑป: a square hall in green and gold under a tall tiered spire
    private static void Mondop(Transform t, Vector3 at, Material green, Material gold, Material orange)
    {
        var m = PrimitiveKit.Group("PhraMondop_พระมณฑป", t, at).transform;
        PrimitiveKit.Box("Base", m, new Vector3(0f, 0.5f, 0f), new Vector3(8f, 1f, 8f), White);
        Redented(m, "Hall", new Vector3(0f, 3.2f, 0f), 6.5f, 4.4f, green, simple: true);
        foreach (float x in new[] { -2.8f, 2.8f })
            foreach (float z in new[] { -2.8f, 2.8f })
                PrimitiveKit.Box("GoldColumn", m, new Vector3(x, 3.2f, z), new Vector3(0.45f, 4.4f, 0.45f), gold, collider: false);
        Redented(m, "Roof", new Vector3(0f, 5.7f, 0f), 7f, 0.6f, gold, collider: false, simple: true);
        Redented(m, "Roof2", new Vector3(0f, 6.4f, 0f), 5.4f, 0.8f, orange, collider: false, simple: true);
        Spire(m, new Vector3(0f, 6.8f, 0f), 9f, gold, green);
    }

    // ปราสาทพระเทพบิดร: a cross-shaped hall with stacked gabled roofs and a porcelain prang on top
    private static void Prasat(Transform t, Vector3 at, Material white, Material gold, Material orange, Material green)
    {
        var p = PrimitiveKit.Group("Prasat_ปราสาทพระเทพบิดร", t, at).transform;
        PrimitiveKit.Box("Base", p, new Vector3(0f, 0.4f, 0f), new Vector3(10f, 0.8f, 10f), white);
        foreach (float yaw in new[] { 0f, 90f })
        {
            var arm = PrimitiveKit.Group("Arm", p, Vector3.zero, yaw).transform;
            PrimitiveKit.Box("Wing", arm, new Vector3(0f, 2.6f, 0f), new Vector3(4.4f, 3.6f, 9f), white);
            foreach (float s in new[] { -1f, 1f })
            {
                PrimitiveKit.Box("Roof", arm, new Vector3(s * 1.45f, 5.2f, 0f), new Vector3(3.6f, 0.2f, 9.6f), orange, new Vector3(0f, 0f, -s * 38f), collider: false);
                PrimitiveKit.Box("RoofEdge", arm, new Vector3(s * 1.5f, 5.25f, 0f), new Vector3(3.7f, 0.12f, 9.9f), green, new Vector3(0f, 0f, -s * 38f), collider: false);
            }
            foreach (float s in new[] { -1f, 1f }) // gold gable boards (หน้าบัน) at the ends
                PrimitiveKit.Box("Gable", arm, new Vector3(0f, 5.4f, s * 4.75f), new Vector3(3.6f, 1.8f, 0.1f), gold, collider: false);
        }
        Prang(p, new Vector3(0f, 5.6f, 0f), 8f, 2.4f, 8);
    }

    // หอระฆัง: a bell hanging in an open white tower under an orange roof
    private static void BellTower(Transform t, Vector3 at, Material white, Material gold, Material orange, Material lacquer)
    {
        var b = PrimitiveKit.Group("BellTower_หอระฆัง", t, at).transform;
        PrimitiveKit.Box("Base", b, new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 4f), white);
        foreach (float x in new[] { -1.6f, 1.6f })
            foreach (float z in new[] { -1.6f, 1.6f })
                PrimitiveKit.Box("Post", b, new Vector3(x, 4.3f, z), new Vector3(0.35f, 2.6f, 0.35f), lacquer, collider: false);
        PrimitiveKit.Cylinder("Bell", b, new Vector3(0f, 4.6f, 0f), new Vector3(1.1f, 0.6f, 1.1f), gold, collider: false);
        Redented(b, "Roof", new Vector3(0f, 5.85f, 0f), 4.6f, 0.5f, orange, collider: false, simple: true);
        Spire(b, new Vector3(0f, 6.1f, 0f), 3.5f, gold, orange);
    }

    // A tiered spire: shrinking gold drums with orange bands up to a point
    private static void Spire(Transform t, Vector3 at, float height, Material gold, Material band)
    {
        const int tiers = 6;
        for (int i = 0; i < tiers; i++)
        {
            float r = Mathf.Lerp(1.6f, 0.35f, i / (float)(tiers - 1));
            float h = height / tiers;
            PrimitiveKit.Cylinder("SpireTier", t, at + new Vector3(0f, h * (i + 0.5f), 0f), new Vector3(r * 2f, h * 0.42f, r * 2f), i % 2 == 0 ? gold : band, collider: false);
        }
        PrimitiveKit.Cylinder("SpireTip", t, at + new Vector3(0f, height + 1f, 0f), new Vector3(0.12f, 1f, 0.12f), gold, collider: false);
    }

    // Khmer-style prang clad in porcelain: stepped base, a ribbed tapering tower, gold finial.
    // width: the tower at its foot; rings: how many bands it rises in
    private static void Prang(Transform t, Vector3 at, float height, float width = 2.6f, int rings = 9)
    {
        var p = PrimitiveKit.Group("Prang_ปรางค์", t, at).transform;
        var tile = PrimitiveKit.Mat("PrangPorcelain", new Color(0.86f, 0.78f, 0.8f), 0.6f);
        var tile2 = PrimitiveKit.Mat("PrangPorcelainBlue", new Color(0.55f, 0.72f, 0.82f), 0.6f);
        float b = width / 2.6f;
        PrimitiveKit.Box("Base", p, new Vector3(0f, 0.5f * b, 0f), new Vector3(4f * b, 1f * b, 4f * b), White);
        PrimitiveKit.Box("Base2", p, new Vector3(0f, 1.4f * b, 0f), new Vector3(3.2f * b, 0.8f * b, 3.2f * b), tile2);
        float y = 1.8f * b;
        for (int i = 0; i < rings; i++)
        {
            float k = i / (float)(rings - 1);
            float s = Mathf.Lerp(width, width * 0.23f, k * k);
            float h = (height - y) / (rings - i);
            PrimitiveKit.Box("Ring", p, new Vector3(0f, y + h / 2f, 0f), new Vector3(s, h * 0.92f, s), i % 2 == 0 ? tile : tile2, new Vector3(0f, 45f * (i % 2), 0f), collider: i == 0);
            y += h;
        }
        PrimitiveKit.Cylinder("Finial", p, new Vector3(0f, y + 0.9f * b, 0f), new Vector3(0.18f * b, 0.9f * b, 0.18f * b), Gold, collider: false);
    }

    // ── แม่น้ำเจ้าพระยา between the city and วัดอรุณ ─────────────────────────────────
    // White embankments on both banks; the city side has a promenade with benches (the classic view
    // of วัดอรุณ across the water), the temple side a promenade lined with flags. The ferry crosses.
    private static void River(Transform root)
    {
        var t = PrimitiveKit.Group("ChaoPhraya_แม่น้ำเจ้าพระยา", root, new Vector3(0f, 0f, RiverZ)).transform;
        const float len = 220f, pierX = 12f, gap = 3.2f;
        var white = White;
        var pave = PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f));
        var wood = PrimitiveKit.Mat("WoodPlank", new Color(0.6f, 0.4f, 0.22f));
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
        PrimitiveKit.Box("Water", t, new Vector3(0f, 0.03f, 0f), new Vector3(len, 0.04f, RiverHalf * 2f), PrimitiveKit.Mat("RiverWater", new Color(0.42f, 0.42f, 0.34f), 0.9f), collider: false);

        // Promenades on both banks, each behind a white balustrade that opens at the pier
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("Promenade", t, new Vector3(0f, 0.02f, side * (RiverHalf + 2f)), new Vector3(len, 0.03f, 4f), pave, collider: false);
        foreach (float side in new[] { -1f, 1f })
        {
            float z = side * (RiverHalf + 0.15f), baseY = 0f;
            foreach (float s in new[] { -1f, 1f }) // two runs either side of the pier opening
            {
                float x0 = s < 0f ? -len / 2f : pierX + gap, x1 = s < 0f ? pierX - gap : len / 2f;
                var mid = new Vector3((x0 + x1) / 2f, 0f, z);
                PrimitiveKit.Box("Balustrade", t, mid + new Vector3(0f, baseY + 0.45f, 0f), new Vector3(x1 - x0, 0.9f, 0.3f), white);
                PrimitiveKit.Box("Coping", t, mid + new Vector3(0f, baseY + 0.95f, 0f), new Vector3(x1 - x0, 0.1f, 0.42f), white, collider: false);
                Wall(t, mid + new Vector3(0f, baseY + 1f, 0f), new Vector3(x1 - x0, 2f, 0.3f));
            }
        }
        for (float x = -60f; x <= 60f; x += 15f) // benches on the city side, looking across
        {
            if (Mathf.Abs(x - pierX) < 5f) continue;
            PrimitiveKit.Box("Bench", t, new Vector3(x, 0.45f, -RiverHalf - 2.2f), new Vector3(2.4f, 0.1f, 0.6f), wood, collider: false);
        }
        foreach (float x in new[] { -70f, -55f, -42f, 42f, 55f, 70f }) // trees along the temple bank
            Places.ShadeTree(t, new Vector3(x, 0f, RiverHalf + 6f), 1f);

        // Flags along the temple-side promenade: the national flag and the yellow ธงธรรมจักร in turn
        for (int i = 0; i < 12; i++)
        {
            var at = new Vector3(-33f + i * 6f, 0f, RiverHalf + 1.2f);
            if (Mathf.Abs(at.x - pierX) < 3f || Mathf.Abs(at.x) < 5f) continue; // the pier, the temple's name board
            if (i % 2 == 0) Places2.ThaiFlag(t, at);
            else DharmaFlag(t, at);
        }

        // The ferry piers, a boat waiting at each, and the crossing itself
        Transform nearLanding = null, farLanding = null;
        foreach (float side in new[] { -1f, 1f })
        {
            const float deckY = 0.25f;
            var pier = new Vector3(pierX, 0f, side * (RiverHalf - 2.4f));
            PrimitiveKit.Box("Pier", t, pier + new Vector3(0f, deckY, 0f), new Vector3(5f, 0.16f, 5f), wood);
            PrimitiveKit.Box("PierRoof", t, pier + new Vector3(0f, deckY + 2.7f, 0f), new Vector3(5.6f, 0.12f, 5.6f), PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f)), collider: false);
            foreach (float x in new[] { -2.3f, 2.3f })
                foreach (float z in new[] { -2.3f, 2.3f })
                    PrimitiveKit.Cylinder("PierPost", t, pier + new Vector3(x, deckY + 1.35f, z), new Vector3(0.14f, 1.35f, 0.14f), steel, collider: false);
            PrimitiveKit.Label(side < 0f ? "ท่าเตียน · เรือหางยาวข้ามฟากไปวัดอรุณ 10 บาท" : "ท่าเรือวัดอรุณ · เรือหางยาวข้ามฟาก 10 บาท", t,
                pier + new Vector3(0f, deckY + 2.45f, -side * 2.85f), 0.035f, Color.white, side < 0f ? 0f : 180f);
            Seaside.LongtailBoat(t, pier + new Vector3(4.8f, 0f, side * -0.6f), 90f, side < 0f ? new Color(0.95f, 0.35f, 0.25f) : new Color(0.2f, 0.6f, 0.9f));
            var landing = PrimitiveKit.Group(side < 0f ? "FerryLanding_Near" : "FerryLanding_Far", t,
                new Vector3(pierX, 0f, side * (RiverHalf + 2.2f)), side < 0f ? 180f : 0f).transform; // facing away from the water
            if (side < 0f) nearLanding = landing; else farLanding = landing;
        }
        foreach (float side in new[] { -1f, 1f })
        {
            var zone = new GameObject("FerryZone_เรือข้ามฟาก");
            zone.transform.SetParent(t, false);
            zone.transform.localPosition = new Vector3(pierX, 1.25f, side * (RiverHalf - 3.8f));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4.6f, 2f, 1.6f);
            zone.AddComponent<FerryCrossing>().Configure(side < 0f ? farLanding : nearLanding,
                side < 0f ? "นั่งเรือหางยาว → วัดอรุณ" : "นั่งเรือหางยาว → ท่าเตียน");
        }

        FerryBoat(t, new Vector3(pierX - 4.4f, 0f, 0f));
        DinnerCruise(t, new Vector3(0f, 0f, -4.5f));

        // Out on the water: a longtail with tourists and a river-express boat
        Seaside.LongtailBoat(t, new Vector3(-26f, 0f, 3f), 95f, new Color(0.95f, 0.8f, 0.2f));
        Seaside.LongtailBoat(t, new Vector3(-48f, 0f, -4f), -85f, new Color(0.3f, 0.85f, 0.6f));
        ExpressBoat(t, new Vector3(50f, 0f, 4f));
        ExpressBoat(t, new Vector3(-75f, 0f, -5f));
    }

    // Crossing by เรือหางยาว: a longtail with its boatman at the tiller and a few passengers, going to
    // and fro between the two piers
    private static void FerryBoat(Transform t, Vector3 at)
    {
        var lane = PrimitiveKit.Group("FerryLane", t, at).transform;
        var b = PrimitiveKit.Group("FerryLongtail_เรือหางยาวข้ามฟาก", lane, Vector3.zero).transform;
        b.gameObject.AddComponent<FerryShuttle>();
        Seaside.LongtailBoat(b, Vector3.zero, 0f, new Color(0.95f, 0.75f, 0.2f));
        // The boatman by the tiller and the passengers on the benches, set low so they look seated
        var boatman = PrimitiveKit.Group("Boatman", b, new Vector3(0.05f, -0.55f, Seaside.BoatmanZ));
        boatman.AddComponent<MarketVendor>().Configure(4);
        boatman.AddComponent<SeatedPose>();
        int look = 6;
        foreach (var p in new[] { new Vector3(-0.28f, -0.6f, -0.9f), new Vector3(0.3f, -0.6f, 0.6f), new Vector3(-0.2f, -0.55f, 2.0f) })
        {
            var rider = PrimitiveKit.Group("Passenger", b, p);
            rider.AddComponent<MarketVendor>().Configure(look++);
            rider.AddComponent<SeatedPose>();
        }
    }

    // เรือสำราญ dinner cruise: a two-deck white boat, glowing windows below, an open deck above with
    // dinner tables, a band and Thai dancers on a little stage, strings of colour bulbs from bow to
    // stern and up to the mast, an LED band round the hull and coloured washes on the water.
    // NightCruise sails it and runs the show; it only comes out after dark.
    private static void DinnerCruise(Transform t, Vector3 at)
    {
        var root = PrimitiveKit.Group("NightCruise_เรือสำราญ", t, at).transform;
        root.gameObject.AddComponent<NightCruise>();
        var b = PrimitiveKit.Group("Boat", root, Vector3.zero, 90f).transform; // sails along x; the bow is local +z
        var white = PrimitiveKit.Mat("JetWhite", new Color(0.95f, 0.95f, 0.96f), 0.7f, 0.2f);
        var gold = Gold;
        var window = PrimitiveKit.Unlit("CruiseWindowLit", new Color(1.3f, 0.9f, 0.45f)); // unlit: lit rooms and bulbs read at night
        var bulb = PrimitiveKit.Unlit("CruiseBulbLit", Color.white * 2f);
        var led = PrimitiveKit.Unlit("CruiseLedLit", new Color(0.3f, 0.5f, 1f) * 2f);
        const float len = 26f, beam = 6f;

        PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.4f, 0f), new Vector3(beam, 1.4f, len), white, collider: false);
        PrimitiveKit.Box("Bow", b, new Vector3(0f, 0.6f, len / 2f + 1.2f), new Vector3(beam * 0.7f, 1.1f, 3f), white, new Vector3(-10f, 0f, 0f), collider: false);
        PrimitiveKit.Box("GoldStripe", b, new Vector3(0f, 1.15f, 0f), new Vector3(beam + 0.04f, 0.12f, len), gold, collider: false);
        PrimitiveKit.Box("LedBand", b, new Vector3(0f, 0.55f, 0f), new Vector3(beam + 0.06f, 0.22f, len - 0.4f), led, collider: false);
        PrimitiveKit.Box("Cabin", b, new Vector3(0f, 2.2f, -0.5f), new Vector3(beam - 0.6f, 2.2f, len - 4f), window, collider: false);
        for (float z = -len / 2f + 2.5f; z < len / 2f - 2f; z += 2f) // window mullions
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("Mullion", b, new Vector3(s * (beam / 2f - 0.28f), 2.2f, z), new Vector3(0.06f, 2.2f, 0.2f), white, collider: false);
        PrimitiveKit.Box("UpperDeck", b, new Vector3(0f, 3.4f, 0f), new Vector3(beam, 0.2f, len - 1f), white, collider: false);

        // Rails round the upper deck, a canopy over the middle, the bridge forward
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Rail", b, new Vector3(s * (beam / 2f - 0.05f), 4.1f, 0f), new Vector3(0.08f, 0.08f, len - 1.2f), white, collider: false);
        foreach (float z in new[] { -len / 2f + 0.65f, len / 2f - 0.65f })
            PrimitiveKit.Box("Rail", b, new Vector3(0f, 4.1f, z), new Vector3(beam - 0.1f, 0.08f, 0.08f), white, collider: false);
        foreach (float s in new[] { -1f, 1f })
            foreach (float z in new[] { -6f, 0f, 6f })
                PrimitiveKit.Box("CanopyPost", b, new Vector3(s * 2.6f, 4.6f, z), new Vector3(0.12f, 2.2f, 0.12f), gold, collider: false);
        PrimitiveKit.Box("Canopy", b, new Vector3(0f, 5.75f, 0f), new Vector3(beam - 0.4f, 0.12f, 13f), PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f)), collider: false);
        PrimitiveKit.Box("Bridge", b, new Vector3(0f, 4.4f, len / 2f - 2.5f), new Vector3(3f, 1.8f, 2.4f), white, collider: false);
        PrimitiveKit.Box("BridgeGlass", b, new Vector3(0f, 4.6f, len / 2f - 1.28f), new Vector3(2.6f, 0.8f, 0.05f), window, collider: false);
        PrimitiveKit.Cylinder("Mast", b, new Vector3(0f, 7f, len / 2f - 2.5f), new Vector3(0.12f, 1.8f, 0.12f), white, collider: false);
        PrimitiveKit.Label("เรือสำราญเจ้าพระยา · ดินเนอร์ล่องเรือ", b, new Vector3(beam / 2f + 0.06f, 2.2f, 0f), 0.06f, new Color(1f, 0.85f, 0.4f), -90f);

        // Dinner tables with white cloths and candle lamps
        var cloth = PrimitiveKit.Mat("TableCloth", Color.white);
        for (int i = 0; i < 4; i++)
            foreach (float s in new[] { -1.5f, 1.5f })
            {
                var p = new Vector3(s, 3.5f, -4.5f + i * 3f);
                PrimitiveKit.Box("Table", b, p + new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.08f, 1.4f), cloth, collider: false);
                PrimitiveKit.Box("TableLeg", b, p + new Vector3(0f, 0.2f, 0f), new Vector3(0.15f, 0.4f, 0.15f), cloth, collider: false);
                PrimitiveKit.Sphere("Candle", b, p + new Vector3(0f, 0.55f, 0f), Vector3.one * 0.14f, window);
            }

        // The stage at the stern: a ramwong band and two dancers in gold
        var stage = new Vector3(0f, 3.5f, -len / 2f + 3f);
        PrimitiveKit.Box("Stage", b, stage + new Vector3(0f, 0.15f, 0f), new Vector3(beam - 0.6f, 0.3f, 3.6f), PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f), collider: false);
        var costume = PrimitiveKit.Mat("DancerGold", new Color(0.95f, 0.72f, 0.2f), 0.6f, 0.4f);
        var skin = PrimitiveKit.Mat("SkinTan", new Color(0.82f, 0.62f, 0.45f));
        foreach (float x in new[] { -0.9f, 0.9f })
        {
            var p = stage + new Vector3(x, 0.3f, 0.4f);
            PrimitiveKit.Cylinder("Dancer", b, p + new Vector3(0f, 0.75f, 0f), new Vector3(0.45f, 0.75f, 0.45f), costume, collider: false);
            PrimitiveKit.Sphere("Head", b, p + new Vector3(0f, 1.75f, 0f), Vector3.one * 0.32f, skin);
            PrimitiveKit.Cylinder("Chada", b, p + new Vector3(0f, 2.15f, 0f), new Vector3(0.12f, 0.25f, 0.12f), Gold, collider: false);
        }
        foreach (float x in new[] { -2f, 2f })
        {
            PrimitiveKit.Box("Ranat", b, stage + new Vector3(x, 0.55f, -1.1f), new Vector3(1.1f, 0.3f, 0.4f), PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f)), collider: false);
            PrimitiveKit.Cylinder("Musician", b, stage + new Vector3(x, 0.85f, -1.55f), new Vector3(0.4f, 0.55f, 0.4f), cloth, collider: false);
            PrimitiveKit.Sphere("Head", b, stage + new Vector3(x, 1.6f, -1.55f), Vector3.one * 0.3f, skin);
        }

        // Strings of bulbs: along both upper rails, round the canopy, and up from bow and stern to the mast
        foreach (float s in new[] { -1f, 1f })
        {
            for (float z = -len / 2f + 1f; z <= len / 2f - 1f; z += 1.1f)
                PrimitiveKit.Sphere("Bulb", b, new Vector3(s * (beam / 2f - 0.05f), 4.25f, z), Vector3.one * 0.2f, bulb);
            for (float z = -6.4f; z <= 6.4f; z += 0.8f)
                PrimitiveKit.Sphere("Bulb", b, new Vector3(s * 2.75f, 5.65f, z), Vector3.one * 0.18f, bulb);
            for (float z = -len / 2f + 1f; z <= len / 2f - 1f; z += 1.1f)
                PrimitiveKit.Sphere("Bulb", b, new Vector3(s * (beam / 2f + 0.03f), 1.4f, z), Vector3.one * 0.16f, bulb);
        }
        var top = new Vector3(0f, 8.7f, len / 2f - 2.5f);
        foreach (var end in new[] { new Vector3(0f, 1.2f, len / 2f + 2.5f), new Vector3(0f, 4.2f, -len / 2f + 0.6f) })
            for (int i = 1; i < 14; i++)
            {
                float k = i / 14f;
                var p = Vector3.Lerp(end, top, k) - new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.6f, 0f); // a little sag
                PrimitiveKit.Sphere("Bulb", b, p, Vector3.one * 0.18f, bulb);
            }

        // Coloured washes thrown on the water round the hull
        foreach (float z in new[] { -9f, 0f, 9f })
        {
            var go = new GameObject("Wash");
            go.transform.SetParent(b, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, z);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 16f;
            light.intensity = 5f;
            light.shadows = LightShadows.None;
        }
    }

    private static void DharmaFlag(Transform t, Vector3 at)
    {
        PrimitiveKit.Cylinder("Flagpole", t, at + new Vector3(0f, 4f, 0f), new Vector3(0.12f, 4f, 0.12f), PrimitiveKit.Mat("RopeWhite", Color.white));
        PrimitiveKit.Box("DharmaFlag", t, at + new Vector3(0.75f, 7.1f, 0f), new Vector3(1.5f, 1f, 0.03f), PrimitiveKit.Mat("DharmaYellow", new Color(1f, 0.8f, 0.1f)), collider: false);
        PrimitiveKit.Cylinder("DharmaWheel", t, at + new Vector3(0.75f, 7.1f, 0f), new Vector3(0.5f, 0.02f, 0.5f), PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f), new Vector3(90f, 0f, 0f), collider: false);
    }

    // เรือด่วนเจ้าพระยา: a long white hull with an orange stripe and a roofed cabin
    private static void ExpressBoat(Transform t, Vector3 at)
    {
        var b = PrimitiveKit.Group("ExpressBoat_เรือด่วน", t, at, 90f).transform;
        var white = PrimitiveKit.Mat("JetWhite", new Color(0.95f, 0.95f, 0.96f), 0.7f, 0.2f);
        PrimitiveKit.Box("Hull", b, new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.9f, 16f), white, collider: false);
        PrimitiveKit.Box("Bow", b, new Vector3(0f, 0.45f, 8.6f), new Vector3(2.2f, 0.7f, 2f), white, new Vector3(-12f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Stripe", b, new Vector3(0f, 0.55f, 0f), new Vector3(3.05f, 0.18f, 16f), PrimitiveKit.Mat("ExpressOrange", new Color(0.95f, 0.5f, 0.1f)), collider: false);
        PrimitiveKit.Box("Cabin", b, new Vector3(0f, 1.5f, -0.5f), new Vector3(2.6f, 1.6f, 11f), PrimitiveKit.Mat("TerminalGlass", new Color(0.45f, 0.6f, 0.68f), 0.92f, 0.5f), collider: false);
        PrimitiveKit.Box("Roof", b, new Vector3(0f, 2.4f, -0.5f), new Vector3(2.9f, 0.15f, 11.6f), white, collider: false);
    }

    // ── วัดอรุณ ───────────────────────────────────────────────────────────────────
    // พระปรางค์ (after the real one): terraces stepped in with redented (ย่อมุม) corners, each edge
    // lined with little pinnacles under red-brown caps; a body with niches; then the tall corn-cob
    // spire in ribbed bands up to a gold trident. Everything is clad in a porcelain mosaic (grey-white
    // with coloured flowers, ArunMosaic). Four smaller prangs stand at the corners, four มณฑป between,
    // and two วิหาร with orange roofs (walk-in) either side.
    private static void WatArun(Transform root, Vector3 at)
    {
        var t = PrimitiveKit.Group("WatArun_วัดอรุณ", root, at).transform;
        var marble = PrimitiveKit.Mat("MarbleFloor", new Color(0.88f, 0.87f, 0.84f), 0.5f);
        PrimitiveKit.Box("Courtyard", t, new Vector3(0f, 0.02f, 0f), new Vector3(30f, 0.04f, 26f), marble, collider: false);

        ArunPrang(t, Vector3.zero, 1f, pinnacles: true);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                ArunPrang(t, new Vector3(x * 12f, 0f, z * 10f), 0.36f, pinnacles: false);

        var tile = Porcelain();
        for (int side = 0; side < 4; side++)
        {
            var dir = Quaternion.Euler(0f, side * 90f, 0f);
            var m = PrimitiveKit.Group("Mondop_มณฑป", t, dir * new Vector3(0f, 0f, -12.5f), side * 90f).transform;
            PrimitiveKit.Box("Base", m, new Vector3(0f, 0.6f, 0f), new Vector3(3f, 1.2f, 3f), White);
            Redented(m, "Body", new Vector3(0f, 2.2f, 0f), 2.2f, 2f, tile);
            Spire(m, new Vector3(0f, 3.2f, 0f), 3.4f, tile, PrimitiveKit.Mat("MosaicTerracotta", new Color(0.66f, 0.38f, 0.26f), 0.5f));
        }

        // Two วิหาร with orange-and-green roofs facing in from either side
        var white = White;
        var gold = Gold;
        var orange = PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f));
        var green = PrimitiveKit.Mat("TempleGreen", new Color(0.15f, 0.45f, 0.3f));
        foreach (float s in new[] { -1f, 1f })
        {
            var hall = PrimitiveKit.Group("Viharn_วิหาร", t, new Vector3(s * 27f, 0f, 3f), s * 90f).transform;
            Places.Ubosot(hall, Vector3.zero, white, gold, orange, green);
        }

        // Guardian yaksha at the gate, the name on a board in front
        Yaksha(t, new Vector3(-3.5f, 0f, -12f), new Color(0.92f, 0.92f, 0.88f));
        Yaksha(t, new Vector3(3.5f, 0f, -12f), new Color(0.15f, 0.5f, 0.35f));
        PrimitiveKit.Box("NameBoard", t, new Vector3(0f, 1.2f, -15.8f), new Vector3(6f, 1f, 0.12f), PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f));
        PrimitiveKit.Label("วัดอรุณราชวราราม", t, new Vector3(0f, 1.2f, -15.88f), 0.05f, new Color(1f, 0.85f, 0.4f));
        foreach (float x in new[] { -7f, 7f })
            foreach (float z in new[] { -12.5f, 12.5f })
                StreetFurniture.LampPost(t, new Vector3(x, 0f, z));
        // Gold floodlights from the four sides light the prang up after dark, two narrow ones from the
        // river side pick out the spire, and warm lines of light trace each terrace
        for (int side = 0; side < 4; side++)
        {
            var dir = Quaternion.Euler(0f, side * 90f + 45f, 0f);
            Flood(t, "FloodLight_ไฟส่องพระปรางค์", dir * new Vector3(0f, 0.4f, -18f), new Vector3(0f, 20f, 0f), 320f, 70f, 55f);
        }
        foreach (float x in new[] { -5f, 5f })
            Flood(t, "SpireLight_ไฟส่องยอดปรางค์", new Vector3(x, 0.4f, -16f), new Vector3(0f, 34f, 0f), 420f, 80f, 26f);
        var glow = NightGlow(t);
        float ty = 0f;
        for (int i = 0; i < ArunTerrace.Length; i++)
        {
            ty += ArunRise[i];
            float half = ArunTerrace[i] / 2f + 0.16f;
            for (int side = 0; side < 4; side++)
            {
                var dir = Quaternion.Euler(0f, side * 90f, 0f);
                foreach (float s in new[] { -1f, 1f }) // either side of the stairs
                    GlowLine(glow, dir * new Vector3(s * 1.3f, ty - 0.25f, -half), dir * new Vector3(s * ArunTerrace[i] * 0.33f, ty - 0.25f, -half));
            }
        }
    }

    // ── Night lighting for the temples ──────────────────────────────────────────────
    private static Material GlowWarm => PrimitiveKit.Unlit("TempleGlowWarm", new Color(1.7f, 1.2f, 0.55f)); // unlit, bright enough to bloom

    // A spot floodlight on the ground with its lamp housing; only on after dark (FloodLight)
    private static void Flood(Transform t, string name, Vector3 local, Vector3 aim, float intensity, float range, float angle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(t, false);
        go.transform.localPosition = local;
        go.transform.LookAt(t.TransformPoint(aim));
        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.72f, 0.38f);
        light.intensity = intensity; // falls off with the square of the distance
        light.range = range;
        light.spotAngle = angle;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel; // "important": not culled for the many street lamps
        go.AddComponent<FloodLight>();
        PrimitiveKit.Box("FloodLamp", t, local, new Vector3(0.5f, 0.35f, 0.5f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
    }

    // A group whose glowing lines only show after dark (NightOnly)
    private static Transform NightGlow(Transform t)
    {
        var g = PrimitiveKit.Group("NightGlow_ไฟประดับ", t, Vector3.zero).transform;
        g.gameObject.AddComponent<NightOnly>();
        return g;
    }

    // A thin line of warm light from a to b (LED outline)
    private static void GlowLine(Transform g, Vector3 a, Vector3 b, float thick = 0.12f)
    {
        var line = PrimitiveKit.Box("GlowLine", g, (a + b) / 2f, new Vector3(thick, thick, Vector3.Distance(a, b) + thick), GlowWarm, collider: false);
        line.transform.localRotation = Quaternion.LookRotation(b - a);
        line.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // One prang: scale 1 is the great one (about 50 m to the tip)
    private static void ArunPrang(Transform parent, Vector3 at, float scale, bool pinnacles)
    {
        var p = PrimitiveKit.Group(scale >= 1f ? "PhraPrang_พระปรางค์" : "PrangTit_ปรางค์ทิศ", parent, at).transform;
        p.localScale = Vector3.one * scale;
        bool detail = scale >= 1f; // the small prangs get fewer, plainer parts
        var tile = Porcelain();
        var white = White;
        var cap = PrimitiveKit.Mat("PinnacleCap", new Color(0.58f, 0.27f, 0.2f), 0.4f);
        var bands = new[]
        {
            PrimitiveKit.Mat("MosaicTerracotta", new Color(0.66f, 0.38f, 0.26f), 0.5f),
            PrimitiveKit.Mat("MosaicTeal", new Color(0.32f, 0.55f, 0.55f), 0.6f),
            PrimitiveKit.Mat("MosaicCream", new Color(0.9f, 0.86f, 0.74f), 0.6f),
        };

        // Terraces: square, redented, each with a band of colour under its edge and a balustrade
        float[] size = ArunTerrace, rise = ArunRise;
        float y = 0f;
        for (int i = 0; i < size.Length; i++)
        {
            Redented(p, "Terrace", new Vector3(0f, y + rise[i] / 2f, 0f), size[i], rise[i], i % 2 == 0 ? tile : white, simple: !detail);
            Redented(p, "Band", new Vector3(0f, y + rise[i] - 0.25f, 0f), size[i] + 0.25f, 0.35f, bands[i % bands.Length], collider: false, simple: !detail);
            y += rise[i];
            if (!detail) continue;
            Balustrade(p, y, size[i], white);
            for (int side = 0; side < 4; side++) // the steep stairs up the middle of each face
            {
                var dir = Quaternion.Euler(0f, side * 90f, 0f);
                PrimitiveKit.Box("Stairs", p, dir * new Vector3(0f, y - rise[i] / 2f, -size[i] / 2f - 0.35f), new Vector3(1.8f, rise[i], 0.5f),
                    PrimitiveKit.Mat("StairShade", new Color(0.5f, 0.47f, 0.43f)), new Vector3(-12f, side * 90f, 0f), collider: false);
            }
            if (pinnacles && i < 3)
            {
                float edge = size[i] / 2f - 0.7f;
                int n = Mathf.Max(2, Mathf.RoundToInt(size[i] / 3.2f));
                for (int side = 0; side < 4; side++)
                {
                    var dir = Quaternion.Euler(0f, side * 90f, 0f);
                    for (int k = 0; k <= n; k++)
                    {
                        float x = Mathf.Lerp(-edge, edge, k / (float)n);
                        if (Mathf.Abs(x) < 1.6f) continue; // the stairs
                        if (k == n && side > 0) continue;  // corners once
                        Pinnacle(p, dir * new Vector3(x, y, -edge), 1f + (k == 0 || k == n ? 0.35f : 0f), tile, cap);
                    }
                }
            }
        }

        // Body (เรือนธาตุ) with a niche on each face
        const float bodyW = 7.5f, bodyH = 6f;
        Redented(p, "Body", new Vector3(0f, y + bodyH / 2f, 0f), bodyW, bodyH, tile, simple: !detail);
        for (int side = 0; side < 4; side++)
        {
            var dir = Quaternion.Euler(0f, side * 90f, 0f);
            PrimitiveKit.Box("Niche", p, dir * new Vector3(0f, y + 2.8f, -bodyW / 2f - 0.6f), new Vector3(1.6f, 3.2f, 0.12f), PrimitiveKit.Mat("NicheShade", new Color(0.18f, 0.16f, 0.15f)), new Vector3(0f, side * 90f, 0f), collider: false);
            PrimitiveKit.Box("NicheFigure", p, dir * new Vector3(0f, y + 2.6f, -bodyW / 2f - 0.7f), new Vector3(1f, 2.2f, 0.14f), white, new Vector3(0f, side * 90f, 0f), collider: false);
            PrimitiveKit.Box("NicheArch", p, dir * new Vector3(0f, y + 4.6f, -bodyW / 2f - 0.65f), new Vector3(2.2f, 0.9f, 0.14f), Gold, new Vector3(0f, side * 90f, 0f), collider: false);
        }
        y += bodyH;
        Redented(p, "BodyCornice", new Vector3(0f, y + 0.2f, 0f), bodyW + 0.7f, 0.4f, bands[1], collider: false);
        y += 0.4f;

        // The corn-cob spire: ribbed bands narrowing to the top, a bell and the gold trident
        int rings = detail ? 20 : 11;
        const float spireH = 26f, foot = 7f, tip = 1.5f;
        float ringH = spireH / rings;
        for (int i = 0; i < rings; i++)
        {
            float k = i / (float)(rings - 1);
            float s = Mathf.Lerp(foot, tip, Mathf.Pow(k, 1.35f)); // stays full, then draws in to the tip
            Redented(p, "Rib", new Vector3(0f, y + ringH * 0.36f, 0f), s, ringH * 0.72f, i % 3 == 2 ? white : tile, collider: i < 2, simple: !detail);
            Redented(p, "Groove", new Vector3(0f, y + ringH * 0.86f, 0f), s * 0.88f, ringH * 0.28f, bands[i % 2 == 0 ? 0 : 1], collider: false, simple: true);
            y += ringH;
        }
        PrimitiveKit.Cylinder("Bell", p, new Vector3(0f, y + 0.5f, 0f), new Vector3(1f, 0.5f, 1f), white, collider: false);
        PrimitiveKit.Cylinder("Mast", p, new Vector3(0f, y + 2.2f, 0f), new Vector3(0.18f, 1.4f, 0.18f), Gold, collider: false);
        foreach (float s in new[] { -0.35f, 0f, 0.35f }) // the trident (ตรีศูล)
            PrimitiveKit.Box("Prong", p, new Vector3(s, y + 3.9f + (s == 0f ? 0.2f : 0f), 0f), new Vector3(0.1f, 0.8f, 0.1f), Gold, collider: false);
        PrimitiveKit.Box("ProngBar", p, new Vector3(0f, y + 3.5f, 0f), new Vector3(0.8f, 0.1f, 0.1f), Gold, collider: false);
    }

    // A redented (ย่อมุม) block: a square with its corners stepped in twice
    // (the outer steps a hair lower so no two tops share a plane; simple: one step, for small prangs)
    private static void Redented(Transform parent, string name, Vector3 at, float size, float height, Material mat, bool collider = true, bool simple = false)
    {
        TiledBox(name, parent, at, new Vector3(size * 0.78f, height, size * 0.78f), mat, collider);
        TiledBox(name, parent, at, new Vector3(size, height - 0.03f, size * 0.5f), mat, false);
        TiledBox(name, parent, at, new Vector3(size * 0.5f, height - 0.03f, size), mat, false);
        if (simple) return;
        TiledBox(name, parent, at, new Vector3(size * 0.9f, height - 0.06f, size * 0.66f), mat, false);
        TiledBox(name, parent, at, new Vector3(size * 0.66f, height - 0.06f, size * 0.9f), mat, false);
    }

    // A plain (unbevelled) box whose texture keeps its size on every face: one repeat per TileMetres
    // (PrimitiveKit's bevelled boxes carry no texture coordinates)
    private const float TileMetres = 2.5f;

    private static GameObject TiledBox(string name, Transform parent, Vector3 at, Vector3 size, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<MeshFilter>().sharedMesh = TiledCube(size);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static readonly System.Collections.Generic.Dictionary<string, Mesh> tiledCache = new System.Collections.Generic.Dictionary<string, Mesh>();

    private static Mesh TiledCube(Vector3 size)
    {
        const string folder = "Assets/Art/Meshes/Tiled";
        string key = $"Tiled_{size.x:0.###}_{size.y:0.###}_{size.z:0.###}";
        if (tiledCache.TryGetValue(key, out var mesh) && mesh != null) return mesh;
        mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>($"{folder}/{key}.asset");
        if (mesh == null)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Art/Meshes")) UnityEditor.AssetDatabase.CreateFolder("Assets/Art", "Meshes");
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Art/Meshes", "Tiled");
            mesh = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            mesh.name = key;
            var v = mesh.vertices;
            var n = mesh.normals;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var p = Vector3.Scale(v[i], size) / TileMetres;
                uv[i] = Mathf.Abs(n[i].y) > 0.5f ? new Vector2(p.x, p.z) : Mathf.Abs(n[i].x) > 0.5f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
            }
            mesh.uv = uv;
            UnityEditor.AssetDatabase.CreateAsset(mesh, $"{folder}/{key}.asset");
        }
        tiledCache[key] = mesh;
        return mesh;
    }

    private static void Balustrade(Transform p, float y, float size, Material white)
    {
        foreach (float k in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Balustrade", p, new Vector3(0f, y + 0.35f, k * (size / 2f - 0.15f)), new Vector3(size * 0.5f, 0.7f, 0.25f), white, collider: false);
            PrimitiveKit.Box("Balustrade", p, new Vector3(k * (size / 2f - 0.15f), y + 0.35f, 0f), new Vector3(0.25f, 0.7f, size * 0.5f), white, collider: false);
        }
    }

    // A little pinnacle (ปรางค์บริวาร) on a terrace edge: a porcelain stem under a red-brown cap
    private static void Pinnacle(Transform p, Vector3 at, float size, Material tile, Material cap)
    {
        TiledBox("PinnacleBase", p, at + new Vector3(0f, 0.4f * size, 0f), new Vector3(0.6f, 0.8f, 0.6f) * size, tile, false);
        PrimitiveKit.Cylinder("PinnacleCap", p, at + new Vector3(0f, 0.92f * size, 0f), new Vector3(0.72f, 0.12f, 0.72f) * size, cap, collider: false);
        PrimitiveKit.Cylinder("PinnacleCap", p, at + new Vector3(0f, 1.18f * size, 0f), new Vector3(0.5f, 0.14f, 0.5f) * size, cap, collider: false);
        PrimitiveKit.Cylinder("PinnacleTip", p, at + new Vector3(0f, 1.6f * size, 0f), new Vector3(0.24f, 0.3f, 0.24f) * size, cap, collider: false);
    }

    // The porcelain mosaic (กระเบื้องเคลือบ): grey-white with flowers of broken china in blue, green,
    // red, yellow and pink, generated once into Assets/Art/Materials
    private static Material Porcelain()
    {
        var mat = PrimitiveKit.Mat("ArunPorcelain", new Color(0.9f, 0.92f, 0.95f), 0.55f);
        const string path = "Assets/Art/Materials/ArunMosaic.asset";
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            const int n = 256;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "ArunMosaic", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            var rng = new System.Random(11);
            Color[] petals = { new Color(0.25f, 0.42f, 0.7f), new Color(0.3f, 0.6f, 0.45f), new Color(0.75f, 0.25f, 0.2f), new Color(0.9f, 0.7f, 0.25f), new Color(0.85f, 0.5f, 0.55f), new Color(0.35f, 0.6f, 0.65f) };
            for (int i = 0; i < px.Length; i++)
            {
                float v = 0.78f + (float)rng.NextDouble() * 0.1f;
                px[i] = new Color(v, v * 0.985f, v * 0.95f);
            }
            void Dot(int cx, int cy, int r, Color c)
            {
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (dx * dx + dy * dy <= r * r)
                            px[((cy + dy + n) % n) * n + (cx + dx + n) % n] = c * (0.9f + (float)rng.NextDouble() * 0.15f);
            }
            for (int gy = 0; gy < 8; gy++)
                for (int gx = 0; gx < 8; gx++)
                {
                    int cx = gx * 32 + 16 + (gy % 2) * 16, cy = gy * 32 + 16;
                    var petal = petals[rng.Next(petals.Length)];
                    int count = 5 + rng.Next(3);
                    for (int k = 0; k < count; k++)
                    {
                        float a = k * Mathf.PI * 2f / count;
                        Dot(cx + Mathf.RoundToInt(Mathf.Cos(a) * 7f), cy + Mathf.RoundToInt(Mathf.Sin(a) * 7f), 4, petal);
                    }
                    Dot(cx, cy, 3, petals[rng.Next(petals.Length)]);
                    for (int k = 0; k < 3; k++) // stray shards between the flowers
                        Dot(cx + rng.Next(-15, 16), cy + rng.Next(-15, 16), 1, petals[rng.Next(petals.Length)]);
                }
            for (int x = 0; x < n; x++) // a fine terracotta line every quarter, like the bands between tiers
                for (int y = 0; y < n; y += 64)
                    px[y * n + x] = new Color(0.62f, 0.36f, 0.26f);
            tex.SetPixels(px);
            tex.Apply(true);
            UnityEditor.AssetDatabase.CreateAsset(tex, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", Vector2.one);
        UnityEditor.EditorUtility.SetDirty(mat);
        return mat;
    }

    // A temple guardian giant (ยักษ์): a body in colour, gold armour and crown, both hands on a club
    private static void Yaksha(Transform t, Vector3 at, Color skin)
    {
        var y = PrimitiveKit.Group("Yaksha_ยักษ์", t, at).transform;
        var body = PrimitiveKit.Mat("Yaksha" + ColorUtility.ToHtmlStringRGB(skin), skin, 0.5f);
        var gold = Gold;
        var plinth = PrimitiveKit.Mat("PlinthGrey", new Color(0.55f, 0.53f, 0.5f));
        PrimitiveKit.Box("Plinth", y, new Vector3(0f, 0.4f, 0f), new Vector3(1.8f, 0.8f, 1.8f), plinth);
        foreach (float s in new[] { -0.35f, 0.35f })
            PrimitiveKit.Box("Leg", y, new Vector3(s, 1.7f, 0f), new Vector3(0.45f, 1.8f, 0.5f), body, collider: false);
        PrimitiveKit.Box("Skirt", y, new Vector3(0f, 2.4f, 0f), new Vector3(1.3f, 0.9f, 0.8f), gold, collider: false);
        PrimitiveKit.Box("Chest", y, new Vector3(0f, 3.4f, 0f), new Vector3(1.4f, 1.3f, 0.8f), body, collider: false);
        PrimitiveKit.Box("Armour", y, new Vector3(0f, 3.5f, -0.42f), new Vector3(1f, 0.9f, 0.06f), gold, collider: false);
        PrimitiveKit.Sphere("Head", y, new Vector3(0f, 4.45f, 0f), new Vector3(0.75f, 0.8f, 0.7f), body);
        PrimitiveKit.Cylinder("Crown", y, new Vector3(0f, 5.2f, 0f), new Vector3(0.55f, 0.45f, 0.55f), gold, collider: false);
        PrimitiveKit.Cylinder("CrownTip", y, new Vector3(0f, 5.85f, 0f), new Vector3(0.22f, 0.3f, 0.22f), gold, collider: false);
        foreach (float s in new[] { -0.85f, 0.85f })
            PrimitiveKit.Box("Arm", y, new Vector3(s, 3.1f, -0.25f), new Vector3(0.35f, 1.2f, 0.4f), body, new Vector3(25f, 0f, -s * 15f), collider: false);
        PrimitiveKit.Cylinder("Club", y, new Vector3(0f, 1.9f, -0.75f), new Vector3(0.22f, 1.5f, 0.22f), PrimitiveKit.Mat("TempleLacquer", new Color(0.45f, 0.07f, 0.06f), 0.6f), collider: false);
        var col = y.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 2.5f, 0f);
        col.size = new Vector3(1.8f, 5f, 1.8f);
    }

    // ── ศาลพระพรหม ─────────────────────────────────────────────────────────────
    private static void BrahmaShrine(Transform root, Vector3 at)
    {
        var t = PrimitiveKit.Group("Shrine_ศาลพระพรหม", root, at).transform;
        var marble = PrimitiveKit.Mat("MarbleFloor", new Color(0.88f, 0.87f, 0.84f), 0.5f);
        var gold = Gold;
        PrimitiveKit.Cylinder("Island", t, new Vector3(0f, 0.15f, 0f), new Vector3(6.4f, 0.15f, 6.4f), marble);
        PrimitiveKit.Cylinder("Pedestal", t, new Vector3(0f, 0.6f, 0f), new Vector3(2.8f, 0.3f, 2.8f), White);
        foreach (float x in new[] { -1.1f, 1.1f })
            foreach (float z in new[] { -1.1f, 1.1f })
                PrimitiveKit.Cylinder("Pillar", t, new Vector3(x, 2.1f, z), new Vector3(0.22f, 1.2f, 0.22f), gold);
        PrimitiveKit.Box("Roof", t, new Vector3(0f, 3.4f, 0f), new Vector3(3f, 0.25f, 3f), gold, collider: false);
        Spire(t, new Vector3(0f, 3.5f, 0f), 2.6f, gold, PrimitiveKit.Mat("TempleOrange", new Color(0.9f, 0.45f, 0.12f)));
        PrimitiveKit.Cylinder("Brahma", t, new Vector3(0f, 1.45f, 0f), new Vector3(0.6f, 0.55f, 0.6f), gold, collider: false);
        PrimitiveKit.Sphere("BrahmaHeads", t, new Vector3(0f, 2.2f, 0f), Vector3.one * 0.5f, gold);
        // Marigold garlands round the pedestal
        var marigold = PrimitiveKit.Mat("Marigold", new Color(1f, 0.6f, 0.05f));
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            PrimitiveKit.Sphere("Garland", t, new Vector3(Mathf.Cos(a) * 1.5f, 0.95f, Mathf.Sin(a) * 1.5f), Vector3.one * 0.28f, marigold);
        }
        PrimitiveKit.Label("ศาลพระพรหม", t, new Vector3(0f, 0.75f, -3.25f), 0.03f, new Color(0.6f, 0.4f, 0.05f));
    }

    private static void Wall(Transform parent, Vector3 local, Vector3 size)
    {
        var go = new GameObject("Blocker");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.AddComponent<BoxCollider>().size = size;
    }

    // ── ท่าอากาศยานสุวรรณภูมิ ──────────────────────────────────────────────────────
    private static void Airport(Transform root, Vector3 at)
    {
        var t = PrimitiveKit.Group("Airport_สุวรรณภูมิ", root, at).transform;
        const float w = 50f, d = 18f, h = 9f;
        float front = d / 2f; // the front faces north (+Z), toward the avenue
        var steel = PrimitiveKit.Mat("TrussWhite", new Color(0.92f, 0.93f, 0.95f), 0.5f, 0.4f);
        var glass = PrimitiveKit.Mat("TerminalGlass", new Color(0.45f, 0.6f, 0.68f), 0.92f, 0.5f);
        var floor = PrimitiveKit.Mat("MarbleFloor", new Color(0.88f, 0.87f, 0.84f), 0.5f);
        var pave = PrimitiveKit.Mat("Pavement", new Color(0.72f, 0.7f, 0.66f));

        PrimitiveKit.Box("Forecourt", t, new Vector3(-2.5f, 0.01f, front + 3.5f), new Vector3(w + 5f, 0.012f, 7f), pave, collider: false);
        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.06f, d), floor, collider: false);
        // Glass walls; the front has a 6 m entrance in the middle
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("FrontGlass", t, new Vector3(s * (w / 4f + 1.5f), h / 2f, front), new Vector3(w / 2f - 3f, h, 0.15f), glass);
        PrimitiveKit.Box("EntranceHead", t, new Vector3(0f, h - 1.5f, front), new Vector3(6f, 3f, 0.15f), glass, collider: false);
        PrimitiveKit.Box("BackGlass", t, new Vector3(0f, h / 2f, -front), new Vector3(w, h, 0.15f), glass);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("SideGlass", t, new Vector3(s * w / 2f, h / 2f, 0f), new Vector3(0.15f, h, d), glass);
        for (float x = -w / 2f; x <= w / 2f + 0.1f; x += 5f) // mullions
            PrimitiveKit.Box("Mullion", t, new Vector3(x, h / 2f, front + 0.1f), new Vector3(0.18f, h, 0.18f), steel, collider: false);

        // The long roof reaching out over the forecourt, ribbed with arched trusses, on tall masts
        const float roofD = 30f;
        float roofZ = front + 6f - roofD / 2f;
        PrimitiveKit.Box("Roof", t, new Vector3(0f, h + 0.5f, roofZ), new Vector3(w + 12f, 0.5f, roofD), steel, collider: false);
        for (float x = -w / 2f - 4f; x <= w / 2f + 4.1f; x += 8f)
        {
            const int seg = 10;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg, a1 = (i + 1) / (float)seg;
                float z0 = roofZ + roofD / 2f - a0 * roofD, z1 = roofZ + roofD / 2f - a1 * roofD;
                float y0 = h + 0.8f + Mathf.Sin(a0 * Mathf.PI) * 3f, y1 = h + 0.8f + Mathf.Sin(a1 * Mathf.PI) * 3f;
                var mid = new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f);
                float len = Mathf.Sqrt((z1 - z0) * (z1 - z0) + (y1 - y0) * (y1 - y0));
                float pitch = Mathf.Atan2(y1 - y0, z0 - z1) * Mathf.Rad2Deg;
                PrimitiveKit.Box("Truss", t, mid, new Vector3(0.4f, 0.5f, len + 0.1f), steel, new Vector3(pitch, 0f, 0f), collider: false);
            }
        }
        for (float x = -w / 2f + 2f; x <= w / 2f - 1.9f; x += 15.33f)
            PrimitiveKit.Cylinder("Mast", t, new Vector3(x, h / 2f, front + 5f), new Vector3(0.6f, h / 2f, 0.6f), steel);
        PrimitiveKit.Label("ท่าอากาศยานสุวรรณภูมิ", t, new Vector3(0f, h + 1.4f, roofZ + roofD / 2f + 0.05f), 0.14f, new Color(0.15f, 0.25f, 0.5f), 180f);

        // Check-in hall: counters, a departure board and the ticket counter for flights
        var counter = PrimitiveKit.Mat("CounterBlue", new Color(0.2f, 0.32f, 0.55f), 0.5f);
        foreach (float x in new[] { -15f, 15f })
        {
            PrimitiveKit.Box("CheckIn", t, new Vector3(x, 0.55f, -2f), new Vector3(10f, 1.1f, 1f), counter);
            PrimitiveKit.Box("CheckInSign", t, new Vector3(x, 3.2f, -2.6f), new Vector3(8f, 0.8f, 0.1f), PrimitiveKit.Glow("StationScreen", new Color(0.1f, 0.35f, 0.6f), 0.6f, 0.5f), collider: false);
            PrimitiveKit.Label(x < 0 ? "เช็คอิน A · ในประเทศ" : "เช็คอิน B · ในประเทศ", t, new Vector3(x, 3.2f, -2.5f), 0.04f, Color.white, 180f);
        }
        PrimitiveKit.Box("TicketCounter", t, new Vector3(0f, 0.55f, 0f), new Vector3(6f, 1.1f, 1.2f), PrimitiveKit.Mat("TempleGold", new Color(0.95f, 0.75f, 0.2f), 0.75f, 0.8f));
        PrimitiveKit.Box("Board", t, new Vector3(0f, 4.6f, -1f), new Vector3(9f, 2.6f, 0.12f), PrimitiveKit.Glow("DepartureBoard", new Color(0.05f, 0.07f, 0.12f), 0.3f, 0.6f), collider: false);
        PrimitiveKit.Label("ขาออก · เที่ยวบินในประเทศ\nเชียงใหม่  ขอนแก่น  อุบลฯ  บุรีรัมย์\nหาดใหญ่  นครศรีฯ  อู่ตะเภา (พัทยา)", t,
            new Vector3(0f, 4.6f, -0.92f), 0.04f, new Color(1f, 0.85f, 0.3f), 180f);
        PrimitiveKit.Label("ขายตั๋วเครื่องบิน", t, new Vector3(0f, 1.6f, 0.65f), 0.035f, new Color(0.4f, 0.2f, 0.05f), 180f);
        var zone = new GameObject("FlightZone_ตั๋วเครื่องบิน");
        zone.transform.SetParent(t, false);
        zone.transform.localPosition = new Vector3(0f, 1f, 1.8f);
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 2f, 2.2f);
        zone.AddComponent<TravelStop>().Configure("สนามบินสุวรรณภูมิ · เที่ยวบินในประเทศ", longDistance: TravelMode.Air);
        // Arrivals come out of the entrance onto the forecourt
        PrimitiveKit.Group("SpawnPoint_Air", t, new Vector3(0f, 0f, front + 2.5f), 0f).AddComponent<SpawnPoint>().Configure(TravelMode.Air);

        // Airside: the apron behind a fence, a jet at the gate with its air bridge, the control tower
        var apronMat = PrimitiveKit.Mat("ApronConcrete", new Color(0.62f, 0.62f, 0.6f));
        PrimitiveKit.Box("Apron", t, new Vector3(5f, 0.012f, -front - 18f), new Vector3(80f, 0.012f, 34f), apronMat, collider: false);
        for (float x = -42f; x <= 47.1f; x += 2.5f)
            PrimitiveKit.Cylinder("FencePost", t, new Vector3(x, 1.2f, -front - 1.2f), new Vector3(0.08f, 1.2f, 0.08f), steel, collider: false);
        var chain = PrimitiveKit.Mat("ChainLink", new Color(0.55f, 0.57f, 0.6f), 0.4f, 0.6f);
        PrimitiveKit.Box("Fence", t, new Vector3(2.5f, 1.2f, -front - 1.2f), new Vector3(89f, 2.4f, 0.05f), chain);
        foreach (float x in new[] { -42f, 47f }) // round the ends of the apron
            PrimitiveKit.Box("Fence", t, new Vector3(x, 1.2f, -front - 18.6f), new Vector3(0.05f, 2.4f, 34.8f), chain);
        Jet(t, new Vector3(5f, 0f, -front - 17f));
        PrimitiveKit.Box("AirBridge", t, new Vector3(-4f, 4.4f, -front - 7.4f), new Vector3(3f, 2.8f, 12.8f), steel, collider: false);
        PrimitiveKit.Cylinder("AirBridgeLeg", t, new Vector3(-4f, 1.5f, -front - 12f), new Vector3(0.5f, 1.5f, 0.5f), steel, collider: false);
        var tower = PrimitiveKit.Group("ControlTower", t, new Vector3(32f, 0f, -front - 26f)).transform;
        PrimitiveKit.Cylinder("Shaft", tower, new Vector3(0f, 15f, 0f), new Vector3(3.2f, 15f, 3.2f), steel);
        PrimitiveKit.Cylinder("Cab", tower, new Vector3(0f, 31.5f, 0f), new Vector3(8f, 1.5f, 8f), glass, collider: false);
        PrimitiveKit.Cylinder("CabRoof", tower, new Vector3(0f, 33.2f, 0f), new Vector3(9f, 0.25f, 9f), steel, collider: false);
        PrimitiveKit.Sphere("Beacon", tower, new Vector3(0f, 34f, 0f), Vector3.one * 0.6f, PrimitiveKit.Glow("BeaconRed", new Color(1f, 0.15f, 0.1f), 2f));
    }

    // A twin-engine airliner in a Thai livery (white, royal purple and gold), nose to the east
    private static void Jet(Transform t, Vector3 at)
    {
        var j = PrimitiveKit.Group("Jet_เครื่องบิน", t, at).transform;
        var white = PrimitiveKit.Mat("JetWhite", new Color(0.95f, 0.95f, 0.96f), 0.7f, 0.2f);
        var purple = PrimitiveKit.Mat("JetPurple", new Color(0.38f, 0.14f, 0.5f), 0.6f, 0.2f);
        var gold = Gold;
        var grey = PrimitiveKit.Mat("JetGrey", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.5f);
        const float len = 34f, r = 2.2f, y = 3.6f;
        PrimitiveKit.Cylinder("Fuselage", j, new Vector3(0f, y, 0f), new Vector3(r * 2f, len / 2f, r * 2f), white, new Vector3(0f, 0f, 90f));
        PrimitiveKit.Sphere("Nose", j, new Vector3(len / 2f, y, 0f), new Vector3(5f, r * 2f, r * 2f), white);
        PrimitiveKit.Cylinder("TailCone", j, new Vector3(-len / 2f - 2.4f, y + 0.6f, 0f), new Vector3(r * 1.3f, 2.6f, r * 1.3f), white, new Vector3(0f, 0f, 75f), collider: false);
        PrimitiveKit.Box("Cheatline", j, new Vector3(0f, y - 0.6f, 0f), new Vector3(len, 0.35f, r * 2f + 0.06f), purple, collider: false);
        PrimitiveKit.Box("GoldLine", j, new Vector3(0f, y - 0.25f, 0f), new Vector3(len, 0.12f, r * 2f + 0.07f), gold, collider: false);
        PrimitiveKit.Box("Windows", j, new Vector3(1f, y + 0.55f, 0f), new Vector3(len - 6f, 0.25f, r * 2f + 0.05f), PrimitiveKit.Mat("JetWindow", new Color(0.1f, 0.12f, 0.16f), 0.9f), collider: false);
        PrimitiveKit.Box("Cockpit", j, new Vector3(len / 2f + 1.2f, y + 0.8f, 0f), new Vector3(1.2f, 0.4f, 2.2f), PrimitiveKit.Mat("JetWindow", new Color(0.1f, 0.12f, 0.16f), 0.9f), collider: false);
        // Wings swept back, engines underneath; tailplane and a purple fin with a gold flourish
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Wing", j, new Vector3(-1.5f, y - 1f, s * 8.5f), new Vector3(5f, 0.45f, 15f), white, new Vector3(0f, s * 25f, 0f), collider: false);
            PrimitiveKit.Cylinder("Engine", j, new Vector3(0.8f, y - 2.1f, s * 6f), new Vector3(2f, 1.8f, 2f), grey, new Vector3(0f, 0f, 90f), collider: false);
            PrimitiveKit.Box("Tailplane", j, new Vector3(-len / 2f - 1.5f, y + 1f, s * 3.6f), new Vector3(2.8f, 0.3f, 6f), white, new Vector3(0f, s * 25f, 0f), collider: false);
            PrimitiveKit.Cylinder("MainGear", j, new Vector3(-2f, 0.6f, s * 2.5f), new Vector3(1.2f, 0.3f, 1.2f), grey, new Vector3(90f, 0f, 0f), collider: false);
        }
        PrimitiveKit.Box("Fin", j, new Vector3(-len / 2f - 1.2f, y + 4.6f, 0f), new Vector3(5f, 7f, 0.35f), purple, new Vector3(0f, 0f, -20f), collider: false);
        PrimitiveKit.Box("FinGold", j, new Vector3(-len / 2f - 1.6f, y + 5.4f, 0f), new Vector3(1.4f, 3f, 0.4f), gold, new Vector3(0f, 0f, -35f), collider: false);
        PrimitiveKit.Cylinder("NoseGear", j, new Vector3(len / 2f - 2f, 0.5f, 0f), new Vector3(0.9f, 0.25f, 0.9f), grey, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("GearLeg", j, new Vector3(len / 2f - 2f, 1.6f, 0f), new Vector3(0.25f, 1.2f, 0.25f), grey, collider: false);
        PrimitiveKit.Label("ไทยไลฟ์แอร์", j, new Vector3(2f, y + 1.2f, r + 0.05f), 0.08f, new Color(0.38f, 0.14f, 0.5f), 180f);
        var col = j.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, y, 0f);
        col.size = new Vector3(len + 6f, 5f, 4.6f);
    }
}
