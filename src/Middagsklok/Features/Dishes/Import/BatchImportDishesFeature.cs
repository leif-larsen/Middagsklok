namespace Middagsklok.Features.Dishes.Import;

public class BatchImportDishesFeature
{
    private readonly IDishImportRepository _importRepository;

    public BatchImportDishesFeature(IDishImportRepository importRepository)
    {
        _importRepository = importRepository;
    }

    public async Task<BatchImportResult> Execute(BatchImportDishesCommand command, CancellationToken ct = default)
    {
        if (command.Dishes.Count == 0)
            throw new ArgumentException("Command must contain at least one dish.", nameof(command));

        var results = new List<BatchImportDishResult>();
        var created = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var dish in command.Dishes)
        {
            try
            {
                // Validate dish
                ValidateDish(dish);

                // Check if dish already exists (case-insensitive)
                var existingId = await _importRepository.FindDishIdByName(dish.Name, ct);
                
                if (existingId.HasValue)
                {
                    results.Add(new BatchImportDishResult(
                        Name: dish.Name,
                        Status: "skipped",
                        DishId: existingId.Value,
                        Error: null));
                    skipped++;
                    continue;
                }

                // Create new dish
                var dishId = await _importRepository.InsertDish(dish, ct);
                
                results.Add(new BatchImportDishResult(
                    Name: dish.Name,
                    Status: "created",
                    DishId: dishId,
                    Error: null));
                created++;
            }
            catch (Exception ex)
            {
                results.Add(new BatchImportDishResult(
                    Name: dish.Name,
                    Status: "failed",
                    DishId: null,
                    Error: ex.Message));
                failed++;
            }
        }

        return new BatchImportResult(
            Total: command.Dishes.Count,
            Created: created,
            Skipped: skipped,
            Failed: failed,
            Results: results);
    }

    private static void ValidateDish(AddDishCommand dish)
    {
        if (string.IsNullOrWhiteSpace(dish.Name))
            throw new ArgumentException("Dish name is required.");

        if (dish.ActiveMinutes < 0)
            throw new ArgumentException("Active minutes cannot be negative.");

        if (dish.TotalMinutes < 0)
            throw new ArgumentException("Total minutes cannot be negative.");

        if (dish.TotalMinutes < dish.ActiveMinutes)
            throw new ArgumentException("Total minutes cannot be less than active minutes.");

        if (dish.KidRating < 1 || dish.KidRating > 5)
            throw new ArgumentException("Kid rating must be between 1 and 5.");

        if (dish.FamilyRating < 1 || dish.FamilyRating > 5)
            throw new ArgumentException("Family rating must be between 1 and 5.");

        if (dish.Ingredients.Count == 0)
            throw new ArgumentException("Dish must have at least one ingredient.");

        foreach (var ingredient in dish.Ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient.Name))
                throw new ArgumentException("Ingredient name is required.");

            if (string.IsNullOrWhiteSpace(ingredient.Category))
                throw new ArgumentException("Ingredient category is required.");

            if (string.IsNullOrWhiteSpace(ingredient.Unit))
                throw new ArgumentException("Ingredient unit is required.");

            if (ingredient.Amount <= 0)
                throw new ArgumentException("Ingredient amount must be greater than zero.");
        }
    }
}
