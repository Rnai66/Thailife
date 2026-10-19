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
        if (existing != null) return existing;

        bool urp = GraphicsSettings.defaultRenderPipeline != null;
        var material = new Material(Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard")) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
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
        return mesh;
    }
}
