using System;
using System.Collections;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Stage window for a venue: pick an instrument, then play the set as a deck of music cards. Each card
// is engraved on a five-line staff for the instrument in your hands — read a note, find every note of
// a chord (คอร์ด C D E F G A B, minors and sevenths later, with the guitar's chord box), fill the gap in
// a scale, play the five notes of a ลายแคน, the root for the bass, a drum bar for the drums — and you
// answer on a one-octave keyboard (or the drum pads), hearing your instrument as you go. Like
// ปราบผี (ExorciseUI) each card burns down: quick and clean is เป๊ะ, right but slow or after a slip is
// ดี, two wrong notes or the fuse out is พลาด. The crowd warms up as you play. The server deals the
// deck size and pays the tips when the set ends (music.lua).
public class MusicUI : MonoBehaviour
{
    private const float PerfectShare = 0.6f; // answered inside this share of the fuse, with no slip: เป๊ะ

    private static MusicUI instance;

    private string venueId;
    private GameObject backdrop;
    private RectTransform card;
    private Text titleText, subtitleText, infoText, statusText, messageText;
    private Image statusPill;
    private RectTransform instrumentList;
    private Button startButton;
    private bool busy, playing, P; // P: upright layout
    private PlayerController frozenPlayer;

    // ── The card game ──
    private RectTransform game, deck, face, fuseFill, meterFill, keysRoot, padsRoot, choicesRoot, judgeRect;
    private int level = 1;
    private int wrongChoice = -1;
    private Text scoreText, judgeText, deckText, liveName;
    private RawImage liveView;
    private Camera liveCam;
    private RenderTexture liveTexture;
    private AudioSource voice;
    private MusicClient.Chart chart;
    private List<MusicTheory.Card> cards;
    private string instrument;
    private int perfect, good, miss, combo, current;
    private float setStart;
    private bool accepting;
    private readonly Dictionary<int, Image> keyImages = new Dictionary<int, Image>();
    private readonly Dictionary<int, Color> keyColors = new Dictionary<int, Color>();

    // the card being played
    private MusicTheory.Card c;
    private StaffView staff;
    private Text revealText, askText;
    private readonly List<int> found = new List<int>();
    private readonly Dictionary<int, RectTransform> slots = new Dictionary<int, RectTransform>(); // note index → its ? box / note
    private float[] slotX;
    private int mistakes, seqAt;
    private bool cardDone, cardWon;

    private static readonly Color StageDark = new Color32(0x16, 0x10, 0x0C, 0xFF);
    private static readonly Color InkNote = new Color32(0x22, 0x18, 0x12, 0xFF);
    private static readonly Color FoundGreen = new Color32(0x1F, 0x7A, 0x3E, 0xFF);
    private static readonly Color MissRed = new Color32(0xB2, 0x3A, 0x22, 0xFF);
    private static readonly Color[] PadColors =
    {
        new Color32(0x4A, 0x5D, 0x8C, 0xFF), new Color32(0xC8, 0x5A, 0x3C, 0xFF), new Color32(0xE8, 0xB6, 0x4C, 0xFF),
        new Color32(0x5E, 0x8C, 0x4F, 0xFF), new Color32(0xB8, 0x8A, 0x2E, 0xFF),
    };

    public static void Show(string venue)
    {
        if (instance == null)
            instance = new GameObject("MusicUI").AddComponent<MusicUI>();
        instance.venueId = venue;
        instance.backdrop.SetActive(true);
        instance.ShowMenu("");
        _ = instance.RefreshMenu();
    }

    public static void Hide()
    {
        if (instance != null && !instance.playing) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        P = UiKit.Portrait;
        card = UiKit.PremiumWindow(transform, "MusicCanvas", UiKit.WindowSize(new Vector2(1500f, 1000f), 1560f), 55, "icon:music",
            out backdrop, out titleText, out subtitleText, Hide, out _);
        titleText.text = "เวทีดนตรี";

        // Musician level + venue details
        // Upright: everything in one column, the instruments two per row
        var info = UiKit.Panel("Info", card, Color.white, new Vector2(0f, P ? 570f : 300f), new Vector2(P ? 690f : 1380f, P ? 230f : 190f), shadow: true);
        info.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        infoText = UiKit.Label(info, 26, "", new Vector2(P ? 0f : -130f, P ? 30f : 20f), new Vector2(P ? 650f : 1080f, P ? 150f : 130f), UiKit.Ink, TextAnchor.MiddleLeft);
        var pill = UiKit.Panel("Status", info, UiKit.OkTint, new Vector2(0f, P ? -85f : -62f), new Vector2(P ? 650f : 1320f, 48f));
        statusPill = pill.GetComponent<Image>();
        statusText = UiKit.Label(pill, 24, "", Vector2.zero, new Vector2(P ? 640f : 1300f, 44f), UiKit.OkGreen);
        statusText.fontStyle = FontStyle.Bold;

        UiKit.Label(card, 28, "เลือกเครื่องดนตรี", new Vector2(P ? -175f : -490f, P ? 420f : 165f), new Vector2(P ? 330f : 400f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(card, 22, "ซื้อเพิ่มได้ที่ร้านเครื่องดนตรีในเมือง", new Vector2(P ? 175f : 390f, P ? 420f : 165f), new Vector2(P ? 330f : 600f, 40f), UiKit.Muted, TextAnchor.MiddleRight);
        instrumentList = UiKit.Container(card, new Vector2(0f, P ? 340f : 85f));
        startButton = UiKit.RoundButton(card, "เริ่มเล่น (พลังงาน 10)", new Vector2(0f, P ? -150f : -200f), new Vector2(600f, 100f), UiKit.Gold, UiKit.GoldInk, StartPerformance, 36);
        messageText = UiKit.Label(card, 28, "", new Vector2(0f, P ? -285f : -330f), new Vector2(P ? 680f : 1300f, 110f), UiKit.Ink);
        messageText.fontStyle = FontStyle.Bold;

        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        BuildGame();
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (frozenPlayer != null) frozenPlayer.InputEnabled = true;
        StopLive();
        if (liveTexture != null) liveTexture.Release();
        if (instance == this) instance = null;
    }

    // ── Menu ────────────────────────────────────────────────────────────────────
    private void ShowMenu(string message)
    {
        game.gameObject.SetActive(false);
        foreach (Transform child in card) if (child != game) child.gameObject.SetActive(true);
        messageText.text = message;
        RenderMenu();
    }

    private async System.Threading.Tasks.Task RefreshMenu()
    {
        var music = MusicClient.Instance;
        if (music == null) return;
        try { await music.RefreshAsync(); } catch (Exception) { }
        RenderMenu();
    }

    // What the deck holds for each instrument
    private static string DeckFor(string id) =>
        id == "drums" ? "การ์ดโน้ตกลอง · จังหวะ 1 ห้อง (ตีแพดตามโน้ต)"
        : id == "bass" ? "การ์ดอ่านโน้ตกุญแจฟา · โน้ตรากของคอร์ด · บันไดเสียง"
        : id == "guitar" ? "การ์ดอ่านโน้ต · คอร์ด (พร้อมตารางจับคอร์ด) · บันไดเสียง"
        : id == "keyboard" ? "การ์ดอ่านโน้ต · คอร์ด · บันไดเสียง"
        : "การ์ดอ่านโน้ต (โด เร มี) · ลายแคน/ลายพิณ · บันไดเสียง";

    private void RenderMenu()
    {
        var music = MusicClient.Instance;
        UiKit.Clear(instrumentList);
        if (music == null || !music.IsOnline)
        {
            titleText.text = "เวทีดนตรี";
            infoText.text = "ต้องออนไลน์ก่อนถึงจะเล่นดนตรีได้";
            statusText.text = "ออฟไลน์";
            statusText.color = UiKit.Danger;
            statusPill.color = UiKit.DangerTint;
            startButton.interactable = false;
            return;
        }

        var s = music.Current;
        var v = music.GetVenue(venueId);
        titleText.text = "เวทีดนตรี";
        subtitleText.text = v?.name ?? venueId;
        string bonus = v == null ? "" : v.bonus == "folk" ? "เครื่องดนตรีพื้นบ้าน +20%" : v.bonus == "modern" ? "เครื่องดนตรีสากล +20%" : "ทุกเครื่องดนตรี +20%";
        bool ready = v != null && v.unlocked && v.open && !v.played_today;
        statusText.text = v == null ? "" : !v.unlocked ? $"ต้องเป็นนักดนตรี Lv {v.level} ขึ้นไป"
            : !v.open ? $"ยังไม่เปิด — เปิด{v.when}"
            : v.played_today ? "คืนนี้เล่นที่ร้านนี้แล้ว — แวะไปร้านอื่น หรือมาใหม่พรุ่งนี้"
            : "เปิดอยู่ เล่นได้เลย!";
        statusText.color = ready ? UiKit.OkGreen : UiKit.Danger;
        statusPill.color = ready ? UiKit.OkTint : UiKit.DangerTint;
        infoText.text = $"<b>นักดนตรี Lv {s.level}</b> · XP {s.xp}/{s.xp_to_next} · เล่นมาแล้ว {s.performances} ครั้ง\n"
                        + (v == null ? "" : $"ทิปพื้นฐาน {v.@base} บาท · {bonus} · {v.when}\n")
                        + $"{s.instrument_name}: {DeckFor(s.instrument)}\n"
                        + $"<b>{MusicTheory.Lesson(s.instrument, s.level)}</b>"
                        + (MusicTheory.NextLesson(s.level) > 0 ? $" · บทต่อไปที่ Lv {MusicTheory.NextLesson(s.level)}" : "");

        int i = 0;
        if (s.instruments != null)
            foreach (var inst in s.instruments)
                AddInstrumentCard(inst, i++);
        startButton.interactable = !busy && v != null && v.unlocked && v.open && !v.played_today;
    }

    // Four cards per row, two rows (8 instruments)
    private void AddInstrumentCard(MusicClient.Instrument item, int index)
    {
        var pos = P ? new Vector2(index % 2 == 0 ? -172f : 172f, -(index / 2) * 115f) : new Vector2((index % 4 - 1.5f) * 345f, -(index / 4) * 115f);
        var c = UiKit.Panel("Instrument", instrumentList, item.equipped ? (Color)new Color32(0xFF, 0xF6, 0xDF, 0xFF) : Color.white, pos, new Vector2(330f, 102f), shadow: true);
        c.gameObject.AddComponent<Outline>().effectColor = item.equipped ? UiKit.Gold : UiKit.CardBorder;
        if (UiKit.Art("Icons/inst_" + item.id) != null)
        {
            var icon = UiKit.IconTile(c, "inst_" + item.id, new Vector2(-118f, 0f), new Vector2(82f, 82f));
            if (!item.owned) foreach (var img in icon.GetComponentsInChildren<Image>()) img.color = new Color(1f, 1f, 1f, 0.45f); // not yours yet
        }
        UiKit.Label(c, 26, item.name, new Vector2(-15f, 20f), new Vector2(130f, 40f), item.owned ? UiKit.Ink : UiKit.Muted, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.Label(c, 20, item.owned ? $"{(item.folk ? "พื้นบ้าน" : "สากล")} x{item.mult:0.00}" : "ยังไม่มี", new Vector2(-15f, -22f), new Vector2(130f, 34f), UiKit.Muted, TextAnchor.MiddleLeft);
        if (item.equipped)
            UiKit.PillLabel(c, "ใช้อยู่", new Vector2(100f, 0f), new Vector2(100f, 44f), UiKit.Gold, UiKit.GoldInk, 22);
        else
        {
            var b = UiKit.RoundButton(c, "ใช้", new Vector2(100f, 0f), new Vector2(100f, 56f), UiKit.Pill, UiKit.Price, () => Equip(item), 24);
            b.interactable = item.owned && !busy;
        }
    }

    private async void Equip(MusicClient.Instrument inst)
    {
        var music = MusicClient.Instance;
        if (busy || music == null) return;
        busy = true;
        try { await music.EquipAsync(inst.id); messageText.text = $"เปลี่ยนเป็น{inst.name}แล้ว"; }
        catch (Exception) { messageText.text = "เปลี่ยนเครื่องดนตรีไม่สำเร็จ"; }
        busy = false;
        RenderMenu();
    }

    // ── Performance ─────────────────────────────────────────────────────────────
    private async void StartPerformance()
    {
        var music = MusicClient.Instance;
        if (busy || music == null) return;
        busy = true;
        try
        {
            chart = await music.StartAsync(venueId);
            BeginGame(music.Current);
        }
        catch (ApiResponseException e)
        {
            messageText.text = e.Message.Contains("energy") ? "พลังงานไม่พอ — กินอะไรหน่อยหรือไปนอนพัก"
                : e.Message.Contains("level") ? "เลเวลนักดนตรียังไม่ถึง"
                : e.Message.Contains("closed") ? "เวทียังไม่เปิดตอนนี้"
                : e.Message.Contains("already played") ? "คืนนี้เล่นที่ร้านนี้แล้ว"
                : e.Message.Contains("sleeping") ? "กำลังนอนอยู่" : "เริ่มเล่นไม่สำเร็จ";
        }
        catch (Exception e)
        {
            messageText.text = "เกิดข้อผิดพลาด ลองใหม่";
            Debug.LogWarning($"[Music] {e.Message}");
        }
        busy = false;
        if (!playing) RenderMenu();
    }

    private void BeginGame(MusicClient.State state)
    {
        instrument = state?.instrument ?? "pin";
        level = state?.level ?? 1;
        int count = chart.cards > 0 ? chart.cards : 10;
        cards = MusicTheory.Deal(instrument, level, count, chart.session * 7919 + (venueId ?? "").Length);

        foreach (Transform child in card) child.gameObject.SetActive(child == game);
        game.gameObject.SetActive(true);
        subtitleText.text = $"{MusicTheory.Lesson(instrument, level)} · เพลง \"{chart.song}\" · {chart.venue_name}";
        liveName.text = state?.instrument_name ?? "";
        perfect = good = miss = combo = 0;
        UpdateScore();
        meterFill.localScale = new Vector3(0f, 1f, 1f);
        judgeText.text = "";

        ActivityPose.Hold("instrument");
        ActivityPose.Do("strum", count * 9f); // play along through the set
        frozenPlayer = FindAnyObjectByType<PlayerController>();
        if (frozenPlayer != null) frozenPlayer.InputEnabled = false; // the letter keys are the keyboard now
        StartLive(frozenPlayer != null ? frozenPlayer.transform : null);

        playing = true;
        StartCoroutine(PlaySet());
    }

    private IEnumerator PlaySet()
    {
        setStart = Time.time;
        for (current = 0; current < cards.Count; current++)
        {
            c = cards[current];
            deckText.text = $"การ์ด {current + 1}/{cards.Count}";
            yield return Flip(BuildFace);
            mistakes = seqAt = 0;
            found.Clear();
            cardDone = cardWon = false;
            float born = Time.time;
            accepting = true;
            while (!cardDone)
            {
                float k = 1f - (Time.time - born) / c.Seconds;
                if (k <= 0f) break;
                fuseFill.localScale = new Vector3(k, 1f, 1f);
                fuseFill.GetComponent<Image>().color = Color.Lerp(new Color(0.95f, 0.25f, 0.12f), UiKit.Gold, k);
                yield return null;
            }
            accepting = false;
            float used = (Time.time - born) / c.Seconds;
            int quality = !cardWon ? 0 : mistakes == 0 && used <= PerfectShare ? 2 : 1;
            Judge(quality);
            ShowAnswer(quality > 0);
            if (cardWon && (c.Kind == MusicTheory.Kind.Chord || c.Kind == MusicTheory.Kind.Lai)) StartCoroutine(Strum(c.Answer));
            float pause = Time.time + (quality == 0 ? 1.8f : 1.2f); // a moment to read the answer
            if (c.Symbols != null) pause = Mathf.Max(pause, Time.time + StartRhythm(c.Symbols) + 0.4f); // hear how long each one is
            while (Time.time < pause) yield return null;
        }
        // the server wants the set to have taken a fair while
        float least = chart.cards > 0 ? cards.Count * 1.5f : chart.duration_ms / 1000f;
        while (Time.time - setStart < least) yield return null;
        EndGame();
    }

    // ── A card face ─────────────────────────────────────────────────────────────
    private void BuildFace()
    {
        UiKit.Clear(face);
        slots.Clear();
        var size = face.sizeDelta;
        float w = size.x, h = size.y;
        UiKit.PillLabel(face, c.Badge, new Vector2(-w * 0.5f + 120f, h * 0.5f - 42f), new Vector2(190f, 46f), UiKit.Teak, UiKit.GoldText, 24);
        UiKit.Label(face, 22, deckText.text, new Vector2(w * 0.5f - 110f, h * 0.5f - 42f), new Vector2(180f, 40f), UiKit.Muted, TextAnchor.MiddleRight);
        var title = UiKit.Label(face, P ? 40 : 46, c.Title, new Vector2(0f, h * 0.5f - (P ? 100f : 92f)), new Vector2(w - 60f, 60f), UiKit.Ink);
        title.fontStyle = FontStyle.Bold;
        askText = UiKit.Label(face, 23, c.Ask, new Vector2(0f, h * 0.5f - (P ? 150f : 138f)), new Vector2(w - 60f, 40f), UiKit.Muted);
        revealText = UiKit.Label(face, 28, "", new Vector2(0f, -h * 0.5f + 40f), new Vector2(w - 60f, 46f), UiKit.Price);
        revealText.fontStyle = FontStyle.Bold;

        // the staff (narrower beside a guitar chord box)
        bool box = !string.IsNullOrEmpty(c.Shape);
        float sp = P ? 17f : 20f;
        float staffW = w - 90f - (box ? (P ? 170f : 210f) : 0f);
        float staffY = P ? -40f : -32f;
        staff = new StaffView(face, new Vector2(box ? -(P ? 80f : 100f) : 0f, staffY), sp, staffW, InkNote, UiKit.Cream);
        bool percussion = c.Kind == MusicTheory.Kind.Drum || c.Kind == MusicTheory.Kind.Groove;
        staff.Draw(percussion ? "drums" : c.Bass ? "bass" : "treble");
        if (box) StaffView.ChordBox(face, new Vector2(w * 0.5f - (P ? 95f : 125f), staffY + 10f), P ? 150f : 180f, c.Shape, c.Title.Replace("คอร์ด ", ""), InkNote);

        float a = staff.NoteLeft + sp, b = staff.NoteRight - sp * 0.5f;
        float mid = (a + b) * 0.5f;
        int bottom = c.Bass ? MusicTheory.BassBottom : MusicTheory.TrebleBottom;
        switch (c.Kind)
        {
            case MusicTheory.Kind.Symbol:
            case MusicTheory.Kind.Beats:
            case MusicTheory.Kind.Count:
                slotX = c.Symbols.Length == 1 ? new[] { mid } : Spread(a + sp, b - sp, c.Symbols.Length);
                for (int i = 0; i < c.Symbols.Length; i++) slots[i] = staff.Value(slotX[i], c.Symbols[i], InkNote);
                break;
            case MusicTheory.Kind.Read:
                var p = c.Notes[0];
                staff.Note(mid, p.Step - bottom, p.Acc, false, InkNote);
                break;
            case MusicTheory.Kind.Scale:
                slotX = Spread(a, b, c.Notes.Count);
                for (int i = 0; i < c.Notes.Count; i++)
                {
                    var n = c.Notes[i];
                    slots[i] = i == c.Hidden ? staff.Missing(slotX[i], UiKit.Price) : staff.Note(slotX[i], n.Step - bottom, n.Acc, true, InkNote);
                }
                break;
            case MusicTheory.Kind.Chord:
            case MusicTheory.Kind.Root:
                slotX = new[] { mid };
                slots[-1] = staff.Missing(mid, UiKit.Price);
                revealText.text = Progress();
                break;
            case MusicTheory.Kind.Lai:
                slotX = Spread(a, b, c.Notes.Count);
                for (int i = 0; i < c.Notes.Count; i++) slots[i] = staff.Missing(slotX[i], UiKit.Price);
                revealText.text = Progress();
                break;
            case MusicTheory.Kind.Drum:
                var d = MusicTheory.Drums[c.Answer[0]];
                staff.Note(mid, d.pos, 0, false, InkNote, d.cross);
                break;
            case MusicTheory.Kind.Groove:
                slotX = Spread(a, b, c.Answer.Length);
                for (int i = 0; i < c.Answer.Length; i++)
                {
                    var g = MusicTheory.Drums[c.Answer[i]];
                    slots[i] = staff.Note(slotX[i], g.pos, 0, false, i == 0 ? UiKit.Price : InkNote, g.cross);
                }
                break;
        }
        BuildControls();
        fuseFill.localScale = Vector3.one;
    }

    // Keys, pads, or (for a rhythm card) four answer buttons
    private void BuildControls()
    {
        bool choose = c.Choices != null, drums = instrument == "drums";
        keysRoot.gameObject.SetActive(!choose && !drums);
        padsRoot.gameObject.SetActive(!choose && drums);
        choicesRoot.gameObject.SetActive(choose);
        wrongChoice = -1;
        UiKit.Clear(choicesRoot);
        if (!choose) return;
        var size = P ? new Vector2(330f, 125f) : new Vector2(420f, 108f);
        for (int i = 0; i < c.Choices.Length; i++)
        {
            var at = new Vector2((i % 2 == 0 ? -0.5f : 0.5f) * (size.x + 16f), (i < 2 ? 0.5f : -0.5f) * (size.y + 14f));
            var b = MakeKey(choicesRoot, at, size, UiKit.Cream, 100 + i);
            var rim = b.gameObject.AddComponent<Outline>();
            rim.effectColor = UiKit.Gold;
            rim.effectDistance = new Vector2(3f, -3f);
            UiKit.Label(b, P ? 30 : 32, c.Choices[i], new Vector2(P ? 0f : 16f, 0f), size - new Vector2(P ? 20f : 70f, 10f), InkNote).fontStyle = FontStyle.Bold;
            if (!P) UiKit.PillLabel(b, "ASDF".Substring(i, 1), new Vector2(-size.x * 0.5f + 34f, 0f), new Vector2(40f, 40f), UiKit.Teak, UiKit.GoldText, 20);
        }
    }

    private static float[] Spread(float a, float b, int n)
    {
        var x = new float[n];
        for (int i = 0; i < n; i++) x[i] = n == 1 ? (a + b) * 0.5f : Mathf.Lerp(a, b, i / (float)(n - 1));
        return x;
    }

    // "G · ? · ?" as the chord is found
    private string Progress()
    {
        var parts = new List<string>();
        if (c.Kind == MusicTheory.Kind.Root) return found.Count > 0 ? c.Notes[0].Name : "?";
        foreach (var n in c.Notes) parts.Add(found.Contains(n.Pc) ? n.Name : "?");
        return string.Join("  ·  ", parts);
    }

    // Draw what was found (green), or the whole answer at the end (green when right, red when not)
    private void Fill(int pc, Color color)
    {
        int bottom = c.Bass ? MusicTheory.BassBottom : MusicTheory.TrebleBottom;
        switch (c.Kind)
        {
            case MusicTheory.Kind.Chord:
            case MusicTheory.Kind.Root:
                if (slots.TryGetValue(-1, out var box) && box != null) { Destroy(box.gameObject); slots.Remove(-1); }
                // the chord stacks up as whole notes; accidentals step left so they don't collide
                for (int i = 0; i < c.Notes.Count; i++)
                {
                    var n = c.Notes[i];
                    if (n.Pc != pc || slots.ContainsKey(i)) continue;
                    slots[i] = staff.Note(slotX[0], n.Step - bottom, n.Acc, true, color);
                    if (n.Acc != 0)
                        foreach (Transform part in slots[i])
                            if (part.name == "Glyph") part.localPosition += new Vector3(-(i % 2) * staff.Space * 1.3f, 0f, 0f);
                }
                break;
            case MusicTheory.Kind.Lai:
                for (int i = 0; i < c.Notes.Count; i++)
                {
                    var n = c.Notes[i];
                    if (n.Pc != pc) continue;
                    if (slots.TryGetValue(i, out var old) && old != null) Destroy(old.gameObject);
                    slots[i] = staff.Note(slotX[i], n.Step - bottom, n.Acc, true, color);
                }
                break;
            case MusicTheory.Kind.Scale:
                if (slots.TryGetValue(c.Hidden, out var gap) && gap != null) Destroy(gap.gameObject);
                var h = c.Notes[c.Hidden];
                slots[c.Hidden] = staff.Note(slotX[c.Hidden], h.Step - bottom, h.Acc, true, color);
                break;
        }
    }

    private void ShowAnswer(bool won)
    {
        revealText.text = c.Reveal;
        revealText.color = won ? FoundGreen : MissRed;
        if (c.Kind == MusicTheory.Kind.Chord || c.Kind == MusicTheory.Kind.Lai || c.Kind == MusicTheory.Kind.Root || c.Kind == MusicTheory.Kind.Scale)
            foreach (int pc in c.Answer)
                if (!found.Contains(pc)) Fill(pc, won ? FoundGreen : MissRed);
        if (c.Kind == MusicTheory.Kind.Read && !won) askText.text = $"คำตอบ: {c.Reveal}";
        if (c.Choices != null && keyImages.TryGetValue(c.Answer[0], out var right) && right != null) right.color = FoundGreen;
        fuseFill.localScale = new Vector3(0f, 1f, 1f);
    }

    // ── Playing a key or a pad ─────────────────────────────────────────────────
    public void Press(int value)
    {
        if (!playing) return;
        bool choice = value >= 100;
        if (choice)
        {
            if (!accepting || cardDone || c.Choices == null || value - 100 >= c.Choices.Length || value == wrongChoice) return;
            bool hit = value == c.Answer[0];
            if (keyImages.TryGetValue(value, out var img) && img != null) img.color = hit ? FoundGreen : MissRed;
            if (hit)
            {
                voice.PlayOneShot(instrument == "drums" ? MusicTones.Drum(1) : MusicTones.Note(instrument, MusicTones.MidiFor(instrument, 0)), GameAudio.SfxVolume);
                Won();
                return;
            }
            wrongChoice = value;
        }
        else
        {
            Flash(value);
            if (instrument == "drums") voice.PlayOneShot(MusicTones.Drum(value), GameAudio.SfxVolume);
            else voice.PlayOneShot(MusicTones.Note(instrument, MusicTones.MidiFor(instrument, value)), GameAudio.SfxVolume);
            if (!accepting || cardDone) return;
        }

        bool right = false; // a wrong answer button falls through to the mistake below
        if (choice) { }
        else if (c.Ordered)
        {
            right = value == c.Answer[seqAt];
            if (right)
            {
                Recolor(slots[seqAt], FoundGreen);
                seqAt++;
                if (seqAt < c.Answer.Length) Recolor(slots[seqAt], UiKit.Price);
                if (seqAt >= c.Answer.Length) Won();
            }
        }
        else
        {
            if (found.Contains(value)) return; // already found that one: no harm
            right = Array.IndexOf(c.Answer, value) >= 0;
            if (right)
            {
                found.Add(value);
                Fill(value, FoundGreen);
                if (c.Kind == MusicTheory.Kind.Chord || c.Kind == MusicTheory.Kind.Lai || c.Kind == MusicTheory.Kind.Root) revealText.text = Progress();
                if (found.Count >= c.Answer.Length) Won();
            }
        }
        if (right) return;
        mistakes++;
        voice.PlayOneShot(MusicTones.Dud(), GameAudio.SfxVolume * 0.6f);
        StartCoroutine(Shake());
        if (mistakes >= 2) cardDone = true; // two wrong notes: this card is lost
    }

    private void Won()
    {
        cardWon = true;
        cardDone = true;
    }

    private static void Recolor(RectTransform note, Color color)
    {
        if (note == null) return;
        foreach (var img in note.GetComponentsInChildren<Image>())
            if (img.name != "Line" && img.name != "Paper") img.color = color;
    }

    private void Judge(int quality)
    {
        if (quality == 2) { perfect++; combo++; }
        else if (quality == 1) { good++; combo++; }
        else { miss++; combo = 0; }
        string text = quality == 2 ? "เป๊ะ!" : quality == 1 ? "ดี" : "พลาด";
        judgeText.text = combo >= 3 && quality > 0 ? $"{text}\nคอมโบ {combo}" : text;
        judgeText.color = quality == 2 ? UiKit.GoldText : quality == 1 ? new Color(0.6f, 0.95f, 0.6f) : new Color(1f, 0.5f, 0.4f);
        StartCoroutine(Pop(judgeRect));
        UpdateScore();
        float crowd = (perfect + good * 0.6f) / Mathf.Max(1, cards.Count);
        meterFill.localScale = new Vector3(Mathf.Clamp01(crowd), 1f, 1f);
    }

    private void UpdateScore()
    {
        scoreText.text = $"เป๊ะ {perfect} · ดี {good} · พลาด {miss}";
    }

    private async void EndGame()
    {
        playing = false;
        accepting = false;
        ActivityPose.Clear();
        StopLive();
        if (frozenPlayer != null) frozenPlayer.InputEnabled = true;
        frozenPlayer = null;

        var music = MusicClient.Instance;
        string result;
        try
        {
            var r = await music.FinishAsync(chart.session, perfect, good);
            result = $"จบเพลง! ความแม่น {r.accuracy}% · ได้ทิป {r.tips} บาท · XP +{r.gained_xp}"
                     + (r.leveled_up ? $"\nเลเวลอัป! นักดนตรี Lv {r.level}" : "");
            if (r.tips > 0) UiKit.FloatAbovePlayer($"ทิป +{r.tips} ฿", new Color(1f, 0.85f, 0.2f));
            if (WalletClient.Instance != null) _ = WalletClient.Instance.RefreshAsync();
        }
        catch (Exception e)
        {
            result = "ส่งผลการแสดงไม่สำเร็จ";
            Debug.LogWarning($"[Music] {e.Message}");
        }
        ShowMenu(result);
    }

    // ── Keyboard shortcuts: the white keys on A S D F G H J, the black on W E T Y U; pads on A S D F G
    private static readonly int[] WhitePc = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] BlackPc = { 1, 3, 6, 8, 10 };

    private void Update()
    {
        if (!playing) return;
        if (c != null && c.Choices != null && accepting)
        {
            for (int i = 0; i < c.Choices.Length && i < 4; i++)
                if (Pressed(WhiteKeys[i])) Press(100 + i);
            return;
        }
        for (int i = 0; i < 7; i++)
            if (Pressed(WhiteKeys[i])) { if (instrument == "drums") { if (i < 5) Press(i); } else Press(WhitePc[i]); }
        if (instrument != "drums")
            for (int i = 0; i < 5; i++)
                if (Pressed(BlackKeys[i])) Press(BlackPc[i]);
    }

#if ENABLE_INPUT_SYSTEM
    private static readonly Key[] WhiteKeys = { Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J };
    private static readonly Key[] BlackKeys = { Key.W, Key.E, Key.T, Key.Y, Key.U };
    private static bool Pressed(Key k) => Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
    private static readonly KeyCode[] WhiteKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J };
    private static readonly KeyCode[] BlackKeys = { KeyCode.W, KeyCode.E, KeyCode.T, KeyCode.Y, KeyCode.U };
    private static bool Pressed(KeyCode k) => Input.GetKeyDown(k);
#else
    private static readonly int[] WhiteKeys = new int[7], BlackKeys = new int[5];
    private static bool Pressed(int k) => false;
#endif

    // ── Building the stage ─────────────────────────────────────────────────────
    private void BuildGame()
    {
        game = UiKit.Container(card, Vector2.zero);
        var size = card.sizeDelta;
        game.sizeDelta = size;
        float w = size.x, h = size.y;
        var stage = UiKit.Panel("Stage", game, StageDark, Vector2.zero, size);
        stage.GetComponent<Image>().raycastTarget = true;
        // spotlights from above
        foreach (float x in new[] { -0.3f, 0.3f })
        {
            var glow = UiKit.Rect("Spot", game, new Color(1f, 0.82f, 0.45f, 0.07f));
            glow.GetComponent<Image>().sprite = UiKit.CircleSprite;
            glow.GetComponent<Image>().raycastTarget = false;
            glow.anchoredPosition = new Vector2(w * x, h * 0.25f);
            glow.sizeDelta = new Vector2(w * 0.55f, h * 0.9f);
        }

        // score, the crowd meter
        scoreText = UiKit.Cartoon(UiKit.Label(game, 28, "", new Vector2(P ? 0f : -w * 0.25f, h * 0.5f - 34f), new Vector2(P ? w - 40f : w * 0.45f, 42f), UiKit.GoldText), 2f);
        var meterBack = UiKit.Panel("Crowd", game, new Color(1f, 1f, 1f, 0.12f), new Vector2(P ? 0f : w * 0.22f, P ? h * 0.5f - 78f : h * 0.5f - 34f), new Vector2(P ? w - 60f : w * 0.48f, 26f));
        meterFill = UiKit.Panel("Fill", meterBack, new Color(1f, 0.55f, 0.25f), Vector2.zero, meterBack.sizeDelta - new Vector2(4f, 4f));
        meterFill.anchorMin = meterFill.anchorMax = meterFill.pivot = new Vector2(0f, 0.5f);
        meterFill.anchoredPosition = new Vector2(2f, 0f);
        UiKit.Cartoon(UiKit.Label(meterBack, 20, "ความมันของคนดู", Vector2.zero, meterBack.sizeDelta, Color.white), 2f);

        // LIVE: the player on stage, filmed as they play
        var liveAt = P ? new Vector2(-w * 0.5f + 115f, h * 0.5f - 210f) : new Vector2(-w * 0.5f + 175f, 120f);
        float liveSize = P ? 170f : 260f;
        var ring = UiKit.Rect("LiveRing", game, UiKit.Gold);
        ring.GetComponent<Image>().sprite = UiKit.CircleSprite;
        ring.anchoredPosition = liveAt;
        ring.sizeDelta = Vector2.one * (liveSize + 10f);
        var lens = UiKit.Rect("Lens", ring, Color.black);
        lens.GetComponent<Image>().sprite = UiKit.CircleSprite;
        lens.sizeDelta = Vector2.one * liveSize;
        lens.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var view = new GameObject("LiveView", typeof(RectTransform), typeof(RawImage));
        view.transform.SetParent(lens, false);
        ((RectTransform)view.transform).sizeDelta = Vector2.one * liveSize;
        liveView = view.GetComponent<RawImage>();
        liveView.raycastTarget = false;
        liveView.enabled = false;
        UiKit.PillLabel(game, "LIVE", liveAt + new Vector2(0f, liveSize * 0.5f + 8f), new Vector2(110f, 36f), new Color(0.85f, 0.15f, 0.12f), Color.white, 20);
        liveName = UiKit.Cartoon(UiKit.Label(game, 24, "", liveAt + new Vector2(0f, -liveSize * 0.5f - 26f), new Vector2(260f, 36f), UiKit.GoldText), 2f);
        deckText = UiKit.Cartoon(UiKit.Label(game, 24, "", liveAt + new Vector2(0f, -liveSize * 0.5f - 62f), new Vector2(260f, 36f), Color.white), 2f);

        // the deck: two cards behind, the face card on top
        var cardSize = P ? new Vector2(w - 40f, 470f) : new Vector2(900f, 420f);
        var cardAt = P ? new Vector2(0f, h * 0.5f - 300f - cardSize.y * 0.5f) : new Vector2(120f, 105f);
        deck = UiKit.Container(game, cardAt);
        for (int i = 2; i >= 1; i--)
        {
            var back = UiKit.Panel("CardBack", deck, Color.Lerp(UiKit.Cream, StageDark, 0.25f * i), new Vector2(i * 9f, -i * 9f), cardSize, shadow: true);
            back.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        }
        face = UiKit.Panel("Card", deck, UiKit.Cream, Vector2.zero, cardSize, shadow: true);
        var outline = face.gameObject.AddComponent<Outline>();
        outline.effectColor = UiKit.Gold;
        outline.effectDistance = new Vector2(3f, -3f);

        // the fuse under it
        var fuseBack = UiKit.Panel("Fuse", game, new Color(1f, 1f, 1f, 0.12f), cardAt + new Vector2(0f, -cardSize.y * 0.5f - 26f), new Vector2(cardSize.x - 40f, 16f));
        fuseFill = UiKit.Panel("Fill", fuseBack, UiKit.Gold, Vector2.zero, fuseBack.sizeDelta - new Vector2(4f, 4f));
        fuseFill.anchorMin = fuseFill.anchorMax = fuseFill.pivot = new Vector2(0f, 0.5f);
        fuseFill.anchoredPosition = new Vector2(2f, 0f);

        judgeText = UiKit.Cartoon(UiKit.Label(game, 64, "", P ? new Vector2(w * 0.5f - 200f, h * 0.5f - 210f) : new Vector2(-w * 0.5f + 175f, -150f), new Vector2(340f, 170f), UiKit.GoldText), 3.5f);
        judgeText.fontStyle = FontStyle.Bold;
        judgeRect = judgeText.rectTransform;

        // the keyboard: one octave, C to B
        float keysY = P ? -h * 0.5f + 330f : -h * 0.5f + 150f;
        float keyW = P ? (w - 40f) / 7f : 124f, keyH = P ? 300f : 250f;
        float keysX = P ? 0f : 120f;
        keysRoot = UiKit.Container(game, new Vector2(keysX, keysY));
        var keyBed = UiKit.Panel("KeyBed", keysRoot, new Color32(0x0C, 0x08, 0x06, 0xFF), new Vector2(0f, 6f), new Vector2(keyW * 7f + 20f, keyH + 30f));
        keyBed.GetComponent<Image>().raycastTarget = false;
        for (int i = 0; i < 7; i++)
        {
            int pc = WhitePc[i];
            var key = MakeKey(keysRoot, new Vector2((i - 3f) * keyW, 0f), new Vector2(keyW - 6f, keyH), Color.white, pc);
            UiKit.Label(key, 34, MusicTheory.Letters[i], new Vector2(0f, -keyH * 0.5f + 70f), new Vector2(keyW - 10f, 44f), InkNote).fontStyle = FontStyle.Bold;
            UiKit.Label(key, 22, MusicTheory.Solfege[i], new Vector2(0f, -keyH * 0.5f + 34f), new Vector2(keyW - 10f, 34f), UiKit.Muted);
            if (!P) UiKit.Label(key, 16, "ASDFGHJ".Substring(i, 1), new Vector2(0f, keyH * 0.5f - 22f), new Vector2(40f, 24f), new Color(0.75f, 0.73f, 0.7f));
        }
        float[] blackAfter = { 0, 1, 3, 4, 5 }; // after C D F G A
        for (int i = 0; i < 5; i++)
        {
            int pc = BlackPc[i];
            var key = MakeKey(keysRoot, new Vector2((blackAfter[i] - 3f + 0.5f) * keyW, keyH * 0.5f - keyH * 0.3f), new Vector2(keyW * 0.62f, keyH * 0.6f), new Color32(0x1C, 0x16, 0x14, 0xFF), pc);
            UiKit.Label(key, 18, MusicTheory.SharpNames[pc], new Vector2(0f, -keyH * 0.3f + 40f), new Vector2(keyW * 0.6f, 26f), new Color(0.85f, 0.85f, 0.85f));
            UiKit.Label(key, 15, MusicTheory.FlatNames[pc], new Vector2(0f, -keyH * 0.3f + 18f), new Vector2(keyW * 0.6f, 22f), new Color(0.6f, 0.6f, 0.6f));
        }

        // the drum pads
        padsRoot = UiKit.Container(game, new Vector2(keysX, keysY + 10f));
        float padSize = P ? 124f : 170f, padGap = P ? 136f : 190f;
        for (int i = 0; i < MusicTheory.Drums.Length; i++)
        {
            var d = MusicTheory.Drums[i];
            var pad = MakeKey(padsRoot, new Vector2((i - 2f) * padGap, 0f), Vector2.one * padSize, PadColors[i], i, circle: true);
            var rim = pad.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(1f, 1f, 1f, 0.5f);
            rim.effectDistance = new Vector2(4f, -4f);
            UiKit.Cartoon(UiKit.Label(pad, P ? 22 : 28, d.name, new Vector2(0f, 14f), new Vector2(padSize - 10f, 40f), Color.white), 2f).fontStyle = FontStyle.Bold;
            UiKit.Cartoon(UiKit.Label(pad, P ? 18 : 22, d.thai, new Vector2(0f, -22f), new Vector2(padSize - 10f, 32f), Color.white), 2f);
            if (!P) UiKit.Label(padsRoot, 18, "ASDFG".Substring(i, 1), new Vector2((i - 2f) * padGap, -padSize * 0.5f - 18f), new Vector2(40f, 26f), new Color(0.75f, 0.73f, 0.7f));
        }
        padsRoot.gameObject.SetActive(false);

        // the answer buttons for the rhythm cards (filled in per card)
        choicesRoot = UiKit.Container(game, new Vector2(keysX, keysY + 10f));
        choicesRoot.gameObject.SetActive(false);

        game.gameObject.SetActive(false);
    }

    // A key or pad that sounds the moment it is touched (not on release)
    private RectTransform MakeKey(Transform parent, Vector2 at, Vector2 size, Color color, int value, bool circle = false)
    {
        var rt = circle ? UiKit.Rect("Pad", parent, color) : UiKit.Panel("Key", parent, color, at, size);
        if (circle)
        {
            rt.GetComponent<Image>().sprite = UiKit.CircleSprite;
            rt.anchoredPosition = at;
            rt.sizeDelta = size;
        }
        var img = rt.GetComponent<Image>();
        keyImages[value] = img;
        keyColors[value] = color;
        var trigger = rt.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
        down.callback.AddListener(_ => Press(value));
        trigger.triggers.Add(down);
        return rt;
    }

    private void Flash(int value)
    {
        if (!keyImages.TryGetValue(value, out var img) || img == null) return;
        StartCoroutine(FlashKey(img, keyColors[value]));
    }

    private static IEnumerator FlashKey(Image img, Color normal)
    {
        float born = Time.time;
        while (Time.time - born < 0.22f && img != null)
        {
            img.color = Color.Lerp(UiKit.Gold, normal, (Time.time - born) / 0.22f);
            yield return null;
        }
        if (img != null) img.color = normal;
    }

    // ── Little animations ──────────────────────────────────────────────────────
    private IEnumerator Flip(Action rebuild)
    {
        for (float t = 0f; t < 0.14f; t += Time.deltaTime) { deck.localScale = new Vector3(1f - t / 0.14f, 1f, 1f); yield return null; }
        rebuild();
        for (float t = 0f; t < 0.14f; t += Time.deltaTime) { deck.localScale = new Vector3(t / 0.14f, 1f, 1f); yield return null; }
        deck.localScale = Vector3.one;
    }

    private IEnumerator Shake()
    {
        var home = face.anchoredPosition;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            face.anchoredPosition = home + new Vector2(Mathf.Sin(t * 70f) * 14f * (1f - t / 0.3f), 0f);
            yield return null;
        }
        face.anchoredPosition = home;
    }

    private static IEnumerator Pop(RectTransform r)
    {
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            r.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t / 0.25f * Mathf.PI));
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    // A rhythm card's signs played at a steady beat: a note sounds for its length, a rest stays silent.
    // Returns how long it takes.
    private float StartRhythm(int[] symbols)
    {
        const float beat = 0.32f;
        float total = 0f;
        foreach (int v in symbols) total += MusicTheory.Values[v].beats * beat;
        StartCoroutine(Rhythm(symbols, beat));
        return total;
    }

    private IEnumerator Rhythm(int[] symbols, float beat)
    {
        for (int i = 0; i < symbols.Length; i++)
        {
            var v = MusicTheory.Values[symbols[i]];
            if (slots.TryGetValue(i, out var sign) && sign != null) StartCoroutine(Pop(sign));
            if (!v.rest)
                voice.PlayOneShot(instrument == "drums" ? MusicTones.Drum(i == 0 ? 0 : 1) : MusicTones.Note(instrument, MusicTones.MidiFor(instrument, 7)), GameAudio.SfxVolume * 0.8f);
            float until = Time.time + v.beats * beat;
            while (Time.time < until) yield return null;
        }
    }

    // A found chord or ลาย rings out, its notes one after another like a strum
    private IEnumerator Strum(int[] pcs)
    {
        if (instrument == "drums") yield break;
        var list = new List<int>(pcs);
        list.Sort();
        foreach (int pc in list)
        {
            voice.PlayOneShot(MusicTones.Note(instrument, MusicTones.MidiFor(instrument, pc)), GameAudio.SfxVolume * 0.8f);
            float until = Time.time + 0.07f;
            while (Time.time < until) yield return null;
        }
    }

    // ── LIVE camera ────────────────────────────────────────────────────────────
    private Transform filming;

    private void StartLive(Transform who)
    {
        StopLive();
        if (who == null) return;
        if (liveTexture == null) liveTexture = new RenderTexture(384, 384, 16) { name = "LivePortrait" };
        var go = new GameObject("LiveCamera");
        liveCam = go.AddComponent<Camera>();
        liveCam.targetTexture = liveTexture;
        liveCam.fieldOfView = 34f;
        liveCam.nearClipPlane = 0.1f;
        liveCam.farClipPlane = 60f;
        liveView.texture = liveTexture;
        liveView.enabled = true;
        filming = who;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (liveCam == null || filming == null) return;
        // from in front and a little to one side, at chest height: the player and the instrument
        var f = filming.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        var side = Vector3.Cross(Vector3.up, f);
        var target = filming.position + Vector3.up * 1.15f;
        liveCam.transform.position = target + f * 2.4f + side * 0.7f + Vector3.up * 0.25f;
        liveCam.transform.LookAt(target);
    }

    private void StopLive()
    {
        if (liveCam != null) Destroy(liveCam.gameObject);
        liveCam = null;
        filming = null;
        if (liveView != null) liveView.enabled = false;
    }
}
