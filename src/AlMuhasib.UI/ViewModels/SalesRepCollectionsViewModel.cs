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

public sealed class PaymentMethodOption
{
    public PaymentMethod Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public sealed class SalesRepCollectionRow
{
    public SalesRepCollection Entity { get; init; } = null!;
    public int Id => Entity.Id;
    public string SalesRepresentativeName { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Amount => Entity.Amount;
    public DateTime CollectionDate => Entity.CollectionDate;
    public string? ReceiptNumber => Entity.ReceiptNumber;
    public string PaymentMethodLabel { get; init; } = string.Empty;
    public decimal HandedOverAmount => Entity.HandedOverAmount;
    public decimal PendingHandoverAmount => Entity.PendingHandoverAmount;
    public string? Notes => Entity.Notes;
    public bool HasPending => Entity.PendingHandoverAmount > 0;
}

public partial class SalesRepCollectionsViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalesRepService _salesRepService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<SalesRepCollectionRow> Rows { get; } = [];
    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<PaymentMethodOption> PaymentMethods { get; } =
    [
        new() { Value = PaymentMethod.Cash, Label = "نقدي" },
        new() { Value = PaymentMethod.Credit, Label = "آجل" },
        new() { Value = PaymentMethod.Installment, Label = "أقساط" },
    ];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private SalesRepresentative? _filterSalesRep;
    [ObservableProperty] private SalesRepCollectionRow? _selectedRow;

    [ObservableProperty] private string _totalCollected = "0";
    [ObservableProperty] private string _totalHandedOver = "0";
    [ObservableProperty] private string _totalPending = "0";

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private SalesRepresentative? _editSalesRep;
    [ObservableProperty] private Customer? _editCustomer;
    [ObservableProperty] private string _editAmount = string.Empty;
    [ObservableProperty] private DateTime _editCollectionDate = DateTime.Today;
    [ObservableProperty] private string _editReceiptNumber = string.Empty;
    [ObservableProperty] private PaymentMethodOption? _editPaymentMethod;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private SalesRepCollectionRow? _rowToDelete;

    [ObservableProperty] private bool _isHandoverDialogOpen;
    [ObservableProperty] private SalesRepCollectionRow? _handoverTarget;
    [ObservableProperty] private string _handoverAmount = string.Empty;
    [ObservableProperty] private string _handoverError = string.Empty;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;
    private Dictionary<int, string> _repNames = [];
    private Dictionary<int, string> _customerNames = [];

    public SalesRepCollectionsViewModel(
        IUnitOfWork unitOfWork,
        ISalesRepService salesRepService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _salesRepService = salesRepService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "تحصيلات المندوبين";
        EditPaymentMethod = PaymentMethods[0];
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "SalesRepCollections");
            await LoadLookupsAsync();
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadLookupsAsync()
    {
        Representatives.Clear();
        Customers.Clear();
        var reps = (await _unitOfWork.SalesRepresentatives.GetAllAsync()).OrderBy(r => r.Name).ToList();
        _repNames = reps.ToDictionary(r => r.Id, r => r.Name);
        foreach (var r in reps) Representatives.Add(r);

        var customers = (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name).ToList();
        _customerNames = customers.ToDictionary(c => c.Id, c => c.Name);
        foreach (var c in customers) Customers.Add(c);
    }

    private static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Credit => "آجل",
        PaymentMethod.Installment => "أقساط",
        _ => method.ToString()
    };

    private async Task LoadAsync()
    {
        var filterRepId = FilterSalesRep?.Id;
        System.Linq.Expressions.Expression<Func<SalesRepCollection, bool>>? predicate =
            filterRepId.HasValue ? c => c.SalesRepresentativeId == filterRepId.Value : null;

        var (allItems, _) = await _unitOfWork.SalesRepCollections.GetPagedAsync(
            1, int.MaxValue, predicate, q => q.OrderByDescending(c => c.CollectionDate));

        TotalCollected = $"{allItems.Sum(c => c.Amount):N0}";
        TotalHandedOver = $"{allItems.Sum(c => c.HandedOverAmount):N0}";
        TotalPending = $"{allItems.Sum(c => c.PendingHandoverAmount):N0}";

        var mapped = allItems.Select(MapRow).AsEnumerable();
        var term = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        if (term is not null)
        {
            mapped = mapped.Where(r =>
                r.SalesRepresentativeName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.ReceiptNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
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
    }

    private SalesRepCollectionRow MapRow(SalesRepCollection entity) => new()
    {
        Entity = entity,
        SalesRepresentativeName = _repNames.GetValueOrDefault(entity.SalesRepresentativeId, "—"),
        CustomerName = _customerNames.GetValueOrDefault(entity.CustomerId, "—"),
        PaymentMethodLabel = PaymentLabel(entity.PaymentMethod)
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
        DialogTitle = "تسجيل تحصيل";
        EditSalesRep = Representatives.FirstOrDefault();
        EditCustomer = Customers.FirstOrDefault();
        EditAmount = string.Empty;
        EditCollectionDate = DateTime.Today;
        EditReceiptNumber = string.Empty;
        EditPaymentMethod = PaymentMethods[0];
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(SalesRepCollectionRow? row)
    {
        if (row is null) return;
        var e = row.Entity;
        _editingId = e.Id;
        IsEditMode = true;
        DialogTitle = "تعديل تحصيل";
        EditSalesRep = Representatives.FirstOrDefault(r => r.Id == e.SalesRepresentativeId);
        EditCustomer = Customers.FirstOrDefault(c => c.Id == e.CustomerId);
        EditAmount = e.Amount.ToString("0");
        EditCollectionDate = e.CollectionDate;
        EditReceiptNumber = e.ReceiptNumber ?? string.Empty;
        EditPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Value == e.PaymentMethod) ?? PaymentMethods[0];
        EditNotes = e.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveCollection()
    {
        if (EditSalesRep is null) { DialogError = "يجب اختيار المندوب"; return; }
        if (EditCustomer is null) { DialogError = "يجب اختيار العميل"; return; }
        if (!decimal.TryParse(EditAmount.Replace(",", ""), out var amount) || amount <= 0)
        {
            DialogError = "المبلغ غير صالح";
            return;
        }

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var entity = await _unitOfWork.SalesRepCollections.GetByIdAsync(_editingId.Value);
                if (entity is null) return;
                ApplyFields(entity, amount);
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUserService.Username;
                _unitOfWork.SalesRepCollections.Update(entity);
            }
            else
            {
                var entity = new SalesRepCollection { CreatedBy = _currentUserService.Username };
                ApplyFields(entity, amount);
                await _unitOfWork.SalesRepCollections.AddAsync(entity);
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

    private void ApplyFields(SalesRepCollection entity, decimal amount)
    {
        entity.SalesRepresentativeId = EditSalesRep!.Id;
        entity.CustomerId = EditCustomer!.Id;
        entity.Amount = amount;
        entity.CollectionDate = EditCollectionDate.Date;
        entity.ReceiptNumber = string.IsNullOrWhiteSpace(EditReceiptNumber) ? null : EditReceiptNumber.Trim();
        entity.PaymentMethod = EditPaymentMethod?.Value ?? PaymentMethod.Cash;
        entity.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
    }

    [RelayCommand] private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(SalesRepCollectionRow? row)
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
            _unitOfWork.SalesRepCollections.SoftDelete(RowToDelete.Entity, _currentUserService.Username);
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
    private void OpenHandoverDialog(SalesRepCollectionRow? row)
    {
        if (row is null || !row.HasPending) return;
        HandoverTarget = row;
        HandoverAmount = row.PendingHandoverAmount.ToString("0");
        HandoverError = string.Empty;
        IsHandoverDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmHandover()
    {
        if (HandoverTarget is null) return;
        if (!decimal.TryParse(HandoverAmount.Replace(",", ""), out var amount) || amount <= 0)
        {
            HandoverError = "المبلغ غير صالح";
            return;
        }

        if (amount > HandoverTarget.PendingHandoverAmount)
        {
            HandoverError = "المبلغ أكبر من المتبقي للتسليم";
            return;
        }

        try
        {
            await _salesRepService.MarkCollectionHandedOverAsync(HandoverTarget.Id, amount);
            IsHandoverDialogOpen = false;
            HandoverTarget = null;
            await LoadAsync();
            BeautifulMessageDialog.ShowSuccess("تم تسجيل التسليم بنجاح");
        }
        catch (Exception ex)
        {
            HandoverError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task HandoverFull()
    {
        if (HandoverTarget is null) return;
        HandoverAmount = HandoverTarget.PendingHandoverAmount.ToString("0");
        await ConfirmHandover();
    }

    [RelayCommand]
    private void CancelHandover()
    {
        IsHandoverDialogOpen = false;
        HandoverTarget = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.SalesRepCollections.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(MapRow).Select(r => new
            {
                المندوب = r.SalesRepresentativeName,
                العميل = r.CustomerName,
                المبلغ = r.Amount,
                التاريخ = r.CollectionDate.ToString("yyyy/MM/dd"),
                رقم_الوصل = r.ReceiptNumber ?? "",
                طريقة_الدفع = r.PaymentMethodLabel,
                المسلّم = r.HandedOverAmount,
                المتبقي = r.PendingHandoverAmount,
                ملاحظات = r.Notes ?? ""
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تحصيلات_المندوبين_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "التحصيلات");
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
            var (allItems, _) = await _unitOfWork.SalesRepCollections.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "المندوب", "العميل", "المبلغ", "التاريخ", "رقم الوصل", "المسلّم", "المتبقي" };
            IList<object[]> rows = allItems.Select(MapRow).Select(r => new object[]
            {
                r.SalesRepresentativeName,
                r.CustomerName,
                r.Amount.ToString("N0"),
                r.CollectionDate.ToString("yyyy/MM/dd"),
                r.ReceiptNumber ?? "",
                r.HandedOverAmount.ToString("N0"),
                r.PendingHandoverAmount.ToString("N0")
            }).ToList();
            _exportService.PrintTable("تحصيلات المندوبين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
