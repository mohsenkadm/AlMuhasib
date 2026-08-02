using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudGoldShopPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldWarehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("PK_GoldWarehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldSuppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreditBalanceIqd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditBalanceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_GoldSuppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldExpenseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_GoldExpenseTypes", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "GoldStockBalances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "GoldInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "GoldInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExchange",
                table: "GoldInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeCashDifference",
                table: "GoldInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LineDirection",
                table: "GoldInvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                INSERT INTO GoldWarehouses (Name, IsDefault, IsActive, Notes, TenantId, SyncId, CreatedAt, CreatedBy, IsDeleted)
                SELECT N'المخزن الرئيسي', 1, 1, N'', t.TenantId, NEWID(), SYSUTCDATETIME(), N'system', 0
                FROM (
                    SELECT DISTINCT TenantId FROM GoldStockBalances
                    UNION
                    SELECT DISTINCT TenantId FROM GoldInvoices
                    UNION
                    SELECT DISTINCT TenantId FROM GoldSettings
                ) t
                WHERE NOT EXISTS (
                    SELECT 1 FROM GoldWarehouses w WHERE w.TenantId = t.TenantId AND w.IsDeleted = 0);

                UPDATE sb
                SET WarehouseId = w.Id
                FROM GoldStockBalances sb
                CROSS APPLY (
                    SELECT TOP 1 Id
                    FROM GoldWarehouses w
                    WHERE w.TenantId = sb.TenantId AND w.IsDeleted = 0
                    ORDER BY w.IsDefault DESC, w.Id
                ) w
                WHERE sb.WarehouseId IS NULL OR sb.WarehouseId = 0;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "GoldStockBalances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "GoldExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpenseTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_GoldExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldExpenses_GoldExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "GoldExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldExpenses_GoldCashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "GoldCashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldExpenses_GoldWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "GoldWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GoldWarehouseTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ToWarehouseId = table.Column<int>(type: "int", nullable: false),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    WeightGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("PK_GoldWarehouseTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldWarehouseTransfers_GoldWarehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "GoldWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldWarehouseTransfers_GoldWarehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "GoldWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouses_TenantId_SyncId",
                table: "GoldWarehouses",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouses_TenantId_Name",
                table: "GoldWarehouses",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldSuppliers_TenantId_SyncId",
                table: "GoldSuppliers",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldSuppliers_TenantId_Name",
                table: "GoldSuppliers",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenseTypes_TenantId_SyncId",
                table: "GoldExpenseTypes",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenseTypes_TenantId_Name",
                table: "GoldExpenseTypes",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenses_TenantId_SyncId",
                table: "GoldExpenses",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenses_TenantId_ExpenseDate",
                table: "GoldExpenses",
                columns: new[] { "TenantId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenses_ExpenseTypeId",
                table: "GoldExpenses",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenses_CashBoxId",
                table: "GoldExpenses",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldExpenses_WarehouseId",
                table: "GoldExpenses",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouseTransfers_TenantId_SyncId",
                table: "GoldWarehouseTransfers",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouseTransfers_TenantId_TransferDate",
                table: "GoldWarehouseTransfers",
                columns: new[] { "TenantId", "TransferDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouseTransfers_FromWarehouseId",
                table: "GoldWarehouseTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldWarehouseTransfers_ToWarehouseId",
                table: "GoldWarehouseTransfers",
                column: "ToWarehouseId");

            migrationBuilder.DropIndex(
                name: "IX_GoldStockBalances_TenantId_KaratValue",
                table: "GoldStockBalances");

            migrationBuilder.CreateIndex(
                name: "IX_GoldStockBalances_TenantId_WarehouseId_KaratValue",
                table: "GoldStockBalances",
                columns: new[] { "TenantId", "WarehouseId", "KaratValue" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldStockBalances_WarehouseId",
                table: "GoldStockBalances",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoldStockBalances_GoldWarehouses_WarehouseId",
                table: "GoldStockBalances",
                column: "WarehouseId",
                principalTable: "GoldWarehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_GoldInvoices_SupplierId",
                table: "GoldInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldInvoices_WarehouseId",
                table: "GoldInvoices",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoldInvoices_GoldSuppliers_SupplierId",
                table: "GoldInvoices",
                column: "SupplierId",
                principalTable: "GoldSuppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GoldInvoices_GoldWarehouses_WarehouseId",
                table: "GoldInvoices",
                column: "WarehouseId",
                principalTable: "GoldWarehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoldInvoices_GoldSuppliers_SupplierId",
                table: "GoldInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_GoldInvoices_GoldWarehouses_WarehouseId",
                table: "GoldInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_GoldStockBalances_GoldWarehouses_WarehouseId",
                table: "GoldStockBalances");

            migrationBuilder.DropTable(name: "GoldWarehouseTransfers");
            migrationBuilder.DropTable(name: "GoldExpenses");
            migrationBuilder.DropTable(name: "GoldExpenseTypes");
            migrationBuilder.DropTable(name: "GoldSuppliers");
            migrationBuilder.DropTable(name: "GoldWarehouses");

            migrationBuilder.DropIndex(
                name: "IX_GoldInvoices_SupplierId",
                table: "GoldInvoices");

            migrationBuilder.DropIndex(
                name: "IX_GoldInvoices_WarehouseId",
                table: "GoldInvoices");

            migrationBuilder.DropIndex(
                name: "IX_GoldStockBalances_TenantId_WarehouseId_KaratValue",
                table: "GoldStockBalances");

            migrationBuilder.DropIndex(
                name: "IX_GoldStockBalances_WarehouseId",
                table: "GoldStockBalances");

            migrationBuilder.DropColumn(name: "SupplierId", table: "GoldInvoices");
            migrationBuilder.DropColumn(name: "WarehouseId", table: "GoldInvoices");
            migrationBuilder.DropColumn(name: "IsExchange", table: "GoldInvoices");
            migrationBuilder.DropColumn(name: "ExchangeCashDifference", table: "GoldInvoices");
            migrationBuilder.DropColumn(name: "LineDirection", table: "GoldInvoiceLines");
            migrationBuilder.DropColumn(name: "WarehouseId", table: "GoldStockBalances");

            migrationBuilder.CreateIndex(
                name: "IX_GoldStockBalances_TenantId_KaratValue",
                table: "GoldStockBalances",
                columns: new[] { "TenantId", "KaratValue" });
        }
    }
}
