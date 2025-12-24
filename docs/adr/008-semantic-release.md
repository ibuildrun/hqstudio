# ADR-008: Semantic Release

**Дата**: 2025-03-15
**Статус**: Accepted

## Контекст

Необходимо было автоматизировать версионирование и релизы. Рассматривались:

1. **Semantic Release** — автоматическое версионирование
2. **Release Please** — Google's release automation
3. **Standard Version** — conventional changelog
4. **Ручное версионирование** — manual tags

Требования:
- Автоматическое определение версии
- Генерация CHANGELOG
- Создание GitHub Releases
- Conventional Commits

## Решение

Выбран **Semantic Release** с Conventional Commits.

Конфигурация:
- `feat:` → minor version (1.x.0)
- `fix:` → patch version (1.0.x)
- `feat!:` / `BREAKING CHANGE:` → major version (x.0.0)

Артефакты релиза:
- CHANGELOG.md
- GitHub Release
- Docker images (GHCR)
- Desktop ZIP

## Последствия

### Положительные

- **Автоматизация**: нет ручной работы с версиями
- **Консистентность**: версии следуют semver
- **CHANGELOG**: автоматическая генерация
- **Трассируемость**: связь коммитов с релизами

### Отрицательные

- **Дисциплина**: требуются правильные commit messages
- **Сложность**: настройка плагинов
- **Зависимость**: от npm экосистемы

### Нейтральные

- Husky + Commitlint для валидации коммитов
- Commitizen для интерактивных коммитов

## Связанные решения

- [ADR-007](007-ci-cd-pipeline.md) — GitHub Actions
