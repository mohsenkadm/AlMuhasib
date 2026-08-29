using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCashBoxesViewModel : ViewModelBase
{
    private readonly IGoldCashService _cashService;
    private readonly IGoldSettingsService _settingsService;
    private readonly IExportService _exportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private List<GoldCashBox> _allBoxes = [];
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
    [ObservableProperty] private bool _isCardView;

    public GoldCashBoxesViewModel(
        IGoldCashService cashService,
        IGoldSettingsService settingsService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _cashService = cashService;
        _settingsService = settingsService;
        _exportService = exportService;
        _toast = toast;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.GoldCashBoxes);
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
            _allBoxes = (await _cashService.GetCashBoxesAsync(activeOnly: false)).ToList();
            ApplyFilters();
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

    private void ApplyFilters()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allBoxes, ColumnFilters)
            : _allBoxes.ToList();

        CashBoxes.Clear();
        foreach (var box in filtered)
            CashBoxes.Add(box);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

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

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allBoxes.Count == 0)
                await LoadAsync();

            var exportData = _allBoxes.Select(b => new
            {
                الاسم = b.Name,
                العملة = b.Currency.ToString(),
                الرصيد = b.Balance,
                افتراضي = b.IsDefault ? "نعم" : "لا",
                نشط = b.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"قاصات_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "القاصات");
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
            if (_allBoxes.Count == 0)
                await LoadAsync();

            var columns = new[] { "الاسم", "العملة", "الرصيد", "افتراضي", "نشط" };
            IList<object[]> rows = _allBoxes.Select(b => new object[]
            {
                b.Name,
                b.Currency.ToString(),
                b.Balance.ToString("N0"),
                b.IsDefault ? "نعم" : "لا",
                b.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("قائمة القاصات", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.GoldCashBoxes, value);
}
