using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Small helpers for building props out of Unity primitives in editor menus.
public static class PrimitiveKit
{
    private const string MaterialFolder = "Assets/Art/Materials";

    // Shared material asset (created once under Assets/Art/Materials)
    public static Material Mat(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return Finish(name, existing);

        bool urp = GraphicsSettings.defaultRenderPipeline != null;
        var material = new Material(Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard")) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return Finish(name, material);
    }

    // Surface finish guessed from the material's name: gold and metal shine, glass/water/neon
    // are glossy, glazed tiles and lacquer have a sheen, everything else is a soft matte
    private static Material Finish(string name, Material m)
    {
        string n = name.ToLowerInvariant();
        float smooth = 0.22f, metal = 0f;
        if (n.Contains("gold") || n.Contains("brass")) { smooth = 0.7f; metal = 0.85f; }
        else if (n.Contains("metal") || n.Contains("steel") || n.Contains("zinc") || n.Contains("tin") || n.Contains("chrome")) { smooth = 0.55f; metal = 0.6f; }
        else if (n.Contains("glass") || n.Contains("window") || n.Contains("water") || n.Contains("neon") || n.Contains("screen")) smooth = 0.85f;
        else if (n.Contains("roof") || n.Contains("tile") || n.Contains("lacquer") || n.Contains("paint") || n.Contains("temple")) smooth = 0.55f;
        else if (n.Contains("asphalt") || n.Contains("dirt") || n.Contains("grass") || n.Contains("soil") || n.Contains("mud")) smooth = 0.08f;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Same, but (re)applies a surface finish every build: smoothness 0 = matte, 1 = glossy;
    // metallic for gold leaf. GPU instancing on so many copies stay cheap on phones.
    public static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
    {
        var material = Mat(name, color);
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    // Light bulbs, neon and lanterns: emissive, so the bloom makes them glow at night
    public static Material Glow(string name, Color color, float intensity = 2f)
    {
        var material = Mat(name, color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    public static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 euler = default, bool collider = true) =>
        Create(PrimitiveType.Cube, name, parent, localPosition, scale, material, euler, collider);

    public static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 euler = default, bool collider = true) =>
        Create(PrimitiveType.Cylinder, name, parent, localPosition, scale, material, euler, collider);

    public static GameObject Sphere(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool collider = false) =>
        Create(PrimitiveType.Sphere, name, parent, localPosition, scale, material, default, collider);

    public static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale,
        Material material, Vector3 euler = default, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        // Boxes get bevelled edges (size baked into the mesh, the collider keeps the box size)
        if (type == PrimitiveType.Cube && MeshKit.TryChamferBox(scale, out var chamfered))
        {
            go.GetComponent<MeshFilter>().sharedMesh = chamfered;
            go.transform.localScale = Vector3.one;
            go.GetComponent<BoxCollider>().size = scale;
        }
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Group(string name, Transform parent, Vector3 localPosition, float yaw = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    // World-space sign text (TextMesh). With yaw 0 it reads correctly from the parent's -Z side,
    // i.e. on the front face of a building whose front points along local -Z.
    public static TextMesh Label(string text, Transform parent, Vector3 localPosition, float size, Color color, float yaw = 0f)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        go.AddComponent<ReadableText>(); // turns round instead of reading mirrored from behind
        return mesh;
    }
}
