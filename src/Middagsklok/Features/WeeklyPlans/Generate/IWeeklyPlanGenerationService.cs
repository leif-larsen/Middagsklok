using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

/// <summary>
/// Service responsible for generating weekly meal plans without persistence.
/// </summary>
public interface IWeeklyPlanGenerationService
{
    /// <summary>
    /// Generates a weekly meal plan based on the provided request.
    /// Returns the plan and explanations without saving to the database.
    /// </summary>
    Task<GeneratedWeeklyPlanResult> Generate(
        GenerateWeeklyPlanRequest request,
        CancellationToken ct = default);
}
