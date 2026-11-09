using UnityEngine;

// The spot by your farm lot's sign (FarmLot): stand here to arrange the zones and build fences (FarmLotUI)
[RequireComponent(typeof(Collider))]
public class FarmLotCounter : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Show(this, "จัดแปลงเกษตร", "สลับโซน นา · บ่อปลา · คอกวัว · เล้าไก่ · สร้างรั้ว", FarmLotUI.Show);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        FarmLotUI.Hide();
    }
}
