using UnityEngine;

// The city junction's traffic lights (ไฟแดง): the main road and the side street (with the zebra
// crossings for people on foot) take turns. Thai-style signal heads with a red countdown; each
// head shows one approach. TrafficCar asks MayGo before its stop line.
public class TrafficLight : MonoBehaviour
{
    public enum Lamp { Red, Yellow, Green }

    [System.Serializable]
    public class Head
    {
        public int approach;        // 0 main road, 1 side street; pedestrian heads follow the side street phase
        public bool pedestrian;
        public GameObject red, yellow, green; // the lit lamps (the dark lenses stay)
        public TextMesh countdown;
    }

    // Phase lengths in seconds: main green, main yellow, all red, side green, side yellow, all red
    private static readonly float[] Phases = { 18f, 3f, 1.5f, 11f, 3f, 1.5f };

    [SerializeField] private Head[] heads;

    private float clock;
    private int lastShown = -1;

    public void Configure(Head[] signalHeads) => heads = signalHeads;

    private int Phase(out float left)
    {
        float t = clock;
        for (int i = 0; i < Phases.Length; i++)
        {
            if (t < Phases[i]) { left = Phases[i] - t; return i; }
            t -= Phases[i];
        }
        left = 0f;
        return 0;
    }

    public Lamp State(int approach)
    {
        int p = Phase(out _);
        int green = approach == 0 ? 0 : 3;
        return p == green ? Lamp.Green : p == green + 1 ? Lamp.Yellow : Lamp.Red;
    }

    public bool MayGo(int approach) => State(approach) == Lamp.Green;

    // Seconds until this approach's lamp changes (for the countdown display)
    private float SecondsLeft(int approach)
    {
        int p = Phase(out float left);
        int green = approach == 0 ? 0 : 3;
        if (p == green || p == green + 1) return left;
        float t = left; // red: count down to the next green
        for (int i = (p + 1) % Phases.Length; i != green; i = (i + 1) % Phases.Length) t += Phases[i];
        return t;
    }

    private void Start() => clock = Random.Range(0f, 10f);

    private void Update()
    {
        float cycle = 0f;
        foreach (float f in Phases) cycle += f;
        clock = (clock + Time.deltaTime) % cycle;
        int second = Mathf.FloorToInt(clock * 4f); // refresh four times a second (blinking walk man)
        if (second == lastShown || heads == null) return;
        lastShown = second;
        bool blink = second % 2 == 0;
        foreach (var h in heads)
        {
            var lamp = State(h.approach);
            if (h.pedestrian) // walk while the side street has green, blink on its yellow
            {
                if (h.green != null) h.green.SetActive(lamp == Lamp.Green || (lamp == Lamp.Yellow && blink));
                if (h.red != null) h.red.SetActive(lamp == Lamp.Red);
            }
            else
            {
                if (h.red != null) h.red.SetActive(lamp == Lamp.Red);
                if (h.yellow != null) h.yellow.SetActive(lamp == Lamp.Yellow);
                if (h.green != null) h.green.SetActive(lamp == Lamp.Green);
            }
            if (h.countdown != null)
            {
                h.countdown.text = Mathf.CeilToInt(SecondsLeft(h.approach)).ToString();
                h.countdown.color = lamp == Lamp.Green ? new Color(0.3f, 1f, 0.4f) : lamp == Lamp.Yellow ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.2f, 0.15f);
            }
        }
    }
}
