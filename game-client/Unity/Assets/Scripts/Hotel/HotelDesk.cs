using UnityEngine;

// Hotel front desk (เคาน์เตอร์ต้อนรับ) at the entrance: book a room or go up to yours.
[RequireComponent(typeof(Collider))]
public class HotelDesk : MonoBehaviour
{
    [SerializeField] private string hotelName = "โรงแรม";

    public string HotelName => hotelName;

    public void Configure(string name) => hotelName = name;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ContextButton.Show(this, hotelName, "จองห้องพัก · เข้าห้องพักของคุณ", () => HotelUI.Show(this));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        HotelUI.Hide();
    }
}
