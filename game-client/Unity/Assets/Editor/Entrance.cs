using UnityEngine;

// Modern, digital-age entrances for buildings (front faces local -Z): automatic sliding glass doors
// in a slim aluminium frame, an LED strip and a digital welcome screen above, a lit lobby behind the
// glass, a floor mat, and a touch-screen kiosk (queue tickets, e-Service, ATM, QR payment) beside it.
public static class Entrance
{
    private static readonly Color Aluminium = new Color(0.2f, 0.21f, 0.23f);

    // face = z of the wall's front surface; x = centre of the doorway
    public static void Modern(Transform b, float x, float face, float width, float height, Color accent,
        string screen = null, string kiosk = null, bool lobby = true)
    {
        var frame = PrimitiveKit.Mat("DoorAluminium", Aluminium);
        var glass = PrimitiveKit.Glow("DoorGlassModern", new Color(0.62f, 0.8f, 0.9f), 0.25f, 0.75f);
        var led = PrimitiveKit.Glow("LedStrip" + ColorKey(accent), accent, 2.5f);
        var steel = PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f));

        if (lobby) // warm light inside, seen when the doors open
            PrimitiveKit.Box("Lobby", b, new Vector3(x, height * 0.5f, face - 0.015f), new Vector3(width, height, 0.02f),
                PrimitiveKit.Glow("LobbyLight", new Color(1f, 0.95f, 0.85f), 0.8f), collider: false);

        // Two glass leaves that slide apart (AutoDoor)
        float leaf = width * 0.5f;
        var left = PrimitiveKit.Group("DoorLeafL", b, new Vector3(x - leaf * 0.5f, height * 0.5f, face - 0.06f)).transform;
        var right = PrimitiveKit.Group("DoorLeafR", b, new Vector3(x + leaf * 0.5f, height * 0.5f, face - 0.06f)).transform;
        foreach (var (t, side) in new[] { (left, 1f), (right, -1f) })
        {
            PrimitiveKit.Box("Glass", t, Vector3.zero, new Vector3(leaf - 0.03f, height - 0.04f, 0.04f), glass, collider: false);
            PrimitiveKit.Box("Rail", t, new Vector3(0f, -height * 0.5f + 0.04f, -0.01f), new Vector3(leaf - 0.03f, 0.08f, 0.05f), frame, collider: false);
            PrimitiveKit.Box("Handle", t, new Vector3(side * (leaf * 0.5f - 0.12f), 0f, -0.04f), new Vector3(0.03f, 0.8f, 0.03f), steel, collider: false);
        }

        // Slim frame, LED strip and the motion sensor
        foreach (float sx in new[] { -1f, 1f })
            PrimitiveKit.Box("DoorJamb", b, new Vector3(x + sx * (width * 0.5f + 0.06f), (height + 0.12f) * 0.5f, face - 0.07f), new Vector3(0.12f, height + 0.12f, 0.18f), frame, collider: false);
        PrimitiveKit.Box("DoorHeader", b, new Vector3(x, height + 0.08f, face - 0.07f), new Vector3(width + 0.36f, 0.16f, 0.18f), frame, collider: false);
        PrimitiveKit.Box("LedStrip", b, new Vector3(x, height + 0.2f, face - 0.1f), new Vector3(width + 0.36f, 0.05f, 0.05f), led, collider: false);
        PrimitiveKit.Sphere("MotionSensor", b, new Vector3(x, height + 0.08f, face - 0.17f), Vector3.one * 0.06f, PrimitiveKit.Glow("SensorRed", new Color(1f, 0.2f, 0.2f), 2f));

        if (!string.IsNullOrEmpty(screen)) // digital welcome screen
        {
            float w = Mathf.Min(width + 1.4f, 0.8f + screen.Length * 0.11f);
            PrimitiveKit.Box("WelcomeScreen", b, new Vector3(x, height + 0.6f, face - 0.08f), new Vector3(w, 0.5f, 0.06f),
                PrimitiveKit.Glow("ScreenDark", new Color(0.05f, 0.09f, 0.14f), 0.5f), collider: false);
            PrimitiveKit.Box("ScreenBezel", b, new Vector3(x, height + 0.6f, face - 0.06f), new Vector3(w + 0.08f, 0.58f, 0.04f), frame, collider: false);
            PrimitiveKit.Label(screen, b, new Vector3(x, height + 0.6f, face - 0.12f), 0.032f, Color.Lerp(accent, Color.white, 0.55f));
        }

        // Door mat and the automatic door's sensor zone
        PrimitiveKit.Box("DoorMat", b, new Vector3(x, 0.012f, face - 0.65f), new Vector3(width + 0.3f, 0.02f, 1f), PrimitiveKit.Mat("DoorMat", new Color(0.18f, 0.18f, 0.2f)), collider: false);
        var sensor = new GameObject("AutoDoorSensor");
        sensor.transform.SetParent(b, false);
        sensor.transform.localPosition = new Vector3(x, 1f, face - 1f);
        var zone = sensor.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(width + 1.4f, 2f, 2.4f);
        sensor.AddComponent<AutoDoor>().Configure(left, right, leaf - 0.05f);

        if (!string.IsNullOrEmpty(kiosk)) Kiosk(b, new Vector3(x + width * 0.5f + 0.9f, 0f, face - 0.45f), kiosk, accent);
    }

    // Touch-screen kiosk on a pedestal: queue tickets, e-Service, ATM or QR payment
    public static void Kiosk(Transform b, Vector3 at, string label, Color accent)
    {
        var white = PrimitiveKit.Mat("KioskWhite", new Color(0.95f, 0.96f, 0.97f));
        PrimitiveKit.Box("KioskBody", b, at + new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 0.36f), white);
        PrimitiveKit.Box("KioskScreen", b, at + new Vector3(0f, 1.22f, -0.06f), new Vector3(0.46f, 0.36f, 0.05f),
            PrimitiveKit.Glow("KioskScreen" + ColorKey(accent), Color.Lerp(accent, Color.white, 0.3f), 1.1f), new Vector3(-20f, 0f, 0f), collider: false);
        PrimitiveKit.Box("KioskSlot", b, at + new Vector3(0f, 0.85f, -0.19f), new Vector3(0.22f, 0.03f, 0.02f), PrimitiveKit.Mat("DoorAluminium", Aluminium), collider: false);
        PrimitiveKit.Box("KioskStripe", b, at + new Vector3(0f, 0.3f, -0.185f), new Vector3(0.5f, 0.06f, 0.01f), PrimitiveKit.Glow("LedStrip" + ColorKey(accent), accent, 2.5f), collider: false);
        PrimitiveKit.Label(label, b, at + new Vector3(0f, 1.55f, -0.05f), 0.022f, Color.Lerp(accent, Color.black, 0.35f));
    }

    // QR payment stand (ป้ายสแกนจ่าย) for a shop counter or market stall
    public static void QrStand(Transform b, Vector3 at, float yaw = 0f)
    {
        var s = PrimitiveKit.Group("QrStand_สแกนจ่าย", b, at, yaw).transform;
        PrimitiveKit.Cylinder("Pole", s, new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), PrimitiveKit.Mat("SteelFrame", new Color(0.7f, 0.72f, 0.75f)), collider: false);
        PrimitiveKit.Box("Board", s, new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.55f, 0.03f), PrimitiveKit.Mat("QrBlue", new Color(0.1f, 0.3f, 0.6f)), collider: false);
        PrimitiveKit.Box("QrCode", s, new Vector3(0f, 1.1f, -0.02f), new Vector3(0.28f, 0.28f, 0.01f), PrimitiveKit.Mat("KioskWhite", new Color(0.95f, 0.96f, 0.97f)), collider: false);
        for (int i = 0; i < 9; i++) // a few dark squares read as a QR code
            if (i % 2 == 0 || i == 3)
                PrimitiveKit.Box("QrDot", s, new Vector3(-0.09f + (i % 3) * 0.09f, 1.19f - (i / 3) * 0.09f, -0.026f), new Vector3(0.07f, 0.07f, 0.005f),
                    PrimitiveKit.Mat("DoorAluminium", Aluminium), collider: false);
        PrimitiveKit.Label("สแกนจ่าย", s, new Vector3(0f, 1.36f, -0.02f), 0.018f, Color.white);
    }

    // Digital menu board on a stand for an open-air restaurant, with a QR payment stand beside it
    public static void MenuBoard(Transform b, Vector3 at, string title, Color accent)
    {
        var frame = PrimitiveKit.Mat("DoorAluminium", Aluminium);
        foreach (float sx in new[] { -0.55f, 0.55f })
            PrimitiveKit.Box("MenuLeg", b, at + new Vector3(sx, 0.8f, 0f), new Vector3(0.06f, 1.6f, 0.06f), frame, collider: false);
        PrimitiveKit.Box("MenuScreen", b, at + new Vector3(0f, 1.5f, -0.02f), new Vector3(1.2f, 0.75f, 0.05f),
            PrimitiveKit.Glow("ScreenDark", new Color(0.05f, 0.09f, 0.14f), 0.5f), collider: false);
        PrimitiveKit.Box("MenuHeader", b, at + new Vector3(0f, 1.78f, -0.05f), new Vector3(1.1f, 0.14f, 0.01f), PrimitiveKit.Glow("LedStrip" + ColorKey(accent), accent, 2.5f), collider: false);
        PrimitiveKit.Label("เมนูวันนี้", b, at + new Vector3(0f, 1.6f, -0.06f), 0.025f, Color.white);
        PrimitiveKit.Label(title, b, at + new Vector3(0f, 1.4f, -0.06f), 0.018f, Color.Lerp(accent, Color.white, 0.5f));
        QrStand(b, at + new Vector3(1.1f, 0f, 0f));
    }

    private static string ColorKey(Color c) => $"{Mathf.RoundToInt(c.r * 9)}{Mathf.RoundToInt(c.g * 9)}{Mathf.RoundToInt(c.b * 9)}";
}
