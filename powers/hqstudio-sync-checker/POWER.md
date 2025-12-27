---
name: "hqstudio-sync-checker"
displayName: "HQStudio Sync Checker"
description: "Checks data type consistency across Web, Desktop, and API components. Identifies mismatches in models, DTOs, and API contracts."
keywords: ["hqstudio", "sync", "types", "web", "desktop", "api", "consistency"]
author: "HQStudio Team"
---

# HQStudio Sync Checker

## Overview

HQStudio has three components that must stay in sync:
- **API** (ASP.NET Core) - Source of truth for data models
- **Desktop** (WPF) - C# DTOs in ApiService.cs
- **Web** (Next.js) - TypeScript types in lib/types.ts

This power helps identify and prevent type mismatches across components.

## Component Locations

| Component | Models Location | Types Location |
|-----------|-----------------|----------------|
| API | `HQStudio.API/Models/` | `HQStudio.API/DTOs/` |
| Desktop | `HQStudio.Desktop/Models/` | `HQStudio.Desktop/Services/ApiService.cs` |
| Web | N/A | `HQStudio.Web/lib/types.ts` |

## Entity Sync Matrix

### Service Entity

| Property | API | Desktop DTO | Desktop Model | Web |
|----------|-----|-------------|---------------|-----|
| Id | `int` | `int` | `int` | `number` |
| Title/Name | `Title: string` | `Title: string` | `Name: string` | `title: string` |
| Description | `string` | `string` | `string` | `description: string` |
| Category | `string` | `string` | `string` | `category: string` |
| Price | `string` | `Price: string` | `PriceFrom: decimal` | `price: string` |
| Icon | `string` | `string` | `string` | `icon: string` |
| IsActive | `bool` | `bool` | `bool` | `isActive: boolean` |
| SortOrder | `int` | `int` | N/A | `sortOrder: number` |

**Known Issues:**
- Desktop Model uses `Name` but API/DTO use `Title`
- Desktop Model uses `PriceFrom: decimal` but API returns `Price: string`

### Client Entity

| Property | API | Desktop DTO | Web |
|----------|-----|-------------|-----|
| Id | `int` | `int` | `number` |
| Name | `string` | `string` | `string` |
| Phone | `string` | `string` | `string` |
| Email | `string?` | `string?` | `string?` |
| CarModel | `string?` | `string?` | `string?` |
| LicensePlate | `string?` | `string?` | `string?` |
| Notes | `string?` | `string?` | N/A |
| CreatedAt | `DateTime` | `DateTime` | `string` (ISO) |

### Order Entity

| Property | API | Desktop DTO | Web |
|----------|-----|-------------|-----|
| Id | `int` | `int` | `number` |
| ClientId | `int` | `int` | `number` |
| Status | `OrderStatus (enum→int)` | `int` | `number` |
| TotalPrice | `decimal` | `decimal` | `number` |
| Notes | `string?` | `string?` | `string?` |
| CreatedAt | `DateTime` | `DateTime` | `string` |
| CompletedAt | `DateTime?` | `DateTime?` | `string?` |

**Known Issues:**
- Status is enum in API, serialized as `int`

### Callback Entity

| Property | API | Desktop DTO | Web |
|----------|-----|-------------|-----|
| Id | `int` | `int` | `number` |
| Name | `string` | `string` | `string` |
| Phone | `string` | `string` | `string` |
| Status | `CallbackStatus (enum→int)` | `int` | `number` |
| Source | `RequestSource (enum→int)` | `int` | `number` |
| CreatedAt | `DateTime` | `DateTime` | `string` |

### DashboardStats

| Property | API | Desktop DTO |
|----------|-----|-------------|
| TotalClients | `int` | `int` |
| TotalOrders | `int` | `int` |
| NewCallbacks | `int` | `int` |
| MonthlyRevenue | `decimal` | `decimal` |
| OrdersInProgress | `int` | `int` |
| CompletedThisMonth | `int` | `int` |
| PopularServices | `List<ServiceStat>` | `List<ServiceStat>` |
| RecentOrders | `List<RecentOrderStat>` | `List<RecentOrderStat>` |

**RecentOrderStat:**
| Property | API | Desktop |
|----------|-----|---------|
| Status | `int` (enum) | `int` ✓ (was `string` - fixed) |

## Enum Mappings

### OrderStatus
| Value | API | Desktop Display |
|-------|-----|-----------------|
| 0 | New | "Новый" |
| 1 | InProgress | "В работе" |
| 2 | Completed | "Завершен" |
| 3 | Cancelled | "Отменен" |

### CallbackStatus
| Value | API | Desktop Display |
|-------|-----|-----------------|
| 0 | New | "Новая" |
| 1 | Processing | "В обработке" |
| 2 | Completed | "Завершена" |
| 3 | Cancelled | "Отменена" |

### RequestSource
| Value | API | Desktop Display |
|-------|-----|-----------------|
| 0 | Website | "Сайт" |
| 1 | Phone | "Телефон" |
| 2 | WalkIn | "Визит" |
| 3 | Email | "Email" |
| 4 | Messenger | "Мессенджер" |
| 5 | Referral | "Рекомендация" |
| 6 | Other | "Другое" |

### UserRole
| Value | API | Desktop |
|-------|-----|---------|
| 0 | Admin | "Admin" |
| 1 | Editor | "Editor" |
| 2 | Manager | "Manager" |

## Sync Verification Checklist

### When Adding New API Endpoint

1. **API Side:**
   - [ ] Create/update Model in `Models/`
   - [ ] Create DTO if needed in `DTOs/`
   - [ ] Document response structure

2. **Desktop Side:**
   - [ ] Add DTO class in `ApiService.cs`
   - [ ] Match all property names (case-insensitive with `_jsonOptions`)
   - [ ] Match all property types (especially enums → int)
   - [ ] Add API method in `ApiService`

3. **Web Side (if needed):**
   - [ ] Add TypeScript interface in `lib/types.ts`
   - [ ] Match property names (camelCase)
   - [ ] Use correct TypeScript types

### When Modifying Existing Entity

1. **Check all three components**
2. **Update in order:** API → Desktop → Web
3. **Run tests:** `make test`
4. **Test manually in each app**

## Common Sync Issues

### Issue: Property Name Mismatch

```
API: "Title"
Desktop: "Name"
```

**Solution:** Map explicitly when creating objects
```csharp
new Service { Name = apiService.Title }
```

### Issue: Enum Serialization

```
API returns: { "status": 1 }
Desktop expects: { "status": "InProgress" }
```

**Solution:** Use `int` type or add JsonConverter
```csharp
public int Status { get; set; }  // Match API
```

### Issue: DateTime Format

```
API returns: "2024-12-27T10:30:00Z"
Web expects: Date object
```

**Solution:** Parse ISO string
```typescript
const date = new Date(response.createdAt)
```

### Issue: Nullable Types

```
API: string? (can be null)
Desktop: string (not nullable)
```

**Solution:** Use nullable types
```csharp
public string? Notes { get; set; }
```

## Verification Commands

```bash
# Check API response structure
curl http://localhost:5000/api/services | jq

# Check Desktop DTO matches
# Look at ApiService.cs DTO classes

# Run all tests to catch mismatches
make test
```

## File Quick Reference

| What | Where |
|------|-------|
| API Models | `HQStudio.API/Models/*.cs` |
| API DTOs | `HQStudio.API/DTOs/*.cs` |
| API Controllers | `HQStudio.API/Controllers/*.cs` |
| Desktop DTOs | `HQStudio.Desktop/Services/ApiService.cs` (line ~700+) |
| Desktop Models | `HQStudio.Desktop/Models/*.cs` |
| Web Types | `HQStudio.Web/lib/types.ts` |
| Web Constants | `HQStudio.Web/lib/constants.ts` |
