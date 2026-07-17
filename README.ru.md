# Микросервисы на ASP.NET Core + Apache Kafka + RabbitMQ

[English version](README.md)

> Учебный тестовый проект распределённой системы на .NET 8.

[![.NET CI](https://github.com/dimitriy-tolstikov/Microservices-Example/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dimitriy-tolstikov/Microservices-Example/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Демонстрация взаимодействия микросервисов через два брокера сообщений с упором на надёжность, наблюдаемость и тестируемость:

- **Apache Kafka** — поток событий о заказах.
- **RabbitMQ** — обработка платежей, доставки и уведомлений.

## Архитектура

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

## Сервисы

| Сервис | Тип | Роль | Брокер |
|---|---|---|---|
| `OrderService` | ASP.NET Core Web API | Принимает HTTP-запросы, сохраняет заказ в SQLite и событие в Outbox | Kafka producer |
| `PaymentService` | Worker Service | Слушает Kafka, имитирует оплату | Kafka consumer + RabbitMQ producer |
| `ShippingService` | Worker Service | Слушает очередь доставки | RabbitMQ consumer |
| `NotificationService` | Worker Service | Слушает очередь уведомлений | RabbitMQ consumer |

## Что реализовано

- **Outbox pattern** в `OrderService` — заказ и событие сохраняются в одной транзакции SQLite, а фоновый паблишер отправляет событие в Kafka.
- **Идемпотентность** в `PaymentService` — повторно обработанные заказы пропускаются.
- **Ручной commit offset** в Kafka — сообщение коммитится только после успешной обработки.
- **Dead Letter Exchange (DLX)** в RabbitMQ — ошибочные сообщения не теряются, а отправляются в очереди `*.dlq`.
- **Async consumers** RabbitMQ — обработка сообщений не блокирует пул потоков.
- **Correlation ID** — пробрасывается из HTTP-запроса через Kafka и RabbitMQ между всеми сервисами.
- **OpenTelemetry tracing** — консольный экспортёр трейсов для всех сервисов.
- **Health checks** — `/health` в `OrderService`.
- **Глобальный обработчик исключений** — `ProblemDetails` для непредвиденных ошибок.
- **Типизированная конфигурация** с валидацией на старте.
- **Юнит- и интеграционные тесты** на xUnit.

## Технологии

- [.NET 8](https://dotnet.microsoft.com/download)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core SQLite](https://learn.microsoft.com/ef/core/)
- [Apache Kafka](https://kafka.apache.org/)
- [RabbitMQ](https://www.rabbitmq.com/)
- [Docker](https://www.docker.com/) & Docker Compose
- [OpenTelemetry](https://opentelemetry.io/)
- [xUnit](https://xunit.net/)

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) с включённым Docker Compose

## Быстрый старт

### 1. Инфраструктура

```bash
docker compose up -d zookeeper kafka rabbitmq
```

Подождите ~15–20 секунд, пока Kafka и RabbitMQ полностью запустятся.

> **Примечание:** В проекте используется команда `docker compose` (Compose V2).

### 2. Запуск сервисов (локально)

Откройте 4 терминала и выполните в каждом:

```bash
# Терминал 1 — API
dotnet run --project src/OrderService/OrderService.csproj

# Терминал 2 — обработка платежей
dotnet run --project src/PaymentService/PaymentService.csproj

# Терминал 3 — доставка
dotnet run --project src/ShippingService/ShippingService.csproj

# Терминал 4 — уведомления
dotnet run --project src/NotificationService/NotificationService.csproj
```

### 3. Создать заказ

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

При сумме `> 10000` платёж будет отклонён для демонстрации обработки ошибок.

## Запуск полностью в Docker

```bash
docker compose up -d --build
```

API будет доступен на `http://localhost:5000`.

Управление RabbitMQ: `http://localhost:15672` (guest / guest).

## Проверка работы

После создания заказа в логах сервисов должны появиться сообщения:

- `OrderService`: `Order {Guid} accepted ...`
- `PaymentService`: `Processing payment for order {Guid}`
- `ShippingService`: `Preparing shipment for order {Guid}. Tracking: TRK-...`
- `NotificationService`: `Sending email to user@example.com: Оплата прошла успешно`

## Структура проекта

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

## Тесты

```bash
# Все тесты
dotnet test Microservices.sln

# Сборка и тесты в Release
dotnet build Microservices.sln --configuration Release
dotnet test Microservices.sln --configuration Release --no-build
```

## CI / CD

При публикации на GitHub workflow `.github/workflows/dotnet.yml` автоматически выполняет:

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `docker compose build`

## Полезные команды

```bash
# Пересобрать решение
dotnet build Microservices.sln

# Остановить всю инфраструктуру
docker compose down

# Посмотреть логи сервиса
docker compose logs -f payment-service

# Просмотр "мертвых" сообщений в RabbitMQ
# Очереди вида *.dlq доступны в management UI: http://localhost:15672
```

## Лицензия

Распространяется под лицензией MIT. См. [LICENSE](LICENSE).
