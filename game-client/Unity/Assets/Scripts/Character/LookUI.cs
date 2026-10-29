using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Character creator (ออกแบบตัวละคร) and the two shops that change a look later:
//  · Create — first time only, free: gender, face, hairstyle and colour, clothes colours
//  · Barber (ร้านตัดผม) — hairstyle and colour; fancier styles and colours need money and a level
//  · Tailor (ร้านเสื้อผ้า) — top and bottom colours, the same way, and ready-made outfits (ชุด):
//    try one on in the preview, buy it once, then wear it again any time for free
// A live 3D preview (LookPreview) shows every change before you pay; the server checks the price.
public class LookUI : MonoBehaviour
{
    public enum Mode { Create, Barber, Tailor }

    private static LookUI instance;

    private class Row
    {
        public RectTransform Root;
        public readonly List<(Button button, Image image, Text tag, int value)> Items = new List<(Button, Image, Text, int)>();
    }

    private GameObject backdrop;
    private RectTransform body, rowsRoot;
    private Text title, subtitle, costText, toast;
    private Button confirm;
    private RawImage view;
    private LookPreview preview;
    private Coroutine toastRun;
    private Mode mode;
    private Look look, original;
    private int level = 1;
    private readonly HashSet<int> owned = new HashSet<int>();
    private bool busy;
    private readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();

    public static void Show(Mode mode)
    {
        if (instance == null) instance = new GameObject("LookUI").AddComponent<LookUI>();
        instance.Open(mode);
    }

    public static bool IsOpen => instance != null && instance.backdrop.activeSelf;

    private void Awake()
    {
        body = UiKit.PremiumWindow(transform, "LookCanvas", UiKit.WindowSize(new Vector2(1640f, 920f), 1560f), 85, "icon:home",
            out backdrop, out title, out subtitle, Close, out _);
        var stage = UiKit.Panel("Stage", body, new Color(0.98f, 0.93f, 0.82f), new Vector2(-560f, 10f), new Vector2(460f, 660f), shadow: true);
        stage.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        view = new GameObject("View", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        view.rectTransform.SetParent(stage, false);
        view.rectTransform.sizeDelta = new Vector2(440f, 640f);
        view.raycastTarget = false;

        rowsRoot = UiKit.Container(body, new Vector2(230f, 0f));
        costText = UiKit.Label(body, 28, "", new Vector2(230f, -300f), new Vector2(960f, 44f), UiKit.Ink);
        costText.font = ThaiFont.Display;
        var shuffle = UiKit.RoundButton(body, "สุ่ม", new Vector2(-10f, -360f), new Vector2(200f, 70f), UiKit.Pill, UiKit.Price, Shuffle, 28);
        confirm = UiKit.RoundButton(body, "ยืนยัน", new Vector2(420f, -360f), new Vector2(360f, 76f), UiKit.PayGreen, Color.white, Confirm, 32);
        toast = UiKit.ToastBar(body, new Vector2(0f, 300f), 900f);
        if (UiKit.Portrait) // upright: the character on top, the option rows (shrunk to fit) under it, buttons at the bottom
        {
            UiKit.Stack(stage, new Vector2(0f, 407f), 0.85f);
            UiKit.Stack(rowsRoot, new Vector2(0f, -120f), 0.72f);
            costText.rectTransform.sizeDelta = new Vector2(680f, 44f);
            UiKit.Stack(costText.rectTransform, new Vector2(0f, -400f));
            UiKit.Stack((RectTransform)shuffle.transform, new Vector2(-180f, -490f));
            UiKit.Stack((RectTransform)confirm.transform, new Vector2(120f, -490f));
        }
        backdrop.SetActive(false);
    }

    private async void Open(Mode m)
    {
        mode = m;
        var profile = ProfileClient.Instance;
        original = profile != null ? profile.Look : default;
        look = original;
        title.text = m == Mode.Create ? "ออกแบบตัวละคร" : m == Mode.Barber ? "ร้านตัดผม" : "ร้านเสื้อผ้า";
        subtitle.text = m == Mode.Create ? "เลือกเพศ หน้าตา ทรงผม และชุด · ทำได้ครั้งแรกฟรี"
            : m == Mode.Barber ? $"ตัดผม {Service} บาท + ราคาทรง/สี · ทรงพิเศษต้องมีเลเวล"
            : $"ลองชุดได้ฟรี · ซื้อชุดครั้งเดียวใส่ได้ตลอด · ตัดสีเสื้อ {Service} บาท + ราคาสี";
        BuildRows();
        if (preview == null) preview = LookPreview.Create();
        view.texture = preview.Texture;
        preview.Show(look);
        preview.Front();
        backdrop.SetActive(true);
        Freeze(true);
        Refresh();
        if (profile != null && profile.IsOnline)
        {
            var state = await profile.GetLookStateAsync();
            if (state != null)
            {
                level = Mathf.Max(1, state.level);
                owned.Clear();
                if (state.outfits != null) foreach (int o in state.outfits) owned.Add(o);
                Refresh();
            }
        }
    }

    private int Service => mode == Mode.Barber ? 40 : mode == Mode.Tailor ? 60 : 0; // profile.lua SERVICE

    private void Close()
    {
        if (mode == Mode.Create && ProfileClient.Instance != null && !ProfileClient.Instance.LookCreated)
        {
            Say("กด ยืนยัน เพื่อเริ่มเล่นด้วยตัวละครนี้", UiKit.Price);
            return;
        }
        backdrop.SetActive(false);
        Freeze(false);
        if (preview != null) { Destroy(preview.gameObject); preview = null; }
    }

    private static void Freeze(bool frozen)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.InputEnabled = !frozen;
    }

    // ── Option rows ──────────────────────────────────────────────────────────
    private void BuildRows()
    {
        UiKit.Clear(rowsRoot);
        rows.Clear();
        float y = 300f;
        if (mode == Mode.Create)
        {
            AddTextRow("gender", "เพศ", new[] { "ชาย", "หญิง" }, ref y);
            AddTextRow("face", "หน้าตา", new[] { "แบบที่ 1", "แบบที่ 2" }, ref y);
        }
        if (mode != Mode.Tailor)
        {
            var names = new string[Look.HairStyles.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Look.HairStyles[i].Name;
            AddTextRow("hair", "ทรงผม", names, ref y, Look.HairStyles);
            AddSwatchRow("hairColor", "สีผม", Look.HairColors, ref y);
        }
        if (mode == Mode.Tailor)
        {
            var names = new string[Look.Outfits.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Look.Outfits[i].Name;
            AddTextRow("outfit", "ชุด", names, ref y, Look.Outfits, 5, 150f, 17);
        }
        if (mode != Mode.Barber)
        {
            AddSwatchRow("top", "เสื้อ", Look.ClothColors, ref y);
            AddSwatchRow("bottom", "กางเกง/กระโปรง", Look.ClothColors, ref y);
        }
    }

    private Row NewRow(string key, string label, ref float y, float height)
    {
        var row = new Row { Root = UiKit.Container(rowsRoot, new Vector2(0f, y)) };
        UiKit.Label(row.Root, 26, label, new Vector2(-400f, 0f), new Vector2(170f, 40f), UiKit.Ink, TextAnchor.MiddleLeft).font = ThaiFont.Display;
        rows[key] = row;
        y -= height;
        return row;
    }

    private void AddTextRow(string key, string label, string[] names, ref float y, Look.Option[] options = null,
        int perLine = 6, float width = 120f, int fontSize = 21)
    {
        int lines = (names.Length + perLine - 1) / perLine;
        var row = NewRow(key, label, ref y, 30f + lines * 66f);
        for (int i = 0; i < names.Length; i++)
        {
            int value = i;
            var at = new Vector2(-220f + (width - 120f) / 2f + (i % perLine) * (width + 8f), -(i / perLine) * 66f);
            var b = UiKit.RoundButton(row.Root, names[i], at, new Vector2(width, 56f), UiKit.Pill, UiKit.Ink, () => Pick(key, value), fontSize);
            Text tag = null;
            if (options != null && !options[i].Free)
                tag = UiKit.Label(b.transform, 15, "", new Vector2(0f, -22f), new Vector2(width, 18f), UiKit.Price);
            row.Items.Add((b, b.GetComponent<Image>(), tag, value));
        }
    }

    private void AddSwatchRow(string key, string label, Look.Option[] options, ref float y)
    {
        var row = NewRow(key, label, ref y, 92f);
        for (int i = 0; i < options.Length; i++)
        {
            int value = i;
            var at = new Vector2(-240f + i * 66f, 4f);
            var sw = UiKit.Rect("Swatch", row.Root, options[i].Color.a > 0f ? options[i].Color : Color.white);
            sw.GetComponent<Image>().sprite = UiKit.CircleSprite;
            sw.anchoredPosition = at;
            sw.sizeDelta = new Vector2(54f, 54f);
            if (options[i].Color.a <= 0f) // "แบบเดิม": the model's own colours
                UiKit.Label(sw, 15, "เดิม", Vector2.zero, new Vector2(54f, 30f), UiKit.Ink);
            var button = sw.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => Pick(key, value));
            var ring = sw.gameObject.AddComponent<Outline>();
            ring.effectDistance = new Vector2(4f, -4f);
            Text tag = options[i].Free ? null : UiKit.Label(row.Root, 14, "", at + new Vector2(0f, -40f), new Vector2(70f, 20f), UiKit.Price);
            row.Items.Add((button, sw.GetComponent<Image>(), tag, value));
        }
    }

    private int Get(string key) => key switch
    {
        "gender" => look.Gender, "face" => look.Face, "hair" => look.Hair, "hairColor" => look.HairColor, "top" => look.Top, "outfit" => look.Outfit, _ => look.Bottom,
    };

    private Look.Option[] OptionsFor(string key) => key switch
    {
        "hair" => Look.HairStyles, "hairColor" => Look.HairColors, "top" => Look.ClothColors, "bottom" => Look.ClothColors, "outfit" => Look.Outfits, _ => null,
    };

    private void Pick(string key, int value)
    {
        switch (key)
        {
            case "gender": look.Gender = value; break;
            case "face": look.Face = value; break;
            case "hair": look.Hair = value; break;
            case "hairColor": look.HairColor = value; break;
            case "top": look.Top = value; break;
            case "bottom": look.Bottom = value; break;
            case "outfit": look.Outfit = value; break;
        }
        preview.Show(look);
        preview.Front();
        GameAudio.Play("whoosh", null, 0.3f);
        Refresh();
    }

    private void Refresh()
    {
        foreach (var kv in rows)
        {
            var options = OptionsFor(kv.Key);
            int current = Get(kv.Key);
            foreach (var item in kv.Value.Items)
            {
                bool selected = item.value == current;
                bool mine = kv.Key == "outfit" && owned.Contains(item.value);
                bool locked = options != null && !options[item.value].Free && !mine && (mode == Mode.Create || options[item.value].Level > level);
                if (item.tag != null)
                    item.tag.text = mine ? "มีแล้ว" : mode == Mode.Create ? "ร้าน" : options[item.value].Level > level ? $"Lv {options[item.value].Level}" : $"{options[item.value].Price}฿";
                item.button.interactable = !locked;
                var outline = item.button.GetComponent<Outline>();
                if (outline != null) outline.effectColor = selected ? UiKit.Gold : new Color(0f, 0f, 0f, 0.15f);
                else item.image.color = selected ? UiKit.Gold : UiKit.Pill;
            }
        }
        int cost = Cost(out bool changed);
        confirm.interactable = !busy && (mode == Mode.Create || changed);
        costText.text = mode == Mode.Create ? "ตัวเลือกพิเศษเพิ่มเติม ซื้อได้ที่ร้านตัดผมและร้านเสื้อผ้าในเมือง"
            : changed ? $"ค่าบริการรวม {cost} บาท · มีเงิน {(WalletClient.Instance != null ? WalletClient.Instance.Baht : 0)} บาท"
            : "เลือกแบบใหม่ที่อยากได้";
    }

    private int Cost(out bool changed)
    {
        changed = false;
        if (mode == Mode.Create) return 0;
        int cost = 0;
        if (mode == Mode.Barber)
        {
            if (look.Hair != original.Hair) { changed = true; cost += Look.HairStyles[look.Hair].Price; }
            if (look.HairColor != original.HairColor) { changed = true; cost += Look.HairColors[look.HairColor].Price; }
            return changed ? cost + Service : 0;
        }
        // Tailor (profile.lua): sewing new colours costs the service; an outfit is bought once
        if (look.Top != original.Top || look.Bottom != original.Bottom)
        {
            changed = true;
            cost += Service;
            if (look.Top != original.Top) cost += Look.ClothColors[look.Top].Price;
            if (look.Bottom != original.Bottom) cost += Look.ClothColors[look.Bottom].Price;
        }
        if (look.Outfit != original.Outfit)
        {
            changed = true;
            if (look.Outfit > 0 && !owned.Contains(look.Outfit)) cost += Look.Outfits[look.Outfit].Price;
        }
        return cost;
    }

    private void Shuffle()
    {
        var r = new System.Random(Environment.TickCount);
        int Choose(Look.Option[] o, int keep)
        {
            var allowed = new List<int>();
            for (int i = 0; i < o.Length; i++)
                if (o[i].Free || (mode != Mode.Create && o[i].Level <= level)) allowed.Add(i);
            return allowed.Count > 0 ? allowed[r.Next(allowed.Count)] : keep;
        }
        if (mode == Mode.Create) { look.Gender = r.Next(2); look.Face = r.Next(2); }
        if (mode != Mode.Tailor) { look.Hair = Choose(Look.HairStyles, look.Hair); look.HairColor = Choose(Look.HairColors, look.HairColor); }
        if (mode != Mode.Barber) { look.Top = Choose(Look.ClothColors, look.Top); look.Bottom = Choose(Look.ClothColors, look.Bottom); }
        if (mode == Mode.Tailor) look.Outfit = r.Next(3) == 0 ? Choose(Look.Outfits, look.Outfit) : 0;
        preview.Show(look);
        preview.Front();
        Refresh();
    }

    private async void Confirm()
    {
        var profile = ProfileClient.Instance;
        if (busy || profile == null) return;
        if (!profile.IsOnline) { Say("ต้องออนไลน์ก่อน", UiKit.Danger); return; }
        busy = true;
        Refresh();
        try
        {
            var r = await profile.SetLookAsync(look, mode == Mode.Create ? "create" : mode == Mode.Barber ? "barber" : "tailor");
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
            busy = false;
            Close();
            UiKit.FloatAbovePlayer(mode == Mode.Create ? "พร้อมออกไปใช้ชีวิตแล้ว!" : r.paid > 0 ? $"หล่อสวยขึ้น! -{r.paid} ฿" : "เปลี่ยนเรียบร้อย", new Color(1f, 0.85f, 0.4f));
            GameAudio.Play(r.paid > 0 ? "coins" : "bell", null, 0.7f);
        }
        catch (ApiResponseException e)
        {
            string m = e.Message;
            Say(m.Contains("baht") ? "เงินไม่พอ" : m.Contains("level") ? "เลเวลยังไม่ถึง" : m.Contains("no change") ? "ยังไม่ได้เปลี่ยนอะไรเลย"
                : m.Contains("already") ? "ออกแบบไปแล้ว — เปลี่ยนได้ที่ร้านในเมือง" : "บันทึกไม่สำเร็จ ลองใหม่", UiKit.Danger);
            busy = false;
            Refresh();
        }
        catch (Exception)
        {
            Say("บันทึกไม่สำเร็จ ลองใหม่", UiKit.Danger);
            busy = false;
            Refresh();
        }
    }

    private void Say(string message, Color color) => toastRun = UiKit.Toast(this, toast, message, color, toastRun);

    private void OnDestroy()
    {
        if (preview != null) Destroy(preview.gameObject);
        if (instance == this) instance = null;
    }
}
