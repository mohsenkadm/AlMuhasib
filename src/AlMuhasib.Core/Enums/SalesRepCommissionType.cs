namespace AlMuhasib.Core.Enums;

/// <summary>نوع عمولة المندوب</summary>
public enum SalesRepCommissionType
{
    /// <summary>نسبة من إجمالي المبيعات</summary>
    PercentOfSales = 0,

    /// <summary>نسبة من صافي الربح</summary>
    PercentOfNetProfit = 1,

    /// <summary>مبلغ ثابت لكل فاتورة</summary>
    FixedPerInvoice = 2,

    /// <summary>عمولة مختلفة حسب المنتج</summary>
    ByProduct = 3,

    /// <summary>عمولة مختلفة حسب العميل</summary>
    ByCustomer = 4
}
