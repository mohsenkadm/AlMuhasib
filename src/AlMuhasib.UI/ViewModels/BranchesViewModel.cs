using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class BranchesViewModel : ViewModelBase
{
    private readonly IBranchService _branchService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<BranchListRow> Branches { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private BranchListRow? _selectedBranch;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editCode = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private bool _editCodeReadOnly;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeactivateDialogOpen;
    [ObservableProperty] private BranchListRow? _branchToDeactivate;

    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _inactiveCount;

    private int? _editingId;
    private List<Branch> _allBranches = [];

    public BranchesViewModel(IBranchService branchService, ICurrentUserService currentUserService)
    {
        _branchService = branchService;
        _currentUserService = currentUserService;
        PageTitle = "الفروع";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, BranchPermissionScreens.Branches);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        _allBranches = (await _branchService.GetAllAsync()).ToList();
        ActiveCount = _allBranches.Count(b => b.IsActive);
        InactiveCount = _allBranches.Count(b => !b.IsActive);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = SearchText?.Trim();
        IEnumerable<Branch> query = _allBranches;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(b =>
                b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || b.Code.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        Branches.Clear();
        foreach (var b in query)
            Branches.Add(new BranchListRow(b));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة فرع جديد";
        EditName = string.Empty;
        EditCode = string.Empty;
        EditIsActive = true;
        EditCodeReadOnly = false;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(BranchListRow? row)
    {
        if (row?.Branch is null) return;
        var b = row.Branch;
        _editingId = b.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات الفرع";
        EditName = b.Name;
        EditCode = b.Code;
        EditIsActive = b.IsActive;
        EditCodeReadOnly = b.IsMain;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveBranch()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم الفرع مطلوب";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditCode))
        {
            DialogError = "رمز الفرع مطلوب";
            return;
        }

        DialogError = string.Empty;
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                await _branchService.UpdateAsync(_editingId.Value, EditName, EditCode, EditIsActive);
            }
            else
            {
                await _branchService.CreateAsync(EditName, EditCode);
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDeactivate(BranchListRow? row)
    {
        if (row?.Branch is null || row.Branch.IsMain) return;
        BranchToDeactivate = row;
        IsDeactivateDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDeactivate()
    {
        if (BranchToDeactivate?.Branch is null) return;
        try
        {
            await _branchService.DeactivateAsync(BranchToDeactivate.Branch.Id);
            IsDeactivateDialogOpen = false;
            BranchToDeactivate = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelDeactivate()
    {
        IsDeactivateDialogOpen = false;
        BranchToDeactivate = null;
    }
}

public sealed class BranchListRow
{
    public BranchListRow(Branch branch) => Branch = branch;

    public Branch Branch { get; }
    public int Id => Branch.Id;
    public string Name => Branch.Name;
    public string Code => Branch.Code;
    public bool IsActive => Branch.IsActive;
    public bool IsMain => Branch.IsMain;
    public string StatusDisplay => Branch.IsActive ? "فعال" : "غير فعال";
    public string TypeDisplay => Branch.IsMain ? "رئيسي" : "فرعي";
    public bool CanDeactivate => !Branch.IsMain && Branch.IsActive;
}
