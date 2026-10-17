using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Thai Life > Setup Nakama Connection
// Creates Assets/Settings/NakamaConfig.asset (git-ignored) and a "Network" object in the open scene.
public static class NakamaSetup
{
    private const string ConfigPath = "Assets/Settings/NakamaConfig.asset";

    [MenuItem("Thai Life/Setup Nakama Connection")]
    internal static void Setup()
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

        var connection = Object.FindAnyObjectByType<NakamaConnection>();
        if (connection == null)
        {
            var go = new GameObject("Network");
            Undo.RegisterCreatedObjectUndo(go, "Add Network");
            connection = go.AddComponent<NakamaConnection>();
        }

        if (connection.GetComponent<MatchSync>() == null)
            Undo.AddComponent<MatchSync>(connection.gameObject);
        if (connection.GetComponent<CareerClient>() == null)
            Undo.AddComponent<CareerClient>(connection.gameObject);
        if (connection.GetComponent<FishingClient>() == null)
            Undo.AddComponent<FishingClient>(connection.gameObject);
        if (connection.GetComponent<WalletClient>() == null)
            Undo.AddComponent<WalletClient>(connection.gameObject);
        if (connection.GetComponent<LifeClient>() == null)
            Undo.AddComponent<LifeClient>(connection.gameObject);
        if (connection.GetComponent<FarmClient>() == null)
            Undo.AddComponent<FarmClient>(connection.gameObject);
        if (connection.GetComponent<CoopClient>() == null)
            Undo.AddComponent<CoopClient>(connection.gameObject);
        if (connection.GetComponent<MusicClient>() == null)
            Undo.AddComponent<MusicClient>(connection.gameObject);
        if (connection.GetComponent<ProfileClient>() == null)
            Undo.AddComponent<ProfileClient>(connection.gameObject);

        // Keep sending/receiving while the window is not focused (two windows side by side)
        PlayerSettings.runInBackground = true;

        var so = new SerializedObject(connection);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(connection.gameObject.scene);

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log("[ThaiLife] Fill in Host and Server Key on NakamaConfig, save the scene (Cmd+S), then press Play.");
    }

    // Builds a small windowed Mac client and launches it as a second player ("-profile mac2").
    [MenuItem("Thai Life/Build && Run Mac Test Client")]
    private static void BuildMacTestClient()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 540;
        PlayerSettings.resizableWindow = true;

        const string appPath = "Builds/Mac/ThaiLifeTest.app";
        var options = new BuildPlayerOptions
        {
            scenes = IOSBuild.BuildScenes(),
            locationPathName = appPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development // development build: allows http to the local server
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"[ThaiLife] Mac build failed: {report.summary.result}");
            return;
        }

        string fullPath = System.IO.Path.GetFullPath(appPath);
        System.Diagnostics.Process.Start("open", $"-n \"{fullPath}\" --args -profile mac2");
        Debug.Log($"[ThaiLife] Launched {fullPath} as profile 'mac2'.");
    }
}
