using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Banishing a ghost (ปราบผี): the หมอผี chants the คาถา word by word — each word comes up with its ยันต์
// burning, and the player must type it before the fire dies (นะโม… อิติปิโส…). A word typed in time seals a
// sigil; enough and the ghost is bound and driven off for the night (ghost_start → ghost_finish with the
// hits). The harder the ghost, the longer the chant and the faster each yantra burns. In English the
// words are the Pali in Roman letters. Starting costs energy whether you win or not.
public class ExorciseUI : MonoBehaviour
{
    private static ExorciseUI instance;

    private GameObject backdrop;
    private Text titleText, subtitleText, info, chant, word, hitsText, result;
    private RectTransform arena, hpFill, timeFill, fieldRect;
    private Image ghostImage;
    private RawImage ghostView;          // the ghost itself, filmed live where it floats (GhostSpot's model)
    private Camera portrait;
    private RenderTexture portraitTexture;
    private InputField input;
    private Button startButton, closeButton;
    private GhostSpot spot;
    private string token;
    private int sigils, need, hits, seconds, level;
    private bool busy, playing;

    // Old Pali chants, a word at a time: the ones to ward off spirits for the small ghosts, then the
    // อิติปิโส (Buddha's virtues) and the ธรรมคุณ for the stronger ones. Thai spelling and the same in Roman.
    private static readonly string[] Easy =
        { "นะโม", "พุทธายะ", "นะมะพะทะ", "จะภะกะสะ", "มะอะอุ", "นะโม", "ตัสสะ", "ภะคะวะโต", "อะระหะโต", "สัมมา", "สัมพุทธัสสะ" };
    private static readonly string[] EasyRoman =
        { "namo", "buddhaya", "namaphatha", "chaphakasa", "maauu", "namo", "tassa", "bhagavato", "arahato", "samma", "sambuddhassa" };
    private static readonly string[] Middle =
        { "อิติปิโส", "ภะคะวา", "อะระหัง", "สัมมาสัมพุทโธ", "วิชชาจะระณะ", "สัมปันโน", "สุคะโต", "โลกะวิทู", "อะนุตตะโร",
          "ปุริสะทัมมะ", "สาระถิ", "สัตถา", "เทวะมะนุสสานัง", "พุทโธ", "ภะคะวาติ" };
    private static readonly string[] MiddleRoman =
        { "itipiso", "bhagava", "araham", "sammasambuddho", "vijjacarana", "sampanno", "sugato", "lokavidu", "anuttaro",
          "purisadamma", "sarathi", "sattha", "devamanussanam", "buddho", "bhagavati" };
    private static readonly string[] Hard =
        { "สวากขาโต", "ภะคะวะตา", "ธัมโม", "สันทิฏฐิโก", "อะกาลิโก", "เอหิปัสสิโก", "โอปะนะยิโก", "ปัจจัตตัง", "เวทิตัพโพ", "วิญญูหีติ" };
    private static readonly string[] HardRoman =
        { "svakkhato", "bhagavata", "dhammo", "sanditthiko", "akaliko", "ehipassiko", "opanayiko", "paccattam", "veditabbo", "vinnuhiti" };

    public static void Show(GhostSpot at)
    {
        if (instance == null) instance = new GameObject("ExorciseUI").AddComponent<ExorciseUI>();
        if (instance.playing) return;
        instance.spot = at;
        instance.Ready();
    }

    private void Awake()
    {
        bool upright = UiKit.Portrait;
        var body = UiKit.PremiumWindow(transform, "ExorciseCanvas", UiKit.WindowSize(new Vector2(1300f, 920f), 1400f), 56, "ผี",
            out backdrop, out titleText, out subtitleText, Close, out _);
        body.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.1f); // night
        float w = upright ? 700f : 1260f, h = upright ? 1240f : 760f;

        var hpBack = UiKit.Panel("Hp", body, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, h * 0.5f - 40f), new Vector2(w - 80f, 30f));
        hpFill = UiKit.Panel("Fill", hpBack, new Color(0.55f, 0.95f, 0.5f), Vector2.zero, new Vector2(w - 84f, 26f));
        hpFill.anchorMin = hpFill.anchorMax = hpFill.pivot = new Vector2(0f, 0.5f);
        hpFill.anchoredPosition = new Vector2(2f, 0f);

        arena = UiKit.Rect("Arena", body, new Color(0f, 0f, 0f, 0f));
        arena.sizeDelta = new Vector2(w - 80f, h - 140f);
        arena.GetComponent<Image>().raycastTarget = false;
        var ghost = UiKit.Rect("Ghost", arena, Color.white);
        ghost.sizeDelta = new Vector2(upright ? 520f : 480f, upright ? 520f : 480f);
        ghost.anchoredPosition = new Vector2(0f, -60f);
        ghostImage = ghost.GetComponent<Image>();
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;
        // a round window onto the real ghost, soft dark edge
        var lens = UiKit.Rect("Lens", ghost, Color.white);
        lens.anchorMin = Vector2.zero;
        lens.anchorMax = Vector2.one;
        lens.sizeDelta = Vector2.zero;
        lens.GetComponent<Image>().sprite = UiKit.CircleSprite;
        lens.GetComponent<Image>().raycastTarget = false;
        lens.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var view = new GameObject("GhostView", typeof(RectTransform), typeof(RawImage));
        view.transform.SetParent(lens, false);
        var vr = (RectTransform)view.transform;
        vr.anchorMin = Vector2.zero;
        vr.anchorMax = Vector2.one;
        vr.sizeDelta = Vector2.zero;
        ghostView = view.GetComponent<RawImage>();
        ghostView.raycastTarget = false;
        ghostView.enabled = false;

        // Everything to read and type sits in the top part: the phone keyboard covers the bottom.
        // The chant so far · the word to type now · its burning fuse · the typing box · the score
        float top = h * 0.5f;
        chant = UiKit.Cartoon(UiKit.Label(body, 26, "", new Vector2(0f, top - 88f), new Vector2(w - 60f, 44f), new Color(0.85f, 0.85f, 0.8f)), 2f);
        chant.supportRichText = true;
        chant.gameObject.AddComponent<NoTranslate>();
        word = UiKit.Cartoon(UiKit.Label(body, 64, "", new Vector2(0f, top - 160f), new Vector2(w - 60f, 86f), UiKit.GoldText), 3.5f);
        word.supportRichText = true;
        word.gameObject.AddComponent<NoTranslate>();
        var fuseBack = UiKit.Panel("Fuse", body, new Color(1f, 1f, 1f, 0.1f), new Vector2(0f, top - 215f), new Vector2(620f, 14f));
        timeFill = UiKit.Panel("Fill", fuseBack, new Color(1f, 0.55f, 0.15f), Vector2.zero, new Vector2(616f, 10f));
        timeFill.anchorMin = timeFill.anchorMax = timeFill.pivot = new Vector2(0f, 0.5f);
        timeFill.anchoredPosition = new Vector2(2f, 0f);
        fieldRect = UiKit.Panel("ChantInput", body, Color.white, new Vector2(0f, top - 275f), new Vector2(620f, 78f), shadow: true);
        fieldRect.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        var typed = UiKit.Label(fieldRect, 34, "", Vector2.zero, new Vector2(580f, 70f), UiKit.Ink);
        typed.supportRichText = false;
        typed.resizeTextForBestFit = false;
        var placeholder = UiKit.Label(fieldRect, 26, "พิมพ์คาถาตรงนี้…", Vector2.zero, new Vector2(580f, 70f), UiKit.Muted);
        placeholder.fontStyle = FontStyle.Italic;
        input = fieldRect.gameObject.AddComponent<InputField>();
        input.textComponent = typed;
        input.placeholder = placeholder;
        input.characterLimit = 40;
        input.lineType = InputField.LineType.SingleLine;
        input.shouldHideMobileInput = true; // type straight into our box, not iOS's bar over the keyboard
        input.keyboardType = TouchScreenKeyboardType.Default;
        fieldRect.gameObject.SetActive(false);
        fuseBack.gameObject.SetActive(false);
        hitsText = UiKit.Cartoon(UiKit.Label(body, 26, "", new Vector2(0f, top - 335f), new Vector2(w - 60f, 40f), Color.white));
        info = UiKit.Label(body, 24, "", new Vector2(0f, -h * 0.5f + 230f), new Vector2(w - 100f, 110f), new Color(0.85f, 0.9f, 0.85f));
        result = UiKit.Cartoon(UiKit.Label(body, 40, "", new Vector2(0f, 40f), new Vector2(w - 60f, 120f), Color.white), 3f);
        startButton = UiKit.RoundButton(body, "เริ่มท่องคาถา", new Vector2(0f, -h * 0.5f + 130f), new Vector2(420f, 92f), UiKit.Gold, UiKit.GoldInk, Begin, 30);
        closeButton = UiKit.RoundButton(body, "กลับ", new Vector2(0f, -h * 0.5f + 130f), new Vector2(300f, 80f), UiKit.Pill, UiKit.Ink, Close, 26);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        Unfilm();
        if (instance == this) instance = null;
    }

    private void Ready()
    {
        var info2 = TempleClient.Instance?.GhostInfo(spot.Ghost);
        string name = info2?.name ?? "ผี";
        titleText.text = $"ปราบ{name}";
        subtitleText.text = "พิมพ์คาถาตามบทให้ทัน ก่อนไฟยันต์มอด";
        level = info2?.level ?? 1;
        ghostImage.sprite = GhostArt.Get(spot.Ghost);
        Film(spot.Model);
        Tint(1f);
        ghostImage.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        info.text = $"{name} ระดับ {info2?.level ?? 1} · ปราบได้ค่าครู {info2?.baht ?? 0:N0} บาท\nเริ่มแล้วใช้พลังงานทันที แม้ปราบไม่สำเร็จ";
        chant.text = "";
        word.text = "";
        hitsText.text = "";
        fieldRect.gameObject.SetActive(false);
        timeFill.parent.gameObject.SetActive(false);
        result.text = "";
        hpFill.localScale = Vector3.one;
        timeFill.localScale = Vector3.one;
        startButton.gameObject.SetActive(true);
        startButton.interactable = true;
        closeButton.gameObject.SetActive(false);
        ClearSigils();
        backdrop.SetActive(true);
    }

    private async void Begin()
    {
        var t = TempleClient.Instance;
        if (busy || t == null || spot == null) return;
        busy = true;
        startButton.interactable = false;
        try
        {
            var r = await t.GhostStartAsync(spot.Spot, spot.Ghost);
            token = r.token;
            sigils = Mathf.Max(1, r.sigils);
            need = Mathf.Max(1, r.need);
            seconds = Mathf.Max(8, r.seconds);
            hits = 0;
            startButton.gameObject.SetActive(false);
            info.text = "";
            GameAudio.Play("ghost");
            StartCoroutine(Play());
        }
        catch (ApiResponseException e)
        {
            info.text = TempleClient.Explain(e);
            startButton.interactable = true;
        }
        catch (Exception)
        {
            info.text = "เชื่อมต่อไม่ได้ ลองใหม่";
            startButton.interactable = true;
        }
        busy = false;
    }

    // The words of this ghost's chant: the small ghosts get the short warding words, stronger ones
    // the อิติปิโส, the strongest carry on into the ธรรมคุณ
    private string[] Words()
    {
        bool roman = Lang.IsEnglish;
        var list = new System.Collections.Generic.List<string>();
        if (level < 10) list.AddRange(roman ? EasyRoman : Easy);
        else if (level < 35) { list.AddRange(roman ? EasyRoman : Easy); list.RemoveRange(5, list.Count - 5); list.AddRange(roman ? MiddleRoman : Middle); }
        else { list.AddRange(roman ? MiddleRoman : Middle); list.AddRange(roman ? HardRoman : Hard); }
        var words = new string[sigils];
        for (int i = 0; i < sigils; i++) words[i] = list[i % list.Count];
        return words;
    }

    // What counts when comparing: no spaces or dots, any case
    private static string Plain(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s.Replace("\u0E4D\u0E32", "\u0E33")) // นิคหิต+า typed for ำ
            if (!char.IsWhiteSpace(c) && c != '…' && c != '.' && c != '-' && c != '\u200B') sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    // Letters you actually press (Thai vowel and tone marks ride on a letter, but are a key press too)
    private static float Keys(string w) => Lang.IsEnglish ? w.Length * 0.75f : w.Length;

    private IEnumerator Play()
    {
        playing = true;
        var words = Words();
        var done = new bool[words.Length];
        var missed = new bool[words.Length];
        // seconds per key: generous for the small ghosts, quicker for the strong ones
        float perKey = Mathf.Lerp(0.95f, 0.62f, Mathf.Clamp01((level - 1) / 49f));
        float start = Time.time;
        fieldRect.gameObject.SetActive(true);
        timeFill.parent.gameObject.SetActive(true);
        UpdateHits();
        for (int i = 0; i < words.Length; i++)
        {
            string target = Plain(words[i]);
            float life = 2.2f + Keys(words[i]) * perKey;
            float born = Time.time;
            input.text = "";
            Focus();
            ShowChant(words, i, done, missed);
            bool ok = false;
            while (Time.time - born < life)
            {
                string typed = Plain(input.text);
                int same = 0;
                while (same < typed.Length && same < target.Length && typed[same] == target[same]) same++;
                bool wrong = same < typed.Length;
                input.textComponent.color = wrong ? UiKit.Danger : UiKit.Ink;
                // the words have no spaces or dots, so what matched is the start of the word itself
                word.text = same > 0 ? $"<color=#8CF07A>{words[i].Substring(0, same)}</color>{words[i].Substring(same)}" : words[i];
                if (typed == target) { ok = true; break; }
                float k = 1f - (Time.time - born) / life;
                timeFill.localScale = new Vector3(Mathf.Clamp01(k), 1f, 1f);
                timeFill.GetComponent<Image>().color = Color.Lerp(new Color(1f, 0.2f, 0.1f), new Color(1f, 0.7f, 0.2f), k);
                Writhe(Time.time - start);
                yield return null;
            }
            if (ok)
            {
                done[i] = true;
                hits++;
                UpdateHits();
                GameAudio.Play("bell");
                Flare();
            }
            else
            {
                missed[i] = true;
                GameAudio.Play("ghost", null, 0.5f);
                StartCoroutine(Lunge());
            }
            ShowChant(words, i + 1, done, missed);
            word.text = ok ? $"<color=#8CF07A>{words[i]}</color>" : $"<color=#FF7A66>{words[i]}</color>";
            float pause = Time.time + 0.35f;
            while (Time.time < pause) { Writhe(Time.time - start); yield return null; }
            // won or lost for sure: stop early
            if (hits >= need || hits + (words.Length - 1 - i) < need) break;
        }
        input.DeactivateInputField();
        fieldRect.gameObject.SetActive(false);
        timeFill.parent.gameObject.SetActive(false);
        // the server wants the chant to have taken a fair while
        while (Time.time - start < seconds * 0.45f) { Writhe(Time.time - start); yield return null; }
        word.text = "";
        Finish();
    }

    private void Focus()
    {
        input.Select();
        input.ActivateInputField();
    }

    private void ShowChant(string[] words, int at, bool[] done, bool[] missed)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            string c = i < at ? (done[i] ? "#8CF07A" : missed[i] ? "#FF7A66" : "#DDDDDD") : i == at ? "#FFD966" : "#77776F";
            sb.Append($"<color={c}>{words[i]}</color>");
        }
        chant.text = sb.ToString();
    }

    // The ghost writhes, and fades as it is bound
    private void Writhe(float t)
    {
        float bound = Mathf.Clamp01(hits / (float)need);
        ghostImage.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 2.1f) * 30f, -60f + Mathf.Sin(t * 3.3f) * 14f);
        Tint(Mathf.Lerp(1f, 0.3f, bound));
    }

    // A missed word: the ghost lunges at you
    private IEnumerator Lunge()
    {
        float born = Time.time;
        while (Time.time - born < 0.5f)
        {
            float k = Mathf.Sin((Time.time - born) / 0.5f * Mathf.PI);
            ghostImage.rectTransform.localScale = Vector3.one * (1f + 0.25f * k);
            yield return null;
        }
        ghostImage.rectTransform.localScale = Vector3.one;
    }

    // A word typed in time: a yantra flares up round the ghost and fades
    private void Flare()
    {
        var size = arena.sizeDelta;
        var r = UiKit.Rect("Yant", arena, Color.white);
        r.sizeDelta = new Vector2(150f, 150f);
        r.anchoredPosition = new Vector2(UnityEngine.Random.Range(-size.x * 0.38f, size.x * 0.38f), UnityEngine.Random.Range(-size.y * 0.35f, size.y * 0.05f));
        var img = r.GetComponent<Image>();
        img.sprite = AmuletArt.Get("yant_8");
        img.preserveAspect = true;
        img.raycastTarget = false;
        r.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.5f, 0.1f, 0.9f);
        StartCoroutine(Fade(r, img));
    }

    private IEnumerator Fade(RectTransform r, Image img)
    {
        float born = Time.time;
        while (r != null && Time.time - born < 1.2f)
        {
            float k = (Time.time - born) / 1.2f;
            r.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, k);
            img.color = new Color(1f, 0.9f, 0.6f, 1f - k);
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }

    private void UpdateHits()
    {
        hitsText.text = $"สะกดได้ {hits}/{need} คำ";
        hpFill.localScale = new Vector3(Mathf.Clamp01(1f - hits / (float)need), 1f, 1f);
    }

    // Point a small camera at the ghost model so the window shows the real thing, lit by its own glow
    private void Film(Transform model)
    {
        Unfilm();
        if (model == null || !model.gameObject.activeInHierarchy) return;
        if (portraitTexture == null) portraitTexture = new RenderTexture(512, 512, 16) { name = "GhostPortrait" };
        var go = new GameObject("GhostCamera");
        portrait = go.AddComponent<Camera>();
        portrait.targetTexture = portraitTexture;
        portrait.clearFlags = CameraClearFlags.SolidColor;
        portrait.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
        portrait.fieldOfView = 32f;
        portrait.nearClipPlane = 0.1f;
        portrait.farClipPlane = 40f;
        ghostView.texture = portraitTexture;
        ghostView.enabled = true;
        filming = model;
        LateUpdate();
    }

    private Transform filming;

    private void LateUpdate()
    {
        if (portrait == null || filming == null) return;
        // frame the whole figure from in front (the ghost faces -Z), following its drift
        var b = new Bounds(filming.position + Vector3.up, Vector3.one);
        foreach (var r in filming.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        float dist = Mathf.Max(b.size.y, b.size.x) * 0.5f / Mathf.Tan(portrait.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
        Vector3 front = -filming.forward;
        front.y = 0f;
        if (front.sqrMagnitude < 0.01f) front = Vector3.back;
        portrait.transform.position = b.center + front.normalized * dist + Vector3.up * b.size.y * 0.05f;
        portrait.transform.LookAt(b.center);
    }

    private void Unfilm()
    {
        if (portrait != null) Destroy(portrait.gameObject);
        portrait = null;
        filming = null;
        if (ghostView != null) ghostView.enabled = false;
    }

    // How solid the ghost looks (it fades as it is bound)
    private void Tint(float a)
    {
        bool live = ghostView != null && ghostView.enabled;
        ghostImage.color = new Color(1f, 1f, 1f, live ? 0f : a);
        if (ghostView != null) ghostView.color = new Color(1f, 1f, 1f, a);
    }

    private void ClearSigils()
    {
        for (int i = arena.childCount - 1; i >= 0; i--)
            if (arena.GetChild(i) != ghostImage.transform) Destroy(arena.GetChild(i).gameObject);
    }

    private async void Finish()
    {
        var t = TempleClient.Instance;
        try
        {
            var r = await t.GhostFinishAsync(token, hits);
            if (r.won)
            {
                result.text = $"ปราบสำเร็จ!\n+{r.reward:N0} บาท · หมอผี XP +{r.xp}";
                result.color = new Color(0.6f, 1f, 0.6f);
                Tint(0f);
                UiKit.FloatAbovePlayer($"+{r.reward:N0} ฿", new Color(1f, 0.85f, 0.3f));
                GameAudio.Play("coins");
            }
            else
            {
                result.text = "ผีหนีไปได้… ลองใหม่คืนหน้า";
                result.color = new Color(1f, 0.6f, 0.5f);
                GameAudio.Play("ghost");
            }
        }
        catch (ApiResponseException e) { result.text = TempleClient.Explain(e); }
        catch (Exception) { result.text = "เชื่อมต่อไม่ได้"; }
        playing = false;
        closeButton.gameObject.SetActive(true);
    }

    private void Close()
    {
        if (playing) return; // no walking away mid-chant
        StopAllCoroutines();
        if (input != null) input.DeactivateInputField();
        Unfilm();
        ClearSigils();
        backdrop.SetActive(false);
    }
}
