using UnityEngine;

// Trigger around a signpost: walking up to it pops the detail card (SignCardUI); leaving hides it.
[RequireComponent(typeof(Collider))]
public class SignPost : MonoBehaviour
{
    [SerializeField] private string title;
    [SerializeField, TextArea] private string details;

    public void Configure(string heading, string text)
    {
        title = heading;
        details = text;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private PlayerController reader; // the player it's showing for

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        reader = player;
        SignCardUI.Show(this, title, details);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        reader = null;
        SignCardUI.Hide(this);
    }

    // No exit comes when the player leaves without walking out (gets into a taxi, is teleported,
    // the controller is switched off): check, and put the card away
    private void Update()
    {
        if (reader == null) return;
        var area = GetComponent<Collider>().bounds;
        area.Expand(1f);
        var cc = reader.GetComponent<CharacterController>();
        if (!reader.isActiveAndEnabled || (cc != null && !cc.enabled) || !area.Contains(reader.transform.position + Vector3.up * 0.5f))
        {
            reader = null;
            SignCardUI.Hide(this);
        }
    }

    private void OnDisable()
    {
        reader = null;
        SignCardUI.Hide(this);
    }
}
