# Команды проекта

## Makefile (РЕКОМЕНДУЕТСЯ)

Проект использует Makefile для упрощения команд. **Всегда используй Makefile вместо прямых команд!**

```bash
# Справка по всем командам
make help

# Установка зависимостей
make install

# Разработка
make dev-api       # Запустить API
make dev-web       # Запустить Web
make dev-desktop   # Запустить Desktop

# Сборка
make build         # Собрать все
make build-api     # Собрать API
make build-web     # Собрать Web
make build-desktop # Собрать Desktop

# Тестирование
make test          # Все тесты
make test-api      # Тесты API
make test-web      # Тесты Web
make test-desktop  # Тесты Desktop
make test-coverage # Тесты с покрытием

# Линтинг
make lint          # Проверить код

# Docker
make docker-dev    # Development
make docker-prod   # Production
make docker-down   # Остановить
make docker-build  # Собрать образы

# Релиз
make release-dry   # Dry-run релиза
make commit        # Интерактивный коммит

# Утилиты
make clean         # Очистить артефакты
make ci-status     # Статус GitHub Actions
make db-migrate    # Миграции БД
```

## Прямые команды (если Makefile недоступен)

### API
```bash
cd HQStudio.API && dotnet run                    # Запуск
dotnet test HQStudio.API.Tests                   # Тесты
dotnet build HQStudio.API -c Release             # Сборка
```

### Web
```bash
cd HQStudio.Web && npm run dev                   # Запуск
cd HQStudio.Web && npm test -- --run             # Тесты
cd HQStudio.Web && npm run build                 # Сборка
cd HQStudio.Web && npm run lint                  # Линтинг
```

### Desktop
```bash
cd HQStudio.Desktop && dotnet run                # Запуск
dotnet test HQStudio.Desktop.Tests               # Тесты
dotnet build HQStudio.Desktop -c Release         # Сборка
```

### Git
```bash
npm run commit                                   # Интерактивный коммит
npm run release:dry                              # Dry-run релиза
```

## Правила

1. **Используй Makefile** — он стандартизирует команды
2. **Перед коммитом** — запусти `make test` и `make lint`
3. **Для коммитов** — используй `make commit` (Commitizen)
4. **Проверяй CI** — после push запусти `make ci-status`
