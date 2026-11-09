using UnityEditor;
using UnityEngine;

// Sculpted figures made with an image-to-3D AI (TRELLIS, MIT licence) from photos, kept in
// Assets/Art/Models/AI as <name>.json (mesh: Unity axes, front +Z, base at y 0, 1 unit tall) and
// <name>.png (its painted texture). Built into a saved Mesh and a URP Lit material on first use.
public static class AiModels
{
    private const string Folder = "Assets/Art/Models/AI";

    [System.Serializable]
    private class Data
    {
        public string name;
        public float[] aspect, pos, nrm, uv;
        public int[] tri;
    }

    private static readonly System.Collections.Generic.HashSet<string> built = new System.Collections.Generic.HashSet<string>();

    public static bool Has(string name) => AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{name}.json") != null;

    // Place the figure standing at `at` (its base), `height` tall, facing local +Z turned by `yaw`
    public static GameObject Place(string name, Transform parent, Vector3 at, float height, float yaw = 0f,
        float smoothness = 0.3f, float metallic = 0f)
    {
        var mesh = MeshFor(name);
        if (mesh == null) return null;
        return MeshKit.Place("AI_" + name, parent, mesh, MaterialFor(name, smoothness, metallic), at,
            new Vector3(0f, yaw, 0f), Vector3.one * height);
    }

    private static Mesh MeshFor(string name)
    {
        string meshPath = $"Assets/Art/Meshes/AI/{name}.asset";
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{name}.json");
        if (json == null) { Debug.LogWarning("AiModels: missing " + name); return null; }
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null && existing.vertexCount > 0 && built.Contains(name)) return existing; // once per build run
        built.Add(name);
        var d = JsonUtility.FromJson<Data>(json.text);
        int n = d.pos.Length / 3;
        var v = new Vector3[n];
        var nr = new Vector3[n];
        var uv = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            v[i] = new Vector3(d.pos[i * 3], d.pos[i * 3 + 1], d.pos[i * 3 + 2]);
            nr[i] = new Vector3(d.nrm[i * 3], d.nrm[i * 3 + 1], d.nrm[i * 3 + 2]);
            uv[i] = new Vector2(d.uv[i * 2], d.uv[i * 2 + 1]);
        }
        var mesh = new Mesh { indexFormat = n > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.vertices = v;
        mesh.normals = nr;
        mesh.uv = uv;
        mesh.triangles = d.tri;
        mesh.RecalculateBounds();
        return MeshKit.SaveCombined("AI/" + name, mesh);
    }

    private static Material MaterialFor(string name, float smoothness, float metallic)
    {
        var mat = PrimitiveKit.Mat("AI_" + name, Color.white, smoothness, metallic);
        string texPath = $"{Folder}/{name}.png";
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null && (importer.maxTextureSize != 1024 || !importer.mipmapEnabled))
        {
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }
}
