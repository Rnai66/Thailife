using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Stage window for a venue: pick an instrument, then play a 3-lane rhythm game.
// Notes fall toward the hit line; tap the lane buttons (or A / S / D) when they arrive.
// The server wrote the chart and pays the tips when the song ends (music.lua).
public class MusicUI : MonoBehaviour
{
    private const float TravelTime = 1.6f;    // seconds a note takes to fall to the line
    private const float PerfectWindow = 0.08f;
    private const float GoodWindow = 0.16f;
    private const float LaneWidth = 220f;
    private const float TopY = 290f, HitY = -180f;

    private static MusicUI instance;

    private string venueId;
    private GameObject backdrop;
    private RectTransform card;
    private Text titleText, subtitleText, infoText, statusText, messageText;
    private Image statusPill;
    private RectTransform instrumentList;
    private Button startButton;

    // Rhythm game
    private RectTransform game;
    private Text songText, scoreText, judgeText;
    private readonly List<(MusicClient.Note note, RectTransform rect, bool done)> notes = new List<(MusicClient.Note, RectTransform, bool)>();
    private MusicClient.Chart chart;
    private float startTime;
    private int perfect, good, miss, combo;
    private bool playing, busy, P; // P: upright layout
    private PlayerController frozenPlayer;

    private static readonly Color[] LaneColors = { new Color32(0xE8, 0x7A, 0x5D, 0xFF), UiKit.Gold, new Color32(0x6F, 0xA8, 0xDC, 0xFF) };

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
        songText = subtitleText;

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

        BuildGame();
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (frozenPlayer != null) frozenPlayer.InputEnabled = true;
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
                        + (v == null ? "" : $"ทิปพื้นฐาน {v.@base} บาท · {bonus} · {v.when}");

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
            BeginGame();
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

    private void BeginGame()
    {
        ActivityPose.Hold("instrument");
        ActivityPose.Do("strum", chart.duration_ms / 1000f + 1f); // play along while the notes run
        foreach (Transform child in card) child.gameObject.SetActive(child == game);
        game.gameObject.SetActive(true);

        foreach (var n in notes) if (n.rect != null) Destroy(n.rect.gameObject);
        notes.Clear();
        foreach (var note in chart.notes)
        {
            var rect = UiKit.Rect("Note", game, LaneColors[Mathf.Clamp(note.lane - 1, 0, 2)]);
            rect.sizeDelta = new Vector2(LaneWidth - 30f, 40f);
            rect.GetComponent<Image>().sprite = UiKit.RoundedSprite;
            rect.GetComponent<Image>().type = Image.Type.Sliced;
            rect.gameObject.SetActive(false);
            notes.Add((note, rect, false));
        }

        perfect = good = miss = combo = 0;
        songText.text = $"เพลง \"{chart.song}\" · {chart.venue_name} · {chart.bpm} BPM";
        judgeText.text = "เตรียมตัว...";
        UpdateScore();

        // Freeze walking (A/S/D are the lane keys)
        frozenPlayer = FindAnyObjectByType<PlayerController>();
        if (frozenPlayer != null) frozenPlayer.InputEnabled = false;

        startTime = Time.time;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        float now = Time.time - startTime;

        // Move notes and count the ones that slipped past
        for (int i = 0; i < notes.Count; i++)
        {
            var (note, rect, done) = notes[i];
            if (done) continue;
            float dt = note.t / 1000f - now;   // seconds until it should be hit
            if (dt < -GoodWindow)
            {
                Judge(i, "พลาด", 0);
                continue;
            }
            if (dt > TravelTime) continue;
            rect.gameObject.SetActive(true);
            rect.anchoredPosition = new Vector2((note.lane - 2) * LaneWidth, Mathf.Lerp(HitY, TopY, dt / TravelTime));
        }

        if (Pressed(0)) Hit(1, now);
        if (Pressed(1)) Hit(2, now);
        if (Pressed(2)) Hit(3, now);

        if (now * 1000f >= chart.duration_ms + 300) EndGame();
    }

    public void PressLane(int lane)
    {
        if (playing) Hit(lane, Time.time - startTime);
    }

    private void Hit(int lane, float now)
    {
        int best = -1;
        float bestDt = float.MaxValue;
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].done || notes[i].note.lane != lane) continue;
            float dt = Mathf.Abs(notes[i].note.t / 1000f - now);
            if (dt < bestDt) { bestDt = dt; best = i; }
        }
        if (best < 0 || bestDt > GoodWindow) return; // nothing close enough: ignore the tap
        if (bestDt <= PerfectWindow) Judge(best, "เป๊ะ!", 2);
        else Judge(best, "ดี", 1);
    }

    private void Judge(int index, string text, int quality)
    {
        var (note, rect, _) = notes[index];
        notes[index] = (note, rect, true);
        rect.gameObject.SetActive(false);
        if (quality == 2) { perfect++; combo++; }
        else if (quality == 1) { good++; combo++; }
        else { miss++; combo = 0; }
        judgeText.text = combo >= 5 ? $"{text}  คอมโบ {combo}" : text;
        judgeText.color = quality == 2 ? UiKit.Price : quality == 1 ? UiKit.PayGreen : UiKit.Danger;
        UpdateScore();
    }

    private void UpdateScore()
    {
        scoreText.text = $"เป๊ะ {perfect} · ดี {good} · พลาด {miss}";
    }

    private async void EndGame()
    {
        playing = false;
        ActivityPose.Clear();
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

    private static bool Pressed(int lane)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        return lane == 0 ? kb.aKey.wasPressedThisFrame : lane == 1 ? kb.sKey.wasPressedThisFrame : kb.dKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(lane == 0 ? KeyCode.A : lane == 1 ? KeyCode.S : KeyCode.D);
#else
        return false;
#endif
    }

    private void BuildGame()
    {
        game = UiKit.Container(card, Vector2.zero);
        game.sizeDelta = card.sizeDelta;
        scoreText = UiKit.Label(game, 28, "", new Vector2(0f, 370f), new Vector2(P ? 680f : 1300f, 44f), UiKit.Price);
        scoreText.fontStyle = FontStyle.Bold;
        judgeText = UiKit.Label(game, 48, "", P ? new Vector2(0f, 450f) : new Vector2(520f, 0f), new Vector2(400f, 80f), UiKit.Ink); // upright: above the lanes
        judgeText.fontStyle = FontStyle.Bold;

        for (int lane = 1; lane <= 3; lane++)
        {
            var track = UiKit.Panel("Lane", game, Color.white, new Vector2((lane - 2) * LaneWidth, 55f), new Vector2(LaneWidth - 10f, 540f));
            track.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        }
        var line = UiKit.Rect("HitLine", game, UiKit.Teak);
        line.anchoredPosition = new Vector2(0f, HitY);
        line.sizeDelta = new Vector2(LaneWidth * 3f, 8f);

        // Big tap buttons under each lane (touch); keyboard A / S / D
        string[] keys = { "A", "S", "D" };
        for (int lane = 1; lane <= 3; lane++)
        {
            int l = lane;
            var b = UiKit.RoundButton(game, keys[lane - 1], new Vector2((lane - 2) * LaneWidth, -310f), new Vector2(LaneWidth - 20f, 150f), LaneColors[lane - 1], UiKit.Ink, () => PressLane(l), 48);
            // Fire on press, not release, so timing feels right
            var trigger = b.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            down.callback.AddListener(_ => PressLane(l));
            trigger.triggers.Add(down);
            b.onClick.RemoveAllListeners();
        }
        game.gameObject.SetActive(false);
    }
}
