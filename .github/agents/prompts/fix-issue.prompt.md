# Fix Issue

Fix a GitHub issue end-to-end: analyze, plan, implement, and create a PR.

---

## Inputs

- `issue`: GitHub issue number (e.g., `81`)

---

## Workflow

### 1. Read & Analyze

- Fetch issue #${{ issue }} and all comments from `leif-larsen/middagsklok` repository
- Identify acceptance criteria, constraints, and related context
- Search the codebase to understand affected areas

### 2. Create Plan

- Break down the work into concrete tasks
- Post the plan as a comment on the issue
- Wait for confirmation before proceeding (unless plan is trivial)

### 3. Branch & Implement

- Fetch latest from `main` branch
- Create feature branch: `feature/issue-${{ issue }}-<short-description>`
- Implement the changes following project conventions
- Run tests to verify the implementation
- Commit with message: `fix: <description> (#${{ issue }})`

### 4. Create Pull Request

- Push the branch to origin
- Create a PR targeting `main`
- Title: `fix: <description> (#${{ issue }})`
- Body should include:
  - Summary of changes
  - Link to issue: `Closes #${{ issue }}`
  - Test verification notes
- Request review from: `hallstein.brotan@novanet.no`

---

## Example Usage

```
fix issue 81
```

```
@nils-georg /fix-issue issue=15
```