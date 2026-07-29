using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class BarcodePriceCheckViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IProductPriceService _productPriceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly bool _pricingEnabled;

    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private string? _productName;
    [ObservableProperty] private string? _displayedBarcode;
    [ObservableProperty] private string? _pricingTypeName;
    [ObservableProperty] private decimal? _salePrice;
    [ObservableProperty] private string _statusMessage = "امسح الباركود لعرض سعر المنتج";
    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private bool _hasPrice;
    [ObservableProperty] private bool _isError;

    public BarcodePriceCheckViewModel(
        IProductService productService,
        IProductPriceService productPriceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _productService = productService;
        _productPriceService = productPriceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        PageTitle = "فحص السعر بالباركود";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.BarcodePriceCheck);
        if (!_currentUserService.IsAdmin && !_currentUserService.CanView(ScreenPermissionRegistry.BarcodePriceCheck))
        {
            StatusMessage = "ليس لديك صلاحية فتح شاشة فحص السعر";
            IsError = true;
            return;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task LookupBarcodeAsync()
    {
        var code = BarcodeInput.Trim();
        BarcodeInput = string.Empty;

        if (string.IsNullOrWhiteSpace(code))
            return;

        IsBusy = true;
        try
        {
            var product = await _productService.GetByBarcodeAsync(code);
            if (product is null)
            {
                ShowNotFound(code);
                return;
            }

            var (price, pricingTypeName) = await ResolvePriceAsync(product.Id);
            ProductName = product.Name;
            DisplayedBarcode = string.IsNullOrWhiteSpace(product.Barcode) ? code : product.Barcode;
            PricingTypeName = pricingTypeName;
            SalePrice = price;
            HasResult = true;
            HasPrice = price is > 0;
            IsError = !HasPrice;
            StatusMessage = HasPrice
                ? "تم العثور على المنتج"
                : "لا يوجد سعر لهذا المنتج";
        }
        catch (Exception ex)
        {
            ClearResult();
            IsError = true;
            StatusMessage = $"تعذّر قراءة السعر:\n{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearDisplay()
    {
        ClearResult();
        StatusMessage = "امسح الباركود لعرض سعر المنتج";
    }

    private async Task<(decimal? Price, string? PricingTypeName)> ResolvePriceAsync(int productId)
    {
        // Same resolution order as PosQuickSaleViewModel:
        // catalog default when pricing enabled, otherwise / fallback last sale price.
        if (_pricingEnabled)
        {
            var prices = await _productPriceService.GetByProductIdAsync(productId);
            var preferred = prices.FirstOrDefault(p => p.PricingType?.IsDefault == true)
                            ?? prices.FirstOrDefault();
            if (preferred is not null)
                return (preferred.SalePrice, preferred.PricingType?.Name);
        }

        var saleItems = (await _unitOfWork.InvoiceItems.FindAsync(
            i => i.ProductId == productId && i.UnitPrice > 0)).ToList();
        var lastSale = saleItems.OrderByDescending(i => i.Id).FirstOrDefault();
        if (lastSale is not null)
            return (lastSale.UnitPrice, null);

        return (null, null);
    }

    private void ShowNotFound(string code)
    {
        ProductName = null;
        DisplayedBarcode = code;
        PricingTypeName = null;
        SalePrice = null;
        HasResult = false;
        HasPrice = false;
        IsError = true;
        StatusMessage = "المنتج غير موجود";
    }

    private void ClearResult()
    {
        ProductName = null;
        DisplayedBarcode = null;
        PricingTypeName = null;
        SalePrice = null;
        HasResult = false;
        HasPrice = false;
        IsError = false;
    }
}
