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

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SignCardUI.Show(this, title, details);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) SignCardUI.Hide(this);
    }

    private void OnDisable() => SignCardUI.Hide(this);
}
