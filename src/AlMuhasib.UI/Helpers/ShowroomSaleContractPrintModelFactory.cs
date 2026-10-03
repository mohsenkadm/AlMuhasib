using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models.Print;
using AlMuhasib.Core.Utilities;

namespace AlMuhasib.UI.Helpers;

/// <summary>يبني نموذج طباعة عقد معرض السيارات من فاتورة مبيعات محفوظة.</summary>
public static class ShowroomSaleContractPrintModelFactory
{
    public static ShowroomSaleContractPrintModel FromInvoice(
        Invoice invoice,
        IEnumerable<Product>? products = null)
    {
        var branding = PrintBrandingProvider.Current;
        var customer = invoice.Customer;
        var paidAmount = invoice.PaymentMethod == PaymentMethod.Cash
            ? invoice.NetAmount
            : Math.Clamp(invoice.PaidAmount, 0m, invoice.NetAmount);
        var remainingAmount = Math.Max(0m, invoice.NetAmount - paidAmount);

        var product = ResolveProduct(invoice, products);
        var passengers = product?.PassengerCount;
        var sizeText = passengers is > 0 ? $"{passengers} راكب" : string.Empty;

        return new ShowroomSaleContractPrintModel
        {
            ContractNumber = invoice.InvoiceNumber,
            ContractDate = invoice.Date.Kind == DateTimeKind.Utc
                ? invoice.Date.ToLocalTime()
                : invoice.Date,
            City = ExtractCityFromAddress(branding.Address),
            SellerName = branding.CompanyName,
            SellerPhone = string.IsNullOrWhiteSpace(branding.PhonePrimary)
                ? branding.PhoneSecondary
                : branding.PhonePrimary,
            SellerAddress = branding.Address,
            SellerIdNumber = branding.CompanyIdNumber,
            SellerIdIssuer = branding.CompanyIdIssuer,
            AnnualRegistrationNote = "مطابق",
            BuyerName = customer?.Name ?? string.Empty,
            BuyerPhone = customer?.Phone ?? string.Empty,
            BuyerAddress = customer?.Address ?? string.Empty,
            BuyerIdNumber = customer?.IdNumber ?? string.Empty,
            BuyerIdIssuer = customer?.IdIssuer ?? string.Empty,
            VehicleName = product?.Name ?? invoice.Items.FirstOrDefault()?.ItemName ?? string.Empty,
            PlateNumber = product?.PlateNumber ?? string.Empty,
            ChassisNumber = product?.ChassisNumber ?? string.Empty,
            VehicleType = product?.VehicleType ?? string.Empty,
            VehicleColor = product?.VehicleColor ?? string.Empty,
            VehicleModel = product?.CarModel ?? string.Empty,
            VehicleSize = sizeText,
            PlateType = product is null
                ? string.Empty
                : VehiclePlateTypeHelper.ToDisplay(product.PlateType),
            TotalAmount = invoice.NetAmount,
            TotalAmountInWords = ArabicAmountToWords.Convert(invoice.NetAmount),
            PaidAmount = paidAmount,
            RemainingAmount = remainingAmount,
            DueDate = invoice.CreditDueDate
        };
    }

    private static Product? ResolveProduct(Invoice invoice, IEnumerable<Product>? products)
    {
        var productList = products?.ToList() ?? [];
        foreach (var item in invoice.Items.Where(i => i.ProductId is > 0))
        {
            var p = productList.FirstOrDefault(x => x.Id == item.ProductId)
                    ?? item.Product;
            if (p is not null && (
                    !string.IsNullOrWhiteSpace(p.ChassisNumber)
                    || !string.IsNullOrWhiteSpace(p.PlateNumber)
                    || !string.IsNullOrWhiteSpace(p.VehicleType)))
                return p;
        }

        var firstId = invoice.Items.FirstOrDefault(i => i.ProductId is > 0)?.ProductId;
        if (firstId is int id)
            return productList.FirstOrDefault(p => p.Id == id)
                   ?? invoice.Items.FirstOrDefault(i => i.ProductId == id)?.Product;

        return null;
    }

    private static string ExtractCityFromAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;
        var parts = address.Split(['،', ',', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[^1] : address.Trim();
    }
}
