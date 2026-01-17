# Batch Dish Import Feature

This feature allows you to import multiple dishes from a JSON file in a single operation.

## Usage

```bash
dotnet run --project ./src/Middagsklok.App/Middagsklok.App.csproj dish import <path-to-json>
```

Example:
```bash
dotnet run --project ./src/Middagsklok.App/Middagsklok.App.csproj dish import sample-dishes.json
```

## JSON Format

The JSON file must contain a `dishes` array with dish objects:

```json
{
  "dishes": [
    {
      "name": "Laks med ovnsgrønnsaker",
      "activeMinutes": 15,
      "totalMinutes": 35,
      "kidRating": 4,
      "familyRating": 5,
      "isPescetarian": true,
      "hasOptionalMeatVariant": false,
      "tags": ["fish", "quick"],
      "ingredients": [
        {
          "name": "laks",
          "category": "fish",
          "amount": 600,
          "unit": "g",
          "optional": false
        },
        {
          "name": "potet",
          "category": "produce",
          "amount": 800,
          "unit": "g",
          "optional": false
        }
      ]
    }
  ]
}
```

### Field Descriptions

**Dish Fields:**
- `name` (string): Name of the dish (required, must be unique case-insensitive)
- `activeMinutes` (int): Active cooking time in minutes (required, >= 0)
- `totalMinutes` (int): Total cooking time in minutes (required, >= activeMinutes)
- `kidRating` (int): Child rating 1-5 (required)
- `familyRating` (int): Family rating 1-5 (required)
- `isPescetarian` (bool): Whether the dish is pescetarian (required)
- `hasOptionalMeatVariant` (bool): Whether meat can be added optionally (required)
- `tags` (array): Optional array of tags (can be null)
- `ingredients` (array): List of ingredients (required, at least one)

**Ingredient Fields:**
- `name` (string): Ingredient name (required)
- `category` (string): Category (e.g., "fish", "produce", "dairy", "dry")
- `amount` (number): Quantity (required, > 0)
- `unit` (string): Unit of measurement (e.g., "g", "ml", "stk", "ss")
- `optional` (bool): Whether ingredient is optional (required)

## Features

### Duplicate Detection
- Dishes with the same name (case-insensitive) are automatically skipped
- The existing dish ID is returned in the result
- No data is modified for skipped dishes

### Ingredient Upsert
- Ingredients are matched by normalized name (case-insensitive)
- Shared ingredients across dishes reuse the same ingredient entity
- New ingredients are created automatically

### Duplicate Aggregation
- Duplicate ingredient lines within a dish are aggregated before saving
- Ingredients are grouped by: normalized name + unit + optional flag
- Amounts are summed for matching groups

### Error Handling
- Each dish is validated before import
- Invalid dishes fail individually without affecting others
- Failed dishes include error messages in the result
- Each dish is inserted in its own transaction

## Output

The command provides a summary and per-dish results:

```
=== Import Summary ===
Total: 5
Created: 3
Skipped: 1
Failed: 1

=== Per-Dish Results ===
✓ Laks med ovnsgrønnsaker [created] (ID: f1795924-9839-47b1-89f5-fcff38921db9)
→ Pasta Carbonara [skipped] (ID: 53ef73d2-082e-47d7-9620-077f67142750)
✓ Veggie taco [created] (ID: ded017d3-94ca-49b7-8905-795909ad3387)
✗ Invalid Dish [failed] - Kid rating must be between 1 and 5.
✓ Fiskekaker med potetmos [created] (ID: 915c72b1-832b-421a-8558-cf70e165b2fb)
```

## Validation Rules

The following validations are applied to each dish:

- Name must not be empty
- Active minutes >= 0
- Total minutes >= active minutes
- Kid rating must be between 1 and 5
- Family rating must be between 1 and 5
- Must have at least one ingredient
- Each ingredient must have:
  - Non-empty name, category, and unit
  - Amount > 0

## Implementation Details

### Architecture

The feature follows the clean architecture pattern:

- **DTOs**: `BatchImportDishesCommand`, `BatchImportResult`, `BatchImportDishResult` in `Features/BatchImportDishes/`
- **Handler**: `BatchImportDishesFeature` with validation logic
- **Repository**: `IDishImportRepository` with `FindDishIdByName` and `InsertDish` methods
- **Database**: EF Core implementation with transactions

### Transaction Handling

- Each dish is inserted in its own transaction
- If a dish fails, its transaction is rolled back
- Other dishes continue processing independently
- This ensures partial imports succeed and provide full visibility into results

## Testing

Comprehensive integration tests cover:
1. Importing multiple dishes successfully
2. Skipping existing dishes (case-insensitive)
3. Continuing on error (other dishes still created)
4. Ingredient upsert (shared ingredients not duplicated)
5. Duplicate ingredient aggregation within a dish
6. Validation of all input fields

Run tests:
```bash
dotnet test tests/Middagsklok.Tests.Integration/Middagsklok.Tests.Integration.csproj
```
