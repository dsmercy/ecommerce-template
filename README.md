# Ecommerce Platform — Full Stack

A production-ready ecommerce platform built with a **React** frontend, **ASP.NET Core 8** modular monolith API, and a containerised infrastructure stack (MySQL, Redis, Meilisearch, Azurite, Loki, Grafana).

---

## Repository Structure

```
/
├── ecommerce/          # ASP.NET Core 8 Web API (modular monolith)
├── ecommerce-app/      # React frontend (Vite)
└── docker/             # Docker Compose + infrastructure config
    ├── docker-compose.yml
    ├── schema.sql
    └── dotnet-logging/ # Serilog + Loki wiring snippets
```

---

## Tech Stack

### Backend

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Database | MySQL 8 via Pomelo EF Core |
| ORM | Entity Framework Core 8 |
| Caching | Redis (StackExchange.Redis) |
| Search | Meilisearch |
| File Storage | Azure Blob Storage / Azurite (local) |
| Authentication | JWT Bearer + Refresh Token rotation |
| Validation | FluentValidation |
| Logging | Serilog → Console + Loki sink |
| Tracing | .NET ActivitySource / W3C TraceContext |

### Frontend

| Concern | Technology |
|---|---|
| Framework | React (Vite) |
| API Calls | Axios (with interceptor-based token refresh) |
| Environment | `.env` via `VITE_API_BASE_URL` |

### Observability

| Service | Role |
|---|---|
| Loki | Log storage and query engine |
| Grafana | Dashboard UI (LogQL queries) |
| Serilog | Structured logging from both API and browser (forwarded via `/api/logs`) |

---

## Infrastructure

### Services & Ports

| Service | Port | Notes |
|---|---|---|
| MySQL | 3306 | Database: `ecommerce_db`, user: `root` / `root` |
| Redis | 6379 | Cart and product caching |
| Meilisearch | 7700 | Full-text product search |
| Azurite | 10000–10002 | Azure Storage emulator (blob, queue, table) |
| Loki | 3100 | Log ingestion and query API |
| Grafana | 3000 | Log dashboard — `admin` / `admin` |

### Starting the Stack

```bash
# Full stack (all services)
docker compose up -d

# Logging stack only
docker compose up -d loki grafana
```

### Stopping

```bash
docker compose down

# Also remove all stored log data
docker compose down -v
```

---

## Quick Start

### 1. Start infrastructure

```bash
cd docker
docker compose up -d
```

### 2. Apply the database migration

```bash
mysql -u root -p ecommerce_db < ecommerce/migrations/add_refresh_token_columns.sql
```

### 3. Configure the API

Create `ecommerce/src/Ecommerce.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=ecommerce_db;User=root;Password=root;CharSet=utf8mb4;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "DEV_SECRET_KEY_MINIMUM_32_CHARACTERS_LONG_HERE",
    "Issuer": "EcommerceAPI",
    "Audience": "EcommerceClients",
    "ExpiryMinutes": "60",
    "RefreshTokenLifetimeDays": "7"
  },
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "ecommerce-media-dev"
  },
  "Meilisearch": {
    "Host": "http://localhost:7700",
    "ApiKey": "dev_master_key"
  }
}
```

### 4. Run the API

```bash
cd ecommerce/src/Ecommerce.API
dotnet run
```

- Swagger UI: `https://localhost:56437/swagger`
- Health check: `https://localhost:56437/health`

### 5. Configure and run the frontend

Create `ecommerce-app/.env`:

```env
VITE_API_BASE_URL=https://localhost:56437
VITE_APP_ENV=development
```

```bash
cd ecommerce-app
npm install
npm run dev
```

---

## API Reference

### Authentication

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register — returns access + refresh token |
| POST | `/api/auth/login` | Public | Login — returns access + refresh token |
| POST | `/api/auth/refresh` | Public | Exchange expired access token for a new pair |
| POST | `/api/auth/revoke` | User | Logout — invalidates the refresh token |

**Login / Register response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "name": "Jane Smith",
    "email": "jane@example.com",
    "role": "CUSTOMER",
    "accessToken": "eyJ...",
    "refreshToken": "aGVs...",
    "refreshTokenExpiry": "2026-03-23T01:07:00Z"
  }
}
```

---

### Products

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/products` | Public | Paginated list — reads from `v_product_catalogue` view |
| GET | `/api/products/{id}` | Public | Full detail with variants, images, inventory |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/{id}` | Admin | Update product |
| DELETE | `/api/products/{id}` | Admin | Soft-delete + remove from Meilisearch |
| POST | `/api/products/{id}/variants` | Admin | Add SKU / colour / size variant |
| POST | `/api/products/{id}/images` | Admin | Upload image (JPEG/PNG/WebP, max 10 MB) |

**Filter query params:** `categoryId`, `minPrice`, `maxPrice`, `brand`, `isActive`, `search`, `page`, `pageSize`

---

### Cart

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/cart` | User | Get cart — served from Redis cache (10 min TTL) |
| POST | `/api/cart/items` | User | Add item — checks live inventory |
| PUT | `/api/cart/items/{itemId}` | User | Update quantity |
| DELETE | `/api/cart/items/{itemId}` | User | Remove item |
| DELETE | `/api/cart` | User | Clear all items |

---

### Orders

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/orders` | User | Create from cart — reserves stock via `sp_reserve_stock` |
| GET | `/api/orders` | User | Paginated order history |
| GET | `/api/orders/{id}` | User | Order detail from `v_order_summary` |
| GET | `/api/orders/admin/summary` | Admin | All orders via `v_order_summary` |
| PATCH | `/api/orders/{id}/status` | Admin | Update status — `CANCELLED` releases stock reservation |

**Order statuses:** `PENDING → PAID → SHIPPED → DELIVERED / CANCELLED`

---

### Payments

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/payments` | User | Record a payment for an order |
| PATCH | `/api/payments/{id}/status` | Admin | `COMPLETED` deducts stock; `FAILED` releases reservation |
| GET | `/api/payments/order/{orderId}` | User | All payments for an order |

**Payment statuses:** `PENDING → COMPLETED / FAILED / REFUNDED`

---

### Other Endpoints

| Resource | Endpoints | Notes |
|---|---|---|
| Reviews | `GET /api/reviews/product/{id}`, `POST /api/reviews` | One per product per user, rating 1–5 |
| Coupons | `GET/POST /api/coupons`, `POST /api/coupons/validate` | Types: `PERCENTAGE`, `FLAT` |
| Categories | `GET/POST/PUT/DELETE /api/categories[/{id}]` | Nested parent/child tree |
| Inventory | `GET /api/inventory/variant/{id}`, `GET /api/inventory/product/{id}`, `PUT /api/inventory/variant/{id}` | Reads `v_inventory_available` |
| Search | `GET /api/search/products`, `POST /api/search/sync` | Full-text via Meilisearch |
| Health | `GET /health` | Public |

---

## Architecture

### Clean Architecture Layers

```
Domain ← Application ← Infrastructure ← API
```

The Application layer defines interfaces (`IProductQueryService`, `IInventoryQueryService`, `IOrderQueryService`) — Infrastructure implements them. Business logic never references Infrastructure directly.

### Project Structure (API)

```
ecommerce/src/
├── Ecommerce.API
│   ├── Extensions/        # Serilog pipeline, JWT + Swagger DI
│   ├── Logging/           # W3C trace and request context enrichers
│   ├── Middleware/        # Security, correlation IDs, exception handling, request logging
│   └── Modules/           # Auth, Cart, Categories, Coupons, Inventory, Orders,
│                          #   Payments, Products, Reviews, Search
├── Ecommerce.Application
│   ├── Common/            # Interfaces, ApiResponse<T>, PagedResult<T>
│   └── Modules/           # DTOs, services, validators per bounded context
├── Ecommerce.Domain
│   ├── Entities/          # Core entities + EF view entities
│   ├── Enums/
│   └── Interfaces/        # IRepository<T>, IUnitOfWork, stored procedure interfaces
└── Ecommerce.Infrastructure
    ├── Cache/             # RedisCacheService
    ├── Data/              # AppDbContext + Fluent API configs
    ├── QueryServices/     # View-backed read service implementations
    ├── Repositories/      # Repository, UnitOfWork, stored procedure wrappers
    └── Services/          # BlobStorage, CurrentUser, Meilisearch, TokenService
```

### MySQL Views

| View | Used by | Purpose |
|---|---|---|
| `v_product_catalogue` | `IProductQueryService` | Active products + category + primary image |
| `v_inventory_available` | `IInventoryQueryService` | `stock_quantity - reserved_quantity` per variant |
| `v_order_summary` | `IOrderQueryService` | Orders joined with user, coupon, and payment status |

### Inventory Stored Procedures

| Procedure | Triggered when | Effect |
|---|---|---|
| `sp_reserve_stock` | Order placed | Increments `reserved_quantity` |
| `sp_deduct_stock` | Payment completed | Decrements `stock_quantity` and `reserved_quantity` |
| `sp_release_reservation` | Order cancelled / payment failed | Decrements `reserved_quantity` only |

If any item fails to reserve during order creation, all previously reserved items are released before returning an error.

### Caching Strategy

| Entity | Cache key | TTL | Invalidated on |
|---|---|---|---|
| Cart | `cart:{userId}` | 10 min | Add / update / remove / clear |
| Product detail | `product:{id}` | 15 min | Update / delete / variant add / image upload |

---

## Middleware Pipeline

```
SecurityHeadersMiddleware          ← outermost — headers on every response
  └── CorrelationIdMiddleware      ← assigns X-Correlation-Id → Serilog LogContext
        └── ExceptionHandlingMiddleware
              └── RequestBodyLoggingMiddleware   ← Trace level only, 32 KB cap
                    └── RequestLoggingMiddleware ← one-line request/response summary
                          └── Swagger / HTTPS / CORS / Auth / Controllers
```

### Security Headers

| Header | Value |
|---|---|
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | camera, mic, geo, payment all disabled |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |

`Server`, `X-Powered-By`, `X-AspNet-Version`, and `X-AspNetMvc-Version` headers are stripped from every response.

---

## Structured Logging

All log entries carry `CorrelationId`, `TraceId`, `SpanId`, and `UserId`.

### Log Sinks

| Sink | Path | Level | Retention |
|---|---|---|---|
| Trace | `logs/trace-YYYYMMDD.log` | Verbose / Debug | 7 days |
| App | `logs/app-YYYYMMDD.log` | Information / Warning | 14 days |
| Error | `logs/errors-YYYYMMDD.log` | Error / Fatal | 60 days |

### Browser Log Forwarding

React app logs are forwarded to Loki **through the .NET API** via `POST /api/logs` — no separate log receiver service is needed.

See `docker/dotnet-logging/` for the wiring files to add to the API project:

| File | Purpose |
|---|---|
| `SerilogSetup.cs` | Configures Serilog → Console + Loki sink |
| `LogEntryDto.cs` | DTO matching the React logger JSON shape |
| `LogsEndpoint.cs` | `POST /api/logs` minimal API endpoint |
| `Program.cs.snippet` | Where to add calls in `Program.cs` |
| `appsettings.snippet.jsonc` | Loki URL config block |
| `install-packages.sh` | NuGet packages to install |

### Useful LogQL Queries (Grafana)

```logql
# All logs from both frontend and backend
{app=~"ecommerce-ui|ecommerce-api"}

# Frontend errors only
{app="ecommerce-ui", level="error"}

# Backend errors only
{app="ecommerce-api", level="error"}

# Web vitals
{app="ecommerce-ui"} |= "web-vital"

# Correlate by timestamp
{app=~"ecommerce-ui|ecommerce-api"} | json
```

---

## Refresh Token System

Tokens are issued in pairs on every login and register. The access token is a short-lived signed JWT; the refresh token is a 64-byte cryptographically random value stored as a BCrypt hash.

**Key behaviours:**

- **Rotation** — every `/refresh` call issues a new pair and invalidates the old one.
- **Hash-only storage** — the raw token never touches the database.
- **Signature verification on refresh** — validates the JWT signature before trusting the user ID it contains.
- **Ownership check on revoke** — `/revoke` verifies the submitted token against the authenticated user's stored hash.

**Axios interceptor pattern (frontend):**
```js
api.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status !== 401 || original._retry) return Promise.reject(error);
    original._retry = true;
    const { data } = await axios.post('/api/auth/refresh', { accessToken, refreshToken });
    saveTokens(data.data);
    original.headers.Authorization = `Bearer ${data.data.accessToken}`;
    return api(original);
  }
);
```

Queue concurrent requests during an in-flight refresh to avoid multiple simultaneous refresh calls each consuming the previous rotation's token.

---

## Response Envelope

All endpoints return `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Success",
  "data": { ... },
  "errors": null
}
```

Paginated endpoints return `PagedResult<T>` inside `data`, with `totalCount`, `page`, `pageSize`, `totalPages`, `hasNext`, and `hasPrevious`.

---

## Exception Handling

`ExceptionHandlingMiddleware` maps all unhandled exceptions to HTTP responses:

| Exception | Status | Log level |
|---|---|---|
| `ValidationException` | 400 | Warning |
| `UnauthorizedAccessException` | 401 | Warning |
| `KeyNotFoundException` | 404 | Warning |
| All others | 500 | Error |

500 responses contain only the correlation ID — no internal details are leaked to the client.

---

## Configuration Reference

```json
{
  "JwtSettings": {
    "Secret": "minimum 32 characters",
    "Issuer": "EcommerceAPI",
    "Audience": "EcommerceClients",
    "ExpiryMinutes": "60",
    "RefreshTokenLifetimeDays": "7"
  },
  "Logging": {
    "Trace": { "Path": "logs/trace-.log", "RetainedFileCountLimit": 7 },
    "App":   { "Path": "logs/app-.log",   "RetainedFileCountLimit": 14 },
    "Error": { "Path": "logs/errors-.log","RetainedFileCountLimit": 60 }
  },
  "Loki": {
    "Uri": "http://localhost:3100"
  }
}
```

**Connection pool tuning** (append to connection string):
```
Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;Connection Timeout=15;
```

---

## Migrating to Microservices

Each module under `Ecommerce.Application/Modules/` maps 1:1 to a potential microservice. To extract a module:

1. Move the module's services, DTOs, and validators to a new project.
2. Replace direct service calls with message-based communication (e.g. MassTransit + RabbitMQ).
3. Scope a new `DbContext` to that module's tables only.
4. Replace `IRepository<>` calls with HTTP clients or event handlers.
5. Extract the module's SQL views and stored procedures to the new service's database.

The view-backed query services and stored procedure wrappers are already fully isolated behind interfaces, making extraction straightforward.
