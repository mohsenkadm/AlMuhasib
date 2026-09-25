using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class CashBankViewModel : ViewModelBase
{
    private readonly ICashBankService _cashBankService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private readonly IFeatureFlagService _featureFlags;

    public CashBankViewModel(
        ICashBankService cashBankService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow,
        IFeatureFlagService featureFlags)
    {
        _cashBankService = cashBankService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        _featureFlags = featureFlags;
        PageTitle = "القاصات والمصرف";
        TransferPager.Bind(LoadTransfersAsync);
        ShowMultiCurrency = _featureFlags.MultiCurrency;
        SelectedCashBoxCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        SelectedBankCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        _featureFlags.FlagsChanged += (_, _) =>
            FeatureUiRefresh.Invoke(() => ShowMultiCurrency = _featureFlags.MultiCurrency);
    }

    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private CurrencyOption? _selectedCashBoxCurrencyOption;
    [ObservableProperty] private CurrencyOption? _selectedBankCurrencyOption;
    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);

    // ── Tab selection ──────────────────────────────────────
    [ObservableProperty]
    private int _selectedTabIndex;

    // ══════════════════════════════════════════════════════
    // TAB 0: CASH BOXES (القاصات)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty]
    private CashBox? _selectedCashBox;

    [ObservableProperty]
    private string _newCashBoxName = string.Empty;

    [ObservableProperty]
    private decimal _newCashBoxBalance;

    [ObservableProperty]
    private bool _isAddCashBoxVisible;

    [ObservableProperty]
    private bool _isEditCashBoxMode;

    [ObservableProperty]
    private int? _editingCashBoxId;

    public ObservableCollection<AccountTransactionRow> CashBoxTransactions { get; } = [];

    [ObservableProperty]
    private string _cashBoxSearchText = string.Empty;

    [ObservableProperty]
    private DateTime? _cashBoxFromDate;

    [ObservableProperty]
    private DateTime? _cashBoxToDate;

    [ObservableProperty]
    private int _cashBoxFilteredCount;

    [ObservableProperty]
    private decimal _cashBoxFilteredCredit;

    [ObservableProperty]
    private decimal _cashBoxFilteredDebit;

    [ObservableProperty]
    private decimal _cashBoxFilteredNet;

    [ObservableProperty]
    private bool _isCashBoxColumnFilterPanelOpen;

    private readonly List<AccountTransactionRow> _allCashBoxTransactions = [];
    private bool _isClearingCashBoxFilters;
    private int _cashBoxLoadGeneration;
    private bool _suppressAccountSelectionReload;

    // ══════════════════════════════════════════════════════
    // TAB 1: BANKS (المصارف)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<BankAccount> BankAccounts { get; } = [];

    [ObservableProperty]
    private BankAccount? _selectedBankAccount;

    [ObservableProperty]
    private string _newBankName = string.Empty;

    [ObservableProperty]
    private string _newBankAccountNumber = string.Empty;

    [ObservableProperty]
    private decimal _newBankBalance;

    [ObservableProperty]
    private bool _isAddBankVisible;

    [ObservableProperty]
    private bool _isEditBankMode;

    [ObservableProperty]
    private int? _editingBankId;

    [ObservableProperty]
    private bool _isAdjustBalanceVisible;

    [ObservableProperty]
    private bool _adjustIsBank;

    [ObservableProperty]
    private decimal _adjustAmount;

    [ObservableProperty]
    private string _adjustReason = string.Empty;

    [ObservableProperty]
    private DateTime _adjustDate = DateTime.Today;

    public bool ShowCashAdjustPanel => IsAdjustBalanceVisible && !AdjustIsBank;
    public bool ShowBankAdjustPanel => IsAdjustBalanceVisible && AdjustIsBank;

    partial void OnIsAdjustBalanceVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowCashAdjustPanel));
        OnPropertyChanged(nameof(ShowBankAdjustPanel));
    }

    partial void OnAdjustIsBankChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowCashAdjustPanel));
        OnPropertyChanged(nameof(ShowBankAdjustPanel));
    }

    public ObservableCollection<AccountTransactionRow> BankTransactions { get; } = [];

    [ObservableProperty]
    private string _bankSearchText = string.Empty;

    [ObservableProperty]
    private DateTime? _bankFromDate;

    [ObservableProperty]
    private DateTime? _bankToDate;

    [ObservableProperty]
    private int _bankFilteredCount;

    [ObservableProperty]
    private decimal _bankFilteredCredit;

    [ObservableProperty]
    private decimal _bankFilteredDebit;

    [ObservableProperty]
    private decimal _bankFilteredNet;

    [ObservableProperty]
    private bool _showUnreconciledOnly;

    [ObservableProperty]
    private bool _isBankColumnFilterPanelOpen;

    private readonly List<AccountTransactionRow> _allBankTransactions = [];
    private bool _isClearingBankFilters;
    private int _bankLoadGeneration;

    // ══════════════════════════════════════════════════════
    // TAB 2: TRANSFERS (التحويلات)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<TransferDisplayItem> Transfers { get; } = [];

    [ObservableProperty]
    private DateTime? _transferFromDate;

    [ObservableProperty]
    private DateTime? _transferToDate;

    // Source
    [ObservableProperty]
    private TransferAccountType _transferFromType;

    [ObservableProperty]
    private object? _transferFromAccount;

    public ObservableCollection<object> TransferFromAccounts { get; } = [];

    // Destination
    [ObservableProperty]
    private TransferAccountType _transferToType;

    [ObservableProperty]
    private object? _transferToAccount;

    public ObservableCollection<object> TransferToAccounts { get; } = [];

    [ObservableProperty]
    private decimal _transferAmount;

    [ObservableProperty]
    private string _transferNotes = string.Empty;

    public PagerState TransferPager { get; } = new() { PageSize = 20 };

    [ObservableProperty]
    private bool _isTransfersColumnFilterPanelOpen;

    // Source balance display
    [ObservableProperty]
    private string _transferFromBalanceText = string.Empty;

    // ══════════════════════════════════════════════════════
    // INITIALIZE
    // ══════════════════════════════════════════════════════
    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "CashAndBank");

            await LoadCashBoxesAsync();
            await LoadBankAccountsAsync();
            await RefreshTransferFromAccountsAsync();
            await RefreshTransferToAccountsAsync();
            await LoadTransfersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        _suppressAccountSelectionReload = true;
        try
        {
            var selectedCashBoxId = SelectedCashBox?.Id;
            var selectedBankId = SelectedBankAccount?.Id;

            await LoadCashBoxesAsync();
            await LoadBankAccountsAsync();
            await RefreshTransferFromAccountsAsync();
            await RefreshTransferToAccountsAsync();
            await LoadTransfersAsync();

            SelectedCashBox = selectedCashBoxId is int cashId
                ? CashBoxes.FirstOrDefault(c => c.Id == cashId)
                : null;
            SelectedBankAccount = selectedBankId is int bankId
                ? BankAccounts.FirstOrDefault(b => b.Id == bankId)
                : null;
        }
        finally
        {
            _suppressAccountSelectionReload = false;
            IsBusy = false;
        }

        // تحميل واحد للحركات بعد استعادة التحديد (يتجنب التكرار من OnSelected* المتزامن)
        if (SelectedCashBox is not null)
            await LoadCashBoxTransactionsAsync(SelectedCashBox.Id);
        else
        {
            _allCashBoxTransactions.Clear();
            CashBoxTransactions.Clear();
            ResetCashBoxStats();
        }

        if (SelectedBankAccount is not null)
            await LoadBankTransactionsAsync(SelectedBankAccount.Id);
        else
        {
            _allBankTransactions.Clear();
            BankTransactions.Clear();
            ResetBankStats();
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 0: CASH BOXES
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private async Task LoadCashBoxesAsync()
    {
        var items = await _cashBankService.GetAllCashBoxesAsync();
        CashBoxes.Clear();
        foreach (var cb in items)
            CashBoxes.Add(cb);
    }

    [RelayCommand]
    private void ShowAddCashBox()
    {
        IsEditCashBoxMode = false;
        EditingCashBoxId = null;
        NewCashBoxName = string.Empty;
        NewCashBoxBalance = 0;
        IsAddCashBoxVisible = true;
    }

    [RelayCommand]
    private void ShowEditCashBox()
    {
        if (SelectedCashBox is null || !CanEdit) return;
        IsEditCashBoxMode = true;
        EditingCashBoxId = SelectedCashBox.Id;
        NewCashBoxName = SelectedCashBox.Name;
        NewCashBoxBalance = SelectedCashBox.Balance;
        IsAddCashBoxVisible = true;
    }

    [RelayCommand]
    private void CancelAddCashBox()
    {
        IsAddCashBoxVisible = false;
        IsEditCashBoxMode = false;
        EditingCashBoxId = null;
    }

    [RelayCommand]
    private async Task SaveCashBoxAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCashBoxName))
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال اسم القاصة");
            return;
        }

        try
        {
            if (IsEditCashBoxMode && EditingCashBoxId.HasValue)
            {
                await _cashBankService.UpdateCashBoxAsync(EditingCashBoxId.Value, NewCashBoxName.Trim());
            }
            else
            {
                await _cashBankService.AddCashBoxAsync(
                    NewCashBoxName.Trim(),
                    NewCashBoxBalance,
                    ShowMultiCurrency
                        ? (SelectedCashBoxCurrencyOption?.Currency ?? AccountingCurrency.IQD)
                        : AccountingCurrency.IQD);
            }

            IsAddCashBoxVisible = false;
            IsEditCashBoxMode = false;
            EditingCashBoxId = null;
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteCashBoxAsync()
    {
        if (SelectedCashBox is null || !CanDelete) return;

        if (!BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف الصندوق «{SelectedCashBox.Name}»؟",
                "تأكيد الحذف"))
            return;

        try
        {
            await _cashBankService.DeleteCashBoxAsync(SelectedCashBox.Id);
            SelectedCashBox = null;
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnSelectedCashBoxChanged(CashBox? value)
    {
        if (_suppressAccountSelectionReload) return;

        if (value is not null)
            _ = LoadCashBoxTransactionsAsync(value.Id);
        else
        {
            _cashBoxLoadGeneration++;
            _allCashBoxTransactions.Clear();
            CashBoxTransactions.Clear();
            ResetCashBoxStats();
        }
    }

    partial void OnCashBoxSearchTextChanged(string value)
    {
        if (!_isClearingCashBoxFilters)
            ApplyCashBoxFilters();
    }

    partial void OnCashBoxFromDateChanged(DateTime? value)
    {
        if (!_isClearingCashBoxFilters)
            ApplyCashBoxFilters();
    }

    partial void OnCashBoxToDateChanged(DateTime? value)
    {
        if (!_isClearingCashBoxFilters)
            ApplyCashBoxFilters();
    }

    [RelayCommand]
    private void ClearCashBoxFilters()
    {
        _isClearingCashBoxFilters = true;
        CashBoxSearchText = string.Empty;
        CashBoxFromDate = null;
        CashBoxToDate = null;
        _isClearingCashBoxFilters = false;
        ApplyCashBoxFilters();
    }

    private async Task LoadCashBoxTransactionsAsync(int cashBoxId)
    {
        var generation = ++_cashBoxLoadGeneration;

        _isClearingCashBoxFilters = true;
        CashBoxSearchText = string.Empty;
        CashBoxFromDate = null;
        CashBoxToDate = null;
        _isClearingCashBoxFilters = false;

        _allCashBoxTransactions.Clear();
        CashBoxTransactions.Clear();
        ResetCashBoxStats();

        try
        {
            var entries = await _cashBankService.GetCashBoxStatementAsync(cashBoxId);
            if (generation != _cashBoxLoadGeneration) return;

            _allCashBoxTransactions.Clear();
            foreach (var e in entries)
                _allCashBoxTransactions.Add(MapEntry(e));

            ApplyCashBoxFilters();
        }
        catch (Exception ex)
        {
            if (generation != _cashBoxLoadGeneration) return;
            BeautifulMessageDialog.ShowError($"خطأ في تحميل الحركات: {ex.Message}");
        }
    }

    private static AccountTransactionRow MapEntry(AccountStatementEntry e) => new()
    {
        Date = e.Date,
        Type = e.Type,
        Description = e.Description,
        PartyName = e.PartyName,
        Credit = e.Credit,
        Debit = e.Debit,
        RunningBalance = e.RunningBalance,
        Reference = e.Reference,
        VoucherId = e.VoucherId,
        IsReconciled = e.IsReconciled,
        IsVoucher = e.SourceType == "Voucher",
        SourceType = e.SourceType,
        SourceId = e.SourceId,
        CanReverse = e.CanReverse
    };

    private void ApplyCashBoxFilters()
    {
        IEnumerable<AccountTransactionRow> query = _allCashBoxTransactions;

        if (CashBoxFromDate.HasValue)
            query = query.Where(t => t.Date.Date >= CashBoxFromDate.Value.Date);

        if (CashBoxToDate.HasValue)
            query = query.Where(t => t.Date.Date <= CashBoxToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(CashBoxSearchText))
        {
            var term = CashBoxSearchText.Trim();
            query = query.Where(t =>
                t.Type.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Reference.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.PartyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (t.Description ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        CashBoxTransactions.Clear();
        foreach (var item in list)
            CashBoxTransactions.Add(item);

        CashBoxFilteredCount = list.Count;
        CashBoxFilteredCredit = list.Sum(t => t.Credit);
        CashBoxFilteredDebit = list.Sum(t => t.Debit);
        CashBoxFilteredNet = CashBoxFilteredCredit - CashBoxFilteredDebit;
    }

    private void ResetCashBoxStats()
    {
        CashBoxFilteredCount = 0;
        CashBoxFilteredCredit = 0;
        CashBoxFilteredDebit = 0;
        CashBoxFilteredNet = 0;
    }

    // ══════════════════════════════════════════════════════
    // TAB 1: BANKS
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private async Task LoadBankAccountsAsync()
    {
        var items = await _cashBankService.GetAllBankAccountsAsync();
        BankAccounts.Clear();
        foreach (var b in items)
            BankAccounts.Add(b);
    }

    [RelayCommand]
    private void ShowAddBank()
    {
        IsEditBankMode = false;
        EditingBankId = null;
        NewBankName = string.Empty;
        NewBankAccountNumber = string.Empty;
        NewBankBalance = 0;
        IsAddBankVisible = true;
    }

    [RelayCommand]
    private void ShowEditBank()
    {
        if (SelectedBankAccount is null || !CanEdit) return;
        IsEditBankMode = true;
        EditingBankId = SelectedBankAccount.Id;
        NewBankName = SelectedBankAccount.Name;
        NewBankAccountNumber = SelectedBankAccount.AccountNumber ?? string.Empty;
        NewBankBalance = SelectedBankAccount.Balance;
        IsAddBankVisible = true;
    }

    [RelayCommand]
    private void CancelAddBank()
    {
        IsAddBankVisible = false;
        IsEditBankMode = false;
        EditingBankId = null;
    }

    [RelayCommand]
    private async Task SaveBankAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBankName))
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال اسم المصرف");
            return;
        }

        try
        {
            string? accNum = string.IsNullOrWhiteSpace(NewBankAccountNumber) ? null : NewBankAccountNumber.Trim();
            if (IsEditBankMode && EditingBankId is int id)
                await _cashBankService.UpdateBankAccountAsync(id, NewBankName.Trim(), accNum);
            else
                await _cashBankService.AddBankAccountAsync(
                    NewBankName.Trim(),
                    accNum,
                    NewBankBalance,
                    ShowMultiCurrency
                        ? (SelectedBankCurrencyOption?.Currency ?? AccountingCurrency.IQD)
                        : AccountingCurrency.IQD);

            IsAddBankVisible = false;
            IsEditBankMode = false;
            EditingBankId = null;
            await LoadBankAccountsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteBankAsync()
    {
        if (SelectedBankAccount is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف المصرف «{SelectedBankAccount.Name}»؟",
                "حذف مصرف"))
            return;

        try
        {
            await _cashBankService.DeleteBankAccountAsync(SelectedBankAccount.Id);
            SelectedBankAccount = null;
            await LoadBankAccountsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ShowAdjustCashBox()
    {
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تسوية الأرصدة");
            return;
        }
        if (SelectedCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر صندوقاً أولاً");
            return;
        }
        AdjustIsBank = false;
        AdjustAmount = 0;
        AdjustReason = string.Empty;
        AdjustDate = DateTime.Today;
        IsAdjustBalanceVisible = true;
    }

    [RelayCommand]
    private void ShowAdjustBank()
    {
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تسوية الأرصدة");
            return;
        }
        if (SelectedBankAccount is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر مصرفاً أولاً");
            return;
        }
        AdjustIsBank = true;
        AdjustAmount = 0;
        AdjustReason = string.Empty;
        AdjustDate = DateTime.Today;
        IsAdjustBalanceVisible = true;
    }

    [RelayCommand]
    private void CancelAdjustBalance() => IsAdjustBalanceVisible = false;

    [RelayCommand]
    private async Task SaveAdjustBalanceAsync()
    {
        if (AdjustAmount == 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغ التسوية (موجب لزيادة / سالب لنقصان)");
            return;
        }
        if (string.IsNullOrWhiteSpace(AdjustReason))
        {
            BeautifulMessageDialog.ShowWarning("سبب التسوية إلزامي");
            return;
        }

        try
        {
            if (AdjustIsBank)
            {
                if (SelectedBankAccount is null) return;
                await _cashBankService.AdjustBankBalanceAsync(
                    SelectedBankAccount.Id, AdjustAmount, AdjustReason.Trim(), AdjustDate);
                await LoadBankAccountsAsync();
                await LoadBankTransactionsAsync(SelectedBankAccount.Id);
            }
            else
            {
                if (SelectedCashBox is null) return;
                await _cashBankService.AdjustCashBoxBalanceAsync(
                    SelectedCashBox.Id, AdjustAmount, AdjustReason.Trim(), AdjustDate);
                await LoadCashBoxesAsync();
                await LoadCashBoxTransactionsAsync(SelectedCashBox.Id);
            }

            IsAdjustBalanceVisible = false;
            BeautifulMessageDialog.ShowSuccess("تم تسجيل تسوية الرصيد");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnSelectedBankAccountChanged(BankAccount? value)
    {
        if (_suppressAccountSelectionReload) return;

        if (value is not null)
            _ = LoadBankTransactionsAsync(value.Id);
        else
        {
            _bankLoadGeneration++;
            _allBankTransactions.Clear();
            BankTransactions.Clear();
            ResetBankStats();
        }
    }

    partial void OnBankSearchTextChanged(string value)
    {
        if (!_isClearingBankFilters)
            ApplyBankFilters();
    }

    partial void OnBankFromDateChanged(DateTime? value)
    {
        if (!_isClearingBankFilters)
            ApplyBankFilters();
    }

    partial void OnBankToDateChanged(DateTime? value)
    {
        if (!_isClearingBankFilters)
            ApplyBankFilters();
    }

    [RelayCommand]
    private void ClearBankFilters()
    {
        _isClearingBankFilters = true;
        BankSearchText = string.Empty;
        BankFromDate = null;
        BankToDate = null;
        ShowUnreconciledOnly = false;
        _isClearingBankFilters = false;
        ApplyBankFilters();
    }

    private async Task LoadBankTransactionsAsync(int bankAccountId)
    {
        var generation = ++_bankLoadGeneration;

        _isClearingBankFilters = true;
        BankSearchText = string.Empty;
        BankFromDate = null;
        BankToDate = null;
        _isClearingBankFilters = false;

        _allBankTransactions.Clear();
        BankTransactions.Clear();
        ResetBankStats();

        try
        {
            var entries = await _cashBankService.GetBankStatementAsync(bankAccountId);
            if (generation != _bankLoadGeneration) return;

            _allBankTransactions.Clear();
            foreach (var e in entries)
                _allBankTransactions.Add(MapEntry(e));

            ApplyBankFilters();
        }
        catch (Exception ex)
        {
            if (generation != _bankLoadGeneration) return;
            BeautifulMessageDialog.ShowError($"خطأ في تحميل الحركات: {ex.Message}");
        }
    }

    private void ApplyBankFilters()
    {
        IEnumerable<AccountTransactionRow> query = _allBankTransactions;

        if (BankFromDate.HasValue)
            query = query.Where(t => t.Date.Date >= BankFromDate.Value.Date);

        if (BankToDate.HasValue)
            query = query.Where(t => t.Date.Date <= BankToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(BankSearchText))
        {
            var term = BankSearchText.Trim();
            query = query.Where(t =>
                t.Type.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Reference.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.PartyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (t.Description ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ShowUnreconciledOnly)
            query = query.Where(t => t.IsVoucher && !t.IsReconciled);

        var list = query.ToList();
        BankTransactions.Clear();
        foreach (var item in list)
            BankTransactions.Add(item);

        BankFilteredCount = list.Count;
        BankFilteredCredit = list.Sum(t => t.Credit);
        BankFilteredDebit = list.Sum(t => t.Debit);
        BankFilteredNet = BankFilteredCredit - BankFilteredDebit;
    }

    partial void OnShowUnreconciledOnlyChanged(bool value) => ApplyBankFilters();

    [RelayCommand]
    private async Task ToggleReconcileAsync(AccountTransactionRow? row)
    {
        if (row is null || !row.IsVoucher || row.VoucherId is not int voucherId)
            return;

        try
        {
            var newValue = !row.IsReconciled;
            await _cashBankService.SetVoucherReconciledAsync(voucherId, newValue);
            row.IsReconciled = newValue;
            ApplyBankFilters();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private void ResetBankStats()
    {
        BankFilteredCount = 0;
        BankFilteredCredit = 0;
        BankFilteredDebit = 0;
        BankFilteredNet = 0;
    }

    // ══════════════════════════════════════════════════════
    // TAB 2: TRANSFERS
    // ══════════════════════════════════════════════════════
    partial void OnTransferFromTypeChanged(TransferAccountType value)
    {
        _ = RefreshTransferFromAccountsAsync();
    }

    partial void OnTransferToTypeChanged(TransferAccountType value)
    {
        _ = RefreshTransferToAccountsAsync();
    }

    partial void OnTransferFromAccountChanged(object? value)
    {
        UpdateFromBalanceText();
    }

    private void UpdateFromBalanceText()
    {
        TransferFromBalanceText = TransferFromAccount switch
        {
            CashBox cb => $"الرصيد: {cb.Balance:N0}",
            BankAccount ba => $"الرصيد: {ba.Balance:N0}",
            _ => string.Empty
        };
    }

    private async Task RefreshTransferFromAccountsAsync()
    {
        try
        {
            TransferFromAccounts.Clear();
            TransferFromAccount = null;
            if (TransferFromType == TransferAccountType.CashBox)
            {
                foreach (var cb in CashBoxes)
                    TransferFromAccounts.Add(cb);
            }
            else
            {
                var banks = await _cashBankService.GetAllBankAccountsAsync();
                foreach (var b in banks)
                    TransferFromAccounts.Add(b);
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ: {ex.Message}");
        }
    }

    private async Task RefreshTransferToAccountsAsync()
    {
        try
        {
            TransferToAccounts.Clear();
            TransferToAccount = null;
            if (TransferToType == TransferAccountType.CashBox)
            {
                foreach (var cb in CashBoxes)
                    TransferToAccounts.Add(cb);
            }
            else
            {
                var banks = await _cashBankService.GetAllBankAccountsAsync();
                foreach (var b in banks)
                    TransferToAccounts.Add(b);
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateTransferAsync()
    {
        if (TransferFromAccount is null || TransferToAccount is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار المصدر والهدف");
            return;
        }
        if (TransferAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال مبلغ صحيح");
            return;
        }

        int fromId = TransferFromAccount is CashBox fromCb ? fromCb.Id : ((BankAccount)TransferFromAccount).Id;
        int toId = TransferToAccount is CashBox toCb ? toCb.Id : ((BankAccount)TransferToAccount).Id;

        // Prevent same-account transfer
        if (TransferFromType == TransferToType && fromId == toId)
        {
            BeautifulMessageDialog.ShowWarning("لا يمكن التحويل لنفس الحساب");
            return;
        }

        IsBusy = true;
        try
        {
            await _cashBankService.CreateTransferAsync(
                TransferFromType, fromId, TransferToType, toId, TransferAmount,
                string.IsNullOrWhiteSpace(TransferNotes) ? null : TransferNotes.Trim());

            TransferAmount = 0;
            TransferNotes = string.Empty;
            TransferFromAccount = null;
            TransferToAccount = null;

            // Refresh all data
            await LoadCashBoxesAsync();
            await LoadBankAccountsAsync();
            await LoadTransfersAsync();

            BeautifulMessageDialog.ShowSuccess("تم التحويل بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadTransfersAsync()
    {
        var (items, totalCount) = await _cashBankService.GetPagedTransfersAsync(
            TransferPager.CurrentPage, TransferPager.PageSize,
            fromDate: TransferFromDate, toDate: TransferToDate);

        Transfers.Clear();
        foreach (var t in items)
            Transfers.Add(t);

        TransferPager.ApplyStats(totalCount);
    }

    partial void OnTransferFromDateChanged(DateTime? value)
    {
        TransferPager.CurrentPage = 1;
        _ = LoadTransfersAsync();
    }

    partial void OnTransferToDateChanged(DateTime? value)
    {
        TransferPager.CurrentPage = 1;
        _ = LoadTransfersAsync();
    }

    [RelayCommand]
    private async Task ReverseTransferAsync(TransferDisplayItem? item)
    {
        if (item is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm(
                $"عكس التحويل بمبلغ {item.Amount:N0} من «{item.FromName}» إلى «{item.ToName}»؟",
                "عكس تحويل"))
            return;

        try
        {
            await _cashBankService.ReverseTransferAsync(item.Id);
            await LoadCashBoxesAsync();
            await LoadBankAccountsAsync();
            await LoadTransfersAsync();
            if (SelectedCashBox is not null)
                await LoadCashBoxTransactionsAsync(SelectedCashBox.Id);
            if (SelectedBankAccount is not null)
                await LoadBankTransactionsAsync(SelectedBankAccount.Id);
            BeautifulMessageDialog.ShowSuccess("تم عكس التحويل");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReverseMovementAsync(AccountTransactionRow? row)
    {
        if (row is null || !row.CanReverse || !CanDelete) return;

        if (row.SourceType == "Voucher" && row.VoucherId is int voucherId)
        {
            if (!BeautifulMessageDialog.ShowConfirm(
                    $"حذف السند {row.Reference} وعكس أثره المحاسبي؟",
                    "عكس سند"))
                return;

            try
            {
                await _cashBankService.DeleteVoucherAsync(voucherId);
                await LoadCashBoxesAsync();
                await LoadBankAccountsAsync();
                if (SelectedCashBox is not null)
                    await LoadCashBoxTransactionsAsync(SelectedCashBox.Id);
                if (SelectedBankAccount is not null)
                    await LoadBankTransactionsAsync(SelectedBankAccount.Id);
                BeautifulMessageDialog.ShowSuccess("تم عكس السند");
            }
            catch (Exception ex)
            {
                BeautifulMessageDialog.ShowError(ex.Message);
            }
            return;
        }

        if (row.SourceType == "Transfer" && row.SourceId is int transferId)
        {
            await ReverseTransferAsync(Transfers.FirstOrDefault(t => t.Id == transferId)
                ?? new TransferDisplayItem { Id = transferId, Amount = row.Credit + row.Debit, FromName = "?", ToName = "?" });
        }
    }

    [RelayCommand]
    private async Task OpenSourceDocumentAsync(AccountTransactionRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.SourceType)) return;

        switch (row.SourceType)
        {
            case "Voucher":
                await _mainWindow.OpenTabAsync(typeof(VouchersViewModel), "السندات", PackIconKind.FileDocument);
                break;
            case "Invoice":
                await _mainWindow.OpenTabAsync(typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister);
                break;
            case "Expense":
                await _mainWindow.OpenTabAsync(typeof(ExpenseViewModel), "المصروفات", PackIconKind.CashMinus);
                break;
            case "Installment":
                await _mainWindow.OpenTabAsync(typeof(InstallmentsViewModel), "الأقساط", PackIconKind.CalendarClock);
                break;
            case "Transfer":
                SelectedTabIndex = 2;
                break;
        }
    }

    private static string GetVoucherTypeName(VoucherType type) => type switch
    {
        VoucherType.Receipt => "سند قبض",
        VoucherType.Payment => "سند صرف",
        VoucherType.BankReceipt => "قبض مصرفي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "قبض دين",
        _ => "سند"
    };

    // ══════════════════════════════════════════════════════
    // EXPORT & PRINT
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void ExportCashBoxTransactions()
    {
        if (CashBoxTransactions.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "حركات_الصندوق.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "المرجع", "الطرف", "دائن", "مدين", "الرصيد", "الوصف" };
        var rows = CashBoxTransactions.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            t.Type,
            t.Reference ?? "",
            t.PartyName ?? "",
            t.Credit,
            t.Debit,
            t.RunningBalance,
            t.Description ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "حركات الصندوق", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintCashBoxTransactions()
    {
        if (CashBoxTransactions.Count == 0) return;
        var cols = new[] { "التاريخ", "النوع", "المرجع", "الطرف", "دائن", "مدين", "الرصيد", "الوصف" };
        var rows = CashBoxTransactions.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            t.Type,
            t.Reference ?? "",
            t.PartyName ?? "",
            t.Credit.ToString("N0"),
            t.Debit.ToString("N0"),
            t.RunningBalance.ToString("N0"),
            t.Description ?? ""
        }).ToList();
        _exportService.PrintTable("حركات الصندوق", cols, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportBankTransactions()
    {
        if (BankTransactions.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "حركات_البنك.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "المرجع", "الطرف", "دائن", "مدين", "الرصيد", "الوصف" };
        var rows = BankTransactions.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            t.Type,
            t.Reference ?? "",
            t.PartyName ?? "",
            t.Credit,
            t.Debit,
            t.RunningBalance,
            t.Description ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "حركات البنك", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintBankTransactions()
    {
        if (BankTransactions.Count == 0) return;
        var cols = new[] { "التاريخ", "النوع", "المرجع", "الطرف", "دائن", "مدين", "الرصيد", "الوصف" };
        var rows = BankTransactions.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            t.Type,
            t.Reference ?? "",
            t.PartyName ?? "",
            t.Credit.ToString("N0"),
            t.Debit.ToString("N0"),
            t.RunningBalance.ToString("N0"),
            t.Description ?? ""
        }).ToList();
        _exportService.PrintTable("حركات البنك", cols, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportCashBoxes()
    {
        if (CashBoxes.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "القاصات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "اسم القاصة", "الرصيد" };
        var rows = CashBoxes.Select(cb => new object[] { cb.Name, cb.Balance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "القاصات", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintCashBoxes()
    {
        if (CashBoxes.Count == 0) return;
        var cols = new[] { "اسم القاصة", "الرصيد" };
        var rows = CashBoxes.Select(cb => new object[] { cb.Name, cb.Balance.ToString("N0") }).ToList();
        _exportService.PrintTable("القاصات", cols, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportBankAccounts()
    {
        if (BankAccounts.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "المصارف.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "اسم المصرف", "رقم الحساب", "الرصيد" };
        var rows = BankAccounts.Select(b => new object[] { b.Name, b.AccountNumber ?? "", b.Balance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المصارف", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintBankAccounts()
    {
        if (BankAccounts.Count == 0) return;
        var cols = new[] { "اسم المصرف", "رقم الحساب", "الرصيد" };
        var rows = BankAccounts.Select(b => new object[] { b.Name, b.AccountNumber ?? "", b.Balance.ToString("N0") }).ToList();
        _exportService.PrintTable("المصارف", cols, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportTransfers()
    {
        if (Transfers.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "التحويلات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "من", "إلى", "المبلغ", "ملاحظات" };
        var rows = Transfers.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            $"{t.FromTypeLabel}: {t.FromName}",
            $"{t.ToTypeLabel}: {t.ToName}",
            t.Amount,
            t.Notes ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "التحويلات", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTransfers()
    {
        if (Transfers.Count == 0) return;
        var cols = new[] { "التاريخ", "من", "إلى", "المبلغ", "ملاحظات" };
        var rows = Transfers.Select(t => new object[]
        {
            t.Date.ToString("yyyy/MM/dd"),
            $"{t.FromTypeLabel}: {t.FromName}",
            $"{t.ToTypeLabel}: {t.ToName}",
            t.Amount.ToString("N0"),
            t.Notes ?? ""
        }).ToList();
        _exportService.PrintTable("التحويلات", cols, (IList<object[]>)rows);
    }
}
