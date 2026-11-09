using System.Collections.Generic;
using UnityEngine;

// One house a player can live in (VillageBuilder / Homes): its style id (home.lua), the height of its
// room's floor, the porch where you arrive and its sleeping spot. The one you live in (HomeClient)
// becomes your walk-in room (HomeInterior) with your bed; the home shrine, the dog and the carport
// move over to it. The others are the neighbours' houses: you can't sleep in them.
// Every house's room is the same 5.6 x 4.6 m, door on its local -Z side, so furniture fits any of them.
public class HomePlot : MonoBehaviour
{
    [SerializeField] private string style = "stilt";
    [SerializeField] private float floorY = 2.6f;
    [SerializeField] private Transform porch;
    [SerializeField] private GameObject sleep;
    [SerializeField] private Transform carport;
    [SerializeField] private int lot; // 0: a house of the map itself (the village stilt house); N: on house lot N

    private static readonly List<HomePlot> all = new List<HomePlot>();

    // Where the shrine group, its dog and the car sat when the scene was built (the stilt house)
    private static Transform shrine, car;
    private static Vector3 shrineHome, carHome;
    private static Quaternion shrineRot, carRot;
    private static UnityEngine.SceneManagement.Scene rememberedScene; // the map they were found in

    public string Style => style;
    public float FloorY => floorY;
    public int Lot => lot;
    // On a lot: yours when it's your number in the หมู่ on show. The map's own house (the stilt house):
    // yours until you have a lot.
    public bool IsMine => lot > 0
        ? HouseLots.Current != null && HouseLots.Current.ShowingMyMoo && lot == HomeClient.MyLot
        : !HomeClient.HasLot && style == HomeClient.MyStyle;

    public void SetLot(int number) => lot = number;
    public void SetCarport(Transform point) => carport = point;

    public void Configure(string styleId, float floorHeight, Transform porchPoint, GameObject sleepZone, Transform carportPoint = null)
    {
        style = styleId;
        floorY = floorHeight;
        porch = porchPoint;
        sleep = sleepZone;
        carport = carportPoint;
    }

    public static HomePlot Mine()
    {
        foreach (var p in all) if (p != null && p.IsMine) return p;
        return null;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable()
    {
        all.Remove(this);
        FreeWaking(); // a disabled object's coroutines stop: don't leave the player frozen
    }

    private void Start()
    {
        Apply();
        if (IsMine && Locations.PendingSpawnId == HomeClient.SpawnId)
        {
            Locations.PendingSpawnId = null;
            PutPlayerHere();
        }
        else if (IsMine && Locations.PendingSpawnId == HomeClient.WakeId)
        {
            Locations.PendingSpawnId = null;
            WakeUp();
        }
    }

    public static void ApplyAll()
    {
        foreach (var p in all) if (p != null) p.Apply();
        HomeInterior.Setup();
    }

    private void Apply()
    {
        bool mine = IsMine;
        if (sleep != null)
        {
            sleep.SetActive(mine);
            sleep.name = mine ? "SleepZone" : "SleepZone_" + style; // the tutorial's arrow finds your own mat
        }
        if (mine) MoveBelongings();
    }

    // The home shrine (amulets, kuman, the dog and its kennel) and the car follow you to this house
    private void MoveBelongings()
    {
        if (rememberedScene != gameObject.scene)
        {
            rememberedScene = gameObject.scene;
            shrine = car = null;
            var s = GameObject.Find("HomeShrine_หิ้งพระ");
            if (s != null) { shrine = s.transform; shrineHome = shrine.position; shrineRot = shrine.rotation; }
            var c = GameObject.Find("Carport_ที่จอดรถใต้ถุน");
            if (c != null) { car = c.transform; carHome = car.position; carRot = car.rotation; }
        }
        bool stilt = style == "stilt";
        bool original = stilt && lot == 0; // the village's own stilt house, where they were built
        if (shrine != null)
        {
            // built for the stilt house (floor 2.6 m up): same place over this house's front door
            if (original) shrine.SetPositionAndRotation(shrineHome, shrineRot);
            else shrine.SetPositionAndRotation(transform.TransformPoint(new Vector3(0f, floorY - 2.6f, 0f)), transform.rotation);
            var dog = shrine.Find("Guardians/dog");
            if (dog != null) dog.localPosition = stilt ? Vector3.zero : new Vector3(-5.6f, 2.6f - floorY, 0.5f); // kennel in the side yard
        }
        if (car != null)
        {
            if (original || carport == null) car.SetPositionAndRotation(carHome, carRot);
            else car.SetPositionAndRotation(carport.position, carport.rotation);
        }
    }

    // The start of each session: you wake up in bed at home, lie a moment, then get up the way people
    // do — roll up to sit on the edge of the bed with your feet on the floor, a breath, then stand up
    // and turn towards the door (so the way out is right in front of you)
    public void WakeUp() => StartCoroutine(WakeRoutine());

    private PlayerController waking; // set while the wake-up plays; freed if it is cut short

    private void OnDestroy() => FreeWaking();

    private void FreeWaking()
    {
        if (waking == null) return;
        foreach (var pose in waking.GetComponents<SeatedPose>()) Destroy(pose);
        waking.enabled = true;
        waking.InputEnabled = true;
        waking = null;
    }

    private System.Collections.IEnumerator WakeRoutine()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) yield break;
        player.InputEnabled = false;
        waking = player;
        Vector3 stand = porch != null ? porch.position : transform.position;
        Vector3 hipsOffset = SeatedPose.HipsOffset(player.transform); // measured standing, before lying down
        Vector3 lieAt = Vector3.zero;
        bool inRoom = false;
        float yaw = 0f, roll = 90f;
        bool Lie()
        {
            Vector3 at; Quaternion rot;
            var room = HomeInterior.Instance;
            if (room != null && room.gameObject == gameObject && room.WakePoint(out at, out rot, out var up))
            {
                stand = up;
                roll = -90f; // head on the pillow (the bed's head end is its -z side)
                inRoom = true;
            }
            else
            {
                var spot = sleep != null ? sleep.GetComponent<SleepSpot>() : null;
                if (spot == null || spot.Bed == null) return false;
                at = spot.Bed.position;
                rot = spot.Bed.rotation;
            }
            yaw = rot.eulerAngles.y;
            lieAt = at;
            player.enabled = true;
            player.Teleport(at, Quaternion.Euler(0f, yaw, 0f));
            player.enabled = false; // no gravity while lying on the bed
            player.transform.rotation = Quaternion.Euler(0f, yaw, roll);
            return true;
        }
        bool lying = Lie();
        while (SceneLoader.Loading) yield return null;
        yield return null;
        lying = Lie() || lying; // the room (and your bed) may only be set up now
        if (!lying) { waking = null; player.InputEnabled = true; PutPlayerHere(); yield break; }
        yield return new WaitForSeconds(1.4f);
        FloatingText.Spawn(player.transform.position + Vector3.up * 1.2f, "อรุณสวัสดิ์! ตื่นแล้ว", new Color(1f, 0.9f, 0.5f));
        var room = HomeInterior.Instance;
        Vector3 door = room != null && room.gameObject == gameObject ? room.DoorPoint : stand + Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
        Quaternion Facing(Vector3 from, Vector3 to)
        {
            var d = to - from;
            d.y = 0f;
            return d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d.normalized, Vector3.up) : Quaternion.Euler(0f, yaw + 90f, 0f);
        }
        if (!inRoom)
        {
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) // sit up
            {
                player.transform.rotation = Quaternion.Euler(0f, yaw, Mathf.Lerp(roll, 0f, Mathf.SmoothStep(0f, 1f, t / 0.8f)));
                yield return null;
            }
            player.enabled = true;
            player.Teleport(stand + Vector3.up * 0.1f, Facing(stand, door));
            player.InputEnabled = true;
            waking = null;
            yield break;
        }

        // 1. roll up to sit on the edge of the bed, facing out, legs over the side
        Vector3 outward = stand - lieAt;
        outward.y = 0f;
        var sitRot = Facing(lieAt, stand);
        Vector3 edge = lieAt + outward * 0.42f;                        // the bed's edge, on the way out
        Vector3 sitRoot = SeatedPose.RootFor(new Vector3(edge.x, lieAt.y + 0.04f, edge.z), sitRot, hipsOffset);
        Vector3 lieRoot = player.transform.position;
        Quaternion lieRot = player.transform.rotation;
        var pose = player.gameObject.AddComponent<SeatedPose>();
        pose.CrossLegged = lieAt.y - stand.y < 0.3f; // a sleeping mat on the floor: sit up cross-legged on it
        const float sitUp = 1.1f;
        for (float t = 0f; t < sitUp; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / sitUp);
            player.transform.SetPositionAndRotation(Vector3.Lerp(lieRoot, sitRoot, k), Quaternion.Slerp(lieRot, sitRot, k));
            yield return null;
        }
        player.transform.SetPositionAndRotation(sitRoot, sitRot);
        yield return new WaitForSeconds(0.7f);                          // a breath, sitting

        // 2. stand up: lean forward, rise onto the feet just in front of the bed
        Destroy(pose);
        yield return null;
        Vector3 standRoot = stand + Vector3.up * 0.02f;
        const float rise = 0.6f;
        for (float t = 0f; t < rise; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / rise);
            var p = Vector3.Lerp(sitRoot, standRoot, k);
            p.y = Mathf.Lerp(sitRoot.y, standRoot.y, Mathf.Sqrt(k));
            player.transform.SetPositionAndRotation(p, sitRot * Quaternion.Euler(Mathf.Sin(k * Mathf.PI) * 18f, 0f, 0f));
            yield return null;
        }

        // 3. turn to the door
        var doorRot = Facing(stand, door);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            player.transform.rotation = Quaternion.Slerp(sitRot, doorRot, Mathf.SmoothStep(0f, 1f, t / 0.5f));
            yield return null;
        }
        player.enabled = true;
        player.Teleport(standRoot + Vector3.up * 0.08f, doorRot);
        player.InputEnabled = true;
        waking = null;
        ScreenToast.Show("ทางออกอยู่ตามลูกศรสีเขียวที่ประตู", new Color(0.3f, 0.85f, 0.45f));
    }

    public void PutPlayerHere()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        var at = porch != null ? porch : transform;
        player.Teleport(at.position + Vector3.up, Quaternion.Euler(0f, at.eulerAngles.y, 0f));
    }
}
