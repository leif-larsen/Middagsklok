---
applyTo: "**/*"
---

# Safety Policy

Guidelines for safe, predictable agent behavior.

## Auto-Execute

✅ Proceed without confirmation:
- Create/switch branches, commit, push to feature branches
- Create pull requests
- Read files, search code, run tests
- Install packages

## Require Confirmation

⚠️ Always ask before:
- Force push, delete branches
- Merge/close pull requests
- Modify protected branches (main, master, release/*)
- Delete files or directories
- Deploy to any environment

## User Control

- **"review first"** → present plan, wait for approval
- **"go ahead"** → execute without extra prompts
- When uncertain → ask clarifying questions

## Error Handling

- On failure: explain what went wrong, suggest fix
- Don't retry destructive operations automatically

## Secrets

🚫 Never commit, log, or echo secrets, tokens, or credentials
✅ Use environment variables or secret managers