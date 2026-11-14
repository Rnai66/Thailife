using System.Collections.Generic;
using UnityEngine;

// The café's furniture (cafe.lua ITEMS) out of simple shapes, in metres. Origin: the middle of the
// piece on the floor; the front (where customers stand or sit facing) is +Z. Wood, cloth and metal
// take the shop's style (CafeStyle) so a whole room reads as one look.
public static class CafeFurniture
{
    private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    private static Shader lit;

    public static GameObject Build(string item, CafeStyle st)
    {
        var root = new GameObject("Cafe_" + item).transform;
        Color wood = st.Wood, cloth = st.Cloth, metal = st.Metal;
        Color white = new Color(0.96f, 0.95f, 0.93f), cup = new Color(0.97f, 0.97f, 0.95f);
        switch (item)
        {
            case "counter": // the bar: a panelled front to the customers, a thick top, the till and cups
                Box(root, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 0.66f), st.CounterBody);
                for (int i = 0; i < 6; i++)
                    Box(root, new Vector3(-1.0f + i * 0.4f, 0.5f, 0.335f), new Vector3(0.05f, 0.9f, 0.02f), Color.Lerp(st.CounterBody, Color.black, 0.25f));
                Box(root, new Vector3(0f, 1.03f, 0.04f), new Vector3(2.5f, 0.06f, 0.76f), wood, 0.6f);
                Box(root, new Vector3(-0.8f, 1.17f, -0.1f), new Vector3(0.36f, 0.22f, 0.3f), new Color(0.15f, 0.15f, 0.17f)); // till
                Box(root, new Vector3(-0.8f, 1.3f, -0.18f), new Vector3(0.3f, 0.18f, 0.02f), Glow(new Color(0.4f, 0.8f, 1f), 0.8f), null, false);
                for (int i = 0; i < 4; i++)
                    Cyl(root, new Vector3(0.3f + i * 0.16f, 1.1f + 0.01f * (i % 2), 0.1f), new Vector3(0.07f, 0.045f, 0.07f), cup);
                Cyl(root, new Vector3(1.0f, 1.14f, 0.05f), new Vector3(0.14f, 0.08f, 0.14f), Glow(new Color(0.85f, 0.95f, 1f), 0.2f, 0.5f)); // cookie jar
                break;

            case "espresso": // a cabinet with the machine, grinder and a stack of cups
                Box(root, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 0.6f), st.CounterBody);
                Box(root, new Vector3(0f, 0.92f, 0f), new Vector3(1.14f, 0.04f, 0.64f), wood, 0.6f);
                Box(root, new Vector3(-0.1f, 1.18f, -0.05f), new Vector3(0.66f, 0.46f, 0.42f), new Color(0.78f, 0.8f, 0.82f), 0.85f);
                Box(root, new Vector3(-0.1f, 1.43f, -0.05f), new Vector3(0.68f, 0.04f, 0.44f), cup);
                foreach (float x in new[] { -0.28f, 0.08f })
                {
                    Cyl(root, new Vector3(x, 1.06f, 0.2f), new Vector3(0.09f, 0.03f, 0.09f), new Color(0.2f, 0.2f, 0.2f));
                    Box(root, new Vector3(x, 1.06f, 0.3f), new Vector3(0.03f, 0.03f, 0.16f), new Color(0.1f, 0.1f, 0.1f));
                }
                Box(root, new Vector3(-0.1f, 1.3f, 0.165f), new Vector3(0.2f, 0.08f, 0.01f), Glow(new Color(1f, 0.6f, 0.2f), 0.6f), null, false);
                Cyl(root, new Vector3(0.38f, 1.12f, 0f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.15f, 0.15f, 0.15f)); // grinder
                Sph(root, new Vector3(0.38f, 1.38f, 0f), new Vector3(0.18f, 0.24f, 0.18f), Glow(new Color(0.55f, 0.35f, 0.2f), 0.1f, 0.7f)); // beans hopper
                for (int i = 0; i < 4; i++) Cyl(root, new Vector3(-0.1f, 1.47f + i * 0.05f, -0.05f), new Vector3(0.08f, 0.025f, 0.08f), cup);
                break;

            case "cake_display": // a lit glass case with cakes on two shelves
                Box(root, new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 0.6f), st.CounterBody);
                Box(root, new Vector3(0f, 1.02f, 0f), new Vector3(1.2f, 0.44f, 0.6f), Glow(new Color(0.8f, 0.92f, 1f), 0.15f, 0.35f), null, false);
                Box(root, new Vector3(0f, 1.25f, 0f), new Vector3(1.22f, 0.03f, 0.62f), metal);
                Box(root, new Vector3(0f, 1.22f, 0f), new Vector3(1.1f, 0.02f, 0.5f), Glow(new Color(1f, 0.95f, 0.85f), 1.4f), null, false);
                Color[] cakes = { new Color(0.95f, 0.85f, 0.7f), new Color(0.4f, 0.22f, 0.12f), new Color(0.95f, 0.6f, 0.65f), new Color(0.6f, 0.8f, 0.4f) };
                for (int i = 0; i < 4; i++)
                {
                    Cyl(root, new Vector3(-0.42f + i * 0.28f, 0.88f, 0f), new Vector3(0.2f, 0.06f, 0.2f), cakes[i]);
                    Sph(root, new Vector3(-0.42f + i * 0.28f, 0.95f, 0f), new Vector3(0.04f, 0.04f, 0.04f), new Color(0.85f, 0.1f, 0.15f));
                }
                break;

            case "shelf_cups": // open shelves on the wall behind the bar: cups, jars of beans, a plant
                foreach (float x in new[] { -0.78f, 0.78f }) Box(root, new Vector3(x, 1f, 0f), new Vector3(0.04f, 2f, 0.34f), wood);
                for (int s = 0; s < 4; s++)
                {
                    float y = 0.35f + s * 0.48f;
                    Box(root, new Vector3(0f, y, 0f), new Vector3(1.6f, 0.04f, 0.34f), wood);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -0.6f + i * 0.3f;
                        if ((s + i) % 3 == 0) Cyl(root, new Vector3(x, y + 0.1f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Glow(new Color(0.5f, 0.32f, 0.18f), 0.05f, 0.75f));
                        else Cyl(root, new Vector3(x, y + 0.06f, 0f), new Vector3(0.09f, 0.04f, 0.09f), (s + i) % 2 == 0 ? cup : st.Accent);
                    }
                }
                Sph(root, new Vector3(0.55f, 2.12f, 0f), new Vector3(0.3f, 0.2f, 0.25f), new Color(0.25f, 0.55f, 0.28f));
                break;

            case "menu_board": // a chalkboard on an easel, chalk lines for the menu
                foreach (float x in new[] { -0.32f, 0.32f })
                    Box(root, new Vector3(x, 0.7f, -0.12f), new Vector3(0.04f, 1.4f, 0.04f), wood, 0.3f, new Vector3(-10f, 0f, 0f));
                Box(root, new Vector3(0f, 0.6f, 0.15f), new Vector3(0.04f, 1.2f, 0.04f), wood, 0.3f, new Vector3(14f, 0f, 0f));
                Box(root, new Vector3(0f, 0.98f, -0.07f), new Vector3(0.72f, 0.82f, 0.03f), wood, 0.3f, new Vector3(-10f, 0f, 0f));
                Box(root, new Vector3(0f, 0.98f, -0.05f), new Vector3(0.64f, 0.74f, 0.02f), new Color(0.12f, 0.16f, 0.14f), 0.1f, new Vector3(-10f, 0f, 0f));
                for (int i = 0; i < 6; i++)
                    Box(root, new Vector3(i == 0 ? 0f : -0.05f, 1.3f - i * 0.1f, -0.04f + i * 0.017f), new Vector3(i == 0 ? 0.36f : 0.3f + 0.04f * (i % 3), 0.018f, 0.01f),
                        i == 0 ? new Color(1f, 0.85f, 0.4f) : white, 0.1f, new Vector3(-10f, 0f, 0f));
                break;

            case "table_round":
                Cyl(root, new Vector3(0f, 0.37f, 0f), new Vector3(0.07f, 0.36f, 0.07f), metal);
                Cyl(root, new Vector3(0f, 0.02f, 0f), new Vector3(0.45f, 0.02f, 0.45f), metal);
                Cyl(root, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.025f, 0.8f), st.TableTop, 0.6f);
                Cup(root, new Vector3(0.15f, 0.77f, 0.1f), cup);
                break;
            case "table_square":
                Legs(root, 0.33f, 0.33f, 0.74f, 0.045f, metal);
                Box(root, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.04f, 0.8f), st.TableTop, 0.6f);
                Cup(root, new Vector3(-0.15f, 0.77f, 0.05f), cup);
                break;
            case "table_long": // a big slab of real wood on black legs, for groups and laptops
                foreach (float x in new[] { -0.85f, 0.85f })
                {
                    Box(root, new Vector3(x, 0.37f, 0f), new Vector3(0.05f, 0.74f, 0.7f), metal);
                    Box(root, new Vector3(x, 0.02f, 0f), new Vector3(0.08f, 0.04f, 0.74f), metal);
                }
                Box(root, new Vector3(0f, 0.76f, 0f), new Vector3(2f, 0.06f, 0.9f), wood, 0.65f);
                Box(root, new Vector3(-0.5f, 0.8f, 0.1f), new Vector3(0.34f, 0.02f, 0.24f), new Color(0.75f, 0.76f, 0.78f), 0.8f); // laptop
                Box(root, new Vector3(-0.5f, 0.91f, -0.01f), new Vector3(0.34f, 0.22f, 0.015f), new Color(0.75f, 0.76f, 0.78f), 0.8f, new Vector3(-12f, 0f, 0f));
                Cup(root, new Vector3(0.4f, 0.79f, 0.1f), cup);
                Cyl(root, new Vector3(0.7f, 0.84f, -0.1f), new Vector3(0.1f, 0.08f, 0.1f), cup);
                Sph(root, new Vector3(0.7f, 0.96f, -0.1f), new Vector3(0.18f, 0.14f, 0.18f), new Color(0.3f, 0.6f, 0.3f));
                break;

            case "chair_wood":
                Legs(root, 0.19f, 0.19f, 0.45f, 0.035f, wood);
                Box(root, new Vector3(0f, 0.46f, 0f), new Vector3(0.44f, 0.04f, 0.44f), wood);
                foreach (float x in new[] { -0.19f, 0.19f }) Box(root, new Vector3(x, 0.7f, -0.2f), new Vector3(0.035f, 0.5f, 0.035f), wood);
                for (int i = 0; i < 3; i++) Box(root, new Vector3(0f, 0.62f + i * 0.13f, -0.2f), new Vector3(0.4f, 0.04f, 0.02f), wood);
                break;
            case "stool_bar":
                Cyl(root, new Vector3(0f, 0.38f, 0f), new Vector3(0.05f, 0.37f, 0.05f), metal);
                Cyl(root, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.02f, 0.36f), metal);
                Cyl(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.01f, 0.3f), metal);
                Cyl(root, new Vector3(0f, 0.76f, 0f), new Vector3(0.36f, 0.04f, 0.36f), st.Cloth);
                break;
            case "armchair":
                Box(root, new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.3f, 0.75f), cloth);
                Box(root, new Vector3(0f, 0.42f, 0.04f), new Vector3(0.6f, 0.12f, 0.64f), Color.Lerp(cloth, Color.white, 0.12f));
                Box(root, new Vector3(0f, 0.65f, -0.3f), new Vector3(0.8f, 0.6f, 0.16f), cloth, 0.3f, new Vector3(-8f, 0f, 0f));
                foreach (float x in new[] { -0.34f, 0.34f }) Box(root, new Vector3(x, 0.5f, 0f), new Vector3(0.13f, 0.26f, 0.72f), cloth);
                Legs(root, 0.32f, 0.3f, 0.07f, 0.04f, wood);
                break;
            case "sofa":
                Box(root, new Vector3(0f, 0.22f, 0f), new Vector3(1.9f, 0.3f, 0.82f), cloth);
                foreach (float x in new[] { -0.42f, 0.42f })
                    Box(root, new Vector3(x, 0.42f, 0.05f), new Vector3(0.8f, 0.12f, 0.68f), Color.Lerp(cloth, Color.white, 0.12f));
                Box(root, new Vector3(0f, 0.66f, -0.33f), new Vector3(1.9f, 0.6f, 0.16f), cloth, 0.3f, new Vector3(-8f, 0f, 0f));
                foreach (float x in new[] { -0.88f, 0.88f }) Box(root, new Vector3(x, 0.5f, 0f), new Vector3(0.14f, 0.28f, 0.8f), cloth);
                Box(root, new Vector3(-0.6f, 0.62f, -0.15f), new Vector3(0.36f, 0.3f, 0.1f), st.Accent, 0.3f, new Vector3(-15f, 10f, 0f)); // cushions
                Box(root, new Vector3(0.62f, 0.62f, -0.15f), new Vector3(0.36f, 0.3f, 0.1f), st.Accent2, 0.3f, new Vector3(-15f, -8f, 0f));
                Legs(root, 0.85f, 0.32f, 0.07f, 0.04f, wood);
                break;
            case "rug":
                Box(root, new Vector3(0f, 0.008f, 0f), new Vector3(2.4f, 0.012f, 1.6f), st.Rug, 0.05f, null, false);
                Box(root, new Vector3(0f, 0.016f, 0f), new Vector3(2.0f, 0.004f, 1.2f), Color.Lerp(st.Rug, Color.white, 0.35f), 0.05f, null, false);
                Box(root, new Vector3(0f, 0.019f, 0f), new Vector3(1.8f, 0.004f, 1.0f), st.Rug, 0.05f, null, false);
                break;

            case "plant_big": // ไทรใบสัก in a tall pot
                Cyl(root, new Vector3(0f, 0.25f, 0f), new Vector3(0.42f, 0.25f, 0.42f), st.Pot);
                Cyl(root, new Vector3(0f, 0.9f, 0f), new Vector3(0.05f, 0.45f, 0.05f), new Color(0.4f, 0.28f, 0.18f));
                for (int i = 0; i < 9; i++)
                {
                    float a = i * 40f * Mathf.Deg2Rad, h = 1.05f + (i % 3) * 0.28f;
                    Sph(root, new Vector3(Mathf.Cos(a) * 0.22f, h, Mathf.Sin(a) * 0.22f), new Vector3(0.3f, 0.34f, 0.12f), new Color(0.18f + 0.03f * (i % 3), 0.45f + 0.04f * (i % 2), 0.2f), 0.4f,
                        new Vector3(0f, -i * 40f, 20f));
                }
                break;
            case "plant_small":
                Cyl(root, new Vector3(0f, 0.12f, 0f), new Vector3(0.24f, 0.12f, 0.24f), st.Pot);
                for (int i = 0; i < 5; i++)
                    Sph(root, new Vector3((i % 2 - 0.5f) * 0.08f, 0.32f + i * 0.04f, (i / 2 - 1) * 0.05f), new Vector3(0.12f, 0.2f, 0.06f), new Color(0.25f, 0.55f, 0.25f), 0.4f, new Vector3(0f, i * 72f, 25f));
                break;
            case "lamp_pendant": // hangs from the ceiling: a cord, a dome shade, a warm bulb
                Cyl(root, new Vector3(0f, 2.15f, 0f), new Vector3(0.012f, 0.25f, 0.012f), new Color(0.1f, 0.1f, 0.1f), 0.3f, null, false);
                Sph(root, new Vector3(0f, 1.88f, 0f), new Vector3(0.36f, 0.22f, 0.36f), st.Shade, 0.6f, null, false);
                Sph(root, new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Glow(new Color(1f, 0.8f, 0.5f), 2.2f), null, false);
                Light(root, new Vector3(0f, 1.7f, 0f), 2.8f, 0.9f);
                break;
            case "lamp_floor": // an arc lamp leaning over a seat
                Cyl(root, new Vector3(0f, 0.02f, -0.25f), new Vector3(0.3f, 0.02f, 0.3f), metal);
                Cyl(root, new Vector3(0f, 0.8f, -0.25f), new Vector3(0.025f, 0.8f, 0.025f), metal);
                Cyl(root, new Vector3(0f, 1.72f, 0f), new Vector3(0.02f, 0.3f, 0.02f), metal, 0.6f, new Vector3(60f, 0f, 0f));
                Sph(root, new Vector3(0f, 1.7f, 0.28f), new Vector3(0.3f, 0.18f, 0.3f), st.Shade);
                Sph(root, new Vector3(0f, 1.63f, 0.28f), new Vector3(0.1f, 0.1f, 0.1f), Glow(new Color(1f, 0.82f, 0.55f), 2f), null, false);
                Light(root, new Vector3(0f, 1.5f, 0.28f), 2.4f, 0.7f);
                break;
            case "bookshelf":
                Box(root, new Vector3(0f, 0.95f, -0.02f), new Vector3(1.2f, 1.9f, 0.04f), wood);
                foreach (float x in new[] { -0.58f, 0.58f }) Box(root, new Vector3(x, 0.95f, 0.12f), new Vector3(0.04f, 1.9f, 0.3f), wood);
                for (int s = 0; s < 5; s++)
                {
                    float y = 0.05f + s * 0.45f;
                    Box(root, new Vector3(0f, y, 0.12f), new Vector3(1.16f, 0.035f, 0.3f), wood);
                    if (s == 4) break;
                    float x = -0.5f;
                    for (int i = 0; x < 0.48f; i++)
                    {
                        float w = 0.04f + 0.02f * ((i * 7 + s) % 3), h = 0.24f + 0.04f * ((i * 3 + s) % 4);
                        Color c = Color.HSVToRGB(((i * 0.17f + s * 0.31f) % 1f), 0.45f, 0.55f + 0.1f * (i % 3));
                        Box(root, new Vector3(x + w * 0.5f, y + 0.02f + h * 0.5f, 0.12f), new Vector3(w, h, 0.22f), c);
                        x += w + 0.005f;
                        if (i % 7 == 6) x += 0.12f;
                    }
                }
                break;
            case "art_frame": // a framed print for the wall, hung at eye height
                Box(root, new Vector3(0f, 1.55f, 0f), new Vector3(0.9f, 0.7f, 0.04f), st.Frame);
                Box(root, new Vector3(0f, 1.55f, 0.022f), new Vector3(0.78f, 0.58f, 0.01f), white);
                Box(root, new Vector3(-0.12f, 1.5f, 0.03f), new Vector3(0.36f, 0.36f, 0.005f), st.Accent);
                Sph(root, new Vector3(0.15f, 1.62f, 0.03f), new Vector3(0.2f, 0.2f, 0.005f), st.Accent2);
                Box(root, new Vector3(0.05f, 1.38f, 0.03f), new Vector3(0.5f, 0.03f, 0.005f), new Color(0.2f, 0.2f, 0.2f));
                break;
            case "neon_sign": // a coffee cup in neon tubes on a dark board
                Box(root, new Vector3(0f, 1.75f, 0f), new Vector3(0.9f, 0.7f, 0.03f), new Color(0.08f, 0.08f, 0.1f), 0.5f);
                var neon = Glow(st.Neon, 3f);
                Box(root, new Vector3(0f, 1.6f, 0.03f), new Vector3(0.36f, 0.025f, 0.02f), neon, null, false);
                foreach (float x in new[] { -0.18f, 0.18f }) Box(root, new Vector3(x, 1.72f, 0.03f), new Vector3(0.025f, 0.26f, 0.02f), neon, null, false);
                Box(root, new Vector3(0f, 1.85f, 0.03f), new Vector3(0.38f, 0.025f, 0.02f), neon, null, false);
                Box(root, new Vector3(0.25f, 1.74f, 0.03f), new Vector3(0.08f, 0.14f, 0.02f), neon, null, false);
                for (int i = 0; i < 3; i++) Box(root, new Vector3(-0.08f + i * 0.08f, 1.97f + 0.02f * (i % 2), 0.03f), new Vector3(0.02f, 0.12f, 0.02f), neon, null, false);
                Light(root, new Vector3(0f, 1.75f, 0.3f), 1.6f, 0.5f, st.Neon);
                break;
            case "coffee_sacks": // กระสอบเมล็ดกาแฟ stacked by the wall
                Color burlap = new Color(0.66f, 0.52f, 0.34f);
                Sph(root, new Vector3(-0.25f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 0.38f), burlap, 0.05f);
                Sph(root, new Vector3(0.25f, 0.22f, 0.02f), new Vector3(0.5f, 0.44f, 0.38f), burlap, 0.05f);
                Sph(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.48f, 0.4f, 0.36f), Color.Lerp(burlap, Color.white, 0.1f), 0.05f, new Vector3(0f, 0f, 6f));
                Box(root, new Vector3(0f, 0.55f, 0.18f), new Vector3(0.2f, 0.14f, 0.005f), new Color(0.15f, 0.1f, 0.05f), 0.1f, null, false);
                break;
            case "bicycle": // จักรยานวินเทจ with a basket of flowers
                foreach (float z in new[] { -0.52f, 0.52f })
                {
                    Cyl(root, new Vector3(0f, 0.34f, z), new Vector3(0.68f, 0.012f, 0.68f), new Color(0.1f, 0.1f, 0.1f), 0.3f, new Vector3(0f, 0f, 90f));
                    Cyl(root, new Vector3(0f, 0.34f, z), new Vector3(0.6f, 0.014f, 0.6f), st.Bike, 0.6f, new Vector3(0f, 0f, 90f));
                }
                Box(root, new Vector3(0f, 0.55f, 0f), new Vector3(0.04f, 0.04f, 0.8f), st.Bike, 0.6f, new Vector3(-18f, 0f, 0f));
                Box(root, new Vector3(0f, 0.55f, -0.22f), new Vector3(0.04f, 0.5f, 0.04f), st.Bike, 0.6f, new Vector3(-15f, 0f, 0f));
                Box(root, new Vector3(0f, 0.6f, 0.46f), new Vector3(0.04f, 0.55f, 0.04f), st.Bike, 0.6f, new Vector3(15f, 0f, 0f));
                Box(root, new Vector3(0f, 0.84f, -0.28f), new Vector3(0.14f, 0.05f, 0.24f), new Color(0.35f, 0.2f, 0.1f));
                Box(root, new Vector3(0f, 0.92f, 0.52f), new Vector3(0.5f, 0.03f, 0.03f), metal);
                Box(root, new Vector3(0f, 0.8f, 0.68f), new Vector3(0.32f, 0.2f, 0.24f), new Color(0.72f, 0.56f, 0.32f));
                for (int i = 0; i < 4; i++)
                    Sph(root, new Vector3(-0.09f + i * 0.06f, 0.94f, 0.66f + (i % 2) * 0.05f), new Vector3(0.08f, 0.08f, 0.08f), i % 2 == 0 ? new Color(0.95f, 0.5f, 0.6f) : new Color(1f, 0.9f, 0.4f));
                break;

            default:
                Box(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), wood);
                break;
        }
        return root.gameObject;
    }

    // Roughly how far a piece reaches from its middle (for the selection ring)
    public static float Size(string item) => item switch
    {
        "counter" => 1.3f, "sofa" => 1.05f, "table_long" => 1.05f, "rug" => 1.3f, "shelf_cups" => 0.85f, "bicycle" => 0.8f,
        "bookshelf" => 0.65f, "espresso" => 0.6f, "cake_display" => 0.65f, _ => 0.45f,
    };

    // Where people sit (local to the piece): chairs, stools, sofas
    public static Vector3[] Seats(string item) => item switch
    {
        "chair_wood" => new[] { new Vector3(0f, 0.48f, 0.02f) },
        "stool_bar" => new[] { new Vector3(0f, 0.8f, 0f) },
        "armchair" => new[] { new Vector3(0f, 0.48f, 0.05f) },
        "sofa" => new[] { new Vector3(-0.42f, 0.48f, 0.05f), new Vector3(0.42f, 0.48f, 0.05f) },
        _ => null,
    };

    private static void Cup(Transform root, Vector3 at, Color cup)
    {
        Cyl(root, at + new Vector3(0f, 0.04f, 0f), new Vector3(0.08f, 0.04f, 0.08f), cup);
        Cyl(root, at + new Vector3(0f, 0.081f, 0f), new Vector3(0.065f, 0.002f, 0.065f), new Color(0.6f, 0.4f, 0.25f), 0.7f, null, false); // latte
        Cyl(root, at + new Vector3(0f, 0.004f, 0f), new Vector3(0.14f, 0.004f, 0.14f), cup);
    }

    private static void Legs(Transform root, float hx, float hz, float h, float t, Color c)
    {
        foreach (float x in new[] { -hx, hx })
        foreach (float z in new[] { -hz, hz })
            Box(root, new Vector3(x, h * 0.5f, z), new Vector3(t, h, t), c);
    }

    private static void Light(Transform root, Vector3 at, float range, float intensity, Color? color = null)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(root, false);
        go.transform.localPosition = at;
        var l = go.AddComponent<UnityEngine.Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color ?? new Color(1f, 0.82f, 0.6f);
        l.shadows = LightShadows.None;
    }

    // Shapes: colliders only on the solid big parts so you can't walk through a counter or sofa
    private static void Box(Transform r, Vector3 p, Vector3 s, Color c, float smooth = 0.3f, Vector3? euler = null, bool solid = true) =>
        Part(PrimitiveType.Cube, r, p, s, Mat(c, smooth), euler, solid);
    private static void Box(Transform r, Vector3 p, Vector3 s, Material m, Vector3? euler = null, bool solid = true) =>
        Part(PrimitiveType.Cube, r, p, s, m, euler, solid);
    private static void Cyl(Transform r, Vector3 p, Vector3 s, Color c, float smooth = 0.3f, Vector3? euler = null, bool solid = false) =>
        Part(PrimitiveType.Cylinder, r, p, s, Mat(c, smooth), euler, solid);
    private static void Cyl(Transform r, Vector3 p, Vector3 s, Material m) =>
        Part(PrimitiveType.Cylinder, r, p, s, m, null, false);
    private static void Sph(Transform r, Vector3 p, Vector3 s, Color c, float smooth = 0.3f, Vector3? euler = null, bool solid = false) =>
        Part(PrimitiveType.Sphere, r, p, s, Mat(c, smooth), euler, solid);
    private static void Sph(Transform r, Vector3 p, Vector3 s, Material m, Vector3? euler = null, bool solid = false) =>
        Part(PrimitiveType.Sphere, r, p, s, m, euler, solid);

    private static void Part(PrimitiveType type, Transform root, Vector3 pos, Vector3 scale, Material m, Vector3? euler, bool solid)
    {
        var go = GameObject.CreatePrimitive(type);
        // only waist-high-and-bigger boxes block walking; small things never trip the player
        if (!solid || scale.x * scale.z < 0.08f) Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    private static Shader Lit => lit != null ? lit : lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    public static Material Mat(Color c, float smooth = 0.3f)
    {
        string key = $"{ColorUtility.ToHtmlStringRGBA(c)}_{smooth:0.00}";
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Lit) { color = c };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        mats[key] = m;
        return m;
    }

    // Lit from inside (bulbs, screens, neon); alpha < 1 makes it see-through glass
    public static Material Glow(Color c, float intensity, float alpha = 1f)
    {
        string key = $"glow_{ColorUtility.ToHtmlStringRGB(c)}_{intensity:0.0}_{alpha:0.00}";
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Lit) { color = new Color(c.r, c.g, c.b, alpha) };
        if (intensity > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
        }
        if (alpha < 1f && m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        mats[key] = m;
        return m;
    }
}
