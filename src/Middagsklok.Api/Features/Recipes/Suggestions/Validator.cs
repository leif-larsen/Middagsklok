namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class Validator
{
    private const int DefaultMaxSuggestions = 5;
    private const int MinimumMaxSuggestions = 1;
    private const int MaximumMaxSuggestions = 10;
    private const int MaximumPromptLength = 2000;

    // Validates and normalizes recipe suggestion input.
    public ValidationResult Validate(Request? request)
    {
        var failures = new List<ValidationError>();

        if (request is null)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Prompt)), "Prompt is required."));
            return new ValidationResult(false, null, failures);
        }

        var prompt = request.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Prompt)), "Prompt is required."));
        }
        else if (prompt.Length > MaximumPromptLength)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.Prompt)),
                $"Prompt must be <= {MaximumPromptLength} characters."));
        }

        var maxSuggestions = request.MaxSuggestions ?? DefaultMaxSuggestions;
        if (maxSuggestions < MinimumMaxSuggestions || maxSuggestions > MaximumMaxSuggestions)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.MaxSuggestions)),
                $"Max suggestions must be between {MinimumMaxSuggestions} and {MaximumMaxSuggestions}."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = new Candidate(prompt!, maxSuggestions);

        return new ValidationResult(true, candidate, Array.Empty<ValidationError>());
    }

    // Converts property names to camelCase field names.
    private static string ToFieldName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        if (propertyName.Length == 1)
        {
            return propertyName.ToLowerInvariant();
        }

        var first = char.ToLowerInvariant(propertyName[0]);

        return $"{first}{propertyName[1..]}";
    }
}

internal sealed record ValidationResult(
    bool IsValid,
    Candidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record Candidate(
    string Prompt,
    int MaxSuggestions);
