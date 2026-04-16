using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CashBankViewModel : ViewModelBase
{
    private readonly ICashBankService _cashBankService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public CashBankViewModel(ICashBankService cashBankService, IUnitOfWork unitOfWork, IExportService exportService, ICurrentUserService currentUserService)
    {
        _cashBankService = cashBankService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "القاصات والمصرف";
    }

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

    public ObservableCollection<AccountTransactionRow> CashBoxTransactions { get; } = [];

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

    public ObservableCollection<AccountTransactionRow> BankTransactions { get; } = [];

    // ══════════════════════════════════════════════════════
    // TAB 2: TRANSFERS (التحويلات)
    // ══════════════════════════════════════════════════════
    public ObservableCollection<Transfer> Transfers { get; } = [];

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

    [ObservableProperty]
    private int _transferCurrentPage = 1;

    [ObservableProperty]
    private int _transferTotalPages = 1;

    private const int TransferPageSize = 20;

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
        NewCashBoxName = string.Empty;
        NewCashBoxBalance = 0;
        IsAddCashBoxVisible = true;
    }

    [RelayCommand]
    private void CancelAddCashBox()
    {
        IsAddCashBoxVisible = false;
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
            await _cashBankService.AddCashBoxAsync(NewCashBoxName.Trim(), NewCashBoxBalance);
            IsAddCashBoxVisible = false;
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnSelectedCashBoxChanged(CashBox? value)
    {
        if (value is not null)
            _ = LoadCashBoxTransactionsAsync(value.Id);
        else
            CashBoxTransactions.Clear();
    }

    private async Task LoadCashBoxTransactionsAsync(int cashBoxId)
    {
        CashBoxTransactions.Clear();

        try
        {
            var vouchers = await _cashBankService.GetVouchersByCashBoxAsync(cashBoxId);
            foreach (var v in vouchers)
            {
                bool isIncome = v.VoucherType is VoucherType.Receipt or VoucherType.DebtReceipt
                    or VoucherType.InvestorDeposit or VoucherType.BankReceipt;

                decimal credit = isIncome ? (v.VoucherType == VoucherType.BankReceipt ? v.Amount - v.BankFees : v.Amount) : 0;
                decimal debit = !isIncome ? v.Amount : 0;

                CashBoxTransactions.Add(new AccountTransactionRow
                {
                    Date = v.Date,
                    Type = GetVoucherTypeName(v.VoucherType),
                    Description = v.Notes ?? string.Empty,
                    Credit = credit,
                    Debit = debit,
                    Reference = v.VoucherNumber
                });
            }

            var transfers = await _cashBankService.GetTransfersByCashBoxAsync(cashBoxId);
            foreach (var t in transfers)
            {
                bool isIncoming = t.ToType == TransferAccountType.CashBox && t.ToId == cashBoxId;
                CashBoxTransactions.Add(new AccountTransactionRow
                {
                    Date = t.Date,
                    Type = "تحويل",
                    Description = t.Notes ?? string.Empty,
                    Credit = isIncoming ? t.Amount : 0,
                    Debit = !isIncoming ? t.Amount : 0,
                    Reference = $"TRF-{t.Id:D4}"
                });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ في تحميل الحركات: {ex.Message}");
        }
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
        NewBankName = string.Empty;
        NewBankAccountNumber = string.Empty;
        NewBankBalance = 0;
        IsAddBankVisible = true;
    }

    [RelayCommand]
    private void CancelAddBank()
    {
        IsAddBankVisible = false;
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
            await _cashBankService.AddBankAccountAsync(NewBankName.Trim(), accNum, NewBankBalance);
            IsAddBankVisible = false;
            await LoadBankAccountsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    partial void OnSelectedBankAccountChanged(BankAccount? value)
    {
        if (value is not null)
            _ = LoadBankTransactionsAsync(value.Id);
        else
            BankTransactions.Clear();
    }

    private async Task LoadBankTransactionsAsync(int bankAccountId)
    {
        BankTransactions.Clear();

        try
        {
            var vouchers = await _cashBankService.GetVouchersByBankAsync(bankAccountId);
            foreach (var v in vouchers)
            {
                BankTransactions.Add(new AccountTransactionRow
                {
                    Date = v.Date,
                    Type = GetVoucherTypeName(v.VoucherType),
                    Description = v.Notes ?? string.Empty,
                    Credit = 0,
                    Debit = v.Amount,
                    Reference = v.VoucherNumber
                });
            }

            var transfers = await _cashBankService.GetTransfersByBankAsync(bankAccountId);
            foreach (var t in transfers)
            {
                bool isIncoming = t.ToType == TransferAccountType.Bank && t.ToId == bankAccountId;
                BankTransactions.Add(new AccountTransactionRow
                {
                    Date = t.Date,
                    Type = "تحويل",
                    Description = t.Notes ?? string.Empty,
                    Credit = isIncoming ? t.Amount : 0,
                    Debit = !isIncoming ? t.Amount : 0,
                    Reference = $"TRF-{t.Id:D4}"
                });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ في تحميل الحركات: {ex.Message}");
        }
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
            TransferCurrentPage, TransferPageSize);

        Transfers.Clear();
        foreach (var t in items)
            Transfers.Add(t);

        TransferTotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / TransferPageSize));
    }

    [RelayCommand]
    private async Task TransferNextPage()
    {
        if (TransferCurrentPage < TransferTotalPages)
        {
            TransferCurrentPage++;
            await LoadTransfersAsync();
        }
    }

    [RelayCommand]
    private async Task TransferPreviousPage()
    {
        if (TransferCurrentPage > 1)
        {
            TransferCurrentPage--;
            await LoadTransfersAsync();
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
            $"{(t.FromType == TransferAccountType.CashBox ? "قاصة" : "مصرف")} #{t.FromId}",
            $"{(t.ToType == TransferAccountType.CashBox ? "قاصة" : "مصرف")} #{t.ToId}",
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
            $"{(t.FromType == TransferAccountType.CashBox ? "قاصة" : "مصرف")} #{t.FromId}",
            $"{(t.ToType == TransferAccountType.CashBox ? "قاصة" : "مصرف")} #{t.ToId}",
            t.Amount.ToString("N0"),
            t.Notes ?? ""
        }).ToList();
        _exportService.PrintTable("التحويلات", cols, (IList<object[]>)rows);
    }
}
