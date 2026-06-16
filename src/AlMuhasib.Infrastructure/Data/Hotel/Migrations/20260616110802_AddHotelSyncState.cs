using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Hotel.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastPulledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPushedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerCursor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.EntityType);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncStates");
        }
    }
}
