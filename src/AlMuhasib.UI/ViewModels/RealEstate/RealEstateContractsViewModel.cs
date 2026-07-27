using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateContractsViewModel : PagedViewModelBase
{
    private readonly IRealEstateContractService _contractService;
    private readonly IRealEstateContractPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private readonly IUserPreferencesService _prefs;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<RealEstateContractListItem> Contracts { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private RealEstateContractStatusFilter _statusFilter = RealEstateContractStatusFilter.All;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private bool _creditOnly;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private RealEstateContractListItem? _selectedContract;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private RealEstateContractListItem? _contractToDelete;
    [ObservableProperty] private string _paymentContractSummary = string.Empty;
    [ObservableProperty] private string _statCountText = "0";
    [ObservableProperty] private string _statTotalText = "0";
    [ObservableProperty] private string _statReceivedText = "0";
    [ObservableProperty] private string _statRemainingText = "0";
    [ObservableProperty] private string _statUnpaidText = "0";

    public IReadOnlyList<EnumDisplayItem<RealEstateContractStatusFilter>> StatusFilters { get; } =
    [
        new(RealEstateContractStatusFilter.All, "الكل"),
        new(RealEstateContractStatusFilter.Active, "نشط"),
        new(RealEstateContractStatusFilter.Completed, "مكتمل"),
        new(RealEstateContractStatusFilter.Cancelled, "ملغى")
    ];

    public record EnumDisplayItem<T>(T Value, string Label) where T : Enum;

    public RealEstateContractsViewModel(
        IRealEstateContractService contractService,
        IRealEstateContractPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService prefs,
        MainWindowViewModel mainWindow)
    {
        _contractService = contractService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        _prefs = prefs;
        _mainWindow = mainWindow;
        PageTitle = "العقود";
        IsCardView = ListViewModeHelper.LoadIsCardView(_prefs, "RealEstateContracts");
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Contracts);
        await LoadContractsAsync();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_prefs, "RealEstateContracts", value);

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
    partial void OnStatusFilterChanged(RealEstateContractStatusFilter value) => _ = ReloadFromFirstPageAsync();
    partial void OnUnpaidOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();
    partial void OnCreditOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();

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
                UpdateStats(filtered);
                return;
            }

            var (items, total) = await _contractService.GetPagedAsync(CurrentPage, PageSize, filter);
            Contracts.Clear();
            foreach (var item in items)
                Contracts.Add(item);
            ApplyPaginationStats(total);

            var allForStats = await _contractService.GetAllForExportAsync(filter);
            UpdateStats(allForStats);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStats(IReadOnlyList<RealEstateContractListItem> rows)
    {
        StatCountText = rows.Count.ToString("N0");
        StatTotalText = rows.Sum(r => r.TotalPrice).ToString("N0");
        StatReceivedText = rows.Sum(r => r.AmountPaid).ToString("N0");
        StatRemainingText = rows.Sum(r => r.RemainingAmount).ToString("N0");
        StatUnpaidText = rows.Count(r => r.RemainingAmount > 0).ToString("N0");
    }

    [RelayCommand]
    private async Task OpenNewContractAsync() =>
        await _mainWindow.OpenTabAsync(typeof(RealEstateContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus, activateIfExists: false);

    [RelayCommand]
    private async Task EditContractAsync(RealEstateContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanEdit) return;
        RealEstateContractNavigationBridge.PendingEditContractId = item.Id;
        await _mainWindow.OpenTabAsync(typeof(RealEstateContractFormViewModel), $"تعديل {item.ContractNumber}", PackIconKind.FileDocumentEdit, activateIfExists: false);
    }

    [RelayCommand]
    private void ConfirmDelete(RealEstateContractListItem? item)
    {
        if (item is null || !CanDelete) return;
        ContractToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteDialogOpen = false;

    [RelayCommand]
    private async Task DeleteConfirmedAsync()
    {
        if (ContractToDelete is null) return;
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
    private void OpenPaymentDialog(RealEstateContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanEdit) return;
        SelectedContract = item;
        PaymentAmount = item.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentContractSummary = $"عقد {item.ContractNumber} — متبقي: {item.RemainingAmount:N0}";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog() => IsPaymentDialogOpen = false;

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (SelectedContract is null) return;
        if (PaymentAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }
        try
        {
            await _contractService.RecordPaymentAsync(SelectedContract.Id, PaymentAmount, PaymentDate, PaymentNotes);
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
    private async Task PrintContractAsync(RealEstateContractListItem? item)
    {
        item ??= SelectedContract;
        if (item is null || !CanPrint) return;
        var contract = await _contractService.GetByIdAsync(item.Id);
        if (contract is not null)
            _printService.PrintContract(contract);
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport) return;
        var rows = await _contractService.GetAllForExportAsync(BuildFilter());
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"RealEstateContracts_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var headers = new[] { "رقم العقد", "التاريخ", "النوع", "العقار", "الموقع", "البائع", "المشتري", "السعر", "المدفوع", "المتبقي", "الدفع", "الحالة" };
        var data = rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"), r.ContractType, r.PropertyType,
            r.PropertyLocation, r.SellerName, r.BuyerName, r.TotalPrice, r.AmountPaid, r.RemainingAmount,
            r.PaymentMode, r.Status
        }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "العقود", headers, data);
        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    [RelayCommand]
    private async Task PrintTableAsync()
    {
        if (!CanPrint) return;
        var rows = await _contractService.GetAllForExportAsync(BuildFilter());
        var headers = new[] { "رقم العقد", "التاريخ", "البائع", "المشتري", "السعر", "المتبقي", "الحالة" };
        var data = rows.Select(r => new object?[]
        {
            r.ContractNumber, r.ContractDate.ToString("yyyy/MM/dd"), r.SellerName, r.BuyerName,
            r.TotalPrice, r.RemainingAmount, r.Status
        }).ToList();
        _exportService.PrintTable("عقود العقارات", headers, data);
    }

    private RealEstateContractFilter BuildFilter() => new()
    {
        SearchText = SearchText,
        DateFrom = DateFrom,
        DateTo = DateTo,
        StatusFilter = StatusFilter,
        UnpaidOnly = UnpaidOnly,
        CreditOnly = CreditOnly
    };
}
