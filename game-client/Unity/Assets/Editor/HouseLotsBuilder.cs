using System;
using UnityEngine;

// The house lots of a map (home.lua LOTS, HouseLots/HouseLot at runtime): each lot is a group at its
// centre turned so its front (the soi) is local -Z, with corner pegs, a sign at the front (บ้านเลขที่ N
// and who lives there) and a "ว่าง" board. The houses players can live in here are built once under
// "HouseTemplates" (kept inactive) and copied onto the lots people own when the game runs.
public static class HouseLotsBuilder
{
    public struct Lot
    {
        public int no;
        public Vector3 at;
        public float yaw, width, depth; // width along the soi, depth back from it

        public Lot(int no, float x, float z, float yaw, float width, float depth)
        {
            this.no = no;
            at = new Vector3(x, 0f, z);
            this.yaw = yaw;
            this.width = width;
            this.depth = depth;
        }
    }

    public static Transform Build(Transform parent, string area, Lot[] lots, Action<Transform> templates, bool lawn)
    {
        var root = PrimitiveKit.Group("HouseLots_" + area, parent, Vector3.zero).transform;
        var tpl = PrimitiveKit.Group("HouseTemplates", root, new Vector3(0f, -40f, 0f)).transform; // out of sight
        templates(tpl);
        tpl.gameObject.SetActive(false);
        root.gameObject.AddComponent<HouseLots>().Configure(area, tpl);

        var peg = PrimitiveKit.Mat("LotPeg", new Color(0.92f, 0.92f, 0.9f));
        var board = PrimitiveKit.Mat("PlasterWhite", new Color(0.93f, 0.92f, 0.88f));
        var post = PrimitiveKit.Mat("DarkWood", new Color(0.36f, 0.22f, 0.12f));
        var red = PrimitiveKit.Mat("LotVacantRed", new Color(0.75f, 0.18f, 0.14f));
        var grass = PrimitiveKit.Mat("Grass", new Color(0.36f, 0.56f, 0.24f));
        foreach (var l in lots)
        {
            var t = PrimitiveKit.Group("Lot_" + l.no, root, l.at, l.yaw).transform;
            float hw = l.width * 0.5f, hd = l.depth * 0.5f;
            if (lawn)
                PrimitiveKit.Box("LotGround", t, new Vector3(0f, 0.022f, 0f), new Vector3(l.width - 0.4f, 0.02f, l.depth - 0.4f), grass, collider: false);
            foreach (var c in new[] { new Vector3(-hw + 0.2f, 0f, -hd + 0.2f), new Vector3(hw - 0.2f, 0f, -hd + 0.2f), new Vector3(-hw + 0.2f, 0f, hd - 0.2f), new Vector3(hw - 0.2f, 0f, hd - 0.2f) })
                PrimitiveKit.Box("LotPeg", t, c + new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), peg, collider: false);

            // บ้านเลขที่: a small board on a post at the front corner, facing the soi
            var sign = PrimitiveKit.Group("LotSign", t, new Vector3(hw - 0.8f, 0f, -hd + 0.5f)).transform;
            PrimitiveKit.Box("Post", sign, new Vector3(0f, 0.55f, 0.05f), new Vector3(0.1f, 1.1f, 0.1f), post);
            PrimitiveKit.Box("Board", sign, new Vector3(0f, 1.25f, 0f), new Vector3(1.3f, 0.6f, 0.05f), board, collider: false);
            var plate = PrimitiveKit.Label("บ้านเลขที่ " + l.no, sign, new Vector3(0f, 1.25f, -0.04f), 0.022f, new Color(0.12f, 0.15f, 0.25f));

            // ว่าง: a red board on a stake in the middle of the empty lot
            var vacant = PrimitiveKit.Group("LotVacant", t, new Vector3(0f, 0f, -hd + 2.5f)).transform;
            PrimitiveKit.Box("Stake", vacant, new Vector3(0f, 0.5f, 0.05f), new Vector3(0.08f, 1f, 0.08f), post, collider: false);
            PrimitiveKit.Box("Board", vacant, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 0.5f, 0.04f), red, collider: false);
            PrimitiveKit.Label("ว่าง\nแปลง " + l.no, vacant, new Vector3(0f, 1.1f, -0.03f), 0.02f, Color.white);
            vacant.gameObject.SetActive(false); // shown once the server says nobody lives here

            t.gameObject.AddComponent<HouseLot>().Configure(l.no, plate, vacant.gameObject);
        }
        return root;
    }

    // One template: a group "Tpl_{style}" at the lot centre's frame; the house is built inside it
    public static Transform Template(Transform templates, string style) =>
        PrimitiveKit.Group("Tpl_" + style, templates, Vector3.zero).transform;
}
