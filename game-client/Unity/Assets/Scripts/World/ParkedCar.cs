using System.Collections.Generic;
using UnityEngine;

// Where each of your own vehicles is: at home (nothing saved: HomeCar parks it under the house or in a
// garage you built), or wherever you last got out of it — another spot in the village, a car park in
// town, another province you drove to. Kept on this device (PlayerPrefs), one record per vehicle id;
// ParkedCarSpawner puts the ones on this map back there, and HomeCar leaves them out of the carport.
public static class ParkedCar
{
    private const string KAway = "car.away"; // the ids away from home, comma separated
    // the old one-vehicle keys (before every vehicle had its own record)
    private const string LScene = "car.scene", LId = "car.id", LName = "car.name", LX = "car.x", LY = "car.y", LZ = "car.z", LYaw = "car.yaw", LSpawn = "car.spawn";

    // Set by DriveJourney before the map loads: arrive in this vehicle (id) at the petrol station
    public static string Arriving { get; set; }

    private static string K(string id, string field) => "car." + id + "." + field;

    public static List<string> AwayIds()
    {
        Migrate();
        var list = new List<string>();
        foreach (var s in PlayerPrefs.GetString(KAway, "").Split(','))
            if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
        return list;
    }

    public static bool IsAway(string id) => !string.IsNullOrEmpty(id) && AwayIds().Contains(id);
    public static string SceneOf(string id) => PlayerPrefs.GetString(K(id, "scene"), "");
    public static string NameOf(string id) => PlayerPrefs.GetString(K(id, "name"), "");
    public static bool AtSpawn(string id) => PlayerPrefs.GetInt(K(id, "spawn"), 0) == 1; // left at the map's drive-in spot
    public static Vector3 PositionOf(string id) => new Vector3(PlayerPrefs.GetFloat(K(id, "x")), PlayerPrefs.GetFloat(K(id, "y")), PlayerPrefs.GetFloat(K(id, "z")));
    public static float YawOf(string id) => PlayerPrefs.GetFloat(K(id, "yaw"));

    public static void Save(string scene, string id, string name, Vector3 at, float yaw)
    {
        if (string.IsNullOrEmpty(id)) return;
        Migrate();
        PlayerPrefs.SetString(K(id, "scene"), scene);
        PlayerPrefs.SetString(K(id, "name"), name ?? "");
        PlayerPrefs.SetFloat(K(id, "x"), at.x);
        PlayerPrefs.SetFloat(K(id, "y"), at.y);
        PlayerPrefs.SetFloat(K(id, "z"), at.z);
        PlayerPrefs.SetFloat(K(id, "yaw"), yaw);
        PlayerPrefs.SetInt(K(id, "spawn"), 0);
        var away = AwayIds();
        if (!away.Contains(id)) away.Add(id);
        PlayerPrefs.SetString(KAway, string.Join(",", away));
        PlayerPrefs.Save();
    }

    // Driving off to another map: the vehicle is there (at its drive-in spot) from now on, even if the
    // app is closed on the way
    public static void SendTo(string scene, string id, string name)
    {
        Save(scene, id, name, Vector3.zero, 0f);
        PlayerPrefs.SetInt(K(id, "spawn"), 1);
        PlayerPrefs.Save();
    }

    // Back home
    public static void Home(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var away = AwayIds();
        away.Remove(id);
        PlayerPrefs.SetString(KAway, string.Join(",", away));
        foreach (var f in new[] { "scene", "name", "x", "y", "z", "yaw", "spawn" }) PlayerPrefs.DeleteKey(K(id, f));
        PlayerPrefs.Save();
    }

    // The one vehicle saved the old way becomes that vehicle's own record
    private static void Migrate()
    {
        if (!PlayerPrefs.HasKey(LScene)) return;
        string id = PlayerPrefs.GetString(LId, "");
        if (!string.IsNullOrEmpty(id))
        {
            PlayerPrefs.SetString(K(id, "scene"), PlayerPrefs.GetString(LScene, ""));
            PlayerPrefs.SetString(K(id, "name"), PlayerPrefs.GetString(LName, ""));
            PlayerPrefs.SetFloat(K(id, "x"), PlayerPrefs.GetFloat(LX));
            PlayerPrefs.SetFloat(K(id, "y"), PlayerPrefs.GetFloat(LY));
            PlayerPrefs.SetFloat(K(id, "z"), PlayerPrefs.GetFloat(LZ));
            PlayerPrefs.SetFloat(K(id, "yaw"), PlayerPrefs.GetFloat(LYaw));
            PlayerPrefs.SetInt(K(id, "spawn"), PlayerPrefs.GetInt(LSpawn, 0));
            string away = PlayerPrefs.GetString(KAway, "");
            if (("," + away + ",").IndexOf("," + id + ",", System.StringComparison.Ordinal) < 0)
                PlayerPrefs.SetString(KAway, string.IsNullOrEmpty(away) ? id : away + "," + id);
        }
        foreach (var k in new[] { LScene, LId, LName, LX, LY, LZ, LYaw, LSpawn }) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }
}
