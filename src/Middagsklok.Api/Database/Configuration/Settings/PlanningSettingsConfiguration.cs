using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Middagsklok.Api.Domain.Settings;

namespace Middagsklok.Api.Database.Configuration.Settings;

public class PlanningSettingsConfiguration : IEntityTypeConfiguration<PlanningSettings>
{
    // Configures the planning settings mapping for EF Core.
    public void Configure(EntityTypeBuilder<PlanningSettings> builder)
    {
        builder.ToTable("planning_settings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .HasColumnName("id");

        builder.Property(settings => settings.WeekStartsOn)
            .HasColumnName("week_starts_on")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(settings => settings.SeafoodPerWeek)
            .HasColumnName("seafood_per_week")
            .HasDefaultValue(2)
            .IsRequired();

        builder.Property(settings => settings.DaysBetween)
            .HasColumnName("days_between")
            .HasDefaultValue(14)
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(settings => settings.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
