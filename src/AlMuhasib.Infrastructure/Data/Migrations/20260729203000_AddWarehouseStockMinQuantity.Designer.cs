using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729203000_AddWarehouseStockMinQuantity")]
    partial class AddWarehouseStockMinQuantity
    {
    }
}
