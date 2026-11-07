using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Cloud.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(CloudDbContext))]
[Migration("20261107180000_AddCloudCategoryBranches")]
public partial class AddCloudCategoryBranches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CategoryBranches",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TenantId = table.Column<int>(type: "int", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                BranchId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryBranches", x => x.Id);
                table.ForeignKey(
                    name: "FK_CategoryBranches_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CategoryBranches_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryBranches_BranchId",
            table: "CategoryBranches",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryBranches_CategoryId",
            table: "CategoryBranches",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryBranches_TenantId_BranchId",
            table: "CategoryBranches",
            columns: new[] { "TenantId", "BranchId" });

        migrationBuilder.CreateIndex(
            name: "IX_CategoryBranches_TenantId_CategoryId_BranchId",
            table: "CategoryBranches",
            columns: new[] { "TenantId", "CategoryId", "BranchId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CategoryBranches");
    }
}
