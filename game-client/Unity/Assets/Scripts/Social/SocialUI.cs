using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// The เพื่อน window (SocialClient):
//  · แชท — talk with everyone on the server, or privately with a friend; report a rude message
//  · เพื่อน — requests to answer, your friends (chat, propose, unfriend, block), players around you to add
//  · คู่ชีวิต — your spouse, proposals waiting for your answer, divorce
public class SocialUI : MonoBehaviour
{
    private const string ChatTab = "chat", FriendsTab = "friends", LoveTab = "love";
    private const int MaxChars = 120;

    private static SocialUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, toastText;
    private RectTransform tabs, content, viewport, inputRow;
    private ScrollRect scroll;
    private InputField input;
    private Coroutine toast;
    private string tab = ChatTab;
    private string talkingTo; // null = everyone
    private string armed;
    private bool busy, typing;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.Close(); return; }
        Open(null, null);
    }

    // Open on a tab ("chat" with a friend's id opens that conversation)
    public static void Open(string which, string friendId)
    {
        if (instance == null) instance = new GameObject("SocialUI").AddComponent<SocialUI>();
        if (which != null) instance.tab = which;
        if (friendId != null) instance.talkingTo = friendId;
        instance.armed = null;
        instance.backdrop.SetActive(true);
        instance.Render();
        var s = SocialClient.Instance;
        if (s != null && s.IsOnline) { _ = Quiet(s.RefreshFriendsAsync()); _ = Quiet(s.RefreshMarriageAsync()); }
    }

    private static async Task Quiet(Task t)
    {
        try { await t; } catch (Exception) { }
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (SocialClient.Instance != null) SocialClient.Instance.Viewing = null;
        if (typing) Freeze(false);
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "SocialCanvas", UiKit.WindowSize(new Vector2(1400f, 940f), 1500f), 56, "พ",
            out backdrop, out titleText, out subtitleText, Close, out _);
        titleText.text = "เพื่อน";
        float w = upright ? 700f : 1300f;
        tabs = UiKit.Container(body, new Vector2(0f, upright ? 615f : 345f));

        Vector2 size = upright ? new Vector2(w, 1120f) : new Vector2(w, 590f);
        viewport = UiKit.Rect("Viewport", body, new Color(0f, 0f, 0f, 0f));
        viewport.anchoredPosition = new Vector2(0f, upright ? 15f : -10f);
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

        // The typing row under the messages (chat tab only)
        inputRow = UiKit.Container(body, new Vector2(0f, upright ? -605f : -350f));
        float fieldW = w - 230f;
        var field = UiKit.Panel("ChatInput", inputRow, Color.white, new Vector2(-115f, 0f), new Vector2(fieldW, 76f), shadow: true);
        field.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        var text = UiKit.Label(field, 26, "", Vector2.zero, new Vector2(fieldW - 40f, 66f), UiKit.Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        var placeholder = UiKit.Label(field, 24, "พิมพ์ข้อความ…", Vector2.zero, new Vector2(fieldW - 40f, 66f), UiKit.Muted, TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        input = field.gameObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = MaxChars;
        input.lineType = InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(_ => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Send(); });
        UiKit.RoundButton(inputRow, "ส่ง", new Vector2(w * 0.5f - 105f, 0f), new Vector2(200f, 76f), UiKit.PayGreen, Color.white, Send, 30);

        toastText = UiKit.ToastBar(body, new Vector2(0f, upright ? 520f : 250f), 900f);
        backdrop.SetActive(false);
        if (SocialClient.Instance != null) SocialClient.Instance.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        if (SocialClient.Instance != null) SocialClient.Instance.Changed -= OnChanged;
        if (instance == this) instance = null;
    }

    private void OnChanged()
    {
        if (backdrop != null && backdrop.activeSelf) Render();
    }

    // Typing W/A/S/D or J/K/L must not move or punch
    private void Update()
    {
        if (!backdrop.activeSelf) return;
        bool now = input.isFocused;
        if (now != typing) Freeze(now);
    }

    private void Freeze(bool on)
    {
        typing = on;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.InputEnabled = !on;
    }

    // ── Layout helpers (content hangs from its top edge) ───────────────────────────────────
    private float y;

    private T Top<T>(T c) where T : Component
    {
        var rt = (RectTransform)c.transform;
        var p = rt.anchoredPosition;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = p;
        return c;
    }

    private float Width => content.sizeDelta.x;

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

    // A row: a name and a few small buttons on the right
    private void Row(string label, Color ink, params (string text, Color fill, Color ink, Action act)[] buttons)
    {
        const float h = 74f;
        y -= h * 0.5f;
        var card = Top(UiKit.Panel("Row", content, Color.white, new Vector2(0f, y), new Vector2(Width - 20f, h), shadow: false));
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        bool upright = UiKit.Portrait;
        float bw = upright ? 128f : 170f;
        float labelW = Width - 60f - buttons.Length * (bw + 10f);
        UiKit.Label(card, 24, label, new Vector2(-(Width - 20f) * 0.5f + 20f + labelW * 0.5f, 0f), new Vector2(labelW, h - 10f), ink, TextAnchor.MiddleLeft);
        float x = (Width - 20f) * 0.5f - 12f - bw * 0.5f;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            var b = buttons[i];
            var act = b.act;
            var btn = UiKit.RoundButton(card, b.text, new Vector2(x, 0f), new Vector2(bw, 56f), b.fill, b.ink, () => act?.Invoke(), upright ? 18 : 20);
            btn.interactable = !busy && act != null;
            x -= bw + 10f;
        }
        y -= h * 0.5f + 10f;
    }

    // ── Drawing ───────────────────────────────────────────────────────────────────────────
    private void Render()
    {
        var s = SocialClient.Instance;
        bool upright = UiKit.Portrait;
        UiKit.Clear(tabs);
        float tw = upright ? 220f : 300f;
        var list = new[] { (ChatTab, "แชท", -tw - 10f), (FriendsTab, "เพื่อน", 0f), (LoveTab, "คู่ชีวิต", tw + 10f) };
        foreach (var (id, label, x) in list)
        {
            bool on = tab == id;
            string which = id;
            string caption = label;
            if (s != null && id == ChatTab && !on && s.UnreadTotal > 0) caption += $" ({s.UnreadTotal})";
            if (s != null && id == FriendsTab && Count(s, SocialClient.Requested) > 0) caption += $" ({Count(s, SocialClient.Requested)})";
            if (s != null && id == LoveTab && s.Wedding?.proposals != null && s.Wedding.proposals.Length > 0) caption += " (!)";
            UiKit.RoundButton(tabs, caption, new Vector2(x, 0f), new Vector2(tw, 60f), on ? UiKit.Teak : UiKit.Pill, on ? UiKit.GoldText : UiKit.Ink,
                () => { tab = which; armed = null; Render(); }, 24);
        }
        inputRow.gameObject.SetActive(tab == ChatTab);
        UiKit.Clear(content);
        y = 0f;

        if (s == null || !s.IsOnline)
        {
            subtitleText.text = "ต้องออนไลน์ก่อน";
            Note("ยังไม่ได้เชื่อมต่อเซิร์ฟเวอร์", UiKit.Muted);
            Finish(top: true);
            return;
        }
        s.Viewing = tab == ChatTab ? (talkingTo ?? "") : null;
        if (tab == ChatTab) { s.MarkRead(talkingTo); RenderChat(s); Finish(top: false); }
        else if (tab == FriendsTab) { subtitleText.text = "ขอเป็นเพื่อน แชทส่วนตัว และขอแต่งงาน"; RenderFriends(s); Finish(top: true); }
        else { subtitleText.text = "แต่งงานกับเพื่อนที่รู้ใจ"; RenderLove(s); Finish(top: true); }
    }

    private void Finish(bool top)
    {
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(viewport.sizeDelta.y, -y + 20f));
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = top ? 1f : 0f;
    }

    private static int Count(SocialClient s, int state)
    {
        int n = 0;
        foreach (var f in s.FriendList) if (f.state == state) n++;
        return n;
    }

    private void RenderChat(SocialClient s)
    {
        var friend = talkingTo != null ? s.FindFriend(talkingTo) : null;
        if (talkingTo != null && (friend == null || friend.state != SocialClient.Friends)) { talkingTo = null; friend = null; }
        subtitleText.text = friend == null ? "คุยกับทุกคนในเซิร์ฟเวอร์ · สุภาพกันนะ" : $"คุยส่วนตัวกับ {friend.name}";

        // Who to talk to: everyone, or one of the friends
        var who = new List<(string id, string name)> { (null, "ทุกคน") };
        foreach (var f in s.FriendList) if (f.state == SocialClient.Friends) who.Add((f.id, f.name));
        float pw = UiKit.Portrait ? 160f : 200f;
        int perRow = Mathf.Max(1, Mathf.FloorToInt((Width - 20f) / (pw + 10f)));
        for (int i = 0; i < who.Count; i++)
        {
            int row = i / perRow, col = i % perRow;
            int inRow = Mathf.Min(perRow, who.Count - row * perRow);
            var (id, name) = who[i];
            bool on = id == talkingTo;
            int n = s.Unread(id);
            string target = id;
            var b = UiKit.RoundButton(content, n > 0 && !on ? $"{name} ({n})" : name,
                new Vector2((col - (inRow - 1) * 0.5f) * (pw + 10f), y - 30f - row * 66f), new Vector2(pw, 56f),
                on ? UiKit.Teak : UiKit.Pill, on ? UiKit.GoldText : UiKit.Ink, () => { talkingTo = target; armed = null; Render(); }, 20);
            Top(b);
        }
        y -= 30f + ((who.Count - 1) / perRow) * 66f + 44f;

        var lines = s.Conversation(talkingTo);
        if (lines.Count == 0)
        {
            Note(friend == null ? "ยังไม่มีใครคุย ทักทายเลย!" : $"เริ่มคุยกับ {friend.name} ได้เลย", UiKit.Muted);
            return;
        }
        float w = Width - 40f;
        foreach (var line in lines)
        {
            string said = $"{line.name}: {line.text}";
            bool canReport = !line.mine && !string.IsNullOrEmpty(line.from);
            float textW = canReport ? w - 140f : w;
            var label = UiKit.Label(content, 23, said, Vector2.zero, new Vector2(textW, 40f),
                line.mine ? UiKit.OkGreen : friend != null ? new Color(0.2f, 0.35f, 0.65f) : UiKit.Ink, TextAnchor.UpperLeft);
            label.supportRichText = false;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            float h = Mathf.Max(34f, label.preferredHeight + 6f);
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(textW, h);
            rt.anchoredPosition = new Vector2(-w * 0.5f + textW * 0.5f, y - h * 0.5f - 4f);
            Top(label);
            if (canReport)
            {
                string key = "report:" + line.GetHashCode();
                var l = line;
                var b = UiKit.RoundButton(content, armed == key ? "ยืนยัน?" : "รายงาน", new Vector2(w * 0.5f - 60f, y - 22f), new Vector2(120f, 38f),
                    UiKit.DangerTint, UiKit.Danger, () => Report(key, l), 16);
                Top(b).interactable = !busy;
            }
            y -= h + 8f;
        }
    }

    private void RenderFriends(SocialClient s)
    {
        var gold = (UiKit.Gold, UiKit.GoldInk);
        var plain = (UiKit.Pill, UiKit.Ink);
        var red = (UiKit.DangerTint, UiKit.Danger);

        bool any = false;
        foreach (var f in s.FriendList)
        {
            if (f.state != SocialClient.Requested) continue;
            if (!any) { Heading("ขอเป็นเพื่อนกับคุณ"); any = true; }
            string id = f.id;
            Row(f.name, UiKit.Ink,
                ("รับเป็นเพื่อน", gold.Item1, gold.Item2, () => Do(c => c.AddFriendAsync(id), "เป็นเพื่อนกันแล้ว!")),
                ("ไม่รับ", red.Item1, red.Item2, () => Do(c => c.RemoveFriendAsync(id), "ไม่รับคำขอแล้ว")));
        }

        Heading("เพื่อนของคุณ");
        bool married = !string.IsNullOrEmpty(s.Wedding?.spouse_id);
        bool proposed = !string.IsNullOrEmpty(s.Wedding?.proposed_to);
        int ring = s.Wedding != null && s.Wedding.ring_cost > 0 ? s.Wedding.ring_cost : 5000;
        any = false;
        foreach (var f in s.FriendList)
        {
            if (f.state != SocialClient.Friends) continue;
            any = true;
            string id = f.id;
            bool spouse = s.Wedding?.spouse_id == id;
            string proposeKey = "propose:" + id, removeKey = "remove:" + id;
            Action propose = married || proposed ? null : (Action)(() => Arm(proposeKey, c => c.ProposeAsync(id)));
            Row((f.online ? "● " : "○ ") + f.name + (spouse ? "  · คู่ชีวิต" : ""), f.online ? UiKit.OkGreen : UiKit.Ink,
                ("แชท", UiKit.PayGreen, Color.white, () => { talkingTo = id; tab = ChatTab; Render(); }),
                (spouse ? "คู่ชีวิต" : armed == proposeKey ? $"ยืนยัน {ring:N0}฿" : "ขอแต่งงาน", new Color(1f, 0.85f, 0.9f), new Color(0.6f, 0.1f, 0.3f), spouse ? null : propose),
                (armed == removeKey ? "ยืนยันลบ" : "ลบเพื่อน", plain.Item1, plain.Item2, spouse ? null : (Action)(() => Arm(removeKey, c => c.RemoveFriendAsync(id), "ลบเพื่อนแล้ว"))));
        }
        if (!any) Note("ยังไม่มีเพื่อน · เพิ่มเพื่อนจากผู้เล่นที่อยู่ใกล้ๆ ด้านล่าง", UiKit.Muted);

        Heading("ผู้เล่นที่อยู่แถวนี้");
        any = false;
        if (MatchSync.Instance != null)
            foreach (var r in MatchSync.Instance.Others)
            {
                var f = s.FindFriend(r.UserId);
                if (f != null && (f.state == SocialClient.Friends || f.state == SocialClient.Requested)) continue;
                any = true;
                string id = r.UserId;
                string blockKey = "block:" + id;
                bool asked = f != null && f.state == SocialClient.Asked;
                bool blocked = f != null && f.state == SocialClient.Blocked;
                Row(r.DisplayName + (asked ? " · ส่งคำขอแล้ว" : blocked ? " · บล็อกอยู่" : ""), UiKit.Ink,
                    (asked ? "ส่งแล้ว" : "เพิ่มเพื่อน", gold.Item1, gold.Item2, asked || blocked ? null : (Action)(() => Do(c => c.AddFriendAsync(id), "ส่งคำขอเป็นเพื่อนแล้ว"))),
                    (armed == blockKey ? "ยืนยันบล็อก" : "บล็อก", red.Item1, red.Item2, blocked ? null : (Action)(() => Arm(blockKey, c => c.BlockAsync(id), "บล็อกแล้ว ไม่เห็นข้อความของคนนี้อีก"))));
            }
        if (!any) Note("ไม่มีผู้เล่นคนอื่นอยู่แถวนี้ตอนนี้", UiKit.Muted);

        any = false;
        foreach (var f in s.FriendList)
        {
            if (f.state != SocialClient.Asked && f.state != SocialClient.Blocked) continue;
            if (MatchSync.Instance != null && MatchSync.Instance.FindByUser(f.id) != null) continue; // shown above
            if (!any) { Heading("ส่งคำขอแล้ว / บล็อกไว้"); any = true; }
            string id = f.id;
            bool blocked = f.state == SocialClient.Blocked;
            Row(f.name + (blocked ? " · บล็อกอยู่" : " · รอตอบรับ"), UiKit.Muted,
                (blocked ? "เลิกบล็อก" : "ยกเลิกคำขอ", plain.Item1, plain.Item2, () => Do(c => c.RemoveFriendAsync(id), blocked ? "เลิกบล็อกแล้ว" : "ยกเลิกคำขอแล้ว")));
        }
    }

    private void RenderLove(SocialClient s)
    {
        var m = s.Wedding;
        if (m == null) { Note("กำลังโหลด…", UiKit.Muted); return; }
        if (!string.IsNullOrEmpty(m.spouse_id))
        {
            Heading("คู่ชีวิตของคุณ");
            string since = m.since_ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(m.since_ms).ToLocalTime().ToString("d MMM yyyy") : "";
            Note($"แต่งงานกับ {m.spouse_name}" + (since != "" ? $" · ตั้งแต่ {since}" : ""), new Color(0.6f, 0.1f, 0.3f), 26);
            y -= 10f;
            string id = m.spouse_id;
            Row(m.spouse_name, UiKit.Ink,
                ("แชท", UiKit.PayGreen, Color.white, () => { talkingTo = id; tab = ChatTab; Render(); }),
                (armed == "divorce" ? "ยืนยันหย่า" : "หย่า", UiKit.DangerTint, UiKit.Danger, () => Arm("divorce", c => c.DivorceAsync())));
        }
        else if (!string.IsNullOrEmpty(m.proposed_to))
        {
            Heading("รอคำตอบ");
            Note($"มอบแหวนขอ {m.proposed_name} แต่งงานแล้ว · ถ้าปฏิเสธจะได้ค่าแหวนคืน", UiKit.Price);
        }
        else
        {
            Heading("ยังโสด");
            Note($"ไปที่แท็บ \"เพื่อน\" แล้วกด \"ขอแต่งงาน\" กับเพื่อนที่รู้ใจ · ค่าแหวนหมั้น {Mathf.Max(m.ring_cost, 0):N0} บาท", UiKit.Muted);
            Note("ห้อยพระที่มีพุทธคุณ \"เสน่ห์\" (พระขุนแผน พระนางพญา) ช่วยเสริมเสน่ห์ให้คนรัก", UiKit.Price, 19);
        }

        if (m.proposals != null && m.proposals.Length > 0)
        {
            Heading("มีคนขอคุณแต่งงาน!");
            foreach (var p in m.proposals)
            {
                string from = p.user_id;
                bool free = string.IsNullOrEmpty(m.spouse_id);
                Row($"ขอแต่งงาน: {p.name}", new Color(0.6f, 0.1f, 0.3f),
                    ("ตกลง", UiKit.Gold, UiKit.GoldInk, free ? (Action)(() => Do(c => c.AnswerAsync(from, true))) : null),
                    ("ปฏิเสธ", UiKit.DangerTint, UiKit.Danger, () => Do(c => c.AnswerAsync(from, false))));
            }
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────────────────
    private async void Send()
    {
        var s = SocialClient.Instance;
        string text = input.text.Trim();
        if (busy || s == null || !s.IsOnline || text.Length == 0) return;
        busy = true;
        try
        {
            await s.SendAsync(text, talkingTo);
            input.text = "";
        }
        catch (Exception e) { Notify(SocialClient.Explain(e), UiKit.Danger); }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void Report(string key, SocialClient.Line line)
    {
        if (armed != key) { armed = key; Render(); return; }
        Do(c => c.ReportAsync(line.from, line.text), "ส่งรายงานแล้ว ขอบคุณ · บล็อกคนนี้ได้ที่แท็บเพื่อน");
    }

    // Big steps take two taps
    private void Arm(string what, Func<SocialClient, Task> action, string done = null)
    {
        if (armed == what) Do(action, done);
        else { armed = what; Render(); }
    }

    private async void Do(Func<SocialClient, Task> action, string done = null)
    {
        var s = SocialClient.Instance;
        if (busy || s == null) return;
        busy = true;
        armed = null;
        Render();
        try
        {
            var task = action(s);
            await task;
            string said = done;
            if (task is Task<SocialClient.Marriage> wedding && !string.IsNullOrEmpty(wedding.Result?.done)) said = wedding.Result.done;
            if (!string.IsNullOrEmpty(said)) Notify(said, UiKit.PayGreen);
            GameAudio.Play("bell");
        }
        catch (ApiResponseException e) { Notify(SocialClient.Explain(e), UiKit.Danger); }
        catch (Exception) { Notify("เกิดข้อผิดพลาด ลองใหม่", UiKit.Danger); }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void Notify(string message, Color color) => toast = UiKit.Toast(this, toastText, message, color, toast);
}
