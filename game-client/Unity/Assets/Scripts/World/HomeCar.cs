using UnityEngine;

// The parking spot under the stilt house (ใต้ถุนบ้าน): shows your best vehicle bought at the bank
// (bank.lua, kind "car") parked here, ready to drive (DrivableCar). Updates when you buy a better one.
// While that vehicle is parked somewhere else (ParkedCar: another map, another spot) the carport is
// empty; ParkedCarSpawner puts it where it is.
public class HomeCar : MonoBehaviour
{
    private BankClient bank;
    private DrivableCar car;
    private string shownId;

    private void Start()
    {
        bank = BankClient.Instance;
        if (bank == null) return;
        bank.Changed += Sync;
        if (bank.Current != null) Sync(bank.Current);
    }

    private void OnDestroy()
    {
        if (bank != null) bank.Changed -= Sync;
    }

    private void Sync(BankClient.State s)
    {
        BankClient.Asset best = null;
        if (s?.assets != null)
            foreach (var a in s.assets)
                if (a.owned && a.kind == "car" && (best == null || a.price > best.price)) best = a;
        string id = best?.id;
        if (id != null && ParkedCar.Away && ParkedCar.Id == id) id = null; // it's out: not here
        if (id == shownId) return;
        if (car != null && car.Driving) return; // swap it once you get out
        shownId = id;
        if (car != null) Destroy(car.gameObject);
        car = null;
        if (id == null) return;

        car = Spawn(id, best.name, transform.position, transform.rotation);
    }

    // Your vehicle `id` standing at a spot, ready to get in (also ParkedCarSpawner's)
    public static DrivableCar Spawn(string id, string displayName, Vector3 at, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var root = new GameObject("MyCar_" + id);
        root.transform.SetPositionAndRotation(at, rotation);
        var player = FindAnyObjectByType<PlayerController>();
        var template = player != null ? player.GetComponent<Renderer>()?.sharedMaterial : null;
        CarModels.Build(id, root.transform, template);
        root.AddComponent<CarSway>().Configure(CarModels.TwoWheels(id), spin: true); // rocks on its springs, wheels turn
        root.AddComponent<CarHeadlights>().Configure(CarModels.TwoWheels(id));        // lights on after dark
        var drivable = root.AddComponent<DrivableCar>();
        drivable.Configure(id, displayName);
        return drivable;
    }

    // Close enough to the carport to count as parked at home
    public static bool AtHome(Vector3 world)
    {
        foreach (var h in FindObjectsByType<HomeCar>(FindObjectsSortMode.None))
            if ((h.transform.position - world).sqrMagnitude < 8f * 8f) return true;
        return false;
    }
}
