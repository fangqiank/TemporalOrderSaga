# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Temporal.io Saga pattern implementation for order processing in .NET 10, with a complete shopping experience (WebAPI + SPA frontend). Demonstrates the compensating transactions pattern using Temporal workflows to orchestrate inventory reservation, payment authorization, and order completion with automatic rollback on failures.

## Build & Run Commands

```powershell
# Restore and build
dotnet build

# Start Temporal infrastructure (required before running workflows)
docker-compose up -d

# Start Worker (terminal 1)
dotnet run --project OrderSaga.Workflows

# Start WebAPI + Frontend (terminal 2)
dotnet run --project Client
```

Temporal UI at `http://localhost:8080`. Shopping page at `http://localhost:5283`. Temporal gRPC on `localhost:7233`.

## Architecture

**Solution structure** (`.slnx` format):

- `OrderSaga.Contracts` — Shared message/record types: `OrderInput`, `OrderResult`, `CreateOrderRequest`, `OrderStatus` enum. No external dependencies.
- `OrderSaga.Workflows` — Temporal Worker host (ASP.NET Core) with `OrderSagaWorkflow` and `OrderSagaActivities`. References Contracts and `Temporalio` NuGet (v1.14.0).
- `Client` — ASP.NET Core WebAPI serving SPA frontend from `wwwroot/`. Uses EF Core + SQLite for product catalog, TemporalClient to start/query workflows.

**Saga pattern flow:**
1. Reserve inventory (10% random failure, 3x retry)
2. Authorize payment (20% random failure, 3x retry)
3. Send confirmation email (5x retry)
4. On failure: execute compensations in reverse order via LIFO stack (VoidPayment → ReleaseInventory)

**API endpoints:**
- `GET /api/products` — List all products
- `GET /api/products/{id}` — Get single product
- `POST /api/orders` — Create order (starts Temporal workflow)
- `GET /api/orders/{workflowId}` — Query order status

## Notes

- All activities are simulated with `Task.Delay` and random failures — no real database/payment/email integrations
- Product catalog seeded in SQLite with 10 items across 5 categories
- Frontend is pure HTML/CSS/JS in wwwroot, no build tools required
