using UnityEngine;

// The counter inside a gold shop (ร้านทอง) or pawn shop (โรงรับจำนำ): step up to it to trade gold
// (GoldUI) or pawn and redeem (PawnUI).
[RequireComponent(typeof(Collider))]
public class GoldCounter : MonoBehaviour
{
    [SerializeField] private string shopName = "ร้านทอง";
    [SerializeField] private bool pawn;

    public void Configure(string name, bool pawnShop)
    {
        shopName = name;
        pawn = pawnShop;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (pawn) ContextButton.Show(this, shopName, "จำนำของตามราคาตลาด · ไถ่ถอนตั๋วจำนำ", () => PawnUI.Show(shopName));
        else ContextButton.Show(this, shopName, "ซื้อขายทองคำ · ราคาเปลี่ยนทุกชั่วโมง", () => GoldUI.Show(shopName));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        if (pawn) PawnUI.Hide(); else GoldUI.Hide();
    }
}
