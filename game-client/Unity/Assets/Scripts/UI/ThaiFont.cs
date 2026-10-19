using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Thai text font. Unity's built-in LegacyRuntime font has no Thai glyphs: the Mac Editor quietly
// falls back to system fonts, but iPhone doesn't, so Thai text came out blank there.
// We bundle Sarabun (SIL OFL, Resources/Fonts) — OS fonts like Thonburi drew vowels/tone marks
// with dotted circles (◌) on iOS. OS fonts remain a fallback. The font is swapped into every
// Text / TextMesh when a scene loads (signs built in the Editor included).
public static class ThaiFont
{
    private static readonly string[] OsFonts = { "Thonburi", "Sukhumvit Set", "Noto Sans Thai", "Tahoma", "Arial Unicode MS" };

    private static Font font;
    private static Font legacy;
    private static Font display;

    // Rounded, friendly display font (Mitr SemiBold, SIL OFL) for signs, buttons, titles and
    // floating text — the cartoon look of the game art. Body text stays in Sarabun.
    public static Font Display
    {
        get
        {
            if (display != null) return display;
            display = Resources.Load<Font>("Fonts/Mitr-SemiBold");
            if (display == null) display = Font;
            return display;
        }
    }

    public static Font Font
    {
        get
        {
            if (font != null) return font;
            legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font = Resources.Load<Font>("Fonts/Sarabun-Regular");
            if (font == null)
            {
                try { font = Font.CreateDynamicFontFromOSFont(OsFonts, 32); }
                catch (System.Exception e) { Debug.LogWarning($"[ThaiFont] OS font unavailable: {e.Message}"); }
            }
            if (font == null) font = legacy;
            return font;
        }
    }

    // Every Thai letter, vowel and tone mark, so the dynamic atlas has them before they're needed
    private const string ThaiChars =
        "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำิีึืฺุู฿เแโใไๅๆ็่้๊๋์ํ๎๐๑๒๓๔๕๖๗๘๙";
    private static readonly int[] CommonSizes = { 20, 24, 26, 28, 30, 32, 34, 36, 40, 44, 48, 52, 56, 64 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        var thai = Font;
        foreach (int size in CommonSizes)
        {
            thai.RequestCharactersInTexture(ThaiChars, size, FontStyle.Normal);
            thai.RequestCharactersInTexture(ThaiChars, size, FontStyle.Bold);
            Display.RequestCharactersInTexture(ThaiChars, size, FontStyle.Normal);
        }

        // When the atlas is rebuilt (new sizes/glyphs), UI Text redraws itself but TextMesh doesn't:
        // nudge every TextMesh so no letters go missing.
        Font.textureRebuilt += rebuilt =>
        {
            if (rebuilt != font && rebuilt != display) return;
            foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string text = mesh.text;
                mesh.text = "";
                mesh.text = text;
            }
        };

        ApplyToScene();
        SceneManager.sceneLoaded += (_, __) => ApplyToScene();
    }

    // Replace the font on every Text and TextMesh currently in the scene
    public static void ApplyToScene()
    {
        var thai = Font;
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.font == null || text.font == legacy) text.font = thai;

        foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Apply(mesh);
    }

    // 3D text (signs, names over heads, floating numbers) uses the display font
    public static void Apply(TextMesh mesh)
    {
        mesh.font = Display;
        mesh.fontStyle = FontStyle.Normal; // Mitr SemiBold is already heavy
        var renderer = mesh.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = Display.material;
    }
}
