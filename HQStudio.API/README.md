# HQ Studio API

Централизованный бэкенд для сайта и WPF приложения.

## Запуск

```bash
cd HQStudio.API
dotnet run
```

API будет доступен на `http://localhost:5000`

Swagger UI: `http://localhost:5000/swagger`

## Авторизация

По умолчанию создаётся пользователь:
- Логин: `admin`
- Пароль: `admin`

### Получение токена

```bash
POST /api/auth/login
{
  "login": "admin",
  "password": "admin"
}
```

Ответ:
```json
{
  "token": "eyJhbG...",
  "user": { "id": 1, "login": "admin", "name": "...", "role": "Admin" }
}
```

Используйте токен в заголовке: `Authorization: Bearer <token>`

## API Endpoints

### Публичные (без авторизации)
- `GET /api/site` — данные для сайта
- `GET /api/services?activeOnly=true` — список услуг
- `POST /api/callbacks` — заявка на обратный звонок
- `POST /api/subscriptions` — подписка на рассылку

### Требуют авторизации
- `GET /api/dashboard` — статистика
- `GET/POST/PUT/DELETE /api/clients` — клиенты
- `GET/POST/PUT/DELETE /api/orders` — заказы
- `GET/POST/PUT/DELETE /api/services` — услуги (редактирование)
- `GET/PUT/DELETE /api/users` — пользователи (только Admin)
- `GET/PUT/DELETE /api/site/*` — контент сайта

## Роли

| Роль | Права |
|------|-------|
| Admin | Полный доступ |
| Editor | Редактирование контента сайта |
| Manager | Работа с клиентами и заказами |
