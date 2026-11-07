using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ป้ายชื่อบนรูปไอคอน: the painted place pictures (ฟาร์ม, วัด, เมือง…) carry a Thai name on a wooden
// ribbon along the bottom. In English an English ribbon of the same shape is laid over it — on UI icons
// (IconTile, HUD buttons) and on the world signposts' medallions — and taken away again in Thai.
public class IconRibbon : MonoBehaviour
{
    // icon file → English name; only these pictures have words painted on them
    private static readonly Dictionary<string, string> names = new Dictionary<string, string>
    {
        { "farm", "Farm" }, { "city", "City" }, { "temple", "Temple" }, { "home", "Home" },
        { "school", "School" }, { "market", "Market" }, { "shop", "Shop" }, { "food", "Food" },
        { "fishing", "Fishing" }, { "travel", "Travel" }, { "hotel", "Hotel" }, { "hospital", "Hospital" },
        { "community", "Community" }, { "boxing", "Boxing" }, { "fair", "Temple Fair" },
        { "koknongna", "Farm Pond" }, { "sao", "Sub-district" }, { "settings", "Settings" },
    };

    // where the painted ribbon sits on the picture (0..1 of the picture, from bottom-left)
    private static readonly Vector2 Min = new Vector2(0.1f, 0.05f), Max = new Vector2(0.9f, 0.228f);
    private static readonly Color Wood = new Color32(0x6E, 0x34, 0x16, 0xFF);
    private static readonly Color Cream = new Color32(0xFF, 0xF3, 0xD6, 0xFF);

    public static bool HasWords(string icon) => icon != null && names.ContainsKey(icon);

    [SerializeField] private string icon;   // world signposts: saved with the scene, ribbon made when needed
    private GameObject ribbon;

    private void OnEnable()
    {
        Lang.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => Lang.Changed -= Refresh;

    private void Refresh()
    {
        if (ribbon == null && Lang.IsEnglish && !string.IsNullOrEmpty(icon)) ribbon = BuildWorld(transform, icon);
        if (ribbon != null) ribbon.SetActive(Lang.IsEnglish);
    }

    // Editor (world builder): mark a sign's icon quad; the English plate is made in play when needed
    public static void MarkWorld(GameObject quad, string icon)
    {
        if (quad == null || !HasWords(icon)) return;
        quad.AddComponent<IconRibbon>().icon = icon;
    }

    // On a UI picture: `picture` is the Image showing the whole icon
    public static void AddUI(RectTransform picture, string icon)
    {
        if (picture == null || !names.TryGetValue(icon ?? "", out var english)) return;
        var bar = new GameObject("RibbonEN", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)bar.transform;
        rt.SetParent(picture, false);
        rt.anchorMin = Min;
        rt.anchorMax = Max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var image = bar.GetComponent<Image>();
        image.sprite = UiKit.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = Wood;
        image.raycastTarget = false;
        var edge = bar.AddComponent<Outline>();
        edge.effectColor = UiKit.Gold;
        edge.effectDistance = new Vector2(1.5f, -1.5f);

        var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var lt = (RectTransform)label.transform;
        lt.SetParent(rt, false);
        lt.anchorMin = Vector2.zero;
        lt.anchorMax = Vector2.one;
        lt.offsetMin = new Vector2(4f, 1f);
        lt.offsetMax = new Vector2(-4f, -1f);
        var text = label.GetComponent<Text>();
        text.font = ThaiFont.Display;
        text.text = english;
        text.color = Cream;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 6;
        text.resizeTextMaxSize = 60;
        text.raycastTarget = false;

        var keeper = picture.gameObject.AddComponent<IconRibbon>();
        keeper.ribbon = bar;
        keeper.Refresh();
    }

    // On a world picture: `quad` is a 1x1 Quad (scaled) facing -z with the icon on it
    private static GameObject BuildWorld(Transform quad, string icon)
    {
        string english = names[icon];
        var bar = new GameObject("RibbonEN");
        bar.transform.SetParent(quad, false);
        bar.transform.localPosition = new Vector3((Min.x + Max.x) * 0.5f - 0.5f, (Min.y + Max.y) * 0.5f - 0.5f, -0.01f);

        var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(plate.GetComponent<Collider>());
        plate.name = "Plate";
        plate.transform.SetParent(bar.transform, false);
        plate.transform.localScale = new Vector3(Max.x - Min.x, Max.y - Min.y, 1f);
        var r = plate.GetComponent<Renderer>();
        r.sharedMaterial = WoodMaterial();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(bar.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        var tm = textGo.AddComponent<TextMesh>();
        tm.fontSize = 64;
        ThaiFont.Apply(tm);
        tm.text = english;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Cream;
        // letter height: most of the plate, smaller for long names (about 0.55 of the height per letter)
        float height = Mathf.Min((Max.y - Min.y) * 0.62f, (Max.x - Min.x) * 0.9f / (english.Length * 0.55f));
        tm.characterSize = height * 10f / tm.fontSize;
        return bar;
    }

    private static Material woodMat;
    private static Material WoodMaterial()
    {
        if (woodMat != null) return woodMat;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        woodMat = new Material(shader) { name = "RibbonWood", color = Wood };
        if (woodMat.HasProperty("_BaseColor")) woodMat.SetColor("_BaseColor", Wood);
        return woodMat;
    }
}
