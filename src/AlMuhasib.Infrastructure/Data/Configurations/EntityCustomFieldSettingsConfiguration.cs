using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class EntityCustomFieldSettingsConfiguration : IEntityTypeConfiguration<EntityCustomFieldSettings>
{
    public void Configure(EntityTypeBuilder<EntityCustomFieldSettings> builder)
    {
        builder.ToTable("EntityCustomFieldSettings");

        builder.Property(e => e.DefinitionsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(e => e.EntityKind)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
