using System.Collections.Generic;
using UnityEngine;

// The parking at home: every vehicle you bought at the bank (bank.lua, kind "car") that isn't parked
// somewhere else (ParkedCar) stands here, ready to drive (DrivableCar). Garages you built round the
// house (โรงรถ / โรงจอดรถ, BuiltPieces) are filled first, the dearest vehicle first; the rest line up at
// the carport under the house, the car in the middle and the bicycle and motorbike beside it.
// Updates when you buy another vehicle, build or move a garage, or drive one home.
public class HomeCar : MonoBehaviour
{
    private BankClient bank;
    private readonly Dictionary<string, DrivableCar> cars = new Dictionary<string, DrivableCar>();
    private string shownKey;
    private float nextCheck;

    private void Start()
    {
        bank = BankClient.Instance;
        if (bank == null) return;
        bank.Changed += OnBank;
        if (bank.Current != null) Sync(bank.Current);
    }

    private void OnDestroy()
    {
        if (bank != null) bank.Changed -= OnBank;
    }

    private void OnBank(BankClient.State s) => Sync(s);

    // garages built or moved, a vehicle driven home: look again now and then
    private void Update()
    {
        if (Time.unscaledTime < nextCheck || bank == null || bank.Current == null) return;
        nextCheck = Time.unscaledTime + 1f;
        Sync(bank.Current);
    }

    private static bool IsGarage(string kind) => kind == "carport" || kind == "garage";

    private void Sync(BankClient.State s)
    {
        if (DrivableCar.Current != null && cars.ContainsValue(DrivableCar.Current)) return; // swap once you get out
        var owned = new List<BankClient.Asset>();
        if (s?.assets != null)
            foreach (var a in s.assets)
                if (a.owned && a.kind == "car" && !ParkedCar.IsAway(a.id)) owned.Add(a);
        owned.Sort((a, b) => b.price.CompareTo(a.price));

        var garages = Garages();
        var key = new System.Text.StringBuilder(transform.position.ToString("F1")).Append('|'); // the carport follows you to a new house (HomePlot)
        foreach (var a in owned) key.Append(a.id).Append(',');
        foreach (var gt in garages) key.Append(gt.position.ToString("F1")).Append(gt.eulerAngles.y.ToString("F0")).Append(';');
        if (key.ToString() == shownKey && !HasMissing()) return;
        shownKey = key.ToString();

        // one parked somewhere else now (you got out away from home) stays where it is
        foreach (var kv in cars) if (kv.Value != null && !ParkedCar.IsAway(kv.Key)) Destroy(kv.Value.gameObject);
        cars.Clear();
        // one just driven home stands where you left it until it is lined up here
        var homeIds = new HashSet<string>();
        foreach (var a in owned) homeIds.Add(a.id);
        foreach (var c in FindObjectsByType<DrivableCar>(FindObjectsSortMode.None))
            if (c != DrivableCar.Current && homeIds.Contains(c.CarId)) Destroy(c.gameObject);

        // a garage takes one car, or two bikes side by side
        int g = 0, inGarage = 0;
        var left = new List<BankClient.Asset>();
        foreach (var a in owned)
        {
            bool two = CarModels.TwoWheels(a.id);
            if (g < garages.Count)
            {
                var t = garages[g];
                Vector3 at = t.position;
                if (two) at += t.right * (inGarage == 0 ? -0.75f : 0.75f);
                cars[a.id] = Spawn(a.id, a.name, at, t.rotation * Quaternion.Euler(0f, 180f, 0f)); // nose out of the open front (-Z)
                inGarage++;
                if (!two || inGarage >= 2) { g++; inGarage = 0; }
                continue;
            }
            left.Add(a);
        }
        // under the house: the first in the carport itself, the others beside it
        float side = 0f;
        for (int i = 0; i < left.Count; i++)
        {
            var a = left[i];
            bool two = CarModels.TwoWheels(a.id);
            float offset = 0f;
            if (i > 0)
            {
                side += (CarModels.TwoWheels(left[i - 1].id) ? 0.55f : 1.1f) + (two ? 0.55f : 1.3f);
                offset = side;
            }
            cars[a.id] = Spawn(a.id, a.name, transform.position + transform.right * offset, transform.rotation);
        }
    }

    private bool HasMissing()
    {
        foreach (var c in cars.Values) if (c == null) return true;
        return false;
    }

    // The garages built round your home, if this carport is your home's
    private List<Transform> Garages()
    {
        var list = new List<Transform>();
        var client = BuildClient.Instance;
        var home = BuiltPieces.AnchorOf("home");
        if (client?.Current?.pieces == null || home == null) return list;
        if ((home.position - transform.position).sqrMagnitude > 20f * 20f) return list; // another house's carport
        foreach (var p in client.Current.pieces)
        {
            if (BuildClient.SiteOf(p) != "home" || !IsGarage(p.kind)) continue;
            var go = BuiltPieces.Shown(p.id);
            if (go != null && go.activeInHierarchy) list.Add(go.transform);
        }
        return list;
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

    // Close enough to the carport, or to a garage you built, to count as parked at home
    public static bool AtHome(Vector3 world)
    {
        foreach (var h in FindObjectsByType<HomeCar>(FindObjectsSortMode.None))
        {
            if ((h.transform.position - world).sqrMagnitude < 8f * 8f) return true;
            foreach (var g in h.Garages()) if ((g.position - world).sqrMagnitude < 5f * 5f) return true;
        }
        return false;
    }
}
