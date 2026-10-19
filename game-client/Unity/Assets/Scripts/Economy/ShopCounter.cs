using UnityEngine;

// Shop counter: walking up to it opens the shop window.
// "community" = ร้านค้าชุมชน (sell fish, buy rods), "camp" = ค่ายมวย front desk (membership).
[RequireComponent(typeof(Collider))]
public class ShopCounter : MonoBehaviour
{
    [SerializeField] private string shopId = "community";

    public void Configure(string id) => shopId = id;
    public string ShopId => shopId;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ShopUI.Show(shopId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            ShopUI.Hide();
    }
}
