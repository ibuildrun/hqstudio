---
inclusion: manual
---

# Self-Hosted GitHub Actions Runner

## Статус
Runner установлен на локальной машине для выполнения задач, требующих GUI (скриншоты Desktop приложения).

## Расположение
```
C:\actions-runner\
```

## Запуск Runner'а

**ВАЖНО:** После перезагрузки ПК runner нужно запустить вручную!

```powershell
C:\actions-runner\run.cmd
```

Окно PowerShell должно оставаться открытым пока runner работает.

## Проверка статуса
- GitHub: Settings → Actions → Runners
- Должен быть статус "Idle" (зелёный)
- Имя runner'а: `hqstudio-local`
- Labels: `self-hosted`, `Windows`, `X64`, `screenshots`

## Если runner не запущен
Workflows с `runs-on: self-hosted` будут висеть в очереди "Waiting for a runner".

## Автозапуск (опционально)
Чтобы runner запускался автоматически при старте Windows:

1. Открыть Task Scheduler (taskschd.msc)
2. Create Basic Task → "GitHub Actions Runner"
3. Trigger: "When the computer starts"
4. Action: Start a program
   - Program: `C:\actions-runner\run.cmd`
   - Start in: `C:\actions-runner`
5. Finish

## Безопасность
⚠️ Public репозиторий + self-hosted runner = риск. Злоумышленник может создать PR с вредоносным кодом, который выполнится на твоём ПК.

Рекомендации:
- Не принимать PR от незнакомых людей без review
- Использовать `pull_request_target` вместо `pull_request` для workflows
- Или сделать репозиторий приватным
