using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Logs in anonymously with the device ID and opens a realtime socket.
// Each "profile" is a separate account, so the Editor and a local build can play together:
// the Editor uses profile "editor"; a build can be started with  -profile <name>.
public class NakamaConnection : MonoBehaviour
{
    private string DeviceIdKey => $"nakama.{Profile}.deviceId";
    private string SessionTokenKey => $"nakama.{Profile}.authToken";
    private string RefreshTokenKey => $"nakama.{Profile}.refreshToken";

    [SerializeField] private NakamaConfig config;
    [SerializeField] private bool connectOnStart = true;

    public IClient Client { get; private set; }
    public ISession Session { get; private set; }
    public ISocket Socket { get; private set; }
    public bool IsConnected => Socket != null && Socket.IsConnected;

    // Why the last attempt failed (shown on the HUD so players and testers can see it)
    public string LastError { get; private set; }

    private const float RetrySeconds = 10f;

    // Raised on the main thread once the socket is open.
    public event Action<NakamaConnection> Connected;

    private string profile;
    public string Profile => profile ??= ResolveProfile();

    private async void Start()
    {
        if (!connectOnStart) return;

        // Keep trying while offline (e.g. right after allowing Local Network access on iPhone)
        while (this != null && isActiveAndEnabled && !IsConnected)
        {
            await ConnectAsync();
            if (IsConnected || this == null) break;
            await Task.Delay(TimeSpan.FromSeconds(RetrySeconds));
        }
    }

    public async Task ConnectAsync()
    {
        if (config == null || !config.IsValid)
        {
            Debug.LogWarning("[Nakama] Missing NakamaConfig host/server key. Use Thai Life > Create Nakama Config.");
            return;
        }

        // Try the main host, then each fallback. On iPhone the WebSocket hangs on ".local" names
        // (HTTP login works, the socket never opens), so a .local name is resolved to its IPv4
        // address first and that address is tried before the name itself.
        var hosts = new System.Collections.Generic.List<string>();
        if (config.host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            string ip = await ResolveIPv4Async(config.host);
            if (ip != null) hosts.Add(ip);
        }
        hosts.Add(config.host);
        if (config.fallbackHosts != null)
            foreach (var h in config.fallbackHosts)
                if (!string.IsNullOrWhiteSpace(h) && !hosts.Contains(h)) hosts.Add(h);

        for (int i = 0; i < hosts.Count; i++)
        {
            bool last = i == hosts.Count - 1;
            Debug.Log($"[Nakama] Trying {config.scheme}://{hosts[i]}:{config.port} ...");
            try
            {
                Client = new Nakama.Client(config.scheme, hosts[i], config.port, config.serverKey, UnityWebRequestAdapter.Instance)
                {
                    Timeout = last ? 15 : 5, // probe quickly before falling back
                };
                Session = await RestoreOrAuthenticateAsync();
                Debug.Log($"[Nakama] Logged in via {hosts[i]}, opening socket ...");

                Socket = Client.NewSocket(useMainThread: true);
                int timeout = last ? 20 : 6;
                var connect = Socket.ConnectAsync(Session, appearOnline: true, connectTimeout: timeout);
                // The SDK's own timeout didn't fire on iOS, so enforce one here
                if (await Task.WhenAny(connect, Task.Delay(TimeSpan.FromSeconds(timeout + 1))) != connect)
                {
                    _ = Socket.CloseAsync();
                    Socket = null;
                    throw new TimeoutException($"socket did not open within {timeout}s");
                }
                await connect;

                Debug.Log($"[Nakama] Connected to {hosts[i]} as user {Session.UserId} (profile '{Profile}')");
                LastError = null;
                Connected?.Invoke(this);
                return;
            }
            catch (ApiResponseException e)
            {
                // The server answered, so the host is right; the request itself was refused
                Debug.LogError($"[Nakama] Server rejected request ({e.StatusCode}): {e.Message}");
                LastError = $"เซิร์ฟเวอร์ปฏิเสธ ({e.StatusCode}): {e.Message}";
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nakama] Could not reach {hosts[i]}: {e.Message}");
                LastError = $"ต่อ {hosts[i]} ไม่ได้: {e.GetType().Name} {e.Message}";
            }
        }
        Debug.LogError($"[Nakama] Connection failed: no server reachable ({string.Join(", ", hosts)})");
    }

    // IPv4 address for a host name (null if it can't be resolved within 3 seconds)
    private static async Task<string> ResolveIPv4Async(string host)
    {
        try
        {
            var lookup = System.Net.Dns.GetHostAddressesAsync(host);
            if (await Task.WhenAny(lookup, Task.Delay(3000)) != lookup) return null;
            // Prefer the Wi-Fi/LAN address; 169.254.x.x is the USB cable link, gone when unplugged
            string linkLocal = null;
            foreach (var address in await lookup)
            {
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || System.Net.IPAddress.IsLoopback(address)) continue;
                string ip = address.ToString();
                if (ip.StartsWith("169.254.")) { linkLocal ??= ip; continue; }
                return ip;
            }
            return linkLocal;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Nakama] Could not resolve {host}: {e.Message}");
        }
        return null;
    }

    private async Task<ISession> RestoreOrAuthenticateAsync()
    {
        string token = PlayerPrefs.GetString(SessionTokenKey, "");
        string refresh = PlayerPrefs.GetString(RefreshTokenKey, "");

        if (!string.IsNullOrEmpty(token))
        {
            var restored = Nakama.Session.Restore(token, refresh);
            if (!restored.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            {
                try
                {
                    await Client.GetAccountAsync(restored); // token still accepted by this server?
                    return restored;
                }
                catch (ApiResponseException)
                {
                    Debug.Log("[Nakama] Saved session rejected (server keys changed?) — logging in again.");
                }
            }
        }

        var session = await Client.AuthenticateDeviceAsync(GetDeviceId(), create: true);
        PlayerPrefs.SetString(SessionTokenKey, session.AuthToken);
        PlayerPrefs.SetString(RefreshTokenKey, session.RefreshToken);
        PlayerPrefs.Save();
        return session;
    }

    // Moving to this phone from another one: log in with the recovery code (account.lua links it as a
    // custom ID), link a fresh device ID for this phone to that account, then reload the scene so
    // every client loads the recovered account.
    public async Task RecoverAsync(string code)
    {
        string normalized = code.Trim().ToUpperInvariant().Replace(" ", "");
        var session = await Client.AuthenticateCustomAsync("recover-" + normalized, create: false);
        string newId = Guid.NewGuid().ToString();
        await Client.LinkDeviceAsync(session, newId);
        PlayerPrefs.SetString(DeviceIdKey, newId);
        PlayerPrefs.SetString(SessionTokenKey, session.AuthToken);
        PlayerPrefs.SetString(RefreshTokenKey, session.RefreshToken);
        PlayerPrefs.Save();
        Debug.Log($"[Nakama] Account recovered: now user {session.UserId}");
        if (Socket != null) await Socket.CloseAsync();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private string GetDeviceId()
    {
        string id = PlayerPrefs.GetString(DeviceIdKey, "");
        if (!string.IsNullOrEmpty(id)) return id;

        // Only the default profile on a real device uses the hardware ID; others get their own.
        id = Profile == "default" ? SystemInfo.deviceUniqueIdentifier : "";
        if (string.IsNullOrEmpty(id) || id == SystemInfo.unsupportedIdentifier)
            id = Guid.NewGuid().ToString();

        PlayerPrefs.SetString(DeviceIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    private static string ResolveProfile()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-profile") return args[i + 1];

        return Application.isEditor ? "editor" : "default";
    }

    // Covers quitting and travelling to another scene (each scene has its own connection)
    private async void OnDestroy()
    {
        if (Socket == null) return;
        try { await Socket.CloseAsync(); }
        catch (Exception) { /* already closed */ }
    }
}
