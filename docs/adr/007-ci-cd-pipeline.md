# ADR-007: GitHub Actions CI/CD

**Дата**: 2024-03-01
**Статус**: Accepted

## Контекст

Необходимо было выбрать CI/CD платформу. Рассматривались:

1. **GitHub Actions** — встроен в GitHub
2. **GitLab CI** — встроен в GitLab
3. **Jenkins** — self-hosted
4. **CircleCI** — облачный
5. **Azure DevOps** — Microsoft

Требования:
- Интеграция с GitHub
- Бесплатный tier для open-source
- Поддержка Windows (для Desktop)
- Docker support

## Решение

Выбран **GitHub Actions** с несколькими workflows:

```
.github/workflows/
├── ci.yml                    # Тесты
├── release.yml               # Релизы
├── pages.yml                 # GitHub Pages
├── codeql.yml                # Security
└── dependabot-automerge.yml  # Auto-merge
```

## Последствия

### Положительные

- **Интеграция**: нативная интеграция с GitHub
- **Бесплатно**: unlimited для public repos
- **Матрица**: Ubuntu + Windows runners
- **Marketplace**: готовые actions
- **Secrets**: безопасное хранение токенов

### Отрицательные

- **Vendor lock-in**: привязка к GitHub
- **Лимиты**: ограничения на private repos
- **Отладка**: сложнее чем локальный Jenkins

### Нейтральные

- YAML конфигурация
- Параллельные jobs

## Связанные решения

- [ADR-008](008-semantic-release.md) — Semantic Release
- [ADR-001](001-monorepo-structure.md) — Монорепозиторий
