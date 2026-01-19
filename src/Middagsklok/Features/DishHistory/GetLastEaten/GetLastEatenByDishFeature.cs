namespace Middagsklok.Features.DishHistory.GetLastEaten;

public class GetLastEatenByDishFeature
{
    private readonly IDishHistoryRepository _historyRepository;

    public GetLastEatenByDishFeature(IDishHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task<Dictionary<Guid, DateOnly>> Execute(CancellationToken ct = default)
    {
        return _historyRepository.GetLastEatenByDish(ct);
    }
}
