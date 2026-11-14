using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// ตักบาตร: walk up to a monk on the morning round (MonkProcession) and choose which food from your bag
// goes in his bowl. The line waits; he lifts the lid, takes it and blesses you while you ไหว้
// (WaiPose). บารมี as much as one meditation for each monk (temple_alms), each monk once a day.
public class AlmsUI : MonoBehaviour
{
    private static AlmsUI instance;

    private GameObject backdrop;
    private RectTransform card, grid;
    private Text info;
    private MonkProcession line;
    private MonkNpc monk;
    private bool busy;

    public static void Open(MonkProcession procession, MonkNpc toMonk)
    {
        if (instance == null) instance = new GameObject("AlmsUI").AddComponent<AlmsUI>();
        instance.Show(procession, toMonk);
    }

    private void Build()
    {
        card = UiKit.Window(transform, "AlmsCanvas", new Vector2(900f, 600f), 60, out backdrop);
        card.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        UiKit.Label(card, 38, "ใส่บาตร", new Vector2(0f, 250f), new Vector2(860f, 56f), UiKit.Ink).fontStyle = FontStyle.Bold;
        info = UiKit.Label(card, 22, "", new Vector2(0f, 200f), new Vector2(840f, 44f), UiKit.Muted);
        grid = UiKit.Rect("Foods", card, new Color(0f, 0f, 0f, 0f));
        grid.anchoredPosition = new Vector2(0f, -10f);
        grid.sizeDelta = new Vector2(840f, 360f);
        grid.GetComponent<Image>().raycastTarget = false;
        UiKit.RoundButton(card, "ไม่ใช่ตอนนี้", new Vector2(0f, -250f), new Vector2(260f, 60f), new Color(0f, 0f, 0f, 0.08f), UiKit.Muted, Close, 22);
    }

    private void Show(MonkProcession procession, MonkNpc toMonk)
    {
        if (card == null) Build();
        line = procession;
        monk = toMonk;
        if (line != null) line.Waiting = true;
        backdrop.SetActive(true);
        busy = false;
        Fill();
    }

    private void Fill()
    {
        UiKit.Clear(grid);
        var a = TempleClient.Instance?.Current?.alms;
        int barami = a != null && a.barami > 0 ? a.barami : 8;
        info.text = $"เลือกอาหารจากกระเป๋าใส่บาตรพระรูปที่ {monk?.Index} · บารมี +{barami}";
        var foods = LifeClient.Instance?.Current?.foods;
        int n = 0;
        if (foods != null)
            foreach (var f in foods)
            {
                if (f == null || f.count <= 0) continue;
                int col = n % 3, row = n / 3;
                if (row > 3) break;
                var food = f;
                UiKit.RoundButton(grid, $"{f.name} ×{f.count}", new Vector2(-280f + col * 280f, 135f - row * 90f), new Vector2(264f, 78f),
                    UiKit.Gold, UiKit.GoldInk, () => Give(food.id, food.name), 22);
                n++;
            }
        if (n == 0)
            UiKit.Label(grid, 26, "ไม่มีอาหารในกระเป๋า\nซื้อข้าวเหนียวหมูปิ้ง กาแฟ ฯลฯ ได้ที่ร้านค้าชุมชนและตลาด",
                Vector2.zero, new Vector2(800f, 120f), UiKit.Ink);
    }

    private async void Give(string food, string name)
    {
        var temple = TempleClient.Instance;
        if (busy || temple == null || monk == null) return;
        busy = true;
        var who = monk;
        try
        {
            var s = await temple.AlmsAsync(who.Index, food);
            backdrop.SetActive(false);
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                Vector3 to = who.transform.position - player.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(to);
                who.Receive(player.transform.position);
                WaiPose.Play(player.gameObject, 3.6f);
            }
            UiKit.FloatAbovePlayer($"ใส่บาตร{name} สาธุ · บารมี +{s.gained_barami}", new Color(1f, 0.85f, 0.4f));
            GameAudio.Play("bell", null, 0.5f);
            line?.Given();
        }
        catch (ApiResponseException e)
        {
            info.text = Why(e.Message);
        }
        catch (Exception)
        {
            info.text = "เชื่อมต่อไม่ได้ ลองใหม่";
        }
        finally
        {
            busy = false;
            if (!backdrop.activeSelf && line != null) line.Waiting = false;
        }
    }

    private static string Why(string m)
    {
        m ??= "";
        if (m.Contains("already given")) return "ใส่บาตรพระรูปนี้แล้ววันนี้ · ใส่รูปถัดไปได้";
        if (m.Contains("no such food")) return "อาหารนี้หมดแล้ว";
        if (m.Contains("not alms time")) return "พระกลับวัดแล้ว · พรุ่งนี้เช้ามาใหม่";
        if (m.Contains("monks don't give")) return "ขณะบวชอยู่ใส่บาตรไม่ได้";
        return "เชื่อมต่อไม่ได้ ลองใหม่";
    }

    private void Close()
    {
        backdrop.SetActive(false);
        if (line != null) line.Waiting = false;
    }

    private void Update()
    {
        // walked away or the window closed some other way: let the line go on
        if (backdrop != null && !backdrop.activeSelf && line != null && line.Waiting && !busy) line.Waiting = false;
    }
}
