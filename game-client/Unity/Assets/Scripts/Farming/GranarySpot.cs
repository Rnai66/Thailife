using UnityEngine;

// Rice barn (ยุ้งข้าว) by the house: standing at it shows what's stored and how much room is left.
[RequireComponent(typeof(Collider))]
public class GranarySpot : MonoBehaviour
{
    private bool playerInside;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Update()
    {
        if (!playerInside) return;

        var farm = FarmClient.Instance;
        if (farm == null || !farm.IsOnline)
        {
            ContextButton.Show(this, "ยุ้งข้าว", "ต้องออนไลน์ก่อนถึงจะดูยุ้งได้", null, enabled: false);
            return;
        }

        var state = farm.Current;
        var parts = new System.Collections.Generic.List<string>();
        if (state.varieties != null)
            foreach (var v in state.varieties)
                parts.Add($"{v.name} {v.stored_kg:0} กก.");

        bool full = state.barn_total >= state.barn_capacity;
        string status = $"ยุ้งข้าว {state.barn_total:0}/{state.barn_capacity:0} กก. · {string.Join(" · ", parts)}"
                        + (full ? " · เต็มแล้ว! ขายที่ร้านค้าชุมชน หรือขยายยุ้ง" : "");
        ContextButton.Show(this, "ยุ้งข้าว", status, null, enabled: false, highlight: full);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        playerInside = false;
        ContextButton.Hide(this);
    }

    private void OnDestroy() => ContextButton.Hide(this);
}
