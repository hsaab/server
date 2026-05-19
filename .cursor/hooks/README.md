# Cursor Hooks

Project hooks for Cursor Agent. These run from the repository root.

## detect-secrets.sh

**Events:** `postToolUse` (Write, StrReplace), `beforeShellExecution`

**Purpose:** Warn when edited content or shell commands look like they contain secrets. This hook is intentionally **non-blocking** — it never denies edits or shell execution.

**Behavior:**

1. Scans file edits from Write/StrReplace and the full shell command string.
2. Matches common secret patterns: PEM private keys, AWS keys, GitHub tokens, Slack tokens, Stripe keys, Bearer/JWT literals, and obvious secret assignments.
3. On match, returns a warning via `additional_context` (file edits) or `agent_message` with `permission: allow` (shell).
4. Always exits 0. `failClosed` is not set, so hook failures fail open.

**Requirements:**

- `jq` must be installed (`brew install jq`)
- Script must be executable: `chmod +x .cursor/hooks/detect-secrets.sh`

**Testing:**

1. Edit a scratch file to include a fake token such as `ghp_abcdefghijklmnopqrstuvwxyz1234567890`.
2. Confirm a warning appears in the Hooks output channel or agent context, and the edit is not blocked.
3. Run a harmless shell command containing a fake token in `-m` text and confirm a warning without blocking execution.
4. Run `dotnet build bitwarden-server.slnx` and confirm no warning on clean commands.

**False positives:** The hook warns only. Review flagged test fixtures or placeholders before committing.

## Configuration

Hooks are registered in [`.cursor/hooks.json`](../hooks.json). Cursor reloads the file on save; restart Cursor if hooks do not appear.

## Related

Claude Code hooks live under `.claude/hooks/` and use a different runtime. Those Stop hooks may block; these Cursor hooks do not.
