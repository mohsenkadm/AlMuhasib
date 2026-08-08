using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public static class SalesRepCommissionTypeLabels
{
    public static string Get(SalesRepCommissionType type) => type switch
    {
        SalesRepCommissionType.PercentOfSales => "نسبة من المبيعات",
        SalesRepCommissionType.PercentOfNetProfit => "نسبة من صافي الربح",
        SalesRepCommissionType.FixedPerInvoice => "مبلغ ثابت لكل فاتورة",
        SalesRepCommissionType.ByProduct => "حسب المنتج",
        SalesRepCommissionType.ByCustomer => "حسب العميل",
        _ => type.ToString()
    };
}

public sealed class CommissionTypeOption
{
    public SalesRepCommissionType Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public sealed class SalesRepCommissionRuleRow
{
    public SalesRepCommissionRule Entity { get; init; } = null!;
    public int Id => Entity.Id;
    public string SalesRepresentativeName { get; init; } = string.Empty;
    public string CommissionTypeLabel { get; init; } = string.Empty;
    public decimal Percentage => Entity.Percentage;
    public decimal FixedAmount => Entity.FixedAmount;
    public string? ProductName { get; init; }
    public string? CustomerName { get; init; }
    public bool IsActive => Entity.IsActive;
    public string? Notes => Entity.Notes;
}

public partial class SalesRepCommissionRulesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<SalesRepCommissionRuleRow> Rows { get; } = [];
    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<CommissionTypeOption> CommissionTypes { get; } =
    [
        new() { Value = SalesRepCommissionType.PercentOfSales, Label = SalesRepCommissionTypeLabels.Get(SalesRepCommissionType.PercentOfSales) },
        new() { Value = SalesRepCommissionType.PercentOfNetProfit, Label = SalesRepCommissionTypeLabels.Get(SalesRepCommissionType.PercentOfNetProfit) },
        new() { Value = SalesRepCommissionType.FixedPerInvoice, Label = SalesRepCommissionTypeLabels.Get(SalesRepCommissionType.FixedPerInvoice) },
        new() { Value = SalesRepCommissionType.ByProduct, Label = SalesRepCommissionTypeLabels.Get(SalesRepCommissionType.ByProduct) },
        new() { Value = SalesRepCommissionType.ByCustomer, Label = SalesRepCommissionTypeLabels.Get(SalesRepCommissionType.ByCustomer) },
    ];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private SalesRepresentative? _filterSalesRep;
    [ObservableProperty] private SalesRepCommissionRuleRow? _selectedRow;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private SalesRepresentative? _editSalesRep;
    [ObservableProperty] private CommissionTypeOption? _editCommissionType;
    [ObservableProperty] private string _editPercentage = "0";
    [ObservableProperty] private string _editFixedAmount = "0";
    [ObservableProperty] private Product? _editProduct;
    [ObservableProperty] private Customer? _editCustomer;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private SalesRepCommissionRuleRow? _rowToDelete;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;
    private Dictionary<int, string> _repNames = [];
    private Dictionary<int, string> _productNames = [];
    private Dictionary<int, string> _customerNames = [];

    public SalesRepCommissionRulesViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "قواعد عمولة المندوبين";
        EditCommissionType = CommissionTypes[0];
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "SalesRepCommissionRules");
            await LoadLookupsAsync();
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadLookupsAsync()
    {
        Representatives.Clear();
        Products.Clear();
        Customers.Clear();

        var reps = (await _unitOfWork.SalesRepresentatives.GetAllAsync()).OrderBy(r => r.Name).ToList();
        _repNames = reps.ToDictionary(r => r.Id, r => r.Name);
        foreach (var r in reps) Representatives.Add(r);

        var products = (await _unitOfWork.Products.GetAllAsync()).OrderBy(p => p.Name).ToList();
        _productNames = products.ToDictionary(p => p.Id, p => p.Name);
        foreach (var p in products) Products.Add(p);

        var customers = (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name).ToList();
        _customerNames = customers.ToDictionary(c => c.Id, c => c.Name);
        foreach (var c in customers) Customers.Add(c);
    }

    private async Task LoadAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        var filterRepId = FilterSalesRep?.Id;

        System.Linq.Expressions.Expression<Func<SalesRepCommissionRule, bool>>? predicate = null;
        if (filterRepId.HasValue && filter is not null)
            predicate = r => r.SalesRepresentativeId == filterRepId && (r.Notes != null && r.Notes.Contains(filter));
        else if (filterRepId.HasValue)
            predicate = r => r.SalesRepresentativeId == filterRepId.Value;
        else if (filter is not null)
            predicate = r => r.Notes != null && r.Notes.Contains(filter);

        var (items, totalCount) = await _unitOfWork.SalesRepCommissionRules.GetPagedAsync(
            CurrentPage, PageSize, predicate, q => q.OrderByDescending(r => r.CreatedAt));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters) || !string.IsNullOrWhiteSpace(filter))
        {
            var (allItems, _) = await _unitOfWork.SalesRepCommissionRules.GetPagedAsync(
                1, int.MaxValue, filterRepId.HasValue ? r => r.SalesRepresentativeId == filterRepId : null,
                q => q.OrderByDescending(r => r.CreatedAt));

            var mapped = allItems.Select(MapRow).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                mapped = mapped.Where(r =>
                    r.SalesRepresentativeName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || r.CommissionTypeLabel.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || (r.ProductName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (r.CustomerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (r.Notes?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var list = mapped.ToList();
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
                list = ColumnFilterEngine.Apply(list, ColumnFilters).ToList();

            MasterDataColumnFilterHelper.ApplyClientPagination(
                list, Rows, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);
        Rows.Clear();
        foreach (var item in items)
            Rows.Add(MapRow(item));
    }

    private SalesRepCommissionRuleRow MapRow(SalesRepCommissionRule entity) => new()
    {
        Entity = entity,
        SalesRepresentativeName = _repNames.GetValueOrDefault(entity.SalesRepresentativeId, "—"),
        CommissionTypeLabel = SalesRepCommissionTypeLabels.Get(entity.CommissionType),
        ProductName = entity.ProductId is int pid ? _productNames.GetValueOrDefault(pid) : null,
        CustomerName = entity.CustomerId is int cid ? _customerNames.GetValueOrDefault(cid) : null
    };

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
        DialogTitle = "إضافة قاعدة عمولة";
        EditSalesRep = Representatives.FirstOrDefault();
        EditCommissionType = CommissionTypes[0];
        EditPercentage = "0";
        EditFixedAmount = "0";
        EditProduct = null;
        EditCustomer = null;
        EditIsActive = true;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(SalesRepCommissionRuleRow? row)
    {
        if (row is null) return;
        var e = row.Entity;
        _editingId = e.Id;
        IsEditMode = true;
        DialogTitle = "تعديل قاعدة عمولة";
        EditSalesRep = Representatives.FirstOrDefault(r => r.Id == e.SalesRepresentativeId);
        EditCommissionType = CommissionTypes.FirstOrDefault(c => c.Value == e.CommissionType) ?? CommissionTypes[0];
        EditPercentage = e.Percentage.ToString("0.##");
        EditFixedAmount = e.FixedAmount.ToString("0.##");
        EditProduct = e.ProductId is int pid ? Products.FirstOrDefault(p => p.Id == pid) : null;
        EditCustomer = e.CustomerId is int cid ? Customers.FirstOrDefault(c => c.Id == cid) : null;
        EditIsActive = e.IsActive;
        EditNotes = e.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveRule()
    {
        if (EditSalesRep is null)
        {
            DialogError = "يجب اختيار المندوب";
            return;
        }

        if (EditCommissionType is null)
        {
            DialogError = "يجب اختيار نوع العمولة";
            return;
        }

        if (!decimal.TryParse(EditPercentage.Replace(",", ""), out var percentage) || percentage < 0)
        {
            DialogError = "النسبة غير صالحة";
            return;
        }

        if (!decimal.TryParse(EditFixedAmount.Replace(",", ""), out var fixedAmount) || fixedAmount < 0)
        {
            DialogError = "المبلغ الثابت غير صالح";
            return;
        }

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var entity = await _unitOfWork.SalesRepCommissionRules.GetByIdAsync(_editingId.Value);
                if (entity is null) return;
                ApplyFields(entity, percentage, fixedAmount);
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUserService.Username;
                _unitOfWork.SalesRepCommissionRules.Update(entity);
            }
            else
            {
                var entity = new SalesRepCommissionRule { CreatedBy = _currentUserService.Username };
                ApplyFields(entity, percentage, fixedAmount);
                await _unitOfWork.SalesRepCommissionRules.AddAsync(entity);
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

    private void ApplyFields(SalesRepCommissionRule entity, decimal percentage, decimal fixedAmount)
    {
        entity.SalesRepresentativeId = EditSalesRep!.Id;
        entity.CommissionType = EditCommissionType!.Value;
        entity.Percentage = percentage;
        entity.FixedAmount = fixedAmount;
        entity.ProductId = EditProduct?.Id;
        entity.CustomerId = EditCustomer?.Id;
        entity.IsActive = EditIsActive;
        entity.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
    }

    [RelayCommand] private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(SalesRepCommissionRuleRow? row)
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
            _unitOfWork.SalesRepCommissionRules.SoftDelete(RowToDelete.Entity, _currentUserService.Username);
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
            var (allItems, _) = await _unitOfWork.SalesRepCommissionRules.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(MapRow).Select(r => new
            {
                المندوب = r.SalesRepresentativeName,
                نوع_العمولة = r.CommissionTypeLabel,
                النسبة = r.Percentage,
                مبلغ_ثابت = r.FixedAmount,
                المنتج = r.ProductName ?? "",
                العميل = r.CustomerName ?? "",
                الحالة = r.IsActive ? "فعال" : "غير فعال",
                ملاحظات = r.Notes ?? ""
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"قواعد_عمولة_المندوبين_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "قواعد العمولة");
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
            var (allItems, _) = await _unitOfWork.SalesRepCommissionRules.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "المندوب", "نوع العمولة", "النسبة", "مبلغ ثابت", "المنتج", "العميل", "الحالة" };
            IList<object[]> rows = allItems.Select(MapRow).Select(r => new object[]
            {
                r.SalesRepresentativeName,
                r.CommissionTypeLabel,
                r.Percentage.ToString("N2"),
                r.FixedAmount.ToString("N0"),
                r.ProductName ?? "",
                r.CustomerName ?? "",
                r.IsActive ? "فعال" : "غير فعال"
            }).ToList();
            _exportService.PrintTable("قواعد عمولة المندوبين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
