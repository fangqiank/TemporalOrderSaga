# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Temporal.io Saga pattern implementation for order processing in .NET 10. Demonstrates the compensating transactions pattern using Temporal workflows to orchestrate inventory reservation, payment authorization, and order completion with automatic rollback on failures.

## Build & Run Commands

```powershell
# Restore and build
dotnet build

# Run all tests (when test project exists)
dotnet test

# Run single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Start Temporal infrastructure (required before running workflows)
docker-compose up -d
```

Temporal UI available at `http://localhost:8080` after docker-compose starts. Temporal gRPC on `localhost:7233`.

## Architecture

**Solution structure** (`.slnx` format):

- `OrderSaga.Contracts` — Shared message/record types: `OrderInput`, `OrderResult`, `ReserveInventoryInput`, `PaymentInput`, and `OrderStatus` enum. No external dependencies.
- `OrderSaga.Workflow` — Temporal workflow definition. References Contracts and `Temporalio` NuGet (v1.14.0).

**Saga pattern flow:**
1. Reserve inventory
2. Authorize payment
3. Complete order
4. On failure: execute compensations in reverse order via `_compensations` stack

**Key types:**
- `IOrderSagaWorkflow` — Workflow interface with `ExecuteAsync`, `CustomerCancelledAsync`, `GetCurrentStatus`
- `OrderSagaWorkflow` — Partially implemented workflow; `ExecuteAsync` is incomplete (stops mid-activity call)
- Activity execution uses `Workflow.ExecuteActivityAsync` with `ActivityOptions` (5s `StartToCloseTimeout`)

**Infrastructure:** PostgreSQL 16 + Temporal 1.25.2 + Temporal UI 2.34.1, all via docker-compose.

## Notes

- The workflow implementation is incomplete — `ExecuteAsync` cuts off mid-call, `CustomerCancelledAsync` throws `NotImplementedException`, compensation logic in `_compensations` stack is declared but not wired up
- No worker/host project yet exists to register and run the workflow against Temporal
- No test project exists yet
