using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public interface IInvoiceTemplateService
{
    IReadOnlyList<InvoiceTemplate> GetTemplates(InvoiceTemplateKind kind);
    void SaveTemplate(InvoiceTemplate template);
    void DeleteTemplate(string templateId);
    void SetDefaultSalesCustomer(int? customerId);
    void SetDefaultPurchaseSupplier(int? supplierId);
    void SetDefaultInstallmentCustomer(int? customerId);
    int? GetDefaultSalesCustomerId();
    int? GetDefaultPurchaseSupplierId();
    int? GetDefaultInstallmentCustomerId();
}
