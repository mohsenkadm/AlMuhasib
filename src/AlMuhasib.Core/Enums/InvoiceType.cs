namespace AlMuhasib.Core.Enums;

public enum InvoiceType
{
    Purchase,
    Sale,
    Installment,
    PurchaseReturn,
    SaleReturn,
    /// <summary>فاتورة تلف — تنقص كمية المخزن دون عميل.</summary>
    Damage
}

public enum InvoiceHoldStatus
{
    None,
    Held,
    Completed
}
