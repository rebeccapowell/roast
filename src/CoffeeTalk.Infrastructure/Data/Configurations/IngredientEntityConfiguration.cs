using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class IngredientEntityConfiguration : IEntityTypeConfiguration<IngredientEntity>
{
    public void Configure(EntityTypeBuilder<IngredientEntity> builder)
    {
        builder.ToTable("ingredients");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.VideoId)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.IsConsumed)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasMaxLength(200);

        builder.Property(entity => entity.ThumbnailUrl)
            .HasMaxLength(512);

        builder.HasMany(entity => entity.Submissions)
            .WithOne(submission => submission.Ingredient)
            .HasForeignKey(submission => submission.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.BrewCycles)
            .WithOne(cycle => cycle.Ingredient)
            .HasForeignKey(cycle => cycle.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
