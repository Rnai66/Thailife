using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Thai Life > Setup Character: turns the Mixamo files in Assets/Characters into the animated
// character the game loads (Resources/Characters/ThaiCharacter.prefab + .controller).
// Expected files (names only need to contain the word): Character (with skin), Idle, Walk, Run,
// Punch, Kick, Jump, Block. Animations downloaded "Without Skin", walk/run with "In Place".
public static class CharacterSetup
{
    private const string SourceFolder = "Assets/Characters";
    private const string OutputFolder = "Assets/Resources/Characters";
    private const float TargetHeight = 1.75f;

    private static readonly string[] Clips = { "idle", "walk", "run", "punch", "kick", "jump", "block" };
    private static readonly string[] Looping = { "idle", "walk", "run", "block" };

    [MenuItem("Thai Life/Setup Character (Mixamo)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Setup Character", "กด Stop (ออกจากโหมด Play) ก่อน แล้วกดเมนูนี้อีกครั้ง", "ตกลง");
            return;
        }

        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Characters");
            EditorUtility.DisplayDialog("Setup Character", "สร้างโฟลเดอร์ Assets/Characters แล้ว\nลากไฟล์ .fbx จาก Mixamo มาใส่ แล้วกดเมนูนี้อีกครั้ง", "ตกลง");
            return;
        }

        var files = Directory.GetFiles(SourceFolder, "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).ToArray();
        string Find(string word) => files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(word));

        string characterPath = Find("character") ?? files.FirstOrDefault(f => !Clips.Any(c => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(c)));
        var missing = Clips.Where(c => Find(c) == null).ToList();
        if (characterPath == null) missing.Insert(0, "character");
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("Setup Character", "ยังขาดไฟล์: " + string.Join(", ", missing) + "\nใน Assets/Characters", "ตกลง");
            return;
        }

        // 1) Humanoid rig for everything; loop the looping clips
        ImportHumanoid(characterPath, false, true);
        foreach (var c in Clips) ImportHumanoid(Find(c), Looping.Contains(c), c != "jump");
        AssetDatabase.Refresh();

        // 2) Animator controller
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Characters");
        string controllerPath = OutputFolder + "/ThaiCharacter.controller";
        var controller = BuildController(controllerPath, c => ClipFrom(Find(c)));

        // 3) Prefab: wrapper root + model scaled to a person's height
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        var root = new GameObject("ThaiCharacter");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.transform.SetParent(root.transform, false);
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (bounds.size.y > 0.01f) instance.transform.localScale = Vector3.one * (TargetHeight / bounds.size.y);
        }
        var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        PrefabUtility.SaveAsPrefabAsset(root, OutputFolder + "/ThaiCharacter.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Setup Character", "ตัวละครพร้อมแล้ว!\nกด Play เพื่อดูตัวละครเดิน วิ่ง ต่อย เตะ", "ตกลง");
        Debug.Log($"[Character] Built from {characterPath}");
    }

    private static void ImportHumanoid(string path, bool loop, bool bakeHeight)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();

        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = loop;
            clip.lockRootRotation = true;           // stay facing where the player faces
            clip.keepOriginalOrientation = true;
            // The game moves the body up for a jump, so the jump clip must not lift it a second time
            clip.lockRootHeightY = bakeHeight;
            clip.keepOriginalPositionY = false;
            clip.heightFromFeet = true;
            // Don't bake forward movement into the pose: the jump/kick clips travel forward and the
            // body would pop back to the start when the clip ends. Unbaked, it's root motion,
            // which the game ignores (the game moves the player itself).
            clip.lockRootPositionXZ = false;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip ClipFrom(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

    private static AnimatorController BuildController(string path, System.Func<string, AnimationClip> clip)
    {
        // Rebuild in place: deleting and re-creating the file leaves the loaded prefab pointing at
        // a dead object ("Animator is not playing an AnimatorController")
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        else
        {
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                if (sub != controller) Object.DestroyImmediate(sub, true);
            controller.parameters = new AnimatorControllerParameter[0];
            controller.layers = new AnimatorControllerLayer[0];
            controller.AddLayer("Base Layer");
        }
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Punch", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;

        // Walk at ~1.6 m/s. Mixamo "Running" is a sprint, so it sits beyond the game's top speed
        // (4 m/s): full joystick is a jog between walk and run, and the run plays a bit slower.
        var loco = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(clip("idle"), 0f);
        tree.AddChild(clip("walk"), 1.6f);
        tree.AddChild(clip("run"), 5.5f);
        var children = tree.children;
        children[2].timeScale = 0.85f;
        tree.children = children;
        sm.defaultState = loco;

        // One-shot actions, sped up to fit the game's cooldowns (punch 0.35 s, kick 0.6 s)
        OneShot(sm, loco, "Punch", clip("punch"), 0.45f);
        OneShot(sm, loco, "Kick", clip("kick"), 0.7f);
        // Jump: the game lifts the body the moment the button is pressed and lands ~0.7 s later,
        // so skip the clip's crouch and fit the rest into the time in the air
        OneShot(sm, loco, "Jump", clip("jump"), 0.7f, startAt: 0.2f, exitAt: 0.95f, blendOut: 0.1f);

        var block = sm.AddState("Block");
        block.motion = clip("block");
        var toBlock = sm.AddAnyStateTransition(block);
        toBlock.AddCondition(AnimatorConditionMode.If, 0f, "Block");
        toBlock.canTransitionToSelf = false;
        toBlock.duration = 0.1f;
        var unblock = block.AddTransition(loco);
        unblock.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");
        unblock.duration = 0.15f;
        return controller;
    }

    private static void OneShot(AnimatorStateMachine sm, AnimatorState back, string trigger, AnimationClip clip, float maxSeconds,
        float startAt = 0f, float exitAt = 0.85f, float blendOut = 0.15f)
    {
        var state = sm.AddState(trigger);
        state.motion = clip;
        state.speed = Mathf.Max(1f, clip.length * (exitAt - startAt) / maxSeconds);
        var enter = sm.AddAnyStateTransition(state);
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        enter.duration = 0.05f;
        enter.offset = startAt;
        enter.canTransitionToSelf = true;
        var exit = state.AddTransition(back);
        exit.hasExitTime = true;
        exit.exitTime = exitAt;
        exit.duration = blendOut;
    }
}
