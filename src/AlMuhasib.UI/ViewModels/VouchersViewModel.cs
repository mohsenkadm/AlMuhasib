using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class VouchersViewModel : PagedViewModelBase, IInvestorLookupHost
{
    /// <summary>يُعيَّن من شريط الإجراءات السريع قبل فتح الشاشة.</summary>
    public static VoucherType? PendingInitialType { get; set; }

    private readonly ICashBankService _cashBankService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly ICurrentUserService _currentUserService;
    private readonly IReportService _reportService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IUserPreferencesService _userPreferences;
    private CancellationTokenSource? _customerBalanceCts;
    private CancellationTokenSource? _supplierBalanceCts;

    public VouchersViewModel(
        ICashBankService cashBankService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        ICurrentUserService currentUserService,
        IReportService reportService,
        IExchangeRateService exchangeRateService,
        IUserPreferencesService userPreferences)
    {
        _cashBankService = cashBankService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _currentUserService = currentUserService;
        _reportService = reportService;
        _exchangeRateService = exchangeRateService;
        _userPreferences = userPreferences;
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
    private string _customerSearchText = string.Empty;

    [ObservableProperty]
    private decimal? _customerOutstandingBalance;

    [ObservableProperty]
    private bool _isCustomerBalanceLoading;

    public bool ShowCustomerBalance =>
        ShowCustomerPickerField && SelectedCustomer is not null;

    public bool HasCustomerOutstandingBalance =>
        (CustomerOutstandingBalance is > 0) || (CustomerOutstandingBalanceUsd is > 0);

    public string CustomerBalanceText
    {
        get
        {
            if (CustomerOutstandingBalance is null && CustomerOutstandingBalanceUsd is null)
                return string.Empty;

            var iqd = CustomerOutstandingBalance ?? 0;
            var usd = CustomerOutstandingBalanceUsd ?? 0;
            if (iqd <= 0 && usd <= 0)
                return "لا ذمم عليه";

            var parts = new List<string>();
            if (iqd > 0) parts.Add($"عليه لنا: {iqd:N0} د.ع");
            if (usd > 0) parts.Add($"عليه لنا: {usd:N2} $");
            return string.Join("  |  ", parts);
        }
    }

    [ObservableProperty]
    private decimal? _customerOutstandingBalanceUsd;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string _supplierSearchText = string.Empty;

    [ObservableProperty]
    private decimal? _supplierOutstandingBalance;

    [ObservableProperty]
    private decimal? _supplierOutstandingBalanceUsd;

    [ObservableProperty]
    private bool _isSupplierBalanceLoading;

    public bool ShowSupplierBalance =>
        ShowSupplierField && SelectedSupplier is not null;

    public bool HasSupplierOutstandingBalance =>
        (SupplierOutstandingBalance is > 0) || (SupplierOutstandingBalanceUsd is > 0);

    public string SupplierBalanceText
    {
        get
        {
            if (SupplierOutstandingBalance is null && SupplierOutstandingBalanceUsd is null)
                return string.Empty;

            var iqd = SupplierOutstandingBalance ?? 0;
            var usd = SupplierOutstandingBalanceUsd ?? 0;
            if (iqd <= 0 && usd <= 0)
                return "لا ذمم علينا";

            var parts = new List<string>();
            if (iqd > 0) parts.Add($"علينا له: {iqd:N0} د.ع");
            if (usd > 0) parts.Add($"علينا له: {usd:N2} $");
            return string.Join("  |  ", parts);
        }
    }

    [ObservableProperty]
    private Investor? _selectedInvestor;

    [ObservableProperty]
    private string _investorSearchText = string.Empty;

    public bool ShowInvestorBalance =>
        ShowInvestorField && SelectedInvestor is not null;

    public string InvestorBalanceText =>
        SelectedInvestor is null
            ? string.Empty
            : $"رصيد الإيداع: {SelectedInvestor.TotalDeposit:N0} د.ع";

    // Collections
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ObservableCollection<BankAccount> BankAccounts { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<Supplier> FilteredSuppliers { get; } = [];
    public ObservableCollection<Investor> Investors { get; } = [];
    public ObservableCollection<Investor> FilteredInvestors { get; } = [];

    // ── Visibility flags per voucher type ──────────────────
    [ObservableProperty]
    private bool _showCustomerField;

    [ObservableProperty]
    private bool _showOptionalCustomerField;

    [ObservableProperty]
    private bool _showCustomerPickerField;

    [ObservableProperty]
    private bool _isCustomerRequired;

    [ObservableProperty]
    private string _customerFieldHint = "العميل (ابحث بالاسم أو الهاتف أو رقم العميل أو المعرف)";

    [ObservableProperty]
    private bool _showSupplierField;

    [ObservableProperty]
    private bool _showInvestorField;

    [ObservableProperty]
    private bool _showBankField;

    [ObservableProperty]
    private bool _showBankFeesField;

    [ObservableProperty]
    private bool _showDocumentLinkFields;

    [ObservableProperty]
    private bool _showPartyTypeField;

    [ObservableProperty]
    private VoucherPartyKind _selectedPartyKind = VoucherPartyKind.Customer;

    [ObservableProperty]
    private bool _showEmployeeField;

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private string _employeeSearchText = string.Empty;

    public ObservableCollection<Employee> Employees { get; } = [];
    public ObservableCollection<Employee> FilteredEmployees { get; } = [];

    // ── Optional document links (Receipt / DebtReceipt) ──
    public ObservableCollection<Invoice> OpenCreditInvoices { get; } = [];
    public ObservableCollection<Installment> OpenInstallments { get; } = [];

    [ObservableProperty]
    private Invoice? _selectedLinkedInvoice;

    [ObservableProperty]
    private Installment? _selectedLinkedInstallment;

    private bool _suppressDocumentLinkMutualClear;

    // ── Voucher list (all types) ───────────────────────────
    public ObservableCollection<Voucher> Vouchers { get; } = [];

    [ObservableProperty]
    private VoucherType? _filterType;

    [ObservableProperty]
    private DateTime? _dateFrom;

    [ObservableProperty]
    private DateTime? _dateTo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private VoucherFilterTypeOption? _selectedFilterTypeOption;

    // ── Voucher type items for the ComboBox ────────────────
    public List<VoucherTypeItem> VoucherTypes { get; } =
    [
        new("سند قبض", VoucherType.Receipt),
        new("سند دفع", VoucherType.Payment),
        new("سند قبض مصرفي", VoucherType.BankReceipt),
        new("إيداع مستثمر", VoucherType.InvestorDeposit),
        new("سحب مستثمر", VoucherType.InvestorWithdrawal),
        new("سند قبض دين", VoucherType.DebtReceipt),
    ];

    public List<VoucherFilterTypeOption> FilterTypeOptions { get; } =
    [
        new("الكل", null),
        new("سند قبض", VoucherType.Receipt),
        new("سند دفع", VoucherType.Payment),
        new("سند قبض مصرفي", VoucherType.BankReceipt),
        new("إيداع مستثمر", VoucherType.InvestorDeposit),
        new("سحب مستثمر", VoucherType.InvestorWithdrawal),
        new("سند قبض دين", VoucherType.DebtReceipt),
    ];

    public ObservableCollection<VoucherPartyTypeItem> AvailablePartyTypes { get; } = [];

    // ══════════════════════════════════════════════════════
    // INITIALIZE
    // ══════════════════════════════════════════════════════
    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Vouchers");
            SelectedFilterTypeOption ??= FilterTypeOptions[0];
            InitializeCreateFormFeatures();

            await LoadLookupsAsync();

            if (PendingInitialType.HasValue)
            {
                SelectedVoucherType = PendingInitialType.Value;
                PendingInitialType = null;
                UpdateFieldVisibility(SelectedVoucherType);
                await GenerateVoucherNumberAsync();
                await RefreshFxRateAsync();
                IsCreateDialogOpen = true;
            }
            else
            {
                UpdateFieldVisibility(SelectedVoucherType);
                await GenerateVoucherNumberAsync();
            }

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
        _allCashBoxes.Clear();
        CashBoxes.Clear();
        foreach (var cb in cashBoxes)
        {
            _allCashBoxes.Add(cb);
            CashBoxes.Add(cb);
        }
        ApplyCashBoxCurrencyFilter();

        var banks = await _cashBankService.GetAllBankAccountsAsync();
        BankAccounts.Clear();
        foreach (var b in banks)
            BankAccounts.Add(b);

        var customers = await _unitOfWork.Customers.GetAllAsync();
        Customers.Clear();
        foreach (var c in customers)
            Customers.Add(c);
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, CustomerSearchText);

        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        Suppliers.Clear();
        foreach (var s in suppliers)
            Suppliers.Add(s);
        SupplierComboBoxFilter.Apply(Suppliers, FilteredSuppliers, SupplierSearchText);

        await RefreshInvestorsAsync();

        var employees = await _unitOfWork.Employees.GetAllAsync();
        Employees.Clear();
        FilteredEmployees.Clear();
        foreach (var e in employees.Where(x => x.IsActive).OrderBy(x => x.Name))
        {
            Employees.Add(e);
            FilteredEmployees.Add(e);
        }
    }

    public async Task RefreshInvestorsAsync()
    {
        var selectedId = SelectedInvestor?.Id;
        var investors = await _unitOfWork.Investors.GetAllAsync();
        Investors.Clear();
        foreach (var inv in investors)
            Investors.Add(inv);
        InvestorComboBoxFilter.Apply(Investors, FilteredInvestors, InvestorSearchText);
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
        ShowPartyTypeField = type is VoucherType.Receipt or VoucherType.Payment or VoucherType.DebtReceipt;
        ShowInvestorField = type is VoucherType.InvestorDeposit or VoucherType.InvestorWithdrawal;
        ShowBankField = type is VoucherType.BankReceipt;
        ShowBankFeesField = type is VoucherType.BankReceipt;

        RebuildAvailablePartyTypes(type);
        ApplyPartyKindVisibility();

        if (!ShowInvestorField)
        {
            SelectedInvestor = null;
            InvestorSearchText = string.Empty;
            InvestorComboBoxFilter.Apply(Investors, FilteredInvestors, null);
        }

        if (!ShowBankField) SelectedBankAccount = null;
        if (!ShowBankFeesField) BankFees = 0;

        UpdateNetAmountText();
    }

    private void RebuildAvailablePartyTypes(VoucherType type)
    {
        AvailablePartyTypes.Clear();
        switch (type)
        {
            case VoucherType.Receipt:
                AvailablePartyTypes.Add(new("زبون", VoucherPartyKind.Customer));
                AvailablePartyTypes.Add(new("موظف", VoucherPartyKind.Employee));
                break;
            case VoucherType.Payment:
                AvailablePartyTypes.Add(new("زبون", VoucherPartyKind.Customer));
                AvailablePartyTypes.Add(new("مورد", VoucherPartyKind.Supplier));
                AvailablePartyTypes.Add(new("موظف", VoucherPartyKind.Employee));
                break;
            case VoucherType.DebtReceipt:
                AvailablePartyTypes.Add(new("زبون", VoucherPartyKind.Customer));
                break;
            case VoucherType.InvestorDeposit:
            case VoucherType.InvestorWithdrawal:
                AvailablePartyTypes.Add(new("مستثمر", VoucherPartyKind.Investor));
                break;
        }

        if (AvailablePartyTypes.Count > 0 &&
            AvailablePartyTypes.All(p => p.Kind != SelectedPartyKind))
            SelectedPartyKind = AvailablePartyTypes[0].Kind;
    }

    partial void OnSelectedPartyKindChanged(VoucherPartyKind value) => ApplyPartyKindVisibility();

    private void ApplyPartyKindVisibility()
    {
        var type = SelectedVoucherType;
        var kind = SelectedPartyKind;

        ShowCustomerField = type is VoucherType.Receipt or VoucherType.DebtReceipt
                            && kind == VoucherPartyKind.Customer;
        ShowOptionalCustomerField = type == VoucherType.Payment && kind == VoucherPartyKind.Customer;
        ShowCustomerPickerField = ShowCustomerField || ShowOptionalCustomerField;
        IsCustomerRequired = ShowCustomerField || (type == VoucherType.Payment && kind == VoucherPartyKind.Customer);
        CustomerFieldHint = ShowOptionalCustomerField && !IsCustomerRequired
            ? "الزبون (ابحث بالاسم أو الهاتف أو رقم العميل أو المعرف)"
            : "العميل (ابحث بالاسم أو الهاتف أو رقم العميل أو المعرف)";

        ShowSupplierField = type == VoucherType.Payment && kind == VoucherPartyKind.Supplier;
        ShowEmployeeField = type is VoucherType.Receipt or VoucherType.Payment
                            && kind == VoucherPartyKind.Employee;
        ShowInvestorField = type is VoucherType.InvestorDeposit or VoucherType.InvestorWithdrawal
                            || kind == VoucherPartyKind.Investor;

        if (!ShowCustomerPickerField)
        {
            SelectedCustomer = null;
            CustomerSearchText = string.Empty;
            CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, null);
        }

        if (!ShowSupplierField)
        {
            SelectedSupplier = null;
            SupplierSearchText = string.Empty;
            SupplierComboBoxFilter.Apply(Suppliers, FilteredSuppliers, null);
        }

        if (!ShowEmployeeField)
        {
            SelectedEmployee = null;
            EmployeeSearchText = string.Empty;
            FilteredEmployees.Clear();
            foreach (var e in Employees)
                FilteredEmployees.Add(e);
        }

        RefreshDocumentLinkVisibility();
        if (!ShowDocumentLinkFields)
            ClearDocumentLinks();
        else
            _ = LoadDocumentLinksAsync();
    }

    partial void OnEmployeeSearchTextChanged(string value)
    {
        if (SelectedEmployee is not null && SelectedEmployee.Name == value)
            return;

        SelectedEmployee = null;
        FilteredEmployees.Clear();
        var term = value?.Trim() ?? string.Empty;
        foreach (var e in Employees.Where(x =>
                     string.IsNullOrEmpty(term) ||
                     x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                     (x.Phone != null && x.Phone.Contains(term))))
            FilteredEmployees.Add(e);
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        if (value is not null)
            EmployeeSearchText = value.Name;
        _ = RefreshEmployeeBalanceAsync();
    }

    private void RefreshDocumentLinkVisibility()
    {
        ShowDocumentLinkFields = ShowCustomerField &&
            SelectedCustomer is not null &&
            SelectedVoucherType is VoucherType.Receipt or VoucherType.DebtReceipt;
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
            CustomerSearchText = CustomerDisplayHelper.FormatDisplayName(value.Name, value.FileNumber);

        if (ShowOptionalCustomerField && value is not null && SelectedSupplier is not null)
        {
            SelectedSupplier = null;
            SupplierSearchText = string.Empty;
            SupplierComboBoxFilter.Apply(Suppliers, FilteredSuppliers, null);
        }

        RefreshDocumentLinkVisibility();
        _ = LoadDocumentLinksAsync();
        _ = RefreshCustomerBalanceAsync();
        OnPropertyChanged(nameof(ShowCustomerBalance));
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null &&
            CustomerDisplayHelper.FormatDisplayName(SelectedCustomer.Name, SelectedCustomer.FileNumber) == value)
            return;

        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
        RefreshDocumentLinkVisibility();
        ClearCustomerBalance();
    }

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value is not null)
            SupplierSearchText = value.Name;

        if (ShowOptionalCustomerField && value is not null && SelectedCustomer is not null)
        {
            SelectedCustomer = null;
            CustomerSearchText = string.Empty;
            CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, null);
        }

        _ = RefreshSupplierBalanceAsync();
        OnPropertyChanged(nameof(ShowSupplierBalance));
    }

    partial void OnSupplierSearchTextChanged(string value)
    {
        if (SelectedSupplier is not null && SelectedSupplier.Name == value)
            return;

        SelectedSupplier = null;
        SupplierComboBoxFilter.Apply(Suppliers, FilteredSuppliers, value);
        ClearSupplierBalance();
    }

    partial void OnCustomerOutstandingBalanceChanged(decimal? value)
    {
        OnPropertyChanged(nameof(HasCustomerOutstandingBalance));
        OnPropertyChanged(nameof(CustomerBalanceText));
    }

    partial void OnCustomerOutstandingBalanceUsdChanged(decimal? value)
    {
        OnPropertyChanged(nameof(HasCustomerOutstandingBalance));
        OnPropertyChanged(nameof(CustomerBalanceText));
    }

    partial void OnIsCustomerBalanceLoadingChanged(bool value) =>
        OnPropertyChanged(nameof(ShowCustomerBalance));

    partial void OnSupplierOutstandingBalanceChanged(decimal? value)
    {
        OnPropertyChanged(nameof(HasSupplierOutstandingBalance));
        OnPropertyChanged(nameof(SupplierBalanceText));
    }

    partial void OnSupplierOutstandingBalanceUsdChanged(decimal? value)
    {
        OnPropertyChanged(nameof(HasSupplierOutstandingBalance));
        OnPropertyChanged(nameof(SupplierBalanceText));
    }

    partial void OnIsSupplierBalanceLoadingChanged(bool value) =>
        OnPropertyChanged(nameof(ShowSupplierBalance));

    partial void OnShowCustomerPickerFieldChanged(bool value) =>
        OnPropertyChanged(nameof(ShowCustomerBalance));

    partial void OnShowSupplierFieldChanged(bool value) =>
        OnPropertyChanged(nameof(ShowSupplierBalance));

    private async Task RefreshCustomerBalanceAsync()
    {
        _customerBalanceCts?.Cancel();
        _customerBalanceCts?.Dispose();
        _customerBalanceCts = new CancellationTokenSource();
        var token = _customerBalanceCts.Token;

        if (SelectedCustomer is null)
        {
            ClearCustomerBalance();
            return;
        }

        IsCustomerBalanceLoading = true;
        IsPartyBalanceLoading = true;
        ShowPartyBalancePanel = true;
        PartyBalanceTitle = "رصيد العميل";
        OnPropertyChanged(nameof(ShowCustomerBalance));

        try
        {
            var statement = await _reportService.GetCustomerStatementAsync(SelectedCustomer.Id);
            if (token.IsCancellationRequested)
                return;

            CustomerOutstandingBalance = statement.Balance;
            CustomerOutstandingBalanceUsd = statement.BalanceUsd;
            SetPartyBalances(statement.Balance, statement.BalanceUsd, "رصيد العميل");
        }
        catch
        {
            if (!token.IsCancellationRequested)
            {
                CustomerOutstandingBalance = null;
                CustomerOutstandingBalanceUsd = null;
                ClearPartyBalances();
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsCustomerBalanceLoading = false;
                OnPropertyChanged(nameof(ShowCustomerBalance));
            }
        }
    }

    private async Task RefreshSupplierBalanceAsync()
    {
        _supplierBalanceCts?.Cancel();
        _supplierBalanceCts?.Dispose();
        _supplierBalanceCts = new CancellationTokenSource();
        var token = _supplierBalanceCts.Token;

        if (SelectedSupplier is null)
        {
            ClearSupplierBalance();
            return;
        }

        IsSupplierBalanceLoading = true;
        IsPartyBalanceLoading = true;
        ShowPartyBalancePanel = true;
        PartyBalanceTitle = "رصيد المورد";
        OnPropertyChanged(nameof(ShowSupplierBalance));

        try
        {
            var statement = await _reportService.GetSupplierStatementAsync(SelectedSupplier.Id);
            if (token.IsCancellationRequested)
                return;

            SupplierOutstandingBalance = statement.Balance;
            SupplierOutstandingBalanceUsd = statement.BalanceUsd;
            SetPartyBalances(statement.Balance, statement.BalanceUsd, "رصيد المورد");
        }
        catch
        {
            if (!token.IsCancellationRequested)
            {
                SupplierOutstandingBalance = null;
                SupplierOutstandingBalanceUsd = null;
                ClearPartyBalances();
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsSupplierBalanceLoading = false;
                OnPropertyChanged(nameof(ShowSupplierBalance));
            }
        }
    }

    private void ClearCustomerBalance()
    {
        _customerBalanceCts?.Cancel();
        CustomerOutstandingBalance = null;
        CustomerOutstandingBalanceUsd = null;
        IsCustomerBalanceLoading = false;
        OnPropertyChanged(nameof(ShowCustomerBalance));
        if (!ShowSupplierField && !ShowEmployeeField && !ShowInvestorField)
            ClearPartyBalances();
    }

    private void ClearSupplierBalance()
    {
        _supplierBalanceCts?.Cancel();
        SupplierOutstandingBalance = null;
        SupplierOutstandingBalanceUsd = null;
        IsSupplierBalanceLoading = false;
        OnPropertyChanged(nameof(ShowSupplierBalance));
        if (!ShowCustomerPickerField && !ShowEmployeeField && !ShowInvestorField)
            ClearPartyBalances();
    }

    partial void OnSelectedInvestorChanged(Investor? value)
    {
        if (value is not null)
            InvestorSearchText = value.Name;
        OnPropertyChanged(nameof(ShowInvestorBalance));
        OnPropertyChanged(nameof(InvestorBalanceText));

        if (value is not null)
            SetPartyBalances(value.TotalDeposit, 0, "رصيد إيداع المستثمر");
        else if (!ShowCustomerPickerField && !ShowSupplierField && !ShowEmployeeField)
            ClearPartyBalances();
    }

    partial void OnShowInvestorFieldChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowInvestorBalance));
    }

    partial void OnInvestorSearchTextChanged(string value)
    {
        if (SelectedInvestor is not null && SelectedInvestor.Name == value)
            return;

        SelectedInvestor = null;
        InvestorComboBoxFilter.Apply(Investors, FilteredInvestors, value);
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadVouchersAsync();
    }

    partial void OnDateToChanged(DateTime? value)
    {
        CurrentPage = 1;
        _ = LoadVouchersAsync();
    }

    partial void OnSelectedFilterTypeOptionChanged(VoucherFilterTypeOption? value)
    {
        FilterType = value?.Type;
        CurrentPage = 1;
        _ = LoadVouchersAsync();
    }

    private async Task LoadDocumentLinksAsync()
    {
        OpenCreditInvoices.Clear();
        OpenInstallments.Clear();
        ClearDocumentLinks();

        if (!ShowDocumentLinkFields || SelectedCustomer is null)
            return;

        try
        {
            var invoices = await _cashBankService.GetOpenCreditInvoicesForCustomerAsync(SelectedCustomer.Id);
            foreach (var inv in invoices)
                OpenCreditInvoices.Add(inv);

            var installments = await _cashBankService.GetOpenInstallmentsForCustomerAsync(SelectedCustomer.Id);
            foreach (var inst in installments)
                OpenInstallments.Add(inst);
        }
        catch
        {
            // optional pickers — ignore load failures
        }
    }

    private void ClearDocumentLinks()
    {
        _suppressDocumentLinkMutualClear = true;
        SelectedLinkedInvoice = null;
        SelectedLinkedInstallment = null;
        _suppressDocumentLinkMutualClear = false;
    }

    partial void OnSelectedLinkedInvoiceChanged(Invoice? value)
    {
        if (_suppressDocumentLinkMutualClear || value is null) return;
        _suppressDocumentLinkMutualClear = true;
        SelectedLinkedInstallment = null;
        _suppressDocumentLinkMutualClear = false;
    }

    partial void OnSelectedLinkedInstallmentChanged(Installment? value)
    {
        if (_suppressDocumentLinkMutualClear || value is null) return;
        _suppressDocumentLinkMutualClear = true;
        SelectedLinkedInvoice = null;
        _suppressDocumentLinkMutualClear = false;
    }

    // OnAmountChanged / OnBankFeesChanged → VouchersViewModel.CreateForm.cs

    private void UpdateNetAmountText()
    {
        if (ShowBankFeesField && BankFees > 0)
            NetAmountText = $"صافي المبلغ: {FormatAmountForCurrency(Amount - BankFees, SelectedCurrency)} {CurrencyAmountSuffix}";
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
        if (IsCustomerRequired && SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار العميل");
            return;
        }
        if (ShowOptionalCustomerField && SelectedCustomer is not null && SelectedSupplier is not null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار الزبون أو المورد فقط، وليس كلاهما");
            return;
        }
        if (ShowInvestorField && SelectedInvestor is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار المستثمر");
            return;
        }
        if (ShowSupplierField && SelectedSupplier is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار المورد");
            return;
        }
        if (ShowEmployeeField && SelectedEmployee is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار الموظف");
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
            var currency = SelectedCashBox.Currency;
            if (ShowMultiCurrency && currency != SelectedCurrency)
            {
                BeautifulMessageDialog.ShowWarning("عملة القاصة يجب أن تطابق عملة السند المحددة");
                return;
            }

            var settlementCurrency = ShowSettlementCurrencyPicker
                ? SettlementCurrency
                : currency;

            var needsFx = currency == AccountingCurrency.USD || settlementCurrency != currency;
            var fxRate = 1m;
            if (needsFx)
            {
                fxRate = FxRate > 0
                    ? FxRate
                    : await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(VoucherDate);
                if (fxRate <= 0)
                {
                    BeautifulMessageDialog.ShowWarning("سعر الصرف مطلوب للمستندات بالدولار أو للتسديد عبر عملتين. سجّل سعر الصرف اليومي أولاً.");
                    return;
                }
            }

            var voucher = new Voucher
            {
                VoucherNumber = VoucherNumber,
                VoucherType = SelectedVoucherType,
                Currency = currency,
                FxRate = fxRate,
                SettlementCurrency = settlementCurrency,
                Amount = Amount,
                BankFees = BankFees,
                CashBoxId = SelectedCashBox.Id,
                BankAccountId = SelectedBankAccount?.Id,
                CustomerId = ShowCustomerPickerField ? SelectedCustomer?.Id : null,
                SupplierId = ShowSupplierField ? SelectedSupplier?.Id : null,
                EmployeeId = ShowEmployeeField ? SelectedEmployee?.Id : null,
                InvestorId = ShowInvestorField ? SelectedInvestor?.Id : null,
                InvoiceId = ShowDocumentLinkFields ? SelectedLinkedInvoice?.Id : null,
                InstallmentId = ShowDocumentLinkFields ? SelectedLinkedInstallment?.Id : null,
                Date = VoucherDate,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            await _cashBankService.CreateVoucherAsync(voucher);

            BeautifulMessageDialog.ShowSuccess($"تم إنشاء {GetVoucherTypeName(SelectedVoucherType)} رقم {voucher.VoucherNumber} بنجاح");

            IsCreateDialogOpen = false;
            ResetCreateFormFields(keepType: true);
            await GenerateVoucherNumberAsync();
            await LoadVouchersAsync();
            await LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            if (message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
            {
                BeautifulMessageDialog.ShowError(
                    "تعذر حفظ السند لأن قاعدة البيانات تحتاج تحديث.\n\n" +
                    "أغلق البرنامج ثم افتحه مرة أخرى بعد التحديث لتطبيق تحديثات قاعدة البيانات.\n\n" +
                    $"التفاصيل: {message}");
                return;
            }

            BeautifulMessageDialog.ShowError(message);
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
            fromDate: DateFrom,
            toDate: DateTo,
            searchTerm: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

        Vouchers.Clear();
        foreach (var v in items)
            Vouchers.Add(v);

        ApplyPaginationStats(totalCount);
    }

    protected override Task OnPageChangedAsync() => LoadVouchersAsync();

    [RelayCommand]
    private async Task SearchVouchersAsync()
    {
        CurrentPage = 1;
        await LoadVouchersAsync();
    }

    [RelayCommand]
    private async Task DeleteVoucherAsync(Voucher? voucher)
    {
        if (voucher is null || !CanDelete) return;

        if (!BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف السند {voucher.VoucherNumber}؟ سيتم عكس أثره المحاسبي ولن يظهر في التقارير."))
            return;

        IsBusy = true;
        try
        {
            await _cashBankService.DeleteVoucherAsync(voucher.Id);
            BeautifulMessageDialog.ShowSuccess("تم حذف السند بنجاح");
            await LoadVouchersAsync();
            await LoadLookupsAsync();
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
    // PRINT / WHATSAPP VOUCHER
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
        if (voucher.Supplier is not null)
            rows.Add(new object[] { "المورد", voucher.Supplier.Name });
        if (voucher.Investor is not null)
            rows.Add(new object[] { "المستثمر", voucher.Investor.Name });
        if (voucher.BankAccount is not null)
            rows.Add(new object[] { "المصرف", voucher.BankAccount.Name });
        if (!string.IsNullOrWhiteSpace(voucher.Notes))
            rows.Add(new object[] { "ملاحظات", voucher.Notes });

        _exportService.PrintTable($"{GetVoucherTypeName(voucher.VoucherType)} - {voucher.VoucherNumber}", columns, rows);
    }

    [RelayCommand]
    private void SendVoucherWhatsApp(Voucher? voucher)
    {
        if (voucher is null) return;

        var model = BuildVoucherPrintModel(voucher);
        var phone = voucher.Customer?.Phone ?? voucher.Supplier?.Phone ?? voucher.Investor?.Phone;
        var name = voucher.Customer?.Name ?? voucher.Supplier?.Name ?? voucher.Investor?.Name ?? model.PartyName ?? "الطرف";
        _whatsAppShare.ShareVoucher(model, phone, name);
    }

    private static VoucherPrintModel BuildVoucherPrintModel(Voucher voucher)
    {
        var typeName = GetVoucherTypeName(voucher.VoucherType);
        string? partyLabel = null;
        string? partyName = null;
        string? partyPhone = null;

        if (voucher.Customer is not null)
        {
            partyLabel = "العميل";
            partyName = CustomerDisplayHelper.FormatDisplayName(voucher.Customer.Name, voucher.Customer.FileNumber);
            partyPhone = voucher.Customer.Phone;
        }
        else if (voucher.Supplier is not null)
        {
            partyLabel = "المورد";
            partyName = voucher.Supplier.Name;
            partyPhone = voucher.Supplier.Phone;
        }
        else if (voucher.Investor is not null)
        {
            partyLabel = "المستثمر";
            partyName = voucher.Investor.Name;
            partyPhone = voucher.Investor.Phone;
        }

        return new VoucherPrintModel
        {
            Title = typeName,
            VoucherNumber = voucher.VoucherNumber,
            VoucherTypeLabel = typeName,
            Date = voucher.Date,
            Amount = voucher.Amount,
            BankFees = voucher.BankFees,
            PartyLabel = partyLabel,
            PartyName = partyName,
            PartyPhone = partyPhone,
            CashBoxName = voucher.CashBox?.Name,
            BankAccountName = voucher.BankAccount?.Name,
            Notes = voucher.Notes,
            CurrencyLabel = AccountingCurrencyHelper.GetLabel(voucher.Currency)
        };
    }

    // ══════════════════════════════════════════════════════
    // EXPORT & PRINT TABLE
    // ══════════════════════════════════════════════════════
    [RelayCommand]
    private void ExportVouchers()
    {
        var columns = new[] { "رقم السند", "النوع", "المبلغ", "العمولة", "التاريخ", "القاصة", "الطرف", "ملاحظات" };
        var rows = Vouchers.Select(v => new object[]
        {
            v.VoucherNumber,
            GetVoucherTypeName(v.VoucherType),
            v.Amount.ToString("N0"),
            v.BankFees > 0 ? v.BankFees.ToString("N0") : "",
            v.Date.ToString("yyyy/MM/dd"),
            v.CashBox?.Name ?? "",
            v.PartyDisplayName,
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

    [RelayCommand]
    private void PrintTable()
    {
        var columns = new[] { "رقم السند", "النوع", "المبلغ", "العمولة", "التاريخ", "القاصة", "الطرف", "ملاحظات" };
        IList<object[]> rows = Vouchers.Select(v => new object[]
        {
            v.VoucherNumber,
            GetVoucherTypeName(v.VoucherType),
            v.Amount.ToString("N0"),
            v.BankFees > 0 ? v.BankFees.ToString("N0") : "",
            v.Date.ToString("yyyy/MM/dd"),
            v.CashBox?.Name ?? "",
            v.PartyDisplayName,
            v.Notes ?? ""
        }).ToList();

        _exportService.PrintTable("قائمة السندات", columns, rows);
    }

    private static string GetVoucherTypeName(VoucherType type) => type switch
    {
        VoucherType.Receipt => "سند قبض",
        VoucherType.Payment => "سند دفع",
        VoucherType.BankReceipt => "سند قبض مصرفي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "سند قبض دين",
        _ => "سند"
    };
}

public enum VoucherPartyKind
{
    Customer,
    Supplier,
    Investor,
    Employee
}

/// <summary>Helper record for voucher type ComboBox items.</summary>
public record VoucherTypeItem(string Name, VoucherType Type)
{
    public override string ToString() => Name;
}

/// <summary>Filter option for voucher list type ComboBox (includes "الكل").</summary>
public record VoucherFilterTypeOption(string Name, VoucherType? Type)
{
    public override string ToString() => Name;
}

public record VoucherPartyTypeItem(string Name, VoucherPartyKind Kind)
{
    public override string ToString() => Name;
}
