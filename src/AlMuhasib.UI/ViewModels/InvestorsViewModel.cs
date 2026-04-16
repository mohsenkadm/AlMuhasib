using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class InvestorsViewModel : ViewModelBase
{
    private readonly IInvestorService _investorService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public InvestorsViewModel(IInvestorService investorService, IUnitOfWork unitOfWork, IExportService exportService, ICurrentUserService currentUserService)
    {
        _investorService = investorService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "المستثمرون";
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    // ══════════════════════════════════════════════════════
    // TAB 0: INVESTOR LIST (قائمة المستثمرين)
    // ══════════════════════════════════════════════════════

    public ObservableCollection<InvestorRow> Investors { get; } = [];

    [ObservableProperty]
    private InvestorRow? _selectedInvestor;

    // Add/Edit form
    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formPhone = string.Empty;

    [ObservableProperty]
    private decimal _formProfitPercentage;

    [ObservableProperty]
    private bool _isEditing;

    private int _editingInvestorId;

    [RelayCommand]
    private async Task SaveInvestorAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسم المستثمر");
            return;
        }
        if (FormProfitPercentage < 0 || FormProfitPercentage > 100)
        {
            BeautifulMessageDialog.ShowWarning("نسبة الربح يجب أن تكون بين 0 و 100");
            return;
        }

        try
        {
            IsBusy = true;
            if (IsEditing)
            {
                await _investorService.UpdateInvestorAsync(_editingInvestorId, FormName.Trim(),
                    string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone.Trim(), FormProfitPercentage);
            }
            else
            {
                await _investorService.AddInvestorAsync(FormName.Trim(),
                    string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone.Trim(), FormProfitPercentage);
            }
            ResetForm();
            await LoadInvestorsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void EditInvestor()
    {
        if (SelectedInvestor is null) return;
        FormName = SelectedInvestor.Name;
        FormPhone = SelectedInvestor.Phone ?? string.Empty;
        FormProfitPercentage = SelectedInvestor.ProfitPercentage;
        _editingInvestorId = SelectedInvestor.Id;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ResetForm();
    }

    private void ResetForm()
    {
        FormName = string.Empty;
        FormPhone = string.Empty;
        FormProfitPercentage = 0;
        IsEditing = false;
        _editingInvestorId = 0;
    }

    [RelayCommand]
    private void ExportInvestors()
    {
        var columns = new[] { "الاسم", "الهاتف", "إجمالي الإيداع", "نسبة الربح %", "إجمالي الأرباح" };
        var rows = Investors.Select(i => new object[]
        {
            i.Name, i.Phone ?? "", i.TotalDeposit.ToString("N0"),
            i.ProfitPercentage.ToString("N2"), i.TotalProfitsEarned.ToString("N0")
        }).ToList();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"المستثمرون_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportToExcel(dialog.FileName, "المستثمرون", columns, (IList<object[]>)rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
    }

    // ══════════════════════════════════════════════════════
    // TAB 1: DEPOSIT (سند إيداع مستثمر)
    // ══════════════════════════════════════════════════════

    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ObservableCollection<Investor> InvestorsList { get; } = [];

    [ObservableProperty]
    private Investor? _depositInvestor;

    [ObservableProperty]
    private decimal _depositAmount;

    [ObservableProperty]
    private DateTime _depositDate = DateTime.Now;

    [ObservableProperty]
    private CashBox? _depositCashBox;

    [ObservableProperty]
    private string _depositNotes = string.Empty;

    public ObservableCollection<InvestorTransaction> RecentDeposits { get; } = [];

    [RelayCommand]
    private async Task SubmitDepositAsync()
    {
        if (DepositInvestor is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر المستثمر");
            return;
        }
        if (DepositAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغ صحيح");
            return;
        }
        if (DepositCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

        try
        {
            IsBusy = true;
            await _investorService.DepositAsync(
                DepositInvestor.Id, DepositAmount, DepositDate, DepositCashBox.Id,
                string.IsNullOrWhiteSpace(DepositNotes) ? null : DepositNotes.Trim());

            BeautifulMessageDialog.ShowSuccess("تم تسجيل الإيداع بنجاح");
            DepositAmount = 0;
            DepositNotes = string.Empty;
            DepositDate = DateTime.Now;

            await LoadRecentDepositsAsync();
            await LoadInvestorsAsync();
            await LoadInvestorsListAsync();
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════
    // TAB 2: WITHDRAWAL (سند سحب من إيداع)
    // ══════════════════════════════════════════════════════

    [ObservableProperty]
    private Investor? _withdrawInvestor;

    [ObservableProperty]
    private decimal _withdrawAmount;

    [ObservableProperty]
    private DateTime _withdrawDate = DateTime.Now;

    [ObservableProperty]
    private CashBox? _withdrawCashBox;

    [ObservableProperty]
    private string _withdrawNotes = string.Empty;

    [ObservableProperty]
    private string _withdrawMaxInfo = string.Empty;

    public ObservableCollection<InvestorTransaction> RecentWithdrawals { get; } = [];

    partial void OnWithdrawInvestorChanged(Investor? value)
    {
        WithdrawMaxInfo = value is not null
            ? $"الحد الأقصى للسحب: {value.TotalDeposit:N0}"
            : string.Empty;
    }

    [RelayCommand]
    private async Task SubmitWithdrawalAsync()
    {
        if (WithdrawInvestor is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر المستثمر");
            return;
        }
        if (WithdrawAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغ صحيح");
            return;
        }
        if (WithdrawCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

        try
        {
            IsBusy = true;
            await _investorService.WithdrawAsync(
                WithdrawInvestor.Id, WithdrawAmount, WithdrawDate, WithdrawCashBox.Id,
                string.IsNullOrWhiteSpace(WithdrawNotes) ? null : WithdrawNotes.Trim());

            BeautifulMessageDialog.ShowSuccess("تم تسجيل السحب بنجاح");
            WithdrawAmount = 0;
            WithdrawNotes = string.Empty;
            WithdrawDate = DateTime.Now;
            WithdrawInvestor = null;

            await LoadRecentWithdrawalsAsync();
            await LoadInvestorsAsync();
            await LoadInvestorsListAsync();
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════
    // TAB 3: PROFIT DISTRIBUTION (توزيع أرباح المستثمرين)
    // ══════════════════════════════════════════════════════

    [ObservableProperty]
    private DateTime _distributionDate = DateTime.Now;

    [ObservableProperty]
    private decimal _distributableProfits;

    [ObservableProperty]
    private decimal _totalToDistribute;

    [ObservableProperty]
    private CashBox? _distributionCashBox;

    [ObservableProperty]
    private bool _isPreviewReady;

    public ObservableCollection<ProfitPreviewItem> ProfitPreviews { get; } = [];

    [RelayCommand]
    private async Task PreviewDistributionAsync()
    {
        try
        {
            IsBusy = true;
            DistributableProfits = await _investorService.GetDistributableProfitsAsync();

            if (DistributableProfits <= 0)
            {
                BeautifulMessageDialog.ShowWarning("لا توجد أرباح متاحة للتوزيع");
                return;
            }

            var previews = await _investorService.PreviewProfitDistributionAsync(DistributionDate, DistributableProfits);
            ProfitPreviews.Clear();
            foreach (var p in previews)
                ProfitPreviews.Add(p);

            TotalToDistribute = ProfitPreviews.Where(p => p.IsIncluded).Sum(p => p.ProfitAmount);
            IsPreviewReady = ProfitPreviews.Count > 0;

            if (!IsPreviewReady)
                BeautifulMessageDialog.ShowWarning("لا يوجد مستثمرون مؤهلون (يجب مرور 15 يوم على الإيداع)");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void RecalculateTotal()
    {
        TotalToDistribute = ProfitPreviews.Where(p => p.IsIncluded).Sum(p => p.ProfitAmount);
    }

    [RelayCommand]
    private async Task ConfirmDistributionAsync()
    {
        if (DistributionCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

var confirmed = BeautifulMessageDialog.ShowConfirm(
                $"سيتم توزيع مبلغ {TotalToDistribute:N0} على المستثمرين المحددين\nهل تريد المتابعة؟");
            if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _investorService.DistributeProfitsAsync(
                DistributionDate, DistributionCashBox.Id,
                DistributableProfits, ProfitPreviews);

            BeautifulMessageDialog.ShowSuccess("تم توزيع الأرباح بنجاح");

            ProfitPreviews.Clear();
            IsPreviewReady = false;
            TotalToDistribute = 0;
            DistributableProfits = await _investorService.GetDistributableProfitsAsync();

            await LoadInvestorsAsync();
            await LoadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PrintDistributionPreview()
    {
        if (ProfitPreviews.Count == 0) return;

        var columns = new[] { "المستثمر", "الإيداع الكلي", "الإيداع المؤهل", "النسبة %", "الربح" };
        var rows = ProfitPreviews.Where(p => p.IsIncluded).Select(p => new object[]
        {
            p.InvestorName, p.TotalDeposit.ToString("N0"), p.EligibleDeposit.ToString("N0"),
            p.ProfitPercentage.ToString("N2"), p.ProfitAmount.ToString("N0")
        }).ToList();

        _exportService.PrintTable($"معاينة توزيع أرباح - {DistributionDate:yyyy/MM/dd}", columns, (IList<object[]>)rows);
    }

    // ══════════════════════════════════════════════════════
    // TAB 4: PROFIT STATEMENT (كشف أرباح المستثمر)
    // ══════════════════════════════════════════════════════

    [ObservableProperty]
    private Investor? _statementInvestor;

    [ObservableProperty]
    private decimal _statementTotalProfits;

    [ObservableProperty]
    private decimal _statementCurrentDeposit;

    public ObservableCollection<ProfitDistributionDetail> StatementDetails { get; } = [];

    partial void OnStatementInvestorChanged(Investor? value) => _ = LoadStatementAsync();

    private async Task LoadStatementAsync()
    {
        StatementDetails.Clear();
        StatementTotalProfits = 0;
        StatementCurrentDeposit = 0;

        if (StatementInvestor is null) return;

        try
        {
            IsBusy = true;
            var details = await _investorService.GetProfitDetailsForInvestorAsync(StatementInvestor.Id);
            foreach (var d in details)
                StatementDetails.Add(d);

            StatementTotalProfits = await _investorService.GetTotalProfitsEarnedAsync(StatementInvestor.Id);
            StatementCurrentDeposit = StatementInvestor.TotalDeposit;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PrintStatement()
    {
        if (StatementInvestor is null || StatementDetails.Count == 0) return;

        var columns = new[] { "التاريخ", "النسبة %", "المبلغ" };
        var rows = StatementDetails.Select(d => new object[]
        {
            d.ProfitDistribution.Date.ToString("yyyy/MM/dd"),
            d.ProfitPercentage.ToString("N2"),
            d.Amount.ToString("N0")
        }).ToList();

        _exportService.PrintTable($"كشف أرباح - {StatementInvestor.Name}", columns, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportStatement()
    {
        if (StatementInvestor is null || StatementDetails.Count == 0) return;

        var columns = new[] { "التاريخ", "النسبة %", "المبلغ" };
        var rows = StatementDetails.Select(d => new object[]
        {
            d.ProfitDistribution.Date.ToString("yyyy/MM/dd"),
            d.ProfitPercentage.ToString("N2"),
            d.Amount.ToString("N0")
        }).ToList();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"كشف_أرباح_{StatementInvestor.Name}_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportToExcel(dialog.FileName, "كشف أرباح", columns, (IList<object[]>)rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
    }

    // ══════════════════════════════════════════════════════
    // INITIALIZATION
    // ══════════════════════════════════════════════════════

    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Investors");

            await LoadInvestorsAsync();
            await LoadInvestorsListAsync();
            await LoadCashBoxesAsync();
            await LoadRecentDepositsAsync();
            await LoadRecentWithdrawalsAsync();
            DistributableProfits = await _investorService.GetDistributableProfitsAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadInvestorsAsync()
    {
        var investors = await _investorService.GetAllInvestorsAsync();
        Investors.Clear();
        foreach (var inv in investors)
        {
            var totalProfits = await _investorService.GetTotalProfitsEarnedAsync(inv.Id);
            Investors.Add(new InvestorRow
            {
                Id = inv.Id,
                Name = inv.Name,
                Phone = inv.Phone,
                TotalDeposit = inv.TotalDeposit,
                ProfitPercentage = inv.ProfitPercentage,
                TotalProfitsEarned = totalProfits
            });
        }
    }

    private async Task LoadInvestorsListAsync()
    {
        var investors = await _investorService.GetAllInvestorsAsync();
        InvestorsList.Clear();
        foreach (var inv in investors)
            InvestorsList.Add(inv);
    }

    private async Task LoadCashBoxesAsync()
    {
        var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
        CashBoxes.Clear();
        foreach (var cb in cashBoxes)
            CashBoxes.Add(cb);
    }

    private async Task LoadRecentDepositsAsync()
    {
        var deposits = await _investorService.GetRecentDepositsAsync();
        RecentDeposits.Clear();
        foreach (var d in deposits)
            RecentDeposits.Add(d);
    }

    private async Task LoadRecentWithdrawalsAsync()
    {
        var withdrawals = await _investorService.GetRecentWithdrawalsAsync();
        RecentWithdrawals.Clear();
        foreach (var w in withdrawals)
            RecentWithdrawals.Add(w);
    }
}

/// <summary>Display row for investor list with calculated TotalProfitsEarned</summary>
public class InvestorRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal TotalProfitsEarned { get; set; }
}
