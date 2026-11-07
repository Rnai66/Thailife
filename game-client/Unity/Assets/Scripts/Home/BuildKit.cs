using System.Collections.Generic;
using UnityEngine;

// What each buildable piece (build.lua's CATALOG) looks like, made at run time from simple shapes.
// A piece is centred on its own origin, runs along its local X (its `length`), faces local -Z, and
// sits on the ground at y 0. Paint 0 is the material's own colour (wood, bamboo, brick…); the others
// repaint the main parts. A ghost (placing / selecting) is the same shape in one flat colour, no colliders.
public static class BuildKit
{
    private static readonly Color[] Paints =
    {
        Color.clear,                              // natural
        new Color(0.95f, 0.95f, 0.93f),           // white
        new Color(0.22f, 0.5f, 0.27f),            // green
        new Color(0.7f, 0.15f, 0.12f),            // red
        new Color(0.2f, 0.35f, 0.7f),             // blue
        new Color(0.12f, 0.12f, 0.13f),           // black
        new Color(0.95f, 0.9f, 0.75f),            // cream
    };

    public static Color PaintColor(int paint, string kind) => paint > 0 && paint < Paints.Length ? Paints[paint] : Natural(kind);

    private static Color Natural(string kind)
    {
        switch (kind)
        {
            case "fence_bamboo": return new Color(0.78f, 0.7f, 0.38f);
            case "fence_iron": case "gate_iron": return new Color(0.15f, 0.15f, 0.16f);
            case "fence_brick": return new Color(0.72f, 0.38f, 0.26f);
            case "fence_wire": return new Color(0.6f, 0.6f, 0.58f);
            case "carport": return new Color(0.75f, 0.77f, 0.8f);
            case "sala": return new Color(0.55f, 0.33f, 0.17f);
            case "planter": return new Color(0.62f, 0.35f, 0.22f);
            default: return new Color(0.58f, 0.38f, 0.2f); // wood
        }
    }

    private static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
    private static readonly Dictionary<Color, Material> flat = new Dictionary<Color, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { lit.Clear(); flat.Clear(); }

    private static Material Lit(Color c)
    {
        if (lit.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
        return lit[c] = m;
    }

    private static Material Flat(Color c)
    {
        if (flat.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        m = new Material(shader) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return flat[c] = m;
    }

    // Build a piece under `parent`. ghost: one flat colour, no colliders.
    public static GameObject Make(string kind, int paint, Transform parent, Color? ghost = null)
    {
        var root = new GameObject("Built_" + kind);
        root.transform.SetParent(parent, false);
        var main = PaintColor(paint, kind);
        Material M(Color c) => ghost.HasValue ? Flat(ghost.Value) : Lit(c);
        var paintMat = M(main);
        var dark = M(new Color(0.2f, 0.2f, 0.21f));
        var wood = M(new Color(0.5f, 0.32f, 0.17f));
        var grey = M(new Color(0.68f, 0.68f, 0.66f));
        bool solid = !ghost.HasValue;

        void Box(Vector3 at, Vector3 size, Material m, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }
        void Cyl(Vector3 at, float radius, float height, Material m, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = m;
        }
        void Block(Vector3 center, Vector3 size)
        {
            if (!solid) return;
            var c = root.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        switch (kind)
        {
            case "fence_wood": // ระแนง: two posts, two rails, pointed pickets
                foreach (float x in new[] { -1f, 1f }) Box(new Vector3(x, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), paintMat);
                foreach (float y in new[] { 0.35f, 0.9f }) Box(new Vector3(0f, y, 0.05f), new Vector3(2f, 0.08f, 0.04f), paintMat);
                for (int i = 0; i < 9; i++)
                {
                    float x = -0.88f + i * 0.22f;
                    Box(new Vector3(x, 0.55f, -0.01f), new Vector3(0.13f, 1.05f, 0.025f), paintMat);
                    Box(new Vector3(x, 1.08f, -0.01f), new Vector3(0.09f, 0.09f, 0.025f), paintMat, new Vector3(0f, 0f, 45f));
                }
                Block(new Vector3(0f, 0.6f, 0f), new Vector3(2f, 1.2f, 0.15f));
                break;
            case "fence_bamboo": // poles side by side, tied to two rails
                for (int i = 0; i < 12; i++) Cyl(new Vector3(-0.92f + i * 0.167f, 0.65f + (i % 2) * 0.05f, 0f), 0.045f, 1.3f + (i % 2) * 0.1f, paintMat);
                foreach (float y in new[] { 0.35f, 1f }) Cyl(new Vector3(0f, y, -0.06f), 0.03f, 2f, paintMat, new Vector3(0f, 0f, 90f));
                Block(new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1.3f, 0.15f));
                break;
            case "fence_wire": // farm fence: posts and four strands
                foreach (float x in new[] { -1f, 1f }) Box(new Vector3(x, 0.65f, 0f), new Vector3(0.1f, 1.3f, 0.1f), paintMat);
                foreach (float y in new[] { 0.3f, 0.6f, 0.9f, 1.2f }) Box(new Vector3(0f, y, -0.06f), new Vector3(2f, 0.015f, 0.015f), dark);
                Block(new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1.3f, 0.12f));
                break;
            case "fence_iron": // square posts, bars with spear tips, top and bottom rails
                foreach (float x in new[] { -1f, 1f }) Box(new Vector3(x, 0.75f, 0f), new Vector3(0.12f, 1.5f, 0.12f), paintMat);
                foreach (float y in new[] { 0.15f, 1.25f }) Box(new Vector3(0f, y, 0f), new Vector3(2f, 0.05f, 0.05f), paintMat);
                for (int i = 0; i < 13; i++)
                {
                    float x = -0.9f + i * 0.15f;
                    Box(new Vector3(x, 0.72f, 0f), new Vector3(0.03f, 1.3f, 0.03f), paintMat);
                    Box(new Vector3(x, 1.4f, 0f), new Vector3(0.06f, 0.06f, 0.03f), paintMat, new Vector3(0f, 0f, 45f));
                }
                Block(new Vector3(0f, 0.75f, 0f), new Vector3(2f, 1.5f, 0.15f));
                break;
            case "fence_brick": // a plastered wall with a cap and end pillars
                Box(new Vector3(0f, 0.75f, 0f), new Vector3(2f, 1.5f, 0.18f), paintMat);
                Box(new Vector3(0f, 1.54f, 0f), new Vector3(2.04f, 0.08f, 0.26f), grey);
                foreach (float x in new[] { -1f, 1f }) Box(new Vector3(x, 0.82f, 0f), new Vector3(0.26f, 1.64f, 0.26f), grey);
                Block(new Vector3(0f, 0.8f, 0f), new Vector3(2.1f, 1.6f, 0.26f));
                break;
            case "gate_wood": // two leaves swung open inward: walk through
            case "gate_iron":
            {
                bool iron = kind == "gate_iron";
                float half = iron ? 1.5f : 1f, h = iron ? 1.6f : 1.2f, leaf = half - 0.1f;
                foreach (float s in new[] { -1f, 1f })
                {
                    Box(new Vector3(s * half, h * 0.55f, 0f), new Vector3(0.22f, h * 1.1f, 0.22f), iron ? grey : paintMat);
                    if (iron) Box(new Vector3(s * half, h * 1.12f, 0f), new Vector3(0.3f, 0.08f, 0.3f), grey);
                    // the leaf, hinged at the post, opened 75 degrees
                    var hinge = Quaternion.Euler(0f, s * -75f, 0f);
                    Vector3 Along(float t, float y) => new Vector3(s * half, 0f, 0f) + hinge * new Vector3(-s * t, y, 0f);
                    for (int i = 0; i < (iron ? 8 : 5); i++)
                    {
                        float t = 0.12f + i * (leaf - 0.15f) / (iron ? 7 : 4);
                        Box(Along(t, h * 0.5f), new Vector3(iron ? 0.03f : 0.13f, h * 0.9f, 0.03f), paintMat, new Vector3(0f, s * -75f, 0f));
                    }
                    foreach (float y in new[] { 0.2f, h * 0.85f })
                        Box(Along(leaf * 0.5f + 0.05f, y), new Vector3(leaf, 0.06f, 0.04f), paintMat, new Vector3(0f, s * -75f, 0f));
                    if (solid)
                    {
                        var c = root.AddComponent<BoxCollider>();
                        c.center = new Vector3(s * half, h * 0.55f, 0f);
                        c.size = new Vector3(0.24f, h * 1.1f, 0.24f);
                    }
                }
                break;
            }
            case "carport": // โรงรถ: four steel posts, a sloping metal roof, a concrete pad
                Box(new Vector3(0f, 0.03f, 0f), new Vector3(3.4f, 0.06f, 5.6f), grey);
                foreach (float x in new[] { -1.6f, 1.6f })
                    foreach (float z in new[] { -2.6f, 2.6f })
                    {
                        Box(new Vector3(x, 1.3f, z), new Vector3(0.12f, 2.6f, 0.12f), paintMat);
                        if (solid) { var c = root.AddComponent<BoxCollider>(); c.center = new Vector3(x, 1.3f, z); c.size = new Vector3(0.14f, 2.6f, 0.14f); }
                    }
                Box(new Vector3(0f, 2.68f, 0f), new Vector3(3.7f, 0.06f, 6f), paintMat, new Vector3(4f, 0f, 0f));
                foreach (float x in new[] { -1.6f, 1.6f }) Box(new Vector3(x, 2.58f, 0f), new Vector3(0.1f, 0.12f, 5.4f), dark);
                break;
            case "sala": // ศาลา: a raised wooden floor, four posts, a two-tier Thai roof, benches
            {
                var roof = M(paint == 0 ? new Color(0.72f, 0.25f, 0.12f) : main);
                var trim = M(new Color(0.9f, 0.7f, 0.28f));
                Box(new Vector3(0f, 0.12f, 0f), new Vector3(3f, 0.24f, 3f), wood);
                foreach (float x in new[] { -1.35f, 1.35f })
                    foreach (float z in new[] { -1.35f, 1.35f })
                    {
                        Box(new Vector3(x, 1.4f, z), new Vector3(0.14f, 2.4f, 0.14f), paintMat);
                        if (solid) { var c = root.AddComponent<BoxCollider>(); c.center = new Vector3(x, 1.4f, z); c.size = new Vector3(0.16f, 2.4f, 0.16f); }
                    }
                foreach (float s in new[] { -1f, 1f })
                {
                    Box(new Vector3(0f, 2.95f, s * 0.95f), new Vector3(3.6f, 0.06f, 2.1f), roof, new Vector3(s * -32f, 0f, 0f));
                    Box(new Vector3(0f, 3.55f, s * 0.45f), new Vector3(2.4f, 0.06f, 1.05f), roof, new Vector3(s * -40f, 0f, 0f));
                }
                Box(new Vector3(0f, 3.9f, 0f), new Vector3(2.5f, 0.1f, 0.1f), trim);
                foreach (float x in new[] { -1.3f, 1.3f }) Box(new Vector3(x, 3.95f, 0f), new Vector3(0.08f, 0.4f, 0.08f), trim, new Vector3(0f, 0f, x > 0f ? -20f : 20f));
                foreach (float s in new[] { -1f, 1f }) Box(new Vector3(s * 1.1f, 0.5f, 0f), new Vector3(0.4f, 0.06f, 2.2f), wood);
                if (solid) { var f = root.AddComponent<BoxCollider>(); f.center = new Vector3(0f, 0.12f, 0f); f.size = new Vector3(3f, 0.24f, 3f); }
                break;
            }
            case "planter": // a long planter box of flowers
            {
                Box(new Vector3(0f, 0.2f, 0f), new Vector3(1f, 0.4f, 0.4f), paintMat);
                Color[] flowers = { new Color(0.95f, 0.3f, 0.5f), new Color(1f, 0.85f, 0.2f), new Color(0.95f, 0.5f, 0.15f), new Color(0.85f, 0.85f, 0.95f) };
                Box(new Vector3(0f, 0.44f, 0f), new Vector3(0.92f, 0.12f, 0.32f), M(new Color(0.25f, 0.5f, 0.2f)));
                for (int i = 0; i < 6; i++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(-0.38f + i * 0.15f, 0.55f + (i % 2) * 0.05f, (i % 2 - 0.5f) * 0.12f);
                    go.transform.localScale = Vector3.one * 0.14f;
                    go.GetComponent<Renderer>().sharedMaterial = M(flowers[i % flowers.Length]);
                }
                Block(new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.5f, 0.4f));
                break;
            }
            default:
                Box(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.2f), paintMat);
                break;
        }
        if (!solid)
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return root;
    }
}
