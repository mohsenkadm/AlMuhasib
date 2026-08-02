using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldAuditReportViewModel : GoldReportViewModelBase
{
    private List<GoldAuditReportRow> _allRows = [];

    public ObservableCollection<GoldAuditReportRow> Rows { get; } = [];

    [ObservableProperty] private string? _entityNameFilter;
    [ObservableProperty] private string _eventCount = "0";
    [ObservableProperty] private string _userCount = "0";
    [ObservableProperty] private string _entityCount = "0";
    [ObservableProperty] private string _actionCount = "0";

    public GoldAuditReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        PageTitle = "سجل تدقيق الذهب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.AuditReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var entity = string.IsNullOrWhiteSpace(EntityNameFilter) ? null : EntityNameFilter.Trim();
            _allRows = (await ReportService.GetAuditReportAsync(DateFrom, DateTo, entity)).ToList();
            EventCount = _allRows.Count.ToString("N0");
            UserCount = _allRows.Select(r => r.UserName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString("N0");
            EntityCount = _allRows.Select(r => r.EntityName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString("N0");
            ActionCount = _allRows.Select(r => r.Action).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString("N0");
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var cols = new[] { "الوقت", "الإجراء", "الكيان", "المعرّف", "المستخدم", "التفاصيل" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Timestamp.ToString("yyyy/MM/dd HH:mm"), r.Action, r.EntityName,
            r.EntityId?.ToString() ?? "—", r.UserName, r.Details
        }).ToList();
        ExportTable("تدقيق_الذهب.xlsx", "سجل التدقيق", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الوقت", "الإجراء", "الكيان", "المستخدم", "التفاصيل" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Timestamp.ToString("yyyy/MM/dd HH:mm"), r.Action, r.EntityName, r.UserName, r.Details
        }).ToList();
        PrintTable("سجل تدقيق الذهب", cols, rows);
    }
}
