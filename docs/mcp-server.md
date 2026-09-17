# Middagsklok MCP server

`src/Middagsklok.Mcp` is a stdio MCP server that lets Claude Code plan the week and fill the Oda
cart. It is a thin HTTP client over the Middagsklok API and holds no state. Checkout still happens
in the Oda shop; this server never talks to Oda.

## Configuration

One environment variable:

| Variable | Example | Meaning |
|---|---|---|
| `MIDDAGSKLOK_API_URL` | `http://praxis-server:3000/api` | Base URL of the API, including the `/api` prefix when going through the frontend proxy. |

The server refuses to start without it.

## Build and register

```bash
dotnet build src/Middagsklok.Mcp -c Release
claude mcp add --scope user middagsklok \
  -e MIDDAGSKLOK_API_URL=http://praxis-server:3000/api \
  -- dotnet <repo>/src/Middagsklok.Mcp/bin/Release/net10.0/Middagsklok.Mcp.dll
```

Logs go to stderr; stdout carries the protocol.

## Tools

| Tool | Backing call | Notes |
|---|---|---|
| `get_weekly_plan(startDate?)` | `GET /weekly-plans/{startDate}` | Defaults to the current week using `weekStartsOn` from planning settings. Adds `dishName` to each day. |
| `list_dishes(includeRetired?)` | `GET /dishes` | Adds `totalMinutes`, drops `instructions` to keep the payload small. |
| `generate_weekly_plan(startDate, overwrite?)` | `POST /weekly-plans/generate/{startDate}` | Refuses when the week already has planned days unless `overwrite` is true. Returns the existing plan with a warning instead. |
| `set_weekly_plan(startDate, days[])` | `PUT /weekly-plans/{startDate}` | Targeted edits: each entry has `date`, optional `dishId` (null clears the day) and optional `servings`. Unlisted days keep their value. |
| `get_shopping_list(startDate)` | `GET /shopping-list/{startDate}` | Scaled amounts, `odaStatus` and the `odaProduct` block with `suggestedPackCount`. |
| `set_ingredient_oda_mapping(...)` | `PUT /ingredients/{id}/oda-mapping` | Call only after the person confirms the product. `notAvailable = true` records "Oda does not stock this". |
| `mark_week_eaten(startDate)` | `POST /weekly-plans/{startDate}/mark-eaten` | Feeds the rotation history. |

## Overwrite guard

The generate endpoint persists immediately and overwrites any existing plan. The guard lives in the
MCP layer: `generate_weekly_plan` reads the week first, and when any day has a dish and `overwrite`
is false it returns the existing plan and a warning without calling the generator.

## Weekly flow

1. "Plan next week": `list_dishes` and `get_weekly_plan`, propose, then `set_weekly_plan` or
   `generate_weekly_plan` on approval.
2. "Fill the Oda cart": `get_shopping_list`, skip pantry staples, add mapped products with the
   suggested package counts, search Oda only for `Unmapped` items.
3. Print the cart with prices and wait for an explicit OK before adding anything.
4. On OK, add to the cart and call `set_ingredient_oda_mapping` for newly confirmed products.
5. Checkout in the Oda shop.
