using UnityEngine;

// A parcel of land in the scene (built by the editor's LandParcels): shows it the way it is for this
// player. Children named "ForSale" (not yours yet: a for-sale board), "Owned" (yours) and
// "Build_<id>" (what you built on it: field, pen, rental, shophouse) are switched on and off.
public class LandSite : MonoBehaviour
{
    [SerializeField] private string parcel = "plain"; // plain | rice | city

    private GameObject forSale, owned;
    private GameObject[] builds;
    private LandClient land;

    public string Parcel => parcel;
    public void Configure(string parcelId) => parcel = parcelId;

    private void Awake()
    {
        forSale = Find("ForSale");
        owned = Find("Owned");
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform) if (child.name.StartsWith("Build_")) list.Add(child.gameObject);
        builds = list.ToArray();
        Show(false, "");
    }

    private GameObject Find(string name)
    {
        var t = transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    private void Start()
    {
        land = LandClient.Instance;
        if (land == null) return;
        land.Changed += OnChanged;
        if (land.Current != null) OnChanged(land.Current);
    }

    private void OnDestroy()
    {
        if (land != null) land.Changed -= OnChanged;
    }

    private void OnChanged(LandClient.State s)
    {
        bool mine = parcel == "plain" ? s.own_plain : parcel == "rice" ? s.own_rice : s.own_city;
        string build = parcel == "plain" ? s.plain_build : parcel == "city" ? s.city_build : "";
        Show(mine, build);
    }

    private void Show(bool mine, string build)
    {
        if (forSale != null) forSale.SetActive(!mine);
        if (owned != null) owned.SetActive(mine);
        foreach (var b in builds) b.SetActive(mine && b.name == "Build_" + build);
    }
}
