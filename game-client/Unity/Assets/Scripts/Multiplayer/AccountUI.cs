using System;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

// Account (บัญชี): your recovery code to write down, and recovering an account from another phone
// with its code (account.lua + NakamaConnection.RecoverAsync). Opened from เมนู → ตั้งค่า.
public class AccountUI : MonoBehaviour
{
    [Serializable] private class CodeResult { public string code; }

    private static AccountUI instance;

    private GameObject backdrop;
    private Text codeText, messageText;
    private Button newCodeButton;
    private Text newCodeLabel;
    private InputField input;
    private bool busy, confirmRecover;
    private Text recoverLabel, deleteLabel;
    private int deleteTaps;

    public static void Show()
    {
        if (instance == null)
            instance = new GameObject("AccountUI").AddComponent<AccountUI>();
        instance.backdrop.SetActive(true);
        instance.confirmRecover = false;
        instance.recoverLabel.text = "กู้คืนบัญชี";
        instance.messageText.text = "";
        instance.deleteTaps = 0;
        instance.deleteLabel.text = "ลบบัญชี";
        instance.LoadCode();
    }

    private void Close() => backdrop.SetActive(false);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Awake()
    {
        bool P = UiKit.Portrait; // upright: one column, the recover button under the code field
        float w = P ? 680f : 1100f;
        var body = UiKit.PremiumWindow(transform, "AccountCanvas", UiKit.WindowSize(new Vector2(1200f, 900f), 1150f), 60, "บ",
            out backdrop, out var title, out var subtitle, Close, out _);
        title.text = "บัญชีผู้เล่น";
        subtitle.text = "เปลี่ยนมือถือหรือลบแอป ก็ยังเล่นต่อได้ด้วยรหัสกู้คืน";

        UiKit.Label(body, 28, "รหัสกู้คืนบัญชีของคุณ (จดเก็บไว้ อย่าบอกใคร)", new Vector2(0f, P ? 440f : 290f), new Vector2(w, P ? 76f : 44f), UiKit.Ink).fontStyle = FontStyle.Bold;
        var codeBox = UiKit.Panel("Code", body, new Color32(0xFF, 0xF6, 0xDF, 0xFF), new Vector2(0f, P ? 355f : 210f), new Vector2(P ? 680f : 760f, 100f), shadow: true);
        codeBox.gameObject.AddComponent<Outline>().effectColor = UiKit.Gold;
        codeText = UiKit.Label(codeBox, 48, "", Vector2.zero, new Vector2(P ? 660f : 740f, 90f), UiKit.Ink);
        codeText.fontStyle = FontStyle.Bold;
        newCodeButton = UiKit.RoundButton(body, "สร้างรหัสใหม่", new Vector2(0f, P ? 255f : 105f), new Vector2(340f, 70f), UiKit.Gold, UiKit.GoldInk, NewCode, 26);
        newCodeLabel = newCodeButton.GetComponentInChildren<Text>();

        UiKit.Label(body, 28, "ย้ายมาจากเครื่องเก่า: ใส่รหัสกู้คืนของบัญชีเดิม", new Vector2(0f, P ? 160f : -10f), new Vector2(w, P ? 76f : 44f), UiKit.Ink).fontStyle = FontStyle.Bold;
        var field = UiKit.Panel("CodeInput", body, Color.white, P ? new Vector2(0f, 70f) : new Vector2(-150f, -95f), new Vector2(620f, 90f), shadow: true);
        field.gameObject.AddComponent<Outline>().effectColor = UiKit.CardBorder;
        var text = UiKit.Label(field, 36, "", Vector2.zero, new Vector2(580f, 80f), UiKit.Ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        var placeholder = UiKit.Label(field, 32, "XXXX-XXXX-XXXX-XXXX", Vector2.zero, new Vector2(580f, 80f), UiKit.Muted, TextAnchor.MiddleLeft);
        input = field.gameObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 24;
        input.lineType = InputField.LineType.SingleLine;
        var recover = UiKit.RoundButton(body, "กู้คืนบัญชี", P ? new Vector2(0f, -40f) : new Vector2(330f, -95f), new Vector2(P ? 360f : 280f, P ? 80f : 90f), UiKit.PayGreen, Color.white, Recover, 28);
        recoverLabel = recover.GetComponentInChildren<Text>();
        UiKit.Label(body, 22, "การกู้คืนจะเปลี่ยนเครื่องนี้ไปใช้บัญชีเดิม (ความคืบหน้าที่เล่นในเครื่องนี้จะไม่ถูกใช้อีก)",
            new Vector2(0f, P ? -130f : -175f), new Vector2(w, P ? 70f : 40f), UiKit.Muted);
        messageText = UiKit.Label(body, 26, "", new Vector2(0f, P ? -215f : -245f), new Vector2(w, 60f), UiKit.Muted);
        var delete = UiKit.RoundButton(body, "ลบบัญชี", new Vector2(0f, P ? -310f : -320f), new Vector2(300f, 64f), UiKit.DangerTint, UiKit.Danger, DeleteAccount, 24);
        deleteLabel = delete.GetComponentInChildren<Text>();
        backdrop.SetActive(false);
    }

    private static NakamaConnection Connection()
    {
        var conn = FindAnyObjectByType<NakamaConnection>();
        return conn != null && conn.IsConnected ? conn : null;
    }

    private async void LoadCode()
    {
        var conn = Connection();
        if (conn == null) { codeText.text = "ต้องออนไลน์ก่อน"; return; }
        try
        {
            var r = await conn.Client.RpcAsync(conn.Session, "account_recovery_get", "{}");
            string code = JsonUtility.FromJson<CodeResult>(r.Payload).code;
            codeText.text = string.IsNullOrEmpty(code) ? "ยังไม่มีรหัส" : code;
            newCodeLabel.text = string.IsNullOrEmpty(code) ? "สร้างรหัสกู้คืน" : "สร้างรหัสใหม่";
        }
        catch (Exception) { codeText.text = "โหลดรหัสไม่ได้"; }
    }

    private async void NewCode()
    {
        var conn = Connection();
        if (busy || conn == null) return;
        busy = true;
        try
        {
            var r = await conn.Client.RpcAsync(conn.Session, "account_recovery_new", "{}");
            codeText.text = JsonUtility.FromJson<CodeResult>(r.Payload).code;
            newCodeLabel.text = "สร้างรหัสใหม่";
            messageText.text = "จดรหัสนี้เก็บไว้ · รหัสเก่า (ถ้ามี) ใช้ไม่ได้แล้ว";
            messageText.color = UiKit.PayGreen;
        }
        catch (Exception) { messageText.text = "สร้างรหัสไม่สำเร็จ ลองใหม่"; messageText.color = UiKit.Danger; }
        busy = false;
    }

    // Three taps: ลบบัญชี → แน่ใจไหม? → ยืนยันลบถาวร
    private async void DeleteAccount()
    {
        var conn = Connection();
        if (busy || conn == null) return;
        deleteTaps++;
        if (deleteTaps == 1)
        {
            deleteLabel.text = "แน่ใจไหม?";
            messageText.text = "การลบบัญชีจะลบเงิน ของ เลเวล และความคืบหน้าทั้งหมดอย่างถาวร กู้คืนไม่ได้";
            messageText.color = UiKit.Danger;
            return;
        }
        if (deleteTaps == 2) { deleteLabel.text = "ยืนยันลบถาวร"; return; }
        busy = true;
        messageText.text = "กำลังลบบัญชี…";
        try { await conn.DeleteAccountAsync(); }
        catch (Exception)
        {
            messageText.text = "ลบบัญชีไม่สำเร็จ ลองใหม่";
            deleteTaps = 0;
            deleteLabel.text = "ลบบัญชี";
        }
        busy = false;
    }

    private async void Recover()
    {
        var conn = Connection();
        string code = input.text.Trim();
        if (busy || conn == null) return;
        if (code.Replace("-", "").Length != 16) { messageText.text = "รหัสต้องมี 16 ตัว เช่น K7QM-2XPA-9RTD-WH3F"; messageText.color = UiKit.Danger; return; }
        if (!confirmRecover) { confirmRecover = true; recoverLabel.text = "ยืนยันกู้คืน?"; return; }
        busy = true;
        messageText.text = "กำลังกู้คืน…";
        messageText.color = UiKit.Muted;
        try
        {
            await conn.RecoverAsync(code);
        }
        catch (ApiResponseException e)
        {
            messageText.text = e.StatusCode == 404 || e.Message.Contains("not found") ? "ไม่พบบัญชีของรหัสนี้ — ตรวจรหัสอีกครั้ง" : "กู้คืนไม่สำเร็จ";
            messageText.color = UiKit.Danger;
        }
        catch (Exception)
        {
            messageText.text = "กู้คืนไม่สำเร็จ ลองใหม่";
            messageText.color = UiKit.Danger;
        }
        busy = false;
        confirmRecover = false;
        recoverLabel.text = "กู้คืนบัญชี";
    }
}
