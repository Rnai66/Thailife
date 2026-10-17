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
    private const float TopY = 330f, HitY = -200f;

    private static MusicUI instance;

    private string venueId;
    private GameObject backdrop;
    private RectTransform card;
    private Text titleText, infoText, messageText;
    private RectTransform instrumentList;
    private Button startButton;

    // Rhythm game
    private RectTransform game;
    private Text songText, scoreText, judgeText;
    private readonly List<(MusicClient.Note note, RectTransform rect, bool done)> notes = new List<(MusicClient.Note, RectTransform, bool)>();
    private MusicClient.Chart chart;
    private float startTime;
    private int perfect, good, miss, combo;
    private bool playing, busy;
    private PlayerController frozenPlayer;

    private static readonly Color[] LaneColors = { new Color(1f, 0.5f, 0.5f), new Color(1f, 0.85f, 0.35f), new Color(0.5f, 0.75f, 1f) };

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
        card = UiKit.Window(transform, "MusicCanvas", new Vector2(1500f, 960f), 55, out backdrop);
        titleText = UiKit.Label(card, 50, "", new Vector2(0f, 420f), new Vector2(1400f, 70f), UiKit.Brown);
        infoText = UiKit.Label(card, 28, "", new Vector2(0f, 330f), new Vector2(1400f, 110f), UiKit.Green);
        UiKit.Label(card, 30, "เลือกเครื่องดนตรี (ซื้อเพิ่มได้ที่ร้านเครื่องดนตรีในเมือง)", new Vector2(0f, 245f), new Vector2(1400f, 44f), UiKit.Brown);
        instrumentList = UiKit.Container(card, new Vector2(0f, 170f));
        startButton = UiKit.Button(card, "เริ่มเล่น (พลังงาน 10)", new Vector2(0f, -120f), new Vector2(520f, 100f), new Color(0.45f, 0.85f, 0.45f), StartPerformance, 40);
        messageText = UiKit.Label(card, 32, "", new Vector2(0f, -260f), new Vector2(1400f, 140f), new Color(0.6f, 0.2f, 0.05f));
        UiKit.Button(card, "ปิด", new Vector2(0f, -420f), new Vector2(280f, 74f), new Color(0.9f, 0.45f, 0.4f), Hide);

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
            startButton.interactable = false;
            return;
        }

        var s = music.Current;
        var v = music.GetVenue(venueId);
        titleText.text = $"เวทีดนตรี · {v?.name ?? venueId}";
        string bonus = v == null ? "" : v.bonus == "folk" ? "เครื่องดนตรีพื้นบ้าน +20%" : v.bonus == "modern" ? "เครื่องดนตรีสากล +20%" : "ทุกเครื่องดนตรี +20%";
        string status = v == null ? "" : !v.unlocked ? $"ต้องเป็นนักดนตรี Lv {v.level} ขึ้นไป"
            : !v.open ? $"ยังไม่เปิด — เปิด{v.when}"
            : v.played_today ? "คืนนี้เล่นที่ร้านนี้แล้ว — แวะไปร้านอื่น หรือมาใหม่พรุ่งนี้"
            : "เปิดอยู่ เล่นได้เลย!";
        infoText.text = $"นักดนตรี Lv {s.level} · XP {s.xp}/{s.xp_to_next} · เล่นมาแล้ว {s.performances} ครั้ง\n"
                        + (v == null ? "" : $"ทิปพื้นฐาน {v.@base} บาท · {bonus} · {v.when}\n{status}");

        int i = 0;
        if (s.instruments != null)
        {
            foreach (var inst in s.instruments)
            {
                var item = inst;
                Color color = item.equipped ? new Color(1f, 0.8f, 0.3f) : item.owned ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.8f, 0.8f, 0.8f, 0.6f);
                string label = item.owned ? $"{item.name}\n{(item.folk ? "พื้นบ้าน" : "สากล")} x{item.mult:0.00}" : $"{item.name}\n(ยังไม่มี)";
                var b = UiKit.Button(instrumentList, label, new Vector2((i % 4 - 1.5f) * 340f, -(i / 4) * 110f), new Vector2(320f, 96f), color, () => Equip(item), 28);
                b.interactable = item.owned && !item.equipped && !busy;
                i++;
            }
        }
        startButton.interactable = !busy && v != null && v.unlocked && v.open && !v.played_today;
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
        foreach (Transform child in card) child.gameObject.SetActive(child == game);
        game.gameObject.SetActive(true);

        foreach (var n in notes) if (n.rect != null) Destroy(n.rect.gameObject);
        notes.Clear();
        foreach (var note in chart.notes)
        {
            var rect = UiKit.Rect("Note", game, LaneColors[Mathf.Clamp(note.lane - 1, 0, 2)]);
            rect.sizeDelta = new Vector2(LaneWidth - 30f, 40f);
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
        judgeText.color = quality == 2 ? new Color(1f, 0.85f, 0.2f) : quality == 1 ? new Color(0.5f, 0.9f, 0.5f) : new Color(1f, 0.4f, 0.4f);
        UpdateScore();
    }

    private void UpdateScore()
    {
        scoreText.text = $"เป๊ะ {perfect} · ดี {good} · พลาด {miss}";
    }

    private async void EndGame()
    {
        playing = false;
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
        songText = UiKit.Label(game, 34, "", new Vector2(0f, 430f), new Vector2(1400f, 50f), UiKit.Brown);
        scoreText = UiKit.Label(game, 30, "", new Vector2(0f, 385f), new Vector2(1400f, 44f), UiKit.Green);
        judgeText = UiKit.Label(game, 48, "", new Vector2(520f, 0f), new Vector2(400f, 80f), UiKit.Brown);

        for (int lane = 1; lane <= 3; lane++)
        {
            var track = UiKit.Rect("Lane", game, new Color(0f, 0f, 0f, 0.08f));
            track.anchoredPosition = new Vector2((lane - 2) * LaneWidth, 65f);
            track.sizeDelta = new Vector2(LaneWidth - 10f, 560f);
        }
        var line = UiKit.Rect("HitLine", game, new Color(0.3f, 0.15f, 0.05f));
        line.anchoredPosition = new Vector2(0f, HitY);
        line.sizeDelta = new Vector2(LaneWidth * 3f, 8f);

        // Big tap buttons under each lane (touch); keyboard A / S / D
        string[] keys = { "A", "S", "D" };
        for (int lane = 1; lane <= 3; lane++)
        {
            int l = lane;
            var b = UiKit.Button(game, keys[lane - 1], new Vector2((lane - 2) * LaneWidth, -330f), new Vector2(LaneWidth - 20f, 150f), LaneColors[lane - 1], () => PressLane(l), 48);
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
