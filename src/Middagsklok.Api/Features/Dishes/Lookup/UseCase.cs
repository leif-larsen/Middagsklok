using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Dishes.Lookup;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the lookup query for dishes.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .OrderBy(dish => dish.Name)
            .ToListAsync(cancellationToken);

        if (dishes.Count == 0)
        {
            var emptyResponse = new Response(Array.Empty<DishLookup>());
            return emptyResponse;
        }

        var lookup = dishes
            .Select(dish => new DishLookup(
                dish.Id.ToString("D"),
                dish.Name,
                dish.DishType.ToString(),
                dish.VibeTags.ToArray()))
            .ToArray();

        var response = new Response(lookup);

        return response;
    }
}
