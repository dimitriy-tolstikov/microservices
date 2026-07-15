# Микросервисы на ASP.NET Core + Apache Kafka + RabbitMQ

> Учебный тестовый проект распределённой системы на .NET 8.

[![.NET CI](https://github.com/dimitriy-tolstikov/Microservices-Example/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dimitriy-tolstikov/Microservices-Example/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Демонстрация взаимодействия микросервисов через два брокера сообщений:

- **Apache Kafka** — поток событий о заказах.
- **RabbitMQ** — обработка платежей, доставки и уведомлений.

## Архитектура

```text
┌──────────────┐     Kafka: orders      ┌─────────────────┐
│ OrderService │ ──────────────────────> │ PaymentService  │
│   (Web API)  │                         │ Kafka consumer  │
└──────────────┘                         │ RabbitMQ publisher
                                         └────────┬────────┘
                                                  │
                       ┌──────────────────────────┼──────────────────────────┐
                       │ RabbitMQ: payments (fanout)                          │
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
| `OrderService` | ASP.NET Core Web API | Принимает HTTP-запросы на создание заказа | Kafka producer |
| `PaymentService` | Worker Service | Слушает Kafka, имитирует оплату | Kafka consumer + RabbitMQ producer |
| `ShippingService` | Worker Service | Слушает очередь доставки | RabbitMQ consumer |
| `NotificationService` | Worker Service | Слушает очередь уведомлений | RabbitMQ consumer |

## Технологии

- [.NET 8](https://dotnet.microsoft.com/download)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Apache Kafka](https://kafka.apache.org/)
- [RabbitMQ](https://www.rabbitmq.com/)
- [Docker](https://www.docker.com/) & Docker Compose
- [Confluent.Kafka](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [RabbitMQ.Client](https://www.rabbitmq.com/dotnet-api-guide.html)

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) с включённым Docker Compose

## Быстрый старт

### 1. Инфраструктура

```bash
docker compose up -d zookeeper kafka rabbitmq
```

Подождите ~15–20 секунд, пока Kafka и RabbitMQ полностью запустятся.

> **Примечание:** В проекте используется команда `docker compose` (Compose V2). Если у вас установлена старая версия `docker-compose`, замените команду соответствующим образом.

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
│   └── workflows/
│       └── dotnet.yml          # CI: сборка решения
├── docker-compose.yml          # Инфраструктура и сервисы
├── Microservices.slnx          # Файл решения .NET
├── LICENSE                     # MIT License
├── README.md                   # Этот файл
└── src/
    ├── Shared                  # Общие события и сообщения
    ├── OrderService            # Web API
    ├── PaymentService          # Worker
    ├── ShippingService         # Worker
    └── NotificationService     # Worker
```

## CI / CD

При публикации на GitHub workflow `.github/workflows/dotnet.yml` автоматически выполняет:

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`

## Полезные команды

```bash
# Пересобрать решение
dotnet build Microservices.slnx

# Остановить всю инфраструктуру
docker compose down

# Посмотреть логи сервиса
docker compose logs -f payment-service
```

## Лицензия

Распространяется под лицензией MIT. См. [LICENSE](LICENSE).
