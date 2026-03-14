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
| Authentication | JWT Bearer |
| Validation | FluentValidation |
| Logging | Serilog (console + rolling files) |
| Local Storage | Azurite (Azure Storage emulator) |

---

## Project Structure

```
src/
├── Ecommerce.API              # Controllers, middleware, DI wiring
│   └── Modules/               # One folder per bounded context
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
├── Ecommerce.Application      # Use cases, services, DTOs, validators
│   ├── Common/                # Shared models, interfaces, behaviors
│   └── Modules/               # Per-module services, DTOs, validators
│
├── Ecommerce.Domain           # Entities, enums, repository interfaces
│
└── Ecommerce.Infrastructure   # EF Core, Redis, Blob, Meilisearch impls
    ├── Data/
    │   └── Configurations/    # Fluent API entity configs
    ├── Cache/
    ├── Repositories/
    └── Services/
```

---

## Quick Start

### 1. Start Infrastructure (Docker)

```bash
docker-compose up -d
```

This starts MySQL, Redis, Meilisearch, and Azurite locally.

### 2. Configure `appsettings.Development.json`

Update connection strings and secrets as needed.  
For Azure Blob Storage locally use the Azurite connection string:

```
UseDevelopmentStorage=true
```

### 3. Run the API

```bash
cd src/Ecommerce.API
dotnet run
```

Swagger UI: [https://localhost:5001/swagger](https://localhost:5001/swagger)

---

## API Reference

### Authentication

| Method | Endpoint | Auth |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |

### Products

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/products` | Public |
| GET | `/api/products/{id}` | Public |
| POST | `/api/products` | Admin |
| PUT | `/api/products/{id}` | Admin |
| DELETE | `/api/products/{id}` | Admin |
| POST | `/api/products/{id}/variants` | Admin |
| POST | `/api/products/{id}/images` | Admin |

### Cart

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/cart` | User |
| POST | `/api/cart/items` | User |
| PUT | `/api/cart/items/{itemId}` | User |
| DELETE | `/api/cart/items/{itemId}` | User |
| DELETE | `/api/cart` | User |

### Orders

| Method | Endpoint | Auth |
|---|---|---|
| POST | `/api/orders` | User |
| GET | `/api/orders` | User |
| GET | `/api/orders/{id}` | User |
| PATCH | `/api/orders/{id}/status` | Admin |

### Payments

| Method | Endpoint | Auth |
|---|---|---|
| POST | `/api/payments` | User |
| PATCH | `/api/payments/{id}/status` | Admin |
| GET | `/api/payments/order/{orderId}` | User |

### Reviews

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/reviews/product/{productId}` | Public |
| POST | `/api/reviews` | User |

### Coupons

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/coupons` | Admin |
| POST | `/api/coupons` | Admin |
| POST | `/api/coupons/validate` | User |

### Categories

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/categories` | Public |
| GET | `/api/categories/{id}` | Public |
| POST | `/api/categories` | Admin |
| PUT | `/api/categories/{id}` | Admin |
| DELETE | `/api/categories/{id}` | Admin |

### Inventory

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/inventory/variant/{variantId}` | Public |
| PUT | `/api/inventory/variant/{variantId}` | Admin |

### Search

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/search/products?query=&categoryId=&minPrice=&maxPrice=` | Public |
| POST | `/api/search/sync` | Admin |

---

## Architecture Decisions

- **Soft Delete** — `users` and `products` tables use `deleted_at`. EF Core global query filters exclude soft-deleted rows automatically.
- **Inventory Reservation** — `reserved_quantity` is incremented when an order is placed and decremented + `stock_quantity` reduced when payment completes.
- **Cart Caching** — Cart data is cached in Redis per user (10 min TTL) and invalidated on mutation.
- **Product Caching** — Individual products cached in Redis (15 min TTL), invalidated on update/delete.
- **Search Sync** — Products are indexed to Meilisearch on create/update and removed on soft-delete. Full re-sync available via `POST /api/search/sync`.
- **Order Snapshot** — `order_items` stores `sku`, `product_name`, and `price` at purchase time to preserve history even if the product changes later.

---

## Logs

- `logs/app-YYYYMMDD.log` — All logs (Info+), rolling daily, retained 30 days
- `logs/errors-YYYYMMDD.log` — Error logs only, retained 60 days

---

## Converting to Microservices

Each module under `Ecommerce.Application/Modules/` maps 1:1 to a potential microservice. To extract a module:

1. Move the module's service + DTOs + validators into a new project.
2. Replace direct service calls with message-based communication (e.g., MassTransit + RabbitMQ).
3. Give the new service its own DbContext scoped to its tables.
4. Replace the shared `IRepository<>` calls with HTTP clients or event handlers.
