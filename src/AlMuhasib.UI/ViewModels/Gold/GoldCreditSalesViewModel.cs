using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCreditSalesViewModel : PagedViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IExportService _exportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private List<GoldInvoiceListItem> _allLoaded = [];

    public ObservableCollection<GoldInvoiceListItem> Invoices { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "مفتوح + جزئي"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "مدفوع جزئياً")
    ];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private GoldInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private int _openCount;

    public GoldCreditSalesViewModel(
        IGoldSaleService saleService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _exportService = exportService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "مبيعات الآجل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CreditSales);
        await LoadAsync();
    }

    protected override Task OnPageChangedAsync() => LoadAsync();

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        ApplyDisplay();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (StatusFilter is null)
            {
                var (openItems, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.Open);
                var (partialItems, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.PartiallyPaid);
                _allLoaded = openItems.Concat(partialItems)
                    .Where(i => i.PaymentMethod == GoldPaymentMethod.Credit || i.RemainingAmount > 0)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.Id)
                    .ToList();
            }
            else
            {
                var (items, _) = await _saleService.GetPagedAsync(1, int.MaxValue, SearchText, status: StatusFilter);
                _allLoaded = items.Where(i => i.PaymentMethod == GoldPaymentMethod.Credit || i.RemainingAmount > 0).ToList();
            }

            TotalRemaining = _allLoaded.Sum(i => i.RemainingAmount);
            OpenCount = _allLoaded.Count;
            ApplyDisplay();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyDisplay()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allLoaded, ColumnFilters)
            : _allLoaded.ToList();

        ApplyPaginationStats(filtered.Count);
        var pageItems = filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        Invoices.Clear();
        foreach (var item in pageItems)
            Invoices.Add(item);
    }

    partial void OnStatusFilterChanged(GoldInvoiceStatus? value) => _ = SearchAsync();

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            await EnsureLoadedForExportAsync();
            var exportData = _allLoaded.Select(i => new
            {
                رقم_الفاتورة = i.InvoiceNumber,
                التاريخ = i.InvoiceDate.ToString("yyyy/MM/dd"),
                الزبون = i.CustomerName ?? "",
                الحالة = i.Status.ToString(),
                الإجمالي = i.TotalAmount,
                المدفوع = i.PaidAmount,
                المتبقي = i.RemainingAmount
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"مبيعات_الآجل_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الآجل");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            await EnsureLoadedForExportAsync();
            var columns = new[] { "رقم الفاتورة", "التاريخ", "الزبون", "الحالة", "الإجمالي", "المدفوع", "المتبقي" };
            IList<object[]> rows = _allLoaded.Select(i => new object[]
            {
                i.InvoiceNumber,
                i.InvoiceDate.ToString("yyyy/MM/dd"),
                i.CustomerName ?? "",
                i.Status.ToString(),
                i.TotalAmount.ToString("N0"),
                i.PaidAmount.ToString("N0"),
                i.RemainingAmount.ToString("N0")
            }).ToList();
            _exportService.PrintTable("فواتير الآجل المفتوحة", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    private async Task EnsureLoadedForExportAsync()
    {
        if (_allLoaded.Count == 0)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenInvoiceDetail(GoldInvoiceListItem? invoice)
    {
        if (invoice is null)
            return;

        try
        {
            var full = await _saleService.GetByIdAsync(invoice.Id);
            if (full is null)
            {
                _toast.ShowError("لم يتم العثور على الفاتورة.");
                return;
            }

            GoldInvoiceDetailDialog.Show(full);
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
