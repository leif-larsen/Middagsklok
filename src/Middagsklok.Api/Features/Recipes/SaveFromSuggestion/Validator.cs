namespace Middagsklok.Api.Features.Recipes.SaveFromSuggestion;

internal sealed class Validator
{
    // Validates the save from suggestion request.
    public ValidationResult Validate(Request? request)
    {
        var errors = new List<ValidationError>();

        if (request is null)
        {
            errors.Add(new ValidationError("request", "Request body is required."));

            return ValidationResult.Failed(errors);
        }

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ValidationError("title", "Title is required."));
        }
        else if (title.Length > 200)
        {
            errors.Add(new ValidationError("title", "Title must not exceed 200 characters."));
        }

        var summary = request.Summary?.Trim();
        if (string.IsNullOrWhiteSpace(summary))
        {
            errors.Add(new ValidationError("summary", "Summary is required."));
        }
        else if (summary.Length > 2000)
        {
            errors.Add(new ValidationError("summary", "Summary must not exceed 2000 characters."));
        }

        if (errors.Count > 0)
        {
            return ValidationResult.Failed(errors);
        }

        var candidate = new ValidatedCandidate(title!, summary!, request.EstimatedTotalMinutes);

        return ValidationResult.Succeeded(candidate);
    }
}

internal sealed record ValidatedCandidate(
    string Title,
    string Summary,
    int? EstimatedTotalMinutes);

internal sealed record ValidationResult(
    bool IsValid,
    ValidatedCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors)
{
    // Creates a successful validation result.
    public static ValidationResult Succeeded(ValidatedCandidate candidate)
    {
        var result = new ValidationResult(true, candidate, Array.Empty<ValidationError>());

        return result;
    }

    // Creates a failed validation result.
    public static ValidationResult Failed(IReadOnlyList<ValidationError> errors)
    {
        var result = new ValidationResult(false, null, errors);

        return result;
    }
}
