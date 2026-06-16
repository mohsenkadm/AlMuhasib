namespace AlMuhasib.Core.Utilities;

public static class ArabicAmountToWords
{
    private static readonly string[] Ones =
    [
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر",
        "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    ];

    private static readonly string[] Tens =
    [
        "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
    ];

    private static readonly string[] Hundreds =
    [
        "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
    ];

    public static string Convert(decimal amount, string currencyName = "دينار")
    {
        if (amount == 0)
            return $"صفر {currencyName} فقط لا غير";

        var whole = (long)Math.Truncate(amount);
        var fraction = (int)Math.Round((amount - whole) * 1000, MidpointRounding.AwayFromZero);
        var words = ConvertWhole(whole);
        if (string.IsNullOrWhiteSpace(words))
            words = "صفر";

        var result = $"{words} {currencyName}";
        if (fraction > 0)
            result += $" و {ConvertWhole(fraction)} فلس";

        return $"{result} فقط لا غير";
    }

    private static string ConvertWhole(long number)
    {
        if (number == 0)
            return string.Empty;

        if (number < 0)
            return $"سالب {ConvertWhole(Math.Abs(number))}";

        var parts = new List<string>();
        var billions = number / 1_000_000_000;
        number %= 1_000_000_000;
        if (billions > 0)
            parts.Add($"{ConvertBelowThousand((int)billions)} مليار");

        var millions = number / 1_000_000;
        number %= 1_000_000;
        if (millions > 0)
            parts.Add($"{ConvertBelowThousand((int)millions)} مليون");

        var thousands = number / 1000;
        number %= 1000;
        if (thousands > 0)
            parts.Add(thousands == 1 ? "ألف" : thousands == 2 ? "ألفان" : $"{ConvertBelowThousand((int)thousands)} ألف");

        if (number > 0)
            parts.Add(ConvertBelowThousand((int)number));

        return string.Join(" و ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string ConvertBelowThousand(int number)
    {
        if (number <= 0)
            return string.Empty;

        if (number < 20)
            return Ones[number];

        if (number < 100)
        {
            var ones = number % 10;
            var tens = number / 10;
            return ones == 0 ? Tens[tens] : $"{Ones[ones]} و {Tens[tens]}";
        }

        var hundred = number / 100;
        var remainder = number % 100;
        var hundredText = Hundreds[hundred];
        return remainder == 0 ? hundredText : $"{hundredText} و {ConvertBelowThousand(remainder)}";
    }
}
