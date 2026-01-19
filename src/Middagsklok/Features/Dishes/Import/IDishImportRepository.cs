namespace Middagsklok.Features.Dishes.Import;

public interface IDishImportRepository
{
    Task<Guid?> FindDishIdByName(string name, CancellationToken ct = default);
    Task<Guid> InsertDish(AddDishCommand cmd, CancellationToken ct = default);
}
