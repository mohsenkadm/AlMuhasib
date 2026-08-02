using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Loyalty;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoyaltyAccountsViewModel : ViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _totalBalanceText = "0";
    [ObservableProperty] private string _totalEarnedText = "0";
    [ObservableProperty] private string _totalRedeemedText = "0";
    [ObservableProperty] private string _accountsCountText = "0";
    [ObservableProperty] private LoyaltyAccountRow? _selectedAccount;
    [ObservableProperty] private bool _isAdjustOpen;
    [ObservableProperty] private int _adjustDelta;
    [ObservableProperty] private string _adjustNote = string.Empty;

    public ObservableCollection<LoyaltyAccountRow> Rows { get; } = [];

    public LoyaltyAccountsViewModel(ILoyaltyService loyaltyService, ICurrentUserService currentUser)
    {
        _loyaltyService = loyaltyService;
        _currentUser = currentUser;
        PageTitle = "حسابات ولاء الزبائن";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUser, "LoyaltyAccounts");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var items = await _loyaltyService.GetAccountsAsync(SearchText);
            Rows.Clear();
            foreach (var row in items)
                Rows.Add(row);

            TotalBalanceText = items.Sum(x => x.PointsBalance).ToString("N0");
            TotalEarnedText = items.Sum(x => x.LifetimeEarned).ToString("N0");
            TotalRedeemedText = items.Sum(x => x.LifetimeRedeemed).ToString("N0");
            AccountsCountText = items.Count.ToString("N0");
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
    private void OpenAdjust()
    {
        if (SelectedAccount is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر حساب زبون أولاً");
            return;
        }

        AdjustDelta = 0;
        AdjustNote = string.Empty;
        IsAdjustOpen = true;
    }

    [RelayCommand]
    private void CloseAdjust() => IsAdjustOpen = false;

    [RelayCommand]
    private async Task ConfirmAdjustAsync()
    {
        if (SelectedAccount is null) return;
        try
        {
            IsBusy = true;
            await _loyaltyService.AdjustPointsAsync(
                SelectedAccount.CustomerId,
                AdjustDelta,
                AdjustNote,
                _currentUser.UserId);
            IsAdjustOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم تعديل رصيد النقاط");
            await LoadAsync();
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
}
