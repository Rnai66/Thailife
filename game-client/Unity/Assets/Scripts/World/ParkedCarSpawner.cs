using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Puts your own vehicles back where you left them on this map (ParkedCar), or — arriving by car
// (DriveJourney) — at the map's drive-in spot (SpawnPoint "drive", the petrol station) with you at
// the wheel. One per map, made after it loads.
public class ParkedCarSpawner : MonoBehaviour
{
    public const string DriveSpawn = "drive";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Make();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Make();

    private static void Make()
    {
        if (FindAnyObjectByType<PlayerController>() == null) return; // the title screen
        new GameObject("ParkedCarSpawner").AddComponent<ParkedCarSpawner>();
    }

    private IEnumerator Start()
    {
        yield return null; // after SpawnPoint has put the player down
        string scene = SceneManager.GetActiveScene().name;
        string arriving = ParkedCar.Arriving;
        ParkedCar.Arriving = null;
        DrivableCar arrived = null;
        int atSpawn = 0;
        foreach (var id in ParkedCar.AwayIds())
        {
            if (ParkedCar.SceneOf(id) != scene) continue;
            Vector3 at;
            Quaternion rot;
            if (ParkedCar.AtSpawn(id) || id == arriving)
            {
                var spawn = DriveSpot();
                var player = FindAnyObjectByType<PlayerController>();
                at = spawn != null ? spawn.position : player != null ? player.transform.position + player.transform.right * 2f : Vector3.zero;
                rot = spawn != null ? spawn.rotation : player != null ? player.transform.rotation : Quaternion.identity;
                // more than one left at the drive-in spot: side by side
                if (id != arriving) at += rot * Vector3.right * (2.8f * ++atSpawn);
                at.y = 0f;
            }
            else
            {
                at = ParkedCar.PositionOf(id);
                rot = Quaternion.Euler(0f, ParkedCar.YawOf(id), 0f);
            }
            var car = HomeCar.Spawn(id, ParkedCar.NameOf(id), at, rot);
            if (id == arriving) arrived = car;
        }
        if (arrived != null)
        {
            yield return null;
            arrived.GetIn();
            UiKit.FloatAbovePlayer("ถึงแล้ว! จอดที่ปั๊มน้ำมัน", UiKit.OkGreen);
        }
        Destroy(gameObject);
    }

    private static Transform DriveSpot()
    {
        foreach (var s in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            if (s.Id == DriveSpawn) return s.transform;
        return null;
    }
}
