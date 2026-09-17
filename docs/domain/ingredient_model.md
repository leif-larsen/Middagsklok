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
| `PACK` | pk |

`PACK` is a fraction of one retail package, for amounts that are naturally "most of a bag".

---

## Oda product mapping

An ingredient may carry at most one `OdaProductMapping`, stored in `ingredient_oda_products` and
loaded with the ingredient. It answers the question "what do I put in the Oda cart for this?".

| Field | Type | Notes |
|---|---|---|
| `availability` | `Available` \| `NotAvailable` | `NotAvailable` records that Oda does not stock the ingredient (takeaway, caravan-trip food), so nobody searches for it again. |
| `odaProductId` | int, nullable | Oda product id. Null when `NotAvailable`. |
| `odaProductName` | string, nullable | Snapshot of the product name at mapping time, for display and drift detection. |
| `packQuantity` + `packUnit` | double + `Unit`, nullable | Size of one retail package, e.g. `400 G`. |
| `confirmedAt` | datetime, nullable | Null means auto-suggested; set once a person confirms the product. |

Three states follow from this: **unmapped** (no row), **mapped** (row with a product) and
**not available** (row without a product). The shopping list exposes them as `odaStatus`.

### Package count rule

`SuggestedPackCount(amount, unit)` returns `ceil(amount / packQuantity)` when the units share a
dimension. `G` and `Kg` convert, as do `Ml` and `L`. A `Pack` amount already counts packages and
ignores the pack size. `Pcs` against a weight or volume pack does not convert; each piece counts
as one package. Any other combination returns `null`, and the caller has to ask.

### Endpoints

| Method | Route | Body |
|---|---|---|
| `GET` | `/ingredients/oda-mappings` | Returns `mappings` and `unmapped` (ordered by how many dishes use the ingredient). |
| `PUT` | `/ingredients/{id}/oda-mapping` | `{ "productId", "productName", "packQuantity", "packUnit", "confirmed" }` or `{ "notAvailable": true }`. Replaces any existing mapping. |
| `DELETE` | `/ingredients/{id}/oda-mapping` | Clears the mapping. |

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
