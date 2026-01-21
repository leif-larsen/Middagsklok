namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// DTO representing a rule violation in a weekly plan.
/// </summary>
public record RuleViolationDto(
    string RuleCode,
    string Message,
    IReadOnlyList<int> DayIndices);
