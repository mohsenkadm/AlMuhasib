using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingTypesAndTransportFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TransportFeeAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PackagingTypeId",
                table: "ProductUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PackagingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SyncId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagingTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackagingTypes_IsDeleted",
                table: "PackagingTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PackagingTypes_Name",
                table: "PackagingTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PackagingTypes_SyncId",
                table: "PackagingTypes",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_PackagingTypeId",
                table: "ProductUnits",
                column: "PackagingTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnits_PackagingTypes_PackagingTypeId",
                table: "ProductUnits",
                column: "PackagingTypeId",
                principalTable: "PackagingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnits_PackagingTypes_PackagingTypeId",
                table: "ProductUnits");

            migrationBuilder.DropTable(
                name: "PackagingTypes");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnits_PackagingTypeId",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "PackagingTypeId",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "TransportFeeAmount",
                table: "Invoices");
        }
    }
}
