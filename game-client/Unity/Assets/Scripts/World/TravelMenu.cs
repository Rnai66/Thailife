using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Travel map at a songthaew stop (ป้ายรถสองแถว), premium layout: one card per destination
// (หมู่บ้าน → ตำบล → เมือง) joined by a route line; the current location is marked "อยู่ที่นี่".
// The boat pier (ท่าเรือ) shows only the sea trips (ทะเล ⇄ เกาะ); the island is not on the road map.
// The skytrain platform in the city shows the trips to other provinces (ต่างจังหวัด).
// Built at runtime; one instance per scene.
public class TravelMenu : MonoBehaviour
{
    private static TravelMenu instance;

    private const float Spacing = 360f, PortraitStep = 230f;

    private static string building; // mode for the instance being built (read in Awake): road / boat / train

    private GameObject backdrop;
    private Text title;
    private string mode;
    private bool portrait;

    public static void Show(string heading = null, bool boat = false, bool train = false)
    {
        string m = train ? "train" : boat ? "boat" : "road";
        if (instance != null && instance.mode != m)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
        if (instance == null)
        {
            building = m;
            instance = new GameObject(m == "boat" ? "BoatMenu" : m == "train" ? "TrainMenu" : "TravelMenu").AddComponent<TravelMenu>();
        }
        instance.title.text = !string.IsNullOrEmpty(heading) ? heading : m == "train" ? "สถานีรถไฟฟ้า" : "ป้ายรถสองแถว";
        instance.backdrop.SetActive(true);
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            Debug.LogWarning("[Travel] No EventSystem in the scene — run Thai Life > Repair Scene.");

        mode = building ?? "road";
        var all = new System.Collections.Generic.List<Location>();
        foreach (var l in Locations.All)
            if (mode == "boat" ? l.Level == LocationLevel.Beach && !l.TrainOnly
                : mode == "train" ? l.TrainOnly || l.Id == "city" || l.Id == "beach"
                : !l.BoatOnly && !l.TrainOnly) all.Add(l);
        portrait = UiKit.Portrait;
        var size = portrait ? new Vector2(UiKit.PortraitWidth, all.Count * PortraitStep + 220f) : new Vector2(Mathf.Max(1500f, all.Count * Spacing + 60f), 880f);
        var body = UiKit.PremiumWindow(transform, "TravelCanvas", size, 50, "icon:travel",
            out backdrop, out title, out var subtitle, Hide, out _);
        title.text = "ป้ายรถสองแถว";
        subtitle.text = mode == "boat" ? "นั่งเรือหางยาวข้ามทะเล" : mode == "train" ? "นั่งรถไฟฟ้าไปต่างจังหวัด" : "เลือกจุดหมายปลายทาง";

        // Route line behind the cards (upright: down the left side)
        var route = UiKit.Rect("Route", body, UiKit.Gold);
        route.anchoredPosition = portrait ? new Vector2(-325f, 0f) : new Vector2(0f, 230f);
        route.sizeDelta = portrait ? new Vector2(6f, (all.Count - 1) * PortraitStep) : new Vector2((all.Count - 1) * Spacing, 6f);

        var current = Locations.Current;
        for (int i = 0; i < all.Count; i++)
            AddStop(body, all[i], i, all.Count, current != null && all[i].Id == current.Id);

        backdrop.SetActive(false);
    }

    private void AddStop(RectTransform body, Location location, int index, int count, bool here)
    {
        if (portrait) { AddStopRow(body, location, index, count, here); return; }
        float x = (index - (count - 1) * 0.5f) * Spacing;

        // Stop marker on the route
        var dot = UiKit.Rect("Stop", body, here ? UiKit.PayGreen : UiKit.Gold);
        dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
        dot.anchoredPosition = new Vector2(x, 230f);
        dot.sizeDelta = new Vector2(44f, 44f);

        var card = UiKit.Panel("Destination", body, here ? (Color)new Color32(0xF3, 0xFA, 0xEC, 0xFF) : Color.white,
            new Vector2(x, -40f), new Vector2(340f, 470f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = here ? UiKit.PayGreen : UiKit.CardBorder;

        // Place picture (บ้าน / อบต. / เมือง)
        string icon = !string.IsNullOrEmpty(location.Icon) ? location.Icon : location.Level == LocationLevel.Village ? "home" : location.Level == LocationLevel.Subdistrict ? "sao"
            : location.Level == LocationLevel.Beach ? "travel" : "city";
        UiKit.IconTile(card, icon, new Vector2(0f, 82f), new Vector2(180f, 180f));

        UiKit.PillLabel(card, Locations.LevelNameTh(location.Level), new Vector2(0f, 205f), new Vector2(160f, 40f),
            here ? UiKit.OkTint : UiKit.Pill, here ? UiKit.OkGreen : UiKit.Price, 26);

        // "ตำบล · อบต. รพ.สต. …" → name on top, places underneath
        string name = location.NameTh, places = "";
        int dotIndex = name.IndexOf('·');
        if (dotIndex > 0)
        {
            places = name.Substring(dotIndex + 1).Trim();
            name = name.Substring(0, dotIndex).Trim();
        }
        UiKit.Label(card, 34, name, new Vector2(0f, -32f), new Vector2(310f, 50f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 21, places, new Vector2(0f, -88f), new Vector2(310f, 60f), UiKit.Muted);

        if (here)
        {
            UiKit.RoundButton(card, "อยู่ที่นี่", new Vector2(0f, -175f), new Vector2(290f, 76f), UiKit.OkTint, UiKit.OkGreen, () => { }, 30)
                .interactable = false;
        }
        else
        {
            var target = location;
            var go = UiKit.RoundButton(card, location.Available ? "ไปที่นี่" : "เร็วๆ นี้", new Vector2(0f, -175f), new Vector2(290f, 76f),
                UiKit.Gold, UiKit.GoldInk, () => Locations.TravelTo(target, mode == "boat" ? "ferry" : null), 32);
            go.interactable = location.Available;
        }
    }

    // Upright: one row per stop, top to bottom — picture, name and places, then the button
    private void AddStopRow(RectTransform body, Location location, int index, int count, bool here)
    {
        float y = ((count - 1) * 0.5f - index) * PortraitStep;
        var dot = UiKit.Rect("Stop", body, here ? UiKit.PayGreen : UiKit.Gold);
        dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
        dot.anchoredPosition = new Vector2(-325f, y);
        dot.sizeDelta = new Vector2(40f, 40f);

        var card = UiKit.Panel("Destination", body, here ? (Color)new Color32(0xF3, 0xFA, 0xEC, 0xFF) : Color.white,
            new Vector2(30f, y), new Vector2(640f, PortraitStep - 16f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = here ? UiKit.PayGreen : UiKit.CardBorder;
        UiKit.IconTile(card, IconOf(location), new Vector2(-235f, 0f), new Vector2(150f, 150f));

        SplitName(location, out string name, out string places);
        UiKit.PillLabel(card, Locations.LevelNameTh(location.Level), new Vector2(-55f, 58f), new Vector2(150f, 36f),
            here ? UiKit.OkTint : UiKit.Pill, here ? UiKit.OkGreen : UiKit.Price, 22);
        UiKit.Label(card, 36, name, new Vector2(-15f, 10f), new Vector2(230f, 50f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 21, places, new Vector2(-15f, -48f), new Vector2(230f, 60f), UiKit.Muted, TextAnchor.UpperLeft);

        var at = new Vector2(212f, 0f);
        var size = new Vector2(190f, 84f);
        if (here)
            UiKit.RoundButton(card, "อยู่ที่นี่", at, size, UiKit.OkTint, UiKit.OkGreen, () => { }, 30).interactable = false;
        else
        {
            var target = location;
            UiKit.RoundButton(card, location.Available ? "ไปที่นี่" : "เร็วๆ นี้", at, size,
                UiKit.Gold, UiKit.GoldInk, () => Locations.TravelTo(target, mode == "boat" ? "ferry" : null), 32).interactable = location.Available;
        }
    }

    private static string IconOf(Location location) =>
        !string.IsNullOrEmpty(location.Icon) ? location.Icon : location.Level == LocationLevel.Village ? "home" : location.Level == LocationLevel.Subdistrict ? "sao"
            : location.Level == LocationLevel.Beach ? "travel" : "city";

    // "ตำบล · อบต. รพ.สต. …" → name on top, places underneath
    private static void SplitName(Location location, out string name, out string places)
    {
        name = location.NameTh;
        places = "";
        int dotIndex = name.IndexOf('·');
        if (dotIndex <= 0) return;
        places = name.Substring(dotIndex + 1).Trim();
        name = name.Substring(0, dotIndex).Trim();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
