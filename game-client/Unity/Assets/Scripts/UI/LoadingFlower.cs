using UnityEngine;
using UnityEngine.UI;

// A turning ดอกพิกุล in front of a "กำลังโหลด…" line in a window: shown while the line says it's
// loading, gone as soon as the text changes to what was loaded. LoadingFlower.Show(text) sets the
// line and puts the flower there (once per Text).
public class LoadingFlower : MonoBehaviour
{
    private Text text;
    private RectTransform mark;

    public static Text Show(Text t, string message = "กำลังโหลด…")
    {
        if (t == null) return null;
        t.text = message;
        var f = t.GetComponent<LoadingFlower>();
        if (f == null)
        {
            f = t.gameObject.AddComponent<LoadingFlower>();
            f.text = t;
            float size = Mathf.Clamp(t.fontSize * 1.25f, 24f, 56f);
            f.mark = PikulSpinner.Make(t.transform, Vector2.zero, size, glow: false);
        }
        f.Place();
        return t;
    }

    private void LateUpdate()
    {
        if (text == null || mark == null) return;
        bool on = text.text.Contains("กำลังโหลด") && text.isActiveAndEnabled;
        if (mark.gameObject.activeSelf != on) mark.gameObject.SetActive(on);
        if (on) Place();
    }

    // just before the first letter, wherever the line is aligned
    private void Place()
    {
        var rt = (RectTransform)text.transform;
        float w = rt.rect.width, used = Mathf.Min(text.preferredWidth, w), gap = mark.sizeDelta.x * 0.65f;
        float x;
        switch (text.alignment)
        {
            case TextAnchor.UpperLeft: case TextAnchor.MiddleLeft: case TextAnchor.LowerLeft:
                x = -w * 0.5f - gap; // just outside the left edge
                break;
            case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight:
                x = w * 0.5f - used - gap;
                break;
            default:
                x = -used * 0.5f - gap;
                break;
        }
        mark.anchoredPosition = new Vector2(x + (0.5f - rt.pivot.x) * w, (0.5f - rt.pivot.y) * rt.rect.height);
    }
}
