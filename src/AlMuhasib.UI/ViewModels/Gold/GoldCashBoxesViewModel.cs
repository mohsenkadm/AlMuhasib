using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCashBoxesViewModel : ViewModelBase
{
    private readonly IGoldCashService _cashService;
    private readonly IGoldSettingsService _settingsService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private int? _editingId;

    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    [ObservableProperty] private GoldCashBox? _selectedCashBox;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private GoldCurrency _editCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _editBalance;
    [ObservableProperty] private bool _editIsDefault;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _message = string.Empty;

    public GoldCashBoxesViewModel(
        IGoldCashService cashService,
        IGoldSettingsService settingsService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _cashService = cashService;
        _settingsService = settingsService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "القاصات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CashBoxes);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            CashBoxes.Clear();
            foreach (var box in await _cashService.GetCashBoxesAsync(activeOnly: false))
                CashBoxes.Add(box);
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
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        EditName = string.Empty;
        EditCurrency = GoldCurrency.IQD;
        EditBalance = 0;
        EditIsDefault = false;
        EditIsActive = true;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(GoldCashBox? box)
    {
        box ??= SelectedCashBox;
        if (box is null || !CanEdit) return;
        _editingId = box.Id;
        IsEditMode = true;
        EditName = box.Name;
        EditCurrency = box.Currency;
        EditBalance = box.Balance;
        EditIsDefault = box.IsDefault;
        EditIsActive = box.IsActive;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            _toast.ShowWarning("أدخل اسم القاصة");
            return;
        }

        try
        {
            if (_editingId.HasValue)
            {
                var box = await _cashService.GetCashBoxByIdAsync(_editingId.Value)
                    ?? throw new InvalidOperationException("القاصة غير موجودة");
                box.Name = EditName.Trim();
                box.Currency = EditCurrency;
                box.Balance = EditBalance;
                box.IsDefault = EditIsDefault;
                box.IsActive = EditIsActive;
                await _cashService.UpdateCashBoxAsync(box);
                Message = "تم تحديث القاصة";
            }
            else
            {
                await _cashService.CreateCashBoxAsync(new GoldCashBox
                {
                    Name = EditName.Trim(),
                    Currency = EditCurrency,
                    Balance = EditBalance,
                    IsDefault = EditIsDefault,
                    IsActive = EditIsActive
                });
                Message = "تم إضافة القاصة";
            }

            IsDialogOpen = false;
            _toast.ShowSuccess(Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(GoldCashBox? box)
    {
        box ??= SelectedCashBox;
        if (box is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف القاصة «{box.Name}»؟", "تأكيد الحذف"))
            return;

        try
        {
            await _cashService.DeleteCashBoxAsync(box.Id, _currentUserService.Username ?? "system");
            _toast.ShowSuccess("تم الحذف");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task EnsureDefaultsAsync()
    {
        IsBusy = true;
        try
        {
            await _settingsService.EnsureDefaultsAsync();
            Message = "تم التأكد من القاصات الافتراضية";
            _toast.ShowSuccess(Message);
            await LoadAsync();
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
}
