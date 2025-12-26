---
event: onFileSave
fileMatch: "HQStudio.API/Controllers/**/*.cs"
description: "Обновление документации API"
---

# Update API Docs

При сохранении контроллера в HQStudio.API/Controllers:

1. Прочитай изменённый контроллер
2. Извлеки информацию об endpoints:
   - HTTP метод (GET, POST, PUT, DELETE)
   - Route
   - Параметры
   - Атрибуты авторизации
3. Обнови соответствующую секцию в docs/API.md
4. Сохрани формат документации:
   ```markdown
   ### {ControllerName}
   
   | Метод | Endpoint | Описание | Авторизация |
   |-------|----------|----------|-------------|
   | GET | /api/... | ... | ... |
   ```
5. Сообщи какие endpoints были обновлены
