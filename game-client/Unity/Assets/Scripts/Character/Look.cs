using System;
using UnityEngine;

// A character's look (หน้าตา): gender, face (which body model), hairstyle and hair colour, top and
// bottom colour, and an outfit (ชุด) worn over them. Saved on the server (profile.lua) as the avatar
// url "look:g.f.h.c.t.b" (+ ".o" while wearing an outfit) so every
// player sees the same; old "skin:N" accounts read as that body with its original hair and clothes.
// Option lists (names, colours, prices, levels) mirror profile.lua — the server checks them.
[Serializable]
public struct Look
{
    public int Gender, Face, Hair, HairColor, Top, Bottom, Outfit;

    public int Skin => Mathf.Clamp(Gender, 0, 1) * 2 + Mathf.Clamp(Face, 0, 1); // 0,1 male · 2,3 female

    public static Look FromSkin(int skin) => new Look { Gender = skin >= 2 ? 1 : 0, Face = skin % 2 };

    public string Code => $"{Gender}.{Face}.{Hair}.{HairColor}.{Top}.{Bottom}" + (Outfit > 0 ? $".{Outfit}" : "");

    public static Look Parse(string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl)) return default;
        if (avatarUrl.StartsWith("skin:") && int.TryParse(avatarUrl.Substring(5), out int skin)) return FromSkin(skin);
        if (!avatarUrl.StartsWith("look:")) return default;
        var p = avatarUrl.Substring(5).Split('.');
        int At(int i, int max) => i < p.Length && int.TryParse(p[i], out int v) ? Mathf.Clamp(v, 0, max - 1) : 0;
        return new Look
        {
            Gender = At(0, 2), Face = At(1, 2), Hair = At(2, HairStyles.Length), HairColor = At(3, HairColors.Length),
            Top = At(4, ClothColors.Length), Bottom = At(5, ClothColors.Length), Outfit = At(6, Outfits.Length),
        };
    }

    public bool Equals(Look o) => Code == o.Code;

    // A varied look for villagers and other NPCs
    public static Look Random(int seed)
    {
        var r = new System.Random(seed);
        var look = new Look { Gender = r.Next(2), Face = r.Next(2), HairColor = r.Next(5), Top = r.Next(ClothColors.Length), Bottom = r.Next(6) };
        look.Hair = r.Next(3) == 0 ? 0 : 1 + r.Next(9);
        return look;
    }

    // ── Options ──────────────────────────────────────────────────────────────
    public struct Option
    {
        public string Name;
        public Color Color;
        public int Price, Level;
        public Option(string name, Color color, int price = 0, int level = 1) { Name = name; Color = color; Price = price; Level = level; }
        public bool Free => Price == 0 && Level <= 1;
    }

    // profile.lua HAIR_STYLES
    public static readonly Option[] HairStyles =
    {
        new Option("ทรงเดิม", Color.clear),
        new Option("สั้นเกรียน", Color.clear),
        new Option("รองทรง", Color.clear),
        new Option("ผมยาว", Color.clear),
        new Option("หางม้า", Color.clear),
        new Option("มวยผม", Color.clear),
        new Option("บ็อบ", Color.clear, 150, 2),
        new Option("ผมฟู", Color.clear, 300, 3),
        new Option("ผมตั้ง", Color.clear, 400, 5),
        new Option("โมฮอว์ก", Color.clear, 600, 7),
        new Option("หัวโล้น", Color.clear, 100, 2),
    };

    // profile.lua HAIR_COLORS
    public static readonly Option[] HairColors =
    {
        new Option("ดำ", new Color(0.08f, 0.07f, 0.07f)),
        new Option("น้ำตาลเข้ม", new Color(0.25f, 0.15f, 0.09f)),
        new Option("น้ำตาล", new Color(0.45f, 0.28f, 0.15f)),
        new Option("บลอนด์", new Color(0.88f, 0.72f, 0.4f), 150, 3),
        new Option("แดง", new Color(0.7f, 0.18f, 0.12f), 200, 4),
        new Option("ชมพู", new Color(1f, 0.5f, 0.72f), 300, 5),
        new Option("ฟ้า", new Color(0.3f, 0.6f, 1f), 300, 5),
        new Option("เขียว", new Color(0.3f, 0.8f, 0.45f), 300, 5),
        new Option("เงิน", new Color(0.85f, 0.87f, 0.9f), 500, 8),
    };

    // profile.lua OUTFITS: name, top and bottom colours (the look of the outfit is in OutfitParts),
    // price, level. Index 0 = no outfit (your own top and bottom colours).
    public static readonly Option[] Outfits =
    {
        new Option("ไม่ใส่ชุด", Color.clear),
        new Option("ชุดปกติขาว ข้าราชการ", new Color(0.96f, 0.96f, 0.94f), 2500, 10),
        new Option("ชุดสูทสากล", new Color(0.12f, 0.14f, 0.22f), 3000, 8),
        new Option("ชุดนักมวยไทย", new Color(0.85f, 0.12f, 0.12f), 800, 3),
        new Option("ชุดกากี อบต.", new Color(0.62f, 0.55f, 0.36f), 1500, 5),
        new Option("ชุดม่อฮ่อม ชาวนา", new Color(0.13f, 0.17f, 0.32f), 300, 1),
        new Option("เสื้อกาวน์หมอ", new Color(0.97f, 0.98f, 1f), 2000, 20),
        new Option("ชุดไทยราชปะแตน", new Color(0.55f, 0.2f, 0.45f), 2500, 12),
        new Option("ชุดกีฬา", new Color(0.1f, 0.45f, 0.9f), 400, 2),
        new Option("ชุดตำรวจ", new Color(0.45f, 0.3f, 0.18f), 1800, 15),
    };

    // Trousers / skirt colour that goes with each outfit
    public static readonly Color[] OutfitBottoms =
    {
        Color.clear,
        new Color(0.96f, 0.96f, 0.94f), // white trousers
        new Color(0.12f, 0.14f, 0.22f), // suit trousers
        new Color(0.85f, 0.12f, 0.12f), // red boxing shorts
        new Color(0.58f, 0.51f, 0.33f), // khaki
        new Color(0.13f, 0.17f, 0.32f), // indigo ม่อฮ่อม
        new Color(0.2f, 0.22f, 0.3f),   // dark trousers under the coat
        new Color(0.35f, 0.12f, 0.3f),  // silk โจงกระเบน
        new Color(0.95f, 0.95f, 0.95f), // white sports shorts
        new Color(0.45f, 0.3f, 0.18f),  // police brown
    };

    // profile.lua CLOTH_COLORS (index 0 = the model's own clothes)
    public static readonly Option[] ClothColors =
    {
        new Option("แบบเดิม", Color.clear),
        new Option("ขาว", new Color(0.95f, 0.95f, 0.93f)),
        new Option("ดำ", new Color(0.12f, 0.12f, 0.13f)),
        new Option("แดง", new Color(0.85f, 0.15f, 0.15f)),
        new Option("น้ำเงิน", new Color(0.15f, 0.3f, 0.8f)),
        new Option("เขียว", new Color(0.2f, 0.65f, 0.3f), 100, 2),
        new Option("เหลือง", new Color(1f, 0.82f, 0.2f), 100, 2),
        new Option("ชมพู", new Color(1f, 0.55f, 0.72f), 150, 3),
        new Option("ม่วง", new Color(0.55f, 0.3f, 0.8f), 150, 3),
        new Option("ส้ม", new Color(1f, 0.55f, 0.15f), 150, 3),
        new Option("กรมท่า", new Color(0.1f, 0.15f, 0.35f), 200, 4),
        new Option("ฟ้าคราม", new Color(0.2f, 0.75f, 0.8f), 200, 4),
        new Option("ทอง", new Color(1f, 0.78f, 0.3f), 1000, 10),
    };
}
