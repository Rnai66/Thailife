using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Travel map at a songthaew stop (ป้ายรถสองแถว), premium layout: one card per destination
// (หมู่บ้าน → ตำบล → เมือง) joined by a route line; the current location is marked "อยู่ที่นี่".
// Built at runtime; one instance per scene.
public class TravelMenu : MonoBehaviour
{
    private static TravelMenu instance;

    private const float Spacing = 360f;

    private GameObject backdrop;
    private Text title;

    public static void Show(string heading = null)
    {
        if (instance == null)
            instance = new GameObject("TravelMenu").AddComponent<TravelMenu>();
        instance.title.text = string.IsNullOrEmpty(heading) ? "ป้ายรถสองแถว" : heading;
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

        var all = Locations.All;
        var body = UiKit.PremiumWindow(transform, "TravelCanvas", new Vector2(Mathf.Max(1500f, all.Count * Spacing + 60f), 880f), 50, "icon:travel",
            out backdrop, out title, out var subtitle, Hide, out _);
        title.text = "ป้ายรถสองแถว";
        subtitle.text = "เลือกจุดหมายปลายทาง";

        // Route line behind the cards
        var route = UiKit.Rect("Route", body, UiKit.Gold);
        route.anchoredPosition = new Vector2(0f, 230f);
        route.sizeDelta = new Vector2((all.Count - 1) * Spacing, 6f);

        var current = Locations.Current;
        for (int i = 0; i < all.Count; i++)
            AddStop(body, all[i], i, all.Count, current != null && all[i].Id == current.Id);

        backdrop.SetActive(false);
    }

    private void AddStop(RectTransform body, Location location, int index, int count, bool here)
    {
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
                UiKit.Gold, UiKit.GoldInk, () => Locations.TravelTo(target), 32);
            go.interactable = location.Available;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
