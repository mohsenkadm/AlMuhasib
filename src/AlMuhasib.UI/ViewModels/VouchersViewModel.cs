using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class VouchersViewModel : ViewModelBase, IInvestorLookupHost
{
    private readonly ICashBankService _cashBankService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public VouchersViewModel(ICashBankService cashBankService, IUnitOfWork unitOfWork, IExportService exportService, ICurrentUserService currentUserService)
    {
        _cashBankService = cashBankService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "السندات";
    }

    // ── Create form ────────────────────────────────────────
    [ObservableProperty]
    private VoucherType _selectedVoucherType;

    [ObservableProperty]
    private string _voucherNumber = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private decimal _bankFees;

    [ObservableProperty]
    private string _netAmountText = string.Empty;

    [ObservableProperty]
    private DateTime _voucherDate = DateTime.Now;

    [ObservableProperty]
    private string _notes = string.Empty;

    // Selections
    [ObservableProperty]
    private CashBox? _selectedCashBox;

    [ObservableProperty]
    private BankAccount? _selectedBankAccount;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private Investor? _selectedInvestor;

    // Collections
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ObservableCollection<BankAccount> BankAccounts { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Investor> Investors { get; } = [];

    // ── Visibility flags per voucher type ──────────────────
    [ObservableProperty]
    private bool _showCustomerField;

    [ObservableProperty]
    private bool _showInvestorField;

    [ObservableProperty]
    private bool _showBankField;

    [ObservableProperty]
    private bool _showBankFeesField;

    // ── Voucher list (all types) ───────────────────────────
    public ObservableCollection<Voucher> Vouchers { get; } = [];

    [ObservableProperty]
    private VoucherType? _filterType;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount;

    private const int PageSize = 20;

    // ── Voucher type items for the ComboBox ────────────────
    public List<VoucherTypeItem> VoucherTypes { get; } =
    [
        new("سند قبض", VoucherType.Receipt),
        new("سند صرف", VoucherType.Payment),
        new("سند قبض مصرفي", VoucherType.BankReceipt),
        new("إيداع مستثمر", VoucherType.InvestorDeposit),
        new("سحب مستثمر", VoucherType.InvestorWithdrawal),
        new("سند قبض دين", VoucherType.DebtReceipt),
    ];

    // ══════════════════════════════════════════════════════
    // INITIALIZE
    // ══════════════════════════════════════════════════════
    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Vouchers");

            await LoadLookupsAsync();
            UpdateFieldVisibility(SelectedVoucherType);
            await GenerateVoucherNumberAsync();
            await LoadVouchersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        var cashBoxes = await _cashBankService.GetAllCashBoxesAsync();
        CashBoxes.Clear();
        foreach (var cb in cashBoxes)
            CashBoxes.Add(cb);

        var banks = await _cashBankService.GetAllBankAccountsAsync();
        BankAccounts.Clear();
        foreach (var b in banks)
            BankAccounts.Add(b);

        var customers = await _unitOfWork.Customers.GetAllAsync();
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);

        await RefreshInvestorsAsync();

        if (CashBoxes.Count > 0)
            SelectedCashBox = CashBoxes[0];
    }

    public async Task RefreshInvestorsAsync()
    {
        var selectedId = SelectedInvestor?.Id;
        var investors = await _unitOfWork.Investors.GetAllAsync();
        Investors.Clear();
        foreach (var inv in investors)
            Investors.Add(inv);
        if (selectedId is int id)
            SelectedInvestor = Investors.FirstOrDefault(i => i.Id == id);
        await Task.CompletedTask;
    }

    // ══════════════════════════════════════════════════════
    // VOUCHER TYPE CHANGED → update form fields
    // ══════════════════════════════════════════════════════
    partial void OnSelectedVoucherTypeChanged(VoucherType value)
    {
        UpdateFieldVisibility(value);
        _ = GenerateVoucherNumberAsync();
    }

    private void UpdateFieldVisibility(VoucherType type)
    {
        ShowCustomerField = type is VoucherType.Receipt or VoucherType.Payment or VoucherType.DebtReceipt;
        ShowInvestorField = type is VoucherType.InvestorDeposit or VoucherType.InvestorWithdrawal;
        ShowBankField = type is VoucherType.BankReceipt;
        ShowBankFeesField = type is VoucherType.BankReceipt;

        // Clear unrelated selections
        if (!ShowCustomerField) SelectedCustomer = null;
        if (!ShowInvestorField) SelectedInvestor = null;
        if (!ShowBankField) SelectedBankAccount = null;
        if (!ShowBankFeesField) BankFees = 0;

        UpdateNetAmountText();
    }

    partial void OnAmountChanged(decimal value) => UpdateNetAmountText();
    partial void OnBankFeesChanged(decimal value) => UpdateNetAmountText();

    private void UpdateNetAmountText()
    {
        if (ShowBankFeesField && BankFees > 0)
            NetAmountText = $"صافي المبلغ: {(Amount - BankFees):N0}";
        else
            NetAmountText = string.Empty;
    }

    private async Task GenerateVoucherNumberAsync()
    {
        try
        {
            VoucherNumber = await _cashBankService.GetNextVoucherNumberAsync(SelectedVoucherType);
        }
        catch
        {
            VoucherNumber = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════
    // CREATE VOUCHER
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private async Task CreateVoucherAsync()
    {
        // Validation
        if (Amount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال مبلغ صحيح");
            return;
        }
        if (SelectedCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار القاصة");
            return;
        }
        if (ShowCustomerField && SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار العميل");
            return;
        }
        if (ShowInvestorField && SelectedInvestor is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار المستثمر");
            return;
        }
        if (ShowBankField && SelectedBankAccount is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار المصرف");
            return;
        }
        if (ShowBankFeesField && BankFees >= Amount)
        {
            BeautifulMessageDialog.ShowWarning("العمولة يجب أن تكون أقل من المبلغ");
            return;
        }

        IsBusy = true;
        try
        {
            var voucher = new Voucher
            {
                VoucherNumber = VoucherNumber,
                VoucherType = SelectedVoucherType,
                Amount = Amount,
                BankFees = BankFees,
                CashBoxId = SelectedCashBox.Id,
                BankAccountId = SelectedBankAccount?.Id,
                CustomerId = SelectedCustomer?.Id,
                InvestorId = SelectedInvestor?.Id,
                Date = VoucherDate,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            await _cashBankService.CreateVoucherAsync(voucher);

            BeautifulMessageDialog.ShowSuccess($"تم إنشاء {GetVoucherTypeName(SelectedVoucherType)} رقم {VoucherNumber} بنجاح");

            // Reset form
            Amount = 0;
            BankFees = 0;
            Notes = string.Empty;
            SelectedCustomer = null;
            SelectedInvestor = null;
            SelectedBankAccount = null;
            VoucherDate = DateTime.Now;
            await GenerateVoucherNumberAsync();
            await LoadVouchersAsync();
            await LoadLookupsAsync(); // Refresh balances
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

    // ══════════════════════════════════════════════════════
    // VOUCHER LIST
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private async Task LoadVouchersAsync()
    {
        var (items, totalCount) = await _cashBankService.GetPagedVouchersAsync(
            CurrentPage, PageSize, FilterType,
            searchTerm: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

        Vouchers.Clear();
        foreach (var v in items)
            Vouchers.Add(v);

        TotalCount = totalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
    }

    [RelayCommand]
    private async Task SearchVouchersAsync()
    {
        CurrentPage = 1;
        await LoadVouchersAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadVouchersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadVouchersAsync();
        }
    }

    // ══════════════════════════════════════════════════════
    // PRINT VOUCHER
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private void PrintVoucher(Voucher? voucher)
    {
        if (voucher is null) return;

        var columns = new[] { "الحقل", "القيمة" };
        var rows = new List<object[]>
        {
            new object[] { "رقم السند", voucher.VoucherNumber },
            new object[] { "النوع", GetVoucherTypeName(voucher.VoucherType) },
            new object[] { "المبلغ", voucher.Amount.ToString("N0") },
            new object[] { "التاريخ", voucher.Date.ToString("yyyy/MM/dd") },
            new object[] { "القاصة", voucher.CashBox?.Name ?? string.Empty },
        };

        if (voucher.BankFees > 0)
        {
            rows.Add(new object[] { "عمولة المصرف", voucher.BankFees.ToString("N0") });
            rows.Add(new object[] { "صافي المبلغ", (voucher.Amount - voucher.BankFees).ToString("N0") });
        }
        if (voucher.Customer is not null)
            rows.Add(new object[] { "العميل", voucher.Customer.Name });
        if (voucher.Investor is not null)
            rows.Add(new object[] { "المستثمر", voucher.Investor.Name });
        if (voucher.BankAccount is not null)
            rows.Add(new object[] { "المصرف", voucher.BankAccount.Name });
        if (!string.IsNullOrWhiteSpace(voucher.Notes))
            rows.Add(new object[] { "ملاحظات", voucher.Notes });

        _exportService.PrintTable($"{GetVoucherTypeName(voucher.VoucherType)} - {voucher.VoucherNumber}", columns, rows);
    }

    // ══════════════════════════════════════════════════════
    // EXPORT
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private void ExportVouchers()
    {
        var columns = new[] { "رقم السند", "النوع", "المبلغ", "العمولة", "التاريخ", "القاصة", "العميل", "المستثمر", "ملاحظات" };
        var rows = Vouchers.Select(v => new object[]
        {
            v.VoucherNumber,
            GetVoucherTypeName(v.VoucherType),
            v.Amount.ToString("N0"),
            v.BankFees > 0 ? v.BankFees.ToString("N0") : "",
            v.Date.ToString("yyyy/MM/dd"),
            v.CashBox?.Name ?? "",
            v.Customer?.Name ?? "",
            v.Investor?.Name ?? "",
            v.Notes ?? ""
        }).ToList();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"السندات_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportToExcel(dialog.FileName, "السندات", columns, (IList<object[]>)rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
    }

    private static string GetVoucherTypeName(VoucherType type) => type switch
    {
        VoucherType.Receipt => "سند قبض",
        VoucherType.Payment => "سند صرف",
        VoucherType.BankReceipt => "سند قبض مصرفي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "سند قبض دين",
        _ => "سند"
    };
}

/// <summary>Helper record for voucher type ComboBox items.</summary>
public record VoucherTypeItem(string Name, VoucherType Type)
{
    public override string ToString() => Name;
}
