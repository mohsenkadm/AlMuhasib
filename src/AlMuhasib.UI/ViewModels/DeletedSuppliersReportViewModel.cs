using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedSuppliersReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedSuppliersReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "موردون محذوفون";
    }

    public ObservableCollection<DeletedSupplierRow> Rows { get; } = [];
    [ObservableProperty] private DeletedSupplierRow? _selectedRow;

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedSuppliersAsync(BuildFilter(), CurrentPage, PageSize);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedSupplierRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"مورد محذوف — {row.Name}",
            $"{row.DetailsSummary}\n\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "موردون_محذوفون.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "الاسم", "الهاتف", "العنوان", "تاريخ الحذف", "حذف بواسطة" };
        var data = Rows.Select(r => new object[]
        {
            r.Name, r.Phone ?? "", r.Address ?? "",
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "موردون محذوفون", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}
