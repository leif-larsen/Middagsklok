using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class DishHistoryConfiguration : IEntityTypeConfiguration<DishHistoryEntity>
{
    public void Configure(EntityTypeBuilder<DishHistoryEntity> builder)
    {
        builder.ToTable("dish_history");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.DishId)
            .HasColumnName("dish_id")
            .IsRequired();

        builder.Property(e => e.Date)
            .HasColumnName("date")
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v))
            .IsRequired();

        builder.Property(e => e.RatingOverride)
            .HasColumnName("rating_override");

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        builder.HasOne(e => e.Dish)
            .WithMany()
            .HasForeignKey(e => e.DishId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.DishId, e.Date })
            .HasDatabaseName("ix_dish_history_dish_date");
    }
}
