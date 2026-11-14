namespace AlMuhasib.Core.Helpers;

public enum PriceLabelSymbology
{
    Ean13 = 0,
    Ean8 = 1
}

/// <summary>تحقق وإنشاء أرقام EAN دون تعديل الباركود المخزّن للمنتج.</summary>
public static class EanBarcode
{
    public readonly record struct Result(bool IsValid, string Code, string? Error);

    public static Result Normalize(string? raw, PriceLabelSymbology symbology)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0)
            return Invalid("لا يوجد باركود لهذا المنتج.");

        if (text.Any(ch => !char.IsDigit(ch)))
            return Invalid("الباركود يحتوي على أحرف غير رقمية ولا يطابق معيار EAN.");

        var dataLength = symbology == PriceLabelSymbology.Ean8 ? 7 : 12;
        var fullLength = dataLength + 1;
        var name = symbology == PriceLabelSymbology.Ean8 ? "EAN-8" : "EAN-13";

        if (text.Length == dataLength)
        {
            var check = CheckDigit(text);
            return new Result(true, text + check, null);
        }

        if (text.Length == fullLength)
        {
            var expected = CheckDigit(text[..dataLength]);
            if (text[^1] != expected)
                return Invalid($"خانة التحقق لا تطابق معيار {name}.");
            return new Result(true, text, null);
        }

        return Invalid($"طول الباركود غير مناسب لـ {name}. المطلوب {dataLength} أرقام أو {fullLength} مع خانة التحقق.");
    }

    public static char CheckDigit(string dataDigits)
    {
        var sum = 0;
        var fromRight = dataDigits.Length;
        for (var i = 0; i < dataDigits.Length; i++)
        {
            var weight = fromRight % 2 == 0 ? 1 : 3;
            sum += (dataDigits[i] - '0') * weight;
            fromRight--;
        }

        return (char)('0' + (10 - (sum % 10)) % 10);
    }

    private static Result Invalid(string error) => new(false, string.Empty, error);
}
