using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class PermissionsViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public PermissionsViewModel(IAuthService authService)
    {
        _authService = authService;
        PageTitle = "الصلاحيات";
        InitializeScreens();
    }

    // ── User Selection ──────────────────────────────────

    public ObservableCollection<UserRow> Users { get; } = [];

    [ObservableProperty]
    private UserRow? _selectedUser;

    partial void OnSelectedUserChanged(UserRow? value)
    {
        if (value is not null)
            _ = LoadPermissionsAsync();
    }

    // ── Permission Grid ─────────────────────────────────

    public ObservableCollection<ScreenPermissionRow> Screens { get; } = [];

    // All available screens with Arabic labels
    private static readonly (string Name, string Label)[] AllScreens =
    [
        ("Dashboard", "لوحة التحكم"),
        ("Products", "المنتجات"),
        ("Categories", "تصنيفات المنتجات"),
        ("Customers", "العملاء"),
        ("Suppliers", "الموردون"),
        ("PurchaseInvoice", "فاتورة مشتريات"),
        ("SaleInvoice", "فاتورة مبيعات"),
        ("InstallmentInvoice", "فاتورة أقساط"),
        ("Installments", "الأقساط"),
        ("OpeningInstallments", "أرصدة الأقساط الافتتاحية"),
        ("Vouchers", "السندات"),
        ("Expenses", "المصاريف"),
        ("CashAndBank", "القاصات والمصرف"),
        ("Investors", "المستثمرون"),
        ("OpeningInvestors", "أرصدة المستثمرين الافتتاحية"),
        ("Warehouses", "المخازن"),
        ("OpeningStock", "الأرصدة الافتتاحية"),
        ("StockAdjustment", "تسوية مخزنية"),
        ("Reports", "التقارير"),
        ("BalanceSheet", "موازنة يومية"),
    ];

    private void InitializeScreens()
    {
        Screens.Clear();
        foreach (var (name, label) in AllScreens)
        {
            Screens.Add(new ScreenPermissionRow { ScreenName = name, ScreenLabel = label });
        }
    }

    // ── Load Users ──────────────────────────────────────

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            var users = await _authService.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in users)
            {
                Users.Add(new UserRow
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    RoleDisplay = u.Role == Core.Enums.UserRole.Admin ? "مدير" : "مستخدم",
                    IsActive = u.IsActive,
                    StatusDisplay = u.IsActive ? "فعال" : "معطّل"
                });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Load Permissions ────────────────────────────────

    [RelayCommand]
    private async Task LoadPermissionsAsync()
    {
        if (SelectedUser is null) return;
        try
        {
            IsBusy = true;
            var permissions = await _authService.GetUserPermissionsAsync(SelectedUser.Id);

            // Reset all
            foreach (var s in Screens)
            {
                s.CanView = false;
                s.CanAdd = false;
                s.CanEdit = false;
                s.CanDelete = false;
                s.CanPrint = false;
                s.CanExport = false;
            }

            // Apply saved permissions
            foreach (var p in permissions)
            {
                var screen = Screens.FirstOrDefault(s => s.ScreenName == p.ScreenName);
                if (screen is null) continue;
                screen.CanView = p.CanView;
                screen.CanAdd = p.CanAdd;
                screen.CanEdit = p.CanEdit;
                screen.CanDelete = p.CanDelete;
                screen.CanPrint = p.CanPrint;
                screen.CanExport = p.CanExport;
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Save Permissions ────────────────────────────────

    [RelayCommand]
    private async Task SavePermissionsAsync()
    {
        if (SelectedUser is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار مستخدم أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var permissions = Screens.Select(s => new Permission
            {
                ScreenName = s.ScreenName,
                CanView = s.CanView,
                CanAdd = s.CanAdd,
                CanEdit = s.CanEdit,
                CanDelete = s.CanDelete,
                CanPrint = s.CanPrint,
                CanExport = s.CanExport
            }).ToList();

            await _authService.SaveUserPermissionsAsync(SelectedUser.Id, permissions);
            BeautifulMessageDialog.ShowSuccess("تم حفظ الصلاحيات بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Select / Deselect All ───────────────────────────

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var s in Screens)
        {
            s.CanView = true;
            s.CanAdd = true;
            s.CanEdit = true;
            s.CanDelete = true;
            s.CanPrint = true;
            s.CanExport = true;
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var s in Screens)
        {
            s.CanView = false;
            s.CanAdd = false;
            s.CanEdit = false;
            s.CanDelete = false;
            s.CanPrint = false;
            s.CanExport = false;
        }
    }

    // ── Init ────────────────────────────────────────────

    public override async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }
}

// ── Display Model ───────────────────────────────────────

public partial class ScreenPermissionRow : ObservableObject
{
    public string ScreenName { get; set; } = string.Empty;
    public string ScreenLabel { get; set; } = string.Empty;

    [ObservableProperty] private bool _canView;
    [ObservableProperty] private bool _canAdd;
    [ObservableProperty] private bool _canEdit;
    [ObservableProperty] private bool _canDelete;
    [ObservableProperty] private bool _canPrint;
    [ObservableProperty] private bool _canExport;
}
