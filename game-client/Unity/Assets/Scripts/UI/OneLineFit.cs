using UnityEngine;
using UnityEngine.UI;

// Keeps a label on ONE line: the text never wraps into a second line that would run into the label
// under it (window titles over their subtitles); a text too long for the box shrinks a little instead,
// down to a readable minimum. Re-measures whenever the text (or the language) changes.
[RequireComponent(typeof(Text))]
public class OneLineFit : MonoBehaviour
{
    private Text label;
    private int maxSize, minSize;
    private string fitted;
    private float fittedWidth;

    public static void Apply(Text t, int min)
    {
        var f = t.GetComponent<OneLineFit>();
        if (f == null) f = t.gameObject.AddComponent<OneLineFit>();
        f.label = t;
        f.maxSize = t.fontSize;
        f.minSize = Mathf.Min(min, t.fontSize);
        t.resizeTextForBestFit = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        f.fitted = null;
    }

    private void LateUpdate()
    {
        if (label == null) return;
        float width = label.rectTransform.rect.width;
        if (label.text == fitted && Mathf.Approximately(width, fittedWidth)) return;
        fitted = label.text;
        fittedWidth = width;
        label.fontSize = maxSize;
        while (label.fontSize > minSize && label.preferredWidth > width) label.fontSize -= 1;
    }
}
