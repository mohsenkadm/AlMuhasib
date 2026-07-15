using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudProductPricing : Migration
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
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "IX_BusinessSettings_TenantId_SyncId",
                table: "BusinessSettings",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_TenantId_Name",
                table: "PricingTypes",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingTypes_TenantId_SyncId",
                table: "PricingTypes",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_PricingTypeId",
                table: "ProductPrices",
                column: "PricingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductId",
                table: "ProductPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_TenantId_ProductId_PricingTypeId",
                table: "ProductPrices",
                columns: new[] { "TenantId", "ProductId", "PricingTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_TenantId_SyncId",
                table: "ProductPrices",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_PricingTypes_PricingTypeId",
                table: "InvoiceItems",
                column: "PricingTypeId",
                principalTable: "PricingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                INSERT INTO PricingTypes (Name, IsDefault, IsActive, TenantId, SyncId, CreatedAt, CreatedBy, IsDeleted)
                SELECT N'سعر مفرد', 1, 1, t.Id, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001', '2026-07-01T00:00:00Z', N'System', 0
                FROM Tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM PricingTypes pt
                    WHERE pt.TenantId = t.Id AND pt.IsDeleted = 0
                      AND (pt.IsDefault = 1 OR pt.SyncId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001' OR pt.Name = N'سعر مفرد')
                );

                INSERT INTO BusinessSettings (ProductPricingEnabled, UpdateProductPriceOnPurchase, TenantId, SyncId, CreatedAt, CreatedBy, IsDeleted)
                SELECT 0, 0, t.Id, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002', '2026-07-01T00:00:00Z', N'System', 0
                FROM Tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM BusinessSettings bs
                    WHERE bs.TenantId = t.Id AND bs.IsDeleted = 0
                );
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
