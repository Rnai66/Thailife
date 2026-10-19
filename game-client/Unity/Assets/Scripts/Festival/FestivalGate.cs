using UnityEngine;

// On an event ground (ลานจัดงาน, tambon / city — งานวัด itself is at the village temple): shows the attractions (child "Attractions") only while a festival of this
// venue's size is on, and keeps the arch banner showing its name (or the next one).
public class FestivalGate : MonoBehaviour
{
    [SerializeField] private string scale = "small"; // "small" = tambon, "big" = city, "any" = the village temple fair
    [SerializeField] private TextMesh banner;

    private GameObject attractions;
    private float nextCheck;

    public string Scale => scale;

    public void Configure(string venueScale, TextMesh bannerText)
    {
        scale = venueScale;
        banner = bannerText;
    }

    private void Awake()
    {
        var t = transform.Find("Attractions");
        if (t != null) attractions = t.gameObject;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 2f;
        var f = FestivalClient.Instance;
        bool on = f != null && f.OpenFor(scale);
        if (attractions != null && attractions.activeSelf != on) attractions.SetActive(on);
        if (banner != null)
            banner.text = on ? f.Current.name
                : f?.Current != null && !string.IsNullOrEmpty(f.Current.name) ? $"งานถัดไป\n{f.Current.name}" : "ลานจัดงาน";
    }
}
