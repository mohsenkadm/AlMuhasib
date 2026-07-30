using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateClauseTemplatesViewModel : ViewModelBase
{
    private readonly IRealEstateClauseTemplateService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IExportService _exportService;
    private List<RealEstateClauseTemplate> _all = [];

    public ObservableCollection<RealEstateClauseTemplate> Templates { get; } = [];

    [ObservableProperty] private RealEstateClauseTemplate? _selected;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private int _editSortOrder = 1;
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editBody = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private bool _isEditOpen;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _totalCountText = "0";
    [ObservableProperty] private string _activeCountText = "0";

    public RealEstateClauseTemplatesViewModel(
        IRealEstateClauseTemplateService service,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IExportService exportService)
    {
        _service = service;
        _currentUserService = currentUserService;
        _toast = toast;
        _exportService = exportService;
        PageTitle = "بنود العقد";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.ClauseTemplates);
        await _service.EnsureDefaultsAsync();
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _all = (await _service.GetAllAsync()).ToList();
        TotalCountText = _all.Count.ToString("N0");
        ActiveCountText = _all.Count(t => t.IsActive).ToString("N0");
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Templates.Clear();
        IEnumerable<RealEstateClauseTemplate> source = _all;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            source = source.Where(t =>
                t.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Body.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            source = ColumnFilterEngine.Apply(source, ColumnFilters);

        foreach (var item in source)
            Templates.Add(item);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilter();

    [RelayCommand]
    private void OpenNew()
    {
        EditId = 0;
        EditSortOrder = _all.Count + 1;
        EditTitle = string.Empty;
        EditBody = string.Empty;
        EditIsActive = true;
        IsEditOpen = true;
    }

    [RelayCommand]
    private void OpenEdit(RealEstateClauseTemplate? item)
    {
        item ??= Selected;
        if (item is null) return;
        EditId = item.Id;
        EditSortOrder = item.SortOrder;
        EditTitle = item.Title;
        EditBody = item.Body;
        EditIsActive = item.IsActive;
        IsEditOpen = true;
    }

    [RelayCommand]
    private void CloseEdit() => IsEditOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTitle) || string.IsNullOrWhiteSpace(EditBody))
        {
            _toast.ShowWarning("العنوان والنص مطلوبان");
            return;
        }
        await _service.SaveAsync(new RealEstateClauseTemplate
        {
            Id = EditId,
            SortOrder = EditSortOrder,
            Title = EditTitle.Trim(),
            Body = EditBody.Trim(),
            IsActive = EditIsActive
        });
        IsEditOpen = false;
        _toast.ShowSuccess("تم الحفظ");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(RealEstateClauseTemplate? item)
    {
        item ??= Selected;
        if (item is null || !CanDelete) return;
        await _service.DeleteAsync(item.Id, _currentUserService.Username ?? "System");
        _toast.ShowSuccess("تم الحذف");
        await LoadAsync();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (!CanExport) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"ClauseTemplates_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        var headers = new[] { "الترتيب", "العنوان", "النص", "مفعّل" };
        var data = Templates.Select(t => new object?[] { t.SortOrder, t.Title, t.Body, t.IsActive ? "نعم" : "لا" }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "البنود", headers, data);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (!CanPrint) return;
        var headers = new[] { "الترتيب", "العنوان", "النص", "مفعّل" };
        var data = Templates.Select(t => new object?[] { t.SortOrder, t.Title, t.Body, t.IsActive ? "نعم" : "لا" }).ToList();
        _exportService.PrintTable("بنود العقد الجاهزة", headers, data);
    }
}
