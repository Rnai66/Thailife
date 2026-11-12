using UnityEngine;

// ตั้งด่านจับความเร็ว: an officer by the road with a speed gun on a tripod. Drive your own vehicle past
// faster than the road's limit (+10) and you're whistled down and handed a ticket (PoliceClient).
// Placed by the Editor (Police.SpeedTrap); the officer is made at runtime.
public class SpeedTrap : MonoBehaviour
{
    [SerializeField] private float radius = 24f;
    [SerializeField] private int fallbackLimit = 60; // a road with no limit sign of its own

    private const int Over = 10;
    private PoliceOfficer officer;
    private float quietUntil;

    public void Configure(float range, int limit)
    {
        radius = range;
        fallbackLimit = limit;
    }

    private void Start()
    {
        officer = PoliceOfficer.Make(transform, transform.position, transform.eulerAngles.y, transform.position.GetHashCode());
    }

    private void Update()
    {
        var car = DrivableCar.Current;
        if (car == null || officer == null || Time.time < quietUntil) return;
        var d = car.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > radius * radius) return;
        officer.FaceTowards(car.transform.position);
        int kmh = Mathf.RoundToInt(car.Kmh);
        int limit = TrafficNet.LimitAt(car.transform.position);
        if (limit <= 0) limit = fallbackLimit;
        if (kmh <= limit + Over) return;
        quietUntil = Time.time + 90f; // one ticket, then the officer lets this one go for a while
        officer.Whistle(stop: true);
        officer.Wave();
        PoliceClient.Stop("speed", kmh, limit);
    }
}
