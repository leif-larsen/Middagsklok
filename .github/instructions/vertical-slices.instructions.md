---
applyTo: "**/Features/**/*.*"
---

# GitHub Copilot Instructions — Vertical Slices with Minimal APIs (.NET)

You are assisting in a .NET solution that uses **Minimal APIs** together with a **Vertical Slice Architecture** (feature-first).

Each feature is implemented as a thin, end-to-end slice. We do NOT use Controllers, MediatR, or similar mediator / pipeline libraries.

## Core principles
- Organize code by **feature**, not by technical layer.
- Each feature represents exactly one use case.
- Prefer small, complete slices that can be built and shipped independently.
- Avoid shared abstractions by default.
- Duplication is acceptable if it preserves slice boundaries and clarity.

## Folder and project structure
- Production code is organized by feature:
  - `src/<App>.Api/Features/<FeatureName>/...`
- Tests live in a **separate test project**:
  - `tests/<App>.Tests/Features/<FeatureName>/...`
- Do NOT introduce folders such as:
  - `Controllers`, `Handlers`, `Services`, `Repositories`

## Feature slice structure (Minimal API)
A typical feature folder may contain:
- `Endpoint.cs` – Minimal API endpoint mapping
- `Request.cs` / `Response.cs` – DTOs
- `UseCase.cs` – the executable use case
- `Validator.cs` – input validation (if applicable)

Names should describe intent:
- Commands: `CreateOrder`, `UpdateCustomerEmail`
- Queries: `GetOrderById`, `SearchCustomers`

## Endpoint rules (Minimal APIs)
- Each slice exposes **one** endpoint.
- Endpoint mapping is local to the slice.
- Endpoint methods must be thin:
  - Bind request
  - Resolve dependencies
  - Call `Execute(...)`
  - Return HTTP result
- Do NOT place business logic in endpoints.

Example intent (conceptual):
- `app.MapPost("/orders", CreateOrder.Endpoint);`
- `app.MapGet("/orders/{id}", GetOrderById.Endpoint);`

## Use-case execution pattern
- Each slice has **one primary use-case class**.
- The public entry point MUST be a method named:

  `Execute(...)`

- No generic handler interfaces.
- No mediator patterns.
- Dependencies are injected via constructor.
- `Execute` coordinates the workflow:
  - validation
  - authorization
  - domain logic
  - persistence
  - response mapping

## Commands vs Queries
- **Commands**
  - Change state
  - Return `void`, an identifier, or a result object
- **Queries**
  - Must be side-effect free
  - May project directly to read models (DTOs)
  - Should avoid loading full aggregates

Do not force commands and queries into the same abstraction.

## Validation and authorization
- Validate input at the boundary of the slice.
- Fail fast before persistence.
- Authorization is handled inside the slice, not globally.
- Use consistent HTTP semantics:
  - 400 – validation errors
  - 401 / 403 – auth failures
  - 404 – not found
  - 409 – conflicts

## Data access rules
- Data access happens inside the slice.
- Use EF Core directly; avoid generic repositories.
- Avoid cross-slice data access.
- Prefer explicit queries and projections.
- Transactions should be explicit and local.

## Testing strategy
- Tests live outside production code, in a separate project.
- Organize tests by feature:
  - `tests/<App>.Tests/Features/<FeatureName>/...`
- Prefer integration tests exercising:
  - Minimal API endpoint
  - Use-case execution
  - Persistence
- Unit tests are acceptable for:
  - validation
  - domain logic with real rules

## Pull request scope
- One PR should contain one slice or a small set of related slices.
- Avoid unrelated refactors.

## Copilot guidance
When implementing a new feature:
1. Create a new feature folder.
2. Define the Minimal API endpoint locally in the slice.
3. Create request/response DTOs.
4. Implement a use-case class with an `Execute` method.
5. Add validation and authorization.
6. Implement persistence or integration logic.
7. Add tests in the test project.
8. Keep changes outside the slice minimal.

If existing code conflicts with these rules, follow them for the new slice instead of rewriting old code.