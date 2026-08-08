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

public partial class SalesRepresentativesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private SalesRepresentative? _selectedRepresentative;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _statusFilter = "الكل";

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editRegion = string.Empty;
    [ObservableProperty] private DateTime _editStartDate = DateTime.Today;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _editMonthlySalary = string.Empty;
    [ObservableProperty] private string _editCompensationNotes = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private SalesRepresentative? _representativeToDelete;

    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _inactiveCount;
    [ObservableProperty] private int _customersLinkedCount;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;

    public string[] StatusFilters { get; } = ["الكل", "فعال", "غير فعال"];

    public SalesRepresentativesViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.SalesRepresentatives);
        PageTitle = "المندوبين";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "SalesRepresentatives");
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        System.Linq.Expressions.Expression<Func<SalesRepresentative, bool>>? searchPredicate = filter is null
            ? null
            : r => r.Name.Contains(filter)
                   || (r.Phone != null && r.Phone.Contains(filter))
                   || (r.Region != null && r.Region.Contains(filter));

        if (StatusFilter == "فعال")
        {
            var basePred = searchPredicate;
            searchPredicate = basePred is null
                ? r => r.IsActive
                : r => r.IsActive && (r.Name.Contains(filter!)
                                      || (r.Phone != null && r.Phone.Contains(filter!))
                                      || (r.Region != null && r.Region.Contains(filter!)));
        }
        else if (StatusFilter == "غير فعال")
        {
            searchPredicate = filter is null
                ? r => !r.IsActive
                : r => !r.IsActive && (r.Name.Contains(filter)
                                      || (r.Phone != null && r.Phone.Contains(filter))
                                      || (r.Region != null && r.Region.Contains(filter)));
        }

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.SalesRepresentatives.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(r => r.CreatedAt));
            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Representatives, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
        }
        else
        {
            var (items, totalCount) = await _unitOfWork.SalesRepresentatives.GetPagedAsync(
                CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(r => r.CreatedAt));
            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);
            Representatives.Clear();
            foreach (var r in items)
                Representatives.Add(r);
        }

        var all = await _unitOfWork.SalesRepresentatives.GetAllAsync();
        ActiveCount = all.Count(r => r.IsActive);
        InactiveCount = all.Count(r => !r.IsActive);
        var customers = await _unitOfWork.Customers.FindAsync(c => c.SalesRepresentativeId != null);
        CustomersLinkedCount = customers.Count();
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadAsync();
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

    partial void OnStatusFilterChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand] private async Task FirstPage() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); } }
    [RelayCommand] private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] private async Task LastPage() { CurrentPage = TotalPages; await LoadAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        StatusFilter = "الكل";
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة مندوب جديد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditRegion = string.Empty;
        EditStartDate = DateTime.Today;
        EditIsActive = true;
        EditMonthlySalary = string.Empty;
        EditCompensationNotes = string.Empty;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(SalesRepresentative rep)
    {
        if (rep is null) return;
        _editingId = rep.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات المندوب";
        EditName = rep.Name;
        EditPhone = rep.Phone ?? string.Empty;
        EditRegion = rep.Region ?? string.Empty;
        EditStartDate = rep.StartDate;
        EditIsActive = rep.IsActive;
        EditMonthlySalary = rep.MonthlySalary?.ToString("0") ?? string.Empty;
        EditCompensationNotes = rep.CompensationNotes ?? string.Empty;
        EditNotes = rep.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveRepresentative()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المندوب مطلوب";
            return;
        }

        decimal? salary = null;
        if (!string.IsNullOrWhiteSpace(EditMonthlySalary))
        {
            if (!decimal.TryParse(EditMonthlySalary.Replace(",", ""), out var s) || s < 0)
            {
                DialogError = "الراتب غير صالح";
                return;
            }
            salary = s;
        }

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var rep = await _unitOfWork.SalesRepresentatives.GetByIdAsync(_editingId.Value);
                if (rep is null) return;
                ApplyFields(rep, salary);
                rep.UpdatedAt = DateTime.UtcNow;
                rep.UpdatedBy = _currentUserService.Username;
                _unitOfWork.SalesRepresentatives.Update(rep);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var rep = new SalesRepresentative { CreatedBy = _currentUserService.Username };
                ApplyFields(rep, salary);
                await _unitOfWork.SalesRepresentatives.AddAsync(rep);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    private void ApplyFields(SalesRepresentative rep, decimal? salary)
    {
        rep.Name = EditName.Trim();
        rep.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
        rep.Region = string.IsNullOrWhiteSpace(EditRegion) ? null : EditRegion.Trim();
        rep.StartDate = EditStartDate.Date;
        rep.IsActive = EditIsActive;
        rep.MonthlySalary = salary;
        rep.CompensationNotes = string.IsNullOrWhiteSpace(EditCompensationNotes) ? null : EditCompensationNotes.Trim();
        rep.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
    }

    [RelayCommand] private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(SalesRepresentative rep)
    {
        if (rep is null) return;
        RepresentativeToDelete = rep;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (RepresentativeToDelete is null) return;
        try
        {
            _unitOfWork.SalesRepresentatives.SoftDelete(RepresentativeToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            RepresentativeToDelete = null;
            await LoadAsync();
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
        RepresentativeToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.SalesRepresentatives.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(r => new
            {
                الاسم = r.Name,
                الهاتف = r.Phone ?? "",
                المنطقة = r.Region ?? "",
                تاريخ_المباشرة = r.StartDate.ToString("yyyy/MM/dd"),
                الحالة = r.IsActive ? "فعال" : "غير فعال",
                الراتب = r.MonthlySalary?.ToString("N0") ?? "",
                ملاحظات_التعويض = r.CompensationNotes ?? "",
                ملاحظات = r.Notes ?? ""
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"المندوبين_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المندوبين");
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
            var (allItems, _) = await _unitOfWork.SalesRepresentatives.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "الاسم", "الهاتف", "المنطقة", "تاريخ المباشرة", "الحالة", "الراتب" };
            IList<object[]> rows = allItems.Select(r => new object[]
            {
                r.Name,
                r.Phone ?? "",
                r.Region ?? "",
                r.StartDate.ToString("yyyy/MM/dd"),
                r.IsActive ? "فعال" : "غير فعال",
                r.MonthlySalary?.ToString("N0") ?? ""
            }).ToList();
            _exportService.PrintTable("قائمة المندوبين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.SalesRepresentatives, value);
}
