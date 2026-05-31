using System.Windows.Threading;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel
{
    private readonly IInvoiceDraftService _draftService;
    private DispatcherTimer? _draftSaveTimer;
    private const string DraftKey = "installment-invoice";

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

    private InstallmentInvoiceDraft BuildDraft() => new()
    {
        InvoiceDate = InvoiceDate,
        CustomerId = SelectedCustomer?.Id,
        WarehouseId = SelectedWarehouse?.Id,
        CashBoxId = SelectedCashBox?.Id,
        Notes = Notes,
        FileNumber = FileNumber,
        InstallmentType = SelectedInstallmentType,
        NumberOfInstallments = NumberOfInstallments,
        InstallmentStartDate = InstallmentStartDate,
        Lines = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)).Select(i => new SalesInvoiceDraftLine
        {
            ProductId = i.ProductId ?? 0,
            ProductName = i.ItemName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    private void ApplyDraft(InstallmentInvoiceDraft draft)
    {
        InvoiceDate = draft.InvoiceDate;
        Notes = draft.Notes ?? string.Empty;
        FileNumber = draft.FileNumber ?? string.Empty;
        SelectedInstallmentType = draft.InstallmentType;
        NumberOfInstallments = draft.NumberOfInstallments;
        InstallmentStartDate = draft.InstallmentStartDate;

        if (draft.CustomerId.HasValue)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == draft.CustomerId);
            if (SelectedCustomer is not null)
                CustomerSearchText = SelectedCustomer.Name;
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
            var row = new InvoiceItemRow
            {
                ProductId = line.ProductId > 0 ? line.ProductId : null,
                ItemName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            };
            WireItemRow(row);
            Items.Add(row);
        }

        if (!Items.Any())
            AddRow();
        RecalculateTotals();
        GenerateSchedulePreview();
    }

    private void TryRestoreDraft()
    {
        if (!_draftService.HasDraft(DraftKey)) return;
        var savedAt = _draftService.GetDraftSavedAt(DraftKey);
        var when = savedAt.HasValue ? savedAt.Value.ToString("yyyy/MM/dd HH:mm") : "";
        if (!BeautifulMessageDialog.ShowConfirm(
                $"يوجد مسودة فاتورة أقساط ({when}).\nهل تريد استعادتها؟"))
            return;

        var draft = _draftService.LoadDraft<InstallmentInvoiceDraft>(DraftKey);
        if (draft is not null)
            ApplyDraft(draft);
    }
}
