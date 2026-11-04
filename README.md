# C# / .NET Backend Portfolio

Три небольших самостоятельных REST API с разными бизнес-задачами. Стек: **C# 12, .NET 8, ASP.NET Core Minimal API, PostgreSQL 16, EF Core 8, Docker Compose, xUnit, GitHub Actions**.

| Проект | Задача | Что демонстрирует |
|---|---|---|
| [StockFlow](projects/StockFlow/README.md) | Товары, заказы, остатки | Транзакции, защита от overselling и повторного возврата остатков |
| [ExpenseLens](projects/ExpenseLens/README.md) | Расходы и бюджеты | LINQ → SQL, отчёт за месяц, точные денежные расчёты |
| [SupportDesk](projects/SupportDesk/README.md) | Обращения и SLA | Data-driven правила из БД, переходы состояний, optimistic concurrency |

## Запуск

Нужен запущенный Docker Engine с Linux containers. Из корня репозитория:

```powershell
docker compose -f projects/StockFlow/compose.yaml up --build -d
docker compose -f projects/ExpenseLens/compose.yaml up --build -d
docker compose -f projects/SupportDesk/compose.yaml up --build -d
```

Каждая команда поднимает отдельные PostgreSQL и API. Одноразовый сервис `migrate` применяет миграции до запуска API. Данные сохраняются в отдельных Docker volumes. Стандартный пароль `portfolio_dev`; его можно переопределить переменной `POSTGRES_PASSWORD` до запуска. Swagger включён в Development.

Для остановки одного проекта:

```powershell
docker compose -f projects/StockFlow/compose.yaml down
```

## Сборка и тесты без Docker

Нужен .NET SDK 8.0.413 или более новый SDK ветки 8.0.

```powershell
dotnet restore Portfolio.sln
dotnet build Portfolio.sln -c Release --no-restore
dotnet test Portfolio.sln -c Release --no-build
```

Быстрые интеграционные тесты запускают настоящий HTTP pipeline через `WebApplicationFactory` и SQLite in-memory. Для SQLite в тестовом хосте деньги хранятся в целых копейках, даты — в UTC ticks; модель PostgreSQL остаётся без этих преобразований. Эти тесты не заменяют проверку PostgreSQL: CI отдельно применяет реальные миграции и запускает те же сценарии на PostgreSQL, включая параллельное списание остатков. Без PostgreSQL этот сценарий явно помечается `Skipped`.

Для проверки с PostgreSQL нужно задать подключение к **отдельной тестовой базе**, после чего запустить тесты одного проекта.

```powershell
$env:PORTFOLIO_TEST_DATABASE = 'Host=localhost;Database=stockflow_test;Username=portfolio;Password=test_password'
dotnet test projects/StockFlow/tests/StockFlow.Tests -c Release
Remove-Item Env:PORTFOLIO_TEST_DATABASE
```

У каждого проекта должна быть своя тестовая база. CI создаёт изолированную базу для каждого задания, а также собирает Docker-образы и проверяет `/health/ready` и Swagger.

Либо запустите тесты прямо внутри Docker из PowerShell:

```powershell
./tools/Test-Postgres.ps1 -Project StockFlow
./tools/Test-Postgres.ps1 -Project ExpenseLens
./tools/Test-Postgres.ps1 -Project SupportDesk
```

Скрипт поднимает стек, создаёт временную тестовую базу, запускает тесты в SDK-контейнере и удаляет только эту временную базу. API и его основная база остаются запущенными для демонстрации.
