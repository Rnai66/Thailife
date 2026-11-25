using UnityEngine;

// ตู้ ATM (built by StreetFurniture.Atm, and the one outside each petrol station's shop): walk up to it
// to deposit or withdraw — the bank's window (BankUI), any time of day, anywhere in the country.
[RequireComponent(typeof(Collider))]
public class AtmMachine : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ContextButton.Show(this, "ตู้ ATM", "ฝาก-ถอนเงิน · ดูยอดเงิน · โอนเข้าบัญชีออมทรัพย์ 24 ชม.", BankUI.Show);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        BankUI.Hide();
    }

    private void OnDisable() => ContextButton.Hide(this);
}
