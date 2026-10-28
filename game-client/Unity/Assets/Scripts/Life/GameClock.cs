using System;

// Shared game clock: 1.5 real seconds = 1 game minute, one game day = 36 real minutes,
// one game month = 30 game days = 18 real hours. Computed from UTC time, so every player
// (and the server, see life_core.lua) agrees on the time of day and the calendar.
public static class GameClock
{
    public const int MinutesPerDay = 24 * 60;

    public const double RealSecondsPerGameMinute = 1.5;
    public const float RealMinutesPerDay = MinutesPerDay * (float)RealSecondsPerGameMinute / 60f;

    // Day 1 starts here; moved back when days went from 24 to 36 real minutes so the day count
    // carried on (same as life_core.lua EPOCH_MS)
    private static readonly DateTime Epoch = new DateTime(2025, 8, 3, 18, 0, 0, DateTimeKind.Utc);

    private static double TotalGameMinutes => (DateTime.UtcNow - Epoch).TotalSeconds / RealSecondsPerGameMinute;

    public const int DaysPerMonth = 30;

    private static readonly string[] MonthsTh =
        { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };

    private static int Day0 => (int)(TotalGameMinutes / MinutesPerDay);

    public static int Day => Day0 + 1;
    public static int DayOfMonth => Day0 % DaysPerMonth + 1;
    public static string MonthTh => MonthsTh[(Day0 / DaysPerMonth) % 12];

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

    // A real-time wait shown in game time: "80 วัน 5 ชม." / "3 ชม. 20 นาที" / "12 นาที"
    public static string Countdown(float realSeconds)
    {
        int m = (int)(Math.Max(0f, realSeconds) / RealSecondsPerGameMinute);
        int d = m / MinutesPerDay, h = m % MinutesPerDay / 60, min = m % 60;
        return d > 0 ? $"{d} วัน {h} ชม." : h > 0 ? $"{h} ชม. {min} นาที" : $"{Math.Max(1, min)} นาที";
    }

    // Date and time for the HUD (the event on today is shown on the HUD menu button and in the calendar)
    public static string Display => $"{DayOfMonth} {MonthTh} · {Hour:00}:{Minute:00} ({PeriodTh})";
}
