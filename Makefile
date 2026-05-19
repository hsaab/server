SHELL := /bin/zsh

DEMO_DIR := .demo
APPHOST_PID := $(DEMO_DIR)/apphost.pid
API_PID := $(DEMO_DIR)/api.pid
TOKEN_PID := $(DEMO_DIR)/token.pid
APPHOST_LOG := $(DEMO_DIR)/apphost.log
API_LOG := $(DEMO_DIR)/api.log
TOKEN_LOG := $(DEMO_DIR)/token.log
TOKEN_FILE := $(DEMO_DIR)/token.json

API_URL := http://localhost:4000
IDENTITY_URL := http://localhost:33656
APPHOST_DASHBOARD_URL := https://localhost:17271
APPHOST_API_PORT := 4010
DEMO_EMAIL := vaulthealth@bw.test
DEMO_PASSWORD := asdfasdfasdf

.PHONY: up down

up:
	@mkdir -p $(DEMO_DIR)
	@if ! command -v cargo >/dev/null 2>&1; then \
		echo "Rust cargo is required for demo seeding because seeded vault data is encrypted through util/RustSdk."; \
		echo "Install Rust/Cargo, then run make up again."; \
		exit 1; \
	fi
	@if [ -f $(APPHOST_PID) ] && kill -0 $$(cat $(APPHOST_PID)) 2>/dev/null; then \
		echo "AppHost already running (pid $$(cat $(APPHOST_PID)))."; \
	else \
		echo "Starting AppHost support stack..."; \
		DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development Services__api__BasePort=$(APPHOST_API_PORT) Demo__SeedOnStartup=true \
			dotnet run --project AppHost/AppHost.csproj > $(APPHOST_LOG) 2>&1 & \
		echo $$! > $(APPHOST_PID); \
	fi
	@if [ -f $(TOKEN_PID) ] && kill -0 $$(cat $(TOKEN_PID)) 2>/dev/null; then \
		echo "Demo token watcher already running (pid $$(cat $(TOKEN_PID)))."; \
	else \
		echo "Starting demo token watcher..."; \
		rm -f $(TOKEN_FILE); \
		( \
			echo "Waiting for $(DEMO_EMAIL) to be seeded and accepted by Identity..."; \
			login_password=$$(dotnet run --project util/SeederUtility -- auth-hash --email "$(DEMO_EMAIL)" --password "$(DEMO_PASSWORD)" --kdf-iterations 5000 2>/dev/null); \
			i=0; \
			while [ $$i -lt 180 ]; do \
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
					-o $(TOKEN_FILE) >/dev/null 2>&1; then \
					echo "Demo token ready at $(TOKEN_FILE)."; \
					exit 0; \
				fi; \
				i=$$((i + 1)); \
				sleep 2; \
			done; \
			echo "Timed out waiting for seeded demo account."; \
			exit 1; \
		) > $(TOKEN_LOG) 2>&1 & \
		echo $$! > $(TOKEN_PID); \
	fi
	@if [ -f $(API_PID) ] && kill -0 $$(cat $(API_PID)) 2>/dev/null; then \
		echo "API watch already running (pid $$(cat $(API_PID)))."; \
	else \
		echo "Starting API watch..."; \
		DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development \
			dotnet watch --project src/Api/Api.csproj run --launch-profile Api > $(API_LOG) 2>&1 & \
		echo $$! > $(API_PID); \
	fi
	@echo ""
	@echo "Local app is starting."
	@echo "Account: $(DEMO_EMAIL) / $(DEMO_PASSWORD)"
	@echo "App dashboard: $(APPHOST_DASHBOARD_URL)"
	@echo "API log: $(API_LOG)"
	@echo "AppHost log: $(APPHOST_LOG)"
	@echo "Token log: $(TOKEN_LOG)"

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
	@echo "Demo is down."
