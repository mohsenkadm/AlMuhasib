using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.RealEstate.Migrations
{
    /// <inheritdoc />
    public partial class RealEstateExpensesAndTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RealEstateExpenseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_RealEstateExpenseTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RealEstateExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelatedContractId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_RealEstateExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealEstateExpenses_RealEstateContracts_RelatedContractId",
                        column: x => x.RelatedContractId,
                        principalTable: "RealEstateContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RealEstateExpenses_RealEstateExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "RealEstateExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenses_ExpenseDate",
                table: "RealEstateExpenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenses_ExpenseTypeId",
                table: "RealEstateExpenses",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenses_IsDeleted",
                table: "RealEstateExpenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenses_RelatedContractId",
                table: "RealEstateExpenses",
                column: "RelatedContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenses_SyncId",
                table: "RealEstateExpenses",
                column: "SyncId");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenseTypes_IsDeleted",
                table: "RealEstateExpenseTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenseTypes_Name",
                table: "RealEstateExpenseTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateExpenseTypes_SyncId",
                table: "RealEstateExpenseTypes",
                column: "SyncId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealEstateExpenses");

            migrationBuilder.DropTable(
                name: "RealEstateExpenseTypes");
        }
    }
}
