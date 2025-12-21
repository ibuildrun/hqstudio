# Coding Conventions

## Общие правила

### Именование
- **C#**: PascalCase для публичных членов, _camelCase для приватных полей
- **TypeScript/JS**: camelCase для переменных и функций, PascalCase для компонентов
- **Файлы**: kebab-case для конфигов, PascalCase для компонентов

### Комментарии
- Код должен быть самодокументируемым
- XML-документация для публичных API методов в C#
- JSDoc для сложных функций в TypeScript

---

## C# / .NET

### Структура файла
```csharp
using System;
using Microsoft.AspNetCore.Mvc;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

/// <summary>
/// Описание контроллера
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExampleController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Items.ToListAsync();
        return Ok(items);
    }
}
```

### Правила
- Использовать `var` когда тип очевиден
- Async/await для всех I/O операций
- Dependency Injection через конструктор
- Не использовать `#region`

---

## TypeScript / React

### Структура компонента
```tsx
'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'

interface Props {
  title: string
  onAction?: () => void
}

export default function ExampleComponent({ title, onAction }: Props) {
  const [isOpen, setIsOpen] = useState(false)

  const handleClick = () => {
    setIsOpen(!isOpen)
    onAction?.()
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="p-4 bg-white rounded-lg"
    >
      <h2 className="text-xl font-bold">{title}</h2>
      <button onClick={handleClick}>
        {isOpen ? 'Закрыть' : 'Открыть'}
      </button>
    </motion.div>
  )
}
```

### Правила
- `'use client'` только для интерактивных компонентов
- Типизация через `interface` (не `type` для props)
- Деструктуризация props в параметрах функции
- Tailwind классы в одну строку (до 3-4 классов)

---

## Git & Commits

### Conventional Commits
```
feat(api): добавлен endpoint для статистики
fix(web): исправлена навигация на мобильных
docs: обновлён README
refactor(desktop): упрощена логика синхронизации
test(api): добавлены тесты для CallbacksController
chore(deps): обновлены зависимости
ci: добавлен CodeQL анализ
```

### Правила
- Коммиты на русском или английском (консистентно в рамках PR)
- Один коммит = одно логическое изменение
- Не коммитить `.env`, `node_modules`, `bin/`, `obj/`

---

## Тестирование

### API Tests (xUnit)
```csharp
[Fact]
public async Task GetAll_WithAuth_ReturnsItems()
{
    // Arrange
    var client = await GetAuthenticatedClient();

    // Act
    var response = await client.GetAsync("/api/items");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var items = await response.Content.ReadFromJsonAsync<List<Item>>();
    items.Should().NotBeEmpty();
}
```

### Web Tests (Vitest)
```typescript
import { describe, it, expect } from 'vitest'
import { formatPrice } from '../lib/utils'

describe('formatPrice', () => {
  it('formats number with spaces', () => {
    expect(formatPrice(15000)).toBe('15 000 ₽')
  })
})
```

---

## Безопасность

### Обязательно
- Валидация всех входных данных
- Параметризованные запросы (EF Core делает автоматически)
- JWT токены с ограниченным сроком
- HTTPS в production

### Запрещено
- Хранить секреты в коде
- Логировать пароли и токены
- Отключать CORS в production
- Использовать `eval()` или `dangerouslySetInnerHTML`
