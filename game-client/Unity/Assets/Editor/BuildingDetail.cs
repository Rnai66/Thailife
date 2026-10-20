using UnityEngine;

// Small things that make Thai town buildings look lived-in: air-conditioner units, rooftop water
// tanks, TV antennas, potted plants, drainpipes, window shades and windows lit from inside.
// All decoration: no colliders, and the same few shared materials so phones batch them.
public static class BuildingDetail
{
    private static Material Steel => PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));
    private static Material White => PrimitiveKit.Mat("AirConWhite", new Color(0.93f, 0.93f, 0.9f));
    private static Material Dark => PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f));

    // Window-type air conditioner hanging on the wall (faces -Z of the parent)
    public static void AirCon(Transform parent, Vector3 pos)
    {
        PrimitiveKit.Box("AirCon", parent, pos, new Vector3(0.8f, 0.55f, 0.35f), White, collider: false);
        PrimitiveKit.Box("AirConGrille", parent, pos + new Vector3(0f, 0f, -0.18f), new Vector3(0.65f, 0.4f, 0.02f), Steel, collider: false);
        PrimitiveKit.Box("AirConBracket", parent, pos + new Vector3(0f, -0.32f, 0.05f), new Vector3(0.8f, 0.05f, 0.4f), Dark, collider: false);
    }

    // Rooftop stainless water tank on a stand (แท็งก์น้ำ)
    public static void WaterTank(Transform parent, Vector3 pos)
    {
        foreach (float x in new[] { -0.5f, 0.5f })
            PrimitiveKit.Box("TankLeg", parent, pos + new Vector3(x, 0.4f, 0f), new Vector3(0.1f, 0.8f, 1f), Dark, collider: false);
        PrimitiveKit.Cylinder("WaterTank", parent, pos + new Vector3(0f, 1.45f, 0f), new Vector3(1.1f, 0.65f, 1.1f), Steel, collider: false);
        PrimitiveKit.Cylinder("TankLid", parent, pos + new Vector3(0f, 2.15f, 0f), new Vector3(0.5f, 0.06f, 0.5f), Steel, collider: false);
    }

    // Outdoor condenser units on a roof
    public static void Condenser(Transform parent, Vector3 pos)
    {
        PrimitiveKit.Box("Condenser", parent, pos + new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.4f), White, collider: false);
        PrimitiveKit.Cylinder("Fan", parent, pos + new Vector3(0.1f, 0.38f, -0.21f), new Vector3(0.45f, 0.02f, 0.45f), Dark, new Vector3(90f, 0f, 0f), collider: false);
    }

    // Old-style TV antenna
    public static void Antenna(Transform parent, Vector3 pos)
    {
        PrimitiveKit.Cylinder("AntennaMast", parent, pos + new Vector3(0f, 1.2f, 0f), new Vector3(0.05f, 1.2f, 0.05f), Steel, collider: false);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("AntennaBar", parent, pos + new Vector3(0f, 1.8f + i * 0.25f, 0f), new Vector3(1.1f - i * 0.25f, 0.03f, 0.03f), Steel, collider: false);
    }

    // Potted plant on a balcony or by a door
    public static void PottedPlant(Transform parent, Vector3 pos, int seed)
    {
        var pot = PrimitiveKit.Mat("ClayPot", new Color(0.72f, 0.38f, 0.22f));
        PrimitiveKit.Cylinder("Pot", parent, pos + new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.2f, 0.35f), pot, collider: false);
        var leaf = PrimitiveKit.Mat(seed % 2 == 0 ? "PlantGreen" : "PlantGreenDark", seed % 2 == 0 ? new Color(0.3f, 0.62f, 0.25f) : new Color(0.18f, 0.45f, 0.2f));
        MeshKit.Place("Leaves", parent, MeshKit.Blob("PlantBlob" + (seed % 4), 60 + seed % 4, 0.3f, 1), leaf, pos + new Vector3(0f, 0.6f, 0f), default, new Vector3(0.6f, 0.6f, 0.6f));
    }

    // Rain drainpipe down a wall
    public static void Drainpipe(Transform parent, Vector3 top, float height)
    {
        PrimitiveKit.Cylinder("Drainpipe", parent, top - new Vector3(0f, height * 0.5f, 0f), new Vector3(0.12f, height * 0.5f, 0.12f), PrimitiveKit.Mat("PipeGrey", new Color(0.55f, 0.57f, 0.6f)), collider: false);
    }

    // A glass pane: some windows glow warm from inside (lights on), some have a striped shade
    public static Material WindowGlass(int index)
    {
        int k = (index * 7 + 3) % 10;
        if (k < 3) return PrimitiveKit.Glow("WindowLit", new Color(1f, 0.82f, 0.5f), 1.1f, 0.6f);
        if (k == 3) return PrimitiveKit.Mat("WindowShade", new Color(0.85f, 0.7f, 0.45f));
        return PrimitiveKit.Mat("WindowGlass", new Color(0.35f, 0.5f, 0.62f));
    }
}
