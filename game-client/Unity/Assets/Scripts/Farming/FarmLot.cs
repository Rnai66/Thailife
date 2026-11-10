using System.Collections.Generic;
using UnityEngine;

// One farm lot (แปลงเกษตร, farmlot.lua) south of the village: four corners ("Slot_1".."Slot_4": front
// left, front right, back left, back right, each 12 x 11 m facing the lot's front, local -Z) that take
// the owner's zones, four sides that take their fences, a sign with the lot number and owner, a "ว่าง"
// board, and the spot by the sign where you arrange your own farm (FarmLotCounter, yours only).
// What the owner built on it (fences, gates…: build.lua site "farm") stands on it too; your own are
// drawn by BuiltPieces. A neighbour's farm is only to look at: the parts that would work your own rice, fish, cows and
// chickens are taken off their copy before it wakes up.
public class FarmLot : MonoBehaviour
{
    public static readonly string[] Sides = { "front", "back", "left", "right" };

    [SerializeField] private int number;
    [SerializeField] private TextMesh plate;
    [SerializeField] private GameObject vacant;
    [SerializeField] private GameObject manage;
    [SerializeField] private Transform[] slots = new Transform[4];
    [SerializeField] private Transform[] sides = new Transform[4]; // front, back, left, right

    private readonly List<GameObject> built = new List<GameObject>();
    private string shown, builtKey;
    private Transform builtRoot;

    public int Number => number;

    public void Configure(int no, TextMesh plateText, GameObject vacantBoard, GameObject manageSpot, Transform[] slotPoints, Transform[] sidePoints)
    {
        number = no;
        plate = plateText;
        vacant = vacantBoard;
        manage = manageSpot;
        slots = slotPoints;
        sides = sidePoints;
    }

    // Returns whether anything had to be built (the caller spreads that over frames)
    public bool Show(FarmLots owner, string[] layout, FarmLots.Fences fences, string ownerName, bool mine, BuildClient.Piece[] pieces = null)
    {
        bool rebuilt = false;
        ShowBuilt(pieces);
        fences ??= new FarmLots.Fences();
        string[] f = { fences.front, fences.back, fences.left, fences.right };
        string key = (layout != null ? string.Join(",", layout) : "") + "|" + string.Join(",", f) + "|" + mine;
        if (key != shown)
        {
            rebuilt = true;
            Remove();
            shown = key;
            for (int i = 0; i < slots.Length && layout != null && i < layout.Length; i++)
                Place(owner.Template("Tpl_" + layout[i]), slots[i], mine);
            Sounds(layout);
            for (int i = 0; i < sides.Length; i++)
            {
                if (string.IsNullOrEmpty(f[i]) || f[i] == "none") continue;
                string part = i == 0 ? "front" : i == 1 ? "back" : "side";
                Place(owner.Template($"Fence_{f[i]}_{part}"), sides[i], true);
            }
        }
        if (plate != null) plate.text = string.IsNullOrEmpty(ownerName) ? $"แปลงเกษตร {number}" : $"แปลงเกษตร {number}\n{ownerName}";
        if (vacant != null) vacant.SetActive(false);
        if (manage != null) manage.SetActive(mine);
        return rebuilt;
    }

    public void Clear(bool known)
    {
        ShowBuilt(null);
        Remove();
        shown = null;
        if (plate != null) plate.text = $"แปลงเกษตร {number}";
        if (vacant != null) vacant.SetActive(known);
        if (manage != null) manage.SetActive(false);
        Sounds(null);
    }

    // เสียงบรรยากาศ: walking onto the lot you hear what's on it — frogs and crickets in the paddies,
    // frogs by the pond, the cows lowing, the hens and the rooster — now and then while you stay
    private void Sounds(string[] layout)
    {
        var zone = GetComponent<AmbientZone>();
        var list = new List<string>();
        if (layout != null)
            foreach (var m in layout)
                switch (m)
                {
                    case "rice": list.Add("frog"); list.Add("crickets"); break;
                    case "pond": list.Add("frog"); break;
                    case "cows": list.Add("cow"); break;
                    case "coop": list.Add("hen"); list.Add("rooster"); list.Add("hen"); break;
                }
        if (list.Count == 0) { if (zone != null) zone.enabled = false; return; }
        var area = new Bounds(transform.position, Vector3.one);
        foreach (var s in slots)
            if (s != null) { area.Encapsulate(s.position + new Vector3(6f, 0f, 6f)); area.Encapsulate(s.position - new Vector3(6f, 0f, 6f)); }
        if (zone == null) zone = gameObject.AddComponent<AmbientZone>();
        zone.enabled = true;
        zone.Configure(area, 2f, 25f, 50f, 0.7f, list.ToArray());
        foreach (var s in list) SynthSounds.Prepare(s);
    }

    [System.Serializable] private class PieceList { public BuildClient.Piece[] items; }

    private void ShowBuilt(BuildClient.Piece[] pieces)
    {
        string key = pieces == null || pieces.Length == 0 ? "" : JsonUtility.ToJson(new PieceList { items = pieces });
        if (key == builtKey && (key == "" || builtRoot != null)) return;
        builtKey = key;
        if (builtRoot != null) Destroy(builtRoot.gameObject);
        builtRoot = null;
        if (key == "") return;
        builtRoot = new GameObject("BuiltPieces_เพื่อนบ้าน").transform; // (the name keeps them out of GroundAt)
        builtRoot.SetParent(transform, false);
        foreach (var p in pieces)
        {
            var go = BuildKit.Make(p.kind, p.color, builtRoot);
            if (go == null) continue;
            BuiltPieces.Place(go.transform, transform, p.x, p.z, p.yaw);
            go.name = "Built_" + p.id;
        }
    }

    private void Remove()
    {
        foreach (var go in built) if (go != null) Destroy(go);
        built.Clear();
    }

    private void Place(GameObject template, Transform at, bool working)
    {
        if (template == null || at == null) return;
        var copy = Instantiate(template, at); // inactive, like its template: nothing has woken up yet
        copy.name = template.name;
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        if (!working) Strip(copy);
        copy.SetActive(true);
        built.Add(copy);
    }

    // A neighbour's farm: no paddies, cages, pens or coop that would show or work your own
    private static void Strip(GameObject copy)
    {
        foreach (var c in copy.GetComponentsInChildren<FarmPlot>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<FishPond>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<CowPen>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<RanchCounter>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<WindowSpot>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<ChickenPen>(true)) DestroyImmediate(c);
        foreach (var c in copy.GetComponentsInChildren<CowWander>(true)) c.gameObject.SetActive(false);
        foreach (var c in copy.GetComponentsInChildren<LandSite>(true))
        {
            var owned = c.transform.Find("Owned");
            var sale = c.transform.Find("ForSale");
            if (owned != null) owned.gameObject.SetActive(true);
            if (sale != null) sale.gameObject.SetActive(false);
            DestroyImmediate(c);
        }
    }
}
