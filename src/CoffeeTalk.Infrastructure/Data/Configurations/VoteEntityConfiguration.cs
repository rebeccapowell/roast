using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class VoteEntityConfiguration : IEntityTypeConfiguration<VoteEntity>
{
    public void Configure(EntityTypeBuilder<VoteEntity> builder)
    {
        builder.ToTable("votes");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CastAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.BrewCycleId, entity.VoterHipsterId })
            .IsUnique();
    }
}
