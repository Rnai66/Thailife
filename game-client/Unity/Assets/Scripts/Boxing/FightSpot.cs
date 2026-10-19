using UnityEngine;

// The steps up to a boxing ring: offers the ring's events (ซ้อม / ชก) through the context
// button. `Ring` is the middle of the canvas, where fighters stand.
[RequireComponent(typeof(Collider))]
public class FightSpot : MonoBehaviour
{
    [SerializeField] private string title = "เวทีมวย";
    [SerializeField] private string[] events = { "pads" };
    [SerializeField] private Transform ring;

    public string Title => title;
    public string[] Events => events;
    public Transform Ring => ring;

    public void Configure(string ringTitle, Transform ringCentre, params string[] eventIds)
    {
        title = ringTitle;
        ring = ringCentre;
        events = eventIds;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null || FightDirector.Busy) return;
        ContextButton.Show(this, "ขึ้นเวที", title, () => BoxingUI.Show(this));
        BoxingClient.Instance?.RefreshAsync();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) ContextButton.Hide(this);
    }
}
