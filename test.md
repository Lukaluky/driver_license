# API Test Guide

Полный чеклист для ручного тестирования роутов через Gateway.

## 1) Базовые настройки

- Base URL Gateway: `http://localhost:5000`
- Все API-запросы отправляй через Gateway.
- Для защищенных роутов добавляй заголовок:
  - `Authorization: Bearer <jwt>`

Рекомендуемые переменные:

- `APPLICANT_EMAIL`
- `APPLICANT_PASSWORD` = `Password1!`
- `APPLICANT_IIN` (12 цифр, валидный)
- `INSPECTOR_EMAIL` = `inspector@test.com`
- `INSPECTOR_PASSWORD` = `Inspector123!`
- `APPLICANT_TOKEN`
- `INSPECTOR_TOKEN`
- `APPLICATION_ID`

---

## 2) Gateway роуты

### GET `/health`
- Назначение: health Gateway.
- Ожидание: `200`.

### GET `/`
- Назначение: информация о gateway и списке маршрутов.
- Ожидание: `200`.

### Проксирование
- Legacy API: `/api/*` -> OrderService.
- Новый префикс: `/api/orders/*` -> OrderService.
- Swagger: `/swagger/*`.
- Hangfire: `/hangfire/*`.

Проверка:
- `GET /api/system/health` -> `200`
- `GET /api/orders/system/health` -> `200`

---

## 3) System роуты

### GET `/api/system/health`
- Назначение: проверка API + DB + Redis.
- Ожидание: `200`, `Status=Healthy`, `Database=Up`, `Redis=Up`.

### GET `/api/system/version`
- Назначение: версия сервиса/окружение.
- Ожидание: `200`.

---

## 4) Auth роуты

## 4.1 Регистрация

### POST `/api/auth/register`
- Назначение: регистрация пользователя (`Applicant` или `Inspector`).
- Тело:
```json
{
  "email": "applicant1@test.com",
  "password": "Password1!",
  "role": "Applicant",
  "iin": "900515300123"
}
```
- Ожидание: `200`.
- Важно: токен на этом шаге может быть пустым (осознанная логика).

### POST `/api/auth/resend-confirmation`
- Назначение: повторная отправка кода.
- Тело:
```json
{
  "email": "applicant1@test.com"
}
```
- Ожидание: `200` или бизнес-ошибка `400/404`.

### POST `/api/auth/confirm-email`
- Назначение: подтверждение email кодом.
- Тело:
```json
{
  "email": "applicant1@test.com",
  "code": "123456"
}
```
- Ожидание: `200`.

## 4.2 Логин Inspector по паролю

### POST `/api/auth/login`
- Назначение: парольный вход только для `Inspector`.
- Тело:
```json
{
  "email": "inspector@test.com",
  "password": "Inspector123!"
}
```
- Ожидание: `200`, в ответе `token`.

Проверка ограничения:
- Для `Applicant` на этом роуте ожидание: `400`.

## 4.3 Логин Applicant по email-коду

### POST `/api/auth/login-request`
Альтернативный роут: `POST /api/auth/applicant/login-request`

- Назначение: отправка кода входа на email.
- Тело:
```json
{
  "email": "applicant1@test.com"
}
```
- Ожидание: `200`.

### POST `/api/auth/login-confirm`
Альтернативный роут: `POST /api/auth/applicant/login-confirm`

- Назначение: подтверждение кода и получение JWT.
- Тело:
```json
{
  "email": "applicant1@test.com",
  "code": "123456"
}
```
- Ожидание: `200`, в ответе `token`.
- Сохрани токен в `APPLICANT_TOKEN`.

---

## 5) Applications роуты

Все роуты требуют JWT.

## 5.1 Applicant

### POST `/api/applications`
- Роль: `Applicant`
- Назначение: создать заявку (ИИН в body не передается, берется из профиля пользователя).
- Тело:
```json
{
  "fullName": "Иванов Иван Иванович",
  "category": "B"
}
```
- Ожидание: `201`.
- Ответ содержит `id` -> сохрани в `APPLICATION_ID`.

### POST `/api/applications/renewal-expired`
- Роль: `Applicant`
- Назначение: перевыпуск при истекшем сроке.
- Тело:
```json
{
  "fullName": "Иванов Иван Иванович",
  "category": "B",
  "expiredAt": "2023-01-01T00:00:00Z"
}
```
- Ожидание: `201` или `400` по бизнес-правилам.

### POST `/api/applications/reissue`
- Роль: `Applicant`
- Назначение: перевыпуск по причине.
- Тело:
```json
{
  "fullName": "Иванов Иван Иванович",
  "category": "B",
  "reason": "Lost",
  "previousLicenceExpiredAt": null
}
```
- Ожидание: `201` или `400` по бизнес-правилам.

### GET `/api/applications/my?page=1&pageSize=10`
- Роль: `Applicant`
- Назначение: мои заявки.
- Ожидание: `200`.

### GET `/api/applications/{APPLICATION_ID}`
- Роль: `Applicant` или `Inspector`
- Назначение: детальная карточка.
- Ожидание: `200` / `404` / `403`.

### POST `/api/applications/{APPLICATION_ID}/cancel`
- Роль: `Applicant`
- Назначение: отмена заявки.
- Ожидание: `200` или `400`.

## 5.2 Inspector

### GET `/api/applications/pending?page=1&pageSize=10`
- Роль: `Inspector`
- Назначение: очередь на рассмотрение.
- Ожидание: `200`.

### GET `/api/applications/assigned/me?page=1&pageSize=10`
- Роль: `Inspector`
- Назначение: заявки, назначенные мне.
- Ожидание: `200`.

### GET `/api/applications/stats`
- Роль: `Inspector`
- Назначение: агрегированная статистика.
- Ожидание: `200`.

### POST `/api/applications/{APPLICATION_ID}/assign`
- Роль: `Inspector`
- Назначение: назначить заявку инспектору.
- Ожидание: `200` или `400`.

### POST `/api/applications/review`
- Роль: `Inspector`
- Назначение: approve/reject заявки.
- Тело:
```json
{
  "applicationId": "00000000-0000-0000-0000-000000000000",
  "approved": true,
  "rejectionReason": null
}
```
- Ожидание: `200` или `400/404`.

### POST `/api/applications/{APPLICATION_ID}/recheck`
- Роль: `Inspector`
- Назначение: повторная постановка внешних проверок в очередь.
- Ожидание: `202`.

### POST `/api/applications/{APPLICATION_ID}/print`
- Роль: `Inspector`
- Назначение: печать прав.
- Ожидание: `200` или `400/404`.

---

## 6) Ролевые и security проверки

- Без токена на `GET /api/applications/my` -> `401`.
- `Applicant` на inspector-роутах -> `403`.
- `Inspector` на applicant-роутах -> `403`.
- `Applicant` парольный логин (`/api/auth/login`) -> `400`.

---

## 7) Redis проверки (кратко)

### Проверка lock на дубль заявок
1. Отправь `POST /api/applications` (категория `B`) два раза подряд от одного `Applicant`.
2. Ожидание:
   - 1-й запрос: `201`
   - 2-й запрос: `409` (активная заявка уже есть)

### Проверка health Redis
- `GET /api/system/health` -> поле `Redis` должно быть `Up`.

---

## 8) Динамические внешние проверки

- Конфигурация хранится в таблице `ExternalCheckProviders`.
- На чистой БД ожидается минимум 2 активные записи:
  - `MVD`
  - `Medical`
- Проверки запускаются при создании/перепроверке заявки через Hangfire job.
