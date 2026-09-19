using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>تحويل نصوص نوع اللوحة (فحص/رسمي) من وإلى التعداد.</summary>
public static class VehiclePlateTypeHelper
{
    public const string InspectionLabel = "فحص";
    public const string OfficialLabel = "رسمي";
    public const string NoneLabel = "بدون";

    public static string ToDisplay(VehiclePlateType type) => type switch
    {
        VehiclePlateType.Inspection => InspectionLabel,
        VehiclePlateType.Official => OfficialLabel,
        _ => NoneLabel
    };

    public static VehiclePlateType Parse(string? value)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(value) || value is "بدون" or "لا" or "none" or "0")
            return VehiclePlateType.None;
        if (value.Contains("فحص", StringComparison.OrdinalIgnoreCase)
            || value.Equals("inspection", StringComparison.OrdinalIgnoreCase)
            || value == "1")
            return VehiclePlateType.Inspection;
        if (value.Contains("رسمي", StringComparison.OrdinalIgnoreCase)
            || value.Equals("official", StringComparison.OrdinalIgnoreCase)
            || value == "2")
            return VehiclePlateType.Official;
        return VehiclePlateType.None;
    }
}
