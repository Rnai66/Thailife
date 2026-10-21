// Which parts of each body model (Resources/Characters/Skin_N) the look system touches. The clothes
// are painted into each model's texture, so for every recolourable texture there is a baked mask
// (Thai Life > Advanced > Bake Character Looks): Resources/Characters/Looks/Skin_N_T_mask.png with
// red = top, green = bottom and blue = hair painted into the body, plus Skin_N_T.txt with the mean
// brightness of each.
public static class LookParts
{
    public class Target
    {
        public string[] Renderers; // renderers that use this texture (their material gets the recoloured copy)
    }

    public class Model
    {
        public Target[] Targets;
        public string[] HairRenderers = new string[0]; // the model's own hair mesh, hidden for a new hairstyle
        public string[] HairBones = new string[0];     // or hair bones to fold away (scale 0)
        public float HairScale = 1f;                   // hairstyles sit a little bigger over hair we can't hide
        public bool OwnHair;                           // hair is part of the body mesh: recolour it, add styles on top
        public UnityEngine.Vector3 HairOffset;         // nudge for the hairstyle, in head heights
    }

    public static readonly Model[] Models =
    {
        // 0 · ชาย หน้า 1 (Remy): top, bottoms and hair are separate meshes with their own textures
        new Model
        {
            Targets = new[] { new Target { Renderers = new[] { "Tops" } }, new Target { Renderers = new[] { "Bottoms" } } },
            HairRenderers = new[] { "Hair" },
            HairOffset = new UnityEngine.Vector3(0f, -0.05f, 0.08f), // high forehead: bring the hairline forward
        },
        // 1 · ชาย หน้า 2 (Ch31): separate meshes sharing one texture
        new Model
        {
            Targets = new[] { new Target { Renderers = new[] { "Ch31_Body", "Ch31_Sweater", "Ch31_Pants", "Ch31_Collar", "Ch31_Shoes" } } },
            HairRenderers = new[] { "Ch31_Hair" },
        },
        // 2 · หญิง หน้า 1 (Girl01): one mesh; the hair hangs off its own bones
        new Model
        {
            Targets = new[] { new Target { Renderers = new[] { "Girl_Body_Geo" } } },
            HairBones = new[] { "Ponytail", "Bow_Left", "Bow_Right" },
            OwnHair = true,
        },
        // 3 · หญิง หน้า 2 (Ch46): one mesh, hair included
        new Model
        {
            Targets = new[] { new Target { Renderers = new[] { "Ch46" } } },
            HairScale = 1.05f,
            OwnHair = true,
        },
    };

    public static Model For(int skin) => Models[UnityEngine.Mathf.Clamp(skin, 0, Models.Length - 1)];
}
