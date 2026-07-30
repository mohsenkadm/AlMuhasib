using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedCustomersReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedCustomersReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "عملاء محذوفون";
    }

    public ObservableCollection<DeletedCustomerRow> Rows { get; } = [];
    [ObservableProperty] private DeletedCustomerRow? _selectedRow;

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedCustomersAsync(BuildFilter(), CurrentPage, PageSize);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedCustomerRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"عميل محذوف — {row.Name}",
            $"{row.DetailsSummary}\n\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "عملاء_محذوفون.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "الاسم", "الهاتف", "رقم الملف", "العنوان", "تاريخ الحذف", "حذف بواسطة" };
        var data = Rows.Select(r => new object[]
        {
            r.Name, r.Phone ?? "", r.FileNumber ?? "", r.Address ?? "",
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "عملاء محذوفون", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "الاسم", "الهاتف", "رقم الملف", "العنوان", "تاريخ الحذف", "حذف بواسطة" };
        var data = Rows.Select(r => new object[]
        {
            r.Name, r.Phone ?? "", r.FileNumber ?? "", r.Address ?? "",
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy
        }).ToList();
        ExportService.PrintTable("عملاء محذوفون", cols, (IList<object[]>)data);
    }
}
