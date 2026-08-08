using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <summary>
    /// Snapshot-only sync. Gold shop tables already exist via CloudGoldShopTables /
    /// CloudGoldShopPhase2 (those migrations shipped with empty Designer stubs, so the
    /// model snapshot never recorded them and EF raised PendingModelChangesWarning).
    /// </summary>
    public partial class SyncCloudPendingModelChanges_20260807 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
