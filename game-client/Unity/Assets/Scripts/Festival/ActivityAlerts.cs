using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// แจ้งเตือนกิจกรรม: when today is วันพระ (ghost pay x2) or a boxing night (every 5/10/15 days of the
// game month), a banner slides down from the top of the screen — on login and when the day turns.
// Each activity day is announced once. The list comes from festival_get (life_core.activities).
public class ActivityAlerts : MonoBehaviour
{
    private const float Stay = 6.5f;

    private static readonly HashSet<string> shown = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => shown.Clear();

    private readonly Queue<FestivalClient.Activity> queue = new Queue<FestivalClient.Activity>();
    private RectTransform banner;
    private Text title, detail;
    private Image stripe;
    private bool showing;
    private FestivalClient festival;

    private void Start()
    {
        Build();
        festival = GetComponent<FestivalClient>();
        if (festival != null) festival.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        if (festival != null) festival.Changed -= OnChanged;
    }

    private void OnChanged(FestivalClient.State s)
    {
        if (s?.activities == null) return;
        foreach (var a in s.activities)
        {
            if (!a.open || string.IsNullOrEmpty(a.key) || shown.Contains(a.key)) continue;
            shown.Add(a.key);
            queue.Enqueue(a);
        }
        if (!showing && queue.Count > 0) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        showing = true;
        while (queue.Count > 0)
        {
            // not over the title or the loading screen
            while (TitleScreen.Up || SceneLoader.Loading) yield return null;
            yield return new WaitForSeconds(1.5f);
            var a = queue.Dequeue();
            bool ghost = a.kind == "ghost";
            title.text = ghost ? "วันนี้วันพระ! ปราบผีได้ค่าครู 2 เท่า" : $"วันนี้มีมวย! {a.name}";
            detail.text = ghost
                ? $"ไปปราบผีที่ {a.where} · ค่าครู ×2 ค่าประสบการณ์ ×1.5 ทั้งวัน"
                : $"ที่ {a.where} · สมัครชกหรือไปเชียร์ได้ทั้งวัน (วันที่ {a.day} ของเดือน)";
            stripe.color = ghost ? new Color32(0x6A, 0x3F, 0xB5, 0xFF) : new Color32(0xC6, 0x28, 0x28, 0xFF);
            banner.gameObject.SetActive(true);
            yield return Slide(0f, 1f, 0.35f);
            yield return new WaitForSeconds(Stay);
            yield return Slide(1f, 0f, 0.3f);
            banner.gameObject.SetActive(false);
        }
        showing = false;
    }

    private IEnumerator Slide(float from, float to, float time)
    {
        float hidden = 200f, shownY = -24f;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(from, to, t / time);
            banner.anchoredPosition = new Vector2(0f, Mathf.Lerp(hidden, shownY, k));
            yield return null;
        }
        banner.anchoredPosition = new Vector2(0f, Mathf.Lerp(hidden, shownY, to));
    }

    private void Build()
    {
        var canvasGo = new GameObject("ActivityAlertCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        bool portrait = UiKit.Portrait;
        float w = portrait ? 700f : 1040f, h = portrait ? 150f : 124f;
        banner = UiKit.Rect("ActivityBanner", canvasGo.transform, new Color32(0xFF, 0xFB, 0xEE, 0xF5));
        banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
        banner.pivot = new Vector2(0.5f, 1f);
        banner.sizeDelta = new Vector2(w, h);
        banner.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        banner.GetComponent<Image>().raycastTarget = false;

        stripe = UiKit.Rect("Stripe", banner, Color.red).GetComponent<Image>();
        stripe.rectTransform.anchorMin = new Vector2(0f, 0f);
        stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
        stripe.rectTransform.pivot = new Vector2(0f, 0.5f);
        stripe.rectTransform.sizeDelta = new Vector2(16f, 0f);
        stripe.rectTransform.anchoredPosition = Vector2.zero;
        stripe.raycastTarget = false;

        title = UiKit.Label(banner, portrait ? 32 : 36, "", new Vector2(10f, h * 0.18f), new Vector2(w - 60f, h * 0.48f), UiKit.Ink);
        title.fontStyle = FontStyle.Bold;
        detail = UiKit.Label(banner, portrait ? 24 : 26, "", new Vector2(10f, -h * 0.24f), new Vector2(w - 60f, h * 0.4f), UiKit.Muted);
        title.raycastTarget = detail.raycastTarget = false;
        banner.gameObject.SetActive(false);
    }
}
