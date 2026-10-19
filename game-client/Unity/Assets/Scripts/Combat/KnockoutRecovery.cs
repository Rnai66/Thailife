using UnityEngine;
using UnityEngine.SceneManagement;

// Knocked out by another player outside the ring: you come round at the health station (รพ.สต.,
// tambon) or the city hospital (โรงพยาบาล) — the hospital if you were in the city or at the beach.
// Travels there if needed, then puts you at the treatment counter with full health.
public class KnockoutRecovery : MonoBehaviour
{
    private static string pending; // "station" | "hospital" while travelling to it

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => pending = null;

    public static string ClinicFor(string scene) => scene == "City" || scene == "Beach" ? "hospital" : "station";
    public static string NameOf(string clinic) => clinic == "hospital" ? "โรงพยาบาล" : "รพ.สต.";

    public static void Go()
    {
        string clinic = ClinicFor(SceneManager.GetActiveScene().name);
        string scene = clinic == "hospital" ? "City" : "Tambon";
        pending = clinic;
        if (SceneManager.GetActiveScene().name == scene)
        {
            new GameObject("KnockoutRecovery").AddComponent<KnockoutRecovery>();
            return;
        }
        Locations.PendingSpawnId = null;
        SceneManager.sceneLoaded += OnLoaded;
        SceneManager.LoadScene(scene);
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLoaded;
        new GameObject("KnockoutRecovery").AddComponent<KnockoutRecovery>();
    }

    // Wait a frame so the player and the scene's spawn points have run their Start first
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        string clinic = pending;
        pending = null;
        var player = FindAnyObjectByType<PlayerController>();
        HealSpot counter = null;
        foreach (var h in FindObjectsByType<HealSpot>())
            if (h.Place == clinic) { counter = h; break; }

        if (player != null && counter != null)
        {
            Vector3 front = counter.transform.position - counter.transform.forward * 1.5f;
            player.Teleport(new Vector3(front.x, counter.transform.position.y, front.z),
                Quaternion.LookRotation(counter.transform.forward, Vector3.up));
        }
        var combat = player != null ? player.GetComponent<PlayerCombat>() : null;
        if (combat != null) combat.RestoreFull();
        UiKit.FloatAbovePlayer($"ฟื้นตัวที่{NameOf(clinic)}", new Color(0.5f, 0.9f, 1f));
        Destroy(gameObject);
    }
}
