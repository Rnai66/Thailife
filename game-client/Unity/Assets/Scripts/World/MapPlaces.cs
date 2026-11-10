using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

// The places on a map worth going to (for the ride map, RideMapUI): found by the names the scene
// builders give them ("WatArun_วัดอรุณ", "ClockTower_หอนาฬิกา"…) — only the kinds listed here, each with
// the name to show and a picture (Resources/Branding/Icons). Rivers come back too, to paint on the map.
public static class MapPlaces
{
    public class Place
    {
        public string name, icon;
        public Vector3 at;      // the middle of it, on the ground
        public Bounds area;     // its footprint
        public bool water;      // a river or canal (drawn, not a destination)
    }

    // prefix -> (name to show, or null for the scene's own Thai name; icon)
    private static readonly Dictionary<string, (string name, string icon)> Kinds = new Dictionary<string, (string, string)>
    {
        // กรุงเทพฯ
        { "WatPhraKaew", (null, "temple") }, { "WatArun", (null, "temple") }, { "Shrine", (null, "temple") },
        { "Airport", (null, "travel") }, { "SanamLuang", (null, "fair") }, { "FleaMarket", ("ตลาดนัด", "market") },
        { "BargeFestival", ("ลานชมขบวนเรือ", "fair") }, { "Bridge", (null, "city") }, { "RoyalLanding", (null, "temple") },
        { "Townhomes", (null, "home") },
        // เมือง
        { "CityTemple", ("วัดกลางเมือง", "temple") }, { "ClockTower", (null, "city") }, { "BoxingCamp", (null, "boxing") },
        { "Station", ("สถานีรถไฟ", "travel") }, { "Hospital", (null, "hospital") }, { "Police", (null, "city") },
        { "Bank", (null, "shop") }, { "FreshMarket", (null, "market") }, { "Hotel", (null, "hotel") },
        { "Church", (null, "temple") }, { "Mosque", (null, "temple") }, { "RedCrossFair", (null, "fair") },
        { "District", ("ที่ว่าการอำเภอ", "city") }, { "PAO", ("องค์การบริหารส่วนจังหวัด", "city") }, { "Kokai", ("มหาวิทยาลัย", "school") },
        { "GoldZone", (null, "shop") }, { "PawnShop", (null, "shop") }, { "FoodCourt", (null, "food") },
        { "NoodleShop", (null, "food") }, { "Tailor", (null, "tailor") }, { "Barber", (null, "barber") },
        { "CoachZone", ("สถานีขนส่ง · รถทัวร์", "travel") }, { "BusTerminal", (null, "travel") }, { "Townhouses", (null, "home") },
        // ต่างจังหวัด: the landmarks (ProvinceKit.Attraction)
        { "AttrTemple", (null, "temple") }, { "AttrShrine", (null, "temple") }, { "AttrNature", (null, "farm") },
        { "AttrCity", (null, "city") }, { "AttrSport", (null, "fair") }, { "AttrMarket", (null, "market") },
        { "AttrBeach", (null, "island") }, { "WelcomeArch", (null, "city") }, { "Monument", (null, "city") },
        // ทุกที่
        { "School", (null, "school") }, { "Market", (null, "market") }, { "Temple", (null, "temple") }, { "Beach", (null, "island") },
    };

    private static readonly Regex Named = new Regex(@"^([A-Za-z0-9]+)_(.+)$");

    public static List<Place> Find()
    {
        var list = new List<Place>();
        var seen = new HashSet<string>();
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            Walk(root.transform, list, seen, 0);
        return list;
    }

    private static void Walk(Transform t, List<Place> list, HashSet<string> seen, int depth)
    {
        if (!t.gameObject.activeInHierarchy || depth > 6) return;
        var m = Named.Match(t.name);
        if (m.Success)
        {
            string prefix = m.Groups[1].Value, thai = m.Groups[2].Value;
            bool water = thai.Contains("แม่น้ำ") || thai.Contains("คลอง") || prefix == "River" || prefix == "Canal" || prefix == "ChaoPhraya";
            if (water || Kinds.ContainsKey(prefix))
            {
                var area = Footprint(t);
                string name = water ? thai : Kinds[prefix].name ?? thai;
                if (area.size.x > 2f && seen.Add(name))
                {
                    var at = area.center;
                    at.y = area.min.y;
                    list.Add(new Place { name = name, icon = water ? null : Kinds[prefix].icon, at = at, area = area, water = water });
                }
                return; // not inside it (its own parts aren't places)
            }
        }
        foreach (Transform c in t) Walk(c, list, seen, depth + 1);
    }

    private static Bounds Footprint(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 4f);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
