using System.Collections.Generic;
using UnityEngine;

// Painted fish for the catch cards (การ์ดปลา): each species is drawn pixel by pixel from a short
// description of its real shape and colours — body outline, back-to-belly shading, scales, lateral
// line, gill cover, eye, the fins with their rays, the tail (forked, rounded, ...), and its marks
// (the dark bars of ปลานิล, the blotches of ปลาช่อน, the spots of ปลาเก๋า, the whiskers of ปลาดุก).
// Made once per species (512×256, transparent) and kept.
public static class FishArt
{
    public const int W = 512, H = 256;

    public class Spec
    {
        public string id, name;
        public float minKg, maxKg;
        public bool rare;
        // shape (in fish lengths)
        public float depth = 0.16f, peak = 0.35f, blunt = 0.7f, stalk = 0.045f, bodyEnd = 0.8f;
        public float tailSpread = 0.16f, fork = 0.5f, round = 0f;
        public float dorsalFrom = 0.3f, dorsalTo = 0.6f, dorsalH = 0.08f; public bool spiny;
        public float analFrom = 0.55f, analTo = 0.72f, analH = 0.06f;
        public float eye = 0.022f, eyeAt = 0.075f;
        public float scale = 0.035f, scaleShow = 0.35f;
        // colours
        public Color back, side, belly, fin, mark;
        public Color finEdge = new Color(0f, 0f, 0f, 0f);
        public int bars; public float barsFrom = 0.25f, barsTo = 0.78f;
        public float spots, spotSize = 0.012f;
        public float blotches;
        public bool barbels, wavy;
    }

    private static readonly Dictionary<string, Spec> specs = new Dictionary<string, Spec>();
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    static FishArt()
    {
        Add(new Spec // ปลานิล: deep grey-green body, 8 dark bars, spiny dorsal, red-tinged fins
        {
            id = "tilapia", name = "ปลานิล", minKg = 0.3f, maxKg = 1.2f, depth = 0.2f, peak = 0.4f, blunt = 0.85f, stalk = 0.055f,
            tailSpread = 0.15f, fork = 0.05f, round = 0.6f, dorsalFrom = 0.25f, dorsalTo = 0.74f, dorsalH = 0.075f, spiny = true,
            analFrom = 0.58f, analTo = 0.74f, analH = 0.07f, scale = 0.04f, scaleShow = 0.4f,
            back = C(0.24f, 0.3f, 0.3f), side = C(0.5f, 0.56f, 0.55f), belly = C(0.86f, 0.86f, 0.8f),
            fin = C(0.52f, 0.42f, 0.44f), finEdge = C(0.75f, 0.25f, 0.2f), mark = C(0.18f, 0.22f, 0.24f), bars = 8,
        });
        Add(new Spec // ปลาตะเพียน: silver-gold, big scales, orange-red fins, deep fork
        {
            id = "barb", name = "ปลาตะเพียน", minKg = 0.2f, maxKg = 0.8f, depth = 0.19f, peak = 0.42f, blunt = 0.8f, stalk = 0.045f,
            tailSpread = 0.18f, fork = 0.75f, dorsalFrom = 0.36f, dorsalTo = 0.52f, dorsalH = 0.13f,
            analFrom = 0.62f, analTo = 0.72f, analH = 0.07f, scale = 0.05f, scaleShow = 0.55f,
            back = C(0.42f, 0.42f, 0.3f), side = C(0.85f, 0.8f, 0.6f), belly = C(0.96f, 0.95f, 0.88f),
            fin = C(0.92f, 0.45f, 0.18f), finEdge = C(0.25f, 0.15f, 0.1f),
        });
        Add(new Spec // ปลาช่อน: long round body, flat head, dark blotches, long dorsal & anal, rounded tail
        {
            id = "snakehead", name = "ปลาช่อน", minKg = 0.5f, maxKg = 2.5f, depth = 0.1f, peak = 0.3f, blunt = 0.45f, stalk = 0.065f,
            bodyEnd = 0.84f, tailSpread = 0.1f, fork = 0f, round = 1f, dorsalFrom = 0.3f, dorsalTo = 0.83f, dorsalH = 0.05f,
            analFrom = 0.5f, analTo = 0.83f, analH = 0.045f, eye = 0.017f, eyeAt = 0.06f, scale = 0.03f, scaleShow = 0.3f,
            back = C(0.2f, 0.22f, 0.16f), side = C(0.42f, 0.42f, 0.3f), belly = C(0.82f, 0.8f, 0.68f),
            fin = C(0.3f, 0.3f, 0.24f), mark = C(0.08f, 0.08f, 0.06f), blotches = 1.4f,
        });
        Add(new Spec // ปลาดุก: smooth dark grey-brown, no scales, whiskers, very long fins, rounded tail
        {
            id = "catfish", name = "ปลาดุก", minKg = 0.4f, maxKg = 1.8f, depth = 0.1f, peak = 0.25f, blunt = 0.4f, stalk = 0.06f,
            bodyEnd = 0.85f, tailSpread = 0.09f, fork = 0f, round = 1f, dorsalFrom = 0.28f, dorsalTo = 0.84f, dorsalH = 0.04f,
            analFrom = 0.45f, analTo = 0.84f, analH = 0.04f, eye = 0.013f, eyeAt = 0.07f, scaleShow = 0f,
            back = C(0.16f, 0.15f, 0.14f), side = C(0.36f, 0.33f, 0.3f), belly = C(0.72f, 0.68f, 0.6f),
            fin = C(0.22f, 0.2f, 0.19f), mark = C(0.55f, 0.52f, 0.46f), spots = 0.5f, spotSize = 0.006f, barbels = true,
        });
        Add(new Spec // ปลาทู: small, streamlined, blue-green back, silver sides, thin dark lines, forked tail
        {
            id = "mackerel", name = "ปลาทู", minKg = 0.1f, maxKg = 0.35f, depth = 0.13f, peak = 0.4f, blunt = 1f, stalk = 0.022f,
            tailSpread = 0.15f, fork = 0.85f, dorsalFrom = 0.3f, dorsalTo = 0.42f, dorsalH = 0.08f,
            analFrom = 0.6f, analTo = 0.7f, analH = 0.04f, scale = 0.025f, scaleShow = 0.15f,
            back = C(0.15f, 0.36f, 0.42f), side = C(0.62f, 0.7f, 0.72f), belly = C(0.94f, 0.95f, 0.95f),
            fin = C(0.6f, 0.62f, 0.55f), mark = C(0.1f, 0.22f, 0.28f), wavy = true,
        });
        Add(new Spec // ปลาหมึก — drawn on its own
        {
            id = "squid", name = "ปลาหมึก", minKg = 0.2f, maxKg = 0.8f, rare = false,
            back = C(0.85f, 0.55f, 0.55f), side = C(0.95f, 0.82f, 0.78f), belly = C(0.98f, 0.94f, 0.9f), fin = C(0.9f, 0.7f, 0.7f), mark = C(0.55f, 0.18f, 0.2f),
        });
        Add(new Spec // ปลากะพงขาว: silver with grey back, big mouth, spiny dorsal, broad rounded-straight tail
        {
            id = "seabass", name = "ปลากะพงขาว", minKg = 0.8f, maxKg = 3f, rare = true, depth = 0.15f, peak = 0.38f, blunt = 1.05f, stalk = 0.05f,
            tailSpread = 0.15f, fork = 0.1f, round = 0.5f, dorsalFrom = 0.28f, dorsalTo = 0.72f, dorsalH = 0.08f, spiny = true,
            analFrom = 0.6f, analTo = 0.72f, analH = 0.065f, eye = 0.02f, scale = 0.03f, scaleShow = 0.35f,
            back = C(0.36f, 0.4f, 0.42f), side = C(0.78f, 0.8f, 0.8f), belly = C(0.96f, 0.96f, 0.95f),
            fin = C(0.55f, 0.55f, 0.52f), finEdge = C(0.3f, 0.3f, 0.3f),
        });
        Add(new Spec // ปลาเก๋า: heavy brown body covered in orange-brown spots, rounded tail
        {
            id = "grouper", name = "ปลาเก๋า", minKg = 1f, maxKg = 4f, rare = true, depth = 0.18f, peak = 0.4f, blunt = 0.9f, stalk = 0.06f,
            tailSpread = 0.14f, fork = 0f, round = 1f, dorsalFrom = 0.28f, dorsalTo = 0.76f, dorsalH = 0.065f, spiny = true,
            analFrom = 0.6f, analTo = 0.76f, analH = 0.07f, eye = 0.018f, scale = 0.02f, scaleShow = 0.15f,
            back = C(0.32f, 0.26f, 0.18f), side = C(0.55f, 0.46f, 0.32f), belly = C(0.82f, 0.74f, 0.6f),
            fin = C(0.42f, 0.34f, 0.24f), mark = C(0.55f, 0.24f, 0.1f), spots = 1f, spotSize = 0.014f, blotches = 0.5f,
        });
        Add(new Spec // ปลาอินทรี: long, silver-blue with wavy bars, sickle tail
        {
            id = "kingfish", name = "ปลาอินทรี", minKg = 2f, maxKg = 6f, rare = true, depth = 0.11f, peak = 0.42f, blunt = 1.15f, stalk = 0.02f,
            bodyEnd = 0.82f, tailSpread = 0.17f, fork = 0.95f, dorsalFrom = 0.25f, dorsalTo = 0.4f, dorsalH = 0.06f, spiny = true,
            analFrom = 0.6f, analTo = 0.72f, analH = 0.04f, eye = 0.016f, eyeAt = 0.07f, scaleShow = 0f,
            back = C(0.2f, 0.32f, 0.48f), side = C(0.72f, 0.76f, 0.82f), belly = C(0.95f, 0.96f, 0.97f),
            fin = C(0.4f, 0.45f, 0.52f), mark = C(0.25f, 0.35f, 0.5f), bars = 14, barsFrom = 0.2f, barsTo = 0.8f, wavy = true,
        });
    }

    private static void Add(Spec s) => specs[s.id] = s;
    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    public static Spec Find(string id) => id != null && specs.TryGetValue(id, out var s) ? s : null;

    // 1–3 stars: how big this one is for its kind (a better rod can push past the usual biggest)
    public static int Stars(string id, float kg)
    {
        var s = Find(id);
        if (s == null) return 1;
        float t = (kg - s.minKg) / Mathf.Max(0.01f, s.maxKg - s.minKg);
        return t >= 0.75f ? 3 : t >= 0.4f ? 2 : 1;
    }

    public static Sprite Get(string id)
    {
        if (id == null) return null;
        if (sprites.TryGetValue(id, out var cached) && cached != null) return cached;
        var spec = Find(id);
        if (spec == null) return null;
        var px = new Color[W * H];
        if (id == "squid") PaintSquid(spec, px); else Paint(spec, px);
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels(px);
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        sprites[id] = sprite;
        return sprite;
    }

    // ── Fish ─────────────────────────────────────────────────────────────────────────────────
    private const float X0 = 0.05f, LEN = 0.9f; // fish from 5% to 95% of the width

    private static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xffff) / 65535f;
    }

    // Half-height of the body at u (0 = snout, 1 = tail tip); 0 outside the body
    private static float Half(Spec s, float u)
    {
        if (u < 0f || u > s.bodyEnd + 0.05f) return 0f;
        if (u > s.bodyEnd) return s.stalk * (1f - Mathf.Pow((u - s.bodyEnd) / 0.05f, 2f)); // tucks into the tail
        if (u < s.peak) return s.depth * Mathf.Pow(Mathf.Sin(Mathf.PI * 0.5f * u / s.peak), s.blunt);
        float t = Mathf.Clamp01((u - s.peak) / (s.bodyEnd - s.peak));
        return s.stalk + (s.depth - s.stalk) * 0.5f * (1f + Mathf.Cos(Mathf.PI * t));
    }

    private static void Paint(Spec s, Color[] px)
    {
        float L = W * LEN;                 // pixels per fish length
        float aa = 1f / L;                 // one pixel in fish lengths
        float mid = 0f;
        float tailLen = 1f - s.bodyEnd;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = (x / (float)W - X0) / LEN;
            float v = (y - H * 0.5f) / L; // up is +
            Color outc = new Color(0, 0, 0, 0);

            // Back fins first (the body is painted over them)
            float hb = Half(s, Mathf.Clamp(u, 0f, s.bodyEnd));
            float top = hb * 0.92f + mid, bot = -hb * 1.05f + mid;

            // Tail
            if (u > s.bodyEnd - 0.04f && u < 1.02f)
            {
                float t = Mathf.Max(0f, (u - s.bodyEnd) / tailLen);
                float spread = Mathf.Lerp(s.stalk, s.tailSpread, Mathf.Pow(Mathf.Min(1f, t), 0.7f));
                float av = Mathf.Abs(v - mid);
                float edge = 1f - s.fork * (1f - Mathf.Clamp01(av / s.tailSpread)) * 0.9f;   // forked: the middle stops short
                if (s.round > 0f) edge = Mathf.Lerp(edge, Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(av / s.tailSpread, 2f))), s.round);
                float inside = Mathf.Min((spread - av) / aa, (edge - t) * tailLen / aa);
                if (inside > -1f) Over(ref outc, FinColor(s, u, v, t, Mathf.Clamp01(av / spread), 0.92f), Mathf.Clamp01(inside + 0.5f));
            }
            // Dorsal fin
            if (u > s.dorsalFrom && u < s.dorsalTo)
            {
                float t = (u - s.dorsalFrom) / (s.dorsalTo - s.dorsalFrom);
                float h = s.dorsalH * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
                if (s.spiny && t < 0.6f) h *= 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(t * 40f));
                float baseY = Half(s, u) * 0.92f + mid;
                float inside = Mathf.Min((baseY + h - v) / aa, (v - baseY + 0.02f) / aa);
                if (inside > -1f) Over(ref outc, FinColor(s, u, v, t, Mathf.Clamp01((v - baseY) / Mathf.Max(0.001f, h)), 0.88f), Mathf.Clamp01(inside + 0.5f));
            }
            // Anal fin
            if (u > s.analFrom && u < s.analTo)
            {
                float t = (u - s.analFrom) / (s.analTo - s.analFrom);
                float h = s.analH * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f));
                float baseY = -Half(s, u) * 1.05f + mid;
                float inside = Mathf.Min((v - (baseY - h)) / aa, (baseY + 0.02f - v) / aa);
                if (inside > -1f) Over(ref outc, FinColor(s, u, v, t, Mathf.Clamp01((baseY - v) / Mathf.Max(0.001f, h)), 0.85f), Mathf.Clamp01(inside + 0.5f));
            }
            // Pelvic fin (small, under the chest)
            {
                float pu = s.peak * 0.85f, pl = 0.07f;
                if (u > pu && u < pu + pl)
                {
                    float t = (u - pu) / pl;
                    float baseY = -Half(s, u) * 1.05f + mid;
                    float h = 0.045f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.5f)) * (1f - t * 0.3f);
                    float inside = Mathf.Min((v - (baseY - h)) / aa, (baseY + 0.02f - v) / aa);
                    if (inside > -1f) Over(ref outc, FinColor(s, u, v, t, Mathf.Clamp01((baseY - v) / 0.045f), 0.8f), Mathf.Clamp01(inside + 0.5f));
                }
            }

            // Body
            float half = Half(s, u);
            if (half > 0f)
            {
                float upper = half * 0.92f + mid, lower = -half * 1.05f + mid;
                float inside = Mathf.Min((upper - v) / aa, (v - lower) / aa);
                if (u < 0.01f) inside = Mathf.Min(inside, u / aa + 1f);
                if (inside > -1f)
                {
                    float hgt = upper - lower;
                    float k = Mathf.Clamp01((v - lower) / hgt);      // 0 belly .. 1 back
                    Color c = k < 0.45f ? Color.Lerp(s.belly, s.side, k / 0.45f) : Color.Lerp(s.side, s.back, Mathf.Pow((k - 0.45f) / 0.55f, 0.8f));

                    // Marks
                    if (s.bars > 0 && u > s.barsFrom && u < s.barsTo)
                    {
                        float bu = (u - s.barsFrom) / (s.barsTo - s.barsFrom) * s.bars;
                        if (s.wavy) bu += Mathf.Sin(v * 90f) * 0.18f;
                        float bar = Mathf.Clamp01((Mathf.Cos(bu * Mathf.PI * 2f) - 0.35f) * 2f) * Mathf.Clamp01((k - 0.2f) * 2.2f);
                        c = Color.Lerp(c, s.mark, bar * (s.wavy ? 0.55f : 0.45f));
                    }
                    if (s.wavy && s.bars == 0 && k > 0.62f) // ปลาทู: fine dark lines on the back
                        c = Color.Lerp(c, s.mark, Mathf.Clamp01(Mathf.Sin((v * 260f) + u * 6f) * 3f - 2.4f) * 0.5f);
                    if (s.blotches > 0f && u > 0.14f && u < s.bodyEnd)
                    {
                        float n = Blobs(u * 9f, v * 18f, 11) * Mathf.Clamp01((k - 0.2f) * 2f) * Mathf.Clamp01((u - 0.14f) * 15f);
                        c = Color.Lerp(c, s.mark, Mathf.Clamp01((n - 0.35f) * 3f) * 0.6f * s.blotches);
                    }
                    if (s.spots > 0f)
                    {
                        float sp = Spots(u, v, s.spotSize * 3.2f, s.spotSize, 23);
                        c = Color.Lerp(c, s.mark, sp * 0.8f * s.spots);
                    }

                    // Scales: overlapping arcs in offset rows, catching the light a little
                    if (s.scaleShow > 0f && u > 0.2f)
                    {
                        float a = u / s.scale, b = v / (s.scale * 0.8f);
                        float row = Mathf.Floor(b);
                        float fa = a + (((int)row & 1) == 0 ? 0f : 0.5f);
                        float cx = Mathf.Floor(fa) + 0.5f, cy = row + 0.5f;
                        float dx = fa - cx, dy = b - cy;
                        float r = Mathf.Sqrt(dx * dx * 1.2f + dy * dy);
                        float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.5f) * 7f) * (dx > -0.1f ? 1f : 0.3f);
                        float glint = Mathf.Clamp01(0.5f - r) * 0.5f;
                        c = Color.Lerp(c, c * 0.62f, rim * s.scaleShow);
                        c = Color.Lerp(c, Color.white, glint * s.scaleShow * 0.35f * k);
                    }
                    // Lateral line
                    float ll = Mathf.Lerp(upper, lower, 0.35f) + Mathf.Sin(u * 7f) * 0.004f;
                    if (u > 0.22f && u < s.bodyEnd) c = Color.Lerp(c, s.belly, Mathf.Clamp01(1f - Mathf.Abs(v - ll) / (aa * 1.4f)) * 0.45f);

                    // Head: a little darker, gill cover arc
                    float gillU = s.eyeAt + s.depth * 0.95f;
                    float gd = Mathf.Sqrt(Mathf.Pow((u - (gillU - 0.12f)) / 1f, 2f) + Mathf.Pow((v - mid) * 0.75f, 2f));
                    if (u < gillU) c *= 0.93f;
                    c = Color.Lerp(c, c * 0.45f, Mathf.Clamp01(1f - Mathf.Abs(gd - 0.12f) / (aa * 1.6f)) * (u < gillU ? 1f : 0f) * 0.9f);

                    // Round body shading + a soft shine along the upper side
                    float n2 = (k - 0.5f) * 2f;
                    float shade = 0.62f + 0.38f * Mathf.Sqrt(Mathf.Clamp01(1f - n2 * n2));
                    c *= shade;
                    float shine = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(k - 0.68f) * 5f), 2f) * Mathf.Clamp01(u * 4f) * Mathf.Clamp01((s.bodyEnd - u) * 6f);
                    c = Color.Lerp(c, Color.white, shine * 0.22f);
                    // dark rim
                    c = Color.Lerp(c * 0.5f, c, Mathf.Clamp01(inside / 2.2f));

                    // Mouth
                    float mouthY = mid - half * 0.15f;
                    if (u < 0.045f && Mathf.Abs(v - mouthY - (u * 0.3f)) < aa * 1.2f) c *= 0.4f;

                    // Eye
                    float eyeY = mid + Half(s, s.eyeAt) * 0.35f;
                    float ed = Mathf.Sqrt(Mathf.Pow(u - s.eyeAt, 2f) + Mathf.Pow(v - eyeY, 2f));
                    if (ed < s.eye + aa)
                    {
                        Color eye = ed < s.eye * 0.55f ? new Color(0.03f, 0.03f, 0.04f) : Color.Lerp(new Color(0.95f, 0.82f, 0.4f), new Color(0.6f, 0.45f, 0.2f), (ed / s.eye - 0.55f) * 2f);
                        float hl = Mathf.Sqrt(Mathf.Pow(u - s.eyeAt + s.eye * 0.25f, 2f) + Mathf.Pow(v - eyeY - s.eye * 0.25f, 2f));
                        if (hl < s.eye * 0.2f) eye = Color.white;
                        if (ed > s.eye - aa * 1.2f) eye = new Color(0.1f, 0.08f, 0.06f);
                        c = Color.Lerp(c, eye, Mathf.Clamp01((s.eye + aa - ed) / aa));
                    }
                    c.a = 1f;
                    Over(ref outc, c, Mathf.Clamp01(inside + 0.5f));
                }
            }

            // Pectoral fin over the body, see-through
            {
                float pu = s.eyeAt + s.depth * 0.95f + 0.01f, pv = mid - Half(s, pu) * 0.35f;
                float du = (u - pu) / 0.09f, dv = (v - pv - (u - pu) * -0.35f) / 0.028f;
                float d = du * du + dv * dv;
                if (du > 0f && d < 1.05f)
                {
                    var fc = FinColor(s, u, v, du, Mathf.Clamp01(du), 0.75f);
                    Over(ref outc, fc, Mathf.Clamp01((1f - d) * 12f) * 0.75f);
                }
            }
            // Whiskers (ปลาดุก)
            if (s.barbels && u < 0.25f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float len = i < 2 ? 0.2f : 0.12f;
                    float t = -u / len + 0.05f; // from the mouth forward and back
                    float wu = u, sign = i % 2 == 0 ? 1f : -1f;
                    float wy = mid - Half(s, 0.02f) * (0.1f + 0.3f * (i % 2)) + sign * Mathf.Sin(Mathf.Clamp01(wu / len) * 2.2f) * 0.05f * (1f + i * 0.2f) - wu * 0.25f * (i < 2 ? 1f : 0.6f);
                    if (wu < len && wu > 0.005f && Mathf.Abs(v - wy) < aa * 1.4f * (1f - wu / len * 0.6f))
                        Over(ref outc, new Color(0.15f, 0.13f, 0.12f, 1f), 0.95f);
                }
            }
            px[y * W + x] = outc;
        }
    }

    // A fin: see-through, with rays, darker or coloured at the edge
    private static Color FinColor(Spec s, float u, float v, float t, float outward, float alpha)
    {
        float rays = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(u * 190f + v * 30f)) * 3f - 2f);
        Color c = Color.Lerp(s.fin, s.fin * 0.6f, rays * 0.6f);
        if (s.finEdge.a > 0f) c = Color.Lerp(c, s.finEdge, Mathf.Clamp01((outward - 0.6f) * 2.5f));
        c.a = alpha * (0.85f + 0.15f * (1f - outward));
        return c;
    }

    // Soft blotches: blurred random cells
    private static float Blobs(float a, float b, int seed)
    {
        int ia = Mathf.FloorToInt(a), ib = Mathf.FloorToInt(b);
        float best = 0f;
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            float h = Hash(ia + i + seed, ib + j - seed);
            if (h < 0.45f) continue;
            float cx = ia + i + Hash(ia + i, ib + j + 7), cy = ib + j + Hash(ia + i + 3, ib + j);
            float d = Mathf.Sqrt((a - cx) * (a - cx) + (b - cy) * (b - cy));
            best = Mathf.Max(best, Mathf.Clamp01(1f - d / (0.5f + h * 0.4f)));
        }
        return best;
    }

    // Round spots, one per cell (some cells empty)
    private static float Spots(float u, float v, float cell, float r, int seed)
    {
        float a = u / cell, b = v / cell;
        int ia = Mathf.FloorToInt(a), ib = Mathf.FloorToInt(b);
        float best = 0f;
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            if (Hash(ia + i + seed, ib + j) < 0.3f) continue;
            float cx = (ia + i + 0.2f + Hash(ia + i, ib + j + seed) * 0.6f) * cell;
            float cy = (ib + j + 0.2f + Hash(ia + i + seed, ib + j + 1) * 0.6f) * cell;
            float rr = r * (0.7f + Hash(ia + i + 5, ib + j + 9) * 0.6f);
            float d = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
            best = Mathf.Max(best, Mathf.Clamp01((rr - d) / (rr * 0.35f)));
        }
        return best;
    }

    // ── Squid ────────────────────────────────────────────────────────────────────────────────
    // Mantle pointing right with the two side fins at its tip, head and big eye, eight arms and two
    // long tentacles trailing to the left; pink-white skin with reddish freckles.
    private static void PaintSquid(Spec s, Color[] px)
    {
        float L = W * LEN, aa = 1f / L;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = (x / (float)W - X0) / LEN;
            float v = (y - H * 0.5f) / L;
            Color outc = new Color(0, 0, 0, 0);
            // fins: a diamond at the mantle tip
            {
                float fu = (u - 0.86f) / 0.13f, fv = v / 0.15f;
                float d = Mathf.Abs(fu) + Mathf.Abs(fv);
                if (d < 1f + aa * 8f)
                {
                    var c = Color.Lerp(s.side, s.back, Mathf.Abs(fv) * 0.5f);
                    c.a = 0.92f;
                    Over(ref outc, c, Mathf.Clamp01((1f - d) * 10f));
                }
            }
            // arms and tentacles
            for (int i = 0; i < 10; i++)
            {
                bool tentacle = i >= 8;
                float len = tentacle ? 0.36f : 0.22f + (i % 4) * 0.015f;
                float t = (0.36f - u) / len;
                if (t < 0f || t > 1f) continue;
                float spreadY = ((i % 8) - 3.5f) * 0.012f * (1f + t * 1.8f) + (tentacle ? (i == 8 ? 0.04f : -0.04f) * t : 0f);
                float wy = spreadY + Mathf.Sin(t * 5f + i) * 0.012f * t;
                float wdt = (tentacle ? 0.006f : 0.012f) * (1f - t * 0.7f) + (tentacle && t > 0.82f ? 0.008f : 0f);
                float inside = (wdt - Mathf.Abs(v - wy)) / aa;
                if (inside > -1f)
                {
                    var c = Color.Lerp(s.side, s.back, 0.3f + 0.4f * Mathf.Abs(Mathf.Sin(u * 300f))); // suckers
                    c *= 0.85f + 0.15f * (1f - t);
                    c.a = 1f;
                    Over(ref outc, c, Mathf.Clamp01(inside + 0.5f));
                }
            }
            // head
            {
                float hu = (u - 0.4f) / 0.07f, hv = v / 0.06f;
                float d = hu * hu + hv * hv;
                if (d < 1.1f) { var c = Color.Lerp(s.side, s.back, 0.35f); c.a = 1f; Over(ref outc, c * (0.8f + 0.2f * Mathf.Sqrt(Mathf.Clamp01(1f - hv * hv))), Mathf.Clamp01((1f - d) * 12f)); }
                float ed = Mathf.Sqrt(Mathf.Pow(u - 0.405f, 2f) + Mathf.Pow(v - 0.012f, 2f));
                if (ed < 0.026f)
                {
                    Color e = ed < 0.016f ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.75f, 0.7f, 0.55f);
                    if (Mathf.Sqrt(Mathf.Pow(u - 0.399f, 2f) + Mathf.Pow(v - 0.018f, 2f)) < 0.005f) e = Color.white;
                    Over(ref outc, e, Mathf.Clamp01((0.026f - ed) / aa));
                }
            }
            // mantle: a long tapering tube
            if (u > 0.44f && u < 0.97f)
            {
                float t = (u - 0.44f) / 0.53f;
                float half = 0.075f * Mathf.Pow(1f - t, 0.75f) * (t < 0.06f ? Mathf.Sqrt(t / 0.06f) * 0.3f + 0.7f : 1f);
                float inside = (half - Mathf.Abs(v)) / aa;
                if (inside > -1f)
                {
                    float k = Mathf.Clamp01((v + half) / (2f * half));
                    Color c = Color.Lerp(s.belly, s.back, k);
                    float sp = Spots(u, v, 0.016f, 0.004f, 41);
                    c = Color.Lerp(c, s.mark, sp * 0.65f * (0.4f + k));
                    float n2 = (k - 0.5f) * 2f;
                    c *= 0.65f + 0.35f * Mathf.Sqrt(Mathf.Clamp01(1f - n2 * n2));
                    c = Color.Lerp(c, Color.white, Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(k - 0.7f) * 5f), 2f) * 0.3f);
                    c = Color.Lerp(c * 0.6f, c, Mathf.Clamp01(inside / 2f));
                    c.a = 1f;
                    Over(ref outc, c, Mathf.Clamp01(inside + 0.5f));
                }
            }
            px[y * W + x] = outc;
        }
    }

    // Paint `c` (with its own alpha × cover) over `dst`
    private static void Over(ref Color dst, Color c, float cover)
    {
        float a = Mathf.Clamp01(c.a * cover);
        if (a <= 0f) return;
        float outA = a + dst.a * (1f - a);
        if (outA <= 0f) return;
        var rgb = (new Color(c.r, c.g, c.b) * a + new Color(dst.r, dst.g, dst.b) * dst.a * (1f - a)) / outA;
        dst = new Color(rgb.r, rgb.g, rgb.b, outA);
    }
}
