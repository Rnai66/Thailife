using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Multiplayer step 2: everyone joins one shared relayed match for the village and
// broadcasts their position ~10 times a second. Other players appear as RemotePlayer avatars.
// Relayed matches trust the client — fine for walking around, NOT for money/items (GDD section 9).
[RequireComponent(typeof(NakamaConnection))]
public class MatchSync : MonoBehaviour
{
    private const long OpPosition = 1;

    [SerializeField] private string matchName = "thaivillage";
    [SerializeField] private Transform localPlayer;
    [SerializeField] private float sendRate = 10f;
    [SerializeField] private float heartbeatInterval = 1f;

    private NakamaConnection connection;
    private IMatch match;
    private readonly Dictionary<string, RemotePlayer> remotes = new Dictionary<string, RemotePlayer>();
    private readonly byte[] sendBuffer = new byte[16];
    private readonly float[] floatBuffer = new float[4];

    private float nextSendTime;
    private float lastSentTime;
    private Vector3 lastSentPosition;
    private float lastSentYaw;

    private void Awake()
    {
        connection = GetComponent<NakamaConnection>();

        if (localPlayer == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) localPlayer = player.transform;
        }
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
            connection.Socket.ReceivedMatchState -= OnMatchState;
            connection.Socket.ReceivedMatchPresence -= OnMatchPresence;
        }
    }

    private async void OnConnected(NakamaConnection conn)
    {
        conn.Socket.ReceivedMatchState += OnMatchState;
        conn.Socket.ReceivedMatchPresence += OnMatchPresence;

        try
        {
            // A named match: everyone asking for the same name lands in the same match.
            match = await conn.Socket.CreateMatchAsync(matchName);
            Debug.Log($"[Match] Joined '{matchName}' ({match.Id}) — {CountOthers(match.Presences)} other player(s) here");

            foreach (var presence in match.Presences)
                AddRemote(presence);

            await SendPositionAsync(force: true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Match] Could not join '{matchName}': {e.Message}");
        }
    }

    private void Update()
    {
        if (match == null || localPlayer == null || !connection.IsConnected) return;
        if (Time.time < nextSendTime) return;

        nextSendTime = Time.time + 1f / sendRate;
        _ = SendPositionAsync(force: false);
    }

    private async Task SendPositionAsync(bool force)
    {
        if (match == null || localPlayer == null) return;

        Vector3 pos = localPlayer.position;
        float yaw = localPlayer.eulerAngles.y;

        bool moved = (pos - lastSentPosition).sqrMagnitude > 0.0001f || Mathf.Abs(Mathf.DeltaAngle(yaw, lastSentYaw)) > 1f;
        bool heartbeat = Time.time - lastSentTime >= heartbeatInterval;
        if (!force && !moved && !heartbeat) return;

        lastSentPosition = pos;
        lastSentYaw = yaw;
        lastSentTime = Time.time;

        floatBuffer[0] = pos.x;
        floatBuffer[1] = pos.y;
        floatBuffer[2] = pos.z;
        floatBuffer[3] = yaw;
        Buffer.BlockCopy(floatBuffer, 0, sendBuffer, 0, sendBuffer.Length);

        try
        {
            await connection.Socket.SendMatchStateAsync(match.Id, OpPosition, sendBuffer);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] Send failed: {e.Message}");
        }
    }

    private void OnMatchState(IMatchState state)
    {
        if (state.OpCode != OpPosition || state.State == null || state.State.Length < 16) return;
        if (!remotes.TryGetValue(state.UserPresence.SessionId, out var remote))
            remote = AddRemote(state.UserPresence);
        if (remote == null) return;

        Buffer.BlockCopy(state.State, 0, floatBuffer, 0, 16);
        remote.SetTarget(new Vector3(floatBuffer[0], floatBuffer[1], floatBuffer[2]), floatBuffer[3]);
    }

    private void OnMatchPresence(IMatchPresenceEvent e)
    {
        foreach (var join in e.Joins)
        {
            if (AddRemote(join) != null)
                _ = SendPositionAsync(force: true); // let the newcomer see us right away
        }

        foreach (var leave in e.Leaves)
        {
            if (remotes.TryGetValue(leave.SessionId, out var remote))
            {
                remotes.Remove(leave.SessionId);
                if (remote != null) Destroy(remote.gameObject);
                Debug.Log($"[Match] {leave.Username} left");
            }
        }
    }

    private RemotePlayer AddRemote(IUserPresence presence)
    {
        if (IsSelf(presence) || remotes.ContainsKey(presence.SessionId)) return null;

        var remote = RemotePlayer.Create(presence, localPlayer);
        remotes[presence.SessionId] = remote;
        Debug.Log($"[Match] {presence.Username} joined");
        return remote;
    }

    // Presence events can arrive before CreateMatchAsync returns (match.Self unknown yet),
    // so compare by user ID. Each profile is its own user, so this is unambiguous.
    private bool IsSelf(IUserPresence presence) =>
        connection.Session != null && presence.UserId == connection.Session.UserId;

    private int CountOthers(IEnumerable<IUserPresence> presences)
    {
        int n = 0;
        foreach (var p in presences)
            if (!IsSelf(p)) n++;
        return n;
    }

    private async void OnApplicationQuit()
    {
        if (match == null || !connection.IsConnected) return;
        try { await connection.Socket.LeaveMatchAsync(match.Id); }
        catch (Exception) { /* socket is closing anyway; the server drops our presence */ }
    }
}
