using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class PosQuickSaleViewModel
{
    private List<CashBox> _allCashBoxes = [];
    private int? _activeHeldInvoiceId;
    private bool _suppressCashBoxCurrencySync;

    public ObservableCollection<CashBox> FilteredCashBoxes { get; } = [];
    public ObservableCollection<PricingType> BulkPricingTypes { get; } = [];
    public ObservableCollection<Invoice> RecentInvoices { get; } = [];

    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private AccountingCurrency _selectedPosCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _currentFxRate;
    [ObservableProperty] private string _fxRateText = string.Empty;
    [ObservableProperty] private string _iqdEquivalentText = string.Empty;
    [ObservableProperty] private bool _showFxInfo;
    [ObservableProperty] private PricingType? _selectedBulkPricingType;
    [ObservableProperty] private int _heldInvoiceCount;

    partial void OnSelectedPosCurrencyChanged(AccountingCurrency value)
    {
        RefreshCurrencyAmountSuffix();
        RefreshFilteredCashBoxes(preferKeepSelection: true);
        RefreshFilteredProducts();
        RefreshFavoriteProducts();
        RequoteCartForCashBoxCurrency();
        _ = RefreshFxAndEquivalentAsync();
        _ = RequotePricingOptionsForCurrencyAsync();
    }

    partial void OnSelectedBulkPricingTypeChanged(PricingType? value)
    {
        if (value is null) return;
        ApplyBulkPricingType(value.Id);
    }

    private void RefreshMultiCurrencyVisibility()
    {
        ShowMultiCurrency = _featureFlags.MultiCurrency;
        if (!ShowMultiCurrency)
            SelectedPosCurrency = AccountingCurrency.IQD;
    }

    private void SeedCashBoxes(IEnumerable<CashBox> boxes)
    {
        _allCashBoxes = boxes.ToList();
        CashBoxes.Clear();
        foreach (var c in _allCashBoxes)
            CashBoxes.Add(c);
        RefreshFilteredCashBoxes(preferKeepSelection: true);
    }

    private void RefreshFilteredCashBoxes(bool preferKeepSelection)
    {
        var previousId = SelectedCashBox?.Id;
        FilteredCashBoxes.Clear();

        IEnumerable<CashBox> query = _allCashBoxes;
        if (ShowMultiCurrency)
            query = query.Where(c => c.Currency == SelectedPosCurrency);

        foreach (var c in query)
            FilteredCashBoxes.Add(c);

        _suppressCashBoxCurrencySync = true;
        try
        {
            if (preferKeepSelection && previousId is int id)
            {
                var keep = FilteredCashBoxes.FirstOrDefault(c => c.Id == id);
                if (keep is not null)
                {
                    SelectedCashBox = keep;
                    return;
                }
            }

            SelectedCashBox = FilteredCashBoxes.FirstOrDefault();
        }
        finally
        {
            _suppressCashBoxCurrencySync = false;
        }
    }

    [RelayCommand]
    private void SelectWarehouse(Warehouse? warehouse)
    {
        if (warehouse is null) return;
        SelectedWarehouse = warehouse;
    }

    [RelayCommand]
    private void SelectCashBox(CashBox? cashBox)
    {
        if (cashBox is null) return;
        SelectedCashBox = cashBox;
    }

    [RelayCommand]
    private void SelectPosCurrency(string? currencyName)
    {
        if (!ShowMultiCurrency) return;
        if (!Enum.TryParse<AccountingCurrency>(currencyName, ignoreCase: true, out var currency))
            return;
        SelectedPosCurrency = currency;
    }

    [RelayCommand]
    private void SelectInvoiceDiscountType(DiscountTypeOption? option)
    {
        if (option is null) return;
        SelectedInvoiceDiscountOption = option;
    }

    [RelayCommand]
    private void SelectBulkPricingType(PricingType? type)
    {
        SelectedBulkPricingType = type;
    }

    [RelayCommand]
    private void EditLineQuantity(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift) return;
        var value = TouchNumericPadDialog.Prompt(
            "الكمية",
            line.DisplayName,
            line.Quantity,
            allowDecimal: true,
            allowZero: false,
            minValue: 0.01m);
        if (value is null) return;
        line.Quantity = value.Value;
        _ = RefreshOfferGiftsAsync();
    }

    [RelayCommand]
    private void EditLinePrice(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift) return;
        var value = TouchNumericPadDialog.Prompt(
            $"السعر ({CurrencyAmountSuffix})",
            line.DisplayName,
            line.UnitPrice,
            allowDecimal: SelectedPosCurrency == AccountingCurrency.USD,
            allowZero: false,
            minValue: 0.01m);
        if (value is null) return;
        line.UnitPrice = AccountingCurrencyHelper.NormalizeAmount(value.Value, PosDocumentCurrency);
    }

    [RelayCommand]
    private void EditLineDiscount(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift || !ShowProductDiscount) return;
        var value = TouchNumericPadDialog.Prompt(
            $"خصم السطر ({CurrencyAmountSuffix})",
            line.DisplayName,
            line.DiscountAmount,
            allowDecimal: SelectedPosCurrency == AccountingCurrency.USD,
            allowZero: true);
        if (value is null) return;
        line.DiscountAmount = AccountingCurrencyHelper.NormalizeAmount(Math.Max(0m, value.Value), PosDocumentCurrency);
    }

    [RelayCommand]
    private void EditInvoiceDiscountValue()
    {
        if (!ShowProductDiscount || InvoiceDiscountType == DiscountType.None)
            return;

        var allowDecimal = InvoiceDiscountType == DiscountType.Percentage
                           || SelectedPosCurrency == AccountingCurrency.USD;
        var value = TouchNumericPadDialog.Prompt(
            InvoiceDiscountValueHint,
            null,
            InvoiceDiscountValue,
            allowDecimal: allowDecimal,
            allowZero: true);
        if (value is null) return;
        InvoiceDiscountValue = InvoiceDiscountType == DiscountType.Percentage
            ? value.Value
            : AccountingCurrencyHelper.NormalizeAmount(value.Value, PosDocumentCurrency);
    }

    [RelayCommand]
    private void EditPaidAmount()
    {
        var value = TouchNumericPadDialog.Prompt(
            PaidAmountHint,
            $"الإجمالي: {AccountingCurrencyHelper.Format(GrandTotal, PosDocumentCurrency)}",
            PaidAmount,
            allowDecimal: SelectedPosCurrency == AccountingCurrency.USD,
            allowZero: true);
        if (value is null) return;
        PaidAmount = AccountingCurrencyHelper.NormalizeAmount(Math.Max(0m, value.Value), PosDocumentCurrency);
    }

    [RelayCommand]
    private async Task ShowHeldInvoicesAsync()
    {
        await LoadHeldInvoicesAsync();
        var items = HeldInvoices.Select(h => new PosInvoiceListItem
        {
            InvoiceId = h.Id,
            Number = h.InvoiceNumber,
            Meta = h.HeldAt?.ToString("yyyy/MM/dd HH:mm") ?? h.Date.ToString("yyyy/MM/dd HH:mm"),
            Amount = h.NetAmount,
            Currency = h.Currency,
            AmountText = AccountingCurrencyHelper.Format(h.NetAmount, h.Currency),
            ShowPrimaryAction = true,
            ShowPrint = false,
            PrimaryActionLabel = "استئناف"
        }).ToList();

        var selected = PosInvoiceListDialog.ShowList(
            "الفواتير المعلقة",
            "اضغط استئناف لإكمال الفاتورة",
            items,
            out _);
        if (selected is null) return;
        await ResumeHeldInvoiceAsync(selected.InvoiceId);
    }

    [RelayCommand]
    private async Task ResumeHeldInvoiceAsync(int invoiceId)
    {
        if (invoiceId <= 0) return;

        if (CartLines.Count > 0 &&
            !BeautifulMessageDialog.ShowConfirm("السلة الحالية غير فارغة. استبدالها بالفاتورة المعلقة؟"))
            return;

        try
        {
            IsBusy = true;
            var inv = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
            if (inv is null || inv.HoldStatus != InvoiceHoldStatus.Held)
            {
                BeautifulMessageDialog.ShowWarning("الفاتورة المعلقة غير موجودة");
                await LoadHeldInvoicesAsync();
                return;
            }

            CartLines.Clear();
            PaidAmount = 0;
            LoyaltyRedeemPoints = 0;
            LoyaltyDiscountAmount = 0m;

            if (ShowMultiCurrency)
                SelectedPosCurrency = inv.Currency;

            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == inv.WarehouseId) ?? SelectedWarehouse;
            if (inv.CashBoxId is int cashId)
                SelectedCashBox = FilteredCashBoxes.FirstOrDefault(c => c.Id == cashId)
                                  ?? CashBoxes.FirstOrDefault(c => c.Id == cashId)
                                  ?? SelectedCashBox;

            if (inv.CustomerId is int custId)
                SelectedPosCustomer = PosCustomers.FirstOrDefault(c => c.Id == custId);

            InvoiceDiscountType = DiscountType.None;
            InvoiceDiscountValue = 0m;
            SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
            if (ShowProductDiscount && inv.DiscountAmount > 0)
            {
                InvoiceDiscountType = DiscountType.FixedAmount;
                InvoiceDiscountValue = inv.DiscountAmount;
                SelectedInvoiceDiscountOption =
                    InvoiceDiscountTypeOptions.FirstOrDefault(o => o.Type == DiscountType.FixedAmount)
                    ?? InvoiceDiscountTypeOptions[0];
            }

            var items = inv.Items?.Count > 0
                ? inv.Items.ToList()
                : (await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == inv.Id)).ToList();

            foreach (var item in items.Where(i => i.ProductId is > 0))
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == item.ProductId)
                              ?? await _unitOfWork.Products.GetByIdAsync(item.ProductId!.Value);
                if (product is null) continue;

                var line = PosCartLine.FromProduct(
                    product,
                    item.UnitPrice,
                    item.Quantity,
                    item.PricingTypeId,
                    ShowProductDiscount);
                line.IsOfferGift = item.IsOfferGift;
                line.OfferId = item.OfferId;
                CartLines.Add(line);
                await EnrichLineFeatureDataAsync(line);
                // Keep held invoice amounts (Enrich may requote from catalog).
                line.UnitPrice = item.UnitPrice;
                if (ShowProductDiscount)
                    line.DiscountAmount = item.DiscountAmount;
            }

            _activeHeldInvoiceId = inv.Id;
            RecalcCartTotals();
            await RefreshFxAndEquivalentAsync();
            StatusMessage = $"تم استئناف الفاتورة المعلقة {inv.InvoiceNumber}";
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر استئناف الفاتورة:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowRecentInvoicesAsync()
    {
        await LoadRecentInvoicesAsync();
        var items = RecentInvoices.Select(h => new PosInvoiceListItem
        {
            InvoiceId = h.Id,
            Number = h.InvoiceNumber,
            Meta = h.Date.ToString("yyyy/MM/dd HH:mm"),
            Amount = h.NetAmount,
            Currency = h.Currency,
            AmountText = AccountingCurrencyHelper.Format(h.NetAmount, h.Currency),
            ShowPrimaryAction = false,
            ShowPrint = true
        }).ToList();

        var selected = PosInvoiceListDialog.ShowList(
            "آخر 10 فواتير",
            "عرض وطباعة الفواتير الأخيرة",
            items,
            out var printRequested);

        if (selected is null || !printRequested)
            return;

        await PrintInvoiceByIdAsync(selected.InvoiceId);
    }

    private async Task LoadRecentInvoicesAsync()
    {
        RecentInvoices.Clear();
        var recent = await _unitOfWork.Invoices.FindAsync(i =>
            i.InvoiceType == InvoiceType.Sale &&
            i.HoldStatus != InvoiceHoldStatus.Held);
        foreach (var inv in recent.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).Take(10))
            RecentInvoices.Add(inv);
    }

    private async Task PrintInvoiceByIdAsync(int invoiceId)
    {
        var inv = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        if (inv is null)
        {
            BeautifulMessageDialog.ShowWarning("لم يُعثر على الفاتورة");
            return;
        }

        var items = inv.Items?.Count > 0
            ? inv.Items.ToList()
            : (await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == inv.Id)).ToList();

        using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
        var export = scope.ServiceProvider.GetRequiredService<IExportService>();
        var productUsage = await LoadUsageInstructionsMapAsync(items.Select(i => i.ProductId));
        export.PrintThermalReceipt(new InvoicePrintModel
        {
            InvoiceNumber = inv.InvoiceNumber,
            Date = inv.Date,
            PartyName = inv.Customer is not null
                ? CustomerDisplayHelper.FormatDisplayName(inv.Customer.Name, inv.Customer.FileNumber)
                : "—",
            PartyLabel = "العميل",
            WarehouseName = inv.Warehouse?.Name ?? SelectedWarehouse?.Name ?? string.Empty,
            Subtotal = items.Sum(i => i.TotalPrice),
            GrandTotal = inv.NetAmount,
            PharmacyUsageReceipt = false,
            CurrencyLabel = AccountingCurrencyHelper.GetLabel(inv.Currency),
            Items = items.Select((it, idx) => new InvoicePrintItem
            {
                Number = idx + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(it.ItemName, it.CustomFieldsJson),
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                TotalPrice = it.TotalPrice,
                UsageInstructions = it.ProductId is int pid ? productUsage.GetValueOrDefault(pid) : null
            }).ToList()
        });
    }

    private async Task RefreshFxAndEquivalentAsync()
    {
        if (!ShowMultiCurrency || SelectedPosCurrency != AccountingCurrency.USD)
        {
            ShowFxInfo = false;
            CurrentFxRate = 0;
            FxRateText = string.Empty;
            IqdEquivalentText = string.Empty;
            return;
        }

        CurrentFxRate = await ResolveFxRateAsync(AccountingCurrency.USD);
        ShowFxInfo = true;
        FxRateText = CurrentFxRate > 0
            ? $"سعر الصرف: {CurrentFxRate:N0} د.ع / $"
            : "سعر الصرف: غير متوفر";
        IqdEquivalentText = CurrentFxRate > 0
            ? $"المعادل: {AccountingCurrencyHelper.Format(GrandTotal * CurrentFxRate, AccountingCurrency.IQD)}"
            : "المعادل: —";
    }

    private void RefreshIqdEquivalentOnly()
    {
        if (!ShowFxInfo || CurrentFxRate <= 0)
            return;
        IqdEquivalentText =
            $"المعادل: {AccountingCurrencyHelper.Format(GrandTotal * CurrentFxRate, AccountingCurrency.IQD)}";
    }

    private async Task LoadBulkPricingTypesAsync()
    {
        BulkPricingTypes.Clear();
        if (!ShowProductPricing) return;
        foreach (var t in (await _unitOfWork.PricingTypes.GetAllAsync())
                 .OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name))
            BulkPricingTypes.Add(t);
    }

    private void ApplyBulkPricingType(int pricingTypeId)
    {
        foreach (var line in CartLines.Where(l => !l.IsOfferGift))
        {
            var match = line.AvailablePricingOptions.FirstOrDefault(o => o.PricingTypeId == pricingTypeId);
            if (match is not null)
                line.SelectedPricingOption = match;
        }
    }

    private async Task RequotePricingOptionsForCurrencyAsync()
    {
        if (!ShowProductPricing) return;
        foreach (var line in CartLines.Where(l => !l.IsOfferGift && l.ProductId > 0))
            await EnrichLineFeatureDataAsync(line);
    }

    private async Task DiscardActiveHeldInvoiceAsync()
    {
        if (_activeHeldInvoiceId is not int heldId)
            return;

        try
        {
            var held = await _unitOfWork.Invoices.GetByIdAsync(heldId);
            if (held is not null && held.HoldStatus == InvoiceHoldStatus.Held)
            {
                // Soft-delete without stock reversal (hold never applied stock).
                held.HoldStatus = InvoiceHoldStatus.Completed;
                held.MarkSoftDeleted(_currentUserService.Username ?? "pos");
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch
        {
            // Non-fatal: sale already completed.
        }
        finally
        {
            _activeHeldInvoiceId = null;
            await LoadHeldInvoicesAsync();
        }
    }

    private void ClearActiveHeldInvoice()
    {
        _activeHeldInvoiceId = null;
    }
}
