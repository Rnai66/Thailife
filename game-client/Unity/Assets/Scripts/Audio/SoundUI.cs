using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Sound settings (ตั้งค่าเสียง): pick the background music or turn it off, and set how loud the music
// and the effects / place sounds are. Saved on the device. Opened from the "เสียง" button on the HUD.
public class SoundUI : MonoBehaviour
{
    private const int Steps = 10;

    private static SoundUI instance;

    private GameObject backdrop;
    private readonly List<Image> trackButtons = new List<Image>();
    private readonly List<Image> qualityButtons = new List<Image>();
    private readonly List<Image> musicBar = new List<Image>(), sfxBar = new List<Image>();
    private Text musicValue, sfxValue;

    public static void Toggle()
    {
        if (instance != null && instance.backdrop.activeSelf) { instance.backdrop.SetActive(false); return; }
        if (instance == null)
            instance = new GameObject("SoundUI").AddComponent<SoundUI>();
        instance.backdrop.SetActive(true);
        instance.Render();
    }

    private void Close() => backdrop.SetActive(false);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "SoundCanvas", new Vector2(1200f, 980f), 56, "icon:settings",
            out backdrop, out var title, out var subtitle, Close, out _);
        title.text = "ตั้งค่า";
        subtitle.text = "เพลงบรรเลง · ความดังเสียง · คุณภาพภาพ";

        // Music choice: off + each track
        UiKit.Label(body, 30, "เพลงบรรเลง", new Vector2(-380f, 270f), new Vector2(320f, 50f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        var names = new List<string> { "ปิดเพลง" };
        foreach (var t in SynthMusic.Tracks) names.Add(t.Name);
        for (int i = 0; i < names.Count; i++)
        {
            int track = i - 1;
            var b = UiKit.RoundButton(body, names[i], new Vector2(-405f + i * 270f, 190f), new Vector2(250f, 80f), Color.white, UiKit.Ink,
                () => { GameAudio.Track = track; Render(); }, 26);
            b.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
            trackButtons.Add(b.GetComponent<Image>());
        }

        musicValue = Volume(body, "ความดังเพลง", 60f, musicBar, d => GameAudio.MusicVolume += d);
        sfxValue = Volume(body, "เสียงเอฟเฟกต์และเสียงสถานที่", -110f, sfxBar, d => { GameAudio.SfxVolume += d; GameAudio.Play("coins"); });

        // Graphics quality: lower it if the phone gets hot or the game stutters
        UiKit.Label(body, 28, "คุณภาพภาพ (ลดลงถ้าเครื่องร้อนหรือกระตุก)", new Vector2(-230f, -220f), new Vector2(620f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        for (int i = 0; i < GraphicsQuality.Names.Length; i++)
        {
            int level = i;
            var b = UiKit.RoundButton(body, GraphicsQuality.Names[i], new Vector2(-270f + i * 270f, -295f), new Vector2(250f, 76f), Color.white, UiKit.Ink,
                () => { GraphicsQuality.Level = level; Render(); }, 26);
            b.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
            qualityButtons.Add(b.GetComponent<Image>());
        }

        UiKit.RoundButton(body, "บัญชี · รหัสกู้คืน", new Vector2(0f, -375f), new Vector2(380f, 70f), UiKit.Gold, UiKit.GoldInk,
            () => { Close(); AccountUI.Show(); }, 26);
        backdrop.SetActive(false);
    }

    // "- [bar] +" row; returns the percentage label
    private Text Volume(RectTransform body, string label, float y, List<Image> bar, System.Action<float> change)
    {
        UiKit.Label(body, 28, label, new Vector2(-230f, y + 50f), new Vector2(620f, 44f), UiKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        UiKit.RoundButton(body, "-", new Vector2(-460f, y - 20f), new Vector2(90f, 80f), UiKit.Gold, UiKit.GoldInk,
            () => { change(-1f / Steps); Render(); }, 44);
        for (int i = 0; i < Steps; i++)
            bar.Add(UiKit.Panel("Seg", body, UiKit.CardBorder, new Vector2(-355f + i * 64f, y - 20f), new Vector2(54f, 50f)).GetComponent<Image>());
        UiKit.RoundButton(body, "+", new Vector2(320f, y - 20f), new Vector2(90f, 80f), UiKit.Gold, UiKit.GoldInk,
            () => { change(1f / Steps); Render(); }, 44);
        return UiKit.Label(body, 30, "", new Vector2(450f, y - 20f), new Vector2(140f, 50f), UiKit.Ink);
    }

    private void Render()
    {
        int track = GameAudio.Track;
        for (int i = 0; i < trackButtons.Count; i++)
            trackButtons[i].color = i - 1 == track ? UiKit.Gold : Color.white;
        for (int i = 0; i < qualityButtons.Count; i++)
            qualityButtons[i].color = i == GraphicsQuality.Level ? UiKit.Gold : Color.white;
        Fill(musicBar, musicValue, GameAudio.MusicVolume);
        Fill(sfxBar, sfxValue, GameAudio.SfxVolume);
    }

    private static void Fill(List<Image> bar, Text value, float level)
    {
        int lit = Mathf.RoundToInt(level * Steps);
        for (int i = 0; i < bar.Count; i++)
            bar[i].color = i < lit ? UiKit.PayGreen : UiKit.CardBorder;
        value.text = lit == 0 ? "ปิด" : $"{lit * 10}%";
    }
}
