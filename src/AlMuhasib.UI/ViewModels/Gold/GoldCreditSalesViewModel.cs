using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCreditSalesViewModel : PagedViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldInvoiceListItem> Invoices { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "مفتوح + جزئي"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "مدفوع جزئياً")
    ];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private GoldInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private int _openCount;

    public GoldCreditSalesViewModel(
        IGoldSaleService saleService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "مبيعات الآجل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CreditSales);
        await LoadAsync();
    }

    protected override Task OnPageChangedAsync() => LoadAsync();

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (StatusFilter is null)
            {
                // Load both Open and PartiallyPaid, then paginate client-side
                var (openItems, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.Open);
                var (partialItems, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.PartiallyPaid);
                var combined = openItems.Concat(partialItems)
                    .Where(i => i.PaymentMethod == GoldPaymentMethod.Credit || i.RemainingAmount > 0)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.Id)
                    .ToList();

                ApplyPaginationStats(combined.Count);
                var pageItems = combined.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                Invoices.Clear();
                foreach (var item in pageItems)
                    Invoices.Add(item);

                TotalRemaining = combined.Sum(i => i.RemainingAmount);
                OpenCount = combined.Count;
            }
            else
            {
                var (items, total) = await _saleService.GetPagedAsync(
                    CurrentPage, PageSize, SearchText, status: StatusFilter);
                var filtered = items.Where(i => i.PaymentMethod == GoldPaymentMethod.Credit || i.RemainingAmount > 0).ToList();
                Invoices.Clear();
                foreach (var item in filtered)
                    Invoices.Add(item);
                ApplyPaginationStats(total);
                TotalRemaining = filtered.Sum(i => i.RemainingAmount);
                OpenCount = total;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnStatusFilterChanged(GoldInvoiceStatus? value) => _ = SearchAsync();
}
