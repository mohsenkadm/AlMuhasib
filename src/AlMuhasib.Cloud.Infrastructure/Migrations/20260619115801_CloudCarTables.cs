using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudCarTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarSaleContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContractDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SellerIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SellerIdDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuyerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BuyerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BuyerIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuyerIdDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnnualOwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AnnualOwnerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CarType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChassisNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarPriceInWords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CarSaleContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarContractPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RemainingBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_CarContractPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarContractPayments_CarSaleContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CarSaleContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarContractPayments_ContractId",
                table: "CarContractPayments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_CarContractPayments_TenantId_SyncId",
                table: "CarContractPayments",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarSaleContracts_TenantId_ContractNumber",
                table: "CarSaleContracts",
                columns: new[] { "TenantId", "ContractNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CarSaleContracts_TenantId_SyncId",
                table: "CarSaleContracts",
                columns: new[] { "TenantId", "SyncId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarContractPayments");

            migrationBuilder.DropTable(
                name: "CarSaleContracts");
        }
    }
}
