using UnityEngine;

// The fish cages in the Kok Nong Na pond: walk up to the bank to open the fish cage window. Each cage's
// fish (one "Fish" group per cage, animated by CageFish) show while that cage has fish in it.
[RequireComponent(typeof(Collider))]
public class FishPond : MonoBehaviour
{
    [SerializeField] private GameObject[] fish = new GameObject[0];

    private bool theirs; // a neighbour's pond (FarmLot.ShowFarm): their fish, no window of yours

    public void Configure(GameObject[] cageFish) => fish = cageFish;

    public void ShowAs(PondClient.Cage[] cages)
    {
        theirs = true;
        PondClient.Changed -= Show;
        Show(new PondClient.State { cages = cages ?? new PondClient.Cage[0] });
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnEnable()
    {
        if (theirs) return;
        PondClient.Changed += Show;
        if (PondClient.Current != null) Show(PondClient.Current);
    }

    private void OnDisable() => PondClient.Changed -= Show;

    private void Show(PondClient.State s)
    {
        for (int i = 0; i < fish.Length; i++)
            if (fish[i] != null)
            {
                var cage = s?.cages != null && i < s.cages.Length ? s.cages[i] : null;
                fish[i].SetActive(cage != null && cage.stage != "empty");
                var swim = fish[i].GetComponent<CageFish>();
                if (swim != null) swim.Show(cage);
            }
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null || theirs) return;
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
