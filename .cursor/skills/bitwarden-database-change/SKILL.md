---
name: bitwarden-database-change
description: Guides Bitwarden database, migration, Dapper, and EF Core changes. Use when editing SQL scripts, stored procedures, repositories, EntityTypeConfiguration classes, EF migrations, or database-backed integration tests.
---

# Bitwarden Database Change

## First Decision

Determine which paths are affected:

- MSSQL: Dapper repositories plus stored procedures.
- PostgreSQL, MySQL, SQLite: EF Core repositories and migrations.
- Shared repository interface: usually requires both implementations and parity tests.

## MSSQL Workflow

1. Update schema source in `src/Sql/dbo` when changing stored procedures or table definitions.
2. Add an idempotent migration in `util/Migrator/DbScripts` named `YYYY-MM-DD_##_Description.sql`.
3. Use plain `CREATE PROCEDURE` in SSDT source and `CREATE OR ALTER PROCEDURE` in migration scripts.
4. Add `SET NOCOUNT ON` to stored procedures.
5. Use nullable defaulted parameters for compatible procedure changes.
6. Avoid normal migration indexes on very large tables; ask for confirmation.

## EF Core Workflow

1. Match Dapper stored-procedure behavior exactly.
2. Keep mappings in `EntityTypeConfiguration<T>` classes.
3. Generate provider migrations with `pwsh ef_migrate.ps1 <MigrationName>` when schema changes require it.
4. Review generated `Up()` and `Down()` methods before committing.
5. Do not configure database-generated IDs for entities that use `CoreHelpers.GenerateComb()`.

## Verification

Use `[DatabaseTheory, DatabaseData]` integration tests for repository changes. Start with the narrow affected integration test project before running broader tests.
