using UnityEngine;

// The parking spot under the stilt house (ใต้ถุนบ้าน): shows your best vehicle bought at the bank
// (bank.lua, kind "car") parked here, ready to drive (DrivableCar). Updates when you buy a better one.
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
        if (id == shownId) return;
        if (car != null && car.Driving) return; // swap it once you get out
        shownId = id;
        if (car != null) Destroy(car.gameObject);
        car = null;
        if (id == null) return;

        var root = new GameObject("MyCar_" + id);
        root.transform.SetPositionAndRotation(transform.position, transform.rotation);
        var player = FindAnyObjectByType<PlayerController>();
        var template = player != null ? player.GetComponent<Renderer>()?.sharedMaterial : null;
        CarModels.Build(id, root.transform, template);
        car = root.AddComponent<DrivableCar>();
        car.Configure(id, best.name);
    }
}
