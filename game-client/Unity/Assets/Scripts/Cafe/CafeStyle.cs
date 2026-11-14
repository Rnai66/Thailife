using UnityEngine;

// The look of a café (cafe.lua STYLES): walls, the feature wall, the floor, and the wood, cloth and
// metal the furniture is made of (CafeFurniture).
public class CafeStyle
{
    public string Id;
    public Color Wall, Feature, Floor, FloorLine, Wood, Cloth, Metal, TableTop, CounterBody, Accent, Accent2, Rug, Pot, Shade, Frame, Neon, Bike;
    public bool Brick, Panels, Planks; // extra wall/floor detail

    public static CafeStyle Of(string id)
    {
        switch (id)
        {
            case "vintage": // sage walls, dark wood, mustard leather, brass
                return new CafeStyle
                {
                    Id = id, Wall = new Color(0.66f, 0.73f, 0.62f), Feature = new Color(0.3f, 0.42f, 0.36f), Floor = new Color(0.36f, 0.23f, 0.14f),
                    FloorLine = new Color(0.25f, 0.15f, 0.09f), Wood = new Color(0.42f, 0.26f, 0.14f), Cloth = new Color(0.72f, 0.48f, 0.18f),
                    Metal = new Color(0.72f, 0.58f, 0.3f), TableTop = new Color(0.94f, 0.92f, 0.86f), CounterBody = new Color(0.3f, 0.42f, 0.36f),
                    Accent = new Color(0.8f, 0.35f, 0.25f), Accent2 = new Color(0.95f, 0.85f, 0.6f), Rug = new Color(0.55f, 0.2f, 0.2f),
                    Pot = new Color(0.85f, 0.82f, 0.75f), Shade = new Color(0.72f, 0.58f, 0.3f), Frame = new Color(0.72f, 0.58f, 0.3f),
                    Neon = new Color(1f, 0.55f, 0.3f), Bike = new Color(0.3f, 0.55f, 0.55f), Panels = true, Planks = true,
                };
            case "loft": // raw concrete, a brick feature wall, black steel, cognac leather
                return new CafeStyle
                {
                    Id = id, Wall = new Color(0.62f, 0.62f, 0.6f), Feature = new Color(0.58f, 0.3f, 0.22f), Floor = new Color(0.42f, 0.42f, 0.41f),
                    FloorLine = new Color(0.36f, 0.36f, 0.35f), Wood = new Color(0.5f, 0.34f, 0.2f), Cloth = new Color(0.55f, 0.3f, 0.15f),
                    Metal = new Color(0.1f, 0.1f, 0.11f), TableTop = new Color(0.5f, 0.34f, 0.2f), CounterBody = new Color(0.18f, 0.18f, 0.19f),
                    Accent = new Color(0.85f, 0.65f, 0.25f), Accent2 = new Color(0.3f, 0.3f, 0.32f), Rug = new Color(0.3f, 0.3f, 0.32f),
                    Pot = new Color(0.25f, 0.25f, 0.26f), Shade = new Color(0.1f, 0.1f, 0.11f), Frame = new Color(0.1f, 0.1f, 0.11f),
                    Neon = new Color(1f, 0.3f, 0.6f), Bike = new Color(0.15f, 0.15f, 0.15f), Brick = true,
                };
            case "thai": // cream walls, teak panels, terracotta tiles, Thai red and gold
                return new CafeStyle
                {
                    Id = id, Wall = new Color(0.94f, 0.88f, 0.75f), Feature = new Color(0.45f, 0.27f, 0.13f), Floor = new Color(0.72f, 0.4f, 0.26f),
                    FloorLine = new Color(0.6f, 0.32f, 0.2f), Wood = new Color(0.45f, 0.27f, 0.13f), Cloth = new Color(0.68f, 0.15f, 0.13f),
                    Metal = new Color(0.3f, 0.18f, 0.09f), TableTop = new Color(0.45f, 0.27f, 0.13f), CounterBody = new Color(0.45f, 0.27f, 0.13f),
                    Accent = new Color(0.9f, 0.7f, 0.25f), Accent2 = new Color(0.2f, 0.45f, 0.4f), Rug = new Color(0.68f, 0.15f, 0.13f),
                    Pot = new Color(0.6f, 0.32f, 0.2f), Shade = new Color(0.86f, 0.73f, 0.45f), Frame = new Color(0.9f, 0.7f, 0.25f),
                    Neon = new Color(1f, 0.8f, 0.35f), Bike = new Color(0.68f, 0.15f, 0.13f), Panels = true,
                };
            default: // minimal: white, light oak, linen, matte black
                return new CafeStyle
                {
                    Id = "minimal", Wall = new Color(0.95f, 0.94f, 0.92f), Feature = new Color(0.84f, 0.8f, 0.74f), Floor = new Color(0.82f, 0.7f, 0.53f),
                    FloorLine = new Color(0.74f, 0.62f, 0.46f), Wood = new Color(0.8f, 0.66f, 0.47f), Cloth = new Color(0.86f, 0.84f, 0.8f),
                    Metal = new Color(0.12f, 0.12f, 0.12f), TableTop = new Color(0.97f, 0.97f, 0.96f), CounterBody = new Color(0.97f, 0.97f, 0.96f),
                    Accent = new Color(0.55f, 0.65f, 0.55f), Accent2 = new Color(0.85f, 0.75f, 0.6f), Rug = new Color(0.8f, 0.76f, 0.7f),
                    Pot = new Color(0.95f, 0.95f, 0.93f), Shade = new Color(0.95f, 0.95f, 0.93f), Frame = new Color(0.12f, 0.12f, 0.12f),
                    Neon = new Color(1f, 0.95f, 0.85f), Bike = new Color(0.95f, 0.95f, 0.93f), Planks = true,
                };
        }
    }
}
