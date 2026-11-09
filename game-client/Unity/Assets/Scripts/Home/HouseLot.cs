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
        }
        if (plate != null) plate.text = string.IsNullOrEmpty(owner) ? $"บ้านเลขที่ {number}" : $"บ้านเลขที่ {number}\n{owner}";
        if (vacant != null) vacant.SetActive(false);
    }

    // Nobody lives here (or it isn't known yet: offline)
    public void Clear(bool known)
    {
        if (house != null) Destroy(house);
        house = null;
        houseStyle = null;
        if (plate != null) plate.text = $"บ้านเลขที่ {number}";
        if (vacant != null) vacant.SetActive(known);
    }
}
