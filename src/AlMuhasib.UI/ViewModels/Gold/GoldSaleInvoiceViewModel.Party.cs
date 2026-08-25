using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSaleInvoiceViewModel
{
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerAddress = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    [RelayCommand]
    private void ShowSelectedPartyDetails()
    {
        if (SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر زبوناً أولاً لعرض تفاصيله");
            return;
        }

        PartyQuickDetailDialog.ShowCustomer(_partyQuickDetail, SelectedCustomer.Id);
    }

    [RelayCommand]
    private void OpenQuickAddCustomer()
    {
        QuickCustomerName = string.Empty;
        QuickCustomerPhone = string.Empty;
        QuickCustomerAddress = string.Empty;
        QuickCustomerError = string.Empty;
        IsQuickAddCustomerOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddCustomer() => IsQuickAddCustomerOpen = false;

    [RelayCommand]
    private async Task SaveQuickCustomer()
    {
        if (string.IsNullOrWhiteSpace(QuickCustomerName))
        {
            QuickCustomerError = "اسم الزبون مطلوب";
            return;
        }

        try
        {
            var created = await _customerService.CreateAsync(new GoldCustomer
            {
                Name = QuickCustomerName.Trim(),
                Phone = QuickCustomerPhone?.Trim() ?? string.Empty,
                Address = QuickCustomerAddress?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedBy = _currentUserService.Username
            });

            Customers.Clear();
            var (items, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
            foreach (var c in items)
                Customers.Add(c);

            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == created.Id)
                ?? new GoldCustomerListItem
                {
                    Id = created.Id,
                    Name = created.Name,
                    Phone = created.Phone,
                    Address = created.Address
                };

            IsQuickAddCustomerOpen = false;
            _toast.ShowSuccess("تم إضافة الزبون");
        }
        catch (Exception ex)
        {
            QuickCustomerError = ex.Message;
        }
    }
}
