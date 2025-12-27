---
name: "hqstudio-api-validator"
displayName: "HQStudio API Validator"
description: "Validates Desktop DTO models match API response structures. Detects type mismatches like int vs string, missing properties, and naming inconsistencies."
keywords: ["hqstudio", "api", "dto", "validation", "desktop", "mapping", "types"]
author: "HQStudio Team"
---

# HQStudio API Validator

## Overview

This power helps detect mismatches between Desktop DTO classes and API response models. Common issues it helps identify:

- Type mismatches (e.g., `Status` as `int` in API but `string` in Desktop)
- Missing properties (e.g., `PriceFrom` not mapped from API `Price`)
- Naming inconsistencies (e.g., `Title` vs `Name`)
- Enum serialization issues (API returns numbers, Desktop expects strings)

## Known Mappings

### API → Desktop Type Mappings

| API Model | Desktop DTO | Location |
|-----------|-------------|----------|
| `Service` | `ApiServiceItem` | `ApiService.cs` |
| `Client` | `ApiClient` | `ApiService.cs` |
| `Order` | `ApiOrder` | `ApiService.cs` |
| `Callback` | `ApiCallback` | `ApiService.cs` |
| `DashboardStats` | `DashboardStats` | `ApiService.cs` |
| `User` | `ApiUser`, `ApiUserDetail` | `ApiService.cs` |

### Critical Field Mappings

| API Field | API Type | Desktop Field | Desktop Type | Notes |
|-----------|----------|---------------|--------------|-------|
| `status` | `int` (enum) | `Status` | `int` | Was `string`, caused deserialization failure |
| `price` | `string` | `PriceFrom` | `decimal` | Requires parsing "от 15 000 ₽" → 15000 |
| `title` | `string` | `Name` | `string` | Different naming |
| `role` | `int` (enum) | `Role` | `string` | Uses `RoleConverter` |

## Validation Checklist

When adding new API endpoints or DTOs:

### 1. Check Enum Handling
```csharp
// API returns enums as integers by default
// Desktop must use int or add JsonConverter

// BAD - will fail silently
public string Status { get; set; }

// GOOD - matches API
public int Status { get; set; }

// ALSO GOOD - with converter
[JsonConverter(typeof(StatusConverter))]
public string Status { get; set; }
```

### 2. Check Property Names
```csharp
// API model
public class Service {
    public string Title { get; set; }  // API uses "Title"
}

// Desktop must map correctly
FeaturedServices.Add(new Service {
    Name = apiService.Title,  // Map Title → Name
});
```

### 3. Check Numeric Types
```csharp
// API returns decimal as number
// Desktop must use decimal, not string

// BAD
public string Price { get; set; }

// GOOD
public decimal Price { get; set; }
```

### 4. Check DateTime Handling
```csharp
// API returns ISO 8601 format
// Desktop should use DateTime with proper parsing

public DateTime CreatedAt { get; set; }
public DateTime? CompletedAt { get; set; }  // Nullable for optional
```

## Common Issues & Solutions

### Issue: Silent Deserialization Failure

**Symptom:** API call returns `null` even though API responds with data

**Cause:** Type mismatch causes JSON deserialization to fail silently

**Solution:**
1. Check API response in browser/Postman
2. Compare JSON types with Desktop DTO
3. Fix type mismatches

```csharp
// Add _jsonOptions for case-insensitive matching
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

// Use in deserialization
return await _http.GetFromJsonAsync<DashboardStats>("/api/dashboard", _jsonOptions);
```

### Issue: Price Not Displaying

**Symptom:** Services show but price is 0

**Cause:** API returns price as formatted string, Desktop expects decimal

**Solution:** Parse the string
```csharp
private static decimal ParsePrice(string? priceStr)
{
    if (string.IsNullOrWhiteSpace(priceStr)) return 0;
    var digits = new string(priceStr.Where(c => char.IsDigit(c)).ToArray());
    return decimal.TryParse(digits, out var result) ? result : 0;
}
```

### Issue: Enum Values Wrong

**Symptom:** Status shows as number instead of text, or wrong status

**Cause:** API returns enum as int, Desktop interprets differently

**Solution:** Use converter or map explicitly
```csharp
public class RoleConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32() switch
            {
                0 => "Admin",
                1 => "Editor",
                2 => "Manager",
                _ => "User"
            };
        }
        return reader.GetString() ?? "User";
    }
}
```

## File Locations

| Component | Path |
|-----------|------|
| API Models | `HQStudio.API/Models/` |
| API DTOs | `HQStudio.API/DTOs/` |
| Desktop DTOs | `HQStudio.Desktop/Services/ApiService.cs` (bottom of file) |
| Desktop Models | `HQStudio.Desktop/Models/` |

## Validation Workflow

1. **Before adding new API endpoint:**
   - Document expected response structure
   - Create matching Desktop DTO
   - Test with actual API response

2. **When debugging data issues:**
   - Check API response in browser: `http://localhost:5000/api/endpoint`
   - Compare with Desktop DTO properties
   - Look for type mismatches

3. **After fixing:**
   - Run Desktop tests: `dotnet test HQStudio.Desktop.Tests`
   - Test manually in app
   - Document the mapping if non-obvious
