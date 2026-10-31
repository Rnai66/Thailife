using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ซุ้มขายบัตร at the fair entrance: the gate pass (บัตรผ่านประตู, once per festival, comes with a few
// coupons) and coupon books for the rides and skill games. Shows what everything costs in coupons.
public class TicketBoothUI : MonoBehaviour
{
    private static TicketBoothUI instance;

    private GameObject backdrop;
    private Text title, subtitle, strip, passInfo, toast, priceList;
    private Button passButton;
    private RectTransform packRow;
    private readonly List<Button> packButtons = new List<Button>();
    private Coroutine toastRun;
    private bool busy;

    public static void Show()
    {
        if (instance == null) instance = new GameObject("TicketBoothUI").AddComponent<TicketBoothUI>();
        instance.backdrop.SetActive(true);
        instance.Load();
    }

    private void Close() => backdrop.SetActive(false);

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "TicketBoothCanvas", UiKit.WindowSize(new Vector2(1400f, 860f), 1420f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        title.text = "ซุ้มขายบัตร";
        subtitle.text = "บัตรผ่านประตู · คูปองเครื่องเล่นและเกม";

        strip = FairHud.Strip(body, new Vector2(0f, 300f), 900f);

        // Gate pass on the left
        var passCard = UiKit.Panel("PassCard", body, Color.white, new Vector2(-330f, 0f), new Vector2(620f, 500f), shadow: true);
        passCard.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(passCard, 30, "บัตรผ่านประตู", new Vector2(0f, 205f), new Vector2(560f, 46f), UiKit.Ink).fontStyle = FontStyle.Bold;
        FairHud.Ticket(passCard, new Vector2(0f, 75f), new Vector2(480f, 170f), new Color32(0xC8, 0x3A, 0x2E, 0xFF), "บัตรผ่านประตู", "เข้าเล่นเครื่องเล่นและเกมได้ตลอดงาน");
        passInfo = UiKit.Label(passCard, 24, "", new Vector2(0f, -70f), new Vector2(560f, 100f), UiKit.Ink);
        passButton = UiKit.RoundButton(passCard, "", new Vector2(0f, -185f), new Vector2(380f, 76f), UiKit.Gold, UiKit.GoldInk, BuyPass, 26);

        // Coupon books on the right
        var couponCard = UiKit.Panel("CouponCard", body, Color.white, new Vector2(330f, 0f), new Vector2(620f, 500f), shadow: true);
        couponCard.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        UiKit.Label(couponCard, 30, "คูปอง", new Vector2(0f, 205f), new Vector2(560f, 46f), UiKit.Ink).fontStyle = FontStyle.Bold;
        packRow = UiKit.Container(couponCard, new Vector2(0f, 40f));
        priceList = UiKit.Label(couponCard, 21, "", new Vector2(0f, -170f), new Vector2(580f, 130f), UiKit.Muted);

        toast = UiKit.ToastBar(body, new Vector2(0f, -305f), 900f);
        if (UiKit.Portrait) // upright: pass on top, coupons underneath
        {
            UiKit.Stack((RectTransform)strip.transform.parent, new Vector2(0f, 560f), 0.75f);
            UiKit.Stack(passCard, new Vector2(0f, 255f), 1.1f);
            UiKit.Stack(couponCard, new Vector2(0f, -320f), 1.1f);
            UiKit.Stack((RectTransform)toast.transform.parent, new Vector2(0f, -10f));
        }
        backdrop.SetActive(false);
    }

    private async void Load()
    {
        var f = FairClient.Instance;
        if (f == null) return;
        Render(f.Current);
        try { Render(await f.RefreshAsync()); }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
    }

    private void Render(FairClient.State s)
    {
        strip.text = FairHud.Format(s);
        if (s == null) return;
        if (!s.open)
        {
            passInfo.text = "ยังไม่มีงานตอนนี้\nดูวันงานถัดไปได้ที่ปฏิทินงาน";
            passButton.GetComponentInChildren<Text>().text = "ปิดขาย";
            passButton.interactable = false;
        }
        else
        {
            passInfo.text = $"{s.fair}\nได้คูปองฟรี {s.pass_coupons} ใบ · ใช้ได้ตลอดงานนี้";
            passButton.GetComponentInChildren<Text>().text = s.has_pass ? "มีบัตรแล้ว ✓" : $"ซื้อบัตร · {s.pass_price} ฿";
            passButton.interactable = !s.has_pass && !busy;
        }

        if (packButtons.Count == 0 && s.packs != null)
            for (int i = 0; i < s.packs.Length; i++)
            {
                var p = s.packs[i];
                float x = (i - (s.packs.Length - 1) * 0.5f) * 190f;
                FairHud.Ticket(packRow, new Vector2(x, 45f), new Vector2(170f, 110f), FairHud.CouponColor, $"{p.coupons} ใบ", "");
                string id = p.id;
                packButtons.Add(UiKit.RoundButton(packRow, $"{p.price} ฿", new Vector2(x, -55f), new Vector2(160f, 64f), UiKit.Gold, UiKit.GoldInk, () => BuyPack(id), 26));
            }
        foreach (var b in packButtons) b.interactable = s.open && s.has_pass && !busy;

        var lines = new List<string>();
        if (s.rides != null) foreach (var r in s.rides) if (r.id != "ferris_village") lines.Add($"{r.name} {r.coupons} ใบ");
        lines.Add("เกมฝีมือ รอบละ 1 ใบ");
        priceList.text = (s.has_pass ? "" : "ต้องมีบัตรผ่านประตูก่อนจึงซื้อคูปองได้\n") + string.Join(" · ", lines) + "\nคะแนนจากเกมเอาไปแลกของที่ซุ้มแลกรางวัล";
    }

    private async void BuyPass()
    {
        var f = FairClient.Instance;
        if (busy || f == null) return;
        busy = true;
        try
        {
            var s = await f.BuyPassAsync();
            Say($"ได้บัตรผ่านประตูแล้ว! แถมคูปอง {s.got} ใบ", UiKit.PayGreen);
            GameAudio.Play("coins", null, 0.6f);
        }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
        busy = false;
        Render(f.Current);
    }

    private async void BuyPack(string pack)
    {
        var f = FairClient.Instance;
        if (busy || f == null) return;
        busy = true;
        try
        {
            var s = await f.BuyCouponsAsync(pack);
            Say($"ได้คูปอง {s.got} ใบ", UiKit.PayGreen);
            GameAudio.Play("coins", null, 0.5f);
        }
        catch (Exception e) { Say(FairClient.Explain(e), UiKit.Danger); }
        busy = false;
        Render(f.Current);
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
