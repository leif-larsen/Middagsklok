using Middagsklok.Mcp.Planning;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Mcp.Tests.Planning;

public sealed class OverwriteGuardTests
{
    // Verifies that a plan with any dish selected counts as planned.
    [Test]
    public async Task DetectsPlannedDays()
    {
        const string plan = """
            {"days":[{"date":"2026-09-18","selection":{"type":"EMPTY","dishId":null}},
                     {"date":"2026-09-19","selection":{"type":"DISH","dishId":"abc"}}]}
            """;

        await Assert.That(OverwriteGuard.HasPlannedDays(plan)).IsTrue();
    }

    // Verifies that an all-empty plan does not block generation.
    [Test]
    public async Task IgnoresEmptyWeeks()
    {
        const string plan = """
            {"days":[{"date":"2026-09-18","selection":{"type":"EMPTY","dishId":null}},
                     {"date":"2026-09-19","selection":{"type":"empty","dishId":null}}]}
            """;

        await Assert.That(OverwriteGuard.HasPlannedDays(plan)).IsFalse();
        await Assert.That(OverwriteGuard.HasPlannedDays("{}")).IsFalse();
    }
}
