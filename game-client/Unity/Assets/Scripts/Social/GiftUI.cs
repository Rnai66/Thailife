using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// The ให้ของ window (social.lua gift_get / gift_send), opened from a friend's row in the เพื่อน window:
//  · โอนเงิน — a few ready amounts or your own, two taps to send (one transaction for both wallets)
//  · ของขวัญ — food, seeds, rice, field crops, fish, chicken feed and fair goods from your bag
// Friends only; the server caps how much money and how many gifts go out in a day.
public class GiftUI : MonoBehaviour
{
    private static GiftUI instance;
    private static readonly int[] Amounts = { 100, 500, 1000, 5000, 10000 };

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private RectTransform content, viewport;
    private ScrollRect scroll;
    private InputField amountField;
    private Coroutine toast;
    private SocialClient.GiftView view;
    private string friendId, friendName, armed;
    private bool busy, typing;
    private float y;

    public static void Open(string id, string name)
    {
        if (instance == null) instance = new GameObject("GiftUI").AddComponent<GiftUI>();
        instance.friendId = id;
        instance.friendName = name;
        instance.view = null;
        instance.armed = null;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Load();
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "GiftCanvas", UiKit.WindowSize(new Vector2(1300f, 900f), 1450f), 58, "ข",
            out backdrop, out titleText, out subtitleText, Close, out _);
        float w = upright ? 700f : 1200f;
        Vector2 size = upright ? new Vector2(w, 1180f) : new Vector2(w, 640f);
        viewport = UiKit.Rect("Viewport", body, new Color(0f, 0f, 0f, 0f));
        viewport.anchoredPosition = new Vector2(0f, upright ? -20f : -20f);
        viewport.sizeDelta = size;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = size;
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        toastText = UiKit.ToastBar(body, new Vector2(0f, upright ? 600f : 330f), 900f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (typing) Freeze(false);
    }

    // Typing an amount must not move the player
    private void Update()
    {
        if (!backdrop.activeSelf) return;
        bool now = amountField != null && amountField.isFocused;
        if (now != typing) Freeze(now);
    }

    private void Freeze(bool on)
    {
        typing = on;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.InputEnabled = !on;
    }

    private async void Load()
    {
        var s = SocialClient.Instance;
        if (s == null || !s.IsOnline) return;
        try { view = await s.GiftGetAsync(friendId); }
        catch (ApiResponseException e) { Notify(SocialClient.Explain(e), UiKit.Danger); }
        catch (Exception) { Notify("เชื่อมต่อไม่ได้ ลองใหม่", UiKit.Danger); }
        if (this != null && backdrop.activeSelf) Render();
    }

    // ── Layout (content hangs from its top edge, like SocialUI) ─────────────────────────────
    private float Width => content.sizeDelta.x;

    private T Top<T>(T c) where T : Component
    {
        var rt = (RectTransform)c.transform;
        var p = rt.anchoredPosition;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = p;
        return c;
    }

    private void Heading(string text)
    {
        y -= 26f;
        Top(UiKit.Label(content, 26, text, new Vector2(0f, y), new Vector2(Width - 40f, 40f), UiKit.Price)).fontStyle = FontStyle.Bold;
        y -= 30f;
    }

    private void Note(string text, Color color, int size = 21)
    {
        y -= 24f;
        Top(UiKit.Label(content, size, text, new Vector2(0f, y), new Vector2(Width - 60f, 44f), color));
        y -= 28f;
    }

    private void Row(string label, params (string text, Action act)[] buttons)
    {
        const float h = 70f;
        y -= h * 0.5f;
        var card = Top(UiKit.Panel("Row", content, Color.white, new Vector2(0f, y), new Vector2(Width - 20f, h), shadow: false));
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        bool upright = UiKit.Portrait;
        float bw = upright ? 120f : 160f;
        float labelW = Width - 60f - buttons.Length * (bw + 10f);
        UiKit.Label(card, 22, label, new Vector2(-(Width - 20f) * 0.5f + 20f + labelW * 0.5f, 0f), new Vector2(labelW, h - 10f), UiKit.Ink, TextAnchor.MiddleLeft);
        float x = (Width - 20f) * 0.5f - 12f - bw * 0.5f;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            var act = buttons[i].act;
            var btn = UiKit.RoundButton(card, buttons[i].text, new Vector2(x, 0f), new Vector2(bw, 52f), UiKit.Gold, UiKit.GoldInk, () => act?.Invoke(), upright ? 18 : 20);
            btn.interactable = !busy && act != null;
            x -= bw + 10f;
        }
        y -= h * 0.5f + 8f;
    }

    // ── Drawing ─────────────────────────────────────────────────────────────────────────────
    private void Render()
    {
        titleText.text = $"ให้ของ · {friendName}";
        UiKit.Clear(content);
        amountField = null;
        y = 0f;
        var s = SocialClient.Instance;
        if (s == null || !s.IsOnline) { subtitleText.text = "ต้องออนไลน์ก่อน"; Note("ยังไม่ได้เชื่อมต่อเซิร์ฟเวอร์", UiKit.Muted); Finish(); return; }
        if (view == null) { subtitleText.text = "ของขวัญและเงินถึงเพื่อนทันที"; Note("กำลังโหลด…", UiKit.Muted); Finish(); return; }
        subtitleText.text = $"เงินของคุณ {view.baht:N0} บาท · วันนี้ส่งได้อีก {view.sends_left} ครั้ง";

        Heading("โอนเงิน");
        Note($"ครั้งละ {view.min_baht:N0} – {view.max_baht:N0} บาท · วันนี้โอนได้อีก {view.baht_left:N0} บาท", UiKit.Muted, 19);
        bool upright = UiKit.Portrait;
        float pw = upright ? 124f : 200f;
        y -= 34f;
        for (int i = 0; i < Amounts.Length; i++)
        {
            int amount = Amounts[i];
            string key = "baht:" + amount;
            bool can = !busy && amount <= view.baht && amount <= view.baht_left && amount <= view.max_baht && view.sends_left > 0;
            var b = UiKit.RoundButton(content, armed == key ? "ยืนยัน?" : $"{amount:N0} ฿", new Vector2((i - (Amounts.Length - 1) * 0.5f) * (pw + 10f), y), new Vector2(pw, 56f),
                armed == key ? UiKit.PayGreen : UiKit.Pill, armed == key ? Color.white : UiKit.Ink, () => Money(key, amount), 21);
            Top(b).interactable = can;
        }
        y -= 34f + 16f;

        // your own amount
        float fieldW = upright ? 380f : 500f;
        y -= 38f;
        var field = Top(UiKit.Panel("Amount", content, Color.white, new Vector2(-110f, y), new Vector2(fieldW, 70f), shadow: true));
        field.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        var text = UiKit.Label(field, 26, "", Vector2.zero, new Vector2(fieldW - 40f, 60f), UiKit.Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        var placeholder = UiKit.Label(field, 22, "ใส่จำนวนเงินเอง…", Vector2.zero, new Vector2(fieldW - 40f, 60f), UiKit.Muted, TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        amountField = field.gameObject.AddComponent<InputField>();
        amountField.textComponent = text;
        amountField.placeholder = placeholder;
        amountField.contentType = InputField.ContentType.IntegerNumber;
        amountField.characterLimit = 6;
        amountField.text = typed;
        amountField.onValueChanged.AddListener(v => typed = v);
        var send = UiKit.RoundButton(content, armed == "baht:typed" ? "ยืนยันโอน?" : "โอน", new Vector2(fieldW * 0.5f - 110f + 120f, y), new Vector2(200f, 70f),
            UiKit.PayGreen, Color.white, MoneyTyped, 24);
        Top(send).interactable = !busy && view.sends_left > 0;
        y -= 38f + 12f;

        Heading("ของขวัญจากกระเป๋า");
        if (view.items == null || view.items.Length == 0)
            Note("ในกระเป๋ายังไม่มีของที่ให้กันได้ (ของกิน เมล็ดพันธุ์ ข้าว ผลผลิต ปลา อาหารไก่ ของรางวัล)", UiKit.Muted);
        else
            foreach (var it in view.items)
            {
                var item = it;
                bool can = !busy && view.sends_left > 0;
                string allKey = "all:" + item.id;
                int some = item.unit == "กก." ? 100 : 10;
                Row($"{item.name} · มี {item.count:N0} {item.unit}",
                    (item.unit == "กก." ? "ให้ 10 กก." : "ให้ 1", can ? (Action)(() => Give(item, item.unit == "กก." ? Mathf.Min(10, item.count) : 1)) : null),
                    (item.unit == "กก." ? "ให้ 100 กก." : "ให้ 10", can && item.count >= some ? (Action)(() => Give(item, some)) : null),
                    (armed == allKey ? "ยืนยัน?" : "ให้หมด", can ? (Action)(() => GiveAll(allKey, item)) : null));
            }
        Finish();
    }

    private string typed = "";

    private void Finish()
    {
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(viewport.sizeDelta.y, -y + 20f));
    }

    // ── Actions ─────────────────────────────────────────────────────────────────────────────
    private void Money(string key, int amount)
    {
        if (armed != key) { armed = key; Render(); return; }
        Do(s => s.GiftMoneyAsync(friendId, amount));
    }

    private void MoneyTyped()
    {
        if (!int.TryParse(typed, out int amount) || amount <= 0) { Notify("ใส่จำนวนเงินก่อน", UiKit.Danger); return; }
        if (armed != "baht:typed") { armed = "baht:typed"; Render(); Notify($"กด \"ยืนยันโอน?\" อีกครั้งเพื่อโอน {amount:N0} บาท ให้{friendName}", UiKit.Price); return; }
        typed = "";
        Do(s => s.GiftMoneyAsync(friendId, amount));
    }

    private void Give(SocialClient.GiftItem item, int count) => Do(s => s.GiftItemAsync(friendId, item.id, count));

    private void GiveAll(string key, SocialClient.GiftItem item)
    {
        if (armed != key) { armed = key; Render(); return; }
        Do(s => s.GiftItemAsync(friendId, item.id, Mathf.Min(item.count, 999)));
    }

    private async void Do(Func<SocialClient, Task<SocialClient.GiftView>> action)
    {
        var s = SocialClient.Instance;
        if (busy || s == null) return;
        busy = true;
        armed = null;
        Render();
        try
        {
            view = await action(s);
            if (!string.IsNullOrEmpty(view?.done)) Notify(view.done, UiKit.PayGreen);
            GameAudio.Play("coins");
        }
        catch (ApiResponseException e) { Notify(SocialClient.Explain(e), UiKit.Danger); }
        catch (Exception) { Notify("เชื่อมต่อไม่ได้ ลองใหม่", UiKit.Danger); }
        finally
        {
            busy = false;
            if (this != null) Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
