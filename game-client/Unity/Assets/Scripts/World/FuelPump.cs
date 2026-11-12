using UnityEngine;

// A petrol pump (ตู้จ่ายน้ำมัน) at a petrol station: walk or drive up to it for the station's window
// (FuelUI) — fill up your vehicles, or drive off from here to another place.
[RequireComponent(typeof(BoxCollider))]
public class FuelPump : MonoBehaviour
{
    [SerializeField] private string station = "ปั๊มน้ำมัน";

    public void Configure(string stationName) => station = stationName;

    private void Awake() => GetComponent<BoxCollider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        bool driving = DrivableCar.Current != null;
        ContextButton.Show(this, "เติมน้ำมัน", driving ? $"{station} · เติมน้ำมัน หรือขับไปที่อื่น" : $"{station} · เติมน้ำมันรถของคุณ", () => FuelUI.Show(station));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }
}
