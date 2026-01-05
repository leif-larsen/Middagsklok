using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class WeeklyPlanItemConfiguration : IEntityTypeConfiguration<WeeklyPlanItemEntity>
{
    public void Configure(EntityTypeBuilder<WeeklyPlanItemEntity> builder)
    {
        builder.ToTable("weekly_plan_item");

        builder.HasKey(e => new { e.PlanId, e.DayIndex });

        builder.Property(e => e.PlanId)
            .HasColumnName("plan_id");

        builder.Property(e => e.DayIndex)
            .HasColumnName("day_index");

        builder.Property(e => e.DishId)
            .HasColumnName("dish_id");

        builder.HasOne(e => e.Dish)
            .WithMany()
            .HasForeignKey(e => e.DishId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
