using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class PlatformDeductionSettlementViewModel : ViewModelBase
{
    private readonly IPlatformDeductionExcelService _excelService;
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<PlatformDeductionRowVm> Rows { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ICollectionView RowsView { get; }

    [ObservableProperty] private CashBox? _selectedCashBox;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _filterMode = "All";
    [ObservableProperty] private string _statusMessage = "اختر ملف إكسل تقرير استقطاع المنصة للبدء";
    [ObservableProperty] private bool _isSettling;

    [ObservableProperty] private int _totalRows;
    [ObservableProperty] private int _matchedCount;
    [ObservableProperty] private int _suggestedCount;
    [ObservableProperty] private int _notFoundCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private decimal _selectedAmount;

    public PlatformDeductionSettlementViewModel(
        IPlatformDeductionExcelService excelService,
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _excelService = excelService;
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        PageTitle = "تسديد استقطاع المنصة";
        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = FilterRow;
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Installments");
            var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
            CashBoxes.Clear();
            foreach (var cb in cashBoxes)
                CashBoxes.Add(cb);
            if (CashBoxes.Count > 0)
                SelectedCashBox = CashBoxes[0];
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnFilterModeChanged(string value) => RowsView.Refresh();

    private bool FilterRow(object obj)
    {
        if (obj is not PlatformDeductionRowVm row)
            return false;

        return FilterMode switch
        {
            "Matched" => row.MatchStatus == PlatformDeductionMatchStatus.Matched,
            "Suggested" => row.MatchStatus == PlatformDeductionMatchStatus.Suggested,
            "NotFound" => row.MatchStatus is PlatformDeductionMatchStatus.NotFound or PlatformDeductionMatchStatus.Invalid,
            "Selected" => row.IsSelected,
            _ => true
        };
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "اختيار تقرير استقطاع المنصة"
        };
        if (dlg.ShowDialog() != true)
            return;

        FilePath = dlg.FileName;
        await LoadAndMatchAsync();
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            BeautifulMessageDialog.ShowWarning("اختر ملف Excel أولاً");
            return;
        }

        await LoadAndMatchAsync();
    }

    private async Task LoadAndMatchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            StatusMessage = "جاري قراءة الملف...";
            var path = FilePath;

            StatusMessage = "جاري تحميل العملاء...";
            var (allCustomers, _) = await _unitOfWork.Customers.GetPagedAsync(1, int.MaxValue);
            var customerNames = allCustomers
                .Select(c => (c.Id, c.Name ?? string.Empty))
                .ToList();

            StatusMessage = "جاري المطابقة (قد تستغرق لحظات للملفات الكبيرة)...";
            var builtRows = await Task.Run(() => BuildMatchedRows(path, customerNames));

            foreach (var old in Rows)
                old.PropertyChanged -= OnRowPropertyChanged;

            using (RowsView.DeferRefresh())
            {
                Rows.Clear();
                foreach (var row in builtRows)
                {
                    row.PropertyChanged += OnRowPropertyChanged;
                    Rows.Add(row);
                }
            }

            RefreshStats();
            StatusMessage = $"تم تحميل {Rows.Count:N0} صفاً — راجع المطابقة ثم حدّد الصفوف للتسديد";
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"فشل قراءة الملف: {ex.Message}");
            StatusMessage = "فشل تحميل الملف";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<PlatformDeductionRowVm> BuildMatchedRows(
        string path,
        List<(int Id, string Name)> customers)
    {
        var imported = _excelService.ParseImportFile(path);

        var byCompact = new Dictionary<string, (int Id, string Name)>(customers.Count, StringComparer.Ordinal);
        var indexed = new List<(int Id, string Name, string Compact)>(customers.Count);
        foreach (var c in customers)
        {
            var key = ArabicNameNormalizer.Compact(c.Name);
            if (key.Length == 0) continue;
            if (!byCompact.ContainsKey(key))
                byCompact[key] = c;
            indexed.Add((c.Id, c.Name, key));
        }

        var rows = new PlatformDeductionRowVm[imported.Count];
        Parallel.For(0, imported.Count, i =>
        {
            var item = imported[i];
            var row = new PlatformDeductionRowVm(item);

            if (item.HasErrors || item.DeductedAmount <= 0)
            {
                row.MatchStatus = PlatformDeductionMatchStatus.Invalid;
                row.MatchLabel = "بيانات غير صالحة";
                rows[i] = row;
                return;
            }

            var compact = ArabicNameNormalizer.Compact(item.CustomerName);
            if (compact.Length > 0 && byCompact.TryGetValue(compact, out var exact))
            {
                row.MatchedCustomerId = exact.Id;
                row.MatchedCustomerName = exact.Name;
                row.MatchStatus = PlatformDeductionMatchStatus.Matched;
                row.MatchLabel = "مطابق";
                row.IsSelected = true;
                rows[i] = row;
                return;
            }

            var suggestions = ArabicNameNormalizer.FindSuggestionsFast(item.CustomerName, indexed);
            if (suggestions.Count > 0)
            {
                foreach (var s in suggestions)
                    row.Suggestions.Add(new CustomerSuggestionVm(s.Id, s.Name, s.Score));
                row.MatchStatus = PlatformDeductionMatchStatus.Suggested;
                row.MatchLabel = "اقتراح تقريبي";
                row.SelectedSuggestion = row.Suggestions[0];
            }
            else
            {
                row.MatchStatus = PlatformDeductionMatchStatus.NotFound;
                row.MatchLabel = "غير موجود";
            }

            rows[i] = row;
        });

        return rows.ToList();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlatformDeductionRowVm.IsSelected)
            or nameof(PlatformDeductionRowVm.MatchStatus)
            or nameof(PlatformDeductionRowVm.MatchedCustomerId))
        {
            RefreshStats();
            if (FilterMode is "Selected" or "Matched" or "Suggested")
                RowsView.Refresh();
        }
    }

    private void RefreshStats()
    {
        TotalRows = Rows.Count;
        MatchedCount = Rows.Count(r => r.MatchStatus == PlatformDeductionMatchStatus.Matched);
        SuggestedCount = Rows.Count(r => r.MatchStatus == PlatformDeductionMatchStatus.Suggested);
        NotFoundCount = Rows.Count(r => r.MatchStatus is PlatformDeductionMatchStatus.NotFound or PlatformDeductionMatchStatus.Invalid);
        SelectedCount = Rows.Count(r => r.IsSelected && r.CanSettle);
        SelectedAmount = Rows.Where(r => r.IsSelected && r.CanSettle).Sum(r => r.DeductedAmount);
    }

    [RelayCommand]
    private void SetFilter(string? mode)
    {
        FilterMode = mode ?? "All";
    }

    [RelayCommand]
    private void SelectAllMatched()
    {
        foreach (var row in Rows.Where(r => r.CanSettle))
            row.IsSelected = true;
        RefreshStats();
        RowsView.Refresh();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var row in Rows)
            row.IsSelected = false;
        RefreshStats();
        RowsView.Refresh();
    }

    [RelayCommand]
    private void AcceptSuggestion(PlatformDeductionRowVm? row)
    {
        if (row is null || row.SelectedSuggestion is null)
            return;

        row.MatchedCustomerId = row.SelectedSuggestion.Id;
        row.MatchedCustomerName = row.SelectedSuggestion.Name;
        row.MatchStatus = PlatformDeductionMatchStatus.Matched;
        row.MatchLabel = "مطابق (يدوي)";
        row.IsSelected = true;
        RefreshStats();
        RowsView.Refresh();
    }

    [RelayCommand]
    private async Task SettleSelectedAsync()
    {
        if (IsSettling) return;

        if (SelectedCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة أولاً");
            return;
        }

        var toPay = Rows.Where(r => r.IsSelected && r.CanSettle).ToList();
        if (toPay.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد صفوف محددة قابلة للتسديد");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"سيتم تسديد {toPay.Count:N0} صفاً بإجمالي {SelectedAmount:N2} د.ع عبر القاصة «{SelectedCashBox.Name}».\nهل تريد المتابعة؟"))
            return;

        IsSettling = true;
        StatusMessage = "جاري التسديد...";
        var result = new PlatformDeductionPayResult();

        try
        {
            foreach (var row in toPay)
            {
                try
                {
                    var payResult = await _installmentService.PayCustomerAmountOldestFirstAsync(
                        row.MatchedCustomerId!.Value,
                        row.DeductedAmount,
                        SelectedCashBox.Id,
                        notes: $"استقطاع منصة | فاتورة:{row.PlatformInvoiceId} | استقطاع:{row.DeductionId}");

                    result.SuccessCount++;
                    result.TotalPaid += payResult.AmountApplied;
                    row.ResultMessage = payResult.AmountRemaining > 0
                        ? $"تم {payResult.AmountApplied:N2} — متبقي بلا قسط {payResult.AmountRemaining:N2}"
                        : $"تم التسديد ({payResult.InstallmentsTouched} قسط)";
                    row.IsSelected = false;
                    row.IsSettled = true;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"{row.ExcelCustomerName}: {ex.Message}");
                    row.ResultMessage = ex.Message;
                }
            }

            RefreshStats();
            var summary = $"نجاح {result.SuccessCount} | فشل {result.FailedCount} | المدفوع {result.TotalPaid:N2}";
            StatusMessage = summary;
            if (result.FailedCount > 0)
                BeautifulMessageDialog.ShowWarning(summary + "\n" + string.Join("\n", result.Errors.Take(10)));
            else
                BeautifulMessageDialog.ShowSuccess(summary);
        }
        finally
        {
            IsSettling = false;
        }
    }
}

public partial class PlatformDeductionRowVm : ObservableObject
{
    public PlatformDeductionRowVm(PlatformDeductionImportRow source)
    {
        RowNumber = source.RowNumber;
        ExcelCustomerName = source.CustomerName;
        MotherName = source.MotherName;
        PlatformInvoiceId = source.PlatformInvoiceId;
        DeductionId = source.DeductionId;
        DeductedAmount = source.DeductedAmount;
        RequestedAmount = source.RequestedAmount;
        DeductionStatus = source.DeductionStatus;
        CustomerCategory = source.CustomerCategory;
        DeductionDate = source.DeductionDate;
        DueDate = source.DueDate;
        ErrorText = source.HasErrors ? string.Join(" | ", source.Errors) : null;
    }

    public int RowNumber { get; }
    public string ExcelCustomerName { get; }
    public string? MotherName { get; }
    public string? PlatformInvoiceId { get; }
    public string? DeductionId { get; }
    public decimal DeductedAmount { get; }
    public decimal RequestedAmount { get; }
    public string? DeductionStatus { get; }
    public string? CustomerCategory { get; }
    public DateTime? DeductionDate { get; }
    public DateTime? DueDate { get; }
    public string? ErrorText { get; }

    public ObservableCollection<CustomerSuggestionVm> Suggestions { get; } = [];

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isSettled;
    [ObservableProperty] private PlatformDeductionMatchStatus _matchStatus = PlatformDeductionMatchStatus.NotFound;
    [ObservableProperty] private string _matchLabel = string.Empty;
    [ObservableProperty] private int? _matchedCustomerId;
    [ObservableProperty] private string? _matchedCustomerName;
    [ObservableProperty] private CustomerSuggestionVm? _selectedSuggestion;
    [ObservableProperty] private string? _resultMessage;

    public bool CanSettle =>
        !IsSettled
        && MatchStatus == PlatformDeductionMatchStatus.Matched
        && MatchedCustomerId.HasValue
        && DeductedAmount > 0;

    public string RowBackground => MatchStatus switch
    {
        PlatformDeductionMatchStatus.Matched => IsSettled ? "#E8F5E9" : "#E8F5E9",
        PlatformDeductionMatchStatus.Suggested => "#FFF8E1",
        PlatformDeductionMatchStatus.Invalid => "#FFEBEE",
        _ => "#FAFAFA"
    };

    public string StatusChipColor => MatchStatus switch
    {
        PlatformDeductionMatchStatus.Matched => "#2E7D32",
        PlatformDeductionMatchStatus.Suggested => "#EF6C00",
        PlatformDeductionMatchStatus.Invalid => "#C62828",
        _ => "#757575"
    };

    partial void OnMatchStatusChanged(PlatformDeductionMatchStatus value) =>
        OnPropertyChanged(nameof(CanSettle));

    partial void OnMatchedCustomerIdChanged(int? value) =>
        OnPropertyChanged(nameof(CanSettle));

    partial void OnIsSettledChanged(bool value) =>
        OnPropertyChanged(nameof(CanSettle));
}

public sealed class CustomerSuggestionVm
{
    public CustomerSuggestionVm(int id, string name, double score)
    {
        Id = id;
        Name = name;
        Score = score;
    }

    public int Id { get; }
    public string Name { get; }
    public double Score { get; }
    public string Display => $"{Name} ({Score:P0})";
}
