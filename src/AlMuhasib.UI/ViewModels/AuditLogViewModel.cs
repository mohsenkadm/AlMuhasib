using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class AuditLogViewModel : ViewModelBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthService _authService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    private static readonly JsonSerializerOptions _prettyJson = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public AuditLogViewModel(IAuditLogService auditLogService, IAuthService authService, IExportService exportService, ICurrentUserService currentUserService)
    {
        _auditLogService = auditLogService;
        _authService = authService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "سجل العمليات";
    }

    // ── Filters ──

    [ObservableProperty] private UserFilterItem? _selectedUser;
    [ObservableProperty] private AuditActionFilterItem? _selectedAction;
    [ObservableProperty] private string? _selectedEntity;
    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;

    public ObservableCollection<UserFilterItem> Users { get; } = [];
    public ObservableCollection<AuditActionFilterItem> Actions { get; } =
    [
        new("الكل", null),
        new("إضافة", AuditAction.Add),
        new("تعديل", AuditAction.Edit),
        new("حذف", AuditAction.Delete),
    ];
    public ObservableCollection<string> EntityNames { get; } = [];

    // ── Data ──

    public ObservableCollection<AuditLogRow> Rows { get; } = [];

    [ObservableProperty] private AuditLogRow? _selectedRow;
    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private string _detailsOldValues = string.Empty;
    [ObservableProperty] private string _detailsNewValues = string.Empty;

    // ── Paging ──

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    private const int PageSize = 50;

    // ── Commands ──

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            await LoadLookupsAsync();
            await ExecuteQueryAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await ExecuteQueryAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await ExecuteQueryAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await ExecuteQueryAsync();
        }
    }

    [RelayCommand]
    private void ShowDetails(AuditLogRow? row)
    {
        if (row is null) return;
        SelectedRow = row;
        DetailsOldValues = FormatJson(row.OldValues);
        DetailsNewValues = FormatJson(row.NewValues);
        IsDetailsOpen = true;
    }

    [RelayCommand]
    private void CloseDetails()
    {
        IsDetailsOpen = false;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (Rows.Count == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "سجل_العمليات.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "التاريخ", "المستخدم", "العملية", "الجدول", "معرف السجل", "القيم القديمة", "القيم الجديدة" };
        var rows = Rows.Select(r => new object[]
        {
            r.Timestamp.ToString("yyyy/MM/dd HH:mm"),
            r.Username,
            r.ActionDisplay,
            r.EntityName,
            r.EntityId,
            r.OldValues ?? "",
            r.NewValues ?? ""
        }).ToList();

        _exportService.ExportToExcel(dlg.FileName, "سجل العمليات", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintAuditLog()
    {
        if (Rows.Count == 0) return;
        var cols = new[] { "التاريخ", "المستخدم", "العملية", "الجدول", "معرف السجل" };
        var rows = Rows.Select(r => new object[]
        {
            r.Timestamp.ToString("yyyy/MM/dd HH:mm"),
            r.Username,
            r.ActionDisplay,
            r.EntityName,
            r.EntityId
        }).ToList();
        _exportService.PrintTable("سجل العمليات", cols, (IList<object[]>)rows);
    }

    // ── Helpers ──

    private async Task ExecuteQueryAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _auditLogService.QueryAsync(
                userId: SelectedUser?.Id,
                action: SelectedAction?.Value,
                entityName: string.IsNullOrEmpty(SelectedEntity) ? null : SelectedEntity,
                from: DateFrom,
                to: DateTo,
                page: CurrentPage,
                pageSize: PageSize);

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));

            Rows.Clear();
            foreach (var r in result.Rows) Rows.Add(r);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    private async Task LoadLookupsAsync()
    {
        if (Users.Count == 0)
        {
            Users.Add(new UserFilterItem("الكل", null));
            var allUsers = await _authService.GetAllUsersAsync();
            foreach (var u in allUsers)
                Users.Add(new UserFilterItem(u.FullName, u.Id));
        }

        if (EntityNames.Count == 0)
        {
            EntityNames.Add(string.Empty); // "الكل"
            var names = await _auditLogService.GetDistinctEntityNamesAsync();
            foreach (var n in names)
                EntityNames.Add(n);
        }
    }

    private static string FormatJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "—";
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, _prettyJson);
        }
        catch
        {
            return json;
        }
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "AuditLog");

        await LoadAsync();
    }
}

public record UserFilterItem(string Display, int? Id)
{
    public override string ToString() => Display;
}

public record AuditActionFilterItem(string Display, AuditAction? Value)
{
    public override string ToString() => Display;
}
