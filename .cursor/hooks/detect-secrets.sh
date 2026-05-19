#!/bin/bash
# detect-secrets.sh
# Non-blocking secrets scanner for Cursor Agent hooks.
# Warns on likely secrets in file edits and shell commands; never blocks.

set -euo pipefail

INPUT=$(cat)

if ! command -v jq >/dev/null 2>&1; then
  exit 0
fi

HOOK_EVENT=$(echo "$INPUT" | jq -r '.hook_event_name // empty')
CONTENT=""
SOURCE=""

case "$HOOK_EVENT" in
  postToolUse)
    TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // empty')
    case "$TOOL_NAME" in
      Write)
        CONTENT=$(echo "$INPUT" | jq -r '.tool_input.contents // empty')
        SOURCE=$(echo "$INPUT" | jq -r '.tool_input.path // "edited file"')
        ;;
      StrReplace)
        CONTENT=$(echo "$INPUT" | jq -r '.tool_input.new_string // empty')
        SOURCE=$(echo "$INPUT" | jq -r '.tool_input.path // "edited file"')
        ;;
      *)
        exit 0
        ;;
    esac
    ;;
  beforeShellExecution)
    CONTENT=$(echo "$INPUT" | jq -r '.command // empty')
    SOURCE="shell command"
    ;;
  *)
    exit 0
    ;;
esac

if [[ -z "$CONTENT" ]]; then
  exit 0
fi

findings=()

scan_pattern() {
  local label="$1"
  local pattern="$2"
  if echo "$CONTENT" | grep -Eiq "$pattern"; then
    findings+=("$label")
  fi
}

scan_pattern "PEM private key" 'BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY'
scan_pattern "AWS access key" 'AKIA[0-9A-Z]{16}'
scan_pattern "GitHub token" 'gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}'
scan_pattern "Slack token" 'xox[baprs]-[A-Za-z0-9-]{10,}'
scan_pattern "Stripe secret key" 'sk_(live|test)_[A-Za-z0-9]{10,}'
scan_pattern "Bearer token literal" 'Bearer [A-Za-z0-9._-]{20,}'
scan_pattern "JWT literal" 'eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.'
scan_pattern "Secret assignment" '(api[_-]?key|secret|password|token)[[:space:]]*=[[:space:]]*['\''"][^'\''"]{8,}['\''"]'

if [[ ${#findings[@]} -eq 0 ]]; then
  exit 0
fi

UNIQUE_FINDINGS=$(printf '%s\n' "${findings[@]}" | sort -u | paste -sd ', ' -)
MESSAGE="Possible secret detected in ${SOURCE}: ${UNIQUE_FINDINGS}. Review before committing — this may be a test fixture or placeholder."

case "$HOOK_EVENT" in
  postToolUse)
    jq -n \
      --arg context "$MESSAGE" \
      '{ "additional_context": $context }'
    ;;
  beforeShellExecution)
    jq -n \
      --arg agent_message "$MESSAGE" \
      '{ "permission": "allow", "agent_message": $agent_message }'
    ;;
esac

exit 0
