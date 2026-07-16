using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Car.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSyncSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudSyncSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoSyncEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoSyncIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudSyncSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CloudSyncSettings",
                columns: new[] { "Id", "AccessToken", "AccessTokenExpiresAt", "ApiBaseUrl", "AutoSyncEnabled", "AutoSyncIntervalMinutes", "LastSuccessfulSyncAt", "LastSyncError", "Password", "RefreshToken", "Username" },
                values: new object[] { 1, null, null, "", false, 15, null, null, "", null, "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudSyncSettings");
        }
    }
}
