using UnityEngine;
using UnityEngine.UI;

// ที่อยู่ของฉัน (menu → ที่อยู่): type in your house number, หมู่, village, ตำบล and อำเภอ, and pick your
// province from all 77. The province is your home province: the บ้านเกิด town sits there on the map,
// so bus, train and plane fares and the drives in your own vehicle are measured from it.
// Built at runtime; one per scene.
public class AddressUI : MonoBehaviour
{
    private static AddressUI instance;

    private GameObject backdrop, picker;
    private Text title, subtitle, toast, provinceLabel, summary;
    private Coroutine toastRoutine;
    private InputField house, moo, village, tambon, amphoe;
    private string province = Provinces.Default;
    private bool portrait, busy;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { Hide(); return; }
        if (instance == null) instance = new GameObject("AddressUI").AddComponent<AddressUI>();
        instance.backdrop.SetActive(true);
        instance.picker.SetActive(false);
        instance.Load();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var size = UiKit.WindowSize(new Vector2(1200f, 900f), 1400f);
        var body = UiKit.PremiumWindow(transform, "AddressCanvas", size, 54, "icon:home", out backdrop, out title, out subtitle, Hide, out _);
        title.text = "ที่อยู่ของฉัน";
        subtitle.text = "จังหวัดบ้านเกิด ใช้คิดระยะทาง ค่าตั๋ว และค่าน้ำมัน";
        var b = size - new Vector2(40f, 156f);

        float fieldW = portrait ? 640f : 520f, rowH = 96f;
        float y = b.y * 0.5f - 70f;
        Vector2 Col(int i) => portrait ? new Vector2(0f, y - i * rowH) : new Vector2(i % 2 == 0 ? -280f : 280f, y - (i / 2) * rowH);
        house = Field(body, "บ้านเลขที่", "เช่น 99/1", Col(0), fieldW, 12);
        moo = Field(body, "หมู่ที่", "เช่น 3", Col(1), fieldW, 4);
        village = Field(body, "หมู่บ้าน", "ชื่อหมู่บ้าน", Col(2), fieldW, 30);
        tambon = Field(body, "ตำบล", "ชื่อตำบล", Col(3), fieldW, 30);
        amphoe = Field(body, "อำเภอ", "ชื่ออำเภอ", Col(4), fieldW, 30);

        // the province: a button that opens the list of all 77
        var at = Col(5);
        UiKit.Label(body, 22, "จังหวัด", at + new Vector2(0f, 34f), new Vector2(fieldW, 30f), UiKit.Muted, TextAnchor.MiddleLeft);
        var pick = UiKit.RoundButton(body, "", at - new Vector2(0f, 8f), new Vector2(fieldW, 64f), Color.white, UiKit.Ink, OpenPicker, 28);
        pick.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        provinceLabel = pick.GetComponentInChildren<Text>();

        float bottomY = portrait ? y - 6 * rowH - 40f : y - 3 * rowH - 40f;
        summary = UiKit.Label(body, 24, "", new Vector2(0f, bottomY), new Vector2(b.x - 80f, 80f), UiKit.Ink);
        UiKit.RoundButton(body, "บันทึกที่อยู่", new Vector2(0f, bottomY - 100f), new Vector2(360f, 84f), UiKit.PayGreen, Color.white, Save, 32);
        toast = UiKit.ToastBar(body, new Vector2(0f, -b.y * 0.5f + 50f), 800f);
        BuildPicker(body, b);
        backdrop.SetActive(false);
    }

    private InputField Field(Transform parent, string caption, string hint, Vector2 at, float w, int max)
    {
        UiKit.Label(parent, 22, caption, at + new Vector2(0f, 34f), new Vector2(w, 30f), UiKit.Muted, TextAnchor.MiddleLeft);
        var box = UiKit.Panel("Field_" + caption, parent, Color.white, at - new Vector2(0f, 8f), new Vector2(w, 64f), shadow: true);
        box.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        var text = UiKit.Label(box, 28, "", Vector2.zero, new Vector2(w - 30f, 58f), UiKit.Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        var placeholder = UiKit.Label(box, 24, hint, Vector2.zero, new Vector2(w - 30f, 58f), UiKit.Muted, TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        var input = box.gameObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = max;
        input.lineType = InputField.LineType.SingleLine;
        input.onValueChanged.AddListener(_ => ShowSummary());
        return input;
    }

    // All 77 provinces in a scrolling grid over the form
    private void BuildPicker(Transform body, Vector2 b)
    {
        var panel = UiKit.Panel("ProvincePicker", body, UiKit.Cream, Vector2.zero, b);
        picker = panel.gameObject;
        UiKit.Label(panel, 30, "เลือกจังหวัด", new Vector2(0f, b.y * 0.5f - 40f), new Vector2(b.x - 200f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.RoundButton(panel, "ปิด", new Vector2(b.x * 0.5f - 90f, b.y * 0.5f - 40f), new Vector2(140f, 60f), UiKit.Pill, UiKit.Ink, () => picker.SetActive(false), 26);
        var view = UiKit.Rect("Viewport", panel, new Color(0f, 0f, 0f, 0f));
        var viewSize = new Vector2(b.x - 40f, b.y - 110f);
        view.anchoredPosition = new Vector2(0f, -35f);
        view.sizeDelta = viewSize;
        view.gameObject.AddComponent<RectMask2D>();
        var grid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(view, false);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        int cols = portrait ? 3 : 5;
        float cw = (viewSize.x - 20f) / cols, ch = 72f;
        int rows = (Provinces.Names.Length + cols - 1) / cols;
        grid.sizeDelta = new Vector2(viewSize.x, rows * ch + 10f);
        for (int i = 0; i < Provinces.Names.Length; i++)
        {
            string name = Provinces.Names[i];
            var at = new Vector2((i % cols - (cols - 1) * 0.5f) * cw, -ch * 0.5f - (i / cols) * ch - 5f);
            var button = UiKit.RoundButton(grid, name, at, new Vector2(cw - 10f, ch - 10f), Color.white, UiKit.Ink, () => Pick(name), 24);
            ((RectTransform)button.transform).anchorMin = ((RectTransform)button.transform).anchorMax = new Vector2(0.5f, 1f);
        }
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        picker.SetActive(false);
    }

    private void OpenPicker() => picker.SetActive(true);

    private void Pick(string name)
    {
        province = name;
        provinceLabel.text = "จังหวัด" + name;
        picker.SetActive(false);
        ShowSummary();
    }

    private AddressClient.Address Current() => new AddressClient.Address
    {
        house = house.text.Trim(), moo = moo.text.Trim(), village = village.text.Trim(), tambon = tambon.text.Trim(),
        amphoe = amphoe.text.Trim(), province = province,
    };

    private void ShowSummary() => summary.text = AddressClient.Line(Current());

    private async void Load()
    {
        Fill(AddressClient.Last ?? new AddressClient.Address());
        try
        {
            var a = await AddressClient.GetAsync();
            if (this != null) Fill(a);
        }
        catch (System.Exception e)
        {
            if (this != null) toastRoutine = UiKit.Toast(this, toast, e.Message.Contains("offline") ? "ต้องออนไลน์ก่อน" : "โหลดที่อยู่ไม่สำเร็จ", UiKit.Danger, toastRoutine);
        }
    }

    private void Fill(AddressClient.Address a)
    {
        house.text = a.house;
        moo.text = a.moo;
        village.text = a.village;
        tambon.text = a.tambon;
        amphoe.text = a.amphoe;
        Pick(string.IsNullOrEmpty(a.province) ? Provinces.Default : a.province);
    }

    private async void Save()
    {
        if (busy) return;
        busy = true;
        try
        {
            var a = await AddressClient.SetAsync(Current());
            if (this == null) return;
            Fill(a);
            toastRoutine = UiKit.Toast(this, toast, $"บันทึกแล้ว · บ้านเกิด จ.{a.province}", UiKit.PayGreen, toastRoutine);
        }
        catch (System.Exception e)
        {
            if (this != null) toastRoutine = UiKit.Toast(this, toast, e.Message.Contains("offline") ? "ต้องออนไลน์ก่อน"
                : e.Message.Contains("too long") ? "ชื่อยาวเกินไป" : "บันทึกไม่สำเร็จ", UiKit.Danger, toastRoutine);
        }
        finally { busy = false; }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
