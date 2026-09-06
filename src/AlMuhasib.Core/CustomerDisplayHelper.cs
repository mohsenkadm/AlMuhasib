using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core;

/// <summary>عرض وبحث موحّد لبيانات العميل (الاسم + رقم العميل + المعرف).</summary>
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

    /// <summary>هل النص يطابق الاسم أو الهاتف أو رقم العميل أو المعرف؟</summary>
    public static bool MatchesSearch(Customer customer, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return true;

        var trimmed = term.Trim();
        if (customer.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
            || (customer.Phone?.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ?? false)
            || (customer.FileNumber?.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ?? false))
            return true;

        var idText = customer.Id.ToString();
        return idText.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
               || idText.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
