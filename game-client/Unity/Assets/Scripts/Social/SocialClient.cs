using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Chat, friends and marriage (social.lua + Nakama's own friends). Listens to the server-wide room
// "world" (only the server posts into it, after cleaning the text), messages from friends arrive as
// notifications (also the ones sent while we were away), and so do friend requests, proposals and
// their answers. Blocked players' messages are never shown.
[RequireComponent(typeof(NakamaConnection))]
public class SocialClient : MonoBehaviour
{
    public class Line
    {
        public string from, name, text;
        public bool mine;
    }

    public class Friend
    {
        public string id, name;
        public int state; // 0 friends · 1 we asked · 2 they asked · 3 blocked
        public bool online;
    }

    [Serializable] public class Proposal { public string user_id, name; }

    [Serializable]
    public class Marriage
    {
        public string spouse_id, spouse_name, proposed_to, proposed_name, done;
        public int ring_cost;
        public long since_ms;
        public Proposal[] proposals;
    }

    [Serializable] private class RoomMessage { public string text, name; }
    [Serializable] private class DirectMessage { public string text, from, name; }
    [Serializable] private class SendRequest { public string text, to; }
    [Serializable] private class SendResult { public string text; }
    [Serializable] private class ReportRequest { public string user_id, text; }
    [Serializable] private class ProposeRequest { public string to; }
    [Serializable] private class AnswerRequest { public string from; public bool accept; }

    public const int Friends = 0, Asked = 1, Requested = 2, Blocked = 3;
    private const int NoteChat = 100, NoteProposal = 110, NoteAccepted = 111, NoteDeclined = 112, NoteDivorced = 113;
    private const int NoteFriendRequest = -2, NoteFriendAccepted = -3;
    private const int Keep = 60;

    public static SocialClient Instance { get; private set; }

    public readonly List<Line> World = new List<Line>();
    private readonly Dictionary<string, List<Line>> direct = new Dictionary<string, List<Line>>();
    public readonly List<Friend> FriendList = new List<Friend>();
    public Marriage Wedding { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected;
    public string SelfId => connection?.Session?.UserId;
    // unread counts: "" = the room, otherwise a friend's id
    private readonly Dictionary<string, int> unread = new Dictionary<string, int>();
    public string Viewing { get; set; } // the conversation open in SocialUI (null = window closed)

    public event Action Changed;

    private NakamaConnection connection;
    private IChannel room;

    private void Awake()
    {
        Instance = this;
        connection = GetComponent<NakamaConnection>();
    }

    private void OnEnable()
    {
        connection.Connected += OnConnected;
        if (connection.IsConnected) OnConnected(connection);
    }

    private void OnDisable()
    {
        connection.Connected -= OnConnected;
        if (connection.Socket != null)
        {
            connection.Socket.ReceivedChannelMessage -= OnRoomMessage;
            connection.Socket.ReceivedNotification -= OnNotification;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        conn.Socket.ReceivedChannelMessage -= OnRoomMessage;
        conn.Socket.ReceivedNotification -= OnNotification;
        conn.Socket.ReceivedChannelMessage += OnRoomMessage;
        conn.Socket.ReceivedNotification += OnNotification;
        try
        {
            await RefreshFriendsAsync();
            await RefreshMarriageAsync();
            room = await conn.Socket.JoinChatAsync("world", ChannelType.Room, persistence: true, hidden: false);
            var history = await conn.Client.ListChannelMessagesAsync(conn.Session, room.Id, 30, false);
            World.Clear();
            var list = new List<IApiChannelMessage>(history.Messages);
            for (int i = list.Count - 1; i >= 0; i--) AddRoom(list[i], quiet: true);
            await ReadWaitingAsync();
            Changed?.Invoke();
        }
        catch (Exception e) { Debug.LogWarning($"[Social] {e.Message}"); }
    }

    // Messages and requests that came while we were away (kept as notifications)
    private async Task ReadWaitingAsync()
    {
        var waiting = await connection.Client.ListNotificationsAsync(connection.Session, 100);
        var done = new List<string>();
        var notes = new List<IApiNotification>(waiting.Notifications);
        notes.Sort((a, b) => string.CompareOrdinal(a.CreateTime, b.CreateTime));
        foreach (var n in notes)
        {
            Handle(n, quiet: n.Code != NoteChat);
            if (n.Code == NoteChat || n.Code >= NoteProposal) done.Add(n.Id);
        }
        if (done.Count > 0) await connection.Client.DeleteNotificationsAsync(connection.Session, done);
    }

    // ── Reading ────────────────────────────────────────────────────────────────────────────
    public List<Line> Conversation(string friendId)
    {
        if (string.IsNullOrEmpty(friendId)) return World;
        if (!direct.TryGetValue(friendId, out var list)) direct[friendId] = list = new List<Line>();
        return list;
    }

    public int Unread(string key) => unread.TryGetValue(key ?? "", out var n) ? n : 0;

    public int UnreadTotal
    {
        get { int n = 0; foreach (var v in unread.Values) n += v; return n; }
    }

    public void MarkRead(string key)
    {
        if (unread.Remove(key ?? "")) Changed?.Invoke();
    }

    public Friend FindFriend(string id)
    {
        foreach (var f in FriendList) if (f.id == id) return f;
        return null;
    }

    public bool IsBlocked(string id) => FindFriend(id)?.state == Blocked;

    public string NameOf(string id)
    {
        var f = FindFriend(id);
        if (f != null) return f.name;
        var r = MatchSync.Instance != null ? MatchSync.Instance.FindByUser(id) : null;
        return r != null ? r.DisplayName : "ผู้เล่น";
    }

    private void Add(List<Line> list, Line line, string key)
    {
        list.Add(line);
        if (list.Count > Keep) list.RemoveRange(0, list.Count - Keep);
        if (!line.mine && Viewing != (key ?? ""))
            unread[key ?? ""] = Unread(key) + 1;
    }

    private void AddRoom(IApiChannelMessage m, bool quiet)
    {
        if (IsBlocked(m.SenderId)) return;
        RoomMessage body;
        try { body = JsonUtility.FromJson<RoomMessage>(m.Content); } catch (Exception) { return; }
        if (body == null || string.IsNullOrEmpty(body.text)) return;
        bool mine = m.SenderId == SelfId;
        var line = new Line { from = m.SenderId, name = body.name, text = body.text, mine = mine };
        if (quiet) { World.Add(line); return; }
        Add(World, line, "");
        // said by someone standing here: a bubble over their head too
        if (mine) { if (MatchSync.Instance?.LocalPlayer != null) SpeechBubble.Show(MatchSync.Instance.LocalPlayer, body.text); }
        else
        {
            var r = MatchSync.Instance != null ? MatchSync.Instance.FindByUser(m.SenderId) : null;
            if (r != null) SpeechBubble.Show(r.transform, body.text);
        }
    }

    private void OnRoomMessage(IApiChannelMessage m)
    {
        if (room == null || m.ChannelId != room.Id) return;
        AddRoom(m, quiet: false);
        Changed?.Invoke();
    }

    private void OnNotification(IApiNotification n)
    {
        Handle(n, quiet: false);
        if (n.Code == NoteChat || n.Code >= NoteProposal) _ = Forget(n.Id);
    }

    private async Task Forget(string id)
    {
        try { await connection.Client.DeleteNotificationsAsync(connection.Session, new[] { id }); } catch (Exception) { }
    }

    private void Handle(IApiNotification n, bool quiet)
    {
        var gold = new Color(1f, 0.85f, 0.4f);
        switch (n.Code)
        {
            case NoteChat:
                DirectMessage dm;
                try { dm = JsonUtility.FromJson<DirectMessage>(n.Content); } catch (Exception) { return; }
                if (dm == null || string.IsNullOrEmpty(dm.text) || IsBlocked(dm.from)) return;
                Add(Conversation(dm.from), new Line { from = dm.from, name = dm.name, text = dm.text }, dm.from);
                if (!quiet && Viewing != dm.from)
                {
                    UiKit.FloatAbovePlayer($"{dm.name}: {dm.text}", new Color(0.7f, 0.9f, 1f));
                    GameAudio.Play("door_chime");
                }
                break;
            case NoteFriendRequest:
            case NoteFriendAccepted:
                if (!quiet) UiKit.FloatAbovePlayer(n.Code == NoteFriendRequest ? "มีคนขอเป็นเพื่อน! (เมนู → เพื่อน)" : "เป็นเพื่อนกันแล้ว!", gold);
                _ = Quietly(RefreshFriendsAsync);
                break;
            case NoteProposal:
            case NoteAccepted:
            case NoteDeclined:
            case NoteDivorced:
                if (!quiet || n.Code == NoteProposal) UiKit.FloatAbovePlayer(n.Subject, n.Code == NoteAccepted ? new Color(1f, 0.6f, 0.8f) : gold);
                if (n.Code == NoteAccepted || n.Code == NoteProposal) GameAudio.Play("bell");
                _ = Quietly(RefreshMarriageAsync);
                if (n.Code != NoteProposal && WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
                break;
        }
        Changed?.Invoke();
    }

    private static async Task Quietly(Func<Task> work)
    {
        try { await work(); } catch (Exception) { }
    }

    // ── Talking ────────────────────────────────────────────────────────────────────────────
    public async Task SendAsync(string text, string toFriend)
    {
        var r = await connection.Client.RpcAsync(connection.Session, "chat_send",
            JsonUtility.ToJson(new SendRequest { text = text, to = toFriend ?? "" }));
        if (!string.IsNullOrEmpty(toFriend))
        {
            var sent = JsonUtility.FromJson<SendResult>(r.Payload);
            Add(Conversation(toFriend), new Line { from = SelfId, name = ProfileClient.Instance?.DisplayName, text = sent?.text ?? text, mine = true }, toFriend);
            Changed?.Invoke();
        }
        // a room message comes back through the room like everyone else's
    }

    public async Task ReportAsync(string userId, string text)
    {
        await connection.Client.RpcAsync(connection.Session, "chat_report", JsonUtility.ToJson(new ReportRequest { user_id = userId, text = text }));
    }

    // ── Friends (Nakama) ───────────────────────────────────────────────────────────────────
    public async Task RefreshFriendsAsync()
    {
        var list = new List<Friend>();
        string cursor = null;
        do
        {
            var page = await connection.Client.ListFriendsAsync(connection.Session, null, 100, cursor);
            foreach (var f in page.Friends)
            {
                string name = string.IsNullOrEmpty(f.User.DisplayName) ? f.User.Username : f.User.DisplayName;
                list.Add(new Friend { id = f.User.Id, name = name, state = f.State, online = f.User.Online });
            }
            cursor = page.Cursor;
        } while (!string.IsNullOrEmpty(cursor));
        FriendList.Clear();
        FriendList.AddRange(list);
        Changed?.Invoke();
    }

    // Asking, or saying yes to their request
    public async Task AddFriendAsync(string id)
    {
        await connection.Client.AddFriendsAsync(connection.Session, new[] { id });
        await RefreshFriendsAsync();
    }

    // Unfriend, say no to a request, take back our request, or unblock
    public async Task RemoveFriendAsync(string id)
    {
        await connection.Client.DeleteFriendsAsync(connection.Session, new[] { id });
        await RefreshFriendsAsync();
    }

    public async Task BlockAsync(string id)
    {
        await connection.Client.BlockFriendsAsync(connection.Session, new[] { id });
        World.RemoveAll(l => l.from == id);
        direct.Remove(id);
        unread.Remove(id);
        await RefreshFriendsAsync();
    }

    // ── Marriage ───────────────────────────────────────────────────────────────────────────
    public async Task<Marriage> RefreshMarriageAsync() => await Call("marry_get", "{}");
    public Task<Marriage> ProposeAsync(string to) => Call("marry_propose", JsonUtility.ToJson(new ProposeRequest { to = to }));
    public Task<Marriage> AnswerAsync(string from, bool accept) => Call("marry_answer", JsonUtility.ToJson(new AnswerRequest { from = from, accept = accept }));
    public Task<Marriage> DivorceAsync() => Call("marry_divorce", "{}");

    private async Task<Marriage> Call(string rpc, string payload)
    {
        var r = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        Wedding = JsonUtility.FromJson<Marriage>(r.Payload);
        if (rpc != "marry_get" && WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        Changed?.Invoke();
        return Wedding;
    }

    // A Thai message for a refusal from social.lua
    public static string Explain(Exception e)
    {
        string m = e.Message;
        return m.Contains("too fast") ? "พิมพ์เร็วไป รอสักครู่"
            : m.Contains("muted") ? "ถูกระงับการแชทชั่วคราว"
            : m.Contains("empty") ? "พิมพ์ข้อความก่อน"
            : m.Contains("not friends") ? "ต้องเป็นเพื่อนกันก่อน"
            : m.Contains("not enough baht") ? "เงินไม่พอซื้อแหวน"
            : m.Contains("they are married") ? "คนนี้แต่งงานไปแล้ว"
            : m.Contains("already married") ? "คุณแต่งงานอยู่แล้ว"
            : m.Contains("already proposed") ? "ขอแต่งงานไปแล้ว รอคำตอบก่อน"
            : m.Contains("no proposal") ? "คำขอนี้หมดไปแล้ว"
            : m.Contains("not married") ? "ยังไม่ได้แต่งงาน"
            : "ทำรายการไม่สำเร็จ";
    }
}
