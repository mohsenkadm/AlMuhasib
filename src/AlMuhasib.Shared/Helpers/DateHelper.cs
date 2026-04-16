namespace AlMuhasib.Shared.Helpers;

public static class DateHelper
{
    public static string ToHijriString(this DateTime date)
    {
        var hijriCalendar = new System.Globalization.HijriCalendar();
        int year = hijriCalendar.GetYear(date);
        int month = hijriCalendar.GetMonth(date);
        int day = hijriCalendar.GetDayOfMonth(date);
        return $"{year}/{month:D2}/{day:D2}";
    }
}
