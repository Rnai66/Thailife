using System.Collections.Generic;
using UnityEngine;

// The village cow pen: shows as many cows as you own (the pen is built with room for eight).
public class CowPen : MonoBehaviour
{
    private readonly List<GameObject> cows = new List<GameObject>();
    private RanchClient ranch;

    public bool HasCows { get; private set; }

    private void Awake()
    {
        foreach (var w in GetComponentsInChildren<CowWander>(true)) cows.Add(w.gameObject);
        Show(0);
    }

    private void Start()
    {
        ranch = RanchClient.Instance;
        if (ranch == null) return;
        ranch.Changed += OnChanged;
        if (ranch.Current != null) Show(ranch.Current.cows);
    }

    private void OnDestroy()
    {
        if (ranch != null) ranch.Changed -= OnChanged;
    }

    private void OnChanged(RanchClient.State s) => Show(s.cows);

    private void Show(int count)
    {
        HasCows = count > 0;
        for (int i = 0; i < cows.Count; i++)
            if (cows[i] != null) cows[i].SetActive(i < count);
    }
}
