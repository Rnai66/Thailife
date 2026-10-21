using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Character name (ชื่อตัวละคร) and look (หน้าตา: body, hairstyle, clothes): the account's display name
// and avatar url ("look:g.f.h.c.t.b"), set through profile.lua. The first time a player connects it
// asks for a name, then opens the character creator; other players' profiles are looked up by user id.
[RequireComponent(typeof(NakamaConnection))]
public class ProfileClient : MonoBehaviour
{
    [Serializable] private class NameRequest { public string name; }
    [Serializable] private class NameResult { public string name; }
    [Serializable] private class LookRequest { public string code; public string place; }
    [Serializable] public class LookResult { public string code; public bool created; public int paid; public int level; }

    public struct Info
    {
        public string Name;
        public Look Look;
    }

    public static ProfileClient Instance { get; private set; }

    public string DisplayName { get; private set; }
    public Look Look { get; private set; }
    public bool LookCreated { get; private set; } = true;
    public bool IsOnline => connection != null && connection.IsConnected;

    public event Action<string> NameChanged;
    public event Action<Look> LookChanged;

    private NakamaConnection connection;
    private readonly Dictionary<string, Info> cache = new Dictionary<string, Info>();

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

    private void OnDisable() => connection.Connected -= OnConnected;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async void OnConnected(NakamaConnection conn)
    {
        try
        {
            var account = await conn.Client.GetAccountAsync(conn.Session);
            DisplayName = account.User.DisplayName;
            Look = Look.Parse(account.User.AvatarUrl);
            NameChanged?.Invoke(DisplayName);
            LookChanged?.Invoke(Look);
            var state = await GetLookStateAsync();
            LookCreated = state == null || state.created;
            if (string.IsNullOrWhiteSpace(DisplayName))
                NameUI.Show(firstTime: true);   // the character creator follows the name
            else if (!LookCreated)
                LookUI.Show(LookUI.Mode.Create);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] Could not load account: {e.Message}");
        }
    }

    // Throws ApiResponseException if the server rejects the name (length etc.)
    public async Task<string> SetNameAsync(string name)
    {
        var result = await connection.Client.RpcAsync(connection.Session, "profile_set_name", JsonUtility.ToJson(new NameRequest { name = name }));
        DisplayName = JsonUtility.FromJson<NameResult>(result.Payload).name;
        cache.Remove(connection.Session.UserId);
        NameChanged?.Invoke(DisplayName);
        return DisplayName;
    }

    // Save a new look: "create" (first time, free options), "barber" (hair) or "tailor" (clothes).
    // Throws ApiResponseException if the server refuses (price, level, wrong shop).
    public async Task<LookResult> SetLookAsync(Look look, string place)
    {
        var result = await connection.Client.RpcAsync(connection.Session, "profile_set_look",
            JsonUtility.ToJson(new LookRequest { code = look.Code, place = place }));
        var r = JsonUtility.FromJson<LookResult>(result.Payload);
        Look = Look.Parse("look:" + r.code);
        LookCreated = true;
        cache.Remove(connection.Session.UserId);
        LookChanged?.Invoke(Look);
        return r;
    }

    public async Task<LookResult> GetLookStateAsync()
    {
        try
        {
            var result = await connection.Client.RpcAsync(connection.Session, "profile_get_look", "{}");
            return JsonUtility.FromJson<LookResult>(result.Payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] Could not load look: {e.Message}");
            return null;
        }
    }

    // Another player changed their name or look: look them up again next time
    public void Forget(string userId) => cache.Remove(userId);

    // Another player's character name and look (name falls back to their username until they pick one)
    public async Task<Info> GetInfoAsync(string userId, string fallback)
    {
        if (cache.TryGetValue(userId, out var cached)) return cached;
        try
        {
            var users = await connection.Client.GetUsersAsync(connection.Session, new[] { userId });
            foreach (var u in users.Users)
            {
                var info = new Info { Name = string.IsNullOrWhiteSpace(u.DisplayName) ? fallback : u.DisplayName, Look = Look.Parse(u.AvatarUrl) };
                cache[userId] = info;
                return info;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] Could not look up {userId}: {e.Message}");
        }
        return new Info { Name = fallback };
    }
}
