using UnityEngine;

// Searchlights (ไฟสปอตไลท์ส่องฟ้า) at the big fair: each child named "Searchlight" sweeps its beam
// slowly round the night sky, out of step with the others. Hidden by day.
public class SkyBeams : MonoBehaviour
{
    private Transform[] lights;
    private bool shown = true;
    private float nextCheck;

    private void Awake()
    {
        var found = new System.Collections.Generic.List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "Searchlight") found.Add(t);
        lights = found.ToArray();
    }

    private void Update()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + 1.5f;
            bool night = DayNightCycle.Dark;
            if (night != shown)
            {
                shown = night;
                foreach (var l in lights) l.gameObject.SetActive(night);
            }
        }
        if (!shown) return;
        float t = Time.time;
        for (int i = 0; i < lights.Length; i++)
        {
            float yaw = Mathf.Sin(t * 0.23f + i * 1.7f) * 70f + i * 90f;
            float tilt = 20f + Mathf.Sin(t * 0.31f + i * 2.3f) * 14f; // from straight up
            lights[i].localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
