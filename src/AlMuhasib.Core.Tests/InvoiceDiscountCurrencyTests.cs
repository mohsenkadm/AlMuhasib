using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class InvoiceDiscountCurrencyTests
{
    [Theory]
    [InlineData(10, 100, 10)]
    [InlineData(12.5, 80, 10)]
    [InlineData(5.25, 40.5, 2.13)]
    public void Percentage_Discount_Applies_On_Usd_Subtotal(decimal percent, decimal subtotal, decimal expected)
    {
        var discount = ProductDiscountHelper.CalculateInvoiceDiscount(
            DiscountType.Percentage, percent, subtotal);
        discount = AccountingCurrencyHelper.NormalizeAmount(discount, AccountingCurrency.USD);
        Assert.Equal(expected, discount);
    }

    [Theory]
    [InlineData(1.5, 20, 1.5)]
    [InlineData(0.25, 10, 0.25)]
    [InlineData(50, 12.5, 12.5)]
    public void Fixed_Discount_Applies_On_Usd_Subtotal(decimal value, decimal subtotal, decimal expected)
    {
        var discount = ProductDiscountHelper.CalculateInvoiceDiscount(
            DiscountType.FixedAmount, value, subtotal);
        discount = AccountingCurrencyHelper.NormalizeAmount(discount, AccountingCurrency.USD);
        Assert.Equal(expected, discount);
    }

    [Fact]
    public void Usd_Net_Keeps_Cents_After_Discount()
    {
        var subtotal = 12.50m;
        var discount = ProductDiscountHelper.CalculateInvoiceDiscount(
            DiscountType.Percentage, 10m, subtotal);
        var net = AccountingCurrencyHelper.NormalizeAmount(subtotal - discount, AccountingCurrency.USD);
        Assert.Equal(1.25m, discount);
        Assert.Equal(11.25m, net);
    }
}
