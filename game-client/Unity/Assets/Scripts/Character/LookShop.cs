using UnityEngine;

// Barber (ร้านตัดผม) or clothes shop (ร้านเสื้อผ้า) counter: walk up to open the look window in that mode.
[RequireComponent(typeof(Collider))]
public class LookShop : MonoBehaviour
{
    [SerializeField] private bool barber = true;

    public void Configure(bool isBarber) => barber = isBarber;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Show(this, barber ? "ตัดผม" : "ซื้อชุด",
            barber ? "ร้านตัดผม · เปลี่ยนทรงผมและสีผม" : "ร้านเสื้อผ้า · เปลี่ยนสีเสื้อและกางเกง",
            () => LookUI.Show(barber ? LookUI.Mode.Barber : LookUI.Mode.Tailor));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }
}
