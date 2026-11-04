using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Sitting in meditation (สมาธิ) before the Buddha: first choose to light incense and candles (ธูปเทียน)
// or just sit, then breathe with the ring — "พุท" in, "โธ" out — until the time is up. Gains บารมี and
// monk XP (temple_meditate start/finish). Getting up early gives nothing.
public class MeditateUI : MonoBehaviour
{
    private static MeditateUI instance;

    private GameObject root, choose, sitting;
    private Text chooseInfo, breath, timer, result;
    private RectTransform ring;
    private Button incenseButton, plainButton;
    private MeditationSpot spot;
    private Coroutine running;
    private bool busy;

    public static void Open(MeditationSpot at)
    {
        if (instance == null) instance = new GameObject("MeditateUI").AddComponent<MeditateUI>();
        if (instance.running != null) return;
        instance.spot = at;
        instance.ShowChoice();
    }

    public static void Hide()
    {
        if (instance != null && instance.running == null) instance.root.SetActive(false);
    }

    private void Awake()
    {
        var canvasGo = new GameObject("MeditateCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 52;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo;

        // Choice card, low on the screen so the Buddha stays in view
        var card = UiKit.Panel("Choose", canvasGo.transform, UiKit.Cream, Vector2.zero, new Vector2(820f, 330f), shadow: true);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.anchoredPosition = new Vector2(0f, 240f);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        choose = card.gameObject;
        UiKit.Label(card, 34, "นั่งสมาธิหน้าพระประธาน", new Vector2(0f, 120f), new Vector2(780f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
        chooseInfo = UiKit.Label(card, 22, "", new Vector2(0f, 62f), new Vector2(780f, 60f), UiKit.Muted);
        incenseButton = UiKit.RoundButton(card, "", new Vector2(-195f, -30f), new Vector2(370f, 90f), UiKit.Gold, UiKit.GoldInk, () => Begin(true), 25);
        plainButton = UiKit.RoundButton(card, "นั่งเฉยๆ", new Vector2(195f, -30f), new Vector2(370f, 90f), UiKit.Pill, UiKit.Ink, () => Begin(false), 25);
        UiKit.RoundButton(card, "ไม่ใช่ตอนนี้", new Vector2(0f, -122f), new Vector2(260f, 56f), new Color(0f, 0f, 0f, 0.08f), UiKit.Muted, () => root.SetActive(false), 21);

        // While sitting: the breathing ring, the time left and a way to get up
        var sit = UiKit.Rect("Sitting", canvasGo.transform, new Color(0f, 0f, 0f, 0f));
        sit.anchorMin = sit.anchorMax = new Vector2(0.5f, 0f);
        sit.anchoredPosition = new Vector2(0f, 260f);
        sit.sizeDelta = new Vector2(700f, 400f);
        sit.GetComponent<Image>().raycastTarget = false;
        sitting = sit.gameObject;
        ring = UiKit.Rect("Ring", sit, new Color(1f, 0.85f, 0.45f, 0.35f));
        ring.GetComponent<Image>().sprite = UiKit.CircleSprite;
        ring.GetComponent<Image>().raycastTarget = false;
        ring.anchoredPosition = new Vector2(0f, 60f);
        ring.sizeDelta = new Vector2(220f, 220f);
        breath = UiKit.Cartoon(UiKit.Label(sit, 46, "", new Vector2(0f, 60f), new Vector2(400f, 70f), Color.white), 3f);
        timer = UiKit.Cartoon(UiKit.Label(sit, 28, "", new Vector2(0f, -90f), new Vector2(600f, 40f), UiKit.GoldText));
        result = UiKit.Cartoon(UiKit.Label(sit, 30, "", new Vector2(0f, 200f), new Vector2(700f, 90f), new Color(0.6f, 1f, 0.65f)), 3f);
        UiKit.RoundButton(sit, "ลุก", new Vector2(0f, -160f), new Vector2(200f, 64f), new Color(0.2f, 0.12f, 0.08f, 0.85f), UiKit.HeaderText, Quit, 24);

        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowChoice()
    {
        var t = TempleClient.Instance?.Current;
        int cost = t?.incense_cost ?? 20;
        chooseInfo.text = t == null ? "ต้องออนไลน์ก่อน"
            : $"บารมี Lv {t.barami} ({t.barami_xp}/{t.barami_to_next})" + (t.ordained ? $" · พระเลเวล {t.monk_level}" : "")
              + "\nจุดธูปเทียนบูชาพระ ได้บารมีเพิ่มขึ้นเท่าตัว";
        incenseButton.GetComponentInChildren<Text>().text = $"จุดธูปเทียน ({cost} บาท)";
        incenseButton.interactable = plainButton.interactable = t != null && !busy;
        choose.SetActive(true);
        sitting.SetActive(false);
        result.text = "";
        root.SetActive(true);
    }

    private async void Begin(bool incense)
    {
        var t = TempleClient.Instance;
        if (busy || t == null || spot == null) return;
        busy = true;
        incenseButton.interactable = plainButton.interactable = false;
        try
        {
            var r = await t.MeditateStartAsync(incense);
            if (incense)
            {
                UiKit.FloatAbovePlayer($"-{t.Current?.incense_cost ?? 20} ฿", new Color(1f, 0.6f, 0.3f));
                GameAudio.Play("coins");
            }
            spot.Sit(incense);
            choose.SetActive(false);
            sitting.SetActive(true);
            GameAudio.Play("bell");
            running = StartCoroutine(Breathe(r.token, Mathf.Max(5, r.seconds)));
        }
        catch (ApiResponseException e)
        {
            chooseInfo.text = TempleClient.Explain(e);
            incenseButton.interactable = plainButton.interactable = true;
        }
        catch (Exception)
        {
            chooseInfo.text = "เชื่อมต่อไม่ได้ ลองใหม่";
            incenseButton.interactable = plainButton.interactable = true;
        }
        busy = false;
    }

    private IEnumerator Breathe(string token, int seconds)
    {
        float end = Time.time + seconds + 0.5f; // a little over, so the server agrees the time is up
        while (Time.time < end)
        {
            float left = end - Time.time;
            float cycle = (Time.time % 8f) / 8f;               // 4 s in, 4 s out
            bool inhale = cycle < 0.5f;
            float k = inhale ? cycle * 2f : 2f - cycle * 2f;
            float size = Mathf.Lerp(170f, 300f, Mathf.SmoothStep(0f, 1f, k));
            ring.sizeDelta = new Vector2(size, size);
            breath.text = inhale ? "หายใจเข้า… พุท" : "หายใจออก… โธ";
            timer.text = $"จิตสงบ… อีก {Mathf.CeilToInt(left)} วินาที";
            yield return null;
        }
        breath.text = "สาธุ";
        timer.text = "";
        Finish(token);
    }

    private async void Finish(string token)
    {
        var t = TempleClient.Instance;
        try
        {
            var r = await t.MeditateFinishAsync(token);
            string gained = $"บารมี +{r.gained_barami}" + (r.gained_monk > 0 ? $" · พระ XP +{r.gained_monk}" : "") + (r.gained_energy > 0 ? $" · พลังงาน +{r.gained_energy}" : "");
            result.text = gained;
            UiKit.FloatAbovePlayer(gained, new Color(1f, 0.85f, 0.4f));
            GameAudio.Play("bell");
        }
        catch (ApiResponseException e) { result.text = TempleClient.Explain(e); }
        catch (Exception) { result.text = "เชื่อมต่อไม่ได้"; }
        StartCoroutine(GetUpSoon()); // stay seated a moment to read the result
    }

    private IEnumerator GetUpSoon()
    {
        yield return new WaitForSeconds(2.5f);
        running = null;
        spot?.Stand();
        root.SetActive(false);
    }

    private void Quit()
    {
        if (running != null) StopCoroutine(running);
        StopAllCoroutines();
        running = null;
        spot?.Stand();
        root.SetActive(false);
    }
}
