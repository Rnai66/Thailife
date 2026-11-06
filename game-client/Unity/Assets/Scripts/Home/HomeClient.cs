using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Where you live (home.lua): the area (village / town / Bangkok) and the house there. A new player is
// asked once (HomeChoiceUI); after that each session starts at home: the game takes you to your
// area's map and puts you on your porch. Your house (HomePlot) becomes your walk-in room with your
// bed, shrine and furniture; the other houses are the neighbours'.
[RequireComponent(typeof(NakamaConnection))]
public class HomeClient : MonoBehaviour
{
    [Serializable]
    public class Style
    {
        public string id, name, desc;
    }

    [Serializable]
    public class Area
    {
        public string id, name, scene, desc;
        public Style[] styles;
    }

    [Serializable]
    public class State
    {
        public bool chosen;
        public string area, area_name, scene, style, style_name, done;
        public int move_fee, paid;
        public Area[] areas;
    }

    [Serializable] private class ChooseRequest { public string area, style; }

    public const string SpawnId = "home";

    public static HomeClient Instance { get; private set; }
    public State Current => cached; // kept across maps, so a new map knows your house at once
    private static State cached;
    public event Action<State> Changed;

    // The house you live in (until the server says, the village stilt house)
    public static string MyStyle => cached?.style ?? "stilt";

    private static bool sentHome; // once per session: start the game at home
    private NakamaConnection connection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { cached = null; sentHome = false; } // domain reload is off

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

    private void OnConnected(NakamaConnection conn) => _ = Refresh();

    private async Task Refresh()
    {
        try
        {
            await Call("home_get");
            StartAtHome();
        }
        catch (Exception e) { Debug.LogWarning($"[Home] {e.Message}"); }
    }

    public async Task<State> ChooseAsync(string area, string style)
    {
        var s = await Call("home_choose", JsonUtility.ToJson(new ChooseRequest { area = area, style = style }));
        GoHome();
        return s;
    }

    // First time online this session: not chosen yet → ask; otherwise go home
    private void StartAtHome()
    {
        if (sentHome || Current == null) return;
        sentHome = true;
        StartCoroutine(AfterTitle());
    }

    private System.Collections.IEnumerator AfterTitle()
    {
        while (TitleScreen.Up || SceneLoader.Loading) yield return null;
        if (!Current.chosen) HomeChoiceUI.Show(firstTime: true);
        else GoHome();
    }

    // To your own porch: travel there if home is another map
    public void GoHome()
    {
        if (Current == null || string.IsNullOrEmpty(Current.scene)) return;
        if (SceneManager.GetActiveScene().name != Current.scene)
        {
            Locations.PendingSpawnId = SpawnId;
            SceneLoader.Load(Current.scene, Current.style_name);
            return;
        }
        HomePlot.Mine()?.PutPlayerHere();
    }

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        cached = s;
        Changed?.Invoke(s);
        HomePlot.ApplyAll();
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message ?? "";
        if (m.Contains("not enough baht")) return "เงินไม่พอค่าย้ายบ้าน";
        if (m.Contains("same home")) return "อยู่บ้านนี้อยู่แล้ว";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }
}
