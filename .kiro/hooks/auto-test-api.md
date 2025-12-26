---
event: onFileSave
fileMatch: "HQStudio.API/**/*.cs"
description: "Автотесты API при сохранении"
---

# Auto Test API

При сохранении C# файла в HQStudio.API:

1. Определи, какой контроллер или сервис был изменён
2. Найди соответствующие тесты в HQStudio.API.Tests
3. Запусти только релевантные тесты командой:
   ```
   dotnet test HQStudio.API.Tests --filter "FullyQualifiedName~{ControllerName}"
   ```
4. Если тесты упали, покажи краткую сводку ошибок
5. Если тесты прошли, просто сообщи "✅ Тесты пройдены"
