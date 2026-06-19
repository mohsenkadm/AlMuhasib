using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Hotel.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantIngredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantMenuCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantMenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantIngredientStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantIngredientId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantIngredientStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantIngredientStocks_RestaurantIngredients_RestaurantIngredientId",
                        column: x => x.RestaurantIngredientId,
                        principalTable: "RestaurantIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantMenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantMenuCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecipeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantMenuItems_RestaurantMenuCategories_RestaurantMenuCategoryId",
                        column: x => x.RestaurantMenuCategoryId,
                        principalTable: "RestaurantMenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantMenuItems_RestaurantRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "RestaurantRecipes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RestaurantRecipeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantRecipeId = table.Column<int>(type: "int", nullable: false),
                    RestaurantIngredientId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantRecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantRecipeLines_RestaurantIngredients_RestaurantIngredientId",
                        column: x => x.RestaurantIngredientId,
                        principalTable: "RestaurantIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantRecipeLines_RestaurantRecipes_RestaurantRecipeId",
                        column: x => x.RestaurantRecipeId,
                        principalTable: "RestaurantRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KitchenStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RestaurantTableId = table.Column<int>(type: "int", nullable: true),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    GuestId = table.Column<int>(type: "int", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CogsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReservationChargeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_RestaurantOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOrders_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantOrders_ReservationCharges_ReservationChargeId",
                        column: x => x.ReservationChargeId,
                        principalTable: "ReservationCharges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantOrders_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantOrders_RestaurantTables_RestaurantTableId",
                        column: x => x.RestaurantTableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantOrders_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantOrderId = table.Column<int>(type: "int", nullable: false),
                    RestaurantMenuItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CogsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderLines_RestaurantMenuItems_RestaurantMenuItemId",
                        column: x => x.RestaurantMenuItemId,
                        principalTable: "RestaurantMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderLines_RestaurantOrders_RestaurantOrderId",
                        column: x => x.RestaurantOrderId,
                        principalTable: "RestaurantOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrderPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantOrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HotelCashBoxId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantOrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderPayments_HotelCashBoxes_HotelCashBoxId",
                        column: x => x.HotelCashBoxId,
                        principalTable: "HotelCashBoxes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantOrderPayments_RestaurantOrders_RestaurantOrderId",
                        column: x => x.RestaurantOrderId,
                        principalTable: "RestaurantOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantStockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantIngredientId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RestaurantOrderId = table.Column<int>(type: "int", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_RestaurantStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantStockMovements_RestaurantIngredients_RestaurantIngredientId",
                        column: x => x.RestaurantIngredientId,
                        principalTable: "RestaurantIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantStockMovements_RestaurantOrders_RestaurantOrderId",
                        column: x => x.RestaurantOrderId,
                        principalTable: "RestaurantOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredients_IsDeleted",
                table: "RestaurantIngredients",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredients_Name",
                table: "RestaurantIngredients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredients_SyncId",
                table: "RestaurantIngredients",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredientStocks_IsDeleted",
                table: "RestaurantIngredientStocks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredientStocks_RestaurantIngredientId",
                table: "RestaurantIngredientStocks",
                column: "RestaurantIngredientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantIngredientStocks_SyncId",
                table: "RestaurantIngredientStocks",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuCategories_IsDeleted",
                table: "RestaurantMenuCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuCategories_SyncId",
                table: "RestaurantMenuCategories",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuItems_IsDeleted",
                table: "RestaurantMenuItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuItems_RecipeId",
                table: "RestaurantMenuItems",
                column: "RecipeId",
                unique: true,
                filter: "[RecipeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuItems_RestaurantMenuCategoryId",
                table: "RestaurantMenuItems",
                column: "RestaurantMenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMenuItems_SyncId",
                table: "RestaurantMenuItems",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderLines_IsDeleted",
                table: "RestaurantOrderLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderLines_RestaurantMenuItemId",
                table: "RestaurantOrderLines",
                column: "RestaurantMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderLines_RestaurantOrderId",
                table: "RestaurantOrderLines",
                column: "RestaurantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderLines_SyncId",
                table: "RestaurantOrderLines",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderPayments_HotelCashBoxId",
                table: "RestaurantOrderPayments",
                column: "HotelCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderPayments_IsDeleted",
                table: "RestaurantOrderPayments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderPayments_RestaurantOrderId",
                table: "RestaurantOrderPayments",
                column: "RestaurantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderPayments_SyncId",
                table: "RestaurantOrderPayments",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_GuestId",
                table: "RestaurantOrders",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_IsDeleted",
                table: "RestaurantOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_OrderDate",
                table: "RestaurantOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_OrderNumber",
                table: "RestaurantOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_ReservationChargeId",
                table: "RestaurantOrders",
                column: "ReservationChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_ReservationId",
                table: "RestaurantOrders",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_RestaurantTableId",
                table: "RestaurantOrders",
                column: "RestaurantTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_RoomId",
                table: "RestaurantOrders",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_Status",
                table: "RestaurantOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrders_SyncId",
                table: "RestaurantOrders",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipeLines_IsDeleted",
                table: "RestaurantRecipeLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipeLines_RestaurantIngredientId",
                table: "RestaurantRecipeLines",
                column: "RestaurantIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipeLines_RestaurantRecipeId",
                table: "RestaurantRecipeLines",
                column: "RestaurantRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipeLines_SyncId",
                table: "RestaurantRecipeLines",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipes_IsDeleted",
                table: "RestaurantRecipes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantRecipes_SyncId",
                table: "RestaurantRecipes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantStockMovements_IsDeleted",
                table: "RestaurantStockMovements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantStockMovements_MovementDate",
                table: "RestaurantStockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantStockMovements_RestaurantIngredientId",
                table: "RestaurantStockMovements",
                column: "RestaurantIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantStockMovements_RestaurantOrderId",
                table: "RestaurantStockMovements",
                column: "RestaurantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantStockMovements_SyncId",
                table: "RestaurantStockMovements",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_IsDeleted",
                table: "RestaurantTables",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_SyncId",
                table: "RestaurantTables",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableNumber",
                table: "RestaurantTables",
                column: "TableNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantIngredientStocks");

            migrationBuilder.DropTable(
                name: "RestaurantOrderLines");

            migrationBuilder.DropTable(
                name: "RestaurantOrderPayments");

            migrationBuilder.DropTable(
                name: "RestaurantRecipeLines");

            migrationBuilder.DropTable(
                name: "RestaurantStockMovements");

            migrationBuilder.DropTable(
                name: "RestaurantMenuItems");

            migrationBuilder.DropTable(
                name: "RestaurantIngredients");

            migrationBuilder.DropTable(
                name: "RestaurantOrders");

            migrationBuilder.DropTable(
                name: "RestaurantMenuCategories");

            migrationBuilder.DropTable(
                name: "RestaurantRecipes");

            migrationBuilder.DropTable(
                name: "RestaurantTables");
        }
    }
}
