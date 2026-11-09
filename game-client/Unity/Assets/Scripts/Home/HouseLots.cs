using System.Collections.Generic;
using UnityEngine;

// The house lots of one map (หมู่บ้าน / เมือง / กรุงเทพ, home.lua lots_get): every lot someone lives on
// shows their house — a copy of their style's template from HouseTemplates (built by the Editor, kept
// inactive) — with their name on the sign; the others show "ว่าง". The หมู่ shown is the one you live
// in when this is your area, else หมู่ที่ 1. Your own house appears at once from what HomeClient knows;
// the neighbours follow when the server answers, and again every minute (people move in and out).
public class HouseLots : MonoBehaviour
{
    [SerializeField] private string area = "village";
    [SerializeField] private Transform templates;

    private readonly Dictionary<int, HouseLot> lots = new Dictionary<int, HouseLot>();
    private float nextRefresh;
    private bool busy;

    public static HouseLots Current { get; private set; }
    public string Area => area;
    public int Moo => HomeClient.MooIn(area);
    // the หมู่ on show is the one you live in (so the lot with your number is yours)
    public bool ShowingMyMoo => HomeClient.LivesIn(area) && Moo == HomeClient.MyMoo;

    public void Configure(string areaId, Transform templateRoot)
    {
        area = areaId;
        templates = templateRoot;
    }

    private void Awake()
    {
        Current = this;
        foreach (var lot in GetComponentsInChildren<HouseLot>(true)) lots[lot.Number] = lot;
        if (templates != null) templates.gameObject.SetActive(false);
    }

    private void OnEnable() { HomeClient.StateChanged += OnHome; FarmLots.Changed += OnFarm; }
    private void OnDisable() { HomeClient.StateChanged -= OnHome; FarmLots.Changed -= OnFarm; }

    private void OnFarm(FarmLots.View v) { if (!HomeClient.LivesIn(area)) nextRefresh = 0f; } // the หมู่ on show may follow your farm

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Start()
    {
        foreach (var lot in lots.Values) lot.Clear(false);
        ShowMine();
        nextRefresh = 0f;
    }

    private void OnHome(HomeClient.State s)
    {
        ShowMine();
        nextRefresh = 0f; // fetch the neighbours again now
    }

    private void Update()
    {
        if (Time.time < nextRefresh || busy) return;
        nextRefresh = Time.time + 60f;
        Refresh();
    }

    public GameObject Template(string style)
    {
        if (templates == null || string.IsNullOrEmpty(style)) return null;
        var t = templates.Find("Tpl_" + style);
        return t != null ? t.gameObject : null;
    }

    // Your own house straight away, from what the game already knows
    private void ShowMine()
    {
        var s = HomeClient.Instance != null ? HomeClient.Instance.Current : null;
        if (s == null || !ShowingMyMoo) { HomePlot.ApplyAll(); return; }
        // a lot you've left (moved within this map) goes back to waiting for the server
        foreach (var lot in lots.Values)
            if (lot.Number != s.lot && lot.Style != null && lot.GetComponentInChildren<HomePlot>() is HomePlot p && p.IsMine) lot.Clear(false);
        if (lots.TryGetValue(s.lot, out var mine)) mine.Show(Template(s.style), s.style, MyName());
        HomePlot.ApplyAll();
    }

    private static string MyName()
    {
        return ProfileClient.Instance != null ? ProfileClient.Instance.DisplayName ?? "" : "";
    }

    private async void Refresh()
    {
        var home = HomeClient.Instance;
        if (home == null || !home.IsOnline) return;
        busy = true;
        try
        {
            var view = await home.LotsAsync(area, Moo);
            if (this == null || view?.lots == null) return;
            foreach (var l in view.lots)
            {
                if (!lots.TryGetValue(l.no, out var lot)) continue;
                if (l.vacant) lot.Clear(true);
                else lot.Show(Template(l.style), l.style, l.owner);
            }
            HomePlot.ApplyAll();
        }
        catch (System.Exception e) { Debug.LogWarning($"[Lots] {e.Message}"); }
        finally { busy = false; }
    }
}
