using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shows every UI Text and world TextMesh in the chosen language (Lang). The game sets its texts in
// Thai as before; at the very end of each frame's LateUpdate (after every other script, before the
// canvases lay out their text) any text that changed is swapped for its English, so the Thai never
// reaches the screen. The Thai is remembered, so switching back to ไทย puts it back.
// Created once, lives across scenes.
[DefaultExecutionOrder(32000)]
public class Localizer : MonoBehaviour
{
    private class Seen
    {
        public string thai, shown;
    }

    private readonly Dictionary<Object, Seen> seen = new Dictionary<Object, Seen>();
    private readonly List<Object> gone = new List<Object>();
    private int frame;
    private static Localizer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("Localizer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Localizer>();
    }

    private void OnEnable()
    {
        Lang.Changed += Switch;
    }

    private void OnDisable()
    {
        Lang.Changed -= Switch;
    }

    private void Switch()
    {
        // back to the Thai each text was given, then (for English) translate again
        foreach (var kv in seen)
        {
            if (kv.Key == null) continue;
            if (kv.Key is Text t) { if (t.text == kv.Value.shown) t.text = kv.Value.thai; }
            else if (kv.Key is TextMesh m) { if (m.text == kv.Value.shown) m.text = kv.Value.thai; }
        }
        seen.Clear();
        frame = 0;
        Apply();
        ApplyWorld();
    }

    private void LateUpdate()
    {
        Apply();
        ApplyWorld();
        if (++frame % 600 == 0) Prune();
    }

    private void Apply()
    {
        if (!Lang.IsEnglish) return;
        foreach (var t in FindObjectsByType<Text>(FindObjectsInactive.Exclude))
        {
            string now = t.text;
            if (string.IsNullOrEmpty(now)) continue;
            if (seen.TryGetValue(t, out var s) && ReferenceEquals(now, s.shown)) continue;
            if (Skip(t)) continue;
            string en = Lang.T(now);
            if (s == null) seen[t] = s = new Seen();
            s.thai = now;
            s.shown = en;
            if (!ReferenceEquals(en, now)) t.text = en;
        }
    }

    private void ApplyWorld()
    {
        if (!Lang.IsEnglish) return;
        foreach (var m in FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude))
        {
            string now = m.text;
            if (string.IsNullOrEmpty(now)) continue;
            if (seen.TryGetValue(m, out var s) && ReferenceEquals(now, s.shown)) continue;
            if (m.GetComponentInParent<NoTranslate>() != null) continue;
            string en = Lang.T(now);
            if (s == null) seen[m] = s = new Seen();
            s.thai = now;
            s.shown = en;
            if (!ReferenceEquals(en, now)) m.text = en;
        }
    }

    // What the player types, and texts marked to keep, stay as they are
    private static bool Skip(Text t)
    {
        if (t.GetComponent<NoTranslate>() != null) return true;
        var field = t.GetComponentInParent<InputField>();
        return field != null && field.textComponent == t;
    }

    private void Prune()
    {
        gone.Clear();
        foreach (var k in seen.Keys) if (k == null) gone.Add(k);
        foreach (var k in gone) seen.Remove(k);
    }
}
