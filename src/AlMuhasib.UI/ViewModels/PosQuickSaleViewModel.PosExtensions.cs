using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class PosQuickSaleViewModel
{
    public ObservableCollection<Customer> PosCustomers { get; } = [];
    public ObservableCollection<Invoice> HeldInvoices { get; } = [];

    [ObservableProperty] private bool _isInstallmentMode;
    [ObservableProperty] private Customer? _selectedPosCustomer;
    [ObservableProperty] private int _installmentCount = 3;

    partial void OnIsInstallmentModeChanged(bool value)
    {
        if (value && PosCustomers.Count == 0)
            _ = LoadPosCustomersAsync();
    }

    private async Task LoadPosCustomersAsync()
    {
        PosCustomers.Clear();
        foreach (var c in await _unitOfWork.Customers.GetAllAsync())
            PosCustomers.Add(c);
    }

    [RelayCommand]
    private async Task HoldInvoiceAsync()
    {
        if (CartLines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("السلة فارغة");
            return;
        }
        if (SelectedWarehouse is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر المخزن");
            return;
        }

        try
        {
            IsBusy = true;
            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Sale,
                CustomerId = SelectedPosCustomer?.Id ?? _userPreferences.Current.DefaultSalesCustomerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = PaymentMethod.Cash,
                Date = DateTime.Now,
                DiscountAmount = ShowProductDiscount ? InvoiceDiscountAmount : 0m,
                HoldStatus = InvoiceHoldStatus.Held,
                HeldAt = DateTime.Now,
                Notes = "فاتورة موقوفة POS"
            };
            var items = CartLines.Select(l => new InvoiceItem
            {
                ProductId = l.ProductId,
                PricingTypeId = l.PricingTypeId,
                ItemName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.IsOfferGift ? 0m : l.UnitPrice,
                DiscountAmount = l.IsOfferGift ? 0m : (ShowProductDiscount ? l.DiscountAmount : 0m),
                TotalPrice = l.IsOfferGift ? 0m : l.LineTotal,
                IsOfferGift = l.IsOfferGift,
                OfferId = l.OfferId,
                CustomFieldsJson = l.ToCustomFieldsJson()
            }).ToList();
            var saved = await _invoiceService.CreateInvoiceAsync(invoice, items, skipStockUpdate: true);
            CartLines.Clear();
            PaidAmount = 0;
            StatusMessage = $"تم إيقاف الفاتورة {saved.InvoiceNumber}";
            await LoadHeldInvoicesAsync();
            BeautifulMessageDialog.ShowSuccess("تم إيقاف الفاتورة — يمكن استئنافها لاحقاً");
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
    private async Task LoadHeldInvoicesAsync()
    {
        HeldInvoices.Clear();
        var held = await _unitOfWork.Invoices.FindAsync(i => i.HoldStatus == InvoiceHoldStatus.Held);
        foreach (var h in held.OrderByDescending(i => i.HeldAt))
            HeldInvoices.Add(h);
    }

    private async Task CompleteInstallmentSaleCoreAsync()
    {
        if (!IsInstallmentMode)
        {
            await CompleteSaleCoreAsync(printReceipt: PrintAfterSale);
            return;
        }

        if (SelectedPosCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر العميل للتقسيط");
            return;
        }

        var minAmount = _userPreferences.Current.PosMinInstallmentAmount;
        if (GrandTotal < minAmount)
        {
            BeautifulMessageDialog.ShowWarning($"الحد الأدنى للتقسيط {minAmount:N0} د.ع");
            return;
        }

        using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
        var credit = scope.ServiceProvider.GetRequiredService<ICustomerCreditService>();
        var check = await credit.CheckCreditAsync(SelectedPosCustomer.Id, GrandTotal, isInstallment: true);
        if (!check.IsAllowed)
        {
            BeautifulMessageDialog.ShowWarning(check.Message ?? "تجاوز حد الائتمان");
            return;
        }

        if (SelectedWarehouse is null || SelectedCashBox is null || CartLines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أكمل البيانات والسلة");
            return;
        }

        IsBusy = true;
        try
        {
            var cartSnapshot = CartLines.ToList();
            var totalSnapshot = GrandTotal;
            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Installment,
                CustomerId = SelectedPosCustomer.Id,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = PaymentMethod.Cash,
                CashBoxId = SelectedCashBox.Id,
                Date = DateTime.Now,
                DiscountAmount = ShowProductDiscount ? InvoiceDiscountAmount : 0m,
                Notes = "بيع تقسيط POS"
            };
            var items = CartLines.Select(l => new InvoiceItem
            {
                ProductId = l.ProductId,
                PricingTypeId = l.PricingTypeId,
                ItemName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.IsOfferGift ? 0m : l.UnitPrice,
                DiscountAmount = l.IsOfferGift ? 0m : (ShowProductDiscount ? l.DiscountAmount : 0m),
                TotalPrice = l.IsOfferGift ? 0m : l.LineTotal,
                IsOfferGift = l.IsOfferGift,
                OfferId = l.OfferId,
                CustomFieldsJson = l.ToCustomFieldsJson()
            }).ToList();
            var saved = await _invoiceService.CreateInvoiceAsync(invoice, items);
            await ApplyPosFeatureSideEffectsOnSaveAsync(cartSnapshot, items);
            var installmentService = scope.ServiceProvider.GetRequiredService<IInstallmentService>();
            await installmentService.CreatePlanAsync(saved.Id, SelectedPosCustomer.Id, null,
                saved.NetAmount, InstallmentCount, DateTime.Today.AddMonths(1));

            LastSavedInvoiceNumber = saved.InvoiceNumber;
            CartLines.Clear();
            PaidAmount = 0;
            StatusMessage = $"تقسيط — {saved.InvoiceNumber}";
            _sound.Play(SoundEffect.Success);

            if (PrintAfterSale)
            {
                try
                {
                    PrintReceiptForInvoice(saved, cartSnapshot, totalSnapshot);
                }
                catch (Exception printEx)
                {
                    BeautifulMessageDialog.ShowWarning($"تم البيع لكن فشلت الطباعة:\n{printEx.Message}");
                }
            }
            else
            {
                BeautifulMessageDialog.ShowSuccess($"تم البيع بالتقسيط\n{saved.InvoiceNumber}");
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
    private async Task PrintThermalReceiptAsync()
    {
        if (string.IsNullOrEmpty(LastSavedInvoiceNumber))
        {
            BeautifulMessageDialog.ShowWarning("لا توجد فاتورة محفوظة للطباعة");
            return;
        }

        var inv = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceNumber == LastSavedInvoiceNumber))
            .FirstOrDefault();
        if (inv is null)
        {
            BeautifulMessageDialog.ShowWarning("لم يُعثر على الفاتورة");
            return;
        }

        // Ensure items are loaded
        var items = inv.Items?.Count > 0
            ? inv.Items
            : (await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == inv.Id)).ToList();

        using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
        var export = scope.ServiceProvider.GetRequiredService<IExportService>();
        var productUsage = await LoadUsageInstructionsMapAsync(items.Select(i => i.ProductId));
        export.PrintThermalReceipt(new InvoicePrintModel
        {
            InvoiceNumber = inv.InvoiceNumber,
            Date = inv.Date,
            PartyName = inv.Customer?.Name ?? SelectedPosCustomer?.Name ?? "—",
            PartyLabel = "العميل",
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            Subtotal = items.Sum(i => i.TotalPrice),
            GrandTotal = inv.NetAmount,
            PharmacyUsageReceipt = false,
            Items = items.Select((it, idx) => new InvoicePrintItem
            {
                Number = idx + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    it.ItemName,
                    it.CustomFieldsJson),
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                TotalPrice = it.TotalPrice,
                UsageInstructions = it.ProductId is int pid ? productUsage.GetValueOrDefault(pid) : null
            }).ToList()
        });
    }

    [RelayCommand]
    private async Task PrintPharmacyReceiptAsync()
    {
        if (!ShowPharmacy)
        {
            BeautifulMessageDialog.ShowWarning("فعّل ميزة الصيدلية أولاً");
            return;
        }

        if (string.IsNullOrEmpty(LastSavedInvoiceNumber))
        {
            BeautifulMessageDialog.ShowWarning("لا توجد فاتورة محفوظة للطباعة");
            return;
        }

        var inv = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceNumber == LastSavedInvoiceNumber))
            .FirstOrDefault();
        if (inv is null)
        {
            BeautifulMessageDialog.ShowWarning("لم يُعثر على الفاتورة");
            return;
        }

        var items = inv.Items?.Count > 0
            ? inv.Items
            : (await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == inv.Id)).ToList();

        var productUsage = await LoadUsageInstructionsMapAsync(items.Select(i => i.ProductId));

        using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
        var export = scope.ServiceProvider.GetRequiredService<IExportService>();
        export.PrintThermalReceipt(new InvoicePrintModel
        {
            InvoiceNumber = inv.InvoiceNumber,
            Date = inv.Date,
            PartyName = inv.Customer?.Name ?? SelectedPosCustomer?.Name ?? "—",
            PartyLabel = "العميل",
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            Subtotal = items.Sum(i => i.TotalPrice),
            GrandTotal = inv.NetAmount,
            PharmacyUsageReceipt = true,
            Items = items.Select((it, idx) => new InvoicePrintItem
            {
                Number = idx + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    it.ItemName,
                    it.CustomFieldsJson),
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                TotalPrice = it.TotalPrice,
                UsageInstructions = it.ProductId is int pid ? productUsage.GetValueOrDefault(pid) : null
            }).ToList()
        });
    }

    private async Task<Dictionary<int, string?>> LoadUsageInstructionsMapAsync(IEnumerable<int?> productIds)
    {
        var ids = productIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var map = new Dictionary<int, string?>();
        foreach (var id in ids)
        {
            var product = _allProducts.FirstOrDefault(p => p.Id == id)
                          ?? await _unitOfWork.Products.GetByIdAsync(id);
            map[id] = product?.UsageInstructions;
        }
        return map;
    }

    private void PrintReceiptForInvoice(Invoice saved, IReadOnlyList<PosCartLine> cartSnapshot, decimal totalSnapshot, bool pharmacyUsage = false)
    {
        using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
        var export = scope.ServiceProvider.GetRequiredService<IExportService>();
        export.PrintThermalReceipt(new InvoicePrintModel
        {
            InvoiceNumber = saved.InvoiceNumber,
            Date = saved.Date,
            PartyName = SelectedPosCustomer?.Name ?? "—",
            PartyLabel = "العميل",
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            Subtotal = totalSnapshot,
            GrandTotal = saved.NetAmount > 0 ? saved.NetAmount : totalSnapshot,
            PharmacyUsageReceipt = pharmacyUsage && ShowPharmacy,
            Items = cartSnapshot.Select((l, idx) => new InvoicePrintItem
            {
                Number = idx + 1,
                ItemName = l.DisplayName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TotalPrice = l.LineTotal,
                UsageInstructions = l.UsageInstructions
            }).ToList()
        });
    }
}
