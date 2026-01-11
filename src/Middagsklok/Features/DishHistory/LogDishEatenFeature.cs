using Middagsklok.Database.Repositories;
using Middagsklok.Domain;

namespace Middagsklok.Features.DishHistory;

public record LogDishEatenRequest(
    Guid DishId,
    DateOnly Date,
    int? RatingOverride = null,
    string? Notes = null);

public class LogDishEatenFeature
{
    private readonly IDishRepository _dishRepository;
    private readonly IDishHistoryRepository _historyRepository;
    private readonly IClock _clock;

    public LogDishEatenFeature(
        IDishRepository dishRepository,
        IDishHistoryRepository historyRepository,
        IClock clock)
    {
        _dishRepository = dishRepository;
        _historyRepository = historyRepository;
        _clock = clock;
    }

    public async Task<DishHistoryEntry> Execute(LogDishEatenRequest request, CancellationToken ct = default)
    {
        // Validate dish exists
        var dish = await _dishRepository.GetByIdWithIngredients(request.DishId, ct)
            ?? throw new ArgumentException($"Dish with id {request.DishId} not found.", nameof(request));

        // Validate date is not in the future
        if (request.Date > _clock.Today)
            throw new ArgumentException("Date cannot be in the future.", nameof(request));

        // Validate rating if provided
        if (request.RatingOverride.HasValue && (request.RatingOverride.Value < 1 || request.RatingOverride.Value > 5))
            throw new ArgumentException("Rating override must be between 1 and 5.", nameof(request));

        // Create history entry
        var entry = new DishHistoryEntry(
            Id: Guid.NewGuid(),
            DishId: request.DishId,
            Date: request.Date,
            RatingOverride: request.RatingOverride,
            Notes: request.Notes);

        await _historyRepository.Add(entry, ct);

        return entry;
    }
}
