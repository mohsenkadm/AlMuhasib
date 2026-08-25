using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldItemsViewModel
{
    [RelayCommand]
    private void DownloadImportTemplate()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "قالب_أصناف_الذهب.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true)
                return;

            File.WriteAllBytes(dialog.FileName, _excelService.GenerateTemplate());
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
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة أصناف");
            return;
        }

        try
        {
            var open = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "استيراد أصناف الذهب من Excel"
            };
            if (open.ShowDialog() != true)
                return;

            var parsed = _excelService.ParseImportFile(open.FileName);
            if (parsed.Count == 0)
            {
                BeautifulMessageDialog.ShowWarning("الملف لا يحتوي على صفوف بيانات");
                return;
            }

            var confirm = BeautifulMessageDialog.ShowConfirm(
                $"معاينة الملف: {parsed.Count} صف.\nهل تريد استيراد الأصناف الآن؟");
            if (!confirm)
                return;

            var imported = 0;
            var skipped = 0;
            var errors = new List<string>();
            var validKarats = (await _pricingService.GetKaratsAsync()).Select(k => k.KaratValue).ToHashSet();

            foreach (var row in parsed)
            {
                try
                {
                    if (!validKarats.Contains(row.KaratValue))
                    {
                        errors.Add($"صف {row.RowNumber}: العيار {row.KaratValue} غير مسجّل");
                        skipped++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(row.Barcode))
                    {
                        var existing = await _inventoryService.GetItemByBarcodeAsync(row.Barcode);
                        if (existing is not null)
                        {
                            skipped++;
                            continue;
                        }
                    }

                    await _inventoryService.CreateItemAsync(new GoldItem
                    {
                        Name = row.Name,
                        Barcode = row.Barcode,
                        KaratValue = row.KaratValue,
                        WeightGrams = row.WeightGrams,
                        SuggestedMakingCharge = row.MakingCharge,
                        CostPerGram = row.CostPerGram,
                        Category = row.Category,
                        Notes = row.Notes,
                        Status = GoldItemStatus.InStock,
                        CreatedBy = _currentUserService.Username
                    });
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"صف {row.RowNumber}: {ex.Message}");
                    skipped++;
                }
            }

            var msg = $"تم استيراد {imported} صنف، وتخطي {skipped}.";
            if (errors.Count > 0)
                msg += $"\nأخطاء: {errors.Count}\n" + string.Join("\n", errors.Take(8));

            if (errors.Count > 0 && imported == 0)
                BeautifulMessageDialog.ShowError(msg);
            else
                BeautifulMessageDialog.ShowSuccess(msg);

            CurrentPage = 1;
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الاستيراد: {ex.Message}");
        }
    }
}
