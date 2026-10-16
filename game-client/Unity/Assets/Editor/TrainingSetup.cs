using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: Thai Life > Add Punching Bag
// Builds a hanging punching bag (กระสอบทราย) on a stand from primitives, for solo boxing practice.
public static class TrainingSetup
{
    private const string MaterialFolder = "Assets/Art/Materials";
    private static readonly Vector3 Position = new Vector3(2.5f, 0f, -2.5f);

    [MenuItem("Thai Life/Add Punching Bag")]
    private static void AddPunchingBag()
    {
        var existing = GameObject.Find("Training_PunchingBag");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog("Punching Bag", "มีกระสอบทรายในฉากแล้ว", "OK");
            return;
        }

        var metal = GetMaterial("Metal", new Color(0.35f, 0.35f, 0.38f));
        var leather = GetMaterial("BagLeather", new Color(0.7f, 0.12f, 0.1f));

        var root = new GameObject("Training_PunchingBag");
        root.transform.position = Position;
        Undo.RegisterCreatedObjectUndo(root, "Add Punching Bag");

        // Stand: post + arm (static, blocks walking)
        Create(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0.9f, 1.45f, 0f), new Vector3(0.15f, 1.45f, 0.15f), metal);
        Create(PrimitiveType.Cube, "Arm", root.transform, new Vector3(0.45f, 2.9f, 0f), new Vector3(1.05f, 0.1f, 0.1f), metal);

        // Swinging part: pivot at the hook, chain and bag hang below it
        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 2.85f, 0f);
        pivot.AddComponent<PunchingBag>();

        var chain = Create(PrimitiveType.Cylinder, "Chain", pivot.transform, new Vector3(0f, -0.2f, 0f), new Vector3(0.03f, 0.2f, 0.03f), metal);
        Object.DestroyImmediate(chain.GetComponent<Collider>());
        Create(PrimitiveType.Cylinder, "Bag", pivot.transform, new Vector3(0f, -1.2f, 0f), new Vector3(0.7f, 0.8f, 0.7f), leather);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[ThaiLife] Punching bag added. Save (Cmd+S), press Play, walk up to it and punch (C / J) or kick (B / K).");
    }

    private static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        bool urp = GraphicsSettings.defaultRenderPipeline != null;
        var material = new Material(Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard")) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
