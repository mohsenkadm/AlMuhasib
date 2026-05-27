using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintBrandingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhonePrimary = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneSecondary = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ShowHeaderText = table.Column<bool>(type: "bit", nullable: false),
                    ShowHeaderImage = table.Column<bool>(type: "bit", nullable: false),
                    HeaderImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    HeaderImageContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShowFooterText = table.Column<bool>(type: "bit", nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ShowFooterImage = table.Column<bool>(type: "bit", nullable: false),
                    FooterImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FooterImageContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintBrandingSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintBrandingSettings_IsDeleted",
                table: "PrintBrandingSettings",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintBrandingSettings");
        }
    }
}
