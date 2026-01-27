# Domain Model: `Ingredient`

## Purpose
`Ingredient` represents a named food item that can be reused across recipes and shopping lists.  
Each ingredient has a default category and a default unit used as a sensible baseline when entering quantities.

---

## Entity
**Ingredient** (Entity)  
- Identity: `ingredientId` (stable, unique identifier)

---

## Attributes

| Field | Type | Required | Example | Notes |
|---|---|---:|---|---|
| `ingredientId` | UUID / string | ✅ | `"9f0c2d..."` | Technical ID. Not editable in UI. |
| `name` | string | ✅ | `"Spaghetti"` | Shown as “Ingredient Name”. |
| `category` | `IngredientCategory` (enum) | ✅ | `PASTA_AND_GRAINS` | Shown as “Category”. |
| `defaultUnit` | `Unit` (enum) | ✅ | `G` | Shown as “Default Unit”, e.g. `g`, `pcs`. |
| `createdAt` | datetime | (recommended) | `"2026-01-27T12:34:56Z"` | Audit field. |
| `updatedAt` | datetime | (recommended) | `"2026-01-27T12:40:00Z"` | Audit field. |

> `createdAt` and `updatedAt` are not visible, but are typically part of a persisted domain entity.

---

## Enums / Value Sets

### `IngredientCategory`

Below is an expanded, realistic set of categories.  
Each entry includes an **enum name** (used in code) and a **display name** (used in UI).

| Enum name | Display name |
|---|---|
| `PRODUCE` | Produce |
| `MEAT` | Meat |
| `POULTRY` | Poultry |
| `SEAFOOD` | Seafood |
| `DAIRY_AND_EGGS` | Dairy & Eggs |
| `PASTA_AND_GRAINS` | Pasta & Grains |
| `BAKERY` | Bakery |
| `CANNED_GOODS` | Canned Goods |
| `FROZEN_FOODS` | Frozen Foods |
| `CONDIMENTS` | Condiments |
| `SPICES_AND_HERBS` | Spices & Herbs |
| `BAKING` | Baking |
| `OILS_AND_VINEGARS` | Oils & Vinegars |
| `BEVERAGES` | Beverages |
| `SNACKS` | Snacks |
| `OTHER` | Other |

> Rationale:  
> - `POULTRY` and `SEAFOOD` are commonly split out from `MEAT` in grocery contexts.  
> - `BAKERY`, `FROZEN_FOODS`, and `OILS_AND_VINEGARS` reduce the overuse of `OTHER`.  
> - This list is still compact enough to stay usable in a dropdown.

---

### `Unit`

Minimum set inferred from the UI:

| Enum name | Display |
|---|---|
| `G` | g |
| `PCS` | pcs |
| `ML`| ml |
| `L` | l |
| `KG` | kg |

*(Can naturally be extended if the domain grows.)*

---

## Invariants (Domain Rules)

- `name` must be non-empty and trimmed.
- `name` should be unique within its ownership scope (user / household / tenant), otherwise duplicates like “Salt” vs “Salt” will degrade UX.
- `category` must be a valid `IngredientCategory`.
- `defaultUnit` must be a valid `Unit`.
- `defaultUnit` should be reasonable for the ingredient type:
  - countable items → `PCS`
  - dry goods → typically `G`
  - liquids → typically `ML`  
  *(This is best enforced as guidance or validation warnings, not hard rules.)*

---

## Example (JSON)

```json
{
  "ingredientId": "9f0c2d2b-9a8c-4c5f-9c42-7d2a8a7d12ab",
  "name": "Spaghetti",
  "category": "PASTA_AND_GRAINS",
  "defaultUnit": "G",
  "createdAt": "2026-01-27T12:34:56Z",
  "updatedAt": "2026-01-27T12:40:00Z"
}
```
