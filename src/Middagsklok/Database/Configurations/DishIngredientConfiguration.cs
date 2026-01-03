using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class DishIngredientConfiguration : IEntityTypeConfiguration<DishIngredientEntity>
{
    public void Configure(EntityTypeBuilder<DishIngredientEntity> builder)
    {
        builder.ToTable("dish_ingredient");

        builder.HasKey(e => new { e.DishId, e.IngredientId });

        builder.Property(e => e.DishId)
            .HasColumnName("dish_id");

        builder.Property(e => e.IngredientId)
            .HasColumnName("ingredient_id");

        builder.Property(e => e.Amount)
            .HasColumnName("amount");

        builder.Property(e => e.Unit)
            .HasColumnName("unit")
            .IsRequired();

        builder.Property(e => e.Optional)
            .HasColumnName("optional");

        builder.HasOne(e => e.Dish)
            .WithMany(d => d.DishIngredients)
            .HasForeignKey(e => e.DishId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Ingredient)
            .WithMany(i => i.DishIngredients)
            .HasForeignKey(e => e.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
