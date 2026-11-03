using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// บ้านผีสิง: walking through the dark house after paying the coupons. The screen goes black, a torch
// circle wobbles down the corridor, and the house's ghosts lunge out one after another (กระสือ, แม่นาค,
// ผีปอบ) with a wail, before the exit door lets you out into the fair lights.
public class HauntedHouseUI : MonoBehaviour
{
    private static HauntedHouseUI instance;

    private GameObject root;
    private RectTransform torch, ghost, eyesL, eyesR;
    private Image dark, ghostImage, flash;
    private Text caption, ghostName;

    private static readonly (string name, string line, Color color)[] Ghosts =
    {
        ("กระสือ", "ไฟดวงเขียวลอยมาหา...", new Color(0.55f, 1f, 0.55f)),
        ("แม่นาค", "พี่มากขา...", new Color(0.95f, 0.95f, 0.98f)),
        ("ผีปอบ", "หิว... หิวเหลือเกิน...", new Color(0.75f, 0.55f, 0.45f)),
    };

    public static void Show(System.Action done)
    {
        if (instance == null) instance = new GameObject("HauntedHouseUI").AddComponent<HauntedHouseUI>();
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.Walk(done));
    }

    private void Awake()
    {
        var canvasGo = new GameObject("HauntedCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo;

        var full = UiKit.Rect("Dark", canvasGo.transform, Color.black);
        full.anchorMin = Vector2.zero;
        full.anchorMax = Vector2.one;
        full.offsetMin = full.offsetMax = Vector2.zero;
        dark = full.GetComponent<Image>();

        // Corridor walls in perspective, lit only inside the torch circle
        for (int i = 0; i < 6; i++)
        {
            float s = 1f - i * 0.15f;
            var frame = UiKit.Rect("DoorFrame", full, new Color(0.18f, 0.1f, 0.08f, 1f - i * 0.12f));
            frame.sizeDelta = new Vector2(1400f * s, 900f * s);
            var inner = UiKit.Rect("Inside", frame, new Color(0.02f, 0.01f, 0.01f));
            inner.sizeDelta = new Vector2(1400f * s - 40f * s, 900f * s - 40f * s);
        }
        torch = UiKit.Rect("Torch", full, new Color(1f, 0.85f, 0.55f, 0.16f));
        torch.GetComponent<Image>().sprite = UiKit.CircleSprite;
        torch.sizeDelta = new Vector2(520f, 520f);

        ghost = UiKit.Rect("Ghost", full, Color.white);
        ghost.GetComponent<Image>().sprite = UiKit.CircleSprite;
        ghostImage = ghost.GetComponent<Image>();
        ghost.sizeDelta = new Vector2(300f, 380f);
        var hair = UiKit.Rect("Hair", ghost, new Color(0.05f, 0.05f, 0.05f));
        hair.GetComponent<Image>().sprite = UiKit.CircleSprite;
        hair.anchoredPosition = new Vector2(0f, 120f);
        hair.sizeDelta = new Vector2(320f, 220f);
        eyesL = Eye(ghost, new Vector2(-55f, 30f));
        eyesR = Eye(ghost, new Vector2(55f, 30f));
        var mouth = UiKit.Rect("Mouth", ghost, new Color(0.25f, 0f, 0f));
        mouth.GetComponent<Image>().sprite = UiKit.CircleSprite;
        mouth.anchoredPosition = new Vector2(0f, -70f);
        mouth.sizeDelta = new Vector2(90f, 120f);
        ghostName = UiKit.Cartoon(UiKit.Label(full, 64, "", new Vector2(0f, -360f), new Vector2(1200f, 90f), new Color(1f, 0.3f, 0.3f)), 4f);
        ghostName.font = ThaiFont.Display;

        flash = UiKit.Rect("Flash", full, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
        flash.rectTransform.anchorMin = Vector2.zero;
        flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        caption = UiKit.Cartoon(UiKit.Label(full, 40, "", new Vector2(0f, 440f), new Vector2(1500f, 70f), new Color(0.9f, 0.85f, 0.75f)), 3f);
        foreach (var g in canvasGo.GetComponentsInChildren<Graphic>()) g.raycastTarget = g == dark; // block taps on the world
        root.SetActive(false);
    }

    private static RectTransform Eye(RectTransform parent, Vector2 at)
    {
        var e = UiKit.Rect("Eye", parent, new Color(1f, 0.1f, 0.05f));
        e.GetComponent<Image>().sprite = UiKit.CircleSprite;
        e.anchoredPosition = at;
        e.sizeDelta = new Vector2(46f, 46f);
        return e;
    }

    private IEnumerator Walk(System.Action done)
    {
        root.SetActive(true);
        ghost.gameObject.SetActive(false);
        ghostName.text = "";
        caption.text = "เดินเข้าไปในความมืด...";
        GameAudio.Play("ghost", null, 0.5f);
        yield return Wander(2.2f);
        foreach (var g in Ghosts)
        {
            caption.text = g.line;
            yield return Wander(1.6f);
            yield return Lunge(g.name, g.color);
        }
        caption.text = "เห็นแสงไฟตรงประตูทางออกแล้ว!";
        for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
        {
            torch.sizeDelta = Vector2.one * Mathf.Lerp(520f, 2600f, t / 1.2f);
            yield return null;
        }
        root.SetActive(false);
        torch.sizeDelta = new Vector2(520f, 520f);
        done?.Invoke();
    }

    // The torch circle wobbling as you creep along
    private IEnumerator Wander(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float w = Time.unscaledTime;
            torch.anchoredPosition = new Vector2(Mathf.Sin(w * 1.3f) * 260f, Mathf.Sin(w * 2.1f) * 90f - 60f);
            torch.GetComponent<Image>().color = new Color(1f, 0.85f, 0.55f, Random.value < 0.06f ? 0.03f : 0.16f); // flicker
            yield return null;
        }
    }

    private IEnumerator Lunge(string name, Color color)
    {
        ghost.gameObject.SetActive(true);
        ghostImage.color = color;
        ghost.anchoredPosition = new Vector2(Random.Range(-300f, 300f), 0f);
        GameAudio.Play("ghost", null, 1f);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.5f;
            ghost.localScale = Vector3.one * Mathf.Lerp(0.15f, 2.2f, k * k);
            ghost.anchoredPosition = Vector2.Lerp(ghost.anchoredPosition, Vector2.zero, k);
            yield return null;
        }
        GameAudio.Play("punch", null, 1f);
        ghostName.text = name + "!!";
        for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
        {
            flash.color = new Color(1f, 1f, 1f, Mathf.Max(0f, 0.7f - t * 2f));
            ghost.anchoredPosition = new Vector2(Random.Range(-14f, 14f), Random.Range(-14f, 14f)); // shaking
            float blink = Mathf.PingPong(t * 8f, 1f);
            eyesL.localScale = eyesR.localScale = Vector3.one * (0.8f + blink * 0.5f);
            yield return null;
        }
        ghost.gameObject.SetActive(false);
        ghostName.text = "";
        flash.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
