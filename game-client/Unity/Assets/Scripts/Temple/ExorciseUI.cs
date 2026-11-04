using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Banishing a ghost (ปราบผี): the หมอผี chants and golden yantra (ยันต์) flare up around the ghost —
// tap each before it fades. Enough sigils and the ghost is bound and driven off for the night
// (ghost_start → ghost_finish with the hits). Starting costs energy whether you win or not.
public class ExorciseUI : MonoBehaviour
{
    private static ExorciseUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, info, chant, hitsText, result;
    private RectTransform arena, hpFill, timeFill;
    private Image ghostImage;
    private Button startButton, closeButton;
    private GhostSpot spot;
    private string token;
    private int sigils, need, hits, seconds;
    private bool busy, playing;

    private static readonly string[] Chants =
        { "โอม… นะโมพุทธายะ", "ยะธาพุทโมนะ", "พุทธังอะระหัง", "นะมะพะทะ", "อิติปิโส ภะคะวา", "จะภะกะสะ", "มะอะอุ" };

    public static void Show(GhostSpot at)
    {
        if (instance == null) instance = new GameObject("ExorciseUI").AddComponent<ExorciseUI>();
        if (instance.playing) return;
        instance.spot = at;
        instance.Ready();
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "ExorciseCanvas", UiKit.WindowSize(new Vector2(1300f, 920f), 1400f), 56, "ผี",
            out backdrop, out titleText, out subtitleText, Close, out _);
        body.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.1f); // night
        float w = upright ? 700f : 1260f, h = upright ? 1240f : 760f;

        var hpBack = UiKit.Panel("Hp", body, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, h * 0.5f - 40f), new Vector2(w - 80f, 30f));
        hpFill = UiKit.Panel("Fill", hpBack, new Color(0.55f, 0.95f, 0.5f), Vector2.zero, new Vector2(w - 84f, 26f));
        hpFill.anchorMin = hpFill.anchorMax = hpFill.pivot = new Vector2(0f, 0.5f);
        hpFill.anchoredPosition = new Vector2(2f, 0f);
        var timeBack = UiKit.Panel("Time", body, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, -h * 0.5f + 30f), new Vector2(w - 80f, 14f));
        timeFill = UiKit.Panel("Fill", timeBack, UiKit.Gold, Vector2.zero, new Vector2(w - 84f, 10f));
        timeFill.anchorMin = timeFill.anchorMax = timeFill.pivot = new Vector2(0f, 0.5f);
        timeFill.anchoredPosition = new Vector2(2f, 0f);

        arena = UiKit.Rect("Arena", body, new Color(0f, 0f, 0f, 0f));
        arena.sizeDelta = new Vector2(w - 80f, h - 140f);
        arena.GetComponent<Image>().raycastTarget = false;
        var ghost = UiKit.Rect("Ghost", arena, Color.white);
        ghost.sizeDelta = new Vector2(upright ? 520f : 480f, upright ? 520f : 480f);
        ghostImage = ghost.GetComponent<Image>();
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;

        chant = UiKit.Cartoon(UiKit.Label(body, 30, "", new Vector2(0f, h * 0.5f - 90f), new Vector2(w - 60f, 46f), UiKit.GoldText), 2.5f);
        hitsText = UiKit.Cartoon(UiKit.Label(body, 26, "", new Vector2(0f, -h * 0.5f + 64f), new Vector2(w - 60f, 40f), Color.white));
        info = UiKit.Label(body, 24, "", new Vector2(0f, -h * 0.5f + 230f), new Vector2(w - 100f, 110f), new Color(0.85f, 0.9f, 0.85f));
        result = UiKit.Cartoon(UiKit.Label(body, 40, "", new Vector2(0f, 40f), new Vector2(w - 60f, 120f), Color.white), 3f);
        startButton = UiKit.RoundButton(body, "เริ่มท่องคาถา", new Vector2(0f, -h * 0.5f + 130f), new Vector2(420f, 92f), UiKit.Gold, UiKit.GoldInk, Begin, 30);
        closeButton = UiKit.RoundButton(body, "กลับ", new Vector2(0f, -h * 0.5f + 130f), new Vector2(300f, 80f), UiKit.Pill, UiKit.Ink, Close, 26);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Ready()
    {
        var info2 = TempleClient.Instance?.GhostInfo(spot.Ghost);
        string name = info2?.name ?? "ผี";
        titleText.text = $"ปราบ{name}";
        subtitleText.text = "แตะยันต์ที่ลุกเป็นไฟให้ทันก่อนดับ";
        ghostImage.sprite = GhostArt.Get(spot.Ghost);
        ghostImage.color = Color.white;
        ghostImage.rectTransform.anchoredPosition = Vector2.zero;
        info.text = $"{name} ระดับ {info2?.level ?? 1} · ปราบได้ค่าครู {info2?.baht ?? 0:N0} บาท\nเริ่มแล้วใช้พลังงานทันที แม้ปราบไม่สำเร็จ";
        chant.text = "";
        hitsText.text = "";
        result.text = "";
        hpFill.localScale = Vector3.one;
        timeFill.localScale = Vector3.one;
        startButton.gameObject.SetActive(true);
        startButton.interactable = true;
        closeButton.gameObject.SetActive(false);
        ClearSigils();
        backdrop.SetActive(true);
    }

    private async void Begin()
    {
        var t = TempleClient.Instance;
        if (busy || t == null || spot == null) return;
        busy = true;
        startButton.interactable = false;
        try
        {
            var r = await t.GhostStartAsync(spot.Spot, spot.Ghost);
            token = r.token;
            sigils = Mathf.Max(1, r.sigils);
            need = Mathf.Max(1, r.need);
            seconds = Mathf.Max(8, r.seconds);
            hits = 0;
            startButton.gameObject.SetActive(false);
            info.text = "";
            GameAudio.Play("ghost");
            StartCoroutine(Play());
        }
        catch (ApiResponseException e)
        {
            info.text = TempleClient.Explain(e);
            startButton.interactable = true;
        }
        catch (Exception)
        {
            info.text = "เชื่อมต่อไม่ได้ ลองใหม่";
            startButton.interactable = true;
        }
        busy = false;
    }

    private IEnumerator Play()
    {
        playing = true;
        float span = seconds * 0.85f;                    // all the sigils flare within this time
        float gap = span / sigils;
        float life = Mathf.Min(gap * 1.35f, 2.4f);       // how long each burns before it fades
        float start = Time.time;
        int spawned = 0;
        float next = 0.8f;
        UpdateHits();
        while (spawned < sigils || arena.childCount > 1)
        {
            float t = Time.time - start;
            if (spawned < sigils && t >= next)
            {
                Spawn(life);
                spawned++;
                next += gap;
                chant.text = Chants[spawned % Chants.Length];
            }
            timeFill.localScale = new Vector3(Mathf.Clamp01(1f - t / (span + life + 0.8f)), 1f, 1f);
            // the ghost writhes, and fades as it is bound
            float bound = Mathf.Clamp01(hits / (float)need);
            ghostImage.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 2.1f) * 30f, Mathf.Sin(t * 3.3f) * 14f);
            ghostImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.35f, bound));
            yield return null;
        }
        // the server wants the chant to have taken a fair while
        while (Time.time - start < seconds * 0.45f) yield return null;
        chant.text = "";
        Finish();
    }

    private void Spawn(float life)
    {
        var size = arena.sizeDelta;
        var pos = new Vector2(UnityEngine.Random.Range(-size.x * 0.42f, size.x * 0.42f), UnityEngine.Random.Range(-size.y * 0.38f, size.y * 0.38f));
        var b = UiKit.RoundButton(arena, "", pos, new Vector2(150f, 150f), new Color(1f, 0.85f, 0.4f), UiKit.GoldInk, () => { }, 20, circle: true);
        var img = UiKit.Rect("Yant", b.transform, Color.white);
        img.sizeDelta = new Vector2(118f, 118f);
        img.GetComponent<Image>().sprite = AmuletArt.Get("yant_8");
        img.GetComponent<Image>().preserveAspect = true;
        img.GetComponent<Image>().raycastTarget = false;
        b.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.5f, 0.1f, 0.9f);
        b.onClick.AddListener(() =>
        {
            hits++;
            UpdateHits();
            GameAudio.Play("bell");
            Destroy(b.gameObject);
        });
        StartCoroutine(Burn(b, life));
    }

    private IEnumerator Burn(Button b, float life)
    {
        float born = Time.time;
        while (b != null && Time.time - born < life)
        {
            float k = (Time.time - born) / life;
            float pulse = 1f + Mathf.Sin(Time.time * 14f) * 0.05f;
            b.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.55f, k) * pulse;
            yield return null;
        }
        if (b != null) Destroy(b.gameObject); // missed: it burns out
    }

    private void UpdateHits()
    {
        hitsText.text = $"สะกดได้ {hits}/{need} ยันต์";
        hpFill.localScale = new Vector3(Mathf.Clamp01(1f - hits / (float)need), 1f, 1f);
    }

    private void ClearSigils()
    {
        for (int i = arena.childCount - 1; i >= 0; i--)
            if (arena.GetChild(i) != ghostImage.transform) Destroy(arena.GetChild(i).gameObject);
    }

    private async void Finish()
    {
        var t = TempleClient.Instance;
        try
        {
            var r = await t.GhostFinishAsync(token, hits);
            if (r.won)
            {
                result.text = $"ปราบสำเร็จ!\n+{r.reward:N0} บาท · หมอผี XP +{r.xp}";
                result.color = new Color(0.6f, 1f, 0.6f);
                ghostImage.color = new Color(1f, 1f, 1f, 0f);
                UiKit.FloatAbovePlayer($"+{r.reward:N0} ฿", new Color(1f, 0.85f, 0.3f));
                GameAudio.Play("coins");
            }
            else
            {
                result.text = "ผีหนีไปได้… ลองใหม่คืนหน้า";
                result.color = new Color(1f, 0.6f, 0.5f);
                GameAudio.Play("ghost");
            }
        }
        catch (ApiResponseException e) { result.text = TempleClient.Explain(e); }
        catch (Exception) { result.text = "เชื่อมต่อไม่ได้"; }
        playing = false;
        closeButton.gameObject.SetActive(true);
    }

    private void Close()
    {
        if (playing) return; // no walking away mid-chant
        StopAllCoroutines();
        ClearSigils();
        backdrop.SetActive(false);
    }
}
