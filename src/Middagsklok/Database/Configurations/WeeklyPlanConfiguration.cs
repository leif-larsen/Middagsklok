using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database.Configurations;

public class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlanEntity>
{
    public void Configure(EntityTypeBuilder<WeeklyPlanEntity> builder)
    {
        builder.ToTable("weekly_plan");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.WeekStartDate)
            .HasColumnName("week_start_date")
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v))
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(e => e.WeekStartDate)
            .IsUnique();

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Plan)
            .HasForeignKey(i => i.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
