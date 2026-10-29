using UnityEngine;

// Night lights on a city tower (CitySkyline), kept soft like a real skyline after dark:
//  · the rooms — facade pieces whose texture has, per floor, a strip of wall and a band of window
//    per room (window, window, pier); about half the rooms are lit, warm at home or cool white in
//    offices, and now and then somebody switches a light on or off. Pieces of a stepped or twisted
//    tower each show their own floors (a slice of the texture);
//  · accents on some towers only: a crown or edge lines in one colour, steady or slowly breathing,
//    a gentle rainbow or chase on a few, a media facade of colour waves on one or two;
//  · red aircraft beacons blinking on the tall roofs, and on the landmark searchlights in the sky.
// Everything sits in the NightLights group, shown only at night (DayNightCycle's ambient).
public class TowerLights : MonoBehaviour
{
    public enum Show { None, Steady, Breathe, Rainbow, Chase, Media }

    private const int RowsPerFloor = 3, ColsPerRoom = 3;

    [SerializeField] private GameObject nightGroup;
    [SerializeField] private Renderer[] facades;
    [SerializeField] private int[] facadeFirstFloor, facadeFloors;
    [SerializeField] private int rooms = 5, floors = 12;
    [SerializeField] private bool stone;                // solid walls between windows (else dark glass)
    [SerializeField] private Renderer[] accents;
    [SerializeField] private Color accentColor = Color.white;
    [SerializeField] private Renderer[] beacons;
    [SerializeField] private Transform[] beams;
    [SerializeField] private Show show = Show.None;
    [SerializeField] private int seed = 1;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    private Texture2D texture;
    private Color32[] pixels;
    private bool[] lit;
    private MaterialPropertyBlock block;
    private System.Random rng;
    private bool night, ready;
    private float nextCheck, nextFlip, nextMedia;

    public void Configure(GameObject group, Renderer[] faces, int[] firstFloor, int[] floorCount, int roomsAcross, int floorCount2,
        bool solidWalls, Renderer[] accentParts, Color accent, Renderer[] redBeacons, Transform[] searchlights, Show style, int randomSeed)
    {
        nightGroup = group;
        facades = faces;
        facadeFirstFloor = firstFloor;
        facadeFloors = floorCount;
        rooms = roomsAcross;
        floors = floorCount2;
        stone = solidWalls;
        accents = accentParts;
        accentColor = accent;
        beacons = redBeacons;
        beams = searchlights;
        show = style;
        seed = randomSeed;
    }

    private void Awake()
    {
        Prepare();
        if (nightGroup != null) nightGroup.SetActive(false);
    }

    private void Prepare()
    {
        if (ready) return;
        ready = true;
        rng = new System.Random(seed);
        block = new MaterialPropertyBlock();
        if (facades == null || facades.Length == 0) return;
        int w = rooms * ColsPerRoom, h = floors * RowsPerFloor;
        texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Rooms" };
        pixels = new Color32[w * h];
        lit = new bool[rooms * floors];
        for (int f = 0; f < floors; f++)
            for (int r = 0; r < rooms; r++) SetRoom(f, r, rng.NextDouble() < (f == 0 ? 1.0 : 0.5));
        Upload();
        for (int i = 0; i < facades.Length; i++)
        {
            var face = facades[i];
            if (face == null) continue;
            int first = facadeFirstFloor != null && i < facadeFirstFloor.Length ? facadeFirstFloor[i] : 0;
            int count = facadeFloors != null && i < facadeFloors.Length ? facadeFloors[i] : floors;
            face.GetPropertyBlock(block);
            block.SetTexture(BaseMap, texture);
            block.SetVector(BaseMapST, new Vector4(1f, count / (float)floors, 0f, first / (float)floors));
            block.SetColor(BaseColor, Color.white);
            face.SetPropertyBlock(block);
        }
    }

    private void OnDestroy()
    {
        if (texture != null) { if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); }
    }

    // A room's pixels: wall strip under the window, two window columns and a pier
    private void SetRoom(int floor, int room, bool on)
    {
        lit[floor * rooms + room] = on;
        // Kept low: the game's grading (exposure +0.45, saturation, bloom) brightens everything a lot
        float k = 0.28f + (float)rng.NextDouble() * 0.22f;
        double kind = rng.NextDouble();
        Color window = !on ? new Color(0.015f, 0.018f, 0.028f)
            : floor == 0 ? new Color(0.5f, 0.42f, 0.3f)
            : kind < 0.55 ? new Color(1f, 0.74f, 0.42f) * k      // warm home light
            : kind < 0.93 ? new Color(0.78f, 0.86f, 1f) * k * 0.9f // office white
            : new Color(0.3f, 0.45f, 0.95f) * k * 0.8f;          // TV
        Color wall = stone ? new Color(0.035f, 0.032f, 0.03f) : new Color(0.01f, 0.012f, 0.018f);
        Color pier = stone ? new Color(0.04f, 0.037f, 0.034f) : new Color(0.012f, 0.014f, 0.02f);
        int w = rooms * ColsPerRoom;
        for (int dy = 0; dy < RowsPerFloor; dy++)
            for (int dx = 0; dx < ColsPerRoom; dx++)
            {
                Color c = dy == 0 ? wall : dx == ColsPerRoom - 1 ? pier : window;
                pixels[(floor * RowsPerFloor + dy) * w + room * ColsPerRoom + dx] = c;
            }
    }

    private void Upload()
    {
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 1.5f + (seed % 7) * 0.1f;
            bool dark = RenderSettings.ambientIntensity < 0.8f;
            if (dark != night)
            {
                night = dark;
                if (nightGroup != null) nightGroup.SetActive(night);
            }
        }
        if (night) Animate(Time.time, true);
    }

    // Editor previews: show the tower as it looks at night at time t (or by day)
    public void PreviewNight(float t)
    {
        Prepare();
        if (nightGroup != null) nightGroup.SetActive(true);
        Animate(t, false);
    }

    public void PreviewDay()
    {
        if (nightGroup != null) nightGroup.SetActive(false);
    }

    private void Animate(float t, bool live)
    {
        if (texture != null && show != Show.Media && live && t >= nextFlip) // somebody switches a light
        {
            nextFlip = t + 1f + (float)rng.NextDouble() * 3f;
            int f = 1 + rng.Next(Mathf.Max(1, floors - 1)), r = rng.Next(rooms);
            SetRoom(f, r, !lit[f * rooms + r]);
            Upload();
        }
        if (texture != null && show == Show.Media && (!live || t >= nextMedia))
        {
            nextMedia = t + 0.1f;
            int w = rooms * ColsPerRoom, h = floors * RowsPerFloor;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (y % RowsPerFloor == 0) { pixels[y * w + x] = new Color(0.015f, 0.018f, 0.025f); continue; }
                    float hue = Mathf.Repeat(0.55f + t * 0.03f + y * 0.006f + Mathf.Sin(x * 0.25f + t * 0.8f) * 0.05f, 1f);
                    float v = 0.08f + 0.2f * (0.5f + 0.5f * Mathf.Sin(y * 0.18f - t * 1.6f + x * 0.12f));
                    pixels[y * w + x] = Color.HSVToRGB(hue, 0.7f, v);
                }
            Upload();
        }

        if (accents != null)
            for (int i = 0; i < accents.Length; i++)
            {
                var r = accents[i];
                if (r == null) continue;
                Color c;
                switch (show)
                {
                    case Show.Breathe:
                        c = accentColor * (0.35f + 0.2f * Mathf.Sin(t * 0.9f + seed));
                        break;
                    case Show.Rainbow:
                        c = Color.HSVToRGB(Mathf.Repeat(t * 0.025f + i * 0.04f + seed * 0.1f, 1f), 0.5f, 0.5f);
                        break;
                    case Show.Chase:
                        float p = Mathf.Repeat(t * 0.35f - i / (float)Mathf.Max(1, accents.Length), 1f);
                        c = accentColor * (0.15f + 0.55f * Mathf.Pow(1f - p, 5f));
                        break;
                    default:
                        c = accentColor * 0.5f;
                        break;
                }
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, c);
                r.SetPropertyBlock(block);
            }

        if (beacons != null) // aircraft warning lights: a short red blink every 1.6 s
        {
            bool on = Mathf.Repeat(t + seed * 0.37f, 1.6f) < 0.25f;
            foreach (var b in beacons)
            {
                if (b == null) continue;
                b.GetPropertyBlock(block);
                block.SetColor(BaseColor, on ? new Color(1.1f, 0.06f, 0.05f) : new Color(0.08f, 0.01f, 0.01f));
                b.SetPropertyBlock(block);
            }
        }

        if (beams != null)
            for (int i = 0; i < beams.Length; i++)
            {
                if (beams[i] == null) continue;
                float a = t * 0.3f + i * 1.7f;
                float yaw = i * (360f / beams.Length) + t * 8f + Mathf.Sin(a * 0.6f) * 30f;
                float tilt = 14f + Mathf.Sin(a) * 10f;
                beams[i].localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
            }
    }
}
