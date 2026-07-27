using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedInvoicesReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedInvoicesReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "فواتير محذوفة";
    }

    public ObservableCollection<DeletedInvoiceRow> Rows { get; } = [];
    public ObservableCollection<InvoiceTypeFilterItem> InvoiceTypes { get; } =
    [
        new("الكل", null),
        new("مبيعات", InvoiceType.Sale),
        new("مشتريات", InvoiceType.Purchase),
        new("أقساط", InvoiceType.Installment),
        new("مرتجع مشتريات", InvoiceType.PurchaseReturn),
    ];

    [ObservableProperty] private InvoiceTypeFilterItem? _selectedInvoiceType;
    [ObservableProperty] private DeletedInvoiceRow? _selectedRow;

    public override async Task InitializeAsync()
    {
        SelectedInvoiceType = InvoiceTypes[0];
        await base.InitializeAsync();
    }

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedInvoicesAsync(
                BuildFilter(), CurrentPage, PageSize, SelectedInvoiceType?.Value);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedInvoiceRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"فاتورة محذوفة — {row.InvoiceNumber}",
            $"{row.DetailsSummary}\n\nتاريخ الفاتورة: {row.InvoiceDate:yyyy/MM/dd}\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذفت بواسطة: {row.DeletedBy}\nملاحظات: {row.Notes ?? "—"}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "فواتير_محذوفة.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "رقم الفاتورة", "النوع", "الطرف", "المخزن", "المبلغ", "تاريخ الفاتورة", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceTypeDisplay, r.PartyName, r.WarehouseName,
            r.NetAmount.ToString("N0"), r.InvoiceDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "فواتير محذوفة", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}

public record InvoiceTypeFilterItem(string Name, InvoiceType? Value)
{
    public override string ToString() => Name;
}
