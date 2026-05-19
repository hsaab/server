---
name: bitwarden-server-change
description: Guides implementation of Bitwarden server C#/.NET changes. Use when adding or modifying API endpoints, commands, queries, services, dependency injection, caching, or tests in src/, test/, or bitwarden_license/.
---

# Bitwarden Server Change

## Workflow

1. Identify the owning project in `bitwarden-server.slnx` and read nearby source plus matching tests.
2. Preserve the local architecture. Prefer command/query classes for new feature logic, but follow existing service patterns when modifying service-based code.
3. Apply repo conventions:
   - File-scoped namespaces.
   - Nullable reference types.
   - `TryAdd*` dependency injection registration.
   - `CoreHelpers.GenerateComb()` for entity IDs.
   - `ActionResult<T>` from controller actions.
4. Check security impact before coding. Do not log or expose vault data, secrets, tokens, passwords, keys, or PII.
5. Add or update focused xUnit tests in the matching test project.
6. Verify with the narrowest useful command first, then broaden if risk is high.

## Useful Commands

```bash
dotnet build bitwarden-server.slnx
dotnet test test/Core.Test/Core.Test.csproj
dotnet test bitwarden-server.slnx
```

## Review Checklist

- Behavior is scoped to the request and follows nearby patterns.
- Tests cover successful behavior, denial or error paths, and security boundaries when relevant.
- Dependency registration is idempotent unless there is a deliberate exception.
- No sensitive data is logged or surfaced in exceptions.
- Public API changes include test coverage and any required OpenAPI update.
