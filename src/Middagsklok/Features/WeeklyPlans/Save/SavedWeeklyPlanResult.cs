using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Save;

public record SavedWeeklyPlanResult(
    WeeklyPlan Plan,
    string Status,
    IReadOnlyList<RuleViolation> Violations);
