# HQ Studio - Запуск с Tuna туннелем
# Использование: .\scripts\start-tuna.ps1 [-Dev] [-Subdomain "your-subdomain"]

param(
    [switch]$Dev,
    [string]$Subdomain = "hqstudio"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 HQ Studio - Запуск с Tuna" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Проверяем наличие .env файла
if (-not (Test-Path ".env")) {
    Write-Host "⚠️  Файл .env не найден. Создаю из .env.example..." -ForegroundColor Yellow
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "📝 Отредактируйте .env файл и добавьте TUNA_TOKEN" -ForegroundColor Yellow
        Write-Host "   Получить токен: https://tuna.am" -ForegroundColor Gray
        exit 1
    }
}

# Устанавливаем поддомен
$env:TUNA_SUBDOMAIN = $Subdomain

if ($Dev) {
    Write-Host "🔧 Режим разработки" -ForegroundColor Green
    Write-Host "📦 Запуск docker-compose.dev.yml..." -ForegroundColor Gray
    docker-compose -f docker-compose.dev.yml up --build
} else {
    Write-Host "🏭 Продакшн режим" -ForegroundColor Green
    Write-Host "📦 Запуск docker-compose.yml..." -ForegroundColor Gray
    docker-compose up --build -d
    
    Write-Host ""
    Write-Host "✅ Сервисы запущены!" -ForegroundColor Green
    Write-Host "🌐 Локально: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "🌍 Публично: https://$Subdomain.tuna.am" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📊 Логи: docker-compose logs -f" -ForegroundColor Gray
    Write-Host "🛑 Остановка: docker-compose down" -ForegroundColor Gray
}
