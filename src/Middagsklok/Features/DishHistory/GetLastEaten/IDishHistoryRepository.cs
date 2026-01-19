namespace Middagsklok.Features.DishHistory.GetLastEaten;

public interface IDishHistoryRepository
{
    Task<Dictionary<Guid, DateOnly>> GetLastEatenByDish(CancellationToken ct = default);
}
