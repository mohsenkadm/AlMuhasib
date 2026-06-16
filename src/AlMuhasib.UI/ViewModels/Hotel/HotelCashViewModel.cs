using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelCashViewModel : PagedViewModelBase
{
    private readonly IHotelCashService _cashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<HotelCashBox> CashBoxes { get; } = [];
    public ObservableCollection<HotelVoucher> Vouchers { get; } = [];

    [ObservableProperty] private HotelCashBox? _selectedCashBox;
    [ObservableProperty] private HotelVoucher? _selectedVoucher;
    [ObservableProperty] private HotelVoucherType? _voucherTypeFilter;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private bool _isCashBoxDialogOpen;
    [ObservableProperty] private bool _isVoucherDialogOpen;
    [ObservableProperty] private bool _isCashBoxEditMode;
    [ObservableProperty] private string _editCashBoxName = string.Empty;
    [ObservableProperty] private decimal _editOpeningBalance;
    [ObservableProperty] private bool _editIsBank;
    [ObservableProperty] private HotelVoucherType _editVoucherType = HotelVoucherType.Receipt;
    [ObservableProperty] private DateTime _editVoucherDate = DateTime.Today;
    [ObservableProperty] private decimal _editVoucherAmount;
    [ObservableProperty] private int? _editVoucherCashBoxId;
    [ObservableProperty] private string _editVoucherDescription = string.Empty;
    [ObservableProperty] private string _editVoucherNotes = string.Empty;

    private int? _editingCashBoxId;

    public IReadOnlyList<HotelVoucherType> VoucherTypeOptions { get; } =
        Enum.GetValues<HotelVoucherType>().ToList();

    public HotelCashViewModel(
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _cashService = cashService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "الصندوق";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.HotelCash);
        await LoadCashBoxesAsync();
        await LoadVouchersAsync();
    }

    partial void OnVoucherTypeFilterChanged(HotelVoucherType? value) => _ = ReloadVouchersAsync();
    partial void OnDateFromChanged(DateTime? value) => _ = ReloadVouchersAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadVouchersAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadVouchersAsync();

    protected override Task OnPageChangedAsync() => LoadVouchersAsync();

    private async Task ReloadVouchersAsync()
    {
        CurrentPage = 1;
        await LoadVouchersAsync();
    }

    [RelayCommand]
    private async Task LoadCashBoxesAsync()
    {
        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync(activeOnly: false))
            CashBoxes.Add(box);
    }

    [RelayCommand]
    private async Task LoadVouchersAsync()
    {
        IsBusy = true;
        try
        {
            var filter = new HotelVoucherFilter
            {
                Type = VoucherTypeFilter,
                DateFrom = DateFrom,
                DateTo = DateTo,
                CashBoxId = SelectedCashBox?.Id
            };

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _cashService.GetVouchersPagedAsync(1, int.MaxValue, filter);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                Vouchers.Clear();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Vouchers, CurrentPage, PageSize,
                    out var filteredTotal, out _, out _);
                ApplyPaginationStats(filteredTotal);
                return;
            }

            var (items, total) = await _cashService.GetVouchersPagedAsync(CurrentPage, PageSize, filter);
            Vouchers.Clear();
            foreach (var v in items)
                Vouchers.Add(v);
            ApplyPaginationStats(total);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenAddCashBoxDialog()
    {
        if (!CanAdd) return;
        _editingCashBoxId = null;
        IsCashBoxEditMode = false;
        EditCashBoxName = string.Empty;
        EditOpeningBalance = 0;
        EditIsBank = false;
        IsCashBoxDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditCashBoxDialog(HotelCashBox? box)
    {
        box ??= SelectedCashBox;
        if (box is null || !CanEdit) return;
        _editingCashBoxId = box.Id;
        IsCashBoxEditMode = true;
        EditCashBoxName = box.Name;
        EditOpeningBalance = box.OpeningBalance;
        EditIsBank = box.IsBank;
        IsCashBoxDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCashBoxDialog() => IsCashBoxDialogOpen = false;

    [RelayCommand]
    private async Task SaveCashBoxAsync()
    {
        if (string.IsNullOrWhiteSpace(EditCashBoxName))
        {
            _toast.ShowWarning("أدخل اسم الصندوق");
            return;
        }

        try
        {
            if (_editingCashBoxId.HasValue)
            {
                var box = await _cashService.GetCashBoxByIdAsync(_editingCashBoxId.Value)
                    ?? throw new InvalidOperationException("الصندوق غير موجود");
                box.Name = EditCashBoxName.Trim();
                box.OpeningBalance = EditOpeningBalance;
                box.IsBank = EditIsBank;
                await _cashService.UpdateCashBoxAsync(box);
            }
            else
            {
                await _cashService.CreateCashBoxAsync(new HotelCashBox
                {
                    Name = EditCashBoxName.Trim(),
                    OpeningBalance = EditOpeningBalance,
                    CurrentBalance = EditOpeningBalance,
                    IsBank = EditIsBank,
                    IsActive = true
                });
            }

            IsCashBoxDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAddVoucherDialog()
    {
        if (!CanAdd) return;
        EditVoucherType = HotelVoucherType.Receipt;
        EditVoucherDate = DateTime.Today;
        EditVoucherAmount = 0;
        EditVoucherCashBoxId = CashBoxes.FirstOrDefault()?.Id;
        EditVoucherDescription = string.Empty;
        EditVoucherNotes = string.Empty;
        IsVoucherDialogOpen = true;
    }

    [RelayCommand]
    private void CloseVoucherDialog() => IsVoucherDialogOpen = false;

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        if (!EditVoucherCashBoxId.HasValue || EditVoucherAmount <= 0)
        {
            _toast.ShowWarning("أكمل بيانات السند");
            return;
        }

        try
        {
            var number = await _cashService.GetNextVoucherNumberAsync(EditVoucherType);
            await _cashService.CreateVoucherAsync(new HotelVoucher
            {
                VoucherNumber = number,
                VoucherDate = EditVoucherDate,
                Type = EditVoucherType,
                Amount = EditVoucherAmount,
                HotelCashBoxId = EditVoucherCashBoxId.Value,
                Description = EditVoucherDescription.Trim(),
                Notes = EditVoucherNotes.Trim()
            });

            IsVoucherDialogOpen = false;
            _toast.ShowSuccess("تم إنشاء السند");
            await LoadCashBoxesAsync();
            await LoadVouchersAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    public static string GetVoucherTypeDisplay(HotelVoucherType type) =>
        HotelDisplayHelper.GetVoucherTypeLabel(type);
}
