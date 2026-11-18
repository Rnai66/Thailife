using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// The church / mosque window (faith.lua): what is on now and next, pray (อธิษฐาน; light a candle first
// in the church) or give (ถวายทรัพย์ / บริจาค). While praying the player kneels and a quiet line of text
// runs with the time left; getting up early gives nothing.
public class FaithUI : MonoBehaviour
{
    private static FaithUI instance;

    private GameObject root, choose, praying;
    private Text title, info, line, timer, result;
    private Button prayButton, candleButton;
    private readonly Button[] giveButtons = new Button[4];
    private PrayerSpot spot;
    private Coroutine running;
    private bool busy;

    private bool Mosque => spot != null && spot.Place == "mosque";

    public static void Open(PrayerSpot at)
    {
        if (instance == null) instance = new GameObject("FaithUI").AddComponent<FaithUI>();
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
        var canvasGo = new GameObject("FaithCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 52;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo;

        var card = UiKit.Panel("Choose", canvasGo.transform, UiKit.Cream, Vector2.zero, new Vector2(860f, 420f), shadow: true);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.anchoredPosition = new Vector2(0f, 270f);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        choose = card.gameObject;
        title = UiKit.Label(card, 34, "", new Vector2(0f, 165f), new Vector2(820f, 50f), UiKit.Ink);
        title.fontStyle = FontStyle.Bold;
        info = UiKit.Label(card, 22, "", new Vector2(0f, 105f), new Vector2(820f, 66f), UiKit.Muted);
        candleButton = UiKit.RoundButton(card, "", new Vector2(-200f, 25f), new Vector2(380f, 86f), UiKit.Gold, UiKit.GoldInk, () => Begin(true), 25);
        prayButton = UiKit.RoundButton(card, "", new Vector2(200f, 25f), new Vector2(380f, 86f), UiKit.Pill, UiKit.Ink, () => Begin(false), 25);
        UiKit.Label(card, 20, "", new Vector2(0f, -45f), new Vector2(820f, 30f), UiKit.Muted).name = "GiveTitle";
        for (int i = 0; i < giveButtons.Length; i++)
        {
            int k = i;
            giveButtons[i] = UiKit.RoundButton(card, "", new Vector2(-300f + i * 200f, -95f), new Vector2(180f, 64f), new Color(0.3f, 0.55f, 0.35f), Color.white, () => Give(k), 22);
        }
        UiKit.RoundButton(card, "ไม่ใช่ตอนนี้", new Vector2(0f, -170f), new Vector2(260f, 54f), new Color(0f, 0f, 0f, 0.08f), UiKit.Muted, () => root.SetActive(false), 21);

        var sit = UiKit.Rect("Praying", canvasGo.transform, new Color(0f, 0f, 0f, 0f));
        sit.anchorMin = sit.anchorMax = new Vector2(0.5f, 0f);
        sit.anchoredPosition = new Vector2(0f, 260f);
        sit.sizeDelta = new Vector2(760f, 400f);
        sit.GetComponent<Image>().raycastTarget = false;
        praying = sit.gameObject;
        line = UiKit.Cartoon(UiKit.Label(sit, 40, "", new Vector2(0f, 60f), new Vector2(740f, 70f), Color.white), 3f);
        timer = UiKit.Cartoon(UiKit.Label(sit, 28, "", new Vector2(0f, -40f), new Vector2(600f, 40f), UiKit.GoldText));
        result = UiKit.Cartoon(UiKit.Label(sit, 30, "", new Vector2(0f, 170f), new Vector2(740f, 90f), new Color(0.6f, 1f, 0.65f)), 3f);
        UiKit.RoundButton(sit, "ลุก", new Vector2(0f, -130f), new Vector2(200f, 64f), new Color(0.2f, 0.12f, 0.08f, 0.85f), UiKit.HeaderText, Quit, 24);

        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private int Amount(int i)
    {
        var a = FaithClient.Instance?.Current?.give_amounts;
        return a != null && i < a.Length ? a[i] : new[] { 20, 100, 500, 1000 }[i];
    }

    private void ShowChoice()
    {
        var c = FaithClient.Instance?.Current;
        var p = FaithClient.Instance?.Of(spot.Place);
        title.text = Mosque ? "มัสยิด · ละหมาดและบริจาค" : "โบสถ์คริสต์ · อธิษฐานและถวายทรัพย์";
        info.text = p == null ? "ต้องออนไลน์ก่อน"
            : (p.now != null ? $"ตอนนี้: {p.now}" + (p.now_done ? " (ร่วมแล้ว)" : " — ร่วมตอนนี้ได้ XP เพิ่ม") : $"ครั้งหน้า: {p.next}")
              + $"\nวัน{c.day_name} · วันนี้{(Mosque ? "ละหมาด" : "อธิษฐาน")}แล้ว {p.prayed} ครั้ง · ให้แล้ว {p.given:N0} บาท";
        bool online = p != null && !busy;
        candleButton.gameObject.SetActive(!Mosque);
        candleButton.GetComponentInChildren<Text>().text = $"จุดเทียนอธิษฐาน ({c?.candle_cost ?? 20} บาท)";
        prayButton.GetComponentInChildren<Text>().text = Mosque ? "ละหมาด" : "อธิษฐาน";
        ((RectTransform)prayButton.transform).anchoredPosition = new Vector2(Mosque ? 0f : 200f, 25f);
        candleButton.interactable = prayButton.interactable = online;
        var giveTitle = choose.transform.Find("GiveTitle")?.GetComponent<Text>();
        if (giveTitle != null) giveTitle.text = Mosque ? $"บริจาคบำรุงมัสยิด (ศอดะเกาะฮ์) · นับ XP วันละไม่เกิน {c?.give_cap ?? 2000:N0} บาท"
                                                       : $"ถวายทรัพย์บำรุงโบสถ์ · นับ XP วันละไม่เกิน {c?.give_cap ?? 2000:N0} บาท";
        for (int i = 0; i < giveButtons.Length; i++)
        {
            giveButtons[i].GetComponentInChildren<Text>().text = $"{Amount(i):N0} ฿";
            giveButtons[i].interactable = online;
        }
        choose.SetActive(true);
        praying.SetActive(false);
        result.text = "";
        root.SetActive(true);
    }

    private async void Give(int i)
    {
        var f = FaithClient.Instance;
        if (busy || f == null || spot == null) return;
        busy = true;
        try
        {
            var r = await f.GiveAsync(spot.Place, Amount(i));
            UiKit.FloatAbovePlayer($"-{Amount(i):N0} ฿", new Color(1f, 0.6f, 0.3f));
            GameAudio.Play("coins");
            ScreenToast.Show(r.done, UiKit.Gold);
        }
        catch (ApiResponseException e) { info.text = FaithClient.Explain(e); }
        catch (Exception) { info.text = "เชื่อมต่อไม่ได้ ลองใหม่"; }
        busy = false;
        if (running == null && root.activeSelf) ShowChoice();
    }

    private async void Begin(bool candle)
    {
        var f = FaithClient.Instance;
        if (busy || f == null || spot == null) return;
        busy = true;
        candleButton.interactable = prayButton.interactable = false;
        try
        {
            var r = await f.PrayStartAsync(spot.Place, candle);
            if (candle)
            {
                UiKit.FloatAbovePlayer($"-{f.Current?.candle_cost ?? 20} ฿", new Color(1f, 0.6f, 0.3f));
                GameAudio.Play("coins");
            }
            spot.Sit(candle);
            choose.SetActive(false);
            praying.SetActive(true);
            running = StartCoroutine(Pray(r.token, Mathf.Max(5, r.seconds)));
        }
        catch (ApiResponseException e) { info.text = FaithClient.Explain(e); candleButton.interactable = prayButton.interactable = true; }
        catch (Exception) { info.text = "เชื่อมต่อไม่ได้ ลองใหม่"; candleButton.interactable = prayButton.interactable = true; }
        busy = false;
    }

    private IEnumerator Pray(string token, int seconds)
    {
        string[] lines = Mosque
            ? new[] { "ตั้งเจตนา… หันหน้าสู่กิบลัต", "ยืนตรง… อ่านด้วยใจสงบ", "รุกูอ์… โค้งคำนับ", "สุญูด… กราบลง", "นั่งสงบ… ขอพรด้วยใจ" }
            : new[] { "สงบใจ… ก้มศีรษะลง", "ขอบคุณสำหรับวันนี้", "อธิษฐานเผื่อคนที่เรารัก", "ขอสันติสุขแก่ทุกคน", "อาเมน" };
        float start = Time.time, end = start + seconds + 0.5f;
        while (Time.time < end)
        {
            float k = (Time.time - start) / (end - start);
            line.text = lines[Mathf.Min(lines.Length - 1, Mathf.FloorToInt(k * lines.Length))];
            timer.text = $"อีก {Mathf.CeilToInt(end - Time.time)} วินาที";
            yield return null;
        }
        timer.text = "";
        Finish(token);
    }

    private async void Finish(string token)
    {
        var f = FaithClient.Instance;
        try
        {
            var r = await f.PrayFinishAsync(spot.Place, token);
            string gained = (r.gained_xp > 0 ? $"XP +{r.gained_xp} · " : "") + $"พลังงาน +{r.gained_energy}";
            line.text = r.done;
            result.text = gained;
            UiKit.FloatAbovePlayer(gained, new Color(1f, 0.85f, 0.4f));
        }
        catch (ApiResponseException e) { result.text = FaithClient.Explain(e); }
        catch (Exception) { result.text = "เชื่อมต่อไม่ได้"; }
        StartCoroutine(GetUpSoon());
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
        StopAllCoroutines();
        running = null;
        spot?.Stand();
        root.SetActive(false);
    }
}
