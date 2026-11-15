namespace AlMuhasib.Core.Helpers;

/// <summary>
/// ترميز وحدات EAN-8 وEAN-13. أشرطة الحراسة أطول، والأرقام تُرسم في الفراغ بينها.
/// </summary>
public static class EanSymbol
{
    public readonly record struct Layout(
        string Modules,
        int LeftQuiet,
        int RightQuiet,
        bool OutsideDigit,
        int LeftDigitCount,
        int RightDigitCount,
        int CenterGuardIndex);

    private static readonly string[] LPatterns =
    [
        "0001101", "0011001", "0010011", "0111101", "0100011",
        "0110001", "0101111", "0111011", "0110111", "0001011"
    ];

    private static readonly string[] GPatterns =
    [
        "0100111", "0110011", "0011011", "0100001", "0011101",
        "0111001", "0000101", "0010001", "0001001", "0010111"
    ];

    private static readonly string[] RPatterns =
    [
        "1110010", "1100110", "1101100", "1000010", "1011100",
        "1001110", "1010000", "1000100", "1001000", "1110100"
    ];

    private static readonly string[] Ean13Parity =
    [
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
    ];

    public static Layout Encode(string code, PriceLabelSymbology symbology) =>
        symbology == PriceLabelSymbology.Ean8 ? EncodeEan8(code) : EncodeEan13(code);

    public static bool IsGuard(Layout layout, int moduleIndex)
    {
        if (moduleIndex < 3 || moduleIndex >= layout.Modules.Length - 3)
            return true;

        return moduleIndex >= layout.CenterGuardIndex && moduleIndex < layout.CenterGuardIndex + 5;
    }

    private static Layout EncodeEan8(string code)
    {
        var modules = "101"
                      + Patterns(code[..4], LPatterns)
                      + "01010"
                      + Patterns(code[4..], RPatterns)
                      + "101";
        return new Layout(modules, 7, 7, false, 4, 4, 3 + 28);
    }

    private static Layout EncodeEan13(string code)
    {
        var parity = Ean13Parity[code[0] - '0'];
        var left = new char[6 * 7];
        for (var i = 0; i < 6; i++)
        {
            var pattern = parity[i] == 'G' ? GPatterns[code[i + 1] - '0'] : LPatterns[code[i + 1] - '0'];
            pattern.CopyTo(0, left, i * 7, 7);
        }

        var modules = "101"
                      + new string(left)
                      + "01010"
                      + Patterns(code[7..], RPatterns)
                      + "101";
        return new Layout(modules, 11, 7, true, 6, 6, 3 + 42);
    }

    private static string Patterns(string digits, string[] table)
    {
        var buffer = new char[digits.Length * 7];
        for (var i = 0; i < digits.Length; i++)
            table[digits[i] - '0'].CopyTo(0, buffer, i * 7, 7);
        return new string(buffer);
    }
}
