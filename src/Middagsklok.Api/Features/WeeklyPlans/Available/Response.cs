namespace Middagsklok.Api.Features.WeeklyPlans.Available;

public sealed record Response(IEnumerable<WeeklyPlanSummary> Plans);

public sealed record WeeklyPlanSummary(
    string StartDate,
    string EndDate);
