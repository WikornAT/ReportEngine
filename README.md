# Exim.ReportEngine

A **Modular Monolith** reporting platform built with **.NET 10**, following **Clean Architecture** and **Domain-Driven Design** principles. The system provides a full lifecycle for defining, configuring, and executing data-driven reports with pluggable renderers and storage back-ends.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Solution Structure](#solution-structure)
- [Modules](#modules)
  - [Reporting](#reporting-module)
  - [Labeling](#labeling-module)
  - [Printing](#printing-module)
  - [Designer](#designer-module)
  - [Scheduling](#scheduling-module)
  - [Dashboard](#dashboard-module)
  - [Templates](#templates-module)
- [Building Blocks](#building-blocks)
- [Hosts](#hosts)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Testing](#testing)
- [Contributing](#contributing)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                        Hosts                            │
│   ApiHost (ASP.NET Core)   │   WorkerHost (.NET Worker) │
└───────────────┬─────────────────────────┬───────────────┘
                │                         │
┌───────────────▼─────────────────────────▼───────────────┐
│                       Modules                           │
│  Reporting │ Labeling │ Printing │ Designer │ ...       │
│  ┌────────┐ Each module contains:                       │
│  │  .Api  │  ← HTTP controllers / endpoints            │
│  │  .App  │  ← Commands, Queries (MediatR/CQRS)        │
│  │  .Dom  │  ← Aggregates, Entities, Domain Events     │
│  │  .Inf  │  ← EF Core, Repositories, Services         │
│  └────────┘                                             │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                    Building Blocks                      │
│  SharedKernel │ Abstractions │ Contracts │ Infrastructure│
└─────────────────────────────────────────────────────────┘
```

Each module is **fully self-contained** — its own database schema, its own migration history, and its own DI registration entry point. Modules communicate only through well-defined contracts in the BuildingBlocks layer, never through direct project references to each other.

---

## Solution Structure

```
Exim.ReportEngine/
├── src/
│   ├── Host/
│   │   ├── Exim.ReportEngine.ApiHost/          # ASP.NET Core Web API host
│   │   └── Exim.ReportEngine.WorkerHost/       # .NET Worker Service host
│   │
│   ├── Modules/
│   │   ├── Reporting/
│   │   │   ├── Reporting.Api/                  # Controllers, request models
│   │   │   ├── Reporting.Application/          # CQRS handlers, DTOs, validators
│   │   │   ├── Reporting.Domain/               # Aggregates, enums, domain rules
│   │   │   └── Reporting.Infrastructure/       # EF Core, services, migrations
│   │   ├── Labeling/
│   │   ├── Printing/
│   │   ├── Designer/
│   │   ├── Scheduling/
│   │   ├── Dashboard/
│   │   └── Templates/
│   │
│   └── BuildingBlocks/
│       ├── Exim.ReportEngine.SharedKernel/     # Result<T>, AppError, PagedResult, ICurrentUserService, IDateTimeProvider
│       ├── Exim.ReportEngine.Abstractions/     # IRepository<T>
│       ├── Exim.ReportEngine.Contracts/        # Cross-module contracts
│       └── Exim.ReportEngine.Infrastructure/  # Shared infrastructure helpers
│
└── tests/
    └── Modules/
        ├── Reporting/
        │   ├── Reporting.Domain.UnitTests/
        │   └── Reporting.Application.UnitTests/
        ├── Printing/
        ├── Designer/
        ├── Scheduling/
        ├── Dashboard/
        └── Templates/
```

---

## Modules

### Reporting Module

The core module — manages the full report lifecycle.

#### Domain concepts

| Aggregate | Description |
|-----------|-------------|
| `ReportDefinition` | Blueprint for a report: metadata, parameters, data sources, template reference |
| `ReportExecution` | A single run of a report definition, with status tracking and output files |

#### Feature slices (`Reporting.Application/Features/`)

**ReportDefinitions**

| Slice | Type | Description |
|-------|------|-------------|
| `Create` | Command | Creates a new definition in `Draft` status |
| `Update` | Command | Updates name, category, description |
| `Activate` | Command | Transitions `Draft`/`Inactive` → `Active` |
| `Deactivate` | Command | Transitions `Active` → `Inactive` |
| `AddDataSource` | Command | Attaches a data source (SQL/SP/HTTP) to a definition |
| `AddParameter` | Command | Declares an input parameter on a definition |
| `GetById` | Query | Returns a full definition with parameters and data sources |
| `GetList` | Query | Returns a paged, filtered list of definitions |

**ReportExecutions**

| Slice | Type | Description |
|-------|------|-------------|
| `Execute` | Command | Runs the full pipeline: validate → query → render → store |
| `GetHistory` | Query | Returns a paged, filtered execution history |

#### Contracts (`Reporting.Application/Contracts/`)

| Interface | Responsibility |
|-----------|---------------|
| `IReportingDbContext` | EF Core unit-of-work abstraction |
| `IReportQueryExecutor` | Executes data source queries, returns JSON payload |
| `IReportRenderer` | Renders data payload into binary output (PDF, XLSX, …) |
| `IReportOutputStorage` | Persists rendered files to durable storage |

#### Report status lifecycle

```
Draft ──► Active ──► Inactive
  ▲                     │
  └─────────────────────┘  (re-activate)
```

---

### Labeling Module

Manages product/asset labeling workflows. Integrated into the API host.

---

### Printing Module

Handles print job scheduling and dispatch. Contains its own domain model with unit tests.

---

### Designer Module

Report template visual design features. Includes domain and application unit tests.

---

### Scheduling Module

Recurring job and cron-based scheduling. Contains domain and application test suites.

---

### Dashboard Module

Aggregated metrics and KPI views. Contains domain and application test suites.

---

### Templates Module

Manages report template assets (file references, versioning).

---

## Building Blocks

### SharedKernel

Cross-cutting primitives used by all modules:

| Type | Description |
|------|-------------|
| `Result<T>` | Discriminated union — success value or `AppError` |
| `AppError` | Typed, immutable error descriptor with factory methods (`NotFound`, `Conflict`, `Validation`, `DomainViolation`) |
| `PagedResult<T>` | Paged envelope for list queries |
| `ICurrentUserService` | Abstracts the authenticated user identity (HTTP or background worker) |
| `IDateTimeProvider` | Abstracts `DateTimeOffset.UtcNow` for deterministic testing |

### Abstractions

| Type | Description |
|------|-------------|
| `IRepository<T>` | Generic repository contract |

---

## Hosts

### ApiHost (`Exim.ReportEngine.ApiHost`)

ASP.NET Core Web API that composes all module controllers.

- Registers module services via `AddReportingApi()`, `AddLabelingInfrastructure()`, etc.
- Exposes Swagger/OpenAPI at `/swagger` in development.
- Loads controllers from module assemblies via `AddApplicationPart`.

### WorkerHost (`Exim.ReportEngine.WorkerHost`)

.NET Worker Service for background processing (scheduled report runs, event consumers, etc.).

- Implements `BackgroundService`.
- Containerised via `Dockerfile`.

---

## Tech Stack

| Concern | Library / Technology |
|---------|---------------------|
| Framework | .NET 10 |
| Web API | ASP.NET Core 10 |
| Background Service | .NET Generic Worker Host |
| CQRS / Mediator | MediatR |
| Validation | FluentValidation |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL (Npgsql provider) |
| API Documentation | Swashbuckle / OpenAPI |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging (structured, `LoggerMessage.Define`) |
| Testing | xUnit (inferred from test projects) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL instance (see connection strings below)

### Configuration

Update `src/Host/Exim.ReportEngine.ApiHost/appsettings.json` (or use user secrets / environment variables):

```json
{
  "ConnectionStrings": {
    "LabelingDb":  "Host=localhost;Port=5432;Database=LabelingDb;Username=dev;Password=dev",
    "ReportingDb": "Host=localhost;Port=5432;Database=ReportingDb;Username=dev;Password=dev"
  }
}
```

### Apply Migrations

```powershell
# Reporting module
dotnet ef database update `
  --project src/Modules/Reporting/Reporting.Infrastructure `
  --startup-project src/Host/Exim.ReportEngine.ApiHost
```

### Run the API Host

```powershell
dotnet run --project src/Host/Exim.ReportEngine.ApiHost
```

Swagger UI: `https://localhost:{port}/swagger`

### Run the Worker Host

```powershell
dotnet run --project src/Host/Exim.ReportEngine.WorkerHost
```

---

## API Reference

Base URL: `/api/reporting`

### Report Definitions

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/report-definitions` | Paged list — query params: `category`, `searchTerm`, `status`, `includeHidden`, `page`, `pageSize` |
| `GET` | `/report-definitions/{id}` | Get by id (includes parameters & data sources) |
| `POST` | `/report-definitions` | Create (`name`, `category`, `description?`, `subCategory?`) |
| `PUT` | `/report-definitions/{id}` | Update metadata |
| `POST` | `/report-definitions/{id}/activate` | Activate |
| `POST` | `/report-definitions/{id}/deactivate` | Deactivate |
| `POST` | `/report-definitions/{id}/data-sources` | Add data source |
| `POST` | `/report-definitions/{id}/parameters` | Add parameter |

### Report Executions

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/executions` | Execute a report (`reportDefinitionId`, `parametersJson`, `requestedFormats`, `correlationId?`) |
| `GET` | `/executions` | Paged history — query params: `reportDefinitionId`, `triggeredBy`, `status`, `page`, `pageSize` |

### Error responses

All failure responses follow [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457):

```json
{
  "title": "Conflict",
  "detail": "A report named 'Sales Summary' already exists in category 'Finance'.",
  "status": 409
}
```

| `AppError.Code` | HTTP Status |
|----------------|------------|
| `*.NotFound` | 404 |
| `Validation` | 422 |
| `Conflict` | 409 |
| `DomainViolation` | 422 |
| anything else | 500 |

---

## Testing

```powershell
# Run all tests
dotnet test

# Run a specific module
dotnet test tests/Modules/Reporting/Reporting.Application.UnitTests
```

Test projects mirror the source structure under `tests/Modules/`.

---

## Contributing

1. Branch from `develop`.
2. Follow the vertical-slice pattern — new features go in `Features/<Aggregate>/<SliceName>/`.
3. Add a FluentValidation validator for every command/query.
4. New cross-cutting types belong in `SharedKernel`, not inside a module.
5. Do not add direct project references between modules.
6. Run `dotnet build` and `dotnet test` before opening a pull request.
