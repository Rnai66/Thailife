using UnityEngine;

// What the temple gave your home, shown at the stilt house: the temple dog by its kennel, the
// กุมารทอง on the shrine shelf (หิ้งพระ), and each amulet and yantra cloth you offered for. Children
// are named after the item ids ("dog", "kuman", "somdej", "yant_5", …) and shown only when owned.
public class HomeGuardians : MonoBehaviour
{
    private float nextCheck;

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 2f;
        var t = TempleClient.Instance;
        if (t == null || t.Current == null)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            return;
        }
        foreach (Transform child in transform)
        {
            bool on = t.Owns(child.name);
            if (child.gameObject.activeSelf != on) child.gameObject.SetActive(on);
        }
    }
}
