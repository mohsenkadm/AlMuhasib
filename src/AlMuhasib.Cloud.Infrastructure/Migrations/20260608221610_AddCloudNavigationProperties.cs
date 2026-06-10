using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloudInvestorId",
                table: "InvestorTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_ProductId",
                table: "WarehouseStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId",
                table: "WarehouseStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_BankAccountId",
                table: "Vouchers",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CashBoxId",
                table: "Vouchers",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CustomerId",
                table: "Vouchers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_InvestorId",
                table: "Vouchers",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitDistributionDetails_InvestorId",
                table: "ProfitDistributionDetails",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CashBoxId",
                table: "Invoices",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplierId",
                table: "Invoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_WarehouseId",
                table: "Invoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProductId",
                table: "InvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorTransactions_CloudInvestorId",
                table: "InvestorTransactions",
                column: "CloudInvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_CashBoxId",
                table: "Installments",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_InstallmentPlanId",
                table: "Installments",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_CustomerId",
                table: "InstallmentPlans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_InvoiceId",
                table: "InstallmentPlans",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CashBoxId",
                table: "Expenses",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseTypeId",
                table: "Expenses",
                column: "ExpenseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_CashBoxes_CashBoxId",
                table: "Expenses",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_ExpenseTypes_ExpenseTypeId",
                table: "Expenses",
                column: "ExpenseTypeId",
                principalTable: "ExpenseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstallmentPlans_Customers_CustomerId",
                table: "InstallmentPlans",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstallmentPlans_Invoices_InvoiceId",
                table: "InstallmentPlans",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Installments_CashBoxes_CashBoxId",
                table: "Installments",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Installments_InstallmentPlans_InstallmentPlanId",
                table: "Installments",
                column: "InstallmentPlanId",
                principalTable: "InstallmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvestorTransactions_Investors_CloudInvestorId",
                table: "InvestorTransactions",
                column: "CloudInvestorId",
                principalTable: "Investors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Invoices_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CashBoxes_CashBoxId",
                table: "Invoices",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Warehouses_WarehouseId",
                table: "Invoices",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitDistributionDetails_Investors_InvestorId",
                table: "ProfitDistributionDetails",
                column: "InvestorId",
                principalTable: "Investors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_BankAccounts_BankAccountId",
                table: "Vouchers",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_CashBoxes_CashBoxId",
                table: "Vouchers",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Customers_CustomerId",
                table: "Vouchers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Investors_InvestorId",
                table: "Vouchers",
                column: "InvestorId",
                principalTable: "Investors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_Products_ProductId",
                table: "WarehouseStocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                table: "WarehouseStocks",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_CashBoxes_CashBoxId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_ExpenseTypes_ExpenseTypeId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_InstallmentPlans_Customers_CustomerId",
                table: "InstallmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_InstallmentPlans_Invoices_InvoiceId",
                table: "InstallmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Installments_CashBoxes_CashBoxId",
                table: "Installments");

            migrationBuilder.DropForeignKey(
                name: "FK_Installments_InstallmentPlans_InstallmentPlanId",
                table: "Installments");

            migrationBuilder.DropForeignKey(
                name: "FK_InvestorTransactions_Investors_CloudInvestorId",
                table: "InvestorTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Invoices_InvoiceId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CashBoxes_CashBoxId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Warehouses_WarehouseId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitDistributionDetails_Investors_InvestorId",
                table: "ProfitDistributionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_BankAccounts_BankAccountId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_CashBoxes_CashBoxId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Customers_CustomerId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Investors_InvestorId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_Products_ProductId",
                table: "WarehouseStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_ProductId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_WarehouseId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_BankAccountId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CashBoxId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CustomerId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_InvestorId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitDistributionDetails_InvestorId",
                table: "ProfitDistributionDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CashBoxId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SupplierId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_WarehouseId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ProductId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvestorTransactions_CloudInvestorId",
                table: "InvestorTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Installments_CashBoxId",
                table: "Installments");

            migrationBuilder.DropIndex(
                name: "IX_Installments_InstallmentPlanId",
                table: "Installments");

            migrationBuilder.DropIndex(
                name: "IX_InstallmentPlans_CustomerId",
                table: "InstallmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_InstallmentPlans_InvoiceId",
                table: "InstallmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CashBoxId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ExpenseTypeId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CloudInvestorId",
                table: "InvestorTransactions");
        }
    }
}
