# HQ Studio API Documentation

## Обзор

REST API для HQ Studio CRM системы. Базовый URL: `http://localhost:5000/api`

## Аутентификация

API использует JWT Bearer токены. Получите токен через `/api/auth/login` и передавайте в заголовке:

```
Authorization: Bearer <token>
```

## Endpoints

### Аутентификация

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| POST | `/auth/login` | Вход в систему | ❌ |
| GET | `/auth/me` | Текущий пользователь | ✅ |
| POST | `/auth/change-password` | Смена пароля | ✅ |

#### POST /auth/login

```json
// Request
{
  "login": "admin",
  "password": "password"
}

// Response 200
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "login": "admin",
    "name": "Администратор",
    "role": 0
  }
}
```

### Клиенты

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/clients` | Список клиентов | ✅ |
| GET | `/clients/{id}` | Клиент по ID | ✅ |
| POST | `/clients` | Создать клиента | ✅ |
| PUT | `/clients/{id}` | Обновить клиента | ✅ |
| DELETE | `/clients/{id}` | Удалить клиента | ✅ Admin |

### Заказы

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/orders` | Список заказов | ✅ |
| GET | `/orders/{id}` | Заказ по ID | ✅ |
| POST | `/orders` | Создать заказ | ✅ |
| PUT | `/orders/{id}/status` | Изменить статус | ✅ |
| DELETE | `/orders/{id}` | Удалить заказ | ✅ Admin |

### Заявки на обратный звонок

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/callbacks` | Список заявок | ✅ |
| GET | `/callbacks/stats` | Статистика | ✅ |
| POST | `/callbacks` | Создать заявку (сайт) | ❌ |
| POST | `/callbacks/manual` | Создать заявку (CRM) | ✅ |
| PUT | `/callbacks/{id}/status` | Изменить статус | ✅ |
| DELETE | `/callbacks/{id}` | Удалить заявку | ✅ Admin |

### Услуги

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/services` | Список услуг | ❌ |
| GET | `/services/{id}` | Услуга по ID | ❌ |
| POST | `/services` | Создать услугу | ✅ |
| PUT | `/services/{id}` | Обновить услугу | ✅ |
| DELETE | `/services/{id}` | Удалить услугу | ✅ Admin |

### Контент сайта

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/site` | Все публичные данные | ❌ |
| GET | `/site/blocks` | Блоки контента | ✅ |
| GET | `/site/testimonials` | Отзывы | ✅ |
| GET | `/site/faq` | FAQ | ✅ |

### Дашборд

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/dashboard` | Статистика | ✅ |

### Пользователи

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/users` | Список пользователей | ✅ Admin |
| GET | `/users/{id}` | Пользователь по ID | ✅ Admin |
| POST | `/users` | Создать пользователя | ✅ Admin |
| PUT | `/users/{id}` | Обновить пользователя | ✅ Admin |
| DELETE | `/users/{id}` | Удалить пользователя | ✅ Admin |

### Health Check

| Метод | Endpoint | Описание | Auth |
|-------|----------|----------|------|
| GET | `/health` | Статус API | ❌ |

## Роли пользователей

| Роль | Код | Права |
|------|-----|-------|
| Admin | 0 | Полный доступ |
| Editor | 1 | Редактирование контента |
| Manager | 2 | Работа с клиентами и заказами |

## Коды ошибок

| Код | Описание |
|-----|----------|
| 200 | Успех |
| 201 | Создано |
| 400 | Неверный запрос |
| 401 | Не авторизован |
| 403 | Доступ запрещён |
| 404 | Не найдено |
| 429 | Слишком много запросов |
| 500 | Внутренняя ошибка |

## Rate Limiting

- Общий лимит: 100 запросов/минуту
- Авторизация: 5 попыток/минуту
- Публичные формы: 10 запросов/5 минут

## Swagger UI

Интерактивная документация доступна по адресу: `http://localhost:5000/swagger`
