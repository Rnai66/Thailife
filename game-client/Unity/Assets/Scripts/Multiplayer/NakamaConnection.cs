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

    // Raised on the main thread once the socket is open.
    public event Action<NakamaConnection> Connected;

    private string profile;
    public string Profile => profile ??= ResolveProfile();

    private async void Start()
    {
        if (connectOnStart)
            await ConnectAsync();
    }

    public async Task ConnectAsync()
    {
        if (config == null || !config.IsValid)
        {
            Debug.LogWarning("[Nakama] Missing NakamaConfig host/server key. Use Thai Life > Create Nakama Config.");
            return;
        }

        try
        {
            Client = new Nakama.Client(config.scheme, config.host, config.port, config.serverKey, UnityWebRequestAdapter.Instance);
            Session = await RestoreOrAuthenticateAsync();

            Socket = Client.NewSocket(useMainThread: true);
            await Socket.ConnectAsync(Session, appearOnline: true);

            Debug.Log($"[Nakama] Connected to {config.host} as user {Session.UserId} (profile '{Profile}')");
            Connected?.Invoke(this);
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"[Nakama] Server rejected request ({e.StatusCode}): {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Nakama] Connection failed: {e.Message}");
        }
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
