---
event: onFileSave
fileMatch: "HQStudio.Web/**/*.{ts,tsx}"
description: "Линтинг при сохранении"
---

# Lint on Save

При сохранении TypeScript/TSX файла в HQStudio.Web:

1. Запусти ESLint для изменённого файла:
   ```
   npm run lint --prefix HQStudio.Web -- --fix {filePath}
   ```
2. Если есть ошибки, которые нельзя автоисправить, покажи их список
3. Если всё чисто, сообщи "✅ Линтинг пройден"

Для C# файлов используй встроенную диагностику IDE (getDiagnostics).
