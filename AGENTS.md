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

## GitHub workflow policy
- For each coding task, create a dedicated branch before making code changes.
- Branch naming format: `codex/<type>/<short-kebab-slug>`.
- Allowed `<type>` values: `feature`, `fix`, `chore`, `docs`, `refactor`, `test`.
- Keep branch names concise and deterministic from the task description.
- Commit messages should use Conventional Commits (for example: `fix(api): handle empty recipe result`).
- Push the task branch to `origin` when work is complete.
- Open a pull request targeting `main` after push.
- If GitHub MCP is available, prefer it for branch/PR/issue operations; otherwise use `git` and `gh`.

## Pull request policy
- Every task branch must produce one PR unless explicitly told otherwise.
- PR title format: `<type>(<scope>): <short summary>`.
- PR body must include these sections:
  - `## Summary`
  - `## Changes`
  - `## Verification`
  - `## Risks`
  - `## Follow-ups`
- `## Verification` must include exact commands executed and their outcomes.
- If tests were not run, state why in `## Verification`.
- Link related issues using `Closes #<number>` or `Related to #<number>` when applicable.

## Issue creation policy
- Create GitHub issues for follow-up work that should not be completed in the current task.
- Create an issue when one of these applies:
  - A bug is discovered but out of current scope.
  - Missing tests are identified and deferred.
  - Technical debt is introduced intentionally for delivery speed.
  - A TODO/FIXME is required to unblock current work.
- Do not create issues for trivial edits that can be completed immediately.
- Issue title format: `<area>: <concise problem statement>`.
- Issue body must include:
  - `## Problem`
  - `## Impact`
  - `## Proposed solution`
  - `## Acceptance criteria`
  - `## Notes`

## Approval policy override for git and GitHub
- Repo-specific override: prior approval is not required for non-destructive `git`, `gh`, or GitHub MCP operations used to complete the workflow above.
- Pre-approved examples include: `git switch -c`, `git add`, `git commit`, `git push`, `gh pr create`, `gh issue create`, and equivalent MCP actions.
- Always ask for approval before destructive or history-rewriting commands (for example: `git reset --hard`, `git rebase -i`, `git push --force`, branch deletion, or remote deletion).
- Always ask for approval before operations outside this repository or involving secrets/permission changes.

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
