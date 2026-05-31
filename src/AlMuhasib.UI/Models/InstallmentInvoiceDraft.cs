using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Models;

public class InstallmentInvoiceDraft
{
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public int? CustomerId { get; set; }
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
    public string? FileNumber { get; set; }
    public InstallmentType InstallmentType { get; set; } = InstallmentType.Manual;
    public int NumberOfInstallments { get; set; } = 6;
    public DateTime InstallmentStartDate { get; set; } = DateTime.Now.AddMonths(1);
    public List<SalesInvoiceDraftLine> Lines { get; set; } = [];
}
