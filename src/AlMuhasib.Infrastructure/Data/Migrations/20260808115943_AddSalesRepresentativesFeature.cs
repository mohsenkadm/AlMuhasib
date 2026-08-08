using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepresentativesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesRepresentativeId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesRepresentativeId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesRepresentatives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MonthlySalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CompensationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SalesRepresentatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepresentativeId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HandedOverAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HandedOverAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SalesRepCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepCollections_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCollections_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCollections_SalesRepresentatives_SalesRepresentativeId",
                        column: x => x.SalesRepresentativeId,
                        principalTable: "SalesRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepCommissionEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepresentativeId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommissionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SalesRepCommissionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionEntries_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionEntries_SalesRepresentatives_SalesRepresentativeId",
                        column: x => x.SalesRepresentativeId,
                        principalTable: "SalesRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepCommissionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepresentativeId = table.Column<int>(type: "int", nullable: false),
                    CommissionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SalesRepCommissionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionRules_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionRules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCommissionRules_SalesRepresentatives_SalesRepresentativeId",
                        column: x => x.SalesRepresentativeId,
                        principalTable: "SalesRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepresentativeId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SalesRepTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepTargets_SalesRepresentatives_SalesRepresentativeId",
                        column: x => x.SalesRepresentativeId,
                        principalTable: "SalesRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesRepresentativeId",
                table: "Invoices",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SalesRepresentativeId",
                table: "Customers",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_CollectionDate",
                table: "SalesRepCollections",
                column: "CollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_CustomerId",
                table: "SalesRepCollections",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_InvoiceId",
                table: "SalesRepCollections",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_IsDeleted",
                table: "SalesRepCollections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_ReceiptNumber",
                table: "SalesRepCollections",
                column: "ReceiptNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_SalesRepresentativeId",
                table: "SalesRepCollections",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCollections_SyncId",
                table: "SalesRepCollections",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_CustomerId",
                table: "SalesRepCommissionEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_InvoiceId",
                table: "SalesRepCommissionEntries",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_IsDeleted",
                table: "SalesRepCommissionEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_SalesRepresentativeId",
                table: "SalesRepCommissionEntries",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_SalesRepresentativeId_InvoiceDate",
                table: "SalesRepCommissionEntries",
                columns: new[] { "SalesRepresentativeId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_SalesRepresentativeId_Status",
                table: "SalesRepCommissionEntries",
                columns: new[] { "SalesRepresentativeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionEntries_SyncId",
                table: "SalesRepCommissionEntries",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_CustomerId",
                table: "SalesRepCommissionRules",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_IsDeleted",
                table: "SalesRepCommissionRules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_ProductId",
                table: "SalesRepCommissionRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_SalesRepresentativeId",
                table: "SalesRepCommissionRules",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_SalesRepresentativeId_CommissionType_IsActive",
                table: "SalesRepCommissionRules",
                columns: new[] { "SalesRepresentativeId", "CommissionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCommissionRules_SyncId",
                table: "SalesRepCommissionRules",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepresentatives_IsActive",
                table: "SalesRepresentatives",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepresentatives_IsDeleted",
                table: "SalesRepresentatives",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepresentatives_Name",
                table: "SalesRepresentatives",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepresentatives_Region",
                table: "SalesRepresentatives",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepresentatives_SyncId",
                table: "SalesRepresentatives",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepTargets_IsDeleted",
                table: "SalesRepTargets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepTargets_SalesRepresentativeId",
                table: "SalesRepTargets",
                column: "SalesRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepTargets_SalesRepresentativeId_PeriodStart_PeriodEnd",
                table: "SalesRepTargets",
                columns: new[] { "SalesRepresentativeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepTargets_SyncId",
                table: "SalesRepTargets",
                column: "SyncId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_SalesRepresentatives_SalesRepresentativeId",
                table: "Customers",
                column: "SalesRepresentativeId",
                principalTable: "SalesRepresentatives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SalesRepresentatives_SalesRepresentativeId",
                table: "Invoices",
                column: "SalesRepresentativeId",
                principalTable: "SalesRepresentatives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_SalesRepresentatives_SalesRepresentativeId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SalesRepresentatives_SalesRepresentativeId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "SalesRepCollections");

            migrationBuilder.DropTable(
                name: "SalesRepCommissionEntries");

            migrationBuilder.DropTable(
                name: "SalesRepCommissionRules");

            migrationBuilder.DropTable(
                name: "SalesRepTargets");

            migrationBuilder.DropTable(
                name: "SalesRepresentatives");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SalesRepresentativeId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SalesRepresentativeId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SalesRepresentativeId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SalesRepresentativeId",
                table: "Customers");
        }
    }
}
