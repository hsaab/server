---
name: build-feature
description: >-
  Plans and builds Bitwarden server features end-to-end with a plan-first
  workflow. Switches to Plan mode, produces an approved plan, then implements
  Core, API, DI, and tests. Use when the user asks to build a feature, implement
  an endpoint, add an API, plan then build, or invokes build-feature.
disable-model-invocation: true
---

# Build Feature (Plan First)

Use this skill when the change needs alignment before coding: database work, auth or crypto, public API contracts, or any non-trivial feature.

## Phase 1 — Plan

1. Read `README.md` and `CONTRIBUTING.md` when the change is broad or unfamiliar.
2. Explore the codebase and identify the nearest controller, query/command, DI registration, and test pattern.
3. Call `SwitchMode` with `target_mode_id: "plan"` before proposing architecture.
4. Research gaps and produce a plan with `CreatePlan`. Include:
   - Concrete file paths per layer
   - Whether database or OpenAPI work is needed
   - Exact verification commands
5. **Stop.** Do not edit files until the user confirms the plan.

## Phase 2 — Implement

After plan approval, switch back to Agent mode and execute [implementation-guide.md](implementation-guide.md) end-to-end:

1. Load the skills listed in the implementation guide.
2. Implement Core model, query or command, interface, and DI registration.
3. Add API models and controller action(s).
4. Add or update focused xUnit tests in the matching test project.
5. Run the narrowest verification commands from the guide, then broaden if needed.

## Phase 3 — Wrap Up

1. Confirm the done checklist in [implementation-guide.md](implementation-guide.md).
2. For auth, crypto, or vault-adjacent changes, run the `security-reviewer` agent.
3. Before declaring done, run the `bitwarden-pr-readiness` skill or `pr-readiness-reviewer` agent when the branch is meant for review.

## When To Use E2E Instead

Use `build-feature-e2e` for small, well-scoped changes with a clear nearby pattern, or when the user explicitly asks to skip planning.
