using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// Step 1 of multiplayer: log in anonymously with the device ID and open a realtime socket.
// Position sync comes later, once this connects reliably.
public class NakamaConnection : MonoBehaviour
{
    private const string DeviceIdKey = "nakama.deviceId";
    private const string SessionTokenKey = "nakama.authToken";
    private const string RefreshTokenKey = "nakama.refreshToken";

    [SerializeField] private NakamaConfig config;
    [SerializeField] private bool connectOnStart = true;

    public IClient Client { get; private set; }
    public ISession Session { get; private set; }
    public ISocket Socket { get; private set; }
    public bool IsConnected => Socket != null && Socket.IsConnected;

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

            Debug.Log($"[Nakama] Connected to {config.host} as user {Session.UserId}");
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
                return restored;
        }

        var session = await Client.AuthenticateDeviceAsync(GetDeviceId(), create: true);
        PlayerPrefs.SetString(SessionTokenKey, session.AuthToken);
        PlayerPrefs.SetString(RefreshTokenKey, session.RefreshToken);
        PlayerPrefs.Save();
        return session;
    }

    private static string GetDeviceId()
    {
        string id = PlayerPrefs.GetString(DeviceIdKey, "");
        if (!string.IsNullOrEmpty(id)) return id;

        id = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(id) || id == SystemInfo.unsupportedIdentifier)
            id = Guid.NewGuid().ToString();

        PlayerPrefs.SetString(DeviceIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    private async void OnApplicationQuit()
    {
        if (Socket != null)
            await Socket.CloseAsync();
    }
}
