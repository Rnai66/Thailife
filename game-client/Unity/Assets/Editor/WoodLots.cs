using UnityEngine;

// ขวาน in the forest behind the tambon canal (woodcut.lua): six big trees you can fell and six tangles
// of overgrown scrub (ไม้รก) to clear, just off the trail, each with the stump it leaves (ChopTree);
// and by the footbridge a chopping block (ตอผ่าฟืน) with a woodpile, to split logs into ฟืน (SplitBlock).
public static partial class TempleLife
{
    public static readonly (string id, string kind, Vector3 at)[] WoodTrees =
    {
        ("wt1", "tree", new Vector3(22f, 0f, 41.5f)), ("wt2", "tree", new Vector3(8.5f, 0f, 42.5f)),
        ("wt3", "tree", new Vector3(-4f, 0f, 43f)), ("wt4", "tree", new Vector3(-15f, 0f, 44.5f)),
        ("wt5", "tree", new Vector3(-30f, 0f, 46f)), ("wt6", "tree", new Vector3(23.5f, 0f, 52.2f)),
        ("ws1", "scrub", new Vector3(24f, 0f, 39.5f)), ("ws2", "scrub", new Vector3(12f, 0f, 43.5f)),
        ("ws3", "scrub", new Vector3(2f, 0f, 48.5f)), ("ws4", "scrub", new Vector3(-12f, 0f, 46f)),
        ("ws5", "scrub", new Vector3(-24f, 0f, 51f)), ("ws6", "scrub", new Vector3(-35f, 0f, 45f)),
    };
    public static readonly Vector3 ChoppingBlock = new Vector3(34f, 0f, 41.8f);

    private static void WoodLots(Transform f)
    {
        int seed = 0;
        foreach (var (id, kind, at) in WoodTrees)
        {
            seed++;
            if (kind == "tree") FellTree(f, id, at, seed);
            else Scrub(f, id, at, seed);
        }
        Block(f, ChoppingBlock);
        Places.Sign(f, ChoppingBlock + new Vector3(1.6f, 0f, -0.6f), "ตอผ่าฟืน\nโค่นต้นไม้ในป่าได้ท่อนไม้\nมาผ่าเป็นฟืนที่นี่ · ต้องมีขวาน", -20f);
    }

    private static Transform Stump(Transform g, float radius, Material bark, Material wood)
    {
        var s = PrimitiveKit.Group("Stump", g, Vector3.zero).transform;
        PrimitiveKit.Cylinder("StumpBark", s, new Vector3(0f, 0.22f, 0f), new Vector3(radius, 0.22f, radius), bark);
        PrimitiveKit.Cylinder("StumpCut", s, new Vector3(0f, 0.445f, 0f), new Vector3(radius * 0.9f, 0.006f, radius * 0.9f), wood, collider: false);
        PrimitiveKit.Cylinder("Rings", s, new Vector3(0f, 0.449f, 0f), new Vector3(radius * 0.45f, 0.004f, radius * 0.45f), PrimitiveKit.Mat("WoodRings", new Color(0.7f, 0.52f, 0.32f)), collider: false);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            PrimitiveKit.Box("Chip", s, new Vector3(Mathf.Cos(a) * (radius + 0.3f), 0.01f, Mathf.Sin(a) * (radius + 0.25f)), new Vector3(0.1f, 0.02f, 0.06f), wood, new Vector3(0f, i * 47f, 0f), collider: false);
        }
        s.gameObject.SetActive(false);
        return s;
    }

    private static void Zone(GameObject g, string id, string kind, Transform standing, Transform stump, float radius)
    {
        var col = g.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;
        col.center = Vector3.up;
        g.AddComponent<ChopTree>().Configure(id, kind, standing.gameObject, stump.gameObject);
    }

    // A tall forest tree with a straight trunk worth felling
    private static void FellTree(Transform parent, string id, Vector3 at, int seed)
    {
        var g = PrimitiveKit.Group("Wood_ต้นไม้ใหญ่_" + id, parent, at, seed * 61f);
        var bark = PrimitiveKit.Mat("ForestBark", new Color(0.3f, 0.24f, 0.18f), 0.1f);
        var cut = PrimitiveKit.Mat("FreshWood", new Color(0.86f, 0.72f, 0.5f));
        var standing = PrimitiveKit.Group("Standing", g.transform, Vector3.zero).transform;
        float h = 6f + (seed % 3) * 0.6f;
        PrimitiveKit.Cylinder("Trunk", standing, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.5f, h * 0.5f, 0.5f), bark);
        PrimitiveKit.Cylinder("Root", standing, new Vector3(0f, 0.25f, 0f), new Vector3(0.95f, 0.25f, 0.95f), bark, collider: false);
        PrimitiveKit.Cylinder("Branch", standing, new Vector3(0.5f, h * 0.7f, 0f), new Vector3(0.12f, 0.9f, 0.12f), bark, new Vector3(0f, 0f, -40f), collider: false);
        Color[] greens = { new Color(0.12f, 0.33f, 0.13f), new Color(0.16f, 0.38f, 0.15f), new Color(0.2f, 0.42f, 0.17f) };
        var blob = MeshKit.Blob("ForestCrown", 77, 0.25f, 1);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + seed * 23f) * Mathf.Deg2Rad;
            var c = new Vector3(Mathf.Cos(a) * 1.2f, h + (i == 0 ? 0.7f : -0.3f), Mathf.Sin(a) * 1.2f);
            MeshKit.Place("Crown", standing, blob, PrimitiveKit.Mat($"ForestLeaves{(i + seed) % 3}", greens[(i + seed) % 3], 0.15f), c,
                new Vector3(0f, i * 70f, 0f), new Vector3(3.4f, 2.3f, 3.2f) * (i == 0 ? 1.1f : 0.9f));
        }
        // an axe mark already started on the trunk, so it reads as "this one you can cut"
        PrimitiveKit.Box("Notch", standing, new Vector3(0f, 0.75f, -0.47f), new Vector3(0.36f, 0.14f, 0.08f), cut, new Vector3(0f, 0f, 0f), collider: false);
        var stump = Stump(g.transform, 0.5f, bark, cut);
        Zone(g, id, "tree", standing, stump, 2.8f);
    }

    // ไม้รก: a tangle of saplings, bushes and creepers grown over the edge of the trail
    private static void Scrub(Transform parent, string id, Vector3 at, int seed)
    {
        var g = PrimitiveKit.Group("Wood_ไม้รก_" + id, parent, at, seed * 37f);
        var bark = PrimitiveKit.Mat("SaplingBark", new Color(0.38f, 0.3f, 0.2f), 0.1f);
        var cut = PrimitiveKit.Mat("FreshWood", new Color(0.86f, 0.72f, 0.5f));
        var vine = PrimitiveKit.Mat("Vine", new Color(0.22f, 0.32f, 0.12f));
        var standing = PrimitiveKit.Group("Standing", g.transform, Vector3.zero).transform;
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + seed * 13f) * Mathf.Deg2Rad, r = 0.25f + (i % 2) * 0.35f, hh = 1.6f + (i % 3) * 0.5f;
            PrimitiveKit.Cylinder("Sapling", standing, new Vector3(Mathf.Cos(a) * r, hh * 0.5f, Mathf.Sin(a) * r), new Vector3(0.08f, hh * 0.5f, 0.08f), bark,
                new Vector3((i - 2) * 6f, 0f, (i % 2 == 0 ? 8f : -8f)), collider: false);
            Nature.Bush(standing, new Vector3(Mathf.Cos(a) * r, hh - 0.4f, Mathf.Sin(a) * r), 0.7f + (i % 2) * 0.25f, seed * 10 + i);
        }
        Nature.Bush(standing, new Vector3(0f, 0f, 0f), 1.4f, seed * 10 + 7);
        Nature.Bush(standing, new Vector3(0.7f, 0f, 0.4f), 1.1f, seed * 10 + 8);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Cylinder("Creeper", standing, new Vector3(-0.4f + i * 0.4f, 1.1f, -0.2f), new Vector3(0.035f, 1.1f, 0.035f), vine, new Vector3(25f * (i - 1), 0f, 15f), collider: false);
        var block = standing.gameObject.AddComponent<BoxCollider>(); // thick enough that you go round it
        block.center = new Vector3(0f, 0.8f, 0f);
        block.size = new Vector3(1.4f, 1.6f, 1.4f);
        var stump = Stump(g.transform, 0.18f, bark, cut);
        foreach (float x in new[] { -0.4f, 0.35f }) // a couple more sapling stumps
            PrimitiveKit.Cylinder("SaplingStump", stump, new Vector3(x, 0.1f, 0.3f), new Vector3(0.08f, 0.1f, 0.08f), bark, collider: false);
        Zone(g, id, "scrub", standing, stump, 2.2f);
    }

    // ตอผ่าฟืน: a big round block with an axe bitten into it, a stack of split wood and a few logs
    private static void Block(Transform parent, Vector3 at)
    {
        var g = PrimitiveKit.Group("SplitBlock_ตอผ่าฟืน", parent, at, -20f);
        var t = g.transform;
        var bark = PrimitiveKit.Mat("ForestBark", new Color(0.3f, 0.24f, 0.18f), 0.1f);
        var cut = PrimitiveKit.Mat("FreshWood", new Color(0.86f, 0.72f, 0.5f));
        var split = PrimitiveKit.Mat("SplitWood", new Color(0.78f, 0.62f, 0.4f));
        PrimitiveKit.Cylinder("Block", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.25f, 0.6f), bark);
        PrimitiveKit.Cylinder("BlockTop", t, new Vector3(0f, 0.505f, 0f), new Vector3(0.55f, 0.006f, 0.55f), cut, collider: false);
        var top = PrimitiveKit.Group("Top", t, new Vector3(0f, 0.52f, 0f)).transform;
        // the woodpile behind: split pieces stacked between two posts
        for (int row = 0; row < 4; row++)
            for (int i = 0; i < 7; i++)
                PrimitiveKit.Box("Firewood", t, new Vector3(-1.05f + i * 0.33f + (row % 2) * 0.12f, 0.12f + row * 0.22f, 1.3f), new Vector3(0.16f, 0.2f, 0.7f), split,
                    new Vector3(0f, (i * 7 + row * 3) % 10 - 5f, 45f), collider: i == 3 && row == 0);
        foreach (float x in new[] { -1.4f, 1.4f })
            PrimitiveKit.Box("PilePost", t, new Vector3(x, 0.55f, 1.3f), new Vector3(0.1f, 1.1f, 0.1f), bark);
        PrimitiveKit.Box("PileRoof", t, new Vector3(0f, 1.15f, 1.3f), new Vector3(3.1f, 0.06f, 1f), PrimitiveKit.Mat("Thatch", new Color(0.7f, 0.6f, 0.38f)), new Vector3(-8f, 0f, 0f), collider: false);
        // a few logs waiting, and the axe left in the block
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Cylinder("Log", t, new Vector3(1.1f, 0.15f + (i == 2 ? 0.26f : 0f), -0.3f + (i % 2) * 0.32f + (i == 2 ? 0.16f : 0f)), new Vector3(0.28f, 0.45f, 0.28f), bark, new Vector3(0f, 0f, 90f), collider: false);
        PrimitiveKit.Cylinder("AxeHandle", t, new Vector3(0.12f, 0.75f, 0.12f), new Vector3(0.04f, 0.3f, 0.04f), PrimitiveKit.Mat("PropWood", new Color(0.55f, 0.38f, 0.2f)), new Vector3(35f, 0f, -20f), collider: false);
        PrimitiveKit.Box("AxeHead", t, new Vector3(0f, 0.53f, 0f), new Vector3(0.05f, 0.1f, 0.16f), PrimitiveKit.Mat("PropIron", new Color(0.3f, 0.3f, 0.32f)), new Vector3(35f, 0f, -20f), collider: false);
        var col = g.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2.2f;
        col.center = Vector3.up;
        g.AddComponent<SplitBlock>().Configure(top);
    }
}
