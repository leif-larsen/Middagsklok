using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the set-or-replace workflow for an ingredient's Oda mapping.
    public async Task<UseCaseResult> Execute(string id, Request request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(id, request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(UpsertOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(i => i.Id == validation.Candidate.IngredientId, cancellationToken);

        if (ingredient is null)
        {
            var notFoundError = new ValidationError("id", "Ingredient not found.");
            var notFoundResult = new UseCaseResult(UpsertOutcome.NotFound, null, new[] { notFoundError });
            return notFoundResult;
        }

        var mapping = validation.Candidate.IsNotAvailable
            ? OdaProductMapping.NotAvailable()
            : OdaProductMapping.ForProduct(
                validation.Candidate.ProductId,
                validation.Candidate.ProductName,
                validation.Candidate.PackQuantity,
                validation.Candidate.PackUnit,
                validation.Candidate.Confirmed);

        ingredient.SetOdaMapping(mapping);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapResponse(ingredient, mapping);
        var result = new UseCaseResult(UpsertOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Maps the stored mapping to the response shape.
    private static Response MapResponse(Ingredient ingredient, OdaProductMapping mapping) =>
        new(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            mapping.Availability.ToString(),
            mapping.OdaProductId,
            mapping.OdaProductName,
            mapping.PackQuantity,
            mapping.PackUnit?.ToString(),
            mapping.IsConfirmed);
}

internal enum UpsertOutcome
{
    Success,
    Invalid,
    NotFound
}

internal sealed record UseCaseResult(
    UpsertOutcome Outcome,
    Response? Mapping,
    IReadOnlyList<ValidationError> Errors);
