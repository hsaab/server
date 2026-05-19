---
name: bitwarden-pr-readiness
description: Checks a Bitwarden server branch before review. Use when preparing a PR, reviewing a diff, summarizing verification, or deciding which build and test commands should run.
---

# Bitwarden PR Readiness

## Steps

1. Inspect `git status`, the changed files, and the diff against the base branch.
2. Classify the change area: API, Core, Identity, Billing, infrastructure, database, commercial, utility, or tests only.
3. Read the nearest source and test patterns for changed files.
4. Check for common readiness gaps:
   - Missing xUnit coverage for new behavior.
   - Missing `[DatabaseData]` tests for repository parity.
   - SQL changes without matching EF or Dapper work.
   - Public API changes without OpenAPI consideration.
   - Security-sensitive changes without denial-path tests.
   - Sensitive data in logs, errors, fixtures, or snapshots.
5. Recommend the smallest verification set that gives confidence.

## Output Template

```markdown
## Readiness Verdict
[Ready / Not ready / Ready with caveats]

## Blocking Issues
- [issue or "None"]

## Recommended Verification
- [exact command or manual check]

## Notes For Reviewers
- [security, database, API, or compatibility context]
```
