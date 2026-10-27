using UnityEngine;

// The fish cages in the Kok Nong Na pond: walk up to the bank to open the fish cage window. Each cage's
// fish (child objects "Fish1".."Fish3") show while that cage has fish in it.
[RequireComponent(typeof(Collider))]
public class FishPond : MonoBehaviour
{
    [SerializeField] private GameObject[] fish = new GameObject[0];

    public void Configure(GameObject[] cageFish) => fish = cageFish;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnEnable()
    {
        PondClient.Changed += Show;
        if (PondClient.Current != null) Show(PondClient.Current);
    }

    private void OnDisable() => PondClient.Changed -= Show;

    private void Show(PondClient.State s)
    {
        for (int i = 0; i < fish.Length; i++)
            if (fish[i] != null)
                fish[i].SetActive(s?.cages != null && i < s.cages.Length && s.cages[i].stage != "empty");
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Show(this, "เลี้ยงปลาในกระชัง", "ปล่อยลูกปลา · ให้อาหาร · จับปลาขาย", FishPondUI.Show);
        if (PondClient.Online)
            try { await PondClient.GetAsync(); } catch (System.Exception) { }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        FishPondUI.Hide();
    }
}
