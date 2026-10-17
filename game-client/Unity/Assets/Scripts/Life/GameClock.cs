using System;

// Shared game clock: 1 real second = 1 game minute, one game day = 24 real minutes.
// Computed from UTC time, so every player (and the server) agrees on the time of day.
public static class GameClock
{
    public const int MinutesPerDay = 24 * 60;

    // Day 1 starts here (any fixed UTC instant works)
    private static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static double TotalGameMinutes => (DateTime.UtcNow - Epoch).TotalSeconds;

    public static int Day => 1 + (int)(TotalGameMinutes / MinutesPerDay);

    // 0..1440
    public static float MinuteOfDay => (float)(TotalGameMinutes % MinutesPerDay);

    public static int Hour => (int)(MinuteOfDay / 60f);
    public static int Minute => (int)(MinuteOfDay % 60f);

    public static bool IsNight => Hour < 6 || Hour >= 19;

    public static string PeriodTh =>
        Hour < 6 ? "กลางคืน" :
        Hour < 11 ? "เช้า" :
        Hour < 13 ? "เที่ยง" :
        Hour < 17 ? "บ่าย" :
        Hour < 19 ? "เย็น" : "กลางคืน";

    public static string Display => $"วันที่ {Day} · {Hour:00}:{Minute:00} ({PeriodTh})";
}
