# Microservices with ASP.NET Core + Apache Kafka + RabbitMQ

[Русская версия](README.ru.md)

> A learning test project of a distributed system on .NET 8.

[![.NET CI](https://github.com/DmitriyOT/Microservices-Example/actions/workflows/dotnet.yml/badge.svg)](https://github.com/DmitriyOT/Microservices-Example/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A demonstration of microservice interaction through two message brokers with a focus on reliability, observability, and testability:

- **Apache Kafka** — order events stream.
- **RabbitMQ** — payment, shipping, and notification processing.

## Architecture

```text
┌──────────────┐     Kafka: orders      ┌─────────────────┐
│ OrderService │ ──────────────────────> │ PaymentService  │
│   (Web API)  │    + outbox pattern    │ Kafka consumer  │
└──────────────┘                         │ RabbitMQ publisher
                                         └────────┬────────┘
                                                  │
                       ┌──────────────────────────┼──────────────────────────┐
                       │ RabbitMQ: payments (direct) + DLX                  │
                       ▼                          ▼                          ▼
            ┌───────────────────┐    ┌───────────────────┐    ┌─────────────────────────┐
            │ payment.processed │    │ shipping.requested│    │ notification.requested  │
            └───────────────────┘    └───────────────────┘    └─────────────────────────┘
                                                       ▲                          ▲
                                                       │                          │
                                            ┌──────────┘                          │
                                            ▼                                     │
                                   ┌─────────────────┐                   ┌─────────────────┐
                                   │ ShippingService │                   │ NotificationService
                                   │  (Worker)       │                   │    (Worker)
                                   └─────────────────┘                   └─────────────────┘
```

## Services

| Service | Type | Role | Broker |
|---|---|---|---|
| `OrderService` | ASP.NET Core Web API | Accepts HTTP requests, saves the order to SQLite and the event to the Outbox | Kafka producer |
| `PaymentService` | Worker Service | Listens to Kafka, simulates payment | Kafka consumer + RabbitMQ producer |
| `ShippingService` | Worker Service | Listens to the shipping queue | RabbitMQ consumer |
| `NotificationService` | Worker Service | Listens to the notification queue | RabbitMQ consumer |

## What's implemented

- **Outbox pattern** in `OrderService` — the order and the event are saved in a single SQLite transaction, and a background publisher sends the event to Kafka.
- **Idempotency** in `PaymentService` — re-processed orders are skipped.
- **Manual offset commit** in Kafka — a message is committed only after successful processing.
- **Dead Letter Exchange (DLX)** in RabbitMQ — failed messages are not lost but routed to `*.dlq` queues.
- **Async consumers** in RabbitMQ — message processing does not block the thread pool.
- **Correlation ID** — propagated from the HTTP request through Kafka and RabbitMQ across all services.
- **OpenTelemetry tracing** — console trace exporter for all services.
- **Health checks** — `/health` in `OrderService`.
- **Global exception handler** — `ProblemDetails` for unexpected errors.
- **Strongly-typed configuration** with validation at startup.
- **Unit and integration tests** with xUnit.

## Technologies

- [.NET 8](https://dotnet.microsoft.com/download)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core SQLite](https://learn.microsoft.com/ef/core/)
- [Apache Kafka](https://kafka.apache.org/)
- [RabbitMQ](https://www.rabbitmq.com/)
- [Docker](https://www.docker.com/) & Docker Compose
- [OpenTelemetry](https://opentelemetry.io/)
- [xUnit](https://xunit.net/)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) with Docker Compose enabled

## Quick start

### 1. Infrastructure

```bash
docker compose up -d zookeeper kafka rabbitmq
```

Wait ~15–20 seconds for Kafka and RabbitMQ to fully start.

> **Note:** The project uses the `docker compose` command (Compose V2).

### 2. Run the services (locally)

Open 4 terminals and run in each:

```bash
# Terminal 1 — API
dotnet run --project src/OrderService/OrderService.csproj

# Terminal 2 — payment processing
dotnet run --project src/PaymentService/PaymentService.csproj

# Terminal 3 — shipping
dotnet run --project src/ShippingService/ShippingService.csproj

# Terminal 4 — notifications
dotnet run --project src/NotificationService/NotificationService.csproj
```

### 3. Create an order

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: demo-123" \
  -d '{
    "customerEmail": "user@example.com",
    "product": "Ноутбук",
    "quantity": 1,
    "amount": 5000
  }'
```

With an amount `> 10000` the payment will be declined to demonstrate error handling.

## Run everything in Docker

```bash
docker compose up -d --build
```

The API will be available at `http://localhost:5000`.

RabbitMQ management UI: `http://localhost:15672` (guest / guest).

## Verifying it works

After creating an order, the following messages should appear in the service logs:

- `OrderService`: `Order {Guid} accepted ...`
- `PaymentService`: `Processing payment for order {Guid}`
- `ShippingService`: `Preparing shipment for order {Guid}. Tracking: TRK-...`
- `NotificationService`: `Sending email to user@example.com: Оплата прошла успешно`

## Project structure

```text
.
├── .github/
│   ├── dependabot.yml
│   └── workflows/
│       └── dotnet.yml          # CI: сборка, тесты, Docker
├── docker-compose.yml          # Инфраструктура и сервисы
├── Microservices.sln           # Файл решения .NET
├── Directory.Build.props       # Общие MSBuild-свойства
├── global.json                 # Pin .NET SDK
├── .editorconfig               # Настройки стиля кода
├── LICENSE                     # MIT License
├── README.md                   # Этот файл
├── src/
│   ├── Shared                  # Общие события, сообщения, мessaging, options
│   ├── OrderService            # Web API + Outbox
│   ├── PaymentService          # Worker
│   ├── ShippingService         # Worker
│   └── NotificationService     # Worker
└── tests/
    ├── OrderService.UnitTests
    ├── Shared.UnitTests
    └── Microservices.IntegrationTests
```

## Tests

```bash
# All tests
dotnet test Microservices.sln

# Build and test in Release
dotnet build Microservices.sln --configuration Release
dotnet test Microservices.sln --configuration Release --no-build
```

## CI / CD

When published to GitHub, the `.github/workflows/dotnet.yml` workflow automatically runs:

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `docker compose build`

## Useful commands

```bash
# Rebuild the solution
dotnet build Microservices.sln

# Stop all infrastructure
docker compose down

# View service logs
docker compose logs -f payment-service

# Viewing "dead" messages in RabbitMQ
# Queues like *.dlq are available in the management UI: http://localhost:15672
```

## License

Distributed under the MIT License. See [LICENSE](LICENSE).
