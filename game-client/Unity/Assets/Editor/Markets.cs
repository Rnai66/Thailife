using UnityEngine;

// Markets: the city's covered fresh market (ตลาดสดเทศบาล) and the tambon's evening
// market (ตลาดนัด) of tents and string lights. The entrance is on local -Z; stalls face the
// middle aisle with the stall keeper behind each table.
// The shop trigger itself is added by TambonBuilder.ShopZone.
public static class Markets
{
    private static readonly Color Wood = new Color(0.6f, 0.4f, 0.22f);

    // Steel-framed hall with a zinc roof, open sides, two rows of produce stalls
    public static GameObject FreshMarket(Transform parent, Vector3 position, string name, string hours, float yaw = 0f)
    {
        var market = PrimitiveKit.Group("FreshMarket_ตลาดสด", parent, position, yaw);
        var t = market.transform;
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.32f, 0.42f, 0.5f));
        var zinc = PrimitiveKit.Mat("RoofZinc", new Color(0.72f, 0.75f, 0.78f));
        var floor = PrimitiveKit.Mat("MarketFloor", new Color(0.62f, 0.62f, 0.6f));
        var bulb = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);
        const float w = 14f, d = 10f, h = 4.2f;

        PrimitiveKit.Box("Floor", t, new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), floor, collider: false);
        for (int i = 0; i <= 4; i++)
        foreach (float z in new[] { -d * 0.5f, d * 0.5f })
            PrimitiveKit.Box("Post", t, new Vector3(-w * 0.5f + i * w / 4f, h * 0.5f, z), new Vector3(0.18f, h, 0.18f), steel);
        // Gable roof along X
        float pitch = 18f, half = d * 0.5f + 0.6f;
        float slope = half / Mathf.Cos(pitch * Mathf.Deg2Rad), rise = Mathf.Tan(pitch * Mathf.Deg2Rad) * half;
        foreach (float side in new[] { -1f, 1f })
            PrimitiveKit.Box("RoofZinc", t, new Vector3(0f, h + rise * 0.5f, side * half * 0.5f), new Vector3(w + 1.2f, 0.08f, slope), zinc, new Vector3(side * pitch, 0f, 0f));
        PrimitiveKit.Box("RoofBeam", t, new Vector3(0f, h, 0f), new Vector3(w, 0.15f, 0.15f), steel, collider: false);

        // Two rows of stalls facing the middle aisle
        int look = 0;
        foreach (float row in new[] { -1f, 1f })
        for (int i = 0; i < 3; i++)
        {
            float x = -4.4f + i * 4.4f;
            var stall = PrimitiveKit.Group("Stall", t, new Vector3(x, 0f, row * 2.6f), row > 0 ? 180f : 0f);
            Stall(stall.transform, (i + (row > 0 ? 3 : 0)) % 6);
            var vendor = new GameObject("Vendor");
            vendor.transform.SetParent(stall.transform, false);
            vendor.transform.localPosition = new Vector3(0f, 0f, -1.1f); // behind the table, facing the aisle (+Z)
            vendor.AddComponent<MarketVendor>().Configure(look++);
            PrimitiveKit.Sphere("Bulb", t, new Vector3(x, h - 0.6f, row * 2.6f), Vector3.one * 0.22f, bulb);
        }

        // Shoppers in the aisle during market hours
        Npcs.Crowd(t, Vector3.zero, new Vector2(11f, 2f), 6, "", 5, 13, Npcs.MarketTalk);

        // Entrance sign over the aisle, readable from both sides
        PrimitiveKit.Box("SignPostL", t, new Vector3(-2.2f, 2.3f, -d * 0.5f - 0.2f), new Vector3(0.15f, 4.6f, 0.15f), steel);
        PrimitiveKit.Box("SignPostR", t, new Vector3(2.2f, 2.3f, -d * 0.5f - 0.2f), new Vector3(0.15f, 4.6f, 0.15f), steel);
        PrimitiveKit.Box("SignBoard", t, new Vector3(0f, 4.9f, -d * 0.5f - 0.2f), new Vector3(5f, 0.9f, 0.12f), PrimitiveKit.Mat("SignGreen", new Color(0.15f, 0.45f, 0.3f)), collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 5f, -d * 0.5f - 0.28f), 0.075f, Color.white);
        PrimitiveKit.Label($"เปิด {hours} น.", t, new Vector3(0f, 4.6f, -d * 0.5f - 0.28f), 0.04f, new Color(1f, 0.9f, 0.6f));
        return market;
    }

    // Evening market: rows of colourful tents along an aisle, string lights overhead
    public static GameObject FleaMarket(Transform parent, Vector3 position, string name, string hours, float yaw = 0f)
    {
        var market = PrimitiveKit.Group("FleaMarket_ตลาดนัด", parent, position, yaw);
        var t = market.transform;
        var pole = PrimitiveKit.Mat("TentPole", new Color(0.85f, 0.85f, 0.85f));
        Material[] canopies =
        {
            PrimitiveKit.Mat("TentRed", new Color(0.85f, 0.2f, 0.18f)), PrimitiveKit.Mat("TentBlue", new Color(0.2f, 0.45f, 0.8f)),
            PrimitiveKit.Mat("TentYellow", new Color(0.98f, 0.78f, 0.2f)), PrimitiveKit.Mat("TentGreen", new Color(0.25f, 0.62f, 0.35f)),
            PrimitiveKit.Mat("TentWhite", new Color(0.95f, 0.95f, 0.92f)),
        };
        var bulb = PrimitiveKit.Glow("BulbWarm", new Color(1f, 0.85f, 0.55f), 2.5f);
        var wire = PrimitiveKit.Mat("Wire", new Color(0.1f, 0.1f, 0.1f));
        var tentRoof = MeshKit.Gable("TentRoof", 2.8f, 0.7f, 2.8f);

        int look = 2;
        foreach (float row in new[] { -1f, 1f })
        for (int i = 0; i < 4; i++)
        {
            float z = -4.5f + i * 3.1f;
            var tent = PrimitiveKit.Group("Tent", t, new Vector3(row * 2.6f, 0f, z), row > 0 ? -90f : 90f);
            var tt = tent.transform;
            foreach (float px in new[] { -1.3f, 1.3f })
            foreach (float pz in new[] { -1.3f, 1.3f })
                PrimitiveKit.Cylinder("Pole", tt, new Vector3(px, 1.1f, pz), new Vector3(0.06f, 1.1f, 0.06f), pole);
            MeshKit.Place("Canopy", tt, tentRoof, canopies[(i * 2 + (row > 0 ? 1 : 0)) % canopies.Length], new Vector3(0f, 2.2f, -1.4f));
            Stall(tt, (i + (row > 0 ? 2 : 0)) % 6);
            var vendor = new GameObject("Vendor");
            vendor.transform.SetParent(tt, false);
            vendor.transform.localPosition = new Vector3(0f, 0f, -0.95f); // behind the table, facing the aisle (+Z)
            vendor.AddComponent<MarketVendor>().Configure(look++);
        }

        // Evening shoppers
        Npcs.Crowd(t, new Vector3(0f, 0f, 0.2f), new Vector2(2.6f, 10f), 7, "", 16, 22, Npcs.MarketTalk);

        // String lights zig-zagging over the aisle
        for (int i = 0; i < 4; i++)
        {
            float z = -4.5f + i * 3.1f;
            var a = new Vector3(-1.3f, 2.3f, z);
            var b = new Vector3(1.3f, 2.3f, z + 1.5f);
            var curve = MeshKit.Curve(a, (a + b) * 0.5f + Vector3.down * 0.35f, b, 8);
            MeshKit.Place("LightString", t, MeshKit.Sweep("LightString", curve, MeshKit.Taper(8, 0.012f, 0.012f), 4), wire, Vector3.zero);
            for (int k = 1; k < 7; k++)
                PrimitiveKit.Sphere("Bulb", t, curve[k] + Vector3.down * 0.06f, Vector3.one * 0.09f, bulb);
        }

        // Entrance arch at the road end (-Z)
        PrimitiveKit.Box("ArchL", t, new Vector3(-2.4f, 1.6f, -7f), new Vector3(0.25f, 3.2f, 0.25f), canopies[0]);
        PrimitiveKit.Box("ArchR", t, new Vector3(2.4f, 1.6f, -7f), new Vector3(0.25f, 3.2f, 0.25f), canopies[0]);
        PrimitiveKit.Box("ArchBoard", t, new Vector3(0f, 3.4f, -7f), new Vector3(5.2f, 0.8f, 0.14f), canopies[2], collider: false);
        PrimitiveKit.Label(name, t, new Vector3(0f, 3.5f, -7.09f), 0.07f, new Color(0.6f, 0.1f, 0.1f));
        PrimitiveKit.Label($"เปิด {hours} น.", t, new Vector3(0f, 3.12f, -7.09f), 0.038f, new Color(0.25f, 0.15f, 0.05f));
        return market;
    }

    // A table with goods piled on it; kind picks fruit, vegetables, fish, snacks, clothes or pots
    private static void Stall(Transform t, int kind)
    {
        var wood = PrimitiveKit.Mat("WoodPlank", Wood);
        PrimitiveKit.Box("Table", t, new Vector3(0f, 0.8f, 0f), new Vector3(2.2f, 0.08f, 1f), wood);
        foreach (float x in new[] { -1f, 1f })
        foreach (float z in new[] { -0.42f, 0.42f })
            PrimitiveKit.Box("Leg", t, new Vector3(x, 0.4f, z), new Vector3(0.07f, 0.8f, 0.07f), wood, collider: false);

        Color[][] goods =
        {
            new[] { new Color(1f, 0.55f, 0.1f), new Color(0.95f, 0.85f, 0.2f), new Color(0.85f, 0.15f, 0.15f) },   // fruit
            new[] { new Color(0.25f, 0.6f, 0.2f), new Color(0.45f, 0.75f, 0.25f), new Color(0.8f, 0.15f, 0.1f) },  // vegetables, chillies
            new[] { new Color(0.7f, 0.75f, 0.8f), new Color(0.55f, 0.6f, 0.65f), new Color(0.9f, 0.9f, 0.95f) },   // fish on ice
            new[] { new Color(0.75f, 0.45f, 0.2f), new Color(0.9f, 0.7f, 0.3f), new Color(0.55f, 0.3f, 0.15f) },   // grilled snacks
            new[] { new Color(0.9f, 0.3f, 0.5f), new Color(0.3f, 0.5f, 0.9f), new Color(0.95f, 0.95f, 0.95f) },   // clothes
            new[] { new Color(0.6f, 0.35f, 0.2f), new Color(0.3f, 0.55f, 0.3f), new Color(0.85f, 0.8f, 0.7f) },   // pots and plants
        };
        var palette = goods[kind];
        var mats = new Material[3];
        for (int i = 0; i < 3; i++) mats[i] = PrimitiveKit.Mat($"Goods{kind}_{i}", palette[i]);
        var blob = MeshKit.Blob("Produce", 41, 0.2f, 1);
        for (int i = 0; i < 9; i++)
        {
            float x = -0.8f + (i % 3) * 0.8f + (i / 3) * 0.12f;
            float z = -0.25f + (i / 3) * 0.25f;
            MeshKit.Place("Goods", t, blob, mats[i % 3], new Vector3(x, 0.95f, z), new Vector3(0f, i * 40f, 0f), new Vector3(0.45f, 0.25f, 0.3f));
        }
    }
}
