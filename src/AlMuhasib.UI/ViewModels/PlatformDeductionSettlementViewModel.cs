using System.Collections.ObjectModel;
using System.ComponentModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
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

    /// <summary>كل صفوف الاستيراد (كل الصفحات) — التحديد والتسديد يعتمدان عليها.</summary>
    private List<PlatformDeductionRowVm> _allRows = [];

    /// <summary>الصفحة الحالية المعروضة في الجدول فقط.</summary>
    public ObservableCollection<PlatformDeductionRowVm> PagedRows { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public PagerState RowsPager { get; } = new() { PageSize = 50 };

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

    private bool _suppressRowViewRefresh;
    private CancellationTokenSource? _loadCts;
    private List<(int Id, string Name, string Compact)>? _customerIndex;
    private Dictionary<string, List<(int Id, string Name, string Compact)>>? _prefixIndex;

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
        RowsPager.Bind(ShowCurrentPageAsync);
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

    partial void OnFilterModeChanged(string value)
    {
        RowsPager.ResetToFirstPage();
        _ = ShowCurrentPageAsync();
    }

    private bool MatchesFilter(PlatformDeductionRowVm row) => FilterMode switch
    {
        "Matched" => row.MatchStatus == PlatformDeductionMatchStatus.Matched,
        "Suggested" => row.MatchStatus == PlatformDeductionMatchStatus.Suggested,
        "NotFound" => row.MatchStatus is PlatformDeductionMatchStatus.NotFound or PlatformDeductionMatchStatus.Invalid,
        "Selected" => row.IsSelected,
        _ => true
    };

    private List<PlatformDeductionRowVm> GetFilteredRows() =>
        _allRows.Where(MatchesFilter).ToList();

    private Task ShowCurrentPageAsync()
    {
        var filtered = GetFilteredRows();
        RowsPager.ApplyStats(filtered.Count);

        var page = Math.Max(1, Math.Min(RowsPager.CurrentPage, Math.Max(1, RowsPager.TotalPages)));
        if (page != RowsPager.CurrentPage)
            RowsPager.CurrentPage = page;

        var skip = (page - 1) * RowsPager.PageSize;
        var slice = filtered.Skip(skip).Take(RowsPager.PageSize).ToList();

        PagedRows.Clear();
        foreach (var row in slice)
            PagedRows.Add(row);

        return Task.CompletedTask;
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

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            StatusMessage = "جاري قراءة الملف وتحميل العملاء...";
            var path = FilePath;

            var allCustomers = await _unitOfWork.Customers.GetAllAsync();
            var customerNames = allCustomers
                .Select(c => (Id: c.Id, Name: c.Name ?? string.Empty))
                .ToList();

            StatusMessage = "جاري قراءة إكسل...";
            var imported = await Task.Run(() => _excelService.ParseImportFile(path), ct);
            ct.ThrowIfCancellationRequested();

            StatusMessage = $"جاري المطابقة الدقيقة ({imported.Count:N0} صف)...";
            var matchResults = await Task.Run(() => BuildExactMatchResults(imported, customerNames), ct);
            ct.ThrowIfCancellationRequested();

            _customerIndex = customerNames
                .Select(c => (c.Id, c.Name, Compact: ArabicNameNormalizer.Compact(c.Name)))
                .Where(c => c.Compact.Length >= 2)
                .ToList();
            _prefixIndex = ArabicNameNormalizer.BuildPrefixIndex(_customerIndex);

            StatusMessage = "جاري عرض النتائج...";
            var builtRows = await Task.Run(() => CreateRowVms(matchResults), ct);
            ct.ThrowIfCancellationRequested();

            ReplaceAllRows(builtRows);
            RefreshStats();
            StatusMessage = $"تم تحميل {_allRows.Count:N0} صفاً — جاري تحسين الاقتراحات التقريبية...";
            IsBusy = false;

            _ = ApplyFuzzySuggestionsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "تم إلغاء التحميل";
            IsBusy = false;
        }
        catch (Exception ex)
        {
            var detail = ex is AggregateException agg
                ? string.Join(" | ", agg.Flatten().InnerExceptions.Select(e => e.Message).Distinct().Take(3))
                : ex.GetBaseException().Message;
            BeautifulMessageDialog.ShowError($"فشل قراءة الملف: {detail}");
            StatusMessage = "فشل تحميل الملف";
            IsBusy = false;
        }
    }

    private void ReplaceAllRows(List<PlatformDeductionRowVm> builtRows)
    {
        foreach (var old in _allRows)
            old.PropertyChanged -= OnRowPropertyChanged;

        _allRows = builtRows;
        foreach (var row in _allRows)
            row.PropertyChanged += OnRowPropertyChanged;

        RowsPager.ResetToFirstPage();
        _ = ShowCurrentPageAsync();
    }

    private static List<PlatformDeductionRowVm> CreateRowVms(List<PlatformMatchResult> matchResults)
    {
        var builtRows = new List<PlatformDeductionRowVm>(matchResults.Count);
        foreach (var result in matchResults)
        {
            var row = new PlatformDeductionRowVm(result.Source);
            row.ApplyMatch(
                result.Status,
                result.MatchLabel,
                result.CustomerId,
                result.CustomerName,
                result.IsSelected,
                result.Suggestions);
            builtRows.Add(row);
        }
        return builtRows;
    }

    private sealed class PlatformMatchResult
    {
        public required PlatformDeductionImportRow Source { get; init; }
        public PlatformDeductionMatchStatus Status { get; init; }
        public string MatchLabel { get; init; } = string.Empty;
        public int? CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public bool IsSelected { get; init; }
        public List<(int Id, string Name, double Score)> Suggestions { get; init; } = [];
    }

    private static List<PlatformMatchResult> BuildExactMatchResults(
        IReadOnlyList<PlatformDeductionImportRow> imported,
        List<(int Id, string Name)> customers)
    {
        if (imported.Count == 0)
            return [];

        var byCompact = new Dictionary<string, (int Id, string Name)>(customers.Count, StringComparer.Ordinal);
        foreach (var c in customers)
        {
            var key = ArabicNameNormalizer.Compact(c.Name);
            if (key.Length == 0) continue;
            if (!byCompact.ContainsKey(key))
                byCompact[key] = c;
        }

        var results = new PlatformMatchResult[imported.Count];
        Parallel.For(0, imported.Count, i =>
        {
            var item = imported[i];
            if (item.HasErrors || item.DeductedAmount <= 0)
            {
                results[i] = new PlatformMatchResult
                {
                    Source = item,
                    Status = PlatformDeductionMatchStatus.Invalid,
                    MatchLabel = "بيانات غير صالحة"
                };
                return;
            }

            var compact = ArabicNameNormalizer.Compact(item.CustomerName);
            if (compact.Length > 0 && byCompact.TryGetValue(compact, out var exact))
            {
                results[i] = new PlatformMatchResult
                {
                    Source = item,
                    Status = PlatformDeductionMatchStatus.Matched,
                    MatchLabel = "مطابق",
                    CustomerId = exact.Id,
                    CustomerName = exact.Name,
                    IsSelected = true
                };
                return;
            }

            results[i] = new PlatformMatchResult
            {
                Source = item,
                Status = PlatformDeductionMatchStatus.NotFound,
                MatchLabel = "غير موجود"
            };
        });

        return results.Select((r, i) => r ?? new PlatformMatchResult
        {
            Source = imported[i],
            Status = PlatformDeductionMatchStatus.NotFound,
            MatchLabel = "غير موجود"
        }).ToList();
    }

    private async Task ApplyFuzzySuggestionsAsync(CancellationToken ct)
    {
        try
        {
            if (_prefixIndex is null || _prefixIndex.Count == 0)
            {
                StatusMessage = $"تم تحميل {_allRows.Count:N0} صفاً — راجع المطابقة ثم حدّد الصفوف للتسديد";
                return;
            }

            var targets = _allRows
                .Where(r => r.MatchStatus == PlatformDeductionMatchStatus.NotFound)
                .ToList();

            if (targets.Count == 0)
            {
                StatusMessage = $"تم تحميل {_allRows.Count:N0} صفاً — راجع المطابقة ثم حدّد الصفوف للتسديد";
                return;
            }

            const int batchSize = 80;
            var suggested = 0;
            for (var offset = 0; offset < targets.Count; offset += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                var batch = targets.Skip(offset).Take(batchSize).ToList();

                var updates = await Task.Run(() =>
                {
                    var list = new List<(PlatformDeductionRowVm Row, List<(int Id, string Name, double Score)> Suggestions)>(batch.Count);
                    foreach (var row in batch)
                    {
                        var suggestions = ArabicNameNormalizer.FindSuggestionsWithPrefixIndex(
                            row.ExcelCustomerName, _prefixIndex!);
                        if (suggestions.Count > 0)
                            list.Add((row, suggestions.ToList()));
                    }
                    return list;
                }, ct);

                _suppressRowViewRefresh = true;
                try
                {
                    foreach (var (row, suggestions) in updates)
                    {
                        row.Suggestions.Clear();
                        foreach (var s in suggestions)
                            row.Suggestions.Add(new CustomerSuggestionVm(s.Id, s.Name, s.Score));
                        row.SelectedSuggestion = row.Suggestions[0];
                        row.MatchStatus = PlatformDeductionMatchStatus.Suggested;
                        row.MatchLabel = "اقتراح تقريبي";
                        suggested++;
                    }
                }
                finally
                {
                    _suppressRowViewRefresh = false;
                }

                StatusMessage =
                    $"تحسين الاقتراحات... {Math.Min(offset + batchSize, targets.Count):N0}/{targets.Count:N0}";
                await Task.Delay(1, ct);
            }

            RefreshStats();
            await ShowCurrentPageAsync();

            StatusMessage =
                $"تم تحميل {_allRows.Count:N0} صفاً — اقتراحات تقريبية: {suggested:N0} — راجع ثم سدّد";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = $"تم التحميل مع تحذير في الاقتراحات: {ex.Message}";
            RefreshStats();
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressRowViewRefresh)
            return;

        if (e.PropertyName is nameof(PlatformDeductionRowVm.IsSelected)
            or nameof(PlatformDeductionRowVm.MatchStatus)
            or nameof(PlatformDeductionRowVm.MatchedCustomerId)
            or nameof(PlatformDeductionRowVm.IsSettled))
        {
            RefreshStats();
            if ((FilterMode is "Selected" or "Matched" or "Suggested" or "NotFound")
                && e.PropertyName is not nameof(PlatformDeductionRowVm.IsSelected))
                _ = ShowCurrentPageAsync();
        }
    }

    private void RefreshStats()
    {
        TotalRows = _allRows.Count;
        MatchedCount = _allRows.Count(r => r.MatchStatus == PlatformDeductionMatchStatus.Matched);
        SuggestedCount = _allRows.Count(r => r.MatchStatus == PlatformDeductionMatchStatus.Suggested);
        NotFoundCount = _allRows.Count(r => r.MatchStatus is PlatformDeductionMatchStatus.NotFound or PlatformDeductionMatchStatus.Invalid);
        SelectedCount = _allRows.Count(r => r.IsSelected && r.CanSettle);
        SelectedAmount = _allRows.Where(r => r.IsSelected && r.CanSettle).Sum(r => r.DeductedAmount);
    }

    [RelayCommand]
    private void SetFilter(string? mode)
    {
        FilterMode = mode ?? "All";
    }

    [RelayCommand]
    private void SelectAllMatched()
    {
        _suppressRowViewRefresh = true;
        try
        {
            // تحديد كل المطابق القابل للتسديد عبر كل الصفحات
            foreach (var row in _allRows.Where(r => r.CanSettle))
                row.IsSelected = true;
        }
        finally
        {
            _suppressRowViewRefresh = false;
        }

        RefreshStats();
        if (FilterMode == "Selected")
            _ = ShowCurrentPageAsync();
        StatusMessage = $"تم تحديد {SelectedCount:N0} صفاً عبر كل الصفحات";
    }

    [RelayCommand]
    private void ClearSelection()
    {
        _suppressRowViewRefresh = true;
        try
        {
            foreach (var row in _allRows)
                row.IsSelected = false;
        }
        finally
        {
            _suppressRowViewRefresh = false;
        }

        RefreshStats();
        if (FilterMode == "Selected")
            _ = ShowCurrentPageAsync();
        StatusMessage = "تم إلغاء التحديد من كل الصفحات";
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
        _ = ShowCurrentPageAsync();
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

        // كل المحدد عبر كل الصفحات وليس الصفحة الحالية فقط
        var toPay = _allRows.Where(r => r.IsSelected && r.CanSettle).ToList();
        if (toPay.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد صفوف محددة قابلة للتسديد");
            return;
        }

        var totalAmount = toPay.Sum(r => r.DeductedAmount);
        if (!BeautifulMessageDialog.ShowConfirm(
                $"سيتم تسديد {toPay.Count:N0} صفاً (من كل الصفحات) بإجمالي {totalAmount:N2} د.ع عبر القاصة «{SelectedCashBox.Name}».\nهل تريد المتابعة؟"))
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
            await ShowCurrentPageAsync();
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

    public void ApplyMatch(
        PlatformDeductionMatchStatus status,
        string matchLabel,
        int? customerId,
        string? customerName,
        bool isSelected,
        IReadOnlyList<(int Id, string Name, double Score)> suggestions)
    {
        MatchStatus = status;
        MatchLabel = matchLabel;
        MatchedCustomerId = customerId;
        MatchedCustomerName = customerName;
        IsSelected = isSelected;
        Suggestions.Clear();
        foreach (var s in suggestions)
            Suggestions.Add(new CustomerSuggestionVm(s.Id, s.Name, s.Score));
        SelectedSuggestion = Suggestions.Count > 0 ? Suggestions[0] : null;
    }

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
