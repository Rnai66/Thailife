using System.Collections.Generic;
using UnityEngine;

// The people in a ตุ๊กตุ๊ก: the driver on his saddle, hand on the handlebar, and (some of them) one or
// two passengers on the back bench. Uses the character looks from Setup Character, seated with
// SeatedPose; without them the tuk-tuk simply runs empty. TukTukHail clears the bench while the
// player rides.
public class TukTukRider : MonoBehaviour
{
    [SerializeField] private int look;
    [SerializeField] private int passengers;

    // Seat tops in the tuk-tuk's space (CityTraffic.TukTuk), facing +Z
    public static readonly Vector3 DriverSeat = new Vector3(0f, 0.83f, 0.35f);
    public static readonly Vector3 BenchSeat = new Vector3(0f, 0.82f, -0.72f);

    private readonly List<GameObject> bench = new List<GameObject>();

    public void Configure(int lookIndex, int passengerCount)
    {
        look = lookIndex;
        passengers = passengerCount;
    }

    private void Start()
    {
        if (CharacterModel.SkinCount == 0) return;
        Sit("Driver", DriverSeat, look, steer: true);
        for (int i = 0; i < passengers; i++)
        {
            float x = passengers == 1 ? 0.15f : (i == 0 ? -0.3f : 0.3f);
            bench.Add(Sit("Passenger", BenchSeat + new Vector3(x, 0f, 0f), look + 3 + i * 5, steer: false));
        }
    }

    public void ShowPassengers(bool on)
    {
        foreach (var p in bench) if (p != null) p.SetActive(on);
    }

    private GameObject Sit(string name, Vector3 seatTop, int skin, bool steer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = seatTop + Vector3.up;
        go.transform.localRotation = Quaternion.identity;
        var model = go.AddComponent<CharacterModel>();
        model.SetSkin(skin % Mathf.Max(1, CharacterModel.SkinCount));
        var hips = SeatedPose.HipsOffset(go.transform);
        go.transform.position = SeatedPose.RootFor(transform.TransformPoint(seatTop + Vector3.up * 0.05f), transform.rotation, hips);
        var pose = go.AddComponent<SeatedPose>();
        pose.HoldTiller = steer;
        var animator = go.GetComponentInChildren<Animator>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.CullCompletely;
        return go;
    }
}
