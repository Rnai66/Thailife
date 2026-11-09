using System.Collections.Generic;
using UnityEngine;

// One farm lot (แปลงเกษตร, farmlot.lua) south of the village: four corners ("Slot_1".."Slot_4": front
// left, front right, back left, back right, each 12 x 11 m facing the lot's front, local -Z) that take
// the owner's zones, four sides that take their fences, a sign with the lot number and owner, a "ว่าง"
// board, and the spot by the sign where you arrange your own farm (FarmLotCounter, yours only).
// A neighbour's farm is only to look at: the parts that would work your own rice, fish, cows and
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
    private string shown;

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

    public void Show(FarmLots owner, string[] layout, FarmLots.Fences fences, string ownerName, bool mine)
    {
        fences ??= new FarmLots.Fences();
        string[] f = { fences.front, fences.back, fences.left, fences.right };
        string key = (layout != null ? string.Join(",", layout) : "") + "|" + string.Join(",", f) + "|" + mine;
        if (key != shown)
        {
            Remove();
            shown = key;
            for (int i = 0; i < slots.Length && layout != null && i < layout.Length; i++)
                Place(owner.Template("Tpl_" + layout[i]), slots[i], mine);
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
    }

    public void Clear(bool known)
    {
        Remove();
        shown = null;
        if (plate != null) plate.text = $"แปลงเกษตร {number}";
        if (vacant != null) vacant.SetActive(known);
        if (manage != null) manage.SetActive(false);
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
