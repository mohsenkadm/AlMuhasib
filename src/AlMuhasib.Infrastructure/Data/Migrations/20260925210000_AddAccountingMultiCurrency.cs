using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingMultiCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MultiCurrencyEnabled",
                table: "BusinessSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CashBoxes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "BankAccounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "Invoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Vouchers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "Vouchers",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "Expenses",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Transfers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "Transfers",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsdToIqd = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_RateDate",
                table: "ExchangeRates",
                column: "RateDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SyncId",
                table: "ExchangeRates",
                column: "SyncId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExchangeRates");

            migrationBuilder.DropColumn(name: "MultiCurrencyEnabled", table: "BusinessSettings");
            migrationBuilder.DropColumn(name: "Currency", table: "CashBoxes");
            migrationBuilder.DropColumn(name: "Currency", table: "BankAccounts");
            migrationBuilder.DropColumn(name: "Currency", table: "Invoices");
            migrationBuilder.DropColumn(name: "FxRate", table: "Invoices");
            migrationBuilder.DropColumn(name: "Currency", table: "Vouchers");
            migrationBuilder.DropColumn(name: "FxRate", table: "Vouchers");
            migrationBuilder.DropColumn(name: "Currency", table: "Expenses");
            migrationBuilder.DropColumn(name: "FxRate", table: "Expenses");
            migrationBuilder.DropColumn(name: "Currency", table: "Transfers");
            migrationBuilder.DropColumn(name: "FxRate", table: "Transfers");
        }
    }
}
