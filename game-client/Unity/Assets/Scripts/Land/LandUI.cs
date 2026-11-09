using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// A parcel of your land (land.lua), opened at its board: who owns it, what to build there, the rent
// waiting and — on a crop field — which crop the plots plant next and the buyer for the harvest.
public class LandUI : MonoBehaviour
{
    private static LandUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, statusText, infoText, toastText;
    private RectTransform buttons;
    private Coroutine toast;
    private string parcel = "plain";
    private string armed;   // the destructive action waiting for a second tap
    private bool busy;

    public static void Show(string parcelId)
    {
        if (instance == null) instance = new GameObject("LandUI").AddComponent<LandUI>();
        instance.parcel = parcelId;
        instance.armed = null;
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
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "LandCanvas", UiKit.WindowSize(new Vector2(1300f, 860f), 1250f), 55, "icon:koknongna",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        var card = UiKit.Panel("Status", body, Color.white, new Vector2(0f, upright ? 420f : 220f), new Vector2(upright ? 690f : 1180f, upright ? 220f : 170f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        statusText = UiKit.Label(card, 34, "", new Vector2(0f, upright ? 60f : 38f), new Vector2(upright ? 650f : 1120f, 52f), UiKit.Ink);
        statusText.fontStyle = FontStyle.Bold;
        infoText = UiKit.Label(card, 22, "", new Vector2(0f, upright ? -40f : -30f), new Vector2(upright ? 650f : 1120f, upright ? 110f : 70f), UiKit.Muted);
        buttons = UiKit.Container(body, new Vector2(0f, upright ? 230f : 70f));
        toastText = UiKit.ToastBar(body, new Vector2(0f, upright ? -520f : -320f), 900f);
        backdrop.SetActive(false);
        if (LandClient.Instance != null) LandClient.Instance.Changed += _ => { if (backdrop != null && backdrop.activeSelf) Render(); };
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private async void Refresh()
    {
        var land = LandClient.Instance;
        if (land == null) return;
        try { await land.RefreshAsync(); } catch (Exception) { }
    }

    private static string ParcelName(string id) =>
        id == "rice" ? "ที่นาติดคลอง 5 ไร่" : id == "city" ? "ที่ดินติดถนนในเมือง" : "ที่ดินเปล่า 1 ไร่";

    // Buttons flow left to right, three to a row (two when upright)
    private int slot;

    private void Add(string text, Color color, Color ink, bool enabled, Action onClick, int fontSize = 24)
    {
        int perRow = UiKit.Portrait ? 2 : 3;
        float w = UiKit.Portrait ? 330f : 370f, h = 92f;
        int row = slot / perRow, col = slot % perRow;
        slot++;
        var pos = new Vector2((col - (perRow - 1) * 0.5f) * (w + 14f), -row * (h + 14f));
        var b = UiKit.RoundButton(buttons, text, pos, new Vector2(w, h), color, ink, () => onClick?.Invoke(), fontSize);
        b.interactable = enabled && !busy && onClick != null;
    }

    private void Heading(string text)
    {
        int perRow = UiKit.Portrait ? 2 : 3;
        if (slot % perRow != 0) slot += perRow - slot % perRow; // start a new row
        int row = slot / perRow;
        UiKit.Label(buttons, 24, text, new Vector2(0f, -row * 106f + 4f), new Vector2(UiKit.Portrait ? 690f : 1180f, 36f), UiKit.Price).fontStyle = FontStyle.Bold;
        slot += perRow; // the heading takes a row of its own, a little shorter than a button row
    }

    private void Render()
    {
        titleText.text = ParcelName(parcel);
        subtitleText.text = parcel == "rice" ? "นา 7-12 แถวหลังในแปลงเกษตรของคุณ"
            : parcel == "city" ? "ข้างธนาคาร ริมถนนใหญ่" : "ริมถนนใหญ่ ใกล้ร้านค้าในหมู่บ้าน";
        UiKit.Clear(buttons);
        slot = 0;

        var s = LandClient.Instance?.Current;
        if (s == null)
        {
            statusText.text = "กำลังโหลด…";
            infoText.text = "ต้องออนไลน์ก่อน";
            return;
        }

        bool mine = parcel == "plain" ? s.own_plain : parcel == "rice" ? s.own_rice : s.own_city;
        if (!mine)
        {
            statusText.text = "ที่ดินแปลงนี้ยังไม่ใช่ของคุณ";
            infoText.text = "ซื้อได้ที่ธนาคารในเมือง (เมนูทรัพย์สิน · ต้องมีเลเวล 50) · ซื้อแล้วกลับมาที่นี่เพื่อใช้ประโยชน์";
            return;
        }

        if (parcel == "rice")
        {
            statusText.text = "ที่นาของคุณ · แปลงนา 7-12";
            infoText.text = "เดินเข้าไปในแปลงนาเพื่อไถ ดำนา รดน้ำ และเกี่ยวข้าว อยู่แถวหลังของแปลงนาในแปลงเกษตรของคุณ · ข้าวที่เกี่ยวเก็บเข้ายุ้งข้าว";
            return;
        }

        string build = parcel == "plain" ? s.plain_build : s.city_build;
        int rent = parcel == "plain" ? s.plain_rent : s.city_rent;
        var built = Find(s, build);

        if (built == null)
        {
            statusText.text = "ที่ดินว่าง · เลือกสิ่งที่จะสร้าง";
            infoText.text = parcel == "plain"
                ? "ไร่: ปลูกข้าวโพด มันสำปะหลัง อ้อย 4 แปลง · คอกวัว: เลี้ยงวัวได้เพิ่ม " + s.pen_cows + " ตัว · บ้านเช่า: ได้ค่าเช่าทุกวัน"
                : "ตึกแถวให้เช่า: ได้ค่าเช่าทุกวันในเกม (เก็บที่นี่ สะสมได้ " + s.max_rent_days + " วัน)";
            foreach (var b in s.builds ?? new LandClient.Build[0])
            {
                if (parcel == "plain" ? b.id == "shophouse" : b.id != "shophouse") continue;
                string id = b.id;
                Add($"สร้าง{b.name}\n{b.cost:N0} บาท" + (b.rent > 0 ? $" · เช่า {b.rent:N0}/วัน" : ""), UiKit.Gold, UiKit.GoldInk, true,
                    () => Do(l => l.BuildAsync(parcel, id)), 22);
            }
            return;
        }

        statusText.text = $"{built.name} ของคุณ";
        if (built.rent > 0)
        {
            infoText.text = $"ค่าเช่า {built.rent:N0} บาทต่อวันในเกม · สะสมได้สูงสุด {s.max_rent_days} วัน แล้วต้องมาเก็บ";
            Add(rent > 0 ? $"เก็บค่าเช่า {rent:N0} บาท" : "ยังไม่มีค่าเช่า", UiKit.PayGreen, Color.white, rent > 0, () => Do(l => l.CollectAsync(parcel)), 26);
        }
        else if (build == "pen")
            infoText.text = $"คอกวัวแปลงที่สอง: เลี้ยงวัวได้เพิ่ม {s.pen_cows} ตัว · ซื้อวัว รีดนม ผสมเทียมได้ที่ฟาร์มวัวข้างๆ";
        else if (build == "field")
        {
            infoText.text = $"เลือกพืชที่จะปลูกครั้งต่อไป แล้วเดินเข้าแปลงไร่ · ผลผลิตเก็บในโรงเก็บ (สูงสุด {s.store:N0} กก.) ขายได้ที่นี่";
            var land = LandClient.Instance;
            Heading("ปลูกครั้งต่อไป");
            foreach (var c in s.crops ?? new LandClient.Crop[0])
            {
                string id = c.id;
                bool picked = land.NextCrop == id;
                Add($"{(picked ? "เลือก · " : "")}{c.name}\nพันธุ์ {c.seed_price:N0} ฿ · {GameClock.Countdown(c.grow_s)}", picked ? UiKit.OkTint : UiKit.Pill, UiKit.Ink, true,
                    () => { land.NextCrop = id; Render(); }, 20);
            }
            Heading("ขายผลผลิต (ลานรับซื้อ)");
            foreach (var c in s.crops ?? new LandClient.Crop[0])
            {
                string id = c.id;
                Add(c.stored > 0 ? $"ขาย{c.name} {c.stored:N0} กก.\nได้ {c.stored * c.price_per_kg:N0} บาท" : $"ไม่มี{c.name}\n({c.price_per_kg} ฿/กก.)",
                    UiKit.PayGreen, Color.white, c.stored > 0, () => Do(l => l.SellAsync(id)), 20);
            }
        }

        // Knocking the building down takes two taps
        bool confirm = armed == "clear";
        Add(confirm ? "แตะอีกครั้ง ยืนยันรื้อถอน" : $"รื้อ{built.name}", UiKit.DangerTint, UiKit.Danger, true,
            () => { if (armed == "clear") Do(l => l.ClearAsync(parcel)); else { armed = "clear"; Render(); } }, 22);
    }

    private static LandClient.Build Find(LandClient.State s, string id)
    {
        if (string.IsNullOrEmpty(id) || s.builds == null) return null;
        foreach (var b in s.builds) if (b.id == id) return b;
        return null;
    }

    private async void Do(Func<LandClient, Task<LandClient.State>> action)
    {
        var land = LandClient.Instance;
        if (busy || land == null) return;
        busy = true;
        armed = null;
        Render();
        try
        {
            var r = await action(land);
            if (!string.IsNullOrEmpty(r.done)) Notify(r.done, UiKit.PayGreen);
            GameAudio.Play("coins");
        }
        catch (ApiResponseException e)
        {
            Notify(e.Message.Contains("not enough baht") ? "เงินไม่พอ"
                : e.Message.Contains("not enough energy") ? "พลังงานไม่พอ — พักก่อน"
                : e.Message.Contains("not your land") ? "ที่ดินนี้ยังไม่ใช่ของคุณ"
                : e.Message.Contains("pen in use") ? "ยังมีวัวอยู่ในคอกนี้ — ขายวัวหรือขยายคอกในหมู่บ้านก่อน"
                : e.Message.Contains("no rent") ? "ยังไม่มีค่าเช่า"
                : e.Message.Contains("nothing to sell") ? "ไม่มีผลผลิตให้ขาย"
                : e.Message.Contains("already built") ? "สร้างไปแล้ว" : "ทำรายการไม่สำเร็จ", UiKit.Danger);
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
