using UnityEngine;

// The outdoor movie (หนังกลางแปลง): a film drawn in code on the screen, frame by frame, the way an old
// projector shows it — grain, scratches, a flickering lamp and hard cuts. The reel goes round:
// the troupe's ดอกพิกุล title, a farmer and his buffalo at sunset, a ghost chasing a man through the
// night, a Muay Thai bout, two lovers under the moon, a night chase with headlights and flashes, then
// "จบ". The screen also lights the crowd in front with the film's colour once it's dark.
// Sits on a Quad facing the audience (the Quad's -Z side); OutdoorCinema builds it.
public class MovieScreen : MonoBehaviour
{
    private const int W = 192, H = 96;
    private const float Fps = 12f;
    private static readonly float[] SceneLength = { 6f, 9f, 10f, 9f, 8f, 9f, 3f };

    private Texture2D tex;
    private Color32[] px;
    private Material material;
    private Light glow;
    private float clock, nextFrame;
    private Renderer rend;
    private System.Random grain;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        px = new Color32[W * H];
        grain = new System.Random(transform.position.GetHashCode());
        if (rend != null)
        {
            material = rend.material; // own copy
            material.SetTexture("_BaseMap", tex);
            material.SetTexture("_MainTex", tex);
            material.SetTexture("_EmissionMap", tex);
            material.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));
            material.SetColor("_EmissionColor", Color.white * 1.15f);
            material.EnableKeyword("_EMISSION");
        }
        var lightGo = new GameObject("ScreenGlow");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -0.2f, -1.2f); // out in front, over the crowd
        glow = lightGo.AddComponent<Light>();
        glow.type = LightType.Spot;
        glow.spotAngle = 120f;
        glow.range = Mathf.Max(14f, transform.lossyScale.x * 2.2f);
        glow.shadows = LightShadows.None;
        lightGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // shine back at the audience
    }

    private void OnDestroy()
    {
        if (tex != null) Destroy(tex);
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        clock += Time.deltaTime;
        if (Time.time < nextFrame) return;
        nextFrame = Time.time + 1f / Fps;
        // only draw when someone could see it
        var cam = Camera.main;
        if (rend == null || !rend.isVisible || cam == null || (cam.transform.position - transform.position).sqrMagnitude > 110f * 110f)
        {
            glow.enabled = false;
            return;
        }

        float total = 0f;
        foreach (float s in SceneLength) total += s;
        float t = clock % total;
        int scene = 0;
        while (t > SceneLength[scene]) { t -= SceneLength[scene]; scene++; }
        float k = t / SceneLength[scene];
        switch (scene)
        {
            case 0: Title(t); break;
            case 1: Sunset(t, k); break;
            case 2: GhostChase(t, k); break;
            case 3: Boxing(t); break;
            case 4: Lovers(t, k); break;
            case 5: NightChase(t, k); break;
            default: TheEnd(t); break;
        }
        // a hard white cut between scenes, then the projector's grain, scratches and flicker
        if (t < 0.12f) Fill(new Color32(255, 250, 235, 255));
        Film(t);
        tex.SetPixels32(px);
        tex.Apply(false);

        var avg = Average();
        float flicker = 0.92f + Mathf.PerlinNoise(clock * 7f, 0.3f) * 0.16f;
        material.SetColor("_EmissionColor", Color.white * 1.15f * flicker);
        glow.enabled = DayNightCycle.Dark;
        glow.color = Color.Lerp(avg, Color.white, 0.25f);
        glow.intensity = 3.5f * flicker * (0.4f + avg.grayscale);
    }

    // ── The reel ─────────────────────────────────────────────────────────────────────────
    private void Title(float t)
    {
        VGrad(new Color32(70, 10, 18, 255), new Color32(15, 2, 6, 255));
        // the troupe's flower, turning, with rays behind it
        float spin = t * 40f;
        for (int i = 0; i < 16; i++)
            Ray(W / 2, H / 2 + 6, (spin * 0.3f + i * 22.5f) * Mathf.Deg2Rad, 80, new Color32(120, 40, 30, 255));
        Flower(W / 2, H / 2 + 6, 22, spin, new Color32(250, 205, 70, 255));
        Circle(W / 2, H / 2 + 6, 6, new Color32(200, 120, 30, 255));
        // marquee bulbs along the top and bottom
        for (int x = 4; x < W; x += 8)
        {
            bool on = ((x / 8) + (int)(t * 6f)) % 2 == 0;
            var c = on ? new Color32(255, 230, 120, 255) : new Color32(90, 60, 30, 255);
            Circle(x, 4, 2, c);
            Circle(x, H - 5, 2, c);
        }
        Rect(W / 2 - 46, 12, 92, 8, new Color32(240, 200, 90, 255)); // the name plate
        Rect(W / 2 - 44, 14, 88, 4, new Color32(110, 25, 20, 255));
    }

    private void Sunset(float t, float k)
    {
        VGrad(new Color32(255, 150, 60, 255), new Color32(110, 40, 90, 255), 30, H);
        Circle(W / 2 + 30, 40 + (int)(k * -8f), 13, new Color32(255, 220, 120, 255));
        Rect(0, 0, W, 34, new Color32(40, 90, 30, 255));
        for (int x = 0; x < W; x += 6) Rect(x, 30, 2, 5 + (x * 7 % 5), new Color32(70, 130, 40, 255)); // rice
        // palms on the skyline
        Palm(20, 34, 26); Palm(160, 34, 32); Palm(176, 34, 22);
        // the farmer leads his buffalo across
        int fx = (int)Mathf.Lerp(-30f, W + 30f, k);
        Buffalo(fx - 22, 30, t);
        Person(fx, 30, 22, t * 6f, new Color32(20, 10, 15, 255), hat: true);
    }

    private void GhostChase(float t, float k)
    {
        VGrad(new Color32(20, 30, 70, 255), new Color32(5, 5, 20, 255));
        Circle(30, 74, 10, new Color32(235, 235, 210, 255));
        Rect(0, 0, W, 20, new Color32(10, 18, 12, 255));
        for (int i = 0; i < 6; i++) Tree(10 + i * 36, 20, 30 + (i * 13 % 14));
        int run = (int)Mathf.Lerp(-20f, W + 40f, k);
        Person(run, 20, 22, t * 14f, new Color32(15, 15, 20, 255), arms: true);
        // the ghost floats after him, bobbing, white dress and long black hair
        int gx = run - 34 + (int)(Mathf.Sin(t * 3f) * 4f);
        int gy = 26 + (int)(Mathf.Sin(t * 5f) * 4f);
        Ghost(gx, gy, t);
        // lightning now and then
        if (Mathf.Repeat(t, 3.3f) < 0.12f) Fill(new Color32(200, 210, 255, 255), 0.5f);
    }

    private void Boxing(float t)
    {
        VGrad(new Color32(40, 30, 30, 255), new Color32(10, 8, 8, 255));
        Rect(20, 16, W - 40, 10, new Color32(60, 70, 140, 255)); // the ring floor
        for (int r = 0; r < 3; r++) Rect(20, 32 + r * 8, W - 40, 1, new Color32(220, 40, 40, 255));
        Rect(20, 16, 3, 40, new Color32(200, 200, 200, 255));
        Rect(W - 23, 16, 3, 40, new Color32(200, 200, 200, 255));
        for (int x = 4; x < W; x += 10) Circle(x, 8 + (x * 3 % 4), 5, new Color32(25, 20, 20, 255)); // the crowd
        float bob = Mathf.Sin(t * 8f) * 2f;
        bool kick = Mathf.Repeat(t, 1.6f) < 0.35f;
        Fighter(W / 2 - 18 + (int)bob, 26, new Color32(200, 40, 40, 255), 1, kick);
        Fighter(W / 2 + 18 - (int)bob, 26, new Color32(40, 70, 200, 255), -1, !kick && Mathf.Repeat(t, 2.3f) < 0.3f);
        if (kick && Mathf.Repeat(t, 1.6f) > 0.25f) Star(W / 2, 46, 6, new Color32(255, 240, 120, 255));
    }

    private void Lovers(float t, float k)
    {
        VGrad(new Color32(30, 40, 90, 255), new Color32(10, 10, 35, 255));
        Circle(W / 2, 66, 16, new Color32(250, 245, 215, 255));
        Rect(0, 0, W, 22, new Color32(20, 40, 70, 255)); // the river
        for (int x = 0; x < W; x += 3)
            if ((x + (int)(t * 10f)) % 9 < 2) Rect(x, 8 + (x * 5 % 9), 2, 1, new Color32(200, 200, 160, 255)); // moon on the water
        Rect(0, 22, W, 6, new Color32(15, 25, 15, 255));
        int gap = (int)Mathf.Lerp(50f, 6f, Mathf.Clamp01(k * 1.6f));
        Person(W / 2 - gap, 28, 22, 0f, new Color32(10, 10, 20, 255));
        Person(W / 2 + gap, 28, 20, 0f, new Color32(10, 10, 20, 255), hair: true);
        if (k > 0.65f) Heart(W / 2, 62, 4 + (int)(Mathf.PingPong(t * 6f, 2f)), new Color32(240, 70, 110, 255));
    }

    private void NightChase(float t, float k)
    {
        VGrad(new Color32(15, 15, 30, 255), new Color32(30, 20, 40, 255));
        for (int i = 0; i < 8; i++) // the city behind, lit windows
        {
            int bx = ((i * 29 - (int)(t * 20f)) % (W + 40) + W + 40) % (W + 40) - 20;
            int bh = 24 + i * 7 % 26;
            Rect(bx, 20, 22, bh, new Color32(25, 25, 45, 255));
            for (int wy = 26; wy < 20 + bh - 3; wy += 6)
                for (int wx = bx + 3; wx < bx + 20; wx += 6)
                    if ((wx * 7 + wy * 3 + i) % 5 != 0) Rect(wx, wy, 2, 3, new Color32(240, 200, 90, 255));
        }
        Rect(0, 0, W, 20, new Color32(35, 35, 40, 255));
        for (int x = 0; x < W; x += 16) Rect((x - (int)(t * 120f)) % W + (x - (int)(t * 120f) < 0 ? W : 0), 9, 8, 2, new Color32(230, 230, 230, 255));
        int cx = (int)Mathf.Lerp(-40f, W + 20f, k);
        Car(cx, 12, new Color32(200, 30, 30, 255));
        Car(cx - 60, 12, new Color32(240, 240, 240, 255), siren: Mathf.Repeat(t, 0.4f) < 0.2f);
        if (Mathf.Repeat(t, 2.7f) < 0.25f) Circle(cx + 50, 30, 14, new Color32(255, 170, 60, 255)); // a blast
    }

    private void TheEnd(float t)
    {
        Fill(new Color32(12, 8, 8, 255));
        // "จบ" in a gold frame (letters drawn as bars)
        Rect(W / 2 - 30, H / 2 - 16, 60, 32, new Color32(230, 180, 70, 255));
        Rect(W / 2 - 27, H / 2 - 13, 54, 26, new Color32(30, 10, 10, 255));
        Flower(W / 2, H / 2, 10, t * 60f, new Color32(250, 205, 70, 255));
    }

    // ── Projector ────────────────────────────────────────────────────────────────────────
    private void Film(float t)
    {
        int scratch = grain.Next(W), scratch2 = grain.Next(10) < 3 ? grain.Next(W) : -1;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                var c = px[i];
                int n = grain.Next(-14, 15);
                // vignette: darker at the edges like a real lamp
                float dx = (x - W * 0.5f) / (W * 0.5f), dy = (y - H * 0.5f) / (H * 0.5f);
                float v = 1f - 0.28f * (dx * dx + dy * dy);
                int r = (int)(c.r * v) + n, g = (int)(c.g * v) + n, b = (int)(c.b * v) + n;
                if (x == scratch || x == scratch2) { r += 60; g += 60; b += 50; }
                px[i] = new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
            }
        if (grain.Next(12) == 0) // a fleck of dust
            Circle(grain.Next(W), grain.Next(H), 1 + grain.Next(2), new Color32(20, 15, 10, 255));
    }

    private Color Average()
    {
        int r = 0, g = 0, b = 0, n = 0;
        for (int i = 0; i < px.Length; i += 37) { r += px[i].r; g += px[i].g; b += px[i].b; n++; }
        return new Color(r / (255f * n), g / (255f * n), b / (255f * n));
    }

    // ── Drawing ──────────────────────────────────────────────────────────────────────────
    private void Put(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        px[y * W + x] = c;
    }

    private void Fill(Color32 c, float amount = 1f)
    {
        for (int i = 0; i < px.Length; i++) px[i] = amount >= 1f ? c : Color32.Lerp(px[i], c, amount);
    }

    private void VGrad(Color32 bottom, Color32 top, int from = 0, int to = H)
    {
        for (int y = from; y < to; y++)
        {
            var c = Color32.Lerp(bottom, top, (y - from) / (float)Mathf.Max(1, to - from - 1));
            for (int x = 0; x < W; x++) px[y * W + x] = c;
        }
    }

    private void Rect(int x0, int y0, int w, int h, Color32 c)
    {
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(H, y0 + h); y++)
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(W, x0 + w); x++)
                px[y * W + x] = c;
    }

    private void Circle(int cx, int cy, int r, Color32 c)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) Put(cx + x, cy + y, c);
    }

    private void Ellipse(int cx, int cy, int rx, int ry, Color32 c)
    {
        for (int y = -ry; y <= ry; y++)
            for (int x = -rx; x <= rx; x++)
                if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) <= 1f) Put(cx + x, cy + y, c);
    }

    private void Line(int x0, int y0, int x1, int y1, Color32 c, int thick = 1)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (int s = 0; s <= steps; s++)
        {
            int x = x0 + (x1 - x0) * s / steps, y = y0 + (y1 - y0) * s / steps;
            for (int a = 0; a < thick; a++) for (int b = 0; b < thick; b++) Put(x + a, y + b, c);
        }
    }

    private void Ray(int cx, int cy, float angle, int len, Color32 c)
    {
        for (int s = 8; s < len; s++)
        {
            float w = s * 0.12f;
            for (int q = -(int)w; q <= (int)w; q++)
                Put(cx + (int)(Mathf.Cos(angle) * s - Mathf.Sin(angle) * q * 0.5f), cy + (int)(Mathf.Sin(angle) * s + Mathf.Cos(angle) * q * 0.5f), c);
        }
    }

    private void Flower(int cx, int cy, int r, float spinDeg, Color32 c)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = (spinDeg + i * 72f) * Mathf.Deg2Rad;
            Circle(cx + (int)(Mathf.Cos(a) * r * 0.55f), cy + (int)(Mathf.Sin(a) * r * 0.55f), (int)(r * 0.48f), c);
        }
    }

    private void Person(int x, int y, int h, float phase, Color32 c, bool hat = false, bool arms = false, bool hair = false)
    {
        int leg = h * 2 / 5, body = h * 2 / 5, head = Mathf.Max(2, h / 7);
        float swing = Mathf.Sin(phase) * leg * 0.45f;
        Line(x, y + leg, x - (int)swing, y, c, 2);
        Line(x, y + leg, x + (int)swing, y, c, 2);
        Rect(x - 2, y + leg, 4, body, c);
        Circle(x, y + leg + body + head, head, c);
        if (hat) Rect(x - head - 2, y + leg + body + head * 2 - 1, head * 2 + 5, 2, c);
        if (hair) Rect(x - head, y + leg + body, head * 2, head + 2, c);
        if (arms) { Line(x, y + leg + body - 2, x + 6, y + leg + body + 4, c); Line(x, y + leg + body - 2, x - 6, y + leg + body - 6 - (int)swing, c); }
        else Line(x, y + leg + body - 2, x + (int)(swing * 0.6f), y + leg + 2, c);
    }

    private void Fighter(int x, int y, Color32 shorts, int facing, bool kick)
    {
        var skin = new Color32(40, 25, 20, 255);
        Rect(x - 3, y + 12, 6, 12, skin);
        Rect(x - 3, y + 8, 6, 5, shorts);
        Circle(x, y + 28, 4, skin);
        Line(x, y + 8, x - 4 * facing, y, skin, 2);
        if (kick) Line(x, y + 9, x + 16 * facing, y + 18, skin, 2);
        else Line(x, y + 8, x + 3 * facing, y, skin, 2);
        Line(x, y + 22, x + 9 * facing, y + 24, skin, 2);
        Circle(x + 10 * facing, y + 24, 2, new Color32(200, 20, 20, 255)); // glove
    }

    private void Ghost(int x, int y, float t)
    {
        var white = new Color32(235, 240, 245, 255);
        Ellipse(x, y + 10, 7, 14, white);
        for (int i = -6; i <= 6; i += 3) Rect(x + i, y - 4 - (int)(Mathf.Abs(Mathf.Sin(t * 6f + i)) * 3f), 2, 4, white);
        Circle(x, y + 28, 6, new Color32(220, 225, 230, 255));
        Rect(x - 7, y + 14, 14, 18, new Color32(8, 8, 10, 255)); // long black hair
        Rect(x - 4, y + 22, 8, 10, new Color32(220, 225, 230, 255));
        Put(x - 2, y + 28, new Color32(200, 0, 0, 255));
        Put(x + 2, y + 28, new Color32(200, 0, 0, 255));
        Line(x + 6, y + 18, x + 16, y + 22, white, 2); // reaching out
    }

    private void Buffalo(int x, int y, float t)
    {
        var c = new Color32(30, 25, 30, 255);
        Ellipse(x, y + 10, 14, 7, c);
        Circle(x + 15, y + 12, 4, c);
        Line(x + 14, y + 16, x + 10, y + 20, c);
        Line(x + 17, y + 16, x + 22, y + 19, c);
        float s = Mathf.Sin(t * 5f) * 2f;
        for (int i = -1; i <= 1; i += 2)
        {
            Line(x + 9 * i, y + 5, x + 9 * i + (int)s, y, c, 2);
            Line(x + 6 * i, y + 5, x + 6 * i - (int)s, y, c, 2);
        }
    }

    private void Palm(int x, int y, int h)
    {
        var c = new Color32(30, 15, 30, 255);
        Line(x, y, x + 3, y + h, c, 2);
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 20f) * Mathf.Deg2Rad;
            Line(x + 3, y + h, x + 3 + (int)(Mathf.Cos(a) * 10f), y + h + (int)(Mathf.Sin(a) * 5f) - 2, c, 1);
        }
    }

    private void Tree(int x, int y, int h)
    {
        var c = new Color32(5, 10, 8, 255);
        Rect(x - 1, y, 3, h / 2, c);
        Circle(x, y + h * 2 / 3, h / 3, c);
    }

    private void Car(int x, int y, Color32 body, bool siren = false)
    {
        Rect(x, y, 34, 7, body);
        Rect(x + 7, y + 7, 18, 6, body);
        Rect(x + 9, y + 8, 6, 4, new Color32(140, 180, 220, 255));
        Rect(x + 17, y + 8, 6, 4, new Color32(140, 180, 220, 255));
        Circle(x + 7, y, 3, new Color32(10, 10, 10, 255));
        Circle(x + 27, y, 3, new Color32(10, 10, 10, 255));
        for (int s = 0; s < 30; s++) // the headlight beam
            for (int q = -s / 5; q <= s / 5; q++)
                Put(x + 35 + s, y + 3 + q, Color32.Lerp(new Color32(255, 250, 200, 255), px[Mathf.Clamp((y + 3 + q), 0, H - 1) * W + Mathf.Clamp(x + 35 + s, 0, W - 1)], s / 30f));
        if (siren)
        {
            Rect(x + 12, y + 13, 4, 2, new Color32(255, 30, 30, 255));
            Rect(x + 16, y + 13, 4, 2, new Color32(40, 80, 255, 255));
        }
    }

    private void Heart(int cx, int cy, int r, Color32 c)
    {
        Circle(cx - r / 2 - 1, cy, r / 2 + 1, c);
        Circle(cx + r / 2 + 1, cy, r / 2 + 1, c);
        for (int y = 0; y <= r + 1; y++) Rect(cx - r - 1 + y, cy - y, (r + 1 - y) * 2 + 1, 1, c);
    }

    private void Star(int cx, int cy, int r, Color32 c)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Line(cx, cy, cx + (int)(Mathf.Cos(a) * r * (i % 2 == 0 ? 1f : 0.5f)), cy + (int)(Mathf.Sin(a) * r * (i % 2 == 0 ? 1f : 0.5f)), c);
        }
    }
}
