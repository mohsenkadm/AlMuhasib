using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudAddVoucherSupplierId : Migration
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
                principalColumn: "Id");

            // Payment = 1 (enum int). Move mis-keyed supplier ids out of CustomerId.
            migrationBuilder.Sql("""
                UPDATE v
                SET v.SupplierId = v.CustomerId,
                    v.CustomerId = NULL
                FROM Vouchers v
                INNER JOIN Suppliers s ON s.Id = v.CustomerId AND s.TenantId = v.TenantId
                LEFT JOIN Customers c ON c.Id = v.CustomerId AND c.TenantId = v.TenantId
                WHERE v.VoucherType = 1
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
                  AND VoucherType = 1;
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
