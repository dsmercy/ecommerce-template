# Ecommerce API — Modular Monolith

A production-ready E-commerce Web API built with **ASP.NET Core 8**, **Clean Architecture**, and a **Modular Monolith** design.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Database | MySQL 8 via Pomelo EF Core |
| ORM | Entity Framework Core 8 |
| Caching | Redis (StackExchange.Redis) |
| Search | Meilisearch |
| File Storage | Azure Blob Storage |
| Authentication | JWT Bearer + Refresh Token rotation |
| Validation | FluentValidation |
| Logging | Serilog — structured, three rolling file sinks |
| Tracing | .NET ActivitySource / W3C TraceContext |
| Local Storage | Azurite (Azure Storage emulator) |

---

## Project Structure

```
ecommerce/
├── migrations/
│   └── add_refresh_token_columns.sql
└── src/
    ├── Ecommerce.API
    │   ├── Extensions/
    │   │   ├── LoggingExtensions.cs       # Serilog three-sink pipeline
    │   │   └── ServiceExtensions.cs       # JWT + Swagger DI helpers
    │   ├── Logging/
    │   │   ├── ActivityEnricher.cs        # W3C TraceId/SpanId → every log entry
    │   │   └── RequestContextEnricher.cs  # UserId/CorrelationId → every log entry
    │   ├── Middleware/
    │   │   ├── CorrelationIdMiddleware.cs       # Assigns + propagates correlation IDs
    │   │   ├── ExceptionHandlingMiddleware.cs   # Global error handling + enrichment
    │   │   ├── RequestBodyLoggingMiddleware.cs  # Trace-level body capture (32 KB cap)
    │   │   ├── RequestLoggingMiddleware.cs      # Request/response summary logging
    │   │   └── SecurityHeadersMiddleware.cs     # CSP, HSTS, X-Frame-Options, etc.
    │   └── Modules/                        # One folder per bounded context
    │       ├── Auth/
    │       ├── Cart/
    │       ├── Categories/
    │       ├── Coupons/
    │       ├── Inventory/
    │       ├── Orders/
    │       ├── Payments/
    │       ├── Products/
    │       ├── Reviews/
    │       └── Search/
    │
    ├── Ecommerce.Application
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IQueryServices.cs      # IProductQueryService, IInventoryQueryService, IOrderQueryService
    │   │   │   └── IServices.cs           # ICacheService, IBlobStorageService, ITokenService, ICurrentUserService
    │   │   └── Models/
    │   │       └── ApiResponse.cs         # ApiResponse<T>, PagedResult<T>, PaginationParams
    │   └── Modules/
    │       ├── Auth/
    │       │   ├── DTOs/AuthDTOs.cs
    │       │   ├── Services/AuthService.cs
    │       │   └── Validators/AuthValidators.cs
    │       ├── Cart/CartModule.cs
    │       ├── Categories/CategoriesModule.cs
    │       ├── Coupons/CouponsModule.cs
    │       ├── Inventory/InventoryModule.cs
    │       ├── Orders/OrderModule.cs
    │       ├── Payments/PaymentsModule.cs
    │       ├── Products/
    │       │   ├── DTOs/ProductDTOs.cs
    │       │   ├── Services/ProductService.cs
    │       │   └── Validators/ProductValidators.cs
    │       ├── Reviews/ReviewsModule.cs
    │       └── Search/  (interface only — impl in Infrastructure)
    │
    ├── Ecommerce.Domain
    │   ├── Entities/
    │   │   ├── Address.cs, BaseEntity.cs, Cart.cs, Category.cs
    │   │   ├── Coupon.cs, Order.cs, Payment.cs, Product.cs
    │   │   ├── ProductVariant.cs, Review.cs, User.cs, ViewEntities.cs
    │   ├── Enums/Enums.cs
    │   └── Interfaces/
    │       ├── IInventoryProcedures.cs    # sp_reserve_stock / sp_deduct_stock / sp_release_reservation
    │       └── IRepository.cs             # IRepository<T>, IUnitOfWork
    │
    └── Ecommerce.Infrastructure
        ├── Cache/
        │   └── RedisCacheService.cs
        ├── Data/
        │   ├── AppDbContext.cs
        │   └── Configurations/            # Fluent API entity + view configs
        ├── QueryServices/
        │   └── QueryServiceImplementations.cs  # View-backed read services
        ├── Repositories/
        │   ├── InventoryProcedures.cs     # MySQL stored procedure wrappers
        │   ├── Repository.cs
        │   └── UnitOfWork.cs
        ├── Services/
        │   ├── BlobStorageService.cs
        │   ├── CurrentUserService.cs
        │   ├── MeilisearchService.cs
        │   └── TokenService.cs            # JWT generation + refresh token helpers
        └── InfrastructureServiceRegistration.cs
```

---

## Quick Start

### 1. Start infrastructure (Docker)

```bash
docker-compose up -d
```

Starts MySQL 8, Redis, Meilisearch, and Azurite locally.

### 2. Apply the database migration

```bash
mysql -u root -p ecommerce_db < migrations/add_refresh_token_columns.sql
```

### 3. Configure `appsettings.Development.json`

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
cd src/Ecommerce.API
dotnet run
```

Swagger UI: `https://localhost:56437/swagger`
Health check: `https://localhost:56437/health`

---

## API Reference

### Authentication

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register — returns access token + refresh token |
| POST | `/api/auth/login` | Public | Login — returns access token + refresh token |
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

**Refresh request:**
```json
{ "accessToken": "eyJ...", "refreshToken": "aGVs..." }
```

**Revoke request:**
```json
{ "refreshToken": "aGVs..." }
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
| POST | `/api/products/{id}/variants` | Admin | Add SKU/color/size variant |
| POST | `/api/products/{id}/images` | Admin | Upload image (JPEG/PNG/WebP, max 10 MB) |

**Product filter query params:** `categoryId`, `minPrice`, `maxPrice`, `brand`, `isActive`, `search`, `page`, `pageSize`

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
| GET | `/api/orders/{id}` | User | Order detail enriched from `v_order_summary` |
| GET | `/api/orders/admin/summary` | Admin | All orders via `v_order_summary` — no application-side joins |
| PATCH | `/api/orders/{id}/status` | Admin | Update status — `CANCELLED` releases stock reservation |

**Order statuses:** `PENDING` → `PAID` → `SHIPPED` → `DELIVERED` / `CANCELLED`

---

### Payments

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/payments` | User | Record a payment for an order |
| PATCH | `/api/payments/{id}/status` | Admin | Update status — `COMPLETED` deducts stock via `sp_deduct_stock`; `FAILED` releases reservation |
| GET | `/api/payments/order/{orderId}` | User | All payments for an order |

**Payment statuses:** `PENDING` → `COMPLETED` / `FAILED` / `REFUNDED`

---

### Reviews

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/reviews/product/{productId}` | Public | Paginated reviews |
| POST | `/api/reviews` | User | Submit review (one per product per user, rating 1–5) |

---

### Coupons

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/coupons` | Admin | List all coupons |
| POST | `/api/coupons` | Admin | Create coupon |
| POST | `/api/coupons/validate` | User | Check a code against an order amount |

**Discount types:** `PERCENTAGE`, `FLAT`

---

### Categories

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/categories` | Public | Full tree (root categories + nested children) |
| GET | `/api/categories/{id}` | Public | Single category |
| POST | `/api/categories` | Admin | Create category (supports parent/child hierarchy) |
| PUT | `/api/categories/{id}` | Admin | Update name or parent |
| DELETE | `/api/categories/{id}` | Admin | Delete category |

---

### Inventory

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/inventory/variant/{variantId}` | Public | Stock for a single variant (reads `v_inventory_available`) |
| GET | `/api/inventory/product/{productId}` | Public | Stock for all variants of a product |
| PUT | `/api/inventory/variant/{variantId}` | Admin | Manual stock override (warehouse recount) |

---

### Search

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/search/products` | Public | Full-text search via Meilisearch |
| POST | `/api/search/sync` | Admin | Re-index all active products |

**Search query params:** `query`, `categoryId`, `minPrice`, `maxPrice`, `page`, `pageSize`

---

### Health

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/health` | Public |

---

## Middleware Pipeline

Middleware executes in this order on every request:

```
SecurityHeadersMiddleware        ← outermost — headers on every response including errors
  └── CorrelationIdMiddleware    ← assigns X-Correlation-Id, pushes into Serilog LogContext
        └── ExceptionHandlingMiddleware  ← catches all unhandled exceptions
              └── RequestBodyLoggingMiddleware  ← buffers + logs body at Trace level
                    └── RequestLoggingMiddleware  ← one-line request/response summary
                          └── Swagger / HTTPS / CORS / Auth / Controllers
```

---

## Security Headers

Every response includes the following headers, set by `SecurityHeadersMiddleware`:

| Header | Value |
|---|---|
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | camera, mic, geo, payment all disabled |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |

The `Server`, `X-Powered-By`, `X-AspNet-Version`, and `X-AspNetMvc-Version` headers are stripped from every response.

> If you expose Swagger in production, relax CSP to allow `script-src 'self' 'unsafe-inline'` and `style-src 'self' 'unsafe-inline'`.

---

## Structured Logging

Three rolling file sinks — all entries carry `CorrelationId`, `TraceId`, `SpanId`, and `UserId`:

| Sink | Path | Level | Retention |
|---|---|---|---|
| Trace | `logs/trace-YYYYMMDD.log` | Verbose/Debug | 7 days |
| App | `logs/app-YYYYMMDD.log` | Information/Warning | 14 days |
| Error | `logs/errors-YYYYMMDD.log` | Error/Fatal | 60 days |

**Log entry format:**
```
2026-03-16 01:07:43.123 +00:00 [INF] [a1b2c3d4] [T:00-abc...] [U:42] → POST /api/orders
```

**Global enrichers:**

- `ActivityEnricher` — reads `Activity.Current` and injects `TraceId`, `SpanId`, `ParentSpanId` (W3C format) into every entry. Compatible with Jaeger, Zipkin, AWS X-Ray, Application Insights.
- `RequestContextEnricher` — injects `UserId`, `CorrelationId`, `RequestPath`, `RequestMethod` into every entry. Registered as a singleton; resolves `IHttpContextAccessor` per log event.

**Request body logging** (`RequestBodyLoggingMiddleware`):
- Only active when Trace/Verbose level is enabled — no buffering overhead in production.
- Logs `application/json`, `application/x-www-form-urlencoded`, and `text/plain` content types.
- 32 KB body cap — larger bodies emit a truncation notice.
- Skips GET/HEAD/DELETE/OPTIONS, all multipart/binary content types, and image uploads.

**Correlation ID resolution order:**
1. `X-Correlation-Id` request header (caller propagation)
2. W3C `traceparent` header trace-id segment
3. New 8-char short GUID (fallback)

---

## Refresh Token System

Tokens are issued in pairs on every login and register. The access token is a short-lived signed JWT; the refresh token is a 64-byte cryptographically random value stored as a BCrypt hash.

**Token storage on the `users` table:**

| Column | Type | Description |
|---|---|---|
| `refresh_token_hash` | `VARCHAR(255)` | BCrypt hash (work factor 11) of the raw token |
| `refresh_token_expiry` | `DATETIME` | Configurable lifetime, default 7 days |

**Key behaviours:**
- **Rotation** — every `/refresh` call issues a new pair and invalidates the old one. A stolen token can only be used once.
- **Hash-only storage** — the raw token never touches the database. A compromised database exposes nothing usable.
- **Signature verification on refresh** — `GetPrincipalFromExpiredToken` validates the JWT signature with `ValidateLifetime = false`, confirming the token was issued by this server before trusting the user ID it contains.
- **Ownership check on revoke** — `/revoke` verifies the submitted token against the authenticated user's stored hash before clearing it.

**UI integration — Axios interceptor pattern:**
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

## Architecture Decisions

**Clean Architecture layers**

Dependency flow: `Domain ← Application ← Infrastructure ← API`. The Application layer never references Infrastructure types directly — it defines interfaces (`IProductQueryService`, `IInventoryQueryService`, `IOrderQueryService`) that Infrastructure implements. This keeps business logic testable in isolation.

**View-backed read services**

Three MySQL views are mapped as keyless EF Core entities:

| View | Used by | Purpose |
|---|---|---|
| `v_product_catalogue` | `IProductQueryService` | Active products + category + primary image URL |
| `v_inventory_available` | `IInventoryQueryService` | `stock_quantity - reserved_quantity` per variant |
| `v_order_summary` | `IOrderQueryService` | Orders joined with user, coupon, and payment status |

Application code reads these views through interfaces — no `AppDbContext` references in the Application layer.

**Inventory stored procedures**

Three MySQL stored procedures handle concurrent inventory mutations atomically:

| Procedure | Called when | Effect |
|---|---|---|
| `sp_reserve_stock` | Order placed | Increments `reserved_quantity` |
| `sp_deduct_stock` | Payment completed | Decrements both `stock_quantity` and `reserved_quantity` |
| `sp_release_reservation` | Order cancelled / payment failed | Decrements `reserved_quantity` only |

If reservation fails for any item during order creation, all previously reserved items are released before returning an error.

**Soft delete**

`users` and `products` use `deleted_at`. EF Core global query filters (`HasQueryFilter`) exclude soft-deleted rows automatically. Products are also removed from the Meilisearch index on soft-delete.

**Caching strategy**

| Entity | Cache key | TTL | Invalidated on |
|---|---|---|---|
| Cart | `cart:{userId}` | 10 min | Add / update / remove / clear |
| Product detail | `product:{id}` | 15 min | Update / delete / variant add / image upload |

**Order snapshot**

`order_items` stores `sku`, `product_name`, `color`, `size`, `unit_price`, and `line_total` at purchase time. Order history remains accurate even if the product is later updated or deleted.

**Search sync**

Products are indexed to Meilisearch on create and update, and removed on soft-delete. A full re-sync is available via `POST /api/search/sync` — run this after first deploy, after seeding, or after schema changes. Index settings (searchable, filterable, sortable attributes) are applied before any documents are added via `EnsureIndexSettingsAsync`.

**Response envelope**

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

**Exception handling**

`ExceptionHandlingMiddleware` catches all unhandled exceptions and maps them to HTTP responses:

| Exception | Status | Log level |
|---|---|---|
| `ValidationException` | 400 | Warning |
| `UnauthorizedAccessException` | 401 | Warning |
| `KeyNotFoundException` | 404 | Warning |
| All others | 500 | Error |

Every error log entry includes `CorrelationId`, `TraceId`, `SpanId`, `UserId`, `RemoteIp`, and `ExceptionType`. The 500 response body contains only the correlation ID as a reference — no internal details leak to the client.

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
    "Trace":  { "Path": "logs/trace-.log",  "RetainedFileCountLimit": 7,  "OutputTemplate": "..." },
    "App":    { "Path": "logs/app-.log",    "RetainedFileCountLimit": 14, "OutputTemplate": "..." },
    "Error":  { "Path": "logs/errors-.log", "RetainedFileCountLimit": 60, "OutputTemplate": "..." }
  }
}
```

Connection pool tuning (add to the connection string):
```
Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;Connection Timeout=15;
```

---

## Converting to Microservices

Each module under `Ecommerce.Application/Modules/` maps 1:1 to a potential microservice. To extract a module:

1. Move the module's service, DTOs, and validators into a new project.
2. Replace direct service calls with message-based communication (e.g. MassTransit + RabbitMQ).
3. Give the new service its own `DbContext` scoped to its tables only.
4. Replace `IRepository<>` calls with HTTP clients or event handlers.
5. Extract the module's SQL views and stored procedures to the new service's database.

The view-backed query services and stored procedure wrappers are already fully isolated behind interfaces, making extraction straightforward.