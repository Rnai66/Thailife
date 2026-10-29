using UnityEngine;

// Fills an area with strolling villagers, optionally only at certain times:
// during a festival (งานวัด / งานอีเวนต์), or between opening hours (ตลาด) on the game clock.
public class VillagerSpawner : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(10f, 10f); // walkable box (local X/Z)
    [SerializeField] private int count = 6;
    [SerializeField] private string festivalScale = "";            // "", "any", "small", "big"
    [SerializeField] private int openHour = 0, closeHour = 24;      // game-clock hours
    [SerializeField] private string phrases = "";                   // lines separated by |
    [SerializeField] private int tourists;                           // how many of them are foreign tourists
    [SerializeField] private string touristPhrases = "";

    private GameObject crowd;
    private float nextCheck;

    public void Configure(Vector2 area, int people, string festival, int open, int close, string lines,
        int foreigners = 0, string foreignLines = "")
    {
        tourists = foreigners;
        touristPhrases = foreignLines;
        size = area;
        count = people;
        festivalScale = festival;
        openHour = open;
        closeHour = close;
        phrases = lines;
    }

    public Vector3 RandomPoint() =>
        transform.TransformPoint(new Vector3(Random.Range(-0.5f, 0.5f) * size.x, 1f, Random.Range(-0.5f, 0.5f) * size.y));

    private bool ShouldBeOut()
    {
        if (festivalScale != "")
        {
            var f = FestivalClient.Instance;
            if (f == null || !f.OpenFor(festivalScale)) return false;
        }
        int h = GameClock.Hour;
        return openHour <= closeHour ? h >= openHour && h < closeHour : h >= openHour || h < closeHour;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 3f;
        bool out_ = ShouldBeOut();
        if (out_ && crowd == null) Spawn();
        else if (!out_ && crowd != null) { Destroy(crowd); crowd = null; }
    }

    private void Spawn()
    {
        crowd = new GameObject("Crowd");
        crowd.transform.SetParent(transform, false);
        string[] lines = string.IsNullOrEmpty(phrases) ? null : phrases.Split('|');
        string[] foreign = string.IsNullOrEmpty(touristPhrases) ? lines : touristPhrases.Split('|');
        int looks = Mathf.Max(1, CharacterModel.SkinCount);
        for (int i = 0; i < count; i++)
        {
            var v = new GameObject("Villager");
            v.transform.SetParent(crowd.transform, false);
            v.transform.position = RandomPoint();
            v.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (i < tourists) v.AddComponent<Villager>().InitTourist(this, Random.Range(0, 100000), foreign);
            else v.AddComponent<Villager>().Init(this, Random.Range(0, looks), lines);
        }
    }
}
