using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DeletedExpensesReportViewModel : SupervisoryReportViewModelBase
{
    public DeletedExpensesReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "مصاريف محذوفة";
    }

    public ObservableCollection<DeletedExpenseRow> Rows { get; } = [];
    [ObservableProperty] private DeletedExpenseRow? _selectedRow;

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetDeletedExpensesAsync(BuildFilter(), CurrentPage, PageSize);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(DeletedExpenseRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        ShowDetailsPanel(
            $"مصروف محذوف — {row.ExpenseTypeName}",
            $"{row.DetailsSummary}\n\nتاريخ المصروف: {row.ExpenseDate:yyyy/MM/dd}\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}");
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "مصاريف_محذوفة.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "النوع", "المبلغ", "القاصة", "تاريخ المصروف", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.ExpenseTypeName, r.Amount.ToString("N0"), r.CashBoxName,
            r.ExpenseDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "مصاريف محذوفة", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "النوع", "المبلغ", "القاصة", "تاريخ المصروف", "تاريخ الحذف", "حذف بواسطة", "ملاحظات" };
        var data = Rows.Select(r => new object[]
        {
            r.ExpenseTypeName, r.Amount.ToString("N0"), r.CashBoxName,
            r.ExpenseDate.ToString("yyyy/MM/dd"),
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "", r.DeletedBy, r.Notes ?? ""
        }).ToList();
        ExportService.PrintTable("مصاريف محذوفة", cols, (IList<object[]>)data);
    }
}
