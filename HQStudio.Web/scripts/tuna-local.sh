#!/bin/bash
# HQ Studio - Запуск Tuna локально (без Docker)
# Использование: ./scripts/tuna-local.sh --subdomain hqstudio

SUBDOMAIN="hqstudio"
PORT=3000

while [[ $# -gt 0 ]]; do
    case $1 in
        --subdomain)
            SUBDOMAIN="$2"
            shift 2
            ;;
        --port)
            PORT="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "🌐 HQ Studio - Tuna Tunnel"
echo "=========================="

# Проверяем установлен ли tuna
if ! command -v tuna &> /dev/null; then
    echo "⚠️  Tuna CLI не найден!"
    echo ""
    echo "Установите Tuna:"
    echo ""
    echo "  curl -fsSL https://get.tuna.am | sh"
    echo ""
    echo "После установки выполните:"
    echo "  tuna login"
    echo ""
    exit 1
fi

echo "✅ Tuna CLI найден"
echo ""
echo "🚀 Запуск туннеля..."
echo "   Локальный порт: $PORT"
echo "   Поддомен: $SUBDOMAIN"
echo ""

# Запускаем tuna
tuna http $PORT --subdomain $SUBDOMAIN
