using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Import;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    private List<ProductImportBranchColumn> _importBranchColumns = [];

    /// <summary>يظهر خيار أعمدة الفروع عند وجود أكثر من فرع نشط.</summary>
    [ObservableProperty] private bool _showBranchColumnsImportToggle;

    /// <summary>تضمين أعمدة ظهور الفروع في قالب/استيراد Excel.</summary>
    [ObservableProperty] private bool _includeBranchColumnsInImport = true;

    private async Task RefreshImportBranchOptionsAsync()
    {
        _importBranchColumns = [];
        ShowBranchColumnsImportToggle = false;

        if (_services.GetService(typeof(IBranchService)) is not IBranchService branchService)
            return;

        var branches = (await branchService.GetActiveAsync())
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Name)
            .ToList();

        if (branches.Count <= 1)
        {
            IncludeBranchColumnsInImport = false;
            return;
        }

        _importBranchColumns = branches
            .Select(b => new ProductImportBranchColumn
            {
                BranchId = b.Id,
                BranchName = b.Name,
                Header = ProductImportSchema.BuildBranchHeader(b.Name)
            })
            .ToList();

        ShowBranchColumnsImportToggle = true;
        IncludeBranchColumnsInImport = true;
    }

    private ProductImportOptions BuildCurrentImportOptions()
    {
        var flags = _userPreferences.Current.FeatureFlags;
        return new ProductImportOptions
        {
            IncludePharmacyFields = flags.TemplatePharmacy,
            IncludeWeightFields = flags.MenuWeight,
            IncludeDiscountFields = flags.ProductDiscountEnabled,
            IncludePricingFields = flags.ProductPricingEnabled,
            IncludeCarShowroomFields = flags.CarShowroom,
            IncludeBranchColumns = IncludeBranchColumnsInImport && _importBranchColumns.Count > 1,
            Branches = IncludeBranchColumnsInImport ? _importBranchColumns : [],
            CustomFields = CustomFieldColumns
                .Select(c => new ProductImportCustomField
                {
                    Slot = c.Slot,
                    Header = string.IsNullOrWhiteSpace(c.Label) ? $"حقل {c.Slot}" : c.Label
                })
                .ToList()
        };
    }

    [RelayCommand]
    private void DownloadImportTemplate()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "قالب_المنتجات.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true)
                return;

            _importService.SaveProductTemplate(dialog.FileName, BuildCurrentImportOptions());
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
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة منتجات");
            return;
        }

        try
        {
            var open = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "استيراد المنتجات من Excel"
            };
            if (open.ShowDialog() != true)
                return;

            var options = BuildCurrentImportOptions();
            var preview = await _importService.PreviewProductsAsync(open.FileName, options);
            var confirm = BeautifulMessageDialog.ShowConfirm(
                $"معاينة الملف: {preview.RowCount} صف.\nهل تريد استيراد المنتجات الآن؟\n(الأسماء الموجودة مسبقاً سيتم تخطيها)");
            if (!confirm)
                return;

            var result = await _importService.ImportProductsAsync(open.FileName, options);
            var msg = $"تم استيراد {result.ImportedCount} منتج، وتخطي {result.SkippedCount}.";
            if (result.Errors.Count > 0)
                msg += $"\nأخطاء: {result.Errors.Count}\n" + string.Join("\n", result.Errors.Take(8));

            if (result.Errors.Count > 0 && result.ImportedCount == 0)
                BeautifulMessageDialog.ShowError(msg);
            else
                BeautifulMessageDialog.ShowSuccess(msg);

            await LoadCategoriesAsync();
            CurrentPage = 1;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الاستيراد: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenBulkEntryAsync()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة منتجات");
            return;
        }

        if (_services.GetService(typeof(MainWindowViewModel)) is not MainWindowViewModel main)
        {
            BeautifulMessageDialog.ShowError("تعذّر فتح واجهة الإضافة المتعددة");
            return;
        }

        await main.OpenTabAsync(
            typeof(BulkProductsEntryViewModel),
            "إضافة منتجات متعددة",
            PackIconKind.TableLarge,
            activateIfExists: true);
    }
}
