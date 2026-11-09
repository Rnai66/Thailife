using UnityEngine;

// One house lot (แปลงบ้าน, home.lua) on the map: its number, the sign at its front with the number and
// who lives there, and a "ว่าง" board while nobody does. HouseLots puts the owner's house on it (a copy of
// that style's template). The lot's front (the soi) is its local -Z, like every house's door.
public class HouseLot : MonoBehaviour
{
    [SerializeField] private int number;
    [SerializeField] private TextMesh plate;     // บ้านเลขที่ N · owner
    [SerializeField] private GameObject vacant;  // the "ว่าง" board

    private GameObject house;
    private string houseStyle;
    private Transform built;    // what the owner built round the house (build.lua), for the neighbours to see
    private string builtKey;

    public int Number => number;
    public string Style => house != null ? houseStyle : null;

    public void Configure(int no, TextMesh plateText, GameObject vacantBoard)
    {
        number = no;
        plate = plateText;
        vacant = vacantBoard;
    }

    // Someone lives here: their house (a copy of the style's template) and their name on the sign
    public void Show(GameObject template, string style, string owner)
    {
        if (house == null || houseStyle != style)
        {
            if (house != null) Destroy(house);
            house = null;
            if (template != null)
            {
                house = Instantiate(template, transform);
                house.name = "House_" + style;
                house.transform.localPosition = Vector3.zero;
                house.transform.localRotation = Quaternion.identity;
                foreach (var plot in house.GetComponentsInChildren<HomePlot>(true)) plot.SetLot(number);
                house.SetActive(true);
            }
            houseStyle = style;
            built = null;
            builtKey = null;
        }
        if (plate != null) plate.text = string.IsNullOrEmpty(owner) ? $"บ้านเลขที่ {number}" : $"บ้านเลขที่ {number}\n{owner}";
        if (vacant != null) vacant.SetActive(false);
    }

    // The owner's fences, gates and so on, each where they put it relative to their house
    public void ShowBuilt(BuildClient.Piece[] pieces, string anchor = null)
    {
        string key = pieces == null || pieces.Length == 0 ? "" : anchor + JsonUtility.ToJson(new PieceList { items = pieces });
        if (key == builtKey && (key == "" || built != null)) return;
        builtKey = key;
        if (built != null) Destroy(built.gameObject);
        built = null;
        // round their lot's house, or a fixed one (BuiltPieces.AnchorFor) for pieces built before house lots
        var plot = !string.IsNullOrEmpty(anchor) ? BuiltPieces.AnchorFor(anchor)
                 : house != null && house.GetComponentInChildren<HomePlot>() != null ? house.GetComponentInChildren<HomePlot>().transform : null;
        if (key == "" || plot == null) return;
        built = new GameObject("BuiltPieces_เพื่อนบ้าน").transform; // (the name keeps them out of GroundAt)
        built.SetParent(plot, false);
        foreach (var p in pieces)
        {
            var go = BuildKit.Make(p.kind, p.color, built);
            if (go == null) continue;
            BuiltPieces.Place(go.transform, plot, p.x, p.z, p.yaw);
            go.name = "Built_" + p.id;
        }
    }

    [System.Serializable] private class PieceList { public BuildClient.Piece[] items; }

    // Where a visitor arrives: in the soi in front of the lot, facing it
    public void FrontPoint(out Vector3 at, out Quaternion facing)
    {
        float frontZ = plate != null ? plate.transform.parent.localPosition.z - 1.4f : -8f;
        at = transform.TransformPoint(new Vector3(0f, 0.1f, frontZ));
        facing = Quaternion.LookRotation(transform.forward, Vector3.up);
    }

    // Nobody lives here (or it isn't known yet: offline)
    public void Clear(bool known)
    {
        if (house != null) Destroy(house);
        house = null;
        houseStyle = null;
        built = null;
        builtKey = null;
        if (plate != null) plate.text = $"บ้านเลขที่ {number}";
        if (vacant != null) vacant.SetActive(known);
    }
}
