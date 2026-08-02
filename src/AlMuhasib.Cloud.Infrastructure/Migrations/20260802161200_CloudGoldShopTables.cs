using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudGoldShopTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MithqalGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ScaleComPort = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScaleBaudRate = table.Column<int>(type: "int", nullable: false),
                    ScaleStabilityThresholdGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AllowManualWeightEdit = table.Column<bool>(type: "bit", nullable: false),
                    LowStockAlertGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OverdueDaysThreshold = table.Column<int>(type: "int", nullable: false),
                    EnabledKaratsCsv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_GoldSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldFxRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsdToIqd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_GoldFxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldKarats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurityFactor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_GoldKarats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldMithqalPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    PricePerMithqal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    FxRateUsed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_GoldMithqalPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    WeightGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SuggestedMakingCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MakingChargeCurrency = table.Column<int>(type: "int", nullable: false),
                    CostPerGram = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TrackAsPiece = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_GoldItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldStockBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    GramsOnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AverageCostPerGram = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_GoldStockBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldCustomers",
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
                    table.PrimaryKey("PK_GoldCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldCashBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_GoldCashBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_GoldNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_GoldVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    PricingCurrency = table.Column<int>(type: "int", nullable: false),
                    PaymentCurrency = table.Column<int>(type: "int", nullable: false),
                    FxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalGoldValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalMakingCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountIqd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalWeightGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WeightFromScale = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_GoldInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldInvoices_GoldCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "GoldCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GoldInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    KaratValue = table.Column<int>(type: "int", nullable: false),
                    WeightGrams = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MithqalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerGram = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GoldValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MakingCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WeightFromScale = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_GoldInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldInvoiceLines_GoldInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "GoldInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoldPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    FxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_GoldPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldPayments_GoldInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "GoldInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_GoldSettings_TenantId_SyncId", table: "GoldSettings", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldFxRates_TenantId_SyncId", table: "GoldFxRates", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldFxRates_TenantId_RateDate", table: "GoldFxRates", columns: new[] { "TenantId", "RateDate" });
            migrationBuilder.CreateIndex(name: "IX_GoldKarats_TenantId_SyncId", table: "GoldKarats", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldKarats_TenantId_KaratValue", table: "GoldKarats", columns: new[] { "TenantId", "KaratValue" });
            migrationBuilder.CreateIndex(name: "IX_GoldMithqalPrices_TenantId_SyncId", table: "GoldMithqalPrices", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldMithqalPrices_TenantId_PriceDate_KaratValue", table: "GoldMithqalPrices", columns: new[] { "TenantId", "PriceDate", "KaratValue" });
            migrationBuilder.CreateIndex(name: "IX_GoldItems_TenantId_SyncId", table: "GoldItems", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldItems_TenantId_Barcode", table: "GoldItems", columns: new[] { "TenantId", "Barcode" });
            migrationBuilder.CreateIndex(name: "IX_GoldStockBalances_TenantId_SyncId", table: "GoldStockBalances", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldStockBalances_TenantId_KaratValue", table: "GoldStockBalances", columns: new[] { "TenantId", "KaratValue" });
            migrationBuilder.CreateIndex(name: "IX_GoldCustomers_TenantId_SyncId", table: "GoldCustomers", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldCustomers_TenantId_Name", table: "GoldCustomers", columns: new[] { "TenantId", "Name" });
            migrationBuilder.CreateIndex(name: "IX_GoldCashBoxes_TenantId_SyncId", table: "GoldCashBoxes", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldNotifications_TenantId_SyncId", table: "GoldNotifications", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldNotifications_TenantId_IsRead", table: "GoldNotifications", columns: new[] { "TenantId", "IsRead" });
            migrationBuilder.CreateIndex(name: "IX_GoldVouchers_TenantId_SyncId", table: "GoldVouchers", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldVouchers_TenantId_VoucherNumber", table: "GoldVouchers", columns: new[] { "TenantId", "VoucherNumber" });
            migrationBuilder.CreateIndex(name: "IX_GoldInvoices_TenantId_SyncId", table: "GoldInvoices", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldInvoices_TenantId_InvoiceNumber", table: "GoldInvoices", columns: new[] { "TenantId", "InvoiceNumber" });
            migrationBuilder.CreateIndex(name: "IX_GoldInvoices_CustomerId", table: "GoldInvoices", column: "CustomerId");
            migrationBuilder.CreateIndex(name: "IX_GoldInvoiceLines_TenantId_SyncId", table: "GoldInvoiceLines", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldInvoiceLines_InvoiceId", table: "GoldInvoiceLines", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_GoldPayments_TenantId_SyncId", table: "GoldPayments", columns: new[] { "TenantId", "SyncId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_GoldPayments_InvoiceId", table: "GoldPayments", column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GoldInvoiceLines");
            migrationBuilder.DropTable(name: "GoldPayments");
            migrationBuilder.DropTable(name: "GoldInvoices");
            migrationBuilder.DropTable(name: "GoldVouchers");
            migrationBuilder.DropTable(name: "GoldNotifications");
            migrationBuilder.DropTable(name: "GoldCashBoxes");
            migrationBuilder.DropTable(name: "GoldCustomers");
            migrationBuilder.DropTable(name: "GoldStockBalances");
            migrationBuilder.DropTable(name: "GoldItems");
            migrationBuilder.DropTable(name: "GoldMithqalPrices");
            migrationBuilder.DropTable(name: "GoldKarats");
            migrationBuilder.DropTable(name: "GoldFxRates");
            migrationBuilder.DropTable(name: "GoldSettings");
        }
    }
}
