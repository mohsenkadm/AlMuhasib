using AlMuhasib.Core.Enums;
using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

internal static class CarTradeLegacyDataMigrator
{
    public static async Task MigrateAsync(CarTradeDbContext context, CancellationToken cancellationToken = default)
    {
        var sells = await context.CarTradeTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TradeType == CarTradeType.Sell && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        if (sells.Count == 0)
            return;

        var buys = await context.CarTradeTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TradeType == CarTradeType.Buy && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var sell in sells)
        {
            var match = buys.FirstOrDefault(b =>
                !b.IsSold &&
                ((!string.IsNullOrWhiteSpace(sell.ChassisNumber) &&
                  string.Equals(b.ChassisNumber.Trim(), sell.ChassisNumber.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(sell.PlateNumber) &&
                  string.Equals(b.PlateNumber.Trim(), sell.PlateNumber.Trim(), StringComparison.OrdinalIgnoreCase))));

            if (match is not null)
            {
                match.IsSold = true;
                match.SaleDate = sell.TransactionDate;
                match.BuyerName = sell.BuyerName;
                match.BuyerPhone = sell.BuyerPhone;
                match.SalePrice = sell.SalePrice;
                match.SalePaymentMode = sell.PaymentMode;
                match.SaleAmountPaid = sell.AmountPaid;
                match.SaleRemainingAmount = sell.RemainingAmount;
                sell.IsDeleted = true;
                sell.DeletedAt = DateTime.UtcNow;
                sell.DeletedBy = "Migration";
                continue;
            }

            var salePaymentMode = sell.PaymentMode;
            var saleAmountPaid = sell.AmountPaid;
            var saleRemaining = sell.RemainingAmount;
            sell.TradeType = CarTradeType.Buy;
            sell.IsSold = true;
            sell.SaleDate = sell.TransactionDate;
            sell.PurchasePrice = sell.SalePrice;
            sell.TotalAmount = sell.SalePrice;
            sell.PaymentMode = CarTradePaymentMode.FullCash;
            sell.AmountPaid = sell.SalePrice;
            sell.RemainingAmount = 0;
            sell.SalePaymentMode = salePaymentMode;
            sell.SaleAmountPaid = saleAmountPaid;
            sell.SaleRemainingAmount = saleRemaining;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
