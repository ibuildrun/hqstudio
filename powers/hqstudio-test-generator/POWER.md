---
name: "hqstudio-test-generator"
displayName: "HQStudio Test Generator"
description: "Generates test templates for API controllers, Desktop ViewModels, and Web components following project conventions."
keywords: ["hqstudio", "tests", "xunit", "vitest", "generator", "testing"]
author: "HQStudio Team"
---

# HQStudio Test Generator

## Overview

This power provides templates and patterns for generating tests across all HQStudio components:
- API integration tests (xUnit + FluentAssertions)
- Desktop unit tests (xUnit)
- Web unit tests (Vitest)

## API Controller Tests

### Template: New Controller Test File

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Models;

namespace HQStudio.API.Tests;

public class {ControllerName}ControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public {ControllerName}ControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClient()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", 
            new { login = "admin", password = "admin123" });
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result!.Token);
        return _client;
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsOk()
    {
        // Arrange
        var client = await GetAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/{endpoint}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/{endpoint}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        var client = await GetAuthenticatedClient();
        var newItem = new { /* properties */ };

        // Act
        var response = await client.PostAsJsonAsync("/api/{endpoint}", newItem);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_ExistingItem_ReturnsOk()
    {
        // Arrange
        var client = await GetAuthenticatedClient();
        var updateData = new { /* properties */ };

        // Act
        var response = await client.PutAsJsonAsync("/api/{endpoint}/1", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        var client = await GetAuthenticatedClient();

        // Act
        var response = await client.DeleteAsync("/api/{endpoint}/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

internal class LoginResult
{
    public string Token { get; set; } = "";
}
```

### Existing Test Patterns

Reference existing tests for patterns:
- `AuthControllerTests.cs` - Authentication flow
- `ServicesControllerTests.cs` - CRUD operations
- `CallbacksControllerTests.cs` - Status updates
- `DashboardControllerTests.cs` - Statistics endpoints

## Desktop ViewModel Tests

### Template: ViewModel Test File

```csharp
using Xunit;
using FluentAssertions;
using HQStudio.ViewModels;
using HQStudio.Models;

namespace HQStudio.Desktop.Tests;

public class {ViewModelName}ViewModelTests
{
    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Act
        var vm = new {ViewModelName}ViewModel();

        // Assert
        vm.Items.Should().NotBeNull();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public void Property_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var vm = new {ViewModelName}ViewModel();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == "SelectedItem")
                propertyChanged = true;
        };

        // Act
        vm.SelectedItem = new Item();

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Command_CanExecute_ReturnsExpected()
    {
        // Arrange
        var vm = new {ViewModelName}ViewModel();

        // Act & Assert
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }
}
```

### Testing Services

```csharp
[Fact]
public void DataService_LoadData_ReturnsItems()
{
    // Arrange
    var service = DataService.Instance;

    // Act
    var items = service.Items;

    // Assert
    items.Should().NotBeNull();
}
```

## Web Component Tests

### Template: Vitest Test File

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest'

describe('{ComponentName}', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('initialization', () => {
    it('renders without crashing', () => {
      // Test implementation
    })
  })

  describe('user interactions', () => {
    it('handles click events', () => {
      // Test implementation
    })
  })
})
```

### Template: Utility Function Tests

```typescript
import { describe, it, expect } from 'vitest'
import { functionName } from '../lib/utils'

describe('functionName', () => {
  it('handles normal input', () => {
    expect(functionName('input')).toBe('expected')
  })

  it('handles edge cases', () => {
    expect(functionName('')).toBe('')
    expect(functionName(null)).toBe(null)
  })

  it('handles error cases', () => {
    expect(() => functionName(undefined)).toThrow()
  })
})
```

### Template: Store Tests

```typescript
import { describe, it, expect, beforeEach } from 'vitest'

describe('Store', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('initializes with default values', () => {
    // Test implementation
  })

  it('persists to localStorage', () => {
    // Test implementation
  })

  it('loads from localStorage', () => {
    // Test implementation
  })
})
```

## Test Generation Checklist

### For New API Endpoint

1. Create test file: `{Controller}ControllerTests.cs`
2. Add tests for:
   - [ ] GET all (with/without auth)
   - [ ] GET by ID (found/not found)
   - [ ] POST create (valid/invalid data)
   - [ ] PUT update (exists/not exists)
   - [ ] DELETE (exists/not exists)
   - [ ] Authorization checks

### For New ViewModel

1. Create test file: `{ViewModel}ViewModelTests.cs`
2. Add tests for:
   - [ ] Constructor initialization
   - [ ] Property change notifications
   - [ ] Command CanExecute logic
   - [ ] Command Execute behavior
   - [ ] Data loading

### For New Web Component

1. Create test file: `__tests__/{component}.test.ts`
2. Add tests for:
   - [ ] Render without errors
   - [ ] Props handling
   - [ ] User interactions
   - [ ] State changes

## Running Tests

```bash
# All tests
make test

# API tests only
make test-api

# Web tests only
make test-web

# Desktop tests only
make test-desktop

# With coverage
make test-coverage
```

## Test Conventions

- Use Arrange-Act-Assert pattern
- One assertion per test when possible
- Descriptive test names: `Method_Scenario_ExpectedResult`
- Mock external dependencies
- Use FluentAssertions for readable assertions
