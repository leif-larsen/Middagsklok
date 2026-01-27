namespace Middagsklok.Api.Database.Configuration.Ingredient;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Api.Domain.Ingredient;

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.DefaultUnit)
            .HasColumnName("default_unit")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(i => i.Name)
            .HasDatabaseName("ix_ingredients_name");
    }
}
