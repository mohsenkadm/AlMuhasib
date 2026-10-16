namespace AlMuhasib.Core.Enums;

public enum InvoiceType
{
    Purchase,
    Sale,
    Installment,
    PurchaseReturn,
    SaleReturn,
    /// <summary>فاتورة تلف — تنقص كمية المخزن دون عميل.</summary>
    Damage,

    /// <summary>فاتورة عرض سعر — للعميل دون تأثير على المخزن أو الحسابات.</summary>
    Quote
}

public enum InvoiceHoldStatus
{
    None,
    Held,
    Completed
}
