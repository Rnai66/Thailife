using UnityEngine;
using UnityEngine.UI;

// The ticket strip every fair window shows: gate pass, coupons in hand and points saved up
public static class FairHud
{
    public static readonly Color CouponColor = new Color32(0xE0, 0x5A, 0x3C, 0xFF);
    public static readonly Color PointColor = new Color32(0x2F, 0x7D, 0xC4, 0xFF);

    public static Text Strip(Transform parent, Vector2 position, float width = 640f)
    {
        var bar = UiKit.Panel("FairStrip", parent, UiKit.Pill, position, new Vector2(width, 56f));
        bar.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        var text = UiKit.Label(bar, 26, "", Vector2.zero, new Vector2(width - 24f, 50f), UiKit.Ink);
        text.supportRichText = true;
        text.font = ThaiFont.Display;
        return text;
    }

    public static string Format(FairClient.State s)
    {
        if (s == null) return "กำลังโหลด...";
        string pass = !s.open ? "ไม่มีงานตอนนี้" : s.has_pass ? "<color=#3B6D11>มีบัตรผ่านประตู ✓</color>" : "<color=#993C1D>ยังไม่มีบัตรผ่านประตู</color>";
        return $"{pass}   ·   คูปอง <color=#E05A3C><b>{s.coupons}</b></color>   ·   แต้ม <color=#2F7DC4><b>{s.points:N0}</b></color>";
    }

    // A paper ticket drawn in UI: coloured stub, perforation dots, the words on it
    public static RectTransform Ticket(Transform parent, Vector2 position, Vector2 size, Color color, string line1, string line2)
    {
        var t = UiKit.Panel("Ticket", parent, color, position, size, shadow: true);
        var stub = UiKit.Rect("Stub", t, new Color(1f, 1f, 1f, 0.25f));
        stub.anchoredPosition = new Vector2(-size.x * 0.5f + size.x * 0.12f, 0f);
        stub.sizeDelta = new Vector2(size.x * 0.24f, size.y - 16f);
        for (int i = 0; i < 7; i++)
        {
            var dot = UiKit.Rect("Hole", t, UiKit.Cream);
            dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
            dot.anchoredPosition = new Vector2(-size.x * 0.5f + size.x * 0.24f, -size.y * 0.4f + i * size.y * 0.8f / 6f);
            dot.sizeDelta = new Vector2(10f, 10f);
        }
        UiKit.Cartoon(UiKit.Label(t, 34, line1, new Vector2(size.x * 0.12f, size.y * 0.15f), new Vector2(size.x * 0.72f, size.y * 0.4f), Color.white), 2.5f).font = ThaiFont.Display;
        UiKit.Label(t, 22, line2, new Vector2(size.x * 0.12f, -size.y * 0.2f), new Vector2(size.x * 0.72f, size.y * 0.35f), new Color(1f, 1f, 1f, 0.9f));
        UiKit.Label(stub, 26, "★", Vector2.zero, stub.sizeDelta, Color.white);
        foreach (var g in t.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        return t;
    }
}
