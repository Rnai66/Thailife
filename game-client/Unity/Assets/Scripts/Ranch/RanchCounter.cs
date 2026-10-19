using UnityEngine;

// Where you get cows: "sao" = the อบต. desk (one free cow), "seller" = the cow seller at the farm gate.
[RequireComponent(typeof(Collider))]
public class RanchCounter : MonoBehaviour
{
    [SerializeField] private string mode = "seller";

    public void Configure(string counterMode) => mode = counterMode;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (mode == "sao")
            ContextButton.Show(this, "รับวัวจาก อบต.", "โครงการวัวให้ชาวบ้าน · รับฟรีคนละ 1 ตัว", () => RanchUI.Show(mode));
        else
            ContextButton.Show(this, "ซื้อวัว", "พ่อค้าวัว · ซื้อวัวเพิ่มเข้าคอก", () => RanchUI.Show(mode));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        RanchUI.Hide();
    }
}
