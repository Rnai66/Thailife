using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Keeps every screen-space canvas usable when the phone is held upright (แนวตั้ง). The UI is laid
// out for 1920x1080 landscape; in portrait each canvas is scaled to a fixed 1240-unit width so the
// HUD's left and right columns still fit side by side, edge-pinned HUD pieces move in from the old
// landscape notch inset to the safe area (status bar / home indicator), and window cards shrink to
// fit across the screen; banners pinned to the top centre drop below the HUD columns. Added to
// every canvas automatically; nothing has to call it.
[RequireComponent(typeof(CanvasScaler))]
public class ScreenFit : MonoBehaviour
{
    public const float PortraitWidth = 1240f;
    private const float LandscapeInset = 120f, PortraitInset = 24f;
    private const float UnderHud = 240f; // top-centre banners drop below the HUD's two top columns
    private const float MaxPortraitScale = 1.7f;

    public static bool Portrait => Screen.height > Screen.width;

    private CanvasScaler scaler;
    private Vector2 landscapeReference;
    private float landscapeMatch;
    private bool? applied;
    private Rect appliedSafe;
    private readonly List<(RectTransform rt, Vector2 position)> pinned = new List<(RectTransform, Vector2)>();
    private readonly List<RectTransform> cards = new List<RectTransform>();

    // ── Automatic attachment: every canvas with a 1920x1080 scaler gets one ─────────────────────────
    private static float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Canvas.preWillRenderCanvases -= Scan;
        Canvas.preWillRenderCanvases += Scan;
        nextScan = 0f;
    }

    private static void Scan()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.25f;
        foreach (var s in FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            if (s.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && s.GetComponent<ScreenFit>() == null)
                s.gameObject.AddComponent<ScreenFit>();
    }

    // A window card (UiKit.Window) that should shrink to fit the screen width in portrait
    public static void FitCard(RectTransform card)
    {
        var canvas = card.GetComponentInParent<CanvasScaler>(true);
        if (canvas == null) return;
        var fit = canvas.GetComponent<ScreenFit>();
        if (fit == null) fit = canvas.gameObject.AddComponent<ScreenFit>();
        fit.cards.Add(card);
        fit.applied = null;
    }

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        landscapeReference = scaler.referenceResolution;
        landscapeMatch = scaler.matchWidthOrHeight;
    }

    private void Update()
    {
        bool portrait = Portrait;
        if (applied == portrait && appliedSafe == Screen.safeArea) return;
        applied = portrait;
        appliedSafe = Screen.safeArea;
        Apply(portrait);
    }

    private void Apply(bool portrait)
    {
        if (portrait)
        {
            scaler.referenceResolution = new Vector2(PortraitWidth, PortraitWidth * 16f / 9f);
            scaler.matchWidthOrHeight = 0f;
        }
        else
        {
            scaler.referenceResolution = landscapeReference;
            scaler.matchWidthOrHeight = landscapeMatch;
        }

        // Canvas units per screen pixel, from the scaler settings just applied
        float unitsPerPixel = portrait ? PortraitWidth / Mathf.Max(1, Screen.width) : 1f;
        PinEdges(portrait, unitsPerPixel);

        // Windows built for portrait (UiKit.Portrait, ~760 wide) are scaled up to fill the width, so
        // their text reads at phone size; wide landscape windows still shrink to fit.
        float width = PortraitWidth - 40f;
        float height = Screen.height * unitsPerPixel * 0.92f;
        cards.RemoveAll(c => c == null);
        foreach (var card in cards)
        {
            float scale = portrait ? Mathf.Min(MaxPortraitScale, width / card.sizeDelta.x, height / card.sizeDelta.y) : 1f;
            card.localScale = Vector3.one * scale;
        }
    }

    // Direct children pinned to a screen edge: in portrait the landscape side inset (clear of the notch)
    // shrinks to a small margin, and top/bottom pieces step inside the status bar and home indicator.
    private void PinEdges(bool portrait, float unitsPerPixel)
    {
        if (pinned.Count == 0)
            foreach (Transform child in transform)
            {
                var rt = child as RectTransform;
                if (rt == null || rt.anchorMin != rt.anchorMax) continue; // stretched (backdrops, touch zones)
                if (rt.anchorMin.x == 0.5f && rt.anchorMin.y == 0.5f) continue;
                pinned.Add((rt, rt.anchoredPosition));
            }

        var safe = Screen.safeArea;
        float top = (Screen.height - safe.yMax) * unitsPerPixel, bottom = safe.yMin * unitsPerPixel;
        foreach (var (rt, position) in pinned)
        {
            if (rt == null) continue;
            if (!portrait) { rt.anchoredPosition = position; continue; }
            var p = position;
            if (rt.anchorMin.x == 0f || rt.anchorMin.x == 1f)
            {
                float side = Mathf.Abs(p.x);
                if (side >= LandscapeInset) side -= LandscapeInset - PortraitInset;
                p.x = Mathf.Sign(p.x) * side;
            }
            if (rt.anchorMin.y == 1f) p.y -= top + (rt.anchorMin.x == 0.5f ? UnderHud : 0f);
            else if (rt.anchorMin.y == 0f) p.y += bottom;
            rt.anchoredPosition = p;
        }
    }
}
