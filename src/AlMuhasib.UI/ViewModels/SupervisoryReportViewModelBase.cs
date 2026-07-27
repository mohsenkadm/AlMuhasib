using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

/// <summary>أساس مشترك لتقارير المحذوفات والتعديلات الرقابية.</summary>
public abstract partial class SupervisoryReportViewModelBase : PagedViewModelBase
{
    protected readonly ISupervisoryReportService SupervisoryService;
    protected readonly IExportService ExportService;
    protected readonly ICurrentUserService CurrentUserService;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private string? _selectedUser;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private string _detailsTitle = string.Empty;
    [ObservableProperty] private string _detailsBody = string.Empty;

    public ObservableCollection<string> Users { get; } = [];

    protected SupervisoryReportViewModelBase(
        ISupervisoryReportService supervisoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        SupervisoryService = supervisoryService;
        ExportService = exportService;
        CurrentUserService = currentUserService;
        PageSize = 50;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, ScreenPermissionRegistry.SupervisoryReports);
        await LoadUsersAsync();
        await SearchAsync();
    }

    protected SupervisoryQueryFilter BuildFilter() => new()
    {
        FromDate = DateFrom,
        ToDate = DateTo,
        DeletedBy = string.IsNullOrWhiteSpace(SelectedUser) || SelectedUser == "الكل" ? null : SelectedUser,
        SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
    };

    protected virtual async Task LoadUsersAsync()
    {
        Users.Clear();
        Users.Add("الكل");
        foreach (var user in await SupervisoryService.GetDeletedByUsernamesAsync())
            Users.Add(user);
        SelectedUser = "الكل";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await ExecuteQueryAsync();
    }

    [RelayCommand]
    private void CloseDetails() => IsDetailsOpen = false;

    protected override Task OnPageChangedAsync() => ExecuteQueryAsync();

    protected abstract Task ExecuteQueryAsync();

    protected void ShowDetailsPanel(string title, string body)
    {
        DetailsTitle = title;
        DetailsBody = body;
        IsDetailsOpen = true;
    }

    protected async Task RunQueryAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
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
}
