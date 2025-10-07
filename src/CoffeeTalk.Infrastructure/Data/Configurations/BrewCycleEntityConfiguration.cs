using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class BrewCycleEntityConfiguration : IEntityTypeConfiguration<BrewCycleEntity>
{
    public void Configure(EntityTypeBuilder<BrewCycleEntity> builder)
    {
        builder.ToTable("brew_cycles");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.StartedAt)
            .IsRequired();

        builder.HasMany(entity => entity.Votes)
            .WithOne(vote => vote.BrewCycle)
            .HasForeignKey(vote => vote.BrewCycleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
