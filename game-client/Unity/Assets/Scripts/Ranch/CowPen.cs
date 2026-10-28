using System.Collections.Generic;
using UnityEngine;

// The village cow pen: shows as many cows as you own (the pen is built with room for eight); calves
// (ลูกวัว) start small and grow to full size as they are fed.
public class CowPen : MonoBehaviour
{
    private readonly List<GameObject> cows = new List<GameObject>();
    private readonly List<Vector3> sizes = new List<Vector3>();
    private RanchClient ranch;

    public bool HasCows { get; private set; }

    private void Awake()
    {
        foreach (var w in GetComponentsInChildren<CowWander>(true)) { cows.Add(w.gameObject); sizes.Add(w.transform.localScale); }
        Show(0, null, 1);
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

    private void OnChanged(RanchClient.State s) => Show(s.cows, s.herd, Mathf.Max(1, s.grown_days));

    private void Show(int count, RanchClient.Cow[] herd, int grownDays)
    {
        HasCows = count > 0;
        for (int i = 0; i < cows.Count; i++)
        {
            if (cows[i] == null) continue;
            cows[i].SetActive(i < count);
            float grow = herd != null && i < herd.Length ? Mathf.Clamp01((float)herd[i].g / grownDays) : 1f;
            cows[i].transform.localScale = sizes[i] * Mathf.Lerp(0.7f, 1f, grow);
        }
    }
}
