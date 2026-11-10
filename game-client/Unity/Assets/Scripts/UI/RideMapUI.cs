using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// แผนที่เลือกที่หมาย (riding a tuk-tuk or taxi, TukTukHail): a clean drawn map of the town — the roads
// the traffic drives (TrafficNet), the rivers and the places worth going to (MapPlaces), the car you're
// in — and beside it the list of those places, nearest first. Tap a place in the list or on the map, or
// any spot (the pin goes to the nearest road): the way the driver would go is drawn in blue with how
// far it is along the roads and about how long. "ไปที่นี่" sends the driver (TrafficCar.GoTo),
// "เที่ยวรอบเมือง" just rides round. While riding, a "แผนที่" button at the side opens it again.
public class RideMapUI : MonoBehaviour
{
    private const int Pixels = 1024, LinePixels = 512;
    private const float CruiseSpeed = 7f; // m/s, for the time it'll take (lights, turns and all)

    private static readonly Color32 Land = new Color32(0xE4, 0xEB, 0xD9, 0xFF);
    private static readonly Color32 Block = new Color32(0xF1, 0xE6, 0xCF, 0xFF);
    private static readonly Color32 Water = new Color32(0x9C, 0xC9, 0xE8, 0xFF);
    private static readonly Color32 RoadEdge = new Color32(0x9A, 0x9E, 0xA6, 0xFF);
    private static readonly Color32 Road = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color32 Trip = new Color32(0x1E, 0x6F, 0xE0, 0xFF);
    private static readonly Color Pin = new Color32(0xE0, 0x3A, 0x2E, 0xFF);

    private static RideMapUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    // the window
    private GameObject window, mapButton;
    private Text title, subtitle, destName, destInfo;
    private RectTransform map, labels, carDot, pin, listContent;
    private RawImage mapImage, lineImage;
    private Texture2D mapTex, lineTex;
    private Button goButton;
    private float mapSize;

    // the map
    private string drawnFor;
    private Vector3 centre;
    private float half;
    private List<MapPlaces.Place> places = new List<MapPlaces.Place>();

    // the ride
    private TrafficCar car;
    private Action<Vector3, string> onGo;
    private Action onRoam, onOpen;
    private bool hasChoice;
    private Vector3 chosen;
    private string chosenName;

    private static RideMapUI Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("RideMapUI").AddComponent<RideMapUI>();
            return instance;
        }
    }

    // Open the map for a passenger in `vehicle`: go(the spot on the road, its name) or roam()
    public static void Open(TrafficCar vehicle, Action<Vector3, string> go, Action roam)
    {
        var ui = Instance;
        ui.car = vehicle;
        ui.onGo = go;
        ui.onRoam = roam;
        ui.Show();
    }

    public static void Close()
    {
        if (instance != null && instance.window != null) instance.window.SetActive(false);
    }

    public static bool IsOpen => instance != null && instance.window != null && instance.window.activeSelf;

    // The round "แผนที่" button at the side while riding (null hides it)
    public static void MapButton(Action open)
    {
        if (open == null)
        {
            if (instance != null && instance.mapButton != null) instance.mapButton.SetActive(false);
            return;
        }
        var ui = Instance;
        ui.onOpen = open;
        ui.BuildButton();
        ui.mapButton.SetActive(true);
    }

    public static void HideAll()
    {
        Close();
        MapButton(null);
    }

    // ── Building ─────────────────────────────────────────────────────────────────────────────────
    private void BuildButton()
    {
        if (mapButton != null) return;
        var go = new GameObject("RideMapButtonCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        mapButton = go;
        var b = UiKit.RoundButton(go.transform, "", Vector2.zero, new Vector2(150f, 150f), UiKit.Teak, UiKit.HeaderText, () => onOpen?.Invoke(), 22, circle: true);
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-150f, 60f);
        rt.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.IconTile(rt, "travel", new Vector2(0f, 14f), new Vector2(76f, 76f)).GetComponent<Image>().raycastTarget = false;
        var label = b.GetComponentInChildren<Text>();
        label.text = "แผนที่";
        label.rectTransform.anchoredPosition = new Vector2(0f, -46f);
        label.rectTransform.sizeDelta = new Vector2(130f, 34f);
    }

    private void Build()
    {
        if (window != null) return;
        bool portrait = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "RideMapCanvas", UiKit.WindowSize(new Vector2(1560f, 900f), 1560f), 60, "icon:travel",
            out window, out title, out subtitle, Close, out _);
        title.text = "จะไปไหนดี?";
        // riding along moves you: the window mustn't fade as if you'd walked away
        window.GetComponent<WindowAutoClose>()?.Configure(Close, 0f, false);
        float bodyW = body.sizeDelta.x, bodyH = body.sizeDelta.y;

        // the map, square, framed
        mapSize = portrait ? bodyW - 40f : bodyH - 40f;
        var mapCentre = portrait ? new Vector2(0f, bodyH * 0.5f - 20f - mapSize * 0.5f) : new Vector2(-bodyW * 0.5f + 20f + mapSize * 0.5f, 0f);
        var frame = UiKit.Panel("MapFrame", body, UiKit.CardBorder, mapCentre, new Vector2(mapSize + 10f, mapSize + 10f));
        frame.gameObject.AddComponent<RectMask2D>();
        var mapGo = new GameObject("Map", typeof(RectTransform), typeof(RawImage), typeof(EventTrigger));
        map = (RectTransform)mapGo.transform;
        map.SetParent(frame, false);
        map.sizeDelta = new Vector2(mapSize, mapSize);
        mapImage = mapGo.GetComponent<RawImage>();
        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(e => Tap((PointerEventData)e));
        mapGo.GetComponent<EventTrigger>().triggers.Add(click);
        lineImage = new GameObject("Trip", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        lineImage.rectTransform.SetParent(map, false);
        lineImage.rectTransform.sizeDelta = map.sizeDelta;
        lineImage.raycastTarget = false;
        labels = UiKit.Container(map, Vector2.zero);
        carDot = Marker(map, new Color(1f, 0.8f, 0.15f), 34f);
        var nose = UiKit.Rect("Nose", carDot, new Color(1f, 0.8f, 0.15f));
        nose.sizeDelta = new Vector2(14f, 14f);
        nose.anchoredPosition = new Vector2(0f, 18f);
        nose.localRotation = Quaternion.Euler(0f, 0f, 45f);
        nose.GetComponent<Image>().raycastTarget = false;
        nose.SetAsFirstSibling();
        pin = Marker(map, Pin, 30f);
        var stick = UiKit.Rect("Stick", pin, new Color(0.45f, 0.08f, 0.06f));
        stick.sizeDelta = new Vector2(6f, 24f);
        stick.anchoredPosition = new Vector2(0f, -24f);
        stick.GetComponent<Image>().raycastTarget = false;
        // north arrow and legend
        var north = UiKit.Panel("North", map, new Color(1f, 1f, 1f, 0.85f), new Vector2(mapSize * 0.5f - 40f, mapSize * 0.5f - 40f), new Vector2(54f, 54f));
        north.GetComponent<Image>().raycastTarget = false;
        UiKit.Label(north, 26, "N", Vector2.zero, new Vector2(50f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;

        // the side (or, upright, the bottom): the places, the choice, the buttons
        float sideW = portrait ? bodyW - 40f : bodyW - mapSize - 80f;
        float sideX = portrait ? 0f : bodyW * 0.5f - 20f - sideW * 0.5f;
        float top = portrait ? mapCentre.y - mapSize * 0.5f - 20f : bodyH * 0.5f - 20f;
        float bottom = -bodyH * 0.5f + 20f;
        const float buttonsH = 84f, summaryH = 130f, headH = 40f;
        float listTop = top - headH, listBottom = bottom + buttonsH + summaryH + 30f;
        UiKit.Label(body, 26, "สถานที่ · แตะเพื่อเลือก (ใกล้สุดก่อน)", new Vector2(sideX, top - headH * 0.5f), new Vector2(sideW, headH), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        var viewport = UiKit.Panel("Places", body, new Color(0.96f, 0.93f, 0.86f), new Vector2(sideX, (listTop + listBottom) * 0.5f), new Vector2(sideW, listTop - listBottom));
        viewport.gameObject.AddComponent<RectMask2D>();
        listContent = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
        listContent.SetParent(viewport, false);
        listContent.anchorMin = listContent.anchorMax = new Vector2(0.5f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.sizeDelta = new Vector2(sideW, 10f);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = listContent;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        float summaryY = bottom + buttonsH + 15f + summaryH * 0.5f;
        var summary = UiKit.Panel("Choice", body, Color.white, new Vector2(sideX, summaryY), new Vector2(sideW, summaryH), shadow: true);
        summary.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        Marker(summary, Pin, 26f).anchoredPosition = new Vector2(-sideW * 0.5f + 40f, 22f);
        destName = UiKit.Label(summary, 30, "", new Vector2(30f, 28f), new Vector2(sideW - 100f, 44f), UiKit.Ink, TextAnchor.MiddleLeft);
        destName.fontStyle = FontStyle.Bold;
        destInfo = UiKit.Label(summary, 23, "", new Vector2(30f, -28f), new Vector2(sideW - 100f, 60f), UiKit.Muted, TextAnchor.MiddleLeft);

        float bw = (sideW - 20f) * 0.5f, by = bottom + buttonsH * 0.5f;
        goButton = UiKit.RoundButton(body, "ไปที่นี่", new Vector2(sideX - sideW * 0.5f + bw * 0.5f, by), new Vector2(bw, buttonsH), UiKit.PayGreen, Color.white, Go, 32);
        UiKit.RoundButton(body, "เที่ยวรอบเมือง", new Vector2(sideX + sideW * 0.5f - bw * 0.5f, by), new Vector2(bw, buttonsH), UiKit.Teak, UiKit.HeaderText, Roam, 28);
        window.SetActive(false);
    }

    private static RectTransform Marker(Transform parent, Color color, float size)
    {
        var rt = UiKit.Rect("Marker", parent, color);
        rt.sizeDelta = new Vector2(size, size);
        var image = rt.GetComponent<Image>();
        image.sprite = UiKit.CircleSprite;
        image.raycastTarget = false;
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Color.white;
        o.effectDistance = new Vector2(3f, -3f);
        return rt;
    }

    // ── Showing ──────────────────────────────────────────────────────────────────────────────────
    private void Show()
    {
        Build();
        window.SetActive(true);
        DrawMap();
        FillList();
        hasChoice = false;
        chosenName = null;
        var trip = car != null ? car.TripLine() : null;
        DrawLine(trip);
        if (trip != null && trip.Count > 0)
        {
            pin.gameObject.SetActive(true);
            pin.anchoredPosition = ToMap(trip[trip.Count - 1]) + new Vector2(0f, 24f);
            destName.text = "กำลังไปที่หมายเดิม";
            destInfo.text = $"อีกราว {Round(TrafficNet.LineLength(trip))} ตามถนน · แตะที่ใหม่เพื่อเปลี่ยน";
        }
        else
        {
            pin.gameObject.SetActive(false);
            destName.text = "ยังไม่ได้เลือกที่หมาย";
            destInfo.text = "แตะสถานที่ในรายการ หรือแตะบนแผนที่ตรงไหนก็ได้";
        }
        subtitle.text = car != null ? "รถพาไปตามถนน แล้วจอดส่งให้ถึงที่" : "";
        goButton.interactable = false;
        Update();
    }

    // The map picture: drawn once per town (it doesn't change while you're there)
    private void DrawMap()
    {
        string key = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + TrafficNet.Count;
        if (key == drawnFor && mapTex != null) return;
        drawnFor = key;
        TrafficNet.EnsureBuilt();
        places = MapPlaces.Find();

        var b = TrafficNet.Extent();
        foreach (var p in places) if (!p.water) b.Encapsulate(p.at);
        centre = b.center;
        half = Mathf.Max(b.size.x, b.size.z) * 0.5f + 18f;

        if (mapTex == null) mapTex = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[Pixels * Pixels];
        for (int i = 0; i < px.Length; i++) px[i] = Land;
        foreach (var p in places) // rivers under everything, then the places' grounds
            if (p.water) FillRect(px, Pixels, p.area, Water, 0f);
        foreach (var p in places)
            if (!p.water) FillRect(px, Pixels, p.area, Block, 1f);
        // the roads: a grey edge, then the white carriageway over it, turns included
        for (int pass = 0; pass < 2; pass++)
        {
            float r = pass == 0 ? 2.7f : 2.0f;
            var c = pass == 0 ? RoadEdge : Road;
            for (int i = 0; i < TrafficNet.Count; i++)
            {
                var route = TrafficNet.Get(i);
                for (float d = 0f; d <= route.length; d += 0.7f) Disc(px, Pixels, TrafficNet.PointAt(route, d, out _), r, c);
                foreach (var t in route.transfers)
                    if (t.curve != null && t.kind == TrafficNet.Kind.Turn)
                        for (int k = 1; k < t.curve.Length; k++)
                            for (float u = 0f; u < 1f; u += 0.34f) Disc(px, Pixels, Vector3.Lerp(t.curve[k - 1], t.curve[k], u), r, c);
            }
        }
        mapTex.SetPixels32(px);
        mapTex.Apply();
        mapImage.texture = mapTex;

        // names on the map
        UiKit.Clear(labels);
        foreach (var p in places)
        {
            if (p.water) continue;
            var dot = Marker(labels, UiKit.Teak, 16f);
            dot.anchoredPosition = ToMap(p.at);
            var label = UiKit.Label(labels, 17, p.name, ToMap(p.at) + new Vector2(0f, 20f), new Vector2(190f, 26f), UiKit.Ink);
            label.resizeTextMinSize = 12;
            var o = label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(1f, 1f, 1f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void FillList()
    {
        UiKit.Clear(listContent);
        var here = car != null ? car.transform.position : centre;
        var list = places.FindAll(p => !p.water);
        list.Sort((a, b) => Flat(a.at - here).CompareTo(Flat(b.at - here)));
        float w = listContent.sizeDelta.x, rowH = 86f, y = -8f;
        foreach (var p in list)
        {
            var place = p;
            var row = UiKit.RoundButton(listContent, "", new Vector2(0f, y - rowH * 0.5f), new Vector2(w - 20f, rowH - 8f), Color.white, UiKit.Ink, () => Choose(place.at, place.name), 24);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            if (!string.IsNullOrEmpty(p.icon)) UiKit.IconTile(rt, p.icon, new Vector2(-w * 0.5f + 52f, 0f), new Vector2(60f, 60f));
            var name = row.GetComponentInChildren<Text>();
            name.text = p.name;
            name.alignment = TextAnchor.MiddleLeft;
            name.rectTransform.anchoredPosition = new Vector2(30f, 12f);
            name.rectTransform.sizeDelta = new Vector2(w - 170f, 40f);
            UiKit.Label(rt, 19, $"ห่างราว {Round(Flat(p.at - here))} (เส้นตรง)", new Vector2(30f, -20f), new Vector2(w - 170f, 28f), UiKit.Muted, TextAnchor.MiddleLeft);
            y -= rowH;
        }
        if (list.Count == 0)
            UiKit.Label(listContent, 22, "แตะบนแผนที่ตรงที่อยากไปได้เลย", new Vector2(0f, -50f), new Vector2(w - 40f, 60f), UiKit.Muted);
        listContent.sizeDelta = new Vector2(w, Mathf.Max(100f, -y + 8f));
        listContent.anchoredPosition = Vector2.zero;
    }

    // ── Choosing ─────────────────────────────────────────────────────────────────────────────────
    private void Tap(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(map, e.position, e.pressEventCamera, out var local)) return;
        // on a place's dot: that place
        foreach (var p in places)
            if (!p.water && Vector2.Distance(ToMap(p.at), local) < 22f) { Choose(p.at, p.name); return; }
        var world = new Vector3(centre.x + local.x / (mapSize * 0.5f) * half, centre.y, centre.z + local.y / (mapSize * 0.5f) * half);
        // a spot by the road: say which place it's near
        string near = null;
        float best = 40f;
        foreach (var p in places)
        {
            if (p.water) continue;
            float d = Flat(p.at - world);
            if (d < best) { best = d; near = p.name; }
        }
        Choose(world, near != null ? $"ริมถนน ใกล้{near}" : "จุดที่ปักหมุดไว้");
    }

    private void Choose(Vector3 world, string name)
    {
        if (!TrafficNet.NearestRoad(world, out var road)) return;
        hasChoice = true;
        chosen = road;
        chosenName = name;
        pin.gameObject.SetActive(true);
        pin.anchoredPosition = ToMap(road) + new Vector2(0f, 24f);
        destName.text = name;
        if (car != null && car.Preview(road, out var line, out float metres))
        {
            DrawLine(line);
            float minutes = metres / CruiseSpeed / 60f;
            destInfo.text = $"ระยะทางตามถนน {Round(metres)} · " + (minutes < 1f ? "ไม่ถึง 1 นาที" : $"ประมาณ {Mathf.CeilToInt(minutes)} นาที");
            goButton.interactable = true;
        }
        else
        {
            DrawLine(null);
            destInfo.text = "ถนนไปไม่ถึงตรงนั้น ลองเลือกที่อื่น";
            goButton.interactable = false;
        }
        GameAudio.Play("pickup");
    }

    private static string Round(float metres) =>
        metres >= 1000f ? $"{metres / 1000f:0.0} กม." : $"{Mathf.Max(10, Mathf.RoundToInt(metres / 10f) * 10)} ม.";

    private void Go()
    {
        if (!hasChoice) return;
        Close();
        onGo?.Invoke(chosen, chosenName);
    }

    private void Roam()
    {
        Close();
        onRoam?.Invoke();
    }

    // ── Drawing ──────────────────────────────────────────────────────────────────────────────────
    private Vector2 ToMap(Vector3 world) => new Vector2(
        (world.x - centre.x) / half * mapSize * 0.5f,
        (world.z - centre.z) / half * mapSize * 0.5f);

    private Vector2 ToPixel(Vector3 world, int size) => new Vector2(
        ((world.x - centre.x) / half * 0.5f + 0.5f) * size,
        ((world.z - centre.z) / half * 0.5f + 0.5f) * size);

    private void Disc(Color32[] px, int size, Vector3 world, float radius, Color32 c)
    {
        var p = ToPixel(world, size);
        float r = radius / (2f * half) * size;
        int x0 = Mathf.Max(0, (int)(p.x - r)), x1 = Mathf.Min(size - 1, (int)(p.x + r) + 1);
        int y0 = Mathf.Max(0, (int)(p.y - r)), y1 = Mathf.Min(size - 1, (int)(p.y + r) + 1);
        float rr = r * r;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - p.x, dy = y + 0.5f - p.y;
                if (dx * dx + dy * dy <= rr) px[y * size + x] = c;
            }
    }

    private void FillRect(Color32[] px, int size, Bounds b, Color32 c, float inset)
    {
        var a = ToPixel(b.min + new Vector3(inset, 0f, inset), size);
        var z = ToPixel(b.max - new Vector3(inset, 0f, inset), size);
        for (int y = Mathf.Max(0, (int)a.y); y < Mathf.Min(size, (int)z.y); y++)
            for (int x = Mathf.Max(0, (int)a.x); x < Mathf.Min(size, (int)z.x); x++)
                px[y * size + x] = c;
    }

    private void DrawLine(List<Vector3> line)
    {
        if (lineTex == null) lineTex = new Texture2D(LinePixels, LinePixels, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[LinePixels * LinePixels];
        if (line != null)
            for (int i = 1; i < line.Count; i++)
            {
                float seg = Vector3.Distance(line[i - 1], line[i]);
                for (float u = 0f; u <= seg; u += 0.8f) Disc(px, LinePixels, Vector3.Lerp(line[i - 1], line[i], seg > 0f ? u / seg : 0f), 1.5f, Trip);
            }
        lineTex.SetPixels32(px);
        lineTex.Apply();
        lineImage.texture = lineTex;
    }

    private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    private void Update()
    {
        if (window == null || !window.activeSelf || car == null) return;
        carDot.anchoredPosition = ToMap(car.transform.position);
        carDot.localRotation = Quaternion.Euler(0f, 0f, -car.transform.eulerAngles.y);
    }

    private void OnDestroy()
    {
        if (mapTex != null) Destroy(mapTex);
        if (lineTex != null) Destroy(lineTex);
    }
}
