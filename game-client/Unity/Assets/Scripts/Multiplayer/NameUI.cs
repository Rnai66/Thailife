using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Name window: shown on first login (can't be skipped) and when tapping the name on the HUD.
public class NameUI : MonoBehaviour
{
    private const int MaxChars = 16;

    private static NameUI instance;

    private GameObject backdrop;
    private InputField input;
    private Text titleText, messageText;
    private Button closeButton;
    private bool busy;

    public static void Show(bool firstTime)
    {
        if (instance == null)
            instance = new GameObject("NameUI").AddComponent<NameUI>();
        instance.titleText.text = firstTime ? "ยินดีต้อนรับสู่บ้านคอโค!\nตั้งชื่อตัวละครของคุณ" : "เปลี่ยนชื่อตัวละคร";
        instance.closeButton.gameObject.SetActive(!firstTime);
        instance.input.text = ProfileClient.Instance?.DisplayName ?? "";
        instance.messageText.text = $"ภาษาไทยหรืออังกฤษ 2-{MaxChars} ตัวอักษร · คนอื่นจะเห็นชื่อนี้บนหัว";
        instance.messageText.color = UiKit.Muted;
        instance.HighlightSkin(ProfileClient.Instance?.Skin ?? 0);
        instance.backdrop.SetActive(true);
        instance.SetPlayerFrozen(true); // typing W/A/S/D or J/K/L must not move or punch
        instance.input.ActivateInputField();
    }

    private void Awake()
    {
        var body = UiKit.PremiumWindow(transform, "NameCanvas", new Vector2(1100f, 880f), 80, "ช",
            out backdrop, out var header, out var sub, null, out _);
        header.text = "ชื่อตัวละคร";
        sub.text = "บ้านคอโค หมู่ 1";
        titleText = UiKit.Label(body, 40, "", new Vector2(0f, 270f), new Vector2(980f, 100f), UiKit.Ink);
        titleText.fontStyle = FontStyle.Bold;

        // Text input (legacy InputField opens the phone keyboard, Thai input included)
        BuildSkinPicker(body);

        var field = UiKit.Panel("NameInput", body, Color.white, new Vector2(0f, 0f), new Vector2(760f, 110f), shadow: true);
        field.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        var text = UiKit.Label(field, 44, "", Vector2.zero, new Vector2(700f, 96f), UiKit.Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        var placeholder = UiKit.Label(field, 40, "พิมพ์ชื่อตรงนี้", Vector2.zero, new Vector2(700f, 96f), UiKit.Muted, TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        input = field.gameObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = MaxChars;
        input.lineType = InputField.LineType.SingleLine;

        messageText = UiKit.Label(body, 26, "", new Vector2(0f, -95f), new Vector2(980f, 60f), UiKit.Muted);
        UiKit.RoundButton(body, "ตกลง", new Vector2(-150f, -230f), new Vector2(270f, 90f), UiKit.PayGreen, Color.white, Save, 38);
        closeButton = UiKit.RoundButton(body, "ยกเลิก", new Vector2(150f, -230f), new Vector2(270f, 90f), UiKit.Pill, UiKit.Danger, Close, 38);
        backdrop.SetActive(false);
    }

    // หน้าตา: one button per look made by Setup Character; picking one changes the character
    // right away (you can see it behind the window) and saves it on the server
    private readonly System.Collections.Generic.List<Image> skinButtons = new System.Collections.Generic.List<Image>();

    private void BuildSkinPicker(RectTransform body)
    {
        int count = CharacterModel.SkinCount;
        if (count < 2) return;
        UiKit.Label(body, 26, "เลือกหน้าตา", new Vector2(0f, 200f), new Vector2(600f, 40f), UiKit.Muted);
        float width = Mathf.Min(150f, 900f / count - 12f);
        for (int i = 0; i < count; i++)
        {
            int skin = i;
            var b = UiKit.RoundButton(body, $"แบบ {i + 1}", new Vector2((i - (count - 1) * 0.5f) * (width + 12f), 130f), new Vector2(width, 80f),
                UiKit.Pill, UiKit.Price, () => PickSkin(skin), 28);
            skinButtons.Add(b.GetComponent<Image>());
        }
        HighlightSkin(ProfileClient.Instance?.Skin ?? 0);
    }

    private void HighlightSkin(int skin)
    {
        for (int i = 0; i < skinButtons.Count; i++)
            skinButtons[i].color = i == skin ? UiKit.Gold : UiKit.Pill;
    }

    private async void PickSkin(int skin)
    {
        var profile = ProfileClient.Instance;
        if (profile == null || !profile.IsOnline) return;
        HighlightSkin(skin);
        try { await profile.SetSkinAsync(skin); }
        catch (Exception e)
        {
            messageText.text = "เปลี่ยนหน้าตาไม่สำเร็จ ลองใหม่";
            messageText.color = UiKit.Danger;
            HighlightSkin(profile.Skin);
            Debug.LogWarning($"[Profile] {e.Message}");
        }
    }

    private void OnDestroy()
    {
        SetPlayerFrozen(false);
        if (instance == this) instance = null;
    }

    private void Close()
    {
        backdrop.SetActive(false);
        SetPlayerFrozen(false);
    }

    private void SetPlayerFrozen(bool frozen)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.InputEnabled = !frozen;
    }

    private async void Save()
    {
        var profile = ProfileClient.Instance;
        if (busy || profile == null || !profile.IsOnline) return;
        busy = true;
        try
        {
            string name = await profile.SetNameAsync(input.text);
            Close();
            UiKit.FloatAbovePlayer($"สวัสดี {name}!", new Color(0.6f, 0.9f, 1f));
        }
        catch (ApiResponseException)
        {
            messageText.text = $"ชื่อต้องยาว 2-{MaxChars} ตัวอักษร ลองใหม่อีกครั้ง";
            messageText.color = UiKit.Danger;
        }
        catch (Exception e)
        {
            messageText.text = "บันทึกชื่อไม่สำเร็จ ลองใหม่";
            messageText.color = UiKit.Danger;
            Debug.LogWarning($"[Profile] {e.Message}");
        }
        busy = false;
    }
}
