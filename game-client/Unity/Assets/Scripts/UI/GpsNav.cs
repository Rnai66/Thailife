using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// GPS นำทาง for your own car or bike (DrivableCar): while you drive, a "นำทาง" button by the minimap
// opens the town map (RideMapUI) to pick where to go. The way along the roads (TrafficNet) is then
// drawn on the minimap, and a panel at the top says the next turn (เลี้ยวซ้าย / เลี้ยวขวา / กลับรถ)
// and how far, with how far it is still to go and about how long. Miss a turn and it works out a new
// way from where you are; get there and it says so and stops. Getting out or leaving the map ends it.
public class GpsNav : MonoBehaviour
{
    private const float OffRoute = 14f;      // metres off the line before it's a wrong turn
    private const float RerouteAfter = 1.2f; // seconds off it before working out a new way
    private const float Arrive = 12f;        // metres from the end: there
    private const float TurnAngle = 40f;     // a bend sharper than this is a turn to call out
    private const float Window = 10f;        // metres either side of a point to judge a bend
    private const float LookAhead = 800f;    // how far ahead to look for the next turn
    private const float AverageSpeed = 9f;   // m/s, for the time left (lights and all)

    private static GpsNav instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GpsNav");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GpsNav>();
    }

    // the HUD
    private GameObject canvasGo, button, panel;
    private Text buttonLabel, turnText, infoText;
    private RectTransform arrow;

    // the trip
    private List<Vector3> line;
    private float[] along;        // metres from the start to each point of line
    private readonly List<Vector3> ahead = new List<Vector3>();
    private Vector3 dest;
    private string destName;
    private int progress;
    private float offSince = -1f, nextDraw, announcedAt = -1f;
    private Scene scene;
    private bool roads;
    private DrivableCar lastCar;

    public static bool Guiding => instance != null && instance.line != null;

    private void Update()
    {
        var car = DrivableCar.Current;
        var active = SceneManager.GetActiveScene();
        if (active != scene)
        {
            scene = active;
            if (line != null) Stop(null);
            lastCar = null;
        }
        if (car == null)
        {
            if (line != null) Stop(null);
            if (canvasGo != null && canvasGo.activeSelf) canvasGo.SetActive(false);
            lastCar = null;
            return;
        }
        if (car != lastCar || (!roads && Time.frameCount % 90 == 0)) // (the town's traffic may sign its roads in late)
        {
            lastCar = car;
            TrafficNet.EnsureBuilt();
            roads = TrafficNet.Count > 0;
        }
        if (!roads) { if (canvasGo != null) canvasGo.SetActive(false); return; }

        Build();
        if (!canvasGo.activeSelf) canvasGo.SetActive(true);
        bool busy = RideMapUI.IsOpen || UiKit.AnyWindowOpen;
        if (button.activeSelf == busy) button.SetActive(!busy);
        buttonLabel.text = line != null ? "GPS" : "นำทาง";
        if (line == null) { if (panel.activeSelf) panel.SetActive(false); return; }
        if (!panel.activeSelf) panel.SetActive(true);
        Follow(car.transform);
    }

    // ── The trip ─────────────────────────────────────────────────────────────────────────────────
    private void OpenMap()
    {
        var car = DrivableCar.Current;
        if (car == null) return;
        var t = car.transform;
        List<Vector3> following = null;
        if (line != null)
        {
            following = new List<Vector3> { t.position };
            for (int i = progress + 1; i < line.Count; i++) following.Add(line[i]);
        }
        RideMapUI.OpenGps(t, spot => TrafficNet.RouteFrom(t.position, t.forward, spot), following, Begin,
            () => { if (line != null) Stop("เลิกนำทางแล้ว"); });
    }

    private void Begin(Vector3 spot, string name)
    {
        var car = DrivableCar.Current;
        if (car == null) return;
        var way = TrafficNet.RouteFrom(car.transform.position, car.transform.forward, spot);
        if (way == null || way.Count < 2)
        {
            ScreenToast.Show("ถนนไปไม่ถึงตรงนั้น ลองเลือกที่อื่น", UiKit.Danger);
            return;
        }
        dest = way[way.Count - 1];
        destName = string.IsNullOrEmpty(name) ? "ที่หมาย" : name;
        SetLine(way);
        GameAudio.Play("pickup");
        ScreenToast.Show($"เริ่มนำทางไป {destName}", UiKit.PayGreen);
    }

    private void SetLine(List<Vector3> way)
    {
        line = way;
        along = new float[way.Count];
        for (int i = 1; i < way.Count; i++) along[i] = along[i - 1] + Flat(way[i] - way[i - 1]);
        progress = 0;
        offSince = -1f;
        announcedAt = -1f;
        nextDraw = 0f;
    }

    private void Stop(string message)
    {
        line = null;
        along = null;
        MiniMap.ClearRoute();
        if (panel != null) panel.SetActive(false);
        if (message != null) ScreenToast.Show(message, UiKit.Gold);
    }

    private void Follow(Transform car)
    {
        Vector3 pos = car.position;

        // where on the line the car is now (searching a little way back and on from last time)
        int from = Mathf.Max(0, progress - 3), to = Mathf.Min(line.Count - 1, progress + 80);
        float best = float.MaxValue, bestAlong = along[progress];
        int bestIndex = progress;
        for (int i = from; i < to; i++)
        {
            Vector3 a = line[i], b = line[i + 1];
            a.y = b.y = pos.y;
            float seg = (b - a).magnitude;
            float t = seg > 0f ? Mathf.Clamp01(Vector3.Dot(pos - a, b - a) / (seg * seg)) : 0f;
            float d = (Vector3.Lerp(a, b, t) - pos).magnitude;
            if (d < best) { best = d; bestIndex = i; bestAlong = along[i] + t * seg; }
        }

        // off the way: work out a new one from here
        if (best > OffRoute)
        {
            if (offSince < 0f) offSince = Time.time;
            if (Time.time - offSince > RerouteAfter)
            {
                var way = TrafficNet.RouteFrom(pos, car.forward, dest);
                if (way != null && way.Count >= 2)
                {
                    SetLine(way);
                    GameAudio.Play("pickup", null, 0.4f);
                    ScreenToast.Show("คำนวณเส้นทางใหม่แล้ว", UiKit.Gold);
                    return;
                }
                offSince = Time.time; // try again in a moment
            }
            turnText.text = "ออกนอกเส้นทาง";
        }
        else offSince = -1f;
        progress = bestIndex;

        float total = along[along.Length - 1];
        float left = Mathf.Max(0f, total - bestAlong);
        if (left < Arrive || (Flat(pos - dest) < Arrive && left < 60f))
        {
            GameAudio.Play("bell");
            UiKit.FloatAbovePlayer($"ถึง{destName}แล้ว", UiKit.GoldText);
            Stop($"ถึงที่หมายแล้ว · {destName}");
            return;
        }

        // the next turn
        if (offSince < 0f)
        {
            float turnAt = -1f, angle = 0f;
            for (float s = bestAlong + 4f; s < Mathf.Min(total - 2f, bestAlong + LookAhead); s += 3f)
            {
                Vector3 back = At(s) - At(s - Window), on = At(s + Window) - At(s);
                back.y = on.y = 0f;
                if (back.sqrMagnitude < 1f || on.sqrMagnitude < 1f) continue;
                float a = Vector3.SignedAngle(back, on, Vector3.up);
                if (Mathf.Abs(a) > TurnAngle) { turnAt = s; angle = a; break; }
            }
            if (turnAt < 0f)
            {
                turnText.text = $"ตรงไป {Round(left)}";
                Point(0f);
            }
            else
            {
                float until = turnAt - bestAlong;
                string what = Mathf.Abs(angle) > 140f ? "กลับรถ" : angle > 0f ? "เลี้ยวขวา" : "เลี้ยวซ้าย";
                turnText.text = until < 15f ? $"{what} ตรงนี้" : $"{what} อีก {Round(until)}";
                Point(Mathf.Abs(angle) > 140f ? 180f : angle > 0f ? -90f : 90f);
                // a chime as a turn comes up
                if (until < 60f && Mathf.Abs(announcedAt - turnAt) > 20f)
                {
                    announcedAt = turnAt;
                    GameAudio.Play("door_chime", null, 0.5f);
                }
            }
        }
        float minutes = left / AverageSpeed / 60f;
        infoText.text = $"{destName} · อีก {Round(left)} · " + (minutes < 1f ? "ไม่ถึง 1 นาที" : $"ราว {Mathf.CeilToInt(minutes)} นาที");

        // the way ahead on the minimap, a few times a second
        if (Time.time >= nextDraw)
        {
            nextDraw = Time.time + 0.1f;
            ahead.Clear();
            ahead.Add(pos);
            for (int i = progress + 1; i < line.Count; i++) ahead.Add(line[i]);
            MiniMap.ShowRoute(ahead, dest);
        }
    }

    // The point s metres along the line
    private Vector3 At(float s)
    {
        if (s <= 0f) return line[0];
        int n = along.Length;
        if (s >= along[n - 1]) return line[n - 1];
        int lo = 0, hi = n - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (along[mid] <= s) lo = mid; else hi = mid;
        }
        float seg = along[hi] - along[lo];
        return Vector3.Lerp(line[lo], line[hi], seg > 0f ? (s - along[lo]) / seg : 0f);
    }

    private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    private static string Round(float metres) =>
        metres >= 1000f ? $"{metres / 1000f:0.0} กม." : $"{Mathf.Max(10, Mathf.RoundToInt(metres / 10f) * 10)} ม.";

    private void Point(float degrees) => arrow.localRotation = Quaternion.Euler(0f, 0f, degrees);

    // ── The HUD ──────────────────────────────────────────────────────────────────────────────────
    private void Build()
    {
        if (canvasGo != null) return;
        canvasGo = new GameObject("GpsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // the round button just left of the minimap
        var b = UiKit.RoundButton(canvasGo.transform, "", Vector2.zero, new Vector2(124f, 124f), UiKit.Teak, UiKit.HeaderText, OpenMap, 22, circle: true);
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-385f, -24f);
        rt.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.IconTile(rt, "travel", new Vector2(0f, 12f), new Vector2(62f, 62f)).GetComponent<Image>().raycastTarget = false;
        buttonLabel = b.GetComponentInChildren<Text>();
        buttonLabel.rectTransform.anchoredPosition = new Vector2(0f, -38f);
        buttonLabel.rectTransform.sizeDelta = new Vector2(120f, 30f);
        buttonLabel.fontSize = 22;
        button = b.gameObject;

        // the guidance panel at the top
        var p = UiKit.Panel("Guidance", canvasGo.transform, new Color(0.14f, 0.08f, 0.05f, 0.92f), Vector2.zero, new Vector2(660f, 124f), shadow: true);
        p.anchorMin = p.anchorMax = new Vector2(0.5f, 1f);
        p.pivot = new Vector2(0.5f, 1f);
        p.anchoredPosition = new Vector2(0f, -14f);
        p.GetComponent<Image>().raycastTarget = false;
        p.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        var disc = UiKit.Panel("ArrowDisc", p, new Color32(0x1E, 0x6F, 0xE0, 0xFF), new Vector2(-260f, 0f), new Vector2(96f, 96f));
        disc.GetComponent<Image>().sprite = UiKit.CircleSprite;
        disc.GetComponent<Image>().raycastTarget = false;
        arrow = new GameObject("Arrow", typeof(RectTransform)).GetComponent<RectTransform>();
        arrow.SetParent(disc, false);
        var shaft = UiKit.Rect("Shaft", arrow, Color.white);
        shaft.sizeDelta = new Vector2(14f, 40f);
        shaft.anchoredPosition = new Vector2(0f, -10f);
        shaft.GetComponent<Image>().raycastTarget = false;
        var head = UiKit.Rect("Head", arrow, Color.white);
        head.sizeDelta = new Vector2(30f, 30f);
        head.anchoredPosition = new Vector2(0f, 12f);
        head.localRotation = Quaternion.Euler(0f, 0f, 45f);
        head.GetComponent<Image>().raycastTarget = false;
        var cover = UiKit.Rect("Cover", arrow, new Color32(0x1E, 0x6F, 0xE0, 0xFF)); // squares the head off into a point
        cover.sizeDelta = new Vector2(44f, 22f);
        cover.anchoredPosition = new Vector2(0f, -2f);
        cover.GetComponent<Image>().raycastTarget = false;
        shaft.SetAsLastSibling();
        turnText = UiKit.Cartoon(UiKit.Label(p, 38, "", new Vector2(56f, 22f), new Vector2(520f, 56f), UiKit.GoldText, TextAnchor.MiddleLeft), 2f);
        infoText = UiKit.Label(p, 23, "", new Vector2(56f, -32f), new Vector2(520f, 40f), UiKit.HeaderText, TextAnchor.MiddleLeft);
        infoText.resizeTextForBestFit = true;
        infoText.resizeTextMinSize = 16;
        infoText.resizeTextMaxSize = 23;
        panel = p.gameObject;
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        MiniMap.ClearRoute();
    }
}
