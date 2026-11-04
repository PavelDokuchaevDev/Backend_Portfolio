# Результаты проверки

Проверено локально 4 ноября 2026 года на Windows с .NET SDK 8.0.413 и Docker Desktop (Linux containers).

| Проверка | Результат |
|---|---|
| Сборка Release | Успешно, без предупреждений и ошибок |
| Быстрые тесты с SQLite | 18 passed, 1 skipped — PostgreSQL concurrency test |
| StockFlow на PostgreSQL 16 | 7 passed, включая 8 параллельных запросов на последнюю единицу |
| ExpenseLens на PostgreSQL 16 | 7 passed |
| SupportDesk на PostgreSQL 16 | 5 passed |
| Соответствие миграций модели | Нет pending model changes во всех проектах |
| Docker Compose config | Валидна для трёх стеков |
| Docker build и запуск | Все три API и PostgreSQL успешно запущены |
| Применение миграций | Все три одноразовых migration-контейнера завершились успешно |
| HTTP readiness и Swagger | 200, OpenAPI доступен для каждого API |

Тесты PostgreSQL запускались через `tools/Test-Postgres.ps1` в отдельных временных базах. После проверки эти базы удалены; основные базы приложений сохранены. Подробные локальные логи находятся в игнорируемой Git папке `TestResults`.

Для повторения:

```powershell
dotnet test Portfolio.sln -c Release
./tools/Test-Postgres.ps1 -Project StockFlow
./tools/Test-Postgres.ps1 -Project ExpenseLens
./tools/Test-Postgres.ps1 -Project SupportDesk
```

Workflow GitHub Actions подготовлен; удалённый запуск CI и публикация репозитория не выполнялись.
