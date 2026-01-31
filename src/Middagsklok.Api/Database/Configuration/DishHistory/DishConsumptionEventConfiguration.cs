using DishConsumptionEventEntity = Middagsklok.Api.Domain.DishHistory.DishConsumptionEvent;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyPlanEntity = Middagsklok.Api.Domain.WeeklyPlan.WeeklyPlan;

namespace Middagsklok.Api.Database.Configuration.DishHistory;

public class DishConsumptionEventConfiguration : IEntityTypeConfiguration<DishConsumptionEventEntity>
{
    // Configures the dish consumption event mapping for EF Core.
    public void Configure(EntityTypeBuilder<DishConsumptionEventEntity> builder)
    {
        builder.ToTable("dish_consumption_events");

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.Id)
            .HasColumnName("id");

        builder.Property(evt => evt.DishId)
            .HasColumnName("dish_id")
            .IsRequired();

        builder.Property(evt => evt.EatenOn)
            .HasColumnName("eaten_on")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(evt => evt.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(evt => evt.WeeklyPlanId)
            .HasColumnName("weekly_plan_id");

        builder.Property(evt => evt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(evt => evt.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne<DishEntity>()
            .WithMany()
            .HasForeignKey(evt => evt.DishId)
            .HasConstraintName("fk_dish_consumption_events_dish_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WeeklyPlanEntity>()
            .WithMany()
            .HasForeignKey(evt => evt.WeeklyPlanId)
            .HasConstraintName("fk_dish_consumption_events_weekly_plan_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evt => new { evt.DishId, evt.EatenOn })
            .HasDatabaseName("ix_dish_consumption_events_dish_id_eaten_on")
            .IsUnique();
    }
}
