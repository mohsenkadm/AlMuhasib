using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTaskAssignedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Users_UserId1",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_UserId1",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserTasks");

            migrationBuilder.AddColumn<int>(
                name: "AssignedByUserId",
                table: "UserTasks",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE UserTasks SET AssignedByUserId = UserId WHERE AssignedByUserId IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "AssignedByUserId",
                table: "UserTasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_AssignedByUserId_IsDeleted",
                table: "UserTasks",
                columns: new[] { "AssignedByUserId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Users_AssignedByUserId",
                table: "UserTasks",
                column: "AssignedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Users_AssignedByUserId",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_AssignedByUserId_IsDeleted",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "UserTasks");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "UserTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_UserId1",
                table: "UserTasks",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Users_UserId1",
                table: "UserTasks",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
