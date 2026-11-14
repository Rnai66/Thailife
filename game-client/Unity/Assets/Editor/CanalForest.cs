using System.Collections.Generic;
using UnityEngine;

// ป่ารกหลังคลอง: a thick, dark forest on the far side of the tambon canal, over a humped wooden
// footbridge. A dirt trail winds through it past the things worth picking (forage.lua, ForageSpot) —
// เห็ดโคน by the termite mounds, หน่อไม้ in the bamboo, ไข่มดแดง up the mango trees, ผักหวานป่า in the
// undergrowth — and past five more haunted spots (GhostSpot) that come out at night.
public static partial class TempleLife
{
    private const float BridgeX = 30f;

    private static readonly Vector3[] ForestTrail =
    {
        new Vector3(30f, 0f, 38.5f), new Vector3(26f, 0f, 44f), new Vector3(14f, 0f, 47f), new Vector3(2f, 0f, 45f),
        new Vector3(-10f, 0f, 50f), new Vector3(-24f, 0f, 47f), new Vector3(-36f, 0f, 52f), new Vector3(-40f, 0f, 44f),
    };
    private static readonly Vector3[] ForestSpur = { new Vector3(14f, 0f, 47f), new Vector3(20f, 0f, 55f) };

    // Builds the bridge and the forest; `canal` is the one Places2.Canal made (its water blocker gets a gap)
    public static void BuildCanalForest(Transform root, GameObject canal)
    {
        var f = PrimitiveKit.Group("CanalForest_ป่ารกหลังคลอง", root, Vector3.zero).transform;
        Footbridge(f, canal);

        PrimitiveKit.Box("ForestFloor", f, new Vector3(0f, 0.008f, 48.5f), new Vector3(98f, 0.012f, 21f),
            PrimitiveKit.Mat("ForestFloor", new Color(0.2f, 0.27f, 0.13f)), collider: false);
        for (int i = 0; i + 1 < ForestTrail.Length; i++) Trail(f, ForestTrail[i], ForestTrail[i + 1]);
        Trail(f, ForestSpur[0], ForestSpur[1]);
        Places.Sign(f, new Vector3(32.4f, 0f, 39.6f), "ป่ารกหลังคลอง\nหาของป่า: เห็ด หน่อไม้ ไข่มดแดง ผักหวาน\nกลางคืนผีดุ!", 0f);

        // What there is to pick
        var keep = new List<Vector3>();
        void Forage(string id, string kind, Vector3 at)
        {
            keep.Add(at);
            switch (kind)
            {
                case "mushroom": TermiteMound(f, id, at); break;
                case "bamboo": Bamboo(f, id, at); break;
                case "ant_eggs": AntTree(f, id, at); break;
                default: PakWan(f, id, at); break;
            }
        }
        Forage("fg1", "mushroom", new Vector3(27f, 0f, 41.5f));
        Forage("fg2", "bamboo", new Vector3(23f, 0f, 48.5f));
        Forage("fg3", "pak_wan", new Vector3(17f, 0f, 44.2f));
        Forage("fg4", "ant_eggs", new Vector3(10f, 0f, 49.8f));
        Forage("fg5", "mushroom", new Vector3(4f, 0f, 42.6f));
        Forage("fg6", "bamboo", new Vector3(-2f, 0f, 49.2f));
        Forage("fg7", "pak_wan", new Vector3(-8f, 0f, 46.8f));
        Forage("fg8", "mushroom", new Vector3(-13.5f, 0f, 52.8f));
        Forage("fg9", "ant_eggs", new Vector3(-20f, 0f, 44.2f));
        Forage("fg10", "bamboo", new Vector3(-28.5f, 0f, 50.5f));
        Forage("fg11", "mushroom", new Vector3(-33f, 0f, 55.2f));
        Forage("fg12", "pak_wan", new Vector3(21.5f, 0f, 56f));

        // ขวาน: trees to fell, scrub to clear and the chopping block (WoodLots)
        WoodLots(f);
        foreach (var (_, _, at) in WoodTrees) keep.Add(at);
        keep.Add(ChoppingBlock);

        // Five more ghosts, out at night
        BananaClump(f, new Vector3(-18.5f, 0f, 50.5f));
        Takhian(f, new Vector3(-5f, 0f, 56.5f));
        DeadTree(f, new Vector3(37f, 0f, 53.5f), 1.5f, 6);
        DeadTree(f, new Vector3(-41f, 0f, 49f), 1.2f, 7);
        var ghosts = new[]
        {
            (new Vector3(6f, 0f, 55f), "tambon4", "krasue"), (new Vector3(-5f, 0f, 54f), "tambon5", "krahang"),
            (new Vector3(-18f, 0f, 48.6f), "tambon6", "tani"), (new Vector3(-38.5f, 0f, 40.5f), "tambon7", "phrai"),
            (new Vector3(35.5f, 0f, 52f), "tambon8", "pret"),
        };
        foreach (var (at, id, ghost) in ghosts) { Spot(f, at, id, ghost); keep.Add(at); }

        // Thick, dark growth everywhere else (kept off the trail and the spots so it stays walkable)
        var rng = new System.Random(4242);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        bool Clear(Vector3 p, float trail, float spot)
        {
            if (Mathf.Abs(p.x - BridgeX) < 3.5f && p.z < 41f) return false;
            for (int i = 0; i + 1 < ForestTrail.Length; i++) if (DistToSegment(p, ForestTrail[i], ForestTrail[i + 1]) < trail) return false;
            if (DistToSegment(p, ForestSpur[0], ForestSpur[1]) < trail) return false;
            foreach (var k in keep) if ((p - k).sqrMagnitude < spot * spot) return false;
            return true;
        }
        var trunks = new List<Vector3>();
        for (int tries = 0; tries < 900 && trunks.Count < 85; tries++)
        {
            var p = new Vector3(R(-47f, 47f), 0f, R(39f, 58.5f));
            if (!Clear(p, 2.4f, 3.2f)) continue;
            bool crowded = false;
            foreach (var q in trunks) if ((p - q).sqrMagnitude < 3.4f * 3.4f) { crowded = true; break; }
            if (crowded) continue;
            trunks.Add(p);
            if (trunks.Count % 9 == 0) Places.ShadeTree(f, p, R(0.9f, 1.2f));
            else ForestTree(f, p, R(0.85f, 1.25f), trunks.Count);
        }
        for (int tries = 0, n = 0; tries < 900 && n < 110; tries++)
        {
            var p = new Vector3(R(-47.5f, 47.5f), 0f, R(38.6f, 59f));
            if (!Clear(p, 1.3f, 1.8f)) continue;
            Nature.Bush(f, p, R(0.8f, 1.6f), 200 + n, flowers: n % 11 == 0);
            n++;
        }
        for (int i = 0; i < 8; i++)
        {
            var p = new Vector3(-44f + i * 12.3f, 0f, 58.6f - (i % 3) * 0.6f);
            Nature.Rock(f, p, 0.8f + (i % 2) * 0.5f, 300 + i);
        }
    }

    private static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = new Vector2(b.x - a.x, b.z - a.z);
        var ap = new Vector2(p.x - a.x, p.z - a.z);
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
        return (ap - ab * t).magnitude;
    }

    // A humped plank footbridge over the canal at x = BridgeX, with rails; cuts a gap in the water blocker
    private static void Footbridge(Transform parent, GameObject canal)
    {
        if (canal != null)
        {
            var blocker = canal.transform.Find("WaterBlocker");
            if (blocker != null)
            {
                var box = blocker.GetComponent<BoxCollider>();
                float half = box.size.x * 0.5f, gapL = BridgeX - canal.transform.position.x - 1.4f, gapR = gapL + 2.8f;
                box.center = new Vector3((-half + gapL) * 0.5f, 0f, 0f);
                box.size = new Vector3(gapL + half, box.size.y, box.size.z);
                var right = blocker.gameObject.AddComponent<BoxCollider>();
                right.center = new Vector3((gapR + half) * 0.5f, 0f, 0f);
                right.size = new Vector3(half - gapR, box.size.y, box.size.z);
            }
        }
        var b = PrimitiveKit.Group("Footbridge_สะพานไม้", parent, new Vector3(BridgeX, 0f, 34f)).transform;
        var plank = PrimitiveKit.Mat("WoodPlank", new Color(0.55f, 0.38f, 0.22f));
        var dark = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        const int Segs = 12;
        const float Len = 8.6f, Seg = Len / Segs;
        for (int i = 0; i < Segs; i++)
        {
            float u = (i + 0.5f) / Segs;
            float h = 0.22f + 0.8f * Mathf.Sin(u * Mathf.PI);
            float z = -Len * 0.5f + Seg * (i + 0.5f);
            PrimitiveKit.Box("Deck", b, new Vector3(0f, h * 0.5f, z), new Vector3(2.2f, h, Seg + 0.02f), plank);
            foreach (float x in new[] { -1.05f, 1.05f })
            {
                PrimitiveKit.Box("Rail", b, new Vector3(x, h + 0.9f, z), new Vector3(0.08f, 0.08f, Seg + 0.02f), dark);
                PrimitiveKit.Box("RailGuard", b, new Vector3(x, h + 0.5f, z), new Vector3(0.06f, 1f, Seg), dark, collider: true);
                if (i % 2 == 0) PrimitiveKit.Box("Baluster", b, new Vector3(x, h + 0.45f, z), new Vector3(0.09f, 0.9f, 0.09f), dark, collider: false);
            }
        }
        foreach (float z in new[] { -2f, 2f })
        foreach (float x in new[] { -1f, 1f })
            PrimitiveKit.Cylinder("Pile", b, new Vector3(x, -0.2f, z), new Vector3(0.18f, 0.6f, 0.18f), dark, collider: false);
    }

    // A tall, dark forest tree: straight trunk, a few lumpy crowns, a creeper hanging down
    private static void ForestTree(Transform parent, Vector3 at, float size, int seed)
    {
        var t = PrimitiveKit.Group("ForestTree_ต้นไม้ป่า", parent, at, seed * 47f).transform;
        t.localScale = Vector3.one * size;
        var bark = PrimitiveKit.Mat("ForestBark", new Color(0.3f, 0.24f, 0.18f), 0.1f);
        Color[] greens = { new Color(0.1f, 0.3f, 0.12f), new Color(0.14f, 0.36f, 0.14f), new Color(0.18f, 0.4f, 0.16f) };
        float h = 5.5f + (seed * 37 % 7) * 0.5f;
        PrimitiveKit.Cylinder("Trunk", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.45f, h * 0.5f, 0.45f), bark);
        PrimitiveKit.Cylinder("Root", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.25f, 0.9f), bark, collider: false);
        var blob = MeshKit.Blob("ForestCrown", 77, 0.25f, 1);
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120f + seed * 29f) * Mathf.Deg2Rad;
            var c = new Vector3(Mathf.Cos(a) * 1.1f, h + (i == 0 ? 0.6f : -0.2f), Mathf.Sin(a) * 1.1f);
            MeshKit.Place("Crown", t, blob, PrimitiveKit.Mat($"ForestLeaves{(i + seed) % 3}", greens[(i + seed) % 3], 0.15f), c,
                new Vector3(0f, i * 70f, 0f), new Vector3(3.4f, 2.2f, 3.2f) * (i == 0 ? 1.1f : 0.9f));
        }
        if (seed % 3 == 0) // a liana down from the crown
            PrimitiveKit.Cylinder("Vine", t, new Vector3(0.9f, h - 1.6f, 0.3f), new Vector3(0.05f, 1.7f, 0.05f), PrimitiveKit.Mat("Vine", new Color(0.22f, 0.32f, 0.12f)), new Vector3(0f, 0f, 8f), collider: false);
    }

    private static void ForageZone(Transform parent, string id, string kind, GameObject crop, float radius = 2.2f)
    {
        var col = parent.gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;
        col.center = Vector3.up;
        parent.gameObject.AddComponent<ForageSpot>().Configure(id, kind, crop);
    }

    // จอมปลวก: a termite mound with เห็ดโคน coming up round its foot
    private static void TermiteMound(Transform parent, string id, Vector3 at)
    {
        var g = PrimitiveKit.Group("Forage_เห็ดโคน_" + id, parent, at).transform;
        var mound = MeshKit.Lathe("TermiteMound", new[]
        {
            new Vector2(1.1f, 0f), new Vector2(0.95f, 0.35f), new Vector2(0.7f, 0.8f), new Vector2(0.45f, 1.3f), new Vector2(0.22f, 1.75f), new Vector2(0.05f, 2f),
        }, 10);
        var m = MeshKit.Place("Mound", g, mound, PrimitiveKit.Mat("TermiteClay", new Color(0.62f, 0.42f, 0.26f), 0.05f), Vector3.zero);
        var c = m.AddComponent<CapsuleCollider>();
        c.radius = 0.8f; c.height = 2f; c.center = Vector3.up;
        var crop = PrimitiveKit.Group("Mushrooms", g, Vector3.zero).transform;
        var cap = PrimitiveKit.Mat("MushroomCap", new Color(0.86f, 0.8f, 0.7f), 0.2f);
        var stem = PrimitiveKit.Mat("MushroomStem", new Color(0.96f, 0.95f, 0.9f));
        for (int i = 0; i < 7; i++)
        {
            float a = (i * 51f + 10f) * Mathf.Deg2Rad, r = 1.25f + (i % 3) * 0.15f;
            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float s = 0.8f + (i % 3) * 0.2f;
            PrimitiveKit.Cylinder("Stem", crop, p + new Vector3(0f, 0.12f * s, 0f), new Vector3(0.05f, 0.12f, 0.05f) * s, stem, collider: false);
            PrimitiveKit.Sphere("Cap", crop, p + new Vector3(0f, 0.25f * s, 0f), new Vector3(0.26f, 0.08f, 0.26f) * s, cap, collider: false);
        }
        ForageZone(g, id, "mushroom", crop.gameObject, 2.4f);
    }

    // กอไผ่: a clump of tall culms leaning out, หน่อไม้ pushing up round the base
    private static void Bamboo(Transform parent, string id, Vector3 at)
    {
        var g = PrimitiveKit.Group("Forage_หน่อไม้_" + id, parent, at).transform;
        var culm = PrimitiveKit.Mat("BambooCulm", new Color(0.45f, 0.6f, 0.22f), 0.35f);
        var leaf = PrimitiveKit.Mat("BambooLeaves", new Color(0.32f, 0.52f, 0.18f), 0.2f);
        var blob = MeshKit.Blob("BambooTuft", 13, 0.3f, 1);
        for (int i = 0; i < 14; i++)
        {
            float a = i * 137.5f * Mathf.Deg2Rad, r = 0.25f + (i % 4) * 0.12f, h = 5.5f + (i * 7 % 5) * 0.5f, lean = 6f + (i % 4) * 4f;
            var dir = Quaternion.Euler(Mathf.Sin(a) * lean, 0f, -Mathf.Cos(a) * lean);
            var foot = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            var c = PrimitiveKit.Cylinder("Culm", g, foot + dir * new Vector3(0f, h * 0.5f, 0f), new Vector3(0.11f, h * 0.5f, 0.11f), culm, collider: i % 3 == 0);
            c.transform.localRotation = dir;
            if (i % 2 == 0) MeshKit.Place("Leaves", g, blob, leaf, foot + dir * new Vector3(0f, h, 0f), new Vector3(0f, i * 40f, 0f), new Vector3(1.6f, 0.8f, 1.6f));
        }
        var crop = PrimitiveKit.Group("Shoots", g, Vector3.zero).transform;
        var shoot = MeshKit.Lathe("BambooShoot", new[] { new Vector2(0.11f, 0f), new Vector2(0.09f, 0.18f), new Vector2(0.04f, 0.38f), new Vector2(0f, 0.48f) }, 8);
        var husk = PrimitiveKit.Mat("ShootHusk", new Color(0.66f, 0.55f, 0.3f));
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 30f) * Mathf.Deg2Rad;
            MeshKit.Place("Shoot", crop, shoot, husk, new Vector3(Mathf.Cos(a) * 1.05f, 0f, Mathf.Sin(a) * 1.05f), new Vector3(8f, i * 50f, 0f), Vector3.one * (0.9f + (i % 2) * 0.3f));
        }
        ForageZone(g, id, "bamboo", crop.gameObject, 2.4f);
    }

    // A mango tree with a red ant nest (รังมดแดง) of folded leaves on a low branch
    private static void AntTree(Transform parent, string id, Vector3 at)
    {
        var g = PrimitiveKit.Group("Forage_ไข่มดแดง_" + id, parent, at).transform;
        var bark = PrimitiveKit.Mat("Bark", new Color(0.36f, 0.24f, 0.14f), 0.15f);
        PrimitiveKit.Cylinder("Trunk", g, new Vector3(0f, 2f, 0f), new Vector3(0.4f, 2f, 0.4f), bark);
        var branch = PrimitiveKit.Cylinder("Branch", g, new Vector3(0.75f, 2.1f, -0.2f), new Vector3(0.12f, 0.8f, 0.12f), bark, new Vector3(0f, 0f, -60f), collider: false);
        var blob = MeshKit.Blob("MangoCrown", 31, 0.22f, 1);
        var leaves = PrimitiveKit.Mat("MangoLeaves", new Color(0.12f, 0.34f, 0.12f), 0.25f);
        MeshKit.Place("Crown", g, blob, leaves, new Vector3(0f, 4.4f, 0f), Vector3.zero, new Vector3(4.2f, 2.6f, 4.2f));
        MeshKit.Place("Crown", g, blob, leaves, new Vector3(1f, 3.6f, 0.6f), new Vector3(0f, 80f, 0f), new Vector3(2.6f, 1.8f, 2.6f));
        var crop = PrimitiveKit.Group("AntNest", g, new Vector3(1.35f, 2.2f, -0.2f)).transform;
        var nest = MeshKit.Blob("AntNest", 5, 0.3f, 1);
        MeshKit.Place("Nest", crop, nest, PrimitiveKit.Mat("AntNestLeaves", new Color(0.55f, 0.5f, 0.2f), 0.2f), Vector3.zero, Vector3.zero, new Vector3(0.6f, 0.55f, 0.55f));
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Sphere("Ant", crop, new Vector3(Mathf.Cos(i * 1.3f) * 0.3f, Mathf.Sin(i * 2.1f) * 0.25f, -0.3f), new Vector3(0.05f, 0.03f, 0.07f),
                PrimitiveKit.Mat("RedAnt", new Color(0.75f, 0.25f, 0.1f)), collider: false);
        ForageZone(g, id, "ant_eggs", crop.gameObject, 2.4f);
    }

    // ผักหวานป่า: a low shrub with tender light-green tips to pick
    private static void PakWan(Transform parent, string id, Vector3 at)
    {
        var g = PrimitiveKit.Group("Forage_ผักหวานป่า_" + id, parent, at).transform;
        Nature.Bush(g, Vector3.zero, 1.2f, 500 + id.Length * 7);
        var crop = PrimitiveKit.Group("Tips", g, Vector3.zero).transform;
        var tip = MeshKit.Blob("PakWanTip", 9, 0.25f, 1);
        var green = PrimitiveKit.Mat("PakWanTips", new Color(0.55f, 0.85f, 0.3f), 0.3f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            MeshKit.Place("Tip", crop, tip, green, new Vector3(Mathf.Cos(a) * 0.55f, 1.05f + (i % 2) * 0.15f, Mathf.Sin(a) * 0.55f), new Vector3(0f, i * 33f, 0f), new Vector3(0.32f, 0.2f, 0.32f));
        }
        ForageZone(g, id, "pak_wan", crop.gameObject, 2f);
    }
}
