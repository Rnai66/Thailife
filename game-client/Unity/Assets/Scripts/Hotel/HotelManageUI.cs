using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// บริหารโรงแรม: the owner's window in the lobby of their hotel in the City (HotelSite). The business itself
// lives at the bank (bank.lua): staff up or down (more staff, more guests and takings, more wages),
// what it makes a day and what has gone into savings so far; decorating the lobby (CafeUI on the
// hotel's CafeSite: furniture, style, the name over the door); and the way up to the penthouse on the
// roof, where the owner sleeps for free (life.lua lets a hotel owner use the "hotel" bed).
public class HotelManageUI : MonoBehaviour
{
    private static HotelManageUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private RectTransform content;
    private Coroutine toast;
    private bool busy;
    private float y;
    private HotelSite site; // which of your hotels this window is for

    public static bool IsOpen => instance != null && instance.backdrop.activeSelf;

    public static void Open(HotelSite which = null)
    {
        if (instance == null) instance = new GameObject("HotelManageUI").AddComponent<HotelManageUI>();
        instance.site = which != null ? which : HotelSite.Instance;
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Refresh();
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "HotelManageCanvas", UiKit.WindowSize(new Vector2(1200f, 820f), 1250f), 58, "ร",
            out backdrop, out titleText, out subtitleText, Close, out _);
        content = UiKit.Rect("Content", body, new Color(0f, 0f, 0f, 0f));
        content.anchoredPosition = new Vector2(0f, -20f);
        content.sizeDelta = upright ? new Vector2(700f, 1000f) : new Vector2(1100f, 600f);
        toastText = UiKit.ToastBar(body, new Vector2(0f, upright ? 560f : 310f), 900f);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Close()
    {
        backdrop.SetActive(false);
        ContextButton.Hide(site); // HotelSite shows it again while you're still in the lobby
    }

    private async void Refresh()
    {
        var bank = BankClient.Instance;
        if (bank == null || !bank.IsOnline) return;
        try { await bank.RefreshAsync(); }
        catch (Exception) { }
        if (this != null && backdrop.activeSelf) Render();
    }

    // ── Layout: rows hanging from the top ─────────────────────────────────────────────────
    private float Width => content.sizeDelta.x;

    private T Top<T>(T c) where T : Component
    {
        var rt = (RectTransform)c.transform;
        var p = rt.anchoredPosition;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = p;
        return c;
    }

    private void Note(string text, Color color, int size = 22)
    {
        y -= 26f;
        Top(UiKit.Label(content, size, text, new Vector2(0f, y), new Vector2(Width - 40f, 48f), color));
        y -= 30f;
    }

    private void Row(string label, string detail, params (string text, Color fill, Color ink, Action act)[] buttons)
    {
        const float h = 96f;
        y -= h * 0.5f;
        var card = Top(UiKit.Panel("Row", content, Color.white, new Vector2(0f, y), new Vector2(Width - 20f, h), shadow: false));
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        bool upright = UiKit.Portrait;
        float bw = upright ? 110f : 170f;
        float labelW = Width - 60f - buttons.Length * (bw + 10f);
        float left = -(Width - 20f) * 0.5f + 20f + labelW * 0.5f;
        UiKit.Label(card, 24, label, new Vector2(left, 16f), new Vector2(labelW, 40f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 19, detail, new Vector2(left, -20f), new Vector2(labelW, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
        float x = (Width - 20f) * 0.5f - 12f - bw * 0.5f;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            var b = buttons[i];
            var btn = UiKit.RoundButton(card, b.text, new Vector2(x, 0f), new Vector2(bw, 60f), b.fill, b.ink, () => b.act?.Invoke(), upright ? 20 : 22);
            btn.interactable = !busy && b.act != null;
            x -= bw + 10f;
        }
        y -= h * 0.5f + 10f;
    }

    // ── Drawing ─────────────────────────────────────────────────────────────────────────────
    private void Render()
    {
        titleText.text = "บริหารโรงแรม";
        UiKit.Clear(content);
        y = 0f;
        var bank = BankClient.Instance;
        var s = bank != null ? bank.Current : null;
        var mine = site != null ? site.Mine : null;
        if (bank == null || !bank.IsOnline) { subtitleText.text = "ต้องออนไลน์ก่อน"; Note("ยังไม่ได้เชื่อมต่อเซิร์ฟเวอร์", UiKit.Muted); return; }
        if (mine == null) { subtitleText.text = "ยังไม่มีธุรกิจโรงแรม"; Note("เปิดกิจการโรงแรมได้ที่ธนาคาร → ธุรกิจ → โรงแรมเล็ก", UiKit.Muted); return; }
        bool first = site.Index == 0;

        subtitleText.text = $"{(first ? "" : "แห่งที่ 2 · ")}{mine.name} · {mine.building}";
        Note($"กำไรต่อวันประมาณ {mine.per_day:N0} ฿ · เข้าบัญชีเงินฝากแล้วทั้งหมด {mine.earned:N0} ฿", mine.per_day >= 0 ? UiKit.OkGreen : UiKit.Danger, 24);
        Note("กำไรเข้าบัญชีธนาคารให้เองทุกวันในเกม · พนักงานยิ่งมาก แขกยิ่งเยอะ แต่ค่าจ้างก็เพิ่มด้วย", UiKit.Muted, 19);
        y -= 10f;

        int slot = mine.slot;
        int max = s != null && s.max_staff > 0 ? s.max_staff : 5;
        Row($"พนักงาน {mine.staff}/{max} คน", "แผนกต้อนรับ แม่บ้าน และพนักงานบริการ",
            ("ลด", UiKit.Pill, UiKit.Danger, mine.staff > 0 ? (Action)(() => Do(b => b.StaffAsync(slot, -1))) : null),
            ("จ้างเพิ่ม", UiKit.Pill, UiKit.OkGreen, mine.staff < max ? (Action)(() => Do(b => b.StaffAsync(slot, 1))) : null));
        if (!first)
        {
            Note("โรงแรมแห่งที่ 2 ริมถนนวงแหวน ล็อบบี้ตกแต่งพร้อมรับแขกแล้ว", UiKit.Muted, 19);
            return;
        }
        Row("ตกแต่งล็อบบี้", "ซื้อและจัดเฟอร์นิเจอร์ เลือกสไตล์ ตั้งชื่อโรงแรมบนป้าย",
            ("ตกแต่ง", UiKit.Gold, UiKit.GoldInk, (Action)Decorate));
        Row("เพนต์เฮาส์บนดาดฟ้า", "ห้องพักส่วนตัวของเจ้าของ นอนพักฟรีได้ทุกเมื่อ",
            ("ขึ้นไปพัก", UiKit.Gold, UiKit.GoldInk, (Action)GoUp));
    }

    private void Decorate()
    {
        var lobby = CafeSite.Hotel;
        var client = CafeClient.Hotel;
        if (lobby == null || client == null || client.Current == null || !client.Current.open)
        {
            Notify("ล็อบบี้ยังโหลดไม่เสร็จ ลองใหม่อีกครั้ง", UiKit.Danger);
            if (client != null) _ = client.RefreshAsync();
            return;
        }
        backdrop.SetActive(false);
        ContextButton.Hide(site);
        CafeUI.Open(lobby);
    }

    private void GoUp()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        Close();
        if (!HotelRoom.Enter("owner", player.transform)) Notify("ยังไม่มีห้องเพนต์เฮาส์ในฉากนี้ (กด Build Whole World)", UiKit.Danger);
    }

    private async void Do(Func<BankClient, Task<BankClient.State>> action)
    {
        var bank = BankClient.Instance;
        if (busy || bank == null) return;
        busy = true;
        Render();
        try
        {
            var s = await action(bank);
            if (!string.IsNullOrEmpty(s?.done)) Notify(s.done, UiKit.PayGreen);
        }
        catch (ApiResponseException e) { Notify(e.Message.Contains("staff limit") ? "จำนวนพนักงานเต็มแล้ว" : "ทำรายการไม่สำเร็จ", UiKit.Danger); }
        catch (Exception) { Notify("เชื่อมต่อไม่ได้ ลองใหม่", UiKit.Danger); }
        finally
        {
            busy = false;
            if (this != null) Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
