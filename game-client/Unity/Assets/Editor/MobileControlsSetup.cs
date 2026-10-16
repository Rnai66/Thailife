using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Menu: Thai Life > Add Mobile Controls
// Adds a screen-space canvas with a floating joystick (left half), a camera drag zone (right half)
// and A/B/C/D action buttons in a PlayStation-style diamond at the bottom right.
public static class MobileControlsSetup
{
    private const string CanvasName = "MobileControls";

    [MenuItem("Thai Life/Add Mobile Controls")]
    private static void AddMobileControls()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        var cameraFollow = Object.FindAnyObjectByType<CameraFollow>();
        if (player == null || cameraFollow == null)
        {
            EditorUtility.DisplayDialog("Mobile Controls", "ไม่พบ Player หรือ CameraFollow ในฉาก — เปิดฉาก ThaiVillage ก่อน", "OK");
            return;
        }

        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Mobile Controls", "มี MobileControls อยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?", "สร้างใหม่", "ยกเลิก"))
                return;
            Undo.DestroyObjectImmediate(existing);
        }

        EnsureEventSystem();

        var actions = player.GetComponent<PlayerActions>();
        if (actions == null) actions = Undo.AddComponent<PlayerActions>(player.gameObject);

        // Canvas scaled for phones in landscape
        var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Add Mobile Controls");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Right half first so the joystick zone is drawn (and hit-tested) on top at the seam
        var lookZone = CreateZone("CameraLookZone", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f));
        var look = lookZone.gameObject.AddComponent<CameraLookZone>();
        SetReference(look, "cameraFollow", cameraFollow);

        var moveZone = CreateZone("JoystickZone", canvasGo.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f));

        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var background = CreateImage("JoystickBackground", moveZone, knob, new Color(1f, 1f, 1f, 0.25f), 260f);
        background.anchorMin = background.anchorMax = Vector2.zero;
        background.anchoredPosition = new Vector2(280f, 260f);

        var handle = CreateImage("JoystickHandle", background, knob, new Color(1f, 1f, 1f, 0.8f), 120f);
        handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.anchoredPosition = Vector2.zero;

        var joystick = moveZone.gameObject.AddComponent<VirtualJoystick>();
        SetReference(joystick, "player", player);
        SetReference(joystick, "background", background);
        SetReference(joystick, "handle", handle);

        // Action buttons sit on top of the camera zone (created later = drawn and hit-tested first)
        CreateActionButtons(canvasGo.transform, actions, knob);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = canvasGo;
        Debug.Log("[ThaiLife] Mobile controls added. Save (Cmd+S), press Play, drag on the left half to walk and the right half to turn the camera.");
    }

    private static void CreateActionButtons(Transform canvas, PlayerActions actions, Sprite circle)
    {
        var group = new GameObject("ActionButtons", typeof(RectTransform));
        var rt = (RectTransform)group.transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(440f, 440f);
        rt.anchoredPosition = new Vector2(-300f, 280f);

        const float spacing = 140f;
        CreateActionButton(rt, "A", "Jump",  ActionInput.Jump,  new Vector2(0f, -spacing), new Color(0.35f, 0.55f, 1f),  actions, circle);
        CreateActionButton(rt, "B", "Kick",  ActionInput.Kick,  new Vector2(spacing, 0f),  new Color(1f, 0.35f, 0.35f),  actions, circle);
        CreateActionButton(rt, "C", "Punch", ActionInput.Punch, new Vector2(-spacing, 0f), new Color(1f, 0.5f, 0.85f),   actions, circle);
        CreateActionButton(rt, "D", "Block", ActionInput.Block, new Vector2(0f, spacing),  new Color(0.35f, 0.85f, 0.5f), actions, circle);
    }

    private static void CreateActionButton(RectTransform parent, string letter, string action, ActionInput input,
        Vector2 position, Color color, PlayerActions actions, Sprite circle)
    {
        var button = CreateImage($"Button{letter}_{action}", parent, circle, new Color(color.r, color.g, color.b, 0.75f), 150f);
        button.anchorMin = button.anchorMax = new Vector2(0.5f, 0.5f);
        button.anchoredPosition = position;
        button.GetComponent<Image>().raycastTarget = true;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateLabel(button, letter, font, 64, new Vector2(0f, 12f));
        CreateLabel(button, action, font, 24, new Vector2(0f, -38f));

        var component = button.gameObject.AddComponent<ActionButton>();
        var so = new SerializedObject(component);
        so.FindProperty("actions").objectReferenceValue = actions;
        so.FindProperty("input").enumValueIndex = (int)input;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLabel(RectTransform parent, string text, Font font, int size, Vector2 position)
    {
        var go = new GameObject("Label_" + text, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 70f);
        rt.anchoredPosition = position;

        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Undo.RegisterCreatedObjectUndo(go, "Add EventSystem");
    }

    private static RectTransform CreateZone(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = Vector2.zero;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Invisible but still receives touches
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        return rt;
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static void SetReference(Object component, string field, Object value)
    {
        var so = new SerializedObject(component);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
