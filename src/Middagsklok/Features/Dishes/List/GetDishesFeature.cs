using Middagsklok.Domain;

namespace Middagsklok.Features.Dishes.List;

public class GetDishesFeature
{
    private readonly IDishRepository _dishRepository;

    public GetDishesFeature(IDishRepository dishRepository)
    {
        _dishRepository = dishRepository;
    }

    public Task<IReadOnlyList<Dish>> Execute(CancellationToken ct = default)
    {
        return _dishRepository.GetAllWithIngredients(ct);
    }
}
