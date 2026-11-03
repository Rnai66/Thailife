using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Cow window (ranch.lua), three places:
//   "sao"    the อบต. desk — collect your one free cow
//   "seller" the cow seller at the farm gate — buy cows, buy feed, sell a cow
//   "pen"    in the cow pen — milk the cows (sold to the dairy co-op), breed, sell, build the pen bigger
// Every cow is a tile you can tap: selling and breeding act on the one picked.
public class RanchUI : MonoBehaviour
{
    private static RanchUI instance;

    private const int Buttons = 4;

    private GameObject backdrop;
    private Text titleText, subtitleText, herdText, infoText, toastText;
    private RectTransform growthRow;
    private readonly Button[] buttons = new Button[Buttons];
    private readonly Text[] labels = new Text[Buttons];
    private readonly Func<RanchClient, Task<RanchClient.State>>[] actions = new Func<RanchClient, Task<RanchClient.State>>[Buttons];
    private Coroutine toast;
    private string mode = "seller";
    private bool busy;
    private int selected = -1;  // 0-based cow picked in the herd
    private int sellArmed = -1; // the cow whose sale waits for a second tap

    public static void Show(string counterMode)
    {
        if (instance == null)
            instance = new GameObject("RanchUI").AddComponent<RanchUI>();
        instance.mode = counterMode;
        instance.selected = -1;
        instance.sellArmed = -1;
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
        var body = UiKit.PremiumWindow(transform, "RanchCanvas", UiKit.WindowSize(new Vector2(1300f, 820f), 1180f), 55, "icon:farm",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        var card = UiKit.Panel("Herd", body, Color.white, new Vector2(0f, 160f), new Vector2(1180f, 300f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        herdText = UiKit.Label(card, 34, "", new Vector2(0f, 118f), new Vector2(1100f, 52f), UiKit.Ink);
        herdText.fontStyle = FontStyle.Bold;
        infoText = UiKit.Label(card, 22, "", new Vector2(0f, 66f), new Vector2(1120f, 56f), UiKit.Muted);
        growthRow = UiKit.Container(card, new Vector2(0f, -10f));

        for (int i = 0; i < Buttons; i++)
        {
            int index = i;
            buttons[i] = UiKit.RoundButton(body, "", new Vector2(-435f + i * 290f, -90f), new Vector2(275f, 96f), UiKit.Gold, UiKit.GoldInk,
                () => Act(index), 22);
            labels[i] = buttons[i].GetComponentInChildren<Text>();
        }
        toastText = UiKit.ToastBar(body, new Vector2(0f, -230f), 900f);
        if (UiKit.Portrait) // upright: the herd card on top, the buttons stacked under it
        {
            card.sizeDelta = new Vector2(690f, 520f);
            UiKit.Stack(card, new Vector2(0f, 240f));
            herdText.rectTransform.sizeDelta = new Vector2(650f, 52f);
            UiKit.Stack(herdText.rectTransform, new Vector2(0f, 222f));
            infoText.rectTransform.sizeDelta = new Vector2(650f, 100f);
            UiKit.Stack(infoText.rectTransform, new Vector2(0f, 148f));
            UiKit.Stack(growthRow, new Vector2(0f, 50f));
            for (int i = 0; i < Buttons; i++)
            {
                ((RectTransform)buttons[i].transform).sizeDelta = new Vector2(640f, 92f);
                UiKit.Stack((RectTransform)buttons[i].transform, new Vector2(0f, -80f - i * 104f));
            }
            UiKit.Stack((RectTransform)toastText.transform.parent, new Vector2(0f, -505f));
        }
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

    private static bool Breedable(RanchClient.Cow c, RanchClient.State s) => c.sex == "f" && c.g >= s.grown_days && !c.pregnant;

    // Lay the shown buttons out side by side (landscape), centred on however many there are
    private void Arrange()
    {
        if (UiKit.Portrait) return;
        int shown = 0;
        foreach (var b in buttons) if (b.gameObject.activeSelf) shown++;
        int k = 0;
        foreach (var b in buttons)
        {
            if (!b.gameObject.activeSelf) continue;
            ((RectTransform)b.transform).anchoredPosition = new Vector2((k - (shown - 1) * 0.5f) * 290f, -90f);
            k++;
        }
    }

    private void Render()
    {
        titleText.text = mode == "sao" ? "อบต. · โครงการวัวให้ชาวบ้าน" : mode == "pen" ? "คอกวัวของคุณ" : "พ่อค้าวัว";
        subtitleText.text = mode == "sao" ? "รับวัวฟรีคนละ 1 ตัว ไปเลี้ยงที่ฟาร์มวัวข้างโคกหนองนา"
            : mode == "pen" ? "แตะเลือกวัวเพื่อขายหรือผสมเทียม · วัวกินอาหารวันละ 1 ถุง · แม่วัวที่ได้กินให้นมวันละ 1 ขวด"
            : "ซื้อวัว · ซื้ออาหารวัว · แตะเลือกวัวที่จะขาย";
        UiKit.Clear(growthRow);

        var s = RanchClient.Instance?.Current;
        if (s == null)
        {
            herdText.text = "กำลังโหลด…";
            infoText.text = "ต้องออนไลน์ก่อน";
            for (int i = 0; i < Buttons; i++) SetButton(i, i == 0 ? "-" : null, false, null);
            Arrange();
            return;
        }

        herdText.text = $"วัว {s.cows}/{s.max_cows} ตัว (เมีย {s.females}) · อาหาร {s.feed} ถุง · นม {s.milk} ขวด";
        int minutesLeft = s.next_day_s / 60; // real minutes until the next game day
        string hunger = s.cows > 0 && s.feed < s.cows ? " · อาหารไม่พอ! วัวที่ไม่ได้กินจะไม่ให้นมและไม่โต" : "";
        bool full = s.cows >= s.max_cows;

        if (mode == "sao")
        {
            infoText.text = s.claimed ? "คุณรับวัวจาก อบต. ไปแล้ว — ซื้อเพิ่มได้จากพ่อค้าวัวที่ฟาร์มวัวข้างโคกหนองนา" : "อบต. แจกวัวให้ชาวบ้านคนละ 1 ตัว (รับได้ครั้งเดียว)";
            SetButton(0, s.claimed ? "รับไปแล้ว" : "รับวัวฟรี 1 ตัว", !s.claimed && !full, r => r.ClaimAsync());
            for (int i = 1; i < Buttons; i++) SetButton(i, null, false, null);
            Arrange();
            return;
        }

        var herd = s.herd ?? new RanchClient.Cow[0];
        if (selected >= herd.Length) selected = -1;
        if (selected < 0) // pick a sensible cow: one ready to breed in the pen, the most grown at the seller
        {
            for (int i = 0; i < herd.Length && selected < 0; i++)
                if (mode == "pen" ? Breedable(herd[i], s) : herd[i].g >= s.grown_days && !herd[i].pregnant) selected = i;
            if (selected < 0 && herd.Length > 0) selected = 0;
        }
        DrawHerd(herd, s);
        var pick = selected >= 0 ? herd[selected] : null;

        if (mode == "pen")
        {
            infoText.text = s.cows == 0 ? "ยังไม่มีวัว — รับฟรีที่ อบต. ในตำบล หรือซื้อจากพ่อค้าวัวหน้าฟาร์ม"
                : $"นมขวดละ {s.milk_price} บาท · วันใหม่ในเกมอีก {minutesLeft} นาที{hunger}";
            if (s.pregnant > 0) infoText.text += $" · แม่วัวตั้งท้อง {s.pregnant} ตัว";
            SetButton(0, s.milk > 0 ? $"รีดนมขาย {s.milk * s.milk_price:N0} บาท" : "ยังไม่มีนม", s.milk > 0, r => r.MilkAsync());
            SetBreed(1, pick, s);
            SetSell(2, pick);
            SetExpand(3, s);
        }
        else
        {
            infoText.text = $"ซื้อ {s.price:N0} บาท · สุ่มเพศ · โตเต็มที่เมื่อกินครบ {s.grown_days} วัน ขายตัวเมีย {s.grown_price:N0} / ตัวผู้ {s.grown_price_male:N0} บาท · ยังไม่โตขายได้ราคาต่ำกว่า{hunger}";
            SetButton(0, full ? $"คอกเต็ม ({s.max_cows} ตัว)" : $"ซื้อวัว · {s.price:N0} บาท", !full, r => r.BuyAsync());
            bool feedRoom = s.feed + s.feed_pack <= s.max_feed;
            SetButton(1, $"อาหารวัว {s.feed_pack} ถุง · {s.feed_pack_price} บาท", feedRoom, r => r.FeedAsync(1));
            SetSell(2, pick);
            SetExpand(3, s);
        }
        Arrange();
    }

    private void SetBreed(int i, RanchClient.Cow pick, RanchClient.State s)
    {
        int cow = selected + 1;
        if (pick == null || !Breedable(pick, s))
        {
            string why = s.breedable == 0 ? (s.females == 0 ? "ไม่มีแม่วัว" : "ยังไม่มีแม่วัวโตพร้อมผสม") : "เลือกแม่วัวที่โตแล้ว";
            SetButton(i, why, false, null);
        }
        else if (s.cows + s.pregnant >= s.max_cows)
            SetButton(i, "คอกเต็ม · ขยายคอกก่อน", false, null);
        else
            SetButton(i, $"ผสมเทียมตัวนี้ · {s.breed_price:N0} บาท", true, r => r.BreedAsync(cow));
    }

    private void SetSell(int i, RanchClient.Cow pick)
    {
        int cow = selected + 1;
        if (pick == null) { SetButton(i, "ยังไม่มีวัวให้ขาย", false, null); return; }
        if (pick.pregnant) { SetButton(i, "ตัวนี้ตั้งท้อง ขายไม่ได้", false, null); return; }
        bool armed = sellArmed == selected;
        // the first tap only arms the sale (no action: Act arms it), the second sells
        SetButton(i, armed ? $"แตะอีกครั้ง ยืนยันขาย {pick.price:N0}" : $"ขายตัวนี้ · {pick.price:N0} บาท", true,
            armed ? r => r.SellAsync(cow) : (Func<RanchClient, Task<RanchClient.State>>)null);
    }

    private void SetExpand(int i, RanchClient.State s)
    {
        if (s.expand_price <= 0) SetButton(i, $"คอกใหญ่สุดแล้ว ({s.max_cows} ตัว)", false, null);
        else SetButton(i, $"ขยายคอก +{s.pen_step} ตัว · {s.expand_price:N0} บาท", true, r => r.ExpandAsync());
    }

    // One tile per cow: sex (or in calf), a growth bar and its sale price; tap to pick it
    private void DrawHerd(RanchClient.Cow[] herd, RanchClient.State s)
    {
        int perRow = UiKit.Portrait ? 5 : 10;
        float step = UiKit.Portrait ? 130f : 112f, rowStep = UiKit.Portrait ? 80f : 84f;
        int rows = Mathf.Max(1, Mathf.CeilToInt(herd.Length / (float)perRow));
        for (int i = 0; i < herd.Length; i++)
        {
            int index = i, row = i / perRow, col = i % perRow;
            int inRow = Mathf.Min(perRow, herd.Length - row * perRow);
            var pos = new Vector2((col - (inRow - 1) * 0.5f) * step, (rows == 1 ? -30f : 0f) - row * rowStep);
            var cow = herd[i];
            bool pickedTile = i == selected;
            var tile = UiKit.Panel("Cow", growthRow, pickedTile ? UiKit.OkTint : UiKit.Cream, pos, new Vector2(step - 8f, rowStep - 6f));
            if (pickedTile) tile.gameObject.AddComponent<Outline>().effectColor = UiKit.PayGreen;
            tile.gameObject.AddComponent<Button>().onClick.AddListener(() => { selected = index; sellArmed = -1; Render(); });
            float bar = step - 26f;
            UiKit.Label(tile, 17, cow.pregnant ? $"ท้อง {cow.due_days}ว." : cow.sex == "m" ? "ตัวผู้" : "ตัวเมีย",
                new Vector2(0f, 22f), new Vector2(step - 10f, 24f), cow.pregnant ? UiKit.PayGreen : cow.sex == "m" ? UiKit.Ink : UiKit.Price).fontStyle = FontStyle.Bold;
            var track = UiKit.Panel("Track", tile, UiKit.CardBorder, new Vector2(0f, 2f), new Vector2(bar, 14f));
            var fillBar = UiKit.Panel("Fill", track, cow.g >= s.grown_days ? UiKit.Gold : UiKit.PayGreen, Vector2.zero, new Vector2(bar * cow.g / s.grown_days, 14f));
            fillBar.anchorMin = fillBar.anchorMax = new Vector2(0f, 0.5f);
            fillBar.pivot = new Vector2(0f, 0.5f);
            UiKit.Label(tile, 15, (cow.g >= s.grown_days ? "โตแล้ว" : $"{cow.g}/{s.grown_days}") + $" · {cow.price / 1000f:0.#}k",
                new Vector2(0f, -20f), new Vector2(step - 10f, 22f), UiKit.Muted);
            foreach (var g in tile.GetComponentsInChildren<Graphic>()) g.raycastTarget = g.gameObject == tile.gameObject;
        }
    }

    private async void Act(int index)
    {
        var ranch = RanchClient.Instance;
        var action = actions[index];
        if (busy || ranch == null) return;
        if (action == null) // the sell button's first tap
        {
            if (labels[index].text.StartsWith("ขายตัวนี้")) { sellArmed = selected; Render(); }
            return;
        }
        bool selling = sellArmed >= 0 && labels[index].text.Contains("ยืนยันขาย");
        sellArmed = -1;
        if (selling) selected = -1; // the sold cow leaves the herd
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
                : e.Message.Contains("pen full") ? "คอกเต็มแล้ว — ขยายคอกก่อน"
                : e.Message.Contains("pen max") ? "คอกใหญ่สุดแล้ว"
                : e.Message.Contains("in calf") ? "แม่วัวตั้งท้องอยู่ ขายไม่ได้"
                : e.Message.Contains("feed store full") ? "ที่เก็บอาหารวัวเต็มแล้ว"
                : e.Message.Contains("not grown") ? "ยังไม่มีวัวที่โตเต็มที่"
                : e.Message.Contains("no milk") ? "ยังไม่มีนมให้รีด"
                : e.Message.Contains("no cow to breed") ? "ตัวนี้ยังผสมเทียมไม่ได้" : "ทำรายการไม่สำเร็จ", UiKit.Danger);
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
