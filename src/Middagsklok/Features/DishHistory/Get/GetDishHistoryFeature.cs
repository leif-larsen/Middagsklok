using Middagsklok.Domain;

namespace Middagsklok.Features.DishHistory.Get;

public class GetDishHistoryFeature
{
    private readonly IDishRepository _dishRepository;
    private readonly IDishHistoryRepository _historyRepository;

    public GetDishHistoryFeature(
        IDishRepository dishRepository,
        IDishHistoryRepository historyRepository)
    {
        _dishRepository = dishRepository;
        _historyRepository = historyRepository;
    }

    public async Task<IReadOnlyList<DishHistoryEntry>> Execute(Guid dishId, CancellationToken ct = default)
    {
        // Validate dish exists
        var dish = await _dishRepository.GetByIdWithIngredients(dishId, ct)
            ?? throw new ArgumentException($"Dish with id {dishId} not found.", nameof(dishId));

        return await _historyRepository.GetForDish(dishId, ct);
    }
}
