namespace AlMuhasib.Core.Intelligence;

/// <summary>يحدد النية من النص المطبّع وسياق المحادثة، بلا مطابقة كلمة واحدة فقط.</summary>
public static class IntentRecognitionService
{
    public static Understanding Recognize(string? question, ConversationContext context)
    {
        var raw = ArabicTextNormalizer.Normalize(question);
        if (SqlTextGuard.IsHostile(raw))
            return new Understanding { Intent = AccountingIntent.Unsafe, NormalizedText = raw };

        var text = ArabicLanguageProcessor.Prepare(question);
        if (string.IsNullOrWhiteSpace(text))
            return new Understanding { Intent = AccountingIntent.Unknown, NormalizedText = text };

        if (IsHelp(text))
            return Make(AccountingIntent.Help, text, false, false);

        if (IsUnsupported(text))
            return Make(AccountingIntent.Unsupported, text, false, false);

        var comparison = Has(text, "قارن", "مقارنه");
        var topic = DetectTopic(text);
        if (topic != Topic.None)
            return Make(ToIntent(topic, comparison), text, false, comparison);

        if (context.LastIntent is not null && IsPeriodFollowUp(text))
        {
            var follow = FollowIntent(context.LastIntent.Value, comparison);
            return Make(follow, text, true, comparison || IsComparisonIntent(follow));
        }

        if (context.CustomerId is not null && IsCustomerAmountFollowUp(text))
            return Make(AccountingIntent.CustomerPurchases, text, true, false);

        return Make(AccountingIntent.Unknown, text, false, false);
    }

    private static Understanding Make(AccountingIntent intent, string text, bool followUp, bool comparison) =>
        new()
        {
            Intent = intent,
            NormalizedText = text,
            IsFollowUp = followUp,
            IsComparison = comparison
        };

    private enum Topic
    {
        None,
        LowStock,
        Financial,
        CustomerDebt,
        SupplierDebt,
        Collections,
        SupplierPayments,
        TopCustomers,
        TopProducts,
        Cash,
        Expenses,
        Profit,
        Purchases,
        Sales,
        CustomerPurchases
    }

    private static Topic DetectTopic(string text)
    {
        if (Has(text, "مخزون منخفض", "حد اعاده", "حد اعاده الطلب", "نقص المخزون", "تحت الحد"))
            return Topic.LowStock;
        if (Has(text, "ملخص") && Has(text, "مالي", "المالي"))
            return Topic.Financial;
        if (Has(text, "ديون", "مستحق", "ارصده", "ذمم") && Has(text, "مورد"))
            return Topic.SupplierDebt;
        if (Has(text, "ديون", "مستحق", "ارصده", "ذمم") && Has(text, "عميل", "عملاء"))
            return Topic.CustomerDebt;
        if (Has(text, "سند قبض", "سندات القبض", "تحصيلات"))
            return Topic.Collections;
        if (Has(text, "سندات الصرف", "سند صرف") && Has(text, "مورد"))
            return Topic.SupplierPayments;
        if (Has(text, "دفعات المورد", "دفعات الموردين"))
            return Topic.SupplierPayments;
        if ((Has(text, "اكثر", "افضل") && Has(text, "عميل", "عملاء")) || Has(text, "اكثر عميل", "افضل عميل"))
            return Topic.TopCustomers;
        if (Has(text, "اكثر", "افضل") && Has(text, "منتج", "منتجات", "مبيع"))
            return Topic.TopProducts;
        if (Has(text, "الصندوق", "صندوق"))
            return Topic.Cash;
        if (Has(text, "مصروف", "مصروفات", "مصاريف"))
            return Topic.Expenses;
        if (Has(text, "ارباح", "ربح", "خساره", "خسائر"))
            return Topic.Profit;
        if (Has(text, "مشتريات", "اشترينا"))
            return Topic.Purchases;
        if (Has(text, "مبيعات", "مبيع", "بيع"))
            return Topic.Sales;
        if (Has(text, "اشترى", "شرى") && !Has(text, "مورد"))
            return Topic.CustomerPurchases;
        return Topic.None;
    }

    private static AccountingIntent ToIntent(Topic topic, bool comparison) => topic switch
    {
        Topic.LowStock => AccountingIntent.LowStock,
        Topic.Financial => AccountingIntent.FinancialSummary,
        Topic.CustomerDebt => AccountingIntent.CustomerDebt,
        Topic.SupplierDebt => AccountingIntent.SupplierDebt,
        Topic.Collections => AccountingIntent.CustomerCollections,
        Topic.SupplierPayments => AccountingIntent.SupplierPayments,
        Topic.TopCustomers => AccountingIntent.TopCustomers,
        Topic.TopProducts => AccountingIntent.TopProducts,
        Topic.Cash => AccountingIntent.CashBalance,
        Topic.Expenses => comparison ? AccountingIntent.ExpenseComparison : AccountingIntent.ExpensesSummary,
        Topic.Profit => comparison ? AccountingIntent.ProfitComparison : AccountingIntent.ProfitSummary,
        Topic.Purchases => AccountingIntent.PurchasesSummary,
        Topic.Sales => comparison ? AccountingIntent.SalesComparison : AccountingIntent.SalesSummary,
        Topic.CustomerPurchases => AccountingIntent.CustomerPurchases,
        _ => AccountingIntent.Unknown
    };

    private static AccountingIntent FollowIntent(AccountingIntent previous, bool comparison) => previous switch
    {
        AccountingIntent.SalesComparison => AccountingIntent.SalesSummary,
        AccountingIntent.ExpenseComparison => AccountingIntent.ExpensesSummary,
        AccountingIntent.ProfitComparison => AccountingIntent.ProfitSummary,
        AccountingIntent.SalesSummary when comparison => AccountingIntent.SalesComparison,
        AccountingIntent.ExpensesSummary when comparison => AccountingIntent.ExpenseComparison,
        AccountingIntent.ProfitSummary when comparison => AccountingIntent.ProfitComparison,
        _ => previous
    };

    private static bool IsComparisonIntent(AccountingIntent intent) =>
        intent is AccountingIntent.SalesComparison or AccountingIntent.ExpenseComparison or AccountingIntent.ProfitComparison;

    private static bool IsHelp(string text) =>
        Has(text, "ماذا تستطيع", "كيف تساعدني", "شنو تكدر", "المهام المتاحه", "help");

    private static bool IsUnsupported(string text) =>
        Has(text, "ضريبه", "ضريبه", "قيد يومي", "قيود يوميه", "اليوميه المحاسبيه");

    private static bool IsCustomerAmountFollowUp(string text)
    {
        if (Has(text, "اشترى", "شرى", "حسابه", "نفس العميل", "كم المبلغ"))
            return true;

        var stripped = RemoveFillers(RemovePeriodWords(text));
        return stripped is "كم" or "وكم" or "";
    }

    private static bool IsPeriodFollowUp(string text)
    {
        if (!HasPeriod(text))
            return false;

        var stripped = RemoveFillers(RemovePeriodWords(text));
        return stripped.Length == 0;
    }

    private static bool HasPeriod(string text) =>
        Has(text, "اليوم", "امس", "البارحه", "هذا الشهر", "الشهر الحالي", "هالشهر", "الشهر الماضي", "الشهر السابق",
            "هذه السنه", "السنه الحاليه", "هالسنه", "السنه الماضيه", "اخر 7", "اخر 30");

    internal static string RemovePeriodWords(string text)
    {
        string[] words =
        [
            "هذا الشهر حتى اليوم", "الشهر الحالي", "هذا الشهر", "هالشهر", "الشهر الماضي", "الشهر السابق",
            "هذه السنه", "السنه الحاليه", "هالسنه", "السنه الماضيه", "اخر 7 ايام", "اخر 30 يوم", "اخر 7", "اخر 30",
            "هذا اليوم", "واليوم", "اليوم", "والبارحه", "البارحه", "وامس", "والامس", "امس"
        ];
        foreach (var word in words)
            text = text.Replace(word, " ", StringComparison.Ordinal);
        return ArabicTextNormalizer.Collapse(text);
    }

    private static string RemoveFillers(string text)
    {
        string[] fillers = ["وماذا", "ماذا", "عن", "و", "كم", "ما", "هذا", "هذه", "في", "الى", "لي", "اعرض", "اعطني", "اريد", "هو", "هي", "ثم", "من", "عندنا", "لدينا", "فضلا", "لو", "سمحت"];
        var kept = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Length > 1 && token.StartsWith('و') ? token[1..] : token)
            .Where(token => token.Length > 0 && !fillers.Contains(token, StringComparer.Ordinal));
        return string.Join(' ', kept);
    }

    private static bool Has(string text, params string[] words)
    {
        foreach (var word in words)
        {
            if (text.Contains(word, StringComparison.Ordinal))
                return true;
        }

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (token.Length < 5)
                continue;

            foreach (var word in words)
            {
                if (word.Length < 5 || Math.Abs(token.Length - word.Length) > 1)
                    continue;
                if (Levenshtein(token, word) <= 1)
                    return true;
            }
        }

        return false;
    }

    private static int Levenshtein(string left, string right)
    {
        var rows = left.Length + 1;
        var cols = right.Length + 1;
        var matrix = new int[rows, cols];
        for (var i = 0; i < rows; i++)
            matrix[i, 0] = i;
        for (var j = 0; j < cols; j++)
            matrix[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[rows - 1, cols - 1];
    }
}
