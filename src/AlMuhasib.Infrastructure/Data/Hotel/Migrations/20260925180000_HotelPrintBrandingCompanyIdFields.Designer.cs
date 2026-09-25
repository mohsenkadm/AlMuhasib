using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Hotel.Migrations
{
    [DbContext(typeof(HotelDbContext))]
    [Migration("20260925180000_HotelPrintBrandingCompanyIdFields")]
    partial class HotelPrintBrandingCompanyIdFields
    {
    }
}
