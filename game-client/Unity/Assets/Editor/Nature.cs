using System.Collections.Generic;
using UnityEngine;

// Plants and ground dressing for the scene builders: coconut palms, shrubs, flowering
// bougainvillea, rocks and grass fields (many tufts merged into a few meshes, cheap on phones).
public static class Nature
{
    public static GameObject CoconutPalm(Transform parent, Vector3 position, float size = 1f, int seed = 0)
    {
        var palm = PrimitiveKit.Group("CoconutPalm_ต้นมะพร้าว", parent, position, seed * 37f);
        var t = palm.transform;
        var bark = PrimitiveKit.Mat("PalmBark", new Color(0.5f, 0.4f, 0.3f), 0.15f);
        var frond = PrimitiveKit.Mat("PalmFrond", new Color(0.26f, 0.55f, 0.18f), 0.3f);
        var nut = PrimitiveKit.Mat("Coconut", new Color(0.38f, 0.45f, 0.15f), 0.4f);

        // Gently leaning, curving trunk
        float lean = 0.8f + (seed % 3) * 0.4f;
        var path = MeshKit.Curve(Vector3.zero, new Vector3(0f, 3.5f, lean * 0.2f), new Vector3(0f, 6.6f, lean), 10);
        MeshKit.Place("Trunk", t, MeshKit.Sweep("PalmTrunk" + seed % 3, path, MeshKit.Taper(10, 0.24f, 0.15f), 10), bark, Vector3.zero, default, Vector3.one * size);
        var collider = palm.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.5f * size, 0f);
        collider.height = 3f * size;
        collider.radius = 0.25f * size;

        Vector3 top = path[path.Length - 1] * size;
        var leaf = MeshKit.Leaf("PalmFrond", 3.4f, 0.55f, 0.6f, 2.3f, 12, 0.6f);
        for (int i = 0; i < 10; i++)
            MeshKit.Place("Frond", t, leaf, frond, top, new Vector3(-20f - (i % 3) * 10f, i * 36f + (i % 2) * 12f, 0f), Vector3.one * size);
        for (int i = 0; i < 4; i++)
            PrimitiveKit.Sphere("Coconut", t, top + Quaternion.Euler(0f, i * 90f + 20f, 0f) * new Vector3(0.18f, -0.25f, 0f) * size, Vector3.one * 0.26f * size, nut);
        return palm;
    }

    public static GameObject Bush(Transform parent, Vector3 position, float size = 1f, int seed = 0, bool flowers = false)
    {
        var bush = PrimitiveKit.Group(flowers ? "Bougainvillea_เฟื่องฟ้า" : "Bush", parent, position, seed * 53f);
        var t = bush.transform;
        var leaves = PrimitiveKit.Mat("BushLeaves", new Color(0.16f, 0.42f, 0.18f), 0.2f);
        var leavesLight = PrimitiveKit.Mat("LeavesLight", new Color(0.3f, 0.58f, 0.22f), 0.2f);
        var bloom = PrimitiveKit.Mat("Bougainvillea", new Color(0.9f, 0.25f, 0.55f), 0.35f);
        var rng = new System.Random(seed);
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3((float)rng.NextDouble() - 0.5f, 0.35f + (float)rng.NextDouble() * 0.2f, (float)rng.NextDouble() - 0.5f) * size;
            MeshKit.Place("Leaves", t, MeshKit.Blob("Bush" + (seed + i) % 4, 30 + (seed + i) % 4, 0.25f, 1), i % 2 == 0 ? leaves : leavesLight,
                p, new Vector3(0f, i * 70f, 0f), new Vector3(1.1f, 0.85f, 1.1f) * size);
        }
        if (flowers)
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3((float)rng.NextDouble() - 0.5f, 0.55f + (float)rng.NextDouble() * 0.35f, (float)rng.NextDouble() - 0.5f) * size * 1.2f;
                MeshKit.Place("Flowers", t, MeshKit.Blob("Bloom", 77, 0.3f, 0), bloom, p, default, Vector3.one * 0.32f * size);
            }
        return bush;
    }

    public static GameObject Rock(Transform parent, Vector3 position, float size = 1f, int seed = 0)
    {
        var rock = MeshKit.Place("Rock", parent, MeshKit.Blob("Rock" + seed % 3, 90 + seed % 3, 0.3f, 1),
            PrimitiveKit.Mat("Rock", new Color(0.5f, 0.49f, 0.46f), 0.2f), position + Vector3.up * 0.12f * size,
            new Vector3(0f, seed * 61f, 0f), new Vector3(1.2f, 0.6f, 0.9f) * size);
        rock.AddComponent<SphereCollider>().radius = 0.4f;
        return rock;
    }

    // Grass tufts scattered over an area (skipping the given rectangles: x0, z0, x1, z1),
    // merged into one mesh per material so hundreds of tufts are a couple of draw calls.
    public static void GrassField(Transform parent, Vector3 center, Vector2 size, int count, int seed, params Vector4[] avoid)
    {
        var tuft = GrassTuft();
        var dark = PrimitiveKit.Mat("GrassTuft", new Color(0.3f, 0.55f, 0.2f), 0.1f);
        var light = PrimitiveKit.Mat("GrassTuftLight", new Color(0.45f, 0.65f, 0.25f), 0.1f);
        var rng = new System.Random(seed);
        var batches = new[] { new List<CombineInstance>(), new List<CombineInstance>() };
        for (int i = 0; i < count; i++)
        {
            float x = center.x + ((float)rng.NextDouble() - 0.5f) * size.x;
            float z = center.z + ((float)rng.NextDouble() - 0.5f) * size.y;
            bool blocked = false;
            foreach (var r in avoid) if (x > r.x && x < r.z && z > r.y && z < r.w) { blocked = true; break; }
            if (blocked) continue;
            float s = 0.7f + (float)rng.NextDouble() * 0.7f;
            var m = Matrix4x4.TRS(new Vector3(x, 0f, z) - parent.position, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * s);
            batches[i % 2].Add(new CombineInstance { mesh = tuft, transform = m });
        }
        for (int b = 0; b < 2; b++)
        {
            if (batches[b].Count == 0) continue;
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(batches[b].ToArray(), true, true);
            var saved = MeshKit.SaveCombined($"GrassField_{seed}_{b}", mesh);
            var go = MeshKit.Place("GrassField", parent, saved, b == 0 ? dark : light, Vector3.zero);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // Five thin blades fanning out, both sides visible
    private static Mesh GrassTuft()
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            Quaternion q = Quaternion.Euler((i - 2) * 14f, i * 72f, 0f);
            int b = v.Count;
            v.Add(q * new Vector3(-0.04f, 0f, 0f));
            v.Add(q * new Vector3(0.04f, 0f, 0f));
            v.Add(q * new Vector3(0.02f, 0.35f + (i % 2) * 0.12f, 0.05f));
            t.AddRange(new[] { b, b + 2, b + 1, b, b + 1, b + 2 });
        }
        var mesh = new Mesh();
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        // Point every normal up so the blades light like the ground they grow from
        var normals = new Vector3[v.Count];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
        mesh.normals = normals;
        return mesh;
    }
}
