using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Generate;

namespace Middagsklok.Features.WeeklyPlans.Suggest;

/// <summary>
/// Feature that suggests a weekly meal plan WITHOUT persisting it.
/// Useful for previewing plans before saving.
/// </summary>
public class SuggestWeeklyPlanFeature
{
    private readonly IWeeklyPlanGenerationService _generationService;

    public SuggestWeeklyPlanFeature(IWeeklyPlanGenerationService generationService)
    {
        _generationService = generationService;
    }

    public async Task<SuggestedWeeklyPlanResult> Execute(
        SuggestWeeklyPlanRequest request,
        CancellationToken ct = default)
    {
        // Create generation request
        var generationRequest = new GenerateWeeklyPlanRequest(
            WeekStartDate: request.WeekStartDate,
            Rules: request.Rules);

        try
        {
            // Generate the plan (without persisting)
            var result = await _generationService.Generate(generationRequest, ct);

            // Return with no violations (generation succeeded)
            return new SuggestedWeeklyPlanResult(
                Plan: result.Plan,
                ExplanationsByDay: result.ExplanationsByDay,
                Violations: Array.Empty<RuleViolation>());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to generate valid plan"))
        {
            // If generation fails due to validation, we could return violations
            // For now, re-throw since generation service throws on violations
            throw;
        }
    }
}
