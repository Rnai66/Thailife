using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Setup Nakama Connection
// Creates Assets/Settings/NakamaConfig.asset (git-ignored) and a "Network" object in the open scene.
public static class NakamaSetup
{
    private const string ConfigPath = "Assets/Settings/NakamaConfig.asset";

    [MenuItem("Thai Life/Setup Nakama Connection")]
    private static void Setup()
    {
        var config = AssetDatabase.LoadAssetAtPath<NakamaConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<NakamaConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
        }

        // The local Docker server uses plain http; allow it in the Editor and development builds only.
        if (PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;

        var connection = Object.FindFirstObjectByType<NakamaConnection>();
        if (connection == null)
        {
            var go = new GameObject("Network");
            Undo.RegisterCreatedObjectUndo(go, "Add Network");
            connection = go.AddComponent<NakamaConnection>();
        }

        var so = new SerializedObject(connection);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(connection.gameObject.scene);

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log("[ThaiLife] Fill in Host and Server Key on NakamaConfig, save the scene (Cmd+S), then press Play.");
    }
}
