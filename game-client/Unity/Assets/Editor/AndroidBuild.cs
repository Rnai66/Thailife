using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu: Thai Life > Build Android ...
//   · Build Android APK (ลองบนมือถือ)    — an .apk to install on your own phone (debug-signed if no keystore)
//   · Build Android Release (Google Play) — the signed .aab to upload to the Play Console
// Same game as iOS: package com.rnai66.thailife, portrait, the same scenes and icon. The version name
// follows iOS (IOSBuild.Version); the version code is the build time yyMMddHH, so every upload is higher.
//
// Signing: the upload keystore stays OFF the repo (the repo is public). Put its details in
// UserSettings/AndroidSigning.json (UserSettings/ is git-ignored):
//   { "keystore": "/Users/you/keys/thailife-upload.keystore", "alias": "upload",
//     "storePass": "...", "keyPass": "..." }
public static class AndroidBuild
{
    private const string PackageId = "com.rnai66.thailife";
    private const string OutputDir = "Builds/Android";
    private const string SigningFile = "UserSettings/AndroidSigning.json";
    private const int MinSdk = 29;    // Android 10+: sharing pictures without storage permission (AndroidShare)
    private const int TargetSdk = 36; // Google Play needs a recent target API for new apps and updates

    [Serializable]
    private class Signing { public string keystore, alias, storePass, keyPass; }

    [MenuItem("Thai Life/Build Android APK (ลองบนมือถือ)")]
    private static void BuildApk() => Build(release: false);

    [MenuItem("Thai Life/Build Android Release (Google Play .aab)")]
    private static void BuildAab() => Build(release: true);

    public static void BuildReleaseBatch() => Build(release: true); // -executeMethod AndroidBuild.BuildReleaseBatch

    private static void Build(bool release)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Say("Unity กำลังคอมไพล์สคริปต์อยู่ รอสักครู่แล้วกดใหม่");
            return;
        }
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Say("ยังไม่ได้ติดตั้ง Android Build Support\n\nเปิด Unity Hub → Installs → Unity 6000.6.6f1 → ⚙ Add modules → ติ๊ก Android Build Support (พร้อม OpenJDK และ Android SDK & NDK Tools) → Install แล้วเปิด Unity ใหม่");
            return;
        }
        Signing sign = null;
        if (File.Exists(SigningFile))
        {
            try { sign = JsonUtility.FromJson<Signing>(File.ReadAllText(SigningFile)); }
            catch (Exception e) { Debug.LogError("[ThaiLife] " + SigningFile + ": " + e.Message); }
            if (sign != null && (string.IsNullOrEmpty(sign.keystore) || !File.Exists(sign.keystore))) sign = null;
        }
        if (release && sign == null)
        {
            Say("ยังไม่มีกุญแจเซ็นแอป (upload keystore)\n\nสร้างตามขั้นตอนในไฟล์ docs/GooglePlay.md แล้วใส่รายละเอียดใน\n" + SigningFile);
            return;
        }

        if (!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[ThaiLife] Could not switch to Android.");
            return;
        }

        Configure(release);
        if (sign != null)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = sign.keystore;
            PlayerSettings.Android.keystorePass = sign.storePass;
            PlayerSettings.Android.keyaliasName = sign.alias;
            PlayerSettings.Android.keyaliasPass = sign.keyPass;
        }
        else PlayerSettings.Android.useCustomKeystore = false; // debug key: fine for your own phone, not for Play

        EditorUserBuildSettings.buildAppBundle = release;
        Directory.CreateDirectory(OutputDir);
        string file = $"ThaiLife9-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}" + (release ? ".aab" : ".apk");
        string path = Path.Combine(OutputDir, file);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = IOSBuild.BuildScenes(),
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        // never leave the passwords in ProjectSettings
        PlayerSettings.Android.keystorePass = "";
        PlayerSettings.Android.keyaliasPass = "";

        if (report.summary.result == BuildResult.Succeeded && !File.Exists(path))
        {
            // Unity says success but wrote nothing: usually the Android module was installed while this Editor was open
            Debug.LogError("[ThaiLife] Android build produced no file. ปิด Unity แล้วเปิดใหม่ (ถ้าเพิ่งติดตั้ง Android Build Support) แล้ว build อีกครั้ง");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Android build", "Build ไม่ได้ไฟล์ออกมา\n\nปิด Unity แล้วเปิดใหม่ (หลังติดตั้ง Android Build Support ต้องเปิดใหม่ครั้งหนึ่ง) แล้วกด build อีกครั้ง", "OK");
        }
        else if (report.summary.result == BuildResult.Succeeded)
        {
            long mb = File.Exists(path) ? new FileInfo(path).Length / (1024 * 1024) : 0;
            Debug.Log($"[ThaiLife] Android {(release ? "AAB" : "APK")} ready: {Path.GetFullPath(path)} ({mb} MB)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
        }
        else Debug.LogError($"[ThaiLife] Android build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
    }

    private static void Configure(bool release)
    {
        PlayerSettings.productName = "ThaiLife9";
        PlayerSettings.bundleVersion = IOSBuild.Version;
        PlayerSettings.Android.bundleVersionCode = int.Parse(DateTime.Now.ToString("yyMMddHH"));
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
        var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Branding/AppIcon.png");
        if (appIcon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { appIcon }, IconKind.Any);

        // 64-bit IL2CPP (Google Play requires arm64), plus 32-bit for older phones
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdk;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetSdk;
        // big game: the scenes and art go into install-time asset packs so the base stays under Play's limit
        PlayerSettings.Android.splitApplicationBinary = release;

        // portrait, as on iOS
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.runInBackground = true;
        PlayerSettings.Android.forceInternetPermission = true;
        // the server is https; plain http only in development builds (a Nakama on the LAN)
        PlayerSettings.insecureHttpOption = release ? InsecureHttpOption.NotAllowed : InsecureHttpOption.DevelopmentOnly;
    }

    private static void Say(string message)
    {
        if (Application.isBatchMode) Debug.LogError("[ThaiLife] " + message);
        else EditorUtility.DisplayDialog("Build Android", message, "OK");
    }
}
