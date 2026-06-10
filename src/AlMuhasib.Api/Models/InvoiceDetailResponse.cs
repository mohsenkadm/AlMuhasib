using AlMuhasib.Core.Enums;

namespace AlMuhasib.Api.Models;

public sealed class InvoiceDetailResponse
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? SupplierSyncId { get; set; }
    public string? SupplierName { get; set; }
    public Guid WarehouseSyncId { get; set; }
    public string? WarehouseName { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingType RoundingType { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public string? CashBoxName { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public string? Notes { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
    public List<InvoiceItemDetail> Items { get; set; } = [];
    public List<InstallmentPlanDetail> InstallmentPlans { get; set; } = [];
}

public sealed class InvoiceItemDetail
{
    public Guid SyncId { get; set; }
    public Guid? ProductSyncId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public sealed class InstallmentPlanDetail
{
    public Guid SyncId { get; set; }
    public Guid CustomerSyncId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public List<InstallmentDetail> Installments { get; set; } = [];
}

public sealed class InstallmentDetail
{
    public Guid SyncId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public Guid? CashBoxSyncId { get; set; }
}
