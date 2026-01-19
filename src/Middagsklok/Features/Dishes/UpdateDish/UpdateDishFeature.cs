namespace Middagsklok.Features.Dishes.UpdateDish;

public class UpdateDishFeature
{
    private readonly IDishUpdateRepository _repository;

    public UpdateDishFeature(IDishUpdateRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateDishResult?> Execute(UpdateDishCommand command, CancellationToken ct = default)
    {
        // Validate command
        var errors = ValidateCommand(command);
        if (errors.Any())
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", errors)}");
        }

        // Check if dish exists
        var exists = await _repository.DishExists(command.DishId, ct);
        if (!exists)
        {
            return null; // 404
        }

        // Perform update
        await _repository.UpdateDish(command, ct);

        return new UpdateDishResult(
            Id: command.DishId,
            UpdatedAt: DateTime.UtcNow,
            Warnings: []
        );
    }

    private static List<string> ValidateCommand(UpdateDishCommand command)
    {
        var errors = new List<string>();

        // Name validation
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required");
        }

        // Time validations
        if (command.ActiveMinutes < 0)
        {
            errors.Add("ActiveMinutes must be >= 0");
        }

        if (command.TotalMinutes <= 0)
        {
            errors.Add("TotalMinutes must be > 0");
        }

        if (command.ActiveMinutes > command.TotalMinutes)
        {
            errors.Add("ActiveMinutes must be <= TotalMinutes");
        }

        // Rating validations
        if (command.KidRating < 1 || command.KidRating > 5)
        {
            errors.Add("KidRating must be between 1 and 5");
        }

        if (command.FamilyRating < 1 || command.FamilyRating > 5)
        {
            errors.Add("FamilyRating must be between 1 and 5");
        }

        // Ingredient validations
        if (command.Ingredients.Count == 0)
        {
            errors.Add("At least 1 ingredient is required");
        }

        for (int i = 0; i < command.Ingredients.Count; i++)
        {
            var ingredient = command.Ingredients[i];

            if (string.IsNullOrWhiteSpace(ingredient.Name))
            {
                errors.Add($"Ingredient {i + 1}: Name is required");
            }

            if (ingredient.Amount <= 0)
            {
                errors.Add($"Ingredient {i + 1}: Amount must be > 0");
            }

            if (string.IsNullOrWhiteSpace(ingredient.Unit))
            {
                errors.Add($"Ingredient {i + 1}: Unit is required");
            }
        }

        return errors;
    }
}
