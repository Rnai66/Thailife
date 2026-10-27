using UnityEngine;

// A walk-up election spot: the candidate desk at the อบต. office ("sao": register, salary) or the
// ballot box at the village hall ("hall": campaign, vote). Opens the election window.
[RequireComponent(typeof(Collider))]
public class ElectionSpot : MonoBehaviour
{
    [SerializeField] private string mode = "sao";

    public void Configure(string spotMode) => mode = spotMode;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private async void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        if (mode == "hall")
            ContextButton.Show(this, "เลือกตั้ง · หาเสียง", "หาเสียงให้ตัวเอง · ลงคะแนนเลือก ส.อบต. และนายก อบต.", () => ElectionUI.Show("hall"));
        else
            ContextButton.Show(this, "สมัครรับเลือกตั้ง", "ลงสมัคร ส.อบต. · นายก อบต. · รับเงินเดือน", () => ElectionUI.Show("sao"));
        if (ElectionClient.Online)
            try { await ElectionClient.GetAsync(); } catch (System.Exception) { }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        ElectionUI.Hide();
    }
}
