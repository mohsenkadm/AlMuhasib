namespace AlMuhasib.Core.Intelligence;

/// <summary>مرادفات محاسبية ولهجة عراقية شائعة. القائمة قابلة للتوسعة.</summary>
public static class AccountingLexicon
{
    private static readonly (string From, string To)[] Replacements =
    [
        ("شكد بعنا", "كم مبيعات"),
        ("قديش بعنا", "كم مبيعات"),
        ("شكد", "كم"),
        ("قديش", "كم"),
        ("شنو", "ما"),
        ("هسه", "الان"),
        ("قربت تخلص", "مخزون منخفض"),
        ("قرب تخلص", "مخزون منخفض"),
        ("راح تخلص", "مخزون منخفض"),
        ("ناقصه بالمخزن", "مخزون منخفض"),
        ("الزباين", "العملاء"),
        ("الزبائن", "العملاء"),
        ("زبائن", "عملاء"),
        ("الزبون", "العميل"),
        ("زبون", "عميل"),
        ("البضاعه", "المخزون"),
        ("بضاعه", "مخزون"),
        ("بعنا", "مبيعات"),
        ("صرفنا", "مصروفات"),
        ("القاصه", "الصندوق"),
        ("قاصه", "صندوق"),
        ("الخزينه", "الصندوق"),
        ("ديوننا", "ديون"),
        ("المستحقه", "المستحق"),
        ("مستحقه", "مستحق"),
        ("الارصده", "ارصده"),
        ("ارباح والخسائر", "ارباح")
    ];

    public static string Apply(string normalized)
    {
        var text = normalized;
        foreach (var (from, to) in Replacements)
            text = text.Replace(from, to, StringComparison.Ordinal);

        return ArabicTextNormalizer.Collapse(text);
    }
}
