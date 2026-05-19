# Cursor Demo Kit

This folder gives Cursor project-native guidance for the Bitwarden server repo.

## What Is Included

- `rules/`: persistent repo context and file-scoped standards for C#, SQL, data access, and tests.
- `skills/`: repeatable workflows for server changes, database changes, feature builds, and PR readiness checks.
- `agents/`: read-only background agents for focused review and diagnostics during demos.
- `hooks/`: non-blocking Cursor Agent hooks registered in `hooks.json`.

## Feature Build Skills

Use these when implementing a new endpoint or feature end-to-end:

| Skill | When to use |
|-------|-------------|
| `build-feature` | Plan first — switches to Plan mode, waits for approval, then implements |
| `build-feature-e2e` | Just build it — short inline plan, immediate implementation |

Both skills share [`.cursor/skills/build-feature/implementation-guide.md`](skills/build-feature/implementation-guide.md) for the Core → DI → API → tests layer cake.

## Hooks

[`hooks/detect-secrets.sh`](hooks/detect-secrets.sh) scans file edits and shell commands for likely secrets. It warns via agent context but **never blocks** edits or commands. See [`hooks/README.md`](hooks/README.md) for setup and testing.

## Demo Flow

1. Ask Cursor to explain the repo layout. The always-on project rule should guide it to `bitwarden-server.slnx`, `src/`, `test/`, `util/`, and `bitwarden_license/`.
2. Open or edit a `.cs` file. The C# rule should steer Cursor toward file-scoped namespaces, nullable reference types, `TryAdd*` dependency injection, and xUnit conventions.
3. Ask for a database change. The database skill and SQL rule should call out Dapper plus EF parity, migration naming, and stored-procedure compatibility.
4. Ask one of the agents to review the branch. The agents are intentionally read-only so they are safe to run in the background during a demo.

## Skills vs Agents

- **Skills** are interactive workflows you invoke directly (`build-feature`, `bitwarden-pr-readiness`, `bitwarden-server-change`).
- **Agents** are read-only background reviewers (`security-reviewer`, `pr-readiness-reviewer`, `database-parity-reviewer`).

For deeper C# and database guidance, also read the richer skills under `.claude/skills/`.
