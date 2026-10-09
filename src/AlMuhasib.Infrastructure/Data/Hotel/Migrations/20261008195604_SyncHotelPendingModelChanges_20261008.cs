using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Hotel.Migrations
{
    /// <summary>
    /// مزامنة اللقطة فقط — لا تغيير مخطط.
    /// يصلح PendingModelChangesWarning بعد عزل كيانات المحاسبة عن HotelDbContext.
    /// </summary>
    public partial class SyncHotelPendingModelChanges_20261008 : Migration
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
