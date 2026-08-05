using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Snapshot-only sync. Schema changes live in AddLoyaltySystem.
    /// Fixes PendingModelChangesWarning so customer MigrateAsync can apply pending migrations.
    /// </summary>
    public partial class SyncPendingModelChanges_20260804 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema changes — model snapshot was out of sync with hand-written AddLoyaltySystem.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
