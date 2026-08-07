using System.Collections.ObjectModel;
using System.Text;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class UserActivityProfileViewModel : PagedViewModelBase
{
    private readonly IUserActivityProfileService _profileService;
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<EntityChangeRow> ModificationRows { get; } = [];
    public ObservableCollection<UserDeletedActivityRow> DeletedRows { get; } = [];

    public ObservableCollection<DeletedKindDisplayItem> DeletedKindOptions { get; } =
    [
        new("الكل", "الكل"),
        new("Invoice", "فواتير"),
        new("Voucher", "سندات"),
        new("Product", "منتجات"),
        new("Customer", "عملاء"),
        new("Supplier", "موردون"),
        new("Expense", "مصروفات")
    ];

    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _roleDisplay = string.Empty;
    [ObservableProperty] private string _statusDisplay = string.Empty;
    [ObservableProperty] private string _lastLoginDisplay = "—";
    [ObservableProperty] private string _lastMachineDisplay = "—";

    [ObservableProperty] private int _modificationsCount;
    [ObservableProperty] private int _deletedCount;
    [ObservableProperty] private int _deletedInvoicesCount;
    [ObservableProperty] private int _totalActivityCount;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-3);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DeletedKindDisplayItem? _selectedDeletedKind;

    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private string _detailsTitle = string.Empty;
    [ObservableProperty] private string _detailsBody = string.Empty;

    [ObservableProperty] private int _modCurrentPage = 1;
    [ObservableProperty] private int _modTotalPages = 1;
    [ObservableProperty] private int _modTotalCount;
    [ObservableProperty] private string _modPaginationInfo = string.Empty;
    [ObservableProperty] private bool _modCanGoPrevious;
    [ObservableProperty] private bool _modCanGoNext;

    [ObservableProperty] private int _delCurrentPage = 1;
    [ObservableProperty] private int _delTotalPages = 1;
    [ObservableProperty] private int _delTotalCount;
    [ObservableProperty] private string _delPaginationInfo = string.Empty;
    [ObservableProperty] private bool _delCanGoPrevious;
    [ObservableProperty] private bool _delCanGoNext;

    private const int TabPageSize = 25;

    public UserActivityProfileViewModel(
        IUserActivityProfileService profileService,
        IInvoiceService invoiceService,
        ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _invoiceService = invoiceService;
        _currentUserService = currentUserService;
        PageTitle = "ملف المستخدم";
        SelectedDeletedKind = DeletedKindOptions[0];
    }

    public override async Task InitializeAsync()
    {
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        if (_currentUserService.UserId is not int userId)
        {
            BeautifulMessageDialog.ShowWarning("لا يوجد مستخدم مسجّل حالياً.");
            return;
        }

        try
        {
            IsBusy = true;
            var info = await _profileService.GetUserInfoAsync(userId);
            if (info is null)
            {
                BeautifulMessageDialog.ShowWarning("تعذّر تحميل بيانات المستخدم.");
                return;
            }

            FullName = string.IsNullOrWhiteSpace(info.FullName) ? info.Username : info.FullName;
            Username = info.Username;
            RoleDisplay = info.RoleDisplay;
            StatusDisplay = info.IsActive ? "نشط" : "موقوف";
            LastLoginDisplay = info.LastLoginAt?.ToString("yyyy/MM/dd HH:mm") ?? "—";
            LastMachineDisplay = string.IsNullOrWhiteSpace(info.LastLoginMachine) ? "—" : info.LastLoginMachine!;

            var stats = await _profileService.GetStatsAsync(info.Username, DateFrom, DateTo);
            ModificationsCount = stats.InvoiceModificationsCount;
            DeletedCount = stats.DeletedRecordsCount;
            DeletedInvoicesCount = stats.DeletedInvoicesCount;
            TotalActivityCount = stats.TotalActivityCount;

            ModCurrentPage = 1;
            DelCurrentPage = 1;
            await LoadModificationsAsync();
            await LoadDeletedAsync();
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

    [RelayCommand]
    private async Task SearchAsync()
    {
        ModCurrentPage = 1;
        DelCurrentPage = 1;
        try
        {
            IsBusy = true;
            var stats = await _profileService.GetStatsAsync(Username, DateFrom, DateTo);
            ModificationsCount = stats.InvoiceModificationsCount;
            DeletedCount = stats.DeletedRecordsCount;
            DeletedInvoicesCount = stats.DeletedInvoicesCount;
            TotalActivityCount = stats.TotalActivityCount;
            await LoadModificationsAsync();
            await LoadDeletedAsync();
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

    private async Task LoadModificationsAsync()
    {
        var (items, total) = await _profileService.GetInvoiceModificationsAsync(
            Username, DateFrom, DateTo, SearchText, ModCurrentPage, TabPageSize);
        ModificationRows.Clear();
        foreach (var row in items) ModificationRows.Add(row);
        ModTotalCount = total;
        ModTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)TabPageSize));
        if (ModCurrentPage > ModTotalPages) ModCurrentPage = ModTotalPages;
        ModCanGoPrevious = ModCurrentPage > 1;
        ModCanGoNext = ModCurrentPage < ModTotalPages;
        ModPaginationInfo = total == 0
            ? "لا توجد تعديلات"
            : $"عرض {(ModCurrentPage - 1) * TabPageSize + 1}-{Math.Min(ModCurrentPage * TabPageSize, total)} من {total}";
    }

    private async Task LoadDeletedAsync()
    {
        var kind = SelectedDeletedKind?.Id;
        var (items, total) = await _profileService.GetDeletedActivitiesAsync(
            Username, DateFrom, DateTo, SearchText, kind, DelCurrentPage, TabPageSize);
        DeletedRows.Clear();
        foreach (var row in items) DeletedRows.Add(row);
        DelTotalCount = total;
        DelTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)TabPageSize));
        if (DelCurrentPage > DelTotalPages) DelCurrentPage = DelTotalPages;
        DelCanGoPrevious = DelCurrentPage > 1;
        DelCanGoNext = DelCurrentPage < DelTotalPages;
        DelPaginationInfo = total == 0
            ? "لا توجد محذوفات"
            : $"عرض {(DelCurrentPage - 1) * TabPageSize + 1}-{Math.Min(DelCurrentPage * TabPageSize, total)} من {total}";
    }

    [RelayCommand]
    private async Task ModFirstPageAsync()
    {
        if (ModCurrentPage <= 1) return;
        ModCurrentPage = 1;
        await WithBusy(LoadModificationsAsync);
    }

    [RelayCommand]
    private async Task ModPreviousPageAsync()
    {
        if (!ModCanGoPrevious) return;
        ModCurrentPage--;
        await WithBusy(LoadModificationsAsync);
    }

    [RelayCommand]
    private async Task ModNextPageAsync()
    {
        if (!ModCanGoNext) return;
        ModCurrentPage++;
        await WithBusy(LoadModificationsAsync);
    }

    [RelayCommand]
    private async Task ModLastPageAsync()
    {
        if (ModCurrentPage >= ModTotalPages) return;
        ModCurrentPage = ModTotalPages;
        await WithBusy(LoadModificationsAsync);
    }

    [RelayCommand]
    private async Task DelFirstPageAsync()
    {
        if (DelCurrentPage <= 1) return;
        DelCurrentPage = 1;
        await WithBusy(LoadDeletedAsync);
    }

    [RelayCommand]
    private async Task DelPreviousPageAsync()
    {
        if (!DelCanGoPrevious) return;
        DelCurrentPage--;
        await WithBusy(LoadDeletedAsync);
    }

    [RelayCommand]
    private async Task DelNextPageAsync()
    {
        if (!DelCanGoNext) return;
        DelCurrentPage++;
        await WithBusy(LoadDeletedAsync);
    }

    [RelayCommand]
    private async Task DelLastPageAsync()
    {
        if (DelCurrentPage >= DelTotalPages) return;
        DelCurrentPage = DelTotalPages;
        await WithBusy(LoadDeletedAsync);
    }

    [RelayCommand]
    private async Task OpenModificationInvoiceAsync(EntityChangeRow? row)
    {
        if (row is null || row.EntityId <= 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد فاتورة مرتبطة بهذا التعديل.");
            return;
        }

        try
        {
            IsBusy = true;
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.EntityId)
                          ?? await _profileService.GetInvoiceIncludingDeletedAsync(row.EntityId);
            if (invoice is null)
            {
                ShowChangeDiffDetails(row);
                return;
            }

            var payment = invoice.InvoiceType == InvoiceType.Installment ? "أقساط" : null;
            InvoiceDetailDialog.Show(invoice, payment);
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

    [RelayCommand]
    private void ShowModificationDiff(EntityChangeRow? row)
    {
        if (row is null) return;
        ShowChangeDiffDetails(row);
    }

    private void ShowChangeDiffDetails(EntityChangeRow row)
    {
        var sb = new StringBuilder();
        sb.AppendLine(row.EntityTitle);
        sb.AppendLine(row.ChangeSummary);
        sb.AppendLine();
        sb.AppendLine($"تاريخ التعديل: {row.Timestamp:yyyy/MM/dd HH:mm}");
        sb.AppendLine($"المعدِّل: {row.ModifiedBy}");
        if (row.Diffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("التغييرات:");
            foreach (var d in row.Diffs)
                sb.AppendLine($"• {d.Field}: {d.OldValue ?? "—"} ← {d.NewValue ?? "—"}");
        }

        DetailsTitle = $"تعديل فاتورة — {row.EntityKey}";
        DetailsBody = sb.ToString();
        IsDetailsOpen = true;
    }

    [RelayCommand]
    private async Task OpenDeletedDetailsAsync(UserDeletedActivityRow? row)
    {
        if (row is null) return;

        if (row.IsInvoice)
        {
            try
            {
                IsBusy = true;
                var invoice = await _profileService.GetInvoiceIncludingDeletedAsync(row.EntityId);
                if (invoice is not null)
                {
                    var payment = invoice.InvoiceType == InvoiceType.Installment ? "أقساط" : null;
                    InvoiceDetailDialog.Show(invoice, payment);
                    return;
                }
            }
            catch (Exception ex)
            {
                BeautifulMessageDialog.ShowError(ex.Message);
                return;
            }
            finally
            {
                IsBusy = false;
            }
        }

        DetailsTitle = $"{row.EntityKindDisplay} محذوف — {row.Title}";
        DetailsBody =
            $"{row.DetailsSummary}\n\nتاريخ السجل: {row.EntityDate:yyyy/MM/dd}\nتاريخ الحذف: {row.DeletedAt:yyyy/MM/dd HH:mm}\nحُذف بواسطة: {row.DeletedBy}";
        IsDetailsOpen = true;
    }

    [RelayCommand]
    private void CloseDetails() => IsDetailsOpen = false;

    private async Task WithBusy(Func<Task> action)
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

    protected override Task OnPageChangedAsync() => Task.CompletedTask;
}

public record DeletedKindDisplayItem(string Id, string Name)
{
    public override string ToString() => Name;
}
