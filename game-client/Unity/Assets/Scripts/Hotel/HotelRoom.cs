using UnityEngine;

// A hotel room of one tier (standard / deluxe / suite), built away from the beach town.
// The front desk sends you to the room of the tier you booked; the room's door brings you back.
public class HotelRoom : MonoBehaviour
{
    [SerializeField] private string tier = "standard";
    [SerializeField] private Transform entry;

    // Where to put you back when you leave (the desk you came from)
    public static Vector3? ReturnPosition;
    public static Quaternion ReturnRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ReturnPosition = null;

    public string Tier => tier;

    public void Configure(string roomTier, Transform entryPoint)
    {
        tier = roomTier;
        entry = entryPoint;
    }

    public static bool Enter(string roomTier, Transform from)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return false;
        foreach (var room in FindObjectsByType<HotelRoom>())
        {
            if (room.tier != roomTier || room.entry == null) continue;
            ReturnPosition = from.position;
            ReturnRotation = player.transform.rotation;
            player.Teleport(room.entry.position + Vector3.up, room.entry.rotation);
            return true;
        }
        return false;
    }

    public static void Leave()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        if (ReturnPosition.HasValue) player.Teleport(ReturnPosition.Value, ReturnRotation);
        else
        {
            var desk = FindAnyObjectByType<HotelDesk>();
            if (desk != null) player.Teleport(desk.transform.position, desk.transform.rotation);
        }
        ReturnPosition = null;
    }
}
