# Local demo workflow (`make up`)

Quick reference for the Cursor/demo stack on this branch. Commands are run from the **repository root**.

## Prerequisites

- .NET SDK 10
- **Docker Desktop** running (SQL Server, Azurite, MailCatcher via AppHost)
- **Rust / Cargo** (demo vault seeding uses `util/RustSdk`)
- Completed server setup: `dev/secrets.json` exists ([setup guide](https://contributing.bitwarden.com/getting-started/server/guide))

## Daily commands

```bash
make up              # Start AppHost + API + demo token; waits until API is on :4000
make status          # Which ports are listening
make open-dashboard  # Open Aspire dashboard with login token (macOS)
make down            # Stop everything (including stale API processes)
```

**Always `make down` before a fresh `make up`** if something feels stuck.

## What `make up` starts

| Piece | Port | Purpose |
|-------|------|---------|
| **API** (use this for curls) | **4000** | Hot-path API you develop against (`dotnet run`) |
| AppHost API | 4010 | Same API inside Aspire (avoid for manual curls) |
| Identity | 33656 | Auth / tokens |
| Aspire dashboard | 15055 (HTTP) | Logs, resources, traces — HTTP avoids cert errors in Cursor’s browser |

Demo account (seeded on startup):

- Email: `vaulthealth@bw.test`
- Password: `asdfasdfasdf`
- Token file: `.demo/token.json` (written by `make up`)
- Login hash for Identity: `dev/demo.auth-hash`

Logs: `.demo/apphost.log`, `.demo/api.log`, `.demo/token.log`

## Aspire dashboard

`make up` runs AppHost **in the background**, so the browser does **not** open automatically (unlike `cd AppHost && dotnet run`, which uses `launchBrowser: true`).

1. Wait for AppHost to finish building (first run can take several minutes).
2. Use the **login URL** printed by `make up`, or run:

   ```bash
   make open-dashboard
   ```

   The URL looks like `http://localhost:15055/login?t=...` — the `?t=` token is required; do not open bare `http://localhost:15055` without the token.

### Cursor browser: Error Code -202

Chromium error **-202** means the HTTPS dev certificate is not trusted (common in Cursor’s embedded browser). **`make up` uses the AppHost `http` launch profile** so the dashboard is **`http://localhost:15055`**, not `https://localhost:17271`.

Keep these in place for the local demo workflow:

- `Makefile` starts AppHost with `--launch-profile http`.
- `Makefile` sets `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`.
- `AppHost/Properties/launchSettings.json` also sets `ASPIRE_ALLOW_UNSECURED_TRANSPORT` for the `http` profile.

If the flag is missing, AppHost fails with:

```text
The 'applicationUrl' setting must be an https address unless the 'ASPIRE_ALLOW_UNSECURED_TRANSPORT' environment variable is set to true.
```

After changing this, restart the stack:

```bash
make down && make up
```

Then open the **`login?t=...`** URL from `make status` or `make open-dashboard`.

If you run AppHost manually with the **`https`** profile and see a cert warning in Safari/Chrome:

```bash
dotnet dev-certs https --trust
dotnet dev-certs https --trust --import Aspire
```

See also: https://aka.ms/aspire/devcerts

## Sample curls

API must be up on **port 4000** (`make status` → `API … up (:4000)`).

```bash
export TOKEN=$(python3 -c 'import json; print(json.load(open(".demo/token.json"))["access_token"])')
export BW_CLIENT_VERSION="2026.5.0"
```

### No auth

```bash
curl -sS http://localhost:4000/alive
curl -sS http://localhost:4000/version | python3 -m json.tool
curl -sS http://localhost:4000/config | python3 -m json.tool
```

### With Bearer token

```bash
curl -sS http://localhost:4000/accounts/profile \
  -H "Authorization: Bearer $TOKEN" \
  -H "Bitwarden-Client-Version: $BW_CLIENT_VERSION" \
  | python3 -m json.tool

curl -sS http://localhost:4000/accounts/organizations \
  -H "Authorization: Bearer $TOKEN" \
  -H "Bitwarden-Client-Version: $BW_CLIENT_VERSION" \
  | python3 -m json.tool

curl -sS http://localhost:4000/folders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Bitwarden-Client-Version: $BW_CLIENT_VERSION" \
  | python3 -m json.tool

# Large response (~75 seeded items)
curl -sS http://localhost:4000/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Bitwarden-Client-Version: $BW_CLIENT_VERSION" \
  | python3 -m json.tool
```

### Refresh token manually

```bash
curl -sS -X POST "http://localhost:33656/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Bitwarden-Client-Version: $BW_CLIENT_VERSION" \
  --data-urlencode "scope=api offline_access" \
  --data-urlencode "client_id=web" \
  --data-urlencode "deviceType=10" \
  --data-urlencode "deviceIdentifier=vault-health-demo" \
  --data-urlencode "deviceName=Vault Health Demo" \
  --data-urlencode "grant_type=password" \
  --data-urlencode "username=vaulthealth@bw.test" \
  --data-urlencode "password=$(tr -d '[:space:]' < dev/demo.auth-hash)" \
  | python3 -m json.tool
```

## `make up` vs raw `dotnet`

| Approach | When to use |
|----------|-------------|
| **`make up`** | Default for demo/feature work: AppHost stack + seeded user + API on **:4000** |
| **`cd AppHost && dotnet run`** | Full Bitwarden stack in foreground; browser opens automatically |
| **`dotnet run --project src/Api`** | API only (no Identity/DB unless already running) |

## Troubleshooting

### Curls: `Connection refused` on `:4000`

The API is not listening. Common causes:

1. **`make up` not run**, or AppHost still building — check `make status`.
2. **`dotnet watch` was used manually** — on macOS it often **never binds :4000** (see below). `make up` uses **`dotnet run`** instead.
3. **Stale processes** — run `make down`, then `make up` again.

Verify:

```bash
make status
curl -sS http://localhost:4000/alive
tail -f .demo/api.log
```

### `make up` says running but curls fail

Older `make up` only checked that a **PID file existed**, not that port **4000** was open. Current Makefile **waits for the port** and fails with a clear error if the API does not start.

### `dotnet watch` on macOS (do not use for demo API)

`dotnet watch` can hang on:

```text
Failed to read '.../src/Api/obj\Debug/net10.0/staticwebassets.development.json'
```

The watch process stays alive but **Kestrel never starts**, so port 4000 stays closed. Use `make up` (`dotnet run`) or restart with `make down && make up`.

For live reload while coding, restart the API:

```bash
make down && make up
# or foreground:
dotnet run --project src/Api/Api.csproj --launch-profile Api
```

### Port 4000 already in use / `Api.pdb` locked

Usually **multiple** `make up` or `dotnet watch` runs left over. Fix:

```bash
make down
lsof -i :4000 -sTCP:LISTEN   # should be empty
make up
```

### Dashboard not loading

- AppHost still compiling — `tail -f .demo/apphost.log` until `Now listening on: http://localhost:15055`
- Use **`make status`** or **`make open-dashboard`** for the `login?t=...` URL (not the bare dashboard root)
- Cursor browser **-202** on `https://localhost:17271` → use the HTTP dashboard on `:15055` and run `make down && make up`
- AppHost fails with `'applicationUrl' setting must be an https address` → restore `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` for the HTTP profile

### Token / 401 on API

- Wait for Identity on `:33656` (`make status`)
- Refresh token (curl block above) or delete `.demo/token.json` and `make up`

## Alternative: full AppHost only

See [AppHost/README.md](../AppHost/README.md) for the upstream Aspire workflow (all services, migrations, MailCatcher, etc.):

```bash
cd AppHost
dotnet run
```
