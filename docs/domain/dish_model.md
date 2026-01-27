# Domain Model: `Dish`

## Purpose
`Dish` represents a cookable dish/recipe used in the meal planner.  
A dish owns its ingredient lines and references shared `Ingredient` entities so ingredients remain consistent across dishes, shopping lists, and inventory.

---

## Entity
**Dish** (Entity)  
- Identity: `dishId` (stable, unique identifier)

---

## Attributes

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `dishId` | UUID / string | ✅ | `"c13a7f..."` | Technical ID. |
| `name` | string | ✅ | `"Spaghetti Carbonara"` | Primary display name. |
| `cuisine` | `CuisineType` (enum) | ✅ | `ITALIAN` | Shown as a tag/pill. |
| `prepTimeMinutes` | integer | ✅ | `10` | `>= 0` |
| `cookTimeMinutes` | integer | ✅ | `20` | `>= 0` |
| `servings` | integer | ✅ | `4` | `> 0` |
| `ingredients` | `DishIngredient[]` | ✅ | see below | The full ingredient list for the dish. |
| `createdAt` | datetime | (recommended) | `"2026-01-27T12:10:00Z"` | Audit field. |
| `updatedAt` | datetime | (recommended) | `"2026-01-27T12:45:00Z"` | Audit field. |

---

## Connection to `Ingredient`

### `DishIngredient` (line item / value object)
Represents one ingredient line inside a dish. This is the real link between `Dish` and `Ingredient`.

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `ingredientId` | UUID / string | ✅ | `"9f0c2d..."` | FK/reference to `Ingredient`. |
| `quantity` | number | ✅ | `400` | Must be `> 0` for a “real” line item. |
| `unit` | `Unit` (enum) | ✅ | `G` | Can default to `Ingredient.defaultUnit` when adding. |
| `note` | string | ❌ | `"finely chopped"` | Optional free-text details. |
| `sortOrder` | integer | (optional) | `1` | Stable ordering in UI. |

> Why this is necessary:  
> `Ingredient` should NOT store dish-specific quantities/units. Those belong to the dish line item.

---

## Enums / Value Sets

### `CuisineType`
| Enum name | Display name |
|---|---|
| `ITALIAN` | Italian |
| `ASIAN` | Asian |
| `MEDITERRANEAN` | Mediterranean |
| `MEXICAN` | Mexican |
| `INDIAN` | Indian |
| `AMERICAN` | American |
| `FRENCH` | French |
| `MIDDLE_EASTERN` | Middle Eastern |
| `JAPANESE` | Japanese |
| `THAI` | Thai |
| `CHINESE` | Chinese |
| `VEGETARIAN` | Vegetarian |
| `VEGAN` | Vegan |
| `OTHER` | Other |

### `Unit`
Reuse the same `Unit` enum used by `Ingredient` (e.g. `G`, `PCS`, etc.).

---

## Invariants (Domain Rules)

- `name` must be non-empty and trimmed.
- `prepTimeMinutes >= 0`
- `cookTimeMinutes >= 0`
- `servings > 0`
- `ingredients` must contain at least one item.
- Each `DishIngredient.quantity > 0`
- `DishIngredient.ingredientId` must reference an existing `Ingredient`.
- Optional but recommended: prevent duplicate `ingredientId` within the same dish (merge quantities instead).

---

## Derived / Computed Values

- `totalTimeMinutes = prepTimeMinutes + cookTimeMinutes`
- `ingredientPreview = first N items of ingredients (sorted by sortOrder)`
  - For display: join `quantity + unit + ingredient.name`

---

## Example (JSON)

```json
{
  "dishId": "c13a7f5b-9a2e-4f6a-9b21-1e73a8d4fabc",
  "name": "Spaghetti Carbonara",
  "cuisine": "ITALIAN",
  "prepTimeMinutes": 10,
  "cookTimeMinutes": 20,
  "servings": 4,
  "ingredients": [
    { "ingredientId": "11111111-1111-1111-1111-111111111111", "quantity": 400, "unit": "G", "sortOrder": 1 },
    { "ingredientId": "22222222-2222-2222-2222-222222222222", "quantity": 4, "unit": "PCS", "sortOrder": 2 },
    { "ingredientId": "33333333-3333-3333-3333-333333333333", "quantity": 200, "unit": "G", "sortOrder": 3 }
  ],
  "createdAt": "2026-01-27T12:10:00Z",
  "updatedAt": "2026-01-27T12:45:00Z"
}
```
