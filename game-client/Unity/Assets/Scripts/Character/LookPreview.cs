using UnityEngine;

// The turntable behind the character creator and the barber / clothes shop windows: a copy of the
// character on a little stage far below the world, filmed by its own camera into a RenderTexture
// that the window shows. Only that camera sees the stage's layer.
public class LookPreview : MonoBehaviour
{
    public const int Layer = 31;

    private Camera cam;
    private Transform turntable;
    private CharacterModel model;
    private string shown;
    private float spin;

    public RenderTexture Texture { get; private set; }

    public static LookPreview Create()
    {
        var root = new GameObject("LookPreviewStage");
        root.transform.position = new Vector3(0f, -500f, 0f);
        return root.AddComponent<LookPreview>();
    }

    private void Awake()
    {
        Texture = new RenderTexture(640, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "LookPreview" };

        turntable = new GameObject("Turntable").transform;
        turntable.SetParent(transform, false);
        var host = new GameObject("Character");
        host.transform.SetParent(turntable, false);
        host.transform.localPosition = new Vector3(0f, 1f, 0f); // CharacterModel stands the feet 1 m below its origin
        model = host.AddComponent<CharacterModel>();

        var camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.05f, 3.3f);
        camGo.transform.LookAt(transform.position + Vector3.up * 0.95f);
        cam = camGo.AddComponent<Camera>();
        cam.cullingMask = 1 << Layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.93f, 0.82f);
        cam.fieldOfView = 34f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        cam.targetTexture = Texture;

        // Soft key and fill lights that only reach the stage
        foreach (var (pos, intensity) in new[] { (new Vector3(1.6f, 2.4f, 2.4f), 6f), (new Vector3(-2f, 1.2f, 1.5f), 2.5f), (new Vector3(0f, 2f, -2f), 3f) })
        {
            var l = new GameObject("PreviewLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = pos;
            l.type = LightType.Point;
            l.range = 8f;
            l.intensity = intensity;
            l.color = new Color(1f, 0.96f, 0.9f);
            l.cullingMask = 1 << Layer;
        }

        // Keep the stage out of the game cameras
        foreach (var c in Camera.allCameras)
            if (c != cam) c.cullingMask &= ~(1 << Layer);
    }

    public void Show(Look look)
    {
        model.SetLook(look);
        shown = null; // re-layer on the next frame (hair is built on the new model)
    }

    // Face the camera for a moment after a change, then turn slowly
    public void Front() => spin = 0f;

    private void LateUpdate()
    {
        spin += Time.unscaledDeltaTime * 22f;
        float yaw = spin < 40f ? 0f : (spin - 40f);
        turntable.localRotation = Quaternion.Euler(0f, Mathf.Sin(yaw * Mathf.Deg2Rad) * 35f, 0f); // faces the camera (+Z)
        if (shown == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            shown = "ok";
        }
    }

    private void OnDestroy()
    {
        if (Texture != null) Texture.Release();
    }
}
