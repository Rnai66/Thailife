using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Travel map at a songthaew stop (ป้ายรถสองแถว), premium layout: one card per destination
// (หมู่บ้าน → ตำบล → เมือง) joined by a route line; the current location is marked "อยู่ที่นี่".
// Built at runtime; one instance per scene.
public class TravelMenu : MonoBehaviour
{
    private static TravelMenu instance;

    private GameObject backdrop;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("TravelMenu").AddComponent<TravelMenu>();
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

        var body = UiKit.PremiumWindow(transform, "TravelCanvas", new Vector2(1500f, 760f), 50, "รถ",
            out backdrop, out var title, out var subtitle, Hide, out _);
        title.text = "ป้ายรถสองแถว";
        subtitle.text = "เลือกจุดหมายปลายทาง";

        // Route line behind the cards
        var route = UiKit.Rect("Route", body, UiKit.Gold);
        route.anchoredPosition = new Vector2(0f, 150f);
        route.sizeDelta = new Vector2(940f, 6f);

        var current = Locations.Current;
        var all = Locations.All;
        for (int i = 0; i < all.Count; i++)
            AddStop(body, all[i], i, all.Count, current != null && all[i].Id == current.Id);

        backdrop.SetActive(false);
    }

    private void AddStop(RectTransform body, Location location, int index, int count, bool here)
    {
        float x = (index - (count - 1) * 0.5f) * 470f;

        // Stop marker on the route
        var dot = UiKit.Rect("Stop", body, here ? UiKit.PayGreen : UiKit.Gold);
        dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
        dot.anchoredPosition = new Vector2(x, 150f);
        dot.sizeDelta = new Vector2(44f, 44f);

        var card = UiKit.Panel("Destination", body, here ? (Color)new Color32(0xF3, 0xFA, 0xEC, 0xFF) : Color.white,
            new Vector2(x, -60f), new Vector2(440f, 340f), shadow: true);
        card.gameObject.AddComponent<Outline>().effectColor = here ? UiKit.PayGreen : UiKit.CardBorder;

        UiKit.PillLabel(card, Locations.LevelNameTh(location.Level), new Vector2(0f, 125f), new Vector2(160f, 44f),
            here ? UiKit.OkTint : UiKit.Pill, here ? UiKit.OkGreen : UiKit.Price, 26);

        // "ตำบลคอโค · อบต. รพ.สต. …" → name on top, places underneath
        string name = location.NameTh, places = "";
        int dotIndex = name.IndexOf('·');
        if (dotIndex > 0)
        {
            places = name.Substring(dotIndex + 1).Trim();
            name = name.Substring(0, dotIndex).Trim();
        }
        UiKit.Label(card, 36, name, new Vector2(0f, 60f), new Vector2(400f, 54f), UiKit.Ink).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 24, places, new Vector2(0f, -10f), new Vector2(390f, 80f), UiKit.Muted);

        if (here)
        {
            UiKit.RoundButton(card, "อยู่ที่นี่", new Vector2(0f, -115f), new Vector2(360f, 76f), UiKit.OkTint, UiKit.OkGreen, () => { }, 30)
                .interactable = false;
        }
        else
        {
            var target = location;
            var go = UiKit.RoundButton(card, location.Available ? "ไปที่นี่" : "เร็วๆ นี้", new Vector2(0f, -115f), new Vector2(360f, 76f),
                UiKit.Gold, UiKit.GoldInk, () => Locations.TravelTo(target), 32);
            go.interactable = location.Available;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
