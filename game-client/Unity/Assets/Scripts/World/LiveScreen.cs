using UnityEngine;

// จอถ่ายทอดสด: a big LED screen showing the royal barge procession live — a small camera follows the
// royal barge (BargeProcession.Lead) and cuts between a side view, the bow close up and a high view
// every few seconds, the way the broadcast does. It only films while someone is near enough to watch,
// a few frames a second, into a small picture, so it costs little. After dark it shows a still card.
public class LiveScreen : MonoBehaviour
{
    [SerializeField] private Renderer screen;
    [SerializeField] private float watchDistance = 70f, fps = 8f, shotLength = 7f;

    private Camera cam;
    private RenderTexture picture;
    private Material material;
    private Color idle;
    private float nextFrame;

    public void Configure(Renderer screenRenderer) => screen = screenRenderer;

    private void Start()
    {
        if (screen == null) { enabled = false; return; }
        picture = new RenderTexture(384, 216, 16) { name = "LiveBargeFeed" };
        material = screen.material; // its own copy
        idle = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.black;
        var go = new GameObject("LiveCamera");
        go.transform.SetParent(transform, false);
        cam = go.AddComponent<Camera>();
        cam.enabled = false;
        cam.targetTexture = picture;
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 260f;
    }

    private void OnDestroy()
    {
        if (picture != null) picture.Release();
    }

    private void Update()
    {
        if (cam == null || Time.time < nextFrame) return;
        nextFrame = Time.time + 1f / fps;
        var viewer = Camera.main;
        if (viewer == null || (viewer.transform.position - screen.transform.position).sqrMagnitude > watchDistance * watchDistance) return;

        var lead = BargeProcession.Lead;
        if (lead == null) { Show(null); return; }

        // cut between three shots
        int shot = (int)(Time.time / shotLength) % 3;
        Vector3 fwd = lead.forward, side = lead.right, at = lead.position + Vector3.up * 2f;
        Vector3 from = shot == 0 ? at + side * -22f + fwd * 6f + Vector3.up * 2f      // alongside, from the bank
                     : shot == 1 ? at + fwd * 20f + side * -7f + Vector3.up * 1f      // the bow coming on
                     : at + side * -30f - fwd * 20f + Vector3.up * 22f;              // high, the columns behind
        Vector3 look = shot == 1 ? at + fwd * 12f : at;
        cam.transform.position = from;
        cam.transform.rotation = Quaternion.LookRotation(look - from, Vector3.up);
        cam.Render();
        Show(picture);
    }

    private void Show(Texture feed)
    {
        if (feed != null)
        {
            material.SetTexture("_BaseMap", feed);
            material.SetColor("_BaseColor", Color.white);
        }
        else
        {
            material.SetTexture("_BaseMap", null);
            material.SetColor("_BaseColor", idle);
        }
    }
}
