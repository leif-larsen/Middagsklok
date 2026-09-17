using Middagsklok.Mcp.Planning;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Mcp.Tests.Planning;

public sealed class WeekResolverTests
{
    // Verifies that a mid-week date resolves back to the configured start day.
    [Test]
    public async Task ResolvesToMostRecentStartDay()
    {
        var thursday = new DateOnly(2026, 9, 17);

        await Assert.That(WeekResolver.WeekStartFor(thursday, DayOfWeek.Friday)).IsEqualTo(new DateOnly(2026, 9, 11));
        await Assert.That(WeekResolver.WeekStartFor(thursday, DayOfWeek.Monday)).IsEqualTo(new DateOnly(2026, 9, 14));
    }

    // Verifies that the start day itself resolves to the same date.
    [Test]
    public async Task StartDayResolvesToItself()
    {
        var friday = new DateOnly(2026, 9, 18);

        await Assert.That(WeekResolver.WeekStartFor(friday, DayOfWeek.Friday)).IsEqualTo(friday);
    }

    // Verifies that the settings value parses case-insensitively and falls back to Monday.
    [Test]
    public async Task ParsesWeekStartSetting()
    {
        await Assert.That(WeekResolver.ParseWeekStartsOn("friday")).IsEqualTo(DayOfWeek.Friday);
        await Assert.That(WeekResolver.ParseWeekStartsOn(null)).IsEqualTo(DayOfWeek.Monday);
        await Assert.That(WeekResolver.ParseWeekStartsOn("Someday")).IsEqualTo(DayOfWeek.Monday);
    }
}
