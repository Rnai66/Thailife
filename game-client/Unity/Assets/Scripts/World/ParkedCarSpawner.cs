using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Puts your own vehicle back where you left it on this map (ParkedCar), or — arriving by car
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
        bool arriving = !string.IsNullOrEmpty(ParkedCar.Arriving);
        ParkedCar.Arriving = null;
        if (!ParkedCar.Away || ParkedCar.Scene != scene) yield break;

        Vector3 at;
        Quaternion rot;
        if (ParkedCar.AtSpawn || arriving)
        {
            var spawn = DriveSpot();
            var player = FindAnyObjectByType<PlayerController>();
            at = spawn != null ? spawn.position : player != null ? player.transform.position + player.transform.right * 2f : Vector3.zero;
            rot = spawn != null ? spawn.rotation : player != null ? player.transform.rotation : Quaternion.identity;
            at.y = 0f;
        }
        else
        {
            at = ParkedCar.Position;
            rot = Quaternion.Euler(0f, ParkedCar.Yaw, 0f);
        }
        var car = HomeCar.Spawn(ParkedCar.Id, ParkedCar.Name, at, rot);
        if (car == null) yield break;
        if (arriving)
        {
            yield return null;
            car.GetIn();
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
