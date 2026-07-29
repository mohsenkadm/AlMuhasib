using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudWarehouseStockMinQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinQuantity",
                table: "WarehouseStocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinQuantity",
                table: "WarehouseStocks");
        }
    }
}
