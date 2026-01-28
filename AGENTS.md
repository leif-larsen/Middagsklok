# AGENTS

This file defines how AI agents should work in this repo. If instructions conflict, follow repo-specific rules first.

## General
- Make only high-confidence suggestions.
- Use the latest C# features (C# 14) and .NET 10.
- Never change `global.json` unless explicitly asked.
- Never change `package.json`, `package-lock.json`, or `NuGet.config` unless explicitly asked.
- Never leave unused `using` directives.
- Ensure code compiles without warnings and tests pass.
- Follow existing project conventions and .editorconfig.

## C# formatting and style
- Prefer file-scoped namespaces and single-line using directives.
- Insert a new line before the opening `{` of any code block.
- Final return statement in a method must be on its own line.
- Use pattern matching and switch expressions when possible.
- Use `nameof` instead of string literals for member names.

## C# implementation rules
- Write clear, concise comments for each function; avoid unnecessary comments.
- Prefer `var` for variable declarations.
- Use namespaces that match folder structure.
- Do not use regions.
- Prefer `record` types for immutable data structures.
- Use expression-bodied members for simple methods and properties.
- Use async/await with `Task`/`Task<T>`.
- Use `IEnumerable<T>` for collections that are not modified.
- Never return mutable collections from public APIs.
- Favor collection and object initializers.
- Use string interpolation instead of `string.Format` or concatenation.
- Favor primary constructors for all types.
- For types without an implementation, do not add a body (example: `public interface IMyInterface;`).
- Do not add postfixes like `Async` or `Impl` to class or method names.
- Centralized package management only: update `CentralPackageVersions.props` instead of `.csproj`.

## Exceptions
- Use exceptions only for exceptional situations.
- Do not use exceptions for control flow.
- Always provide a meaningful message.
- Always create a custom exception type derived from `Exception`.
- Do not use built-in exception types (like `InvalidOperationException` or `ArgumentException`).
- XML doc for exceptions must start with "The exception that is thrown when ...".
- Never suffix exception class names with `Exception`.

## Nullable reference types
- Use `is null` / `is not null` instead of `== null` / `!= null`.
- Trust nullability annotations; avoid redundant null checks.

## Naming
- PascalCase for public members and methods.
- camelCase for locals and private fields.
- Prefix private fields with `_`.
- Prefix interface names with `I`.

## Logging
- Use structured logging with named parameters.
- Use appropriate log levels.
- Use `ILogger<T>` where `T` is the class name.
- Keep logging in separate partial methods: `<SystemName>Logging.cs`.
- Logging class must be `internal static partial`, methods `internal`.
- Use `[LoggerMessage]` without `eventId`.

## Dependency injection
- If convention is `IFoo` -> `Foo`, registration not needed.
- Prefer constructor injection.
- Avoid `IServiceProvider` service locator pattern.
- For singletons, use `[Singleton]` attribute on the class.

## Build, format, test
- Use `dotnet build` from the command line (not VS build tasks).
- Use `dotnet format` for formatting.
- Use `dotnet test` for tests.

## Documentation (docs/)
- All docs live under `docs/`.
- Use Markdown and GFM; diagrams via Mermaid and PlantUML.
- Be clear, concise, specific, and consistent; active voice, present tense.
- Focus on public APIs/features; do not document internals or third-party tools.
- Each folder must include `index.md` and `toc.yml`.
- `toc.yml` must link to folder `toc.yml`, not `index.md`.
- Use relative links; ensure links are valid.
- Always end Markdown with a single empty line.
- Use [GitHub Flavored Markdown](https://github.github.com/gfm/) for additional features.
- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for diagrams.
- Use [PlantUML](https://plantuml.com/) for UML diagrams.
- Never use shell commands or external tools to modify docs after writing them.

## Vertical slices (Minimal APIs)
- Organize by feature: `src/<App>.Api/Features/<FeatureName>/...`
- Tests live in `tests/<App>.Tests/Features/<FeatureName>/...`
- Do not introduce `Controllers`, `Handlers`, `Services`, `Repositories`.
- Each feature exposes one endpoint; keep endpoint thin.
- Each slice has one use-case class with `Execute(...)`.
- Validate at boundary; authorization inside the slice.
- Use EF Core directly; avoid repositories (using AppDbContext).
- Prefer small, complete slices; duplication is acceptable.
- Avoid shared abstractions by default.
- A typical feature folder may contain:
    - `Endpoint.cs` – Minimal API endpoint mapping
    - `Request.cs` / `Response.cs` – DTOs
    - `UseCase.cs` – the executable use case
    - `Validator.cs` – input validation (if applicable) 
- No mediator patterns.
- Dependencies are injected via constructor.
- **Commands**
  - Change state
  - Return `void`, an identifier, or a result object
- **Queries**
  - Must be side-effect free
  - May project directly to read models (DTOs)
  - Should avoid loading full aggregates
- Do not force commands and queries into the same abstraction.

