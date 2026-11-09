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
        public string area, area_name, scene, style, style_name, done, moo_name, address, notice;
        public int move_fee, paid, moo, lot;
        public Area[] areas;
    }

    [Serializable] private class ChooseRequest { public string area, style; }
    [Serializable] private class ChooseLotRequest { public string area, style; public int moo, lot; }
    [Serializable] private class LotsRequest { public string area; public int moo; }

    // lots_get: one หมู่'s lots and how full each หมู่ is
    [Serializable] public class LotEntry { public int no; public bool vacant, mine; public string owner, owner_id, style; }
    [Serializable] public class MooEntry { public int no, used, total; public string name; }
    [Serializable] public class LotsView
    {
        public string area, moo_name;
        public int moo, lot_count, idle_days;
        public LotEntry[] lots;
        public MooEntry[] moos;
    }

    public const string SpawnId = "home";
    public const string WakeId = "wake"; // the session start: wake up in bed at home

    // Set once the session start has decided where you begin (SceneLoader.Enter waits for it)
    public static bool StartDecided { get; private set; }

    public static HomeClient Instance { get; private set; }
    public State Current => cached; // kept across maps, so a new map knows your house at once
    private static State cached;
    public event Action<State> Changed;
    public static event Action<State> StateChanged; // for things in the map (HouseLots), which come and go

    // The house you live in (until the server says, the village stilt house)
    public static string MyStyle => cached?.style ?? "stilt";
    // Your house lot (บ้านเลขที่ lot หมู่ที่ moo) — 0 until the server has given you one
    public static int MyLot => cached != null && cached.chosen ? cached.lot : 0;
    public static int MyMoo => cached != null && cached.chosen ? cached.moo : 0;
    public static bool HasLot => MyLot > 0;
    public static bool LivesIn(string area) => HasLot && cached.area == area;
    // Which หมู่ of an area you see (and play with): your own there, else หมู่ที่ 1
    // (in the village without a home there: the หมู่ your farm lot is in, FarmLots)
    public static int MooIn(string area) => LivesIn(area) && cached.moo > 0 ? cached.moo
        : area == "village" && FarmLots.MyMoo > 0 ? FarmLots.MyMoo : 1;
    public bool IsOnline => connection != null && connection.IsConnected;

    private static bool sentHome; // once per session: start the game at home
    private NakamaConnection connection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { cached = null; sentHome = false; StartDecided = false; } // domain reload is off

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

    // A chosen lot (บ้านเลขที่ lot หมู่ที่ moo); fails with "lot taken" if someone got there first
    public async Task<State> ChooseLotAsync(string area, string style, int moo, int lot)
    {
        var s = await Call("home_choose", JsonUtility.ToJson(new ChooseLotRequest { area = area, style = style, moo = moo, lot = lot }));
        GoHome();
        return s;
    }

    public async Task<LotsView> LotsAsync(string area, int moo)
    {
        var result = await connection.Client.RpcAsync(connection.Session, "lots_get", JsonUtility.ToJson(new LotsRequest { area = area, moo = moo }));
        return JsonUtility.FromJson<LotsView>(result.Payload);
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
        while (TitleScreen.Up || (SceneLoader.Loading && !SceneLoader.Entering)) yield return null;
        if (!Current.chosen) HomeChoiceUI.Show(firstTime: true);
        else GoHome(wake: true);
        StartDecided = true;
    }

    // To your own porch: travel there if home is another map
    public void GoHome(bool wake = false)
    {
        if (Current == null || string.IsNullOrEmpty(Current.scene)) return;
        if (SceneManager.GetActiveScene().name != Current.scene)
        {
            Locations.PendingSpawnId = wake ? WakeId : SpawnId;
            SceneLoader.Load(Current.scene, Current.style_name);
            return;
        }
        var plot = HomePlot.Mine();
        if (plot == null) { Locations.PendingSpawnId = wake ? WakeId : SpawnId; return; } // your house is still on its way (HouseLots)
        if (wake) plot.WakeUp();
        else plot.PutPlayerHere();
    }

    private async Task<State> Call(string rpc, string payload = "{}")
    {
        var result = await connection.Client.RpcAsync(connection.Session, rpc, payload);
        var s = JsonUtility.FromJson<State>(result.Payload);
        cached = s;
        Changed?.Invoke(s);
        StateChanged?.Invoke(s);
        HomePlot.ApplyAll();
        if (!string.IsNullOrEmpty(s.notice)) UiKit.FloatAbovePlayer(s.notice, new Color(1f, 0.8f, 0.4f));
        return s;
    }

    public static string Explain(Exception e)
    {
        string m = e.Message ?? "";
        if (m.Contains("not enough baht")) return "เงินไม่พอค่าย้ายบ้าน";
        if (m.Contains("same home")) return "อยู่บ้านนี้อยู่แล้ว";
        if (m.Contains("lot taken")) return "แปลงนี้มีคนจองไปแล้ว เลือกแปลงอื่น";
        if (m.Contains("area full")) return "ย่านนี้เต็มทุกหมู่แล้ว";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }
}
