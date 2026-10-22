namespace AlMuhasib.Core.Entities.Customs;

public class CustomsReceiptAllocation : BaseEntity
{
    public int ReceiptId { get; set; }
    public CustomsReceipt Receipt { get; set; } = null!;

    public int InvoiceId { get; set; }
    public CustomsInvoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
}
