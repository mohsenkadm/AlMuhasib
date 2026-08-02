using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
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

public partial class GoldVouchersViewModel : PagedViewModelBase
{
    private readonly IGoldCashService _cashService;
    private readonly IGoldCustomerService _customerService;
    private readonly IExportService _exportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldVoucher> Vouchers { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];

    public IReadOnlyList<GoldVoucherTypeFilterOption> TypeFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldVoucherType.Receipt, "قبض"),
        new(GoldVoucherType.Payment, "صرف")
    ];

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    public IReadOnlyList<GoldVoucherTypeOption> VoucherTypes { get; } =
    [
        new(GoldVoucherType.Receipt, "سند قبض"),
        new(GoldVoucherType.Payment, "سند صرف")
    ];

    [ObservableProperty] private GoldVoucherType? _typeFilter;
    [ObservableProperty] private GoldCurrency? _currencyFilter;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private GoldVoucher? _selectedVoucher;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private GoldVoucherType _editType = GoldVoucherType.Receipt;
    [ObservableProperty] private DateTime _editDate = DateTime.Today;
    [ObservableProperty] private GoldCurrency _editCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _editAmount;
    [ObservableProperty] private GoldCashBox? _editCashBox;
    [ObservableProperty] private GoldCustomerListItem? _editCustomer;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _editVoucherNumber = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _message = string.Empty;

    public GoldVouchersViewModel(
        IGoldCashService cashService,
        IGoldCustomerService customerService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _cashService = cashService;
        _customerService = customerService;
        _exportService = exportService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "السندات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Vouchers);
        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync())
            CashBoxes.Add(box);

        Customers.Clear();
        var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        foreach (var c in customers)
            Customers.Add(c);

        await LoadAsync();
    }

    protected override Task OnPageChangedAsync() => LoadAsync();

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    partial void OnTypeFilterChanged(GoldVoucherType? value) => _ = ReloadAsync();
    partial void OnCurrencyFilterChanged(GoldCurrency? value) => _ = ReloadAsync();
    partial void OnDateFromChanged(DateTime? value) => _ = ReloadAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _cashService.GetVouchersPagedAsync(
                    1, int.MaxValue, TypeFilter, CurrencyFilter, DateFrom, DateTo);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Vouchers, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                ApplyPaginationStats(filteredTotal, CurrentPage);
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, total) = await _cashService.GetVouchersPagedAsync(
                CurrentPage, PageSize, TypeFilter, CurrencyFilter, DateFrom, DateTo);
            Vouchers.Clear();
            foreach (var v in items)
                Vouchers.Add(v);
            ApplyPaginationStats(total);
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

    [RelayCommand]
    private async Task OpenCreateDialogAsync()
    {
        if (!CanAdd) return;
        EditType = GoldVoucherType.Receipt;
        EditDate = DateTime.Today;
        EditCurrency = GoldCurrency.IQD;
        EditAmount = 0;
        EditCashBox = CashBoxes.FirstOrDefault(b => b.IsDefault && b.Currency == GoldCurrency.IQD)
            ?? CashBoxes.FirstOrDefault();
        EditCustomer = null;
        EditNotes = string.Empty;
        try
        {
            EditVoucherNumber = await _cashService.GetNextVoucherNumberAsync(EditType);
        }
        catch
        {
            EditVoucherNumber = string.Empty;
        }
        IsDialogOpen = true;
    }

    partial void OnEditTypeChanged(GoldVoucherType value) => _ = RefreshVoucherNumberAsync();

    private async Task RefreshVoucherNumberAsync()
    {
        if (!IsDialogOpen) return;
        try
        {
            EditVoucherNumber = await _cashService.GetNextVoucherNumberAsync(EditType);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (EditAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغ السند");
            return;
        }

        if (EditCashBox is null)
        {
            _toast.ShowWarning("اختر القاصة");
            return;
        }

        try
        {
            var voucher = new GoldVoucher
            {
                VoucherNumber = EditVoucherNumber,
                VoucherDate = EditDate.Date,
                VoucherType = EditType,
                Currency = EditCurrency,
                Amount = EditAmount,
                CashBoxId = EditCashBox.Id,
                CustomerId = EditCustomer?.Id,
                Notes = EditNotes
            };

            var saved = await _cashService.CreateVoucherAsync(voucher);
            Message = $"تم حفظ السند {saved.VoucherNumber}";
            _toast.ShowSuccess(Message);
            BeautifulMessageDialog.ShowSuccess(Message);
            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _cashService.GetVouchersPagedAsync(
                1, int.MaxValue, TypeFilter, CurrencyFilter, DateFrom, DateTo);
            var exportData = allItems.Select(v => new
            {
                رقم_السند = v.VoucherNumber,
                التاريخ = v.VoucherDate.ToString("yyyy/MM/dd"),
                النوع = v.VoucherType.ToString(),
                العملة = v.Currency.ToString(),
                المبلغ = v.Amount,
                ملاحظات = v.Notes
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"سندات_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "السندات");
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
            var (allItems, _) = await _cashService.GetVouchersPagedAsync(
                1, int.MaxValue, TypeFilter, CurrencyFilter, DateFrom, DateTo);
            var columns = new[] { "رقم السند", "التاريخ", "النوع", "العملة", "المبلغ", "ملاحظات" };
            IList<object[]> rows = allItems.Select(v => new object[]
            {
                v.VoucherNumber,
                v.VoucherDate.ToString("yyyy/MM/dd"),
                v.VoucherType.ToString(),
                v.Currency.ToString(),
                v.Amount.ToString("N0"),
                v.Notes
            }).ToList();
            _exportService.PrintTable("قائمة السندات", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
