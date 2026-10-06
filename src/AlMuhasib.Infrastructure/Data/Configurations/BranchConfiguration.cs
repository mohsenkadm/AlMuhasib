using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);

        builder.HasIndex(b => b.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(b => b.IsActive);
        builder.HasIndex(b => b.IsMain);
    }
}

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranches");

        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique();
        builder.HasIndex(ub => ub.BranchId);

        builder.HasOne(ub => ub.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Branch)
            .WithMany(b => b.UserBranches)
            .HasForeignKey(ub => ub.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductBranchConfiguration : IEntityTypeConfiguration<ProductBranch>
{
    public void Configure(EntityTypeBuilder<ProductBranch> builder)
    {
        builder.ToTable("ProductBranches");

        builder.HasIndex(pb => new { pb.ProductId, pb.BranchId }).IsUnique();
        builder.HasIndex(pb => pb.BranchId);

        builder.HasOne(pb => pb.Product)
            .WithMany(p => p.ProductBranches)
            .HasForeignKey(pb => pb.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pb => pb.Branch)
            .WithMany(b => b.ProductBranches)
            .HasForeignKey(pb => pb.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
