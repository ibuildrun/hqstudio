---
event: onFileSave
fileMatch: "HQStudio.API/Models/**/*.cs"
description: "Напоминание о миграции"
---

# Migration Reminder

При сохранении модели в HQStudio.API/Models:

1. Проанализируй изменения в модели:
   - Добавлены новые свойства?
   - Изменены типы данных?
   - Добавлены/удалены атрибуты?
   
2. Если изменения влияют на схему БД, напомни:
   ```
   ⚠️ Обнаружены изменения в модели {ModelName}
   
   Не забудь создать миграцию:
   cd HQStudio.API
   dotnet ef migrations add {SuggestedMigrationName}
   dotnet ef database update
   ```

3. Предложи имя миграции на основе изменений (например: AddPhoneToClient, ChangeOrderStatus)

4. Если изменения не влияют на схему (только методы, комментарии), не показывай напоминание
