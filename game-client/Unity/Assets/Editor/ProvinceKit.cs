using System.Collections.Generic;
using UnityEngine;

// Building blocks for the provinces' landmarks (ProvinceTown and the province builders): bell chedis,
// Khmer prangs, Thai halls, Chinese shrines, hills with naga stairs, waterfalls, a stadium, a race
// circuit, a lake bridge, beaches with dolphins… Every attraction is a group named "Attr<Kind>_<ชื่อ>"
// with a sign, so the taxi map (MapPlaces) lists it as somewhere to go.
public static class ProvinceKit
{
    // ── Materials ─────────────────────────────────────────────────────────────────────────────
    private static Material Mat(string n, Color c, float s = 0.25f, float m = 0f) => PrimitiveKit.Mat(n, c, s, m);
    public static Material White => Mat("PK_ChediWhite", new Color(0.96f, 0.95f, 0.91f), 0.35f);
    public static Material Gold => Mat("PK_Gold", new Color(0.96f, 0.76f, 0.22f), 0.8f, 0.85f);
    public static Material Sandstone => Mat("PK_Sandstone", new Color(0.8f, 0.64f, 0.5f), 0.12f);
    public static Material Brick => Mat("PK_OldBrick", new Color(0.62f, 0.32f, 0.22f), 0.1f);
    public static Material Laterite => Mat("PK_Laterite", new Color(0.6f, 0.38f, 0.28f), 0.08f);
    public static Material Granite => Mat("PK_Granite", new Color(0.7f, 0.69f, 0.66f), 0.3f);
    public static Material Wood => Mat("PK_DarkWood", new Color(0.42f, 0.27f, 0.16f), 0.3f);
    public static Material RoofRed => Mat("PK_RoofRed", new Color(0.72f, 0.2f, 0.12f), 0.45f);
    public static Material RoofGreen => Mat("PK_RoofGreen", new Color(0.15f, 0.48f, 0.3f), 0.5f);
    public static Material RoofBlue => Mat("PK_RoofBlue", new Color(0.13f, 0.3f, 0.62f), 0.5f);
    public static Material ShrineRed => Mat("PK_ShrineRed", new Color(0.78f, 0.1f, 0.08f), 0.45f);
    public static Material Grass => Mat("PK_Lawn", new Color(0.33f, 0.62f, 0.27f), 0.1f);
    public static Material Forest => Mat("PK_Forest", new Color(0.2f, 0.45f, 0.2f), 0.1f);
    public static Material Rock => Mat("PK_Rock", new Color(0.48f, 0.47f, 0.45f), 0.15f);
    public static Material Water => Mat("PK_Water", new Color(0.18f, 0.55f, 0.72f), 0.92f);
    public static Material Fall => Mat("PK_FallWater", new Color(0.78f, 0.93f, 1f), 0.95f);
    public static Material Sand => Mat("BeachSand", new Color(0.95f, 0.86f, 0.62f));
    public static Material Paving => Mat("Pavement", new Color(0.72f, 0.7f, 0.66f));
    public static Material Asphalt => Mat("Asphalt", new Color(0.22f, 0.22f, 0.24f));
    public static Material Paint => Mat("RoadWhite", new Color(0.95f, 0.95f, 0.93f));
    public static Material Dark => Mat("PK_DoorDark", new Color(0.12f, 0.08f, 0.06f), 0.3f);
    public static Material Steel => Mat("PK_Steel", new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.6f);
    public static Material Concrete => Mat("PoleConcrete", new Color(0.78f, 0.77f, 0.74f));

    private static Vector2 V(float x, float y) => new Vector2(x, y);
    private static Transform G(string name, Transform p, Vector3 at, float yaw = 0f) => PrimitiveKit.Group(name, p, at, yaw).transform;

    public static void Blocker(Transform p, Vector3 pos, Vector3 size, float yaw = 0f)
    {
        var b = new GameObject("Blocker");
        b.transform.SetParent(p, false);
        b.transform.localPosition = pos;
        b.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        b.AddComponent<BoxCollider>().size = size;
    }

    // ── The attraction itself: its group (named for the map), a sign, and visitors ──────────────
    public static Transform Attraction(Transform root, string kind, string thai, Vector3 at, float yaw, string info,
        Vector3 sign, Vector3 crowdAt, Vector2 crowdSize, int crowd = 6)
    {
        var t = G($"Attr{kind}_{thai}", root, at, yaw);
        Places.Sign(t, sign, thai + "\n" + info, 0f);
        if (crowd > 0) Npcs.TouristCrowd(t, crowdAt, crowdSize, crowd, Mathf.Max(1, crowd / 3), 6, 21);
        return t;
    }

    // A strip of paving (a path, a forecourt)
    public static void Path(Transform p, Vector3 from, Vector3 to, float width, Material m = null)
    {
        var d = to - from;
        d.y = 0f;
        float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        PrimitiveKit.Box("Path", p, (from + to) * 0.5f + Vector3.up * 0.012f, new Vector3(width, 0.02f, d.magnitude), m ?? Paving, new Vector3(0f, yaw, 0f), collider: false);
    }

    // ── Bell chedi (เจดีย์ทรงระฆัง, Sri Lankan style): white bell, gilded spire ──────────────────
    public static Transform Chedi(Transform p, Vector3 at, float height, Material body = null, bool goldTop = true)
    {
        var g = G("Chedi_เจดีย์", p, at);
        var bell = MeshKit.Lathe("PK_ChediBody", new[]
        {
            V(0.44f, 0f), V(0.44f, 0.035f), V(0.38f, 0.036f), V(0.38f, 0.07f), V(0.32f, 0.071f), V(0.32f, 0.1f),
            V(0.3f, 0.105f), V(0.3f, 0.13f), V(0.295f, 0.17f), V(0.28f, 0.22f), V(0.25f, 0.27f), V(0.2f, 0.31f),
            V(0.13f, 0.34f), V(0.07f, 0.355f), V(0.08f, 0.36f), V(0.08f, 0.4f),
        }, 32);
        var spire = MeshKit.Lathe("PK_ChediSpire", new[]
        {
            V(0.08f, 0f), V(0.055f, 0.02f), V(0.05f, 0.06f), V(0.056f, 0.075f), V(0.04f, 0.1f), V(0.032f, 0.25f),
            V(0.02f, 0.42f), V(0.008f, 0.55f), V(0.002f, 0.6f),
        }, 20);
        MeshKit.Place("Bell", g, bell, body ?? White, Vector3.zero, default, Vector3.one * height);
        MeshKit.Place("Spire", g, spire, goldTop ? Gold : body ?? White, new Vector3(0f, 0.4f * height, 0f), default, Vector3.one * height);
        var col = g.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, height * 0.2f, 0f);
        col.radius = height * 0.36f;
        col.height = height * 0.45f;
        return g;
    }

    // ── Khmer prang (ปรางค์): stepped plinth, cella with four porches, corn-cob tiers, lotus top ──
    public static Transform Prang(Transform p, Vector3 at, float h, Material stone, float yaw = 0f)
    {
        var g = G("Prang_ปรางค์", p, at, yaw);
        float w = 0.4f * h;
        PrimitiveKit.Box("Plinth", g, new Vector3(0f, 0.04f * h, 0f), new Vector3(w * 1.45f, 0.08f * h, w * 1.45f), stone);
        PrimitiveKit.Box("Plinth2", g, new Vector3(0f, 0.11f * h, 0f), new Vector3(w * 1.2f, 0.06f * h, w * 1.2f), stone);
        PrimitiveKit.Box("Cella", g, new Vector3(0f, 0.3f * h, 0f), new Vector3(w, 0.32f * h, w), stone);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        for (int k = 0; k < 4; k++)
        {
            var rot = Quaternion.Euler(0f, k * 90f, 0f);
            var porch = G("Porch", g, rot * new Vector3(0f, 0f, -w * 0.62f), k * 90f);
            PrimitiveKit.Box("PorchBody", porch, new Vector3(0f, 0.27f * h, 0f), new Vector3(w * 0.42f, 0.26f * h, w * 0.3f), stone, collider: false);
            PrimitiveKit.Box("Door", porch, new Vector3(0f, 0.24f * h, -w * 0.151f), new Vector3(w * 0.18f, 0.18f * h, 0.05f), Dark, collider: false);
            MeshKit.Place("Pediment", porch, gable, stone, new Vector3(0f, 0.4f * h, -w * 0.17f), default, new Vector3(w * 0.5f, 0.12f * h, w * 0.32f));
        }
        float y = 0.46f * h;
        for (int i = 0; i < 5; i++)
        {
            float s = w * (0.95f - 0.13f * i), th = 0.075f * h * (1f - 0.07f * i);
            PrimitiveKit.Box("Tier", g, new Vector3(0f, y + th * 0.5f, 0f), new Vector3(s, th, s), stone, collider: false);
            for (int c = 0; c < 4; c++) // little antefixes at the corners
            {
                var q = Quaternion.Euler(0f, 45f + c * 90f, 0f) * new Vector3(0f, 0f, s * 0.68f);
                PrimitiveKit.Box("Antefix", g, new Vector3(q.x, y + th + 0.02f * h, q.z), new Vector3(0.05f * h, 0.06f * h, 0.03f * h), stone, new Vector3(0f, 45f + c * 90f, 0f), collider: false);
            }
            y += th;
        }
        var top = MeshKit.Lathe("PK_PrangTop", new[] { V(0.5f, 0f), V(0.58f, 0.25f), V(0.5f, 0.55f), V(0.25f, 0.85f), V(0.02f, 1f) }, 12);
        MeshKit.Place("Lotus", g, top, stone, new Vector3(0f, y, 0f), new Vector3(0f, 15f, 0f), new Vector3(w * 0.4f, 0.14f * h, w * 0.4f));
        return g;
    }

    // ── Thai hall (วิหาร / อุโบสถ): platform, walls, front veranda, two-tier roof, gold chofa ─────
    public static Transform ThaiHall(Transform p, Vector3 at, float yaw, float w, float d, Material wall, Material roof, string label = null)
    {
        var g = G("ThaiHall_วิหาร", p, at, yaw);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        PrimitiveKit.Box("Platform", g, new Vector3(0f, 0.3f, 0f), new Vector3(w + 2.4f, 0.6f, d + 3.2f), Granite);
        PrimitiveKit.Box("Steps", g, new Vector3(0f, 0.15f, -(d + 3.2f) * 0.5f - 0.5f), new Vector3(3f, 0.3f, 1f), Granite);
        PrimitiveKit.Box("Walls", g, new Vector3(0f, 2.2f, 0.6f), new Vector3(w, 3.2f, d), wall);
        PrimitiveKit.Box("Door", g, new Vector3(0f, 1.8f, 0.6f - d * 0.5f - 0.03f), new Vector3(1.6f, 2.4f, 0.06f), Gold, collider: false);
        for (float x = -w * 0.5f + 0.3f; x <= w * 0.5f - 0.29f; x += (w - 0.6f) / 3f)
        {
            PrimitiveKit.Cylinder("Column", g, new Vector3(x, 2.2f, -d * 0.5f - 0.9f), new Vector3(0.45f, 1.6f, 0.45f), wall);
            PrimitiveKit.Box("Capital", g, new Vector3(x, 3.75f, -d * 0.5f - 0.9f), new Vector3(0.55f, 0.18f, 0.55f), Gold, collider: false);
        }
        float lowD = d + 2.6f;
        MeshKit.Place("RoofLow", g, gable, roof, new Vector3(0f, 3.8f, -lowD * 0.5f + 0.4f), default, new Vector3(w + 1.6f, 2.3f, lowD));
        MeshKit.Place("RoofHigh", g, gable, roof, new Vector3(0f, 4.9f, -lowD * 0.5f + 1.2f), default, new Vector3(w * 0.68f, 2.3f, lowD - 1.6f));
        foreach (float z in new[] { -lowD * 0.5f + 1.15f, lowD * 0.5f - 0.35f }) // gilded gable boards and the chofa on the ridge ends
        {
            MeshKit.Place("GableBoard", g, gable, Gold, new Vector3(0f, 4.95f, z - 0.06f), default, new Vector3(w * 0.6f, 2.1f, 0.08f));
            PrimitiveKit.Box("Chofa", g, new Vector3(0f, 7.45f, z), new Vector3(0.18f, 0.9f, 0.18f), Gold, new Vector3(-25f, 0f, 0f), collider: false);
        }
        for (int i = 0; i < 4; i++) // the naga-tail eave ends (hang hong)
        {
            float sx = i < 2 ? -1f : 1f, z = i % 2 == 0 ? -lowD * 0.5f + 0.4f : lowD * 0.5f + 0.4f;
            PrimitiveKit.Box("HangHong", g, new Vector3(sx * (w * 0.5f + 0.75f), 4f, z), new Vector3(0.14f, 0.55f, 0.14f), Gold, new Vector3(0f, 0f, sx * 25f), collider: false);
        }
        if (!string.IsNullOrEmpty(label))
            PrimitiveKit.Label(label, g, new Vector3(0f, 4.3f, -lowD * 0.5f + 1f), 0.05f, new Color(0.45f, 0.2f, 0.05f));
        return g;
    }

    // A Thai pavilion with a tall stepped spire (ศาลาจตุรมุข / ศาลหลักเมือง style)
    public static Transform SpirePavilion(Transform p, Vector3 at, float yaw, float w, Material wall, Material roof)
    {
        var g = G("Pavilion_มณฑป", p, at, yaw);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        PrimitiveKit.Box("Base", g, new Vector3(0f, 0.4f, 0f), new Vector3(w + 3f, 0.8f, w + 3f), Granite);
        PrimitiveKit.Box("Body", g, new Vector3(0f, 2.4f, 0f), new Vector3(w, 3.2f, w), wall);
        for (int k = 0; k < 2; k++) // cross-shaped roof (จตุรมุข)
            MeshKit.Place("Roof", g, gable, roof, Quaternion.Euler(0f, k * 90f, 0f) * new Vector3(0f, 0f, -(w + 3f) * 0.5f) + Vector3.up * 4f, new Vector3(0f, k * 90f, 0f), new Vector3(w * 0.7f, 2f, w + 3f));
        float y = 5.8f;
        for (int i = 0; i < 5; i++)
        {
            float s = w * (0.55f - i * 0.09f);
            PrimitiveKit.Box("SpireTier", g, new Vector3(0f, y + 0.35f, 0f), new Vector3(s, 0.7f, s), i % 2 == 0 ? Gold : roof, collider: false);
            y += 0.7f;
        }
        var spire = MeshKit.Lathe("PK_Pinnacle", new[] { V(0.5f, 0f), V(0.32f, 0.2f), V(0.36f, 0.3f), V(0.18f, 0.5f), V(0.08f, 0.8f), V(0.01f, 1f) }, 16);
        MeshKit.Place("Pinnacle", g, spire, Gold, new Vector3(0f, y, 0f), default, new Vector3(w * 0.22f, 4f, w * 0.22f));
        PrimitiveKit.Box("Door", g, new Vector3(0f, 2f, -w * 0.5f - 0.03f), new Vector3(1.3f, 2.4f, 0.06f), Gold, collider: false);
        return g;
    }

    // ── Chinese shrine (ศาลเจ้า): red hall, green tiled roof with a curling ridge, lanterns ──────
    public static Transform ChineseShrine(Transform p, Vector3 at, float yaw, float w, float d, string label)
    {
        var g = G("ChineseShrine_ศาลเจ้า", p, at, yaw);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        var lantern = PrimitiveKit.Glow("PK_Lantern", new Color(1f, 0.15f, 0.08f), 1.6f);
        PrimitiveKit.Box("Base", g, new Vector3(0f, 0.3f, 0f), new Vector3(w + 3f, 0.6f, d + 4f), Granite);
        PrimitiveKit.Box("Hall", g, new Vector3(0f, 2.2f, 0.5f), new Vector3(w, 3.2f, d), ShrineRed);
        PrimitiveKit.Box("Door", g, new Vector3(0f, 1.9f, 0.5f - d * 0.5f - 0.03f), new Vector3(2f, 2.6f, 0.06f), Mat("PK_DoorGoldRed", new Color(0.55f, 0.08f, 0.05f), 0.5f), collider: false);
        foreach (float x in new[] { -w * 0.5f + 0.4f, -1.6f, 1.6f, w * 0.5f - 0.4f })
            PrimitiveKit.Cylinder("Column", g, new Vector3(x, 2.2f, -d * 0.5f - 1.2f), new Vector3(0.42f, 1.6f, 0.42f), ShrineRed);
        // the roof: ridge along the front (turn the prism sideways), a low pitch, gold ridge ends
        MeshKit.Place("Roof", g, gable, RoofGreen, new Vector3(-(w + 2.6f) * 0.5f, 3.8f, 0f), new Vector3(0f, 90f, 0f), new Vector3(d + 4f, 1.8f, w + 2.6f));
        PrimitiveKit.Box("Ridge", g, new Vector3(0f, 5.65f, 0f), new Vector3(w + 1.6f, 0.3f, 0.35f), ShrineRed, collider: false);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("RidgeCurl", g, new Vector3(s * (w * 0.5f + 1f), 6f, 0f), new Vector3(0.9f, 0.3f, 0.3f), Gold, new Vector3(0f, 0f, s * 35f), collider: false);
        PrimitiveKit.Sphere("Pearl", g, new Vector3(0f, 6.1f, 0f), Vector3.one * 0.6f, Gold);
        for (float x = -w * 0.5f + 1f; x <= w * 0.5f - 0.9f; x += 2f)
            PrimitiveKit.Sphere("Lantern", g, new Vector3(x, 3.3f, -d * 0.5f - 1.6f), new Vector3(0.55f, 0.65f, 0.55f), lantern);
        PrimitiveKit.Box("NameBoard", g, new Vector3(0f, 4.15f, 0.5f - d * 0.5f - 0.05f), new Vector3(3.6f, 0.7f, 0.08f), Mat("PK_BoardBlack", new Color(0.08f, 0.08f, 0.08f), 0.6f), collider: false);
        PrimitiveKit.Label(label, g, new Vector3(0f, 4.15f, 0.5f - d * 0.5f - 0.1f), 0.04f, new Color(1f, 0.82f, 0.3f));
        var urn = MeshKit.Lathe("PK_Urn", new[] { V(0.3f, 0f), V(0.5f, 0.2f), V(0.55f, 0.5f), V(0.6f, 0.6f), V(0.62f, 0.62f) }, 16);
        MeshKit.Place("IncenseUrn", g, urn, Mat("PK_Bronze", new Color(0.45f, 0.32f, 0.15f), 0.6f, 0.7f), new Vector3(0f, 0.6f, -d * 0.5f - 2.8f), default, Vector3.one * 1.2f);
        return g;
    }

    // ── A hill you can climb: a cone with a flat top, naga stairs up its south face ─────────────
    public static Transform Hill(Transform p, Vector3 at, float yaw, float radius, float height, float topRadius, out Vector3 summit)
    {
        var g = G("Hill_เขา", p, at, yaw);
        string key = $"PK_Hill_{radius:0.0}_{height:0.0}_{topRadius:0.0}";
        var cone = MeshKit.Lathe(key, new[] { V(radius, -0.2f), V(radius, 0f), V(topRadius, height) }, 36);
        var hill = MeshKit.Place("Slope", g, cone, Forest, Vector3.zero);
        hill.AddComponent<MeshCollider>().sharedMesh = cone;
        PrimitiveKit.Cylinder("Summit", g, new Vector3(0f, height, 0f), new Vector3(topRadius * 2f, 0.06f, topRadius * 2f), Paving);
        // trees and rocks over the slope (not on the stairs)
        var rng = new System.Random((int)(radius * 13f + height));
        for (int i = 0; i < 26; i++)
        {
            float a = (float)rng.NextDouble() * 360f;
            if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 14f) continue;
            float f = 0.1f + (float)rng.NextDouble() * 0.75f;
            float r = Mathf.Lerp(radius, topRadius, f) - 0.6f;
            var pos = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, r) + Vector3.up * (height * f - 0.3f);
            if (i % 3 == 0) Nature.Rock(g, pos, 1f + (float)rng.NextDouble(), i);
            else Nature.Bush(g, pos, 1.6f + (float)rng.NextDouble() * 1.2f, i);
        }
        // the stairs: steps of 0.24 m (the CharacterController steps up them), naga rails either side
        const float rise = 0.24f;
        int n = Mathf.CeilToInt(height / rise);
        float run = (radius - topRadius) / n;
        var stairs = G("NagaStairs_บันไดนาค", g, Vector3.zero);
        for (int i = 0; i < n; i++)
        {
            float top = Mathf.Min(height, (i + 1) * rise);
            PrimitiveKit.Box("Step", stairs, new Vector3(0f, top * 0.5f, -radius + (i + 0.5f) * run), new Vector3(3.2f, top, run + 0.02f), Granite);
        }
        float len = Mathf.Sqrt(height * height + (radius - topRadius) * (radius - topRadius));
        float pitch = Mathf.Atan2(height, radius - topRadius) * Mathf.Rad2Deg;
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("NagaBody", stairs, new Vector3(s * 1.85f, height * 0.5f + 0.5f, -(radius + topRadius) * 0.5f), new Vector3(0.45f, 0.5f, len), Gold, new Vector3(-pitch, 0f, 0f));
            var head = G("NagaHead", stairs, new Vector3(s * 1.85f, 0f, -radius - 0.4f));
            PrimitiveKit.Cylinder("Neck", head, new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0.5f), Gold, collider: false);
            for (int k = -2; k <= 2; k++) // the fanned hood of heads
                PrimitiveKit.Sphere("Head", head, new Vector3(k * 0.3f, 1.8f - Mathf.Abs(k) * 0.18f, -0.15f), new Vector3(0.38f, 0.45f, 0.5f), Gold);
        }
        summit = new Vector3(0f, height, 0f);
        return g;
    }

    // ── Waterfall: a rocky cliff, the water down it in steps, a pool at the foot ────────────────
    public static Transform Waterfall(Transform p, Vector3 at, float yaw, float height, int tiers)
    {
        var g = G("Waterfall_น้ำตก", p, at, yaw);
        for (int i = 0; i < 9; i++) // the cliff: big boulders heaped behind
        {
            float x = (i - 4) * 3.4f, h = height * (1f - Mathf.Abs(i - 4) * 0.12f);
            var blob = MeshKit.Blob("PK_Boulder" + (i % 4), 100 + i % 4, 0.22f);
            var r = MeshKit.Place("Cliff", g, blob, Rock, new Vector3(x, h * 0.45f, 6f + Mathf.Abs(i - 4) * 0.8f), new Vector3(0f, i * 37f, 0f), new Vector3(5.5f, h, 5f));
            r.AddComponent<MeshCollider>().sharedMesh = blob;
        }
        float step = height / tiers;
        for (int i = 0; i < tiers; i++)
        {
            float top = height - i * step, z = 3.6f - i * 1.1f, w = 3f + i * 0.9f;
            PrimitiveKit.Box("Fall", g, new Vector3(0f, top - step * 0.5f, z), new Vector3(w, step, 0.12f), Fall, new Vector3(-8f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Ledge", g, new Vector3(0f, top - step, z - 0.5f), new Vector3(w + 2.4f, 0.6f, 1.4f), Rock, collider: false);
            PrimitiveKit.Box("Foam", g, new Vector3(0f, top - step + 0.32f, z - 0.4f), new Vector3(w + 0.6f, 0.1f, 1.1f), Mat("PK_Foam", Color.white, 0.6f), collider: false);
        }
        PrimitiveKit.Cylinder("Pool", g, new Vector3(0f, 0.03f, -2f), new Vector3(14f, 0.02f, 9f), Water, collider: false);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Nature.Rock(g, new Vector3(Mathf.Cos(a) * 7.4f, 0f, -2f + Mathf.Sin(a) * 4.8f), 0.9f + (i % 3) * 0.3f, i + 7);
        }
        for (int i = 0; i < 8; i++)
            Places.ShadeTree(g, new Vector3((i < 4 ? -1f : 1f) * (10f + (i % 4) * 2.6f), 0f, -4f + (i % 4) * 3.2f), 0.9f + (i % 2) * 0.3f);
        return g;
    }

    // ── Statues ─────────────────────────────────────────────────────────────────────────────────
    // A standing Buddha (ปางประทานพร), `h` tall, on a lotus base
    public static Transform StandingBuddha(Transform p, Vector3 at, float yaw, float h)
    {
        var g = G("StandingBuddha_พระยืน", p, at, yaw);
        var gold = Gold;
        var body = G("Figure", g, Vector3.zero);
        body.localScale = Vector3.one * h;
        MeshKit.Place("Lotus", body, MeshKit.Lathe("PK_SBLotus", new[] { V(0.16f, 0f), V(0.19f, 0.03f), V(0.2f, 0.05f), V(0.12f, 0.07f) }, 24), gold, Vector3.zero);
        MeshKit.Place("Robe", body, MeshKit.Lathe("PK_SBRobe", new[]
        {
            V(0.09f, 0.07f), V(0.105f, 0.12f), V(0.1f, 0.3f), V(0.1f, 0.48f), V(0.115f, 0.62f), V(0.13f, 0.7f), V(0.11f, 0.76f), V(0.05f, 0.79f), V(0.04f, 0.8f),
        }, 24), gold, Vector3.zero, default, new Vector3(1f, 1f, 0.7f));
        PrimitiveKit.Sphere("Head", body, new Vector3(0f, 0.85f, 0f), new Vector3(0.11f, 0.13f, 0.11f), gold);
        MeshKit.Place("Ushnisha", body, MeshKit.Lathe("PK_SBFlame", new[] { V(0.035f, 0f), V(0.03f, 0.03f), V(0.012f, 0.08f), V(0.001f, 0.12f) }, 12), gold, new Vector3(0f, 0.9f, 0f));
        foreach (float s in new[] { -1f, 1f })
        {
            PrimitiveKit.Cylinder("Ear", body, new Vector3(s * 0.058f, 0.83f, 0f), new Vector3(0.018f, 0.04f, 0.03f), gold, collider: false);
            PrimitiveKit.Cylinder("Arm", body, new Vector3(s * 0.13f, 0.58f, 0f), new Vector3(0.045f, 0.13f, 0.045f), gold, new Vector3(0f, 0f, s * 6f), collider: false);
        }
        // right forearm forward, palm down and open: giving a blessing
        PrimitiveKit.Cylinder("Forearm", body, new Vector3(0.14f, 0.45f, -0.05f), new Vector3(0.04f, 0.07f, 0.04f), gold, new Vector3(-50f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Hand", body, new Vector3(0.14f, 0.4f, -0.11f), new Vector3(0.05f, 0.015f, 0.07f), gold, collider: false);
        var col = g.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, h * 0.45f, 0f);
        col.radius = h * 0.14f;
        col.height = h * 0.9f;
        return g;
    }

    // A golden mermaid sitting on a rock, combing her hair (นางเงือกทอง)
    public static Transform Mermaid(Transform p, Vector3 at, float yaw, float scale)
    {
        var g = G("Mermaid_นางเงือก", p, at, yaw);
        g.localScale = Vector3.one * scale;
        var rock = MeshKit.Blob("PK_MermaidRock", 77, 0.2f);
        MeshKit.Place("Rock", g, rock, Rock, new Vector3(0f, 0.35f, 0f), default, new Vector3(2.6f, 1.2f, 2f)).AddComponent<MeshCollider>().sharedMesh = rock;
        var gold = Gold;
        PrimitiveKit.Cylinder("Torso", g, new Vector3(0f, 1.45f, 0f), new Vector3(0.36f, 0.32f, 0.26f), gold, new Vector3(0f, 0f, -8f), collider: false);
        PrimitiveKit.Sphere("Head", g, new Vector3(-0.07f, 1.97f, 0f), Vector3.one * 0.28f, gold);
        PrimitiveKit.Box("Hair", g, new Vector3(0.02f, 1.75f, 0.12f), new Vector3(0.3f, 0.6f, 0.08f), gold, new Vector3(0f, 0f, -8f), collider: false);
        PrimitiveKit.Cylinder("ArmUp", g, new Vector3(0.18f, 1.75f, -0.05f), new Vector3(0.08f, 0.2f, 0.08f), gold, new Vector3(0f, 0f, -40f), collider: false);
        PrimitiveKit.Cylinder("ArmDown", g, new Vector3(-0.2f, 1.3f, -0.05f), new Vector3(0.08f, 0.2f, 0.08f), gold, new Vector3(0f, 0f, 20f), collider: false);
        // the tail curls down over the rock and round to the right
        var path = new[] { new Vector3(0f, 1.15f, 0f), new Vector3(0.2f, 1f, -0.15f), new Vector3(0.55f, 0.95f, -0.2f), new Vector3(0.85f, 0.8f, -0.05f), new Vector3(1.05f, 0.75f, 0.2f) };
        MeshKit.Place("Tail", g, MeshKit.Sweep("PK_MermaidTail", path, MeshKit.Taper(path.Length, 0.19f, 0.05f), 10), gold, Vector3.zero);
        PrimitiveKit.Box("Fin", g, new Vector3(1.15f, 0.8f, 0.3f), new Vector3(0.45f, 0.05f, 0.3f), gold, new Vector3(0f, 30f, 20f), collider: false);
        return g;
    }

    // A pink dolphin (โลมาสีชมพู) bobbing at the surface
    public static void Dolphin(Transform p, Vector3 at, float yaw)
    {
        var g = G("PinkDolphin_โลมาสีชมพู", p, at, yaw);
        var pink = Mat("PK_DolphinPink", new Color(0.97f, 0.66f, 0.7f), 0.75f);
        PrimitiveKit.Sphere("Body", g, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.6f, 2.2f), pink);
        PrimitiveKit.Cylinder("Snout", g, new Vector3(0f, 0.12f, 1.2f), new Vector3(0.18f, 0.25f, 0.18f), pink, new Vector3(90f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Dorsal", g, new Vector3(0f, 0.5f, -0.1f), new Vector3(0.06f, 0.4f, 0.4f), pink, new Vector3(-30f, 0f, 0f), collider: false);
        PrimitiveKit.Box("Flukes", g, new Vector3(0f, 0.15f, -1.15f), new Vector3(0.8f, 0.05f, 0.3f), pink, collider: false);
        g.gameObject.AddComponent<Bob>();
    }

    // ── Water: a pond or lake surface (walk round it: blocked) ──────────────────────────────────
    public static void Pond(Transform p, Vector3 at, Vector2 size, bool block = true, float yaw = 0f)
    {
        PrimitiveKit.Box("Water_บ่อน้ำ", p, at + Vector3.up * 0.03f, new Vector3(size.x, 0.02f, size.y), Water, new Vector3(0f, yaw, 0f), collider: false);
        PrimitiveKit.Box("Bank", p, at + Vector3.up * 0.01f, new Vector3(size.x + 1.2f, 0.02f, size.y + 1.2f), Mat("PK_Bank", new Color(0.55f, 0.47f, 0.33f)), new Vector3(0f, yaw, 0f), collider: false);
        if (block) Blocker(p, at + Vector3.up, new Vector3(size.x - 0.6f, 2f, size.y - 0.6f), yaw);
    }

    // ── Walls and gates ─────────────────────────────────────────────────────────────────────────
    // A brick city wall along local X with merlons, a gate with a Thai roof over the middle
    public static Transform CityWall(Transform p, Vector3 at, float yaw, float length, Material brick, string gateLabel)
    {
        var g = G("CityWall_กำแพงเมือง", p, at, yaw);
        const float h = 4.6f, gap = 4f;
        foreach (float s in new[] { -1f, 1f })
        {
            float half = (length * 0.5f - gap * 0.5f);
            float cx = s * (gap * 0.5f + half * 0.5f);
            PrimitiveKit.Box("Wall", g, new Vector3(cx, h * 0.5f, 0f), new Vector3(half, h, 2.2f), brick);
            for (float x = gap * 0.5f + 0.6f; x < length * 0.5f - 0.4f; x += 1.6f)
                PrimitiveKit.Box("Merlon", g, new Vector3(s * x, h + 0.45f, 0f), new Vector3(0.8f, 0.9f, 2.2f), brick, collider: false);
        }
        PrimitiveKit.Box("Lintel", g, new Vector3(0f, h + 0.6f, 0f), new Vector3(gap + 2f, 1.6f, 3f), brick, collider: false);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        MeshKit.Place("GateRoof", g, gable, RoofRed, new Vector3(-(gap + 3f) * 0.5f, h + 1.4f, 0f), new Vector3(0f, 90f, 0f), new Vector3(4.6f, 2.2f, gap + 3f));
        PrimitiveKit.Box("GateBoard", g, new Vector3(0f, h + 0.6f, -1.55f), new Vector3(gap, 0.9f, 0.08f), Mat("PK_BoardBlack", new Color(0.08f, 0.08f, 0.08f), 0.6f), collider: false);
        PrimitiveKit.Label(gateLabel, g, new Vector3(0f, h + 0.6f, -1.62f), 0.04f, new Color(1f, 0.82f, 0.3f));
        return g;
    }

    // A small star fort with corner bastions and old cannons (ป้อมปืน)
    public static Transform Fort(Transform p, Vector3 at, float yaw, float size)
    {
        var g = G("Fort_ป้อม", p, at, yaw);
        float half = size * 0.5f;
        var stone = Mat("PK_FortStone", new Color(0.72f, 0.66f, 0.55f), 0.1f);
        var iron = Mat("PK_CannonIron", new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.6f);
        PrimitiveKit.Box("Floor", g, new Vector3(0f, 0.6f, 0f), new Vector3(size, 1.2f, size), stone);
        PrimitiveKit.Box("Ramp", g, new Vector3(0f, 0.6f, -half - 2.4f), new Vector3(2.4f, 0.1f, 5.2f), stone, new Vector3(-13f, 0f, 0f));
        for (int k = 0; k < 4; k++)
        {
            var q = Quaternion.Euler(0f, k * 90f, 0f);
            bool front = k == 0;
            // parapet along each side (a gap in the front for the ramp)
            if (front)
                foreach (float s in new[] { -1f, 1f })
                    PrimitiveKit.Box("Parapet", g, new Vector3(s * (half * 0.5f + 0.8f), 1.8f, -half + 0.4f), new Vector3(half - 1.6f, 1.2f, 0.8f), stone);
            else
                PrimitiveKit.Box("Parapet", g, q * new Vector3(0f, 0f, -half + 0.4f) + Vector3.up * 1.8f, new Vector3(size, 1.2f, 0.8f), stone, new Vector3(0f, k * 90f, 0f));
            var corner = Quaternion.Euler(0f, 45f + k * 90f, 0f) * new Vector3(0f, 0f, half * 1.41f);
            PrimitiveKit.Box("Bastion", g, corner + Vector3.up * 1.1f, new Vector3(3.6f, 2.2f, 3.6f), stone, new Vector3(0f, 45f, 0f));
            var cannon = G("Cannon_ปืนใหญ่", g, corner * 0.82f + Vector3.up * 1.2f, 45f + k * 90f + 180f);
            PrimitiveKit.Box("Carriage", cannon, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.5f, 1.4f), Wood, collider: false);
            PrimitiveKit.Cylinder("Barrel", cannon, new Vector3(0f, 0.75f, -0.5f), new Vector3(0.36f, 1.1f, 0.36f), iron, new Vector3(80f, 0f, 0f), collider: false);
        }
        return g;
    }

    // ── Houses and streets ──────────────────────────────────────────────────────────────────────
    // A row of two-storey Sino-Portuguese shophouses (ชิโน-โปรตุกีส), arched windows, pastel walls
    public static Transform SinoRow(Transform p, Vector3 at, float yaw, int units, string[] signs)
    {
        var g = G("SinoRow_ตึกชิโน", p, at, yaw);
        Color[] wall = { new Color(0.98f, 0.86f, 0.55f), new Color(0.62f, 0.85f, 0.82f), new Color(0.98f, 0.72f, 0.65f), new Color(0.82f, 0.88f, 0.7f), new Color(0.95f, 0.95f, 0.9f) };
        var trim = Mat("PK_SinoTrim", new Color(0.98f, 0.97f, 0.93f), 0.3f);
        var shutter = Mat("PK_ShutterGreen", new Color(0.2f, 0.45f, 0.38f), 0.3f);
        var tile = Mat("PK_TileTerracotta", new Color(0.7f, 0.33f, 0.2f), 0.2f);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        const float uw = 4.4f, d = 9f, h = 7f;
        for (int i = 0; i < units; i++)
        {
            float x = (i - (units - 1) * 0.5f) * uw;
            var m = Mat("PK_Sino" + (i % wall.Length), wall[i % wall.Length], 0.25f);
            PrimitiveKit.Box("Unit", g, new Vector3(x, h * 0.5f, d * 0.5f), new Vector3(uw - 0.05f, h, d), m);
            PrimitiveKit.Box("Pilaster", g, new Vector3(x - uw * 0.5f + 0.15f, h * 0.5f, -0.06f), new Vector3(0.3f, h, 0.14f), trim, collider: false);
            PrimitiveKit.Box("FiveFootWay", g, new Vector3(x, 0.08f, -1.3f), new Vector3(uw, 0.16f, 2.6f), Paving, collider: false);
            PrimitiveKit.Box("Doors", g, new Vector3(x, 1.3f, -0.03f), new Vector3(uw * 0.62f, 2.5f, 0.06f), shutter, collider: false);
            foreach (float s in new[] { -1f, 1f }) // two arched windows upstairs
            {
                PrimitiveKit.Box("Window", g, new Vector3(x + s * 0.95f, 4.5f, -0.03f), new Vector3(0.8f, 1.5f, 0.06f), shutter, collider: false);
                PrimitiveKit.Cylinder("Arch", g, new Vector3(x + s * 0.95f, 5.25f, -0.04f), new Vector3(0.8f, 0.03f, 0.8f), trim, new Vector3(90f, 0f, 0f), collider: false);
            }
            PrimitiveKit.Box("Cornice", g, new Vector3(x, h - 0.4f, -0.1f), new Vector3(uw, 0.3f, 0.3f), trim, collider: false);
            if (signs != null && i < signs.Length && !string.IsNullOrEmpty(signs[i]))
            {
                PrimitiveKit.Box("SignBoard", g, new Vector3(x, 3f, -0.08f), new Vector3(uw * 0.8f, 0.55f, 0.06f), trim, collider: false);
                PrimitiveKit.Label(signs[i], g, new Vector3(x, 3f, -0.13f), 0.03f, new Color(0.35f, 0.15f, 0.08f));
            }
        }
        MeshKit.Place("Roof", g, gable, tile, new Vector3(-units * uw * 0.5f - 0.2f, h, d * 0.5f), new Vector3(0f, 90f, 0f), new Vector3(d + 0.8f, 2f, units * uw + 0.4f));
        return g;
    }

    // A raised wooden Thai house (เรือนไทยใต้ถุนสูง)
    public static Transform StiltHouse(Transform p, Vector3 at, float yaw, float w, float d)
    {
        var g = G("StiltHouse_เรือนไม้", p, at, yaw);
        var plank = Mat("PK_Plank", new Color(0.55f, 0.37f, 0.22f), 0.2f);
        var roof = Mat("PK_RoofTerracotta", new Color(0.62f, 0.3f, 0.18f), 0.2f);
        var gable = MeshKit.Gable("PK_Gable", 1f, 1f, 1f);
        for (int i = 0; i < 6; i++)
            PrimitiveKit.Cylinder("Stilt", g, new Vector3((i % 3 - 1) * (w * 0.45f), 1f, (i < 3 ? -1f : 1f) * d * 0.45f), new Vector3(0.25f, 1f, 0.25f), Wood);
        PrimitiveKit.Box("Floor", g, new Vector3(0f, 2.05f, 0f), new Vector3(w + 1f, 0.15f, d + 1f), plank);
        PrimitiveKit.Box("Walls", g, new Vector3(0f, 3.4f, 0.3f), new Vector3(w, 2.6f, d - 0.6f), plank);
        PrimitiveKit.Box("Door", g, new Vector3(0f, 3.1f, 0.3f - (d - 0.6f) * 0.5f - 0.03f), new Vector3(1f, 2f, 0.06f), Dark, collider: false);
        MeshKit.Place("Roof", g, gable, roof, new Vector3(0f, 4.6f, -(d + 1.6f) * 0.5f), default, new Vector3(w + 1.4f, 2.4f, d + 1.6f));
        PrimitiveKit.Box("Ladder", g, new Vector3(0f, 1f, -d * 0.5f - 1.3f), new Vector3(1.2f, 0.1f, 2.6f), plank, new Vector3(-38f, 0f, 0f), collider: false);
        return g;
    }

    // A market stall: counter, goods, a striped awning
    public static void Stall(Transform p, Vector3 at, float yaw, Color awning, Color goods)
    {
        var g = G("Stall_แผงลอย", p, at, yaw);
        PrimitiveKit.Box("Counter", g, new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 1f), Wood);
        PrimitiveKit.Box("Goods", g, new Vector3(0f, 1f, 0f), new Vector3(1.9f, 0.2f, 0.8f), Mat("PK_Goods" + ColorUtility.ToHtmlStringRGB(goods), goods, 0.3f), collider: false);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -0.6f, 0.6f })
                PrimitiveKit.Cylinder("Pole", g, new Vector3(x, 1.2f, z), new Vector3(0.06f, 1.2f, 0.06f), Steel, collider: false);
        PrimitiveKit.Box("Awning", g, new Vector3(0f, 2.45f, 0f), new Vector3(2.5f, 0.06f, 1.7f), Mat("PK_Awning" + ColorUtility.ToHtmlStringRGB(awning), awning, 0.3f), new Vector3(-8f, 0f, 0f), collider: false);
    }

    // Strings of little lights between poles (for walking streets and night markets)
    public static void FairyLights(Transform p, Vector3 from, Vector3 to, float height)
    {
        var bulb = PrimitiveKit.Glow("PK_FairyBulb", new Color(1f, 0.85f, 0.45f), 3f);
        int n = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(from, to) / 1.2f));
        for (int i = 0; i <= n; i++)
        {
            float f = i / (float)n;
            var q = Vector3.Lerp(from, to, f) + Vector3.up * (height - Mathf.Sin(f * Mathf.PI) * 0.6f);
            PrimitiveKit.Sphere("Bulb", p, q, Vector3.one * 0.14f, bulb);
        }
    }

    // ── Sport ─────────────────────────────────────────────────────────────────────────────────
    // A football stadium: the pitch with its lines, four stands with roofs, floodlights (L along X)
    public static Transform Stadium(Transform p, Vector3 at, float yaw, float L, float W, string name, Color seats)
    {
        var g = G("Stadium_สนามฟุตบอล", p, at, yaw);
        var turf = Mat("PK_Turf", new Color(0.24f, 0.6f, 0.24f), 0.1f);
        var turf2 = Mat("PK_Turf2", new Color(0.28f, 0.66f, 0.27f), 0.1f);
        var seat = Mat("PK_Seats" + ColorUtility.ToHtmlStringRGB(seats), seats, 0.4f);
        var seat2 = Mat("PK_Seats2" + ColorUtility.ToHtmlStringRGB(seats), new Color(seats.r * 0.8f, seats.g * 0.8f, seats.b * 0.8f), 0.4f);
        var roof = Mat("PK_StandRoof", new Color(0.92f, 0.93f, 0.95f), 0.5f, 0.3f);
        var flood = PrimitiveKit.Glow("PK_Floodlight", new Color(1f, 1f, 0.92f), 3f);
        for (int i = 0; i < 10; i++) // mowing stripes
            PrimitiveKit.Box("Turf", g, new Vector3(-L * 0.5f + (i + 0.5f) * L / 10f, 0.015f, 0f), new Vector3(L / 10f, 0.02f, W), i % 2 == 0 ? turf : turf2, collider: false);
        void Line(Vector3 c, Vector3 s) => PrimitiveKit.Box("PitchLine", g, c + Vector3.up * 0.03f, s, Paint, collider: false);
        float pl = L - 4f, pw = W - 4f;
        Line(new Vector3(0f, 0f, -pw * 0.5f), new Vector3(pl, 0.01f, 0.15f));
        Line(new Vector3(0f, 0f, pw * 0.5f), new Vector3(pl, 0.01f, 0.15f));
        Line(new Vector3(-pl * 0.5f, 0f, 0f), new Vector3(0.15f, 0.01f, pw));
        Line(new Vector3(pl * 0.5f, 0f, 0f), new Vector3(0.15f, 0.01f, pw));
        Line(Vector3.zero, new Vector3(0.15f, 0.01f, pw));
        for (int i = 0; i < 28; i++)
        {
            float a = i * Mathf.PI * 2f / 28f;
            PrimitiveKit.Box("CentreCircle", g, new Vector3(Mathf.Cos(a) * 4.5f, 0.03f, Mathf.Sin(a) * 4.5f), new Vector3(0.15f, 0.01f, 1.05f), Paint, new Vector3(0f, -a * Mathf.Rad2Deg, 0f), collider: false);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            Line(new Vector3(s * (pl * 0.5f - 6f), 0f, 0f), new Vector3(0.15f, 0.01f, 16f));
            Line(new Vector3(s * (pl * 0.5f - 3f), 0f, -8f), new Vector3(6f, 0.01f, 0.15f));
            Line(new Vector3(s * (pl * 0.5f - 3f), 0f, 8f), new Vector3(6f, 0.01f, 0.15f));
            var goal = G("Goal", g, new Vector3(s * pl * 0.5f, 0f, 0f));
            foreach (float z in new[] { -2.4f, 2.4f })
                PrimitiveKit.Cylinder("Post", goal, new Vector3(0f, 1.2f, z), new Vector3(0.12f, 1.2f, 0.12f), Paint);
            PrimitiveKit.Cylinder("Bar", goal, new Vector3(0f, 2.4f, 0f), new Vector3(0.12f, 2.4f, 0.12f), Paint, new Vector3(90f, 0f, 0f), collider: false);
            PrimitiveKit.Box("Net", goal, new Vector3(s * 0.9f, 1.2f, 0f), new Vector3(0.05f, 2.4f, 4.8f), Mat("PK_Net", new Color(0.9f, 0.9f, 0.9f), 0.2f), collider: false);
        }
        // the stands: tiers of seats stepping up and back, a roof over each, gaps at the corners
        for (int side = 0; side < 4; side++)
        {
            bool longSide = side < 2;
            float s = side % 2 == 0 ? -1f : 1f;
            float len = longSide ? L - 2f : W - 2f, edge = longSide ? W * 0.5f + 1.2f : L * 0.5f + 1.2f;
            var stand = G(longSide ? "MainStand" : "EndStand", g, longSide ? new Vector3(0f, 0f, s * edge) : new Vector3(s * edge, 0f, 0f), longSide ? (s > 0 ? 0f : 180f) : (s > 0 ? 90f : -90f));
            // the stand's local -Z faces the pitch
            for (int k = 0; k < 8; k++)
                PrimitiveKit.Box("Tier", stand, new Vector3(0f, (k + 1) * 0.45f, 0.55f + k * 1.1f), new Vector3(len, (k + 1) * 0.9f, 1.1f), k % 2 == 0 ? seat : seat2);
            PrimitiveKit.Box("BackWall", stand, new Vector3(0f, 4.6f, 9.1f), new Vector3(len, 9.2f, 0.4f), Concrete);
            PrimitiveKit.Box("Roof", stand, new Vector3(0f, 10.4f, 5f), new Vector3(len + 1f, 0.3f, 10f), roof, new Vector3(-6f, 0f, 0f), collider: false);
            for (float x = -len * 0.5f + 2f; x <= len * 0.5f - 1.9f; x += Mathf.Max(6f, (len - 4f) / 4f))
                PrimitiveKit.Cylinder("RoofColumn", stand, new Vector3(x, 5f, 9.4f), new Vector3(0.4f, 5f, 0.4f), Steel, collider: false);
            if (longSide)
            {
                PrimitiveKit.Label(name, stand, new Vector3(0f, 7.5f, 9.35f), 0.12f, Color.white, 180f); // outside, on the back wall
                PrimitiveKit.Label(name, stand, new Vector3(0f, 9.6f, 0.2f), 0.1f, new Color(1f, 0.85f, 0.2f));
            }
        }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var c = new Vector3(sx * (L * 0.5f + 5f), 0f, sz * (W * 0.5f + 5f));
                PrimitiveKit.Cylinder("FloodMast", g, c + Vector3.up * 9f, new Vector3(0.5f, 9f, 0.5f), Steel);
                PrimitiveKit.Box("FloodLamps", g, c + Vector3.up * 18.4f, new Vector3(3f, 1.6f, 0.4f), flood, new Vector3(20f, Mathf.Atan2(-sx, -sz) * Mathf.Rad2Deg, 0f), collider: false);
            }
        return g;
    }

    // Rounded corners for a closed polyline (each corner replaced by an arc of `r`)
    public static Vector3[] Smooth(Vector3[] corners, float r, int steps = 6)
    {
        var o = new List<Vector3>();
        int n = corners.Length;
        for (int i = 0; i < n; i++)
        {
            Vector3 a = corners[(i + n - 1) % n], c = corners[i], b = corners[(i + 1) % n];
            float ra = Mathf.Min(r, Vector3.Distance(a, c) * 0.45f), rb = Mathf.Min(r, Vector3.Distance(b, c) * 0.45f);
            var p0 = c + (a - c).normalized * ra;
            var p1 = c + (b - c).normalized * rb;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                o.Add((1 - t) * (1 - t) * p0 + 2f * (1 - t) * t * c + t * t * p1);
            }
        }
        return o.ToArray();
    }

    // A motor-racing circuit along `loop` (local, closed): asphalt, kerbs, start line, grandstand,
    // pit garages, and race cars lapping it (RaceLap)
    public static Transform Circuit(Transform p, Vector3 at, float yaw, Vector3[] loop, float width, string name)
    {
        var g = G("Circuit_สนามแข่งรถ", p, at, yaw);
        var kerbRed = Mat("PK_KerbRed", new Color(0.85f, 0.12f, 0.1f), 0.3f);
        int n = loop.Length;
        for (int i = 0; i < n; i++)
        {
            Vector3 a = loop[i], b = loop[(i + 1) % n];
            var d = b - a;
            float yawSeg = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            PrimitiveKit.Box("Track", g, (a + b) * 0.5f + Vector3.up * 0.025f, new Vector3(width, 0.03f, d.magnitude + 0.6f), Asphalt, new Vector3(0f, yawSeg, 0f), collider: false);
            var side = new Vector3(d.z, 0f, -d.x).normalized;
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box(i % 2 == 0 ? "Kerb" : "KerbW", g, (a + b) * 0.5f + side * s * (width * 0.5f + 0.3f) + Vector3.up * 0.04f, new Vector3(0.6f, 0.05f, d.magnitude + 0.1f), i % 2 == 0 ? kerbRed : Paint, new Vector3(0f, yawSeg, 0f), collider: false);
        }
        // start / finish on the first straight: chequered line and a gantry
        // on the longest straight, whichever way round the loop is drawn
        int longest = 0;
        for (int i = 1; i < n; i++)
            if (Vector3.Distance(loop[i], loop[(i + 1) % n]) > Vector3.Distance(loop[longest], loop[(longest + 1) % n])) longest = i;
        Vector3 s0 = loop[longest], s1 = loop[(longest + 1) % n];
        var centroid = Vector3.zero;
        foreach (var q in loop) centroid += q / n;
        var dir = (s1 - s0).normalized;
        var mid = (s0 + s1) * 0.5f;
        float startYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        var black = Mat("PK_Black", new Color(0.05f, 0.05f, 0.05f), 0.3f);
        for (int i = 0; i < 12; i++)
            for (int j = 0; j < 2; j++)
                PrimitiveKit.Box("Chequer", g, mid + Quaternion.Euler(0f, startYaw, 0f) * new Vector3(-width * 0.5f + (i + 0.5f) * width / 12f, 0.045f, j * 0.5f), new Vector3(width / 12f, 0.01f, 0.5f), (i + j) % 2 == 0 ? black : Paint, new Vector3(0f, startYaw, 0f), collider: false);
        var gantry = G("StartGantry", g, mid, startYaw);
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Leg", gantry, new Vector3(sx * (width * 0.5f + 1f), 3f, 0f), new Vector3(0.4f, 3f, 0.4f), Steel);
        PrimitiveKit.Box("Beam", gantry, new Vector3(0f, 6.2f, 0f), new Vector3(width + 2.4f, 0.9f, 0.6f), black, collider: false);
        PrimitiveKit.Label(name, gantry, new Vector3(0f, 6.2f, -0.35f), 0.06f, Color.white);
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Sphere("StartLight", gantry, new Vector3(-2f + i, 5.4f, -0.2f), Vector3.one * 0.4f, PrimitiveKit.Glow("PK_StartRed", new Color(1f, 0.1f, 0.05f), 2f));
        // grandstand on the outside of the main straight, pit garages on the inside
        var outward = new Vector3(-dir.z, 0f, dir.x);
        if (Vector3.Dot(outward, mid - centroid) < 0f) outward = -outward;
        var stand = G("Grandstand_อัฒจันทร์", g, mid + outward * (width * 0.5f + 4f), Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
        var seat = Mat("PK_GrandstandBlue", new Color(0.12f, 0.35f, 0.75f), 0.4f);
        float len = Mathf.Min(40f, Vector3.Distance(s0, s1) * 0.7f);
        for (int k = 0; k < 7; k++)
            PrimitiveKit.Box("Tier", stand, new Vector3(0f, (k + 1) * 0.45f, 0.55f + k * 1.1f), new Vector3(len, (k + 1) * 0.9f, 1.1f), k % 2 == 0 ? seat : Concrete);
        PrimitiveKit.Box("Roof", stand, new Vector3(0f, 8.4f, 4f), new Vector3(len + 1f, 0.3f, 9f), Mat("PK_StandRoof", new Color(0.92f, 0.93f, 0.95f), 0.5f, 0.3f), new Vector3(-6f, 0f, 0f), collider: false);
        for (float x = -len * 0.5f + 1f; x <= len * 0.5f; x += len / 4f)
            PrimitiveKit.Cylinder("Column", stand, new Vector3(x, 4f, 8.2f), new Vector3(0.35f, 4f, 0.35f), Steel, collider: false);
        var pits = G("PitBuilding_พิท", g, mid - outward * (width * 0.5f + 6f), Mathf.Atan2(-outward.x, -outward.z) * Mathf.Rad2Deg);
        PrimitiveKit.Box("Pits", pits, new Vector3(0f, 2.5f, 3f), new Vector3(len, 5f, 6f), Mat("PK_PitWhite", new Color(0.94f, 0.95f, 0.96f), 0.4f));
        Color[] doors = { new Color(0.85f, 0.1f, 0.1f), new Color(0.1f, 0.3f, 0.8f), new Color(0.95f, 0.75f, 0.1f), new Color(0.1f, 0.6f, 0.3f) };
        for (int i = 0; i < 8; i++)
            PrimitiveKit.Box("Garage", pits, new Vector3(-len * 0.5f + (i + 0.5f) * len / 8f, 1.4f, -0.03f), new Vector3(len / 8f - 0.6f, 2.8f, 0.06f), Mat("PK_Garage" + (i % 4), doors[i % 4], 0.5f), collider: false);
        PrimitiveKit.Label("PIT LANE · " + name, pits, new Vector3(0f, 4.2f, -0.05f), 0.06f, new Color(0.1f, 0.15f, 0.3f));
        // the cars
        Color[] livery = { new Color(0.9f, 0.08f, 0.08f), new Color(0.08f, 0.25f, 0.85f), new Color(1f, 0.75f, 0.05f), new Color(0.05f, 0.6f, 0.3f), new Color(0.95f, 0.95f, 0.95f) };
        float lap = 0f;
        for (int i = 0; i < n; i++) lap += Vector3.Distance(loop[i], loop[(i + 1) % n]);
        for (int i = 0; i < livery.Length; i++)
        {
            var car = RaceCar(g, livery[i], i);
            car.position = g.TransformPoint(loop[0]);
            car.gameObject.AddComponent<RaceLap>().Configure(loop, 24f + i * 1.3f, 10f + i * 0.4f, lap * i / livery.Length);
        }
        return g;
    }

    private static Transform RaceCar(Transform p, Color c, int i)
    {
        var g = G("RaceCar_รถแข่ง" + i, p, Vector3.zero);
        var body = Mat("PK_RaceCar" + ColorUtility.ToHtmlStringRGB(c), c, 0.85f, 0.3f);
        var tyre = Mat("PK_Tyre", new Color(0.06f, 0.06f, 0.06f), 0.2f);
        PrimitiveKit.Box("Tub", g, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.4f, 3.4f), body, collider: false);
        PrimitiveKit.Box("Nose", g, new Vector3(0f, 0.32f, 2f), new Vector3(0.45f, 0.22f, 1.2f), body, new Vector3(4f, 0f, 0f), collider: false);
        PrimitiveKit.Box("FrontWing", g, new Vector3(0f, 0.18f, 2.55f), new Vector3(1.9f, 0.06f, 0.45f), body, collider: false);
        PrimitiveKit.Box("RearWing", g, new Vector3(0f, 0.95f, -1.75f), new Vector3(1.5f, 0.08f, 0.45f), body, collider: false);
        PrimitiveKit.Box("Sidepods", g, new Vector3(0f, 0.38f, -0.3f), new Vector3(1.6f, 0.34f, 1.6f), body, collider: false);
        PrimitiveKit.Sphere("Helmet", g, new Vector3(0f, 0.75f, 0.3f), Vector3.one * 0.32f, Mat("PK_Helmet", new Color(0.95f, 0.95f, 0.2f), 0.8f));
        foreach (float x in new[] { -0.85f, 0.85f })
            foreach (float z in new[] { 1.35f, -1.25f })
                PrimitiveKit.Cylinder("Wheel", g, new Vector3(x, 0.33f, z), new Vector3(0.66f, 0.18f, 0.66f), tyre, new Vector3(0f, 0f, 90f), collider: false);
        return g;
    }

    // ── Bridge over water: concrete deck on ramps, two cable-stayed pylons (along local Z) ───────
    public static Transform CableBridge(Transform p, Vector3 at, float yaw, float length, float deckHeight)
    {
        var g = G("Bridge_สะพาน", p, at, yaw);
        const float w = 5f, ramp = 14f;
        float half = length * 0.5f, pitch = Mathf.Atan2(deckHeight, ramp) * Mathf.Rad2Deg, slope = Mathf.Sqrt(ramp * ramp + deckHeight * deckHeight);
        foreach (float s in new[] { -1f, 1f })
            PrimitiveKit.Box("Ramp", g, new Vector3(0f, deckHeight * 0.5f - 0.15f, s * (half - ramp * 0.5f)), new Vector3(w, 0.3f, slope), Concrete, new Vector3(s * pitch, 0f, 0f));
        PrimitiveKit.Box("Deck", g, new Vector3(0f, deckHeight - 0.15f, 0f), new Vector3(w, 0.3f, length - ramp * 2f + 0.2f), Concrete);
        PrimitiveKit.Box("Asphalt", g, new Vector3(0f, deckHeight + 0.01f, 0f), new Vector3(w - 1.2f, 0.02f, length - ramp * 2f), Asphalt, collider: false);
        foreach (float x in new[] { -1f, 1f })
        {
            PrimitiveKit.Box("Rail", g, new Vector3(x * (w * 0.5f - 0.1f), deckHeight + 0.55f, 0f), new Vector3(0.15f, 0.9f, length - ramp * 2f), Steel);
            foreach (float s in new[] { -1f, 1f })
                PrimitiveKit.Box("RampRail", g, new Vector3(x * (w * 0.5f - 0.1f), deckHeight * 0.5f + 0.55f, s * (half - ramp * 0.5f)), new Vector3(0.15f, 0.9f, slope), Steel, new Vector3(s * pitch, 0f, 0f));
        }
        for (float z = -half + ramp + 6f; z < half - ramp - 3f; z += 12f) // piers into the water
            PrimitiveKit.Box("Pier", g, new Vector3(0f, deckHeight * 0.5f - 0.3f, z), new Vector3(2.4f, deckHeight - 0.4f, 1.6f), Concrete, collider: false);
        float pylonH = deckHeight + 16f;
        foreach (float pz in new[] { -length * 0.18f, length * 0.18f })
        {
            foreach (float x in new[] { -1f, 1f })
                PrimitiveKit.Box("Pylon", g, new Vector3(x * (w * 0.5f + 0.6f), pylonH * 0.5f, pz), new Vector3(0.9f, pylonH, 1.4f), Concrete);
            PrimitiveKit.Box("PylonTie", g, new Vector3(0f, pylonH - 1f, pz), new Vector3(w + 2.1f, 0.8f, 1.2f), Concrete, collider: false);
            for (int k = 1; k <= 5; k++) // the stays fan out both ways
                foreach (float dz in new[] { -1f, 1f })
                    foreach (float x in new[] { -1f, 1f })
                    {
                        var top = new Vector3(x * (w * 0.5f + 0.6f), pylonH - 1.4f - k * 0.5f, pz);
                        var foot = new Vector3(x * (w * 0.5f - 0.1f), deckHeight + 0.2f, pz + dz * k * 4.2f);
                        var d = foot - top;
                        PrimitiveKit.Box("Stay", g, (top + foot) * 0.5f, new Vector3(0.07f, 0.07f, d.magnitude), Mat("PK_Stay", new Color(0.95f, 0.95f, 0.95f), 0.5f), Quaternion.LookRotation(d).eulerAngles, collider: false);
                    }
        }
        foreach (float z in new[] { -half + ramp + 2f, half - ramp - 2f })
            StreetFurniture.LampPost(g, new Vector3(w * 0.5f + 0.4f, deckHeight, z));
        return g;
    }

    // ── Birds ─────────────────────────────────────────────────────────────────────────────────
    // A sarus crane (นกกระเรียนพันธุ์ไทย): grey body, long neck, red head, long legs
    public static void Crane(Transform p, Vector3 at, float yaw, bool feeding)
    {
        var g = G("Crane_นกกระเรียน", p, at, yaw);
        var grey = Mat("PK_CraneGrey", new Color(0.66f, 0.68f, 0.72f), 0.3f);
        var red = Mat("PK_CraneRed", new Color(0.85f, 0.15f, 0.12f), 0.3f);
        var leg = Mat("PK_CraneLeg", new Color(0.8f, 0.55f, 0.55f), 0.2f);
        foreach (float x in new[] { -0.12f, 0.12f })
            PrimitiveKit.Cylinder("Leg", g, new Vector3(x, 0.55f, 0f), new Vector3(0.05f, 0.55f, 0.05f), leg, collider: false);
        PrimitiveKit.Sphere("Body", g, new Vector3(0f, 1.3f, 0f), new Vector3(0.55f, 0.5f, 0.95f), grey);
        PrimitiveKit.Box("Tail", g, new Vector3(0f, 1.25f, -0.55f), new Vector3(0.4f, 0.25f, 0.35f), Mat("PK_CraneTail", new Color(0.45f, 0.47f, 0.5f), 0.3f), new Vector3(20f, 0f, 0f), collider: false);
        float neckPitch = feeding ? 120f : 15f;
        var neckTop = new Vector3(0f, 1.45f, 0.35f) + Quaternion.Euler(neckPitch, 0f, 0f) * new Vector3(0f, 0.8f, 0f);
        PrimitiveKit.Cylinder("Neck", g, (new Vector3(0f, 1.45f, 0.35f) + neckTop) * 0.5f, new Vector3(0.09f, 0.4f, 0.09f), grey, new Vector3(neckPitch, 0f, 0f), collider: false);
        PrimitiveKit.Sphere("Head", g, neckTop, Vector3.one * 0.18f, red);
        PrimitiveKit.Cylinder("Beak", g, neckTop + Quaternion.Euler(feeding ? 170f : 85f, 0f, 0f) * new Vector3(0f, 0.15f, 0f), new Vector3(0.04f, 0.13f, 0.04f), Mat("PK_Beak", new Color(0.3f, 0.3f, 0.3f), 0.4f), new Vector3(feeding ? 170f : 85f, 0f, 0f), collider: false);
    }

    // ── Islands out at sea (scenery) ────────────────────────────────────────────────────────────
    public static void Island(Transform p, Vector3 at, Vector3 size, int seed)
    {
        var rock = MeshKit.Blob("PK_Island" + seed, 300 + seed, 0.25f);
        MeshKit.Place("IslandRock", p, rock, Rock, at + Vector3.up * size.y * 0.15f, new Vector3(0f, seed * 40f, 0f), new Vector3(size.x, size.y * 0.5f, size.z));
        MeshKit.Place("IslandGreen", p, rock, Forest, at + Vector3.up * size.y * 0.35f, new Vector3(0f, seed * 40f + 20f, 0f), new Vector3(size.x * 0.85f, size.y * 0.55f, size.z * 0.85f));
    }

    // A big distant mountain (backdrop beyond the edge of town)
    public static void Mountain(Transform p, Vector3 at, Vector3 size, int seed, string name)
    {
        var g = G("Mountain_" + name, p, at);
        var blob = MeshKit.Blob("PK_Mountain" + seed, 500 + seed, 0.28f, 2);
        MeshKit.Place("Mass", g, blob, Forest, Vector3.zero, new Vector3(0f, seed * 23f, 0f), size);
        MeshKit.Place("Shoulder", g, blob, Mat("PK_ForestFar", new Color(0.28f, 0.5f, 0.33f), 0.1f), new Vector3(size.x * 0.35f, -size.y * 0.15f, size.z * 0.1f), new Vector3(0f, seed * 23f + 90f, 0f), size * 0.7f);
    }
}
