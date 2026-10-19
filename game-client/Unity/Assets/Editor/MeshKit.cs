using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Smooth/organic shapes for the scene builders, beyond Unity's cube/cylinder/sphere:
// lathe (round things like a chedi or a jar), sweep (curved tubes: horns, branches, bananas),
// leaves (curved, tapered, double-sided), low-poly blobs (tree canopies) and gable prisms.
// Meshes are saved once under Assets/Art/Meshes so scenes reference small assets.
public static class MeshKit
{
    private const string Folder = "Assets/Art/Meshes";

    // ── Placing ─────────────────────────────────────────────────────────────────
    public static GameObject Place(string name, Transform parent, Mesh mesh, Material material,
        Vector3 localPosition, Vector3 euler = default, Vector3? scale = null)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale ?? Vector3.one;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static Mesh Save(string key, Mesh mesh)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "Meshes");
        string path = $"{Folder}/{key}.asset";
        int slash = key.LastIndexOf('/');
        if (slash > 0 && !AssetDatabase.IsValidFolder($"{Folder}/{key.Substring(0, slash)}"))
            AssetDatabase.CreateFolder(Folder, key.Substring(0, slash));
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing); // rebuilt with new numbers: update in place
            Object.DestroyImmediate(mesh);
            return existing;
        }
        mesh.name = key.Substring(key.LastIndexOf('/') + 1);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // Save a mesh built elsewhere (e.g. merged grass) under a key
    public static Mesh SaveCombined(string key, Mesh mesh) => Save(key, mesh);

    private static Mesh Finish(string key, List<Vector3> v, List<int> t, List<Vector2> uv = null, bool flat = false)
    {
        var mesh = new Mesh();
        if (flat)
        {
            // Every triangle gets its own vertices: crisp low-poly facets
            var fv = new List<Vector3>(t.Count);
            var ft = new List<int>(t.Count);
            for (int i = 0; i < t.Count; i++) { fv.Add(v[t[i]]); ft.Add(i); }
            v = fv; t = ft; uv = null;
        }
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        if (uv != null) mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return Save(key, mesh);
    }

    // ── Chamfered box: a box whose edges are bevelled, so they catch the light ──
    // The size is baked into the mesh (the object keeps scale 1, its BoxCollider gets the size).
    public static bool TryChamferBox(Vector3 size, out Mesh mesh)
    {
        mesh = null;
        float min = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        if (min < 0.04f) return false; // paper-thin pieces stay plain
        float c = Mathf.Min(0.05f, min * 0.2f);
        string key = $"Boxes/Box_{size.x:0.###}_{size.y:0.###}_{size.z:0.###}";
        if (boxCache.TryGetValue(key, out mesh) && mesh != null) return true;
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{Folder}/{key}.asset");
        if (mesh == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "Meshes");
            if (!AssetDatabase.IsValidFolder(Folder + "/Boxes")) AssetDatabase.CreateFolder(Folder, "Boxes");
            mesh = BuildChamferBox(key, size * 0.5f, c);
        }
        boxCache[key] = mesh;
        return true;
    }

    private static readonly Dictionary<string, Mesh> boxCache = new Dictionary<string, Mesh>();

    private static Mesh BuildChamferBox(string key, Vector3 h, float c)
    {
        // Each corner splits into three points, one pushed out along each axis
        Vector3 P(int sx, int sy, int sz, int axis)
        {
            var p = new Vector3(sx * (h.x - c), sy * (h.y - c), sz * (h.z - c));
            if (axis == 0) p.x = sx * h.x; else if (axis == 1) p.y = sy * h.y; else p.z = sz * h.z;
            return p;
        }
        int[] S(int axis, int sa, int sb, int sc)
        {
            // signs for (x, y, z) given the sign along `axis` and the other two in order
            var r = new int[3];
            r[axis] = sa; r[(axis + 1) % 3] = sb; r[(axis + 2) % 3] = sc;
            return r;
        }
        var v = new List<Vector3>();
        var t = new List<int>();
        void Poly(params Vector3[] pts)
        {
            Vector3 centroid = Vector3.zero;
            foreach (var q in pts) centroid += q;
            centroid /= pts.Length;
            for (int i = 1; i < pts.Length - 1; i++)
            {
                Vector3 a = pts[0], b = pts[i], d = pts[i + 1];
                if (Vector3.Dot(Vector3.Cross(b - a, d - a), centroid) < 0f) (b, d) = (d, b); // face outward
                int n = v.Count;
                v.Add(a); v.Add(b); v.Add(d);
                t.Add(n); t.Add(n + 1); t.Add(n + 2);
            }
        }
        int[][] ring = { new[] { -1, -1 }, new[] { -1, 1 }, new[] { 1, 1 }, new[] { 1, -1 } };
        for (int axis = 0; axis < 3; axis++)
        {
            int b = (axis + 1) % 3, d = (axis + 2) % 3;
            foreach (int sa in new[] { -1, 1 })
            {
                // Flat face
                var face = new Vector3[4];
                for (int k = 0; k < 4; k++) { var sg = S(axis, sa, ring[k][0], ring[k][1]); face[k] = P(sg[0], sg[1], sg[2], axis); }
                Poly(face);
            }
            // Chamfer strips along edges parallel to `axis`
            foreach (int sb in new[] { -1, 1 })
            foreach (int sd in new[] { -1, 1 })
            {
                var lo = S(axis, -1, sb, sd);
                var hi = S(axis, 1, sb, sd);
                Poly(P(lo[0], lo[1], lo[2], b), P(lo[0], lo[1], lo[2], d), P(hi[0], hi[1], hi[2], d), P(hi[0], hi[1], hi[2], b));
            }
        }
        // Corner triangles
        foreach (int sx in new[] { -1, 1 })
        foreach (int sy in new[] { -1, 1 })
        foreach (int sz in new[] { -1, 1 })
            Poly(P(sx, sy, sz, 0), P(sx, sy, sz, 1), P(sx, sy, sz, 2));

        var mesh = new Mesh();
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return Save(key, mesh);
    }

    // ── Lathe: revolve a (radius, height) profile around Y ─────────────────────
    public static Mesh Lathe(string key, Vector2[] profile, int segments = 24, bool capTop = true, bool capBottom = true)
    {
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        int rings = profile.Length;
        for (int r = 0; r < rings; r++)
        for (int s = 0; s <= segments; s++)
        {
            float a = s * Mathf.PI * 2f / segments;
            v.Add(new Vector3(Mathf.Cos(a) * profile[r].x, profile[r].y, Mathf.Sin(a) * profile[r].x));
            uv.Add(new Vector2((float)s / segments, (float)r / (rings - 1)));
        }
        int row = segments + 1;
        for (int r = 0; r < rings - 1; r++)
        for (int s = 0; s < segments; s++)
        {
            int a = r * row + s, b = a + row;
            t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        if (capBottom && profile[0].x > 0.001f) Cap(v, t, uv, profile[0], segments, false);
        if (capTop && profile[rings - 1].x > 0.001f) Cap(v, t, uv, profile[rings - 1], segments, true);
        return Finish(key, v, t, uv);
    }

    private static void Cap(List<Vector3> v, List<int> t, List<Vector2> uv, Vector2 ring, int segments, bool top)
    {
        int center = v.Count;
        v.Add(new Vector3(0f, ring.y, 0f));
        uv.Add(new Vector2(0.5f, 0.5f));
        for (int s = 0; s <= segments; s++)
        {
            float a = s * Mathf.PI * 2f / segments;
            v.Add(new Vector3(Mathf.Cos(a) * ring.x, ring.y, Mathf.Sin(a) * ring.x));
            uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
        }
        for (int s = 0; s < segments; s++)
        {
            int a = center + 1 + s;
            if (top) t.AddRange(new[] { center, a + 1, a });
            else t.AddRange(new[] { center, a, a + 1 });
        }
    }

    // ── Sweep: a round tube along a path, radius per point ─────────────────────
    public static Mesh Sweep(string key, Vector3[] path, float[] radius, int sides = 8)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        Vector3 up = Vector3.up;
        for (int i = 0; i < path.Length; i++)
        {
            Vector3 dir = (i < path.Length - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.95f) up = Vector3.right;
            Vector3 side = Vector3.Cross(dir, up).normalized;
            Vector3 normal = Vector3.Cross(side, dir).normalized;
            for (int s = 0; s <= sides; s++)
            {
                float a = s * Mathf.PI * 2f / sides;
                v.Add(path[i] + (side * Mathf.Cos(a) + normal * Mathf.Sin(a)) * radius[i]);
            }
        }
        int row = sides + 1;
        for (int i = 0; i < path.Length - 1; i++)
        for (int s = 0; s < sides; s++)
        {
            int a = i * row + s, b = a + row;
            t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        return Finish(key, v, t);
    }

    // Points along a quadratic Bezier curve
    public static Vector3[] Curve(Vector3 a, Vector3 control, Vector3 b, int count)
    {
        var p = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float u = i / (float)(count - 1);
            p[i] = (1 - u) * (1 - u) * a + 2 * (1 - u) * u * control + u * u * b;
        }
        return p;
    }

    public static float[] Taper(int count, float from, float to)
    {
        var r = new float[count];
        for (int i = 0; i < count; i++) r[i] = Mathf.Lerp(from, to, i / (float)(count - 1));
        return r;
    }

    // ── Leaf: grows along +Z, arches up then droops; double-sided ──────────────
    public static Mesh Leaf(string key, float length, float width, float rise, float droop, int segments = 10, float tipRound = 0.25f)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            float w = width * Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 1f - tipRound * 0.3f, u)) * 0.5f;
            float y = rise * Mathf.Sin(u * Mathf.PI * 0.6f) - droop * u * u;
            float z = length * u;
            float fold = w * 0.18f; // slight V along the midrib
            v.Add(new Vector3(-w, y + fold, z));
            v.Add(new Vector3(0f, y, z));
            v.Add(new Vector3(w, y + fold, z));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 3, b = a + 3;
            t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1, a + 1, b + 1, a + 2, a + 2, b + 1, b + 2 });
        }
        // Back side: its own vertices (so its normals face down), reversed triangles
        int offset = v.Count, count = t.Count;
        v.AddRange(v.GetRange(0, offset));
        for (int i = 0; i < count; i += 3) t.AddRange(new[] { t[i] + offset, t[i + 2] + offset, t[i + 1] + offset });
        return Finish(key, v, t);
    }

    // ── Blob: low-poly faceted rock/canopy (icosphere with jitter) ─────────────
    public static Mesh Blob(string key, int seed, float jitter = 0.18f, int subdivisions = 1)
    {
        float p = (1f + Mathf.Sqrt(5f)) / 2f;
        var v = new List<Vector3>
        {
            new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
            new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
            new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1),
        };
        var t = new List<int>
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
        };
        for (int d = 0; d < subdivisions; d++)
        {
            var mid = new Dictionary<long, int>();
            var nt = new List<int>();
            int Mid(int a, int b)
            {
                long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(k, out int i)) return i;
                v.Add((v[a] + v[b]) * 0.5f);
                return mid[k] = v.Count - 1;
            }
            for (int i = 0; i < t.Count; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            t = nt;
        }
        var rng = new System.Random(seed);
        for (int i = 0; i < v.Count; i++)
            v[i] = v[i].normalized * (0.5f * (1f + ((float)rng.NextDouble() * 2f - 1f) * jitter));
        return Finish(key, v, t, flat: true);
    }

    // ── Gable prism: triangle (width x height) facing -Z, depth along +Z ───────
    public static Mesh Gable(string key, float width, float height, float depth)
    {
        float w = width * 0.5f;
        var v = new List<Vector3>
        {
            new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(0, height, 0),
            new Vector3(-w, 0, depth), new Vector3(w, 0, depth), new Vector3(0, height, depth),
        };
        var t = new List<int> { 0, 2, 1, 3, 4, 5, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4, 0, 1, 4, 0, 4, 3 };
        return Finish(key, v, t, flat: true);
    }

    // Row of small upward fins along +X (bai raka / naga scales on a gable edge)
    public static Mesh Fins(string key, float length, int count, float height, float thickness)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        float step = length / count;
        for (int i = 0; i < count; i++)
        {
            float x0 = -length * 0.5f + i * step;
            int b = v.Count;
            v.Add(new Vector3(x0, 0, -thickness)); v.Add(new Vector3(x0 + step, 0, -thickness)); v.Add(new Vector3(x0 + step * 0.8f, height, -thickness));
            v.Add(new Vector3(x0, 0, thickness)); v.Add(new Vector3(x0 + step, 0, thickness)); v.Add(new Vector3(x0 + step * 0.8f, height, thickness));
            t.AddRange(new[] { b, b + 2, b + 1, b + 3, b + 4, b + 5, b, b + 3, b + 5, b, b + 5, b + 2, b + 1, b + 2, b + 5, b + 1, b + 5, b + 4 });
        }
        return Finish(key, v, t, flat: true);
    }

    // Tapered roof slab: a thick panel that is wider at the eave (bottom) than the ridge
    public static Mesh RoofPanel(string key, float eaveWidth, float ridgeWidth, float slope, float thickness)
    {
        float e = eaveWidth * 0.5f, r = ridgeWidth * 0.5f;
        var v = new List<Vector3>
        {
            new Vector3(-e, 0, 0), new Vector3(e, 0, 0), new Vector3(r, 0, slope), new Vector3(-r, 0, slope),
            new Vector3(-e, thickness, 0), new Vector3(e, thickness, 0), new Vector3(r, thickness, slope), new Vector3(-r, thickness, slope),
        };
        var t = new List<int>
        {
            4, 6, 5, 4, 7, 6,   0, 1, 2, 0, 2, 3,
            0, 4, 5, 0, 5, 1,   1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3,   3, 7, 4, 3, 4, 0,
        };
        return Finish(key, v, t, flat: true);
    }
}
