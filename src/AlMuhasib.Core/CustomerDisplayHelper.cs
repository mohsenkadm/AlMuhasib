using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core;

/// <summary>عرض وبحث موحّد لبيانات العميل (الاسم + رقم العميل).</summary>
public static class CustomerDisplayHelper
{
    /// <summary>تنسيق العرض: "محسن كاظم 289" أو الاسم فقط إذا لم يوجد رقم.</summary>
    public static string FormatDisplayName(string name, string? fileNumber)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
            return string.Empty;

        var trimmedNumber = fileNumber?.Trim();
        return string.IsNullOrWhiteSpace(trimmedNumber)
            ? trimmedName
            : $"{trimmedName} {trimmedNumber}";
    }

    public static string FormatDisplayName(Customer customer) =>
        FormatDisplayName(customer.Name, customer.FileNumber);

    /// <summary>هل النص يطابق الاسم أو الهاتف أو رقم العميل؟</summary>
    public static bool MatchesSearch(Customer customer, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return true;

        return customer.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
               || (customer.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
               || (customer.FileNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
