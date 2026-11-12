using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Professions window (อาชีพ, trades.lua): every profession with its level, XP bar and rank title,
// and the next title to aim for. "แสดง" puts that profession's title by your name; "อัตโนมัติ"
// shows your highest one. Opened by tapping the level line on the HUD.
public class ProfessionUI : MonoBehaviour
{
    [Serializable]
    private class Trade
    {
        public string id, name, how, rank, next_rank;
        public int level, xp, xp_to_next, next_at;
    }

    [Serializable]
    private class State
    {
        public Trade[] trades;
        public string shown, title;
        public bool picked;
    }

    [Serializable]
    private class ShowRequest { public string trade; }

    private static readonly Dictionary<string, string> Short = new Dictionary<string, string>
    {
        { "boxer", "มวย" }, { "fisher", "ปลา" }, { "farmer", "นา" }, { "fishpond", "บ่อ" }, { "chicken", "ไก่" },
        { "cattle", "วัว" }, { "musician", "ดนตรี" }, { "merchant", "ค้า" }, { "repair", "ช่าง" }, { "computer", "คอม" },
        { "teacher", "ครู" }, { "programmer", "โค้ด" }, { "doctor", "หมอ" }, { "guide", "เรือ" }, { "politician", "สภา" },
    };

    private static ProfessionUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, shownText, toastText;
    private RectTransform list;
    private Button autoButton;
    private State state;
    private Coroutine toast;
    private bool busy, portrait;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null) instance = new GameObject("ProfessionUI").AddComponent<ProfessionUI>();
        instance.backdrop.SetActive(true);
        instance.Render();
        instance.Call("trades_get", "{}");
    }

    private void Close() => backdrop.SetActive(false);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Awake()
    {
        portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "ProfessionCanvas", UiKit.WindowSize(new Vector2(1720f, 1000f), 1740f), 56, "icon:school",
            out backdrop, out titleText, out subtitleText, Close, out _);
        titleText.text = "อาชีพของฉัน";
        subtitleText.text = "ทำงานไหนบ่อย อาชีพนั้นเลเวลขึ้น · ฉายาใหม่ที่ Lv 10/25/50/75/100";

        float top = portrait ? 722f : 376f;
        shownText = UiKit.Label(body, 30, "", new Vector2(portrait ? -120f : -170f, top), new Vector2(portrait ? 460f : 1100f, 48f), UiKit.Ink, TextAnchor.MiddleLeft);
        shownText.fontStyle = FontStyle.Bold;
        autoButton = UiKit.RoundButton(body, "อัตโนมัติ (สูงสุด)", new Vector2(portrait ? 230f : 620f, top), new Vector2(portrait ? 230f : 320f, 56f), UiKit.Pill, UiKit.GoldInk,
            () => Show(""), 24);
        list = UiKit.Container(body, Vector2.zero);
        toastText = UiKit.ToastBar(body, new Vector2(0f, portrait ? -755f : -380f), 900f);
        backdrop.SetActive(false);
    }

    private void Render()
    {
        UiKit.Clear(list);
        if (state?.trades == null)
        {
            LoadingFlower.Show(shownText);
            autoButton.interactable = false;
            return;
        }
        shownText.text = $"ฉายาที่แสดง: {state.title}" + (state.picked ? "" : "  (อาชีพที่เลเวลสูงสุด)");
        autoButton.interactable = state.picked && !busy;
        for (int i = 0; i < state.trades.Length; i++)
            Card(state.trades[i], portrait ? new Vector2(0f, 655f - i * 98f) : new Vector2(i % 2 == 0 ? -420f : 420f, 300f - (i / 2) * 96f));
    }

    private void Card(Trade t, Vector2 at)
    {
        bool shown = t.id == state.shown;
        // landscape cards are wider (two columns); upright ones fill the width. Left: name, rank, XP bar.
        // Right: the next title, how to earn XP, the "show" button.
        float w = portrait ? 700f : 820f, h = 92f;
        float colW = portrait ? 300f : 350f, rightW = portrait ? 240f : 300f;
        float colX = -w * 0.5f + 100f + colW * 0.5f, rightX = w * 0.5f - 14f - rightW * 0.5f;
        var card = UiKit.Panel("Trade", list, shown ? new Color32(0xFF, 0xF6, 0xDF, 0xFF) : Color.white, at, new Vector2(w, h), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = shown ? UiKit.Gold : UiKit.CardBorder;

        var tile = UiKit.Panel("Icon", card, UiKit.Pill, new Vector2(-w * 0.5f + 50f, 0f), new Vector2(76f, 76f));
        UiKit.Label(tile, 28, Short.TryGetValue(t.id, out var s) ? s : "?", Vector2.zero, new Vector2(72f, 72f), UiKit.Price).fontStyle = FontStyle.Bold;

        UiKit.Label(card, 28, $"{t.name} · Lv {t.level}", new Vector2(colX, 25f), new Vector2(colW, 38f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 23, t.rank, new Vector2(colX, -8f), new Vector2(colW, 32f), UiKit.Price, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        var track = UiKit.Panel("Track", card, new Color32(0xF1, 0xEF, 0xE8, 0xFF), new Vector2(colX, -34f), new Vector2(colW, 12f));
        var fill = UiKit.Panel("Fill", track, UiKit.PayGreen, Vector2.zero,
            new Vector2(t.xp_to_next > 0 ? colW * Mathf.Clamp01((float)t.xp / t.xp_to_next) : colW, 12f));
        fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);

        UiKit.Label(card, 21, t.next_at > 0 ? $"Lv {t.next_at}: {t.next_rank}" : "สูงสุดแล้ว!", new Vector2(rightX, 28f), new Vector2(rightW, 30f), UiKit.Ink);
        UiKit.Label(card, 20, t.how, new Vector2(rightX, 1f), new Vector2(rightW, 28f), UiKit.Muted);
        string id = t.id;
        var b = UiKit.RoundButton(card, shown ? "กำลังแสดง" : "แสดง", new Vector2(rightX, -29f), new Vector2(190f, 34f),
            shown ? UiKit.OkTint : UiKit.Gold, shown ? UiKit.OkGreen : UiKit.GoldInk, () => Show(id), 21);
        b.interactable = !shown && !busy;
    }

    private void Show(string trade) => Call("trade_show", JsonUtility.ToJson(new ShowRequest { trade = trade }), true);

    private async void Call(string rpc, string payload, bool changesTitle = false)
    {
        var conn = FindAnyObjectByType<NakamaConnection>();
        if (conn == null || !conn.IsConnected) { Notify("ต้องออนไลน์ก่อน", UiKit.Danger); return; }
        if (busy) return;
        busy = true;
        try
        {
            var result = await conn.Client.RpcAsync(conn.Session, rpc, payload);
            state = JsonUtility.FromJson<State>(result.Payload);
            if (changesTitle)
            {
                Notify($"ฉายาใหม่: {state.title}", UiKit.PayGreen);
                CareerClient.Instance?.Reload(); // the HUD line picks up the new title
            }
        }
        catch (ApiResponseException e)
        {
            Notify("ทำรายการไม่สำเร็จ", UiKit.Danger);
            Debug.LogWarning($"[Professions] {e.Message}");
        }
        catch (Exception e)
        {
            Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger);
            Debug.LogWarning($"[Professions] {e.Message}");
        }
        busy = false;
        if (backdrop.activeSelf) Render();
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
