using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoyaltyLedgerViewModel : ViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IExportService _exportService;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private LoyaltyTypeOption? _selectedTypeOption;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<LoyaltyPointTransaction> Rows { get; } = [];

    public IReadOnlyList<LoyaltyTypeOption> TypeOptions { get; } =
    [
        new(null, "الكل"),
        new(LoyaltyTransactionType.Earn, "كسب"),
        new(LoyaltyTransactionType.Redeem, "استبدال"),
        new(LoyaltyTransactionType.Adjust, "تعديل"),
        new(LoyaltyTransactionType.Expire, "انتهاء")
    ];

    public sealed record LoyaltyTypeOption(LoyaltyTransactionType? Value, string Label)
    {
        public override string ToString() => Label;
    }

    public LoyaltyLedgerViewModel(
        ILoyaltyService loyaltyService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IExportService exportService)
    {
        _loyaltyService = loyaltyService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _exportService = exportService;
        PageTitle = "سجل حركات النقاط";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUser, "LoyaltyLedger");
        Customers.Clear();
        Customers.Add(new Customer { Id = 0, Name = "كل الزبائن" });
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(x => x.Name))
            Customers.Add(c);
        SelectedCustomer = Customers[0];
        SelectedTypeOption = TypeOptions[0];
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            int? customerId = SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null;
            var items = await _loyaltyService.GetLedgerAsync(customerId, SelectedTypeOption?.Value, DateFrom, DateTo);
            Rows.Clear();
            foreach (var row in items)
                Rows.Add(row);
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
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "سجل_نقاط_الولاء.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "التاريخ", "الزبون", "النوع", "النقاط", "المبلغ", "الرصيد بعد", "الفاتورة", "ملاحظة" };
        var rows = Rows.Select(r => new object[]
        {
            r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            r.Customer?.Name ?? r.CustomerId.ToString(),
            r.Type.ToString(),
            r.Points,
            r.CurrencyAmount,
            r.BalanceAfter,
            r.Invoice?.InvoiceNumber ?? "",
            r.Note ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "سجل الولاء", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}
