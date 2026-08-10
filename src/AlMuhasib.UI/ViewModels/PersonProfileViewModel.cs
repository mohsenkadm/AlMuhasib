using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class PersonProfileViewModel : ViewModelBase
{
    private readonly IPersonProfileService _personProfileService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWhatsAppShareService _whatsAppShare;

    private List<PersonLookupItem> _allPeople = [];
    private PersonProfileResult? _currentProfile;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private PersonLookupItem? _selectedPerson;
    [ObservableProperty] private PersonTypeFilterItem _selectedTypeFilter = null!;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;

    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private bool _showEmptyState = true;
    [ObservableProperty] private string _periodLabel = "جميع الفترات";

    [ObservableProperty] private string _personName = "—";
    [ObservableProperty] private string _typeLabel = string.Empty;
    [ObservableProperty] private string _phone = "—";
    [ObservableProperty] private string _address = "—";
    [ObservableProperty] private string _notes = "—";
    [ObservableProperty] private string _fileNumber = "—";
    [ObservableProperty] private string _typeAccent = "#1565C0";
    [ObservableProperty] private string _typeAccentLight = "#E3F2FD";
    [ObservableProperty] private PackIconKind _typeIcon = PackIconKind.Account;

    [ObservableProperty] private string _totalDebit = "0";
    [ObservableProperty] private string _totalCredit = "0";
    [ObservableProperty] private string _balance = "0";
    [ObservableProperty] private string _transactionCount = "0";

    [ObservableProperty] private bool _showCustomerExtras;
    [ObservableProperty] private bool _showInvestorExtras;
    [ObservableProperty] private string _reliabilityScore = "—";
    [ObservableProperty] private string _maxCreditLimit = "—";
    [ObservableProperty] private string _maxInstallmentDebt = "—";
    [ObservableProperty] private string _guarantorInfo = "—";
    [ObservableProperty] private string _totalDeposit = "—";
    [ObservableProperty] private string _openingBalance = "—";
    [ObservableProperty] private string _profitPercentage = "—";

    [ObservableProperty] private string _profileContentKey = "empty";

    public ObservableCollection<PersonLookupItem> FilteredPeople { get; } = [];
    public ObservableCollection<PersonTypeFilterItem> TypeFilters { get; } = [];
    public ObservableCollection<PersonTimelineDisplayItem> TimelineItems { get; } = [];
    public ObservableCollection<PersonSectionDisplayItem> Sections { get; } = [];

    public PersonProfileViewModel(
        IPersonProfileService personProfileService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IWhatsAppShareService whatsAppShare)
    {
        _personProfileService = personProfileService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _whatsAppShare = whatsAppShare;
        PageTitle = "ملف الشخص";

        TypeFilters.Add(new PersonTypeFilterItem(null, "الكل", PackIconKind.AccountMultiple) { IsSelected = true });
        TypeFilters.Add(new PersonTypeFilterItem(PersonPartyType.Customer, "عملاء", PackIconKind.Account));
        TypeFilters.Add(new PersonTypeFilterItem(PersonPartyType.Supplier, "موردون", PackIconKind.Factory));
        TypeFilters.Add(new PersonTypeFilterItem(PersonPartyType.Investor, "مستثمرون", PackIconKind.TrendingUp));
        SelectedTypeFilter = TypeFilters[0];
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "PersonProfile");
        await ReloadPeopleAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (SelectedPerson is not null &&
            (SelectedPerson.DisplayText == value || SelectedPerson.Name == value))
            return;

        SelectedPerson = null;
        ApplyPeopleFilter();
    }

    partial void OnSelectedTypeFilterChanged(PersonTypeFilterItem value)
    {
        if (value is null) return;
        foreach (var filter in TypeFilters)
            filter.IsSelected = ReferenceEquals(filter, value);
    }

    partial void OnSelectedPersonChanged(PersonLookupItem? value)
    {
        if (value is not null)
        {
            SearchText = value.DisplayText;
            _ = LoadProfileAsync();
        }
    }

    [RelayCommand]
    private async Task ReloadPeopleAsync()
    {
        try
        {
            IsBusy = true;
            var type = SelectedTypeFilter?.Type;
            _allPeople = await _personProfileService.SearchPeopleAsync(null, type);
            ApplyPeopleFilter();
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

    private void ApplyPeopleFilter()
    {
        FilteredPeople.Clear();
        var term = SearchText?.Trim();
        IEnumerable<PersonLookupItem> query = _allPeople;

        if (!string.IsNullOrWhiteSpace(term) &&
            (SelectedPerson is null || (SelectedPerson.DisplayText != term && SelectedPerson.Name != term)))
        {
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(p.Phone) && p.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                p.DisplayText.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.TypeLabel.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query.Take(200))
            FilteredPeople.Add(item);
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        if (SelectedPerson is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار شخص من القائمة");
            return;
        }

        if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value.Date > DateTo.Value.Date)
        {
            BeautifulMessageDialog.ShowWarning("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            return;
        }

        try
        {
            IsBusy = true;
            var profile = await _personProfileService.GetProfileAsync(
                SelectedPerson.PartyType, SelectedPerson.Id, DateFrom, DateTo);

            if (profile is null)
            {
                BeautifulMessageDialog.ShowWarning("تعذر تحميل بيانات الشخص");
                ClearProfile();
                return;
            }

            ApplyProfile(profile);
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
    private void ClearSelection()
    {
        SelectedPerson = null;
        SearchText = string.Empty;
        ClearProfile();
        ApplyPeopleFilter();
    }

    [RelayCommand]
    private async Task SelectTypeFilterAsync(PersonTypeFilterItem? filter)
    {
        if (filter is null) return;
        SelectedTypeFilter = filter;
        await ReloadPeopleAsync();
    }

    [RelayCommand]
    private void ToggleSection(PersonSectionDisplayItem? section)
    {
        if (section is null) return;
        section.IsExpanded = !section.IsExpanded;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (_currentProfile is null || TimelineItems.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للتصدير");
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = $"ملف_{_currentProfile.Name}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "التاريخ", "النوع", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _currentProfile.Timeline
            .Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.CategoryLabel,
                r.Description,
                r.Debit,
                r.Credit,
                r.RunningBalance
            })
            .ToList();

        _exportService.ExportToExcel(dlg.FileName, $"ملف {_currentProfile.Name}", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        if (_currentProfile is null || TimelineItems.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للطباعة");
            return;
        }

        var cols = new[] { "التاريخ", "النوع", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _currentProfile.Timeline
            .Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.CategoryLabel,
                r.Description,
                r.Debit.ToString("N0"),
                r.Credit.ToString("N0"),
                r.RunningBalance.ToString("N0")
            })
            .ToList();

        var title = $"ملف {_currentProfile.Name} ({_currentProfile.TypeLabel}) — {PeriodLabel}";
        var summary = new List<string>
        {
            $"النوع: {_currentProfile.TypeLabel}",
            $"الهاتف: {Phone}",
            $"الفترة: {PeriodLabel}",
            $"عدد الحركات: {TransactionCount}",
            $"إجمالي المدين: {TotalDebit}",
            $"إجمالي الدائن: {TotalCredit}",
            $"الرصيد: {Balance}"
        };

        _exportService.PrintTable(title, cols, rows, summary);
    }

    private void ApplyProfile(PersonProfileResult profile)
    {
        _currentProfile = profile;
        HasSelection = true;
        ShowEmptyState = false;
        ProfileContentKey = $"{profile.PartyType}-{profile.Id}-{DateTime.Now.Ticks}";

        PersonName = profile.Name;
        TypeLabel = profile.TypeLabel;
        Phone = string.IsNullOrWhiteSpace(profile.Phone) ? "—" : profile.Phone!;
        Address = string.IsNullOrWhiteSpace(profile.Address) ? "—" : profile.Address!;
        Notes = string.IsNullOrWhiteSpace(profile.Notes) ? "—" : profile.Notes!;
        FileNumber = string.IsNullOrWhiteSpace(profile.FileNumber) ? "—" : profile.FileNumber!;
        PeriodLabel = BuildPeriodLabel();

        (TypeAccent, TypeAccentLight, TypeIcon) = profile.PartyType switch
        {
            PersonPartyType.Customer => ("#1565C0", "#E3F2FD", PackIconKind.Account),
            PersonPartyType.Supplier => ("#EF6C00", "#FFF3E0", PackIconKind.Factory),
            PersonPartyType.Investor => ("#2E7D32", "#E8F5E9", PackIconKind.TrendingUp),
            _ => ("#1565C0", "#E3F2FD", PackIconKind.Account)
        };

        TotalDebit = FormatCurrency(profile.TotalDebit);
        TotalCredit = FormatCurrency(profile.TotalCredit);
        Balance = FormatCurrency(profile.Balance);
        TransactionCount = profile.TransactionCount.ToString("N0");

        ShowCustomerExtras = profile.PartyType == PersonPartyType.Customer;
        ShowInvestorExtras = profile.PartyType == PersonPartyType.Investor;

        if (ShowCustomerExtras)
        {
            ReliabilityScore = profile.ReliabilityScore?.ToString("N0") ?? "—";
            MaxCreditLimit = profile.MaxCreditLimit.HasValue ? FormatCurrency(profile.MaxCreditLimit.Value) : "—";
            MaxInstallmentDebt = profile.MaxInstallmentDebt.HasValue ? FormatCurrency(profile.MaxInstallmentDebt.Value) : "—";
            GuarantorInfo = string.IsNullOrWhiteSpace(profile.GuarantorName)
                ? "—"
                : $"{profile.GuarantorName}" + (string.IsNullOrWhiteSpace(profile.GuarantorPhone) ? "" : $" — {profile.GuarantorPhone}");
        }

        if (ShowInvestorExtras)
        {
            TotalDeposit = profile.TotalDeposit.HasValue ? FormatCurrency(profile.TotalDeposit.Value) : "—";
            OpeningBalance = profile.OpeningBalance.HasValue ? FormatCurrency(profile.OpeningBalance.Value) : "—";
            ProfitPercentage = profile.ProfitPercentage.HasValue
                ? $"{profile.ProfitPercentage.Value:N2}%"
                : "—";
        }

        TimelineItems.Clear();
        var index = 0;
        foreach (var item in profile.Timeline)
        {
            TimelineItems.Add(CreateTimelineDisplay(item, index++));
        }

        Sections.Clear();
        foreach (var section in profile.Sections)
        {
            var display = new PersonSectionDisplayItem
            {
                Key = section.Key,
                Title = section.Title,
                Count = section.Count,
                IsExpanded = section.IsExpanded,
                Icon = MapSectionIcon(section.Key)
            };
            foreach (var row in section.Rows)
                display.Rows.Add(row);
            Sections.Add(display);
        }

        ApplyCustomerInsights(profile.PartyType == PersonPartyType.Customer ? profile.CustomerInsights : null);
        CustomerTabSearch = string.Empty;
        CustomerSelectedTab = 0;
    }

    private void ClearProfile()
    {
        _currentProfile = null;
        HasSelection = false;
        ShowEmptyState = true;
        ProfileContentKey = "empty";
        PersonName = "—";
        TypeLabel = string.Empty;
        Phone = "—";
        Address = "—";
        Notes = "—";
        FileNumber = "—";
        TotalDebit = "0";
        TotalCredit = "0";
        Balance = "0";
        TransactionCount = "0";
        ShowCustomerExtras = false;
        ShowInvestorExtras = false;
        TimelineItems.Clear();
        Sections.Clear();
        ApplyCustomerInsights(null);
        CustomerTabSearch = string.Empty;
    }

    private static PersonTimelineDisplayItem CreateTimelineDisplay(PersonTimelineItem item, int index)
    {
        var (icon, accent, accentLight) = item.Category switch
        {
            PersonTimelineCategory.Invoice => (PackIconKind.FileDocumentOutline, "#1565C0", "#E3F2FD"),
            PersonTimelineCategory.Voucher => (PackIconKind.Receipt, "#00838F", "#E0F7FA"),
            PersonTimelineCategory.InstallmentPayment => (PackIconKind.CalendarCheck, "#6A1B9A", "#F3E5F5"),
            PersonTimelineCategory.OpeningBalance => (PackIconKind.History, "#455A64", "#ECEFF1"),
            PersonTimelineCategory.Deposit => (PackIconKind.CashPlus, "#2E7D32", "#E8F5E9"),
            PersonTimelineCategory.Withdrawal => (PackIconKind.CashMinus, "#C62828", "#FFEBEE"),
            PersonTimelineCategory.ProfitDistribution => (PackIconKind.ChartLine, "#558B2F", "#F1F8E9"),
            _ => (PackIconKind.CircleOutline, "#607D8B", "#ECEFF1")
        };

        return new PersonTimelineDisplayItem
        {
            Date = item.Date,
            CategoryLabel = item.CategoryLabel,
            Description = item.Description,
            Debit = item.Debit,
            Credit = item.Credit,
            RunningBalance = item.RunningBalance,
            DebitLabel = item.Debit > 0 ? item.Debit.ToString("N0") : "—",
            CreditLabel = item.Credit > 0 ? item.Credit.ToString("N0") : "—",
            BalanceLabel = item.RunningBalance.ToString("N0"),
            Icon = icon,
            Accent = accent,
            AccentLight = accentLight,
            AnimationDelayMs = Math.Min(index * 40, 600)
        };
    }

    private static PackIconKind MapSectionIcon(string key) => key switch
    {
        "invoices" => PackIconKind.FileDocumentMultipleOutline,
        "vouchers" => PackIconKind.ReceiptTextOutline,
        "installments" => PackIconKind.CalendarMultipleCheck,
        "attachments" => PackIconKind.Paperclip,
        "transactions" => PackIconKind.SwapHorizontal,
        "profits" => PackIconKind.ChartTimelineVariant,
        _ => PackIconKind.FolderOutline
    };

    private string BuildPeriodLabel()
    {
        if (!DateFrom.HasValue && !DateTo.HasValue)
            return "جميع الفترات";
        if (DateFrom.HasValue && DateTo.HasValue)
            return $"{DateFrom:yyyy/MM/dd} — {DateTo:yyyy/MM/dd}";
        if (DateFrom.HasValue)
            return $"من {DateFrom:yyyy/MM/dd}";
        return $"حتى {DateTo:yyyy/MM/dd}";
    }

    private static string FormatCurrency(decimal value) => $"{value:N0} د.ع";
}

public partial class PersonTypeFilterItem : ObservableObject
{
    public PersonTypeFilterItem(PersonPartyType? type, string label, PackIconKind icon)
    {
        Type = type;
        Label = label;
        Icon = icon;
    }

    public PersonPartyType? Type { get; }
    public string Label { get; }
    public PackIconKind Icon { get; }

    [ObservableProperty] private bool _isSelected;
}

public partial class PersonTimelineDisplayItem : ObservableObject
{
    public DateTime Date { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public string DebitLabel { get; set; } = "—";
    public string CreditLabel { get; set; } = "—";
    public string BalanceLabel { get; set; } = "—";
    public PackIconKind Icon { get; set; }
    public string Accent { get; set; } = "#1565C0";
    public string AccentLight { get; set; } = "#E3F2FD";
    public int AnimationDelayMs { get; set; }
}

public partial class PersonSectionDisplayItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public PackIconKind Icon { get; set; }

    [ObservableProperty] private bool _isExpanded = true;

    public ObservableCollection<PersonProfileDetailRow> Rows { get; } = [];
}
