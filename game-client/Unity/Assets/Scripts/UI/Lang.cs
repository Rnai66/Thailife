using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// Game language (ภาษา): Thai is how the game is written; English comes from Resources/Lang/en.txt,
// one "Thai<TAB>English" pair per line (\n for a line break). A Thai text with "{}" holes is a
// template: "ได้ {} บาท<TAB>Earned {} baht" also matches "ได้ 120 บาท"; "{0}"/"{1}" on the English
// side reorder the holes. What a hole caught is translated too (item names and the like).
// Localizer applies this to every Text and TextMesh on screen, so the rest of the code stays Thai.
public static class Lang
{
    public const string Thai = "th", English = "en";
    private const string Key = "lang";

    private static string current;
    public static event Action Changed;

    public static string Current
    {
        get
        {
            if (current == null)
            {
                string saved = PlayerPrefs.GetString(Key, "");
                current = saved == Thai || saved == English ? saved
                    : Application.systemLanguage == SystemLanguage.Thai ? Thai : English;
            }
            return current;
        }
        set
        {
            if (value != Thai && value != English || value == Current) return;
            current = value;
            PlayerPrefs.SetString(Key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static bool IsEnglish => Current == English;

    // Pick by language in code: Lang.Pick("ไทย", "Thai")
    public static string Pick(string thai, string english) => IsEnglish ? english : thai;

    // ── The dictionary ───────────────────────────────────────────────────────────────────
    private class Template
    {
        public Regex match;
        public string english;
        public int weight;
    }

    private static Dictionary<string, string> exact;
    private static List<Template> templates;
    private static List<KeyValuePair<string, string>> phrases; // longest first, for pieces of a text
    private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

    private static void Load()
    {
        if (exact != null) return;
        exact = new Dictionary<string, string>();
        templates = new List<Template>();
        phrases = new List<KeyValuePair<string, string>>();
        var asset = Resources.Load<TextAsset>("Lang/en");
        if (asset == null) { Debug.LogWarning("[Lang] Resources/Lang/en.txt missing"); return; }
        foreach (var raw in asset.text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            int tab = line.IndexOf('\t');
            if (tab <= 0 || tab == line.Length - 1) continue;
            string th = line.Substring(0, tab).Replace("\\n", "\n");
            string en = line.Substring(tab + 1).Replace("\\n", "\n");
            if (th.Contains("{}"))
            {
                var parts = th.Split(new[] { "{}" }, StringSplitOptions.None);
                var sb = new StringBuilder("^");
                int literal = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append("(.+?)");
                    sb.Append(Regex.Escape(parts[i]));
                    literal += parts[i].Trim().Length;
                }
                sb.Append("$");
                if (literal < 2) continue; // "{} {}" would match anything
                templates.Add(new Template { match = new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant), english = en, weight = literal });
            }
            else
            {
                exact[th] = en;
                if (HasThai(th) && th.Length >= 4) phrases.Add(new KeyValuePair<string, string>(th, en));
            }
        }
        templates.Sort((a, b) => b.weight.CompareTo(a.weight));
        phrases.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
    }

    public static bool HasThai(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s) if (c >= '฀' && c <= '๿') return true;
        return false;
    }

    // Thai text as the player should read it now
    public static string T(string thai)
    {
        if (!IsEnglish || !HasThai(thai)) return thai;
        Load();
        if (cache.TryGetValue(thai, out var done)) return done;
        string result = Translate(thai, 0);
        if (cache.Count > 4000) cache.Clear();
        cache[thai] = result;
        return result;
    }

    private static readonly string[] Separators = { "\n", " · ", " — ", " | " };

    private static string Translate(string s, int depth)
    {
        if (!HasThai(s) || depth > 4) return s;
        string trimmed = s.Trim();
        if (trimmed.Length != s.Length)
        {
            int lead = s.IndexOf(trimmed, StringComparison.Ordinal);
            return s.Substring(0, lead) + Translate(trimmed, depth) + s.Substring(lead + trimmed.Length);
        }
        if (exact.TryGetValue(s, out var en)) return en;

        foreach (var t in templates)
        {
            var m = t.match.Match(s);
            if (!m.Success) continue;
            var holes = new string[m.Groups.Count - 1];
            for (int i = 1; i < m.Groups.Count; i++) holes[i - 1] = Translate(m.Groups[i].Value, depth + 1);
            return Fill(t.english, holes);
        }

        // A longer text: translate it piece by piece
        foreach (var sep in Separators)
        {
            if (!s.Contains(sep)) continue;
            var parts = s.Split(new[] { sep }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++) parts[i] = Translate(parts[i], depth + 1);
            return string.Join(sep, parts);
        }
        int colon = s.IndexOf(": ", StringComparison.Ordinal);
        if (colon > 0)
            return Translate(s.Substring(0, colon), depth + 1) + ": " + Translate(s.Substring(colon + 2), depth + 1);

        return Phrases(s);
    }

    private static string Fill(string english, string[] holes)
    {
        var sb = new StringBuilder();
        int next = 0;
        for (int i = 0; i < english.Length; i++)
        {
            char c = english[i];
            if (c == '{' && i + 1 < english.Length)
            {
                if (english[i + 1] == '}') { sb.Append(next < holes.Length ? holes[next] : ""); next++; i++; continue; }
                int close = english.IndexOf('}', i);
                if (close > i + 1 && int.TryParse(english.Substring(i + 1, close - i - 1), out int n))
                {
                    sb.Append(n < holes.Length ? holes[n] : "");
                    i = close;
                    continue;
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // Last resort: swap every known Thai phrase inside the text for its English
    private static string Phrases(string s)
    {
        string result = s;
        foreach (var p in phrases)
        {
            if (!HasThai(result)) break;
            int at = result.IndexOf(p.Key, StringComparison.Ordinal);
            while (at >= 0)
            {
                string before = at > 0 && !char.IsWhiteSpace(result[at - 1]) && result[at - 1] != '(' ? " " : "";
                int end = at + p.Key.Length;
                string after = end < result.Length && !char.IsWhiteSpace(result[end]) && !char.IsPunctuation(result[end]) ? " " : "";
                string swap = before + p.Value + after;
                result = result.Substring(0, at) + swap + result.Substring(end);
                at = result.IndexOf(p.Key, at + swap.Length, StringComparison.Ordinal);
            }
        }
        return result;
    }
}
