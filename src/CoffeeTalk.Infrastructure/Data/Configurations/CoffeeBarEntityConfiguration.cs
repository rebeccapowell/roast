using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeTalk.Infrastructure.Data.Configurations;

public sealed class CoffeeBarEntityConfiguration : IEntityTypeConfiguration<CoffeeBarEntity>
{
    public void Configure(EntityTypeBuilder<CoffeeBarEntity> builder)
    {
        builder.ToTable("coffee_bars");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code)
            .HasMaxLength(6)
            .IsRequired();

        builder.HasIndex(entity => entity.Code)
            .IsUnique();

        builder.Property(entity => entity.Theme)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.SubmissionPolicy)
            .HasConversion<int>();

        builder.Property(entity => entity.SubmissionsLocked)
            .IsRequired();

        builder.HasMany(entity => entity.Hipsters)
            .WithOne(hipster => hipster.CoffeeBar)
            .HasForeignKey(hipster => hipster.CoffeeBarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Ingredients)
            .WithOne(ingredient => ingredient.CoffeeBar)
            .HasForeignKey(ingredient => ingredient.CoffeeBarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Submissions)
            .WithOne(submission => submission.CoffeeBar)
            .HasForeignKey(submission => submission.CoffeeBarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Sessions)
            .WithOne(session => session.CoffeeBar)
            .HasForeignKey(session => session.CoffeeBarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
