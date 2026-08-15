using System.Windows.Threading;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel
{
    private readonly IInvoiceDraftService _draftService;
    private DispatcherTimer? _draftSaveTimer;
    private const string DraftKey = "purchase-invoice";

    private void ScheduleDraftSave()
    {
        if (IsSaved) return;
        _draftSaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _draftSaveTimer.Stop();
        _draftSaveTimer.Tick -= OnDraftSaveTick;
        _draftSaveTimer.Tick += OnDraftSaveTick;
        _draftSaveTimer.Start();
    }

    private void OnDraftSaveTick(object? sender, EventArgs e)
    {
        _draftSaveTimer?.Stop();
        if (IsSaved || !Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName))) return;
        _draftService.SaveDraft(DraftKey, BuildDraft());
    }

    private PurchaseInvoiceDraft BuildDraft() => new()
    {
        InvoiceDate = InvoiceDate,
        SupplierId = SelectedSupplier?.Id,
        WarehouseId = SelectedWarehouse?.Id,
        IsCashPayment = IsCashPayment,
        CashBoxId = SelectedCashBox?.Id,
        Notes = Notes,
        Lines = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
            .Select(InvoiceDraftLineMapper.ToDraftLine)
            .ToList()
    };

    private void ApplyDraft(PurchaseInvoiceDraft draft)
    {
        InvoiceDate = draft.InvoiceDate;
        IsCashPayment = draft.IsCashPayment;
        Notes = draft.Notes ?? string.Empty;
        if (draft.SupplierId.HasValue)
        {
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == draft.SupplierId);
            if (SelectedSupplier is not null)
                SupplierSearchText = SelectedSupplier.Name;
        }
        if (draft.WarehouseId.HasValue)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == draft.WarehouseId);
        if (draft.CashBoxId.HasValue)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == draft.CashBoxId);

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var line in draft.Lines)
        {
            var row = InvoiceDraftLineMapper.ToRow(line, Products);
            WireItemRow(row);
            Items.Add(row);
            _ = LoadPurchaseRowFeatureDataAsync(row);
        }

        if (!Items.Any())
            AddRow();
        RecalculateTotals();
    }

    private void TryRestoreDraft()
    {
        if (!_draftService.HasDraft(DraftKey)) return;
        var savedAt = _draftService.GetDraftSavedAt(DraftKey);
        var when = savedAt.HasValue ? savedAt.Value.ToString("yyyy/MM/dd HH:mm") : "";
        if (!BeautifulMessageDialog.ShowConfirm(
                $"يوجد مسودة فاتورة مشتريات ({when}).\nهل تريد استعادتها؟"))
            return;

        var draft = _draftService.LoadDraft<PurchaseInvoiceDraft>(DraftKey);
        if (draft is not null)
            ApplyDraft(draft);
    }
}
