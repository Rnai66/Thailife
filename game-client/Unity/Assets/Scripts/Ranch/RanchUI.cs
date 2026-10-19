using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Cow window: at the อบต. you collect your one free cow; at the farm gate you buy more from the cow seller.
public class RanchUI : MonoBehaviour
{
    private static RanchUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, herdText, infoText, toastText;
    private Button actionButton;
    private Text actionLabel;
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
        var body = UiKit.PremiumWindow(transform, "RanchCanvas", new Vector2(1100f, 700f), 55, "ว",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        var card = UiKit.Panel("Herd", body, Color.white, new Vector2(0f, 120f), new Vector2(980f, 170f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        herdText = UiKit.Label(card, 40, "", new Vector2(0f, 30f), new Vector2(900f, 60f), UiKit.Ink);
        herdText.fontStyle = FontStyle.Bold;
        infoText = UiKit.Label(card, 24, "", new Vector2(0f, -35f), new Vector2(900f, 60f), UiKit.Muted);
        actionButton = UiKit.RoundButton(body, "", new Vector2(0f, -70f), new Vector2(560f, 100f), UiKit.Gold, UiKit.GoldInk, Act, 32);
        actionLabel = actionButton.GetComponentInChildren<Text>();
        toastText = UiKit.ToastBar(body, new Vector2(0f, -200f), 760f);
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

    private void Render()
    {
        bool sao = mode == "sao";
        titleText.text = sao ? "อบต. · โครงการวัวให้ชาวบ้าน" : "พ่อค้าวัว";
        subtitleText.text = sao ? "รับวัวฟรีคนละ 1 ตัว ไปเลี้ยงที่ฟาร์มวัวในหมู่บ้าน" : "ซื้อวัวเพิ่มเข้าคอกที่ฟาร์มวัว";

        var s = RanchClient.Instance?.Current;
        if (s == null)
        {
            herdText.text = "กำลังโหลด…";
            infoText.text = "ต้องออนไลน์ก่อน";
            actionLabel.text = "-";
            actionButton.interactable = false;
            return;
        }

        herdText.text = $"วัวในคอกของคุณ {s.cows}/{s.max_cows} ตัว";
        bool full = s.cows >= s.max_cows;
        if (sao)
        {
            infoText.text = s.claimed ? "คุณรับวัวจาก อบต. ไปแล้ว — ซื้อเพิ่มได้จากพ่อค้าวัวที่ฟาร์มวัว" : "อบต. แจกวัวให้ชาวบ้านคนละ 1 ตัว (รับได้ครั้งเดียว)";
            actionLabel.text = s.claimed ? "รับไปแล้ว" : "รับวัวฟรี 1 ตัว";
            actionButton.interactable = !s.claimed && !full && !busy;
        }
        else
        {
            infoText.text = full ? "คอกเต็มแล้ว (สูงสุด 8 ตัว)"
                : !s.claimed ? $"วัวตัวละ {s.price:N0} บาท · อย่าลืมไปรับวัวฟรี 1 ตัวที่ อบต. ในตำบล" : $"วัวตัวละ {s.price:N0} บาท";
            actionLabel.text = full ? "คอกเต็ม" : $"ซื้อวัว 1 ตัว · {s.price:N0} บาท";
            actionButton.interactable = !full && !busy;
        }
    }

    private async void Act()
    {
        var ranch = RanchClient.Instance;
        if (busy || ranch == null) return;
        busy = true;
        Render();
        try
        {
            var r = await (mode == "sao" ? ranch.ClaimAsync() : ranch.BuyAsync());
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            if (mode != "sao") GameAudio.Play("coins");
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("not enough baht") ? "เงินไม่พอซื้อวัว"
                : e.Message.Contains("already") ? "รับวัวจาก อบต. ไปแล้ว"
                : e.Message.Contains("pen full") ? "คอกเต็มแล้ว (สูงสุด 8 ตัว)" : "ทำรายการไม่สำเร็จ", UiKit.Danger);
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
