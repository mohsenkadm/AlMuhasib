namespace AlMuhasib.Shared.Helpers;

/// <summary>تطبيع أرقام الهواتف العراقية لروابط واتساب (wa.me).</summary>
public static class IraqiPhoneHelper
{
    /// <summary>أرقام بدون + (مثل 9647765694495) للاستخدام في wa.me.</summary>
    public static bool TryNormalizeForWhatsApp(
        string? raw,
        out string waDigits,
        out string displayE164,
        out string? errorMessage)
    {
        waDigits = string.Empty;
        displayE164 = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            errorMessage = "يرجى إدخال رقم الهاتف.";
            return false;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            errorMessage = "رقم الهاتف غير صالح.";
            return false;
        }

        if (digits.StartsWith("964", StringComparison.Ordinal))
        {
            waDigits = digits;
        }
        else if (digits.StartsWith('0') && digits.Length >= 10)
        {
            waDigits = "964" + digits[1..];
        }
        else if (digits.Length is >= 9 and <= 10)
        {
            waDigits = "964" + digits;
        }
        else
        {
            errorMessage = "صيغة الرقم غير مدعومة. مثال: 07765694495";
            return false;
        }

        if (waDigits.Length < 12 || waDigits.Length > 13)
        {
            errorMessage = "طول الرقم غير صحيح بعد التحويل.";
            return false;
        }

        displayE164 = "+" + waDigits;
        return true;
    }
}
