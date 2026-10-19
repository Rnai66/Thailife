using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Hotel front desk window: three room tiers to book for one game day, and "เข้าห้องพัก" to go up
// to the room you booked (sleep there for free; better rooms add breakfast / full energy).
public class HotelUI : MonoBehaviour
{
    private static HotelUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, statusText, toastText;
    private RectTransform cards;
    private Button enterButton;
    private Coroutine toast;
    private HotelDesk desk;
    private bool busy;
    private float nextTick;

    public static void Show(HotelDesk from)
    {
        if (instance == null)
            instance = new GameObject("HotelUI").AddComponent<HotelUI>();
        instance.desk = from;
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
        var body = UiKit.PremiumWindow(transform, "HotelCanvas", new Vector2(1400f, 860f), 55, "icon:hotel",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        subtitleText.text = "จองห้องพัก 1 วันในเกม (24 นาที) · นอนในห้องฟรีตลอดการจอง";
        var status = UiKit.Panel("Status", body, Color.white, new Vector2(0f, 290f), new Vector2(1300f, 90f), shadow: true);
        status.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        statusText = UiKit.Label(status, 26, "", new Vector2(-170f, 0f), new Vector2(900f, 60f), UiKit.Ink, TextAnchor.MiddleLeft);
        statusText.fontStyle = FontStyle.Bold;
        enterButton = UiKit.RoundButton(status, "เข้าห้องพัก", new Vector2(480f, 0f), new Vector2(300f, 70f), UiKit.PayGreen, Color.white, EnterRoom, 28);
        cards = UiKit.Container(body, new Vector2(0f, -40f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, -340f), 900f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        var hotel = HotelClient.Instance;
        if (hotel == null) return;
        try { await hotel.RefreshAsync(); Render(); } catch (Exception) { }
    }

    private void Update()
    {
        if (!backdrop.activeSelf || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        RenderStatus();
    }

    private void RenderStatus()
    {
        var hotel = HotelClient.Instance;
        bool booked = hotel != null && hotel.Booked;
        int minutes = hotel != null ? Mathf.CeilToInt(hotel.SecondsLeft / 60f) : 0;
        statusText.text = hotel?.Current == null ? "ต้องออนไลน์ก่อน"
            : booked ? $"ห้องของคุณ: {hotel.Current.room} ({hotel.Current.hotel}) · เหลือ {minutes} นาที"
            : "ยังไม่ได้จองห้อง — เลือกห้องด้านล่าง";
        enterButton.gameObject.SetActive(booked);
    }

    private void Render()
    {
        titleText.text = desk != null ? desk.HotelName : "โรงแรม";
        RenderStatus();
        UiKit.Clear(cards);
        var hotel = HotelClient.Instance;
        var tiers = hotel?.Current?.tiers;
        if (tiers == null) return;
        bool booked = hotel.Booked;
        Color[] tints = { new Color32(0xEA, 0xF3, 0xDE, 0xFF), new Color32(0xE3, 0xF0, 0xFB, 0xFF), new Color32(0xFF, 0xF3, 0xD0, 0xFF) };
        for (int i = 0; i < tiers.Length; i++)
        {
            var t = tiers[i];
            float x = (i - 1) * 440f;
            var card = UiKit.Panel("Room", cards, tints[i % tints.Length], new Vector2(x, 0f), new Vector2(410f, 400f), shadow: true);
            card.gameObject.AddComponent<Outline>().effectColor = i == 2 ? UiKit.Gold : UiKit.CardBorder;
            UiKit.Label(card, 30, t.name, new Vector2(0f, 140f), new Vector2(380f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
            UiKit.Label(card, 40, $"{t.price:N0} บาท", new Vector2(0f, 70f), new Vector2(380f, 60f), UiKit.Price).fontStyle = FontStyle.Bold;
            UiKit.Label(card, 22, t.perks, new Vector2(0f, -15f), new Vector2(360f, 100f), UiKit.Muted);
            string tier = t.id;
            var b = UiKit.RoundButton(card, booked ? "จองแล้ว" : "จองห้องนี้", new Vector2(0f, -140f), new Vector2(300f, 76f), UiKit.Gold, UiKit.GoldInk,
                () => Book(tier), 28);
            b.interactable = !booked && !busy;
        }
    }

    private async void Book(string tier)
    {
        var hotel = HotelClient.Instance;
        if (busy || hotel == null) return;
        busy = true;
        Render();
        try
        {
            var r = await hotel.BookAsync(tier, desk != null ? desk.HotelName : "");
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play("coins");
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("not enough baht") ? "เงินไม่พอจองห้องนี้"
                : e.Message.Contains("already booked") ? "คุณจองห้องไว้แล้ว" : "จองไม่สำเร็จ", UiKit.Danger);
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

    private void EnterRoom()
    {
        var hotel = HotelClient.Instance;
        if (hotel == null || !hotel.Booked || desk == null) return;
        if (HotelRoom.Enter(hotel.Current.tier, desk.transform))
        {
            Hide();
            ContextButton.Hide(desk);
            GameAudio.Play("door_chime", null, 0.5f);
        }
        else Notify("ไม่พบห้องพัก", UiKit.Danger);
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
