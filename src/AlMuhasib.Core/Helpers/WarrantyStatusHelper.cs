namespace AlMuhasib.Core.Helpers;

/// <summary>حالة الضمان والمدة المتبقية لقالب محلات الموبايلات.</summary>
public static class WarrantyStatusHelper
{
    public const string ActiveLabel = "تحت الضمان";
    public const string ExpiredLabel = "منتهي (Expired)";
    public const string EmptyLabel = "—";

    public static bool IsActive(DateTime? warrantyEndDate, DateTime? asOf = null)
    {
        if (!warrantyEndDate.HasValue) return false;
        var day = (asOf ?? DateTime.Today).Date;
        return warrantyEndDate.Value.Date >= day;
    }

    public static string StatusLabel(DateTime? warrantyEndDate, DateTime? asOf = null)
    {
        if (!warrantyEndDate.HasValue) return EmptyLabel;
        return IsActive(warrantyEndDate, asOf) ? ActiveLabel : ExpiredLabel;
    }

    /// <summary>نص المدة المتبقية أو المنقضية (أيام).</summary>
    public static string RemainingLabel(DateTime? warrantyEndDate, DateTime? asOf = null)
    {
        if (!warrantyEndDate.HasValue) return EmptyLabel;
        var day = (asOf ?? DateTime.Today).Date;
        var end = warrantyEndDate.Value.Date;
        var days = (end - day).Days;
        if (days > 0) return $"{days} يوم متبقي";
        if (days == 0) return "ينتهي اليوم";
        return $"منتهٍ منذ {Math.Abs(days)} يوم";
    }

    public static string FormatEndDate(DateTime? warrantyEndDate) =>
        warrantyEndDate?.ToString("yyyy/MM/dd") ?? EmptyLabel;

    public static string CombinedLabel(DateTime? warrantyEndDate, DateTime? asOf = null)
    {
        if (!warrantyEndDate.HasValue) return EmptyLabel;
        return $"{StatusLabel(warrantyEndDate, asOf)} — {RemainingLabel(warrantyEndDate, asOf)}";
    }
}
