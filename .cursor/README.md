# Cursor Demo Kit

This folder gives Cursor project-native guidance for the Bitwarden server repo.

## What Is Included

- `rules/`: persistent repo context and file-scoped standards for C#, SQL, data access, and tests.
- `skills/`: repeatable workflows for server changes, database changes, and PR readiness checks.
- `agents/`: read-only background agents for focused review and diagnostics during demos.

## Demo Flow

1. Ask Cursor to explain the repo layout. The always-on project rule should guide it to `bitwarden-server.slnx`, `src/`, `test/`, `util/`, and `bitwarden_license/`.
2. Open or edit a `.cs` file. The C# rule should steer Cursor toward file-scoped namespaces, nullable reference types, `TryAdd*` dependency injection, and xUnit conventions.
3. Ask for a database change. The database skill and SQL rule should call out Dapper plus EF parity, migration naming, and stored-procedure compatibility.
4. Ask one of the agents to review the branch. The agents are intentionally read-only so they are safe to run in the background during a demo.
