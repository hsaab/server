SHELL := /bin/zsh

DEMO_DIR := .demo
APPHOST_PID := $(DEMO_DIR)/apphost.pid
API_PID := $(DEMO_DIR)/api.pid
TOKEN_PID := $(DEMO_DIR)/token.pid
APPHOST_LOG := $(DEMO_DIR)/apphost.log
API_LOG := $(DEMO_DIR)/api.log
TOKEN_LOG := $(DEMO_DIR)/token.log
TOKEN_FILE := $(DEMO_DIR)/token.json
AUTH_HASH_FILE := dev/demo.auth-hash

API_URL := http://localhost:4000
IDENTITY_URL := http://localhost:33656
APPHOST_DASHBOARD_URL := https://localhost:17271
APPHOST_API_PORT := 4010
DEMO_EMAIL := vaulthealth@bw.test
DEMO_PASSWORD := asdfasdfasdf
TOKEN_POLL_INTERVAL := 0.5
TOKEN_POLL_MAX := 360

.PHONY: up down status open-dashboard

# AppHost in the background does not auto-open a browser (unlike `cd AppHost && dotnet run`).
# After make up, use `make open-dashboard` or the printed login URL.

up:
	@mkdir -p $(DEMO_DIR)
	@if ! command -v cargo >/dev/null 2>&1; then \
		echo "Rust cargo is required for demo seeding because seeded vault data is encrypted through util/RustSdk."; \
		echo "Install Rust/Cargo, then run make up again."; \
		exit 1; \
	fi
	@if lsof -ti :17271 -sTCP:LISTEN >/dev/null 2>&1; then \
		echo "AppHost already running (dashboard on $(APPHOST_DASHBOARD_URL))."; \
	elif [ -f $(APPHOST_PID) ] && kill -0 $$(cat $(APPHOST_PID)) 2>/dev/null; then \
		echo "AppHost already starting (pid $$(cat $(APPHOST_PID)))."; \
	else \
		echo "Starting AppHost support stack..."; \
		: > $(APPHOST_LOG); \
		DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development Services__api__BasePort=$(APPHOST_API_PORT) Demo__SeedOnStartup=true \
			dotnet run --project AppHost/AppHost.csproj --launch-profile https > $(APPHOST_LOG) 2>&1 & \
		echo $$! > $(APPHOST_PID); \
	fi
	@if python3 -c 'import json,base64,time,sys; \
t=json.load(open("$(TOKEN_FILE)")); \
p=t["access_token"].split(".")[1]; p+="="*(-len(p)%4); \
exp=json.loads(base64.urlsafe_b64decode(p)).get("exp",0); \
sys.exit(0 if exp>time.time()+30 else 1)' 2>/dev/null; then \
		echo "Demo token still valid ($(TOKEN_FILE))."; \
	elif [ -f $(TOKEN_PID) ] && kill -0 $$(cat $(TOKEN_PID)) 2>/dev/null; then \
		echo "Demo token watcher already running (pid $$(cat $(TOKEN_PID)))."; \
	else \
		echo "Starting demo token watcher..."; \
		( \
			echo "Waiting for Identity and $(DEMO_EMAIL)..."; \
			login_password=$$(grep -v '^#' $(AUTH_HASH_FILE) | tr -d '[:space:]'); \
			if [ -z "$$login_password" ]; then \
				echo "Missing auth hash in $(AUTH_HASH_FILE)."; \
				exit 1; \
			fi; \
			i=0; \
			while [ $$i -lt $(TOKEN_POLL_MAX) ]; do \
				i=$$((i + 1)); \
				if [ $$i -eq 1 ] || [ $$((i % 10)) -eq 0 ]; then \
					echo "Token attempt $$i/$(TOKEN_POLL_MAX)..."; \
				fi; \
				if curl --max-time 5 -fsS -X POST "$(IDENTITY_URL)/connect/token" \
					-H "Content-Type: application/x-www-form-urlencoded" \
					-H "Bitwarden-Client-Version: 2026.5.0" \
					--data-urlencode "scope=api offline_access" \
					--data-urlencode "client_id=web" \
					--data-urlencode "deviceType=10" \
					--data-urlencode "deviceIdentifier=vault-health-demo" \
					--data-urlencode "deviceName=Vault Health Demo" \
					--data-urlencode "grant_type=password" \
					--data-urlencode "username=$(DEMO_EMAIL)" \
					--data-urlencode "password=$$login_password" \
					-o $(TOKEN_FILE) 2>>$(TOKEN_LOG); then \
					echo "Demo token ready at $(TOKEN_FILE)."; \
					exit 0; \
				fi; \
				sleep $(TOKEN_POLL_INTERVAL); \
			done; \
			echo "Timed out waiting for seeded demo account after $$i attempts."; \
			exit 1; \
		) >> $(TOKEN_LOG) 2>&1 & \
		echo $$! > $(TOKEN_PID); \
	fi
	@if lsof -ti :4000 -sTCP:LISTEN >/dev/null 2>&1; then \
		echo "API already running on $(API_URL)."; \
	elif [ -f $(API_PID) ] && kill -0 $$(cat $(API_PID)) 2>/dev/null; then \
		echo "API already starting (pid $$(cat $(API_PID)))."; \
	else \
		echo "Starting API on $(API_URL)..."; \
		: > $(API_LOG); \
		DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development \
			dotnet run --project src/Api/Api.csproj --launch-profile Api > $(API_LOG) 2>&1 & \
		echo $$! > $(API_PID); \
	fi
	@echo "Waiting for API on $(API_URL)..."
	@i=0; \
	while [ $$i -lt 90 ]; do \
		if lsof -ti :4000 -sTCP:LISTEN >/dev/null 2>&1; then \
			echo "API is ready."; \
			break; \
		fi; \
		i=$$((i + 1)); \
		sleep 2; \
	done; \
	if ! lsof -ti :4000 -sTCP:LISTEN >/dev/null 2>&1; then \
		echo "API did not start. Check $(API_LOG) (dotnet watch is broken on macOS; make up uses dotnet run)."; \
		exit 1; \
	fi
	@echo ""
	@echo "Local app is ready."
	@echo "Account: $(DEMO_EMAIL) / $(DEMO_PASSWORD)"
	@dashboard_url=$$(grep -Eo 'https://localhost:17271/login\?t=[^[:space:]]+' $(APPHOST_LOG) 2>/dev/null | tail -1); \
	if [ -n "$$dashboard_url" ]; then \
		echo "App dashboard: $$dashboard_url"; \
	else \
		echo "App dashboard: $(APPHOST_DASHBOARD_URL) (login URL appears in $(APPHOST_LOG) after AppHost finishes building)"; \
		echo "  Run: make open-dashboard   or: tail -f $(APPHOST_LOG)"; \
	fi
	@echo "API log: $(API_LOG)"
	@echo "AppHost log: $(APPHOST_LOG)"
	@echo "Token log: $(TOKEN_LOG)"
	@if grep -q 'No trusted Aspire development certificate' $(APPHOST_LOG) 2>/dev/null || \
		! dotnet dev-certs https --check >/dev/null 2>&1; then \
		echo ""; \
		echo "If the dashboard shows a certificate warning, run:"; \
		echo "  dotnet dev-certs https --trust"; \
		echo "  dotnet dev-certs https --trust --import Aspire"; \
	fi

open-dashboard:
	@i=0; \
	while [ $$i -lt 180 ]; do \
		dashboard_url=$$(grep -Eo 'https://localhost:17271/login\?t=[^[:space:]]+' $(APPHOST_LOG) 2>/dev/null | tail -1); \
		if [ -n "$$dashboard_url" ] && lsof -ti :17271 -sTCP:LISTEN >/dev/null 2>&1; then \
			echo "Opening $$dashboard_url"; \
			open "$$dashboard_url"; \
			exit 0; \
		fi; \
		i=$$((i + 1)); \
		sleep 2; \
	done; \
	echo "Dashboard not ready. Run make up, wait for AppHost, then try again."; \
	echo "  tail -f $(APPHOST_LOG)"; \
	exit 1

status:
	@echo "Demo status"
	@for pair in \
		"Aspire dashboard:17271" \
		"Identity:33656" \
		"API (watch):4000" \
		"AppHost API:$(APPHOST_API_PORT)"; do \
		name=$${pair%%:*}; \
		port=$${pair##*:}; \
		if lsof -ti :$$port -sTCP:LISTEN >/dev/null 2>&1; then \
			echo "  $$name: up (:$$port)"; \
		else \
			echo "  $$name: down (:$$port)"; \
		fi; \
	done
	@dashboard_url=$$(grep -Eo 'https://localhost:17271/login\?t=[^[:space:]]+' $(APPHOST_LOG) 2>/dev/null | tail -1); \
	if [ -n "$$dashboard_url" ]; then \
		echo "  Dashboard login: $$dashboard_url"; \
	fi

down:
	@for pidfile in $(TOKEN_PID) $(API_PID) $(APPHOST_PID); do \
		if [ -f $$pidfile ]; then \
			pid=$$(cat $$pidfile); \
			if kill -0 $$pid 2>/dev/null; then \
				echo "Stopping $$pid..."; \
				pkill -P $$pid 2>/dev/null || true; \
				kill $$pid 2>/dev/null || true; \
			fi; \
			rm -f $$pidfile; \
		fi; \
	done
	@pids=$$(pgrep -f 'src/Api/Api.csproj' 2>/dev/null || true); \
	if [ -n "$$pids" ]; then \
		echo "Stopping stale API processes: $$pids"; \
		for pid in $$pids; do \
			pkill -P $$pid 2>/dev/null || true; \
			kill $$pid 2>/dev/null || true; \
		done; \
		sleep 1; \
		pkill -9 -f 'src/Api/Api.csproj' 2>/dev/null || true; \
	fi
	@pids=$$(lsof -ti :4000 -sTCP:LISTEN 2>/dev/null || true); \
	if [ -n "$$pids" ]; then \
		echo "Stopping processes on port 4000: $$pids"; \
		kill $$pids 2>/dev/null || true; \
		sleep 1; \
		pids=$$(lsof -ti :4000 -sTCP:LISTEN 2>/dev/null || true); \
		if [ -n "$$pids" ]; then \
			kill -9 $$pids 2>/dev/null || true; \
		fi; \
	fi
	@echo "Demo is down."
