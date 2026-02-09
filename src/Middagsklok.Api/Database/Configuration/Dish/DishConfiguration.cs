using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using DishIngredientEntity = Middagsklok.Api.Domain.Dish.DishIngredient;
using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Middagsklok.Api.Database.Configuration.Dish;

public class DishConfiguration : IEntityTypeConfiguration<DishEntity>
{
    public void Configure(EntityTypeBuilder<DishEntity> builder)
    {
        builder.ToTable("dishes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Cuisine)
            .HasColumnName("cuisine")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.PrepTimeMinutes)
            .HasColumnName("prep_time_minutes")
            .IsRequired();

        builder.Property(d => d.CookTimeMinutes)
            .HasColumnName("cook_time_minutes")
            .IsRequired();

        builder.Property(d => d.Servings)
            .HasColumnName("servings")
            .IsRequired();

        builder.Property(d => d.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(5000);

        builder.Property(d => d.IsSeafood)
            .HasColumnName("is_seafood")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.IsVegetarian)
            .HasColumnName("is_vegetarian")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.IsVegan)
            .HasColumnName("is_vegan")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Ignore(d => d.TotalTimeMinutes);

        builder.OwnsMany(d => d.Ingredients, ingredientBuilder =>
        {
            ingredientBuilder.ToTable("dish_ingredients");

            ingredientBuilder.WithOwner()
                .HasForeignKey("dish_id");

            ingredientBuilder.Property<int>("id")
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            ingredientBuilder.HasKey("id");

            ingredientBuilder.Property(i => i.IngredientId)
                .HasColumnName("ingredient_id")
                .IsRequired();

            ingredientBuilder.Property(i => i.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            ingredientBuilder.Property(i => i.Unit)
                .HasColumnName("unit")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            ingredientBuilder.Property(i => i.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

            ingredientBuilder.Property(i => i.SortOrder)
                .HasColumnName("sort_order");

            ingredientBuilder.HasOne<IngredientEntity>()
                .WithMany()
                .HasForeignKey(i => i.IngredientId)
                .HasConstraintName("fk_dish_ingredients_ingredient_id")
                .OnDelete(DeleteBehavior.Restrict);

            ingredientBuilder.HasIndex("dish_id", nameof(DishIngredientEntity.SortOrder))
                .HasDatabaseName("ix_dish_ingredients_dish_id_sort_order");
        });

        builder.HasIndex(d => d.Name)
            .HasDatabaseName("ix_dishes_name");

        builder.HasIndex(d => d.Cuisine)
            .HasDatabaseName("ix_dishes_cuisine");
    }
}
