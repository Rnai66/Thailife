using System.Collections.Generic;
using UnityEngine;

// A cow pen: shows as many of your cows as live there; calves (ลูกวัว) start small and grow to full
// size as they are fed. The village pen holds the herd up to its own room; a second pen built on your
// land (land.lua, landPen) shows the cows beyond that.
public class CowPen : MonoBehaviour
{
    [SerializeField] private bool landPen;

    public void Configure(bool onLand) => landPen = onLand;

    private readonly List<GameObject> cows = new List<GameObject>();
    private readonly List<Vector3> sizes = new List<Vector3>();
    private RanchClient ranch;

    public bool HasCows { get; private set; }

    private void Awake()
    {
        foreach (var w in GetComponentsInChildren<CowWander>(true)) { cows.Add(w.gameObject); sizes.Add(w.transform.localScale); }
        Show(0, 0, null, 1);
    }

    private void Start()
    {
        ranch = RanchClient.Instance;
        if (ranch == null) return;
        ranch.Changed += OnChanged;
        if (ranch.Current != null) OnChanged(ranch.Current);
    }

    private void OnDestroy()
    {
        if (ranch != null) ranch.Changed -= OnChanged;
    }

    private void OnChanged(RanchClient.State s)
    {
        int villageRoom = s.max_cows - s.land_cows;
        int first = landPen ? villageRoom : 0;
        int shown = landPen ? s.cows : Mathf.Min(s.cows, villageRoom);
        Show(first, shown, s.herd, Mathf.Max(1, s.grown_days));
    }

    // Cows first..count-1 of the herd, one per cow model in this pen
    private void Show(int first, int count, RanchClient.Cow[] herd, int grownDays)
    {
        HasCows = count > first;
        for (int i = 0; i < cows.Count; i++)
        {
            if (cows[i] == null) continue;
            int cow = first + i;
            cows[i].SetActive(cow < count);
            float grow = herd != null && cow < herd.Length ? Mathf.Clamp01((float)herd[cow].g / grownDays) : 1f;
            cows[i].transform.localScale = sizes[i] * Mathf.Lerp(0.7f, 1f, grow);
        }
    }
}
