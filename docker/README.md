# Logging Stack — Grafana + Loki

## Architecture

```
Browser (React app)               .NET Web API
    │                                  │
    │  POST /api/logs (JSON)            │  Serilog
    ▼                                  │
.NET API /api/logs endpoint ───────────┘
    │
    │  Serilog.Sinks.Grafana.Loki
    ▼
Loki :3100  ←── log storage & query engine
    │
    │  LogQL queries
    ▼
Grafana :3000  ←── dashboard UI
```

Browser logs are forwarded to Loki **through the existing .NET API** using
`POST /api/logs`. No separate log-receiver service is needed.

## Start the logging stack

```bash
docker compose up -d loki grafana
```

Or start everything at once:

```bash
docker compose up -d
```

## URLs

| Service    | URL                         | Credentials   |
|------------|-----------------------------|---------------|
| Grafana    | http://localhost:3000        | admin / admin |
| Loki (API) | http://localhost:3100/ready  | —             |

## React app config

In `ecommerce-app/.env` — no change needed.
The logger already uses `VITE_API_BASE_URL` and posts to `/api/logs`:

```
VITE_API_BASE_URL=https://localhost:56437
VITE_APP_ENV=development
```

## .NET API wiring

See the `dotnet-logging/` folder for the files to add to your API project:

| File                        | What it does                                      |
|-----------------------------|---------------------------------------------------|
| `SerilogSetup.cs`           | Configures Serilog → Console + Loki sink          |
| `LogEntryDto.cs`            | DTO matching the React logger JSON shape          |
| `LogsEndpoint.cs`           | `POST /api/logs` minimal API endpoint             |
| `Program.cs.snippet`        | Shows exactly where to add calls in Program.cs   |
| `appsettings.snippet.jsonc` | Loki URL config block                             |
| `install-packages.sh`       | NuGet packages to install                         |

## Useful LogQL queries in Grafana

```logql
# All logs from both frontend and backend
{app=~"ecommerce-ui|ecommerce-api"}

# Frontend errors only
{app="ecommerce-ui", level="error"}

# Backend errors only
{app="ecommerce-api", level="error"}

# Web vitals
{app="ecommerce-ui"} |= "web-vital"

# Correlate by timestamp — see what the API was doing when the browser errored
{app=~"ecommerce-ui|ecommerce-api"} | json
```

## Stop the stack

```bash
docker compose down

# Remove all stored log data
docker compose down -v
```
