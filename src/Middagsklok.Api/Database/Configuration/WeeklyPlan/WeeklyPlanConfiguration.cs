using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using PlannedDayEntity = Middagsklok.Api.Domain.WeeklyPlan.PlannedDay;
using WeeklyPlanEntity = Middagsklok.Api.Domain.WeeklyPlan.WeeklyPlan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Middagsklok.Api.Database.Configuration.WeeklyPlan;

public class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlanEntity>
{
    // Configures the weekly plan mapping for EF Core.
    public void Configure(EntityTypeBuilder<WeeklyPlanEntity> builder)
    {
        builder.ToTable("weekly_plans");

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Id)
            .HasColumnName("id");

        builder.Property(plan => plan.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(plan => plan.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(plan => plan.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Ignore(plan => plan.EndDate);
        builder.Ignore(plan => plan.PlannedDishes);

        builder.OwnsMany(plan => plan.Days, dayBuilder =>
        {
            dayBuilder.ToTable("weekly_plan_days");

            dayBuilder.WithOwner()
                .HasForeignKey("weekly_plan_id");

            dayBuilder.Property<int>("id")
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            dayBuilder.HasKey("id");

            dayBuilder.Property(day => day.Date)
                .HasColumnName("date")
                .HasColumnType("date")
                .IsRequired();

            dayBuilder.OwnsOne(day => day.Selection, selectionBuilder =>
            {
                selectionBuilder.Property(selection => selection.Type)
                    .HasColumnName("selection_type")
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                selectionBuilder.Property(selection => selection.DishId)
                    .HasColumnName("dish_id");

                selectionBuilder.HasOne<DishEntity>()
                    .WithMany()
                    .HasForeignKey(selection => selection.DishId)
                    .HasConstraintName("fk_weekly_plan_days_dish_id")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            dayBuilder.Navigation(day => day.Selection)
                .IsRequired();

            dayBuilder.HasIndex("weekly_plan_id", nameof(PlannedDayEntity.Date))
                .HasDatabaseName("ix_weekly_plan_days_weekly_plan_id_date")
                .IsUnique();
        });

        builder.HasIndex(plan => plan.StartDate)
            .HasDatabaseName("ix_weekly_plans_start_date")
            .IsUnique();
    }
}
