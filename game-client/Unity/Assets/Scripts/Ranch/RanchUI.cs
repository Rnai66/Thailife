using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Cow window (ranch.lua), three places:
//   "sao"    the อบต. desk — collect your one free cow
//   "seller" the cow seller at the farm gate — buy cows, buy feed, sell a fully grown cow
//   "pen"    in the cow pen — milk the cows (sold to the dairy co-op), see feed and growth
public class RanchUI : MonoBehaviour
{
    private static RanchUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, herdText, infoText, toastText;
    private RectTransform growthRow;
    private readonly Button[] buttons = new Button[3];
    private readonly Text[] labels = new Text[3];
    private readonly Func<RanchClient, Task<RanchClient.State>>[] actions = new Func<RanchClient, Task<RanchClient.State>>[3];
    private Coroutine toast;
    private string mode = "seller";
    private bool busy;

    public static void Show(string counterMode)
    {
        if (instance == null)
            instance = new GameObject("RanchUI").AddComponent<RanchUI>();
        instance.mode = counterMode;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Refresh();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "RanchCanvas", new Vector2(1300f, 780f), 55, "icon:farm",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        var card = UiKit.Panel("Herd", body, Color.white, new Vector2(0f, 150f), new Vector2(1180f, 230f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        herdText = UiKit.Label(card, 36, "", new Vector2(0f, 70f), new Vector2(1100f, 56f), UiKit.Ink);
        herdText.fontStyle = FontStyle.Bold;
        infoText = UiKit.Label(card, 24, "", new Vector2(0f, 5f), new Vector2(1100f, 70f), UiKit.Muted);
        growthRow = UiKit.Container(card, new Vector2(0f, -70f));

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i] = UiKit.RoundButton(body, "", new Vector2(-390f + i * 390f, -90f), new Vector2(370f, 96f), UiKit.Gold, UiKit.GoldInk,
                () => Act(index), 26);
            labels[i] = buttons[i].GetComponentInChildren<Text>();
        }
        toastText = UiKit.ToastBar(body, new Vector2(0f, -230f), 900f);
        backdrop.SetActive(false);
        var ranch = RanchClient.Instance;
        if (ranch != null) ranch.Changed += _ => { if (backdrop != null && backdrop.activeSelf) Render(); };
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        var ranch = RanchClient.Instance;
        if (ranch == null) return;
        try { await ranch.RefreshAsync(); } catch (Exception) { }
    }

    private void SetButton(int i, string text, bool enabled, Func<RanchClient, Task<RanchClient.State>> action)
    {
        buttons[i].gameObject.SetActive(text != null);
        if (text == null) return;
        labels[i].text = text;
        buttons[i].interactable = enabled && !busy;
        actions[i] = action;
    }

    private void Render()
    {
        titleText.text = mode == "sao" ? "อบต. · โครงการวัวให้ชาวบ้าน" : mode == "pen" ? "คอกวัวของคุณ" : "พ่อค้าวัว";
        subtitleText.text = mode == "sao" ? "รับวัวฟรีคนละ 1 ตัว ไปเลี้ยงที่ฟาร์มวัวในหมู่บ้าน"
            : mode == "pen" ? "วัวกินอาหารวันละ 1 ถุง · วัวที่ได้กินให้นมวันละ 1 ขวดและโตขึ้น"
            : "ซื้อวัว · ซื้ออาหารวัว · ขายวัวที่โตเต็มที่";
        UiKit.Clear(growthRow);

        var s = RanchClient.Instance?.Current;
        if (s == null)
        {
            herdText.text = "กำลังโหลด…";
            infoText.text = "ต้องออนไลน์ก่อน";
            for (int i = 0; i < buttons.Length; i++) SetButton(i, i == 0 ? "-" : null, false, null);
            return;
        }

        herdText.text = $"วัว {s.cows}/{s.max_cows} ตัว · อาหารวัว {s.feed} ถุง · นมรอรีด {s.milk} ขวด";
        int hoursLeft = s.next_day_s / 60; // real minutes until the next game day
        string hunger = s.cows > 0 && s.feed < s.cows ? " · อาหารไม่พอ! วัวที่ไม่ได้กินจะไม่ให้นมและไม่โต" : "";
        bool full = s.cows >= s.max_cows;

        if (mode == "sao")
        {
            infoText.text = s.claimed ? "คุณรับวัวจาก อบต. ไปแล้ว — ซื้อเพิ่มได้จากพ่อค้าวัวที่ฟาร์มวัวในหมู่บ้าน" : "อบต. แจกวัวให้ชาวบ้านคนละ 1 ตัว (รับได้ครั้งเดียว)";
            SetButton(0, s.claimed ? "รับไปแล้ว" : "รับวัวฟรี 1 ตัว", !s.claimed && !full, r => r.ClaimAsync());
            SetButton(1, null, false, null);
            SetButton(2, null, false, null);
            return;
        }

        // Growth bars, one per cow (fully grown = gold)
        if (s.growth != null)
            for (int i = 0; i < s.growth.Length; i++)
            {
                float x = (i - (s.growth.Length - 1) * 0.5f) * 130f;
                var track = UiKit.Panel("Track", growthRow, UiKit.CardBorder, new Vector2(x, 0f), new Vector2(110f, 18f));
                var fillBar = UiKit.Panel("Fill", track, s.growth[i] >= s.grown_days ? UiKit.Gold : UiKit.PayGreen, Vector2.zero, new Vector2(110f * s.growth[i] / s.grown_days, 18f));
                fillBar.anchorMin = fillBar.anchorMax = new Vector2(0f, 0.5f);
                fillBar.pivot = new Vector2(0f, 0.5f);
                UiKit.Label(growthRow, 18, s.growth[i] >= s.grown_days ? "โตแล้ว" : $"{s.growth[i]}/{s.grown_days}", new Vector2(x, -24f), new Vector2(120f, 26f), UiKit.Muted);
            }

        if (mode == "pen")
        {
            infoText.text = s.cows == 0 ? "ยังไม่มีวัว — รับฟรีที่ อบต. ในตำบล หรือซื้อจากพ่อค้าวัวหน้าฟาร์ม"
                : $"นมขวดละ {s.milk_price} บาท · วันใหม่ในเกมอีก {hoursLeft} นาที{hunger}";
            SetButton(0, s.milk > 0 ? $"รีดนมขาย {s.milk * s.milk_price:N0} บาท" : "ยังไม่มีนม", s.milk > 0, r => r.MilkAsync());
            SetButton(1, null, false, null);
            SetButton(2, null, false, null);
            return;
        }

        infoText.text = $"วัวโตเต็มที่เมื่อได้กินครบ {s.grown_days} วัน ขายได้ตัวละ {s.grown_price:N0} บาท · อาหารวัวเก็บได้ {s.max_feed} ถุง{hunger}";
        SetButton(0, full ? "คอกเต็ม (8 ตัว)" : $"ซื้อวัว · {s.price:N0} บาท", !full, r => r.BuyAsync());
        bool feedRoom = s.feed + s.feed_pack <= s.max_feed;
        SetButton(1, $"อาหารวัว {s.feed_pack} ถุง · {s.feed_pack_price} บาท", feedRoom, r => r.FeedAsync(1));
        SetButton(2, s.grown > 0 ? $"ขายวัวโต · {s.grown_price:N0} บาท" : "ยังไม่มีวัวโต", s.grown > 0, r => r.SellAsync());
    }

    private async void Act(int index)
    {
        var ranch = RanchClient.Instance;
        var action = actions[index];
        if (busy || ranch == null || action == null) return;
        busy = true;
        Render();
        if (mode == "pen" && index == 0) ActivityPose.Do("milk", 2f); // milking into the bucket
        try
        {
            var r = await action(ranch);
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            if (mode != "sao") GameAudio.Play("coins");
            if (mode == "pen") GameAudio.Play("cow", null, 0.6f);
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("not enough baht") ? "เงินไม่พอ"
                : e.Message.Contains("not enough energy") ? "พลังงานไม่พอ — พักก่อน"
                : e.Message.Contains("already") ? "รับวัวจาก อบต. ไปแล้ว"
                : e.Message.Contains("pen full") ? "คอกเต็มแล้ว (สูงสุด 8 ตัว)"
                : e.Message.Contains("feed store full") ? "ที่เก็บอาหารวัวเต็มแล้ว"
                : e.Message.Contains("not grown") ? "ยังไม่มีวัวที่โตเต็มที่"
                : e.Message.Contains("no milk") ? "ยังไม่มีนมให้รีด" : "ทำรายการไม่สำเร็จ", UiKit.Danger);
        }
        catch (Exception)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
        }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
