namespace Middagsklok.Features.WeeklyPlans.Generate;

/// <summary>
/// Feature that generates and persists a weekly meal plan.
/// </summary>
public class GenerateWeeklyPlanFeature
{
    private readonly IWeeklyPlanGenerationService _generationService;
    private readonly IWeeklyPlanRepository _planRepository;

    public GenerateWeeklyPlanFeature(
        IWeeklyPlanGenerationService generationService,
        IWeeklyPlanRepository planRepository)
    {
        _generationService = generationService;
        _planRepository = planRepository;
    }

    public async Task<GeneratedWeeklyPlanResult> Execute(
        GenerateWeeklyPlanRequest request,
        CancellationToken ct = default)
    {
        // Generate the plan using the service (no persistence)
        var result = await _generationService.Generate(request, ct);

        // Persist the plan
        var savedPlan = await _planRepository.CreateOrReplace(result.Plan, ct);

        return new GeneratedWeeklyPlanResult(savedPlan, result.ExplanationsByDay);
    }
}
