using System.Collections.Generic;
using UnityEngine;

// Dresses a freshly made body model (CharacterModel) in a Look:
//  · clothes — the model's texture is copied through the LookRecolor shader with the baked mask, so
//    the top and bottom take the chosen colours with their folds and patterns kept (one GPU copy per
//    outfit, shared by everyone wearing it);
//  · hair — the model's own hair is hidden and a hairstyle built from simple shapes is fixed to the
//    head bone, in the chosen colour. "ทรงเดิม" keeps the model's own hair.
public static class CharacterLook
{
    private static readonly Dictionary<string, Material> outfits = new Dictionary<string, Material>();
    private static readonly Dictionary<int, Material> hairMats = new Dictionary<int, Material>();
    private static Material recolor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        outfits.Clear();
        hairMats.Clear();
        recolor = null;
    }

    public static void Apply(GameObject model, int skin, Look look)
    {
        if (model == null) return;
        var parts = LookParts.For(skin);
        bool hairPaint = parts.OwnHair && (look.Hair > 0 || look.HairColor > 0);
        if (look.Top > 0 || look.Bottom > 0 || hairPaint) Dress(model, skin, parts, look, hairPaint);
        if (look.Hair > 0) Hair(model, parts, look);
    }

    // ── Clothes ──────────────────────────────────────────────────────────────
    private static void Dress(GameObject model, int skin, LookParts.Model parts, Look look, bool hairPaint)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int t = 0; t < parts.Targets.Length; t++)
        {
            var target = parts.Targets[t];
            Material original = null;
            foreach (var r in renderers)
                if (System.Array.IndexOf(target.Renderers, r.name) >= 0) { original = r.sharedMaterial; break; }
            if (original == null) continue;

            string key = $"{skin}.{t}.{look.Top}.{look.Bottom}.{(hairPaint ? look.HairColor : -1)}";
            if (!outfits.TryGetValue(key, out var dressed) || dressed == null)
            {
                dressed = Recolor(original, $"Characters/Looks/Skin_{skin}_{t}", look, hairPaint);
                if (dressed == null) continue;
                outfits[key] = dressed;
            }
            foreach (var r in renderers)
                if (System.Array.IndexOf(target.Renderers, r.name) >= 0 && r.sharedMaterial == original)
                    r.sharedMaterial = dressed;
        }
    }

    private static Material Recolor(Material original, string bake, Look look, bool hairPaint)
    {
        var mask = Resources.Load<Texture2D>(bake + "_mask");
        var tex = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.mainTexture;
        if (mask == null || tex == null) return null;
        if (recolor == null)
        {
            var shader = Resources.Load<Shader>("Shaders/LookRecolor") ?? Shader.Find("Hidden/ThaiLife/LookRecolor");
            if (shader == null) return null;
            recolor = new Material(shader);
        }
        // Mean fabric brightness of the top and bottom, measured when the mask was baked
        var means = new Vector4(0.5f, 0.5f, 0.5f, 0f);
        var info = Resources.Load<TextAsset>(bake);
        if (info != null)
        {
            var p = info.text.Split(' ');
            for (int i = 0; i < p.Length && i < 3; i++)
                if (float.TryParse(p[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                    means[i] = v;
        }
        recolor.SetTexture("_Mask", mask);
        recolor.SetColor("_TopColor", look.Top > 0 ? Look.ClothColors[look.Top].Color : Color.white);
        recolor.SetColor("_BottomColor", look.Bottom > 0 ? Look.ClothColors[look.Bottom].Color : Color.white);
        recolor.SetVector("_Means", means);
        recolor.SetColor("_HairColor", Look.HairColors[Mathf.Clamp(look.HairColor, 0, Look.HairColors.Length - 1)].Color);
        recolor.SetVector("_Use", new Vector4(look.Top > 0 ? 1f : 0f, look.Bottom > 0 ? 1f : 0f, hairPaint ? 1f : 0f, 0f));

        int size = Mathf.Min(1024, Mathf.Max(tex.width, tex.height));
        var rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            useMipMap = true, autoGenerateMips = true, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear,
            name = bake + "_outfit",
        };
        Graphics.Blit(tex, rt, recolor);
        var dressed = new Material(original) { name = original.name + " (outfit)" };
        if (dressed.HasProperty("_BaseMap")) dressed.SetTexture("_BaseMap", rt);
        dressed.mainTexture = rt;
        return dressed;
    }

    // ── Hair ─────────────────────────────────────────────────────────────────
    private static void Hair(GameObject model, LookParts.Model parts, Look look)
    {
        var animator = model.GetComponentInChildren<Animator>();
        var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (head == null) return;

        // Hide the model's own hair
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            if (System.Array.IndexOf(parts.HairRenderers, r.name) >= 0) r.enabled = false;
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            foreach (var bone in parts.HairBones)
                if (t.name.EndsWith(bone)) t.localScale = Vector3.zero;

        // Head frame from the body's facing: head bone at the base of the skull, HeadTop_End at the crown
        var root = model.transform;
        Transform top = null;
        foreach (var t in head.GetComponentsInChildren<Transform>())
            if (t.name.EndsWith("HeadTop_End")) { top = t; break; }
        float h = top != null ? Vector3.Dot(top.position - head.position, root.up) : 0.22f;
        if (h < 0.08f || h > 0.6f) h = 0.22f;

        var holder = new GameObject("Hairstyle").transform;
        var o = parts.HairOffset;
        holder.SetPositionAndRotation(head.position + root.up * (h * (0.5f + o.y)) + root.forward * (h * (0.04f + o.z)) + root.right * (h * o.x), root.rotation);
        holder.localScale = Vector3.one * h * parts.HairScale;
        Build(holder, look.Hair, HairMaterial(look.HairColor), !parts.OwnHair);
        holder.SetParent(head, true);
    }

    private static Material HairMaterial(int color)
    {
        if (hairMats.TryGetValue(color, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader) { name = "Hair" + color };
        var c = Look.HairColors[Mathf.Clamp(color, 0, Look.HairColors.Length - 1)].Color;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
        hairMats[color] = m;
        return m;
    }

    // Shapes in head units: origin = middle of the head, y up, z towards the face; the head is about
    // 0.84 wide, 1 tall and 0.95 deep, the face at z ≈ +0.45
    // cap = false on models whose hair is part of the body: no shell over it, just the extra pieces
    private static void Build(Transform h, int style, Material mat, bool cap)
    {
        void Cap(Transform t, Material m, float size) { if (cap) CapShell(t, m, size); }
        switch (style)
        {
            case 1: // สั้นเกรียน
                Cap(h, mat, 0.96f);
                if (cap) Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.34f, 0.2f), new Vector3(0.76f, 0.2f, 0.3f), new Vector3(-12f, 0f, 0f)); // hairline
                break;
            case 2: // รองทรง
                Cap(h, mat, 1f);
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.3f, 0.26f), new Vector3(0.8f, 0.3f, 0.36f), new Vector3(-12f, 0f, 0f));
                break;
            case 3: // ผมยาว
                Cap(h, mat, 1.02f);
                Part(h, PrimitiveType.Capsule, mat, new Vector3(0f, -0.42f, -0.3f), new Vector3(0.86f, 0.7f, 0.34f), new Vector3(8f, 0f, 0f));
                foreach (float x in new[] { -0.4f, 0.4f })
                    Part(h, PrimitiveType.Capsule, mat, new Vector3(x, -0.22f, -0.04f), new Vector3(0.2f, 0.48f, 0.26f), new Vector3(0f, 0f, x * -12f));
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.32f, 0.25f), new Vector3(0.78f, 0.28f, 0.34f), new Vector3(-15f, 0f, 0f));
                break;
            case 4: // หางม้า
                Cap(h, mat, 1f);
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.22f, -0.52f), new Vector3(0.2f, 0.2f, 0.2f), Vector3.zero);
                Part(h, PrimitiveType.Capsule, mat, new Vector3(0f, -0.12f, -0.64f), new Vector3(0.24f, 0.4f, 0.24f), new Vector3(18f, 0f, 0f));
                break;
            case 5: // มวยผม
                Cap(h, mat, 1f);
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.52f, -0.3f), new Vector3(0.44f, 0.4f, 0.44f), Vector3.zero);
                break;
            case 6: // บ็อบ
                Cap(h, mat, 1.02f);
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.32f, 0.25f), new Vector3(0.82f, 0.3f, 0.36f), new Vector3(-10f, 0f, 0f));
                foreach (float x in new[] { -0.42f, 0.42f })
                    Part(h, PrimitiveType.Capsule, mat, new Vector3(x, -0.08f, -0.02f), new Vector3(0.24f, 0.34f, 0.52f), Vector3.zero);
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, -0.1f, -0.36f), new Vector3(0.92f, 0.62f, 0.42f), Vector3.zero);
                break;
            case 7: // ผมฟู
                Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.26f, -0.22f), new Vector3(1.38f, 1.15f, 1.32f), Vector3.zero);
                break;
            case 8: // ผมตั้ง
                Cap(h, mat, 0.97f);
                for (int i = 0; i < 9; i++)
                {
                    float a = (i % 3 - 1) * 28f, b = (i / 3 - 1) * 26f;
                    var dir = Quaternion.Euler(b - 10f, 0f, a) * Vector3.up;
                    Part(h, PrimitiveType.Cube, mat, new Vector3(0f, 0.1f, -0.08f) + dir * 0.5f, new Vector3(0.13f, 0.34f, 0.13f), new Vector3(b - 10f, 45f, a));
                }
                break;
            case 9: // โมฮอว์ก
                Part(h, PrimitiveType.Cube, mat, new Vector3(0f, 0.5f, -0.06f), new Vector3(0.14f, 0.32f, 0.86f), Vector3.zero);
                Part(h, PrimitiveType.Cube, mat, new Vector3(0f, 0.36f, 0.3f), new Vector3(0.14f, 0.3f, 0.2f), new Vector3(-30f, 0f, 0f));
                break;
            // 10 หัวโล้น: nothing — the hair is just hidden
        }
    }

    // The base of most styles: a shell over the top, back and sides of the head, clear of the face
    private static void CapShell(Transform h, Material mat, float size) =>
        Part(h, PrimitiveType.Sphere, mat, new Vector3(0f, 0.15f, -0.065f), new Vector3(0.97f, 0.92f, 1.06f) * size, Vector3.zero);

    private static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = "Hair";
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col); // editor previews
        }
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
