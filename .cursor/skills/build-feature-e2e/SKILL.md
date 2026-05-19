---
name: build-feature-e2e
description: >-
  Builds Bitwarden server features end-to-end without switching to Plan mode.
  Explores nearby patterns, states a short inline plan, then implements Core,
  API, DI, and tests immediately. Use when the user asks to build feature e2e,
  just build it, implement without planning, or invokes build-feature-e2e.
disable-model-invocation: true
---

# Build Feature E2E

Use this skill when the requirement is clear, the nearby pattern is obvious, and planning overhead is not needed.

## Workflow

1. Read `README.md` or `CONTRIBUTING.md` only when the change area is unfamiliar.
2. Explore the codebase and identify the nearest controller, query/command, DI registration, and test pattern.
3. Post a short inline plan in chat before editing:
   - Files to add or change per layer
   - Tests to add
   - Verification commands
4. **Do not** call `SwitchMode`, `CreatePlan`, or wait for approval.
5. Execute [../build-feature/implementation-guide.md](../build-feature/implementation-guide.md) end-to-end:
   - Load the skills listed there
   - Implement Core → DI → API → tests
   - Follow the database path when repositories or schema change
   - Run narrow verification first, then broaden if needed
6. Confirm the done checklist in the implementation guide.
7. For auth, crypto, or vault-adjacent changes, run the `security-reviewer` agent before declaring done.

## When To Use Plan-First Instead

Use `build-feature` when the change touches database migrations, auth or crypto, public API contracts, or spans multiple projects with unclear boundaries.
