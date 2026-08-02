namespace AlMuhasib.Core.Entities;

/// <summary>إعدادات قواعد نظام الولاء (سجل واحد لكل قاعدة بيانات).</summary>
public class LoyaltySettings : BaseEntity
{
    /// <summary>مبلغ صافي الفاتورة المطلوب لنقطة واحدة (مثال: 1000 د.ع).</summary>
    public decimal PointsPerAmount { get; set; } = 1000m;

    /// <summary>قيمة النقطة بالدينار عند الاستبدال.</summary>
    public decimal PointValueInCurrency { get; set; } = 100m;

    /// <summary>حد أدنى لصافي الفاتورة لكسب النقاط.</summary>
    public decimal MinInvoiceAmountToEarn { get; set; }

    /// <summary>أقل نقاط مسموح استبدالها دفعة واحدة.</summary>
    public int MinPointsToRedeem { get; set; } = 1;

    /// <summary>سقف نسبة الاستبدال من أساس الفاتورة (0–100).</summary>
    public decimal MaxRedeemPercentOfInvoice { get; set; } = 50m;

    /// <summary>انتهاء النقاط بعد أيام — null بلا انتهاء.</summary>
    public int? PointsExpireAfterDays { get; set; }

    /// <summary>هل تُكسب نقاط من البيع الآجل.</summary>
    public bool EarnOnCreditSales { get; set; } = true;

    /// <summary>تقريب الكسب للأسفل.</summary>
    public bool RoundEarnDown { get; set; } = true;
}
