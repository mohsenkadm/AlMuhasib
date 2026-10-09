using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Qaid.TelegramBot.Infrastructure.Data;

public sealed class BotDbContext : DbContext, IDataProtectionKeyContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    public DbSet<LinkCodeEntity> LinkCodes => Set<LinkCodeEntity>();
    public DbSet<TelegramLinkEntity> TelegramLinks => Set<TelegramLinkEntity>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LinkCodeEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CodeHash).IsUnique();
            e.Property(x => x.CodeHash).HasMaxLength(128);
            e.Property(x => x.CompanyName).HasMaxLength(256);
            e.Property(x => x.Username).HasMaxLength(128);
        });

        modelBuilder.Entity<TelegramLinkEntity>(e =>
        {
            e.HasKey(x => x.TelegramUserId);
            e.Property(x => x.CompanyName).HasMaxLength(256);
            e.Property(x => x.Username).HasMaxLength(128);
            e.HasIndex(x => new { x.TenantId, x.TenantAccountId });
        });
    }
}
