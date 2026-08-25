using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldPurchaseInvoiceViewModel
{
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private bool _isQuickAddSupplierOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerAddress = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;
    [ObservableProperty] private string _quickSupplierName = string.Empty;
    [ObservableProperty] private string _quickSupplierPhone = string.Empty;
    [ObservableProperty] private string _quickSupplierAddress = string.Empty;
    [ObservableProperty] private string _quickSupplierError = string.Empty;

    [RelayCommand]
    private void ShowSelectedCustomerDetails()
    {
        if (SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر زبوناً أولاً لعرض تفاصيله");
            return;
        }

        PartyQuickDetailDialog.ShowCustomer(_partyQuickDetail, SelectedCustomer.Id);
    }

    [RelayCommand]
    private void ShowSelectedSupplierDetails()
    {
        if (SelectedSupplier is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر مورداً أولاً لعرض تفاصيله");
            return;
        }

        PartyQuickDetailDialog.ShowSupplier(_partyQuickDetail, SelectedSupplier.Id);
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
    private void OpenQuickAddSupplier()
    {
        QuickSupplierName = string.Empty;
        QuickSupplierPhone = string.Empty;
        QuickSupplierAddress = string.Empty;
        QuickSupplierError = string.Empty;
        IsQuickAddSupplierOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddCustomer() => IsQuickAddCustomerOpen = false;

    [RelayCommand]
    private void CancelQuickAddSupplier() => IsQuickAddSupplierOpen = false;

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

            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == created.Id);
            IsQuickAddCustomerOpen = false;
            _toast.ShowSuccess("تم إضافة الزبون");
        }
        catch (Exception ex)
        {
            QuickCustomerError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveQuickSupplier()
    {
        if (string.IsNullOrWhiteSpace(QuickSupplierName))
        {
            QuickSupplierError = "اسم المورد مطلوب";
            return;
        }

        try
        {
            var created = await _supplierService.CreateAsync(new GoldSupplier
            {
                Name = QuickSupplierName.Trim(),
                Phone = QuickSupplierPhone?.Trim() ?? string.Empty,
                Address = QuickSupplierAddress?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedBy = _currentUserService.Username
            });

            Suppliers.Clear();
            var (items, _) = await _supplierService.GetPagedAsync(1, 500, activeOnly: true);
            foreach (var s in items)
                Suppliers.Add(s);

            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == created.Id);
            IsQuickAddSupplierOpen = false;
            _toast.ShowSuccess("تم إضافة المورد");
        }
        catch (Exception ex)
        {
            QuickSupplierError = ex.Message;
        }
    }
}
