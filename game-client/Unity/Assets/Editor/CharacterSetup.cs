using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Thai Life > Setup Character: turns the Mixamo files in Assets/Characters into the animated
// characters the game loads (Resources/Characters/Skin_N.prefab + ThaiCharacter.controller).
// Expected files (names only need to contain the word): Character, Character2, ... (with skin,
// one per look), Idle, Walk, Run, Punch, Kick, Jump, Block; optional Wai/Pray/Bow and Wave.
// Animations downloaded "Without Skin", walk/run with "In Place".
public static class CharacterSetup
{
    private const string SourceFolder = "Assets/Characters";
    private const string OutputFolder = "Assets/Resources/Characters";
    private const float TargetHeight = 1.75f;

    private static readonly string[] Clips = { "idle", "walk", "run", "punch", "kick", "jump", "block" };
    private static readonly string[] Looping = { "idle", "walk", "run", "block" };

    // Optional greeting clips (ทักทาย): file name containing any of the words
    private static readonly (string trigger, string[] words)[] Optional =
    {
        ("Wai", new[] { "wai", "pray", "bow" }),
        ("Wave", new[] { "wav" }),
    };

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
        string NameOf(string f) => Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
        string Find(string word) => files.FirstOrDefault(f => NameOf(f).Contains(word));
        string FindAny(string[] words) => words.Select(Find).FirstOrDefault(f => f != null);
        bool IsClip(string f) => Clips.Concat(Optional.SelectMany(o => o.words)).Any(w => NameOf(f).Contains(w));

        // Looks (หน้าตา): Character.fbx, Character2.fbx, Character3.fbx ... in number order
        var characters = files.Where(f => NameOf(f).Contains("character")).OrderBy(TrailingNumber).ThenBy(f => f).ToList();
        if (characters.Count == 0) characters = files.Where(f => !IsClip(f)).Take(1).ToList();

        var missing = Clips.Where(c => Find(c) == null).ToList();
        if (characters.Count == 0) missing.Insert(0, "character");
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("Setup Character", "ยังขาดไฟล์: " + string.Join(", ", missing) + "\nใน Assets/Characters", "ตกลง");
            return;
        }
        // A "character" downloaded Without Skin is only a skeleton: invisible in the game
        var skinless = characters.Where(c => AssetDatabase.LoadAssetAtPath<GameObject>(c)?.GetComponentInChildren<SkinnedMeshRenderer>() == null).ToList();
        if (skinless.Count > 0)
        {
            characters = characters.Except(skinless).ToList();
            EditorUtility.DisplayDialog("Setup Character", "ไฟล์นี้ไม่มีตัวละคร (โหลดแบบ Without Skin) จึงข้ามไป:\n"
                + string.Join("\n", skinless.Select(Path.GetFileName))
                + "\n\nโหลดใหม่จากแท็บ Characters โดยตั้ง Skin = With Skin", "ตกลง");
            if (characters.Count == 0) return;
        }
        var optional = Optional.Select(o => (o.trigger, path: FindAny(o.words))).Where(o => o.path != null).ToList();

        // 1) Humanoid rig for everything; loop the looping clips
        foreach (var c in characters)
        {
            ImportHumanoid(c, false, true);
            ExtractTextures(c);
        }
        foreach (var c in Clips) ImportHumanoid(Find(c), Looping.Contains(c), c != "jump");
        foreach (var o in optional) ImportHumanoid(o.path, false, true);
        AssetDatabase.Refresh();

        // 2) One animator controller shared by every look (humanoid clips retarget)
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Characters");
        string controllerPath = OutputFolder + "/ThaiCharacter.controller";
        var controller = BuildController(controllerPath, c => ClipFrom(Find(c)),
            optional.Select(o => (o.trigger, ClipFrom(o.path))).ToList());

        // 3) Prefabs: Skin_0, Skin_1 ... (+ ThaiCharacter = the first look, for older builds)
        // Some Mixamo characters come in with the body flipped below the feet; retry those with
        // the axis conversion baked in, and leave out any that are still wrong
        var flipped = characters.Where(c => !UprightOrFixed(c)).ToList();
        characters = characters.Except(flipped).ToList();
        for (int i = 0; i < characters.Count; i++)
        {
            SavePrefab(characters[i], controller, $"{OutputFolder}/Skin_{i}.prefab");
            if (i == 0) SavePrefab(characters[i], controller, OutputFolder + "/ThaiCharacter.prefab");
        }
        if (flipped.Count > 0)
            EditorUtility.DisplayDialog("Setup Character", "ตัวละครนี้นำเข้าแบบกลับหัว แก้อัตโนมัติไม่ได้ จึงข้ามไป:\n"
                + string.Join("\n", flipped.Select(Path.GetFileName)) + "\n\nลองโหลดตัวละครตัวอื่นจาก Mixamo แทน", "ตกลง");
        for (int i = characters.Count; i < 8; i++) AssetDatabase.DeleteAsset($"{OutputFolder}/Skin_{i}.prefab");
        AssetDatabase.SaveAssets();

        string extras = optional.Count > 0 ? "\nท่าพิเศษ: " + string.Join(", ", optional.Select(o => o.trigger == "Wai" ? "ไหว้" : "โบกมือ")) : "";
        EditorUtility.DisplayDialog("Setup Character", $"ตัวละครพร้อมแล้ว! ({characters.Count} แบบ){extras}\nกด Play เพื่อทดสอบ", "ตกลง");
        Debug.Log($"[Character] Built {characters.Count} look(s): {string.Join(", ", characters.Select(Path.GetFileName))}");
    }

    // Mixamo embeds the skin/clothes pictures in the .fbx; pull them out so the materials use them
    // (otherwise the character renders plain grey/white)
    private static void ExtractTextures(string path)
    {
        // One "Textures" folder next to the models: that's where Unity looks when it links
        // materials to pictures (Mixamo picture names are unique per character)
        string folder = SourceFolder + "/Textures";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (importer.userData == "textures-extracted") return;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(SourceFolder, "Textures");
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.materialSearch = ModelImporterMaterialSearch.Local;
        if (!importer.ExtractTextures(folder)) Debug.LogWarning($"[Character] No embedded textures in {path}");
        AssetDatabase.Refresh();
        importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.userData = "textures-extracted";
        importer.SaveAndReimport(); // re-link the materials to the extracted pictures
    }

    private static int TrailingNumber(string path)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(path).TrimEnd('.', ' ').Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return digits.Length > 0 ? int.Parse(digits) : 1;
    }

    // Feet at the model's origin and the body above them
    private static bool IsUpright(string path)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            bool upright = bounds.min.y > -0.2f * bounds.size.y;
            if (!upright) Debug.LogWarning($"[Character] {path} looks flipped: bounds {bounds.min.y:0.00}..{bounds.max.y:0.00}");
            return upright;
        }
        finally { Object.DestroyImmediate(instance); }
    }

    private static bool UprightOrFixed(string path)
    {
        if (IsUpright(path)) return true;
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.bakeAxisConversion = !importer.bakeAxisConversion;
        importer.SaveAndReimport();
        if (IsUpright(path)) { Debug.Log($"[Character] Fixed {path} by switching Bake Axis Conversion"); return true; }
        return false;
    }

    // Wrapper root + model scaled to a person's height
    private static void SavePrefab(string characterPath, AnimatorController controller, string prefabPath)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.transform.SetParent(root.transform, false);
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (bounds.size.y > 0.01f) instance.transform.localScale = Vector3.one * (TargetHeight / bounds.size.y);
        }
        var animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
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

    private static AnimatorController BuildController(string path, System.Func<string, AnimationClip> clip,
        System.Collections.Generic.List<(string trigger, AnimationClip clip)> extras)
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

        // Greetings: play at their own pace
        foreach (var (trigger, extra) in extras)
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            OneShot(sm, loco, trigger, extra, extra.length, exitAt: 0.9f, blendOut: 0.25f);
        }

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
