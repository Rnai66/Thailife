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
    private const long OpAction = 2;
    private const long OpHit = 3;     // attacker → victim only
    private const long OpHealth = 4;  // victim → everyone
    private const long OpChat = 5;    // one byte: QuickChat phrase index (no free text)
    private const long OpProfile = 6; // "my name/look changed, look me up again"

    // Sanity check on incoming hits: the attacker must be roughly within reach
    private const float MaxHitDistance = 3f;

    [Tooltip("Empty = use the scene name, so each location (village, tambon, …) has its own match")]
    [SerializeField] private string matchName = "";
    [SerializeField] private Transform localPlayer;
    [SerializeField] private float sendRate = 10f;
    [SerializeField] private float heartbeatInterval = 1f;

    public static MatchSync Instance { get; private set; }

    private NakamaConnection connection;
    private ProfileClient profile;
    private PlayerActions localActions;
    private PlayerCombat localCombat;
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
        if (string.IsNullOrWhiteSpace(matchName))
            matchName = gameObject.scene.name.ToLowerInvariant();

        if (localPlayer == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) localPlayer = player.transform;
        }

        Instance = this;
        profile = GetComponent<ProfileClient>();
        if (localPlayer != null)
        {
            localActions = localPlayer.GetComponent<PlayerActions>();
            localCombat = PlayerCombat.GetOrAdd(localPlayer.gameObject);
        }
    }

    private void OnEnable()
    {
        connection.Connected += OnConnected;
        if (localActions != null) localActions.Performed += OnLocalAction;
        if (localCombat != null) localCombat.HealthChanged += OnLocalHealthChanged;
        if (profile != null) { profile.NameChanged += OnLocalNameChanged; profile.LookChanged += OnLocalLookChanged; }
        if (connection.IsConnected) OnConnected(connection);
    }

    private void OnDisable()
    {
        connection.Connected -= OnConnected;
        if (localActions != null) localActions.Performed -= OnLocalAction;
        if (localCombat != null) localCombat.HealthChanged -= OnLocalHealthChanged;
        if (profile != null) { profile.NameChanged -= OnLocalNameChanged; profile.LookChanged -= OnLocalLookChanged; }
        if (connection.Socket != null)
        {
            connection.Socket.ReceivedMatchState -= OnMatchState;
            connection.Socket.ReceivedMatchPresence -= OnMatchPresence;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
            await SendHealthAsync(null);
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

    private async void OnLocalAction(PlayerAction action)
    {
        if (match == null || !connection.IsConnected) return;
        try
        {
            await connection.Socket.SendMatchStateAsync(match.Id, OpAction, new[] { (byte)action });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] Send action failed: {e.Message}");
        }
    }

    private void OnLocalHealthChanged(int health) => _ = SendHealthAsync(null);

    // presences == null broadcasts to everyone in the match
    private async Task SendHealthAsync(IUserPresence[] presences)
    {
        if (match == null || localCombat == null || !connection.IsConnected) return;
        try
        {
            await connection.Socket.SendMatchStateAsync(match.Id, OpHealth, new[] { (byte)localCombat.Health }, presences);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] Send health failed: {e.Message}");
        }
    }

    // Our punch/kick reached another player's avatar; tell only that player.
    public async void SendHit(RemotePlayer target, HitInfo hit)
    {
        if (match == null || !connection.IsConnected) return;
        try
        {
            await connection.Socket.SendMatchStateAsync(match.Id, OpHit, hit.ToBytes(), new[] { target.Presence });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] Send hit failed: {e.Message}");
        }
    }

    private void OnMatchState(IMatchState state)
    {
        if (state.State == null) return;
        if (!remotes.TryGetValue(state.UserPresence.SessionId, out var remote))
            remote = AddRemote(state.UserPresence);
        if (remote == null) return;

        if (state.OpCode == OpPosition && state.State.Length >= 16)
        {
            Buffer.BlockCopy(state.State, 0, floatBuffer, 0, 16);
            remote.SetTarget(new Vector3(floatBuffer[0], floatBuffer[1], floatBuffer[2]), floatBuffer[3]);
        }
        else if (state.OpCode == OpAction && state.State.Length >= 1)
        {
            remote.PlayAction((PlayerAction)state.State[0]);
        }
        else if (state.OpCode == OpChat && state.State.Length >= 1)
        {
            string phrase = QuickChat.Get(state.State[0]);
            if (phrase != null) SpeechBubble.Show(remote.transform, phrase);
        }
        else if (state.OpCode == OpProfile)
        {
            if (profile != null) profile.Forget(remote.UserId);
            LookUpName(remote);
        }
        else if (state.OpCode == OpHealth && state.State.Length >= 1)
        {
            remote.SetHealth(state.State[0]);
        }
        else if (state.OpCode == OpHit && localCombat != null && HitInfo.TryParse(state.State, out var hit))
        {
            bool inReach = Vector3.Distance(remote.transform.position, localPlayer.position) <= MaxHitDistance;
            if (inReach && !remote.IsKnockedOut)
                localCombat.ApplyHit(hit);
        }
    }

    private void OnMatchPresence(IMatchPresenceEvent e)
    {
        foreach (var join in e.Joins)
        {
            if (AddRemote(join) != null)
            {
                // Let the newcomer see us (and our health) right away
                _ = SendPositionAsync(force: true);
                _ = SendHealthAsync(new[] { join });
            }
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

        var remote = RemotePlayer.Create(presence, localPlayer, this);
        remotes[presence.SessionId] = remote;
        LookUpName(remote);
        Debug.Log($"[Match] {presence.Username} joined");
        return remote;
    }

    // Show the character name (stored server-side) instead of the random username
    private async void LookUpName(RemotePlayer remote)
    {
        if (profile == null) return;
        var info = await profile.GetInfoAsync(remote.UserId, remote.Presence.Username);
        if (remote == null) return;
        remote.SetDisplayName(info.Name);
        remote.SetLook(info.Look);
    }

    // Say a preset phrase: bubble over our head here and on everyone else's screen
    public void SendChat(int phrase)
    {
        if (localPlayer == null || QuickChat.Get(phrase) == null) return;
        SpeechBubble.Show(localPlayer, QuickChat.Get(phrase));
        _ = SendAsync(OpChat, new[] { (byte)phrase });
    }

    private void OnLocalNameChanged(string name) => _ = SendAsync(OpProfile, new byte[] { 1 });
    private void OnLocalLookChanged(Look look) => _ = SendAsync(OpProfile, new byte[] { 1 });

    private async Task SendAsync(long opCode, byte[] data)
    {
        if (match == null || !connection.IsConnected) return;
        try { await connection.Socket.SendMatchStateAsync(match.Id, opCode, data); }
        catch (Exception e) { Debug.LogWarning($"[Match] Send {opCode} failed: {e.Message}"); }
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
