using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Character name (ชื่อตัวละคร): the account's display name, set through profile.lua.
// Asks for a name the first time a player connects; other players' names are looked up by user id.
[RequireComponent(typeof(NakamaConnection))]
public class ProfileClient : MonoBehaviour
{
    [Serializable] private class NameRequest { public string name; }
    [Serializable] private class NameResult { public string name; }

    public static ProfileClient Instance { get; private set; }

    public string DisplayName { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected;

    public event Action<string> NameChanged;

    private NakamaConnection connection;
    private readonly Dictionary<string, string> nameCache = new Dictionary<string, string>();

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
            NameChanged?.Invoke(DisplayName);
            if (string.IsNullOrWhiteSpace(DisplayName))
                NameUI.Show(firstTime: true);
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
        nameCache[connection.Session.UserId] = DisplayName;
        NameChanged?.Invoke(DisplayName);
        return DisplayName;
    }

    // Another player's character name (falls back to their username until they pick one)
    public async Task<string> GetNameAsync(string userId, string fallback)
    {
        if (nameCache.TryGetValue(userId, out var cached)) return cached;
        try
        {
            var users = await connection.Client.GetUsersAsync(connection.Session, new[] { userId });
            foreach (var u in users.Users)
            {
                string name = string.IsNullOrWhiteSpace(u.DisplayName) ? fallback : u.DisplayName;
                nameCache[userId] = name;
                return name;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] Could not look up {userId}: {e.Message}");
        }
        return fallback;
    }
}
