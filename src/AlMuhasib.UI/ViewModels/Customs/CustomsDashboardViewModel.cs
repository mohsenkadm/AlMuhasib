using AlMuhasib.Core.Interfaces;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsDashboardViewModel : ViewModelBase
{
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private int _vehiclesInProgress;
    [ObservableProperty] private int _unpaidInvoices;

    public CustomsDashboardViewModel(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Dashboard);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoaded = false;
        try
        {
            // Placeholder KPIs until domain services/reports land in a later phase.
            CustomerCount = 0;
            InvoiceCount = 0;
            VehiclesInProgress = 0;
            UnpaidInvoices = 0;
            await Task.CompletedTask;
        }
        finally
        {
            IsLoaded = true;
        }
    }
}
