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

### Conventional Commits (на русском языке!)

**ВАЖНО:** Все сообщения коммитов должны быть на русском языке!

```
feat(api): добавлен endpoint для статистики
fix(web): исправлена навигация на мобильных
docs: обновлён README
refactor(desktop): упрощена логика синхронизации
test(api): добавлены тесты для CallbacksController
chore(deps): обновлены зависимости
ci: добавлен CodeQL анализ
```

### Типы коммитов и влияние на версию

| Тип | Описание | Релиз |
|-----|----------|-------|
| `feat` | Новая функциональность | **minor** (1.x.0) |
| `fix` | Исправление бага | **patch** (1.0.x) |
| `perf` | Улучшение производительности | **patch** |
| `refactor` | Рефакторинг кода | **patch** |
| `docs` | Документация | Без релиза |
| `style` | Форматирование | Без релиза |
| `test` | Тесты | Без релиза |
| `build` | Сборка/зависимости | Без релиза |
| `ci` | CI/CD конфигурация | Без релиза |
| `chore` | Прочие изменения | Без релиза |
| `revert` | Откат изменений | Зависит от типа |

### Области (Scopes)

| Scope | Описание |
|-------|----------|
| `api` | HQStudio.API (ASP.NET Core) |
| `web` | HQStudio.Web (Next.js) |
| `desktop` | HQStudio.Desktop (WPF) |
| `tests` | Тесты любого компонента |
| `docker` | Docker конфигурация |
| `ci` | CI/CD пайплайны |
| `deps` | Зависимости |
| `release` | Автоматические релизы |

### Правила коммитов

- Формат: `тип(область): описание на русском`
- Один коммит = одно логическое изменение
- Не коммитить: `.env`, `node_modules`, `bin/`, `obj/`, `publish/`
- Breaking changes: добавить `!` после типа и `BREAKING CHANGE:` в footer

### Интерактивный коммит

```bash
npm run commit  # Запуск Commitizen
```

### Git Hooks (Husky)

Автоматическая проверка коммитов через Commitlint:
```
.husky/
└── commit-msg    # npx --no -- commitlint --edit $1
```

### Проверка CI/CD

После пуша в main ОБЯЗАТЕЛЬНО проверять статус GitHub Actions:

```powershell
# PowerShell - проверка статуса последних workflow runs
Invoke-RestMethod -Uri "https://api.github.com/repos/randomu3/hqstudio/actions/runs?per_page=5" `
  -Headers @{Accept="application/vnd.github.v3+json"} | `
  Select-Object -ExpandProperty workflow_runs | `
  ForEach-Object { "$($_.name) | $($_.status) | $($_.conclusion)" }
```

```bash
# GitHub CLI
gh run list --limit 5
gh run view <run-id> --log-failed  # просмотр ошибок
```

Все 5 workflows должны быть `success`:
- **CI** - тесты API, Web, Desktop + Codecov
- **Release** - semantic-release, CHANGELOG, GitHub Release, Docker, Desktop
- **CodeQL** - анализ безопасности C# и JS/TS
- **Deploy to GitHub Pages** - деплой веб-приложения
- **Dependabot Auto-merge** - автомерж patch/minor updates

### Если CI падает

1. Проверить логи: `gh run view <run-id> --log-failed`
2. Исправить тесты локально:
   ```bash
   dotnet test HQStudio.API.Tests  # API тесты
   npm test --prefix HQStudio.Web  # Web тесты
   dotnet test HQStudio.Desktop.Tests --filter "Category!=Integration"  # Desktop
   ```
3. Сделать fix коммит и push

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
