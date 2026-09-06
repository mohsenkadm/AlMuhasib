using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core;

/// <summary>عرض وبحث موحّد لبيانات المورد (الاسم + الهاتف + المعرف).</summary>
public static class SupplierDisplayHelper
{
    public static string FormatDisplayName(Supplier supplier)
    {
        var name = supplier.Name?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"{name} #{supplier.Id}";
    }

    /// <summary>هل النص يطابق الاسم أو الهاتف أو المعرف؟</summary>
    public static bool MatchesSearch(Supplier supplier, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return true;

        var trimmed = term.Trim();
        if (supplier.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
            || (supplier.Phone?.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ?? false))
            return true;

        var idText = supplier.Id.ToString();
        return idText.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
               || idText.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
