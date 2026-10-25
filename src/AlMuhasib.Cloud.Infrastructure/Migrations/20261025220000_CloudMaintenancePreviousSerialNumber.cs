using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <summary>سيريال الجهاز السابق عند الاستبدال في سجلات الصيانة (سحابة).</summary>
public partial class CloudMaintenancePreviousSerialNumber : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.MaintenanceRecords', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.MaintenanceRecords', N'PreviousSerialNumber') IS NULL
                ALTER TABLE [dbo].[MaintenanceRecords] ADD [PreviousSerialNumber] nvarchar(100) NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.MaintenanceRecords', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.MaintenanceRecords', N'PreviousSerialNumber') IS NOT NULL
                ALTER TABLE [dbo].[MaintenanceRecords] DROP COLUMN [PreviousSerialNumber];
            """);
    }
}
