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
        instance.backdrop.SetActive(true);
        instance.SetPlayerFrozen(true); // typing W/A/S/D or J/K/L must not move or punch
        instance.input.ActivateInputField();
    }

    private void Awake()
    {
        var card = UiKit.Window(transform, "NameCanvas", new Vector2(1000f, 640f), 80, out backdrop);
        titleText = UiKit.Label(card, 46, "", new Vector2(0f, 220f), new Vector2(900f, 130f), UiKit.Brown);

        // Text input (legacy InputField opens the phone keyboard, Thai input included)
        var field = UiKit.Rect("NameInput", card, Color.white);
        field.anchoredPosition = new Vector2(0f, 60f);
        field.sizeDelta = new Vector2(700f, 100f);
        var text = UiKit.Label(field, 44, "", Vector2.zero, new Vector2(660f, 90f), UiKit.Brown, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        var placeholder = UiKit.Label(field, 40, "พิมพ์ชื่อตรงนี้", Vector2.zero, new Vector2(660f, 90f), new Color(0.6f, 0.55f, 0.5f), TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        input = field.gameObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = MaxChars;
        input.lineType = InputField.LineType.SingleLine;

        messageText = UiKit.Label(card, 28, "", new Vector2(0f, -50f), new Vector2(900f, 80f), new Color(0.45f, 0.35f, 0.25f));
        UiKit.Button(card, "ตกลง", new Vector2(-150f, -200f), new Vector2(260f, 90f), new Color(0.45f, 0.85f, 0.45f), Save, 40);
        closeButton = UiKit.Button(card, "ยกเลิก", new Vector2(150f, -200f), new Vector2(260f, 90f), new Color(0.9f, 0.45f, 0.4f), Close, 40);
        backdrop.SetActive(false);
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
        }
        catch (Exception e)
        {
            messageText.text = "บันทึกชื่อไม่สำเร็จ ลองใหม่";
            Debug.LogWarning($"[Profile] {e.Message}");
        }
        busy = false;
    }
}
