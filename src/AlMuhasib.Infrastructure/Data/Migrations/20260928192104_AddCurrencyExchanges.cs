using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyExchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrencyExchanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromCashBoxId = table.Column<int>(type: "int", nullable: false),
                    ToCashBoxId = table.Column<int>(type: "int", nullable: false),
                    FromCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FromAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyExchanges_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrencyExchanges_CashBoxes_FromCashBoxId",
                        column: x => x.FromCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyExchanges_CashBoxes_ToCashBoxId",
                        column: x => x.ToCashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_BranchId",
                table: "CurrencyExchanges",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_Date",
                table: "CurrencyExchanges",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_FromCashBoxId",
                table: "CurrencyExchanges",
                column: "FromCashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_IsDeleted",
                table: "CurrencyExchanges",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_SyncId",
                table: "CurrencyExchanges",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_ToCashBoxId",
                table: "CurrencyExchanges",
                column: "ToCashBoxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyExchanges");
        }
    }
}
