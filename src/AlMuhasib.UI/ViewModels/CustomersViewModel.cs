using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly IDataImportService _importService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IFeatureFlagService _featureFlags;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<SalesRepresentative> SalesRepresentatives { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private string _paginationText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private bool _isCardView;

    // Dialog state
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _dialogTitle = string.Empty;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editPhone = string.Empty;

    [ObservableProperty]
    private string _editAddress = string.Empty;

    [ObservableProperty]
    private string _editFileNumber = string.Empty;

    [ObservableProperty]
    private string _editNotes = string.Empty;

    [ObservableProperty]
    private SalesRepresentative? _editSalesRepresentative;

    [ObservableProperty]
    private bool _showSalesRepSelection;

    [ObservableProperty]
    private string _dialogError = string.Empty;

    // Delete confirmation
    [ObservableProperty]
    private bool _isDeleteDialogOpen;

    [ObservableProperty]
    private Customer? _customerToDelete;

    private int? _editingCustomerId;
    private System.Timers.Timer? _debounceTimer;

    public CustomersViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        IDataImportService importService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        ICustomFieldSettingsService customFieldSettings,
        IFeatureFlagService featureFlags)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _importService = importService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _featureFlags = featureFlags;
        ShowSalesRepSelection = _featureFlags.SalesRepresentatives;
        _featureFlags.FlagsChanged += (_, _) =>
            ShowSalesRepSelection = _featureFlags.SalesRepresentatives;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Customers);
        PageTitle = "العملاء";
        ConfigureCustomFields(customFieldSettings);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Customers");
            await LoadCustomFieldDefinitionsAsync();
            await LoadSalesRepresentativesAsync();
            await LoadCustomersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSalesRepresentativesAsync()
    {
        SalesRepresentatives.Clear();
        if (!_featureFlags.SalesRepresentatives) return;
        var reps = await _unitOfWork.SalesRepresentatives.GetAllAsync();
        foreach (var r in reps.Where(x => x.IsActive).OrderBy(x => x.Name))
            SalesRepresentatives.Add(r);
    }

    private async Task LoadCustomersAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText)
            ? null
            : SearchText.Trim();

        System.Linq.Expressions.Expression<Func<Customer, bool>>? searchPredicate = filter is null
            ? null
            : c => c.Name.Contains(filter) || (c.Phone != null && c.Phone.Contains(filter)) || (c.FileNumber != null && c.FileNumber.Contains(filter));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.Customers.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(c => c.CreatedAt));

            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Customers, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _unitOfWork.Customers.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(c => c.CreatedAt));

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Customers.Clear();
        foreach (var c in items)
            Customers.Add(c);
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadCustomersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadCustomersAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadCustomersAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadCustomersAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadCustomersAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadCustomersAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task OpenAddDialog()
    {
        _editingCustomerId = null;
        IsEditMode = false;
        DialogTitle = "إضافة عميل جديد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditFileNumber = string.Empty;
        EditNotes = string.Empty;
        EditSalesRepresentative = null;
        DialogError = string.Empty;
        await ResetCustomFieldEditorsAsync(null);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(Customer customer)
    {
        if (customer is null) return;
        _editingCustomerId = customer.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات العميل";
        EditName = customer.Name;
        EditPhone = customer.Phone ?? string.Empty;
        EditAddress = customer.Address ?? string.Empty;
        EditFileNumber = customer.FileNumber ?? string.Empty;
        EditNotes = customer.Notes ?? string.Empty;
        EditSalesRepresentative = customer.SalesRepresentativeId is int rid
            ? SalesRepresentatives.FirstOrDefault(r => r.Id == rid)
            : null;
        DialogError = string.Empty;
        await ResetCustomFieldEditorsAsync(customer.CustomFieldsJson);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveCustomer()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم العميل مطلوب";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingCustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(_editingCustomerId.Value);
                if (customer is null) return;

                customer.Name = EditName.Trim();
                customer.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
                customer.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
                customer.FileNumber = string.IsNullOrWhiteSpace(EditFileNumber) ? null : EditFileNumber.Trim();
                customer.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
                customer.SalesRepresentativeId = ShowSalesRepSelection ? EditSalesRepresentative?.Id : null;
                customer.CustomFieldsJson = SerializeCustomFieldsFromEditors();
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var name = EditName.Trim();
                var phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
                var address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
                var fileNumber = string.IsNullOrWhiteSpace(EditFileNumber) ? null : EditFileNumber.Trim();
                var notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
                var salesRepId = ShowSalesRepSelection ? EditSalesRepresentative?.Id : null;
                var customFields = SerializeCustomFieldsFromEditors();

                Customer? softDeleted = null;
                if (!string.IsNullOrWhiteSpace(fileNumber))
                {
                    softDeleted = await _unitOfWork.Customers.FindSoftDeletedFirstAsync(
                        c => c.FileNumber == fileNumber);
                }

                if (softDeleted is null && !await _unitOfWork.Customers.AnyAsync(c => c.Name == name))
                {
                    softDeleted = await _unitOfWork.Customers.FindSoftDeletedFirstAsync(c => c.Name == name);
                }

                if (softDeleted is not null)
                {
                    softDeleted.RestoreFromSoftDelete(_currentUserService.Username);
                    softDeleted.Name = name;
                    softDeleted.Phone = phone;
                    softDeleted.Address = address;
                    softDeleted.FileNumber = fileNumber;
                    softDeleted.Notes = notes;
                    softDeleted.SalesRepresentativeId = salesRepId;
                    softDeleted.CustomFieldsJson = customFields;
                    _unitOfWork.Customers.Update(softDeleted);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    var customer = new Customer
                    {
                        Name = name,
                        Phone = phone,
                        Address = address,
                        FileNumber = fileNumber,
                        Notes = notes,
                        SalesRepresentativeId = salesRepId,
                        CustomFieldsJson = customFields,
                        CreatedBy = _currentUserService.Username
                    };

                    await _unitOfWork.Customers.AddAsync(customer);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            IsDialogOpen = false;
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(Customer customer)
    {
        if (customer is null) return;
        CustomerToDelete = customer;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (CustomerToDelete is null) return;
        try
        {
            _unitOfWork.Customers.SoftDelete(CustomerToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            CustomerToDelete = null;
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        CustomerToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Customers.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(c => new
            {
                الاسم = c.Name,
                الهاتف = c.Phone ?? "",
                العنوان = c.Address ?? "",
                رقم_الملف = c.FileNumber ?? "",
                ملاحظات = c.Notes ?? "",
                تاريخ_الإنشاء = c.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"العملاء_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "العملاء");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DownloadImportTemplate()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "قالب_العملاء.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true)
                return;

            _importService.SaveCustomerTemplate(dialog.FileName);
            BeautifulMessageDialog.ShowSuccess("تم حفظ قالب الاستيراد بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء حفظ القالب: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة عملاء");
            return;
        }

        try
        {
            var open = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "استيراد العملاء من Excel"
            };
            if (open.ShowDialog() != true)
                return;

            var preview = await _importService.PreviewCustomersAsync(open.FileName);
            var confirm = BeautifulMessageDialog.ShowConfirm(
                $"معاينة الملف: {preview.RowCount} صف.\nهل تريد استيراد العملاء الآن؟\n(الأسماء الموجودة مسبقاً سيتم تخطيها)");
            if (!confirm)
                return;

            var result = await _importService.ImportCustomersAsync(open.FileName);
            var msg = $"تم استيراد {result.ImportedCount} عميل، وتخطي {result.SkippedCount}.";
            if (result.Errors.Count > 0)
                msg += $"\nأخطاء: {result.Errors.Count}\n" + string.Join("\n", result.Errors.Take(8));

            if (result.Errors.Count > 0 && result.ImportedCount == 0)
                BeautifulMessageDialog.ShowError(msg);
            else
                BeautifulMessageDialog.ShowSuccess(msg);

            CurrentPage = 1;
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الاستيراد: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Customers.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "الاسم", "الهاتف", "العنوان", "رقم الملف", "ملاحظات", "تاريخ الإنشاء" };
            IList<object[]> rows = allItems.Select(c => new object[]
            {
                c.Name,
                c.Phone ?? "",
                c.Address ?? "",
                c.FileNumber ?? "",
                c.Notes ?? "",
                c.CreatedAt.ToString("yyyy/MM/dd")
            }).ToList();
            _exportService.PrintTable("قائمة العملاء", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════
    // ATTACHMENTS
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private bool _isAttachmentsDialogOpen;

    [ObservableProperty]
    private Customer? _attachmentsCustomer;

    public ObservableCollection<CustomerAttachment> Attachments { get; } = [];

    private static string AttachmentsBaseFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments", "Customers");

    [RelayCommand]
    private async Task OpenAttachments(Customer? customer)
    {
        if (customer is null) return;
        AttachmentsCustomer = customer;
        await LoadAttachmentsAsync(customer.Id);
        IsAttachmentsDialogOpen = true;
    }

    private async Task LoadAttachmentsAsync(int customerId)
    {
        Attachments.Clear();
        var items = await _unitOfWork.CustomerAttachments.FindAsync(a => a.CustomerId == customerId);
        foreach (var a in items)
            Attachments.Add(a);
    }

    [RelayCommand]
    private async Task AddAttachmentFromFile()
    {
        if (AttachmentsCustomer is null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "صور ومستندات|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf;*.doc;*.docx;*.xls;*.xlsx|كل الملفات|*.*",
            Multiselect = true,
            Title = "اختر المرفقات"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var customerFolder = Path.Combine(AttachmentsBaseFolder, AttachmentsCustomer.Id.ToString());
            Directory.CreateDirectory(customerFolder);

            foreach (var filePath in dialog.FileNames)
            {
                var fileName = Path.GetFileName(filePath);
                var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}";
                var destPath = Path.Combine(customerFolder, uniqueName);

                File.Copy(filePath, destPath, overwrite: true);

                var attachment = new CustomerAttachment
                {
                    CustomerId = AttachmentsCustomer.Id,
                    FileName = fileName,
                    FilePath = destPath,
                    CreatedBy = _currentUserService.Username
                };
                await _unitOfWork.CustomerAttachments.AddAsync(attachment);
            }

            await _unitOfWork.SaveChangesAsync();
            await LoadAttachmentsAsync(AttachmentsCustomer.Id);
            BeautifulMessageDialog.ShowSuccess($"تم إرفاق {dialog.FileNames.Length} ملف بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء الإرفاق: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddAttachmentFromScanner()
    {
        try
        {
            if (AttachmentsCustomer is null) return;

            // Use WIA COM via late binding (dynamic) to avoid COM reference issues
            dynamic wiaDialog = Activator.CreateInstance(Type.GetTypeFromProgID("WIA.CommonDialog")!)!;
            dynamic? image = wiaDialog.ShowAcquireImage();

            if (image is null) return;

            var customerFolder = Path.Combine(AttachmentsBaseFolder, AttachmentsCustomer.Id.ToString());
            Directory.CreateDirectory(customerFolder);

            var fileName = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var destPath = Path.Combine(customerFolder, fileName);
            image.SaveFile(destPath);

            var attachment = new CustomerAttachment
            {
                CustomerId = AttachmentsCustomer.Id,
                FileName = fileName,
                FilePath = destPath,
                CreatedBy = _currentUserService.Username
            };
            await _unitOfWork.CustomerAttachments.AddAsync(attachment);
            await _unitOfWork.SaveChangesAsync();
            await LoadAttachmentsAsync(AttachmentsCustomer.Id);
            BeautifulMessageDialog.ShowSuccess("تم المسح الضوئي والإرفاق بنجاح");
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            BeautifulMessageDialog.ShowWarning("لم يتم العثور على ماسح ضوئي متصل أو تم إلغاء العملية");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء المسح الضوئي: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenAttachmentFile(CustomerAttachment? attachment)
    {
        if (attachment is null || !File.Exists(attachment.FilePath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = attachment.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"لا يمكن فتح الملف: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteAttachment(CustomerAttachment? attachment)
    {
        if (attachment is null || AttachmentsCustomer is null) return;
        try
        {
            _unitOfWork.CustomerAttachments.SoftDelete(attachment, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();

            if (File.Exists(attachment.FilePath))
                File.Delete(attachment.FilePath);

            await LoadAttachmentsAsync(AttachmentsCustomer.Id);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"خطأ أثناء حذف المرفق: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CloseAttachments() => IsAttachmentsDialogOpen = false;

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Customers, value);
}
