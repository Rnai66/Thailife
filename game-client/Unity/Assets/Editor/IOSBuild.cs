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
    private const string Version = "1.2"; // marketing version shown in TestFlight / the App Store

    [MenuItem("Thai Life/Build iOS (Xcode project)")]
    private static void Build() => Build(release: false);

    // Release build for TestFlight / the App Store: no development player, a fresh build number
    // (yyMMddHHmm, always higher than the last upload). Archive and upload it from Xcode.
    [MenuItem("Thai Life/Build iOS Release (TestFlight)")]
    private static void BuildRelease() => Build(release: true);

    public static void BuildReleaseBatch() => Build(release: true); // -executeMethod IOSBuild.BuildReleaseBatch

    private static void Build(bool release)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorUtility.DisplayDialog("Build iOS", "Unity กำลังคอมไพล์สคริปต์อยู่ — รอสักครู่ให้เสร็จ แล้วกด Build iOS อีกครั้ง", "OK");
            return;
        }

        if (!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("[ThaiLife] Could not switch to iOS. Is iOS Build Support installed?");
            return;
        }

        Configure();
        if (release)
        {
            PlayerSettings.iOS.buildNumber = System.DateTime.Now.ToString("yyMMddHHmm"); // to the minute, above the first upload (2026102114)
            Debug.Log($"[ThaiLife] Release build {PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber})");
        }

        var options = new BuildPlayerOptions
        {
            scenes = BuildScenes(),
            locationPathName = OutputPath,
            target = BuildTarget.iOS,
            options = release ? BuildOptions.None : BuildOptions.Development // development allows http to a local Nakama
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
        PlayerSettings.productName = "ThaiLife9"; // App Store name
        PlayerSettings.bundleVersion = Version;
        // App icon from the game artwork (Resources/Branding/AppIcon.png, 1024x1024)
        var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Branding/AppIcon.png");
        if (appIcon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { appIcon }, IconKind.Any);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;

        // Portrait (held upright, one-handed like PUBG Mobile's vertical mode): joystick bottom left,
        // action cluster bottom right; ScreenFit re-fits the landscape-designed UI
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

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
        // Only standard HTTPS/TLS: no export-compliance questions on every TestFlight upload
        root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

        plist.WriteToFile(plistPath);
    }
#endif
}
