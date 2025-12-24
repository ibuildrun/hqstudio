# HQ Studio Makefile
# Упрощённые команды для разработки

.PHONY: help install dev build test lint clean docker release

# Цвета для вывода
CYAN := \033[36m
GREEN := \033[32m
YELLOW := \033[33m
RESET := \033[0m

help: ## Показать справку
	@echo "$(CYAN)HQ Studio - Доступные команды:$(RESET)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-15s$(RESET) %s\n", $$1, $$2}'

# ===========================================
# Установка зависимостей
# ===========================================

install: ## Установить все зависимости
	@echo "$(CYAN)Установка зависимостей...$(RESET)"
	npm install
	cd HQStudio.Web && npm install --legacy-peer-deps
	dotnet restore HQStudio.API/HQStudio.API.csproj
	dotnet restore HQStudio.Desktop/HQStudio.csproj
	dotnet restore HQStudio.API.Tests/HQStudio.API.Tests.csproj
	dotnet restore HQStudio.Desktop.Tests/HQStudio.Desktop.Tests.csproj
	@echo "$(GREEN)✓ Зависимости установлены$(RESET)"

# ===========================================
# Разработка
# ===========================================

dev-api: ## Запустить API (development)
	cd HQStudio.API && dotnet run

dev-web: ## Запустить Web (development)
	cd HQStudio.Web && npm run dev

dev-desktop: ## Запустить Desktop (development)
	cd HQStudio.Desktop && dotnet run

# ===========================================
# Сборка
# ===========================================

build: build-api build-web build-desktop ## Собрать все проекты

build-api: ## Собрать API
	@echo "$(CYAN)Сборка API...$(RESET)"
	dotnet build HQStudio.API/HQStudio.API.csproj -c Release
	@echo "$(GREEN)✓ API собран$(RESET)"

build-web: ## Собрать Web
	@echo "$(CYAN)Сборка Web...$(RESET)"
	cd HQStudio.Web && npm run build
	@echo "$(GREEN)✓ Web собран$(RESET)"

build-desktop: ## Собрать Desktop
	@echo "$(CYAN)Сборка Desktop...$(RESET)"
	dotnet build HQStudio.Desktop/HQStudio.csproj -c Release
	@echo "$(GREEN)✓ Desktop собран$(RESET)"

# ===========================================
# Тестирование
# ===========================================

test: test-api test-web test-desktop ## Запустить все тесты

test-api: ## Запустить тесты API
	@echo "$(CYAN)Тесты API...$(RESET)"
	dotnet test HQStudio.API.Tests --verbosity minimal
	@echo "$(GREEN)✓ Тесты API пройдены$(RESET)"

test-web: ## Запустить тесты Web
	@echo "$(CYAN)Тесты Web...$(RESET)"
	cd HQStudio.Web && npm test -- --run
	@echo "$(GREEN)✓ Тесты Web пройдены$(RESET)"

test-desktop: ## Запустить тесты Desktop
	@echo "$(CYAN)Тесты Desktop...$(RESET)"
	dotnet test HQStudio.Desktop.Tests --verbosity minimal --filter "Category!=Integration"
	@echo "$(GREEN)✓ Тесты Desktop пройдены$(RESET)"

test-coverage: ## Запустить тесты с покрытием
	@echo "$(CYAN)Тесты с покрытием...$(RESET)"
	dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"
	cd HQStudio.Web && npm test -- --coverage
	@echo "$(GREEN)✓ Отчёты покрытия сгенерированы$(RESET)"

# ===========================================
# Линтинг
# ===========================================

lint: lint-web ## Проверить код линтером

lint-web: ## Линтинг Web
	@echo "$(CYAN)Линтинг Web...$(RESET)"
	cd HQStudio.Web && npm run lint
	@echo "$(GREEN)✓ Линтинг пройден$(RESET)"

# ===========================================
# Docker
# ===========================================

docker-dev: ## Запустить Docker (development)
	docker-compose -f docker-compose.dev.yml up --build

docker-prod: ## Запустить Docker (production)
	docker-compose up --build -d

docker-down: ## Остановить Docker
	docker-compose down
	docker-compose -f docker-compose.dev.yml down

docker-build: ## Собрать Docker образы
	@echo "$(CYAN)Сборка Docker образов...$(RESET)"
	docker build -t hqstudio-api:local ./HQStudio.API
	docker build -t hqstudio-web:local ./HQStudio.Web
	@echo "$(GREEN)✓ Docker образы собраны$(RESET)"

# ===========================================
# Релиз
# ===========================================

release-dry: ## Dry-run релиза
	npm run release:dry

commit: ## Интерактивный коммит (Commitizen)
	npm run commit

# ===========================================
# Утилиты
# ===========================================

clean: ## Очистить артефакты сборки
	@echo "$(CYAN)Очистка...$(RESET)"
	rm -rf HQStudio.API/bin HQStudio.API/obj
	rm -rf HQStudio.Desktop/bin HQStudio.Desktop/obj
	rm -rf HQStudio.API.Tests/bin HQStudio.API.Tests/obj
	rm -rf HQStudio.Desktop.Tests/bin HQStudio.Desktop.Tests/obj
	rm -rf HQStudio.Web/.next HQStudio.Web/out HQStudio.Web/node_modules/.cache
	@echo "$(GREEN)✓ Очистка завершена$(RESET)"

ci-status: ## Проверить статус CI
	@echo "$(CYAN)Статус GitHub Actions:$(RESET)"
	@curl -s "https://api.github.com/repos/randomu3/hqstudio/actions/runs?per_page=5" | \
		jq -r '.workflow_runs[] | "\(.name) | \(.status) | \(.conclusion)"'

db-migrate: ## Применить миграции БД
	cd HQStudio.API && dotnet ef database update

db-seed: ## Заполнить БД тестовыми данными
	cd HQStudio.API && dotnet run -- --seed
