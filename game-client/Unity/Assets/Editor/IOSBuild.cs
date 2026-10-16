using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

// Menu: Thai Life > Build iOS (Xcode project)
// Configures iOS player settings and exports a development Xcode project to Builds/iOS.
// Install on the phone with Xcode (open Builds/iOS/Unity-iPhone.xcodeproj → Run) or xcodebuild.
public static class IOSBuild
{
    private const string BundleId = "com.rnai66.thailife";
    private const string TeamId = "H4X9654279";
    private const string OutputPath = "Builds/iOS";

    [MenuItem("Thai Life/Build iOS (Xcode project)")]
    private static void Build()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("[ThaiLife] Could not switch to iOS. Is iOS Build Support installed?");
            return;
        }

        Configure();

        var options = new BuildPlayerOptions
        {
            scenes = BuildScenes(),
            locationPathName = OutputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.Development // allows http to the local Nakama server
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        string xcodeProject = Path.Combine(OutputPath, "Unity-iPhone.xcodeproj");

        // Unity can report Succeeded even when the iOS postprocess threw (e.g. module installed
        // while the Editor was open), so also check that the Xcode project really exists.
        if (report.summary.result == BuildResult.Succeeded && Directory.Exists(xcodeProject))
            Debug.Log($"[ThaiLife] iOS Xcode project ready: {Path.GetFullPath(OutputPath)}");
        else if (report.summary.result == BuildResult.Succeeded)
            Debug.LogError("[ThaiLife] iOS build produced no Xcode project. If iOS Build Support was just installed, restart Unity and build again.");
        else
            Debug.LogError($"[ThaiLife] iOS build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
    }

    // All enabled scenes in Build Settings (village first = the scene the app starts in)
    internal static string[] BuildScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) list.Add(scene.path);
        return list.ToArray();
    }

    private static void Configure()
    {
        PlayerSettings.productName = "Thai Life";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;

        // Landscape only: joystick left, buttons right
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        PlayerSettings.runInBackground = true;
        if (PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
    }

#if UNITY_IOS
    // iOS asks permission before an app talks to devices on the local network (the Mac running Nakama).
    [PostProcessBuild]
    private static void AddLocalNetworkPermission(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(path, "Info.plist");
        if (!File.Exists(plistPath)) return; // export failed earlier; Build() reports it
        var plist = new UnityEditor.iOS.Xcode.PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;

        root.SetString("NSLocalNetworkUsageDescription", "เชื่อมต่อเซิร์ฟเวอร์เกมในวง Wi-Fi เดียวกัน (สำหรับทดสอบ)");

        var ats = root["NSAppTransportSecurity"] as UnityEditor.iOS.Xcode.PlistElementDict
                  ?? root.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsLocalNetworking", true);

        plist.WriteToFile(plistPath);
    }
#endif
}
