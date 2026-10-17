using System;
using System.Text;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Chicken beauty contest (ประกวดไก่สวย) at the monthly temple fair (งานวัด). Your most beautiful
// adult chicken competes with four villagers' chickens; the server scores it. Once per fair.
public class ContestUI : MonoBehaviour
{
    private static ContestUI instance;

    private GameObject backdrop;
    private Text infoText;
    private Text resultText;
    private Button enterButton;
    private bool busy;
    private float nextRender;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("ContestUI").AddComponent<ContestUI>();
        instance.backdrop.SetActive(true);
        instance.resultText.text = "จัดในงานวัดเดือนละครั้ง (วันที่ 1-2 ของเดือน)\nรางวัล: ที่ 1 = 1,000 บาท · ที่ 2 = 500 บาท · ที่ 3 = 250 บาท\nผสมพันธุ์ไก่ให้ลูกสวยกว่าพ่อแม่ แล้วส่งตัวที่สวยที่สุดมาประกวด";
        CoopClient.Instance?.Refresh();
        instance.Render();
    }

    public static void Hide()
    {
        if (instance != null) instance.backdrop.SetActive(false);
    }

    private void Awake()
    {
        var card = UiKit.Window(transform, "ContestCanvas", new Vector2(1100f, 900f), 50, out backdrop);
        UiKit.Label(card, 54, "ประกวดไก่สวย · งานวัดคอโค", new Vector2(0f, 400f), new Vector2(1000f, 70f), UiKit.Brown);
        infoText = UiKit.Label(card, 32, "", new Vector2(0f, 320f), new Vector2(1000f, 90f), UiKit.Green);
        enterButton = UiKit.Button(card, "ส่งไก่เข้าประกวด (พลังงาน 5)", new Vector2(0f, 220f), new Vector2(560f, 90f), new Color(1f, 0.8f, 0.3f), Enter);
        resultText = UiKit.Label(card, 32, "", new Vector2(0f, -80f), new Vector2(1000f, 460f), UiKit.Brown);
        UiKit.Button(card, "ปิด", new Vector2(0f, -390f), new Vector2(300f, 74f), new Color(0.9f, 0.45f, 0.4f), Hide);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (backdrop.activeSelf && Time.time >= nextRender) Render();
    }

    private void Render()
    {
        nextRender = Time.time + 1f;
        var coop = CoopClient.Instance;
        if (coop == null || !coop.IsOnline)
        {
            infoText.text = "ต้องออนไลน์ก่อนถึงจะประกวดได้";
            enterButton.interactable = false;
            return;
        }

        CoopClient.Chicken best = null;
        if (coop.Current.chickens != null)
            foreach (var c in coop.Current.chickens)
                if (c.adult && (best == null || c.beauty > best.beauty)) best = c;

        var s = coop.Current;
        string fair = s.fair_open
            ? (s.entered_this_fair ? "งานวัดนี้ส่งประกวดไปแล้ว — รอเดือนหน้า" : $"งานวัดเปิดอยู่! เหลือเวลาอีก {CoopClient.Countdown(s.fair_ends_in_s - coop.Age)}")
            : $"ยังไม่ถึงงานวัด — งานครั้งถัดไปในอีก {CoopClient.Countdown(s.next_fair_in_s - coop.Age)}";
        infoText.text = (best == null ? "ยังไม่มีไก่โตเต็มวัย — เลี้ยงไก่ที่เล้าข้างบ้านก่อน"
                                      : $"ตัวแทนของคุณ: {best.name} สี{best.color} ความสวย {best.beauty}") + $"\n{fair}";
        enterButton.interactable = !busy && best != null && s.fair_open && !s.entered_this_fair;
    }

    private async void Enter()
    {
        var coop = CoopClient.Instance;
        if (busy || coop == null) return;
        busy = true;
        try
        {
            var r = await coop.EnterContestAsync();
            var text = new StringBuilder();
            for (int i = 0; i < r.contest.Length; i++)
            {
                var e = r.contest[i];
                text.AppendLine(e.mine ? $"ที่ {i + 1}  {e.name}  {e.score} คะแนน  (ของคุณ)" : $"ที่ {i + 1}  {e.name}  {e.score} คะแนน");
            }
            text.AppendLine();
            text.Append(r.prize > 0 ? $"ยินดีด้วย! ได้ที่ {r.place} รับรางวัล {r.prize:N0} บาท" : $"ได้ที่ {r.place} — ผสมพันธุ์ให้ไก่สวยขึ้นแล้วมาใหม่งานวัดเดือนหน้า");
            resultText.text = text.ToString();
            if (r.prize > 0) UiKit.FloatAbovePlayer($"ที่ {r.place}! +{r.prize} ฿", new Color(1f, 0.85f, 0.2f));
        }
        catch (ApiResponseException e)
        {
            resultText.text = e.Message.Contains("energy") ? "พลังงานไม่พอ — ไปนอนพักที่ชานบ้านก่อน"
                : e.Message.Contains("closed") ? "งานวัดยังไม่เปิด"
                : e.Message.Contains("already entered") ? "งานวัดนี้ส่งประกวดไปแล้ว"
                : e.Message.Contains("no adult") ? "ยังไม่มีไก่โตเต็มวัย" : "ส่งประกวดไม่สำเร็จ";
        }
        catch (Exception e)
        {
            resultText.text = "เกิดข้อผิดพลาด ลองใหม่";
            Debug.LogWarning($"[Contest] {e.Message}");
        }
        busy = false;
        Render();
    }
}
