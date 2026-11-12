using UnityEngine;
using UnityEngine.UI;

// The petrol station's window (ปั๊มน้ำมัน): two tabs.
//   เติมน้ำมัน   each of your vehicles with its gauge, the fuel it takes and the price per litre:
//               ฿100, ฿500 or fill it up (fuel.lua takes the money)
//   ขับไปที่อื่น  in your own vehicle: every place you can drive to from here, how far, the fuel it
//               takes and how long; go, and DriveJourney takes you there with the car.
// Built at runtime; one per scene.
public class FuelUI : MonoBehaviour
{
    private static FuelUI instance;

    private GameObject backdrop;
    private Text title, subtitle, toast;
    private Coroutine toastRoutine;
    private RectTransform content;
    private Button fillTab, driveTab;
    private bool driveMode, busy;
    private bool portrait;
    private Vector2 bodySize;

    public static void Show(string station)
    {
        if (instance == null) instance = new GameObject("FuelUI").AddComponent<FuelUI>();
        instance.title.text = station;
        instance.backdrop.SetActive(true);
        instance.Open(false);
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var size = UiKit.WindowSize(new Vector2(1300f, 900f), 1500f);
        bodySize = size - new Vector2(40f, 156f);
        var body = UiKit.PremiumWindow(transform, "FuelCanvas", size, 52, "icon:travel", out backdrop, out title, out subtitle, Hide, out _);
        subtitle.text = "เติมน้ำมัน · ขับรถไปเมืองอื่น";
        float top = bodySize.y * 0.5f - 50f;
        fillTab = UiKit.RoundButton(body, "เติมน้ำมัน", new Vector2(-170f, top), new Vector2(320f, 70f), UiKit.Gold, UiKit.GoldInk, () => Open(false), 30);
        driveTab = UiKit.RoundButton(body, "ขับไปที่อื่น", new Vector2(170f, top), new Vector2(320f, 70f), UiKit.Pill, UiKit.Ink, () => Open(true), 30);
        content = UiKit.Container(body, new Vector2(0f, -40f));
        toast = UiKit.ToastBar(body, new Vector2(0f, -bodySize.y * 0.5f + 50f), 900f);
        backdrop.SetActive(false);
    }

    private void Open(bool drive)
    {
        driveMode = drive;
        fillTab.GetComponent<Image>().color = drive ? UiKit.Pill : UiKit.Gold;
        driveTab.GetComponent<Image>().color = drive ? UiKit.Gold : UiKit.Pill;
        UiKit.Clear(content);
        Message("กำลังโหลด…");
        if (drive) LoadTrips(); else LoadCars();
    }

    private void Message(string text) =>
        LoadingFlower.Show(UiKit.Label(content, 30, "", new Vector2(0f, 40f), new Vector2(bodySize.x - 80f, 200f), UiKit.Muted), text);

    private void Say(string text, bool bad) =>
        toastRoutine = UiKit.Toast(this, toast, text, bad ? UiKit.Danger : UiKit.PayGreen, toastRoutine);

    // ── เติมน้ำมัน ───────────────────────────────────────────────────────────
    private async void LoadCars()
    {
        FuelClient.State state;
        try { state = await FuelClient.GetAsync(); }
        catch (System.Exception e)
        {
            if (this == null || driveMode) return;
            UiKit.Clear(content);
            Message(FuelClient.Explain(e));
            return;
        }
        if (this == null || driveMode) return;
        UiKit.Clear(content);
        if (state.cars.Length == 0)
        {
            Message("ยังไม่มีรถของตัวเอง\nซื้อมอเตอร์ไซค์หรือรถยนต์ได้ที่ธนาคารในตัวเมือง");
            return;
        }
        float rowH = portrait ? 190f : 100f, step = rowH + 12f;
        float headY = (bodySize.y - 140f) * 0.5f - 30f;
        float y = headY - 40f - rowH * 0.5f;
        foreach (var car in state.cars)
        {
            CarRow(car, y, rowH);
            y -= step;
        }
        UiKit.Label(content, 22, $"ราคาหน้าปั๊มวันนี้ · {FuelClient.FuelName("g95")} ฿{FuelClient.Price("g95"):0.00} · {FuelClient.FuelName("diesel")} ฿{FuelClient.Price("diesel"):0.00} ต่อลิตร",
            new Vector2(0f, headY), new Vector2(bodySize.x - 60f, 40f), UiKit.Price);
    }

    private void CarRow(FuelClient.Car car, float y, float h)
    {
        float w = bodySize.x - 60f;
        var row = UiKit.Panel("Car", content, Color.white, new Vector2(0f, y), new Vector2(w, h), shadow: true);
        row.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        bool needs = car.kmpl > 0f && car.tank > 0f;
        float infoY = portrait ? 45f : 0f;
        float left = -w * 0.5f + 20f;
        UiKit.Label(row, 32, car.name, new Vector2(left + 150f, infoY + 20f), new Vector2(300f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        if (!needs)
        {
            UiKit.Label(row, 24, "ไม่ต้องเติมน้ำมัน · ปั่นด้วยแรงขา", new Vector2(left + 150f, infoY - 22f), new Vector2(300f, 36f), UiKit.Muted, TextAnchor.MiddleLeft);
            return;
        }
        UiKit.Label(row, 22, $"{FuelClient.FuelName(car.fuel)} · ฿{FuelClient.Price(car.fuel):0.00}/ลิตร · {car.kmpl:0} กม./ลิตร",
            new Vector2(left + 190f, infoY - 22f), new Vector2(380f, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
        // the gauge
        float gx = portrait ? w * 0.5f - 170f : -w * 0.5f + 560f, gw = portrait ? 300f : 260f;
        var bar = UiKit.Panel("Gauge", row, new Color(0.9f, 0.88f, 0.84f), new Vector2(gx, infoY + 8f), new Vector2(gw, 26f));
        float k = Mathf.Clamp01(car.litres / car.tank);
        var fill = UiKit.Panel("Fill", bar, k < 0.15f ? UiKit.Danger : UiKit.PayGreen, new Vector2(-gw * 0.5f + gw * k * 0.5f, 0f), new Vector2(Mathf.Max(8f, gw * k), 26f));
        fill.gameObject.SetActive(k > 0.005f);
        UiKit.Label(row, 22, $"{car.litres:0.0} / {car.tank:0} ลิตร", new Vector2(gx, infoY - 26f), new Vector2(gw, 30f), UiKit.Ink);

        float room = car.tank - car.litres;
        int full = Mathf.CeilToInt(room * FuelClient.Price(car.fuel));
        float by = portrait ? -50f : 0f, bx = portrait ? -w * 0.5f + 130f : w * 0.5f - 470f;
        var id = car.id;
        Btn(row, "฿100", new Vector2(bx, by), 130f, () => Buy(id, 100, false), room > 0.05f);
        Btn(row, "฿500", new Vector2(bx + 145f, by), 130f, () => Buy(id, 500, false), room > 0.05f);
        Btn(row, room > 0.05f ? $"เต็มถัง ฿{full:N0}" : "ถังเต็ม", new Vector2(bx + 335f, by), 230f, () => Buy(id, 0, true), room > 0.05f);
    }

    private static void Btn(Transform parent, string text, Vector2 at, float width, UnityEngine.Events.UnityAction act, bool on)
    {
        var b = UiKit.RoundButton(parent, text, at, new Vector2(width, 66f), UiKit.Gold, UiKit.GoldInk, act, 26);
        b.interactable = on;
    }

    private async void Buy(string car, int baht, bool full)
    {
        if (busy) return;
        busy = true;
        try
        {
            var b = await FuelClient.BuyAsync(car, baht, full);
            if (this == null) return;
            GameAudio.Play("pa_chime", null, 0.4f);
            Say($"เติม {b.added:0.00} ลิตร · จ่าย ฿{b.paid:N0}", false);
            UiKit.FloatAbovePlayer($"-฿{b.paid:N0} ค่าน้ำมัน", UiKit.Price);
            if (!driveMode) { UiKit.Clear(content); LoadCars(); }
        }
        catch (System.Exception e) { if (this != null) Say(FuelClient.Explain(e), true); }
        finally { busy = false; }
    }

    // ── ขับไปที่อื่น ─────────────────────────────────────────────────────────
    private async void LoadTrips()
    {
        var car = DrivableCar.Current;
        var here = Locations.Current;
        if (car == null || here == null)
        {
            UiKit.Clear(content);
            Message("ขับรถของคุณเข้ามาที่ปั๊มก่อน\nแล้วเลือกจุดหมายที่จะขับไป");
            return;
        }
        FuelClient.Quote quote;
        try { quote = await FuelClient.QuoteAsync(here.Id, car.CarId); }
        catch (System.Exception e)
        {
            if (this == null || !driveMode) return;
            UiKit.Clear(content);
            Message(FuelClient.Explain(e));
            return;
        }
        if (this == null || !driveMode) return;
        UiKit.Clear(content);
        bool fuelled = quote.tank > 0f;
        float headY = (bodySize.y - 140f) * 0.5f - 30f;
        UiKit.Label(content, 26, fuelled ? $"{car.CarName} · น้ำมัน {quote.litres:0.0}/{quote.tank:0} ลิตร" : $"{car.CarName} · ปั่นไปเอง",
            new Vector2(0f, headY), new Vector2(bodySize.x - 60f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;

        int cols = portrait ? 3 : 4;
        float cw = portrait ? 220f : 285f, ch = portrait ? 230f : 165f, sx = cw + 12f, sy = ch + 11f;
        int i = 0;
        foreach (var trip in quote.trips)
        {
            var place = Find(trip.to);
            if (place == null || !place.Available) continue;
            int r = i / cols, c = i % cols;
            var at = new Vector2((c - (cols - 1) * 0.5f) * sx, headY - 40f - ch * 0.5f - r * sy);
            TripCard(place, trip, at, new Vector2(cw, ch), fuelled && trip.litres > quote.litres + 0.001f);
            i++;
        }
        if (i == 0) Message("ไม่มีที่ให้ขับไปจากที่นี่");
    }

    private static Location Find(string id)
    {
        foreach (var l in Locations.All) if (l.Id == id) return l;
        return null;
    }

    private void TripCard(Location place, FuelClient.Trip trip, Vector2 at, Vector2 size, bool short_)
    {
        var card = UiKit.Panel("Trip", content, Color.white, at, size, shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        string name = place.NameTh.Split('·')[0].Trim();
        if (place.Id == "city") name = "ตัวเมือง (บ้านเกิด)";
        float top = size.y * 0.5f;
        UiKit.Label(card, 26, name, new Vector2(0f, top - 26f), new Vector2(size.x - 16f, 40f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 20, $"{trip.km:0} กม. · {FuelClient.Hours(trip.hours)}", new Vector2(0f, top - 56f), new Vector2(size.x - 16f, 30f), UiKit.Muted);
        UiKit.Label(card, 20, trip.litres > 0f ? $"ใช้น้ำมัน {trip.litres:0.0} ลิตร" : "ไม่ใช้น้ำมัน", new Vector2(0f, top - 84f), new Vector2(size.x - 16f, 30f),
            short_ ? UiKit.Danger : UiKit.Price);
        var target = place;
        var b = UiKit.RoundButton(card, short_ ? "น้ำมันไม่พอ" : "ออกเดินทาง", new Vector2(0f, -size.y * 0.5f + 36f), new Vector2(size.x - 30f, 56f),
            UiKit.PayGreen, Color.white, () => Go(target), 24);
        b.interactable = !short_;
    }

    private async void Go(Location target)
    {
        var car = DrivableCar.Current;
        var here = Locations.Current;
        if (busy || car == null || here == null) return;
        busy = true;
        try
        {
            var d = await FuelClient.GoAsync(here.Id, target.Id, car.CarId);
            if (this == null) return;
            Hide();
            DriveJourney.Begin(target, d, car);
        }
        catch (System.Exception e) { if (this != null) Say(FuelClient.Explain(e), true); }
        finally { busy = false; }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
