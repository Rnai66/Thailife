// Preset phrases for greetings (ทักทาย). Only the index travels over the network, so nobody
// can send arbitrary text. Append new phrases at the end — don't reorder.
public static class QuickChat
{
    public static readonly string[] Phrases =
    {
        "สวัสดี!",
        "ขอบคุณนะ",
        "ไปไหนมา?",
        "กินข้าวหรือยัง?",
        "ไปตกปลากัน!",
        "มาซ้อมมวยกัน!",
        "เก่งมาก!",
        "555+",
        "ขอโทษนะ",
        "แล้วเจอกัน!",
    };

    public const int Hello = 0;

    public static string Get(int index) => index >= 0 && index < Phrases.Length ? Phrases[index] : null;
}
