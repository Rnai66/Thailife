using UnityEngine;

// The bank counter: walk up to it to open the bank window.
[RequireComponent(typeof(Collider))]
public class BankCounter : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ContextButton.Show(this, "ธนาคาร", "ฝาก-ถอน · กู้เงิน · ซื้อที่ดิน/รถ · ธุรกิจ", BankUI.Show);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        BankUI.Hide();
    }
}
