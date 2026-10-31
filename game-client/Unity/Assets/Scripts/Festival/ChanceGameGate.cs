using UnityEngine;

// สอยดาว, บิงโก, มหาลัย ก.ไก่ and ปาโป่ง are paid games with a random prize, which Apple counts as
// simulated gambling. Their booths stay hidden unless the server turns them on (CHANCE_GAMES=true).
public class ChanceGameGate : MonoBehaviour
{
    private bool shown = true;
    private float nextCheck;

    private void OnEnable()
    {
        nextCheck = 0f;
        Check();
    }

    private void Update()
    {
        if (Time.time >= nextCheck) Check();
    }

    private void Check()
    {
        nextCheck = Time.time + 1f;
        var festival = FestivalClient.Instance;
        bool on = festival != null && festival.Current != null && festival.Current.chance_games;
        if (on == shown) return;
        shown = on;
        foreach (Transform child in transform) child.gameObject.SetActive(on);
    }
}
