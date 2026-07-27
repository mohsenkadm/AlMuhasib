using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PricingTypeId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductPricingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdateProductPriceOnPurchase = table.Column<bool>(type: "bit", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PricingTypeId = table.Column<int>(type: "int", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPrices_PricingTypes_PricingTypeId",
                        column: x => x.PricingTypeId,
                        principalTable: "PricingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_PricingTypeId",
                table: "InvoiceItems",
                column: "PricingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSettings_IsDeleted",
                table: "BusinessSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSettings_SyncId",
                table: "BusinessSettings",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_IsDeleted",
                table: "PricingTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_Name",
                table: "PricingTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_SyncId",
                table: "PricingTypes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_IsDeleted",
                table: "ProductPrices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_PricingTypeId",
                table: "ProductPrices",
                column: "PricingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductId_PricingTypeId",
                table: "ProductPrices",
                columns: new[] { "ProductId", "PricingTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_SyncId",
                table: "ProductPrices",
                column: "SyncId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_PricingTypes_PricingTypeId",
                table: "InvoiceItems",
                column: "PricingTypeId",
                principalTable: "PricingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM PricingTypes WHERE IsDeleted = 0 AND (IsDefault = 1 OR Name = N'سعر مفرد'))
                BEGIN
                    INSERT INTO PricingTypes (Name, IsDefault, IsActive, SyncId, CreatedAt, CreatedBy, IsDeleted)
                    VALUES (N'سعر مفرد', 1, 1, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001', '2026-07-01T00:00:00Z', N'System', 0);
                END

                IF NOT EXISTS (SELECT 1 FROM BusinessSettings WHERE IsDeleted = 0)
                BEGIN
                    INSERT INTO BusinessSettings (ProductPricingEnabled, UpdateProductPriceOnPurchase, SyncId, CreatedAt, CreatedBy, IsDeleted)
                    VALUES (0, 0, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002', '2026-07-01T00:00:00Z', N'System', 0);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_PricingTypes_PricingTypeId",
                table: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "BusinessSettings");

            migrationBuilder.DropTable(
                name: "ProductPrices");

            migrationBuilder.DropTable(
                name: "PricingTypes");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_PricingTypeId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "PricingTypeId",
                table: "InvoiceItems");
        }
    }
}
