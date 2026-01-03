using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class IngredientConfiguration : IEntityTypeConfiguration<IngredientEntity>
{
    public void Configure(EntityTypeBuilder<IngredientEntity> builder)
    {
        builder.ToTable("ingredient");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .IsRequired();

        builder.Property(e => e.DefaultUnit)
            .HasColumnName("default_unit")
            .IsRequired();

        builder.HasIndex(e => e.Name)
            .IsUnique();
    }
}
