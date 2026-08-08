using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.SalesRep;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public sealed class SalesRepTargetRow
{
    public SalesRepTarget Entity { get; init; } = null!;
    public int Id => Entity.Id;
    public string SalesRepresentativeName { get; init; } = string.Empty;
    public DateTime PeriodStart => Entity.PeriodStart;
    public DateTime PeriodEnd => Entity.PeriodEnd;
    public decimal TargetAmount => Entity.TargetAmount;
    public decimal AchievedAmount { get; init; }
    public decimal AchievementPercent { get; init; }
    public string? Notes => Entity.Notes;
}

public partial class SalesRepTargetsViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalesRepService _salesRepService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<SalesRepTargetRow> Rows { get; } = [];
    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private SalesRepresentative? _filterSalesRep;
    [ObservableProperty] private SalesRepTargetRow? _selectedRow;
    [ObservableProperty] private string _averageAchievement = "0%";

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private SalesRepresentative? _editSalesRep;
    [ObservableProperty] private DateTime _editPeriodStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _editPeriodEnd = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
    [ObservableProperty] private string _editTargetAmount = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private SalesRepTargetRow? _rowToDelete;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;
    private Dictionary<int, string> _repNames = [];
    private Dictionary<int, SalesRepTargetProgress> _progressByTarget = [];

    public SalesRepTargetsViewModel(
        IUnitOfWork unitOfWork,
        ISalesRepService salesRepService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _salesRepService = salesRepService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "أهداف المندوبين";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "SalesRepTargets");
            await LoadLookupsAsync();
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadLookupsAsync()
    {
        Representatives.Clear();
        var reps = (await _unitOfWork.SalesRepresentatives.GetAllAsync()).OrderBy(r => r.Name).ToList();
        _repNames = reps.ToDictionary(r => r.Id, r => r.Name);
        foreach (var r in reps) Representatives.Add(r);
    }

    private async Task LoadAsync()
    {
        var filterRepId = FilterSalesRep?.Id;
        System.Linq.Expressions.Expression<Func<SalesRepTarget, bool>>? predicate =
            filterRepId.HasValue ? t => t.SalesRepresentativeId == filterRepId.Value : null;

        var progress = await _salesRepService.GetTargetProgressAsync(filterRepId, DateTime.Today);
        _progressByTarget = progress.ToDictionary(p => p.TargetId);

        var (allItems, _) = await _unitOfWork.SalesRepTargets.GetPagedAsync(
            1, int.MaxValue, predicate, q => q.OrderByDescending(t => t.PeriodStart));

        var mapped = allItems.Select(MapRow).AsEnumerable();
        var term = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        if (term is not null)
        {
            mapped = mapped.Where(r =>
                r.SalesRepresentativeName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = mapped.ToList();
        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            list = ColumnFilterEngine.Apply(list, ColumnFilters).ToList();

        AverageAchievement = list.Count == 0
            ? "0%"
            : $"{list.Average(r => r.AchievementPercent):N1}%";

        MasterDataColumnFilterHelper.ApplyClientPagination(
            list, Rows, CurrentPage, PageSize,
            out var filteredTotal, out var filteredPages, out var filteredText);
        TotalCount = filteredTotal;
        TotalPages = filteredPages;
        PaginationText = filteredText;
    }

    private SalesRepTargetRow MapRow(SalesRepTarget entity)
    {
        _progressByTarget.TryGetValue(entity.Id, out var progress);
        return new SalesRepTargetRow
        {
            Entity = entity,
            SalesRepresentativeName = _repNames.GetValueOrDefault(entity.SalesRepresentativeId, "—"),
            AchievedAmount = progress?.AchievedAmount ?? 0,
            AchievementPercent = progress?.AchievementPercent ?? 0
        };
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

    partial void OnFilterSalesRepChanged(SalesRepresentative? value)
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
        FilterSalesRep = null;
        await LoadLookupsAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة هدف مبيعات";
        EditSalesRep = Representatives.FirstOrDefault();
        EditPeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EditPeriodEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        EditTargetAmount = string.Empty;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(SalesRepTargetRow? row)
    {
        if (row is null) return;
        var e = row.Entity;
        _editingId = e.Id;
        IsEditMode = true;
        DialogTitle = "تعديل هدف المبيعات";
        EditSalesRep = Representatives.FirstOrDefault(r => r.Id == e.SalesRepresentativeId);
        EditPeriodStart = e.PeriodStart;
        EditPeriodEnd = e.PeriodEnd;
        EditTargetAmount = e.TargetAmount.ToString("0");
        EditNotes = e.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveTarget()
    {
        if (EditSalesRep is null)
        {
            DialogError = "يجب اختيار المندوب";
            return;
        }

        if (!decimal.TryParse(EditTargetAmount.Replace(",", ""), out var amount) || amount <= 0)
        {
            DialogError = "مبلغ الهدف غير صالح";
            return;
        }

        if (EditPeriodEnd.Date < EditPeriodStart.Date)
        {
            DialogError = "نهاية الفترة يجب أن تكون بعد بدايتها";
            return;
        }

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var entity = await _unitOfWork.SalesRepTargets.GetByIdAsync(_editingId.Value);
                if (entity is null) return;
                ApplyFields(entity, amount);
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUserService.Username;
                _unitOfWork.SalesRepTargets.Update(entity);
            }
            else
            {
                var entity = new SalesRepTarget { CreatedBy = _currentUserService.Username };
                ApplyFields(entity, amount);
                await _unitOfWork.SalesRepTargets.AddAsync(entity);
            }

            await _unitOfWork.SaveChangesAsync();
            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    private void ApplyFields(SalesRepTarget entity, decimal amount)
    {
        entity.SalesRepresentativeId = EditSalesRep!.Id;
        entity.PeriodStart = EditPeriodStart.Date;
        entity.PeriodEnd = EditPeriodEnd.Date;
        entity.TargetAmount = amount;
        entity.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
    }

    [RelayCommand] private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(SalesRepTargetRow? row)
    {
        if (row is null) return;
        RowToDelete = row;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (RowToDelete is null) return;
        try
        {
            _unitOfWork.SalesRepTargets.SoftDelete(RowToDelete.Entity, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            RowToDelete = null;
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
        RowToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.SalesRepTargets.GetPagedAsync(1, int.MaxValue);
            var progress = await _salesRepService.GetTargetProgressAsync(null, DateTime.Today);
            var map = progress.ToDictionary(p => p.TargetId);
            var exportData = allItems.Select(t =>
            {
                map.TryGetValue(t.Id, out var p);
                return new
                {
                    المندوب = _repNames.GetValueOrDefault(t.SalesRepresentativeId, ""),
                    من = t.PeriodStart.ToString("yyyy/MM/dd"),
                    إلى = t.PeriodEnd.ToString("yyyy/MM/dd"),
                    الهدف = t.TargetAmount,
                    المحقق = p?.AchievedAmount ?? 0,
                    نسبة_التحقق = p?.AchievementPercent ?? 0,
                    ملاحظات = t.Notes ?? ""
                };
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"أهداف_المندوبين_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الأهداف");
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
            var columns = new[] { "المندوب", "من", "إلى", "الهدف", "المحقق", "نسبة التحقق %" };
            IList<object[]> rows = Rows.Select(r => new object[]
            {
                r.SalesRepresentativeName,
                r.PeriodStart.ToString("yyyy/MM/dd"),
                r.PeriodEnd.ToString("yyyy/MM/dd"),
                r.TargetAmount.ToString("N0"),
                r.AchievedAmount.ToString("N0"),
                r.AchievementPercent.ToString("N1")
            }).ToList();
            _exportService.PrintTable("أهداف المندوبين", columns, rows);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
