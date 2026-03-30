# Cloud Billing Telemetry Microservice

> **Enterprise-grade ASP.NET Core 8 microservice** that ingests, normalizes, and serves cloud billing telemetry from AWS, Azure, and GCP — built on Clean Architecture with CQRS, MediatR, EF Core 8, Redis, and full observability.

---

## Architecture

```
MyApp.Api             → REST API (Controllers, Middleware, Program.cs)
  ↓
MyApp.Application     → CQRS (Commands, Queries, Validators, Behaviors, DTOs)
  ↓
MyApp.Domain          → Entities, Value Objects, Domain Events, Enums
  ↑
MyApp.Infrastructure  → EF Core/PostgreSQL, Redis, Provider Normalizers
```

## Tech Stack

| Concern           | Technology                                |
|-------------------|-------------------------------------------|
| API Framework     | ASP.NET Core 8                            |
| CQRS / Events     | MediatR 12                                |
| ORM               | EF Core 8 + Npgsql (PostgreSQL)           |
| Caching           | Redis (StackExchange.Redis)               |
| Validation        | FluentValidation 11                       |
| Mapping           | AutoMapper 13                             |
| Auth              | JWT Bearer                                |
| Rate Limiting     | ASP.NET Core Built-in                     |
| Logging           | Serilog (Console + Seq)                   |
| Observability     | OpenTelemetry → Jaeger + Prometheus       |
| Testing           | xUnit + Moq + FluentAssertions            |

## Supported Cloud Providers

| Provider | Billing Format                    |
|----------|-----------------------------------|
| **AWS**  | Cost and Usage Report (CUR) JSON  |
| **Azure**| Cost Management Export JSON       |
| **GCP**  | Billing Export (BigQuery JSON)    |

---

## Quick Start

### Local (Docker Compose)

```bash
# Start all services: API, PostgreSQL, Redis, Jaeger, Prometheus, Grafana
docker compose up -d

# View Swagger UI
open http://localhost:8080

# View Jaeger traces
open http://localhost:16686

# View Prometheus metrics
open http://localhost:9090

# View Grafana dashboards
open http://localhost:3000   # admin / admin
```

### Local Development (dotnet)

```bash
# Prerequisites: .NET 8 SDK, PostgreSQL, Redis

cp .env.example .env
# Edit .env with your DB/Redis credentials

dotnet restore
dotnet run --project MyApp.Api
```

---

## API Reference

### Ingestion Endpoints

| Method | Path                        | Description                    |
|--------|-----------------------------|--------------------------------|
| `POST` | `/api/v1/billing/ingest`    | Ingest a single billing record |
| `POST` | `/api/v1/billing/ingest/batch` | Ingest up to 1000 records   |

#### Single Ingest — Example Request (AWS)

```json
POST /api/v1/billing/ingest
Content-Type: application/json

{
  "provider": "AWS",
  "accountId": "123456789012",
  "correlationId": "req-abc123",
  "rawPayload": {
    "lineItem/UnblendedCost": "12.3456",
    "lineItem/CurrencyCode": "USD",
    "lineItem/UsageAmount": "100",
    "lineItem/UsageUnit": "Hrs",
    "lineItem/ProductCode": "AmazonEC2",
    "product/region": "us-east-1",
    "lineItem/UsageStartDate": "2024-01-01T00:00:00Z",
    "lineItem/UsageEndDate": "2024-01-02T00:00:00Z"
  }
}
```

### Query Endpoints

| Method | Path                        | Description                         |
|--------|-----------------------------|-------------------------------------|
| `GET`  | `/api/v1/billing/records`   | Paginated list with filters         |
| `GET`  | `/api/v1/billing/aggregate` | Cost aggregation over a time period |

#### Query Records — Example

```
GET /api/v1/billing/records?accountId=123456789012&provider=AWS&from=2024-01-01&page=1&pageSize=50
```

#### Aggregate — Example

```
GET /api/v1/billing/aggregate?accountId=123456789012&from=2024-01-01&to=2024-02-01
```

---

## Running Tests

```bash
# All tests
dotnet test --collect:"XPlat Code Coverage"

# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests
dotnet test --filter "Category=Integration"
```

---

## EF Core Migrations

```bash
cd MyApp.Api
dotnet ef migrations add InitialCreate --project ../MyApp.Infrastructure
dotnet ef database update
```

---

## Observability

- **Traces** → Jaeger at `http://localhost:16686`
- **Metrics** → Prometheus scrapes `/metrics`; Grafana at `:3000`
- **Logs** → Serilog structured JSON; optionally ship to Seq at `:5341`
- **Health** → `GET /health` (checks PostgreSQL + Redis)
- **Correlation IDs** → Every request tagged with `X-Correlation-Id`

---

## Project Structure

```
aspnet-enterprise-api/
├── MyApp.Api/                     # ASP.NET Core Web host
│   ├── Controllers/               # Ingestion + Query endpoints
│   ├── Middleware/                # Exception handling, Correlation ID
│   ├── Program.cs                 # Full DI wiring
│   └── appsettings.json
├── MyApp.Application/             # Use-cases (CQRS)
│   ├── Commands/                  # IngestBillingRecord, IngestBillingBatch
│   ├── Queries/                   # GetBillingRecords, GetBillingAggregate
│   ├── Behaviors/                 # Logging, Validation MediatR pipeline
│   ├── Validators/                # FluentValidation
│   ├── Mappings/                  # AutoMapper profiles
│   ├── DTOs/                      # Request/response models
│   └── Interfaces/                # Repository + service contracts
├── MyApp.Domain/                  # Pure domain model
│   ├── Entities/                  # BillingRecord aggregate root
│   ├── ValueObjects/              # MoneyAmount, ServiceIdentifier
│   ├── Events/                    # BillingRecordIngested
│   └── Enums/                     # CloudProvider, BillingStatus
├── MyApp.Infrastructure/          # External services
│   ├── Persistence/               # EF Core DbContext + migrations
│   ├── Repositories/              # BillingRepository
│   ├── Services/                  # AWS/Azure/GCP normalizers
│   ├── Caching/                   # RedisCacheService
│   └── Extensions/                # DI registration
├── MyApp.Tests/
│   ├── Unit/                      # Domain, Application, Normalizer tests
│   └── Integration/               # Repository integration tests
├── infra/
│   └── prometheus.yml
├── Dockerfile                     # Multi-stage production build
├── docker-compose.yml             # Full local dev stack
└── .env.example
```
