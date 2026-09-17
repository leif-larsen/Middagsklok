using Middagsklok.Mcp.Planning;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Mcp.Tests.Planning;

public sealed class PlanEditorTests
{
    private static readonly DateOnly WeekStart = new(2026, 9, 18);

    // Verifies that edits overlay the existing plan and untouched days survive.
    [Test]
    public async Task OverlaysEditsOnExistingPlan()
    {
        const string existing = """
            {"days":[{"date":"2026-09-18","selection":{"type":"DISH","dishId":"taco"},"servings":null},
                     {"date":"2026-09-19","selection":{"type":"DISH","dishId":"burger"},"servings":5}]}
            """;
        var edits = new[] { new PlannedDayEdit("2026-09-19", null, null), new PlannedDayEdit("2026-09-20", "pannekake", 4) };

        var body = PlanEditor.BuildUpsertBody(WeekStart, existing, edits);
        var days = body["days"]!.AsArray();

        await Assert.That(days.Count).IsEqualTo(7);
        await Assert.That(days[0]!["selection"]!["dishId"]!.GetValue<string>()).IsEqualTo("taco");
        await Assert.That(days[1]!["selection"]!["type"]!.GetValue<string>()).IsEqualTo("EMPTY");
        await Assert.That(days[2]!["selection"]!["dishId"]!.GetValue<string>()).IsEqualTo("pannekake");
        await Assert.That(days[2]!["servings"]!.GetValue<int>()).IsEqualTo(4);
        await Assert.That(days[6]!["date"]!.GetValue<string>()).IsEqualTo("2026-09-24");
    }

    // Verifies that a missing plan starts from seven empty days.
    [Test]
    public async Task StartsFromEmptyWeekWithoutExistingPlan()
    {
        var body = PlanEditor.BuildUpsertBody(WeekStart, null, [new PlannedDayEdit("2026-09-18", "taco", null)]);
        var days = body["days"]!.AsArray();

        await Assert.That(days.Count).IsEqualTo(7);
        await Assert.That(days[0]!["selection"]!["type"]!.GetValue<string>()).IsEqualTo("DISH");
        await Assert.That(days.Skip(1).All(day => day!["selection"]!["type"]!.GetValue<string>() == "EMPTY")).IsTrue();
    }

    // Verifies that an edit outside the week is refused rather than silently dropped.
    [Test]
    public async Task RefusesEditOutsideWeek()
    {
        var act = () => PlanEditor.BuildUpsertBody(WeekStart, null, [new PlannedDayEdit("2026-10-01", "taco", null)]);

        await Assert.That(act).Throws<PlanEditOutsideWeek>();
    }
}
