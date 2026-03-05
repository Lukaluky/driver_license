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
- MailPit (локальный SMTP для просмотра отправленных писем)

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

**GateWay.API** проксирует запросы к backend-сервисам, ограничивает частоту обращений (100 запросов в минуту на IP) и поддерживает расширяемую схему маршрутов.

### Расширяемость Gateway

Маршрутизация сделана через конфиг `src/GateWay.API/appsettings.json`:

- `orders-api-v1-route` — ` /api/orders/{**catch-all}` (новый префикс для OrderService)
- `orders-api-legacy-route` — ` /api/{**catch-all}` (обратная совместимость)
- `checks-api-route` — ` /api/checks/{**catch-all}` (шаблон под будущий сервис проверок)
- `swagger-route`, `hangfire-route` — проксирование тех. интерфейсов

Чтобы добавить **новый сервис**, достаточно:

1. Добавить новый `Cluster` с адресом сервиса.
2. Добавить `Route` с публичным префиксом (`/api/<service>/{**catch-all}`).
3. При необходимости добавить `Transforms` (например `PathRemovePrefix`).
4. Добавить переменную окружения в `docker-compose.yml` для адреса destination.

Отказоустойчивость на уровне Gateway:

- `LoadBalancingPolicy: PowerOfTwoChoices`
- `Passive health checks` (временное исключение проблемной destination)
- `ActivityTimeout` для downstream-запросов
- дублирование destination для failover-ready схемы

## Запуск через Docker Compose

Убедитесь, что установлен [Docker](https://www.docker.com/).

```bash
docker compose up --build
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
| Future checks API (demo profile) | http://localhost:5060 |
| MailPit UI | http://localhost:8025 |
| MailPit SMTP | localhost:1025 |
| PostgreSQL | localhost:8432 |
| Redis | localhost:6399 |

> `future-checks-api` запускается только с profile `future`:
>
> `docker compose --profile future up --build`

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

## Отправка email через RabbitMQ Consumer

- Коды подтверждения и уведомления публикуются в очередь `email-notifications`.
- Фоновый consumer (`RabbitMqEmailConsumer`) читает очередь и отправляет письма через SMTP.
- Для локального Docker-запуска используется MailPit (`http://localhost:8025`) — все письма видны в UI.

## API (основные маршруты)

### Auth

- `POST /api/auth/register`
- `POST /api/auth/confirm-email`
- `POST /api/auth/resend-confirmation`
- `POST /api/auth/login` (только Inspector, вход по паролю)
- `POST /api/auth/applicant/login-request` (Applicant, запрос кода входа на email)
- `POST /api/auth/applicant/login-confirm` (Applicant, подтверждение кода и выдача JWT)

### Applications

- `POST /api/applications`
- `POST /api/applications/renewal-expired` (перевыпуск, если срок прав истек)
- `POST /api/applications/reissue` (перевыпуск: утеря, порча, смена данных, истечение)
- `GET /api/applications/my`
- `GET /api/applications/assigned/me`
- `GET /api/applications/pending`
- `GET /api/applications/stats`
- `GET /api/applications/{id}`
- `POST /api/applications/{id}/assign`
- `POST /api/applications/{id}/review`
- `POST /api/applications/{id}/cancel`
- `POST /api/applications/{id}/recheck`
- `POST /api/applications/{id}/print`

### System

- `GET /api/system/health`
- `GET /api/system/version`

## Расширяемость внешних проверок

Проверки вынесены в динамическую конфигурацию:

- таблица `ExternalCheckProviders` хранит активные внешние проверки (имя, URL, path, метод, таймаут, порядок).
- `ExternalCheckService` читает включенные записи из БД и запускает их по `ExecutionOrder`.
- `ExternalCheckJob` остается неизменным и вызывает общий оркестратор.

Чтобы добавить новую проверку:

1. Добавить запись в `ExternalCheckProviders` (`IsEnabled = true`).
2. Указать `BaseUrl` и `Path` (можно с шаблоном `{iin}`).
3. Задать `ExecutionOrder` и `TimeoutSeconds`.

После этого новая проверка автоматически попадает в пайплайн без изменения кода `ExternalCheckJob`.

## Бизнес-правила по ИИН и категориям

- ИИН теперь указывается при регистрации пользователя (`register`) и хранится в профиле.
- При подаче заявки ИИН всегда автоматически берется из профиля пользователя по `user_id`.
- В DTO создания заявки поле ИИН отсутствует.
- Для категорий `C` и `D`:
  - требуется возраст не меньше 21 года,
  - требуется уже выданная категория `B` (статус `Printed`).
