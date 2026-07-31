using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel
{
    private int? _editingInvoiceId;
    private DispatcherTimer? _invoiceSearchTimer;
    private CancellationTokenSource? _invoiceSearchCts;

    [ObservableProperty] private bool _isInvoiceSearchOpen;
    [ObservableProperty] private string _invoiceSearchText = string.Empty;
    [ObservableProperty] private bool _invoiceSearchSortNewestFirst = true;
    [ObservableProperty] private bool _isInvoiceSearchLoading;

    public ObservableCollection<InvoiceSearchListItem> InvoiceSearchResults { get; } = [];

    [RelayCommand]
    private async Task OpenInvoiceSearch()
    {
        InvoiceSearchText = string.Empty;
        InvoiceSearchSortNewestFirst = true;
        IsInvoiceSearchOpen = true;
        await RefreshInvoiceSearchAsync();
    }

    [RelayCommand]
    private void CloseInvoiceSearch() => IsInvoiceSearchOpen = false;

    [RelayCommand]
    private async Task SelectInvoiceFromSearch(InvoiceSearchListItem? item)
    {
        if (item is null) return;

        IsInvoiceSearchOpen = false;
        await Task.Yield();

        try
        {
            await LoadInvoiceForEditAsync(item.Id);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الفاتورة:\n{ex.Message}");
        }
    }

    partial void OnInvoiceSearchTextChanged(string value) => ScheduleInvoiceSearchRefresh();

    partial void OnInvoiceSearchSortNewestFirstChanged(bool value) => _ = RefreshInvoiceSearchAsync();

    private void ScheduleInvoiceSearchRefresh()
    {
        _invoiceSearchTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
        _invoiceSearchTimer.Stop();
        _invoiceSearchTimer.Tick -= OnInvoiceSearchTimerTick;
        _invoiceSearchTimer.Tick += OnInvoiceSearchTimerTick;
        _invoiceSearchTimer.Start();
    }

    private void OnInvoiceSearchTimerTick(object? sender, EventArgs e)
    {
        _invoiceSearchTimer?.Stop();
        _ = RefreshInvoiceSearchAsync();
    }

    private async Task RefreshInvoiceSearchAsync()
    {
        if (!IsInvoiceSearchOpen) return;

        _invoiceSearchCts?.Cancel();
        _invoiceSearchCts = new CancellationTokenSource();
        var token = _invoiceSearchCts.Token;

        IsInvoiceSearchLoading = true;
        try
        {
            var results = await _invoiceService.SearchAsync(
                InvoiceType.Installment,
                InvoiceSearchText,
                InvoiceSearchSortNewestFirst,
                limit: 50,
                cancellationToken: token);

            if (token.IsCancellationRequested) return;

            InvoiceSearchResults.Clear();
            foreach (var invoice in results)
            {
                InvoiceSearchResults.Add(new InvoiceSearchListItem
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    PartyName = invoice.Customer?.Name ?? "—",
                    Date = invoice.Date,
                    NetAmount = invoice.NetAmount
                });
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر البحث:\n{ex.Message}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsInvoiceSearchLoading = false;
        }
    }

    public async Task LoadInvoiceForEditAsync(int invoiceId)
    {
        if (HasUnsavedChanges &&
            !BeautifulMessageDialog.ShowConfirm("لديك تغييرات غير محفوظة. هل تريد تجاهلها وتحميل الفاتورة المحددة؟"))
            return;

        var invoice = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        if (invoice is null || invoice.InvoiceType != InvoiceType.Installment)
        {
            BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة");
            return;
        }

        var plan = invoice.InstallmentPlans.FirstOrDefault();
        var hasPaidInstallments = plan?.Installments.Any(i => i.PaidAmount > 0) == true;
        if (hasPaidInstallments &&
            !BeautifulMessageDialog.ShowConfirm(
                "هذه الفاتورة لديها أقساط مسدّدة. التعديل سيعيد إنشاء خطة الأقساط. هل تتابع؟"))
            return;

        _editingInvoiceId = invoiceId;
        IsSaved = false;
        _savedInvoice = null;
        _savedItems = [];
        _savedPlan = null;
        ErrorMessage = string.Empty;

        InvoiceNumber = invoice.InvoiceNumber;
        InvoiceDate = invoice.Date;
        Notes = invoice.Notes ?? string.Empty;
        TransportFeeAmount = ShowTransportFee ? invoice.TransportFeeAmount : 0m;

        if (invoice.CustomerId.HasValue)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            CustomerSearchText = SelectedCustomer?.Name ?? invoice.Customer?.Name ?? string.Empty;
        }
        else
        {
            SelectedCustomer = null;
            CustomerSearchText = string.Empty;
        }

        if (ShowDriverSelection && invoice.DriverId.HasValue)
            SelectedDriver = Drivers.FirstOrDefault(d => d.Id == invoice.DriverId);
        else
            SelectedDriver = null;

        SelectedWarehouse = invoice.WarehouseId > 0
            ? Warehouses.FirstOrDefault(w => w.Id == invoice.WarehouseId)
            : null;

        if (invoice.CashBoxId.HasValue && invoice.CashBoxId > 0)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == invoice.CashBoxId);

        if (plan is not null)
        {
            NumberOfInstallments = plan.NumberOfInstallments;
            InstallmentStartDate = plan.StartDate;
            FileNumber = plan.FileNumber ?? string.Empty;
            SelectedInstallmentType = plan.InstallmentType;
        }

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var item in invoice.Items)
        {
            var row = new InvoiceItemRow
            {
                ProductId = item.ProductId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            InvoiceCustomFieldsHelper.ApplyFromJson(row, item.CustomFieldsJson);
            if (row.UnitConversionFactor > 0 && row.UnitConversionFactor != 1m)
            {
                row.Quantity = item.Quantity / row.UnitConversionFactor;
                row.UnitPrice = item.UnitPrice * row.UnitConversionFactor;
            }
            WireItemRow(row);
            Items.Add(row);
            _ = LoadRowUnitsAsync(row);
        }

        if (Items.Count == 0)
            AddRow();

        RecalculateTotals();
        GenerateSchedulePreview();
        _draftService.ClearDraft(DraftKey);
    }

    private void ClearEditingInvoiceId() => _editingInvoiceId = null;
}
