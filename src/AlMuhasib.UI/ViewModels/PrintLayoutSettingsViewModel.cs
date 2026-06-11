using System.Collections.ObjectModel;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class PrintLayoutSettingsViewModel : ViewModelBase
{
    private readonly IPrintBrandingService _brandingService;
    private readonly ICurrentUserService _currentUserService;
    private int _settingsId;

    public ObservableCollection<string> AvailablePrinters { get; } = [];

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phonePrimary = string.Empty;
    [ObservableProperty] private string _phoneSecondary = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _details = string.Empty;
    [ObservableProperty] private string _footerText = string.Empty;

    [ObservableProperty] private bool _showHeaderText = true;
    [ObservableProperty] private bool _showHeaderImage;
    [ObservableProperty] private bool _showFooterText = true;
    [ObservableProperty] private bool _showFooterImage;

    [ObservableProperty] private byte[]? _headerImageData;
    [ObservableProperty] private byte[]? _footerImageData;
    [ObservableProperty] private ImageSource? _headerImagePreview;
    [ObservableProperty] private ImageSource? _footerImagePreview;

    [ObservableProperty] private string? _selectedPrinter;
    [ObservableProperty] private string _paperSize = "A4";
    [ObservableProperty] private bool _showPrintPreview = true;

    [ObservableProperty] private FlowDocument? _previewDocument;

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private bool _isSaved;

    public PrintLayoutSettingsViewModel(IPrintBrandingService brandingService, ICurrentUserService currentUserService)
    {
        _brandingService = brandingService;
        _currentUserService = currentUserService;
        PageTitle = "إعدادات الطباعة";
        LoadPermissions(currentUserService, "PrintSettings");
    }

    public override async Task InitializeAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var settings = await _brandingService.GetOrCreateSettingsAsync();
            _settingsId = settings.Id;
            CompanyName = settings.CompanyName;
            Address = settings.Address;
            PhonePrimary = settings.PhonePrimary;
            PhoneSecondary = settings.PhoneSecondary;
            Email = settings.Email;
            Details = settings.Details;
            FooterText = settings.FooterText;
            ShowHeaderText = settings.ShowHeaderText;
            ShowHeaderImage = settings.ShowHeaderImage;
            ShowFooterText = settings.ShowFooterText;
            ShowFooterImage = settings.ShowFooterImage;
            HeaderImageData = settings.HeaderImageData;
            FooterImageData = settings.FooterImageData;
            HeaderImagePreview = CreateImageSource(HeaderImageData);
            FooterImagePreview = CreateImageSource(FooterImageData);

            LoadPrinterPreferences();
            RefreshPreview();
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر التحميل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل إعدادات الطباعة.");
            return;
        }

        try
        {
            IsBusy = true;
            var settings = new PrintBrandingSettings
            {
                Id = _settingsId,
                CompanyName = CompanyName.Trim(),
                Address = Address.Trim(),
                PhonePrimary = PhonePrimary.Trim(),
                PhoneSecondary = PhoneSecondary.Trim(),
                Email = Email.Trim(),
                Details = Details.Trim(),
                FooterText = FooterText.Trim(),
                ShowHeaderText = ShowHeaderText,
                ShowHeaderImage = ShowHeaderImage && HeaderImageData is { Length: > 0 },
                ShowFooterText = ShowFooterText,
                ShowFooterImage = ShowFooterImage && FooterImageData is { Length: > 0 },
                HeaderImageData = ShowHeaderImage ? HeaderImageData : null,
                FooterImageData = ShowFooterImage ? FooterImageData : null,
                UpdatedBy = _currentUserService.Username
            };

            await _brandingService.SaveAsync(settings);
            _settingsId = settings.Id;
            SavePrinterPreferences();
            IsSaved = true;
            StatusMessage = "تم حفظ الإعدادات — ستُطبَّق على جميع الطباعات.";
            BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات الطباعة بنجاح.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"فشل الحفظ: {ex.Message}";
            BeautifulMessageDialog.ShowError(StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void PickHeaderImage()
    {
        var data = PickImageBytes();
        if (data is null) return;
        HeaderImageData = data;
        HeaderImagePreview = CreateImageSource(data);
        ShowHeaderImage = true;
        RefreshPreview();
    }

    [RelayCommand]
    private void PickFooterImage()
    {
        var data = PickImageBytes();
        if (data is null) return;
        FooterImageData = data;
        FooterImagePreview = CreateImageSource(data);
        ShowFooterImage = true;
        RefreshPreview();
    }

    [RelayCommand]
    private void ClearHeaderImage()
    {
        HeaderImageData = null;
        HeaderImagePreview = null;
        ShowHeaderImage = false;
        RefreshPreview();
    }

    [RelayCommand]
    private void ClearFooterImage()
    {
        FooterImageData = null;
        FooterImagePreview = null;
        ShowFooterImage = false;
        RefreshPreview();
    }

    [RelayCommand]
    private void RefreshPreview() => PreviewDocument = PrintBrandingFlowDocumentHelper.BuildPreviewDocument(BuildSnapshot());

    partial void OnCompanyNameChanged(string value) => RefreshPreview();
    partial void OnAddressChanged(string value) => RefreshPreview();
    partial void OnPhonePrimaryChanged(string value) => RefreshPreview();
    partial void OnPhoneSecondaryChanged(string value) => RefreshPreview();
    partial void OnEmailChanged(string value) => RefreshPreview();
    partial void OnDetailsChanged(string value) => RefreshPreview();
    partial void OnFooterTextChanged(string value) => RefreshPreview();
    partial void OnShowHeaderTextChanged(bool value) => RefreshPreview();
    partial void OnShowHeaderImageChanged(bool value) => RefreshPreview();
    partial void OnShowFooterTextChanged(bool value) => RefreshPreview();
    partial void OnShowFooterImageChanged(bool value) => RefreshPreview();

    private PrintBrandingSnapshot BuildSnapshot() => new()
    {
        CompanyName = CompanyName,
        Address = Address,
        PhonePrimary = PhonePrimary,
        PhoneSecondary = PhoneSecondary,
        Email = Email,
        Details = Details,
        FooterText = FooterText,
        ShowHeaderText = ShowHeaderText,
        ShowHeaderImage = ShowHeaderImage && HeaderImageData is { Length: > 0 },
        HeaderImageData = HeaderImageData,
        ShowFooterText = ShowFooterText,
        ShowFooterImage = ShowFooterImage && FooterImageData is { Length: > 0 },
        FooterImageData = FooterImageData
    };

    private void LoadPrinterPreferences()
    {
        AvailablePrinters.Clear();
        foreach (var queue in new LocalPrintServer().GetPrintQueues())
            AvailablePrinters.Add(queue.FullName);

        PrintPreferences.Load();
        SelectedPrinter = PrintPreferences.PreferredPrinter;
        PaperSize = PrintPreferences.PaperSize;
        ShowPrintPreview = PrintPreferences.ShowPrintPreview;
    }

    private void SavePrinterPreferences()
    {
        PrintPreferences.PreferredPrinter = SelectedPrinter;
        PrintPreferences.PaperSize = PaperSize;
        PrintPreferences.ShowPrintPreview = ShowPrintPreview;
        PrintPreferences.Save();
    }

    private static byte[]? PickImageBytes()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "صور|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "اختر صورة"
        };
        if (dlg.ShowDialog() != true)
            return null;

        try
        {
            return File.ReadAllBytes(dlg.FileName);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر قراءة الصورة: {ex.Message}");
            return null;
        }
    }

    private static ImageSource? CreateImageSource(byte[]? data)
    {
        if (data is null or { Length: 0 })
            return null;

        try
        {
            var bmp = new BitmapImage();
            using var ms = new MemoryStream(data);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
