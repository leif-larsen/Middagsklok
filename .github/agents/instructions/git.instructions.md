---
applyTo: "**/*"
---

# Git Conventions

## Branch Naming

```
<type>/<short-description>
```

| Type | Use for |
|------|---------|
| `feature/` | New features |
| `fix/` | Bug fixes |
| `chore/` | Maintenance, refactoring, dependencies |
| `docs/` | Documentation only |

**Examples:** `feature/user-authentication`, `fix/null-reference-export`, `chore/update-packages`

## Commit Messages

Use **conventional commits**:

```
<type>: <description>

[optional body]
```

| Type | Use for |
|------|---------|
| `feat` | New feature |
| `fix` | Bug fix |
| `refactor` | Code change (no new feature or fix) |
| `test` | Adding/updating tests |
| `docs` | Documentation |
| `chore` | Build, dependencies, config |

**Examples:**
- `feat: add user export to CSV`
- `fix: handle null customer in report`
- `refactor: extract validation to separate class`

## Pull Requests

- Use clear, descriptive titles (same format as commits)
- Reference related issues: `Fixes #123` or `Closes #123`
- Keep PRs focused — one feature/fix per PR
- Squash commits when merging to main

## Before Committing

- **Build first:** Run `dotnet build` to verify compilation
- Fix any errors before staging
- Run tests if changes affect testable code

## Guidelines

- Commit early, commit often
- Pull and rebase before pushing to avoid merge conflicts
- Use `.gitignore` for build artifacts, IDE files, and local configs