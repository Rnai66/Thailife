using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the Prototype 0.1 scene (GDD section 7) so it doesn't have to be assembled by hand.
// Menu: Thai Life > Advanced > Reset Scene to Prototype (destructive: replaces the whole scene)
// Runs automatically once if Assets/Scenes/ThaiVillage.unity does not exist yet.
[InitializeOnLoad]
public static class ThaiVillageSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ThaiVillage.unity";
    private const string MaterialFolder = "Assets/Art/Materials";
    private const string SettingsFolder = "Assets/Settings";
    private const string AutoBuildKey = "ThaiLife.AutoBuildAttempted";

    static ThaiVillageSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(AutoBuildKey, false)) return;
            SessionState.SetBool(AutoBuildKey, true);

            if (!File.Exists(ScenePath))
                Build(askBeforeOverwrite: false);
        };
    }

    [MenuItem("Thai Life/Advanced/Reset Scene to Prototype (deletes everything)")]
    private static void BuildFromMenu() => Build(askBeforeOverwrite: true);

    [MenuItem("Thai Life/Advanced/Setup URP")]
    private static void SetupUrpFromMenu() => EnsureUrp();

    private static void Build(bool askBeforeOverwrite)
    {
        if (askBeforeOverwrite && File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("รีเซ็ตฉาก",
                "จะลบทุกอย่างในฉาก ThaiVillage (หมู่บ้าน, Network, ปุ่มควบคุม ฯลฯ) แล้วสร้างฉากต้นแบบใหม่\n\nถ้าแค่ต้องการเติมส่วนที่หาย ให้ใช้ Thai Life → Repair Scene แทน",
                "ลบและรีเซ็ต", "ยกเลิก"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureUrp();
        Directory.CreateDirectory(MaterialFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting
        var sun = new GameObject("Directional Light");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.84f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Environment (positions/scales from GDD 7.2)
        CreatePrimitive(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(5f, 1f, 5f),
            GetMaterial("Grass", new Color(0.36f, 0.62f, 0.29f)));

        CreatePrimitive(PrimitiveType.Cube, "House", new Vector3(4f, 1f, 4f), new Vector3(3f, 2f, 3f),
            GetMaterial("Wood", new Color(0.55f, 0.36f, 0.2f)));

        // Simple pitched roof: a box rotated 45° sitting on the house
        var roof = CreatePrimitive(PrimitiveType.Cube, "Roof", new Vector3(4f, 2.6f, 4f), new Vector3(2.4f, 2.4f, 3.4f),
            GetMaterial("RoofTile", new Color(0.72f, 0.25f, 0.15f)));
        roof.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        var tree = new GameObject("Tree");
        var trunk = CreatePrimitive(PrimitiveType.Cylinder, "TreeTrunk", new Vector3(-4f, 1f, 3f), new Vector3(0.4f, 2f, 0.4f),
            GetMaterial("Bark", new Color(0.4f, 0.26f, 0.13f)));
        var top = CreatePrimitive(PrimitiveType.Sphere, "TreeTop", new Vector3(-4f, 3f, 3f), new Vector3(2f, 2f, 2f),
            GetMaterial("Leaves", new Color(0.18f, 0.5f, 0.2f)));
        trunk.transform.SetParent(tree.transform, true);
        top.transform.SetParent(tree.transform, true);

        // Player: capsule mesh, CharacterController instead of CapsuleCollider, no Rigidbody
        var player = CreatePrimitive(PrimitiveType.Capsule, "Player", new Vector3(0f, 1f, 0f), Vector3.one,
            GetMaterial("Player", new Color(0.95f, 0.75f, 0.2f)));
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero; // capsule primitive's pivot is at its middle
        var controller = player.AddComponent<PlayerController>();

        // Camera
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = player.transform.position + new Vector3(0f, 4f, -6f);
        camGo.transform.LookAt(player.transform.position + Vector3.up * 1.2f);
        var follow = camGo.AddComponent<CameraFollow>();

        // Wire references (GDD 7.5)
        SetReference(follow, "target", player.transform);
        SetReference(controller, "cameraTransform", camGo.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        Selection.activeGameObject = player;
        Debug.Log($"[ThaiLife] Built {ScenePath}. Press Play and move with WASD / arrow keys.");
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
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
        Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SetReference(Object component, string field, Object value)
    {
        var so = new SerializedObject(component);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == path) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(path, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
    }

    // Creates and assigns a URP asset if the project has none (e.g. opened without the URP template).
    private static void EnsureUrp()
    {
        if (GraphicsSettings.defaultRenderPipeline != null) return;

        Directory.CreateDirectory(SettingsFolder);
        AssetDatabase.Refresh();
        string rendererPath = $"{SettingsFolder}/Mobile_Renderer.asset";
        string pipelinePath = $"{SettingsFolder}/Mobile_RPAsset.asset";

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);

            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.shadowDistance = 40f; // keep shadows cheap for mobile
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
            AssetDatabase.SaveAssets();
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        Debug.Log("[ThaiLife] URP asset assigned in Graphics and Quality settings.");
    }
}
