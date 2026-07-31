using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class DriversViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    public ObservableCollection<Driver> Drivers { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private Driver? _selectedDriver;
    [ObservableProperty] private bool _isCardView;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private Driver? _driverToDelete;

    private int? _editingDriverId;
    private System.Timers.Timer? _debounceTimer;

    public DriversViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Drivers);
        PageTitle = "السواقين";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Drivers");
            await LoadDriversAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDriversAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        System.Linq.Expressions.Expression<Func<Driver, bool>>? searchPredicate = filter is null
            ? null
            : d => d.Name.Contains(filter)
                   || (d.Phone != null && d.Phone.Contains(filter))
                   || (d.Address != null && d.Address.Contains(filter));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.Drivers.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(d => d.CreatedAt));

            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Drivers, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _unitOfWork.Drivers.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(d => d.CreatedAt));

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Drivers.Clear();
        foreach (var d in items)
            Drivers.Add(d);
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadDriversAsync();
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
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDriversAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadDriversAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadDriversAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadDriversAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadDriversAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadDriversAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingDriverId = null;
        IsEditMode = false;
        DialogTitle = "إضافة سائق جديد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Driver driver)
    {
        if (driver is null) return;
        _editingDriverId = driver.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات السائق";
        EditName = driver.Name;
        EditPhone = driver.Phone ?? string.Empty;
        EditAddress = driver.Address ?? string.Empty;
        EditNotes = driver.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveDriver()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم السائق مطلوب";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingDriverId.HasValue)
            {
                var driver = await _unitOfWork.Drivers.GetByIdAsync(_editingDriverId.Value);
                if (driver is null) return;

                driver.Name = EditName.Trim();
                driver.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
                driver.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
                driver.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
                driver.UpdatedAt = DateTime.UtcNow;
                driver.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Drivers.Update(driver);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var driver = new Driver
                {
                    Name = EditName.Trim(),
                    Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
                    Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim(),
                    Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                    CreatedBy = _currentUserService.Username
                };

                await _unitOfWork.Drivers.AddAsync(driver);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadDriversAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(Driver driver)
    {
        if (driver is null) return;
        DriverToDelete = driver;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (DriverToDelete is null) return;
        try
        {
            _unitOfWork.Drivers.SoftDelete(DriverToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            DriverToDelete = null;
            await LoadDriversAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        DriverToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Drivers.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(d => new
            {
                الاسم = d.Name,
                الهاتف = d.Phone ?? "",
                العنوان = d.Address ?? "",
                ملاحظات = d.Notes ?? "",
                تاريخ_الإنشاء = d.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"السواقين_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "السواقين");
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
            var (allItems, _) = await _unitOfWork.Drivers.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "الاسم", "الهاتف", "العنوان", "ملاحظات", "تاريخ الإنشاء" };
            IList<object[]> rows = allItems.Select(d => new object[]
            {
                d.Name,
                d.Phone ?? "",
                d.Address ?? "",
                d.Notes ?? "",
                d.CreatedAt.ToString("yyyy/MM/dd")
            }).ToList();
            _exportService.PrintTable("قائمة السواقين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Drivers, value);
}
