using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public interface IInvoiceQueueService
{
    IReadOnlyList<InvoiceQueueItem> GetItems(InvoiceQueueKind kind);
    void Enqueue<T>(
        InvoiceQueueKind kind,
        string name,
        T payload,
        int lineCount,
        decimal totalAmount,
        AccountingCurrency currency = AccountingCurrency.IQD) where T : class;
    T? Load<T>(string id) where T : class;
    void Remove(string id);
}
