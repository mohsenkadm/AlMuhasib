using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class CustomerLoyaltyAccountConfiguration : IEntityTypeConfiguration<CustomerLoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<CustomerLoyaltyAccount> builder)
    {
        builder.ToTable("CustomerLoyaltyAccounts");

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
