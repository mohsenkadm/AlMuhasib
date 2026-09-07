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
    private readonly IInvoiceService _invoiceService;

    public DeletedInvoicesReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IInvoiceService invoiceService)
        : base(supervisoryService, exportService, currentUserService)
    {
        _invoiceService = invoiceService;
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
        new("مرتجع مبيعات", InvoiceType.SaleReturn),
        new("تلف", InvoiceType.Damage),
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
    private async Task RestoreInvoice(DeletedInvoiceRow? row)
    {
        if (row is null) return;

        var confirmed = BeautifulMessageDialog.ShowConfirm(
            $"استرجاع الفاتورة {row.InvoiceNumber}؟\n\n" +
            "سيتم إعادة تفعيل الفاتورة وإرجاع تأثيرها على المخزون والصندوق " +
            "(نفس منطق الحذف المعكوس — الكمية تُعاد كما كانت قبل الحذف).",
            "تأكيد الاسترجاع");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            await _invoiceService.RestoreInvoiceAsync(row.Id);
            BeautifulMessageDialog.ShowSuccess($"تم استرجاع الفاتورة {row.InvoiceNumber} بنجاح");
            if (SelectedRow?.Id == row.Id)
                IsDetailsOpen = false;
            await ExecuteQueryAsync();
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

    [RelayCommand]
    private void PrintTable()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "رقم الفاتورة", "النوع", "الطرف", "المخزن", "المبلغ", "تاريخ الفاتورة", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceTypeDisplay, r.PartyName, r.WarehouseName,
            r.NetAmount.ToString("N0"), r.InvoiceDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.PrintTable("فواتير محذوفة", cols, (IList<object[]>)data);
    }
}

public record InvoiceTypeFilterItem(string Name, InvoiceType? Value)
{
    public override string ToString() => Name;
}
