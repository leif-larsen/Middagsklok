# Claude Code Instructions

## After code changes
After making code changes, always:
1. Create a git commit with a descriptive message.
2. Open a GitHub pull request targeting `main`.

Do this automatically without waiting to be asked.

For follow-up commits, always check the current branch's PR status with `gh pr view --json state` before pushing. If the PR is already closed or merged, create a new branch rather than pushing to the old one.
