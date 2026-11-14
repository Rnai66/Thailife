using System.Collections.Generic;
using UnityEngine;

// The café building in the City (built by the editor's CafeShop) shown the way it is for this player.
// Run a coffee business (ธนาคาร → ธุรกิจ → ร้านกาแฟ) and it's your shop: your name over the door,
// the walls and floor in your style, the furniture where you put it (CafeClient), baristas behind the
// bar (one per staff, up to 2) and customers in the seats while it's open (07:00-20:00). Stand inside
// for "จัดร้าน" (CafeUI). Otherwise it's an empty shop to let.
public class CafeSite : MonoBehaviour
{
    [SerializeField] private float halfW = 4.7f, halfD = 3.8f, height = 2.4f; // the room, metres
    [SerializeField] private Vector3 signAt;                                   // over the door (local)

    private const int OpenFrom = 7 * 60, OpenTo = 20 * 60;

    private Transform interior, decor, styleLayer, people;
    private TextMesh sign;
    private GameObject toLet;
    private readonly List<(Renderer r, string part)> surfaces = new List<(Renderer, string)>();
    private readonly Dictionary<int, GameObject> pieces = new Dictionary<int, GameObject>();
    private CafeClient client;
    private string shownStyle;
    private bool shownOpen, hadButton, crowdOpen, crowdDirty;
    private Transform player;
    private float nextCrowd;

    public static CafeSite Instance { get; private set; }
    public Transform Room => decor;
    public float HalfW => halfW;
    public float HalfD => halfD;
    public IReadOnlyDictionary<int, GameObject> Pieces => pieces;
    public CafeStyle Style { get; private set; } = CafeStyle.Of("minimal");
    public bool Mine => client != null && client.Current != null && client.Current.open;

    public void Configure(float roomHalfW, float roomHalfD, float roomHeight, Vector3 signPosition)
    {
        halfW = roomHalfW;
        halfD = roomHalfD;
        height = roomHeight;
        signAt = signPosition;
    }

    private void Awake()
    {
        Instance = this;
        interior = transform.Find("Interior");
        if (interior == null) interior = transform;
        // A space in metres at the floor's middle, whatever the building's scale
        decor = new GameObject("CafeDecor").transform;
        decor.SetParent(interior, false);
        decor.localScale = Vector3.one / Mathf.Max(0.01f, interior.lossyScale.x);
        styleLayer = new GameObject("StyleLayer").transform;
        styleLayer.SetParent(decor, false);
        people = new GameObject("People").transform;
        people.SetParent(decor, false);

        // The room's painted surfaces (Interiors.Rooms) take the style's colours
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            string p = r.transform.parent != null ? r.transform.parent.name : "";
            if (r.name == "Paint" && p.StartsWith("Wall")) surfaces.Add((r, "wall"));
            else if (r.name == "AccentWall") surfaces.Add((r, "feature"));
            else if (r.name == "Floor" && p == "Interior") surfaces.Add((r, "floor"));
            else if (r.name == "Joint" && p == "Interior") surfaces.Add((r, "line"));
        }

        // The sign over the door (part of the outside: hidden from inside like the rest)
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "ShopSign";
        Destroy(board.GetComponent<Collider>());
        board.transform.SetParent(transform, false);
        board.transform.localPosition = signAt;
        board.transform.localScale = new Vector3(5.2f, 0.7f, 0.12f);
        board.GetComponent<Renderer>().sharedMaterial = CafeFurniture.Mat(new Color(0.16f, 0.12f, 0.1f), 0.5f);
        var label = new GameObject("Label");
        label.transform.SetParent(board.transform.parent, false);
        label.transform.localPosition = signAt + new Vector3(0f, 0f, -0.08f);
        var font = ThaiFont;
        sign = label.AddComponent<TextMesh>();
        sign.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        sign.fontSize = 64;
        sign.characterSize = 0.07f;
        sign.anchor = TextAnchor.MiddleCenter;
        sign.alignment = TextAlignment.Center;
        sign.color = new Color(1f, 0.88f, 0.6f);
        label.AddComponent<ReadableText>();

        BuildToLet();
        Show(null);
    }

    // Thai vowels like ำ need the game font (Mitr); the built-in one drops them
    private static Font ThaiFont => Resources.Load<Font>("Fonts/Mitr-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // An empty shop: a stand-up board inside the door
    private void BuildToLet()
    {
        toLet = new GameObject("ToLet");
        toLet.transform.SetParent(decor, false);
        toLet.transform.localPosition = new Vector3(0f, 0f, -halfD + 1.4f);
        toLet.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var board = CafeFurniture.Build("menu_board", CafeStyle.Of("minimal"));
        board.transform.SetParent(toLet.transform, false);
        var t = new GameObject("Text");
        t.transform.SetParent(toLet.transform, false);
        t.transform.localPosition = new Vector3(0f, 1.0f, 0.08f);
        t.transform.localRotation = Quaternion.Euler(10f, 180f, 0f);
        var font = ThaiFont;
        var m = t.AddComponent<TextMesh>();
        m.font = font;
        t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        m.text = Lang.T("ร้านว่าง ให้เช่า\nเปิดร้านกาแฟ\nที่ธนาคาร");
        m.fontSize = 48;
        m.characterSize = 0.022f;
        m.anchor = TextAnchor.MiddleCenter;
        m.alignment = TextAlignment.Center;
        m.color = Color.white;
    }

    private void Start()
    {
        client = CafeClient.Instance;
        if (client == null) return;
        client.Changed += Show;
        if (client.Current != null) Show(client.Current);
    }

    private void OnDestroy()
    {
        if (client != null) client.Changed -= Show;
        if (Instance == this) Instance = null;
        if (hadButton) ContextButton.Hide(this);
    }

    private void Show(CafeClient.State s)
    {
        bool open = s != null && s.open;
        string style = open ? s.style : "minimal";
        if (style != shownStyle)
        {
            shownStyle = style;
            Style = CafeStyle.Of(style);
            ApplyStyle();
        }
        sign.text = open ? (string.IsNullOrEmpty(s.name) ? Lang.T("ร้านกาแฟ") : s.name) : Lang.T("ให้เช่า");
        toLet.SetActive(!open);

        foreach (var go in pieces.Values) if (go != null) Destroy(go);
        pieces.Clear();
        if (open && s.pieces != null)
            foreach (var p in s.pieces)
            {
                var go = CafeFurniture.Build(p.item, Style);
                go.transform.SetParent(decor, false);
                go.transform.localPosition = new Vector3(p.x, 0f, p.z);
                go.transform.localRotation = Quaternion.Euler(0f, p.r, 0f);
                go.name = $"Cafe_{p.item}_{p.id}";
                pieces[p.id] = go;
            }
        shownOpen = open;
        crowdDirty = true; // re-seat everyone (pieces moved)
        nextCrowd = 0f;
    }

    private void ApplyStyle()
    {
        foreach (var (r, part) in surfaces)
        {
            if (r == null) continue;
            r.sharedMaterial = part switch
            {
                "wall" => CafeFurniture.Mat(Style.Wall, 0.1f),
                "feature" => CafeFurniture.Mat(Style.Feature, 0.15f),
                "floor" => CafeFurniture.Mat(Style.Floor, Style.Id == "loft" ? 0.7f : 0.55f),
                _ => CafeFurniture.Mat(Style.FloorLine, 0.3f),
            };
        }
        // The style's own touches: a brick feature wall, wooden panels, floor planks
        for (int i = styleLayer.childCount - 1; i >= 0; i--) Destroy(styleLayer.GetChild(i).gameObject);
        if (Style.Brick)
        {
            var mortar = CafeFurniture.Mat(new Color(0.75f, 0.72f, 0.68f), 0.1f);
            var brick = CafeFurniture.Mat(Style.Feature, 0.1f);
            var brick2 = CafeFurniture.Mat(Color.Lerp(Style.Feature, Color.black, 0.15f), 0.1f);
            Slab(new Vector3(0f, height * 0.5f, halfD - 0.02f), new Vector3(halfW * 2f - 0.1f, height, 0.01f), mortar);
            int row = 0;
            for (float y = 0.04f; y < height - 0.05f; y += 0.075f, row++)
                for (float x = -halfW + (row % 2 == 0 ? 0.06f : 0.18f); x < halfW - 0.12f; x += 0.24f)
                    Slab(new Vector3(x + 0.11f, y + 0.032f, halfD - 0.035f), new Vector3(0.22f, 0.064f, 0.02f), (row + (int)(x * 4f)) % 3 == 0 ? brick2 : brick);
        }
        if (Style.Panels)
        {
            var panel = CafeFurniture.Mat(Style.Id == "thai" ? Style.Wood : Style.Feature, 0.35f);
            var rail = CafeFurniture.Mat(Color.Lerp(Style.Wood, Color.black, 0.2f), 0.4f);
            foreach (float sx in new[] { -1f, 1f })
            {
                Slab(new Vector3(sx * (halfW - 0.03f), 0.5f, 0f), new Vector3(0.02f, 1f, halfD * 2f - 0.1f), panel);
                Slab(new Vector3(sx * (halfW - 0.045f), 1.02f, 0f), new Vector3(0.03f, 0.05f, halfD * 2f - 0.1f), rail);
                for (float z = -halfD + 0.5f; z < halfD - 0.2f; z += 0.6f)
                    Slab(new Vector3(sx * (halfW - 0.045f), 0.5f, z), new Vector3(0.02f, 0.9f, 0.025f), rail);
            }
        }
        if (Style.Planks)
        {
            var line = CafeFurniture.Mat(Style.FloorLine, 0.3f);
            for (float x = -halfW + 0.18f; x < halfW; x += 0.18f)
                Slab(new Vector3(x, 0.046f, 0f), new Vector3(0.008f, 0.003f, halfD * 2f - 0.1f), line);
        }
        if (Style.Id == "thai") // a row of hanging paper lanterns along the feature wall
            for (float x = -halfW + 0.8f; x < halfW - 0.5f; x += 1.2f)
                Slab(new Vector3(x, height - 0.45f, halfD - 0.5f), new Vector3(0.22f, 0.3f, 0.22f), CafeFurniture.Glow(new Color(1f, 0.55f, 0.3f), 1.2f));
    }

    private void Slab(Vector3 p, Vector3 s, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(styleLayer, false);
        go.transform.localPosition = p;
        go.transform.localScale = s;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    // Is a world point inside the room?
    public bool Inside(Vector3 world)
    {
        var p = decor.InverseTransformPoint(world);
        return Mathf.Abs(p.x) < halfW && Mathf.Abs(p.z) < halfD && p.y < height + 1f;
    }

    // A floor spot (room space) `distance` in front of someone, kept in the room
    public Vector3 SpotInFront(Transform who, float distance)
    {
        var p = decor.InverseTransformPoint(who.position + who.forward * distance);
        float hw = client?.Current != null ? client.Current.half_w : halfW - 0.2f;
        float hd = client?.Current != null ? client.Current.half_d : halfD - 0.2f;
        return new Vector3(Mathf.Clamp(p.x, -hw, hw), 0f, Mathf.Clamp(p.z, -hd, hd));
    }

    private void Update()
    {
        if (player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        bool inside = Inside(player.position);

        // The owner, standing inside: จัดร้าน
        bool offer = inside && Mine && !CafeUI.IsOpen && !ContextButton.TakenByOther(this);
        if (offer && !ContextButton.IsShownBy(this))
        {
            var s = client.Current;
            ContextButton.Show(this, "จัดร้าน", $"{(string.IsNullOrEmpty(s.name) ? Lang.T("ร้านกาแฟของคุณ") : s.name)} · {Lang.T("สไตล์")}{s.StyleName} · {Lang.T("พนักงาน")} {s.staff} {Lang.T("คน")}",
                () => CafeUI.Open(this));
            hadButton = true;
        }
        else if (!offer && hadButton)
        {
            ContextButton.Hide(this);
            hadButton = false;
        }

        // Customers come and go with the hours (looked at every few seconds, only when you're near)
        if (Time.time < nextCrowd) return;
        nextCrowd = Time.time + 3f;
        float m = GameClock.MinuteOfDay;
        bool busyHours = shownOpen && m >= OpenFrom && m < OpenTo
                         && (player.position - transform.position).sqrMagnitude < 60f * 60f;
        if (busyHours != crowdOpen || crowdDirty)
        {
            crowdDirty = false;
            Crowd(busyHours);
        }
    }

    // Baristas behind the bar and customers in the seats
    private void Crowd(bool on)
    {
        crowdOpen = on;
        for (int i = people.childCount - 1; i >= 0; i--) Destroy(people.GetChild(i).gameObject);
        if (!on) return;
        var s = client.Current;
        int seed = GameClock.Day * 31;

        // Baristas: behind the first counter (or the espresso machine), facing out
        int baristas = Mathf.Min(2, s.staff);
        Transform bar = null;
        foreach (var p in s.pieces ?? new CafeClient.Piece[0])
            if (p.item == "counter" && pieces.TryGetValue(p.id, out var go)) { bar = go.transform; break; }
        if (bar == null)
            foreach (var p in s.pieces ?? new CafeClient.Piece[0])
                if (p.item == "espresso" && pieces.TryGetValue(p.id, out var go)) { bar = go.transform; break; }
        for (int i = 0; i < baristas; i++)
        {
            Vector3 at = bar != null
                ? decor.InverseTransformPoint(bar.TransformPoint(new Vector3(-0.5f + i * 1f, 0f, -0.75f)))
                : new Vector3(-0.6f + i * 1.2f, 0f, halfD - 0.8f);
            Quaternion face = bar != null ? Quaternion.Inverse(decor.rotation) * bar.rotation : Quaternion.Euler(0f, 180f, 0f);
            Person($"Barista_{i + 1}", at, face, seed + i, seated: false, apron: true);
        }

        // Customers: as many as the staff can serve, in the seats you've put out
        int want = 1 + s.staff * 2;
        int n = 0;
        foreach (var p in s.pieces ?? new CafeClient.Piece[0])
        {
            var seats = CafeFurniture.Seats(p.item);
            if (seats == null || !pieces.TryGetValue(p.id, out var go)) continue;
            foreach (var seat in seats)
            {
                if (n >= want) break;
                if ((seed + p.id * 7 + n) % 4 == 3) continue; // not every seat is taken
                Vector3 at = decor.InverseTransformPoint(go.transform.TransformPoint(seat));
                at.y = 0f;
                Quaternion face = Quaternion.Inverse(decor.rotation) * go.transform.rotation;
                Person($"Customer_{n + 1}", at, face, seed + 100 + n * 13, seated: true, apron: false, seatHeight: seat.y);
                n++;
            }
        }
    }

    private void Person(string name, Vector3 at, Quaternion face, int seed, bool seated, bool apron, float seatHeight = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(people, false);
        go.transform.localPosition = at;
        go.transform.localRotation = face;
        var model = go.AddComponent<CharacterModel>();
        var look = Look.Random(seed);
        if (apron) look.Top = 0;
        model.SetLook(look);
        if (seated)
        {
            model.Seated = true;
            go.AddComponent<CafeSitter>().Configure(seatHeight, seed);
        }
        else go.AddComponent<CafeBarista>().Configure(seed);
    }
}
