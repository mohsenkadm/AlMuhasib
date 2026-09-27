namespace AlMuhasib.Core.Enums;

/// <summary>
/// نطاق عملة موحّد لتقارير المحاسبة.
/// لا يُجمع IQD مع USD في حقل واحد — عند All تُعرض حقول *Usd منفصلة.
/// </summary>
public enum ReportCurrencyScope
{
    /// <summary>دينار فقط (الافتراضي — توافق البيانات القديمة).</summary>
    Iqd = 0,

    /// <summary>دولار فقط.</summary>
    Usd = 1,

    /// <summary>كلا العملتين مع إفصاح منفصل (بدون مجموع مختلط).</summary>
    All = 2
}
