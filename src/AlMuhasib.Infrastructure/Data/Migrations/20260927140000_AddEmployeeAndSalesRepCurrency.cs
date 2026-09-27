using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAndSalesRepCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpeningBalanceCurrency",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SalesRepCollections",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SalesRepCommissionEntries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "IQD");

            // وسم عمولات موجودة بعملة فاتورة المصدر
            migrationBuilder.Sql("""
                UPDATE e
                SET e.Currency = i.Currency
                FROM SalesRepCommissionEntries e
                INNER JOIN Invoices i ON i.Id = e.InvoiceId
                WHERE i.Currency IS NOT NULL AND i.Currency <> ''
                """);

            migrationBuilder.Sql("""
                UPDATE c
                SET c.Currency = i.Currency
                FROM SalesRepCollections c
                INNER JOIN Invoices i ON i.Id = c.InvoiceId
                WHERE c.InvoiceId IS NOT NULL
                  AND i.Currency IS NOT NULL AND i.Currency <> ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpeningBalanceCurrency",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SalesRepCollections");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SalesRepCommissionEntries");
        }
    }
}
