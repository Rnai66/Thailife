using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Thai ghosts (no colliders), facing -Z, each with its tell-tale look and a dim coloured glow so it
// reads in the dark. Placed by TempleLife at GhostSpots; GhostSpot shows them only at night and makes
// them drift and stare. Made to unsettle rather than amuse: deathly pale skin, dark sunken eye sockets
// with a pin of light in each, long lank black hair hanging in strands (over the face for the women),
// see-through gowns that thin away into rags above the ground, thin arms reaching out with long fingers.
public static class Ghosts
{
    private static Material M(string name, Color c) => PrimitiveKit.Mat("Ghost" + name, c, 0.3f);
    private static Material G(string name, Color c, float i = 2.5f) => PrimitiveKit.Glow("GhostGlow" + name, c, i);

    // A see-through cloth with a faint glow of its own (alpha-blended URP Lit)
    private static Material Veil(string name, Color c, float alpha, float glow = 0.18f)
    {
        var m = PrimitiveKit.Mat("GhostVeil" + name, c, 0.2f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", (float)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * glow);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Transform Build(Transform parent, Vector3 local, string id)
    {
        var root = PrimitiveKit.Group("Ghost_" + id, parent, local).transform;
        var body = PrimitiveKit.Group("Body", root, Vector3.zero).transform;
        switch (id)
        {
            case "krasue": Krasue(body); Glow(root, new Color(0.4f, 1f, 0.5f), 1.6f); break;
            case "krahang": Krahang(body); Glow(root, new Color(1f, 0.6f, 0.3f), 2f); break;
            case "pop": Pop(body); Glow(root, new Color(0.8f, 0.4f, 1f), 2f); break;
            case "tani": Lady(body, "tani", new Color(0.3f, 0.62f, 0.32f), new Color(0.82f, 0.9f, 0.8f), 1.7f); Banana(body); Glow(root, new Color(0.4f, 1f, 0.45f), 2f); break;
            case "takhian": Lady(body, "takhian", new Color(0.62f, 0.12f, 0.12f), new Color(0.9f, 0.88f, 0.84f), 1.7f); Sash(body); Glow(root, new Color(1f, 0.45f, 0.35f), 2f); break;
            case "phrai": Lady(body, "phrai", new Color(0.9f, 0.93f, 0.97f), new Color(0.86f, 0.9f, 0.94f), 1.75f); Glow(root, new Color(0.6f, 0.85f, 1f), 2.2f); break;
            case "pret": Pret(body); Glow(root, new Color(0.75f, 0.75f, 0.9f), 3.5f); break;
            case "nak": Lady(body, "nak", new Color(0.48f, 0.3f, 0.58f), new Color(0.9f, 0.88f, 0.86f), 1.75f, longest: true); Baby(body); Glow(root, new Color(0.75f, 0.55f, 1f), 2.2f); break;
            default: Lady(body, "white", new Color(0.92f, 0.92f, 0.92f), new Color(0.9f, 0.9f, 0.9f), 1.7f); break;
        }
        return root;
    }

    // A dim point light the colour of the ghost, so it glows in the dark
    private static void Glow(Transform root, Color c, float height)
    {
        var go = new GameObject("GhostLight");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, height, -0.6f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = 4.5f;
        l.intensity = 1.6f;
        l.shadows = LightShadows.None;
    }

    // ── Tubes: many round strands (hair, fingers, guts, ribs) merged into one mesh ──────────────
    private class Tubes
    {
        private readonly List<Vector3> v = new List<Vector3>();
        private readonly List<int> t = new List<int>();

        public void Add(Vector3[] path, float from, float to, int sides = 6)
        {
            int start = v.Count;
            Vector3 up = Vector3.up;
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 dir = (i < path.Length - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.95f) up = Vector3.forward;
                Vector3 side = Vector3.Cross(dir, up).normalized;
                Vector3 normal = Vector3.Cross(side, dir).normalized;
                float r = Mathf.Lerp(from, to, i / (float)(path.Length - 1));
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    v.Add(path[i] + (side * Mathf.Cos(a) + normal * Mathf.Sin(a)) * r);
                }
            }
            int row = sides + 1;
            for (int i = 0; i < path.Length - 1; i++)
            for (int s = 0; s < sides; s++)
            {
                int a = start + i * row + s, b = a + row;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        }

        public void Place(string key, string name, Transform parent, Material m)
        {
            var mesh = new Mesh { indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshKit.Place(name, parent, MeshKit.SaveCombined("Ghost/" + key, mesh), m, Vector3.zero);
        }
    }

    private static float Noise(int i, int k) => Mathf.Sin(i * 12.9898f + k * 78.233f) * 0.5f; // -0.5..0.5, fixed

    // ── Faces and hair ───────────────────────────────────────────────────────────────────
    // Long lank hair: strands from the crown hang down the back and sides; `cover` lets a curtain of it
    // fall over the face with one eye showing through; `wild` makes it stand out matted and grey.
    private static void Hair(Transform t, string key, Vector3 at, float size, Color hair, float length, bool cover, bool wild = false)
    {
        var m = M("Hair" + Key(hair), hair);
        PrimitiveKit.Sphere("Scalp", t, at + new Vector3(0f, 0.05f, 0.02f) * size, new Vector3(0.39f, 0.4f, 0.4f) * size, m);
        var tubes = new Tubes();
        int strands = wild ? 30 : 36;
        for (int i = 0; i < strands; i++)
        {
            float ang = Mathf.Lerp(-155f, 155f, i / (float)(strands - 1)) * Mathf.Deg2Rad; // 0 = straight back
            Vector3 radial = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            float len = length * (0.85f + 0.3f * (Noise(i, 1) + 0.5f));
            var path = new Vector3[7];
            for (int k = 0; k < path.Length; k++)
            {
                float u = k / (float)(path.Length - 1);
                float drop = 0.14f - u * len;
                float out_ = 0.19f + 0.05f * Mathf.Min(1f, u * 3f) + (wild ? 0.22f * u : 0.04f * u);
                Vector3 wig = new Vector3(Noise(i, k), 0f, Noise(k, i)) * (wild ? 0.12f : 0.04f) * u;
                path[k] = at + (radial * out_ + Vector3.up * (drop + (wild ? 0.08f * Mathf.Sin(u * 3f) : 0f)) + wig) * size;
            }
            tubes.Add(path, 0.024f * size, 0.008f * size);
        }
        if (cover)
        {
            // the curtain over the face, parted just off-centre so one eye stares out
            for (int i = 0; i < 12; i++)
            {
                float x = Mathf.Lerp(-0.16f, 0.16f, i / 11f);
                if (x > -0.02f && x < 0.075f) continue;
                float len = length * (0.75f + 0.25f * (Noise(i, 7) + 0.5f));
                var path = new Vector3[6];
                for (int k = 0; k < path.Length; k++)
                {
                    float u = k / (float)(path.Length - 1);
                    path[k] = at + new Vector3(x * (1f + u * 0.3f) + Noise(i, k) * 0.03f * u, 0.17f - u * len, -0.17f - 0.06f * Mathf.Sin(Mathf.Min(1f, u * 2.5f) * Mathf.PI * 0.5f)) * size;
                }
                tubes.Add(path, 0.022f * size, 0.008f * size);
            }
        }
        tubes.Place("Hair_" + key, "Hair", t, m);
    }

    private static void Face(Transform t, Vector3 at, float size, Color skin, bool gaping = true, bool fangs = false, bool tongue = false, Color? eyeLight = null)
    {
        var flesh = M("Skin" + Key(skin), skin);
        var hollow = M("Hollow", new Color(0.05f, 0.04f, 0.05f));
        PrimitiveKit.Sphere("Head", t, at, new Vector3(0.34f, 0.42f, 0.36f) * size, flesh);
        PrimitiveKit.Sphere("Jaw", t, at + new Vector3(0f, -0.12f, -0.03f) * size, new Vector3(0.24f, 0.18f, 0.26f) * size, flesh);
        var pupil = G("Pupil" + Key(eyeLight ?? Color.red), eyeLight ?? new Color(1f, 0.25f, 0.15f), 6f);
        foreach (float sx in new[] { -1f, 1f })
        {
            // dark sunken sockets, a pin of light deep in each, bruised shadows under them
            PrimitiveKit.Sphere("Socket", t, at + new Vector3(sx * 0.072f, 0.025f, -0.152f) * size, new Vector3(0.095f, 0.07f, 0.05f) * size, hollow);
            PrimitiveKit.Sphere("Pupil", t, at + new Vector3(sx * 0.072f, 0.022f, -0.172f) * size, Vector3.one * 0.022f * size, pupil);
            PrimitiveKit.Sphere("Bruise", t, at + new Vector3(sx * 0.075f, -0.025f, -0.158f) * size, new Vector3(0.09f, 0.03f, 0.03f) * size, M("Bruise", new Color(0.32f, 0.28f, 0.36f)));
            PrimitiveKit.Sphere("Cheek", t, at + new Vector3(sx * 0.1f, -0.07f, -0.13f) * size, new Vector3(0.05f, 0.07f, 0.04f) * size, M("CheekHollow", Color.Lerp(skin, Color.black, 0.35f)));
        }
        PrimitiveKit.Sphere("Nose", t, at + new Vector3(0f, -0.03f, -0.178f) * size, new Vector3(0.035f, 0.06f, 0.03f) * size, flesh);
        if (gaping) // the mouth hangs open, a black hole
            PrimitiveKit.Sphere("Mouth", t, at + new Vector3(0f, -0.13f, -0.165f) * size, new Vector3(0.065f, 0.09f, 0.03f) * size, hollow);
        else
            PrimitiveKit.Sphere("Mouth", t, at + new Vector3(0f, -0.12f, -0.17f) * size, new Vector3(0.07f, 0.02f, 0.02f) * size, M("Lips", new Color(0.35f, 0.05f, 0.08f)));
        if (fangs)
            foreach (float sx in new[] { -1f, 1f })
                PrimitiveKit.Box("Fang", t, at + new Vector3(sx * 0.025f, -0.1f, -0.175f) * size, new Vector3(0.012f, 0.04f, 0.01f) * size, M("Fang", new Color(0.92f, 0.9f, 0.8f)), new Vector3(0f, 0f, sx * -8f), collider: false);
        if (tongue)
        {
            var lick = new Tubes();
            lick.Add(MeshKit.Curve(at + new Vector3(0f, -0.14f, -0.17f) * size, at + new Vector3(0.02f, -0.4f, -0.3f) * size, at + new Vector3(-0.03f, -0.62f, -0.22f) * size, 9), 0.028f * size, 0.014f * size, 8);
            lick.Place("Tongue_" + Key(skin), "Tongue", t, M("Tongue", new Color(0.55f, 0.06f, 0.12f)));
        }
    }

    // A see-through gown flaring down from the shoulders and thinning to rags above the ground
    private static void Gown(Transform t, string key, Color cloth, float top, float height, float width)
    {
        var veil = Veil(Key(cloth), cloth, 0.72f);
        MeshKit.Place("Gown", t, MeshKit.Lathe("Ghost/Gown", new[]
        {
            new Vector2(0.17f, 0f), new Vector2(0.24f, -0.1f), new Vector2(0.24f, -0.3f), new Vector2(0.26f, -0.5f),
            new Vector2(0.32f, -0.75f), new Vector2(0.4f, -0.95f), new Vector2(0.46f, -1f),
        }, 20, false, false), veil, new Vector3(0f, top, 0f), default, new Vector3(width / 0.6f, height, width / 0.6f * 0.8f));
        // rags hanging from the hem, swaying
        var rag = MeshKit.Leaf("Ghost/Rag", 0.45f, 0.11f, 0.0f, 0.02f, 6, 0.6f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * Mathf.PI * 2f / 14f;
            float r = 0.44f * width / 0.6f;
            var at = new Vector3(Mathf.Sin(a) * r, top - height * 0.98f, Mathf.Cos(a) * r * 0.8f);
            MeshKit.Place("Rag", t, rag, veil, at, new Vector3(95f + Noise(i, 3) * 20f, a * Mathf.Rad2Deg, Noise(i, 4) * 25f), Vector3.one * (0.7f + 0.6f * (Noise(i, 5) + 0.5f)));
        }
    }

    // Thin arms reaching out low, hands hanging limp with long bony fingers
    private static void Arms(Transform t, string key, Color skin, float shoulder, float reach = 0.5f, float spread = 0.24f)
    {
        var tubes = new Tubes();
        foreach (float sx in new[] { -1f, 1f })
        {
            var sh = new Vector3(sx * spread, shoulder, 0f);
            var elbow = new Vector3(sx * (spread + 0.04f), shoulder - 0.2f, -reach * 0.45f);
            var wrist = new Vector3(sx * (spread - 0.02f), shoulder - 0.22f, -reach);
            tubes.Add(new[] { sh, Vector3.Lerp(sh, elbow, 0.5f), elbow }, 0.05f, 0.035f, 8);
            tubes.Add(new[] { elbow, Vector3.Lerp(elbow, wrist, 0.5f), wrist }, 0.035f, 0.026f, 8);
            for (int f = 0; f < 4; f++) // the fingers droop down from the wrist
            {
                float fx = (f - 1.5f) * 0.018f;
                var knuckle = wrist + new Vector3(fx, -0.02f, -0.06f);
                tubes.Add(new[] { wrist + new Vector3(fx * 0.5f, 0f, -0.01f), knuckle, knuckle + new Vector3(fx * 0.3f, -0.08f, -0.03f), knuckle + new Vector3(fx * 0.4f, -0.15f, 0.0f) },
                    0.011f, 0.006f, 5);
            }
        }
        tubes.Place("Arms_" + key, "Arms", t, M("Skin" + Key(skin), skin));
    }

    // ── The ghosts ───────────────────────────────────────────────────────────────────────
    private static void Lady(Transform t, string key, Color cloth, Color skin, float headY, bool longest = false)
    {
        Gown(t, key, cloth, headY - 0.25f, 1.3f, 0.6f);
        PrimitiveKit.Sphere("Shoulders", t, new Vector3(0f, headY - 0.3f, 0f), new Vector3(0.42f, 0.14f, 0.24f), M("Skin" + Key(skin), skin));
        Arms(t, key, skin, headY - 0.33f, 0.5f);
        Face(t, new Vector3(0f, headY, 0f), 1f, skin, gaping: true, fangs: longest);
        Hair(t, key, new Vector3(0f, headY, 0f), 1f, new Color(0.03f, 0.03f, 0.04f), longest ? 1.5f : 1.15f, cover: true);
    }

    private static void Krasue(Transform t)
    {
        // Only a woman's head, floating, trailing her heart, lungs and a long glistening coil of gut
        var guts = G("Guts", new Color(0.85f, 0.18f, 0.28f), 1.4f);
        guts.SetFloat("_Smoothness", 0.9f);
        var heart = G("Heart", new Color(0.7f, 0.04f, 0.08f), 1.8f);
        var coil = new Tubes();
        var path = new Vector3[40];
        for (int i = 0; i < path.Length; i++)
        {
            float u = i / (float)(path.Length - 1);
            path[i] = new Vector3(Mathf.Sin(u * 14f) * 0.1f * (1f - u * 0.4f), 1.42f - u * 1.15f, Mathf.Cos(u * 14f) * 0.07f);
        }
        coil.Add(path, 0.045f, 0.03f, 8);
        for (int s = 0; s < 4; s++) // strings of blood dripping off it
            coil.Add(MeshKit.Curve(new Vector3((s - 1.5f) * 0.06f, 1.2f - s * 0.12f, 0f), new Vector3((s - 1.5f) * 0.07f, 0.9f - s * 0.1f, 0.02f), new Vector3((s - 1.5f) * 0.05f, 0.65f - s * 0.12f, 0f), 5), 0.008f, 0.003f, 4);
        coil.Place("Guts", "Guts", t, guts);
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Sphere("Lung", t, new Vector3(sx * 0.1f, 1.4f, 0f), new Vector3(0.12f, 0.18f, 0.1f), heart);
        PrimitiveKit.Sphere("Heart", t, new Vector3(0f, 1.3f, -0.05f), new Vector3(0.12f, 0.14f, 0.1f), heart);
        PrimitiveKit.Sphere("Neck", t, new Vector3(0f, 1.52f, 0f), new Vector3(0.1f, 0.12f, 0.1f), heart);
        var skin = new Color(0.88f, 0.86f, 0.82f);
        Face(t, new Vector3(0f, 1.75f, 0f), 1f, skin, gaping: true, fangs: true, eyeLight: new Color(0.5f, 1f, 0.5f));
        Hair(t, "krasue", new Vector3(0f, 1.75f, 0f), 1f, new Color(0.03f, 0.03f, 0.04f), 0.7f, cover: false);
    }

    private static void Krahang(Transform t)
    {
        // A dark gaunt man flying on winnowing baskets (กระด้ง) for wings, a pestle (สาก) between his legs
        var skin = new Color(0.4f, 0.3f, 0.24f);
        var flesh = M("Skin" + Key(skin), skin);
        var basket = M("Kradong", new Color(0.72f, 0.55f, 0.3f));
        MeshKit.Place("Torso", t, MeshKit.Lathe("Ghost/KrahangTorso", new[]
        {
            new Vector2(0.16f, 0f), new Vector2(0.15f, 0.15f), new Vector2(0.19f, 0.35f), new Vector2(0.23f, 0.5f), new Vector2(0.12f, 0.58f),
        }, 14), flesh, new Vector3(0f, 0.95f, 0f), default, new Vector3(1f, 1f, 0.7f));
        PrimitiveKit.Cylinder("Loincloth", t, new Vector3(0f, 0.92f, 0f), new Vector3(0.36f, 0.1f, 0.26f), M("Loincloth", new Color(0.35f, 0.1f, 0.08f)), collider: false);
        var limbs = new Tubes();
        foreach (float sx in new[] { -1f, 1f })
        {
            limbs.Add(new[] { new Vector3(sx * 0.08f, 0.9f, 0f), new Vector3(sx * 0.12f, 0.62f, 0.12f), new Vector3(sx * 0.1f, 0.35f, 0.3f) }, 0.06f, 0.035f, 8);
            limbs.Add(new[] { new Vector3(sx * 0.2f, 1.42f, 0f), new Vector3(sx * 0.55f, 1.5f, 0.02f), new Vector3(sx * 0.82f, 1.48f, 0.05f) }, 0.05f, 0.03f, 8);
            var wing = PrimitiveKit.Group("Wing", t, new Vector3(sx * 0.9f, 1.47f, 0.05f)).transform;
            wing.localRotation = Quaternion.Euler(0f, 0f, sx * 70f);
            MeshKit.Place("Kradong", wing, MeshKit.Lathe("Ghost/Kradong", new[]
            {
                new Vector2(0f, -0.02f), new Vector2(0.3f, -0.015f), new Vector2(0.45f, 0f), new Vector2(0.5f, 0.03f), new Vector2(0.48f, 0.04f),
            }, 20, false, false), basket, Vector3.zero);
        }
        limbs.Place("KrahangLimbs", "Limbs", t, flesh);
        PrimitiveKit.Cylinder("Sak", t, new Vector3(0f, 0.55f, 0.35f), new Vector3(0.08f, 0.5f, 0.08f), M("Pestle", new Color(0.4f, 0.27f, 0.16f)), new Vector3(-40f, 0f, 0f), collider: false);
        Face(t, new Vector3(0f, 1.66f, 0f), 0.95f, skin, gaping: true, fangs: true, eyeLight: new Color(1f, 0.55f, 0.2f));
        Hair(t, "krahang", new Vector3(0f, 1.66f, 0f), 0.95f, new Color(0.06f, 0.05f, 0.04f), 0.35f, cover: false, wild: true);
    }

    private static void Pop(Transform t)
    {
        // A hunched old woman with matted grey hair, staring eyes and a long tongue lolling out
        var cloth = new Color(0.3f, 0.28f, 0.32f);
        var skin = new Color(0.66f, 0.62f, 0.58f);
        var hunch = PrimitiveKit.Group("Hunch", t, Vector3.zero).transform;
        hunch.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        Gown(hunch, "pop", cloth, 1.25f, 1.1f, 0.62f);
        Arms(hunch, "pop", skin, 1.15f, 0.65f, 0.22f);
        Face(hunch, new Vector3(0f, 1.5f, -0.1f), 1f, skin, gaping: true, fangs: true, tongue: true, eyeLight: new Color(0.85f, 0.5f, 1f));
        Hair(hunch, "pop", new Vector3(0f, 1.5f, -0.1f), 1f, new Color(0.72f, 0.72f, 0.74f), 0.8f, cover: false, wild: true);
    }

    private static void Pret(Transform t)
    {
        // Taller than a palm-sugar tree, all skin and bone, a swollen belly and a mouth no bigger than a
        // needle's eye
        var skin = new Color(0.5f, 0.48f, 0.53f);
        var m = M("Skin" + Key(skin), skin);
        var bones = new Tubes();
        foreach (float sx in new[] { -1f, 1f })
        {
            // knobbly legs and arms, the knees and elbows bigger than the limbs between
            bones.Add(new[] { new Vector3(sx * 0.12f, 2.25f, 0f), new Vector3(sx * 0.13f, 1.2f, -0.05f), new Vector3(sx * 0.12f, 0.1f, 0.02f) }, 0.07f, 0.04f, 8);
            PrimitiveKit.Sphere("Knee", t, new Vector3(sx * 0.13f, 1.2f, -0.05f), Vector3.one * 0.1f, m);
            bones.Add(new[] { new Vector3(sx * 0.26f, 3.3f, 0f), new Vector3(sx * 0.34f, 2.6f, -0.12f), new Vector3(sx * 0.3f, 1.9f, -0.2f) }, 0.05f, 0.03f, 8);
            PrimitiveKit.Sphere("Elbow", t, new Vector3(sx * 0.34f, 2.6f, -0.12f), Vector3.one * 0.08f, m);
            for (int f = 0; f < 4; f++)
                bones.Add(new[] { new Vector3(sx * 0.3f + (f - 1.5f) * 0.02f, 1.9f, -0.2f), new Vector3(sx * 0.3f + (f - 1.5f) * 0.03f, 1.72f, -0.24f) }, 0.012f, 0.006f, 5);
        }
        for (int r = 0; r < 5; r++) // ribs showing through the skin
            foreach (float sx in new[] { -1f, 1f })
                bones.Add(MeshKit.Curve(new Vector3(0f, 3.25f - r * 0.1f, -0.13f), new Vector3(sx * 0.18f, 3.2f - r * 0.1f, -0.1f), new Vector3(sx * 0.2f, 3.12f - r * 0.1f, 0.05f), 5), 0.018f, 0.012f, 5);
        bones.Add(new[] { new Vector3(0f, 3.4f, 0f), new Vector3(0f, 3.8f, -0.02f), new Vector3(0f, 4.1f, 0f) }, 0.06f, 0.05f, 8); // the long thin neck
        bones.Place("PretBones", "Bones", t, m);
        PrimitiveKit.Sphere("Belly", t, new Vector3(0f, 2.55f, -0.08f), new Vector3(0.5f, 0.55f, 0.5f), m);
        MeshKit.Place("Chest", t, MeshKit.Lathe("Ghost/PretChest", new[]
        {
            new Vector2(0.14f, 0f), new Vector2(0.2f, 0.15f), new Vector2(0.24f, 0.4f), new Vector2(0.2f, 0.55f), new Vector2(0.08f, 0.62f),
        }, 14), m, new Vector3(0f, 2.8f, 0f), default, new Vector3(1f, 1f, 0.7f));
        PrimitiveKit.Cylinder("Loincloth", t, new Vector3(0f, 2.22f, 0f), new Vector3(0.46f, 0.09f, 0.4f), M("Rag", new Color(0.3f, 0.26f, 0.22f)), collider: false);
        Face(t, new Vector3(0f, 4.25f, 0f), 0.75f, skin, gaping: false, eyeLight: new Color(0.8f, 0.8f, 1f));
        PrimitiveKit.Sphere("PinMouth", t, new Vector3(0f, 4.16f, -0.135f), new Vector3(0.02f, 0.02f, 0.01f), M("PinMouth", Color.black));
        Hair(t, "pret", new Vector3(0f, 4.25f, 0f), 0.75f, new Color(0.25f, 0.25f, 0.27f), 0.5f, cover: false, wild: true);
    }

    private static void Banana(Transform t)
    {
        // นางตานี lives in a wild banana (กล้วยตานี): a clump of big drooping leaves behind her
        var leaf = M("BananaLeaf", new Color(0.25f, 0.55f, 0.2f));
        var mesh = MeshKit.Leaf("Ghost/BananaLeaf", 1.7f, 0.55f, 0.5f, 0.6f, 12, 0.3f);
        for (int i = 0; i < 5; i++)
        {
            float a = -70f + i * 35f;
            MeshKit.Place("Leaf", t, mesh, leaf, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.9f, 0.6f), new Vector3(-55f, a, 0f));
        }
        PrimitiveKit.Cylinder("Trunk", t, new Vector3(0f, 0.6f, 0.65f), new Vector3(0.22f, 0.6f, 0.22f), M("BananaTrunk", new Color(0.35f, 0.45f, 0.22f)), collider: false);
    }

    private static void Sash(Transform t) =>
        PrimitiveKit.Box("Sabai", t, new Vector3(0f, 1.3f, -0.1f), new Vector3(0.5f, 0.1f, 0.42f), M("SabaiGold", new Color(0.9f, 0.7f, 0.25f)), new Vector3(0f, 0f, 30f), collider: false);

    private static void Baby(Transform t)
    {
        PrimitiveKit.Sphere("Swaddle", t, new Vector3(0.22f, 1.22f, -0.36f), new Vector3(0.3f, 0.2f, 0.2f), Veil("Swaddle", new Color(0.95f, 0.95f, 0.93f), 0.85f));
        PrimitiveKit.Sphere("BabyHead", t, new Vector3(0.35f, 1.29f, -0.37f), Vector3.one * 0.13f, M("BabySkin", new Color(0.85f, 0.82f, 0.8f)));
    }

    private static string Key(Color c) => ColorUtility.ToHtmlStringRGB(c);
}
