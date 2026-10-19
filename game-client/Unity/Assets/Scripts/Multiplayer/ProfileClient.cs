using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Character name (ชื่อตัวละคร) and look (หน้าตา, skin): the account's display name and avatar url
// ("skin:N"), set through profile.lua. Asks for a name the first time a player connects;
// other players' profiles are looked up by user id.
[RequireComponent(typeof(NakamaConnection))]
public class ProfileClient : MonoBehaviour
{
    [Serializable] private class NameRequest { public string name; }
    [Serializable] private class NameResult { public string name; }
    [Serializable] private class SkinRequest { public int skin; }

    public struct Info
    {
        public string Name;
        public int Skin;
    }

    public static ProfileClient Instance { get; private set; }

    public string DisplayName { get; private set; }
    public int Skin { get; private set; }
    public bool IsOnline => connection != null && connection.IsConnected;

    public event Action<string> NameChanged;
    public event Action<int> SkinChanged;

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
            Skin = ParseSkin(account.User.AvatarUrl);
            NameChanged?.Invoke(DisplayName);
            SkinChanged?.Invoke(Skin);
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
        cache.Remove(connection.Session.UserId);
        NameChanged?.Invoke(DisplayName);
        return DisplayName;
    }

    public async Task SetSkinAsync(int skin)
    {
        await connection.Client.RpcAsync(connection.Session, "profile_set_skin", JsonUtility.ToJson(new SkinRequest { skin = skin }));
        Skin = skin;
        cache.Remove(connection.Session.UserId);
        SkinChanged?.Invoke(Skin);
    }

    public static int ParseSkin(string avatarUrl) =>
        avatarUrl != null && avatarUrl.StartsWith("skin:") && int.TryParse(avatarUrl.Substring(5), out int n) ? n : 0;

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
                var info = new Info { Name = string.IsNullOrWhiteSpace(u.DisplayName) ? fallback : u.DisplayName, Skin = ParseSkin(u.AvatarUrl) };
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
