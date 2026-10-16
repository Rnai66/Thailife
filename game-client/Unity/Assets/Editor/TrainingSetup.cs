using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Add Punching Bag
// Builds a hanging punching bag (กระสอบทราย) on a stand from primitives — career training tier 2.
// BuildBag is also used by VillageBuilder for the boxing camp's heavy bags (tier 3).
public static class TrainingSetup
{
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

        var root = BuildBag("Training_PunchingBag", null, Position, "bag", 3, 4, new Color(0.7f, 0.12f, 0.1f));
        Undo.RegisterCreatedObjectUndo(root, "Add Punching Bag");

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[ThaiLife] Punching bag added. Save (Cmd+S), press Play, walk up to it and punch (C / J) or kick (B / K).");
    }

    public static GameObject BuildBag(string name, Transform parent, Vector3 position, string targetId, int requiredLevel, int xpPerHit, Color bagColor)
    {
        var metal = PrimitiveKit.Mat("Metal", new Color(0.35f, 0.35f, 0.38f));
        var leather = PrimitiveKit.Mat(bagColor == new Color(0.7f, 0.12f, 0.1f) ? "BagLeather" : $"BagLeather_{targetId}", bagColor);

        var root = new GameObject(name);
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        root.AddComponent<TrainingTarget>().Configure(targetId, requiredLevel, xpPerHit);

        // Stand: post + arm (static, blocks walking)
        PrimitiveKit.Cylinder("Post", root.transform, new Vector3(0.9f, 1.45f, 0f), new Vector3(0.15f, 1.45f, 0.15f), metal);
        PrimitiveKit.Box("Arm", root.transform, new Vector3(0.45f, 2.9f, 0f), new Vector3(1.05f, 0.1f, 0.1f), metal);

        // Swinging part: pivot at the hook, chain and bag hang below it
        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 2.85f, 0f);
        pivot.AddComponent<PunchingBag>();

        var chain = PrimitiveKit.Cylinder("Chain", pivot.transform, new Vector3(0f, -0.2f, 0f), new Vector3(0.03f, 0.2f, 0.03f), metal);
        Object.DestroyImmediate(chain.GetComponent<Collider>());
        PrimitiveKit.Cylinder("Bag", pivot.transform, new Vector3(0f, -1.2f, 0f), new Vector3(0.7f, 0.8f, 0.7f), leather);

        return root;
    }
}
