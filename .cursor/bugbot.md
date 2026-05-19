# Bugbot Guidance

Use this guidance when reviewing changes in the Bitwarden server repository. Prioritize concrete correctness, security, privacy, and migration risks over style-only feedback.

## High-Priority Checks

- Preserve Bitwarden's zero-knowledge model. Flag any change that logs, returns, persists, or exposes vault data, keys, passwords, recovery material, tokens, or sensitive identifiers beyond the established boundary.
- Verify authorization and tenant isolation for API, Identity, Admin, Organization, Collection, Group, Cipher, Send, and Provider changes. Look for missing user, organization, provider, or enterprise boundary checks.
- Check that new endpoints validate inputs, return established response models, avoid leaking implementation details in errors, and have focused API tests.
- For database-backed behavior, confirm MSSQL Dapper and EF Core providers remain aligned in filtering, ordering, null semantics, transactions, and side effects.
- Review SQL, migrations, and stored procedures for idempotency, compatibility with existing deployments, large-table risk, and whether a breaking stored-procedure change needs a versioned replacement.
- Ensure security-sensitive flows keep token lifetimes, replay protections, crypto operations, and audit logging consistent with nearby code.
- Confirm new services, commands, queries, repositories, and hosted jobs are registered through existing dependency injection patterns and covered by focused xUnit tests.

## Repo-Specific Context

- Main solution: `bitwarden-server.slnx`.
- Source: `src/`; tests: `test/`; commercial code: `bitwarden_license/`; operational scripts and migrations: `util/`.
- Treat warnings as errors and respect `.editorconfig`.
- Prefer findings that cite the changed file, explain the behavioral risk, and suggest the smallest practical fix or missing test.

## Lower-Signal Feedback To Avoid

- Do not flag formatting, naming, or refactoring preferences unless they hide a real bug or violate an established local pattern.
- Do not ask for broad rewrites when a targeted guard, test, or parity fix would address the issue.
- Do not suggest logging additional secrets, tokens, vault item contents, or personally identifiable information as a debugging aid.
