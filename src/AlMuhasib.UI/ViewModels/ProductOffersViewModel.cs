using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductOffersViewModel : ViewModelBase
{
    private readonly IProductOfferService _offerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFeatureFlagService _featureFlags;

    public ObservableCollection<ProductOffer> Offers { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _activeOnlyFilter;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private ProductOffer? _selectedOffer;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private Product? _editTriggerProduct;
    [ObservableProperty] private decimal _editTriggerQuantity = 1m;
    [ObservableProperty] private Product? _editGiftProduct;
    [ObservableProperty] private decimal _editGiftQuantity = 1m;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private ProductOffer? _offerToDelete;
    [ObservableProperty] private bool _featureEnabled;
    [ObservableProperty] private string _activeOffersCount = "0";

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;

    public ProductOffersViewModel(
        IProductOfferService offerService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags)
    {
        _offerService = offerService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _featureFlags = featureFlags;
        PageTitle = "إدارة العروض";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "ProductOffers");
            FeatureEnabled = _featureFlags.ProductOffers;
            Products.Clear();
            foreach (var p in await _unitOfWork.Products.GetAllAsync())
                Products.Add(p);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        var (items, totalCount) = await _offerService.GetPagedAsync(
            CurrentPage, PageSize, SearchText, ActiveOnlyFilter ? true : null);
        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);
        ActiveOffersCount = items.Count(o => o.IsActive).ToString("N0");

        Offers.Clear();
        foreach (var item in items)
            Offers.Add(item);
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
            await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnActiveOnlyFilterChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!FeatureEnabled)
        {
            BeautifulMessageDialog.ShowWarning("فعّل ميزة عروض المنتجات من إعدادات الميزات أولاً.");
            return;
        }

        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة عرض جديد";
        EditName = string.Empty;
        EditIsActive = true;
        EditTriggerProduct = null;
        EditTriggerQuantity = 1m;
        EditGiftProduct = null;
        EditGiftQuantity = 1m;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(ProductOffer item)
    {
        if (item is null) return;
        _editingId = item.Id;
        IsEditMode = true;
        DialogTitle = "تعديل العرض";
        EditName = item.Name;
        EditIsActive = item.IsActive;
        EditTriggerProduct = Products.FirstOrDefault(p => p.Id == item.TriggerProductId);
        EditTriggerQuantity = item.TriggerQuantity;
        EditGiftProduct = Products.FirstOrDefault(p => p.Id == item.GiftProductId);
        EditGiftQuantity = item.GiftQuantity;
        EditNotes = item.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveOffer()
    {
        try
        {
            DialogError = string.Empty;
            if (EditTriggerProduct is null || EditGiftProduct is null)
            {
                DialogError = "اختر المنتج المشغّل ومنتج الهدية.";
                return;
            }

            var offer = new ProductOffer
            {
                Id = _editingId ?? 0,
                Name = EditName,
                IsActive = EditIsActive,
                TriggerProductId = EditTriggerProduct.Id,
                TriggerQuantity = EditTriggerQuantity,
                GiftProductId = EditGiftProduct.Id,
                GiftQuantity = EditGiftQuantity,
                Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim()
            };

            if (_editingId is null)
                await _offerService.CreateAsync(offer);
            else
                await _offerService.UpdateAsync(offer);

            IsDialogOpen = false;
            await LoadAsync();
            BeautifulMessageDialog.ShowSuccess("تم حفظ العرض بنجاح");
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(ProductOffer item)
    {
        OfferToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (OfferToDelete is null) return;
        try
        {
            await _offerService.SoftDeleteAsync(OfferToDelete.Id);
            IsDeleteDialogOpen = false;
            OfferToDelete = null;
            await LoadAsync();
            BeautifulMessageDialog.ShowSuccess("تم حذف العرض");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        OfferToDelete = null;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "عروض_المنتجات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "اسم العرض", "المنتج المشترى", "كمية التفعيل", "منتج الهدية", "كمية الهدية", "الحالة" };
        var rows = Offers.Select(o => new object[]
        {
            o.Name,
            o.TriggerProduct?.Name ?? "",
            o.TriggerQuantity,
            o.GiftProduct?.Name ?? "",
            o.GiftQuantity,
            o.IsActive ? "نشط" : "موقوف"
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "عروض المنتجات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void PrintTable()
    {
        var cols = new[] { "اسم العرض", "المنتج المشترى", "كمية التفعيل", "منتج الهدية", "كمية الهدية", "الحالة" };
        var rows = Offers.Select(o => new object[]
        {
            o.Name,
            o.TriggerProduct?.Name ?? "",
            o.TriggerQuantity,
            o.GiftProduct?.Name ?? "",
            o.GiftQuantity,
            o.IsActive ? "نشط" : "موقوف"
        }).ToList();
        _exportService.PrintTable("عروض المنتجات", cols, rows);
    }
}
