namespace Middagsklok.Features.Dishes.UpdateDish;

public interface IDishUpdateRepository
{
    Task<bool> DishExists(Guid dishId, CancellationToken ct = default);
    Task UpdateDish(UpdateDishCommand command, CancellationToken ct = default);
}
