using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Settings;

namespace Middagsklok.Api.Features.Settings.Get;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the planning settings retrieval workflow.
    public async Task<UseCaseResult> Execute(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlanningSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            var notFoundResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                Array.Empty<ValidationError>());
            return notFoundResult;
        }

        var response = MapSettings(settings);
        var result = new UseCaseResult(FetchOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Maps the planning settings entity to the response.
    private static Response MapSettings(PlanningSettings settings) =>
        new(settings.Id.ToString("D"), settings.WeekStartsOn.ToString());
}

internal enum FetchOutcome
{
    Success,
    NotFound
}

internal sealed record UseCaseResult(
    FetchOutcome Outcome,
    Response? Settings,
    IReadOnlyList<ValidationError> Errors);
