using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSyncSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WarehouseStocks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "WarehouseStocks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Warehouses",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Vouchers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Vouchers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserTasks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "UserTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserNotes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "UserNotes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Transfers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Suppliers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Suppliers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProfitDistributions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "ProfitDistributions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProfitDistributionDetails",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "ProfitDistributionDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PrintBrandingSettings",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "PrintBrandingSettings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Permissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Permissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "InvoiceItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvestorTransactions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "InvestorTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Investors",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Investors",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Installments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Installments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InstallmentPlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "InstallmentPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExpenseTypes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "ExpenseTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Expenses",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Customers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomerAttachments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "CustomerAttachments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Categories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CashBoxes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "CashBoxes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CapitalEntries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "CapitalEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BankAccounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuditLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CloudSyncSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoSyncEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoSyncIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudSyncSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastPulledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPushedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerCursor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.EntityType);
                });

            migrationBuilder.InsertData(
                table: "CloudSyncSettings",
                columns: new[] { "Id", "AccessToken", "AccessTokenExpiresAt", "ApiBaseUrl", "AutoSyncEnabled", "AutoSyncIntervalMinutes", "LastSuccessfulSyncAt", "LastSyncError", "Password", "RefreshToken", "Username" },
                values: new object[] { 1, null, null, "", false, 15, null, null, "", null, "" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_SyncId",
                table: "WarehouseStocks",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_SyncId",
                table: "Warehouses",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SyncId",
                table: "Vouchers",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_SyncId",
                table: "UserTasks",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SyncId",
                table: "Users",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_SyncId",
                table: "UserNotes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_SyncId",
                table: "Transfers",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SyncId",
                table: "Suppliers",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitDistributions_SyncId",
                table: "ProfitDistributions",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitDistributionDetails_SyncId",
                table: "ProfitDistributionDetails",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SyncId",
                table: "Products",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintBrandingSettings_SyncId",
                table: "PrintBrandingSettings",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_SyncId",
                table: "Permissions",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SyncId",
                table: "Invoices",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_SyncId",
                table: "InvoiceItems",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorTransactions_SyncId",
                table: "InvestorTransactions",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Investors_SyncId",
                table: "Investors",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_SyncId",
                table: "Installments",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_SyncId",
                table: "InstallmentPlans",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_SyncId",
                table: "ExpenseTypes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SyncId",
                table: "Expenses",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SyncId",
                table: "Customers",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAttachments_SyncId",
                table: "CustomerAttachments",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SyncId",
                table: "Categories",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_SyncId",
                table: "CashBoxes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_CapitalEntries_SyncId",
                table: "CapitalEntries",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_SyncId",
                table: "BankAccounts",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SyncId",
                table: "AuditLogs",
                column: "SyncId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudSyncSettings");

            migrationBuilder.DropTable(
                name: "SyncStates");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_SyncId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_SyncId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_SyncId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_SyncId",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_Users_SyncId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserNotes_SyncId",
                table: "UserNotes");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_SyncId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SyncId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitDistributions_SyncId",
                table: "ProfitDistributions");

            migrationBuilder.DropIndex(
                name: "IX_ProfitDistributionDetails_SyncId",
                table: "ProfitDistributionDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_SyncId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PrintBrandingSettings_SyncId",
                table: "PrintBrandingSettings");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_SyncId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SyncId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_SyncId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvestorTransactions_SyncId",
                table: "InvestorTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Investors_SyncId",
                table: "Investors");

            migrationBuilder.DropIndex(
                name: "IX_Installments_SyncId",
                table: "Installments");

            migrationBuilder.DropIndex(
                name: "IX_InstallmentPlans_SyncId",
                table: "InstallmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseTypes_SyncId",
                table: "ExpenseTypes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SyncId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SyncId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAttachments_SyncId",
                table: "CustomerAttachments");

            migrationBuilder.DropIndex(
                name: "IX_Categories_SyncId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxes_SyncId",
                table: "CashBoxes");

            migrationBuilder.DropIndex(
                name: "IX_CapitalEntries_SyncId",
                table: "CapitalEntries");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_SyncId",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_SyncId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserNotes");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "UserNotes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProfitDistributions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "ProfitDistributions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProfitDistributionDetails");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "ProfitDistributionDetails");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PrintBrandingSettings");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "PrintBrandingSettings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvestorTransactions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "InvestorTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Installments");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Installments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InstallmentPlans");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "InstallmentPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "ExpenseTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomerAttachments");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "CustomerAttachments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CashBoxes");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "CashBoxes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CapitalEntries");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "CapitalEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "AuditLogs");
        }
    }
}
