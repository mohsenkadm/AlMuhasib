namespace AlMuhasib.Core.Interfaces.Services;

public interface IPrintService
{
    Task PrintInvoiceAsync(int invoiceId);
    Task PrintVoucherAsync(int voucherId);
    Task PrintInstallmentPlanAsync(int installmentPlanId);
    Task PrintReportAsync(string reportName, object reportData);
}
