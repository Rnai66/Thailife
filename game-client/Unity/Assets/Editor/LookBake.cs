using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Thai Life > Advanced > Bake Character Looks
// The clothes are painted into each body model's texture, so for every texture in LookParts this
// paints a mask of where the top (red) and the bottom (green) are, by rasterising the mesh's
// triangles in UV space: separate clothes meshes (Remy, Ch31) are taken whole; one-piece models
// (Girl01, Ch46) are split by the bones the triangles hang on (spine/arms = top, hips/legs = bottom)
// and bare skin is left out by colour. Also writes the mean brightness of each region.
public static class LookBake
{
    private const int Size = 512;
    private const string Folder = "Assets/Resources/Characters/Looks";

    [MenuItem("Thai Life/Advanced/Bake Character Looks")]
    public static void Bake()
    {
        Directory.CreateDirectory(Folder);
        for (int skin = 0; skin < LookParts.Models.Length; skin++)
        {
            var prefab = Resources.Load<GameObject>($"Characters/Skin_{skin}");
            if (prefab == null) continue;
            var parts = LookParts.Models[skin];
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int t = 0; t < parts.Targets.Length; t++)
            {
                var target = parts.Targets[t];
                Material material = null;
                foreach (var r in renderers)
                    if (System.Array.IndexOf(target.Renderers, r.name) >= 0) { material = r.sharedMaterial; break; }
                var tex = material == null ? null : (material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture);
                if (tex == null) { Debug.LogWarning($"[LookBake] Skin_{skin} target {t}: no texture"); continue; }
                var pixels = Read(tex);

                var mask = new byte[Size * Size * 3]; // r top, g bottom, b hair
                foreach (var r in renderers)
                {
                    if (System.Array.IndexOf(target.Renderers, r.name) < 0) continue;
                    Paint(r, skin, mask, parts.OwnHair);
                }
                bool oneMesh = parts.Targets.Length == 1 && target.Renderers.Length == 1;
                if (oneMesh) Unskin(mask, pixels); // one-piece body: keep bare skin out, hair only where it looks like hair
                mask = Grow(mask, 2);

                // Mean brightness (linear, as the shader sees it) of each region
                double[] sum = new double[3]; int[] n = new int[3];
                for (int i = 0; i < Size * Size; i++)
                    for (int c = 0; c < 3; c++)
                        if (mask[i * 3 + c] > 128)
                        {
                            var col = pixels[i].linear;
                            sum[c] += 0.299 * col.r + 0.587 * col.g + 0.114 * col.b;
                            n[c]++;
                        }
                float top = n[0] > 0 ? (float)(sum[0] / n[0]) : 0.5f, bottom = n[1] > 0 ? (float)(sum[1] / n[1]) : 0.5f;
                float hair = n[2] > 0 ? (float)(sum[2] / n[2]) : 0.5f;

                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                var outPixels = new Color32[Size * Size];
                for (int i = 0; i < outPixels.Length; i++) outPixels[i] = new Color32(mask[i * 3], mask[i * 3 + 1], mask[i * 3 + 2], 255);
                png.SetPixels32(outPixels);
                png.Apply();
                string path = $"{Folder}/Skin_{skin}_{t}_mask.png";
                File.WriteAllBytes(path, png.EncodeToPNG());
                Object.DestroyImmediate(png);
                File.WriteAllText($"{Folder}/Skin_{skin}_{t}.txt",
                    top.ToString("0.000", CultureInfo.InvariantCulture) + " " + bottom.ToString("0.000", CultureInfo.InvariantCulture)
                    + " " + hair.ToString("0.000", CultureInfo.InvariantCulture));
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                Debug.Log($"[LookBake] Skin_{skin} target {t}: top {n[0]} px (mean {top:0.00}), bottom {n[1]} px (mean {bottom:0.00}), hair {n[2]} px (mean {hair:0.00})");
            }
        }
        AssetDatabase.Refresh();
    }

    // The texture as gamma colours at mask size (works on non-readable textures)
    private static Color[] Read(Texture tex)
    {
        var rt = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var pixels = copy.GetPixels();
        Object.DestroyImmediate(copy);
        return pixels;
    }

    // Which region a triangle belongs to: 0 top, 1 bottom, 2 head (hair), -1 none
    private static int Region(SkinnedMeshRenderer r, int skin, BoneWeight[] w, int a, int b, int c, bool ownHair)
    {
        switch (r.name)
        {
            case "Tops": case "Ch31_Sweater": case "Ch31_Collar": return 0;
            case "Bottoms": case "Ch31_Pants": return 1;
        }
        if (skin < 2 || w == null || w.Length == 0) return -1;
        int votesTop = 0, votesBottom = 0, votesHead = 0;
        foreach (int v in new[] { a, b, c })
        {
            string bone = BoneName(r, w[v]);
            if (bone.Contains("Head") || bone.Contains("Hair")) { votesHead++; continue; }
            if (bone.Contains("Hand") || bone.Contains("Foot") || bone.Contains("Toe") || bone.Contains("Neck")) continue;
            if (bone.Contains("Spine") || bone.Contains("Shoulder") || bone.Contains("Arm")) votesTop++;
            else if (bone.Contains("Hips") || bone.Contains("Leg")) votesBottom++;
        }
        if (votesTop >= 2) return 0;
        if (votesBottom >= 2) return 1;
        if (ownHair && votesHead >= 2) return 2;
        return -1;
    }

    private static string BoneName(SkinnedMeshRenderer r, BoneWeight w)
    {
        int index = w.boneIndex0;
        float best = w.weight0;
        if (w.weight1 > best) { index = w.boneIndex1; best = w.weight1; }
        if (w.weight2 > best) { index = w.boneIndex2; best = w.weight2; }
        if (w.weight3 > best) index = w.boneIndex3;
        return index >= 0 && index < r.bones.Length && r.bones[index] != null ? r.bones[index].name : "";
    }

    private static void Paint(SkinnedMeshRenderer r, int skin, byte[] mask, bool ownHair)
    {
        var mesh = r.sharedMesh;
        if (mesh == null) return;
        var uv = mesh.uv;
        var tris = mesh.triangles;
        var weights = mesh.boneWeights;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int region = Region(r, skin, weights, tris[i], tris[i + 1], tris[i + 2], ownHair);
            if (region < 0) continue;
            Fill(mask, region, Wrap(uv[tris[i]]), Wrap(uv[tris[i + 1]]), Wrap(uv[tris[i + 2]]));
        }
    }

    private static Vector2 Wrap(Vector2 uv) => new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y)) * Size;

    private static void Fill(byte[] mask, int channel, Vector2 a, Vector2 b, Vector2 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)))), y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
        float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
        if (Mathf.Abs(area) < 1e-6f) return;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = ((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y)) / area;
                float w1 = ((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y)) / area;
                float w2 = 1f - w0 - w1;
                if (w0 >= -0.02f && w1 >= -0.02f && w2 >= -0.02f) mask[(y * Size + x) * 3 + channel] = 255;
            }
    }

    // Leave out texels that look like bare skin (warm, lightly saturated, bright)
    private static void Unskin(byte[] mask, Color[] pixels)
    {
        for (int i = 0; i < Size * Size; i++)
        {
            Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
            bool skin = h > 0.015f && h < 0.12f && s > 0.12f && s < 0.6f && v > 0.4f; // peach, not pink fabric
            if (skin) { mask[i * 3] = 0; mask[i * 3 + 1] = 0; mask[i * 3 + 2] = 0; }
            // On the head only dark or strongly coloured texels are hair (not the face or eyes)
            bool hair = (v < 0.3f || (s > 0.3f && h > 0.15f && h < 0.88f)) && !skin; // black, or a cool colour (not blush)
            if (!hair) mask[i * 3 + 2] = 0;
        }
    }

    // Spread each region by a few texels so the seams between UV islands don't show the old colour
    private static byte[] Grow(byte[] mask, int steps)
    {
        for (int s = 0; s < steps; s++)
        {
            var next = (byte[])mask.Clone();
            for (int y = 1; y < Size - 1; y++)
                for (int x = 1; x < Size - 1; x++)
                    for (int c = 0; c < 3; c++)
                    {
                        int i = (y * Size + x) * 3 + c;
                        if (mask[i] != 0) continue;
                        if (mask[i - 3] != 0 || mask[i + 3] != 0 || mask[i - Size * 3] != 0 || mask[i + Size * 3] != 0) next[i] = 255;
                    }
            mask = next;
        }
        return mask;
    }
}
