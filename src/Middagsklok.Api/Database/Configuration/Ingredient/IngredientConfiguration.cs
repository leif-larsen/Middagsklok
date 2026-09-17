using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Middagsklok.Api.Database.Configuration.Ingredient;

public class IngredientConfiguration : IEntityTypeConfiguration<IngredientEntity>
{
    public void Configure(EntityTypeBuilder<IngredientEntity> builder)
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

        builder.Property(i => i.IsPantryStaple)
            .HasColumnName("is_pantry_staple")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.OwnsOne(i => i.OdaMapping, mappingBuilder =>
        {
            mappingBuilder.ToTable("ingredient_oda_products");

            mappingBuilder.WithOwner()
                .HasForeignKey("ingredient_id");

            mappingBuilder.Property(m => m.Availability)
                .HasColumnName("availability")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            mappingBuilder.Property(m => m.OdaProductId)
                .HasColumnName("oda_product_id");

            mappingBuilder.Property(m => m.OdaProductName)
                .HasColumnName("oda_product_name")
                .HasMaxLength(300);

            mappingBuilder.Property(m => m.PackQuantity)
                .HasColumnName("pack_quantity");

            mappingBuilder.Property(m => m.PackUnit)
                .HasColumnName("pack_unit")
                .HasConversion<string>()
                .HasMaxLength(10);

            mappingBuilder.Property(m => m.ConfirmedAt)
                .HasColumnName("confirmed_at");

            mappingBuilder.Property(m => m.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            mappingBuilder.Property(m => m.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            mappingBuilder.Ignore(m => m.IsConfirmed);

            mappingBuilder.HasIndex(m => m.OdaProductId)
                .HasDatabaseName("ix_ingredient_oda_products_oda_product_id");
        });

        builder.Navigation(i => i.OdaMapping)
            .AutoInclude();

        builder.HasIndex(i => i.Name)
            .HasDatabaseName("ix_ingredients_name");
    }
}
