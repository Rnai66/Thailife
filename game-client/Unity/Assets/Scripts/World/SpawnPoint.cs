using UnityEngine;

// Where the player appears after travelling here (Locations.PendingSpawnId must match id).
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id = "busstop";

    public void Configure(string spawnId) => id = spawnId;

    private void Start()
    {
        if (Locations.PendingSpawnId != id) return;
        Locations.PendingSpawnId = null;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            player.Teleport(transform.position + Vector3.up, transform.rotation);
    }
}
