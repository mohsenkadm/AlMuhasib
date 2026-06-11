using System.Collections.ObjectModel;
using System.IO;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class MigrationWizardViewModel : ViewModelBase
{
    private readonly IDataImportService _importService;
    private readonly IOpeningInstallmentExcelService _openingInstallmentExcel;
    private readonly IInstallmentService _installmentService;

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string? _selectedFilePath;
    [ObservableProperty] private int _lastImportedCount;
    [ObservableProperty] private int _lastSkippedCount;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private int _stepTransitionToken;

    public ObservableCollection<OpeningInstallmentImportRow> InstallmentImportRows { get; } = [];
    public ObservableCollection<string> PreviewSampleRows { get; } = [];

    public int TotalSteps => 4;
    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 0 && !IsCompleted;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;
    public bool CanGoNext => !IsCompleted;
    public bool HasPreviewRows => PreviewSampleRows.Count > 0;
    public bool HasInstallmentRows => InstallmentImportRows.Count > 0;
    public int InstallmentValidCount => InstallmentImportRows.Count(r => r.IsValid);
    public int InstallmentInvalidCount => InstallmentImportRows.Count(r => !r.IsValid);
    public bool HasLastResult => LastImportedCount > 0 || LastSkippedCount > 0;

    public string StepTitle => CurrentStep switch
    {
        0 => "١ — العملاء",
        1 => "٢ — الموردون",
        2 => "٣ — المنتجات",
        3 => "٤ — أرصدة الأقساط الافتتاحية",
        _ => "اكتمل"
    };

    public string StepDescription => CurrentStep switch
    {
        0 => "حمّل قالب Excel، املأ أعمدة: الاسم، الهاتف، العنوان، الحد الائتماني — ثم استورد الملف.",
        1 => "حمّل قالب الموردين: الاسم، الهاتف، العنوان، الرصيد الافتتاحي.",
        2 => "حمّل قالب المنتجات: الاسم، الباركود، سعر البيع، سعر الشراء، الكمية الافتتاحية.",
        3 => "حمّل قالب أرصدة الأقساط: اسم العميل، رقم الملف، المبلغ، عدد الأقساط، المدفوع، تاريخ البداية.",
        _ => "تم إكمال جميع خطوات النقل. راجع البيانات في الشاشات المناسبة."
    };

    public MigrationWizardViewModel(
        IDataImportService importService,
        IOpeningInstallmentExcelService openingInstallmentExcel,
        IInstallmentService installmentService,
        ICurrentUserService currentUserService)
    {
        _importService = importService;
        _openingInstallmentExcel = openingInstallmentExcel;
        _installmentService = installmentService;
        PageTitle = "معالج النقل من نظام قديم";
        LoadPermissions(currentUserService, "DataImport");
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepDescription));
        SelectedFilePath = null;
        PreviewSampleRows.Clear();
        OnPropertyChanged(nameof(HasPreviewRows));
        LastImportedCount = 0;
        LastSkippedCount = 0;
        OnPropertyChanged(nameof(HasLastResult));
        StatusMessage = string.Empty;
        StepTransitionToken++;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Filter = "Excel|*.xlsx;*.xls", Title = "اختر ملف Excel" };
        if (dlg.ShowDialog() != true) return;

        SelectedFilePath = dlg.FileName;
        _ = LoadPreviewAsync();
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;

        try
        {
            IsBusy = true;
            PreviewSampleRows.Clear();
            InstallmentImportRows.Clear();

            if (CurrentStep == 3)
            {
                var rows = _openingInstallmentExcel.ParseImportFile(SelectedFilePath);
                foreach (var row in rows)
                    InstallmentImportRows.Add(row);
                StatusMessage = rows.Count == 0
                    ? "الملف لا يحتوي على بيانات"
                    : $"تم قراءة {rows.Count} سطر — صالح: {InstallmentValidCount} | يحتاج تصحيح: {InstallmentInvalidCount}";
                OnPropertyChanged(nameof(HasInstallmentRows));
                OnPropertyChanged(nameof(InstallmentValidCount));
                OnPropertyChanged(nameof(InstallmentInvalidCount));
            }
            else
            {
                DataImportPreview preview = CurrentStep switch
                {
                    0 => await _importService.PreviewCustomersAsync(SelectedFilePath),
                    1 => await _importService.PreviewSuppliersAsync(SelectedFilePath),
                    2 => await _importService.PreviewProductsAsync(SelectedFilePath),
                    _ => new DataImportPreview()
                };
                foreach (var row in preview.SampleRows)
                    PreviewSampleRows.Add(row);
                StatusMessage = preview.RowCount == 0
                    ? "الملف لا يحتوي على بيانات"
                    : $"معاينة: {preview.RowCount} سطر جاهز للاستيراد";
                OnPropertyChanged(nameof(HasPreviewRows));
            }
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
    private async Task DownloadTemplateAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = CurrentStep switch
            {
                0 => "قالب_العملاء.xlsx",
                1 => "قالب_الموردين.xlsx",
                2 => "قالب_المنتجات.xlsx",
                3 => "قالب_أرصدة_الاقساط.xlsx",
                _ => "قالب.xlsx"
            }
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            switch (CurrentStep)
            {
                case 0: _importService.SaveCustomerTemplate(dlg.FileName); break;
                case 1: _importService.SaveSupplierTemplate(dlg.FileName); break;
                case 2: _importService.SaveProductTemplate(dlg.FileName); break;
                case 3:
                    var bytes = _openingInstallmentExcel.GenerateTemplate();
                    await File.WriteAllBytesAsync(dlg.FileName, bytes);
                    break;
            }
            BeautifulMessageDialog.ShowSuccess("تم حفظ القالب بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportStepAsync()
    {
        if (CurrentStep == 3)
        {
            await ImportInstallmentsAsync();
            return;
        }

        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            BeautifulMessageDialog.ShowWarning("اختر ملف Excel أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var result = CurrentStep switch
            {
                0 => await _importService.ImportCustomersAsync(SelectedFilePath),
                1 => await _importService.ImportSuppliersAsync(SelectedFilePath),
                2 => await _importService.ImportProductsAsync(SelectedFilePath),
                _ => null
            };
            if (result is not null)
            {
                LastImportedCount = result.ImportedCount;
                LastSkippedCount = result.SkippedCount;
                OnPropertyChanged(nameof(HasLastResult));
                StatusMessage = $"تم استيراد {result.ImportedCount} — تخطي {result.SkippedCount}";
                if (result.Errors.Count > 0)
                    StatusMessage += $"\n{string.Join("\n", result.Errors.Take(3))}";
                BeautifulMessageDialog.ShowSuccess(StatusMessage);
            }
            SelectedFilePath = null;
            PreviewSampleRows.Clear();
            OnPropertyChanged(nameof(HasPreviewRows));
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

    private async Task ImportInstallmentsAsync()
    {
        if (InstallmentImportRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("اختر ملف Excel واعرض المعاينة أولاً");
            return;
        }

        if (InstallmentInvalidCount > 0)
        {
            BeautifulMessageDialog.ShowWarning($"يوجد {InstallmentInvalidCount} سطر يحتوي أخطاء — صحّح الملف أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var requests = InstallmentImportRows.Select(r => new OpeningInstallmentBalanceRequest
            {
                CustomerName = r.CustomerName,
                FileNumber = r.FileNumber,
                TotalAmount = r.TotalAmount,
                NumberOfInstallments = r.NumberOfInstallments,
                PaidInstallmentsCount = r.PaidInstallmentsCount,
                StartDate = r.StartDate,
                Notes = r.Notes
            }).ToList();

            var result = await _installmentService.CreateOpeningBalancePlansBatchAsync(requests);
            LastImportedCount = result.SuccessCount;
            LastSkippedCount = result.FailedCount;
            OnPropertyChanged(nameof(HasLastResult));

            if (result.SuccessCount > 0 && result.FailedCount == 0)
            {
                StatusMessage = $"تم استيراد {result.SuccessCount} رصيد افتتاحي بنجاح";
                BeautifulMessageDialog.ShowSuccess(StatusMessage);
                InstallmentImportRows.Clear();
                SelectedFilePath = null;
                OnPropertyChanged(nameof(HasInstallmentRows));
            }
            else if (result.SuccessCount > 0)
            {
                StatusMessage = $"نجح {result.SuccessCount} | فشل {result.FailedCount}";
                BeautifulMessageDialog.ShowWarning(StatusMessage);
            }
            else
            {
                StatusMessage = string.Join("\n", result.Errors.Take(5));
                BeautifulMessageDialog.ShowError(StatusMessage);
            }
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
    private void NextStep()
    {
        if (IsCompleted) return;
        if (CurrentStep < TotalSteps - 1)
            CurrentStep++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0 && !IsCompleted)
            CurrentStep--;
    }

    [RelayCommand]
    private void Finish()
    {
        IsCompleted = true;
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(StepDescription));
        BeautifulMessageDialog.ShowSuccess("اكتمل معالج النقل — راجع البيانات في الشاشات المناسبة");
    }
}
