using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class PricingTypesViewModel : ViewModelBase
{
    private readonly IPricingTypeService _pricingTypeService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    public ObservableCollection<PricingType> PricingTypes { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private PricingType? _selectedPricingType;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private bool _editIsDefault;
    [ObservableProperty] private string _dialogError = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private PricingType? _pricingTypeToDelete;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;

    public PricingTypesViewModel(
        IPricingTypeService pricingTypeService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _pricingTypeService = pricingTypeService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, "PricingTypes");
        PageTitle = "أنواع التسعير";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "PricingTypes");
            await _pricingTypeService.EnsureDefaultExistsAsync();
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        var (items, totalCount) = await _pricingTypeService.GetPagedAsync(CurrentPage, PageSize, SearchText);
        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        PricingTypes.Clear();
        foreach (var item in items)
            PricingTypes.Add(item);
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة نوع تسعير";
        EditName = string.Empty;
        EditIsActive = true;
        EditIsDefault = false;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(PricingType item)
    {
        if (item is null) return;
        _editingId = item.Id;
        IsEditMode = true;
        DialogTitle = "تعديل نوع التسعير";
        EditName = item.Name;
        EditIsActive = item.IsActive;
        EditIsDefault = item.IsDefault;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SavePricingType()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم نوع التسعير مطلوب";
            return;
        }

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                await _pricingTypeService.UpdateAsync(new PricingType
                {
                    Id = _editingId.Value,
                    Name = EditName.Trim(),
                    IsActive = EditIsActive,
                    IsDefault = EditIsDefault
                });
            }
            else
            {
                await _pricingTypeService.CreateAsync(new PricingType
                {
                    Name = EditName.Trim(),
                    IsActive = EditIsActive,
                    IsDefault = EditIsDefault
                });
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(PricingType item)
    {
        if (item is null) return;
        PricingTypeToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (PricingTypeToDelete is null) return;
        try
        {
            await _pricingTypeService.DeleteAsync(PricingTypeToDelete.Id);
            IsDeleteDialogOpen = false;
            PricingTypeToDelete = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
            IsDeleteDialogOpen = false;
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        PricingTypeToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _pricingTypeService.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(t => new
            {
                الاسم = t.Name,
                افتراضي = t.IsDefault ? "نعم" : "لا",
                نشط = t.IsActive ? "نعم" : "لا",
                تاريخ_الإنشاء = t.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"انواع_التسعير_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "أنواع التسعير");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, "PricingTypes", value);
}
