# Oda integration plan

Goal: let Claude plan the week's dinners in Middagsklok and fill the resulting shopping list into
an Oda cart, from Claude Code on the Mac, with quantities that reflect what is actually cooked.

## Decisions taken

| Question | Decision |
|---|---|
| Trigger surface | Claude Code on the Mac only. Local stdio MCP over Tailscale. Nothing exposed publicly. |
| Plan authorship | Full read/write. Claude may generate, adjust and commit weekly plans. |
| Cart autonomy | Claude resolves the whole cart, prints line items and total, adds only on explicit OK. |
| Portion scaling | Quantities stored **per serving**. Guests handled by a servings override on the planned day. |
| Amount data entry | Claude-led interview, 6 to 8 dishes per batch, written straight to the API. |
| Pack-waste optimization | No build. Claude reasons about it at planning time. |

Explicitly out of scope: remote/HTTP MCP, public exposure, API authentication, checkout,
pack optimization inside the generator. Checkout and payment always happen in the Oda shop.

## Architecture

Claude is the bridge. Middagsklok never talks to Oda, and Oda never talks to Middagsklok.

```
Claude Code (Mac)
  |
  |-- Middagsklok MCP (stdio, local process)
  |        `-- HTTP over Tailscale --> praxis-server:5116 (Middagsklok API)
  |
  `-- Oda MCP (remote, already connected)
```

Consequences worth stating:

- No new network exposure. The API stays reachable only on the tailnet.
- No API authentication work. Tailscale remains the perimeter, unchanged from today.
- The MCP server is a thin HTTP client. It holds no state and no database connection.

## Phase 0: validation (done, 2026-08-08)

Ran the full loop by hand against week 2026-08-07 (12 distinct shopping-list items).

| Outcome | Count | Examples |
|---|---|---|
| Clean match | 6 | Pizzamel, Fetaost, Agurk, Mais, Wokgrønnsaker, Sweet&Sour |
| Plausible guess | 3 | Pasta (which shape?), Paprika (pack is 2 stk, need 1), Tørrgjær |
| Blocked | 3 | Kyllingfilet, Nudler, Tortelloni |

Roughly 254 kr resolvable, three unknowns.

**Two orthogonal failure modes, both need fixing:**

- **Quantity failure.** `1 Pcs Kyllingfilet` could mean one fillet (~150 g) or one 400 g pack.
  Oda offers 400 g at 90,90, 950 g at 170,00 and 1,4 kg at 189,00. Fixed by real amounts.
- **Search failure.** Tortelloni returns generic dry pasta and never resolves. Search will not
  fix this; only a saved product id will. Fixed by the mapping table.

Ingredients like Agurk and Fetaost survive `1 Pcs` only because one piece genuinely equals one
product. That is luck, not a working model.

## Data state as of 2026-08-08

- 43 dishes, 224 dish-ingredient rows, 141 ingredients.
- **153 of 224 rows (68%) are the placeholder `1 pcs`.**
- 10 dishes have no instructions, and they are heavily-cooked ones: Taco, Napolitansk pizza,
  Pølse i brød, Toast, Sushi, Tortelloini, Pytt i panne, Quesedilas, torsk m/pastasalt.
- 32 dishes have been eaten at least once. 11 have never been eaten.

Six dishes already carry clean amounts and are the model to copy: Myk laksepasta med fløtesaus,
Blomkålsuppe, Lapskaus med grønnsaker, Vegetar Moussaka, Omelett, Ovnsbakt laks.

**Some dishes are missing ingredients entirely, not just amounts.** Tortelloini has one
ingredient. Sushi has one. Napolitansk pizza and Pytt i panne have two. These need their
ingredient lists completed before amounts mean anything.

## Phase 1: dish data model (done, PR #67)

Household size is **3**, not 4. Every dish currently claims `serves: 4`, so that field is
unreliable and no longer drives the shopping list. Scaling uses the planned day's servings,
falling back to `PlanningSettings.HouseholdSize`.

`Unit.Pack` was added after the Taco pilot: several amounts are naturally expressed as a
fraction of a retail package ("0,75 pk revet ost"), and converting those to grams would have
required knowing the Oda product first, making phase 2 depend on phase 3. Pack fractions
aggregate and round to whole packages once, at the end.

Still open before the interview can happen in the UI: the dish edit page cannot yet set
scaling, per-ingredient unit or servings. The API preserves omitted fields on update, so the
current frontend cannot destroy entered data, but entry itself has to go through the API.



Three scaling behaviours on `DishIngredient` cover every case:

```csharp
public enum IngredientScaling
{
    PerServing,   // total = Quantity * servings    -> kjøttdeig 50 g/porsjon
    PerDish,      // total = Quantity, never scales -> 1 boks hakkede tomater, krydder
    PerPerson     // total = Quantity * PersonCount -> kylling 150 g, only Leif eats it
}
```

`PerServing` stores **per serving**, not per dish. Taco kjøttdeig becomes 50 g/porsjon: a normal
week at 4 servings gives 200 g, a guest dinner at 10 servings gives 500 g. Leif's own figures
(200 g for the family, 600 g with guests) work out to about 50 and 60 g per head, so linear
scaling tracks reality closely enough. This beats a binary guest toggle because two guests and
eight guests produce different numbers.

`PerPerson` handles the case where meat is added for one person only. It deliberately does not
scale when guests arrive.

Schema deltas:

```
dish_ingredients    + scaling      int NOT NULL DEFAULT 0   -- IngredientScaling
                    + person_count int NULL                 -- only when scaling = PerPerson
weekly_plan_days    + servings     int NULL                 -- overrides default for one meal
planning_settings   + household_size int NOT NULL DEFAULT 4
ingredients         + is_pantry_staple boolean NOT NULL DEFAULT false
```

API: `PUT /dishes/{id}` and `POST /dishes` accept the new per-ingredient fields.
`GET /shopping-list/{startDate}` applies scaling when aggregating, using each planned day's
`servings` (falling back to `household_size`).

Frontend: the dish edit page needs the scaling selector and per-serving amount. Minimal change,
but required so Leif can correct data without going through Claude.

**Migration note:** existing quantities are per-dish totals, not per-serving. Default all existing
rows to `PerDish` so nothing silently changes meaning, then convert to `PerServing` during the
data-entry sprint.

## Phase 2: amount data entry sprint

The bottleneck, and the only part that needs Leif's attention rather than build time.

Format: Claude proposes amounts in Norwegian from the dish name, ingredient list and instructions;
Leif corrects; Claude writes via `PUT /dishes/{id}`. Six to eight dishes per batch.

Order by rotation value, not alphabetically:

1. **Taco** (12 ingredients, 11 placeholders, no instructions). Highest value single dish.
2. Rotation staples with bad data: Alt-i-ett pasta, Fiskegrateng, Lasagne, Kjappkjo, Hamburger,
   Nachos, Fish'n'chips, Pastasalat, Kyllingwok.
3. Dishes needing their ingredient list completed first: Tortelloini, Sushi, Napolitansk pizza,
   Pytt i panne, Toast, Pølse i brød.
4. The 11 never-eaten dishes. Defer until one shows up in a plan.

Skip the six dishes that already have clean data.

## Phase 3: Oda product mapping (done, 2026-09-17)

Built as designed below, with three deviations worth knowing about:

- The mapping is an EF Core `OwnsOne` on `Ingredient`, so the table is keyed by `ingredient_id`
  and has no separate `id` column. It loads with the ingredient.
- "Not available at Oda" lives in the same row as an `availability` column, with the product
  fields nullable. The shopping list reports `odaStatus` as `Unmapped`, `Mapped` or `NotAvailable`.
- `PUT /ingredients/{id}/oda-mapping` takes either product fields or `{ "notAvailable": true }`.

Details and the package-count rule: `domain/ingredient_model.md`.

Now much more valuable, because amounts are real and pack arithmetic means something.

```
ingredient_oda_products
  id                uuid    PK
  ingredient_id     uuid    FK -> ingredients(id), UNIQUE, cascade delete
  oda_product_id    int     NOT NULL
  oda_product_name  text    NOT NULL   -- snapshot, for display and drift detection
  pack_quantity     double  NOT NULL   -- e.g. 400
  pack_unit         int     NOT NULL   -- Unit enum, e.g. G
  confirmed_at      timestamptz NULL   -- NULL means auto-suggested, not confirmed
  created_at, updated_at
```

Model as an EF Core owned entity on the `Ingredient` aggregate, matching how `DishIngredient`
hangs off `Dish`. Keep the private-setter plus `Update()` style.

```
GET    /ingredients/oda-mappings        -> all mappings, plus unmapped ingredients
PUT    /ingredients/{id}/oda-mapping    -> set or replace
DELETE /ingredients/{id}/oda-mapping    -> clear
```

`GET /shopping-list/{startDate}` gains an additive `odaProduct` block per item, so the existing
frontend page keeps working:

```json
{
  "name": "Kyllingfilet",
  "amount": 600, "unit": "G",
  "dishes": ["Kyllingwok", "Burrito Bowl"],
  "isPantryStaple": false,
  "odaProduct": {
    "productId": 66899,
    "productName": "Ytterøy Kyllingfilet naturell",
    "packQuantity": 400, "packUnit": "G",
    "suggestedPackCount": 2,
    "confirmed": true
  }
}
```

Pack-count rule: `ceil(amount / packQuantity)` when units are compatible. `G <-> Kg` and
`Ml <-> L` convert normally. `Pcs` against a weight-based pack does not convert; treat `Pcs` as
"one package". Anything else returns `null` and Claude asks.

Mapping capture runs as its own pass over the 141 ingredients, on the ingredient axis rather than
the dish axis, since ingredients repeat across dishes.

### Known mappings captured during the interview

| Ingredient | Oda product | Note |
|---|---|---|
| Nudler | 15199, "R Nudler med kyllingsmak 5x85g" | 5 packets per purchase, 2 used per meal |
| Kyllingfilet | strimlet kyllingfilet | Leif buys the pre-cut strips, not whole fillets |
| Ost | Tine revet ost | |
| Fetaost | the tomato variant | |

### Unit choice rule

Prefer `G`/`Ml`/`Pcs` whenever Leif states a weight, volume or count. Reserve `Pack` for amounts
that are genuinely pack-relative with no natural weight ("0,75 pk revet ost"). A `Pack` fraction
that really encodes a count inside a multipack is fragile: it breaks when Oda changes the pack
size, whereas `Pcs` plus a pack size on the mapping survives it.

Keep the unit consistent per ingredient across dishes. `GET /shopping-list` keys on
`(ingredientId, unit)`, so the same ingredient in two units produces two separate lines.

### Not stocked at Oda

Some ingredients are deliberately unmappable:

- `Tortelloni` is a pre-made meal bought elsewhere, used mainly on caravan trips.
- `Sushi` is restaurant takeaway and never a grocery purchase at all.

The mapping needs a way to record "not available at Oda" so Claude stops searching for these every
week. A nullable mapping row is not enough, since that is indistinguishable from "not yet mapped".
The pantry-staple flag is the wrong lever: it means "always in stock", not "cannot be bought here",
and conflating them would make the shopping list lie.

## Phase 2.5: ingredient hygiene (issue #78) — do this BEFORE phase 3

Measured 2026-08-09: **52 of 149 ingredients are junk** (32 orphaned, 20 recipe-prose duplicates
living only inside three AI-generated recipes).

This reorders the plan. Mapping runs per ingredient, so doing phase 3 first would waste roughly a
third of the effort on records that should not exist. Worse, duplicates like `Potet` and `poteter`
split the shopping list into two lines that each round up to their own package, so the user
over-buys. The three generated recipes get converted to portion scaling as part of the cleanup,
since remapping them to canonical ingredients is the same operation.

## Phase 4: the MCP server (done, 2026-09-17)

Built as designed. Usage, configuration and the tool table: `mcp-server.md`. Two notes:

- The API is reached through the frontend proxy at `http://praxis-server:3000/api`, not `:5116`,
  so `MIDDAGSKLOK_API_URL` carries the full base including `/api`.
- `set_weekly_plan` merges edits into the existing week before calling the API's upsert, which
  requires all seven days. Days not named keep their value.

New project `src/Middagsklok.Mcp`, .NET 10, stdio transport, official C# MCP SDK
(`ModelContextProtocol` on NuGet; pin the version at implementation time rather than assuming).
Single environment variable: `MIDDAGSKLOK_API_URL=http://praxis-server:5116`.

Seven task-shaped tools. Deliberately not a 1:1 wrapper of the 24 REST routes.

| Tool | Backing call | Notes |
|---|---|---|
| `get_weekly_plan(start_date?)` | `GET /weekly-plans/{startDate}` | Defaults to current week using `weekStartsOn` (Friday). Resolves dish ids to names. |
| `list_dishes()` | `GET /dishes` | Repertoire with flags, vibe tags, total time, `lastEatenOn`. |
| `generate_weekly_plan(start_date, overwrite=false)` | `POST /weekly-plans/generate/{startDate}` | See overwrite guard below. |
| `set_weekly_plan(start_date, days[])` | `PUT /weekly-plans/{startDate}` | Targeted edits and servings overrides. |
| `get_shopping_list(start_date)` | `GET /shopping-list/{startDate}` | Scaled amounts plus mapping status. |
| `set_ingredient_oda_mapping(...)` | `PUT /ingredients/{id}/oda-mapping` | Called after Leif confirms a product. |
| `mark_week_eaten(start_date)` | `POST /weekly-plans/{startDate}/mark-eaten` | Feeds the days-between rotation logic. |

### Overwrite guard

`POST /weekly-plans/generate/{startDate}` persists immediately and overwrites any existing plan.
It only refuses when the week is already marked eaten (returns Conflict).

`generate_weekly_plan` must therefore call `GET /weekly-plans/{startDate}` first. If a plan exists
with any non-empty day and `overwrite` is false, return the existing plan and a warning instead of
generating. This lives in the MCP layer; no API change required.

## Not building

**Pack optimization in the generator.** Wanting a second chicken dish when 200 g of a 400 g pack
would otherwise go to waste is a bin-packing problem tangled with `seafoodPerWeek`, `daysBetween`
and no-repeat rules, and the objective is fuzzy: chicken freezes, fresh herbs do not, and
sometimes you simply do not want burrito bowl twice. Once amounts and pack sizes exist, Claude can
raise it conversationally at planning time and be argued with. The generator stays dumb.

**Remote HTTP MCP.** Would require real authentication on an API that currently has none, TLS, and
a public ingress decision. Deferred indefinitely.

## Weekly flow once built

1. "Plan next week." Claude reads dishes and history, proposes a week, flags pack waste and
   suggests swaps, commits on approval.
2. "Fill the Oda cart." Claude reads the scaled shopping list, skips pantry staples, resolves
   mapped ingredients to products and pack counts, searches Oda for unmapped ones.
3. Claude prints the full cart with prices and total, and waits.
4. On OK, one batched `manipulate_cart` call, plus `set_ingredient_oda_mapping` for newly
   confirmed products.
5. Leif checks out in the Oda shop.

## Build order

Phase 1 (schema) must land before phase 2 (data entry), or amounts get entered twice.
Phase 2 is the real bottleneck and the only part gated on Leif's attention.
Phase 3 can be built in parallel with phase 2.
Phase 4 is ergonomics: it turns a multi-step session into two sentences, but changes nothing
about what is possible.
