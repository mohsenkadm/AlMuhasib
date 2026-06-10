using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillEmptySyncIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "Categories", "Products", "Customers", "Suppliers", "Warehouses", "WarehouseStocks",
                "CashBoxes", "BankAccounts", "Investors", "ExpenseTypes", "Invoices", "InvoiceItems",
                "InstallmentPlans", "Installments", "Vouchers", "Expenses", "Transfers",
                "InvestorTransactions", "ProfitDistributions", "ProfitDistributionDetails",
                "CapitalEntries", "CustomerAttachments", "PrintBrandingSettings", "AuditLogs",
                "Permissions", "Users", "UserTasks", "UserNotes"
            };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($"""
                    UPDATE [{table}]
                    SET [SyncId] = NEWID()
                    WHERE [SyncId] = '00000000-0000-0000-0000-000000000000';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
