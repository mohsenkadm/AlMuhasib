using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncCloudPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.CarTradeTransactions', 'IsSold') IS NULL
                    ALTER TABLE [CarTradeTransactions] ADD [IsSold] bit NOT NULL CONSTRAINT [DF_CarTradeTransactions_IsSold] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleAmountPaid') IS NULL
                    ALTER TABLE [CarTradeTransactions] ADD [SaleAmountPaid] decimal(18,2) NOT NULL CONSTRAINT [DF_CarTradeTransactions_SaleAmountPaid] DEFAULT 0.0;

                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleDate') IS NULL
                    ALTER TABLE [CarTradeTransactions] ADD [SaleDate] datetime2 NULL;

                IF COL_LENGTH('dbo.CarTradeTransactions', 'SalePaymentMode') IS NULL
                    ALTER TABLE [CarTradeTransactions] ADD [SalePaymentMode] int NOT NULL CONSTRAINT [DF_CarTradeTransactions_SalePaymentMode] DEFAULT 0;

                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleRemainingAmount') IS NULL
                    ALTER TABLE [CarTradeTransactions] ADD [SaleRemainingAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_CarTradeTransactions_SaleRemainingAmount] DEFAULT 0.0;

                IF COL_LENGTH('dbo.CarTradePayments', 'PaymentKind') IS NULL
                    ALTER TABLE [CarTradePayments] ADD [PaymentKind] int NOT NULL CONSTRAINT [DF_CarTradePayments_PaymentKind] DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.CarTradeTransactions', 'IsSold') IS NOT NULL
                    ALTER TABLE [CarTradeTransactions] DROP COLUMN [IsSold];
                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleAmountPaid') IS NOT NULL
                    ALTER TABLE [CarTradeTransactions] DROP COLUMN [SaleAmountPaid];
                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleDate') IS NOT NULL
                    ALTER TABLE [CarTradeTransactions] DROP COLUMN [SaleDate];
                IF COL_LENGTH('dbo.CarTradeTransactions', 'SalePaymentMode') IS NOT NULL
                    ALTER TABLE [CarTradeTransactions] DROP COLUMN [SalePaymentMode];
                IF COL_LENGTH('dbo.CarTradeTransactions', 'SaleRemainingAmount') IS NOT NULL
                    ALTER TABLE [CarTradeTransactions] DROP COLUMN [SaleRemainingAmount];
                IF COL_LENGTH('dbo.CarTradePayments', 'PaymentKind') IS NOT NULL
                    ALTER TABLE [CarTradePayments] DROP COLUMN [PaymentKind];
                """);
        }
    }
}
