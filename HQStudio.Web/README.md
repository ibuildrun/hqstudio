# HQ Studio - Тюнинг и шумоизоляция

Премиальный сайт автотюнинг студии на Next.js 14 с SSR для SEO.

## 🚀 Быстрый старт

### Локальная разработка

```bash
npm install
npm run dev
```

Сайт будет доступен на http://localhost:3000

## 🌐 Публичный доступ через Tuna

Tuna — российский сервис туннелирования (аналог ngrok).

### Шаг 1: Установка Tuna CLI

**Windows (PowerShell):**
```powershell
irm https://get.tuna.am | iex
```

**Linux/Mac:**
```bash
curl -fsSL https://get.tuna.am | sh
```

### Шаг 2: Авторизация

```bash
tuna login
```

Откроется браузер для входа в аккаунт tuna.am

### Шаг 3: Запуск туннеля

**Вариант A: Локально (рекомендуется для разработки)**

1. Запустите сайт:
```bash
npm run dev
```

2. В другом терминале запустите Tuna:

**Windows:**
```powershell
.\scripts\tuna-local.ps1 -Subdomain "hqstudio"
```

**Linux/Mac:**
```bash
./scripts/tuna-local.sh --subdomain hqstudio
```

Или напрямую:
```bash
tuna http 3000 --subdomain hqstudio
```

**Вариант B: Через Docker**

1. Создайте `.env` файл:
```bash
cp .env.example .env
```

2. Добавьте токен Tuna в `.env`:
```env
TUNA_TOKEN=ваш_токен
TUNA_SUBDOMAIN=hqstudio
```

3. Запустите:
```bash
# Продакшн
docker-compose up --build -d

# Разработка
docker-compose -f docker-compose.dev.yml up --build
```

### Результат

После запуска сайт будет доступен:
- Локально: http://localhost:3000
- Публично: https://hqstudio.tuna.am (или ваш поддомен)

## 📁 Структура проекта

```
HQStudio site/
├── app/                    # Next.js App Router
│   ├── layout.tsx          # Root layout с SEO метаданными
│   ├── page.tsx            # Главная страница (SSR)
│   ├── ClientPage.tsx      # Клиентская часть
│   ├── globals.css         # Глобальные стили
│   └── sitemap.ts          # Sitemap для SEO
├── components/             # React компоненты
├── lib/                    # Утилиты и константы
├── public/                 # Статические файлы
├── scripts/                # Скрипты запуска
├── Dockerfile              # Продакшн образ
└── docker-compose.yml      # Docker Compose
```

## 🔧 SEO оптимизации

- ✅ Server-Side Rendering (SSR)
- ✅ JSON-LD структурированные данные (Schema.org)
- ✅ Open Graph и Twitter Cards
- ✅ Автоматический sitemap.xml
- ✅ robots.txt
- ✅ Семантическая HTML разметка
- ✅ Мета-теги для Яндекс

## 🛠 Команды

```bash
npm run dev      # Разработка
npm run build    # Сборка
npm run start    # Продакшн сервер
```

## 🐳 Docker команды

```bash
docker-compose logs -f          # Логи
docker-compose down             # Остановка
docker-compose up --build -d    # Пересборка
```

## 📝 Переменные окружения

| Переменная | Описание |
|------------|----------|
| `TUNA_TOKEN` | Токен для Tuna туннеля |
| `TUNA_SUBDOMAIN` | Поддомен на tuna.am |
