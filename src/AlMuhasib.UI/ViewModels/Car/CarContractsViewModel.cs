using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace AlMuhasib.UI.ViewModels.Car;

public partial class CarContractsViewModel : PagedViewModelBase
{
    private readonly ICarContractService _contractService;
    private readonly ICarContractPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<CarContractListItem> Contracts { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CarContractStatusFilter _statusFilter = CarContractStatusFilter.All;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private CarContractListItem? _selectedContract;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private CarContractListItem? _contractToDelete;
    [ObservableProperty] private bool _isDetailDialogOpen;
    [ObservableProperty] private CarContractDetailDisplay? _detailContract;
    [ObservableProperty] private string _paymentContractSummary = string.Empty;

    public CarContractsViewModel(
        ICarContractService contractService,
        ICarContractPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _contractService = contractService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "العقود";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarPermissionRegistry.CarContracts);
        await LoadContractsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350) { AutoReset = false };
        _debounceTimer.Elapsed += async (_, _) =>
        {
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadContractsAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnDateFromChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnStatusFilterChanged(CarContractStatusFilter value) => _ = ReloadFromFirstPageAsync();
    partial void OnUnpaidOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadContractsAsync();
    }

    protected override Task OnPageChangedAsync() => LoadContractsAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadContractsAsync();
    }

    [RelayCommand]
    private async Task LoadContractsAsync()
    {
        IsBusy = true;
        try
        {
            var filter = BuildFilter();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _contractService.GetPagedAsync(1, int.MaxValue, filter);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Contracts, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, total) = await _contractService.GetPagedAsync(CurrentPage, PageSize, filter);
            Contracts.Clear();
            foreach (var item in items)
                Contracts.Add(item);
            ApplyPaginationStats(total);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenNewContractAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus, activateIfExists: false);

    [RelayCommand]
    private async Task EditContractAsync(CarContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanEdit)
            return;

        CarContractNavigationBridge.PendingEditContractId = item.Id;
        await _mainWindow.OpenTabAsync(typeof(CarContractFormViewModel), $"تعديل {item.ContractNumber}", PackIconKind.FileDocumentEdit, activateIfExists: false);
    }

    [RelayCommand]
    private async Task ViewContractDetailsAsync(CarContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null)
            return;

        var contract = await _contractService.GetByIdAsync(item.Id);
        if (contract is null)
        {
            _toast.ShowError("العقد غير موجود");
            return;
        }

        SelectedContract = item;
        DetailContract = CarContractDetailDisplay.FromEntity(contract);
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDetailDialog()
    {
        IsDetailDialogOpen = false;
        DetailContract = null;
    }

    [RelayCommand]
    private async Task DetailPrintAsync()
    {
        if (DetailContract is null)
            return;

        await PrintContractAsync(Contracts.FirstOrDefault(c => c.Id == DetailContract.Id) ?? SelectedContract);
    }

    [RelayCommand]
    private async Task DetailEditAsync()
    {
        if (SelectedContract is null)
            return;

        IsDetailDialogOpen = false;
        await EditContractAsync(SelectedContract);
    }

    [RelayCommand]
    private void DetailDeleteAsync()
    {
        if (SelectedContract is null)
            return;

        IsDetailDialogOpen = false;
        ConfirmDelete(SelectedContract);
    }

    [RelayCommand]
    private void ConfirmDelete(CarContractListItem? item)
    {
        if (item is null || !CanDelete)
            return;

        ContractToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteDialogOpen = false;

    [RelayCommand]
    private async Task DeleteConfirmedAsync()
    {
        if (ContractToDelete is null)
            return;

        try
        {
            await _contractService.DeleteAsync(ContractToDelete.Id, _currentUserService.Username ?? "System");
            IsDeleteDialogOpen = false;
            ContractToDelete = null;
            _toast.ShowSuccess("تم حذف العقد");
            await LoadContractsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenPaymentDialog(CarContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanEdit)
            return;

        SelectedContract = item;
        PaymentAmount = item.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentContractSummary = $"عقد {item.ContractNumber} — {item.BuyerName} (متبقي: {item.RemainingAmount:N0})";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog() => IsPaymentDialogOpen = false;

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (SelectedContract is null)
            return;

        if (PaymentAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }

        try
        {
            await _contractService.RecordPaymentAsync(
                SelectedContract.Id, PaymentAmount, PaymentDate, PaymentNotes);
            IsPaymentDialogOpen = false;
            _toast.ShowSuccess("تم تسجيل التسديد بنجاح");
            await LoadContractsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintContractAsync(CarContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanPrint)
            return;

        var contract = await _contractService.GetByIdAsync(item.Id);
        if (contract is not null)
            _printService.PrintContract(contract);
    }

    [RelayCommand]
    private async Task PrintAllAsync()
    {
        if (!CanPrint)
            return;

        var rows = await _contractService.GetAllForExportAsync(BuildFilter());
        foreach (var row in rows)
        {
            var contract = await _contractService.GetByIdAsync(row.Id);
            if (contract is not null)
                _printService.PrintContract(contract, 1);
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport)
            return;

        var rows = await _contractService.GetAllForExportAsync(BuildFilter());
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarContracts_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var headers = new[] { "رقم العقد", "التاريخ", "البائع", "المشتري", "اللوحة", "السعر", "الواصل", "المتبقي", "الحالة" };
        var data = rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"), r.SellerName, r.BuyerName,
            r.PlateNumber, r.CarPrice, r.AmountReceived, r.RemainingAmount, r.Status
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "العقود", headers, data);
        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    private CarContractFilter BuildFilter() => new()
    {
        SearchText = SearchText,
        DateFrom = DateFrom,
        DateTo = DateTo,
        StatusFilter = StatusFilter,
        UnpaidOnly = UnpaidOnly
    };
}
