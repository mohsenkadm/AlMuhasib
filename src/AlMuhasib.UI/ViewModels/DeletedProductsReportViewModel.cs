using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedProductsReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedProductsReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "منتجات محذوفة";
    }

    public ObservableCollection<DeletedProductRow> Rows { get; } = [];
    [ObservableProperty] private DeletedProductRow? _selectedRow;

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedProductsAsync(BuildFilter(), CurrentPage, PageSize);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedProductRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"منتج محذوف — {row.Name}",
            $"{row.DetailsSummary}\n\nالوصف: {row.Description ?? "—"}\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "منتجات_محذوفة.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "الاسم", "الباركود", "التصنيف", "الوصف", "تاريخ الحذف", "حذف بواسطة" };
        var data = Rows.Select(r => new object[]
        {
            r.Name, r.Barcode ?? "", r.CategoryName, r.Description ?? "",
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "منتجات محذوفة", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}
