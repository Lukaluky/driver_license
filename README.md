# Driver Licence System

API для приёма и обработки заявок на получение водительских удостоверений.

## Технологии

- .NET 10, ASP.NET Core
- Entity Framework Core + PostgreSQL (Npgsql)
- Redis (кэширование)
- RabbitMQ (очередь сообщений / email-уведомления)
- Hangfire (фоновые задачи)
- YARP (API Gateway с rate limiting)
- JWT-аутентификация
- FluentValidation, Mapster
- WireMock (эмуляция внешних сервисов МВД и медкомиссии)
- Swagger / OpenAPI

## Архитектура

Проект построен по принципам Clean Architecture:

```
src/
├── OrderService.Domain           — сущности, перечисления, интерфейсы
├── OrderService.Application      — DTO, сервисы, валидаторы, маппинг
├── OrderService.Infrastructure   — EF Core, репозитории, внешние сервисы, джобы
├── OrderService.API              — Web API (контроллеры, Swagger, Hangfire Dashboard)
└── GateWay.API                   — YARP reverse proxy + rate limiting

tests/
├── OrderService.UnitTests        — xUnit, Moq, FluentAssertions
└── OrderService.IntegrationTests — WebApplicationFactory
```

**GateWay.API** проксирует все запросы к OrderService.API и ограничивает частоту обращений (100 запросов в минуту на IP).

## Запуск через Docker Compose

Убедитесь, что установлен [Docker](https://www.docker.com/).

```bash
docker-compose up --build
```

После запуска будут доступны:

| Сервис | Адрес |
|--------|-------|
| API Gateway | http://localhost:5000 |
| OrderService API | http://localhost:5050 |
| Swagger UI | http://localhost:5050/swagger |
| Hangfire Dashboard | http://localhost:5050/hangfire |
| RabbitMQ Management | http://localhost:15672 (guest / guest) |
| WireMock | http://localhost:8080 |
| PostgreSQL | localhost:8432 |
| Redis | localhost:6399 |

## Локальный запуск без Docker

Требования: [.NET 10 SDK](https://dotnet.microsoft.com/), PostgreSQL, Redis, RabbitMQ.

```bash
# OrderService API
dotnet run --project src/OrderService.API

# Gateway
dotnet run --project src/GateWay.API
```

Настройки подключения — в `appsettings.json` каждого проекта.

## Тесты

```bash
# Unit-тесты
dotnet test tests/OrderService.UnitTests

# Интеграционные тесты
dotnet test tests/OrderService.IntegrationTests
```

## WireMock (эмуляция внешних сервисов)

В каталоге `wiremock/` находятся стабы для внешних проверок:

- **МВД** — `POST /api/mvd/check` (задержка 1.5 с)
- **Медкомиссия** — `POST /api/medical/check` (задержка 2 с)

При запуске через Docker Compose WireMock поднимается автоматически на порту 8080.
