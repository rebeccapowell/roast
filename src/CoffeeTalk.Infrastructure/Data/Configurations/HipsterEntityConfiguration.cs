using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class HipsterEntityConfiguration : IEntityTypeConfiguration<HipsterEntity>
{
    public void Configure(EntityTypeBuilder<HipsterEntity> builder)
    {
        builder.ToTable("hipsters");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Username)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(entity => entity.NormalizedUsername)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CoffeeBarId, entity.NormalizedUsername })
            .IsUnique();

        builder.Property(entity => entity.MaxIngredientQuota)
            .IsRequired();

        builder.HasMany(entity => entity.Submissions)
            .WithOne(submission => submission.Hipster)
            .HasForeignKey(submission => submission.HipsterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Votes)
            .WithOne(vote => vote.Voter)
            .HasForeignKey(vote => vote.VoterHipsterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
