using UnityEngine;

// โรงแรมในเมือง, east of the café on the same road: a three-storey hotel with a walk-in lobby (Interiors
// "empty", furnished at runtime by the owner like the café) and the owner's penthouse on the roof (Seaside.Room "owner",
// open-topped like the beach rooms, with a bed you can sleep in for free). HotelSite (runtime) makes it
// the hotel of whoever runs a hotel business at the bank — staff behind the desk, guests checking in,
// "บริหารโรงแรม" in the lobby — and a hotel to let for everyone else.
public static class HotelBiz
{
    public static readonly Vector3 Site = new Vector3(82f, 0f, -31f); // front to the road (yaw 180 faces +Z), like the café

    public static void Build(Transform root)
    {
        const float width = 16f, depth = 12f;
        const int floors = 3;
        var b = CityCivic.Block(root, Site, 180f, "โรงแรม", width, depth, floors, new Color(0.96f, 0.93f, 0.86f), new Color(0.55f, 0.4f, 0.2f),
            new Color(0.45f, 0.28f, 0.1f), new Color(1f, 0.88f, 0.55f), "MyHotel_โรงแรมของผู้เล่น", kiosk: null, interior: "empty");
        float h = floors * 3.2f;

        // The roof is for the penthouse: clear away the water tank, condensers, antenna and stair house
        for (int i = b.childCount - 1; i >= 0; i--)
        {
            var c = b.GetChild(i);
            if (c.localPosition.y >= h + 0.25f) Object.DestroyImmediate(c.gameObject);
        }
        Seaside.Room(b, new Vector3(0f, h, 0.2f), "owner", "เพนต์เฮาส์เจ้าของโรงแรม");
        // a gold name in lights along the penthouse's front, readable from the street
        PrimitiveKit.Box("RoofSign", b, new Vector3(0f, h + 3.3f, -5.6f), new Vector3(7f, 0.9f, 0.12f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);
        PrimitiveKit.Label("HOTEL · โรงแรม", b, new Vector3(0f, h + 3.3f, -5.68f), 0.08f, new Color(0.35f, 0.15f, 0.05f));
        foreach (float x in new[] { -3.2f, 3.2f })
            PrimitiveKit.Box("SignPost", b, new Vector3(x, h + 1.4f, -5.6f), new Vector3(0.12f, 2.8f, 0.12f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);

        // The street front: the roof sign names it, so no board across the top floor's windows, and the
        // owner's name board over the door takes the place of the entrance's "ยินดีต้อนรับ"
        for (int i = b.childCount - 1; i >= 0; i--)
        {
            var c = b.GetChild(i);
            var text = c.GetComponent<TextMesh>();
            if (c.name == "NameBoard" || (text != null && (text.text == "โรงแรม" || text.text == "ยินดีต้อนรับ"))) Object.DestroyImmediate(c.gameObject);
        }
        CafeShop.DropWelcome(b);

        // the lobby in metres (Interiors.Rooms: 0.2 walls, a 3.2 storey, all scaled), furnished at runtime
        // like the café (CafeSite kind "hotel"); HotelSite offers บริหารโรงแรม to the owner
        float s = b.localScale.x;
        float hw = (width * 0.5f - 0.2f) * s, hd = (depth * 0.5f - 0.2f) * s;
        b.gameObject.AddComponent<CafeSite>().Configure(hw, hd, 3.2f * s, new Vector3(0f, 3.7f, -depth * 0.5f - 0.1f), "hotel");
        b.gameObject.AddComponent<HotelSite>().Configure(hw, hd, 3.2f * s);

        // potted palms either side of the steps
        foreach (float x in new[] { -3.4f, 3.4f })
            Nature.CoconutPalm(b, new Vector3(x, 0f, -depth * 0.5f - 2.6f), 0.55f, (int)(x * 10f) + 77);

        // behind it: the pool, the pool bar, the car park and the drive in from the road (HotelGrounds)
        HotelGrounds.Build(root);
    }

    // โรงแรมแห่งที่ 2: for a player running a second hotel business — four storeys on the east side of
    // town, its front to the ring road (between the house lots and the road). The lobby comes furnished
    // (Interiors "hotel"); HotelSite index 1 offers บริหารโรงแรม to its owner.
    public static readonly Vector3 SecondSite = new Vector3(83f, 0f, 22f);

    public static void BuildSecond(Transform root)
    {
        const float width = 16f, depth = 12f;
        const int floors = 4;
        var b = CityCivic.Block(root, SecondSite, 270f, "โรงแรม", width, depth, floors, new Color(0.9f, 0.93f, 0.96f), new Color(0.2f, 0.32f, 0.5f),
            new Color(0.12f, 0.2f, 0.38f), new Color(1f, 0.88f, 0.55f), "MyHotel2_โรงแรมแห่งที่สอง", kiosk: null, interior: "hotel");
        float h = floors * 3.2f;
        for (int i = b.childCount - 1; i >= 0; i--)
        {
            var c = b.GetChild(i);
            if (c.localPosition.y >= h + 0.25f) Object.DestroyImmediate(c.gameObject); // a clean roof for the sign
        }
        PrimitiveKit.Box("RoofSign", b, new Vector3(0f, h + 1.2f, -5.6f), new Vector3(7f, 1f, 0.12f), PrimitiveKit.Glow("HotelGold", new Color(1f, 0.78f, 0.35f), 1.5f), collider: false);
        PrimitiveKit.Label("HOTEL · โรงแรม", b, new Vector3(0f, h + 1.2f, -5.68f), 0.08f, new Color(0.35f, 0.15f, 0.05f));
        foreach (float x in new[] { -3.2f, 3.2f })
            PrimitiveKit.Box("SignPost", b, new Vector3(x, h + 0.35f, -5.6f), new Vector3(0.12f, 0.7f, 0.12f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        // balconies on the floors above the lobby
        var rail = PrimitiveKit.Mat("RailMetal", new Color(0.2f, 0.22f, 0.24f));
        for (int f = 1; f < floors; f++)
        {
            PrimitiveKit.Box("Balcony", b, new Vector3(0f, f * 3.2f + 0.05f, -6.5f), new Vector3(14f, 0.15f, 1f), PrimitiveKit.Mat("MyHotel2Trim", new Color(0.2f, 0.32f, 0.5f)), collider: false);
            PrimitiveKit.Box("BalconyRail", b, new Vector3(0f, f * 3.2f + 0.6f, -6.95f), new Vector3(14f, 0.07f, 0.07f), rail, collider: false);
        }
        float s = b.localScale.x;
        b.gameObject.AddComponent<HotelSite>().Configure((width * 0.5f - 0.2f) * s, (depth * 0.5f - 0.2f) * s, 3.2f * s, 1);
        foreach (float x in new[] { -3.4f, 3.4f })
            Nature.CoconutPalm(b, new Vector3(x, 0f, -depth * 0.5f - 2.2f), 0.55f, (int)(x * 10f) + 91);
    }
}
