using UnityEngine;

// Counter of the community shop (ร้านค้าชุมชน): walking up to it opens the shop window.
[RequireComponent(typeof(Collider))]
public class ShopCounter : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ShopUI.Show();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ShopUI.Hide();
    }
}
