using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.WeeklyPlans.Available;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the query for available weekly plans.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var plans = await _dbContext.WeeklyPlans
            .AsNoTracking()
            .OrderBy(plan => plan.StartDate)
            .ToListAsync(cancellationToken);

        if (plans.Count == 0)
        {
            var emptyResponse = new Response(Array.Empty<WeeklyPlanSummary>());
            return emptyResponse;
        }

        var summaries = plans
            .Select(plan => new WeeklyPlanSummary(
                FormatDate(plan.StartDate),
                FormatDate(plan.EndDate)))
            .ToArray();

        var response = new Response(summaries);

        return response;
    }

    // Formats date values for the API response.
    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
