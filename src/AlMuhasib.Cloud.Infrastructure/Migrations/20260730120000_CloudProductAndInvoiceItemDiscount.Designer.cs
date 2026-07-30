using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    [DbContext(typeof(CloudDbContext))]
    [Migration("20260730120000_CloudProductAndInvoiceItemDiscount")]
    partial class CloudProductAndInvoiceItemDiscount
    {
    }
}
