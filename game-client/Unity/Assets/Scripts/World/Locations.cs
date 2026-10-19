using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Administrative levels of the Thai world map.
public enum LocationLevel
{
    Village,      // หมู่บ้าน
    Subdistrict,  // ตำบล
    City,         // เมือง (อำเภอ/จังหวัด)
}

// One travel destination. Each is its own scene with its own online match.
public class Location
{
    public string Id;
    public string NameTh;
    public LocationLevel Level;
    public string Scene;      // null = not built yet ("เร็วๆ นี้")
    public string SpawnId;    // SpawnPoint to arrive at

    public bool Available => !string.IsNullOrEmpty(Scene);
}

// The world map. Add new places here and build their scene in the Editor.
public static class Locations
{
    public static readonly IReadOnlyList<Location> All = new List<Location>
    {
        new Location { Id = "village_khoko", NameTh = "หมู่บ้าน", Level = LocationLevel.Village, Scene = "ThaiVillage", SpawnId = "busstop" },
        new Location { Id = "tambon_khoko", NameTh = "ตำบล · อบต. รพ.สต. โรงเรียน คลองตกปลา", Level = LocationLevel.Subdistrict, Scene = "Tambon", SpawnId = "busstop" },
        new Location { Id = "city", NameTh = "ตัวเมือง · ค่ายมวย หอนาฬิกา", Level = LocationLevel.City, Scene = "City", SpawnId = "busstop" },
    };

    public static string LevelNameTh(LocationLevel level) => level switch
    {
        LocationLevel.Village => "หมู่บ้าน",
        LocationLevel.Subdistrict => "ตำบล",
        _ => "เมือง",
    };

    public static Location Current
    {
        get
        {
            string scene = SceneManager.GetActiveScene().name;
            foreach (var location in All)
                if (location.Scene == scene) return location;
            return null;
        }
    }

    // Spawn point to use after the next scene load (set by TravelMenu, read by SpawnPoint)
    public static string PendingSpawnId { get; set; }

    public static void TravelTo(Location destination)
    {
        if (destination == null || !destination.Available) return;
        PendingSpawnId = destination.SpawnId;
        SceneManager.LoadScene(destination.Scene);
    }
}
