using Middagsklok.Domain;

namespace Middagsklok.Features.Dishes.GetDishDetails;

public class GetDishDetailsFeature
{
    private readonly IDishDetailsRepository _repository;

    public GetDishDetailsFeature(IDishDetailsRepository repository)
    {
        _repository = repository;
    }

    public Task<Dish?> Execute(GetDishDetailsQuery query, CancellationToken ct = default)
    {
        return _repository.GetByIdWithIngredients(query.DishId, ct);
    }
}
