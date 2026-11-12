using UnityEngine;

// Where your own vehicle is: under the stilt house at home (nothing saved), or wherever you last got
// out of it — another spot in the village, a car park in town, another province you drove to. Kept on
// this device (PlayerPrefs); ParkedCarSpawner puts it back there when you come to that map, and
// HomeCar leaves the carport empty while it's away.
public static class ParkedCar
{
    private const string KScene = "car.scene", KId = "car.id", KName = "car.name", KX = "car.x", KY = "car.y", KZ = "car.z", KYaw = "car.yaw", KSpawn = "car.spawn";

    // Set by DriveJourney before the map loads: arrive in this vehicle (id) at the petrol station
    public static string Arriving { get; set; }

    public static bool Away => PlayerPrefs.HasKey(KScene);
    public static string Scene => PlayerPrefs.GetString(KScene, "");
    public static string Id => PlayerPrefs.GetString(KId, "");
    public static string Name => PlayerPrefs.GetString(KName, "");
    public static bool AtSpawn => PlayerPrefs.GetInt(KSpawn, 0) == 1; // left at the map's drive-in spot (arrived, not yet moved)
    public static Vector3 Position => new Vector3(PlayerPrefs.GetFloat(KX), PlayerPrefs.GetFloat(KY), PlayerPrefs.GetFloat(KZ));
    public static float Yaw => PlayerPrefs.GetFloat(KYaw);

    public static void Save(string scene, string id, string name, Vector3 at, float yaw)
    {
        PlayerPrefs.SetString(KScene, scene);
        PlayerPrefs.SetString(KId, id);
        PlayerPrefs.SetString(KName, name ?? "");
        PlayerPrefs.SetFloat(KX, at.x);
        PlayerPrefs.SetFloat(KY, at.y);
        PlayerPrefs.SetFloat(KZ, at.z);
        PlayerPrefs.SetFloat(KYaw, yaw);
        PlayerPrefs.SetInt(KSpawn, 0);
        PlayerPrefs.Save();
    }

    // Driving off to another map: the car is there (at its drive-in spot) from now on, even if the
    // app is closed on the way
    public static void SendTo(string scene, string id, string name)
    {
        Save(scene, id, name, Vector3.zero, 0f);
        PlayerPrefs.SetInt(KSpawn, 1);
        PlayerPrefs.Save();
    }

    // Back under the house
    public static void Home()
    {
        foreach (var k in new[] { KScene, KId, KName, KX, KY, KZ, KYaw, KSpawn }) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }
}
