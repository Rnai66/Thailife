using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Administrative levels of the Thai world map.
public enum LocationLevel
{
    Village,      // หมู่บ้าน
    Subdistrict,  // ตำบล
    City,         // เมือง (อำเภอ/จังหวัด)
    Beach,        // ทะเล (ท่องเที่ยว)
    Province,     // ต่างจังหวัด (รถทัวร์ รถไฟ เครื่องบิน)
}

// Ways to reach another province (travel.lua prices each)
public static class TravelMode
{
    public const string Bus = "bus", Train = "train", Air = "air";
}

// One travel destination. Each is its own scene with its own online match.
public class Location
{
    public string Id;
    public string NameTh;
    public LocationLevel Level;
    public string Scene;      // null = not built yet ("เร็วๆ นี้")
    public string SpawnId;    // SpawnPoint to arrive at
    public string Icon;       // Resources/Branding/Icons picture (null = by level)
    public bool BoatOnly;     // only reached by boat from the beach (not on the songthaew map)
    public bool Intercity;    // on the long-distance map (bus terminal, station, airport)
    public bool Airport;      // has an airport (flights between provinces)

    public bool Available => !string.IsNullOrEmpty(Scene);
}

// The world map. Add new places here and build their scene in the Editor.
public static class Locations
{
    public static readonly IReadOnlyList<Location> All = new List<Location>
    {
        new Location { Id = "village_khoko", NameTh = "หมู่บ้าน", Level = LocationLevel.Village, Scene = "ThaiVillage", SpawnId = "busstop" },
        new Location { Id = "tambon_khoko", NameTh = "ตำบล · อบต. รพ.สต. โรงเรียน คลองตกปลา", Level = LocationLevel.Subdistrict, Scene = "Tambon", SpawnId = "busstop" },
        new Location { Id = "city", NameTh = "ตัวเมือง · ค่ายมวย หอนาฬิกา", Level = LocationLevel.City, Scene = "City", SpawnId = "busstop", Intercity = true },
        new Location { Id = "beach", NameTh = "ทะเล · ชายหาด ท่าเรือประมง ตกปลาทะเล", Level = LocationLevel.Beach, Scene = "Beach", SpawnId = "busstop" },
        new Location { Id = "island", NameTh = "เกาะ · อ่าวมาหยา ชิงช้า หาสมบัติ เรือใบ", Level = LocationLevel.Beach, Scene = "Island", SpawnId = "ferry", Icon = "island", BoatOnly = true },
        // Other provinces: reached by bus, train or plane; spawn by how you came (bus / train / air)
        new Location { Id = "bangkok", NameTh = "กรุงเทพฯ · วัดพระแก้ว วัดอรุณ สุวรรณภูมิ", Level = LocationLevel.Province, Scene = "Bangkok", Intercity = true, Airport = true },
        new Location { Id = "chiangmai", NameTh = "เชียงใหม่ · ดอยสุเทพ ถนนคนเดิน", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "pattaya", NameTh = "พัทยา · ชายหาด วอล์กกิ้งสตรีท", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "khonkaen", NameTh = "ขอนแก่น · บึงแก่นนคร พระมหาธาตุ", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "ubon", NameTh = "อุบลราชธานี · แห่เทียนพรรษา แม่น้ำมูล", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "buriram", NameTh = "บุรีรัมย์ · สนามช้าง ปราสาทพนมรุ้ง", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "songkhla", NameTh = "สงขลา · หาดใหญ่ แหลมสมิหลา", Level = LocationLevel.Province, Intercity = true, Airport = true },
        new Location { Id = "nakhonsi", NameTh = "นครศรีธรรมราช · วัดพระมหาธาตุ", Level = LocationLevel.Province, Intercity = true, Airport = true },
    };

    public static string LevelNameTh(LocationLevel level) => level switch
    {
        LocationLevel.Village => "หมู่บ้าน",
        LocationLevel.Subdistrict => "ตำบล",
        LocationLevel.Beach => "ท่องเที่ยว",
        LocationLevel.Province => "ต่างจังหวัด",
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

    public static void TravelTo(Location destination, string spawnId = null)
    {
        if (destination == null || !destination.Available) return;
        PendingSpawnId = spawnId ?? destination.SpawnId;
        SceneManager.LoadScene(destination.Scene);
    }
}
