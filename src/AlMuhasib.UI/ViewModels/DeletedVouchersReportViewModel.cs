using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedVouchersReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedVouchersReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "سندات محذوفة";
    }

    public ObservableCollection<DeletedVoucherRow> Rows { get; } = [];
    public ObservableCollection<VoucherTypeFilterItem> VoucherTypes { get; } =
    [
        new("الكل", null),
        new("قبض", VoucherType.Receipt),
        new("صرف", VoucherType.Payment),
        new("قبض مصرفي", VoucherType.BankReceipt),
        new("إيداع مستثمر", VoucherType.InvestorDeposit),
        new("سحب مستثمر", VoucherType.InvestorWithdrawal),
        new("قبض دين", VoucherType.DebtReceipt),
    ];

    [ObservableProperty] private VoucherTypeFilterItem? _selectedVoucherType;
    [ObservableProperty] private DeletedVoucherRow? _selectedRow;

    public override async Task InitializeAsync()
    {
        SelectedVoucherType = VoucherTypes[0];
        await base.InitializeAsync();
    }

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedVouchersAsync(
                BuildFilter(), CurrentPage, PageSize, SelectedVoucherType?.Value);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedVoucherRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"سند محذوف — {row.VoucherNumber}",
            $"{row.DetailsSummary}\n\nتاريخ السند: {row.VoucherDate:yyyy/MM/dd}\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}\nملاحظات: {row.Notes ?? "—"}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "سندات_محذوفة.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "رقم السند", "النوع", "الطرف", "القاصة", "المبلغ", "تاريخ السند", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.VoucherNumber, r.VoucherTypeDisplay, r.PartyName, r.CashBoxName,
            r.Amount.ToString("N0"), r.VoucherDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "سندات محذوفة", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "رقم السند", "النوع", "الطرف", "القاصة", "المبلغ", "تاريخ السند", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.VoucherNumber, r.VoucherTypeDisplay, r.PartyName, r.CashBoxName,
            r.Amount.ToString("N0"), r.VoucherDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.PrintTable("سندات محذوفة", cols, (IList<object[]>)data);
    }
}

public record VoucherTypeFilterItem(string Name, VoucherType? Value)
{
    public override string ToString() => Name;
}
