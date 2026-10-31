using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ซุ้มแลกรางวัล: every prize on the shelves with its price in points, from ลูกชิ้นปิ้ง up to the
// Red Cross fair style big prizes on gold tiles (หม้อหุงข้าว จักรยาน ทีวี ตู้เย็น). Points keep from one
// festival to the next. Tap a prize, then tap again to swap the points for it.
public class PrizeCounterUI : MonoBehaviour
{
    private static PrizeCounterUI instance;

    private class Tile
    {
        public FairClient.Prize prize;
        public Image back;
        public Text cost;
        public Button button;
        public Text action;
    }

    private GameObject backdrop, prizeLayer;
    private RectTransform body, grid;
    private Text title, subtitle, strip, toast;
    private readonly List<Tile> tiles = new List<Tile>();
    private Coroutine toastRun;
    private string selected;
    private bool busy;

    public static void Show()
    {
        if (instance == null) instance = new GameObject("PrizeCounterUI").AddComponent<PrizeCounterUI>();
        instance.selected = null;
        instance.backdrop.SetActive(true);
        instance.Load();
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (prizeLayer != null) Destroy(prizeLayer);
    }

    private void Awake()
    {
        body = UiKit.PremiumWindow(transform, "PrizeCounterCanvas", UiKit.WindowSize(new Vector2(1540f, 900f), 1500f), 58, "icon:fair",
            out backdrop, out title, out subtitle, Close, out _);
        title.text = "ซุ้มแลกรางวัล";
        subtitle.text = "เอาแต้มจากเกมฝีมือมาแลกของ แต้มสะสมข้ามงานได้";
        strip = FairHud.Strip(body, new Vector2(0f, UiKit.Portrait ? 600f : 330f), 900f);
        grid = UiKit.Container(body, Vector2.zero);
        toast = UiKit.ToastBar(body, new Vector2(0f, UiKit.Portrait ? -600f : -330f), 900f);
        if (UiKit.Portrait) UiKit.Stack((RectTransform)strip.transform.parent, new Vector2(0f, 600f), 0.75f);
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

    private void Build(FairClient.Prize[] prizes)
    {
        UiKit.Clear(grid);
        tiles.Clear();
        int cols = UiKit.Portrait ? 3 : 6;
        Vector2 size = UiKit.Portrait ? new Vector2(220f, 180f) : new Vector2(225f, 182f);
        Vector2 step = size + new Vector2(10f, 10f);
        float top = UiKit.Portrait ? 460f : 205f;
        for (int i = 0; i < prizes.Length; i++)
        {
            var p = prizes[i];
            int c = i % cols, r = i / cols;
            var at = new Vector2((c - (cols - 1) * 0.5f) * step.x, top - r * step.y);
            var tile = new Tile { prize = p };
            var t = UiKit.Panel("Prize", grid, Color.white, at, size, shadow: true);
            t.gameObject.AddComponent<Outline>().effectColor = p.big ? UiKit.Gold : UiKit.CardBorder;
            tile.back = t.GetComponent<Image>();
            if (UiKit.Art("Icons/" + p.icon) != null) UiKit.IconTile(t, p.icon, new Vector2(-size.x * 0.25f, 30f), new Vector2(84f, 84f));
            UiKit.Label(t, 22, p.name, new Vector2(size.x * 0.2f, 42f), new Vector2(size.x * 0.52f, 60f), UiKit.Ink).font = ThaiFont.Display;
            tile.cost = UiKit.Label(t, 22, $"{p.points:N0} แต้ม", new Vector2(size.x * 0.2f, 2f), new Vector2(size.x * 0.52f, 30f), FairHud.PointColor);
            tile.cost.fontStyle = FontStyle.Bold;
            string id = p.id;
            tile.button = UiKit.RoundButton(t, "", new Vector2(0f, -55f), new Vector2(size.x - 24f, 54f), UiKit.Gold, UiKit.GoldInk, () => Pick(id), 22);
            tile.action = tile.button.GetComponentInChildren<Text>();
            tiles.Add(tile);
        }
    }

    private void Render(FairClient.State s)
    {
        strip.text = FairHud.Format(s);
        if (s?.prizes == null) return;
        if (tiles.Count != s.prizes.Length) Build(s.prizes);
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            t.prize = s.prizes[i];
            bool afford = s.points >= t.prize.points;
            bool picked = selected == t.prize.id;
            t.back.color = picked ? new Color32(0xFF, 0xE9, 0xB0, 0xFF) : t.prize.big ? new Color32(0xFF, 0xF4, 0xD0, 0xFF) : Color.white;
            t.action.text = picked ? "แตะอีกครั้งเพื่อแลก" : afford ? "แลก" : $"ขาดอีก {t.prize.points - s.points:N0}";
            t.button.interactable = s.open && afford && !busy;
        }
    }

    private void Pick(string id)
    {
        var f = FairClient.Instance;
        if (f == null || busy) return;
        if (selected != id)
        {
            selected = id;
            Render(f.Current);
            return;
        }
        selected = null;
        Redeem(id);
    }

    private async void Redeem(string id)
    {
        var f = FairClient.Instance;
        busy = true;
        Render(f.Current);
        try
        {
            var s = await f.RedeemAsync(id);
            if (prizeLayer != null) Destroy(prizeLayer);
            prizeLayer = FairPrize.Show(this, body, s.big ? "รางวัลใหญ่!" : "แลกสำเร็จ!", s.icon, $"{s.prize}\nใช้ {s.spent:N0} แต้ม", true, s.big);
            LifeClient.Instance?.Refresh();
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
