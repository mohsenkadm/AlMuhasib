using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningInvestorsViewModel : ViewModelBase
{
    private readonly IInvestorService _investorService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<OpeningInvestorRow> Rows { get; } = [];

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public OpeningInvestorsViewModel(IInvestorService investorService, ICurrentUserService currentUserService)
    {
        _investorService = investorService;
        _currentUserService = currentUserService;
        PageTitle = "أرصدة المستثمرين الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningInvestors");
            await LoadInvestorsAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadInvestorsAsync()
    {
        var investors = await _investorService.GetAllInvestorsAsync();
        Rows.Clear();
        foreach (var inv in investors)
        {
            Rows.Add(new OpeningInvestorRow
            {
                InvestorId = inv.Id,
                Name = inv.Name,
                Phone = inv.Phone ?? string.Empty,
                ProfitPercentage = inv.ProfitPercentage,
                OpeningBalance = inv.OpeningBalance,
                TotalDeposit = inv.TotalDeposit
            });
        }
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (Rows.Count == 0)
        {
            ErrorMessage = "لا يوجد مستثمرون. أضف مستثمرين من شاشة المستثمرون أولاً.";
            return;
        }

        if (Rows.Any(r => string.IsNullOrWhiteSpace(r.Name)))
        {
            ErrorMessage = "يرجى إدخال اسم لكل مستثمر";
            return;
        }

        if (Rows.Any(r => r.OpeningBalance < 0))
        {
            ErrorMessage = "الرصيد الافتتاحي لا يمكن أن يكون سالباً";
            return;
        }

        IsBusy = true;
        try
        {
            var items = Rows.Select(r => new InvestorOpeningBalanceItem
            {
                InvestorId = r.InvestorId,
                Name = r.Name,
                Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone,
                ProfitPercentage = r.ProfitPercentage,
                OpeningBalance = r.OpeningBalance
            }).ToList();

            await _investorService.SaveOpeningBalancesAsync(items);
            await LoadInvestorsAsync();
            BeautifulMessageDialog.ShowSuccess("تم حفظ أرصدة المستثمرين الافتتاحية بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadInvestorsAsync();
}

public partial class OpeningInvestorRow : ObservableObject
{
    [ObservableProperty] private int _investorId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private decimal _profitPercentage;
    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private decimal _totalDeposit;
}
