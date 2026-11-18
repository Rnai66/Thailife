using UnityEngine;

// ต้นกฐิน / ต้นผ้าป่า beside the amulet pavilion: a bamboo money tree on a teak stand, banknotes clipped
// to its branches and a gold umbrella on top, the folded robes and a pail of temple goods at its foot and
// a donation box. Walk up to it to give (TempleCounter "merit" → TempleUI ทำบุญ): open in the
// กฐิน and ผ้าป่า festivals, otherwise it says when the next one is.
public static partial class TempleLife
{
    private static void MeritTree(Transform parent, Vector3 at, float yaw)
    {
        var t = PrimitiveKit.Group("MeritTree_ต้นกฐินผ้าป่า", parent, at, yaw).transform;
        var teak = PrimitiveKit.Mat("Teak", new Color(0.45f, 0.27f, 0.13f));
        var gold = PrimitiveKit.Mat("Gold", new Color(0.95f, 0.75f, 0.25f));
        var bamboo = PrimitiveKit.Mat("MeritBamboo", new Color(0.72f, 0.62f, 0.32f), 0.3f);
        var robe = PrimitiveKit.Mat("MeritRobe", new Color(0.88f, 0.48f, 0.1f), 0.2f);
        Color[] notes = { new Color(0.45f, 0.65f, 0.4f), new Color(0.55f, 0.45f, 0.75f), new Color(0.85f, 0.4f, 0.35f), new Color(0.4f, 0.55f, 0.8f), new Color(0.6f, 0.45f, 0.3f) };

        // the stand
        PrimitiveKit.Box("Stand", t, new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.6f, 1.1f), teak);
        PrimitiveKit.Box("StandTop", t, new Vector3(0f, 0.62f, 0f), new Vector3(1.6f, 0.05f, 1.2f), gold, collider: false);
        PrimitiveKit.Box("Cloth", t, new Vector3(0f, 0.45f, -0.56f), new Vector3(1.5f, 0.3f, 0.02f), PrimitiveKit.Mat("MeritCloth", new Color(0.95f, 0.85f, 0.4f)), collider: false);

        // the tree: a bamboo pole, four tiers of twigs, notes clipped at the ends
        PrimitiveKit.Cylinder("Pole", t, new Vector3(0f, 1.75f, 0.1f), new Vector3(0.06f, 1.1f, 0.06f), bamboo, collider: false);
        int n = 0;
        for (int tier = 0; tier < 4; tier++)
        {
            float h = 1.2f + tier * 0.45f, reach = 0.55f - tier * 0.1f;
            int twigs = 6 - tier;
            for (int k = 0; k < twigs; k++)
            {
                float a = (k * 360f / twigs + tier * 25f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0.35f, Mathf.Sin(a));
                var mid = new Vector3(0f, h, 0.1f) + dir * reach * 0.5f;
                PrimitiveKit.Cylinder("Twig", t, mid, new Vector3(0.015f, reach * 0.55f, 0.015f), bamboo, Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, collider: false);
                var end = new Vector3(0f, h, 0.1f) + dir * reach;
                PrimitiveKit.Box("Note", t, end + Vector3.down * 0.06f, new Vector3(0.17f, 0.09f, 0.008f),
                    PrimitiveKit.Mat("MeritNote" + n % notes.Length, notes[n % notes.Length], 0.2f), new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 8f), collider: false);
                n++;
            }
        }
        // ฉัตร on the top
        PrimitiveKit.Cylinder("Umbrella", t, new Vector3(0f, 2.95f, 0.1f), new Vector3(0.5f, 0.03f, 0.5f), gold, collider: false);
        PrimitiveKit.Cylinder("Umbrella2", t, new Vector3(0f, 3.08f, 0.1f), new Vector3(0.32f, 0.03f, 0.32f), gold, collider: false);
        PrimitiveKit.Sphere("Finial", t, new Vector3(0f, 3.2f, 0.1f), Vector3.one * 0.08f, gold);

        // ไตรจีวร on a tray, a pail of temple goods
        PrimitiveKit.Cylinder("Tray", t, new Vector3(-0.45f, 0.67f, -0.2f), new Vector3(0.5f, 0.02f, 0.5f), gold, collider: false);
        for (int i = 0; i < 3; i++)
            PrimitiveKit.Box("Robe", t, new Vector3(-0.45f, 0.72f + i * 0.07f, -0.2f), new Vector3(0.36f - i * 0.04f, 0.06f, 0.26f - i * 0.03f), robe, collider: false);
        PrimitiveKit.Cylinder("Pail", t, new Vector3(0.48f, 0.82f, -0.2f), new Vector3(0.3f, 0.18f, 0.3f), PrimitiveKit.Mat("MeritPail", new Color(0.95f, 0.75f, 0.15f), 0.5f), collider: false);
        PrimitiveKit.Cylinder("PailWrap", t, new Vector3(0.48f, 1.05f, -0.2f), new Vector3(0.24f, 0.08f, 0.24f), PrimitiveKit.Mat("MeritWrap", new Color(0.9f, 0.9f, 0.85f), 0.6f), collider: false);

        // the donation box (ตู้ทำบุญ) beside the stand
        PrimitiveKit.Box("BoxLegs", t, new Vector3(1.2f, 0.35f, -0.1f), new Vector3(0.45f, 0.7f, 0.4f), teak);
        PrimitiveKit.Box("Box", t, new Vector3(1.2f, 0.95f, -0.1f), new Vector3(0.5f, 0.5f, 0.45f), PrimitiveKit.Mat("DisplayGlass", new Color(0.8f, 0.92f, 0.95f), 0.9f), collider: false);
        PrimitiveKit.Box("BoxLid", t, new Vector3(1.2f, 1.22f, -0.1f), new Vector3(0.54f, 0.05f, 0.49f), teak, collider: false);
        PrimitiveKit.Box("Slot", t, new Vector3(1.2f, 1.25f, -0.1f), new Vector3(0.22f, 0.01f, 0.03f), PrimitiveKit.Mat("SpeakerBlack", new Color(0.08f, 0.08f, 0.09f)), collider: false);
        for (int i = 0; i < 5; i++)
            PrimitiveKit.Box("Note", t, new Vector3(1.1f + i * 0.05f, 0.75f + (i % 2) * 0.03f, -0.1f), new Vector3(0.17f, 0.01f, 0.09f),
                PrimitiveKit.Mat("MeritNote" + i % notes.Length, notes[i % notes.Length], 0.2f), new Vector3(0f, i * 31f, 0f), collider: false);
        PrimitiveKit.Label("ตู้ทำบุญ", t, new Vector3(1.2f, 0.9f, -0.33f), 0.025f, new Color(0.6f, 0.08f, 0.08f));
        Places.Sign(t, new Vector3(-1.3f, 0f, -0.9f), "ต้นกฐิน · ต้นผ้าป่า\nร่วมทำบุญช่วงงาน ได้บารมีและ XP", 0f);

        var zone = PrimitiveKit.Group("MeritCounter", t, new Vector3(0f, 1f, -1.3f));
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 2f, 1.8f);
        zone.AddComponent<TempleCounter>().Configure(TempleUI.Merit);
    }
}
