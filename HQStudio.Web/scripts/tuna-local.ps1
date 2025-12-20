# HQ Studio - Запуск Tuna локально (без Docker)
# Использование: .\scripts\tuna-local.ps1 -Subdomain "hqstudio"

param(
    [string]$Subdomain = "hqstudio",
    [int]$Port = 3000
)

$ErrorActionPreference = "Stop"

Write-Host "🌐 HQ Studio - Tuna Tunnel" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan

# Проверяем установлен ли tuna
$tunaPath = Get-Command tuna -ErrorAction SilentlyContinue

if (-not $tunaPath) {
    Write-Host "⚠️  Tuna CLI не найден!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Установите Tuna одним из способов:" -ForegroundColor White
    Write-Host ""
    Write-Host "1. Через PowerShell:" -ForegroundColor Gray
    Write-Host "   irm https://get.tuna.am | iex" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Скачайте с сайта:" -ForegroundColor Gray
    Write-Host "   https://tuna.am/download" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "После установки выполните:" -ForegroundColor Gray
    Write-Host "   tuna login" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

Write-Host "✅ Tuna CLI найден: $($tunaPath.Source)" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Запуск туннеля..." -ForegroundColor Yellow
Write-Host "   Локальный порт: $Port" -ForegroundColor Gray
Write-Host "   Поддомен: $Subdomain" -ForegroundColor Gray
Write-Host ""

# Запускаем tuna
tuna http $Port --subdomain $Subdomain

Write-Host ""
Write-Host "🌍 Ваш сайт доступен по адресу:" -ForegroundColor Green
Write-Host "   https://$Subdomain.tuna.am" -ForegroundColor Cyan
