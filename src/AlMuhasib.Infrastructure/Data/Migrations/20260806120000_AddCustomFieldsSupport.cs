using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomFieldsJson",
                table: "Products",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFieldsJson",
                table: "Customers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFieldsJson",
                table: "Suppliers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFieldsJson",
                table: "Investors",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntityCustomFieldSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityKind = table.Column<int>(type: "int", nullable: false),
                    DefinitionsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
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
                    table.PrimaryKey("PK_EntityCustomFieldSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityCustomFieldSettings_EntityKind",
                table: "EntityCustomFieldSettings",
                column: "EntityKind",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EntityCustomFieldSettings_IsDeleted",
                table: "EntityCustomFieldSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EntityCustomFieldSettings_SyncId",
                table: "EntityCustomFieldSettings",
                column: "SyncId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EntityCustomFieldSettings");

            migrationBuilder.DropColumn(name: "CustomFieldsJson", table: "Products");
            migrationBuilder.DropColumn(name: "CustomFieldsJson", table: "Customers");
            migrationBuilder.DropColumn(name: "CustomFieldsJson", table: "Suppliers");
            migrationBuilder.DropColumn(name: "CustomFieldsJson", table: "Investors");
        }
    }
}
