using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class BrewSessionEntityConfiguration : IEntityTypeConfiguration<BrewSessionEntity>
{
    public void Configure(EntityTypeBuilder<BrewSessionEntity> builder)
    {
        builder.ToTable("brew_sessions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.StartedAt)
            .IsRequired();

        builder.HasMany(entity => entity.Cycles)
            .WithOne(cycle => cycle.BrewSession)
            .HasForeignKey(cycle => cycle.BrewSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
