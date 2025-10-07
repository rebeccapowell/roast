using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class SubmissionEntityConfiguration : IEntityTypeConfiguration<SubmissionEntity>
{
    public void Configure(EntityTypeBuilder<SubmissionEntity> builder)
    {
        builder.ToTable("submissions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.SubmittedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CoffeeBarId, entity.IngredientId, entity.HipsterId })
            .IsUnique();
    }
}
