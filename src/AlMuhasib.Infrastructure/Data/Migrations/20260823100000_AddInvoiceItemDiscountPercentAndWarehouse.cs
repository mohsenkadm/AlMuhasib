using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemDiscountPercentAndWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "InvoiceItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_WarehouseId",
                table: "InvoiceItems",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Warehouses_WarehouseId",
                table: "InvoiceItems",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Warehouses_WarehouseId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_WarehouseId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "InvoiceItems");
        }
    }
}
