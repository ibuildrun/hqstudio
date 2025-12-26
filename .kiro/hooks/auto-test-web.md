---
event: onFileSave
fileMatch: "HQStudio.Web/lib/**/*.ts"
description: "Автотесты Web при сохранении"
---

# Auto Test Web

При сохранении TypeScript файла в HQStudio.Web/lib:

1. Определи имя изменённого файла (например, utils.ts → utils.test.ts)
2. Запусти соответствующий тест:
   ```
   npm test --prefix HQStudio.Web -- --run {testFile}
   ```
3. Если тесты упали, покажи ошибки
4. Если тесты прошли, сообщи "✅ Тесты пройдены"
