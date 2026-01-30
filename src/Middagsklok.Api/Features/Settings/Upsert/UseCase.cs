using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Settings;

namespace Middagsklok.Api.Features.Settings.Upsert;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the planning settings upsert workflow.
    public async Task<UseCaseResult> Execute(Request request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(UpsertOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var settings = await _dbContext.PlanningSettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new PlanningSettings(validation.Candidate.WeekStartsOn);
            _dbContext.PlanningSettings.Add(settings);
        }
        else
        {
            settings.Update(validation.Candidate.WeekStartsOn);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapSettings(settings);
        var result = new UseCaseResult(UpsertOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Maps the planning settings entity to the response.
    private static Response MapSettings(PlanningSettings settings) =>
        new(settings.Id.ToString("D"), settings.WeekStartsOn.ToString());
}

internal enum UpsertOutcome
{
    Success,
    Invalid
}

internal sealed record UseCaseResult(
    UpsertOutcome Outcome,
    Response? Settings,
    IReadOnlyList<ValidationError> Errors);
