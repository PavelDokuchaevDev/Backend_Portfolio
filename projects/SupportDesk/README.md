# SupportDesk — обращения и SLA

REST API службы поддержки. Срок решения обращения определяется SLA-правилом из PostgreSQL. Новые приоритеты добавляются через API без изменения C#.

**Главная инженерная задача:** применить правило из данных и защитить состояние обращения от конкурентных изменений. При создании фиксируется DueAt, при смене статуса клиент передаёт Version. EF concurrency token защищает UPDATE в БД.

## Запуск из корня репозитория

```powershell
docker compose -f projects/SupportDesk/compose.yaml up --build -d
```

Swagger: [localhost:5103/swagger](http://localhost:5103/swagger).

## API

| Метод | Маршрут | Назначение |
|---|---|---|
| POST | `/api/policies` | Добавить приоритет и SLA в минутах |
| GET | `/api/policies` | Все правила |
| POST | `/api/tickets` | Создать обращение с существующим приоритетом |
| GET | `/api/tickets/{id}` | Прочитать обращение и Version |
| PATCH | `/api/tickets/{id}/status` | Сменить статус с проверкой версии |
| GET | `/api/tickets/overdue` | До 100 просроченных незавершённых обращений |

Примеры: [requests.http](requests.http). Правило с одинаковым приоритетом создать повторно нельзя. Используйте существующее правило при повторной демонстрации.

Разрешённая последовательность: `Open → InProgress → Resolved`. Пропуск шага или устаревшая Version возвращают 409. После успешного обновления используйте новую Version из ответа. Названия приоритетов нормализуются к нижнему регистру. Все временные отметки — UTC.

## Что смотреть в коде

- `Domain/Entities.cs`: вычисление DueAt и переходы состояний.
- `Application/AppService.cs`: поиск правила и TimeProvider через DI.
- `Infrastructure/AppDbContext.cs`: concurrency token и запрос просроченных обращений.
- `tests/SupportDesk.Tests/Scenarios.cs`: SLA, устаревшие версии, два DbContext и overdue-фильтр.

```powershell
dotnet test projects/SupportDesk/tests/SupportDesk.Tests -c Release
```

В модель не входят пользователи, назначения исполнителей, уведомления и редактор существующих политик. Времена SLA задаются календарными минутами без учёта рабочего графика.
