using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// App Store screenshots (iPhone 6.9", landscape 2868 x 1320): opens each scene, places a camera and
// a few characters, adds a wood-sign caption in the game's lettering and renders to PNG in
// Builds/StoreScreenshots. Nothing is saved back into the scenes.
//   Menu: Thai Life → Capture App Store Screenshots   (or batch: -executeMethod StoreScreenshots.CaptureAll)
public static class StoreScreenshots
{
    private const int Width = 2868, Height = 1320;
    private const string OutDir = "Builds/StoreScreenshots";

    private struct Person { public Vector3 Pos; public float Yaw; public int Skin; public string Clip; public float Time; public string Look; }

    private class Shot
    {
        public string File, Scene, Title, Subtitle;
        public Vector3 Camera, LookAt;
        public float Fov = 50f;
        public bool Night, HideUpper;
        public Person[] People = new Person[0];
    }

    private static readonly Shot[] Shots =
    {
        new Shot { File = "01_village", Scene = "ThaiVillage", Title = "ใช้ชีวิตในหมู่บ้านไทย", Subtitle = "บ้านไม้ใต้ถุนสูง ทำนา เลี้ยงไก่ เลี้ยงวัว",
            Camera = new Vector3(-3f, 6.5f, -9f), LookAt = new Vector3(6f, 2.4f, 6f),
            People = new[] { new Person { Pos = new Vector3(1.5f, 0f, 0.5f), Yaw = 215f, Skin = 2, Clip = "Wave", Time = 0.5f },
                             new Person { Pos = new Vector3(3.2f, 0f, 1.4f), Yaw = 230f, Skin = 3, Clip = "Idle", Time = 0.3f } } },
        new Shot { File = "02_beach", Scene = "Beach", Title = "เที่ยวทะเล พักโรงแรมริมหาด", Subtitle = "เรือหางยาว ตกปลาทะเล อาหารทะเลสด",
            Camera = new Vector3(-6f, 2.6f, 5f), LookAt = new Vector3(12f, 1.2f, 30f),
            People = new[] { new Person { Pos = new Vector3(-1f, 0f, 11f), Yaw = 30f, Skin = 2, Clip = "Walk", Time = 0.2f },
                             new Person { Pos = new Vector3(0.6f, 0f, 12.2f), Yaw = 200f, Skin = 3, Clip = "Wave", Time = 0.4f } } },
        new Shot { File = "03_beach_town", Scene = "Beach", Title = "เมืองชายหาดใต้ภูเขาเขียว", Subtitle = "โรงแรม ร้านอาหาร ร้านสะดวกซื้อ ร้านนวด",
            Camera = new Vector3(-2f, 4f, 9f), LookAt = new Vector3(-14f, 6f, -22f), Fov = 55f,
            People = new[] { new Person { Pos = new Vector3(-9f, 0f, -11.5f), Yaw = 90f, Skin = 3, Clip = "Walk", Time = 0.6f } } },
        new Shot { File = "04_city", Scene = "City", Title = "เมืองใหญ่ รถไฟฟ้า ตลาด ธนาคาร", Subtitle = "เปิดร้าน จ้างพนักงาน ซื้อรถ ซื้อที่ดิน",
            Camera = new Vector3(-27f, 2.6f, -13.5f), LookAt = new Vector3(-17f, 5f, 1f), Fov = 55f,
            People = new[] { new Person { Pos = new Vector3(-22f, 0f, -4.6f), Yaw = 210f, Skin = 2, Clip = "Walk", Time = 0.1f },
                             new Person { Pos = new Vector3(-20f, 0f, -4.2f), Yaw = 230f, Skin = 3, Clip = "Wave", Time = 0.6f } } },
        new Shot { File = "05_festival_night", Scene = "City", Title = "งานวัด งานประเพณี ทุกเดือน", Subtitle = "ชิงช้าสวรรค์ ม้าหมุน มหาลัย ก.ไก่ ชกมวยงานวัด",
            Camera = new Vector3(-25f, 3.6f, -20f), LookAt = new Vector3(-42f, 5.5f, -33f), Fov = 60f, Night = true,
            People = new[] { new Person { Pos = new Vector3(-29f, 0f, -24f), Yaw = 140f, Skin = 2, Clip = "Wave", Time = 0.3f },
                             new Person { Pos = new Vector3(-30.5f, 0f, -25.5f), Yaw = 300f, Skin = 3, Clip = "Idle", Time = 0.5f } } },
        new Shot { File = "06_concert_night", Scene = "Beach", Title = "คอนเสิร์ตริมทะเล ระดับประเทศ", Subtitle = "ขึ้นเวทีเป็นนักดนตรี รับทิปจากผู้ชม",
            Camera = new Vector3(24f, 2.8f, 6.5f), LookAt = new Vector3(40f, 6f, 12.5f), Fov = 62f, Night = true },
    };

    [MenuItem("Thai Life/Capture App Store Screenshots")]
    public static void CaptureAll()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        var clips = LoadClips();
        // Edit-mode renders of the big village scene mixed up colours between materials with the SRP batcher on
        var urp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        bool batcher = urp != null && urp.useSRPBatcher;
        if (urp != null) urp.useSRPBatcher = false;
        string only = System.Environment.GetEnvironmentVariable("SHOTS_ONLY"); // e.g. "01" to redo one shot
        foreach (var shot in Shots)
            if (string.IsNullOrEmpty(only) || shot.File.StartsWith(only)) Capture(shot, clips);
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity"); // drop the temporary changes
        if (urp != null) urp.useSRPBatcher = batcher;
        Debug.Log($"[ThaiLife] App Store screenshots written to {Path.GetFullPath(OutDir)}");
    }

    // Quick look at the tour boat at the pier (Builds/StoreScreenshots/preview_boat.png); not a store shot
    [MenuItem("Thai Life/Advanced/Preview Longtail Boat")]
    public static void PreviewBoat()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        Capture(new Shot { File = "preview_boat", Scene = "Beach", Title = "เรือหางยาว", Subtitle = "ตัวเรือใหม่ · นั่งในเรือ",
            Camera = new Vector3(15f, 3.6f, 31f), LookAt = new Vector3(21.3f, 0.6f, 26f), Fov = 50f }, LoadClips());
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    [MenuItem("Thai Life/Advanced/Preview Signpost")]
    public static void PreviewSign()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        Capture(new Shot { File = "preview_sign", Scene = "Beach", Title = "ป้ายบอกทาง", Subtitle = "ดีไซน์ใหม่",
            Camera = new Vector3(25.6f, 2.0f, 14.2f), LookAt = new Vector3(27.4f, 1.6f, 18.5f), Fov = 45f }, LoadClips());
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    [MenuItem("Thai Life/Advanced/Preview School")]
    public static void PreviewSchool()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        var clips = LoadClips();
        Capture(new Shot { File = "preview_school", Scene = "Tambon", Title = "โรงเรียน", Subtitle = "เดินเข้าได้ · เวทีมวย",
            Camera = new Vector3(-2f, 5f, -12f), LookAt = new Vector3(-8f, 1.5f, -26f), Fov = 60f }, clips);
        Capture(new Shot { File = "preview_school_inside", Scene = "Tambon", Title = "ห้องเรียน · ห้องสมุด", Subtitle = "ข้างในโรงเรียน",
            Camera = new Vector3(-10f, 9f, -20f), LookAt = new Vector3(-10f, 0f, -28.5f), Fov = 70f, HideUpper = true }, clips);
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    [MenuItem("Thai Life/Advanced/Preview Soi Dao Stall")]
    public static void PreviewSoiDao()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        // Fairground at (-36, -30) turned 180°: the pens sit at world (-26.4, -21.8) and (-26.4, -30.2), gates facing -X
        var clips = LoadClips();
        Capture(new Shot { File = "preview_soidao", Scene = "Tambon", Title = "คอกสอยดาว", Subtitle = "ต้นสอยดาวกลางคอก",
            Camera = new Vector3(-33f, 5.5f, -21.8f), LookAt = new Vector3(-25.5f, 0.8f, -21.8f), Fov = 60f }, clips);
        Capture(new Shot { File = "preview_kokai", Scene = "Tambon", Title = "มหาลัย ก.ไก่", Subtitle = "คอกห้องเรียน · กระดานดำ",
            Camera = new Vector3(-31.5f, 5.2f, -30.2f), LookAt = new Vector3(-25f, 0.8f, -30.2f), Fov = 60f, HideUpper = true }, clips);
        // Village temple fair: both pens east of the temple wall (temple at -32, 21)
        Capture(new Shot { File = "preview_village_pens", Scene = "ThaiVillage", Title = "งานวัดหมู่บ้าน", Subtitle = "คอกสอยดาว · คอก ก.ไก่ หน้าวัด",
            Camera = new Vector3(-26f, 7f, -11f), LookAt = new Vector3(-32f, 1.5f, 0f), Fov = 64f, HideUpper = true }, clips);
        Capture(new Shot { File = "preview_temple_stages", Scene = "ThaiVillage", Title = "วัดยามค่ำคืน", Subtitle = "ไฟประดับ โบสถ์ เจดีย์ ดนตรีพื้นบ้าน",
            Camera = new Vector3(-31f, 9f, -9f), LookAt = new Vector3(-27f, 2.5f, 15f), Fov = 62f, Night = true }, clips);
        Capture(new Shot { File = "preview_contest_ring", Scene = "ThaiVillage", Title = "ประกวดไก่สวย", Subtitle = "ข้างเวทีมวยงานวัด",
            Camera = new Vector3(-41f, 7f, 3f), LookAt = new Vector3(-53f, 1.5f, 15f), Fov = 60f, Night = true }, clips);
        Capture(new Shot { File = "preview_ramwong", Scene = "ThaiVillage", Title = "เวทีรำวงชาวบ้าน", Subtitle = "ซื้อบัตร 20 บาท ขึ้นไปรำรอบโต๊ะดอกไม้",
            Camera = new Vector3(-43f, 4.2f, -33f), LookAt = new Vector3(-54f, 2.2f, -29.5f), Fov = 58f, Night = true }, clips);
        Capture(new Shot { File = "preview_village_fairground", Scene = "ThaiVillage", Title = "ลานอเนกประสงค์", Subtitle = "หนังกลางแปลง · ชิงช้าสวรรค์ · เวทีรำวง",
            Camera = new Vector3(-30f, 11f, -9f), LookAt = new Vector3(-47f, 2f, -33f), Fov = 62f, Night = true }, clips);
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    // The four character models side by side, plus a log of their parts (meshes and materials)
    [MenuItem("Thai Life/Advanced/Preview Character Models")]
    public static void PreviewSkins()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        var log = new System.Text.StringBuilder();
        for (int i = 0; i < 8; i++)
        {
            var prefab = Resources.Load<GameObject>($"Characters/Skin_{i}");
            if (prefab == null) break;
            log.AppendLine($"Skin_{i}:");
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = r is SkinnedMeshRenderer sm ? sm.sharedMesh : null;
                log.AppendLine($"  {r.name} sub={(mesh != null ? mesh.subMeshCount : 0)} bounds={r.bounds.size} mats=" +
                               string.Join(",", r.sharedMaterials.Select(m => m != null ? m.name : "null")));
            }
        }
        File.WriteAllText($"{OutDir}/character_parts.txt", log.ToString());
        Capture(new Shot { File = "preview_skins", Scene = "Beach", Title = "ตัวละคร", Subtitle = "Skin 0 · 1 · 2 · 3",
            Camera = new Vector3(0f, 1.3f, 5.2f), LookAt = new Vector3(0f, 1f, 10f), Fov = 45f,
            People = new[]
            {
                new Person { Skin = 0, Pos = new Vector3(-2.1f, 0f, 10f), Yaw = 180f, Clip = "Idle" },
                new Person { Skin = 1, Pos = new Vector3(-0.7f, 0f, 10f), Yaw = 180f, Clip = "Idle" },
                new Person { Skin = 2, Pos = new Vector3(0.7f, 0f, 10f), Yaw = 180f, Clip = "Idle" },
                new Person { Skin = 3, Pos = new Vector3(2.1f, 0f, 10f), Yaw = 180f, Clip = "Idle" },
            } }, LoadClips());
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    // Hairstyles and outfit colours on each body model (Builds/StoreScreenshots/preview_looks_*.png)
    [MenuItem("Thai Life/Advanced/Preview Looks")]
    public static void PreviewLooks()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(OutDir);
        var clips = LoadClips();
        string[][] sets =
        {
            new[] { "0.0.0.0.0.0", "0.0.1.0.3.2", "0.0.2.2.4.1", "0.1.0.0.0.0", "0.1.8.0.6.10", "0.1.9.4.2.2" },
            new[] { "1.0.0.0.0.0", "1.0.3.3.7.1", "1.0.4.4.11.2", "1.1.0.0.0.0", "1.1.5.2.5.4", "1.1.6.5.1.8" },
        };
        for (int s = 0; s < sets.Length; s++)
        {
            var people = new Person[sets[s].Length];
            for (int i = 0; i < people.Length; i++)
                people[i] = new Person { Look = sets[s][i], Pos = new Vector3(-2.75f + i * 1.1f, 0f, 10f), Yaw = 180f, Clip = "Idle" };
            Capture(new Shot { File = "preview_looks_" + s, Scene = "Beach", Title = s == 0 ? "ตัวละครชาย" : "ตัวละครหญิง", Subtitle = "ทรงผม · สีชุด",
                Camera = new Vector3(0f, 1.3f, 5.6f), LookAt = new Vector3(0f, 1.05f, 10f), Fov = 42f, People = people }, clips);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/ThaiVillage.unity");
    }

    private static Dictionary<string, AnimationClip> LoadClips()
    {
        var result = new Dictionary<string, AnimationClip>();
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Characters/ThaiCharacter.controller");
        if (controller == null) return result;
        // Idle / walk come from the Locomotion blend tree (by speed threshold), the rest from their states
        foreach (var child in controller.layers[0].stateMachine.states)
        {
            var state = child.state;
            if (state.motion is BlendTree tree)
            {
                var motions = tree.children.OrderBy(c => c.threshold).Select(c => c.motion as AnimationClip).Where(c => c != null).ToArray();
                if (motions.Length > 0) result["Idle"] = motions[0];
                if (motions.Length > 1) result["Walk"] = motions[1];
            }
            else if (state.motion is AnimationClip clip) result[state.name] = clip;
        }
        Debug.Log("[ThaiLife] Screenshot poses: " + string.Join(", ", result.Select(kv => $"{kv.Key}={kv.Value.name}")));
        return result;
    }

    private static void Capture(Shot shot, Dictionary<string, AnimationClip> clips)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/{shot.Scene}.unity");
        if (shot.HideUpper)
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name == "UpperFloor" || t.name == "Canopy") t.gameObject.SetActive(false); // look into walk-in buildings and tents
        if (System.Environment.GetEnvironmentVariable("SHOTS_NOPOLISH") != "1") ScenePolish.ApplyNow();
        SetTimeOfDay(shot.Night, shot.LookAt - shot.Camera);

        // The concert light show only animates in Play mode: run one frame of it so beams are coloured and aimed
        foreach (var show in Object.FindObjectsByType<ConcertLights>())
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(ConcertLights).GetMethod("Awake", flags)?.Invoke(show, null);
            typeof(ConcertLights).GetMethod("Update", flags)?.Invoke(show, null);
        }

        // Hide the scene's own player and gameplay camera so they don't show up
        foreach (var pc in Object.FindObjectsByType<PlayerController>()) pc.gameObject.SetActive(false);
        foreach (var c in Object.FindObjectsByType<Camera>()) c.enabled = false;

        var people = new List<GameObject>();
        AnimationMode.StartAnimationMode();
        try
        {
            foreach (var p in shot.People)
            {
                var look = string.IsNullOrEmpty(p.Look) ? global::Look.FromSkin(p.Skin) : global::Look.Parse("look:" + p.Look);
                var prefab = Resources.Load<GameObject>($"Characters/Skin_{look.Skin}");
                if (prefab == null) continue;
                var go = Object.Instantiate(prefab, p.Pos, Quaternion.Euler(0f, p.Yaw, 0f));
                people.Add(go);
                CharacterLook.Apply(go, look.Skin, look);
                var animator = go.GetComponentInChildren<Animator>();
                if (animator != null && clips.TryGetValue(p.Clip, out var clip))
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animator.gameObject, clip, clip.length * p.Time);
                    AnimationMode.EndSampling();
                }
            }

            var camGo = new GameObject("StoreCamera");
            camGo.transform.position = shot.Camera;
            camGo.transform.LookAt(shot.LookAt);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = shot.Fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 400f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var canvas = Caption(cam, shot);
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"{OutDir}/{shot.File}.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(camGo);
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            foreach (var go in people) Object.DestroyImmediate(go);
        }
    }

    // Daylight like the game at mid-morning, or evening with the lamps and festival lights on
    private static void SetTimeOfDay(bool night, Vector3 view)
    {
        var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            // Sunlight from behind the camera (a little to the side), so the subject is front-lit
            float yaw = Mathf.Atan2(view.x, view.z) * Mathf.Rad2Deg + 35f;
            sun.transform.rotation = Quaternion.Euler(night ? 12f : 45f, yaw, 0f);
            sun.intensity = night ? 0.12f : 1.25f;
            sun.color = night ? new Color(0.45f, 0.55f, 0.9f) : new Color(1f, 0.96f, 0.88f);
        }
        RenderSettings.sun = sun;
        // Sky light: outside Play mode the skybox ambient isn't computed, so set it like the game's daylight
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = night ? new Color(0.12f, 0.15f, 0.28f) : new Color(0.62f, 0.72f, 0.9f);
        RenderSettings.ambientEquatorColor = night ? new Color(0.1f, 0.1f, 0.16f) : new Color(0.58f, 0.62f, 0.6f);
        RenderSettings.ambientGroundColor = night ? new Color(0.05f, 0.05f, 0.07f) : new Color(0.38f, 0.36f, 0.3f);
        RenderSettings.ambientIntensity = night ? 0.5f : 1f;
        RenderSettings.fogColor = night ? new Color(0.08f, 0.1f, 0.17f) : new Color(0.7f, 0.84f, 0.97f);
        if (night && RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            RenderSettings.skybox.SetFloat("_Exposure", 0.12f);

        // Lamps only switch on at runtime; turn them on by hand for the evening shots
        foreach (var lamp in Object.FindObjectsByType<StreetLamp>())
        {
            var kind = (StreetLamp.Kind)new SerializedObject(lamp).FindProperty("kind").enumValueIndex;
            lamp.Configure(kind);
            var l = lamp.GetComponentInChildren<Light>(true);
            if (l != null) l.enabled = night || kind == StreetLamp.Kind.Ring;
        }
    }

    // Wood-sign caption top-left: big title + one line of features, in the game's Mitr lettering
    private static GameObject Caption(Camera cam, Shot shot)
    {
        var go = new GameObject("StoreCaption", typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Width, Height);
        scaler.matchWidthOrHeight = 0.5f;

        var plate = UiKit.WoodPlate(go.transform, Vector2.zero, new Vector2(1500f, 290f));
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 1f);
        plate.pivot = new Vector2(0f, 1f);
        plate.anchoredPosition = new Vector2(70f, -60f);
        var title = UiKit.Cartoon(UiKit.Label(plate, 104, shot.Title, new Vector2(0f, 40f), new Vector2(1420f, 140f), UiKit.WoodInk), 5f);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        var sub = UiKit.Cartoon(UiKit.Label(plate, 52, shot.Subtitle, new Vector2(0f, -78f), new Vector2(1420f, 80f), UiKit.GoldText), 3f);
        sub.horizontalOverflow = HorizontalWrapMode.Overflow;

        // ThaiLife9 logo top-right (game artwork)
        var logo = UiKit.Art("Logo");
        if (logo != null)
        {
            var img = UiKit.Rect("Logo", go.transform, Color.white);
            img.anchorMin = img.anchorMax = new Vector2(1f, 1f);
            img.pivot = new Vector2(1f, 1f);
            img.anchoredPosition = new Vector2(-50f, -40f);
            img.sizeDelta = new Vector2(520f, 520f * logo.rect.height / logo.rect.width);
            img.GetComponent<Image>().sprite = logo;
        }
        return go;
    }
}
