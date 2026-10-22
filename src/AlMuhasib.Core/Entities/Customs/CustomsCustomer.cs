using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsCustomer : BaseEntity
{
    public CustomsCustomerType CustomerType { get; set; } = CustomsCustomerType.Individual;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }

    public ICollection<CustomsInvoice> Invoices { get; set; } = [];
    public ICollection<CustomsReceipt> Receipts { get; set; } = [];
}
