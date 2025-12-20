#!/bin/bash
# HQ Studio - Запуск с Tuna туннелем
# Использование: ./scripts/start-tuna.sh [--dev] [--subdomain your-subdomain]

set -e

DEV_MODE=false
SUBDOMAIN="hqstudio"

while [[ $# -gt 0 ]]; do
    case $1 in
        --dev)
            DEV_MODE=true
            shift
            ;;
        --subdomain)
            SUBDOMAIN="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "🚀 HQ Studio - Запуск с Tuna"
echo "================================"

# Проверяем наличие .env файла
if [ ! -f ".env" ]; then
    echo "⚠️  Файл .env не найден. Создаю из .env.example..."
    if [ -f ".env.example" ]; then
        cp .env.example .env
        echo "📝 Отредактируйте .env файл и добавьте TUNA_TOKEN"
        echo "   Получить токен: https://tuna.am"
        exit 1
    fi
fi

export TUNA_SUBDOMAIN=$SUBDOMAIN

if [ "$DEV_MODE" = true ]; then
    echo "🔧 Режим разработки"
    echo "📦 Запуск docker-compose.dev.yml..."
    docker-compose -f docker-compose.dev.yml up --build
else
    echo "🏭 Продакшн режим"
    echo "📦 Запуск docker-compose.yml..."
    docker-compose up --build -d
    
    echo ""
    echo "✅ Сервисы запущены!"
    echo "🌐 Локально: http://localhost:3000"
    echo "🌍 Публично: https://$SUBDOMAIN.tuna.am"
    echo ""
    echo "📊 Логи: docker-compose logs -f"
    echo "🛑 Остановка: docker-compose down"
fi
