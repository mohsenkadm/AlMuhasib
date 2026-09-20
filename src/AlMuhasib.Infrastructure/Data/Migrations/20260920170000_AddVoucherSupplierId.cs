using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherSupplierId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SupplierId",
                table: "Vouchers",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Suppliers_SupplierId",
                table: "Vouchers",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Repair: Payment vouchers that stored a supplier id in CustomerId (and no real customer).
            migrationBuilder.Sql("""
                UPDATE v
                SET v.SupplierId = v.CustomerId,
                    v.CustomerId = NULL
                FROM Vouchers v
                INNER JOIN Suppliers s ON s.Id = v.CustomerId
                LEFT JOIN Customers c ON c.Id = v.CustomerId
                WHERE v.VoucherType = N'Payment'
                  AND v.SupplierId IS NULL
                  AND v.CustomerId IS NOT NULL
                  AND c.Id IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Vouchers
                SET CustomerId = ISNULL(CustomerId, SupplierId)
                WHERE SupplierId IS NOT NULL
                  AND VoucherType = N'Payment';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Suppliers_SupplierId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_SupplierId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Vouchers");
        }
    }
}
