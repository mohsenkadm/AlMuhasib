using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CloudProductCarShowroomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleColor",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCount",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlateNumber",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlateType",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VehicleType", table: "Products");
            migrationBuilder.DropColumn(name: "ChassisNumber", table: "Products");
            migrationBuilder.DropColumn(name: "VehicleColor", table: "Products");
            migrationBuilder.DropColumn(name: "PassengerCount", table: "Products");
            migrationBuilder.DropColumn(name: "PlateNumber", table: "Products");
            migrationBuilder.DropColumn(name: "PlateType", table: "Products");
        }
    }
}
