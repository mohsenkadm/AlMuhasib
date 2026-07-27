using System.Collections.ObjectModel;
using System.Text;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductModificationsReportViewModel : SupervisoryReportViewModelBase
{
    public ProductModificationsReportViewModel(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(supervisoryService, exportService, currentUserService)
    {
        PageTitle = "تعديلات المنتجات";
    }

    public ObservableCollection<EntityChangeRow> Rows { get; } = [];
    public ObservableCollection<ChangeFieldDiff> SelectedDiffs { get; } = [];
    [ObservableProperty] private EntityChangeRow? _selectedRow;

    protected override async Task LoadUsersAsync()
    {
        Users.Clear();
        Users.Add("الكل");
        foreach (var user in await SupervisoryService.GetModifierUsernamesAsync("Product"))
            Users.Add(user);
        SelectedUser = "الكل";
    }

    protected override async Task ExecuteQueryAsync()
    {
        await RunQueryAsync(async () =>
        {
            var result = await SupervisoryService.GetProductModificationsAsync(BuildFilter(), CurrentPage, PageSize);
            ApplyPaginationStats(result.TotalCount);
            Rows.Clear();
            foreach (var row in result.Items) Rows.Add(row);
        });
    }

    [RelayCommand]
    private void ShowDetails(EntityChangeRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        SelectedDiffs.Clear();
        foreach (var diff in row.Diffs) SelectedDiffs.Add(diff);

        var sb = new StringBuilder();
        sb.AppendLine(row.EntityTitle);
        if (!string.IsNullOrWhiteSpace(row.ChangeSummary))
            sb.AppendLine(row.ChangeSummary);
        sb.AppendLine();
        sb.AppendLine($"تاريخ التعديل: {row.Timestamp:yyyy/MM/dd HH:mm}");
        sb.AppendLine($"المعدِّل: {row.ModifiedBy}");
        if (row.Diffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("الحقول المتغيرة:");
            foreach (var d in row.Diffs)
                sb.AppendLine($"• {d.Field}: {d.OldValue ?? "—"} ← {d.NewValue ?? "—"}");
        }

        ShowDetailsPanel($"تعديل منتج — {row.EntityKey}", sb.ToString());
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تعديلات_المنتجات.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "التاريخ", "المنتج", "ملخص التعديل", "المعدِّل", "القيم القديمة", "القيم الجديدة" };
        var data = Rows.Select(r => new object[]
        {
            r.Timestamp.ToString("yyyy/MM/dd HH:mm"),
            r.EntityTitle,
            r.ChangeSummary,
            r.ModifiedBy,
            r.OldValues ?? "",
            r.NewValues ?? ""
        }).ToList();
        ExportService.ExportToExcel(dlg.FileName, "تعديلات المنتجات", cols, (IList<object[]>)data);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}
