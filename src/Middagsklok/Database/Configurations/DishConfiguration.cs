using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class DishConfiguration : IEntityTypeConfiguration<DishEntity>
{
    public void Configure(EntityTypeBuilder<DishEntity> builder)
    {
        builder.ToTable("dish");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(e => e.ActiveMinutes)
            .HasColumnName("active_minutes");

        builder.Property(e => e.TotalMinutes)
            .HasColumnName("total_minutes");

        builder.Property(e => e.KidRating)
            .HasColumnName("kid_rating");

        builder.Property(e => e.FamilyRating)
            .HasColumnName("family_rating");

        builder.Property(e => e.IsPescetarian)
            .HasColumnName("is_pescetarian");

        builder.Property(e => e.HasOptionalMeatVariant)
            .HasColumnName("has_optional_meat_variant");

        builder.HasIndex(e => e.Name)
            .IsUnique();
    }
}
