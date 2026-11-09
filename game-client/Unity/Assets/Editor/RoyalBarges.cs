using System.Collections.Generic;
using UnityEngine;

// ขบวนเรือพระราชพิธี (the Royal Barge Procession) on the Chao Phraya in front of วัดอรุณ, with the
// crowds that come out to watch it. Three columns sail down the river together (BargeProcession):
//   middle  the royal barges: เรือนารายณ์ทรงสุบรรณ (Garuda carrying Vishnu at the bow),
//           เรือสุพรรณหงส์ (the golden swan, a gold บุษบก pavilion), เรืออนันตนาคราช (seven-headed naga)
//           and เรืออเนกชาติภุชงค์ (red, little nagas along the hull)
//   either side  the escorts: เรือกระบี่ปราบเมืองมาร (monkey warrior), เรืออสุรวายุภักษ์ (giant bird,
//           a cannon in the bow) and lines of black-and-gold เรือดั้ง
// Every barge: a lacquered hull painted with ลายไทย, rising to its figurehead and tail, oarsmen in red
// two to a row with gold oars (they row in time), tiered umbrellas (ฉัตร), a steersman, a flag officer
// at the bow and the boat's chanter. Each barge's parts are merged by material into a few meshes.
// On the banks: people packed two deep along both promenades, seated stands under white tents beside
// วัดอรุณ, a red-and-gold royal landing on its pontoon, yellow and purple flags; across in the city a
// festival plaza with a big live screen, rows of chairs, a food market, street carts and tuk-tuks.
public static class RoyalBarges
{
    // Columns across the river (local z of the river group; the city bank is -z, วัดอรุณ +z)
    private const float RoyalLane = 1.8f, OuterLane = 6.25f, InnerLane = -3.25f; // oars clear of each other and the piers

    private static Material gold, red, black, skin, white, crewRed, hatGold, pink, navy, glassGreen;

    public static void Build(Transform river, float riverHalf, float length)
    {
        gold = PrimitiveKit.Mat("BargeGold", new Color(0.93f, 0.72f, 0.26f), 0.82f, 0.85f);
        red = PrimitiveKit.Mat("BargeRed", new Color(0.7f, 0.08f, 0.06f), 0.55f);
        black = PrimitiveKit.Mat("BargeBlack", new Color(0.07f, 0.06f, 0.06f), 0.75f);
        skin = PrimitiveKit.Mat("SkinTan", new Color(0.82f, 0.62f, 0.45f));
        white = PrimitiveKit.Mat("UniformWhite", new Color(0.95f, 0.95f, 0.93f), 0.3f);
        crewRed = PrimitiveKit.Mat("OarsmanRed", new Color(0.82f, 0.12f, 0.1f), 0.25f);
        hatGold = PrimitiveKit.Mat("OarsmanHat", new Color(0.86f, 0.18f, 0.12f), 0.4f);
        pink = PrimitiveKit.Mat("OarsmanPink", new Color(0.86f, 0.5f, 0.55f), 0.25f);
        navy = PrimitiveKit.Mat("NarayanaBlue", new Color(0.12f, 0.14f, 0.28f), 0.6f);
        glassGreen = PrimitiveKit.Mat("BargeGlassGreen", new Color(0.12f, 0.5f, 0.3f), 0.85f, 0.3f);

        var procession = PrimitiveKit.Group("RoyalBargeProcession_ขบวนเรือพระราชพิธี", river, Vector3.zero).transform;
        var roots = new List<Transform>();
        var lengths = new List<float>();
        void Add(Transform b, float len) { roots.Add(b); lengths.Add(len); }

        // the middle column, led by Narai, the swan in the place of honour
        Add(NaraiSongSuban(procession, new Vector3(-6f, 0f, RoyalLane)), 26f);
        Add(Suphannahong(procession, new Vector3(-40f, 0f, RoyalLane)), 30f);
        Add(Anantanakkharat(procession, new Vector3(-74f, 0f, RoyalLane)), 27f);
        Add(Anekchat(procession, new Vector3(-106f, 0f, RoyalLane)), 24f);
        // the escort columns
        Add(AnimalBarge(procession, new Vector3(4f, 0f, OuterLane), monkey: true), 18f);
        Add(AnimalBarge(procession, new Vector3(4f, 0f, InnerLane), monkey: false), 18f);
        for (int i = 0; i < 5; i++)
        {
            Add(Dang(procession, new Vector3(-17f - i * 20f, 0f, OuterLane), i), 16f);
            Add(Dang(procession, new Vector3(-17f - i * 20f, 0f, InnerLane), i + 5), 16f);
        }
        procession.gameObject.AddComponent<BargeProcession>().Configure(roots.ToArray(), lengths.ToArray());

        Spectators(river, riverHalf, length);
        RoyalLanding(river, new Vector3(-41f, 0f, riverHalf - 2.6f)); // to the side, clear of the view
        BankFlags(river, riverHalf, length);
    }

    // ── Hulls ────────────────────────────────────────────────────────────────────
    // A long narrow lacquered hull, bow to +z, the waterline at y 0: deep in the middle, rising thin to a
    // high stem and stern. Two sub-meshes: the painted sides (UV along the length, up the side) and the deck.
    private static Mesh HullMesh(string key, float length, float beam, float depth, float bowRise, float sternRise)
    {
        const int sections = 44;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var sides = new List<int>();
        var deck = new List<int>();
        float[] ring = { -1f, -0.96f, -0.62f, 0f, 0.62f, 0.96f, 1f };   // across, as a fraction of half-beam
        float[] drop = { 0f, 0.5f, 0.92f, 1f, 0.92f, 0.5f, 0f };          // down, as a fraction of the side's height
        for (int i = 0; i <= sections; i++)
        {
            float u = i / (float)sections, z = (u - 0.5f) * length, s = Mathf.Sin(Mathf.PI * u);
            float w = Mathf.Max(0.05f, beam * 0.5f * Mathf.Pow(s, 0.55f));
            float top = Top(u, depth, bowRise, sternRise);
            float bottom = Mathf.Lerp(top - 0.22f, -depth * 0.45f, Mathf.Pow(s, 0.35f));
            for (int j = 0; j < ring.Length; j++)
            {
                float y = Mathf.Lerp(top, bottom, drop[j]);
                v.Add(new Vector3(ring[j] * w, y, z));
                uv.Add(new Vector2(z * 0.5f, (y - bottom) / Mathf.Max(0.01f, top - bottom)));
            }
        }
        int n = ring.Length;
        for (int i = 0; i < sections; i++)
            for (int j = 0; j < n - 1; j++)
            {
                int a = i * n + j, b = a + 1, c = a + n, d = c + 1;
                sides.AddRange(new[] { a, b, c, b, d, c });
            }
        int deckStart = v.Count;
        for (int i = 0; i <= sections; i++)
        {
            float u = i / (float)sections, z = (u - 0.5f) * length, s = Mathf.Sin(Mathf.PI * u);
            float w = Mathf.Max(0.05f, beam * 0.5f * Mathf.Pow(s, 0.55f)) * 0.97f;
            float top = Top(u, depth, bowRise, sternRise) - 0.03f;
            v.Add(new Vector3(-w, top, z)); uv.Add(new Vector2(0f, z));
            v.Add(new Vector3(w, top, z)); uv.Add(new Vector2(1f, z));
        }
        for (int i = 0; i < sections; i++)
        {
            int l0 = deckStart + i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
            deck.AddRange(new[] { l0, l1, r0, r0, l1, r1 });
        }
        var mesh = new Mesh { subMeshCount = 2 };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(sides, 0);
        mesh.SetTriangles(deck, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return MeshKit.SaveCombined("Barges/" + key, mesh);
    }

    private static float Top(float u, float depth, float bowRise, float sternRise) =>
        depth + bowRise * Mathf.Pow(Mathf.Max(0f, (u - 0.8f) / 0.2f), 2.2f) + sternRise * Mathf.Pow(Mathf.Max(0f, (0.2f - u) / 0.2f), 2.2f);

    private static float HalfBeam(float z, float length, float beam) =>
        Mathf.Max(0.05f, beam * 0.5f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(z / length + 0.5f)), 0.55f));

    // ลายไทย painted on the hull: little four-petal flowers and curling vines in `ink` on `ground`,
    // a plain band along the gunwale and the waterline
    private static Material Pattern(string name, Color ground, Color ink, float smooth = 0.7f, float metal = 0.3f)
    {
        var mat = PrimitiveKit.Mat(name, Color.white, smooth, metal);
        string path = $"Assets/Art/Materials/{name}Tex.asset";
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            const int n = 128;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = name + "Tex", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = ground;
            void Dot(float cx, float cy, float r, Color c)
            {
                for (int y = Mathf.FloorToInt(cy - r); y <= cy + r; y++)
                    for (int x = Mathf.FloorToInt(cx - r); x <= cx + r; x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                            px[((y % n + n) % n) * n + (x % n + n) % n] = c;
            }
            for (int gy = 0; gy < 2; gy++)
                for (int gx = 0; gx < 2; gx++)
                {
                    float cx = gx * 64f + 32f, cy = gy * 40f + 44f;
                    for (int k = 0; k < 4; k++) // petals
                    {
                        float a = k * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                        Dot(cx + Mathf.Cos(a) * 7f, cy + Mathf.Sin(a) * 7f, 5f, ink);
                    }
                    Dot(cx, cy, 3f, ground);
                    Dot(cx, cy, 2f, ink);
                    for (int s = 0; s < 40; s++) // a vine curling out to the next flower
                    {
                        float k = s / 40f, ang = k * Mathf.PI * 3f;
                        Dot(cx + 12f + k * 40f, cy + Mathf.Sin(ang) * 9f * (1f - k * 0.5f), 1.3f, ink);
                    }
                }
            for (int x = 0; x < n; x++) // bands: along the bottom (waterline) and top (gunwale)
            {
                for (int y = 0; y < 8; y++) px[y * n + x] = Color.Lerp(ink, ground, 0.25f);
                for (int y = 116; y < 128; y++) px[y * n + x] = ink;
                for (int y = 108; y < 111; y++) px[y * n + x] = ink;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            UnityEditor.AssetDatabase.CreateAsset(tex, path);
        }
        mat.SetTexture("_BaseMap", tex);
        UnityEditor.EditorUtility.SetDirty(mat);
        return mat;
    }

    // ── Small shared shapes (low poly: hundreds of people) ──────────────────────
    private static Mesh cube, ball, cone, rod;
    internal static Mesh Cube
    {
        get
        {
            if (cube != null) return cube;
            var v = new List<Vector3>();
            var t = new List<int>();
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var f in n)
            {
                Vector3 a = new Vector3(f.y, f.z, f.x), b = Vector3.Cross(f, a);
                int s = v.Count;
                v.Add((f - a - b) * 0.5f); v.Add((f - a + b) * 0.5f); v.Add((f + a + b) * 0.5f); v.Add((f + a - b) * 0.5f);
                t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            // make every face front-facing outward whatever the corner order came out as
            var tri = m.triangles;
            var verts = m.vertices;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 p0 = verts[tri[i]], p1 = verts[tri[i + 1]], p2 = verts[tri[i + 2]];
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2) / 3f) < 0f) { (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]); }
            }
            m.triangles = tri;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return cube = MeshKit.SaveCombined("Barges/LowCube", m);
        }
    }
    internal static Mesh Ball => ball != null ? ball : ball = MeshKit.Lathe("Barges/LowBall", new[]
        { new Vector2(0f, -0.5f), new Vector2(0.3f, -0.4f), new Vector2(0.48f, -0.12f), new Vector2(0.48f, 0.12f), new Vector2(0.3f, 0.4f), new Vector2(0f, 0.5f) }, 8);
    internal static Mesh Cone => cone != null ? cone : cone = MeshKit.Lathe("Barges/LowCone", new[]
        { new Vector2(0.5f, 0f), new Vector2(0.34f, 0.32f), new Vector2(0.14f, 0.72f), new Vector2(0f, 1f) }, 7);
    // a rounded torso (drop-in for Cube: centred, 1 x 1 x 1): narrow waist, broad rounded shoulders
    private static Mesh torso;
    internal static Mesh Torso => torso != null ? torso : torso = MeshKit.Lathe("Barges/LowTorso", new[]
        { new Vector2(0f, -0.5f), new Vector2(0.4f, -0.5f), new Vector2(0.43f, -0.15f), new Vector2(0.5f, 0.25f), new Vector2(0.46f, 0.42f), new Vector2(0.24f, 0.5f), new Vector2(0f, 0.5f) }, 8);
    internal static Mesh Rod => rod != null ? rod : rod = MeshKit.Lathe("Barges/LowRod", new[] { new Vector2(0.5f, 0f), new Vector2(0.5f, 1f) }, 6);

    internal static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 at, Vector3 scale, Vector3 euler = default) =>
        MeshKit.Place(name, parent, mesh, mat, at, euler, scale);

    // A rod from a to b (radius r)
    internal static void Stick(string name, Transform parent, Material mat, Vector3 a, Vector3 b, float r)
    {
        var dir = b - a;
        var go = Part(name, parent, Rod, mat, a, new Vector3(r * 2f, dir.magnitude, r * 2f));
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
    }

    // ── People ───────────────────────────────────────────────────────────────────
    // An oarsman sitting on the deck facing the bow, his oar on his side (in `oars`, which rows in time)
    private static void Oarsman(Transform crew, Transform oars, float x, float deckY, float z, float side, Material uniform, Material hat, Material oar, float gunwaleX, float reach)
    {
        // kneeling on the deck: folded legs, the body leaning into the stroke, both hands on the oar
        Part("Legs", crew, Cube, uniform, new Vector3(x, deckY + 0.1f, z - 0.05f), new Vector3(0.3f, 0.2f, 0.5f));
        Part("Body", crew, Torso, uniform, new Vector3(x, deckY + 0.42f, z + 0.03f), new Vector3(0.36f, 0.5f, 0.26f), new Vector3(14f, 0f, 0f));
        Part("Sash", crew, Torso, gold, new Vector3(x, deckY + 0.24f, z + 0.0f), new Vector3(0.37f, 0.08f, 0.27f), new Vector3(14f, 0f, 0f));
        Part("Head", crew, Ball, skin, new Vector3(x, deckY + 0.8f, z + 0.12f), Vector3.one * 0.22f);
        // หมวกทรงประพาส: a tall pointed cap, a gold band round the brim
        Part("Hat", crew, Cone, hat, new Vector3(x, deckY + 0.85f, z + 0.11f), new Vector3(0.24f, 0.3f, 0.24f));
        Part("HatBand", crew, Rod, gold, new Vector3(x, deckY + 0.84f, z + 0.11f), new Vector3(0.26f, 0.04f, 0.26f));
        // the oar: grip by his hands, over the gunwale, blade down in the water
        var grip = new Vector3(x + side * 0.15f, deckY + 0.55f, z + 0.3f);
        foreach (float s in new[] { -1f, 1f })
            Stick("Arm", crew, uniform, new Vector3(x + s * 0.16f, deckY + 0.62f, z + 0.08f), grip + new Vector3(0f, -0.02f, s * 0.05f), 0.045f);
        var blade = new Vector3(side * (gunwaleX + reach), 0.05f, z + 0.2f);
        Stick("OarShaft", oars, oar, grip, blade, 0.03f);
        var dir = (blade - grip).normalized;
        var b = Part("OarBlade", oars, Cube, oar, blade - dir * 0.15f, new Vector3(0.05f, 0.55f, 0.16f));
        b.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
    }

    // Someone standing on a barge (the steersman, an officer, the chanter)
    private static void Standing(Transform parent, Vector3 at, Material uniform, Material hat, bool staff = false)
    {
        foreach (float s in new[] { -1f, 1f })
        {
            Part("Leg", parent, Rod, uniform, at + new Vector3(s * 0.1f, 0f, 0f), new Vector3(0.15f, 0.9f, 0.15f));
            Stick("Arm", parent, uniform, at + new Vector3(s * 0.24f, 1.36f, 0f), at + new Vector3(s * 0.28f, 0.86f, 0.04f), 0.055f);
        }
        Part("Body", parent, Torso, uniform, at + new Vector3(0f, 1.15f, 0f), new Vector3(0.44f, 0.58f, 0.28f));
        Part("Belt", parent, Torso, gold, at + new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 0.07f, 0.26f));
        Part("Head", parent, Ball, skin, at + new Vector3(0f, 1.6f, 0f), Vector3.one * 0.26f);
        Part("Hat", parent, Cone, hat, at + new Vector3(0f, 1.69f, 0f), new Vector3(0.3f, 0.36f, 0.3f));
        if (staff) Stick("Staff", parent, gold, at + new Vector3(0.28f, 0f, 0.1f), at + new Vector3(0.28f, 2.1f, 0.1f), 0.03f);
    }

    // ฉัตร: tiers of embroidered cloth on a gold pole, narrowing upward to a gold finial
    private static void TieredUmbrella(Transform parent, Vector3 at, float height, int tiers, Material cloth)
    {
        Stick("Pole", parent, gold, at, at + Vector3.up * (height + 0.8f), 0.035f);
        for (int i = 0; i < tiers; i++)
        {
            float k = i / (float)(tiers - 1), r = Mathf.Lerp(0.55f, 0.24f, k), y = height - (tiers - 1 - i) * 0.32f;
            Part("Tier", parent, Rod, i % 2 == 0 ? cloth : gold, at + new Vector3(0f, y - 0.22f, 0f), new Vector3(r * 2f, 0.22f, r * 2f));
            Part("TierRim", parent, Rod, gold, at + new Vector3(0f, y - 0.24f, 0f), new Vector3(r * 2f + 0.04f, 0.04f, r * 2f + 0.04f));
        }
        Part("Finial", parent, Cone, gold, at + Vector3.up * (height + 0.05f), new Vector3(0.12f, 0.6f, 0.12f));
    }

    // ── Barges ───────────────────────────────────────────────────────────────────
    // The common body: hull, crew rows (skipping the pavilion), oars, steersman, flag officer and chanter.
    // Returns the barge root (rotated so its bow points down the river, +x).
    private static Transform Barge(Transform procession, string name, Vector3 at, float length, float beam, float depth,
        float bowRise, float sternRise, Material sides, Material deckMat, Material uniform, Material hat, Material oar,
        float pavilionFrom, float pavilionTo, out Transform body, out float deckY)
    {
        var root = PrimitiveKit.Group("Barge_" + name, procession, at, 90f).transform;
        body = PrimitiveKit.Group("Body", root, Vector3.zero).transform;
        var crew = PrimitiveKit.Group("Crew", root, Vector3.zero).transform;
        var oarsL = PrimitiveKit.Group("OarsL", root, Vector3.zero).transform;
        var oarsR = PrimitiveKit.Group("OarsR", root, Vector3.zero).transform;
        var hull = MeshKit.Place("Hull", body, HullMesh(name, length, beam, depth, bowRise, sternRise), sides, Vector3.zero);
        hull.GetComponent<MeshRenderer>().sharedMaterials = new[] { sides, deckMat };
        deckY = depth - 0.03f;
        // gilded gunwales: a gold moulding along both sides, from stern to stem
        const int railPoints = 36;
        foreach (float s in new[] { -1f, 1f })
        {
            var rail = new Vector3[railPoints];
            for (int i = 0; i < railPoints; i++)
            {
                float u = Mathf.Lerp(0.03f, 0.97f, i / (railPoints - 1f)), z = (u - 0.5f) * length;
                rail[i] = new Vector3(s * HalfBeam(z, length, beam) * 0.99f, Top(u, depth, bowRise, sternRise) + 0.02f, z);
            }
            MeshKit.Place("Gunwale", body, MeshKit.Sweep($"Barges/{name}Rail{(s < 0f ? "L" : "R")}", rail, MeshKit.Taper(railPoints, 0.07f, 0.07f), 6), gold, Vector3.zero);
        }

        float from = -length * 0.5f + 3.2f, to = length * 0.5f - 3.4f;
        for (float z = from; z <= to; z += 0.82f)
        {
            if (z > pavilionFrom - 0.4f && z < pavilionTo + 0.4f) continue;
            float w = HalfBeam(z, length, beam);
            float reach = beam > 2f ? 1.1f : 1f;
            Oarsman(crew, oarsL, -w * 0.5f, deckY, z, -1f, uniform, hat, oar, w, reach);
            Oarsman(crew, oarsR, w * 0.5f, deckY, z, 1f, uniform, hat, oar, w, reach);
        }
        Standing(crew, new Vector3(0f, deckY, -length * 0.5f + 2.4f), uniform, hat);               // steersman
        Stick("Tiller", crew, black, new Vector3(0.2f, deckY + 1f, -length * 0.5f + 2.2f), new Vector3(0.6f, -0.4f, -length * 0.5f + 0.4f), 0.05f);
        Standing(crew, new Vector3(0f, deckY, length * 0.5f - 2.8f), uniform, hat);                // flag officer at the bow
        Stick("FlagStaff", crew, gold, new Vector3(0.3f, deckY + 0.3f, length * 0.5f - 2.7f), new Vector3(0.3f, deckY + 2.6f, length * 0.5f - 2.7f), 0.03f);
        Part("Flag", crew, Cube, red, new Vector3(0.3f, deckY + 2.3f, length * 0.5f - 3.1f), new Vector3(0.03f, 0.5f, 0.7f));
        Standing(crew, new Vector3(0f, deckY, pavilionFrom - 1f), white, gold, staff: true);       // the chanter (พันพาย)
        return root;
    }

    // Make the figure just built (body's children from `from` on) bigger, about `pivot` on the stem, so
    // the sacred beasts at bow and stern stand out over the crews as they do on the real barges
    private static void Enlarge(Transform body, int from, Vector3 pivot, float scale)
    {
        var group = new GameObject("Figure").transform;
        group.SetParent(body, false);
        group.localPosition = pivot;
        group.localScale = Vector3.one * scale;
        var kids = new List<Transform>();
        for (int i = from; i < body.childCount; i++)
            if (body.GetChild(i) != group) kids.Add(body.GetChild(i));
        foreach (var k in kids)
        {
            Vector3 p = k.localPosition, s = k.localScale;
            var r = k.localRotation;
            k.SetParent(group, false);
            k.localPosition = p - pivot;
            k.localRotation = r;
            k.localScale = s;
        }
    }

    private static void Finish(Transform root, string name)
    {
        foreach (var part in new[] { "Body", "Crew", "OarsL", "OarsR" })
        {
            var t = root.Find(part);
            if (t != null) MeshKit.MergeByMaterial(t, $"Barges/{name}_{part}");
        }
    }

    // เรือพระที่นั่งสุพรรณหงส์: the golden swan, its neck rising high at the bow with a crimson tassel
    // from the beak and a garland round the neck, its tail curling up at the stern; the gold บุษบก
    // pavilion amidships, ฉัตร fore and aft
    // which way the sculpted swan and Narai face as made (turned so they look ahead, +Z)
    private const float SwanYaw = 0f, NaraiYaw = 0f;

    private static Transform Suphannahong(Transform p, Vector3 at)
    {
        const float L = 30f, B = 2.5f, D = 0.9f;
        var hullMat = Pattern("BargeLaiGold", new Color(0.86f, 0.64f, 0.22f), new Color(0.55f, 0.32f, 0.08f), 0.8f, 0.7f);
        var root = Barge(p, "Suphannahong", at, L, B, D, 1.2f, 1.6f, hullMat, red, crewRed, hatGold, gold, -4f, 2f, out var body, out float deck);
        float bowTop = D + 1.2f, sternTop = D + 1.6f;

        int mark = body.childCount;
        // the sculpted figure (AI-made) when present, else the built one
        if (AiModels.Has("Swan")) AiModels.Place("Swan", body, new Vector3(0f, bowTop - 0.5f, L * 0.5f - 0.9f), 7.2f, SwanYaw);
        else
        {
            // the neck: up from the stem in an S, the head looking ahead
            var neck = MeshKit.Curve(new Vector3(0f, bowTop - 0.3f, L * 0.5f - 0.6f), new Vector3(0f, bowTop + 3.2f, L * 0.5f + 1.8f), new Vector3(0f, bowTop + 4.6f, L * 0.5f + 0.3f), 14);
            MeshKit.Place("SwanNeck", body, MeshKit.Sweep("Barges/SwanNeck", neck, MeshKit.Taper(14, 0.62f, 0.3f), 10), gold, Vector3.zero);
            var head = neck[13] + new Vector3(0f, 0.25f, 0.15f);
            Part("Head", body, Ball, gold, head, new Vector3(0.62f, 0.66f, 0.95f));
            Part("Beak", body, Cone, gold, head + new Vector3(0f, -0.05f, 0.42f), new Vector3(0.3f, 0.8f, 0.3f), new Vector3(90f, 0f, 0f));
            foreach (float s in new[] { -1f, 1f })
                Part("Eye", body, Ball, black, head + new Vector3(s * 0.28f, 0.1f, 0.15f), Vector3.one * 0.1f);
            Part("Crest", body, Cone, gold, head + new Vector3(0f, 0.35f, -0.15f), new Vector3(0.2f, 0.7f, 0.2f), new Vector3(-30f, 0f, 0f));
            // crimson tassel (พู่) from the beak, gold ball on its cord
            var tip = head + new Vector3(0f, -0.15f, 1.1f);
            Stick("TasselCord", body, gold, tip, tip + Vector3.down * 0.9f, 0.02f);
            Part("TasselBall", body, Ball, gold, tip + Vector3.down * 0.95f, Vector3.one * 0.16f);
            Part("Tassel", body, Cone, red, tip + Vector3.down * 1.8f, new Vector3(0.28f, 0.85f, 0.28f));
            // white flower garland round the neck
            var g = neck[5];
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                Part("Garland", body, Ball, white, g + new Vector3(Mathf.Cos(a) * 0.66f, Mathf.Sin(a) * 0.3f - 0.35f, Mathf.Sin(a) * 0.5f), Vector3.one * 0.17f);
            }
            // glass inlay along the neck
            for (int i = 2; i < 12; i += 2)
                foreach (float s in new[] { -1f, 1f })
                    Part("Inlay", body, Ball, glassGreen, neck[i] + new Vector3(s * Mathf.Lerp(0.6f, 0.3f, i / 13f), 0f, 0f), Vector3.one * 0.12f);
            Enlarge(body, mark, new Vector3(0f, bowTop - 0.3f, L * 0.5f - 0.6f), 1.75f);
        }
        mark = body.childCount;
        // the tail curling up at the stern
        var tail = MeshKit.Curve(new Vector3(0f, sternTop - 0.3f, -L * 0.5f + 0.5f), new Vector3(0f, sternTop + 2.6f, -L * 0.5f - 1.4f), new Vector3(0f, sternTop + 3.2f, -L * 0.5f + 0.4f), 12);
        MeshKit.Place("SwanTail", body, MeshKit.Sweep("Barges/SwanTail", tail, MeshKit.Taper(12, 0.4f, 0.06f), 8), gold, Vector3.zero);
        Enlarge(body, mark, new Vector3(0f, sternTop - 0.3f, -L * 0.5f + 0.5f), 1.45f);

        Busabok(body, new Vector3(0f, deck, -1f));
        foreach (float z in new[] { -8.5f, -6.5f, 4.5f, 6.5f })
            TieredUmbrella(body, new Vector3(0f, deck, z), 2.6f, 7, red);
        Finish(root, "Suphannahong");
        return root;
    }

    // บุษบก: the gold pavilion with a tiered spire, crimson curtains tied back, an officer at each corner
    private static void Busabok(Transform body, Vector3 at)
    {
        Part("Platform", body, Cube, gold, at + new Vector3(0f, 0.35f, 0f), new Vector3(2.1f, 0.7f, 3.4f));
        Part("Cushion", body, Cube, red, at + new Vector3(0f, 0.78f, 0f), new Vector3(1.6f, 0.16f, 2.6f));
        foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -1.5f, 1.5f })
            {
                Stick("Column", body, gold, at + new Vector3(x, 0.7f, z), at + new Vector3(x, 3f, z), 0.07f);
                Part("Curtain", body, Cube, red, at + new Vector3(x * 1.02f, 2.2f, z * 0.82f), new Vector3(0.04f, 1.5f, 0.45f));
            }
        float y = 3f;
        for (int i = 0; i < 5; i++) // the stepped roof
        {
            float k = 1f - i * 0.17f;
            Part("Roof", body, Cube, gold, at + new Vector3(0f, y + 0.12f, 0f), new Vector3(2.3f * k, 0.24f, 3.6f * k));
            Part("Eave", body, Cube, red, at + new Vector3(0f, y + 0.27f, 0f), new Vector3(2.1f * k, 0.06f, 3.3f * k));
            y += 0.34f;
        }
        Part("Spire", body, Cone, gold, at + new Vector3(0f, y, 0f), new Vector3(0.8f, 2.4f, 0.8f));
        Part("SpireTip", body, Rod, gold, at + new Vector3(0f, y + 2.3f, 0f), new Vector3(0.05f, 0.6f, 0.05f));
        foreach (float x in new[] { -1.4f, 1.4f })
            foreach (float z in new[] { -2.1f, 2.1f })
                Standing(body, at + new Vector3(x, 0f, z), white, gold);
    }

    // กัญญา: a curved crimson canopy on gold posts, gold curtains at the ends
    private static void Kanya(Transform body, Vector3 at, float len)
    {
        Part("Platform", body, Cube, gold, at + new Vector3(0f, 0.25f, 0f), new Vector3(1.9f, 0.5f, len));
        foreach (float x in new[] { -0.85f, 0.85f })
            foreach (float z in new[] { -len * 0.45f, len * 0.45f })
                Stick("Post", body, gold, at + new Vector3(x, 0.5f, z), at + new Vector3(x, 2.4f, z), 0.06f);
        var roof = MeshKit.Gable("Barges/KanyaRoof", 2.4f, 0.7f, 1f);
        MeshKit.Place("Canopy", body, roof, red, at + new Vector3(0f, 2.35f, -len * 0.5f - 0.2f), Vector3.zero, new Vector3(1f, 1f, len + 0.4f));
        Part("Ridge", body, Cube, gold, at + new Vector3(0f, 3.07f, 0f), new Vector3(0.1f, 0.1f, len + 0.5f));
        foreach (float z in new[] { -len * 0.5f - 0.2f, len * 0.5f + 0.2f })
        {
            Part("GableGold", body, Cube, gold, at + new Vector3(0f, 2.6f, z), new Vector3(2.2f, 0.4f, 0.05f));
            Part("Curtain", body, Cube, gold, at + new Vector3(0f, 1.5f, z * 0.96f), new Vector3(1.7f, 1.6f, 0.04f));
        }
        foreach (float x in new[] { -1.2f, 1.2f })
            Standing(body, at + new Vector3(x, 0f, len * 0.5f + 0.6f), white, gold);
    }

    // เรือพระที่นั่งนารายณ์ทรงสุบรรณ: Vishnu, dark-skinned and four-armed, riding the red-and-gold Garuda
    private static Transform NaraiSongSuban(Transform p, Vector3 at)
    {
        const float L = 26f, B = 2.4f, D = 0.9f;
        var hullMat = Pattern("BargeLaiGoldRed", new Color(0.86f, 0.64f, 0.22f), new Color(0.62f, 0.12f, 0.08f), 0.8f, 0.6f);
        var root = Barge(p, "NaraiSongSuban", at, L, B, D, 1.4f, 1.8f, hullMat, red, crewRed, hatGold, gold, -2.5f, 2.5f, out var body, out float deck);
        float top = D + 1.4f;
        var g = new Vector3(0f, top, L * 0.5f - 0.6f);
        var garudaRed = PrimitiveKit.Mat("GarudaRed", new Color(0.72f, 0.12f, 0.1f), 0.6f, 0.2f);
        int mark = body.childCount;
        // the sculpted figure (AI-made) when present, else the built one
        if (AiModels.Has("Narai")) AiModels.Place("Narai", body, g + new Vector3(0f, -0.3f, -0.2f), 6.4f, NaraiYaw);
        else
        {
            // Garuda: crouching body, wings spread up and out, a fierce head with a gold crown
            Part("GarudaBody", body, Cube, garudaRed, g + new Vector3(0f, 0.9f, 0.2f), new Vector3(1.1f, 1.5f, 0.9f), new Vector3(-10f, 0f, 0f));
            Part("Breastplate", body, Cube, gold, g + new Vector3(0f, 1f, 0.67f), new Vector3(0.8f, 0.9f, 0.08f), new Vector3(-10f, 0f, 0f));
            Part("GarudaHead", body, Ball, garudaRed, g + new Vector3(0f, 1.95f, 0.45f), new Vector3(0.75f, 0.7f, 0.75f));
            Part("Beak", body, Cone, gold, g + new Vector3(0f, 1.85f, 0.9f), new Vector3(0.25f, 0.45f, 0.25f), new Vector3(110f, 0f, 0f));
            Part("Crown", body, Cone, gold, g + new Vector3(0f, 2.2f, 0.4f), new Vector3(0.6f, 0.9f, 0.6f));
            foreach (float s in new[] { -1f, 1f })
            {
                Part("Wing", body, Cube, gold, g + new Vector3(s * 1.05f, 1.6f, 0f), new Vector3(1.4f, 0.14f, 0.7f), new Vector3(0f, 0f, s * 38f));
                Part("WingTip", body, Cube, gold, g + new Vector3(s * 1.7f, 2.35f, -0.05f), new Vector3(0.7f, 0.12f, 0.5f), new Vector3(0f, 0f, s * 65f));
                Part("Arm", body, Cube, garudaRed, g + new Vector3(s * 0.7f, 1.3f, 0.55f), new Vector3(0.22f, 0.8f, 0.22f), new Vector3(-40f, 0f, s * 10f));
                Part("Eye", body, Ball, black, g + new Vector3(s * 0.2f, 2.02f, 0.8f), Vector3.one * 0.1f);
            }
            // Vishnu standing on Garuda's shoulders: dark body, gold dress, tall crown, four arms with the conch, discus, mace
            var n = g + new Vector3(0f, 2.55f, 0.1f);
            Part("NaraiLegs", body, Cube, gold, n + new Vector3(0f, 0.35f, 0f), new Vector3(0.45f, 0.7f, 0.3f));
            Part("NaraiBody", body, Cube, navy, n + new Vector3(0f, 1f, 0f), new Vector3(0.5f, 0.65f, 0.3f));
            Part("NaraiHead", body, Ball, navy, n + new Vector3(0f, 1.52f, 0f), Vector3.one * 0.36f);
            Part("NaraiCrown", body, Cone, gold, n + new Vector3(0f, 1.62f, 0f), new Vector3(0.32f, 1f, 0.32f));
            foreach (float s in new[] { -1f, 1f })
                foreach (float up in new[] { 0f, 1f })
                {
                    var hand = n + new Vector3(s * (0.55f + up * 0.15f), 0.95f + up * 0.55f, 0.15f);
                    Stick("NaraiArm", body, navy, n + new Vector3(s * 0.25f, 1.15f, 0f), hand, 0.07f);
                    Part(up > 0f ? "Discus" : "Mace", body, up > 0f ? Rod : Cone, gold, hand, up > 0f ? new Vector3(0.3f, 0.04f, 0.3f) : new Vector3(0.12f, 0.45f, 0.12f), up > 0f ? new Vector3(90f, 0f, 0f) : Vector3.zero);
                }
            Enlarge(body, mark, g, 1.85f);
        }
        mark = body.childCount;
        // gold flame tail at the stern
        var tail = MeshKit.Curve(new Vector3(0f, D + 1.5f, -L * 0.5f + 0.5f), new Vector3(0f, D + 3.6f, -L * 0.5f - 0.9f), new Vector3(0f, D + 4f, -L * 0.5f + 0.5f), 10);
        MeshKit.Place("Tail", body, MeshKit.Sweep("Barges/NaraiTail", tail, MeshKit.Taper(10, 0.35f, 0.05f), 8), gold, Vector3.zero);
        Enlarge(body, mark, new Vector3(0f, D + 1.5f, -L * 0.5f + 0.5f), 1.45f);
        Kanya(body, new Vector3(0f, deck, 0f), 4.4f);
        foreach (float z in new[] { -7f, -5.2f, 5.2f, 7f })
            TieredUmbrella(body, new Vector3(0f, deck, z), 2.4f, 5, glassGreen);
        Finish(root, "NaraiSongSuban");
        return root;
    }

    // เรือพระที่นั่งอนันตนาคราช: seven naga heads fanning out over the bow, crests flaming up
    private static Transform Anantanakkharat(Transform p, Vector3 at)
    {
        const float L = 27f, B = 2.4f, D = 0.9f;
        var hullMat = Pattern("BargeLaiGoldGreen", new Color(0.86f, 0.66f, 0.24f), new Color(0.1f, 0.42f, 0.26f), 0.8f, 0.6f);
        var root = Barge(p, "Anantanakkharat", at, L, B, D, 1.3f, 2f, hullMat, red, crewRed, hatGold, gold, -2.4f, 2.4f, out var body, out float deck);
        float top = D + 1.3f;
        int mark = body.childCount;
        // the sculpted seven-headed naga (AI-made from a photo) when present, else the swept necks
        if (AiModels.Has("Naga")) AiModels.Place("Naga", body, new Vector3(0f, -0.3f, L * 0.5f - 0.6f), 5.2f);
        else for (int k = -3; k <= 3; k++)
        {
            var neck = MeshKit.Curve(new Vector3(0f, top - 0.2f, L * 0.5f - 0.6f),
                new Vector3(k * 0.3f, top + 1.9f, L * 0.5f + 0.7f),
                new Vector3(k * 0.52f, top + 2.9f - Mathf.Abs(k) * 0.22f, L * 0.5f + 1f - Mathf.Abs(k) * 0.12f), 10);
            MeshKit.Place("NagaNeck", body, MeshKit.Sweep($"Barges/NagaNeck{k + 3}", neck, MeshKit.Taper(10, 0.26f, 0.17f), 8), gold, Vector3.zero);
            var head = neck[9] + new Vector3(0f, 0.08f, 0.12f);
            Part("NagaHead", body, Ball, gold, head, new Vector3(0.34f, 0.3f, 0.55f));
            Part("NagaCrest", body, Cone, gold, head + new Vector3(0f, 0.12f, -0.08f), new Vector3(0.18f, 0.55f, 0.18f), new Vector3(-15f, 0f, k * 8f));
            Part("NagaJaw", body, Cone, glassGreen, head + new Vector3(0f, -0.08f, 0.28f), new Vector3(0.16f, 0.3f, 0.16f), new Vector3(90f, 0f, 0f));
        }
        if (!AiModels.Has("Naga")) Enlarge(body, mark, new Vector3(0f, top - 0.2f, L * 0.5f - 0.6f), 1.7f);
        mark = body.childCount;
        var tail = MeshKit.Curve(new Vector3(0f, D + 1.7f, -L * 0.5f + 0.5f), new Vector3(0f, D + 4f, -L * 0.5f - 1f), new Vector3(0f, D + 4.4f, -L * 0.5f + 0.6f), 12);
        MeshKit.Place("NagaTail", body, MeshKit.Sweep("Barges/NagaTail", tail, MeshKit.Taper(12, 0.4f, 0.05f), 8), gold, Vector3.zero);
        Enlarge(body, mark, new Vector3(0f, D + 1.7f, -L * 0.5f + 0.5f), 1.45f);
        Kanya(body, new Vector3(0f, deck, 0f), 4.2f);
        foreach (float z in new[] { -7.5f, -5.6f, 5.6f, 7.5f })
            TieredUmbrella(body, new Vector3(0f, deck, z), 2.4f, 5, red);
        Finish(root, "Anantanakkharat");
        return root;
    }

    // เรือพระที่นั่งอเนกชาติภุชงค์: a crimson hull with little gold nagas along it, a gilded stem
    private static Transform Anekchat(Transform p, Vector3 at)
    {
        const float L = 24f, B = 2.2f, D = 0.85f;
        var hullMat = Pattern("BargeLaiRed", new Color(0.62f, 0.08f, 0.06f), new Color(0.93f, 0.72f, 0.26f), 0.75f, 0.3f);
        var root = Barge(p, "Anekchat", at, L, B, D, 1.6f, 1.6f, hullMat, red, crewRed, hatGold, gold, -2.2f, 2.2f, out var body, out float deck);
        for (int i = 0; i < 9; i++) // little nagas along both sides
        {
            float z = -9f + i * 2.25f, w = HalfBeam(z, L, B);
            foreach (float s in new[] { -1f, 1f })
                Part("SmallNaga", body, Cone, gold, new Vector3(s * (w + 0.02f), D - 0.25f, z), new Vector3(0.18f, 0.5f, 0.18f), new Vector3(70f, 0f, 0f));
        }
        Part("StemCap", body, Cone, gold, new Vector3(0f, D + 1.5f, L * 0.5f - 0.3f), new Vector3(0.35f, 1.2f, 0.35f), new Vector3(-20f, 0f, 0f));
        Part("SternCap", body, Cone, gold, new Vector3(0f, D + 1.5f, -L * 0.5f + 0.3f), new Vector3(0.35f, 1.2f, 0.35f), new Vector3(20f, 0f, 0f));
        Kanya(body, new Vector3(0f, deck, 0f), 4f);
        foreach (float z in new[] { -6.5f, 6.5f })
            TieredUmbrella(body, new Vector3(0f, deck, z), 2.2f, 5, gold);
        Finish(root, "Anekchat");
        return root;
    }

    // Escort barges with a warrior seated at the bow holding two banners: กระบี่ปราบเมืองมาร (the
    // white monkey) or อสุรวายุภักษ์ (the green giant-bird, a cannon under him). Black hull, gold lai.
    private static Transform AnimalBarge(Transform p, Vector3 at, bool monkey)
    {
        const float L = 18f, B = 1.6f, D = 0.8f;
        string name = monkey ? "KrabiPrabMuangMan" : "AsuraWayuphak";
        var hullMat = Pattern("BargeLaiBlack", new Color(0.07f, 0.06f, 0.06f), new Color(0.88f, 0.68f, 0.25f), 0.8f, 0.4f);
        var root = Barge(p, name, at, L, B, D, 0.9f, 1.2f, hullMat, black, pink, red, gold, 0f, 0f, out var body, out float deck);
        var figure = PrimitiveKit.Mat(monkey ? "KrabiWhite" : "AsuraGreen", monkey ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.15f, 0.42f, 0.3f), 0.6f, 0.2f);
        if (monkey && AiModels.Has("Hanuman"))
        {
            // the sculpted monkey warrior (AI-made from a photo): gilded, dark-skinned, banners in hand
            AiModels.Place("Hanuman", body, new Vector3(0f, -0.15f, L * 0.5f - 1.5f), 4.2f);
            Finish(root, name);
            return root;
        }
        var f = new Vector3(0f, D + 0.9f, L * 0.5f - 0.9f);
        int mark = body.childCount;
        Part("Seat", body, Cube, gold, f + new Vector3(0f, -0.1f, 0f), new Vector3(1f, 0.3f, 1.2f));
        Part("FigureBody", body, Cube, gold, f + new Vector3(0f, 0.6f, 0f), new Vector3(0.75f, 1f, 0.55f));
        Part("FigureSkin", body, Cube, figure, f + new Vector3(0f, 0.65f, 0.29f), new Vector3(0.55f, 0.7f, 0.05f));
        Part("FigureHead", body, Ball, figure, f + new Vector3(0f, 1.38f, 0.05f), new Vector3(0.52f, 0.52f, 0.5f));
        Part("Crown", body, Cone, gold, f + new Vector3(0f, 1.5f, 0f), new Vector3(0.36f, 1.1f, 0.36f));
        Part("Muzzle", body, Ball, monkey ? figure : gold, f + new Vector3(0f, 1.3f, 0.3f), new Vector3(0.3f, 0.22f, 0.25f));
        foreach (float s in new[] { -1f, 1f })
        {
            Part("Leg", body, Cube, figure, f + new Vector3(s * 0.3f, 0.15f, 0.45f), new Vector3(0.24f, 0.24f, 0.7f));
            var hand = f + new Vector3(s * 0.55f, 0.9f, 0.3f);
            Stick("Arm", body, figure, f + new Vector3(s * 0.38f, 0.95f, 0f), hand, 0.09f);
            Stick("BannerPole", body, red, hand + Vector3.down * 0.8f, hand + Vector3.up * 1.6f, 0.03f);
            Part("Banner", body, Cube, gold, hand + new Vector3(s * 0.25f, 1.3f, 0f), new Vector3(0.45f, 0.5f, 0.03f), new Vector3(0f, 0f, s * -10f));
        }
        Enlarge(body, mark, new Vector3(0f, D + 0.3f, L * 0.5f - 0.9f), 1.8f);
        if (!monkey)
            Part("Cannon", body, Rod, black, new Vector3(0f, D + 0.15f, L * 0.5f - 0.6f), new Vector3(0.3f, 1.3f, 0.3f), new Vector3(80f, 0f, 0f));
        Finish(root, name);
        return root;
    }

    // เรือดั้ง: the long black escort barge, gold lai on its sides, a low curved stem, a little red
    // canopy for its officer on every other boat, a red pennant at the stern
    private static Transform Dang(Transform p, Vector3 at, int index)
    {
        const float L = 16f, B = 1.5f, D = 0.75f;
        var hullMat = Pattern("BargeLaiBlack", new Color(0.07f, 0.06f, 0.06f), new Color(0.88f, 0.68f, 0.25f), 0.8f, 0.4f);
        bool canopy = index % 2 == 0;
        var root = Barge(p, "Dang" + index, at, L, B, D, 1.1f, 1.2f, hullMat, black, crewRed, hatGold, gold,
            canopy ? -1.2f : 0f, canopy ? 1.2f : 0f, out var body, out float deck);
        Part("StemCap", body, Cone, gold, new Vector3(0f, D + 1.05f, L * 0.5f - 0.25f), new Vector3(0.22f, 0.7f, 0.22f), new Vector3(-25f, 0f, 0f));
        Part("SternCap", body, Cone, gold, new Vector3(0f, D + 1.15f, -L * 0.5f + 0.25f), new Vector3(0.22f, 0.7f, 0.22f), new Vector3(25f, 0f, 0f));
        if (canopy)
        {
            foreach (float x in new[] { -0.55f, 0.55f })
                foreach (float z in new[] { -0.9f, 0.9f })
                    Stick("Post", body, gold, new Vector3(x, deck, z), new Vector3(x, deck + 1.9f, z), 0.04f);
            MeshKit.Place("Canopy", body, MeshKit.Gable("Barges/DangRoof", 1.5f, 0.45f, 1f), red, new Vector3(0f, deck + 1.85f, -1.1f), Vector3.zero, new Vector3(1f, 1f, 2.2f));
            Standing(body, new Vector3(0f, deck, 0f), white, gold);
        }
        Stick("Pennant", body, gold, new Vector3(0f, deck, -L * 0.5f + 1.2f), new Vector3(0f, deck + 2.6f, -L * 0.5f + 1.2f), 0.03f);
        Part("PennantFlag", body, Cube, red, new Vector3(0f, deck + 2.35f, -L * 0.5f + 0.75f), new Vector3(0.03f, 0.4f, 0.9f));
        Finish(root, "Dang" + index);
        return root;
    }

    // ── The crowds ───────────────────────────────────────────────────────────────
    private static Material[] shirts;
    private static Material hair, trousers;

    private static void Person(Transform parent, Vector3 at, System.Random rng, bool seated = false)
    {
        var shirt = shirts[rng.Next(shirts.Length)];
        float h = 0.92f + (float)rng.NextDouble() * 0.16f, turn = ((float)rng.NextDouble() - 0.5f) * 30f;
        var e = new Vector3(0f, turn, 0f);
        if (seated)
        {
            Part("Chair", parent, Cube, white, at + new Vector3(0f, 0.42f, -0.05f), new Vector3(0.48f, 0.06f, 0.46f));
            Part("ChairBack", parent, Cube, white, at + new Vector3(0f, 0.72f, -0.27f), new Vector3(0.48f, 0.55f, 0.05f));
            Part("Legs", parent, Cube, trousers, at + new Vector3(0f, 0.5f, 0.12f), new Vector3(0.32f, 0.16f, 0.42f), e);
            Part("Body", parent, Torso, shirt, at + new Vector3(0f, 0.85f * h, -0.08f), new Vector3(0.4f, 0.58f * h, 0.26f), e);
            Part("Head", parent, Ball, skin, at + new Vector3(0f, 1.32f * h, -0.06f), Vector3.one * 0.24f);
            Part("Hair", parent, Ball, hair, at + new Vector3(0f, 1.39f * h, -0.09f), new Vector3(0.25f, 0.15f, 0.25f));
            return;
        }
        var right = Quaternion.Euler(e) * Vector3.right;
        foreach (float s in new[] { -1f, 1f })
        {
            Part("Leg", parent, Rod, trousers, at + right * (s * 0.095f), new Vector3(0.14f, 0.84f * h, 0.14f));
            Stick("Arm", parent, shirt, at + right * (s * 0.23f) + Vector3.up * 1.34f * h, at + right * (s * 0.27f) + Vector3.up * 0.86f * h, 0.05f);
        }
        Part("Body", parent, Torso, shirt, at + new Vector3(0f, 1.12f * h, 0f), new Vector3(0.42f, 0.58f * h, 0.27f), e);
        Part("Head", parent, Ball, skin, at + new Vector3(0f, 1.6f * h, 0f), Vector3.one * 0.25f);
        Part("Hair", parent, Ball, hair, at + new Vector3(0f, 1.68f * h, -0.03f), new Vector3(0.26f, 0.16f, 0.26f));
        int extra = rng.Next(10);
        if (extra < 2) // a little flag to wave
        {
            Stick("HandFlag", parent, white, at + new Vector3(0.25f, 1.1f, 0.1f), at + new Vector3(0.3f, 1.9f, 0.15f), 0.012f);
            Part("HandFlagCloth", parent, Cube, extra == 0 ? Yellow : Purple, at + new Vector3(0.45f, 1.78f, 0.15f), new Vector3(0.3f, 0.2f, 0.01f));
        }
        else if (extra == 2) // a sun umbrella
        {
            Stick("Umbrella", parent, hair, at + new Vector3(0.15f, 1.2f, 0f), at + new Vector3(0.15f, 2.05f, 0f), 0.015f);
            Part("UmbrellaTop", parent, Cone, shirts[rng.Next(shirts.Length)], at + new Vector3(0.15f, 1.85f, 0f), new Vector3(1f, 0.32f, 1f));
        }
        else if (extra == 3) // holding up a phone to film
            Part("Phone", parent, Cube, trousers, at + new Vector3(0.1f, 1.65f, 0.3f), new Vector3(0.08f, 0.14f, 0.01f));
    }

    private static Material Yellow => PrimitiveKit.Mat("RoyalYellow", new Color(1f, 0.82f, 0.1f));
    private static Material Purple => PrimitiveKit.Mat("RoyalPurple", new Color(0.55f, 0.35f, 0.75f));

    private static void PrepareCrowd()
    {
        if (shirts != null && shirts[0] != null) return;
        shirts = new[]
        {
            Yellow, Yellow, Yellow, PrimitiveKit.Mat("ShirtYellowPale", new Color(1f, 0.9f, 0.45f)),
            PrimitiveKit.Mat("ShirtWhite", new Color(0.95f, 0.95f, 0.95f)), PrimitiveKit.Mat("ShirtWhite", new Color(0.95f, 0.95f, 0.95f)),
            Purple, PrimitiveKit.Mat("ShirtPink", new Color(0.95f, 0.6f, 0.7f)), PrimitiveKit.Mat("ShirtBlue", new Color(0.3f, 0.5f, 0.85f)),
            PrimitiveKit.Mat("ShirtBlack", new Color(0.12f, 0.12f, 0.14f)),
        };
        hair = PrimitiveKit.Mat("HairBlack", new Color(0.06f, 0.05f, 0.05f));
        trousers = PrimitiveKit.Mat("TrousersDark", new Color(0.18f, 0.2f, 0.26f));
    }

    // People along both promenades two deep at the rail, and seated stands under white tents beside
    // วัดอรุณ. In blocks of 40 m (merged by material) so the far ones can be culled.
    private static void Spectators(Transform river, float half, float length)
    {
        PrepareCrowd();
        var rng = new System.Random(1906);
        int block = 0;
        for (float x0 = -length * 0.5f + 4f; x0 < length * 0.5f - 4f; x0 += 40f)
            foreach (float side in new[] { -1f, 1f })
            {
                var g = PrimitiveKit.Group("Crowd_ผู้ชม", river, Vector3.zero).transform;
                for (float x = x0; x < Mathf.Min(x0 + 40f, length * 0.5f - 4f); x += 0.7f)
                {
                    if (Mathf.Abs(x - Bangkok.PierX) < 4.2f) continue;                      // the ferry pier
                    if (side > 0f && (Mathf.Abs(x) < 5f || FlagAt(x))) continue;            // the temple's board, the flags
                    foreach (float row in new[] { 0.45f, 0.95f })
                    {
                        if (rng.Next(9) == 0) continue;                                     // a gap now and then
                        float z = side * (half + row) + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                        var at = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 0.2f, 0f, z);
                        var person = PrimitiveKit.Group("P", g, at, side < 0f ? 0f : 180f).transform; // facing the water
                        Person(person, Vector3.zero, rng);
                    }
                }
                MeshKit.MergeByMaterial(g, $"Barges/Crowd{block++}");
            }

        // Stands beside วัดอรุณ: white tents over rows of chairs, everyone facing the river
        var tent = PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.95f, 0.92f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var g = PrimitiveKit.Group("Stand_อัฒจันทร์", river, new Vector3(sx * 27.5f, 0f, half + 8.2f), 180f).transform;
            for (int r = 0; r < 6; r++)
                for (float x = -9f; x <= 9f; x += 0.62f)
                {
                    if (rng.Next(12) == 0) continue;
                    Person(PrimitiveKit.Group("P", g, new Vector3(x, 0f, -2.6f + r * 1f), 0f).transform, Vector3.zero, rng, seated: true);
                }
            foreach (float x in new[] { -9.6f, -3.2f, 3.2f, 9.6f })
                foreach (float z in new[] { -3.4f, 3.4f })
                    Stick("TentPole", g, white, new Vector3(x, 0f, z), new Vector3(x, 3f, z), 0.05f);
            MeshKit.Place("TentRoof", g, MeshKit.Gable("Barges/StandRoof", 7.6f, 1.2f, 1f), tent, new Vector3(-10f, 3f, 0f), new Vector3(0f, 90f, 0f), new Vector3(1f, 1f, 20f));
            MeshKit.MergeByMaterial(g, $"Barges/Stand{(sx < 0f ? 0 : 1)}");
        }
    }

    private static bool FlagAt(float x)
    {
        for (int i = 0; i < 12; i++)
            if (Mathf.Abs(x - (-33f + i * 6f)) < 0.5f) return true;
        return false;
    }

    // พลับพลา: the royal landing on its pontoon off the วัดอรุณ bank: grey deck, red-and-gold gateways
    // with lotus-bud finials, white drapes along the rails, a red carpet to a little gold pavilion
    private static void RoyalLanding(Transform river, Vector3 at)
    {
        var g = PrimitiveKit.Group("RoyalLanding_พลับพลา", river, at).transform;
        var deck = PrimitiveKit.Mat("PontoonGrey", new Color(0.45f, 0.47f, 0.5f));
        var lacquer = PrimitiveKit.Mat("LandingRed", new Color(0.78f, 0.1f, 0.08f), 0.6f);
        var drape = PrimitiveKit.Mat("DrapeWhite", new Color(0.97f, 0.97f, 0.97f), 0.2f);
        Part("Pontoon", g, Cube, deck, new Vector3(0f, 0.2f, 0f), new Vector3(9f, 0.4f, 3.6f));
        Part("Carpet", g, Cube, red, new Vector3(0f, 0.41f, 0.3f), new Vector3(7f, 0.02f, 1.4f));
        foreach (float x in new[] { -4f, 0f, 4f }) // gateways facing the river
        {
            foreach (float s in new[] { -0.9f, 0.9f })
            {
                Part("GatePost", g, Cube, lacquer, new Vector3(x + s, 1.9f, -1.6f), new Vector3(0.28f, 3f, 0.28f));
                Part("Lotus", g, Cone, gold, new Vector3(x + s, 3.4f, -1.6f), new Vector3(0.36f, 0.6f, 0.36f));
            }
            foreach (float y in new[] { 2.6f, 3.1f })
                Part("GateBeam", g, Cube, lacquer, new Vector3(x, y, -1.6f), new Vector3(2.3f, 0.16f, 0.18f));
        }
        foreach (float z in new[] { -1.7f, 1.7f })
        {
            Part("Rail", g, Cube, lacquer, new Vector3(0f, 1.1f, z), new Vector3(9f, 0.08f, 0.08f));
            Part("Drape", g, Cube, drape, new Vector3(0f, 0.8f, z), new Vector3(8.8f, 0.5f, 0.05f));
        }
        // the little gold pavilion
        foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -0.7f, 0.9f })
                Stick("Column", g, gold, new Vector3(x, 0.4f, z), new Vector3(x, 2.8f, z), 0.06f);
        MeshKit.Place("Roof", g, MeshKit.Gable("Barges/LandingRoof", 2.6f, 0.9f, 1f), red, new Vector3(-1.2f, 2.8f, 0.1f), new Vector3(0f, 90f, 0f), new Vector3(1f, 1f, 2.4f));
        foreach (float x in new[] { -2.6f, 2.6f })
            Standing(g, new Vector3(x, 0.4f, 0.5f), white, gold);
        MeshKit.MergeByMaterial(g, "Barges/RoyalLanding");
    }

    // Yellow (ธงพระราชพิธี) and purple flags on tall poles along the city promenade
    private static void BankFlags(Transform river, float half, float length)
    {
        var pole = PrimitiveKit.Mat("RopeWhite", Color.white);
        var g = PrimitiveKit.Group("CeremonyFlags_ธงพระราชพิธี", river, Vector3.zero).transform;
        int i = 0;
        for (float x = -length * 0.5f + 8f; x < length * 0.5f - 8f; x += 9f)
        {
            if (Mathf.Abs(x - 12f) < 5f) continue;
            var at = new Vector3(x, 0f, -half - 3.85f);
            Stick("Flagpole", g, pole, at, at + Vector3.up * 7f, 0.06f);
            Part("Flag", g, Cube, i++ % 2 == 0 ? Yellow : Purple, at + new Vector3(0.7f, 6.4f, 0f), new Vector3(1.3f, 0.9f, 0.03f));
        }
        MeshKit.MergeByMaterial(g, "Barges/BankFlags");
    }

    // ── The city side: a festival plaza for watching ───────────────────────────────
    // A big live screen (LiveScreen films the procession) under a purple ทรงพระเจริญ banner, rows of
    // seated watchers facing it, a riverside food market, street-food carts with umbrellas, tuk-tuks
    // waiting for fares, and people strolling round it all.
    public static void Plaza(Transform root)
    {
        PrepareCrowd();
        var g = PrimitiveKit.Group("BargeFestival_ลานชมขบวนเรือ", root, new Vector3(19f, 0f, 32f)).transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
        var frame = PrimitiveKit.Mat("ScreenFrame", new Color(0.1f, 0.1f, 0.12f), 0.6f, 0.3f);

        // the screen, facing the seats (south, -z)
        var screenAt = new Vector3(0f, 0f, 9f);
        foreach (float x in new[] { -4.3f, 4.3f })
            PrimitiveKit.Box("ScreenLeg", g, screenAt + new Vector3(x, 2.6f, 0.2f), new Vector3(0.3f, 5.2f, 0.3f), steel);
        PrimitiveKit.Box("ScreenFrame", g, screenAt + new Vector3(0f, 4.4f, 0.15f), new Vector3(8.4f, 4.8f, 0.3f), frame, collider: false);
        var screen = PrimitiveKit.Create(PrimitiveType.Quad, "LiveScreen", g, screenAt + new Vector3(0f, 4.4f, -0.02f), new Vector3(8f, 4.5f, 1f),
            PrimitiveKit.Unlit("LiveScreenPicture", new Color(0.05f, 0.08f, 0.2f)), new Vector3(0f, 180f, 0f), collider: false);
        screen.transform.localRotation = Quaternion.identity; // a quad shows its face to -z
        g.gameObject.AddComponent<LiveScreen>().Configure(screen.GetComponent<Renderer>());
        PrimitiveKit.Box("Banner", g, screenAt + new Vector3(0f, 7.35f, 0.1f), new Vector3(8.4f, 1.1f, 0.12f), Purple, collider: false);
        PrimitiveKit.Label("ทรงพระเจริญ", g, screenAt + new Vector3(0f, 7.35f, -0.05f), 0.09f, new Color(1f, 0.95f, 0.8f));
        PrimitiveKit.Label("ถ่ายทอดสด · ขบวนเรือพระราชพิธี", g, screenAt + new Vector3(0f, 1.6f, -0.05f), 0.04f, new Color(1f, 0.85f, 0.4f));

        // seated watchers in rows facing the screen
        var seats = PrimitiveKit.Group("Seats_ผู้ชมจอ", g, Vector3.zero).transform;
        var rng = new System.Random(77);
        for (int r = 0; r < 7; r++)
            for (float x = -6f; x <= 6f; x += 0.66f)
            {
                if (Mathf.Abs(x) < 0.5f || rng.Next(8) == 0) continue; // an aisle down the middle
                Person(PrimitiveKit.Group("P", seats, new Vector3(x, 0f, 4.5f - r * 1.05f), 0f).transform, Vector3.zero, rng, seated: true);
            }
        MeshKit.MergeByMaterial(seats, "Barges/PlazaSeats");

        // the market beside it and carts along the front
        Markets.FleaMarket(root, new Vector3(39f, 0f, 30f), "ตลาดริมน้ำ · ชมขบวนเรือ", "07:00-22:00");
        Cart(g, new Vector3(-14f, 0f, 6f), "ไก่ย่าง ส้มตำ", new Color(0.9f, 0.3f, 0.2f), 0);
        Cart(g, new Vector3(-14f, 0f, 1f), "น้ำมะพร้าว", new Color(0.3f, 0.7f, 0.35f), 1);
        Cart(g, new Vector3(-14f, 0f, -4f), "ลูกชิ้นปิ้ง", new Color(0.95f, 0.7f, 0.2f), 2);
        for (int i = 0; i < 3; i++)
        {
            var v = PrimitiveKit.Group("ParkedTukTuk_ตุ๊กตุ๊ก", root, new Vector3(-17f + i * 3.4f, 0f, 40.5f), 90f).transform;
            CityTraffic.TukTuk(v, i);
        }
        Places.Sign(root, new Vector3(-4f, 0f, 40f), "ตุ๊กตุ๊ก · ไปสนามหลวง วัดพระแก้ว\nเริ่ม 60 บาท", 180f);
        Npcs.TouristCrowd(root, new Vector3(19f, 0f, 22.5f), new Vector2(26f, 3f), 8, 3, 6, 22);
        Npcs.Crowd(root, new Vector3(2f, 0f, 38f), new Vector2(10f, 4f), 5, "", 7, 22, Npcs.MarketTalk);
    }

    // A street-food cart: a box on wheels, a glass case, a striped umbrella, the vendor behind it
    private static void Cart(Transform parent, Vector3 at, string name, Color umbrella, int look)
    {
        var c = PrimitiveKit.Group("FoodCart_รถเข็น", parent, at, 90f).transform;
        var steelCart = PrimitiveKit.Mat("CartSteel", new Color(0.8f, 0.82f, 0.84f), 0.8f, 0.6f);
        PrimitiveKit.Box("Cart", c, new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 0.8f, 0.8f), steelCart);
        PrimitiveKit.Box("Glass", c, new Vector3(0f, 1.25f, 0f), new Vector3(1.4f, 0.5f, 0.6f), PrimitiveKit.Mat("TerminalGlass", new Color(0.45f, 0.6f, 0.68f), 0.92f, 0.5f), collider: false);
        foreach (float x in new[] { -0.6f, 0.6f })
            PrimitiveKit.Cylinder("Wheel", c, new Vector3(x, 0.2f, 0.42f), new Vector3(0.4f, 0.04f, 0.4f), black, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Cylinder("UmbrellaPole", c, new Vector3(0.7f, 1.4f, -0.3f), new Vector3(0.04f, 1.4f, 0.04f), steelCart, collider: false);
        MeshKit.Place("Umbrella", c, Cone, PrimitiveKit.Mat("CartUmbrella" + look, umbrella), new Vector3(0.7f, 2.4f, -0.3f), Vector3.zero, new Vector3(2.2f, 0.5f, 2.2f));
        PrimitiveKit.Label(name, c, new Vector3(0f, 0.75f, -0.42f), 0.03f, new Color(0.2f, 0.1f, 0.05f));
        var vendor = new GameObject("Vendor");
        vendor.transform.SetParent(c, false);
        vendor.transform.localPosition = new Vector3(0f, 0f, 0.95f);
        vendor.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        vendor.AddComponent<MarketVendor>().Configure(10 + look);
    }
}
