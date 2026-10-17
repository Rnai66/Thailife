using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Fishing flow while standing at a FishingSpot: one button (or F) to cast, then to reel when the bobber dips.
//   Idle → Casting → Waiting → Bite → Reeling → Idle
// Built at runtime; appears only inside a fishing spot.
public class FishingUI : MonoBehaviour
{
    private enum State { Idle, Casting, Waiting, Bite, Reeling }

    private const float BiteWindow = 2f;   // how long the bobber stays down (server allows a bit more)

    private static FishingUI instance;

    private FishingSpot spot;
    private State state;
    private float biteTime;
    private GameObject root;
    private Button button;
    private Text buttonLabel;
    private Image buttonImage;
    private Text status;
    private GameObject bobber;

    public static void Enter(FishingSpot fishingSpot)
    {
        if (instance == null)
            instance = new GameObject("FishingUI").AddComponent<FishingUI>();
        instance.spot = fishingSpot;
        string rod = WalletClient.Instance != null ? WalletClient.Instance.RodName : null;
        instance.SetState(State.Idle, $"ยืนริมคลองแล้ว กด ตกปลา (F){(string.IsNullOrEmpty(rod) ? "" : $" · ใช้{rod}")}");
        instance.root.SetActive(true);
    }

    public static void Exit(FishingSpot fishingSpot)
    {
        if (instance == null || instance.spot != fishingSpot) return;
        instance.spot = null;
        instance.RemoveBobber();
        instance.state = State.Idle;
        instance.root.SetActive(false);
    }

    private void Awake() => Build();

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        RemoveBobber();
    }

    private void Update()
    {
        if (spot == null) return;

        if (PressedKey()) OnButton();

        if (state == State.Waiting && Time.time >= biteTime)
        {
            SetState(State.Bite, "ปลากินเบ็ด! ดึงเลย!");
        }
        else if (state == State.Bite && Time.time >= biteTime + BiteWindow)
        {
            RemoveBobber();
            SetState(State.Idle, "ช้าไป ปลาหลุด! ลองใหม่");
        }

        AnimateBobber();
    }

    private async void OnButton()
    {
        var client = FishingClient.Instance;
        if (client == null || !client.IsOnline)
        {
            SetState(State.Idle, "ต้องออนไลน์ก่อนถึงจะตกปลาได้");
            return;
        }

        try
        {
            switch (state)
            {
                case State.Idle:
                    SetState(State.Casting, "เหวี่ยงเบ็ด...");
                    float biteIn = await client.CastAsync();
                    if (spot == null) return; // walked away meanwhile
                    biteTime = Time.time + biteIn;
                    SpawnBobber();
                    SetState(State.Waiting, "รอปลากินเบ็ด... (ดึงเร็วไปปลาจะตกใจ)");
                    break;

                case State.Waiting:
                case State.Bite:
                    SetState(State.Reeling, "ดึง!");
                    var result = await client.ReelAsync();
                    RemoveBobber();
                    ShowResult(result);
                    break;
            }
        }
        catch (Exception e)
        {
            RemoveBobber();
            SetState(State.Idle, e.Message.Contains("energy") ? "พลังงานไม่พอ — ไปนอนพักที่ชานบ้านก่อน"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่" : "ตกปลาไม่สำเร็จ ลองใหม่");
            Debug.LogWarning($"[Fishing] {e.Message}");
        }
    }

    private void ShowResult(FishingClient.ReelResult result)
    {
        var player = FindAnyObjectByType<PlayerController>();
        Vector3 at = (player != null ? player.transform.position : spot.BobberPosition) + Vector3.up * 2f;

        if (result.caught)
        {
            FloatingText.Spawn(at, $"ได้{result.fish_name} {result.kg:0.00} กก.!", new Color(0.4f, 0.9f, 1f));
            SetState(State.Idle, $"ได้{result.fish_name} {result.kg:0.00} กก. — ตกต่อได้เลย");
        }
        else
        {
            string why = result.reason == "early" ? "ดึงเร็วไป ปลาตกใจหนี" : "ช้าไป ปลาหลุด";
            SetState(State.Idle, $"{why} — ลองใหม่");
        }
    }

    private void SetState(State next, string message)
    {
        state = next;
        status.text = message;

        bool busy = next == State.Casting || next == State.Reeling;
        button.interactable = !busy;
        buttonLabel.text = next == State.Idle ? "ตกปลา" : next == State.Bite ? "ดึง!" : next == State.Waiting ? "ดึง" : "...";
        buttonImage.color = next == State.Bite ? new Color(1f, 0.35f, 0.25f, 0.95f) : new Color(0.2f, 0.55f, 0.85f, 0.85f);
    }

    private static bool PressedKey()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F);
#else
        return false;
#endif
    }

    // ── Bobber (ทุ่น) ──────────────────────────────────────────────────────────
    private void SpawnBobber()
    {
        RemoveBobber();
        bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bobber.name = "Bobber";
        Destroy(bobber.GetComponent<Collider>());
        bobber.transform.localScale = Vector3.one * 0.25f;
        bobber.transform.position = spot.BobberPosition;

        var renderer = bobber.GetComponent<Renderer>();
        var playerRenderer = FindAnyObjectByType<PlayerController>()?.GetComponent<Renderer>();
        var material = playerRenderer != null ? new Material(playerRenderer.sharedMaterial) : renderer.material;
        material.color = new Color(0.9f, 0.15f, 0.1f);
        renderer.sharedMaterial = material;
    }

    private void AnimateBobber()
    {
        if (bobber == null || spot == null) return;
        Vector3 basePos = spot.BobberPosition;
        float y = state == State.Bite
            ? -0.12f + Mathf.Sin(Time.time * 25f) * 0.05f   // dipping and shaking
            : Mathf.Sin(Time.time * 2f) * 0.03f;            // gentle float
        bobber.transform.position = basePos + new Vector3(0f, y, 0f);
    }

    private void RemoveBobber()
    {
        if (bobber != null) Destroy(bobber);
        bobber = null;
    }

    // ── UI ─────────────────────────────────────────────────────────────────────
    private void Build()
    {
        var canvasGo = new GameObject("FishingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Big round button above the A/B/C/D diamond
        var buttonGo = new GameObject("FishButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)buttonGo.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-300f, 600f);
        rt.sizeDelta = new Vector2(220f, 220f);
        buttonImage = buttonGo.GetComponent<Image>();
        button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(OnButton);
        buttonLabel = CreateText(rt, font, 48, Vector2.zero, new Vector2(220f, 220f));

        // Status line at the top centre
        var statusBg = new GameObject("Status", typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)statusBg.transform;
        srt.SetParent(canvasGo.transform, false);
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -80f);
        srt.sizeDelta = new Vector2(1000f, 80f);
        var bg = statusBg.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;
        status = CreateText(srt, font, 38, Vector2.zero, new Vector2(980f, 80f));

        root.SetActive(false);
    }

    private static Text CreateText(Transform parent, Font font, int size, Vector2 position, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = size;
        return text;
    }
}
