using UnityEngine;
using UnityEngine.UI;

// The loading mark: a ดอกพิกุล turning slowly on a soft golden glow, breathing a little as it turns.
// Runs on unscaled time (it spins while the game is paused or a scene loads).
public class PikulSpinner : MonoBehaviour
{
    private RectTransform flower, halo;
    private float phase;

    public static RectTransform Make(Transform parent, Vector2 position, float size, bool glow = true)
    {
        var root = UiKit.Rect("Pikul", parent, new Color(0f, 0f, 0f, 0f));
        root.sizeDelta = new Vector2(size, size);
        root.anchoredPosition = position;
        root.GetComponent<Image>().raycastTarget = false;
        var spinner = root.gameObject.AddComponent<PikulSpinner>();
        if (glow)
        {
            spinner.halo = UiKit.Rect("Glow", root, new Color(1f, 0.82f, 0.35f, 0.55f));
            spinner.halo.sizeDelta = new Vector2(size * 1.7f, size * 1.7f);
            var hi = spinner.halo.GetComponent<Image>();
            hi.sprite = PikulArt.Glow;
            hi.raycastTarget = false;
        }
        spinner.flower = UiKit.Rect("Flower", root, Color.white);
        spinner.flower.sizeDelta = new Vector2(size, size);
        var fi = spinner.flower.GetComponent<Image>();
        fi.sprite = PikulArt.Pikul;
        fi.raycastTarget = false;
        spinner.phase = Random.value * 6f;
        return root;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        phase += dt;
        if (flower != null)
        {
            flower.Rotate(0f, 0f, -110f * dt);
            float s = 1f + 0.06f * Mathf.Sin(phase * 3.2f);
            flower.localScale = new Vector3(s, s, 1f);
        }
        if (halo != null)
        {
            float g = 0.9f + 0.15f * Mathf.Sin(phase * 2.1f);
            halo.localScale = new Vector3(g, g, 1f);
        }
    }
}
