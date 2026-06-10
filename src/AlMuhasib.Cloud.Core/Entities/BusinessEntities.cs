using AlMuhasib.Core.Enums;

namespace AlMuhasib.Cloud.Core.Entities;

public class CloudCategory : CloudBaseEntity { public string Name { get; set; } = string.Empty; }
public class CloudProduct : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public CloudCategory Category { get; set; } = null!;
}
public class CloudWarehouse : CloudBaseEntity { public string Name { get; set; } = string.Empty; public string? Location { get; set; } }
public class CloudCustomer : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }
}
public class CloudSupplier : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
public class CloudCashBox : CloudBaseEntity { public string Name { get; set; } = string.Empty; public decimal Balance { get; set; } }
public class CloudBankAccount : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
}
public class CloudInvestor : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ProfitPercentage { get; set; }
    public ICollection<CloudInvestorTransaction> Transactions { get; set; } = [];
}
public class CloudExpenseType : CloudBaseEntity { public string Name { get; set; } = string.Empty; }
public class CloudPrintBrandingSettings : CloudBaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhonePrimary { get; set; } = string.Empty;
    public string PhoneSecondary { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool ShowHeaderText { get; set; } = true;
    public bool ShowHeaderImage { get; set; }
    public byte[]? HeaderImageData { get; set; }
    public string? HeaderImageContentType { get; set; }
    public bool ShowFooterText { get; set; } = true;
    public string FooterText { get; set; } = string.Empty;
    public bool ShowFooterImage { get; set; }
    public byte[]? FooterImageData { get; set; }
    public string? FooterImageContentType { get; set; }
}
public class CloudWarehouseStock : CloudBaseEntity
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public CloudWarehouse Warehouse { get; set; } = null!;
    public CloudProduct Product { get; set; } = null!;
}
public class CloudInvoice : CloudBaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingType RoundingType { get; set; }
    public int? CashBoxId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public string? Notes { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
    public CloudCustomer? Customer { get; set; }
    public CloudSupplier? Supplier { get; set; }
    public CloudWarehouse Warehouse { get; set; } = null!;
    public CloudCashBox? CashBox { get; set; }
    public ICollection<CloudInvoiceItem> Items { get; set; } = [];
    public ICollection<CloudInstallmentPlan> InstallmentPlans { get; set; } = [];
}
public class CloudInvoiceItem : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public CloudInvoice Invoice { get; set; } = null!;
    public CloudProduct? Product { get; set; }
}
public class CloudInstallmentPlan : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public CloudInvoice Invoice { get; set; } = null!;
    public CloudCustomer Customer { get; set; } = null!;
    public ICollection<CloudInstallment> Installments { get; set; } = [];
}
public class CloudInstallment : CloudBaseEntity
{
    public int InstallmentPlanId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int? CashBoxId { get; set; }
    public CloudInstallmentPlan InstallmentPlan { get; set; } = null!;
    public CloudCashBox? CashBox { get; set; }
}
public class CloudVoucher : CloudBaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public int? CustomerId { get; set; }
    public int? InvestorId { get; set; }
    public int CashBoxId { get; set; }
    public int? BankAccountId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public CloudCustomer? Customer { get; set; }
    public CloudInvestor? Investor { get; set; }
    public CloudCashBox CashBox { get; set; } = null!;
    public CloudBankAccount? BankAccount { get; set; }
}
public class CloudExpense : CloudBaseEntity
{
    public int ExpenseTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CashBoxId { get; set; }
    public string? Notes { get; set; }
    public CloudExpenseType ExpenseType { get; set; } = null!;
    public CloudCashBox CashBox { get; set; } = null!;
}
public class CloudTransfer : CloudBaseEntity
{
    public TransferAccountType FromType { get; set; }
    public int FromId { get; set; }
    public TransferAccountType ToType { get; set; }
    public int ToId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
public class CloudInvestorTransaction : CloudBaseEntity
{
    public int InvestorId { get; set; }
    public InvestorTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
public class CloudProfitDistribution : CloudBaseEntity
{
    public DateTime Date { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal DistributedAmount { get; set; }
}
public class CloudProfitDistributionDetail : CloudBaseEntity
{
    public int ProfitDistributionId { get; set; }
    public int InvestorId { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal Amount { get; set; }
    public CloudInvestor Investor { get; set; } = null!;
}
public class CloudCapitalEntry : CloudBaseEntity
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public CapitalEntryType Type { get; set; }
    public string? Notes { get; set; }
}
public class CloudCustomerAttachment : CloudBaseEntity
{
    public int CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? FileData { get; set; }
}
