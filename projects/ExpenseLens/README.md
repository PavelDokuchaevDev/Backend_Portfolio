# ExpenseLens — расходы и бюджеты

REST API для учёта расходов по категориям и анализа бюджета за месяц. Категории и бюджеты находятся в БД, отчёт строится из фактических расходов.

**Главная инженерная задача:** корректно рассчитать расходы в границах месяца, сохранить категории без операций и показать превышение бюджета. Агрегация выполняется в PostgreSQL, деньги хранятся как `numeric(18,2)`, дата расхода — как `date`.

## Запуск из корня репозитория

```powershell
docker compose -f projects/ExpenseLens/compose.yaml up --build -d
```

Swagger: [localhost:5102/swagger](http://localhost:5102/swagger).

## API

| Метод | Маршрут | Назначение |
|---|---|---|
| POST | `/api/categories` | Создать категорию и месячный бюджет |
| GET | `/api/categories` | Получить категории |
| GET | `/api/categories/{id}` | Получить категорию |
| POST | `/api/expenses` | Добавить расход |
| GET | `/api/expenses/{id}` | Получить расход |
| GET | `/api/reports/monthly?year=2026&month=11` | Сумма и остаток бюджета по категориям |

Примеры: [requests.http](requests.http). При повторном создании категории с тем же именем PostgreSQL вернёт 409; используйте новую категорию или ID существующей.

Демо: бюджет 100, расходы 60 и 55 → Spent = 115, Remaining = -15, OverBudget = true. Расходы за соседний месяц не попадут в отчёт. Категория без расходов вернёт Spent = 0.

## Что смотреть в коде

- `Domain/Entities.cs`: положительные суммы, точность до двух знаков, обязательные поля.
- `Infrastructure/AppDbContext.cs`: SQL-агрегация Report и индекс `(CategoryId, Date)`.
- `tests/ExpenseLens.Tests/Scenarios.cs`: границы периода, пустые категории и превышение бюджета.

```powershell
dotnet test projects/ExpenseLens/tests/ExpenseLens.Tests -c Release
```

Все суммы в одной условной валюте; пользователи, обмен валют, возвраты и история изменения бюджетов не входят в модель.
