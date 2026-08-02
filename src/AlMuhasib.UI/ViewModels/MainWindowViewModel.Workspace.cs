using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private WorkspaceProfile _workspaceProfile;

    [ObservableProperty] private bool _showQuickPos = true;
    [ObservableProperty] private bool _showQuickSale = true;
    [ObservableProperty] private bool _showQuickPurchase = true;
    [ObservableProperty] private bool _showQuickReceipt = true;
    [ObservableProperty] private bool _showQuickPayment = true;
    [ObservableProperty] private bool _showQuickInstallments = true;
    [ObservableProperty] private bool _showQuickInstallmentInvoice = true;
    [ObservableProperty] private bool _showQuickReturn = true;
    [ObservableProperty] private bool _showQuickAssistant = true;
    [ObservableProperty] private bool _showQuickNewCarContract;
    [ObservableProperty] private bool _showQuickCarContractsList;
    [ObservableProperty] private bool _showQuickNewReservation;
    [ObservableProperty] private bool _showQuickCheckIn;

    public string WorkspaceProfileDisplay => WorkspaceProfile switch
    {
        WorkspaceProfile.Cashier => "كاشير",
        WorkspaceProfile.Accountant => "محاسب",
        _ => "كامل"
    };

    public void LoadWorkspaceProfile()
    {
        WorkspaceProfile = _userPreferences.Current.WorkspaceProfile;
        ApplyWorkspaceProfile();
    }

    partial void OnWorkspaceProfileChanged(WorkspaceProfile value) => ApplyWorkspaceProfile();

    private void ApplyWorkspaceProfile()
    {
        if (_moduleRegistry.IsHotelManagement)
        {
            ShowQuickPos = false;
            ShowQuickSale = false;
            ShowQuickPurchase = false;
            ShowQuickReceipt = false;
            ShowQuickPayment = false;
            ShowQuickInstallments = false;
            ShowQuickInstallmentInvoice = false;
            ShowQuickReturn = false;
            ShowQuickAssistant = false;
            ShowQuickNewCarContract = false;
            ShowQuickCarContractsList = false;
            ShowQuickNewReservation = true;
            ShowQuickCheckIn = true;
            return;
        }

        ShowQuickNewReservation = false;
        ShowQuickCheckIn = false;

        if (_moduleRegistry.IsCarContracts)
        {
            ShowQuickPos = false;
            ShowQuickSale = false;
            ShowQuickPurchase = false;
            ShowQuickReceipt = false;
            ShowQuickPayment = false;
            ShowQuickInstallments = false;
            ShowQuickInstallmentInvoice = false;
            ShowQuickReturn = false;
            ShowQuickAssistant = false;
            ShowQuickNewCarContract = true;
            ShowQuickCarContractsList = true;
            return;
        }

        if (_moduleRegistry.IsCarTrading)
        {
            ShowQuickPos = false;
            ShowQuickSale = false;
            ShowQuickPurchase = false;
            ShowQuickReceipt = false;
            ShowQuickPayment = false;
            ShowQuickInstallments = false;
            ShowQuickInstallmentInvoice = false;
            ShowQuickReturn = false;
            ShowQuickAssistant = false;
            ShowQuickNewCarContract = false;
            ShowQuickCarContractsList = false;
            return;
        }

        if (_moduleRegistry.IsGoldShop)
        {
            ShowQuickPos = false;
            ShowQuickSale = false;
            ShowQuickPurchase = false;
            ShowQuickReceipt = false;
            ShowQuickPayment = false;
            ShowQuickInstallments = false;
            ShowQuickInstallmentInvoice = false;
            ShowQuickReturn = false;
            ShowQuickAssistant = false;
            ShowQuickNewCarContract = false;
            ShowQuickCarContractsList = false;
            return;
        }

        ShowQuickNewCarContract = false;
        ShowQuickCarContractsList = false;

        switch (WorkspaceProfile)
        {
            case WorkspaceProfile.Cashier:
                ShowQuickPos = true;
                ShowQuickSale = true;
                ShowQuickPurchase = false;
                ShowQuickReceipt = true;
                ShowQuickPayment = false;
                ShowQuickInstallments = true;
                ShowQuickInstallmentInvoice = true;
                ShowQuickReturn = true;
                ShowQuickAssistant = true;
                break;
            case WorkspaceProfile.Accountant:
                ShowQuickPos = false;
                ShowQuickSale = true;
                ShowQuickPurchase = true;
                ShowQuickReceipt = true;
                ShowQuickPayment = true;
                ShowQuickInstallments = true;
                ShowQuickInstallmentInvoice = false;
                ShowQuickReturn = true;
                ShowQuickAssistant = true;
                break;
            default:
                ShowQuickPos = true;
                ShowQuickSale = true;
                ShowQuickPurchase = true;
                ShowQuickReceipt = true;
                ShowQuickPayment = true;
                ShowQuickInstallments = true;
                ShowQuickInstallmentInvoice = true;
                ShowQuickReturn = true;
                ShowQuickAssistant = true;
                break;
        }
    }

    [RelayCommand]
    private void SetWorkspaceProfileFull() => SaveWorkspaceProfile(WorkspaceProfile.Full);

    [RelayCommand]
    private void SetWorkspaceProfileCashier() => SaveWorkspaceProfile(WorkspaceProfile.Cashier);

    [RelayCommand]
    private void SetWorkspaceProfileAccountant() => SaveWorkspaceProfile(WorkspaceProfile.Accountant);

    private void SaveWorkspaceProfile(WorkspaceProfile profile)
    {
        WorkspaceProfile = profile;
        _userPreferences.Update(p => p.WorkspaceProfile = profile);
        ApplyWorkspaceProfile();
        IsQuickAssistOpen = false;
        _toast.ShowSuccess($"تم تطبيق ملف العمل: {WorkspaceProfileDisplay}");
    }

    [RelayCommand]
    private async Task SeedDemoDataAsync()
    {
        if (!_currentUserService.IsAdmin)
        {
            _toast.ShowWarning("تحميل البيانات التجريبية متاح للمسؤول فقط.");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                "سيتم إضافة منتجات وعملاء ومخزون تجريبي إذا كانت قاعدة البيانات فارغة من المنتجات.\nهل تريد المتابعة؟",
                "تحميل بيانات تجريبية"))
            return;

        IsQuickAssistOpen = false;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var demo = scope.ServiceProvider.GetRequiredService<IDemoDataService>();
            var result = await demo.TrySeedAsync();
            if (result.Success)
                _toast.ShowSuccess(result.Message);
            else
                _toast.ShowWarning(result.Message);
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
