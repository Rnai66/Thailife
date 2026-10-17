using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Coop window (เล้าไก่): feed, sell eggs, hatch fertile eggs, see and sell chickens.
// Built at runtime; opened by CoopSpot.
public class CoopUI : MonoBehaviour
{
    private static CoopUI instance;

    private GameObject backdrop;
    private Text statusText;
    private Text eggText;
    private Text messageText;
    private Button feedButton;
    private Button sellEggsButton;
    private Button hatchButton;
    private RectTransform list;
    private bool busy;
    private float nextRender;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("CoopUI").AddComponent<CoopUI>();
        instance.backdrop.SetActive(true);
        instance.messageText.text = "";
        CoopClient.Instance?.Refresh();
        instance.Render(rebuildList: true);
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var card = UiKit.Window(transform, "CoopCanvas", new Vector2(1500f, 980f), 50, out backdrop);

        UiKit.Label(card, 52, "เล้าไก่", new Vector2(0f, 440f), new Vector2(1400f, 70f), UiKit.Brown);
        statusText = UiKit.Label(card, 30, "", new Vector2(0f, 380f), new Vector2(1400f, 46f), UiKit.Green);
        eggText = UiKit.Label(card, 30, "", new Vector2(0f, 330f), new Vector2(1400f, 46f), UiKit.Brown);

        feedButton = UiKit.Button(card, "ให้อาหาร", new Vector2(-420f, 260f), new Vector2(380f, 80f), new Color(1f, 0.8f, 0.3f), () => Act("feed"));
        sellEggsButton = UiKit.Button(card, "ขายไข่", new Vector2(0f, 260f), new Vector2(380f, 80f), new Color(1f, 0.8f, 0.3f), () => Act("sell_eggs"));
        hatchButton = UiKit.Button(card, "ฟักไข่มีเชื้อ", new Vector2(420f, 260f), new Vector2(380f, 80f), new Color(0.6f, 0.85f, 1f), () => Act("hatch"));

        list = UiKit.Container(card, new Vector2(0f, 150f));
        messageText = UiKit.Label(card, 32, "", new Vector2(0f, -380f), new Vector2(1400f, 50f), new Color(0.6f, 0.2f, 0.05f));
        UiKit.Button(card, "ปิด", new Vector2(0f, -445f), new Vector2(300f, 70f), new Color(0.9f, 0.45f, 0.4f), Hide);

        backdrop.SetActive(false);
        if (CoopClient.Instance != null) CoopClient.Instance.Changed += _ => Render(rebuildList: true);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        // Countdowns tick every second (the list is only rebuilt when the data changes,
        // so buttons don't get replaced under the player's finger)
        if (backdrop.activeSelf && Time.time >= nextRender) Render(rebuildList: false);
    }

    private void Render(bool rebuildList)
    {
        nextRender = Time.time + 1f;
        var coop = CoopClient.Instance;
        if (coop == null || !coop.IsOnline)
        {
            statusText.text = "ต้องออนไลน์ก่อนถึงจะดูเล้าได้";
            eggText.text = "";
            UiKit.Clear(list);
            feedButton.interactable = sellEggsButton.interactable = hatchButton.interactable = false;
            return;
        }

        var s = coop.Current;
        float fed = s.fed_for_s - coop.Age;
        int count = (s.chickens?.Length ?? 0) + (s.hatching_s?.Length ?? 0);
        statusText.text = $"ไก่ {count}/{s.capacity} ตัว · อาหารไก่ {s.feed_bags} ถุง · "
                          + (fed > 0 ? $"อิ่มอีก {CoopClient.Countdown(fed)} (แม่ไก่ออกไข่อยู่)" : "หิว! ให้อาหารแม่ไก่ถึงจะออกไข่");

        string hatching = "";
        if (s.hatching_s != null && s.hatching_s.Length > 0)
        {
            var times = new System.Collections.Generic.List<string>();
            foreach (int t in s.hatching_s) times.Add(CoopClient.Countdown(t - coop.Age));
            hatching = $" · กำลังฟัก {s.hatching_s.Length} ฟอง ({string.Join(", ", times)})";
        }
        eggText.text = $"ไข่ไก่ {s.eggs} ฟอง ({s.egg_price}฿) · ไข่มีเชื้อ {s.fertile_eggs} ฟอง ({s.fertile_egg_price}฿){hatching}";

        feedButton.interactable = !busy && s.feed_bags > 0;
        sellEggsButton.interactable = !busy && s.eggs + s.fertile_eggs > 0;
        hatchButton.interactable = !busy && s.fertile_eggs > 0 && count < s.capacity;

        // Chickens in two columns
        if (!rebuildList) return;
        UiKit.Clear(list);
        if (s.chickens == null || s.chickens.Length == 0)
        {
            UiKit.Label(list, 32, "ยังไม่มีไก่ — ซื้อแม่ไก่/พ่อไก่ และอาหารไก่ที่ร้านค้าชุมชน", Vector2.zero, new Vector2(1300f, 60f), UiKit.Brown);
            return;
        }
        for (int i = 0; i < s.chickens.Length; i++)
            AddChickenRow(s.chickens[i], i, coop.Age);
    }

    private void AddChickenRow(CoopClient.Chicken c, int index, float age)
    {
        float x = index % 2 == 0 ? -360f : 360f;
        float y = -(index / 2) * 112f;
        var row = UiKit.Rect("Chicken", list, new Color(1f, 1f, 1f, 0.6f));
        row.anchoredPosition = new Vector2(x, y);
        row.sizeDelta = new Vector2(700f, 100f);

        var swatch = UiKit.Rect("Feather", row, CoopClient.FeatherColor(c.color));
        swatch.anchoredPosition = new Vector2(-310f, 0f);
        swatch.sizeDelta = new Vector2(60f, 60f);

        string stage = c.adult ? CoopClient.SexTh(c) : $"ลูกไก่ (โตใน {CoopClient.Countdown(c.grown_in_s - age)})";
        UiKit.Label(row, 30, $"{c.name} · {stage}", new Vector2(-40f, 20f), new Vector2(460f, 44f), UiKit.Brown, TextAnchor.MiddleLeft);
        UiKit.Label(row, 26, $"สี{c.color} · ความสวย {c.beauty}", new Vector2(-40f, -22f), new Vector2(460f, 40f), new Color(0.35f, 0.3f, 0.25f), TextAnchor.MiddleLeft);

        var sell = UiKit.Button(row, $"ขาย {c.price}฿", new Vector2(270f, 0f), new Vector2(150f, 76f), new Color(1f, 0.7f, 0.5f), () => Act("sell_chicken", c.id), 28);
        sell.interactable = !busy;
    }

    private async void Act(string action, int chicken = 0)
    {
        var coop = CoopClient.Instance;
        if (busy || coop == null) return;
        busy = true;
        try
        {
            var r = await coop.ActAsync(action, chicken);
            switch (action)
            {
                case "feed": messageText.text = "ให้อาหารแล้ว แม่ไก่จะออกไข่เรื่อยๆ"; break;
                case "sell_eggs": messageText.text = $"ขายไข่ {r.sold} ฟอง ได้ {r.earned} บาท"; UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.2f)); break;
                case "hatch": messageText.text = "เริ่มฟักไข่แล้ว ลูกไก่จะได้ความสวยจากพ่อแม่"; break;
                case "sell_chicken": messageText.text = $"ขาย{r.sold_name} ได้ {r.earned} บาท"; UiKit.FloatAbovePlayer($"+{r.earned} ฿", new Color(1f, 0.85f, 0.2f)); break;
            }
        }
        catch (ApiResponseException e)
        {
            messageText.text = e.Message.Contains("energy") ? "พลังงานไม่พอ — ไปนอนพักที่ชานบ้านก่อน"
                : e.Message.Contains("feed") ? "ไม่มีอาหารไก่ — ซื้อที่ร้านค้าชุมชน"
                : e.Message.Contains("full") ? "เล้าเต็มแล้ว (8 ตัว)"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่" : "ทำไม่ได้ตอนนี้";
        }
        catch (Exception e)
        {
            messageText.text = "เกิดข้อผิดพลาด ลองใหม่";
            Debug.LogWarning($"[Coop] {e.Message}");
        }
        busy = false;
        Render(rebuildList: true);
    }
}
