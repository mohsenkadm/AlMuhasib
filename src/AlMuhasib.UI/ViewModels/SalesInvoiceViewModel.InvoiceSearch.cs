using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
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

    partial void OnIsInvoiceSearchOpenChanged(bool value)
    {
        if (!value)
            _invoiceSearchCts?.Cancel();
    }

    [RelayCommand]
    private void CloseInvoiceSearch()
    {
        IsInvoiceSearchOpen = false;
    }

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
                InvoiceType.Sale,
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
        if (invoice is null || invoice.InvoiceType != InvoiceType.Sale)
        {
            BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة");
            return;
        }

        if (invoice.PaymentMethod == PaymentMethod.Credit && invoice.PaidAmount > 0 &&
            !BeautifulMessageDialog.ShowConfirm(
                "هذه الفاتورة الآجلة لديها مبالغ مسدّدة. التعديل قد يؤثر على سجل التسديد. هل تتابع؟"))
            return;

        _editingInvoiceId = invoiceId;
        IsSaved = false;
        _savedInvoice = null;
        _savedItems = [];
        ErrorMessage = string.Empty;

        InvoiceNumber = invoice.InvoiceNumber;
        InvoiceDate = invoice.Date;
        Notes = invoice.Notes ?? string.Empty;

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

        SelectedWarehouse = invoice.WarehouseId > 0
            ? Warehouses.FirstOrDefault(w => w.Id == invoice.WarehouseId)
            : null;

        SelectedPaymentMethod = invoice.PaymentMethod == PaymentMethod.Installment
            ? PaymentMethod.Cash
            : invoice.PaymentMethod;

        if (SelectedPaymentMethod == PaymentMethod.Credit)
            CreditDueDate = invoice.CreditDueDate ?? DateTime.Today.AddMonths(1);
        else
            CreditDueDate = null;

        if (IsCashPayment && invoice.CashBoxId.HasValue)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == invoice.CashBoxId);

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
            WireItemRow(row);
            Items.Add(row);
        }

        if (Items.Count == 0)
            AddRow();

        RecalculateTotals();
        _draftService.ClearDraft(DraftKey);
    }

    private void ClearEditingInvoiceId() => _editingInvoiceId = null;
}
