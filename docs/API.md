# HQ Studio API Documentation

## Overview

REST API for the HQ Studio CRM system. Base URL: `http://localhost:5000/api`

## Authentication

The API uses JWT Bearer tokens. Get a token via `/api/auth/login` and pass it in the header:

```
Authorization: Bearer <token>
```

## Endpoints

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/auth/login` | Login | ❌ |
| GET | `/auth/me` | Current user | ✅ |
| POST | `/auth/change-password` | Change password | ✅ |

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
    "name": "Administrator",
    "role": 0
  }
}
```

### Clients

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/clients` | List clients | ✅ |
| GET | `/clients/{id}` | Get client by ID | ✅ |
| POST | `/clients` | Create client | ✅ |
| PUT | `/clients/{id}` | Update client | ✅ |
| DELETE | `/clients/{id}` | Delete client | ✅ Admin |

### Orders

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/orders` | List orders | ✅ |
| GET | `/orders/{id}` | Get order by ID | ✅ |
| POST | `/orders` | Create order | ✅ |
| PUT | `/orders/{id}/status` | Change status | ✅ |
| DELETE | `/orders/{id}` | Delete order | ✅ Admin |

### Callback Requests

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/callbacks` | List requests | ✅ |
| GET | `/callbacks/stats` | Statistics | ✅ |
| POST | `/callbacks` | Create request (website) | ❌ |
| POST | `/callbacks/manual` | Create request (CRM) | ✅ |
| PUT | `/callbacks/{id}/status` | Change status | ✅ |
| DELETE | `/callbacks/{id}` | Delete request | ✅ Admin |

### Services

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/services` | List services | ❌ |
| GET | `/services/{id}` | Get service by ID | ❌ |
| POST | `/services` | Create service | ✅ |
| PUT | `/services/{id}` | Update service | ✅ |
| DELETE | `/services/{id}` | Delete service | ✅ Admin |

### Site Content

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/site` | All public data | ❌ |
| GET | `/site/blocks` | Content blocks | ✅ |
| GET | `/site/testimonials` | Testimonials | ✅ |
| GET | `/site/faq` | FAQ | ✅ |

### Dashboard

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/dashboard` | Statistics | ✅ |

### Users

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/users` | List users | ✅ Admin |
| GET | `/users/{id}` | Get user by ID | ✅ Admin |
| POST | `/users` | Create user | ✅ Admin |
| PUT | `/users/{id}` | Update user | ✅ Admin |
| DELETE | `/users/{id}` | Delete user | ✅ Admin |

### Health Check

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/health` | API status | ❌ |

## User Roles

| Role | Code | Permissions |
|------|------|-------------|
| Admin | 0 | Full access |
| Editor | 1 | Content editing |
| Manager | 2 | Work with clients and orders |

## Error Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not found |
| 429 | Too many requests |
| 500 | Internal error |

## Rate Limiting

- General limit: 100 requests/minute
- Authorization: 5 attempts/minute
- Public forms: 10 requests/5 minutes

## Swagger UI

Interactive documentation available at: `http://localhost:5000/swagger`
