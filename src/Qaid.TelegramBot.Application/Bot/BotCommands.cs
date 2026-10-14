namespace Qaid.TelegramBot.Application.Bot;

/// <summary>أوامر تيليجرام الظاهرة عند كتابة /</summary>
public static class BotCommands
{
    public static IReadOnlyList<(string Command, string Description)> All { get; } =
    [
        ("start", "تسجيل الدخول / القائمة"),
        ("today", "ملخص اليوم"),
        ("cash", "أرصدة النقد والبنوك"),
        ("sales", "المبيعات اليومية"),
        ("expenses", "المصروفات"),
        ("pnl", "الأرباح والخسائر"),
        ("overdue", "أقساط متأخرة"),
        ("stock", "حد أدنى للمخزون"),
        ("collections", "تحصيلات الزبائن"),
        ("position", "الوضع المالي"),
        ("cogs", "تكلفة البضاعة"),
        ("customer", "كشف زبون: /customer الاسم"),
        ("supplier", "كشف مورد: /supplier الاسم"),
        ("system", "لوحة النظام التخصصي"),
        ("sync", "آخر مزامنة"),
        ("branch", "تغيير الفرع"),
        ("link", "ربط الحساب"),
        ("unlink", "إلغاء الربط"),
        ("help", "المساعدة")
    ];

    public static string HelpText()
        => """
        أوامر بوت قيد:
        /start — القائمة
        /today — ملخص اليوم
        /cash — النقد والبنوك
        /sales — المبيعات
        /expenses — المصروفات
        /pnl — الأرباح والخسائر
        /overdue — أقساط متأخرة
        /stock — مخزون حرج
        /collections — تحصيلات الزبائن
        /position — الوضع المالي
        /cogs — تكلفة البضاعة
        /customer الاسم — كشف زبون
        /supplier الاسم — كشف مورد
        /system — لوحة النظام (سيارات/فندق/عقار/ذهب)
        /sync — المزامنة
        /branch — تغيير الفرع
        /link — ربط الحساب
        /unlink — إلغاء الربط
        """.Trim();
}
